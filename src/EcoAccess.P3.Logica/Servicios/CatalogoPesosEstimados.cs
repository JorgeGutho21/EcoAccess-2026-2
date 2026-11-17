using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Peso de referencia de una unidad típica de un material. Es una estimación, nunca un pesaje.</summary>
    public sealed record PesoReferencia(string CodigoMaterial, decimal KgPorUnidad, string UnidadTipica);

    /// <summary>
    /// Fase 5: catálogo versionado de pesos estimados por unidad. El depósito guarda el resultado en
    /// <c>peso_estimado_kg</c>, aparte de <c>peso_medido_kg</c>, que solo llena una báscula (RN-12). Una foto o una
    /// declaración nunca producen un peso medido.
    /// </summary>
    /// <remarks>
    /// Valores de orden de magnitud para envases comunes en Colombia, redondeados. No provienen de una medición del
    /// proyecto. Para cambiarlos se crea una versión nueva; los depósitos ya guardados conservan la estimación con la
    /// que se calcularon.
    /// </remarks>
    public static class CatalogoPesosEstimados
    {
        public const string Version = "2026.11.10-1";

        /// <summary>Unidades máximas que se estiman por depósito: más es una declaración que no se cree sin báscula.</summary>
        public const int MaximoUnidades = 50;

        private static readonly IReadOnlyDictionary<string, PesoReferencia> Pesos = new[]
        {
            new PesoReferencia("PET", 0.025m, "botella PET de 600 ml"),
            new PesoReferencia("LATA_ALUMINIO", 0.015m, "lata de aluminio de 330 ml"),
            new PesoReferencia("METAL", 0.050m, "lata de acero de conservas"),
            new PesoReferencia("CARTON", 0.100m, "caja de cartón pequeña plegada"),
            new PesoReferencia("PAPEL", 0.005m, "hoja tamaño carta"),
            new PesoReferencia("VIDRIO", 0.250m, "botella de vidrio de 330 ml"),
            new PesoReferencia("MULTICAPA", 0.030m, "envase de leche o jugo de 1 L"),
            new PesoReferencia("PEAD", 0.040m, "envase de detergente pequeño"),
            new PesoReferencia("PLASTICO_RIGIDO", 0.030m, "recipiente plástico rígido"),
            new PesoReferencia("PLASTICO_FLEXIBLE", 0.005m, "empaque flexible"),
            new PesoReferencia("BOLSA_PLASTICA", 0.005m, "bolsa plástica"),
            new PesoReferencia("ORGANICO", 0.150m, "porción de restos orgánicos"),
            new PesoReferencia("RESTOS_ALIMENTOS", 0.150m, "porción de restos de comida"),
            new PesoReferencia("SERVILLETA_USADA", 0.003m, "servilleta usada"),
            new PesoReferencia("ICOPOR_CONTAMINADO", 0.010m, "recipiente de icopor"),
        }.ToDictionary(p => p.CodigoMaterial, StringComparer.Ordinal);

        public static IReadOnlyList<PesoReferencia> Todos { get; } = Pesos.Values.OrderBy(p => p.CodigoMaterial, StringComparer.Ordinal).ToList();

        public static PesoReferencia? Buscar(string? codigoMaterial) =>
            codigoMaterial is not null && Pesos.TryGetValue(codigoMaterial, out var p) ? p : null;

        /// <summary>
        /// Peso estimado para <paramref name="unidades"/> unidades (1 si no se declaró), con tres decimales. Null si el
        /// material no tiene referencia o la cantidad no es creíble: entonces el depósito queda «sin peso».
        /// </summary>
        public static decimal? Estimar(string? codigoMaterial, int? unidades)
        {
            int n = unidades ?? 1;
            if (n < 1 || n > MaximoUnidades || Buscar(codigoMaterial) is not { } p) return null;
            decimal kg = decimal.Round(p.KgPorUnidad * n, 3, MidpointRounding.AwayFromZero);
            return kg > 0m ? kg : null;
        }
    }
}
