using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Dispositivos
{

    // Piloto exclusivamente local: la elección de HTTPS/MQTT del ESP32 físico sigue pendiente.
    internal static class DireccionDispositivoLocal
    {
        internal static void Validar(Uri direccion)
        {
            Compat.ThrowIfNull(direccion, nameof(direccion));
            if (!direccion.IsAbsoluteUri || direccion.Scheme != Uri.UriSchemeHttp || direccion.Host != "127.0.0.1"
                || direccion.Port < 1024 || direccion.UserInfo.Length != 0 || direccion.Query.Length != 0 || direccion.Fragment.Length != 0
                || direccion.AbsolutePath is not ("/comandos/" or "/eventos/" or "/vigencia/"))
                throw new ArgumentException("El piloto requiere HTTP en 127.0.0.1, puerto no privilegiado y ruta /comandos/, /eventos/ o /vigencia/.");
        }

        internal static async Task<byte[]> LeerAsync(Stream origen, CancellationToken ct)
        {
            using var copia = new MemoryStream();
            byte[] bloque = new byte[1024];
            while (true)
            {
                int n = await origen.ReadAsync(bloque.AsMemory(), ct).AsTask().WaitAsync(ct);
                if (n == 0) return copia.ToArray();
                if (copia.Length + n > ProtocoloDispositivoHmac.MaximoPaquete)
                    throw new InvalidDataException("Mensaje de dispositivo demasiado grande.");
                copia.Write(bloque, 0, n);
            }
        }
    }

    /// <summary>Entrega paquetes a un endpoint local y solo acepta recibos HMAC del mensaje exacto.</summary>
    public sealed class CanalDispositivoHttp : ICanalDispositivo, IDisposable
    {
        private readonly Uri _direccion;
        private readonly ProtocoloDispositivoHmac _protocolo;
        private readonly CredencialDispositivo _credencial;
        private readonly HttpClient _cliente;
        private readonly bool _redReal;
        public string Canal => "http-loopback-hmac";

        public CanalDispositivoHttp(Uri direccion, ProtocoloDispositivoHmac protocolo, CredencialDispositivo credencial)
            : this(direccion, protocolo, credencial, new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false, MaxConnectionsPerServer = 1, MaxResponseHeadersLength = 8 }) { }

        internal CanalDispositivoHttp(Uri direccion, ProtocoloDispositivoHmac protocolo,
            CredencialDispositivo credencial, HttpMessageHandler manejador)
        {
            try
            {
                DireccionDispositivoLocal.Validar(direccion);
                Compat.ThrowIfNull(protocolo, nameof(protocolo));
                Compat.ThrowIfNull(credencial, nameof(credencial));
            }
            catch { manejador.Dispose(); throw; }
            _direccion = direccion; _protocolo = protocolo; _credencial = credencial;
            _redReal = manejador is HttpClientHandler;
            _cliente = new HttpClient(manejador, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
        }

        public async Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_direccion.AbsolutePath != "/comandos/") throw new InvalidOperationException("Este endpoint no recibe comandos.");
            var recibido = await EnviarPaqueteAsync(_protocolo.FirmarComando(_credencial, comando), cancellationToken);
            return recibido.Exitoso && recibido.Valor.Aceptado;
        }

        public async Task<Resultado<RecepcionFirmable>> EnviarPaqueteAsync(byte[] paquete, CancellationToken ct = default)
        {
            Compat.ThrowIfNull(paquete, nameof(paquete));
            ct.ThrowIfCancellationRequested();
            if (paquete.Length is 0 or > ProtocoloDispositivoHmac.MaximoPaquete)
                throw new ArgumentException("Paquete de dispositivo inválido.");
            // Por la red real, un proceso bloqueado solo puede enviar COMANDOS a servidores de dispositivo que él mismo levantó.
            // Eventos y consultas de vigencia van del dispositivo al bridge y no mueven ninguna tapa: no se bloquean (la
            // regresión de integración del 12/11/2026 mostró que bloquearlos rompía el simulador entre procesos).
            if (_redReal && _direccion.AbsolutePath == "/comandos/") GuardiaHardware.ExigirDestinoSeguro(_direccion);
            byte[] original = paquete.ToArray();
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(TimeSpan.FromSeconds(5));
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, _direccion);
            solicitud.Content = new ByteArrayContent(original);
            solicitud.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var respuesta = await _cliente.SendAsync(solicitud, HttpCompletionOption.ResponseHeadersRead, limite.Token);
            if (respuesta.StatusCode != HttpStatusCode.OK || respuesta.Content.Headers.ContentLength > ProtocoloDispositivoHmac.MaximoPaquete)
                return Resultado<RecepcionFirmable>.Rechazo(MotivoRechazo.SinPermiso, "El transporte no confirmó el mensaje.");
            using var entrada = await respuesta.Content.ReadAsStreamAsync(limite.Token);
            byte[] recibo = await DireccionDispositivoLocal.LeerAsync(entrada, limite.Token);
            return _protocolo.ValidarRecepcion(recibo, original);
        }

        public async Task<bool> ConsultarVigenciaAsync(ComandoApertura comando, CancellationToken ct = default)
        {
            if (_direccion.AbsolutePath != "/vigencia/") throw new InvalidOperationException("Este endpoint no consulta vigencia.");
            // Un nonce nuevo por consulta impide reutilizar un recibo anterior como autorización actual.
            var resultado = await EnviarPaqueteAsync(_protocolo.FirmarConsultaVigencia(_credencial, comando), ct);
            if (!resultado.Exitoso) throw new IOException("La autoridad no confirmó la vigencia.");
            return resultado.Valor.Aceptado;
        }

        public void Dispose() => _cliente.Dispose();
    }

    public sealed record RespuestaHttpDispositivo(int Estado, byte[] Cuerpo)
    {
        public static RespuestaHttpDispositivo Rechazar() => new(401, Array.Empty<byte>());
    }

    /// <summary>Servidor local acotado, sin SQL; el callback autentica y delega a los servicios existentes.</summary>
    public sealed class ServidorMensajesDispositivoHttp : IDisposable
    {
        private readonly HttpListener _escucha = new();
        private readonly int _puerto;
        private readonly SemaphoreSlim _procesamiento = new(1, 1);
        private readonly Dictionary<string, Func<byte[], CancellationToken, Task<RespuestaHttpDispositivo>>> _rutas = new(StringComparer.Ordinal);

        public ServidorMensajesDispositivoHttp(Uri direccion, Func<byte[], CancellationToken, Task<RespuestaHttpDispositivo>> procesar,
            params (Uri Direccion, Func<byte[], CancellationToken, Task<RespuestaHttpDispositivo>> Procesar)[] adicionales)
        {
            DireccionDispositivoLocal.Validar(direccion);
            _puerto = direccion.Port;
            Agregar(direccion, procesar);
            foreach (var ruta in adicionales)
            {
                DireccionDispositivoLocal.Validar(ruta.Direccion);
                if (ruta.Direccion.Port != direccion.Port) throw new ArgumentException("Las rutas del servidor deben compartir puerto.");
                Agregar(ruta.Direccion, ruta.Procesar);
            }
            void Agregar(Uri uri, Func<byte[], CancellationToken, Task<RespuestaHttpDispositivo>> callback)
            {
                Compat.ThrowIfNull(callback, nameof(callback));
                _rutas.Add(uri.AbsolutePath, callback); _escucha.Prefixes.Add(uri.AbsoluteUri);
            }
        }

        public async Task EjecutarAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _escucha.Start();
            GuardiaHardware.RegistrarPuertoPropio(_puerto);
            using var cerrar = ct.Register(() => _escucha.Close());
            try
            {
                // Una solicitud a la vez y cinco segundos por solicitud: memoria y concurrencia acotadas.
                while (!ct.IsCancellationRequested)
                {
                    var contexto = await _escucha.GetContextAsync().WaitAsync(ct);
                    await AtenderAsync(contexto, ct);
                }
            }
            catch (Exception) when (ct.IsCancellationRequested) { }
            finally
            {
                GuardiaHardware.LiberarPuertoPropio(_puerto);
                _escucha.Close();
                // No liberar la sesión del dispositivo mientras una callback anterior aún pueda escribir.
                await _procesamiento.WaitAsync();
                _procesamiento.Release();
            }
        }

        private async Task AtenderAsync(HttpListenerContext contexto, CancellationToken ct)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                contexto.Response.KeepAlive = false;
                if (contexto.Request.HttpMethod != "POST") { contexto.Response.StatusCode = 405; return; }
                var solicitada = contexto.Request.Url;
                if (solicitada is null || !_rutas.TryGetValue(solicitada.AbsolutePath, out var procesar) || solicitada.Query.Length != 0)
                { contexto.Response.StatusCode = 404; return; }
                if (contexto.Request.ContentLength64 > ProtocoloDispositivoHmac.MaximoPaquete)
                { contexto.Response.StatusCode = 413; return; }
                if (!string.Equals(contexto.Request.ContentType?.Split(';')[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
                { contexto.Response.StatusCode = 415; return; }
                byte[] paquete = await DireccionDispositivoLocal.LeerAsync(contexto.Request.InputStream, limite.Token);
                if (paquete.Length == 0) { contexto.Response.StatusCode = 400; return; }
                await _procesamiento.WaitAsync(limite.Token);
                Task<RespuestaHttpDispositivo> tarea;
                try { tarea = procesar(paquete, limite.Token) ?? throw new InvalidOperationException("Procesador inválido."); }
                catch { _procesamiento.Release(); throw; }
                // Si una dependencia ignora la cancelación, conserva el cupo hasta que termine realmente.
                _ = tarea.ContinueWith(t => { _ = t.Exception; _procesamiento.Release(); }, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                var respuesta = await tarea.WaitAsync(limite.Token);
                if (respuesta.Estado is < 200 or > 599 || respuesta.Cuerpo.Length > ProtocoloDispositivoHmac.MaximoPaquete)
                    throw new InvalidDataException("Respuesta de dispositivo inválida.");
                contexto.Response.StatusCode = respuesta.Estado;
                contexto.Response.ContentType = "application/json";
                contexto.Response.ContentLength64 = respuesta.Cuerpo.Length;
                await contexto.Response.OutputStream.WriteAsync(respuesta.Cuerpo, limite.Token);
            }
            catch (InvalidDataException) { ErrorSeguro(contexto, 413); }
            catch (OperationCanceledException) { ErrorSeguro(contexto, 408); }
            catch (Exception) { ErrorSeguro(contexto, 503); }
            finally
            {
                try { contexto.Request.InputStream.Close(); } catch (Exception) { }
                try { contexto.Response.Close(); } catch (Exception) { }
            }
        }

        private static void ErrorSeguro(HttpListenerContext contexto, int estado)
        {
            // El cliente pudo cortar la conexión o ya haber recibido cabeceras; no debe terminar el servidor.
            try { contexto.Response.StatusCode = estado; contexto.Response.ContentLength64 = 0; } catch (Exception) { }
        }

        public void Dispose() => _escucha.Close();
    }
}
