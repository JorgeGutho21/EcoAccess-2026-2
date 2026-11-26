using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35.Expo
{
    /// <summary>
    /// Uno de los materiales del Modo ExpoSoftware. Reutiliza el código del catálogo de 35 (<see cref="Codigo35"/>) y
    /// un código de material de Oracle existente (<see cref="CodigoOracle"/>); no se inventan códigos nuevos en la base.
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
    /// Catálogo visual activo de ExpoSoftware: 13 residuos reales fotografiados en la estación (5 verdes, 5 blancos,
    /// 3 negros; esponja y caja de cartón añadidas el 26/11/2026). El modelo elige una etiqueta cerrada (<see cref="MaterialExpo.Etiqueta"/>); si devuelve un nombre libre,
    /// las reglas deterministas lo traducen. Lo que no encaja queda como desconocido o de gestión especial: nunca se fuerza
    /// a una caneca. La clase conserva su nombre histórico (antes 9 materiales) para no romper las integraciones.
    /// </summary>
    public static class CatalogoExpo9
    {
        public const string Version = "2026.11.26-expo13";

        public static IReadOnlyList<MaterialExpo> Todos { get; } = new[]
        {
            new MaterialExpo("M01", "Papa o media papa", "papa", CategoriaMaterial.OrganicoAprovechable, "V03", "ORGANICO", "potato", "🥔"),
            new MaterialExpo("M02", "Limón entero o partido", "limón", CategoriaMaterial.OrganicoAprovechable, "V05", "ORGANICO", "lime", "🍋"),
            new MaterialExpo("M03", "Manzana verde entera o partida", "manzana verde", CategoriaMaterial.OrganicoAprovechable, null, "ORGANICO", "green_apple", "🍏"),
            new MaterialExpo("M04", "Lulo entero o partido", "lulo", CategoriaMaterial.OrganicoAprovechable, null, "ORGANICO", "lulo", "🟠"),
            new MaterialExpo("M05", "Cáscara de banano", "cáscara de banano", CategoriaMaterial.OrganicoAprovechable, "V01", "ORGANICO", "banana_peel", "🍌"),
            new MaterialExpo("M06", "Papel de aluminio limpio", "papel aluminio", CategoriaMaterial.Aprovechable, null, "LATA_ALUMINIO", "aluminum_foil", "🥈"),
            new MaterialExpo("M07", "Envase de vidrio rosado de sérum, vacío y limpio", "envase de vidrio de sérum", CategoriaMaterial.Aprovechable, "B07", "VIDRIO", "serum_bottle", "🧪"),
            new MaterialExpo("M08", "Papel de cuaderno cuadriculado o rayado, arrugado", "papel de cuaderno", CategoriaMaterial.Aprovechable, "B08", "PAPEL", "notebook_paper", "📄"),
            new MaterialExpo("M09", "Bolsa de papel kraft o cartón marrón arrugado", "papel kraft", CategoriaMaterial.Aprovechable, "B09", "CARTON", "kraft_paper", "📦"),
            new MaterialExpo("M10", "Envase de pegamento instantáneo vacío (Gota Mágica, Astro Glue)", "envase de pegamento", CategoriaMaterial.NoAprovechable, null, "OTRO_RESIDUO", "glue_bottle", "🧴"),
            new MaterialExpo("M11", "Servilleta usada", "servilleta usada", CategoriaMaterial.NoAprovechable, "N01", "SERVILLETA_USADA", "used_napkin", "🧻"),
            new MaterialExpo("M12", "Esponja de cocina usada", "esponja", CategoriaMaterial.NoAprovechable, null, "OTRO_RESIDUO", "sponge", "🧽"),
            new MaterialExpo("M13", "Caja de cartón vacía (medicamento o producto)", "caja de cartón", CategoriaMaterial.Aprovechable, null, "CARTON", "paper_box", "📦"),
        };

        /// <summary>Catálogo anterior de 9 (E01–E09): solo para leer el historial ya guardado en Oracle.</summary>
        public static IReadOnlyList<MaterialExpo> Historicos { get; } = new[]
        {
            new MaterialExpo("E01", "Media manzana", "media manzana", CategoriaMaterial.OrganicoAprovechable, null, "ORGANICO", "apple", "🍎"),
            new MaterialExpo("E02", "Cáscara de banano", "cáscara de banano", CategoriaMaterial.OrganicoAprovechable, "V01", "ORGANICO", "banana_peel", "🍌"),
            new MaterialExpo("E03", "Papa o media papa", "papa", CategoriaMaterial.OrganicoAprovechable, "V03", "ORGANICO", "potato", "🥔"),
            new MaterialExpo("E04", "Papel kraft o cartón", "papel kraft o cartón", CategoriaMaterial.Aprovechable, "B09", "CARTON", "kraft_cardboard", "📦"),
            new MaterialExpo("E05", "Botella plástica", "botella plástica", CategoriaMaterial.Aprovechable, "B01", "PET", "plastic_bottle", "🧴"),
            new MaterialExpo("E06", "Frasco de vidrio", "frasco de vidrio", CategoriaMaterial.Aprovechable, "B06", "VIDRIO", "glass_jar", "🫙"),
            new MaterialExpo("E07", "Papel higiénico usado", "papel higiénico usado", CategoriaMaterial.NoAprovechable, "N02", "SERVILLETA_USADA", "toilet_paper", "🧻"),
            new MaterialExpo("E08", "Servilleta usada", "servilleta usada", CategoriaMaterial.NoAprovechable, "N01", "SERVILLETA_USADA", "used_napkin", "🧽"),
            new MaterialExpo("E09", "Envoltura metalizada", "envoltura metalizada", CategoriaMaterial.NoAprovechable, "N08", "PLASTICO_SIN_ID", "snack_wrapper", "🍫"),
        };

        public static MaterialExpo? PorCodigo(string? codigo) =>
            Todos.FirstOrDefault(m => string.Equals(m.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
            ?? Historicos.FirstOrDefault(m => string.Equals(m.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

        public static MaterialExpo? PorEtiqueta(string? etiqueta) =>
            Todos.FirstOrDefault(m => string.Equals(m.Etiqueta, etiqueta?.Trim(), StringComparison.OrdinalIgnoreCase));

        // Partes de la escena que no son residuos: la mano, la mesa, la pared, la pantalla del computador.
        private static readonly Regex Ignorados = new(@"\b(hands?|fingers?|palm|thumb|gloves?|rings?|jewel(?:le)?ry|bracelets?|wristwatch|arms?|wrist|robot\w*|mascot|table|desk|tabletop|surface|background|wall|floor|shadow|cloth|tablecloth|logo|sticker|label|helmet|earpiece|leaf logo|laptop|computer|screen|monitor|keyboard)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Gestión especial o peligro: nunca abre una caneca por foto.
        private static readonly Regex Especiales = new(@"\b(batter(y|ies)|pill|pills|blister|medicine|medication|syringe|needle|cable|charger|wire|phone|electronic\w*|broken|shard|knife|razor|blade|lighter|aerosol|spray|chemical|solvent|paint|bleach|person|people|face|child|animal|dog|cat|pila|jeringa|medicamento)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Nombres libres (respaldo si el modelo no usa la etiqueta). Orden: lo más específico primero.
        private static readonly (string Codigo, Regex Patron)[] Reglas =
        {
            ("M12", new Regex(@"\b(sponges?|esponj\w*|scouring pad|scrub(ber|bing)? pad|dish ?scrubber)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M13", new Regex(@"\b(box|boxes|caja|cajita|packag(e|ing)|carton box|cardboard box)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M10", new Regex(@"\b(glue|super ?glue|adhesive|pegamento|gota m[aá]gica|astro)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M07", new Regex(@"\b(serum|dropper|cosmetic bottle|pink (glass )?bottle|s[eé]rum|gotero)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M06", new Regex(@"\b(alumin(i)?um foil|tin foil|foil ball|foil-wrapped|silver foil|papel aluminio)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M11", new Regex(@"\b(napkins?|tissues?|paper towel|kleenex|toilet paper|servillet\w*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M08", new Regex(@"\b(notebook|graph paper|grid paper|lined paper|ruled paper|cuaderno)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M09", new Regex(@"\b(kraft|cardboard|brown paper|paper bag|carton|cart[oó]n)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M04", new Regex(@"\b(lulo|naranjilla|orange)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M03", new Regex(@"\b(apples?|manzanas?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M02", new Regex(@"\b(limes?|lemons?|lim[oó]n\w*)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M05", new Regex(@"\b(banana|bananas|banano|guineo|plantain)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("M01", new Regex(@"\b(potato|potatoes|papa|papas|patata)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        };

        // Parecidos que NO son de los 11 (envoltura metalizada de mecato frente a papel aluminio limpio, botella plástica,
        // bolsa plástica, envase de champú, lata, tapabocas, esponja…): quedan como desconocidos y se pide confirmación.
        private static readonly Regex FueraDeAlcance = new(@"\b(chips?|crisps?|snack\w*|wrappers?|candy|mylar|juice box|milk|tetra|pizza|greasy|cans?|soda can|plastic bottle|water bottle|plastic bag|grocery bag|shopping bag|trash bag|garbage bag|polyethylene|cellophane|shampoo|conditioner|lotion|soap|detergent|face mask|surgical mask|mask|wet wipes?|towelettes?|eggshells?|cup|styrofoam|foam)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex PapelSinDetalle = new(@"\bpaper\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex PapelConDetalle = new(@"\b(notebook|graph|grid|lined|ruled|napkin|tissue|toilet|kraft|brown|bag|foil|towel)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Nombre libre compatible con cada etiqueta. El catálogo cerrado tiende a forzar el parecido más cercano (medido:
        // tomate → potato, cebolla → potato, esponja → aluminum_foil, carta → used_napkin); el nombre libre lo delata.
        private static readonly Dictionary<string, Regex> Compatibles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["potato"] = new(@"\b(potato\w*|papas?|patata|tuber)\b", RegexOptions.IgnoreCase),
            ["lime"] = new(@"\b(limes?|lemons?|citrus|lim[oó]n)\b", RegexOptions.IgnoreCase),
            ["green_apple"] = new(@"\b(apples?|manzanas?)\b", RegexOptions.IgnoreCase),
            ["lulo"] = new(@"\b(lulo|naranjilla|oranges?|fruit)\b", RegexOptions.IgnoreCase),
            ["banana_peel"] = new(@"\b(banana\w*|peel|plantain|guineo)\b", RegexOptions.IgnoreCase),
            ["aluminum_foil"] = new(@"(foil|alumin|silver|tin)", RegexOptions.IgnoreCase),
            ["serum_bottle"] = new(@"\b(bottle|dropper|serum|vial|perfume|cosmetic|glass)\b", RegexOptions.IgnoreCase),
            ["notebook_paper"] = new(@"(paper|napkin|tissue|kraft|cardboard|carton)", RegexOptions.IgnoreCase),
            // Servilleta: solo con evidencia de papel suave en el nombre libre (nunca «plastic», «bag», «wrapper», «wipe»).
            ["used_napkin"] = new(@"\b(napkins?|tissues?|paper towels?|kleenex|serviettes?|servillet\w*)\b", RegexOptions.IgnoreCase),
            ["kraft_paper"] = new(@"(paper|kraft|cardboard|carton|bag)", RegexOptions.IgnoreCase),
            ["glue_bottle"] = new(@"(glue|bottle|adhesive|nozzle|tube)", RegexOptions.IgnoreCase),
            ["sponge"] = new(@"\b(sponges?|esponj\w*|scour\w*|scrub\w*)\b", RegexOptions.IgnoreCase),
            ["paper_box"] = new(@"\b(box\w*|caja\w*|carton|cardboard|packag\w*|packet)\b", RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// Combina el nombre libre y la etiqueta cerrada del mismo objeto. Gestión especial o parte de la escena por el
        /// nombre manda; una etiqueta solo se acepta si el nombre libre es compatible. Si no lo es, o la etiqueta es «other»,
        /// el objeto queda por confirmar: nunca se abre una caneca por un parecido forzado.
        /// </summary>
        public static (TipoNombreExpo Tipo, MaterialExpo? Material) Interpretar(string? nombre, string? etiqueta)
        {
            nombre = NormalizarNombre(nombre);
            if (string.IsNullOrWhiteSpace(nombre)) return Interpretar(etiqueta);
            var porEtiquetaPapel = PorEtiqueta(etiqueta);
            // Papel (cuaderno, servilleta) con un nombre libre de plástico o envoltura: no es papel. Por confirmar.
            if (porEtiquetaPapel is { Codigo: "M08" or "M11" } && NoEsPapel.IsMatch(nombre!)) return (TipoNombreExpo.Desconocido, null);
            if (string.IsNullOrWhiteSpace(etiqueta)) return Interpretar(nombre);
            // Etiqueta libre (ni una de las 13 ni «other»): el modelo describió el objeto en lugar de elegir. Se interpreta junto
            // con el nombre (medido el 26/11: caja de medicamento → nombre «Xialox», etiqueta «medicine box»).
            if (PorEtiqueta(etiqueta) is null && !etiqueta!.Trim().Equals("other", StringComparison.OrdinalIgnoreCase)) return Interpretar(nombre + " " + NormalizarNombre(etiqueta));
            var porNombre = Interpretar(nombre);
            if (porNombre.Tipo is TipoNombreExpo.GestionEspecial or TipoNombreExpo.Ignorado) return porNombre;
            if (FueraDeAlcance.IsMatch(nombre)) return (TipoNombreExpo.Desconocido, null);
            var porEtiqueta = Interpretar(etiqueta);
            if (porEtiqueta.Tipo == TipoNombreExpo.GestionEspecial) return porEtiqueta;
            if (porEtiqueta.Tipo != TipoNombreExpo.Material) return (TipoNombreExpo.Desconocido, null);
            var m = porEtiqueta.Material!;
            bool compatible = (porNombre.Tipo == TipoNombreExpo.Material && porNombre.Material!.Caneca == m.Caneca)
                || (Compatibles.TryGetValue(m.Etiqueta, out var patron) && patron.IsMatch(nombre!));
            return compatible ? porEtiqueta : (TipoNombreExpo.Desconocido, null);
        }

        private static readonly Regex CajaVacia = new(@"\b(box|boxes|caja|cajita|carton box|cardboard box|package|packaging)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        // Lo que sí es de gestión especial aunque venga en caja: pastillas, blíster, pilas, jeringas, cables o aparatos.
        private static readonly Regex ContenidoEspecial = new(@"\b(pills?|tablets?|capsules?|blisters?|batter(y|ies)|syringes?|needles?|cables?|chargers?|phones?|electronic\w*|pastillas?|pilas?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Nombres libres que delatan plástico o envoltura aunque la etiqueta sea de papel.
        private static readonly Regex NoEsPapel = new(@"\b(plastic|bags?|wrappers?|wrap|film|wipes?|nylon|polythene)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Cítricos que el modelo no separa del lulo (medido: el lulo real se nombra «orange» y la cáscara de naranja
        // externa también; una pregunta sí/no de contraste no los distingue). Nunca abren solos: se piden confirmar.
        private static readonly Regex Naranjas = new(@"\b(oranges?|mandarins?|tangerines?|clementines?|satsumas?|citrus peel|naranjas?|mandarinas?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>El modelo a veces une palabras con guion bajo («crumpled_tissue», «plastic_bag»): se separan para las reglas.</summary>
        public static string? NormalizarNombre(string? nombre) => nombre?.Replace('_', ' ').Trim();

        /// <summary>
        /// Motivo por el que un material ya aceptado NO debe abrir solo (se pide confirmación) o null si no hay duda.
        /// Hoy: un cítrico (naranja, mandarina) en el nombre libre de cualquier material verde, porque el lulo y la naranja
        /// son indistinguibles para el modelo. Y la servilleta usada SIEMPRE: medido en la evaluación consolidada del
        /// 2026-11-25, el modelo nombra «tissue» y etiqueta «used_napkin» a una bolsa plástica arrugada, a papel higiénico,
        /// a una toallita húmeda y a papel blanco liso; ningún nombre ni pregunta sí/no probada los separa.
        /// </summary>
        public static string? MotivoDuda(string? nombre, MaterialExpo material)
        {
            if (material.Codigo == "M11") return "¿servilleta usada? (se confunde con bolsa plástica y papel liso)";
            string n = NormalizarNombre(nombre) ?? "";
            // Excepción medida (25/11/2026): el lulo real llega como nombre «orange» con etiqueta «lulo» (2/2 fotos reales) y
            // nunca abría solo. Ese caso exacto abre la verde; cáscaras, mandarinas u «orange» con otra etiqueta se confirman.
            if (material.Codigo == "M04" && Regex.IsMatch(n, @"^oranges?$", RegexOptions.IgnoreCase)) return null;
            if (material.Caneca == CategoriaMaterial.OrganicoAprovechable && Naranjas.IsMatch(n) && !Regex.IsMatch(n, @"\b(lulo|naranjilla)\b", RegexOptions.IgnoreCase))
                return "¿lulo o naranja/mandarina?";
            return null;
        }

        /// <summary>Traduce la etiqueta o el nombre del modelo a uno de los 13 materiales, o lo marca como ignorado, desconocido o especial.</summary>
        public static (TipoNombreExpo Tipo, MaterialExpo? Material) Interpretar(string? nombre)
        {
            string n = (nombre ?? "").Trim();
            if (n.Length == 0) return (TipoNombreExpo.Ignorado, null);
            if (n.Equals("other", StringComparison.OrdinalIgnoreCase)) return (TipoNombreExpo.Desconocido, null);
            if (PorEtiqueta(n) is { } exacta) return (TipoNombreExpo.Material, exacta);
            n = n.Replace('_', ' ');
            // Caja vacía de medicamento o producto («medicine box», «Xialox box»): es cartón (blanca), no el medicamento.
            if (CajaVacia.IsMatch(n) && !ContenidoEspecial.IsMatch(n) && !FueraDeAlcance.IsMatch(n)) return (TipoNombreExpo.Material, PorCodigo("M13"));
            if (Especiales.IsMatch(n)) return (TipoNombreExpo.GestionEspecial, null);
            if (FueraDeAlcance.IsMatch(n)) return (TipoNombreExpo.Desconocido, null);
            // «crumpled paper» sin más detalle: puede ser cuaderno (BLANCA) o servilleta usada (NEGRA). Por confirmar.
            if (PapelSinDetalle.IsMatch(n) && !PapelConDetalle.IsMatch(n)) return (TipoNombreExpo.Desconocido, null);
            foreach (var (codigo, patron) in Reglas)
                if (patron.IsMatch(n)) return (TipoNombreExpo.Material, PorCodigo(codigo));
            return Ignorados.IsMatch(n) ? (TipoNombreExpo.Ignorado, null) : (TipoNombreExpo.Desconocido, null);
        }
    }

    /// <summary>
    /// Política automática de permanencia abierta según la cantidad de canecas DISTINTAS necesarias (no la cantidad de
    /// residuos): 1 → 7 s, 2 → 14 s, 3 → 21 s. Es la fuente de la regla en C#; el panel, la pasarela y la web la replican
    /// con los mismos valores (prueba de coherencia). Las pruebas de calibración usan su propio tiempo configurable.
    /// </summary>
    public static class PoliticaTiemposApertura
    {
        public const int UnaCanecaMs = 7000, DosCanecasMs = 14000, TresCanecasMs = 21000;

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
