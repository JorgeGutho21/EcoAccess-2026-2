using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Bot.Transportes;

namespace EcoAccess.P3.Bot.Telegram
{

    public sealed record EstadoConfiguracionTelegram(long IdBot, string NombreBot, bool WebhookActivo, int ActualizacionesPendientes);

    /// <summary>
    /// Cliente real de la Bot API (https://core.telegram.org/bots/api) con HttpClient y System.Text.Json.
    /// El token forma parte de la ruta (/bot{token}/método), así que ninguna URL ni excepción de HttpClient sale de
    /// esta clase: todo error se convierte en <see cref="ErrorTelegramException"/> con un texto propio.
    /// </summary>
    public sealed partial class ClienteTelegramHttp : IClienteTelegram, IDisposable
    {
        /// <summary>Margen sobre el long polling antes de dar por perdida la conexión.</summary>
        private static readonly TimeSpan MargenRed = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan TiempoEnvio = TimeSpan.FromSeconds(30);

        private readonly HttpClient _http;
        private readonly bool _propio;
        private readonly Uri _raizArchivos;

        // .NET Framework limita a 2 las conexiones simultáneas por servidor. El largo polling de getUpdates ocupa una
        // durante hasta 30 s; con «escribiendo…» o un envío en la otra, getFile esperaba turno y vencía a los 10 s
        // («No pude descargar la foto»). Con este límite y un cliente aparte para el polling, nunca compiten.
        private const int ConexionesPorServidor = 16;

