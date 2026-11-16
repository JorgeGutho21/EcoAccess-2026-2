using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Descripción concreta de lo que se ve («una manzana verde», «una cáscara de banano», «una servilleta usada») a partir
    /// del nombre libre que dio el modelo. Igual que <see cref="InterpretadorNombreObjeto"/>, el texto del modelo nunca se
    /// muestra: solo se buscan palabras de una lista cerrada y la frase en español la arma EcoAccess. Si el nombre no trae un
    /// sustantivo conocido que coincida con la clave ya decidida, no hay descripción y se usa la frase general del catálogo.
    /// La descripción no cambia el destino ni la admisión: es solo lenguaje.
    /// </summary>
    public static class DescriptorObjeto
    {
        private sealed record Sustantivo(string Clave, string Singular, string Plural, bool Femenino, string Emoji, bool Incontable = false);

        private static readonly Dictionary<string, Sustantivo> Sustantivos = new(StringComparer.Ordinal)
        {
            // Frutas y verduras enteras (whole_produce)
            ["apple"] = new("whole_produce", "manzana", "manzanas", true, "🍎"),
            ["banana"] = new("whole_produce", "banano", "bananos", false, "🍌"),
            ["orange"] = new("whole_produce", "naranja", "naranjas", true, "🍊"),
            ["tangerine"] = new("whole_produce", "mandarina", "mandarinas", true, "🍊"),
            ["mandarin"] = new("whole_produce", "mandarina", "mandarinas", true, "🍊"),
            ["mango"] = new("whole_produce", "mango", "mangos", false, "🥭"),
            ["papaya"] = new("whole_produce", "papaya", "papayas", true, "🍈"),
            ["pineapple"] = new("whole_produce", "piña", "piñas", true, "🍍"),
            ["watermelon"] = new("whole_produce", "sandía", "sandías", true, "🍉"),
            ["melon"] = new("whole_produce", "melón", "melones", false, "🍈"),
            ["avocado"] = new("whole_produce", "aguacate", "aguacates", false, "🥑"),
            ["tomato"] = new("whole_produce", "tomate", "tomates", false, "🍅"),
            ["carrot"] = new("whole_produce", "zanahoria", "zanahorias", true, "🥕"),
            ["potato"] = new("whole_produce", "papa", "papas", true, "🥔"),
            ["onion"] = new("whole_produce", "cebolla", "cebollas", true, "🧅"),
            ["lettuce"] = new("whole_produce", "lechuga", "lechugas", true, "🥬"),
            ["cabbage"] = new("whole_produce", "repollo", "repollos", false, "🥬"),
            ["lemon"] = new("whole_produce", "limón", "limones", false, "🍋"),
            ["lime"] = new("whole_produce", "limón", "limones", false, "🍋"),
            ["pear"] = new("whole_produce", "pera", "peras", true, "🍐"),
            ["grape"] = new("whole_produce", "uva", "uvas", true, "🍇"),
            ["strawberry"] = new("whole_produce", "fresa", "fresas", true, "🍓"),
            ["guava"] = new("whole_produce", "guayaba", "guayabas", true, "🍈"),
            ["cucumber"] = new("whole_produce", "pepino", "pepinos", false, "🥒"),
            ["broccoli"] = new("whole_produce", "brócoli", "brócolis", false, "🥦"),
            ["peach"] = new("whole_produce", "durazno", "duraznos", false, "🍑"),
            ["kiwi"] = new("whole_produce", "kiwi", "kiwis", false, "🥝"),

            // Comida preparada (served_food)
            ["rice"] = new("served_food", "arroz", "arroz", false, "🍚", Incontable: true),
            ["pasta"] = new("served_food", "pasta", "pasta", true, "🍝", Incontable: true),
            ["noodle"] = new("served_food", "pasta", "pasta", true, "🍜", Incontable: true),
            ["bread"] = new("served_food", "pan", "panes", false, "🍞"),
            ["pizza"] = new("served_food", "pizza", "pizzas", true, "🍕"),
            ["sandwich"] = new("served_food", "sándwich", "sándwiches", false, "🥪"),
            ["burger"] = new("served_food", "hamburguesa", "hamburguesas", true, "🍔"),
            ["soup"] = new("served_food", "sopa", "sopas", true, "🍲"),
            ["salad"] = new("served_food", "ensalada", "ensaladas", true, "🥗"),
            ["cake"] = new("served_food", "torta", "tortas", true, "🍰"),

            // Restos vegetales (plant_waste)
            ["leaf"] = new("plant_waste", "hoja", "hojas", true, "🍂"),
            ["leaves"] = new("plant_waste", "hoja", "hojas", true, "🍂"),
            ["branch"] = new("plant_waste", "rama", "ramas", true, "🌿"),
            ["twig"] = new("plant_waste", "ramita", "ramitas", true, "🌿"),

            // Aprovechables
            ["bottle"] = new("plastic_bottle", "botella plástica", "botellas plásticas", true, "🧴"),
            ["jar"] = new("glass_bottle", "frasco de vidrio", "frascos de vidrio", false, "🫙"),
            ["can"] = new("can", "lata", "latas", true, "🥫"),
            ["box"] = new("cardboard", "caja de cartón", "cajas de cartón", true, "📦"),
            ["cardboard"] = new("cardboard", "cartón", "cartones", false, "📦", Incontable: true),
            ["newspaper"] = new("paper", "periódico", "periódicos", false, "📰"),
            ["magazine"] = new("paper", "revista", "revistas", true, "📰"),
            ["notebook"] = new("paper", "cuaderno", "cuadernos", false, "📓"),
            ["envelope"] = new("paper", "sobre de papel", "sobres de papel", false, "✉️"),
            ["sheet"] = new("paper", "hoja de papel", "hojas de papel", true, "📄"),
            ["cup"] = new("plastic_container", "vaso", "vasos", false, "🥤"),
            ["tray"] = new("plastic_container", "bandeja", "bandejas", true, "🍱"),
            ["lid"] = new("plastic_container", "tapa", "tapas", true, "🧴"),
            ["bag"] = new("plastic_bag", "bolsa plástica", "bolsas plásticas", true, "🛍️"),
            ["carton"] = new("drink_carton", "caja de leche o jugo (multicapa)", "cajas de leche o jugo (multicapa)", true, "🧃"),

            // No aprovechables y sanitarios
            ["napkin"] = new("used_tissue", "servilleta", "servilletas", true, "🧻"),
            ["tissue"] = new("used_tissue", "pañuelo desechable", "pañuelos desechables", false, "🧻"),
            ["mask"] = new("face_mask", "tapabocas", "tapabocas", false, "😷"),
            ["candy"] = new("snack_wrapper", "envoltura de dulce", "envolturas de dulce", true, "🍬"),
            ["chocolate"] = new("snack_wrapper", "envoltura de chocolate", "envolturas de chocolate", true, "🍫"),
            ["chip"] = new("snack_wrapper", "paquete de papas fritas", "paquetes de papas fritas", false, "🥔"),

            // Especiales y electrónicos
            ["battery"] = new("battery", "pila", "pilas", true, "🔋"),
            ["phone"] = new("phone", "celular", "celulares", false, "📱"),
            ["smartphone"] = new("phone", "celular", "celulares", false, "📱"),
            ["laptop"] = new("laptop", "computador portátil", "computadores portátiles", false, "💻"),
            ["charger"] = new("charger", "cargador", "cargadores", false, "🔌"),
            ["cable"] = new("cable", "cable", "cables", false, "🔌"),
            ["headphone"] = new("headphones", "audífono", "audífonos", false, "🎧"),
            ["earphone"] = new("headphones", "audífono", "audífonos", false, "🎧"),
            ["pill"] = new("medicine", "pastilla", "pastillas", true, "💊"),
            ["medicine"] = new("medicine", "medicamento", "medicamentos", false, "💊"),
            ["syringe"] = new("syringe", "jeringa", "jeringas", true, "💉"),
            ["needle"] = new("syringe", "aguja", "agujas", true, "💉"),
            ["bulb"] = new("bulb", "bombillo", "bombillos", false, "💡"),
            ["aerosol"] = new("aerosol", "aerosol", "aerosoles", false, "🧯"),
            ["spray"] = new("aerosol", "aerosol", "aerosoles", false, "🧯"),
        };

        /// <summary>Adjetivos que se pueden VER: color, madurez y estado. Masculino y femenino singular.</summary>
        private static readonly Dictionary<string, (string M, string F)> Adjetivos = new(StringComparer.Ordinal)
        {
            ["green"] = ("verde", "verde"), ["red"] = ("rojo", "roja"), ["yellow"] = ("amarillo", "amarilla"),
            ["ripe"] = ("maduro", "madura"), ["rotten"] = ("descompuesto", "descompuesta"), ["moldy"] = ("con moho", "con moho"),
            ["dry"] = ("seco", "seca"), ["dried"] = ("seco", "seca"), ["crumpled"] = ("arrugado", "arrugada"),
            ["dirty"] = ("sucio", "sucia"), ["soiled"] = ("sucio", "sucia"), ["greasy"] = ("con grasa", "con grasa"),
            ["used"] = ("usado", "usada"), ["empty"] = ("vacío", "vacía"), ["broken"] = ("roto", "rota"),
            ["wet"] = ("mojado", "mojada"), ["clean"] = ("limpio", "limpia"), ["crushed"] = ("aplastado", "aplastada"),
        };

        // Un color solo describe frutas, verduras y hojas; en un envase el color no aporta y puede confundir.
        private static readonly HashSet<string> Colores = new(StringComparer.Ordinal) { "green", "red", "yellow" };
        private static readonly HashSet<string> ConColor = new(StringComparer.Ordinal) { "whole_produce", "plant_waste" };
        private static readonly string[] Cascaras = { "peel", "peels", "skin", "skins", "rind", "peeling", "peelings" };
        private static readonly string[] PalabrasResto = { "leftover", "leftovers", "scrap", "scraps", "remains", "core" };

        /// <summary>
        /// La descripción concreta, o null si el nombre no trae un sustantivo conocido de esa misma clave. Así una «orange
        /// plastic bottle» nunca se describe como «una naranja»: la clave decidida (botella) manda.
        /// </summary>
        public static DescripcionObjeto? Describir(string? nombreLibre, string? clave)
        {
            if (clave is null || nombreLibre is null) return null;
            string[] tokens = Tokens(nombreLibre);
            if (tokens.Length == 0) return null;

            if (clave == "food_scraps") return Restos(tokens);
            if (clave == "glass_bottle" && tokens.Any(t => t is "bottle" or "bottles"))
                return new DescripcionObjeto(Plural(tokens, "bottle") ? "botellas de vidrio" : "una botella de vidrio", "🍾");

            for (int i = tokens.Length - 1; i >= 0; i--)
            {
                (Sustantivo? s, bool plural) = Buscar(tokens[i]);
                if (s is null || s.Clave != clave) continue;
                string adjetivos = AdjetivosDe(tokens, s, plural);
                string complemento = Complemento(tokens, s);
                string nucleo = (plural ? s.Plural : s.Singular) + complemento + adjetivos;
                string articulo = s.Incontable || plural ? "" : s.Femenino ? "una " : "un ";
                return new DescripcionObjeto(articulo + nucleo, s.Emoji);
            }
            return null;
        }

        private static DescripcionObjeto? Restos(string[] tokens)
        {
            if (tokens.Contains("coffee")) return new DescripcionObjeto("residuos de café", "☕");
            if (tokens.Any(t => t is "eggshell" or "eggshells") || (tokens.Any(t => t is "egg" or "eggs") && tokens.Any(t => t is "shell" or "shells")))
                return new DescripcionObjeto("cáscaras de huevo", "🥚");
            Sustantivo? alimento = tokens.Select(t => Buscar(t).S).FirstOrDefault(s => s is { Clave: "whole_produce" or "served_food" });
            if (tokens.Any(Cascaras.Contains))
            {
                bool varias = tokens.Any(t => t is "peels" or "skins" or "peelings");
                string cascara = varias ? "cáscaras" : "una cáscara";
                return alimento is null ? new DescripcionObjeto(varias ? "cáscaras" : "una cáscara", "🍃")
                    : new DescripcionObjeto($"{cascara} de {alimento.Singular}", alimento.Emoji);
            }
            if (tokens.Any(PalabrasResto.Contains) && alimento is not null) return new DescripcionObjeto($"restos de {alimento.Singular}", alimento.Emoji);
            return tokens.Any(t => t is "rotten" or "moldy") && alimento is not null
                ? new DescripcionObjeto($"{(alimento.Femenino ? "una" : "un")} {alimento.Singular} {(alimento.Femenino ? "descompuesta" : "descompuesto")}", alimento.Emoji)
                : null;
        }

        /// <summary>Lo que precisa el objeto: «botella plástica de agua», «lata de bebida», «hojas secas».</summary>
        private static string Complemento(string[] tokens, Sustantivo s) => s.Clave switch
        {
            "plastic_bottle" when tokens.Contains("water") => " de agua",
            "plastic_bottle" when tokens.Any(t => t is "soda" or "cola") => " de gaseosa",
            "plastic_bottle" when tokens.Contains("juice") => " de jugo",
            "can" when tokens.Any(t => t is "soda" or "beer" or "drink" or "cola") => " de bebida",
            "can" when tokens.Any(t => t is "food" or "tuna" or "beans" or "canned") => " de alimentos",
            "plastic_container" when tokens.Contains("yogurt") => " de yogur",
            "plastic_container" when tokens.Contains("plastic") => " plástico",
            "used_tissue" when tokens.Contains("toilet") => " de papel higiénico",
            _ => "",
        };

        private static string AdjetivosDe(string[] tokens, Sustantivo s, bool plural)
        {
            var elegidos = new List<string>(2);
            string? color = tokens.FirstOrDefault(t => Colores.Contains(t));
            if (color is not null && ConColor.Contains(s.Clave)) elegidos.Add(Concordar(Adjetivos[color], s.Femenino, plural));
            string? estado = tokens.FirstOrDefault(t => Adjetivos.ContainsKey(t) && !Colores.Contains(t));
            if (estado is not null) elegidos.Add(Concordar(Adjetivos[estado], s.Femenino, plural));
            return elegidos.Count == 0 ? "" : " " + string.Join(" y ", elegidos);
        }

        private static string Concordar((string M, string F) adjetivo, bool femenino, bool plural)
        {
            string forma = femenino ? adjetivo.F : adjetivo.M;
            if (!plural || forma.StartsWith("con ", StringComparison.Ordinal)) return forma;
            return forma.EndsWith("e", StringComparison.Ordinal) || forma.EndsWith("o", StringComparison.Ordinal)
                || forma.EndsWith("a", StringComparison.Ordinal) ? forma + "s" : forma + "es";
        }

        private static bool Plural(string[] tokens, string singular) => tokens.Contains(singular + "s") || tokens.Contains(singular + "es");

        private static (Sustantivo? S, bool Plural) Buscar(string palabra)
        {
            if (Sustantivos.TryGetValue(palabra, out Sustantivo? s)) return (s, palabra == "leaves");
            foreach (string singular in Singulares(palabra))
                if (Sustantivos.TryGetValue(singular, out s)) return (s, !s.Incontable);
            return (null, false);
        }

        private static IEnumerable<string> Singulares(string palabra)
        {
            if (palabra.EndsWith("ies", StringComparison.Ordinal)) yield return palabra.Substring(0, palabra.Length - 3) + "y";
            if (palabra.EndsWith("es", StringComparison.Ordinal)) yield return palabra.Substring(0, palabra.Length - 2);
            if (palabra.EndsWith("s", StringComparison.Ordinal) && !palabra.EndsWith("ss", StringComparison.Ordinal)) yield return palabra.Substring(0, palabra.Length - 1);
        }

        private static string[] Tokens(string nombre)
        {
            string recortado = nombre.Length > 120 ? nombre.Substring(0, 120) : nombre;
            return new string(recortado.ToLowerInvariant().Select(c => char.IsLetter(c) ? c : ' ').ToArray())
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
