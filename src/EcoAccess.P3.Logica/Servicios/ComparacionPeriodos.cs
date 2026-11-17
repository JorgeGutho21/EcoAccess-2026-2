using System;
using System.Collections.Generic;
using System.Globalization;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    public sealed record FilaComparacion(string Indicador, long Actual, long Anterior, long Diferencia, string Variacion);

    /// <summary>Compara dos períodos de igual duración. No inventa porcentajes: sin base anterior no hay variación que calcular.</summary>
    public static class ComparacionPeriodos
    {
        public const string SinBase = "No aplica: período anterior en cero";

        public static string Variacion(long actual, long anterior)
        {
            if (anterior == 0) return actual == 0 ? "Sin cambio" : SinBase;
            decimal porcentaje = decimal.Round((actual - anterior) * 100m / Math.Abs(anterior), 1, MidpointRounding.AwayFromZero);
            return porcentaje.ToString("+0.#;-0.#;0", CultureInfo.GetCultureInfo("es-CO")) + " %";
        }

        public static FilaComparacion Fila(string indicador, long actual, long anterior) =>
            new(indicador, actual, anterior, actual - anterior, Variacion(actual, anterior));

        public static IReadOnlyList<FilaComparacion> Filas(IndicadoresInforme actual, IndicadoresInforme anterior) => new[]
        {
            Fila("Solicitudes", actual.Solicitudes, anterior.Solicitudes),
            Fila("Depósitos registrados", actual.Depositos, anterior.Depositos),
            Fila("Depósitos verificados", actual.Verificados, anterior.Verificados),
            Fila("Unidades verificadas", actual.UnidadesVerificadas, anterior.UnidadesVerificadas),
            Fila("Ciudadanos participantes únicos", actual.Ciudadanos, anterior.Ciudadanos),
            Fila("Ecopuntos acreditados", actual.Acreditados, anterior.Acreditados),
            Fila("Ecopuntos reversados", actual.Reversos, anterior.Reversos),
            Fila("Ecopuntos netos", actual.Netos, anterior.Netos),
            Fila("Opiniones recibidas", actual.Opiniones, anterior.Opiniones),
        };
    }
}
