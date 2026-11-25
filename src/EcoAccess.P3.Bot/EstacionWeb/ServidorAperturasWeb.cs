using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Bot.EstacionWeb
{
    /// <summary>
    /// Valida y traduce el JSON de la estación web (sites/ecoaccess-estacion/server-local.js). Separado del escucha HTTP
    /// para probarlo sin red.
    /// <para>Contrato: POST <c>{"solicitud","idCaneca","color"}</c> con <c>Content-Type: application/json</c> y el
    /// encabezado <c>X-EcoAccess: estacion</c> → <c>{"aceptada","mensaje","abiertoMs"}</c>.</para>
    /// <para>El tipo JSON y el encabezado propio obligan a una verificación CORS que nunca se aprueba: una página web
    /// cualquiera abierta en el equipo no puede pedir una apertura. Si se configura un token, también se exige.</para>
    /// </summary>
    public sealed class ReceptorAperturasWeb
    {
        public const string EncabezadoEstacion = "X-EcoAccess";
        public const string EncabezadoToken = "X-EcoAccess-Token";
        public const int TamanoMaximo = 2048;

        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly ServicioAperturasWeb _servicio;
        private readonly byte[]? _token;

        public ReceptorAperturasWeb(ServicioAperturasWeb servicio, string? token)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            if (!Compat.IsNullOrWhiteSpace(token))
            {
                if (token!.Trim().Length < 16) throw new ArgumentException("El token de la estación web debe tener al menos 16 caracteres.", nameof(token));
                _token = Encoding.UTF8.GetBytes(token.Trim());
            }
        }

        public async Task<(int Codigo, byte[] Cuerpo)> RecibirAsync(
            string? tipoContenido, string? encabezadoEstacion, string? token, byte[] cuerpo, CancellationToken cancellationToken)
        {
            if (encabezadoEstacion != "estacion" || !TokenCorrecto(token)) return Responder(403, new(false, "Solicitud no autorizada."));
            if (tipoContenido is null || !tipoContenido.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return Responder(415, new(false, "Se esperaba JSON."));
            if (cuerpo is null || cuerpo.Length == 0 || cuerpo.Length > TamanoMaximo) return Responder(413, new(false, "Cuerpo inválido."));

            SolicitudAperturaWeb? solicitud;
            try
            {
                using JsonDocument documento = JsonDocument.Parse(cuerpo);
                JsonElement raiz = documento.RootElement;
                solicitud = raiz.ValueKind == JsonValueKind.Object
                    && raiz.TryGetProperty("solicitud", out JsonElement s) && s.ValueKind == JsonValueKind.String
                    && raiz.TryGetProperty("idCaneca", out JsonElement i) && i.TryGetInt32(out int idCaneca)
                    && raiz.TryGetProperty("color", out JsonElement c) && c.ValueKind == JsonValueKind.String
                    ? new SolicitudAperturaWeb(s.GetString()!, idCaneca, c.GetString()!)
                    : null;
            }
            catch (JsonException)
            {
                solicitud = null;
            }
            if (solicitud is null) return Responder(400, new(false, "Faltan solicitud, idCaneca o color."));

            RespuestaAperturaWeb respuesta = await _servicio.SolicitarAsync(solicitud, cancellationToken).ConfigureAwait(false);
            // 200 aun si no abre: la estación muestra el mensaje tal cual. Los códigos de error quedan para peticiones mal formadas.
            return Responder(200, respuesta);
        }

        private bool TokenCorrecto(string? token)
        {
            if (_token is null) return true;
            if (token is null) return false;
            return Compat.FixedTimeEquals(Encoding.UTF8.GetBytes(token), _token);
        }

        private static (int, byte[]) Responder(int codigo, RespuestaAperturaWeb respuesta) =>
            (codigo, JsonSerializer.SerializeToUtf8Bytes(respuesta, Json));
    }

    /// <summary>
    /// Escucha HTTP solo en 127.0.0.1 para la estación web (por defecto <c>http://127.0.0.1:8190/aperturas/</c>). Atiende
    /// una petición a la vez; el cuerpo tiene tamaño y plazo de lectura limitados.
    /// </summary>
    public sealed class ServidorAperturasWeb
    {
        public const string PrefijoPredeterminado = "http://127.0.0.1:8190/aperturas/";
        private static readonly TimeSpan PlazoLectura = TimeSpan.FromSeconds(5);

        private readonly ReceptorAperturasWeb _receptor;
        private readonly string _prefijo;
        private readonly Action<string> _registro;

        public ServidorAperturasWeb(ReceptorAperturasWeb receptor, string prefijo, Action<string> registro)
        {
            _receptor = receptor ?? throw new ArgumentNullException(nameof(receptor));
            _prefijo = PrefijoValido(prefijo) ? prefijo : throw new ArgumentException("El prefijo debe ser http://127.0.0.1:<puerto>/<ruta>/", nameof(prefijo));
            _registro = RegistroTecnicoSeguro.Proteger(registro);
        }

        internal static bool PrefijoValido(string? prefijo) =>
            Uri.TryCreate(prefijo, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttp && uri.Host == "127.0.0.1"
            && uri.Port >= 1024 && uri.AbsolutePath.Length > 1 && uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal)
            && uri.Query.Length == 0 && uri.UserInfo.Length == 0 && prefijo!.EndsWith("/", StringComparison.Ordinal);

        public async Task EjecutarAsync(CancellationToken cancellationToken)
        {
            using var escucha = new HttpListener();
            escucha.Prefixes.Add(_prefijo);
            escucha.Start();
            _registro($"Estación web: esperando aperturas en {_prefijo}");
            using CancellationTokenRegistration alCancelar = cancellationToken.Register(escucha.Stop);
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext contexto;
                try
                {
                    contexto = await escucha.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception error) when (error is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    continue;
                }
                await AtenderAsync(contexto, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AtenderAsync(HttpListenerContext contexto, CancellationToken cancellationToken)
        {
            HttpListenerResponse respuesta = contexto.Response;
            try
            {
                HttpListenerRequest peticion = contexto.Request;
                if (peticion.HttpMethod != "POST" || !EsMismaRuta(peticion.Url))
                {
                    respuesta.StatusCode = peticion.HttpMethod == "POST" ? 404 : 405;
                    return;
                }
                if (peticion.ContentLength64 > ReceptorAperturasWeb.TamanoMaximo)
                {
                    respuesta.StatusCode = 413;
                    return;
                }
                byte[] cuerpo;
                using (var plazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    plazo.CancelAfter(PlazoLectura);
                    try { cuerpo = await LeerAsync(peticion.InputStream, plazo.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        respuesta.StatusCode = 408;
                        return;
                    }
                }
                var (codigo, salida) = await _receptor.RecibirAsync(peticion.ContentType, peticion.Headers[ReceptorAperturasWeb.EncabezadoEstacion],
                    peticion.Headers[ReceptorAperturasWeb.EncabezadoToken], cuerpo, cancellationToken).ConfigureAwait(false);
                respuesta.StatusCode = codigo;
                respuesta.ContentType = "application/json; charset=utf-8";
                respuesta.Headers["Cache-Control"] = "no-store";
                respuesta.Headers["X-Content-Type-Options"] = "nosniff";
                await respuesta.OutputStream.WriteAsync(salida, 0, salida.Length, cancellationToken).ConfigureAwait(false);
                _registro($"Estación web: respuesta {codigo}.");
            }
            catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _registro($"Estación web: petición no atendida ({error.GetType().Name}).");
                try { respuesta.StatusCode = 500; } catch (InvalidOperationException) { /* encabezados ya enviados */ }
            }
            finally
            {
                try { respuesta.Close(); }
                catch (Exception error) when (error is HttpListenerException or ObjectDisposedException or InvalidOperationException) { }
            }
        }

        private bool EsMismaRuta(Uri? url) =>
            url is not null && string.Equals(url.AbsolutePath, new Uri(_prefijo).AbsolutePath, StringComparison.Ordinal);

        private static async Task<byte[]> LeerAsync(Stream entrada, CancellationToken cancellationToken)
        {
            using var memoria = new MemoryStream();
            byte[] bufer = new byte[1024];
            int leidos;
            while ((leidos = await entrada.ReadAsync(bufer, cancellationToken).AsTask().WaitAsync(cancellationToken).ConfigureAwait(false)) > 0)
            {
                memoria.Write(bufer, 0, leidos);
                if (memoria.Length > ReceptorAperturasWeb.TamanoMaximo) break;
            }
            return memoria.ToArray();
        }
    }
}
