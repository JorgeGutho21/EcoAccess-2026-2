using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text;

namespace EcoAccess.P3.Bot
{

    /// <summary>
    /// Escucha HTTP local para <see cref="PaginaRegistro"/>, con el mismo esquema que <see cref="ServidorWebhook"/>:
    /// el TLS lo termina el proxy inverso, que reenvía <c>https://&lt;dominio&gt;/registro/</c> a este prefijo local
    /// (por defecto <c>http://127.0.0.1:8081/registro/</c>). Atiende hasta <see cref="SimultaneasPredeterminadas"/>
    /// peticiones a la vez, cada una con plazo de lectura y plazo total; con el cupo lleno responde 503. Así un cliente
    /// lento o una consulta colgada no retienen la página. Toda respuesta lleva los encabezados de seguridad.
    /// </summary>
    public sealed class ServidorRegistro
    {
        public const int SimultaneasPredeterminadas = 64;

        private static readonly TimeSpan PlazoLecturaPredeterminado = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan PlazoTotal = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan EsperaTrasFallo = TimeSpan.FromMilliseconds(250);

        private readonly Func<string?, CancellationToken, Task<RespuestaPagina>> _mostrar;
        private readonly Func<string?, byte[], string?, CancellationToken, Task<RespuestaPagina>> _enviar;
        private readonly string _prefijo;
        private readonly Action<string> _registro;
        private readonly TimeSpan _plazoLectura;
        private readonly int _simultaneas;

        /// <summary>Registros procesándose a la vez (hash PBKDF2 + Oracle): la mitad de los núcleos, al menos uno.</summary>
        internal static int ProcesamientoSimultaneo => Math.Max(1, Environment.ProcessorCount / 2);
        private readonly SemaphoreSlim _procesamiento = new(ProcesamientoSimultaneo, ProcesamientoSimultaneo);

        public ServidorRegistro(PaginaRegistro pagina, string prefijo, Action<string> registro)
            : this(pagina, prefijo, registro, PlazoLecturaPredeterminado)
        {
        }

        public ServidorRegistro(PaginaVinculacion pagina, string prefijo, Action<string> registro)
        {
            Compat.ThrowIfNull(pagina, nameof(pagina));
            _mostrar = pagina.MostrarAsync;
            _enviar = pagina.EnviarAsync;
            _prefijo = ValidarPrefijo(prefijo);
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            _plazoLectura = PlazoLecturaPredeterminado;
            _simultaneas = SimultaneasPredeterminadas;
        }

        /// <summary>Las pruebas acortan el plazo de lectura y reducen el cupo para provocar el 503.</summary>
        internal ServidorRegistro(
            PaginaRegistro pagina, string prefijo, Action<string> registro, TimeSpan plazoLectura, int simultaneas = SimultaneasPredeterminadas)
        {
            Compat.ThrowIfNull(pagina, nameof(pagina));
            _mostrar = pagina.MostrarAsync;
            _enviar = (tipo, cuerpo, _, ct) => pagina.EnviarAsync(tipo, cuerpo, ct);
            _prefijo = ValidarPrefijo(prefijo);
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            _plazoLectura = plazoLectura > TimeSpan.Zero ? plazoLectura : throw new ArgumentOutOfRangeException(nameof(plazoLectura));
            _simultaneas = simultaneas > 0 ? simultaneas : throw new ArgumentOutOfRangeException(nameof(simultaneas));
        }

        /// <summary>
        /// Un túnel como Tailscale Funnel conserva el nombre público en la petición, y HttpListener solo la acepta con el
        /// comodín «+». Aun así, <see cref="AtenderAsync"/> rechaza cualquier conexión que no venga de este equipo.
        /// </summary>
        private bool PrefijoComodin => _prefijo.StartsWith(Comodin, StringComparison.Ordinal);

        private const string Comodin = "http://+:";

        private static string SinComodin(string prefijo) =>
            prefijo.StartsWith(Comodin, StringComparison.Ordinal) ? "http://127.0.0.1:" + prefijo.Substring(Comodin.Length) : prefijo;

