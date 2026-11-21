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
    /// Matriz de ecopuntos por unidad propuesta el 21/11/2026 (versión <see cref="Version"/>), PENDIENTE de aprobación
    /// administrativa: no usa peso. Caneca blanca: lata 10, PET 8, cartón 7, vidrio 7, papel 5 (metal y multicapa conservan
    /// la versión 2026.11.10-1 hasta que se reevalúen). Caneca verde: ORGANICO 2; Oracle tiene un único código orgánico,
    /// así que no se pueden distinguir frutas (4), café (3) y hojas (2) sin dividir ese material: se usa el valor menor.
    /// Caneca negra: 0. Es
    /// configurable: en Oracle cada valor es una <see cref="ReglaEcopuntos"/> con vigencia que crea un administrador
    /// (<see cref="ServicioEcopuntos.RegistrarReglaAsync"/>). Los materiales con 0 no tienen regla: sin regla vigente
    /// el sistema no acredita nada, en lugar de inventar puntos.
    /// </summary>
    public static class MatrizEcopuntosPropuesta
    {
        public const string Version = "2026.11.21-1";

        private static readonly IReadOnlyDictionary<string, int> Puntos = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["LATA_ALUMINIO"] = 10,
            ["PET"] = 8,
            ["CARTON"] = 7,
            ["VIDRIO"] = 7,
            ["PAPEL"] = 5,
            ["METAL"] = 3,
            ["MULTICAPA"] = 2,
            ["ORGANICO"] = 2,
        };

        /// <summary>Materiales con 0 ecopuntos en la propuesta, por categoría o por código.</summary>
        public static readonly IReadOnlyList<string> SinPuntos = new[]
        {
            "PLASTICO_FLEXIBLE", "BOLSA_PLASTICA", "ICOPOR_CONTAMINADO", "RESTOS_ALIMENTOS",
            "SERVILLETA_USADA", "PANAL", "TOALLA_HIGIENICA", "TAMPON", "CONDON", "JERINGA", "CORTOPUNZANTE",
            "MEDICAMENTO", "PILA", "BATERIA", "RAEE", "BOMBILLO", "ACEITE",
        };

        public static IReadOnlyList<(string Codigo, int Puntos)> ConPuntos { get; } =
            Puntos.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Select(p => (p.Key, p.Value)).ToList();

        /// <summary>Puntos por unidad; 0 para todo lo que no está en la matriz (incluidos peligrosos y no aprovechables).</summary>
        public static int PorUnidad(Material material)
        {
            Compat.ThrowIfNull(material, nameof(material));
            // La caneca negra (y cualquier categoría desconocida) nunca da puntos.
            if (material.Categoria is not (CategoriaMaterial.Aprovechable or CategoriaMaterial.OrganicoAprovechable)) return 0;
            return Puntos.TryGetValue(material.Codigo, out int p) ? p : 0;
        }

        /// <summary>Puntos por unidad que la propuesta asigna a un residuo del catálogo (0 en la caneca negra).</summary>
        public static int DeResiduo(ResiduoPrototipo residuo)
        {
            Compat.ThrowIfNull(residuo, nameof(residuo));
            if (residuo.Caneca == CategoriaMaterial.NoAprovechable) return 0;
            return Puntos.TryGetValue(residuo.CodigoMaterial, out int p) ? p : 0;
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
