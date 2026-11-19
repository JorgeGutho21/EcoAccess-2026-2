using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Grupos que ve la persona en la pantalla táctil de la estación. Agrupan por lo que la persona tiene en la mano
    /// (un plástico, un papel…), no por caneca: dentro de «Papel y cartón» hay papel limpio (blanca) y servilleta usada
    /// (negra). La caneca sigue saliendo de la categoría guardada en Oracle para cada material.
    /// </summary>
    public enum GrupoEstacion
    {
        Plastico = 1,
        PapelCarton = 2,
        VidrioMetal = 3,
        Comida = 4,
        Otros = 5,
        Especiales = 6
    }

    /// <summary>Lo que muestra la estación al elegir un material: destino, si va en alguna de las tres canecas y la indicación.</summary>
    /// <param name="Destino">Calculado con <see cref="ResolutorDestino.PorCategoria"/>, el mismo que usa Telegram.</param>
    public sealed record OrientacionEstacion(Material Material, DestinoResiduo Destino, string Titulo, string Indicacion)
    {
        /// <summary>Solo blanca, verde y negra son canecas de la estación; lo demás no va en ninguna.</summary>
        public bool VaEnCaneca => Destino is DestinoResiduo.Blanca or DestinoResiduo.Verde or DestinoResiduo.Negra;
    }

    /// <summary>
    /// Reglas de la pantalla táctil de la estación (observación del profesor Patiño: la misma aplicación, conectada a la
    /// misma base, con un solo botón grande y luego «escoge»). Es lógica pura: no consulta Oracle ni abre canecas; la
    /// estación solo orienta, y la apertura sigue siendo del flujo de Telegram con QR y presencia.
    /// </summary>
    public static class GuiaEstacionTactil
    {
        /// <summary>Grupos en el orden en que aparecen en pantalla.</summary>
        public static IReadOnlyList<GrupoEstacion> Grupos { get; } = new[]
        {
            GrupoEstacion.Plastico, GrupoEstacion.PapelCarton, GrupoEstacion.VidrioMetal,
            GrupoEstacion.Comida, GrupoEstacion.Otros, GrupoEstacion.Especiales
        };

        public static string Nombre(GrupoEstacion grupo) => grupo switch
        {
            GrupoEstacion.Plastico => "Plástico",
            GrupoEstacion.PapelCarton => "Papel y cartón",
            GrupoEstacion.VidrioMetal => "Vidrio y metal",
            GrupoEstacion.Comida => "Comida",
            GrupoEstacion.Otros => "Higiénicos y otros",
            GrupoEstacion.Especiales => "Pilas y medicinas",
            _ => grupo.ToString()
        };

        /// <summary>
        /// Grupo de un material. Lo peligroso siempre va a «Especiales» y lo orgánico a «Comida», por su categoría; el resto
        /// se reconoce por palabras de su código o nombre. Lo que no se reconoce queda en «Otros», así un material nuevo
        /// del catálogo nunca desaparece de la pantalla.
        /// </summary>
        public static GrupoEstacion GrupoDe(Material material)
        {
            if (material is null) throw new ArgumentNullException(nameof(material));
            if (material.Categoria == CategoriaMaterial.Peligroso) return GrupoEstacion.Especiales;
            if (material.Categoria == CategoriaMaterial.OrganicoAprovechable) return GrupoEstacion.Comida;

            string texto = Normalizar(material.Codigo + " " + material.Nombre);
            if (Contiene(texto, "PAPEL", "CARTON", "MULTICAPA", "SERVILLETA", "CUADERNO")) return GrupoEstacion.PapelCarton;
            if (Contiene(texto, "VIDRIO", "LATA", "METAL", "ALUMINIO", "CHATARRA")) return GrupoEstacion.VidrioMetal;
            if (Contiene(texto, "PLASTIC", "PET", "PEAD", "BOLSA", "ICOPOR")) return GrupoEstacion.Plastico;
            return GrupoEstacion.Otros;
        }

        /// <summary>Materiales del grupo, ordenados por nombre.</summary>
        public static IReadOnlyList<Material> MaterialesDe(GrupoEstacion grupo, IEnumerable<Material> catalogo) =>
            (catalogo ?? throw new ArgumentNullException(nameof(catalogo)))
                .Where(m => GrupoDe(m) == grupo)
                .OrderBy(m => m.Nombre, StringComparer.CurrentCulture)
                .ToList();

        /// <summary>Orientación del material según su categoría en Oracle; los especiales no van en ninguna de las tres canecas.</summary>
        public static OrientacionEstacion Orientar(Material material)
        {
            if (material is null) throw new ArgumentNullException(nameof(material));
            DestinoResiduo destino = ResolutorDestino.PorCategoria(material.Categoria);
            // Para las tres canecas basta la guía general (o la precaución del material); la ruta de separación guardada en Oracle
            // solo se muestra para lo especial, porque es la que dice adónde llevarlo.
            string indicacion = destino == DestinoResiduo.Posconsumo
                ? material.RutaSeparacion ?? material.Precauciones ?? GuiaSeparacion.Para(material.Categoria).Indicacion
                : material.Precauciones ?? GuiaSeparacion.Para(material.Categoria).Indicacion;
            string titulo = destino switch
            {
                DestinoResiduo.Blanca => "Caneca blanca",
                DestinoResiduo.Verde => "Caneca verde",
                DestinoResiduo.Negra => "Caneca negra",
                _ => "No va en estas canecas",
            };
            return new OrientacionEstacion(material, destino, titulo, indicacion);
        }

        private static bool Contiene(string texto, params string[] palabras) => palabras.Any(p => texto.Contains(p));

        private static string Normalizar(string texto)
        {
            var salida = new StringBuilder(texto.Length);
            foreach (char c in texto.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    salida.Append(char.ToUpperInvariant(c));
            return salida.ToString();
        }
    }
}