        private static string ValidarPrefijo(string prefijo)
        {
            if (!Uri.TryCreate(SinComodin(prefijo), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp
                || !uri.IsLoopback || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
                || !prefijo.EndsWith('/'))
                throw new ArgumentException("El registro debe escuchar solo HTTP loopback, detrás de HTTPS.", nameof(prefijo));
            return prefijo;
        }

        /// <summary>
        /// Atiende hasta que se cancela y espera a las peticiones en curso; un fallo de una petición o del escucha no
        /// detiene el servidor.
        /// </summary>
        public async Task EjecutarAsync(CancellationToken cancellationToken)
        {
            using var escucha = new HttpListener();
            escucha.Prefixes.Add(_prefijo);
            escucha.Start();
            _registro($"Página de registro escuchando en {_prefijo}");
            AsegurarHilos(_simultaneas);
            using var cupos = new SemaphoreSlim(_simultaneas);
            var enCurso = new List<Task>();
            using (cancellationToken.Register(escucha.Stop))
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
                        // Un error que se repite no debe girar en vacío ni llenar el registro.
                        _registro($"Registro: no se pudo recibir una petición (código {error.ErrorCode}).");
                        try
                        {
                            await Task.Delay(EsperaTrasFallo, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        continue;
                    }

                    if (!cupos.Wait(0))
                    {
                        Responder(contexto.Response, 503, ("Retry-After", "5"));
                        continue;
                    }

                    enCurso.RemoveAll(t => t.IsCompleted);
                    long aceptada = System.Diagnostics.Stopwatch.GetTimestamp();
                    enCurso.Add(Task.Run(() => AtenderConCupoAsync(contexto, aceptada, cupos, cancellationToken), CancellationToken.None));
                }
            }

            await Task.WhenAll(enCurso).ConfigureAwait(false);
        }

        /// <summary>
        /// Cada registro calcula un hash PBKDF2 y consulta Oracle (ODP.NET es síncrono en .NET Framework) ocupando un hilo
        /// hasta terminar. Con el mínimo por defecto (uno por núcleo) una ráfaga de 30 personas agotaba los hilos: .NET
        /// Framework agrega de a uno cada ~0,5 s. Se aseguran tantos hilos de trabajo y de E/S (IOCP) como cupos, como
        /// prevención. Solo sube los mínimos.
        /// </summary>
        internal static void AsegurarHilos(int simultaneas)
        {
            ThreadPool.GetMinThreads(out int trabajo, out int entradaSalida);
            if (trabajo < simultaneas || entradaSalida < simultaneas)
                ThreadPool.SetMinThreads(Math.Max(trabajo, simultaneas), Math.Max(entradaSalida, simultaneas));
        }

