using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>Resumen determinista del universo combinatorio de canecas para un catálogo de materiales.</summary>
    public sealed record ResumenCombinatorioCatalogo(
        int TotalMateriales,
        int TotalVerde,
        int TotalBlanca,
        int TotalNegra,
        int ParesSinRepeticion,
        int ParesUnaCaneca,
        int ParesDosCanecas,
        int TriosSinRepeticion,
        int TriosUnaCaneca,
        int TriosDosCanecas,
        int TriosTresCanecas,
        int TotalSinRepeticionParesYTrios,
        int ParesConRepeticion,
        int TriosConRepeticion);

    /// <summary>
    /// Motor matemático determinista de selección de canecas:
    /// CanecasAOrdenar = CategoriasOrdinariasUnicas(MaterialesDetectadosValidos).
    /// Separa las pruebas matemáticas combinatorias de la precisión empírica de visión artificial.
    /// </summary>
    public static class MotorCombinacionesCanecas
    {
        private static readonly CategoriaMaterial[] OrdenCanecas =
        {
            CategoriaMaterial.OrganicoAprovechable, // VERDE
            CategoriaMaterial.Aprovechable,         // BLANCA
            CategoriaMaterial.NoAprovechable,       // NEGRA
        };

        /// <summary>
        /// Calcula el conjunto deduplicado y ordenado de canecas ordinarias requeridas (0, 1, 2 o 3).
        /// Independiente del orden de entrada y de la cantidad repetida de objetos de una misma categoría.
        /// </summary>
        public static IReadOnlyList<CategoriaMaterial> CalcularCanecasUnicas(IEnumerable<CategoriaMaterial> categorias)
        {
            if (categorias is null) return Array.Empty<CategoriaMaterial>();
            var presentes = new HashSet<CategoriaMaterial>(categorias);
            return OrdenCanecas.Where(c => presentes.Contains(c)).ToList();
        }

        /// <summary>Evalúa la combinación canónica C01..C07 (o Ninguna) para un conjunto de categorías.</summary>
        public static CombinacionCanecas CalcularCombinacion(IEnumerable<CategoriaMaterial> categorias) =>
            MotorReglasMultirresiduo.CalcularCombinacion(CalcularCanecasUnicas(categorias));

        /// <summary>
        /// Enumera exhaustivamente todos los pares y tríos (sin repetición y con repetición) del catálogo indicado
        /// y verifica por conteo directo cuántas canecas distintas requiere cada escena.
        /// </summary>
        public static ResumenCombinatorioCatalogo EvaluarUniversoCombinatorio(IReadOnlyList<MaterialPrioritario35>? catalogo = null)
        {
            IReadOnlyList<MaterialPrioritario35> items = catalogo ?? CatalogoPrioritario35.Todos;
            int n = items.Count;
            int v = items.Count(m => m.Caneca == CategoriaMaterial.OrganicoAprovechable);
            int b = items.Count(m => m.Caneca == CategoriaMaterial.Aprovechable);
            int ne = items.Count(m => m.Caneca == CategoriaMaterial.NoAprovechable);

            int paresSinRep = 0, pares1 = 0, pares2 = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    paresSinRep++;
                    int c = CalcularCanecasUnicas(new[] { items[i].Caneca, items[j].Caneca }).Count;
                    if (c == 1) pares1++;
                    else if (c == 2) pares2++;
                }
            }

            int triosSinRep = 0, trios1 = 0, trios2 = 0, trios3 = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    for (int k = j + 1; k < n; k++)
                    {
                        triosSinRep++;
                        int c = CalcularCanecasUnicas(new[] { items[i].Caneca, items[j].Caneca, items[k].Caneca }).Count;
                        if (c == 1) trios1++;
                        else if (c == 2) trios2++;
                        else if (c == 3) trios3++;
                    }
                }
            }

            int paresConRep = 0;
            for (int i = 0; i < n; i++)
                for (int j = i; j < n; j++)
                    paresConRep++;

            int triosConRep = 0;
            for (int i = 0; i < n; i++)
                for (int j = i; j < n; j++)
                    for (int k = j; k < n; k++)
                        triosConRep++;

            return new ResumenCombinatorioCatalogo(
                n, v, b, ne,
                paresSinRep, pares1, pares2,
                triosSinRep, trios1, trios2, trios3,
                paresSinRep + triosSinRep,
                paresConRep, triosConRep);
        }
    }
}