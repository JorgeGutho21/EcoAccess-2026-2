using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35.Expo
{
    /// <summary>
    /// Uno de los nueve materiales del Modo ExpoSoftware. Reutiliza el código del catálogo de 35 (<see cref="Codigo35"/>) y
    /// el código de material de Oracle cuando existen; la media manzana no tiene código propio en el catálogo de 35 y se
    /// registra con el material orgánico genérico de Oracle (ORGANICO), sin inventar códigos nuevos en la base.
    /// </summary>
    public sealed record MaterialExpo(
        string Codigo,
        string Nombre,
        string NombreCorto,
        CategoriaMaterial Caneca,
        string? Codigo35,
        string CodigoOracle,
        string Etiqueta,
        string Emoji);

    /// <summary>Clasificación de un nombre devuelto por el modelo visual.</summary>
    public enum TipoNombreExpo
    {
        Material = 1,
        Ignorado = 2,
        Desconocido = 3,
        GestionEspecial = 4,
    }

    /// <summary>
    /// Catálogo prioritario de ExpoSoftware: 9 materiales (3 por caneca). El modelo describe libremente lo que ve con un
    /// nombre corto en inglés y estas reglas deterministas de C# lo traducen a uno de los 9 materiales. Lo que no encaja
    /// queda como desconocido o de gestión especial: nunca se fuerza a una caneca.
    /// </summary>
    public static class CatalogoExpo9
    {
        public const string Version = "2026.11.25-expo9";

        public static IReadOnlyList<MaterialExpo> Todos { get; } = new[]
        {
            new MaterialExpo("E01", "Media manzana", "media manzana", CategoriaMaterial.OrganicoAprovechable, null, "ORGANICO", "apple", "🍎"),
            new MaterialExpo("E02", "Cáscara de banano", "cáscara de banano", CategoriaMaterial.OrganicoAprovechable, "V01", "ORGANICO", "banana_peel", "🍌"),
            new MaterialExpo("E03", "Papa o media papa", "papa", CategoriaMaterial.OrganicoAprovechable, "V03", "ORGANICO", "potato", "🥔"),
            new MaterialExpo("E04", "Bolsa de papel kraft o cartón arrugado, limpio y seco", "papel kraft o cartón", CategoriaMaterial.Aprovechable, "B09", "CARTON", "kraft_cardboard", "📦"),
            new MaterialExpo("E05", "Botella plástica pequeña, vacía y limpia", "botella plástica", CategoriaMaterial.Aprovechable, "B01", "PET", "plastic_bottle", "🧴"),
            new MaterialExpo("E06", "Frasco pequeño de vidrio, vacío y limpio", "frasco de vidrio", CategoriaMaterial.Aprovechable, "B06", "VIDRIO", "glass_jar", "🫙"),
            new MaterialExpo("E07", "Papel higiénico usado (muestra higiénica)", "papel higiénico usado", CategoriaMaterial.NoAprovechable, "N02", "SERVILLETA_USADA", "toilet_paper", "🧻"),
            new MaterialExpo("E08", "Servilleta usada o contaminada", "servilleta usada", CategoriaMaterial.NoAprovechable, "N01", "SERVILLETA_USADA", "used_napkin", "🧽"),
            new MaterialExpo("E09", "Envoltura de mecato metalizada no aprovechable", "envoltura metalizada", CategoriaMaterial.NoAprovechable, "N08", "PLASTICO_SIN_ID", "snack_wrapper", "🍫"),
        };

        public static MaterialExpo? PorCodigo(string? codigo) => Todos.FirstOrDefault(m => string.Equals(m.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        public static MaterialExpo? PorEtiqueta(string? etiqueta) => Todos.FirstOrDefault(m => string.Equals(m.Etiqueta, etiqueta, StringComparison.OrdinalIgnoreCase));

        // Partes de la escena que no son residuos: la mano que sostiene el objeto, la mesa o el personaje ECO de las láminas.
        private static readonly Regex Ignorados = new(@"\b(hands?|fingers?|palm|thumb|gloves?|arms?|wrist|robot\w*|mascot|table|desk|tabletop|surface|background|wall|floor|shadow|cloth|tablecloth|logo|sticker|label|helmet|earpiece|leaf logo)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Gestión especial o peligro: nunca abre una caneca por foto.
        private static readonly Regex Especiales = new(@"\b(batter(y|ies)|pill|pills|blister|medicine|medication|tablet|syringe|needle|cable|charger|wire|phone|electronic\w*|broken|shard|knife|razor|blade|lighter|aerosol|spray|chemical|person|people|face|child|animal|dog|cat|pila|jeringa|medicamento)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Orden importante: lo más específico primero (vidrio antes que botella; papel higiénico antes que papel genérico).
        private static readonly (string Codigo, Regex Patron)[] Reglas =
        {
            ("E01", new Regex(@"\b(apples?|manzanas?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E02", new Regex(@"\b(banana|bananas|banano|guineo|plantain)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E03", new Regex(@"\b(potato|potatoes|papa|papas|patata)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E07", new Regex(@"\b(toilet|papel higi\w*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E08", new Regex(@"\b(napkins?|tissues?|servillet\w*|paper towel|kleenex)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E09", new Regex(@"\b(chips?|crisps?|snack\w*|wrappers?|wrapping|candy bag|cracker\w*|foil bag|metallic bag|mylar|packet|envoltura|mecato|paquete)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E06", new Regex(@"\b(glass|jar|jars|frasco|vidrio)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E05", new Regex(@"\b(bottles?|botella)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("E04", new Regex(@"\b(cardboard|kraft|paper bag|brown paper|carton|box|boxes|caja|cart[oó]n|bolsa de papel)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        };

        // Variantes que parecen de los 9 pero no lo son (fuera de alcance o no aprovechables): quedan como desconocidas.
        private static readonly Regex FueraDeAlcance = new(@"\b(juice box|milk|tetra|pizza|greasy|aluminum foil|aluminium foil|tin foil|can\b|cans\b|soda can|game case|cup|styrofoam|foam)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex PapelBlancoDudoso = new(@"\b(tissues?|tissue paper|paper|crumpled|white)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex SenalUso = new(@"\b(used|dirty|stained|soiled|greasy|wet|sucia|usada|manchad\w*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Traduce un nombre libre del modelo a uno de los 9 materiales, o lo marca como ignorado, desconocido o especial.</summary>
        public static (TipoNombreExpo Tipo, MaterialExpo? Material) Interpretar(string? nombre)
        {
            string n = (nombre ?? "").Trim();
            if (n.Length == 0) return (TipoNombreExpo.Ignorado, null);
            if (Especiales.IsMatch(n)) return (TipoNombreExpo.GestionEspecial, null);
            if (FueraDeAlcance.IsMatch(n)) return (TipoNombreExpo.Desconocido, null);
            // Papel blanco arrugado o «tissue» sin señal de uso: puede ser una servilleta usada (NEGRA) o papel limpio /
            // bolsa plástica (no van a la negra). Medido: una bola de papel cuadriculado y una bolsa plástica se leían como
            // «tissue paper». Queda por confirmar, nunca abre sola.
            if (PapelBlancoDudoso.IsMatch(n) && !SenalUso.IsMatch(n) && !Regex.IsMatch(n, @"\b(toilet|napkins?|servillet\w*|brown|kraft|cardboard|carton|bag|box|chips?|snack\w*|wrapper)\b", RegexOptions.IgnoreCase))
                return (TipoNombreExpo.Desconocido, null);
            // Papel aluminio: solo la envoltura de mecato (chips, snack, paquete) es el material E09.
            if (Regex.IsMatch(n, @"\bfoil\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(n, @"\b(chips?|crisps?|snack\w*|candy|cracker\w*|packet|bag)\b", RegexOptions.IgnoreCase))
                return (TipoNombreExpo.Desconocido, null);
            foreach (var (codigo, patron) in Reglas)
                if (patron.IsMatch(n)) return (TipoNombreExpo.Material, PorCodigo(codigo));
            return Ignorados.IsMatch(n) ? (TipoNombreExpo.Ignorado, null) : (TipoNombreExpo.Desconocido, null);
        }
    }

    /// <summary>
    /// Política automática de permanencia abierta según la cantidad de canecas DISTINTAS necesarias (no la cantidad de
    /// residuos): 1 → 5 s, 2 → 7 s, 3 → 10 s. Las pruebas manuales de calibración usan su propio tiempo configurable.
    /// </summary>
    public static class PoliticaTiemposApertura
    {
        public const int UnaCanecaMs = 5000, DosCanecasMs = 7000, TresCanecasMs = 10000;

        public static int DuracionMs(int canecasDistintas) => canecasDistintas switch
        {
            <= 0 => 0,
            1 => UnaCanecaMs,
            2 => DosCanecasMs,
            _ => TresCanecasMs,
        };

        public static int DuracionMs(IEnumerable<CategoriaMaterial> canecas) => DuracionMs(canecas.Distinct().Count());
    }
}