        private async Task AtenderConCupoAsync(HttpListenerContext contexto, long aceptada, SemaphoreSlim cupos, CancellationToken cancellationToken)
        {
            try
            {
                await AtenderAsync(contexto, aceptada, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // El servidor se está deteniendo.
            }
            finally
            {
                cupos.Release();
            }
        }

        /// <summary>Respuesta sin cuerpo para los rechazos que no llegan a la página.</summary>
        private static void Responder(HttpListenerResponse respuesta, int codigo, params (string Nombre, string Valor)[] extra)
        {
            PonerEncabezados(respuesta);
            foreach ((string nombre, string valor) in extra)
            {
                respuesta.Headers[nombre] = valor;
            }

            FijarCodigoSiSePuede(respuesta, codigo);
            Cerrar(respuesta);
        }

        private static void PonerEncabezados(HttpListenerResponse respuesta)
        {
            foreach ((string nombre, string valor) in PaginaRegistro.Encabezados)
            {
                respuesta.Headers[nombre] = valor;
            }
        }

        private static void Cerrar(HttpListenerResponse respuesta)
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

        private static TimeSpan CpuProceso() { using var p = System.Diagnostics.Process.GetCurrentProcess(); return p.TotalProcessorTime; }

        private static long Ms(long desde, long hasta) => (hasta - desde) * 1000 / System.Diagnostics.Stopwatch.Frequency;

        private async Task AtenderAsync(HttpListenerContext contexto, long aceptada, CancellationToken cancellationToken)
        {
            HttpListenerRequest peticion = contexto.Request;
            HttpListenerResponse respuesta = contexto.Response;
            PonerEncabezados(respuesta);
            // El plazo total cubre también la consulta a Oracle: una base colgada no retiene el cupo para siempre.
            using var total = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            total.CancelAfter(PlazoTotal);
            try
            {
                // Solo el proxy local llega hasta aquí, y detrás de un túnel nunca se aceptan credenciales sin HTTPS.
                if (!peticion.IsLocal || (PrefijoComodin && peticion.Headers["X-Forwarded-Proto"] != "https"))
                {
                    respuesta.StatusCode = 403;
                    return;
                }
                // El proxy público solo puede servir la ruta de vinculación configurada, no sus subrutas.
                if (!string.Equals(peticion.Url?.AbsolutePath, new Uri(SinComodin(_prefijo)).AbsolutePath, StringComparison.Ordinal))
                {
                    respuesta.StatusCode = 404;
                    return;
                }
                RespuestaPagina pagina;
                if (peticion.HttpMethod == "GET")
                {
                    string? consulta = peticion.Url?.Query.TrimStart('?');
                    pagina = await Task.Run(() => _mostrar(consulta, total.Token), total.Token).ConfigureAwait(false);
                }
                else if (peticion.HttpMethod == "POST")
                {
                    if (peticion.ContentLength64 > PaginaRegistro.TamanoMaximo)
                    {
                        respuesta.StatusCode = 413;
                        return;
                    }

                    byte[] cuerpo;
                    var leidos = new long[1];
                    long inicioLectura = System.Diagnostics.Stopwatch.GetTimestamp();
                    int gcInicio = GC.CollectionCount(2);
                    TimeSpan cpuInicio = CpuProceso();
                    using (var plazo = CancellationTokenSource.CreateLinkedTokenSource(total.Token))
                    {
                        plazo.CancelAfter(_plazoLectura);
                        try
                        {
                            cuerpo = await LeerAsync(peticion.InputStream, leidos, plazo.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                            // Solo tiempos y tamaños, nunca el contenido: distingue una cola del servidor de un cliente lento.
                            long ahora = System.Diagnostics.Stopwatch.GetTimestamp();
                            ThreadPool.GetAvailableThreads(out int trabajoLibre, out int esLibre);
                            double cpu = (CpuProceso() - cpuInicio).TotalMilliseconds / Math.Max(1, Ms(inicioLectura, ahora)) / Environment.ProcessorCount * 100;
                            _registro($"Registro: el cliente no envió el formulario a tiempo (en cola {Ms(aceptada, inicioLectura)} ms, " +
                                      $"leyendo {Ms(inicioLectura, ahora)} ms, {leidos[0]} de {peticion.ContentLength64} bytes, " +
                                      $"Expect={peticion.Headers["Expect"] ?? "-"}, hilos libres {trabajoLibre}/{esLibre}, CPU del proceso {cpu:0}% de {Environment.ProcessorCount} núcleos, " +
                                      $"GC gen2 {GC.CollectionCount(2) - gcInicio}).");
                            respuesta.StatusCode = 408;
                            return;
                        }
                    }

                    // La lectura terminó en un hilo de E/S (IOCP). PBKDF2 consume CPU y ODP.NET bloquea el hilo en .NET Framework:
                    // hacerlo aquí deja el puerto IOCP con todos sus hilos ocupados (concurrencia = núcleos) y las lecturas de
                    // OTRAS peticiones no se despachan; vencían a los 3 s con 0 bytes leídos aunque el cliente ya había enviado
                    // todo (408 con 30 personas). Se continúa en un hilo de trabajo y el hilo de E/S queda libre.
                    string? tipo = peticion.ContentType, cookie = peticion.Cookies[PaginaVinculacion.NombreCookie]?.Value;
                    // Misión nocturna (12/11/2026): con 30 registros a la vez, 30 PBKDF2 en el grupo de hilos llevaban el proceso al
                    // 96 % de 8 núcleos y las continuaciones de las demás peticiones esperaban más de 3 s (408 con 0 bytes leídos).
                    // Como mucho la mitad de los núcleos hace este trabajo de CPU; el resto espera su turno sin ocupar hilos.
                    await _procesamiento.WaitAsync(total.Token).ConfigureAwait(false);
                    try { pagina = await Task.Run(() => _enviar(tipo, cuerpo, cookie, total.Token), total.Token).ConfigureAwait(false); }
                    finally { _procesamiento.Release(); }
                }
                else
                {
                    respuesta.StatusCode = 405;
                    respuesta.Headers["Allow"] = "GET, POST";
                    return;
                }

                byte[] html = Encoding.UTF8.GetBytes(pagina.Html);
                respuesta.StatusCode = pagina.Codigo;
                respuesta.ContentType = "text/html; charset=utf-8";
                respuesta.ContentLength64 = html.Length;
                if (pagina.Cookie is not null) respuesta.Headers["Set-Cookie"] = pagina.Cookie;
                if (pagina.Codigo == 429)
                {
                    respuesta.Headers["Retry-After"] = "60";
                }

                await respuesta.OutputStream.WriteAsync(html, total.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _registro("Registro: la petición superó el plazo total.");
                FijarCodigoSiSePuede(respuesta, 503);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                // Solo el tipo: el mensaje podría citar datos del formulario.
                _registro($"Registro: petición no atendida ({error.GetType().Name}).");
                FijarCodigoSiSePuede(respuesta, 500);
            }
            finally
            {
                Cerrar(respuesta);
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

        /// <summary>Lee como máximo <see cref="PaginaRegistro.TamanoMaximo"/> + 1 bytes; la página rechaza el exceso con 413.</summary>
        private static async Task<byte[]> LeerAsync(Stream entrada, long[] total, CancellationToken cancellationToken)
        {
            using var memoria = new MemoryStream();
            byte[] bufer = new byte[8192];
            int leidos;
            while ((leidos = await entrada.ReadAsync(bufer, cancellationToken).AsTask().WaitAsync(cancellationToken).ConfigureAwait(false)) > 0)
            {
                memoria.Write(bufer, 0, leidos);
                total[0] = memoria.Length;
                if (memoria.Length > PaginaRegistro.TamanoMaximo)
                {
                    break;
                }
            }

            return memoria.ToArray();
        }
    }
}
