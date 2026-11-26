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
    /// <remarks><see cref="Duda"/> no nulo: el material es la mejor hipótesis pero no hay evidencia suficiente para abrir solo
    /// (cítrico que puede ser lulo, material elegido solo por descarte en una pregunta sí/no, verificación fallida u omitida);
    /// se muestra con su caneca sugerida y la apertura exige confirmación.</remarks>
    public sealed record MaterialDetectadoExpo(MaterialExpo Material, int Cantidad, string NombreModelo, string? Duda = null)
    {
        public bool PorConfirmar => Duda is not null;
    }

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
    /// <remarks><see cref="InferenciaMs"/> incluye la pregunta principal y las de verificación; <see cref="VerificacionMs"/> y
    /// <see cref="Preguntas"/> (número de llamadas al modelo) permiten separar ambas en las evaluaciones de latencia.</remarks>
    public sealed record MedicionExpo(long PreprocesoMs, long InferenciaMs, long ValidacionMs, long TotalMs, bool DesdeCache, int LadoPx,
        long VerificacionMs = 0, int Preguntas = 0);

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
        string? Error = null,
        string? RespuestaModelo = null)
    {
        /// <summary>Puede abrir sin preguntar: solo materiales reconocidos de los 9 y ningún objeto dudoso.</summary>
        public bool PuedeAbrirAutomaticamente => Estado == EstadoReconocimientoExpo.Reconocido && Canecas.Count > 0 && !Materiales.Any(m => m.PorConfirmar);

        /// <summary>Resumen para el ciudadano (Telegram, PC): materiales, categorías, canecas y tiempo.</summary>
        public string Resumen()
        {
            var sb = new StringBuilder();
            if (Estado == EstadoReconocimientoExpo.Error) return "⚠️ No pude analizar la foto en este momento. Intenta de nuevo o elige la caneca manualmente.";
            if (Materiales.Count > 0)
            {
                sb.AppendLine("Identifiqué:");
                foreach (var m in Materiales)
                    sb.AppendLine($"• {m.Material.Emoji} {Capital(m.Material.NombreCorto)}{(m.Cantidad > 1 ? " ×" + m.Cantidad.ToString(CultureInfo.InvariantCulture) : "")}: {Categoria(m.Material.Caneca)}{(m.PorConfirmar ? " (por confirmar: " + m.Duda + ")" : "")}");
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
            "Identify every separate waste object held in the hand or lying on a surface. Ignore hands, people, tables, walls, " +
            "screens and computers. For each object first write 'name': a short common English name of what it really is " +
            "(e.g. tomato, onion, playing card, sponge, medicine box, USB cable, plastic bag, pill blister, potato, lime, crumpled paper, foil ball, dropper bottle, glue bottle). " +
            "Then write 'label': the matching label from this list, or other if it is not clearly one of them:\n" +
            "potato = potato or half potato, brown skin, pale flesh;\n" +
            "lime = small green lime or lemon, whole or cut;\n" +
            "green_apple = green apple, whole or cut;\n" +
            "lulo = round orange fruit with fuzzy skin, green juicy flesh with seeds when cut (naranjilla);\n" +
            "banana_peel = banana peel;\n" +
            "aluminum_foil = shiny silver crumpled aluminum foil ball;\n" +
            "serum_bottle = small pink or salmon glass bottle with white dropper cap;\n" +
            "notebook_paper = crumpled white paper with printed grid squares or ruled lines;\n" +
            "kraft_paper = crumpled brown kraft paper or cardboard;\n" +
            "glue_bottle = small white plastic super glue bottle with pointed nozzle and green label;\n" +
            "used_napkin = crumpled soft thin white napkin or tissue, plain or with dark marks, no printed grid;\n" +
            "sponge = kitchen dish sponge, soft foam pad, often covered with a mesh or a scouring layer;\n" +
            "paper_box = small printed cardboard box or package, e.g. an empty medicine or product box;\n" +
            "other = anything else (tomato, onion, card, plastic bag, blister, battery, cable...).\n" +
            "Answer compact JSON: {\"items\":[{\"name\":\"...\",\"label\":\"...\",\"count\":1}]}";

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
                        ["required"] = new[] { "name", "label", "count" },
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["name"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["label"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["count"] = new Dictionary<string, object> { ["type"] = "integer" },
                        },
                    },
                },
            },
        };

        private static readonly Regex ObjetoJson = new("\\{[^{}]{0,400}\\}", RegexOptions.CultureInvariant);
        private static readonly Regex CampoNombre = new("\"name\"\\s*:\\s*\"(?<v>[^\"]{1,80})\"", RegexOptions.CultureInvariant);
        private static readonly Regex CampoEtiqueta = new("\"label\"\\s*:\\s*\"(?<v>[^\"]{1,80})\"", RegexOptions.CultureInvariant);
        private static readonly Regex CampoCantidad = new("\"count\"\\s*:\\s*(?<v>\\d{1,2})", RegexOptions.CultureInvariant);

        private readonly string _url;
        private readonly object _keepAlive;
        private readonly TimeSpan _limite;

        public string Modelo { get; }
        public int LadoPx { get; }
        /// <summary>Máximo de tokens de la respuesta principal (ECOACCESS_VISION_NUM_PREDICT; 220 por defecto).</summary>
        public int NumPredict { get; }

        public const int NumPredictPredeterminado = 220;

        public ReconocedorExpo9(string? modelo = null, int? ladoPx = null, string? urlOllama = null, TimeSpan? limite = null, int? numPredict = null)
        {
            Modelo = (modelo ?? Environment.GetEnvironmentVariable("ECOACCESS_VISION_MODELO") ?? ModeloPredeterminado).Trim();
            int lado = ladoPx ?? (int.TryParse(Environment.GetEnvironmentVariable("ECOACCESS_VISION_LADO"), out int l) ? l : LadoPredeterminado);
            LadoPx = Math.Min(1024, Math.Max(256, lado));
            int np = numPredict ?? (int.TryParse(Environment.GetEnvironmentVariable("ECOACCESS_VISION_NUM_PREDICT"), out int n) ? n : NumPredictPredeterminado);
            NumPredict = Math.Min(400, Math.Max(48, np));
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
                    contenido = await PedirAsync(base64, NumPredict, limite.Token).ConfigureAwait(false);
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
            long verMs = 0;
            int preguntas = 1;
            if (r.Estado is EstadoReconocimientoExpo.Reconocido or EstadoReconocimientoExpo.ParcialConDesconocidos)
            {
                var ver = Stopwatch.StartNew();
                var verificado = await VerificarConfundiblesAsync(r, imagen, ct).ConfigureAwait(false);
                r = verificado.R;
                preguntas += verificado.Preguntas;
                verMs = ver.ElapsedMilliseconds;
            }
            r = r with
            {
                RespuestaModelo = contenido,
                Medicion = new MedicionExpo(preMs, infMs + verMs, val.ElapsedMilliseconds - verMs, total.ElapsedMilliseconds, false, LadoPx, verMs, preguntas),
            };
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
        /// Pares que el modelo confunde (medido con las fotos reales de la estación) y que cambian de caneca: se confirman con
        /// UNA pregunta cerrada sí/no sobre la misma foto. «Sí» elige el primero; «no», el segundo. Si la pregunta falla, el
        /// residuo pasa a «por confirmar» y nada se abre solo. Si ambos del par aparecen en la foto, no se pregunta.
        /// </summary>
        internal static readonly (string Si, string No, string Pregunta, int Lado)[] Verificaciones =
        {
            ("serum_bottle", "glue_bottle", "Look at the small bottle. Is it made of pink or salmon colored glass?", 512),
            ("kraft_paper", "notebook_paper|used_napkin", "Look at the crumpled paper. Is it brown (kraft paper or cardboard)?", 512),
            ("notebook_paper", "used_napkin", "Look closely at the crumpled white paper. Are there thin blue printed lines forming squares (graph paper) or ruled lines?", 768),
        };

        /// <summary>
        /// Confirmaciones de un solo material (sin pareja de descarte): si la respuesta no es «sí», el material queda por confirmar.
        /// Papa: el modelo nombra «potato» a un aguacate partido y a una bolsa plástica arrugada (medido en las externas).
        /// </summary>
        internal static readonly (string Etiqueta, string Pregunta, int Lado)[] Confirmaciones =
        {
            ("potato", "Is there a potato or half potato in the photo?", 512),
        };

        internal const string DudaDescarte = "elegido solo por descarte en la verificación";
        internal const string DudaSinVerificar = "la verificación no respondió";
        internal const string DudaVariosParecidos = "varios objetos parecidos de distinta caneca sin verificar";
        internal const string DudaNoConfirmado = "la verificación no lo confirmó";

        private async Task<bool?> PreguntarImagenAsync(byte[] imagen, string pregunta, int lado, CancellationToken ct)
        {
            try
            {
                string b64 = Convert.ToBase64String(Reducir(imagen, lado));
                await TurnoGpu.WaitAsync(ct).ConfigureAwait(false);
                try { return await PreguntarAsync(b64, pregunta, ct).ConfigureAwait(false); }
                finally { TurnoGpu.Release(); }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Aplica a la lista de materiales la respuesta a una verificación de par (<see cref="Verificaciones"/>). «Sí» es evidencia
        /// directa del primero; «no» solo descarta, así que si cambia el material el nuevo queda por confirmar (servilleta elegida
        /// porque «no tiene cuadrícula», pegamento porque «no es vidrio rosado»: así abrían la negra una bolsa plástica, papel liso
        /// o una botella plástica). Sin respuesta, por confirmar. Función pura para poder probarla sin el modelo.
        /// </summary>
        internal static MaterialDetectadoExpo AplicarVerificacion(MaterialDetectadoExpo actual, string si, string[] grupoNo, bool? respuesta)
        {
            if (respuesta is null) return actual with { Duda = actual.Duda ?? DudaSinVerificar };
            // «No» con varias opciones (papel blanco): se conserva la etiqueta del modelo y decide la pregunta siguiente.
            string destino = respuesta.Value ? si : grupoNo.Length == 1 ? grupoNo[0] : grupoNo.Contains(actual.Material.Etiqueta) ? actual.Material.Etiqueta : grupoNo[0];
            var elegido = CatalogoExpo9.PorEtiqueta(destino)!;
            if (elegido.Codigo == actual.Material.Codigo)
                return respuesta.Value && actual.Duda == DudaDescarte ? actual with { Duda = CatalogoExpo9.MotivoDuda(actual.NombreModelo, elegido) } : actual;
            // Cambio de material: la duda propia del material anterior no se hereda; la del nuevo sí (servilleta, cítrico).
            string? duda = respuesta.Value ? CatalogoExpo9.MotivoDuda(actual.NombreModelo, elegido) : DudaDescarte;
            return new MaterialDetectadoExpo(elegido, actual.Cantidad, actual.NombreModelo + " → " + elegido.Etiqueta, duda);
        }

        /// <summary>
        /// Regla multirresiduo: con dos o más objetos del mismo grupo confundible la pregunta sí/no no sabría a cuál se refiere,
        /// así que no se hace; si esos objetos van a canecas distintas (p. ej. kraft + servilleta), quedan por confirmar.
        /// Devuelve true si marcó alguno.
        /// </summary>
        internal static bool MarcarParecidosSinVerificar(List<MaterialDetectadoExpo> materiales, string si, string[] grupoNo)
        {
            bool EnGrupo(MaterialDetectadoExpo m) => m.Material.Etiqueta == si || grupoNo.Contains(m.Material.Etiqueta);
            if (materiales.Count(EnGrupo) < 2 || materiales.Where(EnGrupo).Select(m => m.Material.Caneca).Distinct().Count() < 2) return false;
            bool marco = false;
            for (int k = 0; k < materiales.Count; k++)
                if (EnGrupo(materiales[k]) && !materiales[k].PorConfirmar) { materiales[k] = materiales[k] with { Duda = DudaVariosParecidos }; marco = true; }
            return marco;
        }

        private async Task<(ResultadoExpo9 R, int Preguntas)> VerificarConfundiblesAsync(ResultadoExpo9 r, byte[] imagen, CancellationToken ct)
        {
            var materiales = r.Materiales.ToList();
            var desconocidos = r.Desconocidos.ToList();
            bool cambio = false;
            int preguntas = 0;
            foreach (var (si, no, pregunta, lado) in Verificaciones)
            {
                var grupoNo = no.Split('|');
                bool EnGrupo(MaterialDetectadoExpo m) => m.Material.Etiqueta == si || grupoNo.Contains(m.Material.Etiqueta);
                int i = materiales.FindIndex(EnGrupo);
                if (i < 0) continue;
                if (materiales.Count(EnGrupo) > 1) { cambio |= MarcarParecidosSinVerificar(materiales, si, grupoNo); continue; }
                preguntas++;
                bool? respuesta = await PreguntarImagenAsync(imagen, pregunta, lado, ct).ConfigureAwait(false);
                var nuevo = AplicarVerificacion(materiales[i], si, grupoNo, respuesta);
                if (nuevo != materiales[i]) { materiales[i] = nuevo; cambio = true; }
            }
            foreach (var (etiqueta, pregunta, lado) in Confirmaciones)
            {
                int i = materiales.FindIndex(m => m.Material.Etiqueta == etiqueta && !m.PorConfirmar);
                if (i < 0) continue;
                preguntas++;
                bool? respuesta = await PreguntarImagenAsync(imagen, pregunta, lado, ct).ConfigureAwait(false);
                if (respuesta != true) { materiales[i] = materiales[i] with { Duda = respuesta is null ? DudaSinVerificar : DudaNoConfirmado }; cambio = true; }
            }
            if (!cambio) return (r, preguntas);
            var canecas = MotorCombinacionesCanecas.CalcularCanecasUnicas(materiales.Select(m => m.Material.Caneca));
            var estado = materiales.Count == 0 ? EstadoReconocimientoExpo.SinMaterialesExpo
                : desconocidos.Count > 0 || materiales.Any(m => m.PorConfirmar) ? EstadoReconocimientoExpo.ParcialConDesconocidos : EstadoReconocimientoExpo.Reconocido;
            return (r with
            {
                Estado = estado,
                Materiales = materiales.OrderBy(m => OrdenCaneca(m.Material.Caneca)).ThenBy(m => m.Material.Codigo, StringComparer.Ordinal).ToList(),
                Desconocidos = desconocidos,
                Canecas = canecas,
                Combinacion = MotorCombinacionesCanecas.CalcularCombinacion(canecas),
                DuracionAperturaMs = PoliticaTiemposApertura.DuracionMs(canecas.Count),
            }, preguntas);
        }

        private async Task<bool?> PreguntarAsync(string base64, string pregunta, CancellationToken ct)
        {
            var cuerpo = new Dictionary<string, object>
            {
                ["model"] = Modelo,
                ["stream"] = false,
                ["keep_alive"] = _keepAlive,
                ["think"] = false,
                ["format"] = new Dictionary<string, object>
                {
                    ["type"] = "object", ["required"] = new[] { "answer" },
                    ["properties"] = new Dictionary<string, object> { ["answer"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = new[] { "yes", "no" } } },
                },
                ["options"] = new Dictionary<string, object> { ["temperature"] = 0, ["num_ctx"] = 2048, ["num_predict"] = 12 },   // mismo contexto: sin recargar el modelo
                ["messages"] = new object[]
                {
                    new Dictionary<string, object> { ["role"] = "user", ["content"] = pregunta + " Answer JSON {\"answer\":\"yes\"} or {\"answer\":\"no\"}.", ["images"] = new[] { base64 } },
                },
            };
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limite.CancelAfter(TimeSpan.FromSeconds(15));
            using var peticion = new HttpRequestMessage(HttpMethod.Post, _url + "/api/chat") { Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json") };
            using var respuesta = await Http.SendAsync(peticion, HttpCompletionOption.ResponseContentRead, limite.Token).ConfigureAwait(false);
            if (!respuesta.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false));
            string texto = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
            return texto.Contains("\"yes\"") ? true : texto.Contains("\"no\"") ? false : null;
        }

        /// <summary>
        /// Validación determinista de la respuesta del modelo: extrae cada (etiqueta, cantidad) aunque el JSON venga cortado,
        /// traduce a los 11 materiales, elimina duplicados por material y calcula las canecas y el tiempo de apertura.
        /// </summary>
        public static ResultadoExpo9 Interpretar(string? respuestaModelo, string modelo, string hash = "")
        {
            var cero = new MedicionExpo(0, 0, 0, 0, false, 0);
            if (string.IsNullOrWhiteSpace(respuestaModelo))
                return new ResultadoExpo9(EstadoReconocimientoExpo.Error, Array.Empty<MaterialDetectadoExpo>(), Array.Empty<string>(),
                    Array.Empty<CategoriaMaterial>(), CombinacionCanecas.Ninguna, 0, modelo, cero, hash, "Respuesta vacía del modelo.");

            var cantidades = new Dictionary<string, (MaterialExpo M, int C, string N, string? D)>(StringComparer.Ordinal);
            var desconocidos = new List<string>();
            bool especial = false;
            foreach (Match m in ObjetoJson.Matches(respuestaModelo))
            {
                string? libre = CampoNombre.Match(m.Value) is { Success: true } mn ? mn.Groups["v"].Value.Trim() : null;
                string? etiqueta = CampoEtiqueta.Match(m.Value) is { Success: true } me ? me.Groups["v"].Value.Trim() : null;
                if (libre is null && etiqueta is null) continue;
                int cant = CampoCantidad.Match(m.Value) is { Success: true } mc ? Math.Max(1, Math.Min(20, int.Parse(mc.Groups["v"].Value, CultureInfo.InvariantCulture))) : 1;
                string nombre = libre is not null && etiqueta is not null && !string.Equals(libre, etiqueta, StringComparison.OrdinalIgnoreCase) ? libre + " / " + etiqueta : (libre ?? etiqueta)!;
                var (tipo, material) = CatalogoExpo9.Interpretar(libre, etiqueta);
                switch (tipo)
                {
                    case TipoNombreExpo.Material:
                    {
                        string? duda = CatalogoExpo9.MotivoDuda(libre, material!);
                        cantidades[material.Codigo] = cantidades.TryGetValue(material.Codigo, out var prev) ? (material, prev.C + cant, prev.N, prev.D ?? duda) : (material, cant, nombre, duda);
                        break;
                    }
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
                .Select(v => new MaterialDetectadoExpo(v.M, v.C, v.N, v.D)).ToList();
            var canecas = MotorCombinacionesCanecas.CalcularCanecasUnicas(materiales.Select(m => m.Material.Caneca));
            EstadoReconocimientoExpo estado = especial ? EstadoReconocimientoExpo.GestionEspecial
                : materiales.Count == 0 ? EstadoReconocimientoExpo.SinMaterialesExpo
                : desconocidos.Count > 0 || materiales.Any(m => m.PorConfirmar) ? EstadoReconocimientoExpo.ParcialConDesconocidos
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
