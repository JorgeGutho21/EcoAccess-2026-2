using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Transportes;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>Por qué Groq no dio una propuesta. No lleva la clave ni la respuesta del servicio.</summary>
    public sealed class FalloGroqException : Exception
    {
        public FalloGroqException(string motivo) : base(motivo) { }
    }

    /// <summary>Límites que Groq informa para esta cuenta en cada respuesta: solicitudes por día y tokens por minuto.</summary>
    public sealed record LimitesGroq(long? SolicitudesDia, long? SolicitudesRestantes, long? TokensMinuto, long? TokensRestantes);

    /// <summary>
    /// Qwen 3.8 por la API de Groq compatible con Chat Completions. Solo acepta fotos que la persona autorizó, las envía
    /// sin EXIF y a 512 px, y trata la respuesta como dato no confiable: lo que no sea una opción de la lista cerrada se
    /// rechaza. Se respeta 429 con Retry-After, una credencial rechazada detiene los envíos y hay un límite diario.
    /// </summary>
    public sealed class ClasificadorGroq : IClasificadorMaterial, IDisposable
    {
        public const string Servidor = "https://api.groq.com/openai/v1/";
        public const string ArchivoEstado = "estado-groq.json";

        private const int LadoImagen = 512;
        private const int MaximoIntentos = 2;
        private static readonly TimeSpan EsperaReintento = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan PausaSinRetryAfter = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan PausaMaxima = TimeSpan.FromHours(1);

        private readonly ConfiguracionGroq _configuracion;
        private readonly UsoGroq _uso;
        private readonly HttpClient _http;
        private readonly Action<string> _registrar;
        private readonly Func<TimeSpan, CancellationToken, Task> _demora;
        private readonly TimeProvider _reloj;
        private readonly string _archivoEstado;
        private readonly SemaphoreSlim _cupo = new(2, 2);
        private readonly object _cerrojo = new();
        private readonly Dictionary<string, Task<PropuestaVision>> _enCurso = new(StringComparer.Ordinal);
        private DateTime _pausaHastaUtc;
        private string? _bloqueo;

        public ClasificadorGroq(ConfiguracionGroq configuracion, UsoGroq uso, Action<string> registrar)
            : this(configuracion, uso, registrar, new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            }))
        { }

        internal ClasificadorGroq(ConfiguracionGroq configuracion, UsoGroq uso, Action<string> registrar, HttpClient http,
            Func<TimeSpan, CancellationToken, Task>? demora = null, TimeProvider? reloj = null, string? archivoEstado = null)
        {
            _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
            _uso = uso ?? throw new ArgumentNullException(nameof(uso));
            _registrar = RegistroTecnicoSeguro.Proteger(registrar);
            _http = http;
            _http.Timeout = Timeout.InfiniteTimeSpan;
            _demora = demora ?? Task.Delay;
            _reloj = reloj ?? TimeProvider.System;
            _archivoEstado = archivoEstado ?? Path.Combine(Path.GetDirectoryName(EstadoVisionLocal.Archivo)!, ArchivoEstado);
        }

        public string Proveedor => "groq:" + _configuracion.Modelo;

        public TimeSpan TiempoMaximo => TimeSpan.FromTicks(_configuracion.TiempoMaximo.Ticks * MaximoIntentos) + EsperaReintento;

        public LimitesGroq? UltimosLimites { get; private set; }

        /// <summary>Lo que falta de la pausa pedida con 429. Cero si Groq no pidió esperar.</summary>
        public TimeSpan EsperaPendiente
        {
            get
            {
                TimeSpan resto = _pausaHastaUtc - _reloj.GetUtcNow().UtcDateTime;
                return resto > TimeSpan.Zero ? resto : TimeSpan.Zero;
            }
        }

        public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
        {
            // Última barrera: ningún camino del programa envía una foto sin la marca de autorización.
            if (imagen.Envio == EnvioExterno.Prohibido) throw new VisionExternaNoAutorizadaException();

            // Dos reintentos de la misma foto mientras la primera sigue en curso comparten una sola llamada de pago.
            string huella = Compat.ToHexString(Compat.Sha256(imagen.Contenido));
            lock (_cerrojo)
            {
                if (_enCurso.TryGetValue(huella, out Task<PropuestaVision>? existente)) return existente;
                Task<PropuestaVision> tarea = EnviarYLiberarAsync(huella, imagen, ct);
                if (!tarea.IsCompleted) _enCurso[huella] = tarea;
                return tarea;
            }
        }

        private async Task<PropuestaVision> EnviarYLiberarAsync(string huella, ImagenResiduo imagen, CancellationToken ct)
        {
            try { return await EnviarAsync(imagen, ct).ConfigureAwait(false); }
            finally { lock (_cerrojo) { _enCurso.Remove(huella); } }
        }

        private async Task<PropuestaVision> EnviarAsync(ImagenResiduo imagen, CancellationToken ct)
        {
            await _cupo.WaitAsync(ct).ConfigureAwait(false);
            var reloj = Stopwatch.StartNew();
            try
            {
                ComprobarQueSePuedeEnviar();
                ImagenResiduo preparada = PreparadorImagen.Preparar(imagen, LadoImagen);
                // Una foto borrosa o vacía no se paga ni sale del equipo.
                if (CalidadImagen.Insuficiente(preparada.Contenido))
                    return new PropuestaVision(TipoPropuestaVision.ImagenBorrosa, null, null, ClaveObjeto: EcoAccess.P3.Logica.Servicios.CatalogoObjetos.NoDistinguible);
                string foto = Convert.ToBase64String(preparada.Contenido);
                for (int intento = 1; ; intento++)
                {
                    if (!_uso.Reservar(_configuracion.MaximoSolicitudesDia, _configuracion.PresupuestoDiaUsd))
                        throw new FalloGroqException("Se alcanzó el límite diario configurado para Groq.");
                    try
                    {
                        PropuestaVision propuesta = await PedirAsync(foto, ct).ConfigureAwait(false);
                        EstadoVisionLocal.Registrar(_archivoEstado, _configuracion.Modelo, true, reloj.Elapsed, null);
                        return propuesta;
                    }
                    catch (Exception error) when (intento < MaximoIntentos && !ct.IsCancellationRequested
                        && error is TransitorioException or HttpRequestException or OperationCanceledException)
                    {
                        await _demora(EsperaReintento, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                string resumen = error is FalloGroqException ? error.Message : error is OperationCanceledException ? "Tiempo de espera agotado" : error.GetType().Name;
                EstadoVisionLocal.Registrar(_archivoEstado, _configuracion.Modelo, false, reloj.Elapsed, resumen);
                throw new FalloGroqException(resumen);
            }
            finally { _cupo.Release(); }
        }

        private void ComprobarQueSePuedeEnviar()
        {
            if (_bloqueo is { } motivo) throw new FalloGroqException(motivo);
            if (_reloj.GetUtcNow().UtcDateTime < _pausaHastaUtc) throw new FalloGroqException("Groq pidió esperar por límite de solicitudes.");
        }

        private async Task<PropuestaVision> PedirAsync(string fotoBase64, CancellationToken ct)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(_configuracion.TiempoMaximo);
            var cuerpo = new Dictionary<string, object>
            {
                ["model"] = _configuracion.Modelo,
                // Modo directo, sin razonamiento visible: una elección entre opciones no necesita gastar tokens en pensar.
                ["reasoning_effort"] = "none",
                ["reasoning_format"] = "hidden",
                ["temperature"] = 0,
                ["max_completion_tokens"] = 160,
                ["stream"] = false,
                ["response_format"] = new Dictionary<string, object>
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new Dictionary<string, object> { ["name"] = "propuesta_residuo", ["schema"] = VocabularioVision.EsquemaExtendido },
                },
                ["messages"] = new object[]
                {
                    new { role = "system", content = VocabularioVision.InstruccionesExtendidas },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = VocabularioVision.Pregunta },
                            new { type = "image_url", image_url = new { url = "data:image/jpeg;base64," + fotoBase64 } },
                        },
                    },
                },
            };
            using var peticion = new HttpRequestMessage(HttpMethod.Post, Servidor + "chat/completions") { Content = JsonContent.Create(cuerpo) };
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuracion.ApiKey);
            using HttpResponseMessage respuesta = await _http.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, limite.Token).ConfigureAwait(false);
            UltimosLimites = LeerLimites(respuesta) ?? UltimosLimites;
            await ValidarEstadoAsync(respuesta, limite.Token).ConfigureAwait(false);
            byte[] datos = await LecturaAcotada.DesdeHttpAsync(respuesta.Content, 64 * 1024, limite.Token).ConfigureAwait(false);
            return Interpretar(datos);
        }

        private static LimitesGroq? LeerLimites(HttpResponseMessage respuesta)
        {
            long? Valor(string encabezado) => respuesta.Headers.TryGetValues(encabezado, out IEnumerable<string>? valores)
                && long.TryParse(System.Linq.Enumerable.FirstOrDefault(valores), NumberStyles.None, CultureInfo.InvariantCulture, out long n) ? n : (long?)null;
            var limites = new LimitesGroq(Valor("x-ratelimit-limit-requests"), Valor("x-ratelimit-remaining-requests"),
                Valor("x-ratelimit-limit-tokens"), Valor("x-ratelimit-remaining-tokens"));
            return limites.SolicitudesDia is null && limites.TokensMinuto is null ? null : limites;
        }

        /// <summary>Solo el código de error de la API, reducido a letras, cifras y guiones: nunca su mensaje libre.</summary>
        private static async Task<string> CodigoErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
        {
            try
            {
                using JsonDocument documento = JsonDocument.Parse(await LecturaAcotada.DesdeHttpAsync(respuesta.Content, 16 * 1024, ct).ConfigureAwait(false));
                string? codigo = documento.RootElement.GetProperty("error").GetProperty("code").GetString();
                return codigo is { Length: > 0 and <= 60 } && System.Text.RegularExpressions.Regex.IsMatch(codigo, "^[A-Za-z0-9_-]+$") ? ", " + codigo : "";
            }
            catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or InvalidDataException or IOException) { return ""; }
        }

        private async Task ValidarEstadoAsync(HttpResponseMessage respuesta, CancellationToken ct)
        {
            int codigo = (int)respuesta.StatusCode;
            if (respuesta.IsSuccessStatusCode) return;
            if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                Bloquear($"Groq rechazó la credencial (HTTP {codigo}). Revísala con Configurar-Credenciales.ps1.");
            if (respuesta.StatusCode == HttpStatusCode.NotFound)
                Bloquear($"Groq no reconoce el modelo {_configuracion.Modelo} (HTTP 404). Configura Vision:Groq:Modelo si fue retirado.");
            if (codigo == 429)
            {
                TimeSpan pausa = EsperaPedida(respuesta);
                _pausaHastaUtc = _reloj.GetUtcNow().UtcDateTime + pausa;
                _registrar($"Groq pidió esperar {pausa.TotalSeconds:0} s por límite de solicitudes.");
                throw new FalloGroqException("Groq pidió esperar por límite de solicitudes.");
            }
            if (codigo >= 500) throw new TransitorioException();
            string motivo = await CodigoErrorAsync(respuesta, ct).ConfigureAwait(false);
            _registrar($"Groq rechazó la solicitud (HTTP {codigo}{motivo}).");
            throw new FalloGroqException($"Groq rechazó la solicitud (HTTP {codigo}{motivo}).");
        }

        private void Bloquear(string motivo)
        {
            if (Interlocked.CompareExchange(ref _bloqueo, motivo, null) is null) _registrar(motivo);
            throw new FalloGroqException(motivo);
        }

        private static TimeSpan EsperaPedida(HttpResponseMessage respuesta)
        {
            TimeSpan? pedida = respuesta.Headers.RetryAfter?.Delta;
            if (pedida is null && respuesta.Headers.TryGetValues("retry-after", out IEnumerable<string>? valores))
                foreach (string valor in valores)
                    if (double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out double segundos) && segundos >= 0) pedida = TimeSpan.FromSeconds(segundos);
            if (pedida is not { } espera || espera <= TimeSpan.Zero) return PausaSinRetryAfter;
            return espera > PausaMaxima ? PausaMaxima : espera;
        }

        private PropuestaVision Interpretar(byte[] datos)
        {
            using JsonDocument documento = JsonDocument.Parse(datos, new JsonDocumentOptions { MaxDepth = 16 });
            JsonElement raiz = documento.RootElement;
            if (raiz.TryGetProperty("usage", out JsonElement uso) && uso.ValueKind == JsonValueKind.Object) RegistrarConsumo(uso);
            // El modelo que respondió debe ser el pedido: no se acepta un sustituto silencioso.
            if (!raiz.TryGetProperty("model", out JsonElement modelo) || modelo.GetString() != _configuracion.Modelo)
                throw new InvalidDataException("Respondió un modelo distinto del configurado.");
            JsonElement opcion = raiz.GetProperty("choices")[0];
            if (opcion.GetProperty("finish_reason").GetString() != "stop") throw new InvalidDataException("La respuesta quedó incompleta.");
            string contenido = opcion.GetProperty("message").GetProperty("content").GetString() ?? throw new InvalidDataException("Respuesta sin contenido.");
            return VocabularioVision.Leer(contenido);
        }

        private void RegistrarConsumo(JsonElement uso)
        {
            long entrada = Tokens(uso, "prompt_tokens"), salida = Tokens(uso, "completion_tokens");
            _uso.RegistrarTokens(entrada, salida, CostoUsd(entrada, salida));
        }

        private decimal CostoUsd(long entrada, long salida) =>
            (entrada * _configuracion.PrecioEntradaMillon + salida * _configuracion.PrecioSalidaMillon) / 1_000_000m;

        private static long Tokens(JsonElement uso, string campo) =>
            uso.TryGetProperty(campo, out JsonElement valor) && valor.ValueKind == JsonValueKind.Number && valor.TryGetInt64(out long n) && n >= 0 ? n : 0;

        public void Dispose()
        {
            _http.Dispose();
            _cupo.Dispose();
        }

        private sealed class TransitorioException : Exception { }
    }
}
