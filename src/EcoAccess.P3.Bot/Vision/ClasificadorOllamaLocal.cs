using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using EcoAccess.P3.Bot.Transportes;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>
    /// Modelo local exacto en el Ollama del equipo (127.0.0.1, sin nube, sin proxy ni redirecciones). La foto viaja en
    /// el campo <c>images</c> de <c>/api/chat</c> codificada en base64; la respuesta es JSON con esquema y C# la valida
    /// antes de convertirla en propuesta. Cada resultado queda en <see cref="EstadoVisionLocal"/> para el diagnóstico.
    /// </summary>
    public sealed class ClasificadorOllamaLocal : IClasificadorMaterial, IDisposable
    {
        public const string Modelo = "qwen3-vl:2b-instruct";
        public const string Servidor = "http://127.0.0.1:11434";

        /// <summary>
        /// Límite de una inferencia. Medido en este equipo: la carga en frío del modelo más la primera foto tarda bastante
        /// más que las siguientes, por eso no puede ser corto; tampoco infinito, para no bloquear la cola de fotos.
        /// </summary>
        public static readonly TimeSpan TiempoMaximoInferencia = TimeSpan.FromSeconds(110);

        private readonly HttpClient _http;
        private readonly SemaphoreSlim _cupo = new(1, 1);
        private readonly string _mantenerCargado;
        private readonly string _archivoEstado;
        private readonly bool _validarServidor;
        private readonly string _directorioControl;
        public string Proveedor => "ollama-local:" + Modelo;

        /// <param name="mantenerCargado">Valor de <c>keep_alive</c>: cuánto conserva Ollama el modelo en memoria tras una foto.</param>
        public ClasificadorOllamaLocal(string mantenerCargado = "15m")
            : this(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false }), mantenerCargado, validarServidor: true) { }
        internal ClasificadorOllamaLocal(HttpClient http, string mantenerCargado = "15m", string? archivoEstado = null, bool validarServidor = false, string? directorioControl = null)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(mantenerCargado, "^(0|[1-9][0-9]{0,2}[smh])$"))
                throw new ArgumentException("keep_alive debe ser 0 o un número con s, m u h.", nameof(mantenerCargado));
            _http = http; _http.Timeout = Timeout.InfiniteTimeSpan; _mantenerCargado = mantenerCargado; _archivoEstado = archivoEstado ?? EstadoVisionLocal.Archivo; _validarServidor = validarServidor;
            _directorioControl = directorioControl ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3", "Vision", "control-inferencia");
        }

        public async Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
        {
            // Una foto a la vez: el modelo comparte memoria de video y dos inferencias simultáneas se vuelven lentas.
            await _cupo.WaitAsync(ct);
            var reloj = Stopwatch.StartNew();
            try
            {
                using var exclusion = _validarServidor ? ExclusionInferenciaLocal.Adquirir(_directorioControl) : null;
                if (_validarServidor) await DiagnosticoOllamaLocal.ExigirSinCompetenciaAsync(_http, ct);
                ImagenResiduo preparada = PreparadorImagen.Preparar(imagen);
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct); limite.CancelAfter(TiempoMaximoInferencia);
                var cuerpo = new
                {
                    model = Modelo,
                    stream = false,
                    keep_alive = _mantenerCargado,
                    format = FormatoPropuestaVision.EsquemaLocal(),
                    options = new { num_ctx = 4096, num_predict = 256, temperature = 0 },
                    messages = new object[]{new{role="system",content=FormatoPropuestaVision.InstruccionesLocal(catalogo)},
                        new{role="user",content="Classify the waste item in this photo.",images=new[]{Convert.ToBase64String(preparada.Contenido)}}}
                };
                using var peticion = new HttpRequestMessage(HttpMethod.Post, Servidor + "/api/chat") { Content = JsonContent.Create(cuerpo) };
                using var respuesta = await _http.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, limite.Token);
                if (!respuesta.IsSuccessStatusCode) throw new InvalidOperationException($"HTTP {(int)respuesta.StatusCode} del modelo local.");
                byte[] datos = await LecturaAcotada.DesdeHttpAsync(respuesta.Content, 64 * 1024, limite.Token);
                using var doc = JsonDocument.Parse(datos); var raiz = doc.RootElement;
                if (raiz.GetProperty("model").GetString() != Modelo || raiz.GetProperty("done").ValueKind != JsonValueKind.True
                    || raiz.GetProperty("done_reason").GetString() != "stop") throw new InvalidDataException("El modelo local no terminó correctamente.");
                PropuestaVision propuesta = FormatoPropuestaVision.LeerLocal(raiz.GetProperty("message").GetProperty("content").GetString()!);
                EstadoVisionLocal.Registrar(_archivoEstado, true, reloj.Elapsed, null);
                return MapeoObjetoCatalogoLocal.Normalizar(propuesta, catalogo);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                // Solo el tipo y un resumen breve: nunca la imagen ni la respuesta completa del modelo.
                EstadoVisionLocal.Registrar(_archivoEstado, false, reloj.Elapsed, error is OperationCanceledException ? "Tiempo de espera agotado" : error.GetType().Name);
                throw new InvalidOperationException("No se pudo clasificar localmente. No se enviará la imagen a la nube.");
            }
            finally { _cupo.Release(); }
        }


        public void Dispose() => _http.Dispose();
    }
}
