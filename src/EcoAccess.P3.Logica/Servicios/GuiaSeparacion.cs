using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Cómo se separa un residuo de una categoría: el color de la caneca y una indicación breve.</summary>
    /// <param name="ColorCaneca">Color del código de la Resolución 2184 de 2019; null si no va en ninguna caneca de colores.</param>
    public sealed record IndicacionSeparacion(CategoriaMaterial Categoria, string Nombre, string? ColorCaneca, string Indicacion);

    /// <summary>
    /// Guía educativa de separación en la fuente según el código de colores de la Resolución 2184 de 2019
    /// (Ministerio de Ambiente y Desarrollo Sostenible): blanco para aprovechables, verde para orgánicos
    /// aprovechables y negro para no aprovechables. Los peligrosos no pertenecen a ese código: se indica llevarlos
    /// a un punto de recolección o programa posconsumo, sin citar otra norma que no se haya verificado.
    /// </summary>
    /// <remarks>
    /// Responde cómo se clasifica un material en general; si una caneca concreta lo recibe lo decide su lista de
    /// materiales (<see cref="Contenedor.AdmiteMaterial"/>) y si la persona puede usarla, <see cref="ServicioInteracciones"/>.
    /// </remarks>
    public static class GuiaSeparacion
    {
        /// <summary>Orientación específica antes que la regla general; evita enviar jeringas a una ruta de medicamentos.</summary>
        public static string Orientacion(Material material) => string.Join("\n", new[]
        {
            material.Descripcion ?? Para(material.Categoria).Indicacion,
            material.Precauciones is { } precauciones ? "Precauciones: " + precauciones : null,
            material.RutaSeparacion is { } ruta ? "Ruta de separación: " + ruta : null
        }.Where(texto => !Compat.IsNullOrWhiteSpace(texto)));

        private static readonly IReadOnlyDictionary<CategoriaMaterial, IndicacionSeparacion> Indicaciones =
            new Dictionary<CategoriaMaterial, IndicacionSeparacion>
            {
                [CategoriaMaterial.Aprovechable] = new(CategoriaMaterial.Aprovechable, "Aprovechable", "blanca",
                    "Plástico, vidrio, metales, papel y cartón. Entrégalos limpios y secos."),
                [CategoriaMaterial.OrganicoAprovechable] = new(CategoriaMaterial.OrganicoAprovechable, "Orgánico aprovechable", "verde",
                    "Restos de comida y otros residuos orgánicos que se pueden aprovechar."),
                [CategoriaMaterial.NoAprovechable] = new(CategoriaMaterial.NoAprovechable, "No aprovechable", "negra",
                    "Papel higiénico, servilletas, y papeles o empaques contaminados con comida."),
                [CategoriaMaterial.Peligroso] = new(CategoriaMaterial.Peligroso, "Especial o peligroso", null,
                    "Requiere una ruta diferenciada. Consulta el gestor adecuado para este residuo; no va en la caneca piloto."),
            };

        /// <summary>Las indicaciones de todas las categorías, en el orden del enum.</summary>
        public static IReadOnlyList<IndicacionSeparacion> Todas { get; } =
            Compat.GetValues<CategoriaMaterial>().Select(Para).ToList();

        public static IndicacionSeparacion Para(CategoriaMaterial categoria) =>
            Indicaciones.TryGetValue(categoria, out IndicacionSeparacion? indicacion)
                ? indicacion
                : throw new ArgumentOutOfRangeException(nameof(categoria), categoria, "Categoría de material no definida.");
    }
}
