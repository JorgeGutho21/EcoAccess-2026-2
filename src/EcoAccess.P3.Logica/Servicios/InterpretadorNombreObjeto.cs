using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Traduce el nombre libre que da un motor visual («Plastic soda bottle») a una clave de <see cref="CatalogoObjetos"/>.
    /// El texto del modelo no se muestra a nadie ni llega a Oracle: solo se buscan en él palabras conocidas, y lo que no
    /// coincide queda sin clave. Un nombre no abre la caneca: un residuo además necesita otra lectura que lo corrobore.
    /// </summary>
    public static class InterpretadorNombreObjeto
    {
        private static readonly string[] Cortes = { "on", "in", "with", "near", "at", "under", "from", "by", "inside", "against", "beside", "next", "over", "behind", "and" };
        private static readonly string[] Sosteniendo = { "holding", "drinking", "carrying", "showing" };
        private static readonly string[] Restos = { "peel", "peels", "peeled", "peeling", "peelings", "core", "scrap", "scraps", "leftover", "leftovers", "compost", "rotten" };
        private static readonly string[] BotellaVidrio = { "glass", "wine", "beer", "champagne", "liquor", "whiskey" };
        // «Canned fruit», «canned food label»: el envase metálico es el residuo, no la comida de la etiqueta.
        private static readonly string[] Enlatado = { "can", "canned", "tin", "tinned" };
        // «Foil wrapper», «aluminum foil tray»: el aluminio es metal, no un empaque multicapa ni un recipiente plástico.
        private static readonly string[] Aluminio = { "foil", "aluminum", "aluminium" };
        private static readonly string[] BotellaPlastico = { "plastic", "water", "soda", "pet", "cola", "juice", "soft" };

        private static readonly Dictionary<string, string> Compuestos = new(StringComparer.Ordinal)
        {
            ["teddy bear"] = "toy", ["computer mouse"] = "computer_mouse", ["power adapter"] = "charger", ["ac adapter"] = "charger", ["circuit board"] = "electronic_other",
            ["power bank"] = "electronic_other", ["sanitary pad"] = "diaper_or_pad", ["toilet paper"] = "used_tissue", ["paper towel"] = "used_tissue", ["milk carton"] = "drink_carton",
            ["juice carton"] = "drink_carton", ["juice box"] = "drink_carton", ["tetra pak"] = "drink_carton", ["washing machine"] = "appliance", ["coffee table"] = "table",
            ["light bulb"] = "bulb", ["plastic bag"] = "plastic_bag", ["shopping bag"] = "plastic_bag", ["blank background"] = CatalogoObjetos.NoDistinguible,
            ["white background"] = CatalogoObjetos.NoDistinguible, ["black background"] = CatalogoObjetos.NoDistinguible, ["potted tree"] = "plant", ["bonsai tree"] = "plant", ["tablet computer"] = "tablet", ["razor blade"] = "syringe",
            // Evaluación de productos de Colombia (10/11/2026): el modelo nombraba bien y el intérprete fallaba.
            // La caja de huevos es cartón moldeado, no un envase multicapa; una bolsa de papas es un empaque metalizado.
            ["egg carton"] = "cardboard", ["egg tray"] = "cardboard", ["egg box"] = "cardboard",
            ["chip bag"] = "snack_wrapper", ["chips bag"] = "snack_wrapper", ["crisps bag"] = "snack_wrapper", ["crisp bag"] = "snack_wrapper", ["snack bag"] = "snack_wrapper", ["candy bag"] = "snack_wrapper",
        };

        // «Broken eggshell», «Egg shell fragment»: la cáscara es un resto orgánico, no comida servida.
        private static readonly string[] Cascara = { "eggshell", "eggshells" };
        private static readonly string[] Concha = { "shell", "shells" };

        private static readonly Dictionary<string, string> Palabras = Construir(new (string Clave, string Lista)[]
        {
            ("dog", "dog puppy hound retriever labrador collie"),
            ("cat", "cat kitten"),
            ("bird", "bird parrot pigeon chicken hen rooster duck goose sparrow poultry"),
            ("horse", "horse cow calf sheep goat pig donkey pony bull"),
            ("other_animal", "animal bear lion tiger elephant monkey rabbit squirrel fish snake lizard frog deer insect butterfly bee turtle"),
            ("tree", "tree palm"),
            ("plant", "plant shrub bush fern cactus succulent houseplant bonsai"),
            ("flower", "flower daffodil rose tulip daisy orchid sunflower blossom lily"),
            ("grass", "grass lawn"),
            ("plant_waste", "branch twig leaf leaves pruning stump"),
            ("people", "person people man woman boy girl child kid student worker player group crowd protester adult baby family team audience portrait face"),
            ("computer_mouse", "mouse"),
            ("keyboard", "keyboard"),
            ("laptop", "laptop computer pc"),
            ("phone", "phone smartphone cellphone iphone"),
            ("tablet", "tablet ipad"),
            ("charger", "charger adapter plug"),
            ("cable", "cable wire cord"),
            ("headphones", "headphone earphone earbud headset"),
            ("monitor", "monitor television tv screen"),
            ("printer", "printer copier"),
            ("electronic_other", "router camera speaker radio console remote calculator circuit motherboard electronic"),
            ("door", "door doorway gate"),
            ("window", "window"),
            ("chair", "chair stool armchair"),
            ("table", "table desk"),
            ("furniture", "sofa couch bed shelf cabinet wardrobe bookcase dresser furniture bench mattress"),
            ("appliance", "refrigerator fridge freezer washer dryer oven microwave dishwasher stove appliance blender toaster"),
            ("toy", "toy doll lego plush"),
            ("shoes", "shoe sneaker boot sandal slipper heel"),
            ("clothes", "shirt tshirt pant jeans trouser dress jacket coat robe clothes clothing sweater hoodie sock skirt garment uniform"),
            ("tool", "hammer screwdriver wrench pliers saw drill tool mallet spanner"),
            ("served_food", "plate meal dish food steak rice salad pasta pizza burger sandwich soup bread cake meat vegetable fruit bowl noodle taco egg banana apple orange breakfast lunch dinner dessert waffle tomato"),
            ("plastic_bottle", "bottle"),
            ("glass_bottle", "jar glass"),
            ("can", "can tin canned tinned"),
            ("plastic_container", "cup tub container bucket tray lid yogurt custard"),
            ("plastic_bag", "bag film"),
            ("drink_carton", "carton tetra"),
            ("cardboard", "box cardboard"),
            ("paper", "paper document newspaper magazine sheet page text writing letter envelope"),
            ("metal_item", "metal aluminum aluminium foil"),
            ("snack_wrapper", "wrapper packet package chip crisp candy snack chocolate"),
            ("condom", "condom"),
            ("diaper_or_pad", "diaper nappy pad tampon bandage"),
            ("used_tissue", "tissue napkin"),
            ("gloves", "glove"),
            ("battery", "battery"),
            ("medicine", "pill blister medicine capsule"),
            ("bulb", "bulb"),
            ("syringe", "syringe needle razor"),
            (CatalogoObjetos.VariosObjetos, "pile heap trash garbage litter rubbish waste debris dump"),
        });

        private static Dictionary<string, string> Construir((string Clave, string Lista)[] grupos)
        {
            var palabras = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string clave, string lista) in grupos)
                foreach (string palabra in lista.Split(' ')) palabras[palabra] = clave;
            return palabras;
        }

        /// <summary>Clave del objeto nombrado, o null si el texto no menciona nada conocido.</summary>
        /// <param name="materialIncierto">Nombró una botella sin decir de qué: la otra lectura debe resolver si es plástico o vidrio.</param>
        public static string? Clave(string? nombre, out bool materialIncierto)
        {
            materialIncierto = false;
            string[] tokens = Tokens(nombre);
            if (tokens.Length == 0) return null;
            if (tokens.Any(Restos.Contains) && tokens.Any(t => Buscar(t) == "served_food" || Restos.Contains(t))) return "food_scraps";
            if (tokens.Any(Cascara.Contains) || (tokens.Any(t => t is "egg" or "eggs") && tokens.Any(Concha.Contains))) return "food_scraps";

            // Quien sostiene o bebe de un envase: importa el envase, no la persona.
            int accion = Array.FindIndex(tokens, Sosteniendo.Contains);
            if (accion >= 0)
            {
                if (tokens[accion] == "drinking" && tokens.Skip(accion + 1).Contains("water")) { materialIncierto = false; return "plastic_bottle"; }
                string? sostenido = BuscarEnFrase(tokens.Skip(accion + 1).ToArray());
                if (sostenido is not null && CatalogoObjetos.Buscar(sostenido)?.CodigoMaterial is not null) return Afinar(sostenido, tokens, out materialIncierto);
            }

            foreach (string[] tramo in Tramos(tokens))
            {
                int de = Array.IndexOf(tramo, "of");
                string? clave = de >= 0
                    ? BuscarEnFrase(tramo.Skip(de + 1).ToArray()) ?? BuscarEnFrase(tramo.Take(de).ToArray())
                    : BuscarEnFrase(tramo);
                if (clave is not null) return Afinar(clave, tokens, out materialIncierto);
            }
            return null;
        }

        private static string Afinar(string clave, string[] tokens, out bool materialIncierto)
        {
            materialIncierto = false;
            if (clave == "paper" && tokens.Contains("cardboard")) return "cardboard";
            if (clave == "served_food" && tokens.Any(Enlatado.Contains)) return "can";
            if (clave is "snack_wrapper" or "plastic_container" && tokens.Any(Aluminio.Contains)) return "metal_item";
            // «Cardboard tray»: el material nombrado gana sobre la forma.
            if (clave == "plastic_container" && tokens.Contains("cardboard")) return "cardboard";
            // «Battery pile heap», «Battery box»: se ven pilas; el montón o la caja no cambian que sean residuo especial.
            if (clave is CatalogoObjetos.VariosObjetos or "cardboard" && tokens.Any(t => Buscar(t) == "battery")) return "battery";
            if (clave != "plastic_bottle") return clave;
            if (tokens.Any(BotellaVidrio.Contains)) return "glass_bottle";
            materialIncierto = !tokens.Any(BotellaPlastico.Contains);
            return clave;
        }

        private static string[] Tokens(string? nombre)
        {
            if (nombre is null) return Array.Empty<string>();
            string recortado = nombre.Length > 120 ? nombre.Substring(0, 120) : nombre;
            var limpio = new string(recortado.ToLowerInvariant().Select(c => char.IsLetter(c) ? c : ' ').ToArray());
            return limpio.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        }

        private static IEnumerable<string[]> Tramos(string[] tokens)
        {
            var actual = new List<string>();
            foreach (string token in tokens)
            {
                if (Cortes.Contains(token) || Sosteniendo.Contains(token))
                {
                    if (actual.Count > 0) yield return actual.ToArray();
                    actual.Clear();
                }
                else actual.Add(token);
            }
            if (actual.Count > 0) yield return actual.ToArray();
        }

        /// <summary>En inglés el sustantivo principal va al final de la frase: «Toy car», «Soda can», «Computer monitor».</summary>
        private static string? BuscarEnFrase(string[] frase)
        {
            for (int i = frase.Length - 1; i >= 0; i--)
            {
                if (i > 0 && Compuestos.TryGetValue(frase[i - 1] + " " + Singular(frase[i]), out string? compuesto)) return compuesto;
                if (Buscar(frase[i]) is { } clave) return clave;
            }
            return null;
        }

        private static string? Buscar(string palabra)
        {
            if (Palabras.TryGetValue(palabra, out string? clave)) return clave;
            string singular = Singular(palabra);
            return Palabras.TryGetValue(singular, out clave) ? clave : null;
        }

        private static string Singular(string palabra)
        {
            if (palabra == "leaves" || palabra == "clothes") return palabra;
            if (palabra.EndsWith("ches", StringComparison.Ordinal) || palabra.EndsWith("xes", StringComparison.Ordinal) || palabra.EndsWith("shes", StringComparison.Ordinal))
                return palabra.Substring(0, palabra.Length - 2);
            if (palabra.EndsWith("ies", StringComparison.Ordinal)) return palabra.Substring(0, palabra.Length - 3) + "y";
            return palabra.EndsWith("s", StringComparison.Ordinal) && !palabra.EndsWith("ss", StringComparison.Ordinal) ? palabra.Substring(0, palabra.Length - 1) : palabra;
        }

        /// <summary>Objeto que corresponde a una lectura por material, cuando el nombre libre no dijo nada conocido.</summary>
        public static string? ClaveDeMaterial(string? codigoMaterial) => codigoMaterial switch
        {
            "PET" => "plastic_bottle",
            "PLASTICO_SIN_ID" => "plastic_container",
            "BOLSA_PLASTICA" => "plastic_bag",
            "MULTICAPA" => "drink_carton",
            "CARTON" => "cardboard",
            "PAPEL" => "paper",
            "VIDRIO" => "glass_bottle",
            "METAL" => "metal_item",
            "LATA_ALUMINIO" => "can",
            "ORGANICO" => "food_scraps",
            "PILA" => "battery",
            "MEDICAMENTO" => "medicine",
            "BOMBILLO" => "bulb",
            "RAEE" => "electronic_other",
            "OBJETO_COMPUESTO" => "snack_wrapper",
            "CONDON" => "condom",
            "TOALLA_HIGIENICA" => "diaper_or_pad",
            "SERVILLETA_USADA" => "used_tissue",
            "JERINGA" => "syringe",
            _ => null,
        };
    }
}
