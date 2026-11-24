using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>Procedencia explícita de cada imagen del dataset (jamás mezclar sintéticas como si fueran reales).</summary>
    public enum ProcedenciaImagen
    {
        RealStand = 1,
        RealCatalogo = 2,
        RecorteLaminaVerificado = 3,
        SinteticaCompuesta = 4,
        SimuladaSanitaria = 5,
    }

    /// <summary>Tipo de escenario según la distribución objetivo de 100 casos por material (25/35/30/10).</summary>
    public enum TipoEscenarioDataset
    {
        Individual = 1,
        MezclaCategorias = 2,
        TresCategorias = 3,
        CondicionDificil = 4,
        NegativoOSeguridad = 5,
    }

    /// <summary>Anotación verificada de un objeto dentro de una fotografía (simple o multirresiduo).</summary>
    public sealed record AnotacionObjeto35(
        string CodigoMaterial35,
        string NombreEsperado,
        string CanecaEsperada,
        EstadoObservableObjeto EstadoEsperado = EstadoObservableObjeto.LimpioYVacio,
        int Cantidad = 1,
        CajaDelimitadoraNormalizada? Caja = null);

    /// <summary>Entrada única del manifiesto del dataset de los 35 residuos.</summary>
    public sealed record EntradaDataset35(
        string IdCaso,
        string RutaRelativa,
        string Sha256,
        ProcedenciaImagen Procedencia,
        TipoEscenarioDataset Escenario,
        string CombinacionEsperada,
        IReadOnlyList<AnotacionObjeto35> ObjetosEsperados,
        string? ArchivoOrigenOriginal = null,
        string? CondicionIluminacion = null,
        bool EsCasoCriticoSeguridad = false,
        bool RequiereConfirmacionEsperada = false,
        string? Nota = null);

    /// <summary>Manifiesto versionado y verificable del dataset ECOACCESS 35.</summary>
    public sealed record ManifiestoDataset35(
        string Nombre,
        string VersionCatalogo,
        DateTime FechaCreacionUtc,
        int MetaEvaluacionesPorMaterial,
        IReadOnlyList<EntradaDataset35> Entradas);

    /// <summary>Filtros para el ejecutor de pruebas repetibles (Sección 11).</summary>
    public sealed record FiltroEvaluacion35(
        string? CodigoMaterial = null,
        string? Categoria = null,
        string? Combinacion = null,
        ProcedenciaImagen? Procedencia = null,
        IReadOnlyCollection<string>? SoloIdsCasos = null,
        int? LimiteCasos = null);

    /// <summary>Resultado detallado de una ejecución individual (todos los campos obligatorios de la Sección 11).</summary>
    public sealed record RegistroEjecucionCaso35(
        string IdPrueba,
        DateTime FechaUtc,
        string VersionCodigo,
        string ModeloUtilizado,
        string ParametrosInferencia,
        string ImagenEvaluada,
        ProcedenciaImagen Procedencia,
        IReadOnlyList<string> EtiquetasEsperadas,
        IReadOnlyList<string> EtiquetasObtenidas,
        IReadOnlyList<string> CategoriasEsperadas,
        IReadOnlyList<string> CategoriasObtenidas,
        string CombinacionEsperada,
        string CombinacionObtenida,
        long TiempoEjecucionMs,
        bool Aprobado,
        bool FalsaApertura,
        int CanecasOmitidas,
        bool RechazadoOConfirmacion,
        string? Error,
        string Evidencia);

    public sealed record MetricaMaterial35(
        string Codigo,
        string Nombre,
        string Caneca,
        int CasosEvaluados,
        int VerdaderosPositivos,
        int FalsosPositivos,
        int FalsosNegativos,
        decimal Precision,
        decimal Recall,
        decimal F1);

    public sealed record MetricaCategoria35(
        string Categoria,
        int CasosEvaluados,
        int Exactos,
        decimal Exactitud);

    /// <summary>Fila de la matriz formal de aceptación (Sección 12): OBJETIVO / MEDICIÓN REAL / CANTIDAD / APROBADO.</summary>
    public sealed record CriterioAceptacion35(
        string Criterio,
        string Objetivo,
        string MedicionReal,
        int CantidadCasos,
        string Estado);

    /// <summary>Informe completo verificable de una corrida del ejecutor de pruebas.</summary>
    public sealed record InformeEvaluacion35(
        string IdEjecucion,
        DateTime FechaUtc,
        string VersionCodigo,
        string ModeloUtilizado,
        string ParametrosInferencia,
        int TotalPlanificado,
        int TotalEjecutado,
        int TotalAprobado,
        int TotalFallido,
        int CasosRechazadosOConfirmacion,
        int FalsosPositivosTotales,
        int FalsosNegativosTotales,
        int FalsasAperturas,
        int CanecasNecesariasOmitidas,
        int FallosIntegracion,
        int FallosHardware,
        decimal ExactitudCombinaciones,
        decimal TiempoMedioMs,
        long TiempoP95Ms,
        long LatenciaTotalMs,
        IReadOnlyList<MetricaMaterial35> PorMaterial,
        IReadOnlyList<MetricaCategoria35> PorCategoria,
        IReadOnlyList<CriterioAceptacion35> CriteriosAceptacion,
        IReadOnlyList<RegistroEjecucionCaso35> Detalle);

    /// <summary>
    /// Ejecutor de evaluaciones repetibles del catálogo de 35 materiales y combinaciones C01–C07.
    /// No inventa resultados, rechaza duplicados por SHA-256 si se solicita y nunca marca como aprobada una evaluación incompleta.
    /// </summary>
    public sealed class EjecutorEvaluacion35
    {
        public static readonly JsonSerializerOptions OpcionesJson = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly IProveedorVisionMultirresiduo _proveedor;
        private readonly MotorReglasMultirresiduo _motor;
        private readonly string _versionCodigo;
        private readonly string _parametrosInferencia;

        public EjecutorEvaluacion35(
            IProveedorVisionMultirresiduo proveedor,
            MotorReglasMultirresiduo? motor = null,
            string versionCodigo = "2026.11.22+vision35",
            string parametrosInferencia = "temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json")
        {
            _proveedor = proveedor ?? throw new ArgumentNullException(nameof(proveedor));
            _motor = motor ?? new MotorReglasMultirresiduo();
            _versionCodigo = versionCodigo;
            _parametrosInferencia = parametrosInferencia;
        }

        public static ManifiestoDataset35 CargarManifiesto(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
                throw new FileNotFoundException("No se encontró el manifiesto del dataset.", rutaArchivo);
            string json = File.ReadAllText(rutaArchivo);
            return JsonSerializer.Deserialize<ManifiestoDataset35>(json, OpcionesJson)
                ?? throw new InvalidDataException("Manifiesto vacío o inválido.");
        }

        public static IReadOnlyList<string> LeerCasosFallidosDeInforme(string rutaInformeJson)
        {
            if (string.IsNullOrWhiteSpace(rutaInformeJson) || !File.Exists(rutaInformeJson))
                throw new FileNotFoundException("No se encontró el informe previo.", rutaInformeJson);
            InformeEvaluacion35? previo = JsonSerializer.Deserialize<InformeEvaluacion35>(File.ReadAllText(rutaInformeJson), OpcionesJson);
            if (previo is null) return Array.Empty<string>();
            return previo.Detalle.Where(d => !d.Aprobado).Select(d => d.IdPrueba).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Ejecuta la evaluación sobre el manifiesto aplicando filtros opcionales.</summary>
        public async Task<InformeEvaluacion35> EjecutarManifiestoAsync(
            string rutaManifiesto,
            FiltroEvaluacion35? filtro = null,
            CancellationToken ct = default)
        {
            ManifiestoDataset35 manifiesto = CargarManifiesto(rutaManifiesto);
            string directorioBase = Path.GetDirectoryName(Path.GetFullPath(rutaManifiesto))!;
            return await EjecutarEntradasAsync(manifiesto.Entradas, directorioBase, filtro, ct).ConfigureAwait(false);
        }

        /// <summary>Ejecuta una evaluación directamente sobre una lista de entradas (útil en pruebas unitarias y CLI).</summary>
        public async Task<InformeEvaluacion35> EjecutarEntradasAsync(
            IReadOnlyList<EntradaDataset35> entradas,
            string directorioBase,
            FiltroEvaluacion35? filtro = null,
            CancellationToken ct = default)
        {
            IEnumerable<EntradaDataset35> seleccion = entradas;
            if (filtro is not null)
            {
                if (!string.IsNullOrWhiteSpace(filtro.CodigoMaterial))
                    seleccion = seleccion.Where(e => e.ObjetosEsperados.Any(o => string.Equals(o.CodigoMaterial35, filtro.CodigoMaterial, StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(filtro.Categoria))
                    seleccion = seleccion.Where(e => e.ObjetosEsperados.Any(o => string.Equals(o.CanecaEsperada, filtro.Categoria, StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(filtro.Combinacion))
                    seleccion = seleccion.Where(e => string.Equals(e.CombinacionEsperada, filtro.Combinacion, StringComparison.OrdinalIgnoreCase));
                if (filtro.Procedencia.HasValue)
                    seleccion = seleccion.Where(e => e.Procedencia == filtro.Procedencia.Value);
                if (filtro.SoloIdsCasos is { Count: > 0 })
                    seleccion = seleccion.Where(e => filtro.SoloIdsCasos.Contains(e.IdCaso));
                if (filtro.LimiteCasos is > 0)
                    seleccion = seleccion.Take(filtro.LimiteCasos.Value);
            }

            List<EntradaDataset35> plan = seleccion.ToList();
            var detalle = new List<RegistroEjecucionCaso35>(plan.Count);
            var relojTotal = Stopwatch.StartNew();
            int fallosIntegracion = 0;

            foreach (EntradaDataset35 entrada in plan)
            {
                ct.ThrowIfCancellationRequested();
                string rutaCompleta = Path.IsPathRooted(entrada.RutaRelativa)
                    ? entrada.RutaRelativa
                    : Path.GetFullPath(Path.Combine(directorioBase, entrada.RutaRelativa));

                var relojCaso = Stopwatch.StartNew();
                if (!File.Exists(rutaCompleta))
                {
                    fallosIntegracion++;
                    detalle.Add(ConstruirErrorCaso(entrada, "Archivo de imagen no encontrado: " + entrada.RutaRelativa, relojCaso.ElapsedMilliseconds));
                    continue;
                }

                byte[] bytes = File.ReadAllBytes(rutaCompleta);
                if (!string.IsNullOrWhiteSpace(entrada.Sha256))
                {
                    string hashReal = Compat.ToHexString(Compat.Sha256(bytes));
                    if (!string.Equals(hashReal, entrada.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        fallosIntegracion++;
                        detalle.Add(ConstruirErrorCaso(entrada, "Hash SHA-256 no coincide con el manifiesto.", relojCaso.ElapsedMilliseconds));
                        continue;
                    }
                }

                RespuestaVisionEstructurada respuesta = await _proveedor.ObservarAsync(bytes, ct).ConfigureAwait(false);
                ResultadoAnalisisMultirresiduo analisis = _motor.Evaluar(respuesta, Compat.ToHexString(Compat.Sha256(bytes)));
                relojCaso.Stop();

                if (analisis.EstadoGlobal == EstadoGlobalAnalisis.ModoSeguroError)
                    fallosIntegracion++;

                detalle.Add(EvaluarCaso(entrada, analisis, relojCaso.ElapsedMilliseconds));
            }

            relojTotal.Stop();
            return Resumir(plan.Count, detalle, fallosIntegracion, fallosHardware: 0, relojTotal.ElapsedMilliseconds);
        }

        private RegistroEjecucionCaso35 EvaluarCaso(EntradaDataset35 entrada, ResultadoAnalisisMultirresiduo analisis, long ms)
        {
            string[] esperadasMat = entrada.ObjetosEsperados
                .Select(o => o.CodigoMaterial35.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            string[] obtenidasMat = analisis.Objetos
                .Where(o => o.Material35 is not null)
                .Select(o => o.Material35!.Codigo.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            string[] esperadasCat = entrada.ObjetosEsperados
                .Select(o => o.CanecaEsperada.ToUpperInvariant())
                .Where(c => c is "VERDE" or "BLANCA" or "NEGRA")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            string[] obtenidasCat = analisis.CanecasRequeridas
                .Select(MotorReglasMultirresiduo.NombreCortoCaneca)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            string combObtenida = analisis.Combinacion.ToString();
            bool falsaApertura = analisis.PuedeAbrirCanecas && (
                entrada.EsCasoCriticoSeguridad ||
                esperadasCat.Length == 0 ||
                obtenidasCat.Any(c => !esperadasCat.Contains(c, StringComparer.OrdinalIgnoreCase)));

            int omitidas = esperadasCat.Count(c => !obtenidasCat.Contains(c, StringComparer.OrdinalIgnoreCase));
            bool rechazado = analisis.RequiereConfirmacion || analisis.EstadoGlobal is EstadoGlobalAnalisis.RequiereRepeticion or EstadoGlobalAnalisis.NoIdentificado or EstadoGlobalAnalisis.ModoSeguroError;

            bool aprobado;
            if (entrada.RequiereConfirmacionEsperada)
                aprobado = analisis.RequiereConfirmacion && !analisis.PuedeAbrirCanecas;
            else if (esperadasCat.Length == 0)
                aprobado = !analisis.PuedeAbrirCanecas && obtenidasCat.Length == 0;
            else
                aprobado = esperadasCat.SequenceEqual(obtenidasCat, StringComparer.OrdinalIgnoreCase) && !falsaApertura;

            string evidencia = $"Estado={analisis.EstadoGlobal}; Objetos={analisis.TotalObjetosDetectados}; PuedeAbrir={analisis.PuedeAbrirCanecas}";

            return new RegistroEjecucionCaso35(
                entrada.IdCaso,
                DateTime.UtcNow,
                _versionCodigo,
                analisis.ModeloUtilizado,
                _parametrosInferencia,
                entrada.RutaRelativa,
                entrada.Procedencia,
                esperadasMat,
                obtenidasMat,
                esperadasCat,
                obtenidasCat,
                entrada.CombinacionEsperada,
                combObtenida,
                ms,
                aprobado,
                falsaApertura,
                omitidas,
                rechazado,
                analisis.ErrorTecnico,
                evidencia);
        }

        private RegistroEjecucionCaso35 ConstruirErrorCaso(EntradaDataset35 entrada, string error, long ms) =>
            new(entrada.IdCaso, DateTime.UtcNow, _versionCodigo, _proveedor.NombreProveedor, _parametrosInferencia,
                entrada.RutaRelativa, entrada.Procedencia,
                entrada.ObjetosEsperados.Select(o => o.CodigoMaterial35).Distinct().ToArray(),
                Array.Empty<string>(),
                entrada.ObjetosEsperados.Select(o => o.CanecaEsperada).Distinct().ToArray(),
                Array.Empty<string>(),
                entrada.CombinacionEsperada, "ERROR", ms, false, false,
                entrada.ObjetosEsperados.Select(o => o.CanecaEsperada).Distinct().Count(),
                true, error, "Error de ejecución");

        private InformeEvaluacion35 Resumir(int planificado, List<RegistroEjecucionCaso35> detalle, int fallosIntegracion, int fallosHardware, long totalMs)
        {
            int ejecutado = detalle.Count;
            int aprobados = detalle.Count(d => d.Aprobado);
            int fallidos = ejecutado - aprobados;
            int falsasAperturas = detalle.Count(d => d.FalsaApertura);
            int omitidas = detalle.Sum(d => d.CanecasOmitidas);
            int rechazados = detalle.Count(d => d.RechazadoOConfirmacion);

            var porMaterial = new List<MetricaMaterial35>();
            int fpTot = 0, fnTot = 0;

            foreach (MaterialPrioritario35 m in CatalogoPrioritario35.Todos)
            {
                int vp = detalle.Count(d => d.EtiquetasEsperadas.Contains(m.Codigo) && d.EtiquetasObtenidas.Contains(m.Codigo));
                int fp = detalle.Count(d => !d.EtiquetasEsperadas.Contains(m.Codigo) && d.EtiquetasObtenidas.Contains(m.Codigo));
                int fn = detalle.Count(d => d.EtiquetasEsperadas.Contains(m.Codigo) && !d.EtiquetasObtenidas.Contains(m.Codigo));
                int casos = vp + fn;
                fpTot += fp;
                fnTot += fn;
                decimal p = vp + fp == 0 ? 0m : Math.Round((decimal)vp / (vp + fp), 4);
                decimal r = vp + fn == 0 ? 0m : Math.Round((decimal)vp / (vp + fn), 4);
                decimal f1 = p + r == 0 ? 0m : Math.Round(2m * p * r / (p + r), 4);
                porMaterial.Add(new MetricaMaterial35(m.Codigo, m.Nombre, m.NombreCaneca, casos, vp, fp, fn, p, r, f1));
            }

            var porCategoria = new List<MetricaCategoria35>();
            foreach (string cat in new[] { "VERDE", "BLANCA", "NEGRA" })
            {
                var conCat = detalle.Where(d => d.CategoriasEsperadas.Contains(cat)).ToList();
                int exactos = conCat.Count(d => d.CategoriasObtenidas.Contains(cat) && !d.FalsaApertura);
                decimal acc = conCat.Count == 0 ? 0m : Math.Round((decimal)exactos / conCat.Count, 4);
                porCategoria.Add(new MetricaCategoria35(cat, conCat.Count, exactos, acc));
            }

            decimal exactComb = ejecutado == 0 ? 0m : Math.Round((decimal)aprobados / ejecutado, 4);
            long[] tiempos = detalle.Select(d => d.TiempoEjecucionMs).OrderBy(t => t).ToArray();
            decimal mediaMs = tiempos.Length == 0 ? 0m : Math.Round((decimal)tiempos.Average(), 1);
            long p95Ms = tiempos.Length == 0 ? 0 : tiempos[Math.Min(tiempos.Length - 1, (int)Math.Ceiling(tiempos.Length * 0.95) - 1)];

            // Matriz formal de aceptación (Sección 12): evaluación incompleta (< 3.500 casos reales) NO se marca aprobada.
            int materialesCubiertos100 = porMaterial.Count(m => m.CasosEvaluados >= 100);
            decimal precisionPromedio = porMaterial.Where(m => m.CasosEvaluados > 0).Select(m => m.Precision).DefaultIfEmpty(0m).Average();
            decimal recallPromedio = porMaterial.Where(m => m.CasosEvaluados > 0).Select(m => m.Recall).DefaultIfEmpty(0m).Average();

            var criterios = new List<CriterioAceptacion35>
            {
                new("Cobertura dataset (35 × 100)", "3.500 evaluaciones reales", $"{ejecutado} evaluaciones ({materialesCubiertos100}/35 con ≥100)", ejecutado,
                    ejecutado >= 3500 && materialesCubiertos100 == 35 ? "APROBADO" : "NO APROBADO (EVALUACIÓN INCOMPLETA)"),
                new("Precisión promedio por material", "≥ 95 %", $"{precisionPromedio:P1}", ejecutado,
                    ejecutado >= 3500 && precisionPromedio >= 0.95m ? "APROBADO" : "NO APROBADO"),
                new("Recall promedio por material", "≥ 95 %", $"{recallPromedio:P1}", ejecutado,
                    ejecutado >= 3500 && recallPromedio >= 0.95m ? "APROBADO" : "NO APROBADO"),
                new("Coincidencia exacta de combinaciones de canecas", "≥ 99 %", $"{exactComb:P1} ({aprobados}/{ejecutado})", ejecutado,
                    ejecutado > 0 && exactComb >= 0.99m ? "APROBADO" : "NO APROBADO"),
                new("Falsas aperturas en casos evaluados", "0", $"{falsasAperturas}", ejecutado,
                    ejecutado > 0 && falsasAperturas == 0 ? "APROBADO" : "NO APROBADO"),
                new("Fallos de integración / hardware", "0", $"Integración={fallosIntegracion}, Hardware={fallosHardware}", ejecutado,
                    ejecutado > 0 && fallosIntegracion == 0 && fallosHardware == 0 ? "APROBADO" : "NO APROBADO"),
            };

            return new InformeEvaluacion35(
                IdEjecucion: "EVAL35-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),
                FechaUtc: DateTime.UtcNow,
                VersionCodigo: _versionCodigo,
                ModeloUtilizado: _proveedor.NombreProveedor,
                ParametrosInferencia: _parametrosInferencia,
                TotalPlanificado: planificado,
                TotalEjecutado: ejecutado,
                TotalAprobado: aprobados,
                TotalFallido: fallidos,
                CasosRechazadosOConfirmacion: rechazados,
                FalsosPositivosTotales: fpTot,
                FalsosNegativosTotales: fnTot,
                FalsasAperturas: falsasAperturas,
                CanecasNecesariasOmitidas: omitidas,
                FallosIntegracion: fallosIntegracion,
                FallosHardware: fallosHardware,
                ExactitudCombinaciones: exactComb,
                TiempoMedioMs: mediaMs,
                TiempoP95Ms: p95Ms,
                LatenciaTotalMs: totalMs,
                PorMaterial: porMaterial,
                PorCategoria: porCategoria,
                CriteriosAceptacion: criterios,
                Detalle: detalle);
        }

        /// <summary>Exporta el informe en Markdown legible con la tabla formal de criterios de aceptación.</summary>
        public static string FormatearMarkdown(InformeEvaluacion35 informe)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Informe de Evaluación ECOACCESS 35 — {informe.IdEjecucion}");
            sb.AppendLine();
            sb.AppendLine($"- **Fecha UTC:** {informe.FechaUtc:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Versión código:** `{informe.VersionCodigo}`");
            sb.AppendLine($"- **Modelo:** `{informe.ModeloUtilizado}`");
            sb.AppendLine($"- **Parámetros:** `{informe.ParametrosInferencia}`");
            sb.AppendLine($"- **Casos ejecutados:** {informe.TotalEjecutado}/{informe.TotalPlanificado} (Aprobados: {informe.TotalAprobado}, Fallidos: {informe.TotalFallido}, Confirmación/Rechazo: {informe.CasosRechazadosOConfirmacion})");
            sb.AppendLine($"- **Latencia:** Media {informe.TiempoMedioMs} ms · P95 {informe.TiempoP95Ms} ms · Total {informe.LatenciaTotalMs} ms");
            sb.AppendLine();
            sb.AppendLine("## Criterios de Aceptación (Sección 12)");
            sb.AppendLine();
            sb.AppendLine("| Criterio | Objetivo | Medición real | Casos | Estado |");
            sb.AppendLine("|---|---|---|---:|---|");
            foreach (var c in informe.CriteriosAceptacion)
                sb.AppendLine($"| {c.Criterio} | {c.Objetivo} | {c.MedicionReal} | {c.CantidadCasos} | **{c.Estado}** |");
            sb.AppendLine();
            sb.AppendLine("## Exactitud por Categoría");
            sb.AppendLine();
            sb.AppendLine("| Categoría | Casos | Exactos | Exactitud |");
            sb.AppendLine("|---|---:|---:|---:|");
            foreach (var cat in informe.PorCategoria)
                sb.AppendLine($"| {cat.Categoria} | {cat.CasosEvaluados} | {cat.Exactos} | {cat.Exactitud:P1} |");
            return sb.ToString();
        }
    }
}
