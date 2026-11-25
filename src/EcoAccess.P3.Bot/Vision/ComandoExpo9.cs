using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35.Expo;

namespace EcoAccess.P3.Bot.Vision
{
    /// <summary>
    /// Evaluación del Modo ExpoSoftware sin hardware: recorre datasets/exposoftware9/manifiesto.json con el mismo
    /// <see cref="ReconocedorExpo9"/> que usan Telegram y el PC y calcula exactitud por material, por caneca, multirresiduo,
    /// falsas aperturas y latencias (media, mediana, P95, máximo). No mueve motores ni escribe en Oracle.
    /// Uso: --evaluar-expo9 [manifiesto.json] [--lado 384|448|512] [--num-predict n] [--modelo nombre] [--salida informe.md]
    /// [--detalle casos.jsonl] [--conjuntos real_ajuste,negativa,compuesta,evaluacion_externa,complementaria,negativa_externa]
    /// El detalle por caso (respuesta cruda del modelo incluida) se guarda junto al informe con extensión .jsonl.
    /// </summary>
    internal static class ComandoExpo9
    {
        /// <summary>
        /// Un caso del manifiesto. <see cref="CanecaReferencia"/> solo existe en los materiales complementarios (fuera del catálogo
        /// operativo): es la caneca que les corresponde por la Resolución 2184 de 2019 y sirve para distinguir una apertura
        /// fuera de catálogo pero en la caneca correcta de una apertura incorrecta. Las columnas desconocidas se ignoran.
        /// </summary>
        internal sealed record Caso(string Archivo, string Conjunto, string Origen, string[] Materiales, string[] Canecas, bool Ajuste, bool Especial,
            string? CanecaReferencia = null, string? Descripcion = null)
        {
            public bool Complementaria => string.Equals(Conjunto, "complementaria", StringComparison.OrdinalIgnoreCase);
            public bool Negativa => Conjunto.StartsWith("negativa", StringComparison.OrdinalIgnoreCase);
            /// <summary>Canecas que se pueden abrir sin que sea una falsa apertura.</summary>
            public string[] CanecasPermitidas => Complementaria && CanecaReferencia is { Length: > 0 } r ? new[] { r } : Canecas;
        }

        /// <summary>Resultado de un caso ya clasificado según las reglas de la evaluación (ver <see cref="Clasificar"/>).</summary>
        internal sealed record Fila(Caso C, ResultadoExpo9 R, bool MatOk, bool CanOk, int Extra, int Omitidas, bool Falsa, bool AutoAbre, bool Correcto);

        /// <summary>
        /// Reglas de puntuación. Falsa apertura: se habría abierto SIN preguntar una caneca que no corresponde (para un complementario,
        /// distinta de su caneca de referencia; para una negativa, cualquiera). Correcto: catálogo y compuestas → apertura automática
        /// con exactamente las canecas esperadas; negativas → ninguna apertura automática; complementarios → rechazo (sin apertura
        /// automática) o apertura limitada a su caneca de referencia.
        /// </summary>
        internal static Fila Clasificar(Caso c, ResultadoExpo9 r)
        {
            var detMat = r.Materiales.Select(m => m.Material.Codigo).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var detCan = r.Canecas.Select(Nombre).ToArray();
            bool matOk = detMat.SequenceEqual(c.Materiales.OrderBy(x => x, StringComparer.Ordinal));
            bool canOk = detCan.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(c.Canecas.OrderBy(x => x, StringComparer.Ordinal));
            bool auto = r.PuedeAbrirAutomaticamente;
            int extra = detCan.Except(c.CanecasPermitidas).Count(), omit = c.Canecas.Except(detCan).Count();
            bool falsa = auto && extra > 0;
            bool correcto = c.Negativa ? !auto : c.Complementaria ? !falsa : auto && canOk;
            return new Fila(c, r, matOk, canOk, extra, omit, falsa, auto, correcto);
        }

