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
    /// Uso: --evaluar-expo9 [manifiesto.json] [--lado 384|512|640] [--modelo nombre] [--salida informe.md] [--conjuntos evaluacion,multirresiduo,negativa]
    /// </summary>
    internal static class ComandoExpo9
    {
        private sealed record Caso(string Archivo, string Conjunto, string Origen, string[] Materiales, string[] Canecas, bool Ajuste, bool Especial);

        public static async Task<int> EvaluarAsync(string[] args)
        {
            string manifiesto = args.Skip(1).FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal) && a.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                ?? Path.Combine("datasets", "exposoftware9", "manifiesto.json");
            int? lado = Opcion(args, "--lado") is { } l ? int.Parse(l, CultureInfo.InvariantCulture) : null;
            string? modelo = Opcion(args, "--modelo");
            string? salida = Opcion(args, "--salida");
            var conjuntos = (Opcion(args, "--conjuntos") ?? "evaluacion,multirresiduo,negativa").Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(manifiesto)) { Console.Error.WriteLine("No existe el manifiesto: " + manifiesto); return 2; }
            string baseDir = Path.GetDirectoryName(Path.GetFullPath(manifiesto))!;

            var casos = new List<Caso>();
            using (var doc = JsonDocument.Parse(File.ReadAllText(manifiesto, Encoding.UTF8)))
            {
                foreach (var e in doc.RootElement.GetProperty("entradas").EnumerateArray())
                {
                    string conjunto = e.GetProperty("conjunto").GetString()!;
                    if (!conjuntos.Contains(conjunto)) continue;
                    casos.Add(new Caso(e.GetProperty("archivo").GetString()!, conjunto, e.GetProperty("origen").GetString()!,
                        e.GetProperty("materiales").EnumerateArray().Select(x => x.GetString()!).ToArray(),
                        e.GetProperty("canecas").EnumerateArray().Select(x => x.GetString()!).ToArray(),
                        e.TryGetProperty("usada_en_ajuste", out var aj) && aj.ValueKind == JsonValueKind.True,
                        e.TryGetProperty("gestion_especial", out var ge) && ge.ValueKind == JsonValueKind.True));
                }
            }

            var rec = new ReconocedorExpo9(modelo, lado);
            Console.WriteLine($"Modelo {rec.Modelo} · lado {rec.LadoPx}px · {casos.Count} casos · precalentando…");
            long frio = await rec.PrecalentarAsync();
            Console.WriteLine($"Precalentamiento: {frio} ms");

            var filas = new List<(Caso C, ResultadoExpo9 R, bool MatOk, bool CanOk, int Extra, int Omitidas, bool FalsaApertura)>();
            foreach (var c in casos)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(baseDir, c.Archivo.Replace('/', Path.DirectorySeparatorChar)));
                ResultadoExpo9 r = await rec.ReconocerAsync(bytes);
                var detMat = r.Materiales.Select(m => m.Material.Codigo).OrderBy(x => x).ToArray();
                var detCan = r.Canecas.Select(Nombre).ToArray();
                bool matOk = detMat.SequenceEqual(c.Materiales.OrderBy(x => x));
                bool canOk = detCan.OrderBy(x => x).SequenceEqual(c.Canecas.OrderBy(x => x));
                int extra = detCan.Except(c.Canecas).Count(), omit = c.Canecas.Except(detCan).Count();
                // Falsa apertura: el sistema habría abierto SIN preguntar una caneca que no corresponde.
                bool falsa = r.PuedeAbrirAutomaticamente && extra > 0;
                filas.Add((c, r, matOk, canOk, extra, omit, falsa));
                Console.WriteLine($"{(canOk ? "OK " : "MAL")} {r.Medicion.InferenciaMs,5} ms {c.Archivo,-52} esperado [{string.Join(",", c.Materiales)}] → [{string.Join(",", r.Materiales.Select(m => m.Material.Codigo + "=" + m.NombreModelo))}] {r.Estado}{(r.Desconocidos.Count > 0 ? " ?" + string.Join("|", r.Desconocidos) : "")}");
            }

            string informe = Informe(filas.Select(f => (f.C, f.R, f.MatOk, f.CanOk, f.Extra, f.Omitidas, f.FalsaApertura)).ToList(), rec, frio, manifiesto);
            Console.WriteLine();
            Console.WriteLine(informe);
            if (salida is not null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(salida))!); File.WriteAllText(salida, informe, new UTF8Encoding(false)); }
            return 0;
        }

        private static string Informe(List<(Caso C, ResultadoExpo9 R, bool MatOk, bool CanOk, int Extra, int Omitidas, bool Falsa)> f, ReconocedorExpo9 rec, long frio, string manifiesto)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Evaluación Modo ExpoSoftware (9 materiales) — {rec.Modelo}, lado {rec.LadoPx}px");
            sb.AppendLine();
            sb.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm} · Manifiesto: `{manifiesto.Replace('\\', '/')}` · Sin hardware (no se movió ninguna caneca).");
            sb.AppendLine($"Precalentamiento del modelo: {frio} ms. Las imágenes marcadas `usada_en_ajuste` se excluyen de las métricas independientes.");
            sb.AppendLine();
            sb.AppendLine("| Conjunto | Casos | Canecas correctas | Materiales exactos | Falsas aperturas | Canecas omitidas | Ambiguos/desconocidos |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
            foreach (var g in f.Where(x => !x.C.Ajuste).GroupBy(x => x.C.Conjunto))
                sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Count(x => x.CanOk)} ({Pct(g.Count(x => x.CanOk), g.Count())}) | {g.Count(x => x.MatOk)} ({Pct(g.Count(x => x.MatOk), g.Count())}) | {g.Count(x => x.Falsa)} | {g.Sum(x => x.Omitidas)} | {g.Count(x => x.R.Estado is EstadoReconocimientoExpo.ParcialConDesconocidos or EstadoReconocimientoExpo.GestionEspecial)} |");
            var indep = f.Where(x => !x.C.Ajuste).ToList();
            sb.AppendLine($"| **Total independiente** | {indep.Count} | {indep.Count(x => x.CanOk)} ({Pct(indep.Count(x => x.CanOk), indep.Count)}) | {indep.Count(x => x.MatOk)} ({Pct(indep.Count(x => x.MatOk), indep.Count)}) | {indep.Count(x => x.Falsa)} | {indep.Sum(x => x.Omitidas)} | {indep.Count(x => x.R.Estado is EstadoReconocimientoExpo.ParcialConDesconocidos or EstadoReconocimientoExpo.GestionEspecial)} |");
            sb.AppendLine();
            sb.AppendLine("## Exactitud por material (individual, conjunto de evaluación)");
            sb.AppendLine();
            sb.AppendLine("| Material | Casos | Material correcto | Caneca correcta |");
            sb.AppendLine("|---|---:|---:|---:|");
            foreach (var m in CatalogoExpo9.Todos)
            {
                var g = indep.Where(x => x.C.Conjunto == "evaluacion" && x.C.Materiales.SequenceEqual(new[] { m.Codigo })).ToList();
                sb.AppendLine($"| {m.Codigo} {m.Nombre} | {g.Count} | {g.Count(x => x.MatOk)} | {g.Count(x => x.CanOk)} |");
            }
            sb.AppendLine();
            sb.AppendLine("## Exactitud por categoría (caneca esperada presente en el resultado)");
            sb.AppendLine();
            foreach (string can in new[] { "VERDE", "BLANCA", "NEGRA" })
            {
                var g = indep.Where(x => x.C.Canecas.Contains(can)).ToList();
                int ok = g.Count(x => x.R.Canecas.Select(Nombre).Contains(can));
                sb.AppendLine($"- {can}: {ok}/{g.Count} ({Pct(ok, g.Count)})");
            }
            var neg = indep.Where(x => x.C.Conjunto == "negativa").ToList();
            sb.AppendLine($"- Negativas sin apertura automática: {neg.Count(x => !x.R.PuedeAbrirAutomaticamente)}/{neg.Count}");
            sb.AppendLine();
            sb.AppendLine("## Latencia del reconocimiento (modelo cargado, sin caché)");
            sb.AppendLine();
            var inf = f.Where(x => !x.R.Medicion.DesdeCache).Select(x => (double)x.R.Medicion.InferenciaMs).OrderBy(x => x).ToList();
            var tot = f.Where(x => !x.R.Medicion.DesdeCache).Select(x => (double)x.R.Medicion.TotalMs).OrderBy(x => x).ToList();
            var pre = f.Where(x => !x.R.Medicion.DesdeCache).Select(x => (double)x.R.Medicion.PreprocesoMs).OrderBy(x => x).ToList();
            sb.AppendLine("| Etapa | Media | Mediana | P95 | Máximo |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            sb.AppendLine(Linea("Preprocesamiento", pre));
            sb.AppendLine(Linea("Inferencia Ollama", inf));
            sb.AppendLine(Linea("Total reconocimiento", tot));
            sb.AppendLine();
            var malos = f.Where(x => !x.CanOk).ToList();
            if (malos.Count > 0)
            {
                sb.AppendLine("## Casos con caneca distinta a la esperada");
                sb.AppendLine();
                foreach (var x in malos)
                    sb.AppendLine($"- `{x.C.Archivo}` ({x.C.Origen}{(x.C.Ajuste ? ", usada en ajuste" : "")}): esperado {string.Join("+", x.C.Canecas.DefaultIfEmpty("ninguna"))}, obtenido {string.Join("+", x.R.Canecas.Select(Nombre).DefaultIfEmpty("ninguna"))} · {x.R.Estado}" +
                        $"{(x.R.Desconocidos.Count > 0 ? " · no reconocido: " + string.Join(", ", x.R.Desconocidos) : "")}{(x.Falsa ? " · **FALSA APERTURA**" : "")}");
            }
            return sb.ToString();
        }

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
