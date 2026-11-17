using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Estado de los ecopuntos de un depósito. Se deduce de los datos; no se guarda aparte.</summary>
    public enum EstadoEcopuntos
    {
        /// <summary>El depósito existe pero nadie lo verificó: los puntos son potenciales y no se pueden canjear.</summary>
        Provisional = 1,

        /// <summary>Un operario verificó el depósito y se acreditaron puntos netos positivos.</summary>
        Confirmado = 2,

        /// <summary>Se acreditaron y luego se revirtieron (netos 0), o el material verificado no da puntos.</summary>
        Anulado = 3,
    }

    /// <summary>
    /// Fase 6: matriz de ecopuntos por unidad propuesta en la misión de cierre (versión <see cref="Version"/>). Es
    /// configurable: en Oracle cada valor es una <see cref="ReglaEcopuntos"/> con vigencia que crea un administrador
    /// (<see cref="ServicioEcopuntos.RegistrarReglaAsync"/>). Los materiales con 0 no tienen regla: sin regla vigente
    /// el sistema no acredita nada, en lugar de inventar puntos.
    /// </summary>
    public static class MatrizEcopuntosPropuesta
    {
        public const string Version = "2026.11.10-1";

        private static readonly IReadOnlyDictionary<string, int> Puntos = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["LATA_ALUMINIO"] = 5,
            ["PET"] = 4,
            ["CARTON"] = 3,
            ["VIDRIO"] = 3,
            ["METAL"] = 3,
            ["PAPEL"] = 2,
            ["MULTICAPA"] = 2,
        };

        /// <summary>Materiales con 0 ecopuntos en la propuesta, por categoría o por código.</summary>
        public static readonly IReadOnlyList<string> SinPuntos = new[]
        {
            "PLASTICO_FLEXIBLE", "BOLSA_PLASTICA", "ICOPOR_CONTAMINADO", "ORGANICO", "RESTOS_ALIMENTOS",
            "SERVILLETA_USADA", "PANAL", "TOALLA_HIGIENICA", "TAMPON", "CONDON", "JERINGA", "CORTOPUNZANTE",
            "MEDICAMENTO", "PILA", "BATERIA", "RAEE", "BOMBILLO", "ACEITE",
        };

        public static IReadOnlyList<(string Codigo, int Puntos)> ConPuntos { get; } =
            Puntos.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Select(p => (p.Key, p.Value)).ToList();

        /// <summary>Puntos por unidad; 0 para todo lo que no está en la matriz (incluidos peligrosos y no aprovechables).</summary>
        public static int PorUnidad(Material material)
        {
            Compat.ThrowIfNull(material, nameof(material));
            if (material.Categoria != CategoriaMaterial.Aprovechable) return 0;
            return Puntos.TryGetValue(material.Codigo, out int p) ? p : 0;
        }

        /// <summary>Puntos potenciales de una declaración (provisional), con el mismo tope de unidades del catálogo de pesos.</summary>
        public static int Potenciales(Material material, int? unidades)
        {
            int n = unidades ?? 1;
            return n < 1 || n > CatalogoPesosEstimados.MaximoUnidades ? 0 : PorUnidad(material) * n;
        }

        /// <summary>Estado de los ecopuntos de un depósito según su verificación y sus puntos netos.</summary>
        public static EstadoEcopuntos Estado(Deposito deposito, int? puntosNetos)
        {
            Compat.ThrowIfNull(deposito, nameof(deposito));
            if (!deposito.EstaVerificado) return EstadoEcopuntos.Provisional;
            return puntosNetos is > 0 ? EstadoEcopuntos.Confirmado : EstadoEcopuntos.Anulado;
        }
    }
}
