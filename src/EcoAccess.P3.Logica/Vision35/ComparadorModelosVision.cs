using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>Resultado de comparar dos dictámenes visuales (p. ej., LFM3-VL vs. Qwen3-VL) sobre la misma foto.</summary>
    public sealed record ResultadoConsensoModelos(
        ResultadoAnalisisMultirresiduo DictamenA,
        ResultadoAnalisisMultirresiduo DictamenB,
        bool CoincidenCanecas,
        bool PuedeAbrirConSeguridad,
        ResultadoAnalisisMultirresiduo DictamenSeguroFinal,
        string ResumenConsenso);

    /// <summary>Informe comparativo en igualdad de condiciones entre dos modelos visuales (FASE 2).</summary>
    public sealed record InformeComparativoModelos(
        DateTime FechaUtc,
        string ModeloA,
        string ModeloB,
        InformeEvaluacion35 InformeA,
        InformeEvaluacion35 InformeB,
        int CasosEnComun,
        int CoincidenciasCanecas,
        int DiscrepanciasBloqueadas,
        string ModeloRecomendadoPorEvidencia,
        string Justificacion);

    /// <summary>
    /// Comparador de modelos visuales locales (LFM3-VL vs. Qwen3-VL) bajo idénticas imágenes y reglas.
    /// Garantiza la regla de seguridad de la Fase 2: una discrepancia entre modelos JAMÁS produce una apertura automática.
    /// </summary>
    public static class ComparadorModelosVision
    {
        /// <summary>
        /// Combina el dictamen de dos modelos sobre una misma imagen. Si discrepan en las canecas requeridas o alguno
        /// solicita confirmación/error, bloquea la apertura automática y exige confirmación humana.
        /// </summary>
        public static ResultadoConsensoModelos CombinarConSeguridad(
            ResultadoAnalisisMultirresiduo a,
            ResultadoAnalisisMultirresiduo b)
        {
            Compat.ThrowIfNull(a, nameof(a));
            Compat.ThrowIfNull(b, nameof(b));

            bool mismasCanecas = a.CanecasRequeridas.OrderBy(c => (int)c)
                .SequenceEqual(b.CanecasRequeridas.OrderBy(c => (int)c));

            if (mismasCanecas && a.PuedeAbrirCanecas && b.PuedeAbrirCanecas &&
                a.EstadoGlobal == EstadoGlobalAnalisis.Clasificado &&
                b.EstadoGlobal == EstadoGlobalAnalisis.Clasificado)
            {
                return new ResultadoConsensoModelos(
                    a, b, CoincidenCanecas: true, PuedeAbrirConSeguridad: true,
                    DictamenSeguroFinal: a,
                    ResumenConsenso: $"Consenso verificado entre {a.ModeloUtilizado} y {b.ModeloUtilizado}: {a.Combinacion}.");
            }

            string motivo = !mismasCanecas
                ? $"Discrepancia entre modelos ({a.ModeloUtilizado}={a.Combinacion} vs {b.ModeloUtilizado}={b.Combinacion}): apertura bloqueada; se requiere confirmación humana."
                : $"Al menos un modelo solicitó confirmación o reportó incidencia ({a.EstadoGlobal} / {b.EstadoGlobal}): no se abre automáticamente.";

            var unionCanecas = a.CanecasRequeridas.Concat(b.CanecasRequeridas).Distinct().OrderBy(c => (int)c).ToList();
            var seguro = new ResultadoAnalisisMultirresiduo(
                EstadoGlobalAnalisis.RequiereConfirmacion,
                a.Objetos.Count >= b.Objetos.Count ? a.Objetos : b.Objetos,
                unionCanecas,
                MotorReglasMultirresiduo.CalcularCombinacion(unionCanecas),
                Math.Max(a.TotalObjetosDetectados, b.TotalObjetosDetectados),
                RequiereConfirmacion: true,
                PuedeAbrirCanecas: false,
                MensajeUsuario: "⚠️ " + motivo,
                ModeloUtilizado: $"{a.ModeloUtilizado} + {b.ModeloUtilizado}",
                LatenciaMs: a.LatenciaMs + b.LatenciaMs,
                ErrorTecnico: !mismasCanecas ? "DISCREPANCIA_MODELOS" : null,
                HashImagenSha256: a.HashImagenSha256 ?? b.HashImagenSha256);

            return new ResultadoConsensoModelos(a, b, mismasCanecas, PuedeAbrirConSeguridad: false, seguro, motivo);
        }

        /// <summary>Ejecuta la evaluación completa de ambos proveedores sobre exactamente el mismo manifiesto y filtro.</summary>
        public static async Task<InformeComparativoModelos> CompararManifiestoAsync(
            string rutaManifiesto,
            IProveedorVisionMultirresiduo proveedorA,
            IProveedorVisionMultirresiduo proveedorB,
            FiltroEvaluacion35? filtro = null,
            CancellationToken ct = default)
        {
            var ejA = new EjecutorEvaluacion35(proveedorA);
            var ejB = new EjecutorEvaluacion35(proveedorB);

            InformeEvaluacion35 infA = await ejA.EjecutarManifiestoAsync(rutaManifiesto, filtro, ct).ConfigureAwait(false);
            InformeEvaluacion35 infB = await ejB.EjecutarManifiestoAsync(rutaManifiesto, filtro, ct).ConfigureAwait(false);

            var mapaB = infB.Detalle.ToDictionary(d => d.IdPrueba, StringComparer.OrdinalIgnoreCase);
            int comun = 0, coinciden = 0, discrepancias = 0;
            foreach (var casoA in infA.Detalle)
            {
                if (!mapaB.TryGetValue(casoA.IdPrueba, out var casoB)) continue;
                comun++;
                if (string.Equals(casoA.CombinacionObtenida, casoB.CombinacionObtenida, StringComparison.OrdinalIgnoreCase))
                    coinciden++;
                else
                    discrepancias++;
            }

            // Selección estrictamente por evidencia: primero menos falsas aperturas, luego mayor exactitud de combinaciones, luego menor P95.
            string recomendado;
            string justificacion;
            if (infA.FalsasAperturas != infB.FalsasAperturas)
            {
                bool ganaA = infA.FalsasAperturas < infB.FalsasAperturas;
                recomendado = ganaA ? proveedorA.NombreProveedor : proveedorB.NombreProveedor;
                justificacion = $"Menor número de falsas aperturas ({infA.FalsasAperturas} vs {infB.FalsasAperturas}).";
            }
            else if (infA.ExactitudCombinaciones != infB.ExactitudCombinaciones)
            {
                bool ganaA = infA.ExactitudCombinaciones > infB.ExactitudCombinaciones;
                recomendado = ganaA ? proveedorA.NombreProveedor : proveedorB.NombreProveedor;
                justificacion = $"Mayor exactitud en combinaciones de canecas ({infA.ExactitudCombinaciones:P1} vs {infB.ExactitudCombinaciones:P1}) con iguales falsas aperturas ({infA.FalsasAperturas}).";
            }
            else
            {
                bool ganaA = infA.TiempoP95Ms <= infB.TiempoP95Ms;
                recomendado = ganaA ? proveedorA.NombreProveedor : proveedorB.NombreProveedor;
                justificacion = $"Igual exactitud y falsas aperturas; menor latencia P95 ({infA.TiempoP95Ms} ms vs {infB.TiempoP95Ms} ms).";
            }

            return new InformeComparativoModelos(
                DateTime.UtcNow, proveedorA.NombreProveedor, proveedorB.NombreProveedor,
                infA, infB, comun, coinciden, discrepancias, recomendado, justificacion);
        }

        public static string FormatearMarkdown(InformeComparativoModelos cmp)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Comparativa de Modelos Visuales ECOACCESS 35");
            sb.AppendLine();
            sb.AppendLine($"- **Fecha UTC:** {cmp.FechaUtc:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Casos comunes:** {cmp.CasosEnComun} · **Coincidencias:** {cmp.CoincidenciasCanecas} · **Discrepancias bloqueadas de forma segura:** {cmp.DiscrepanciasBloqueadas}");
            sb.AppendLine($"- **Recomendado por evidencia:** `{cmp.ModeloRecomendadoPorEvidencia}` ({cmp.Justificacion})");
            sb.AppendLine();
            sb.AppendLine("| Métrica | " + cmp.ModeloA + " | " + cmp.ModeloB + " |");
            sb.AppendLine("|---|---:|---:|");
            sb.AppendLine($"| Exactitud combinaciones | {cmp.InformeA.ExactitudCombinaciones:P1} ({cmp.InformeA.TotalAprobado}/{cmp.InformeA.TotalEjecutado}) | {cmp.InformeB.ExactitudCombinaciones:P1} ({cmp.InformeB.TotalAprobado}/{cmp.InformeB.TotalEjecutado}) |");
            sb.AppendLine($"| Falsas aperturas | {cmp.InformeA.FalsasAperturas} | {cmp.InformeB.FalsasAperturas} |");
            sb.AppendLine($"| Canecas omitidas | {cmp.InformeA.CanecasNecesariasOmitidas} | {cmp.InformeB.CanecasNecesariasOmitidas} |");
            sb.AppendLine($"| Casos a confirmación/rechazo | {cmp.InformeA.CasosRechazadosOConfirmacion} | {cmp.InformeB.CasosRechazadosOConfirmacion} |");
            sb.AppendLine($"| Latencia media | {cmp.InformeA.TiempoMedioMs} ms | {cmp.InformeB.TiempoMedioMs} ms |");
            sb.AppendLine($"| Latencia P95 | {cmp.InformeA.TiempoP95Ms} ms | {cmp.InformeB.TiempoP95Ms} ms |");
            sb.AppendLine($"| Fallos de integración | {cmp.InformeA.FallosIntegracion} | {cmp.InformeB.FallosIntegracion} |");
            return sb.ToString();
        }
    }
}
