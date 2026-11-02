using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Una foto del conjunto de evaluación: la etiqueta correcta y la que obtuvo el servicio.</summary>
    /// <param name="Esperada">Código del material, o el nombre de un <see cref="ResultadoClasificacion"/> distinto de Identificado.</param>
    public sealed record MuestraEvaluacion(string Esperada, string Obtenida);

    /// <summary>Precisión y exhaustividad de una etiqueta; null cuando el denominador es cero.</summary>
    public sealed record MetricaEtiqueta(string Etiqueta, int Esperadas, int Obtenidas, int Aciertos, decimal? Precision, decimal? Exhaustividad);

    /// <summary>
    /// Resultado de una evaluación. Describe el conjunto con el que se calculó y nada más: con el simulador mide el
    /// flujo, no un modelo; con un proveedor real solo vale para ese conjunto, esa versión y ese umbral.
    /// </summary>
    /// <param name="Abstenciones">Fotos en las que el servicio no propuso material (incierta o proveedor no disponible).</param>
    public sealed record InformeEvaluacion(int Total, int Aciertos, decimal Exactitud, int Abstenciones, decimal TasaAbstencion, IReadOnlyList<MetricaEtiqueta> PorEtiqueta);

    /// <summary>Cálculo de métricas de clasificación sobre muestras etiquetadas. Solo aritmética: no ejecuta ningún clasificador.</summary>
    public static class EvaluadorClasificacion
    {
        /// <summary>
        /// Etiqueta comparable de una conclusión: el código del material si se identificó, o el nombre del resultado.
        /// No chocan: los códigos de material van en mayúsculas («PET») y los resultados no («SinResiduo»).
        /// </summary>
        /// <exception cref="ArgumentException">Si dice Identificado pero no trae la consulta del material.</exception>
        public static string Etiqueta(ClasificacionImagen clasificacion)
        {
            Compat.ThrowIfNull(clasificacion, nameof(clasificacion));
            if (clasificacion.Resultado != ResultadoClasificacion.Identificado)
            {
                return clasificacion.Resultado.ToString();
            }

            return clasificacion.Consulta?.Material.Codigo
                ?? throw new ArgumentException("Una clasificación identificada debe traer el material.", nameof(clasificacion));
        }

        /// <exception cref="ArgumentException">Si no hay muestras o alguna etiqueta está vacía.</exception>
        public static InformeEvaluacion Evaluar(IReadOnlyCollection<MuestraEvaluacion> muestras)
        {
            Compat.ThrowIfNull(muestras, nameof(muestras));
            if (muestras.Count == 0)
            {
                throw new ArgumentException("La evaluación necesita al menos una muestra.", nameof(muestras));
            }

            if (muestras.Any(m => m is null || Compat.IsNullOrWhiteSpace(m.Esperada) || Compat.IsNullOrWhiteSpace(m.Obtenida)))
            {
                throw new ArgumentException("Cada muestra necesita etiqueta esperada y obtenida.", nameof(muestras));
            }

            int aciertos = muestras.Count(m => m.Esperada == m.Obtenida);
            int abstenciones = muestras.Count(m =>
                m.Obtenida is nameof(ResultadoClasificacion.Incierta) or nameof(ResultadoClasificacion.ProveedorNoDisponible)
                    or nameof(ResultadoClasificacion.CuotaGratuitaAgotada));

            List<MetricaEtiqueta> porEtiqueta = muestras
                .SelectMany(m => new[] { m.Esperada, m.Obtenida })
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(etiqueta =>
                {
                    int esperadas = muestras.Count(m => m.Esperada == etiqueta);
                    int obtenidas = muestras.Count(m => m.Obtenida == etiqueta);
                    int correctas = muestras.Count(m => m.Esperada == etiqueta && m.Obtenida == etiqueta);
                    return new MetricaEtiqueta(etiqueta, esperadas, obtenidas, correctas, Proporcion(correctas, obtenidas), Proporcion(correctas, esperadas));
                })
                .ToList();

            return new InformeEvaluacion(muestras.Count, aciertos, Proporcion(aciertos, muestras.Count)!.Value,
                abstenciones, Proporcion(abstenciones, muestras.Count)!.Value, porEtiqueta);
        }

        private static decimal? Proporcion(int parte, int total) =>
            total == 0 ? null : decimal.Round((decimal)parte / total, 4, MidpointRounding.AwayFromZero);
    }
}
