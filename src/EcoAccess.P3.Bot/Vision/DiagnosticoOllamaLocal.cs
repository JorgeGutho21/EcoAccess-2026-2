using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using EcoAccess.P3.Bot.Transportes;

namespace EcoAccess.P3.Bot.Vision
{

    public sealed record SaludOllama(bool Disponible, bool ModeloInstalado, bool ModeloResidente,
        int OtrosModelosResidentes, string? Version, string Mensaje)
    {
        public bool ListoParaInferencia => Disponible && ModeloInstalado && OtrosModelosResidentes == 0;
    }

    /// <summary>Solo GET local y acotado. No consulta Oracle, genera imágenes ni descarga modelos.</summary>
    public sealed class DiagnosticoOllamaLocal : IDisposable
    {
        private readonly HttpClient _http;
        public DiagnosticoOllamaLocal() : this(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })) { }
        internal DiagnosticoOllamaLocal(HttpClient http) { _http = http; _http.Timeout = Timeout.InfiniteTimeSpan; }

        internal static async Task<JsonDocument> LeerAsync(HttpClient http, string recurso, CancellationToken ct)
        {
            using var r = await http.GetAsync(ClasificadorOllamaLocal.Servidor + recurso, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!r.IsSuccessStatusCode) throw new InvalidDataException("El servicio local no respondió correctamente.");
            return JsonDocument.Parse(await LecturaAcotada.DesdeHttpAsync(r.Content, 128 * 1024, ct), new JsonDocumentOptions { MaxDepth = 16 });
        }

        public async Task<SaludOllama> ComprobarAsync(CancellationToken ct = default)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct); limite.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                using var version = await LeerAsync(_http, "/api/version", limite.Token);
                string? v = version.RootElement.GetProperty("version").GetString();
                if (Compat.IsNullOrWhiteSpace(v) || v.Length > 40 || v.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c is not ('.' or '-')))
                    throw new InvalidDataException("Versión local inválida.");
                using var etiquetas = await LeerAsync(_http, "/api/tags", limite.Token);
                bool instalado = etiquetas.RootElement.GetProperty("models").EnumerateArray()
                    .Any(m => m.GetProperty("name").GetString() == ClasificadorOllamaLocal.Modelo);
                using var residentes = await LeerAsync(_http, "/api/ps", limite.Token);
                var nombres = residentes.RootElement.GetProperty("models").EnumerateArray().Select(m => m.GetProperty("name").GetString()).ToArray();
                int otros = nombres.Count(n => n != ClasificadorOllamaLocal.Modelo);
                return new(true, instalado, nombres.Contains(ClasificadorOllamaLocal.Modelo), otros, v,
                    !instalado ? "Falta el modelo local. Puedes elegir el material manualmente." :
                    otros > 0 ? "Otro modelo ocupa Ollama. No se descargó ni detuvo ningún modelo; la selección manual sigue disponible." :
                    "Ollama y Qwen están disponibles. Este diagnóstico no ejecutó reconocimiento ni demuestra precisión.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return new(false, false, false, 0, null, "Ollama no está disponible o su respuesta no es válida. Puedes elegir el material manualmente."); }
        }

        internal static async Task ExigirSinCompetenciaAsync(HttpClient http, CancellationToken ct)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct); limite.CancelAfter(TimeSpan.FromSeconds(5));
            using var doc = await LeerAsync(http, "/api/ps", limite.Token);
            if (doc.RootElement.GetProperty("models").EnumerateArray().Any(m => m.GetProperty("name").GetString() != ClasificadorOllamaLocal.Modelo))
                throw new InvalidOperationException("Ollama está ocupado con otro modelo. Usa la selección manual o libera ese modelo desde su aplicación.");
        }
        public void Dispose() => _http.Dispose();
    }
}
