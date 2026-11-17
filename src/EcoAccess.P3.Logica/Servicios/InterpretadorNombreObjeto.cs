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
        // «Rind» y «husk» (misión maestra, 14/11/2026): «watermelon rind» es una cáscara, no una fruta entera.
        private static readonly string[] Restos = { "peel", "peels", "peeled", "peeling", "peelings", "core", "scrap", "scraps", "leftover", "leftovers", "compost", "rotten",
            "rind", "rinds", "husk", "husks" };
        private static readonly string[] BotellaVidrio = { "glass", "wine", "beer", "champagne", "liquor", "whiskey" };
        // «Canned fruit», «canned food label»: el envase metálico es el residuo, no la comida de la etiqueta.
        private static readonly string[] Enlatado = { "can", "canned", "tin", "tinned" };
        // «Foil wrapper», «aluminum foil tray»: el aluminio es metal, no un empaque multicapa ni un recipiente plástico.
        private static readonly string[] Aluminio = { "foil", "aluminum", "aluminium" };
        private static readonly string[] BotellaPlastico = { "plastic", "water", "soda", "pet", "cola", "juice", "soft" };

        private static readonly Dictionary<string, string> Compuestos = new(StringComparer.Ordinal)
        {
            ["teddy bear"] = "toy", ["computer mouse"] = "computer_mouse", ["power adapter"] = "charger", ["ac adapter"] = "charger", ["circuit board"] = "electronic_other",
            ["power bank"] = "electronic_other", ["sanitary pad"] = "diaper_or_pad", ["paper towel"] = "used_tissue", ["milk carton"] = "drink_carton",
            ["juice carton"] = "drink_carton", ["juice box"] = "drink_carton", ["tetra pak"] = "drink_carton", ["washing machine"] = "appliance", ["coffee table"] = "table",
            ["light bulb"] = "bulb", ["plastic bag"] = "plastic_bag", ["shopping bag"] = "plastic_bag", ["blank background"] = CatalogoObjetos.NoDistinguible,
            ["white background"] = CatalogoObjetos.NoDistinguible, ["black background"] = CatalogoObjetos.NoDistinguible, ["potted tree"] = "plant", ["bonsai tree"] = "plant", ["tablet computer"] = "tablet",
            // Misión maestra (14/11/2026): una cuchilla se nombraba «una jeringa». La ruta (cortopunzante) no cambia; el nombre sí.
            ["razor blade"] = "disposable_razor", ["disposable razor"] = "disposable_razor", ["shaving razor"] = "disposable_razor", ["safety razor"] = "disposable_razor",
            ["grass clipping"] = "plant_waste", ["cut grass"] = "plant_waste", ["mowed grass"] = "plant_waste",
            // Evaluación de productos de Colombia (10/11/2026): el modelo nombraba bien y el intérprete fallaba.
            // La caja de huevos es cartón moldeado, no un envase multicapa; una bolsa de papas es un empaque metalizado.
            // «Tissue paper»: papel higiénico o pañuelo usado, residuo sanitario; no papel aprovechable.
            ["tissue paper"] = "used_tissue",
            ["egg carton"] = "cardboard", ["egg tray"] = "cardboard", ["egg box"] = "cardboard",
            ["water bag"] = "water_bag", ["ice bag"] = "water_bag", ["water packet"] = "water_bag", ["water pouch"] = "water_bag", ["water sachet"] = "water_bag",
            ["foil wrapper"] = "snack_wrapper", ["foil bag"] = "snack_wrapper", ["shiny wrapper"] = "snack_wrapper", ["metallic wrapper"] = "snack_wrapper",
            ["chip bag"] = "snack_wrapper", ["chips bag"] = "snack_wrapper", ["crisps bag"] = "snack_wrapper", ["crisp bag"] = "snack_wrapper", ["snack bag"] = "snack_wrapper", ["candy bag"] = "snack_wrapper",
            // Tercera ronda (10/11/2026): «Air conditioner» y «Fan» caían en la segunda lectura, que respondía «metal».
            // Un aire acondicionado o un ventilador es un aparato, nunca un residuo aprovechable para la caneca.
            ["air conditioner"] = "appliance", ["air conditioning"] = "appliance", ["ac unit"] = "appliance",
            ["computer fan"] = "electronic_other", ["cooling fan"] = "electronic_other", ["cpu fan"] = "electronic_other",
            // Catálogo ampliado (12/11/2026).
            ["coffee ground"] = "food_scraps", ["tea bag"] = "food_scraps", ["pizza box"] = "greasy_paper", ["glass shard"] = "broken_glass",
            ["broken glass"] = "broken_glass", ["spray can"] = "aerosol", ["paint can"] = "chemical_container", ["cooking oil"] = "cooking_oil",
            ["plastic fork"] = "disposable_cutlery", ["plastic spoon"] = "disposable_cutlery", ["plastic knife"] = "disposable_cutlery",
            ["plastic straw"] = "disposable_cutlery", ["plastic cutlery"] = "disposable_cutlery", ["cigarette butt"] = "cigarette_butt",
            ["foam tray"] = "styrofoam", ["foam cup"] = "styrofoam", ["foam box"] = "styrofoam", ["foam container"] = "styrofoam",
            ["cleaning product"] = "chemical_container", ["bleach bottle"] = "chemical_container", ["detergent bottle"] = "chemical_container",
            ["toilet paper"] = "used_tissue",
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
            ("people", "person people man woman boy girl child kid student worker player group crowd protester adult baby family team audience portrait face " +
                "officer soldier police policeman policewoman military army guard nurse doctor chef musician singer guitarist dancer athlete"),
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
            ("appliance", "refrigerator fridge freezer washer dryer oven microwave dishwasher stove appliance blender toaster fan heater conditioner"),
            ("toy", "toy doll lego plush"),
            ("shoes", "shoe sneaker boot sandal slipper heel"),
            ("clothes", "shirt tshirt pant jeans trouser dress jacket coat robe clothes clothing sweater hoodie sock skirt garment uniform"),
            ("tool", "hammer screwdriver wrench pliers saw drill tool mallet spanner"),
            ("served_food", "plate meal dish food steak rice salad pasta pizza burger sandwich soup bread cake meat bowl noodle taco egg breakfast lunch dinner dessert waffle"),
            // Fruta o verdura entera (prueba real 12/11/2026: una manzana verde se leía «comida preparada»).
            ("whole_produce", "fruit apple banana orange pear mango lemon lime grape strawberry pineapple papaya watermelon melon avocado guava " +
                "tangerine mandarin peach plum kiwi vegetable tomato tomatoes carrot potato potatoes onion lettuce cabbage cucumber pepper broccoli zucchini"),
            ("plastic_bottle", "bottle"),
            ("glass_bottle", "jar glass"),
            ("can", "can tin canned tinned"),
            ("plastic_container", "cup tub container bucket tray lid yogurt custard"),
            // Caso real del 12/11/2026 (órdenes 1685 y 1686): una bolsa transparente de agua o hielo se nombró como «packet» y se
            // trató como empaque metalizado. Las palabras genéricas de empaque son PLÁSTICO FLEXIBLE (solo orienta); lo metalizado
            // exige una pista visible de mecato o brillo (chip, candy, snack, foil, shiny...).
            ("plastic_bag", "bag film wrapper packet package pouch sachet"),
            ("drink_carton", "carton tetra"),
            ("cardboard", "box cardboard"),
            ("paper", "paper document newspaper magazine sheet page text writing letter envelope"),
            ("metal_item", "metal aluminum aluminium foil"),
            ("snack_wrapper", "chip crisp candy snack chocolate"),
            ("condom", "condom"),
            ("diaper_or_pad", "diaper nappy pad tampon bandage"),
            ("used_tissue", "tissue napkin"),
            ("gloves", "glove"),
            ("face_mask", "mask facemask respirator"),
            ("battery", "battery"),
            ("medicine", "pill blister medicine capsule"),
            ("bulb", "bulb"),
            ("syringe", "syringe needle"),
            ("styrofoam", "styrofoam polystyrene foam icopor"),
            ("cigarette_butt", "cigarette cigar"),
            ("thermal_paper", "receipt ticket"),
            ("disposable_cutlery", "fork spoon straw cutlery utensil"),
            ("mirror_or_ceramic", "mirror ceramic porcelain mug"),
            ("wood", "wood plank lumber"),
            ("aerosol", "aerosol spray"),
            ("chemical_container", "bleach detergent solvent pesticide insecticide chemical"),
            ("cooking_oil", "oil"),
            (CatalogoObjetos.VariosObjetos, "pile heap trash garbage litter rubbish waste debris dump"),
        });

        // Catálogo colombiano ampliado: agrega palabras y frases de dos palabras SIN reemplazar las del intérprete base, para no
        // cambiar decisiones ya medidas en las rondas visuales 3 a 7.
        private static readonly HashSet<string> Genericas = new(StringComparer.Ordinal) { "electronic_other", CatalogoObjetos.VariosObjetos, CatalogoObjetos.NoDistinguible };
        private static readonly int Ampliadas = Ampliar();

        private static int Ampliar()
        {
            int agregadas = 0;
            // Primero las frases, contra el intérprete BASE: una frase solo entra si el base no la resolvía, o la resolvía a una
            // clave genérica que la ampliación precisa (un «bluetooth speaker» era «otro aparato electrónico»).
            var nuevas = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (CatalogoAmpliado.Entrada e in CatalogoAmpliado.Entradas)
                foreach (string frase in e.Frases.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] partes = frase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length != 2) continue;
                    string llave = partes[0] + " " + Singular(partes[1]);
                    string? base_ = BuscarEnFrase(partes);
                    bool existente = Compuestos.TryGetValue(llave, out string? previa) && !Genericas.Contains(previa);
                    // Reemplaza al base si este no la resolvía, si era genérico, o si la ruta ampliada es otra y NO es la blanca: las
                    // entradas ampliadas no tienen material, así que el cambio solo puede quitar aperturas («pill bottle» deja de ser
                    // una botella PET). Una precisión con la misma ruta conserva la clave ya medida.
                    bool masSegura = base_ is not null && e.Ruta != RutaDisposicion.Blanca
                        && CatalogoObjetos.Buscar(base_) is { } visto && MatrizDecisionResiduos.Ruta(visto) != e.Ruta;
                    if (!existente && !nuevas.ContainsKey(llave) && (base_ is null || Genericas.Contains(base_) || masSegura)) nuevas[llave] = e.Clave;
                }
            foreach (var par in nuevas) { Compuestos[par.Key] = par.Value; agregadas++; }
            foreach (CatalogoAmpliado.Entrada e in CatalogoAmpliado.Entradas)
                foreach (string palabra in e.Palabras.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!Palabras.ContainsKey(palabra) && Buscar(palabra) is null) { Palabras[palabra] = e.Clave; agregadas++; }
            return agregadas;
        }

        /// <summary>Palabras y frases que aportó el catálogo ampliado (diagnóstico y pruebas).</summary>
        internal static int PalabrasAmpliadas => Ampliadas;

        private static Dictionary<string, string> Construir((string Clave, string Lista)[] grupos)
        {
            var palabras = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string clave, string lista) in grupos)
                foreach (string palabra in lista.Split(' ')) palabras[palabra] = clave;
            return palabras;
        }

        // Comida a partir de animales: la foto de un plato con pescado decía «Identifiqué un animal» (11/11/2026), porque «fish»
        // estaba solo en la lista de animales. Con una palabra culinaria o de cocina, es comida; con «raw», comida cruda.
        private static readonly string[] CarneOPescado = { "fish", "salmon", "tuna", "tilapia", "trout", "cod", "sardine", "sardines", "shrimp", "shrimps", "prawn",
            "prawns", "seafood", "meat", "beef", "pork", "steak", "sausage", "sausages", "ham", "bacon", "fillet", "filet", "drumstick", "wings" };
        private static readonly string[] Pescados = { "fish", "salmon", "tuna", "tilapia", "trout", "cod", "sardine", "sardines", "shrimp", "shrimps", "prawn", "prawns", "seafood" };
        private static readonly string[] Culinario = { "cooked", "grilled", "fried", "baked", "roasted", "roast", "steamed", "smoked", "boiled", "breaded", "plate", "plated",
            "dish", "meal", "served", "sauce", "rice", "potato", "potatoes", "fries", "salad", "dinner", "lunch", "breakfast", "food", "cuisine", "stew", "soup",
            "curry", "platter", "ceviche", "sushi", "fillet", "filet", "patacon", "patacones", "plantain", "arepa", "beans", "lemon", "lime", "vegetables", "bowl" };
        private static readonly string[] Crudo = { "raw", "uncooked" };
        private static readonly string[] EnvaseDeHuevos = { "carton", "cartons", "box", "boxes", "tray", "trays", "container", "package", "pack", "crate" };

        // Palabras que convierten una fruta o verdura en un plato preparado: «fruit salad», «mashed potatoes», «apple pie».
        private static readonly string[] Preparado = { "salad", "cooked", "grilled", "fried", "baked", "roasted", "roast", "steamed", "boiled", "mashed",
            "plate", "plated", "dish", "meal", "served", "sauce", "soup", "stew", "pie", "cake", "juice", "smoothie", "dessert", "fries", "chips", "bowl" };
        private static readonly string[] PapelUsado = { "used", "dirty", "soiled", "crumpled", "stained", "wiped", "wet" };
        private static readonly string[] TuboCarton = { "tube", "core", "cardboard", "empty" };

        /// <summary>«Grilled fish with potatoes» = comida con pescado; «raw chicken breast» = crudo; «brown eggs» = huevos.</summary>
        private static string? Alimento(string[] tokens)
        {
            bool huevos = tokens.Any(t => t is "egg" or "eggs");
            if (huevos && tokens.Any(EnvaseDeHuevos.Contains))
                // La caja vacía es cartón (Compuestos). Con huevos dentro («carton of eggs», «egg tray with eggs», «eggs in a
                // carton») no es un residuo vacío: la foto carton-05 abría la BLANCA (misión maestra, 14/11/2026).
                return ConHuevosDentro(tokens) ? "egg_carton_with_eggs" : null;
            bool carne = tokens.Any(CarneOPescado.Contains) || (tokens.Contains("chicken") && (tokens.Any(Culinario.Contains) || tokens.Any(Crudo.Contains)));
            if (carne && tokens.Any(Crudo.Contains)) return "raw_food";
            if (carne && tokens.Any(Culinario.Contains)) return tokens.Any(Pescados.Contains) ? "served_fish" : "served_food";
            if (huevos) return tokens.Any(t => Culinario.Contains(t) && t is not "food") ? "served_food" : "eggs";
            // «Green apple», «bunch of bananas»: entera. «Fruit salad», «apple pie»: preparada. Una lata de fruta es la lata.
            // Solo si ninguna otra palabra nombra otro objeto: «orange plastic bottle», «orange cat» siguen su propia regla.
            if (tokens.Any(t => Buscar(t) == "whole_produce") && !tokens.Any(Enlatado.Contains)
                && tokens.All(t => Buscar(t) is null or "whole_produce" or "served_food"))
                return tokens.Any(Preparado.Contains) ? "served_food" : "whole_produce";
            return null;
        }

        private static bool ConHuevosDentro(string[] tokens)
        {
            int envase = Array.FindIndex(tokens, EnvaseDeHuevos.Contains);
            return tokens.Skip(envase + 1).Any(t => t is "egg" or "eggs")
                || tokens.Take(envase).Any(t => t is "in" or "inside")
                || tokens.Any(t => t is "full" or "filled" or "containing" or "dozen");
        }

        // Misión maestra (14/11/2026): lo que dice el NOMBRE sobre el contenido. Nunca se supone que un envase está vacío; si el
        // nombre dice que tiene algo, no hay residuo vacío que depositar. La pregunta de contenido del modelo cubre el resto.
        private static readonly string[] Contenedores = { "plastic_bottle", "glass_bottle", "plastic_container", "drink_carton", "can", "cardboard", "juice_container",
            "soda_bottle_big", "yogurt_bottle", "glass_jar_food", "tuna_can", "beer_can" };
        private static readonly string[] PalabrasLleno = { "full", "filled", "half", "partially", "liquid", "unopened", "sealed", "containing" };
        private static readonly string[] ContenidoVisible = { "water", "juice", "soda", "coffee", "milk", "drink", "beer", "liquid", "tea", "food", "soup", "rice",
            "leftover", "leftovers", "ice", "yogurt", "cereal", "fries" };
        private static readonly string[] Recipiente = { "cup", "glass", "mug" };
        private static readonly string[] BolsaFlexible = { "bag", "bags", "sachet", "sachets", "pouch", "pouches", "packet", "packets" };
        // Limpiadores y químicos de uso doméstico: el envase se parece a una botella de bebida (incidente de entrenamiento 12/11/2026).
        private static readonly string[] Quimico = { "cleaning", "cleaner", "disinfectant", "chlorine", "ammonia", "degreaser", "bleach", "detergent", "solvent", "varsol" };
        private static readonly string[] Aseo = { "soap", "shampoo", "conditioner", "lotion", "gel", "sanitizer" };
        // Mecato colombiano: los empaques de papas, chicharrones, galletas y chocolatinas son metalizados o multicapa.
        private static readonly string[] Mecato = { "chip", "chips", "crisp", "crisps", "candy", "snack", "snacks", "chocolate", "shiny", "metallic", "metalized", "foil",
            "doritos", "cheetos", "chicharron", "chicharrones", "choclitos", "todito", "pringles", "lays", "cookie", "cookies", "biscuit", "biscuits", "cracker", "crackers", "wafer" };

        private static bool NombraContenido(string[] tokens)
        {
            if (tokens.Any(PalabrasLleno.Contains)) return true;
            int con = Array.IndexOf(tokens, "with");
            if (con >= 0 && tokens.Skip(con + 1).Any(ContenidoVisible.Contains)) return true;
            // «Glass of water», «cup of coffee»: una bebida servida, no un envase vacío.
            for (int i = 0; i + 1 < tokens.Length; i++)
                if (Recipiente.Contains(tokens[i]) && tokens[i + 1] == "of") return true;
            return false;
        }

        /// <summary>
        /// Papel higiénico: un rollo no dice si está usado (prueba real del 12/11/2026). Usado → sanitario (negra); tubo de cartón
        /// limpio → cartón (blanca); rollo sin más → objeto sin residuo supuesto, que no abre ninguna tapa.
        /// </summary>
        private static string? PapelHigienico(string[] tokens)
        {
            if (!tokens.Contains("toilet") || !tokens.Any(t => t is "paper" or "roll" or "rolls" or "tissue")) return null;
            if (tokens.Any(PapelUsado.Contains)) return "used_tissue";
            if (tokens.Any(TuboCarton.Contains)) return "cardboard";
            return "toilet_paper_roll";
        }

        /// <summary>Clave del objeto nombrado, o null si el texto no menciona nada conocido.</summary>
        /// <param name="materialIncierto">Nombró una botella sin decir de qué: la otra lectura debe resolver si es plástico o vidrio.</param>
        public static string? Clave(string? nombre, out bool materialIncierto)
        {
            materialIncierto = false;
            string[] tokens = Tokens(nombre);
            if (tokens.Length == 0) return null;
            // Antes que los restos: «toilet paper core» es el tubo de cartón, no un resto de comida.
            if (PapelHigienico(tokens) is { } papel) return papel;
            // «Bag of drinking water», «plastic bag of water»: bolsa de agua, no botella PET ni empaque metalizado (órdenes 1685/1686).
            if (tokens.Any(t => t is "water" or "ice") && tokens.Any(BolsaFlexible.Contains) && !tokens.Any(t => t is "bottle" or "bottles")) return "water_bag";
            if (tokens.Any(Restos.Contains) && tokens.Any(t => Buscar(t) is "served_food" or "whole_produce" || Restos.Contains(t) || CarneOPescado.Contains(t))) return "food_scraps";
            if (tokens.Any(Cascara.Contains) || (tokens.Any(t => t is "egg" or "eggs") && tokens.Any(Concha.Contains))) return "food_scraps";
            if (Alimento(tokens) is { } alimento) return alimento;

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

        // Envases que contienen comida o bebida: «bottle of orange juice», «box of apples». El residuo es el envase.
        private static readonly string[] EnvasesDeComida = { "plastic_bottle", "glass_bottle", "drink_carton", "plastic_container", "plastic_bag", "cardboard" };

        private static string Afinar(string clave, string[] tokens, out bool materialIncierto)
        {
            materialIncierto = false;
            // El estado visible cambia el destino: icopor no es plástico aprovechable, el cartón con grasa va a la negra y el
            // vidrio roto corta (catálogo ampliado, 12/11/2026).
            if (tokens.Any(t => t is "styrofoam" or "foam" or "polystyrene" or "icopor")
                && clave is "plastic_container" or "cardboard" or "served_food" or "whole_produce" or "plastic_bag" or CatalogoObjetos.VariosObjetos)
                return "styrofoam";
            if (clave is "paper" or "cardboard" && tokens.Any(t => t is "greasy" or "oily" or "grease" or "stained"))
                return "greasy_paper";
            if (clave is "glass_bottle" or "plastic_bottle" && tokens.Contains("glass") && tokens.Any(t => t is "broken" or "shattered" or "shard" or "shards" or "cracked"))
                return "broken_glass";
            if (clave is "whole_produce" or "served_food" && tokens.Select(Buscar).FirstOrDefault(k => k is not null && EnvasesDeComida.Contains(k)) is { } envase)
                clave = envase;
            if (clave == "paper" && tokens.Contains("cardboard")) return "cardboard";
            if (clave is "served_food" or "whole_produce" && tokens.Any(Enlatado.Contains)) return "can";
            // Un empaque flexible solo es metalizado con una pista visible de mecato o brillo («chip packet», «candy wrapper»).
            if (clave == "plastic_bag" && tokens.Any(Mecato.Contains))
                clave = "snack_wrapper";
            if (Contenedores.Contains(clave))
            {
                if (tokens.Any(Quimico.Contains) && !tokens.Contains("empty")) return "chemical_container";
                if (tokens.Any(Aseo.Contains) && clave is "plastic_bottle" or "plastic_container") return "liquid_soap";
                if (NombraContenido(tokens)) return "container_with_content";
            }
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
