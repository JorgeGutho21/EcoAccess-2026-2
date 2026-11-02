using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace EcoAccess.P3.Bot
{

    /// <summary>
    /// Escucha HTTP local para <see cref="ReceptorWebhook"/> con <see cref="HttpListener"/>. Telegram exige HTTPS
    /// público: el TLS lo termina un proxy inverso (nginx, Caddy, túnel) que reenvía a este prefijo local, por
    /// ejemplo <c>http://127.0.0.1:8080/telegram/</c>. Hasta ocho peticiones avanzan en paralelo;
    /// el diálogo garantiza exclusión/versionado por persona; el orden de entrega de la red no se presupone.
    /// El secreto se revisa antes del cuerpo y su lectura tiene plazo.
    /// </summary>
    public sealed class ServidorWebhook
    {
        private static readonly TimeSpan PlazoLecturaPredeterminado = TimeSpan.FromSeconds(10);

        private readonly ReceptorWebhook _receptor;
        private readonly string _prefijo;
        private readonly Action<string> _registro;
        private readonly TimeSpan _plazoLectura;

        public ServidorWebhook(ReceptorWebhook receptor, string prefijo, Action<string> registro)
            : this(receptor, prefijo, registro, PlazoLecturaPredeterminado)
        {
        }

        /// <summary>Las pruebas acortan el plazo de lectura para no esperar 10 segundos.</summary>
        internal ServidorWebhook(ReceptorWebhook receptor, string prefijo, Action<string> registro, TimeSpan plazoLectura)
        {
            _receptor = receptor ?? throw new ArgumentNullException(nameof(receptor));
            _prefijo = Compat.IsNullOrWhiteSpace(prefijo) ? throw new ArgumentException("Falta el prefijo.", nameof(prefijo)) : prefijo;
            _registro = RegistroTecnicoSeguro.Proteger(registro);
            _plazoLectura = plazoLectura > TimeSpan.Zero ? plazoLectura : throw new ArgumentOutOfRangeException(nameof(plazoLectura));
        }

        /// <summary>Atiende hasta que se cancela; un fallo de una petición o del escucha no detiene el servidor.</summary>
        public async Task EjecutarAsync(CancellationToken cancellationToken)
        {
            using var escucha = new HttpListener();
            escucha.Prefixes.Add(_prefijo);
            escucha.Start();
            _registro($"Webhook escuchando en {_prefijo}");
            using CancellationTokenRegistration alCancelar = cancellationToken.Register(escucha.Stop);
            using var cupos = new SemaphoreSlim(8, 8);
            var pendientes = new HashSet<Task>();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext contexto;
                    try
                    {
                        contexto = await escucha.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (HttpListenerException error)
                    {
                        _registro($"Webhook: no se pudo recibir una petición (código {error.ErrorCode}).");
                        continue;
                    }

                    if (!cupos.Wait(0))
                    {
                        try
                        {
                            contexto.Response.StatusCode = 503;
                            contexto.Response.Headers["Retry-After"] = "1";
                            contexto.Response.Close();
                        }
                        catch (Exception error) when (error is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                        { _registro("Webhook: el cliente abandonó una petición sin cupo."); }
                        continue;
                    }
                    Task tarea = Task.Run(async () =>
                    {
                        try { await AtenderAsync(contexto, cancellationToken).ConfigureAwait(false); }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                        catch (Exception error) { _registro($"Webhook: atención finalizada con error ({error.GetType().Name})."); }
                        finally { cupos.Release(); }
                    }, CancellationToken.None);
                    lock (pendientes) pendientes.Add(tarea);
                    _ = tarea.ContinueWith(t => { lock (pendientes) pendientes.Remove(t); }, CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
            }
            finally
            {
                Task[] drenaje;
                lock (pendientes) drenaje = pendientes.ToArray();
                await Task.WhenAll(drenaje).ConfigureAwait(false);
            }
        }

        private async Task AtenderAsync(HttpListenerContext contexto, CancellationToken cancellationToken)
        {
            HttpListenerResponse respuesta = contexto.Response;
            try
            {
                if (contexto.Request.HttpMethod != "POST")
                {
                    respuesta.StatusCode = 405;
                    return;
                }

                string? secreto = contexto.Request.Headers[ReceptorWebhook.EncabezadoSecreto];
                if (!_receptor.SecretoCorrecto(secreto))
                {
                    respuesta.StatusCode = 401;
                    return;
                }

                if (contexto.Request.ContentLength64 > ReceptorWebhook.TamanoMaximo)
                {
                    respuesta.StatusCode = 413;
                    return;
                }

                byte[] cuerpo;
                using (var plazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    plazo.CancelAfter(_plazoLectura);
                    try
                    {
                        cuerpo = await LeerAsync(contexto.Request.InputStream, plazo.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        _registro("Webhook: el cliente no envió el cuerpo a tiempo.");
                        respuesta.StatusCode = 408;
                        return;
                    }
                }

                respuesta.StatusCode = await _receptor.RecibirAsync(secreto, cuerpo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _registro($"Webhook: petición no atendida ({error.GetType().Name}).");
                FijarCodigoSiSePuede(respuesta, 500);
            }
            finally
            {
                try
                {
                    respuesta.Close();
                }
                catch (Exception error) when (error is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    // El cliente ya se fue; no hay a quién responder.
                }
            }
        }

        private static void FijarCodigoSiSePuede(HttpListenerResponse respuesta, int codigo)
        {
            try
            {
                respuesta.StatusCode = codigo;
            }
            catch (InvalidOperationException)
            {
                // Los encabezados ya salieron.
            }
        }

        /// <summary>
        /// Lee como máximo <see cref="ReceptorWebhook.TamanoMaximo"/> + 1 bytes para detectar un cuerpo excesivo.
        /// WaitAsync hace efectivo el plazo aunque el flujo de HttpListener no atienda la cancelación.
        /// </summary>
        private static async Task<byte[]> LeerAsync(Stream entrada, CancellationToken cancellationToken)
        {
            using var memoria = new MemoryStream();
            byte[] bufer = new byte[8192];
            int leidos;
            while ((leidos = await entrada.ReadAsync(bufer, cancellationToken).AsTask().WaitAsync(cancellationToken).ConfigureAwait(false)) > 0)
            {
                memoria.Write(bufer, 0, leidos);
                if (memoria.Length > ReceptorWebhook.TamanoMaximo)
                {
                    break;
                }
            }

            return memoria.ToArray();
        }
    }
}