        internal static List<Caso> LeerManifiesto(string manifiesto, ISet<string>? conjuntos)
        {
            var casos = new List<Caso>();
            using var doc = JsonDocument.Parse(File.ReadAllText(manifiesto, Encoding.UTF8));
            foreach (var e in doc.RootElement.GetProperty("entradas").EnumerateArray())
            {
                string conjunto = e.GetProperty("conjunto").GetString()!;
                if (conjuntos is not null && !conjuntos.Contains(conjunto)) continue;
                if (e.TryGetProperty("activa", out var act) && act.ValueKind == JsonValueKind.False) continue;   // retiradas a _archivo
                casos.Add(new Caso(e.GetProperty("archivo").GetString()!, conjunto, e.TryGetProperty("origen", out var o) ? o.GetString() ?? "" : "",
                    Lista(e, "materiales"), Lista(e, "canecas"),
                    e.TryGetProperty("usada_en_ajuste", out var aj) && aj.ValueKind == JsonValueKind.True,
                    e.TryGetProperty("gestion_especial", out var ge) && ge.ValueKind == JsonValueKind.True,
                    e.TryGetProperty("caneca_referencia", out var cr) && cr.ValueKind == JsonValueKind.String ? cr.GetString() : null,
                    e.TryGetProperty("descripcion", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null));
            }
            return casos;
        }

        private static string[] Lista(JsonElement e, string nombre) =>
            e.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToArray() : Array.Empty<string>();

        public static async Task<int> EvaluarAsync(string[] args)
        {
            string manifiesto = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal) && a.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                ?? Path.Combine("datasets", "exposoftware9", "manifiesto.json");
            int? lado = Opcion(args, "--lado") is { } l ? int.Parse(l, CultureInfo.InvariantCulture) : null;
            int? numPredict = Opcion(args, "--num-predict") is { } np ? int.Parse(np, CultureInfo.InvariantCulture) : null;
            string? modelo = Opcion(args, "--modelo");
            string? salida = Opcion(args, "--salida");
            string? detalle = Opcion(args, "--detalle") ?? (salida is null ? null : Path.ChangeExtension(salida, ".jsonl"));
            var conjuntos = (Opcion(args, "--conjuntos") ?? "evaluacion,multirresiduo,negativa").Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(manifiesto)) { Console.Error.WriteLine("No existe el manifiesto: " + manifiesto); return 2; }
            string baseDir = Path.GetDirectoryName(Path.GetFullPath(manifiesto))!;
            var casos = LeerManifiesto(manifiesto, conjuntos);

            var rec = new ReconocedorExpo9(modelo, lado, numPredict: numPredict);
            Console.WriteLine($"Modelo {rec.Modelo} · lado {rec.LadoPx}px · num_predict {rec.NumPredict} · {casos.Count} casos · precalentando…");
            long frio = await rec.PrecalentarAsync();
            Console.WriteLine($"Precalentamiento: {frio} ms");

