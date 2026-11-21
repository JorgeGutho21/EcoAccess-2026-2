using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Fotografías de los residuos (carpetas CANECA BLANCA, VERDE y NEGRA, integradas con
    /// scripts/documentacion/Integrar-FotosResiduos.py). Cada foto pertenece a un residuo de <see cref="CatalogoPrototipo"/>
    /// y a su caneca; las claves usan el formato de <see cref="ResiduoPrototipo.Imagen"/> («caneca.fotos.residuo-n»).
    /// Las fotos que no tienen residuo equivalente quedan fuera (ver docs/evidencias/catalogo_fotos/inventario_fotos.csv).
    /// </summary>
    public static partial class CatalogoFotos
    {
        // Diferido: el orden de inicialización entre las dos partes de la clase no está garantizado.
        private static readonly Lazy<Dictionary<string, IReadOnlyList<string>>> PorResiduo = new(() =>
            Generadas.GroupBy(f => f.Residuo, StringComparer.Ordinal)
                     .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(f => f.Clave).ToList(), StringComparer.Ordinal));

        /// <summary>Número de fotografías integradas.</summary>
        public static int Total => Generadas.Length;

        /// <summary>Claves de las fotos de un residuo (vacío si no tiene).</summary>
        public static IReadOnlyList<string> De(string residuoId) =>
            residuoId is not null && PorResiduo.Value.TryGetValue(residuoId, out var fotos) ? fotos : Array.Empty<string>();

        /// <summary>Caneca (verde, blanca o negra) en la que quedó cada foto, para comprobar la coherencia con el catálogo.</summary>
        public static IEnumerable<(string Residuo, string Caneca, string Clave)> Todas => Generadas;

        /// <summary>Guías visuales (láminas con varios residuos) de una caneca.</summary>
        public static IReadOnlyList<string> Guias(CategoriaMaterial caneca)
        {
            string nombre = caneca switch
            {
                CategoriaMaterial.OrganicoAprovechable => CatalogoPrototipo.Verde,
                CategoriaMaterial.Aprovechable => CatalogoPrototipo.Blanca,
                _ => CatalogoPrototipo.Negra,
            };
            return GuiasGeneradas.Where(g => g.Caneca == nombre).Select(g => g.Clave).ToList();
        }
    }
}
