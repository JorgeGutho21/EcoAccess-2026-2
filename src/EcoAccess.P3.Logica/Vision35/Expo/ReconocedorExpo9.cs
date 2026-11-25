using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35.Expo
{
    /// <summary>Un material de los 9 reconocido en la foto, con la cantidad declarada por el modelo.</summary>
    public sealed record MaterialDetectadoExpo(MaterialExpo Material, int Cantidad, string NombreModelo);

    /// <summary>Estado global del reconocimiento Expo.</summary>
    public enum EstadoReconocimientoExpo
    {
        /// <summary>Solo materiales de los 9: se pueden abrir las canecas calculadas.</summary>
        Reconocido = 1,
        /// <summary>Hay materiales de los 9 y además objetos que no se reconocen: se abren solo con confirmación.</summary>
        ParcialConDesconocidos = 2,
        /// <summary>Hay un objeto de gestión especial o peligroso: no se abre ninguna caneca por foto.</summary>
        GestionEspecial = 3,
        /// <summary>Ningún material de los 9 (foto vacía u objetos fuera del alcance).</summary>
        SinMaterialesExpo = 4,
        /// <summary>El modelo no respondió o la respuesta no se pudo leer.</summary>
        Error = 5,
    }

    /// <summary>Tiempos medidos de una clasificación (milisegundos).</summary>
    public sealed record MedicionExpo(long PreprocesoMs, long InferenciaMs, long ValidacionMs, long TotalMs, bool DesdeCache, int LadoPx);

    /// <summary>Resultado completo y ya validado por C# de una foto en Modo ExpoSoftware.</summary>
    public sealed record ResultadoExpo9(
        EstadoReconocimientoExpo Estado,
        IReadOnlyList<MaterialDetectadoExpo> Materiales,
        IReadOnlyList<string> Desconocidos,
        IReadOnlyList<CategoriaMaterial> Canecas,
        CombinacionCanecas Combinacion,
        int DuracionAperturaMs,
        string Modelo,
        MedicionExpo Medicion,
        string HashSha256,
        string? Error = null)
    {
        /// <summary>Puede abrir sin preguntar: solo materiales reconocidos de los 9 y ningún objeto dudoso.</summary>
        public bool PuedeAbrirAutomaticamente => Estado == EstadoReconocimientoExpo.Reconocido && Canecas.Count > 0;

        /// <summary>Resumen para el ciudadano (Telegram, PC): materiales, categorías, canecas y tiempo.</summary>
        public string Resumen()
        {
            var sb = new StringBuilder();
            if (Estado == EstadoReconocimientoExpo.Error) return "⚠️ No pude analizar la foto en este momento. Intenta de nuevo o elige la caneca manualmente.";
            if (Materiales.Count > 0)
            {
                sb.AppendLine("Identifiqué:");
                foreach (var m in Materiales)
                    sb.AppendLine($"• {m.Material.Emoji} {Capital(m.Material.NombreCorto)}{(m.Cantidad > 1 ? " ×" + m.Cantidad.ToString(CultureInfo.InvariantCulture) : "")}: {Categoria(m.Material.Caneca)}");
            }
            if (Desconocidos.Count > 0)
                sb.AppendLine((Estado == EstadoReconocimientoExpo.GestionEspecial ? "⛔ Gestión especial (no va en estas canecas): " : "❔ No reconocido (no lo deposites aquí): ")
                    + string.Join(", ", Desconocidos));
            if (Canecas.Count > 0 && Estado != EstadoReconocimientoExpo.GestionEspecial)
            {
                sb.AppendLine();
                sb.AppendLine($"Canecas seleccionadas: {string.Join(", ", Canecas.Select(c => MotorReglasMultirresiduo.NombreCortoCaneca(c).ToLowerInvariant()))}.");
                sb.Append($"Tiempo de apertura: {DuracionAperturaMs / 1000} segundos.");
            }
            else if (Estado == EstadoReconocimientoExpo.SinMaterialesExpo)
                sb.Append("No encontré ninguno de los 9 materiales de la demostración. Acerca el residuo a la cámara o elige la caneca manualmente.");
            else if (Estado == EstadoReconocimientoExpo.GestionEspecial)
                sb.Append("No se abrirá ninguna caneca. Retira ese objeto y envía otra foto.");
            return sb.ToString().TrimEnd();
        }

        private static string Capital(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        public static string Categoria(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => "orgánico",
            CategoriaMaterial.Aprovechable => "aprovechable",
            CategoriaMaterial.NoAprovechable => "no aprovechable",
            _ => "sin categoría",
        };
    }

    /// <summary>
    /// Reconocedor del Modo ExpoSoftware: UNA sola inferencia por foto contra Ollama local, con el modelo cargado en memoria,
    /// conexión HTTP reutilizada, imagen reducida en memoria (lado mayor configurable, relación de aspecto preservada),
    /// respuesta JSON corta y caché por huella SHA-256. Las inferencias se serializan en el proceso para no saturar la GPU
    /// (Ollama encola además las de otros procesos). La validación y el cálculo de canecas son deterministas en C#.
    /// </summary>
    public sealed class ReconocedorExpo9
    {
        public const string ModeloPredeterminado = "lfm3-vl:latest";
        public const int LadoPredeterminado = 512;

        // Compartidos por todo el proceso: una conexión reutilizada y un turno de GPU.
        private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false }) { Timeout = Timeout.InfiniteTimeSpan };
        private static readonly SemaphoreSlim TurnoGpu = new(1, 1);
        private static readonly object BloqueoCache = new();
        private static readonly Dictionary<string, ResultadoExpo9> Cache = new(StringComparer.Ordinal);
        private static readonly Queue<string> OrdenCache = new();
        private const int CapacidadCache = 64;

        public static string Instruccion { get; } =
            "List every separate physical object on the table or in the hand. For each give a short common English name " +
            "(e.g. apple, half apple, banana peel, potato, half potato, brown paper bag, crumpled cardboard, plastic bottle, glass jar, " +
            "toilet paper, napkin, tissue, chips bag, snack wrapper, cable, pill blister, lemon, tomato, onion, sponge, razor). " +
            "Ignore hands, table and background. Answer with compact JSON only.";

        private static readonly object Esquema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["required"] = new[] { "items" },
            ["properties"] = new Dictionary<string, object>
            {
                ["items"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["required"] = new[] { "name", "count" },
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["name"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["count"] = new Dictionary<string, object> { ["type"] = "integer" },
                        },
                    },
                },
            },
        };

        private static readonly Regex ItemJson = new("\"name\"\\s*:\\s*\"(?<n>[^\"]{1,80})\"\\s*,\\s*\"count\"\\s*:\\s*(?<c>\\d{1,2})", RegexOptions.CultureInvariant);

        private readonly string _url;
        private readonly object _keepAlive;
        private readonly TimeSpan _limite;

        public string Modelo { get; }
        public int LadoPx { get; }

        public ReconocedorExpo9(string? modelo = null, int? ladoPx = null, string? urlOllama = null, TimeSpan? limite = null)
        {
            Modelo = (modelo ?? Environment.GetEnvironmentVariable("ECOACCESS_VISION_MODELO") ?? ModeloPredeterminado).Trim();
            int lado = ladoPx ?? (int.TryParse(Environment.GetEnvironmentVariable("ECOACCESS_VISION_LADO"), out int l) ? l : LadoPredeterminado);
            LadoPx = Math.Min(1024, Math.Max(256, lado));
            _url = (urlOllama ?? Environment.GetEnvironmentVariable("ECOACCESS_OLLAMA_URL") ?? "http://127.0.0.1:11434").TrimEnd('/');
            // En la estación anfitriona el modelo queda cargado indefinidamente (-1): la primera foto no paga la carga en frío.
            string ka = Environment.GetEnvironmentVariable("ECOACCESS_OLLAMA_KEEP_ALIVE") ?? "-1";
            _keepAlive = int.TryParse(ka, out int kaNum) ? kaNum : ka;
            _limite = limite ?? TimeSpan.FromSeconds(30);
        }

        /// <summary>Carga el modelo y su proyector visual con una imagen mínima. Devuelve los milisegundos o -1 si falló.</summary>
        public async Task<long> PrecalentarAsync(CancellationToken ct = default)
        {
            var reloj = Stopwatch.StartNew();
            try
            {
                using var bmp = new System.Drawing.Bitmap(64, 64);
                using (var g = System.Drawing.Graphics.FromImage(bmp)) g.Clear(System.Drawing.Color.White);
                using var ms = new MemoryStream(); bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                await TurnoGpu.WaitAsync(ct).ConfigureAwait(false);
                try { await PedirAsync(Convert.ToBase64String(ms.ToArray()), 8, ct).ConfigureAwait(false); }
                finally { TurnoGpu.Release(); }
                return reloj.ElapsedMilliseconds;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return -1; }
        }

        /// <summary>Clasifica una foto. Nunca lanza por fallos del modelo: devuelve <see cref="EstadoReconocimientoExpo.Error"/>.</summary>
        public async Task<ResultadoExpo9> ReconocerAsync(byte[] imagen, CancellationToken ct = default)
        {
            var total = Stopwatch.StartNew();
            string hash = Huella(imagen);
            string claveCache = hash + "|" + Modelo + "|" + LadoPx.ToString(CultureInfo.InvariantCulture);
            lock (BloqueoCache)
            {
                if (Cache.TryGetValue(claveCache, out var previo))
                    return previo with { Medicion = new MedicionExpo(0, 0, 0, total.ElapsedMilliseconds, true, LadoPx) };
            }

            long preMs, infMs = 0;
            string base64;
            try
            {
                var pre = Stopwatch.StartNew();
                base64 = Convert.ToBase64String(Reducir(imagen, LadoPx));
                preMs = pre.ElapsedMilliseconds;
            }
            catch (Exception e)
            {
                return Fallo(hash, "La imagen no es válida (" + e.GetType().Name + ").", total.ElapsedMilliseconds);
            }

            string contenido;
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
                limite.CancelAfter(_limite);
                await TurnoGpu.WaitAsync(limite.Token).ConfigureAwait(false);
                try
                {
                    var inf = Stopwatch.StartNew();
                    contenido = await PedirAsync(base64, 160, limite.Token).ConfigureAwait(false);
                    infMs = inf.ElapsedMilliseconds;
                }
                finally { TurnoGpu.Release(); }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e)
            {
                return Fallo(hash, e is OperationCanceledException ? "Tiempo máximo de inferencia excedido." : "Ollama no respondió (" + e.GetType().Name + ").", total.ElapsedMilliseconds);
            }

            var val = Stopwatch.StartNew();
            ResultadoExpo9 r = Interpretar(contenido, Modelo, hash);
            r = r with { Medicion = new MedicionExpo(preMs, infMs, val.ElapsedMilliseconds, total.ElapsedMilliseconds, false, LadoPx) };
            if (r.Estado != EstadoReconocimientoExpo.Error)
            {
                lock (BloqueoCache)
                {
                    if (!Cache.ContainsKey(claveCache))
                    {
                        Cache[claveCache] = r; OrdenCache.Enqueue(claveCache);
                        while (OrdenCache.Count > CapacidadCache) Cache.Remove(OrdenCache.Dequeue());
                    }
                }
            }
            return r;
        }

        /// <summary>
        /// Validación determinista de la respuesta del modelo: extrae cada (nombre, cantidad) aunque el JSON venga cortado,
        /// traduce a los 9 materiales, elimina duplicados por material y calcula las canecas y el tiempo de apertura.
        /// </summary>
        public static ResultadoExpo9 Interpretar(string? respuestaModelo, string modelo, string hash = "")
        {
            var cero = new MedicionExpo(0, 0, 0, 0, false, 0);
            if (string.IsNullOrWhiteSpace(respuestaModelo))
                return new ResultadoExpo9(EstadoReconocimientoExpo.Error, Array.Empty<MaterialDetectadoExpo>(), Array.Empty<string>(),
                    Array.Empty<CategoriaMaterial>(), CombinacionCanecas.Ninguna, 0, modelo, cero, hash, "Respuesta vacía del modelo.");

            var cantidades = new Dictionary<string, (MaterialExpo M, int C, string N)>(StringComparer.Ordinal);
            var desconocidos = new List<string>();
            bool especial = false;
            foreach (Match m in ItemJson.Matches(respuestaModelo))
            {
                string nombre = m.Groups["n"].Value.Trim();
                int cant = Math.Max(1, Math.Min(20, int.Parse(m.Groups["c"].Value, CultureInfo.InvariantCulture)));
                var (tipo, material) = CatalogoExpo9.Interpretar(nombre);
                switch (tipo)
                {
                    case TipoNombreExpo.Material:
                        cantidades[material!.Codigo] = cantidades.TryGetValue(material.Codigo, out var prev) ? (material, prev.C + cant, prev.N) : (material, cant, nombre);
                        break;
                    case TipoNombreExpo.GestionEspecial:
                        especial = true; if (!desconocidos.Contains(nombre, StringComparer.OrdinalIgnoreCase)) desconocidos.Add(nombre);
                        break;
                    case TipoNombreExpo.Desconocido:
                        if (!desconocidos.Contains(nombre, StringComparer.OrdinalIgnoreCase)) desconocidos.Add(nombre);
                        break;
                }
            }

            var materiales = cantidades.Values
                .OrderBy(v => OrdenCaneca(v.M.Caneca)).ThenBy(v => v.M.Codigo, StringComparer.Ordinal)
                .Select(v => new MaterialDetectadoExpo(v.M, v.C, v.N)).ToList();
            var canecas = MotorCombinacionesCanecas.CalcularCanecasUnicas(materiales.Select(m => m.Material.Caneca));
            EstadoReconocimientoExpo estado = especial ? EstadoReconocimientoExpo.GestionEspecial
                : materiales.Count == 0 ? EstadoReconocimientoExpo.SinMaterialesExpo
                : desconocidos.Count > 0 ? EstadoReconocimientoExpo.ParcialConDesconocidos
                : EstadoReconocimientoExpo.Reconocido;
            if (especial) canecas = Array.Empty<CategoriaMaterial>();
            return new ResultadoExpo9(estado, materiales, desconocidos, canecas, MotorCombinacionesCanecas.CalcularCombinacion(canecas),
                PoliticaTiemposApertura.DuracionMs(canecas.Count), modelo, cero, hash);
        }

        private static int OrdenCaneca(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => 0,
            CategoriaMaterial.Aprovechable => 1,
            _ => 2,
        };

        private ResultadoExpo9 Fallo(string hash, string error, long ms) =>
            new(EstadoReconocimientoExpo.Error, Array.Empty<MaterialDetectadoExpo>(), Array.Empty<string>(), Array.Empty<CategoriaMaterial>(),
                CombinacionCanecas.Ninguna, 0, Modelo, new MedicionExpo(0, 0, 0, ms, false, LadoPx), hash, error);

        private async Task<string> PedirAsync(string base64, int maxTokens, CancellationToken ct)
        {
            var cuerpo = new Dictionary<string, object>
            {
                ["model"] = Modelo,
                ["stream"] = false,
                ["keep_alive"] = _keepAlive,
                ["think"] = false,
                ["format"] = Esquema,
                ["options"] = new Dictionary<string, object> { ["temperature"] = 0, ["num_ctx"] = 2048, ["num_predict"] = maxTokens },
                ["messages"] = new object[]
                {
                    new Dictionary<string, object> { ["role"] = "system", ["content"] = Instruccion },
                    new Dictionary<string, object> { ["role"] = "user", ["content"] = "Objects?", ["images"] = new[] { base64 } },
                },
            };
            using var peticion = new HttpRequestMessage(HttpMethod.Post, _url + "/api/chat")
            {
                Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json"),
            };
            using var respuesta = await Http.SendAsync(peticion, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
            string texto = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!respuesta.IsSuccessStatusCode) throw new HttpRequestException("HTTP " + (int)respuesta.StatusCode);
            using var doc = JsonDocument.Parse(texto);
            return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
        }

        /// <summary>Reduce en memoria (nunca toca el archivo original) preservando la relación de aspecto.</summary>
        public static byte[] Reducir(byte[] original, int lado)
        {
            using var entrada = new MemoryStream(original, writable: false);
            using var img = System.Drawing.Image.FromStream(entrada, useEmbeddedColorManagement: false, validateImageData: true);
            int mayor = Math.Max(img.Width, img.Height);
            double f = mayor <= lado ? 1.0 : (double)lado / mayor;
            int w = Math.Max(1, (int)Math.Round(img.Width * f)), h = Math.Max(1, (int)Math.Round(img.Height * f));
            using var bmp = new System.Drawing.Bitmap(w, h);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, w, h);
            }
            var codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
            using var parametros = new System.Drawing.Imaging.EncoderParameters(1);
            parametros.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
            using var salida = new MemoryStream();
            bmp.Save(salida, codec, parametros);
            return salida.ToArray();
        }

        public static string Huella(byte[] datos)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(datos)).Replace("-", "").ToLowerInvariant();
        }
    }
}
