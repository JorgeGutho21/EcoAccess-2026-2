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
    /// Las láminas con varios residuos ya llegan recortadas panel por panel: aquí no existe la lámina completa. Las fotos
    /// sin residuo equivalente quedan como pendientes solo para el panel administrativo (ver
    /// docs/evidencias/catalogo_fotos/inventario_fotos.csv y recortes_laminas.csv).
    /// </summary>
    public static partial class CatalogoFotos
    {
        // Diferido: el orden de inicialización entre las dos partes de la clase no está garantizado.
        private static readonly Lazy<Dictionary<string, IReadOnlyList<string>>> PorResiduo = new(() =>
            Generadas.GroupBy(f => f.Residuo, StringComparer.Ordinal)
                     .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(f => f.Clave).Distinct(StringComparer.Ordinal).ToList(), StringComparer.Ordinal));

        /// <summary>Número de fotografías asociadas a un residuo (individuales y paneles recortados).</summary>
        public static int Total => Generadas.Length;

        /// <summary>Claves de las fotos de un residuo (vacío si no tiene).</summary>
        public static IReadOnlyList<string> De(string residuoId) =>
            residuoId is not null && PorResiduo.Value.TryGetValue(residuoId, out var fotos) ? fotos : Array.Empty<string>();

        /// <summary>Caneca (verde, blanca o negra) en la que quedó cada foto, para comprobar la coherencia con el catálogo.</summary>
        public static IEnumerable<(string Residuo, string Caneca, string Clave)> Todas => Generadas;

        /// <summary>Fotos de la carpeta de una caneca sin residuo equivalente: (clave, razón). Solo para administración.</summary>
        public static IReadOnlyList<(string Clave, string Nota)> Pendientes(CategoriaMaterial caneca)
        {
            string nombre = Nombre(caneca);
            return PendientesGeneradas.Where(p => p.Caneca == nombre).Select(p => (p.Clave, p.Nota)).ToList();
        }

        /// <summary>
        /// Fotos de los residuos de una caneca para la galería, en el orden del catálogo y sin repetir claves. Nunca incluye
        /// láminas ni fotos pendientes.
        /// </summary>
        public static IReadOnlyList<FotoCaneca> DeCaneca(CategoriaMaterial caneca)
        {
            var fotos = new List<FotoCaneca>();
            var vistas = new HashSet<string>(StringComparer.Ordinal);
            foreach (ResiduoPrototipo r in CatalogoPrototipo.DeCaneca(caneca))
                foreach (string clave in CatalogoPrototipo.Imagenes(r))
                    if (vistas.Add(clave)) fotos.Add(new FotoCaneca(clave, r.Id, r.Nombre, null, TipoFoto.Residuo));
            return fotos;
        }

        /// <summary>Inventario de la carpeta original de una caneca (lo calcula el script de integración).</summary>
        public static InventarioFotos Inventario(CategoriaMaterial caneca)
        {
            string nombre = Nombre(caneca);
            return InventarioGenerado.FirstOrDefault(i => i.Caneca == nombre) ?? new InventarioFotos(nombre, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>Contadores de una caneca calculados del catálogo real (nada escrito a mano en los formularios).</summary>
        public static ContadorCaneca Contar(CategoriaMaterial caneca)
        {
            IReadOnlyList<ResiduoPrototipo> residuos = CatalogoPrototipo.DeCaneca(caneca);
            int visibles = residuos.Count(CatalogoPrototipo.TieneImagen);
            InventarioFotos inv = Inventario(caneca);
            return new ContadorCaneca(caneca, residuos.Count, visibles, residuos.Count - visibles, inv.Originales,
                inv.Recortes, inv.Duplicadas, inv.Invalidas, DeCaneca(caneca).Count, inv.Pendientes);
        }

        /// <summary>Resumen de las tres canecas: los totales suman identificadores únicos de residuo.</summary>
        public static ContadorGeneral ContarTodo()
        {
            var canecas = CatalogoPrototipo.Canecas.Select(c => Contar(c.Caneca)).ToList();
            int unicos = CatalogoPrototipo.Todos.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count();
            int visibles = CatalogoPrototipo.Todos.Where(CatalogoPrototipo.TieneImagen).Select(r => r.Id).Distinct(StringComparer.Ordinal).Count();
            return new ContadorGeneral(canecas, unicos, visibles, unicos - visibles, canecas.Sum(c => c.Fotos), canecas.Sum(c => c.FotosPendientes));
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

        /// <summary>Foto sin residuo equivalente en el catálogo: solo en administración, nunca depositable.</summary>
        PorClasificar,
    }

    /// <summary>Una foto de la galería: clave de imagen, residuo (si lo tiene), título, nota y tipo.</summary>
    public sealed record FotoCaneca(string Clave, string? ResiduoId, string Titulo, string? Nota, TipoFoto Tipo);

    /// <summary>
    /// Inventario de la carpeta original de una caneca: archivos, láminas, paneles detectados, recortes publicados, fotos
    /// individuales, duplicados excluidos (exactos o perceptuales), archivos inválidos o sin revisar y fotos pendientes.
    /// </summary>
    public sealed record InventarioFotos(string Caneca, int Originales, int Laminas, int Paneles, int Recortes, int Individuales,
        int Duplicadas, int Invalidas, int Pendientes);

    /// <summary>
    /// Contadores de una caneca: residuos únicos del catálogo, los que tienen imagen (los que ve el ciudadano), los que
    /// esperan recurso gráfico, archivos originales, recortes de láminas, duplicados excluidos, inválidos, fotos
    /// publicadas y fotos pendientes de clasificar.
    /// </summary>
    public sealed record ContadorCaneca(CategoriaMaterial Caneca, int Residuos, int ConImagen, int SinImagen, int Originales,
        int Recortes, int Duplicadas, int Invalidas, int Fotos, int FotosPendientes);

    /// <summary>Resumen general de las tres canecas.</summary>
    public sealed record ContadorGeneral(IReadOnlyList<ContadorCaneca> Canecas, int ResiduosUnicos, int ConImagen, int SinImagen,
        int Fotos, int FotosPendientes);
}