        public ClienteTelegramHttp(string tokenBot)
            : this(tokenBot, new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, MaxConnectionsPerServer = ConexionesPorServidor }), propio: true)
        {
            _polling = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, MaxConnectionsPerServer = ConexionesPorServidor })
            {
                BaseAddress = _http.BaseAddress, Timeout = Timeout.InfiniteTimeSpan, MaxResponseContentBufferSize = 1024 * 1024,
            };
        }

        /// <summary>Cliente exclusivo del largo polling (getUpdates); los envíos y descargas van por <see cref="_http"/>.</summary>
        private readonly HttpClient? _polling;

        /// <summary>Permite inyectar un <see cref="HttpMessageHandler"/> de prueba a través del HttpClient.</summary>
        internal ClienteTelegramHttp(string tokenBot, HttpClient http, bool propio)
        {
            if (!TokenValido(tokenBot))
            {
                throw new ArgumentException("El token del bot no es válido.", nameof(tokenBot));
            }

            _http = http ?? throw new ArgumentNullException(nameof(http));
            _http.BaseAddress = new Uri("https://api.telegram.org/bot" + tokenBot + "/");
            _http.Timeout = Timeout.InfiniteTimeSpan;
            _http.MaxResponseContentBufferSize = 1024 * 1024;
            _raizArchivos = new Uri("https://api.telegram.org/file/bot" + tokenBot + "/");
            _propio = propio;
        }

        /// <summary>Muestra «escribiendo…» en el chat. Si falla no pasa nada: el análisis sigue igual.</summary>
        public async Task IndicarEscribiendoAsync(long chat, CancellationToken cancellationToken = default)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TimeSpan.FromSeconds(2));
            try { await LlamarAsync("sendChatAction", new JsonObject { ["chat_id"] = chat, ["action"] = "typing" }, TimeSpan.FromSeconds(2), limite.Token); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception) { /* Es solo una señal visual. */ }
        }

        public async Task<ImagenResiduo> DescargarFotoAsync(string archivo, CancellationToken cancellationToken = default)
        {
            if (Compat.IsNullOrWhiteSpace(archivo) || archivo.Length > 512 || archivo.Any(char.IsControl))
                throw new ErrorTelegramException("La referencia de foto no es válida.");
            // Un reintento acotado ante fallos transitorios de red (no ante respuestas definitivas de Telegram).
            for (int intento = 1; ; intento++)
            {
                try { return await DescargarUnaVezAsync(archivo, cancellationToken).ConfigureAwait(false); }
                catch (ErrorTelegramException e) when (intento < 2 && !cancellationToken.IsCancellationRequested && !e.Data.Contains(Definitivo))
                {
                    await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private const string Definitivo = "ecoaccess.definitivo";
        /// <summary>Solo tiempo agotado, fallo de conexión o error 5xx de Telegram justifican repetir getFile.</summary>
        private static bool Transitorio(ErrorTelegramException e) => !e.EsTokenRechazado && !e.EsConflicto
            && (e.Message.Contains("a tiempo") || e.Message.StartsWith("No se pudo conectar", StringComparison.Ordinal) || e.Message.Contains("(HTTP 5"));
        private static ErrorTelegramException FalloDefinitivo(string mensaje) { var e = new ErrorTelegramException(mensaje); e.Data[Definitivo] = true; return e; }

        private async Task<ImagenResiduo> DescargarUnaVezAsync(string archivo, CancellationToken cancellationToken)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TimeSpan.FromSeconds(12));
            // Solo la consulta getFile (metadatos) admite un reintento; la descarga del archivo nunca se repite.
            bool descargando = false;
            try
            {
                JsonElement metadatos;
                try { metadatos = await LlamarAsync("getFile", new JsonObject { ["file_id"] = archivo }, TimeSpan.FromSeconds(8), limite.Token); }
                catch (ErrorTelegramException e) when (!Transitorio(e)) { e.Data[Definitivo] = true; throw; }
                // Metadatos sin objeto o sin file_path de texto: repetir getFile no lo arregla (no es un fallo de red).
                if (metadatos.ValueKind != JsonValueKind.Object)
                    throw FalloDefinitivo("Telegram devolvió datos de foto ilegibles.");
                if (metadatos.TryGetProperty("file_size", out var size)
                    && (size.ValueKind != JsonValueKind.Number || !size.TryGetInt64(out long bytes) || bytes < 0 || bytes > ServicioClasificacionImagen.MaximoBytes))
                throw FalloDefinitivo("La foto excede el tamaño permitido.");
                if (!metadatos.TryGetProperty("file_path", out var rutaJson) || rutaJson.ValueKind != JsonValueKind.String)
                    throw FalloDefinitivo("Telegram devolvió una ruta de foto no válida.");
                string? ruta = rutaJson.GetString();
                if (Compat.IsNullOrEmpty(ruta) || ruta.Length > 512
                    || ruta.Split('/').Any(s => s.Length == 0 || s is "." or ".." || s.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not '-')))
                throw FalloDefinitivo("Telegram devolvió una ruta de foto no válida.");
                descargando = true;
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_raizArchivos, ruta));
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, limite.Token);
                if (!response.IsSuccessStatusCode) throw new ErrorTelegramException("No se pudo obtener el archivo de la foto.");
                byte[] contenido = await LecturaAcotada.DesdeHttpAsync(response.Content, ServicioClasificacionImagen.MaximoBytes, limite.Token);
                return new ImagenResiduo(contenido, response.Content.Headers.ContentType?.MediaType);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (ErrorTelegramException e) { if (descargando) e.Data[Definitivo] = true; throw; }
            catch (Exception)
            {
                var e = new ErrorTelegramException("No se pudo descargar la foto de Telegram.");
                if (descargando) e.Data[Definitivo] = true;
                throw e;
            }
        }

        /// <summary>
        /// Forma de los tokens de BotFather: id numérico, dos puntos y la clave en Base64 URL. Así ningún carácter
        /// (?, #, /, espacios) puede alterar la URL que se arma con él.
        /// </summary>
        public static bool TokenValido(string? tokenBot) =>
            tokenBot is not null && FormaToken().IsMatch(tokenBot);

        private static readonly Regex PatronFormaToken = new Regex(@"^\d{5,}:[A-Za-z0-9_-]{20,}\z", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(1000));
        private static Regex FormaToken() => PatronFormaToken;

        /// <summary>Solo lectura: identidad y presencia de webhook, sin consumir updates ni revelar su URL.</summary>
        public async Task<EstadoConfiguracionTelegram> ComprobarConfiguracionAsync(string nombreEsperado, CancellationToken ct = default)
        {
            if (Compat.IsNullOrWhiteSpace(nombreEsperado) || nombreEsperado.Length is < 5 or > 32
                || nombreEsperado.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c != '_'))
                throw new ArgumentException("Configura Telegram:NombreBot sin @ ni espacios.");
            JsonElement yo = await LlamarAsync("getMe", new JsonObject(), TimeSpan.FromSeconds(10), ct);
            if (yo.ValueKind != JsonValueKind.Object || !yo.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out long identidad) || identidad <= 0
                || !yo.TryGetProperty("is_bot", out var bot) || bot.ValueKind != JsonValueKind.True
                || !yo.TryGetProperty("username", out var nombre) || nombre.ValueKind != JsonValueKind.String
                || !string.Equals(nombre.GetString(), nombreEsperado, StringComparison.OrdinalIgnoreCase))
                throw new ErrorTelegramException("La identidad de Telegram no coincide con Telegram:NombreBot o no es válida.");
            JsonElement hook = await LlamarAsync("getWebhookInfo", new JsonObject(), TimeSpan.FromSeconds(10), ct);
            if (hook.ValueKind != JsonValueKind.Object || !hook.TryGetProperty("url", out var url) || url.ValueKind != JsonValueKind.String
                || !hook.TryGetProperty("pending_update_count", out var pendientes) || pendientes.ValueKind != JsonValueKind.Number || !pendientes.TryGetInt32(out int cantidad) || cantidad < 0)
                throw new ErrorTelegramException("Telegram devolvió un estado de webhook ilegible.");
            return new(identidad, nombre.GetString()!, !Compat.IsNullOrEmpty(url.GetString()), cantidad);
        }

        public async Task<IReadOnlyList<UpdateRecibido>> ObtenerUpdatesAsync(long offset, TimeSpan espera, CancellationToken cancellationToken)
        {
            var cuerpo = new JsonObject
            {
                ["offset"] = offset,
                ["timeout"] = (int)espera.TotalSeconds,
                ["allowed_updates"] = new JsonArray("message", "callback_query"),
            };
            JsonElement resultado = await LlamarAsync("getUpdates", cuerpo, espera + MargenRed, cancellationToken, _polling).ConfigureAwait(false);
            try
            {
                return TraductorUpdates.TraducirLista(resultado);
            }
            catch (JsonException)
            {
                throw new ErrorTelegramException("Telegram devolvió updates ilegibles.");
            }
        }

        public async Task EnviarAsync(RespuestaBot respuesta, CancellationToken cancellationToken)
        {
            Compat.ThrowIfNull(respuesta, nameof(respuesta));
            var cuerpo = new JsonObject { ["chat_id"] = respuesta.ChatId, ["text"] = respuesta.Texto };
            if (respuesta.Botones.Count > 0)
            {
                // Por defecto un botón por fila (los nombres de material son largos); el menú pide dos columnas.
                var filas = new JsonArray(respuesta.Botones
                    .Chunk(Compat.Clamp(respuesta.Columnas, 1, 3))
                    .Select(fila => (JsonNode)new JsonArray(fila
                        .Select(b => (JsonNode)new JsonObject { ["text"] = b.Texto, ["callback_data"] = b.Dato }).ToArray()))
                    .ToArray());
                cuerpo["reply_markup"] = new JsonObject { ["inline_keyboard"] = filas };
            }

            if (respuesta.Imagen is { } imagen && respuesta.Texto.Length <= 1024 && await EnviarImagenOficialAsync(respuesta, imagen, cuerpo, cancellationToken).ConfigureAwait(false))
            { /* foto + leyenda + botones en un solo mensaje */ }
            else await LlamarAsync("sendMessage", cuerpo, TiempoEnvio, cancellationToken);
            if (respuesta.Estadisticas is { } estadisticas)
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                limite.CancelAfter(TiempoEnvio);
                try
                {
                    byte[] png = GraficaEstadisticasPersonal.Crear(estadisticas);
                    using var formulario = new MultipartFormDataContent();
                    formulario.Add(new StringContent(respuesta.ChatId.ToString(System.Globalization.CultureInfo.InvariantCulture)), "chat_id");
                    formulario.Add(new StringContent("Tu evolución personal en EcoAccess. Consulta los detalles en /estadisticas."), "caption");
                    var foto = new ByteArrayContent(png); foto.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    formulario.Add(foto, "photo", "estadisticas.png");
                    using var envio = await _http.PostAsync("sendPhoto", formulario, limite.Token);
                    if (envio.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
                        throw Fallo("sendPhoto", envio.StatusCode, default);
                    byte[] bytes = await LecturaAcotada.DesdeHttpAsync(envio.Content, 128 * 1024, limite.Token);
                    using var documento = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16 });
                    if (!envio.IsSuccessStatusCode || !documento.RootElement.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
                        throw Fallo("sendPhoto", envio.StatusCode, documento.RootElement);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (ErrorTelegramException) { throw; }
                catch (Exception) { throw new ErrorTelegramException("No se pudo enviar la gráfica; tus estadísticas siguen disponibles en el mensaje de texto."); }
            }
        }

        /// <summary>
        /// Envía una imagen oficial (recursos/bot/&lt;clave&gt;.jpg, optimizada a ≤ 100 KB) como foto con leyenda. Tras el primer envío
        /// guarda el file_id que devuelve Telegram y lo reutiliza: no vuelve a subir el archivo. Si algo falla devuelve false y
        /// el texto se envía solo (la imagen nunca bloquea la respuesta ni la apertura, que ya se ordenó antes).
        /// </summary>
        private async Task<bool> EnviarImagenOficialAsync(RespuestaBot respuesta, string clave, JsonObject cuerpoTexto, CancellationToken ct)
        {
            if (!Regex.IsMatch(clave, "^[a-z_]{3,32}$")) return false;
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recursos", "bot", clave + ".jpg");
            string? fileId = CacheImagenes.Obtener(clave);
            try
            {
                if (fileId is not null)
                {
                    var cuerpo = new JsonObject { ["chat_id"] = respuesta.ChatId, ["photo"] = fileId, ["caption"] = respuesta.Texto };
                    if (cuerpoTexto["reply_markup"] is JsonNode teclado) cuerpo["reply_markup"] = teclado.DeepClone();
                    try { await LlamarAsync("sendPhoto", cuerpo, TiempoEnvio, ct).ConfigureAwait(false); return true; }
                    catch (ErrorTelegramException) { CacheImagenes.Olvidar(clave); }
                }
                if (!File.Exists(ruta)) return false;
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
                limite.CancelAfter(TiempoEnvio);
                using var formulario = new MultipartFormDataContent();
                formulario.Add(new StringContent(respuesta.ChatId.ToString(System.Globalization.CultureInfo.InvariantCulture)), "chat_id");
                formulario.Add(new StringContent(respuesta.Texto), "caption");
                if (cuerpoTexto["reply_markup"] is JsonNode markup) formulario.Add(new StringContent(markup.ToJsonString()), "reply_markup");
                var foto = new ByteArrayContent(File.ReadAllBytes(ruta)); foto.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                formulario.Add(foto, "photo", clave + ".jpg");
                using var envio = await _http.PostAsync("sendPhoto", formulario, limite.Token).ConfigureAwait(false);
                byte[] bytes = await LecturaAcotada.DesdeHttpAsync(envio.Content, 256 * 1024, limite.Token).ConfigureAwait(false);
                using var documento = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16 });
                var raiz = documento.RootElement;
                if (!envio.IsSuccessStatusCode || !raiz.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True) return false;
                if (raiz.TryGetProperty("result", out var r) && r.TryGetProperty("photo", out var tamanos) && tamanos.ValueKind == JsonValueKind.Array && tamanos.GetArrayLength() > 0
                    && tamanos[tamanos.GetArrayLength() - 1].TryGetProperty("file_id", out var id) && id.GetString() is { Length: > 0 and < 512 } nuevo)
                    CacheImagenes.Guardar(clave, nuevo);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return false; }
        }

        public Task ResponderBotonAsync(string idBoton, CancellationToken cancellationToken) =>
            LlamarAsync("answerCallbackQuery", new JsonObject { ["callback_query_id"] = idBoton }, TiempoEnvio, cancellationToken);

        private async Task<JsonElement> LlamarAsync(string metodo, JsonObject cuerpo, TimeSpan limite, CancellationToken cancellationToken, HttpClient? cliente = null)
        {
            using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            tiempo.CancelAfter(limite);
            HttpResponseMessage respuesta;
            try
            {
                using var peticion = new HttpRequestMessage(HttpMethod.Post, metodo) { Content = JsonContent.Create(cuerpo) };
                respuesta = await (cliente ?? _http).SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, tiempo.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw new ErrorTelegramException($"Telegram no respondió a {metodo} a tiempo.");
            }
            catch (HttpRequestException)
            {
                // El texto de HttpRequestException puede citar la dirección: no se propaga.
                throw new ErrorTelegramException($"No se pudo conectar con Telegram ({metodo}).");
            }

            using (respuesta)
            {
                // Estos estados son terminales aunque un proxy entregue HTML en lugar del JSON de Telegram.
                if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
                    throw Fallo(metodo, respuesta.StatusCode, default);
                JsonDocument documento;
                try
                {
                    byte[] contenido = await LecturaAcotada.DesdeHttpAsync(respuesta.Content, 1024 * 1024, tiempo.Token).ConfigureAwait(false);
                    documento = JsonDocument.Parse(contenido, new JsonDocumentOptions { MaxDepth = 32 });
                }
                catch (Exception error) when (error is JsonException or HttpRequestException or InvalidOperationException or IOException
                                              || (error is OperationCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    throw new ErrorTelegramException($"Telegram devolvió una respuesta ilegible a {metodo} (HTTP {(int)respuesta.StatusCode}).");
                }

                using (documento)
                {
                    JsonElement raiz = documento.RootElement;
                    bool ok = raiz.ValueKind == JsonValueKind.Object
                        && raiz.TryGetProperty("ok", out JsonElement valor)
                        && valor.ValueKind == JsonValueKind.True;
                    if (ok && respuesta.IsSuccessStatusCode)
                    {
                        return raiz.TryGetProperty("result", out JsonElement resultado) ? resultado.Clone() : default;
                    }

                    throw Fallo(metodo, respuesta.StatusCode, raiz);
                }
            }
        }

        private static ErrorTelegramException Fallo(string metodo, HttpStatusCode estado, JsonElement raiz)
        {
            TimeSpan? espera = null;
            if (raiz.ValueKind == JsonValueKind.Object
                && raiz.TryGetProperty("parameters", out JsonElement parametros)
                && parametros.ValueKind == JsonValueKind.Object
                && parametros.TryGetProperty("retry_after", out JsonElement segundos)
                && segundos.ValueKind == JsonValueKind.Number
                && segundos.TryGetInt32(out int valor)
                && valor > 0)
            {
                espera = TimeSpan.FromSeconds(valor);
            }

            bool conflicto = estado == HttpStatusCode.Conflict;
            // La Bot API responde 404 a un token inexistente y 401 a uno revocado.
            bool tokenRechazado = estado is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound;
            string detalle = conflicto
                ? " Hay un webhook activo u otra instancia del bot con el mismo token."
                : tokenRechazado ? " El token del bot fue revocado o no es válido." : string.Empty;
            // 400/403 al enviar: chat inexistente o que bloqueó al bot; reintentar el mismo mensaje no sirve.
            bool destinoNoDisponible = metodo is "sendMessage" or "sendPhoto" && estado is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden;
            return new ErrorTelegramException(
                $"Telegram rechazó {metodo} (HTTP {(int)estado}).{detalle}", espera, conflicto, tokenRechazado, destinoNoDisponible);
        }

        public void Dispose()
        {
            if (_propio)
            {
                _polling?.Dispose();
                _http.Dispose();
            }
        }
    }
}
