using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Vision
{

    public sealed record CeldaConfusion(string Esperada, string Obtenida, int Cantidad);

    /// <summary>Medidas de un motor sobre un corpus. Solo describen ese corpus; P99 es null con menos de 100 muestras.</summary>
    public sealed record ResumenVision(string Proveedor, int Evaluadas, int Aciertos, int Errores, int Abstenciones, decimal PrecisionGlobal,
        IReadOnlyList<MetricaEtiqueta> PorMaterial, IReadOnlyList<CeldaConfusion> MatrizConfusion, int FalsasAdmisiones, int FalsosRechazos,
        long LatenciaMediaMs, long LatenciaMedianaMs, long LatenciaP95Ms, long? LatenciaP99Ms, int FallosTecnicos, decimal DisponibilidadObservada,
        long TokensEntrada, long TokensSalida, decimal CostoEstimadoUsd);

    public sealed record ComparacionVisionInforme(DateTime FechaUtc, string Conjunto, string LicenciaDeclarada, int Solicitadas, IReadOnlyList<ResumenVision> Motores);

    /// <summary>Solo aritmética sobre un informe ya medido; no ejecuta modelos ni escribe en Oracle.</summary>
    public static class MetricasVision
    {
        public static ResumenVision Calcular(InformeVisionArchivos informe, IReadOnlyCollection<Material> catalogo,
            long tokensEntrada = 0, long tokensSalida = 0, decimal costoUsd = 0m)
        {
            Compat.ThrowIfNull(informe, nameof(informe));
            var muestras = informe.Muestras;
            if (muestras.Count == 0) throw new ArgumentException("El informe no tiene muestras.", nameof(informe));

            var codigos = new HashSet<string>(catalogo.Select(m => m.Codigo), StringComparer.Ordinal);
            var aprovechables = new HashSet<string>(catalogo
                .Where(m => m.Categoria is CategoriaMaterial.Aprovechable or CategoriaMaterial.OrganicoAprovechable).Select(m => m.Codigo), StringComparer.Ordinal);

            int aciertos = muestras.Count(m => m.Esperada == m.Obtenida);
            int tecnicos = muestras.Count(m => m.Obtenida == nameof(ResultadoClasificacion.ProveedorNoDisponible));
            int abstenciones = muestras.Count(m => m.Esperada != m.Obtenida && !codigos.Contains(m.Obtenida)
                && m.Obtenida != nameof(ResultadoClasificacion.ProveedorNoDisponible));
            // Decir «aprovechable» de algo que no lo es contamina el reciclaje; es el error más grave de la evaluación.
            int falsasAdmisiones = muestras.Count(m => aprovechables.Contains(m.Obtenida) && !aprovechables.Contains(m.Esperada));
            int falsosRechazos = muestras.Count(m => aprovechables.Contains(m.Esperada) && !codigos.Contains(m.Obtenida));
            long[] tiempos = muestras.Select(m => m.DuracionMilisegundos).OrderBy(t => t).ToArray();

            return new ResumenVision(informe.Proveedor, muestras.Count, aciertos, muestras.Count - aciertos, abstenciones,
                Proporcion(aciertos, muestras.Count), informe.Metricas.PorEtiqueta,
                muestras.GroupBy(m => (m.Esperada, m.Obtenida)).Select(g => new CeldaConfusion(g.Key.Esperada, g.Key.Obtenida, g.Count()))
                    .OrderBy(c => c.Esperada, StringComparer.Ordinal).ThenBy(c => c.Obtenida, StringComparer.Ordinal).ToArray(),
                falsasAdmisiones, falsosRechazos, (long)tiempos.Average(), Percentil(tiempos, 0.50), Percentil(tiempos, 0.95),
                tiempos.Length >= 100 ? Percentil(tiempos, 0.99) : (long?)null, tecnicos, Proporcion(muestras.Count - tecnicos, muestras.Count),
                tokensEntrada, tokensSalida, costoUsd);
        }

        /// <summary>Percentil por rango más cercano: siempre es un tiempo realmente observado.</summary>
        public static long Percentil(IReadOnlyList<long> ordenados, double percentil)
        {
            if (ordenados.Count == 0) throw new ArgumentException("No hay valores.", nameof(ordenados));
            int rango = (int)Math.Ceiling(percentil * ordenados.Count);
            return ordenados[Math.Max(0, Math.Min(ordenados.Count - 1, rango - 1))];
        }

        private static decimal Proporcion(int parte, int total) => decimal.Round((decimal)parte / total, 4, MidpointRounding.AwayFromZero);
    }
}
