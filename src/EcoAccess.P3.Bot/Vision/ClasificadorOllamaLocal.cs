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
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>
    /// Modelo local exacto en el Ollama del equipo (127.0.0.1, sin nube, sin proxy ni redirecciones). La foto viaja en
    /// el campo <c>images</c> de <c>/api/chat</c> codificada en base64; la respuesta es JSON con esquema y C# la valida
    /// antes de convertirla en propuesta. Cada resultado queda en <see cref="EstadoVisionLocal"/> para el diagnóstico.
    /// </summary>
    public sealed class ClasificadorOllamaLocal : IClasificadorMaterial, IDisposable
    {
        /// <summary>Modelo elegido por mediciones en el equipo de referencia; ver docs/vision/07_reconocimiento_visual.md.</summary>
        public const string Modelo = "lfm3-vl:latest";
        public const string Servidor = "http://127.0.0.1:11434";

        /// <summary>
        /// Límite de una inferencia. Medido en este equipo: la carga en frío del modelo más la primera foto tarda bastante
        /// más que las siguientes, por eso no puede ser corto; tampoco infinito, para no bloquear la cola de fotos.
        /// </summary>
        public static readonly TimeSpan TiempoMaximoInferencia = TimeSpan.FromSeconds(45);
        internal static readonly TimeSpan EsperaTurnoBot = TimeSpan.FromSeconds(30);
        internal static readonly TimeSpan EsperaTurnoEvaluacion = TimeSpan.FromMinutes(10);

        /// <summary>Lado mayor de la imagen enviada al modelo: suficiente para distinguir el objeto y barato de codificar.</summary>
        private const int LadoImagen = 512;

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
            _directorioControl = directorioControl ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Vision", "control-inferencia");
        }

        public async Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
        {
            // Una foto a la vez: el modelo comparte memoria de video y dos inferencias simultáneas se vuelven lentas.
            await _cupo.WaitAsync(ct);
            var reloj = Stopwatch.StartNew();
            try
            {
                // El bot atiende a una persona: espera su turno (máx. 30 s) con prioridad sobre evaluaciones y pruebas,
                // que esperan más y le ceden el modelo. Antes fallaba al instante si otra inferencia estaba activa.
                bool prioritario = EstadoVisionLocal.Origen == "bot";
                using var exclusion = _validarServidor
                    ? await ExclusionInferenciaLocal.AdquirirAsync(_directorioControl, prioritario ? EsperaTurnoBot : EsperaTurnoEvaluacion, prioritario, ct)
                    : null;
                ImagenResiduo preparada = PreparadorImagen.Preparar(imagen, LadoImagen);
                // Una foto borrosa o vacía no se consulta: el modelo inventa un objeto antes que abstenerse.
                if (CalidadImagen.Insuficiente(preparada.Contenido))
                {
                    EstadoVisionLocal.Registrar(_archivoEstado, true, reloj.Elapsed, null);
                    return new PropuestaVision(TipoPropuestaVision.ImagenBorrosa, null, null, ClaveObjeto: CatalogoObjetos.NoDistinguible);
                }
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct); limite.CancelAfter(TiempoMaximoInferencia);
                string foto = Convert.ToBase64String(preparada.Contenido);
                // Primero qué se ve, sin lista cerrada. Solo lo que puede ser un material pasa a la lectura por material.
                string nombre = await PedirAsync(VocabularioVision.InstruccionNombre, VocabularioVision.PreguntaNombre, null, 14, foto, false, limite.Token);
                string? clave = InterpretadorNombreObjeto.Clave(nombre, out _);
                PropuestaVision? lectura = VocabularioVision.NecesitaLectura(clave)
                    ? VocabularioVision.Leer(await PedirAsync(VocabularioVision.Instrucciones, VocabularioVision.Pregunta, VocabularioVision.Esquema, 40, foto, true, limite.Token))
                    : null;
                bool personas = VocabularioVision.NecesitaVerPersonas(clave) && JsonDocument.Parse(await PedirAsync(VocabularioVision.InstruccionPersonas, VocabularioVision.PreguntaPersonas,
                    VocabularioVision.EsquemaPersonas, 12, foto, true, limite.Token)).RootElement.GetProperty("respuesta").GetString() == "yes";
                PropuestaVision propuesta = VocabularioVision.Combinar(nombre, lectura, personas);
                // Validación independiente antes de ofrecer depositar: ¿es una persona o un animal vivo? (ronda 6, persona-03).
                // Cualquier respuesta distinta de «no» cuenta como sí: ante la duda, la foto solo orienta.
                if (VocabularioVision.NecesitaVerSujetoVivo(propuesta.ClaveObjeto))
                {
                    string respuesta = await PedirAsync(VocabularioVision.InstruccionPersonas, VocabularioVision.PreguntaSujetoVivo,
                        VocabularioVision.EsquemaPersonas, 12, foto, true, limite.Token);
                    propuesta = VocabularioVision.SiHaySujetoVivo(propuesta, !VocabularioVision.RespondioNo(respuesta));
                }
                EstadoVisionLocal.Registrar(_archivoEstado, true, reloj.Elapsed, null);
                return propuesta;
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


        private async Task<string> PedirAsync(string sistema, string pregunta, object? esquema, int maximoTokens, string foto, bool exigirFin, CancellationToken ct)
        {
            var cuerpo = new Dictionary<string, object>
            {
                ["model"] = Modelo,
                ["stream"] = false,
                ["keep_alive"] = _mantenerCargado,
                ["think"] = false,
                ["options"] = new { num_ctx = 2048, num_predict = maximoTokens, temperature = 0 },
                ["messages"] = new object[]
                {
                    new { role = "system", content = sistema },
                    new { role = "user", content = pregunta, images = new[] { foto } }
                },
            };
            if (esquema is not null) cuerpo["format"] = esquema;
            using var peticion = new HttpRequestMessage(HttpMethod.Post, Servidor + "/api/chat") { Content = JsonContent.Create(cuerpo) };
            using var respuesta = await _http.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!respuesta.IsSuccessStatusCode) throw new InvalidOperationException($"HTTP {(int)respuesta.StatusCode} del modelo local.");
            byte[] datos = await LecturaAcotada.DesdeHttpAsync(respuesta.Content, 64 * 1024, ct);
            using var doc = JsonDocument.Parse(datos); var raiz = doc.RootElement;
            // Un nombre cortado por el límite de tokens sirve igual: solo se buscan palabras en él.
            if (raiz.GetProperty("model").GetString() != Modelo || raiz.GetProperty("done").ValueKind != JsonValueKind.True
                || (exigirFin && raiz.GetProperty("done_reason").GetString() != "stop")) throw new InvalidDataException("El modelo local no terminó correctamente.");
            return raiz.GetProperty("message").GetProperty("content").GetString()!;
        }

        /// <summary>Carga el modelo al iniciar el bot para que la primera foto no pague la carga en frío. Un fallo no impide arrancar.</summary>
        public async Task PrecalentarAsync(CancellationToken ct = default)
        {
            try
            {
                using var peticion = new HttpRequestMessage(HttpMethod.Post, Servidor + "/api/generate")
                {
                    Content = JsonContent.Create(new { model = Modelo, keep_alive = _mantenerCargado, stream = false })
                };
                using var respuesta = await _http.SendAsync(peticion, ct);
            }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException) { }
        }

        public void Dispose() => _http.Dispose();
    }
}
