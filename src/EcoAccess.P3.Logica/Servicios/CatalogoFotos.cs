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
    /// Las fotos sin residuo equivalente se muestran «por clasificar» con su razón, nunca como depositables
    /// (ver docs/evidencias/catalogo_fotos/inventario_fotos.csv).
    /// </summary>
    public static partial class CatalogoFotos
    {
        // Diferido: el orden de inicialización entre las dos partes de la clase no está garantizado.
        private static readonly Lazy<Dictionary<string, IReadOnlyList<string>>> PorResiduo = new(() =>
            Generadas.GroupBy(f => f.Residuo, StringComparer.Ordinal)
                     .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(f => f.Clave).ToList(), StringComparer.Ordinal));

        /// <summary>Número de fotografías asociadas a un residuo.</summary>
        public static int Total => Generadas.Length;

        /// <summary>Número de fotos que se muestran en total (de residuos, guías y por clasificar).</summary>
        public static int TotalVisibles => Generadas.Length + GuiasGeneradas.Length + PendientesGeneradas.Length;

        /// <summary>Claves de las fotos de un residuo (vacío si no tiene).</summary>
        public static IReadOnlyList<string> De(string residuoId) =>
            residuoId is not null && PorResiduo.Value.TryGetValue(residuoId, out var fotos) ? fotos : Array.Empty<string>();

        /// <summary>Caneca (verde, blanca o negra) en la que quedó cada foto, para comprobar la coherencia con el catálogo.</summary>
        public static IEnumerable<(string Residuo, string Caneca, string Clave)> Todas => Generadas;

        /// <summary>Guías visuales (láminas con varios residuos) de una caneca.</summary>
        public static IReadOnlyList<string> Guias(CategoriaMaterial caneca)
        {
            string nombre = Nombre(caneca);
            return GuiasGeneradas.Where(g => g.Caneca == nombre).Select(g => g.Clave).ToList();
        }

        /// <summary>Fotos de la carpeta de una caneca que no tienen residuo equivalente: (clave, razón).</summary>
        public static IReadOnlyList<(string Clave, string Nota)> Pendientes(CategoriaMaterial caneca)
        {
            string nombre = Nombre(caneca);
            return PendientesGeneradas.Where(p => p.Caneca == nombre).Select(p => (p.Clave, p.Nota)).ToList();
        }

        /// <summary>
        /// TODAS las fotos de la carpeta de una caneca, para la galería: primero las de cada residuo (en el orden del
        /// catálogo), luego las guías y al final las que están por clasificar (nunca se presentan como depositables).
        /// </summary>
        public static IReadOnlyList<FotoCaneca> DeCaneca(CategoriaMaterial caneca)
        {
            var fotos = new List<FotoCaneca>();
            foreach (ResiduoPrototipo r in CatalogoPrototipo.DeCaneca(caneca))
                fotos.AddRange(De(r.Id).Select(clave => new FotoCaneca(clave, r.Id, r.Nombre, null, TipoFoto.Residuo)));
            fotos.AddRange(Guias(caneca).Select(clave => new FotoCaneca(clave, null, "Guía visual", null, TipoFoto.Guia)));
            fotos.AddRange(Pendientes(caneca).Select(p => new FotoCaneca(p.Clave, null, "Por clasificar", p.Nota, TipoFoto.PorClasificar)));
            return fotos;
        }

        private static string Nombre(CategoriaMaterial caneca) => caneca switch
        {
            CategoriaMaterial.OrganicoAprovechable => CatalogoPrototipo.Verde,
            CategoriaMaterial.Aprovechable => CatalogoPrototipo.Blanca,
            _ => CatalogoPrototipo.Negra,
        };
    }

    /// <summary>Qué es una foto de la galería de una caneca.</summary>
    public enum TipoFoto
    {
        /// <summary>Foto de un residuo del catálogo.</summary>
        Residuo,

        /// <summary>Lámina con varios residuos de la caneca.</summary>
        Guia,

        /// <summary>Foto sin residuo equivalente en el catálogo: se muestra con su razón y no se puede depositar.</summary>
        PorClasificar,
    }

    /// <summary>Una foto de la galería: clave de imagen, residuo (si lo tiene), título, nota y tipo.</summary>
    public sealed record FotoCaneca(string Clave, string? ResiduoId, string Titulo, string? Nota, TipoFoto Tipo);
}
