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

        public ClienteTelegramHttp(string tokenBot)
            : this(tokenBot, new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }), propio: true)
        {
        }

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
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TimeSpan.FromSeconds(15));
            try
            {
                JsonElement metadatos = await LlamarAsync("getFile", new JsonObject { ["file_id"] = archivo }, TimeSpan.FromSeconds(10), limite.Token);
                if (metadatos.TryGetProperty("file_size", out var size)
                    && (!size.TryGetInt64(out long bytes) || bytes < 0 || bytes > ServicioClasificacionImagen.MaximoBytes))
                    throw new ErrorTelegramException("La foto excede el tamaño permitido.");
                string? ruta = metadatos.GetProperty("file_path").GetString();
                if (Compat.IsNullOrEmpty(ruta) || ruta.Length > 512
                    || ruta.Split('/').Any(s => s.Length == 0 || s is "." or ".." || s.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not '-')))
                    throw new ErrorTelegramException("Telegram devolvió una ruta de foto no válida.");
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_raizArchivos, ruta));
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, limite.Token);
                if (!response.IsSuccessStatusCode) throw new ErrorTelegramException("No se pudo obtener el archivo de la foto.");
                byte[] contenido = await LecturaAcotada.DesdeHttpAsync(response.Content, ServicioClasificacionImagen.MaximoBytes, limite.Token);
                return new ImagenResiduo(contenido, response.Content.Headers.ContentType?.MediaType);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (ErrorTelegramException) { throw; }
            catch (Exception) { throw new ErrorTelegramException("No se pudo descargar la foto de Telegram."); }
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
            JsonElement resultado = await LlamarAsync("getUpdates", cuerpo, espera + MargenRed, cancellationToken).ConfigureAwait(false);
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

            await LlamarAsync("sendMessage", cuerpo, TiempoEnvio, cancellationToken);
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

        public Task ResponderBotonAsync(string idBoton, CancellationToken cancellationToken) =>
            LlamarAsync("answerCallbackQuery", new JsonObject { ["callback_query_id"] = idBoton }, TiempoEnvio, cancellationToken);

        private async Task<JsonElement> LlamarAsync(string metodo, JsonObject cuerpo, TimeSpan limite, CancellationToken cancellationToken)
        {
            using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            tiempo.CancelAfter(limite);
            HttpResponseMessage respuesta;
            try
            {
                using var peticion = new HttpRequestMessage(HttpMethod.Post, metodo) { Content = JsonContent.Create(cuerpo) };
                respuesta = await _http.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, tiempo.Token).ConfigureAwait(false);
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
                _http.Dispose();
            }
        }
    }
}