            var filas = new List<Fila>();
            var lineas = new List<string>();
            foreach (var c in casos)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(baseDir, c.Archivo.Replace('/', Path.DirectorySeparatorChar)));
                ResultadoExpo9 r = await rec.ReconocerAsync(bytes);
                var f = Clasificar(c, r);
                filas.Add(f);
                lineas.Add(JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["archivo"] = c.Archivo, ["conjunto"] = c.Conjunto, ["origen"] = c.Origen, ["usada_en_ajuste"] = c.Ajuste,
                    ["esperado_materiales"] = c.Materiales, ["esperado_canecas"] = c.Canecas, ["caneca_referencia"] = c.CanecaReferencia,
                    ["estado"] = r.Estado.ToString(), ["materiales"] = r.Materiales.Select(m => m.Material.Codigo + "=" + m.NombreModelo).ToArray(),
                    ["desconocidos"] = r.Desconocidos.ToArray(), ["canecas"] = r.Canecas.Select(Nombre).ToArray(),
                    ["abre_automatico"] = f.AutoAbre, ["falsa_apertura"] = f.Falsa, ["correcto"] = f.Correcto,
                    ["inferencia_ms"] = r.Medicion.InferenciaMs, ["verificacion_ms"] = r.Medicion.VerificacionMs, ["preguntas"] = r.Medicion.Preguntas,
                    ["total_ms"] = r.Medicion.TotalMs, ["respuesta"] = r.RespuestaModelo,
                }));
                Console.WriteLine($"{(f.Correcto ? "OK " : f.Falsa ? "FA!" : "MAL")} {r.Medicion.InferenciaMs,5} ms {c.Archivo,-58} esperado [{string.Join(",", c.Materiales.DefaultIfEmpty(c.CanecaReferencia ?? "-"))}] → [{string.Join(",", r.Materiales.Select(m => m.Material.Codigo + "=" + m.NombreModelo))}] {r.Estado}{(r.Desconocidos.Count > 0 ? " ?" + string.Join("|", r.Desconocidos) : "")}");
            }

            string informe = Informe(filas, rec, frio, manifiesto);
            Console.WriteLine();
            Console.WriteLine(informe);
            if (salida is not null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(salida))!); File.WriteAllText(salida, informe, new UTF8Encoding(false)); }
            if (detalle is not null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(detalle))!); File.WriteAllLines(detalle, lineas, new UTF8Encoding(false)); }
            return 0;
        }

        private static string FilaTabla(string nombre, IReadOnlyCollection<Fila> g) =>
            $"| {nombre} | {g.Count} | {g.Count(x => x.Correcto)} ({Pct(g.Count(x => x.Correcto), g.Count)}) | {g.Count(x => x.AutoAbre && x.CanOk)} | {g.Count(x => x.CanOk)} | " +
            $"{g.Count(x => !x.AutoAbre && x.R.Estado == EstadoReconocimientoExpo.ParcialConDesconocidos)} | {g.Count(x => !x.AutoAbre)} | **{g.Count(x => x.Falsa)}** | {g.Sum(x => x.Omitidas)} |";

        internal static string Informe(List<Fila> f, ReconocedorExpo9 rec, long frio, string manifiesto)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Evaluación Modo ExpoSoftware (catálogo de 11) — {rec.Modelo}, lado {rec.LadoPx}px, num_predict {rec.NumPredict}");
            sb.AppendLine();
            sb.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm} · Manifiesto: `{manifiesto.Replace('\\', '/')}` · Sin hardware (no se movió ninguna caneca).");
            sb.AppendLine($"Precalentamiento del modelo: {frio} ms. «Aceptación» = fotos externas NO usadas en ajuste (evaluación independiente).");
            sb.AppendLine();
            sb.AppendLine("Correcto: catálogo y compuestas → apertura automática con exactamente las canecas esperadas; negativas → ninguna apertura automática; " +
                "complementarios (fuera del catálogo) → sin apertura automática o, si abre, solo su caneca de la Resolución 2184. " +
                "Falsa apertura: abre SIN preguntar una caneca que no corresponde.");
            sb.AppendLine();
            sb.AppendLine("| Conjunto | Casos | Correctos | Apertura automática correcta | Caneca correcta (con o sin confirmación) | Pide confirmación | Sin apertura automática | Falsas aperturas | Canecas omitidas |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var g in f.GroupBy(x => (x.C.Conjunto, x.C.Origen, x.C.Ajuste)).OrderBy(g => g.Key.Ajuste).ThenBy(g => g.Key.Conjunto, StringComparer.Ordinal))
                sb.AppendLine(FilaTabla($"{g.Key.Conjunto} ({g.Key.Origen}{(g.Key.Ajuste ? ", ajuste" : "")})", g.ToList()));
            var acept = f.Where(x => !x.C.Ajuste && x.C.Origen == "externa").ToList();
            var indep = f.Where(x => !x.C.Ajuste).ToList();
            sb.AppendLine(FilaTabla("**Aceptación independiente (externas sin ajuste)**", acept));
            sb.AppendLine(FilaTabla("Independiente total (sin ajuste)", indep));
            sb.AppendLine(FilaTabla("Usadas en ajuste", f.Where(x => x.C.Ajuste).ToList()));
            sb.AppendLine(FilaTabla("Fotos reales propias (todas)", f.Where(x => x.C.Origen == "real").ToList()));
            sb.AppendLine();

            var comp = f.Where(x => x.C.Complementaria).ToList();
            if (comp.Count > 0)
            {
                sb.AppendLine("## Materiales complementarios (generalización, fuera del catálogo operativo)");
                sb.AppendLine();
                sb.AppendLine("| Material | Caneca (Res. 2184) | Casos | Rechazo correcto (sin apertura automática) | Abre solo su caneca | Apertura incorrecta |");
                sb.AppendLine("|---|---|---:|---:|---:|---:|");
                foreach (var g in comp.GroupBy(x => (x.C.Descripcion ?? "?", x.C.CanecaReferencia ?? "?")).OrderBy(g => g.Key.Item2, StringComparer.Ordinal).ThenBy(g => g.Key.Item1, StringComparer.Ordinal))
                    sb.AppendLine($"| {g.Key.Item1} | {g.Key.Item2} | {g.Count()} | {g.Count(x => !x.AutoAbre)} | {g.Count(x => x.AutoAbre && !x.Falsa)} | {g.Count(x => x.Falsa)} |");
                foreach (var (nombre, g) in new[] { ("Total (aceptación)", comp.Where(x => !x.C.Ajuste).ToList()), ("Total (ajuste)", comp.Where(x => x.C.Ajuste).ToList()) })
                    sb.AppendLine($"| **{nombre}** | | {g.Count} | {g.Count(x => !x.AutoAbre)} ({Pct(g.Count(x => !x.AutoAbre), g.Count)}) | {g.Count(x => x.AutoAbre && !x.Falsa)} | **{g.Count(x => x.Falsa)}** |");
                sb.AppendLine();
            }

            sb.AppendLine("## Exactitud por material del catálogo (fotos individuales)");
            sb.AppendLine();
            sb.AppendLine("| Material | Casos (ajuste + independientes) | Material correcto | Apertura automática correcta |");
            sb.AppendLine("|---|---:|---:|---:|");
            foreach (var m in CatalogoExpo9.Todos)
            {
                var g = f.Where(x => x.C.Materiales.SequenceEqual(new[] { m.Codigo })).ToList();
                if (g.Count == 0) { sb.AppendLine($"| {m.Codigo} {m.Nombre} | 0 | - | - |"); continue; }
                sb.AppendLine($"| {m.Codigo} {m.Nombre} | {g.Count(x => x.C.Ajuste)} + {g.Count(x => !x.C.Ajuste)} | {g.Count(x => x.MatOk)} | {g.Count(x => x.Correcto)} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Latencia del reconocimiento (modelo cargado, sin caché)");
            sb.AppendLine();
            var sin = f.Where(x => !x.R.Medicion.DesdeCache && x.R.Estado != EstadoReconocimientoExpo.Error).ToList();
            List<double> Orden(Func<Fila, double> s) => sin.Select(s).OrderBy(x => x).ToList();
            sb.AppendLine("| Etapa | Media | Mediana | P95 | Máximo |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            sb.AppendLine(Linea("Preprocesamiento", Orden(x => x.R.Medicion.PreprocesoMs)));
            sb.AppendLine(Linea("Primera inferencia (pregunta principal)", Orden(x => x.R.Medicion.InferenciaMs - x.R.Medicion.VerificacionMs)));
            sb.AppendLine(Linea("Inferencia total (principal + verificación)", Orden(x => x.R.Medicion.InferenciaMs)));
            sb.AppendLine(Linea("Reconocimiento completo", Orden(x => x.R.Medicion.TotalMs)));
            int conVer = sin.Count(x => x.R.Medicion.Preguntas > 1);
            sb.AppendLine();
            sb.AppendLine($"Casos con pregunta de verificación: {conVer}/{sin.Count} ({Pct(conVer, sin.Count)}); errores del modelo: {f.Count(x => x.R.Estado == EstadoReconocimientoExpo.Error)}.");
            sb.AppendLine();

            var falsas = f.Where(x => x.Falsa).ToList();
            sb.AppendLine("## Falsas aperturas");
            sb.AppendLine();
            if (falsas.Count == 0) sb.AppendLine("Ninguna.");
            foreach (var x in falsas) sb.AppendLine(Caso1(x));
            sb.AppendLine();
            var malos = f.Where(x => !x.Correcto && !x.Falsa).ToList();
            if (malos.Count > 0)
            {
                sb.AppendLine("## Otros casos no correctos (sin falsa apertura)");
                sb.AppendLine();
                foreach (var x in malos) sb.AppendLine(Caso1(x));
            }
            return sb.ToString();
        }

        private static string Caso1(Fila x) =>
            $"- `{x.C.Archivo}` ({x.C.Conjunto}, {x.C.Origen}{(x.C.Ajuste ? ", usada en ajuste" : "")}): esperado {string.Join("+", x.C.Canecas.DefaultIfEmpty(x.C.CanecaReferencia is null ? "ninguna" : "sin apertura o " + x.C.CanecaReferencia))}, " +
            $"obtenido {string.Join("+", x.R.Canecas.Select(Nombre).DefaultIfEmpty("ninguna"))} · {x.R.Estado}{(x.AutoAbre ? " (automática)" : "")}" +
            $"{(x.R.Materiales.Count > 0 ? " · " + string.Join(", ", x.R.Materiales.Select(m => m.Material.Codigo + "=" + m.NombreModelo)) : "")}" +
            $"{(x.R.Desconocidos.Count > 0 ? " · no reconocido: " + string.Join(", ", x.R.Desconocidos) : "")}{(x.Falsa ? " · **FALSA APERTURA**" : "")}";

        /// <summary>
        /// Prueba del registro en Oracle sin hardware: inserta una operación, la avanza de estado, la reintenta (no debe
        /// duplicarse) y la lee. Uso: --probar-registro-expo9 --secreto ecoaccess-p3-pruebas (esquema aislado).
        /// </summary>
        public static async Task<int> ProbarRegistroAsync(IRegistroOperacionesEstacion repo, Func<Task<bool>> tablaDisponible,
            Func<Task<List<(string Clave, string? Codigo, string? Caneca, string TipoUsuario, string? Metodo, int DuracionMs)>>>? estadisticas = null)
        {
            if (!await tablaDisponible()) { Console.Error.WriteLine("La base no tiene OPERACION_ESTACION (V030 pendiente)."); return 3; }
            string clave = "prueba-expo9:" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var op = new OperacionEstacion(clave, DateTime.UtcNow, CanalEstacion.PC, TipoOperacionEstacion.PRUEBA_TECNICA, null, "prueba-automatica", ServicioEstacionExpo.NombreEstacion,
                "E01 manzana, E05 botella plástica, E08 servilleta", "ORGANICO_APROVECHABLE,APROVECHABLE,NO_APROVECHABLE", "VERDE,BLANCA,NEGRA", 3, 10000, "SIMULTANEA",
                "Reconocido", EstadoHardwareEstacion.SIMULADO, EstadoOperacionEstacion.EN_EJECUCION, ModeloVision: "lfm3-vl:latest", LatenciaMs: 1700, Detalle: "Prueba automática sin hardware.");
            int fallos = 0;
            void Ver(bool ok, string texto) { Console.WriteLine((ok ? "OK    " : "FALLO ") + texto); if (!ok) fallos++; }
            await repo.GuardarAsync(op);
            await repo.GuardarAsync(op with { Estado = EstadoOperacionEstacion.CICLO_COMPLETADO });
            await repo.GuardarAsync(op with { Estado = EstadoOperacionEstacion.CICLO_COMPLETADO });   // reintento: no duplica
            await repo.GuardarAsync(op with { Estado = EstadoOperacionEstacion.DISPOSICION_CONFIRMADA, Tipo = TipoOperacionEstacion.DISPOSICION_DECLARADA, DisposicionDeclarada = true });
            await repo.GuardarAsync(op with { Estado = EstadoOperacionEstacion.CICLO_COMPLETADO });   // no retrocede
            var recientes = await repo.RecientesAsync(null, 50);
            var mias = recientes.Where(x => x.Clave == clave).ToList();
            Ver(mias.Count == 1, $"una sola fila para la clave tras 5 escrituras (filas: {mias.Count})");
            Ver(mias.FirstOrDefault()?.Estado == EstadoOperacionEstacion.DISPOSICION_CONFIRMADA, "el estado avanzó a DISPOSICION_CONFIRMADA y no retrocedió");
            Ver(mias.FirstOrDefault()?.DisposicionDeclarada == true && mias.FirstOrDefault()?.DisposicionVerificada == false, "disposición declarada sí, verificada no (sin sensores)");
            Ver(mias.FirstOrDefault()?.Canecas == "VERDE,BLANCA,NEGRA" && mias.FirstOrDefault()?.DuracionMs == 10000, "canecas y tiempo 10 s conservados");
            // Cola local: con Oracle inaccesible la operación queda pendiente y se sube al volver.
            string carpeta = Path.Combine(Path.GetTempPath(), "ecoaccess-cola-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var caido = new RegistroOperacionesConCola(new RegistroQueFalla(), carpeta);
            string clave2 = clave + "-cola";
            bool confirmado = await caido.RegistrarAsync(op with { Clave = clave2 });
            Ver(!confirmado && caido.Pendientes == 1, "Oracle caído: no se informa guardado y queda 1 pendiente");
            var vuelto = new RegistroOperacionesConCola(repo, carpeta);
            int subidas = await vuelto.SincronizarAsync();
            Ver(subidas == 1 && vuelto.Pendientes == 0, $"al volver Oracle se sincronizó la pendiente ({subidas}) sin repetir apertura");
            var tras = (await repo.RecientesAsync(null, 50)).Where(x => x.Clave == clave2).ToList();
            Ver(tras.Count == 1 && tras[0].SincronizadaOffline, "la fila sincronizada quedó marcada como offline");
            try { Directory.Delete(carpeta, true); } catch (IOException) { }

            // Bitácora de la web ciudadana (pasarela): apertura + confirmación del ciudadano → una fila, idempotente, sin reescribir la bitácora.
            string raizWeb = Path.Combine(Path.GetTempPath(), "ecoaccess-web-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(Path.Combine(raizWeb, "Ciudadano"));
            string claveWeb = "prueba" + Guid.NewGuid().ToString("N").Substring(0, 18);
            string bitacora = Path.Combine(raizWeb, "Ciudadano", "operaciones-ciudadanas.jsonl");
            string ahora = DateTime.UtcNow.ToString("o");
            File.WriteAllText(bitacora,
                "{\"idOperacion\":\"web:" + claveWeb + "\",\"fechaUtc\":\"" + ahora + "\",\"sesion\":\"PRUEBA\",\"canal\":\"WEB\",\"tipo\":\"OPERACION_AUTOMATICA\",\"consulta\":\"tengo una botella y una papa\"," +
                "\"materiales\":[{\"id\":\"B01\",\"oracle\":\"PET\"},{\"id\":\"V03\",\"oracle\":\"ORGANICO\"}],\"canecas\":\"VB\",\"estadoApertura\":\"COMANDADA\",\"modo\":\"grupo\",\"tiempoProgramadoMs\":7000,\"idUsuario\":0}\n" +
                "{\"idOperacion\":\"web:" + claveWeb + ":confirmacion\",\"operacionOrigen\":\"web:" + claveWeb + "\",\"fechaUtc\":\"" + ahora + "\",\"sesion\":\"PRUEBA\",\"canal\":\"WEB\",\"tipo\":\"DISPOSICION_CONFIRMADA_CIUDADANO\"}\n");
            string antes = File.ReadAllText(bitacora);
            var web = new RegistroOperacionesConCola(repo, raizWeb, subirOperacionesWeb: true);
            int w1 = await web.SincronizarAsync();
            int w2 = await new RegistroOperacionesConCola(repo, raizWeb, subirOperacionesWeb: true).SincronizarAsync();   // tras «reinicio»
            var filasWeb = (await repo.RecientesAsync(null, 50)).Where(x => x.Clave == "web:" + claveWeb).ToList();
            Ver(w1 == 2 && w2 == 0, $"bitácora web: 2 eventos subidos y 0 al reprocesar tras reinicio ({w1}, {w2})");
            Ver(filasWeb.Count == 1 && filasWeb[0].Canal == CanalEstacion.WEB && filasWeb[0].Canecas == "VERDE,BLANCA" && filasWeb[0].DuracionMs == 7000,
                "una sola fila WEB con verde + blanca y 7 s");
            Ver(filasWeb.FirstOrDefault()?.Estado == EstadoOperacionEstacion.DISPOSICION_CONFIRMADA && filasWeb[0].DisposicionDeclarada && !filasWeb[0].DisposicionVerificada,
                "la confirmación del ciudadano quedó como declarada, nunca verificada");
            Ver(File.ReadAllText(bitacora) == antes, "la bitácora de la pasarela no se modificó");

            // V033: invitado de la web con dos residuos (limón → verde, papel aluminio → blanca) = una operación, dos detalles, 14 s.
            if (estadisticas is not null)
            {
                string claveInv = "web:prueba-invitado-" + Guid.NewGuid().ToString("N").Substring(0, 12);
                var inv = new OperacionEstacion(claveInv, DateTime.UtcNow, CanalEstacion.WEB, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, null, "web-ciudadana:PRUEBA",
                    ServicioEstacionExpo.NombreEstacion, "V31 ORGANICO, B30 LATA_ALUMINIO", "ORGANICO_APROVECHABLE,APROVECHABLE", "VERDE,BLANCA", 2,
                    PoliticaTiemposApertura.DuracionMs(2), "SIMULTANEA", "TEXTO_DETERMINISTA", EstadoHardwareEstacion.SIMULADO, EstadoOperacionEstacion.REGISTRADA,
                    TipoUsuario: "INVITADO", Metodo: "VOZ", SesionAnonima: "prueba-anonima-0001",
                    Detalles: new[] { new DetalleOperacionEstacion("V31", "Limón", "ORGANICO", "VERDE", 1, "VOZ"), new DetalleOperacionEstacion("B30", "Papel de aluminio limpio", "LATA_ALUMINIO", "BLANCA", 1, "VOZ") });
                await repo.GuardarAsync(inv);
                await repo.GuardarAsync(inv);   // reintento: no duplica cabecera ni detalle
                var filas = (await estadisticas()).Where(f => f.Clave == claveInv).ToList();
                Ver(filas.Count == 2, $"V033: una operación de invitado con 2 filas de detalle tras 2 escrituras (filas: {filas.Count})");
                Ver(filas.All(f => f.TipoUsuario == "INVITADO" && f.Metodo == "VOZ" && f.DuracionMs == 14000), "V033: invitado, método VOZ y 14 s");
                Ver(filas.Select(f => f.Caneca).OrderBy(x => x).SequenceEqual(new[] { "BLANCA", "VERDE" }), "V033: limón → verde, aluminio → blanca");
            }
            try { Directory.Delete(raizWeb, true); } catch (IOException) { }
            Console.WriteLine(fallos == 0 ? $"RESULTADO: todas correctas (claves {clave}, {clave2}, web:{claveWeb})" : $"RESULTADO: {fallos} fallidas");
            return fallos == 0 ? 0 : 1;
        }

        private sealed class RegistroQueFalla : IRegistroOperacionesEstacion
        {
            public Task GuardarAsync(OperacionEstacion operacion, System.Threading.CancellationToken ct = default) => throw new InvalidOperationException("Oracle no disponible (simulado).");
            public Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, System.Threading.CancellationToken ct = default) => throw new InvalidOperationException();
        }

        /// <summary>Latencia ANTES (recorrido anterior de Telegram, varias preguntas) vs DESPUÉS (una inferencia), mismas fotos.</summary>
        public static async Task<int> CompararLatenciaAsync(string[] args)
        {
            string manifiesto = Path.Combine("datasets", "exposoftware9", "manifiesto.json");
            string baseDir = Path.GetDirectoryName(Path.GetFullPath(manifiesto))!;
            var archivos = new List<string>();
            using (var doc = JsonDocument.Parse(File.ReadAllText(manifiesto, Encoding.UTF8)))
                foreach (var e in doc.RootElement.GetProperty("entradas").EnumerateArray())
                    if (e.GetProperty("conjunto").GetString() == "evaluacion") archivos.Add(Path.Combine(baseDir, e.GetProperty("archivo").GetString()!.Replace('/', Path.DirectorySeparatorChar)));
            EstadoVisionLocal.Origen = "evaluacion";
            using var antes = new ClasificadorOllamaLocal("30m");
            var nuevo = new ReconocedorExpo9();
            await antes.PrecalentarAsync(); await nuevo.PrecalentarAsync();
            var tA = new List<double>(); var tB = new List<double>();
            foreach (string f in archivos)
            {
                byte[] b = File.ReadAllBytes(f);
                var img = new EcoAccess.P3.Logica.Contratos.ImagenResiduo(b, "image/jpeg");
                var r1 = System.Diagnostics.Stopwatch.StartNew();
                try { await antes.ClasificarAsync(img, Array.Empty<Material>()); await antes.ListarAsync(img); } catch (Exception) { }
                r1.Stop(); tA.Add(r1.ElapsedMilliseconds);
                var r2 = System.Diagnostics.Stopwatch.StartNew();
                var res = await nuevo.ReconocerAsync(b.Concat(new byte[] { 0 }).ToArray());   // byte extra: evita la caché
                tB.Add(res.Medicion.TotalMs);
                Console.WriteLine($"{Path.GetFileName(f),-32} antes {r1.ElapsedMilliseconds,6} ms · después {res.Medicion.TotalMs,6} ms");
            }
            tA.Sort(); tB.Sort();
            Console.WriteLine("| Recorrido | Media | Mediana | P95 | Máximo |");
            Console.WriteLine("|---|---:|---:|---:|---:|");
            Console.WriteLine(Linea("Antes: recorrido de varias preguntas (ClasificadorOllamaLocal + lista multiobjeto)", tA));
            Console.WriteLine(Linea("Después: ReconocedorExpo9 (una inferencia, 512 px)", tB));
            return 0;
        }

        private static string Linea(string nombre, List<double> v) => v.Count == 0 ? $"| {nombre} | - | - | - | - |"
            : FormattableString.Invariant($"| {nombre} | {v.Average():0} ms | {Mediana(v):0} ms | {v[(int)Math.Ceiling(0.95 * v.Count) - 1]:0} ms | {v[^1]:0} ms |");
        private static double Mediana(List<double> v) => v.Count % 2 == 1 ? v[v.Count / 2] : (v[v.Count / 2 - 1] + v[v.Count / 2]) / 2;
        private static string Pct(int a, int b) => b == 0 ? "-" : (100.0 * a / b).ToString("0", CultureInfo.InvariantCulture) + " %";
        private static string Nombre(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => "VERDE",
            CategoriaMaterial.Aprovechable => "BLANCA",
            CategoriaMaterial.NoAprovechable => "NEGRA",
            _ => "?",
        };
        private static string? Opcion(string[] args, string nombre)
        {
            int i = Array.FindIndex(args, a => string.Equals(a, nombre, StringComparison.OrdinalIgnoreCase));
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
