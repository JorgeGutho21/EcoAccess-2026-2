using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Lo que un motor visual puede decir que ve, sea o no un residuo. Es una lista cerrada: el modelo elige una clave y
    /// el texto para la persona lo redacta EcoAccess. Si un objeto puede ser un material, solo Oracle dice si se admite.
    /// </summary>
    /// <param name="Definicion">Lo que lee el modelo para elegir esta opción.</param>
    /// <param name="Frase">Con artículo, para «Identifiqué …».</param>
    /// <param name="Explicacion">Por qué no continúa en la caneca, para el caso en que el catálogo no lo admite.</param>
    /// <param name="CodigoMaterial">Material del catálogo que le corresponde, o null si no es un residuo que exista allí.</param>
    /// <param name="Objeto">Objeto que usa el resto del flujo cuando se admite.</param>
    public sealed record ObjetoConocido(string Clave, string Definicion, ContextoObjeto Contexto, string Emoji, string Frase, string Explicacion,
        string? CodigoMaterial = null, ObjetoVisible? Objeto = null)
    {
        public string Mensaje => Contexto == ContextoObjeto.Persona
            ? $"{Emoji} Veo {Frase}, pero no identifico un residuo para depositar.\n{Explicacion}"
            : $"{Emoji} Identifiqué {Frase}.\n{Explicacion}";
    }

    public static class CatalogoObjetos
    {
        /// <summary>Clave de una foto que no se puede distinguir: es una abstención real, no un objeto ajeno al catálogo.</summary>
        public const string NoDistinguible = "unclear";

        /// <summary>Clave de varios objetos sin uno principal.</summary>
        public const string VariosObjetos = "mixed_trash";

        private const string SinContenedorElectronicos = "♻️ EcoAccess todavía no dispone de contenedores para residuos electrónicos.\n🌱 Si vas a desecharlo, busca un punto autorizado de recolección de aparatos electrónicos.";
        private const string NoAdmitidoPiloto = "♻️ Este objeto no está admitido en nuestra caneca piloto.";
        private const string Voluminoso = "♻️ Nuestra caneca piloto no recibe objetos voluminosos.";
        private const string Sanitario = "⚠️ No está admitido en la caneca aprovechable de EcoAccess.\nDeposítalo correctamente en un recipiente para residuos no aprovechables, conforme a las indicaciones locales.";
        private const string VidaAnimal = "💚 Los animales son seres vivos, no residuos. En EcoAccess promovemos el respeto y cuidado de la vida animal.\n📸 Envíame una foto de un residuo para continuar.";
        private const string Reparar = "♻️ Este objeto no está admitido en nuestra caneca piloto. Si aún sirve, considera repararlo, donarlo o reutilizarlo.";
        private const string SinCanecaPiloto = "♻️ Nuestra caneca piloto no recibe este residuo.";
        private const string OrientarComida = "🌱 Si son restos que deseas desechar, puedo orientarte hacia la caneca verde.";

        /// <summary>
        /// Alimentos que pueden seguir siendo comida. Fuera del flujo de disposición (sin QR vigente) no se suponen residuo:
        /// Eco solo orienta. Dentro del flujo, la foto ya expresa la intención de desechar y van a la caneca verde.
        /// </summary>
        public static readonly IReadOnlyCollection<string> AlimentosPorConfirmar = new HashSet<string>(StringComparer.Ordinal) { "served_food", "served_fish", "raw_food", "eggs", "whole_produce" };

        /// <summary>Material orgánico del catálogo con el que se deposita un alimento en el flujo de disposición.</summary>
        public const string MaterialAlimentos = "ORGANICO";

        /// <summary>
        /// Material con el que este objeto puede depositarse en la verde o la negra de una estación, o null si no corresponde.
        /// Lo aprovechable sigue su propio recorrido (requiere segunda lectura corroborada) y lo especial nunca va a una caneca.
        /// </summary>
        public static string? MaterialEnEstacion(ObjetoConocido visto)
        {
            if (visto is null) return null;
            if (AlimentosPorConfirmar.Contains(visto.Clave)) return MaterialAlimentos;
            return visto.Contexto is ContextoObjeto.ResiduoOrganico or ContextoObjeto.ResiduoNoAprovechable or ContextoObjeto.ResiduoSanitario
                ? visto.CodigoMaterial : null;
        }

        public static IReadOnlyList<ObjetoConocido> Todos { get; } = new[]
        {
            new ObjetoConocido("dog", "dog", ContextoObjeto.AnimalVivo, "🐶", "un perro", VidaAnimal),
            new ObjetoConocido("cat", "cat", ContextoObjeto.AnimalVivo, "🐱", "un gato", "💚 Los animales no son residuos. Cuidemos y respetemos la vida."),
            new ObjetoConocido("bird", "bird, chicken or other poultry", ContextoObjeto.AnimalVivo, "🐦", "un ave", VidaAnimal),
            new ObjetoConocido("horse", "horse, cow or other farm animal", ContextoObjeto.AnimalVivo, "🐴", "un animal de granja", VidaAnimal),
            new ObjetoConocido("other_animal", "any other animal", ContextoObjeto.AnimalVivo, "🐾", "un animal", VidaAnimal),

            new ObjetoConocido("tree", "a whole tree outdoors", ContextoObjeto.PlantaViva, "🌳", "un árbol", "💚 Los árboles son seres vivos. EcoAccess no dispone de contenedores para este tipo de elementos."),
            new ObjetoConocido("plant", "living plant in a pot or garden", ContextoObjeto.PlantaViva, "🌿", "una planta", "💚 Las plantas no son residuos. ¡Cuidemos nuestra naturaleza!"),
            new ObjetoConocido("flower", "flower", ContextoObjeto.PlantaViva, "🌸", "una flor", "💚 Las plantas no son residuos. ¡Cuidemos nuestra naturaleza!"),
            new ObjetoConocido("grass", "lawn or grass", ContextoObjeto.PlantaViva, "🌱", "césped", "💚 Las plantas no son residuos. ¡Cuidemos nuestra naturaleza!"),
            new ObjetoConocido("plant_waste", "cut branches, dry leaves or pruning waste", ContextoObjeto.ResiduoOrganico, "🍃", "restos vegetales", "♻️ EcoAccess todavía no dispone de una caneca específica para estos residuos."),

            new ObjetoConocido("people", "people are the main subject", ContextoObjeto.Persona, "📷", "personas", "♻️ Envíame una foto del objeto que deseas desechar."),
            new ObjetoConocido("people_computers", "people using computers", ContextoObjeto.Persona, "📷", "personas y computadores", "♻️ Envíame una foto del objeto que deseas desechar."),

            new ObjetoConocido("computer_mouse", "computer mouse", ContextoObjeto.ResiduoElectronico, "🖱️", "un mouse de computador", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("keyboard", "computer keyboard", ContextoObjeto.ResiduoElectronico, "⌨️", "un teclado", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("laptop", "laptop or desktop computer", ContextoObjeto.ResiduoElectronico, "💻", "un computador", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("phone", "cell phone", ContextoObjeto.ResiduoElectronico, "📱", "un celular", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("tablet", "tablet", ContextoObjeto.ResiduoElectronico, "📱", "una tableta", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("charger", "charger or power adapter", ContextoObjeto.ResiduoElectronico, "🔌", "un cargador", SinContenedorElectronicos, "CARGADOR", ObjetoVisible.Aparato),
            new ObjetoConocido("cable", "cable or wire", ContextoObjeto.ResiduoElectronico, "🔌", "un cable", SinContenedorElectronicos, "CABLE", ObjetoVisible.Aparato),
            new ObjetoConocido("headphones", "headphones or earphones", ContextoObjeto.ResiduoElectronico, "🎧", "unos audífonos", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("monitor", "monitor or television", ContextoObjeto.ResiduoElectronico, "🖥️", "un monitor", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("printer", "printer", ContextoObjeto.ResiduoElectronico, "🖨️", "una impresora", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("electronic_other", "other electronic device or circuit board", ContextoObjeto.ResiduoElectronico, "🔌", "un aparato electrónico", SinContenedorElectronicos, "RAEE", ObjetoVisible.Aparato),
            new ObjetoConocido("battery", "battery or cell", ContextoObjeto.ResiduoEspecial, "🔋", "una pila o batería", "⚠️ No la deposites en la caneca piloto. Llévala a un punto de recolección de pilas; si está dañada o hinchada, no la manipules sin protección.", "PILA", ObjetoVisible.PilaOBateria),
            new ObjetoConocido("medicine", "pills, blister or medicine", ContextoObjeto.ResiduoEspecial, "💊", "un medicamento", "⚠️ No lo deposites en la caneca piloto. Llévalo a un punto autorizado de devolución de medicamentos.", "MEDICAMENTO", ObjetoVisible.Medicamento),
            new ObjetoConocido("bulb", "light bulb", ContextoObjeto.ResiduoEspecial, "💡", "un bombillo", "⚠️ No lo deposites en la caneca piloto: puede romperse. Entrégalo en un punto de recolección de bombillos.", "BOMBILLO", ObjetoVisible.Bombillo),
            new ObjetoConocido("syringe", "syringe, needle or blade", ContextoObjeto.ResiduoEspecial, "⚠️", "un elemento cortopunzante", "⚠️ No lo deposites en la caneca piloto ni lo manipules sin protección. Guárdalo en un recipiente rígido y entrégalo en un punto de gestión de residuos peligrosos.", "JERINGA", ObjetoVisible.Jeringa),

            new ObjetoConocido("door", "door", ContextoObjeto.ObjetoCotidiano, "🚪", "una puerta", Voluminoso),
            new ObjetoConocido("window", "window", ContextoObjeto.ObjetoCotidiano, "🪟", "una ventana", Voluminoso),
            new ObjetoConocido("chair", "chair", ContextoObjeto.ObjetoCotidiano, "🪑", "una silla", NoAdmitidoPiloto),
            new ObjetoConocido("table", "table or desk", ContextoObjeto.ObjetoCotidiano, "🪑", "una mesa", Voluminoso),
            new ObjetoConocido("furniture", "sofa, bed, shelf or other furniture", ContextoObjeto.ObjetoCotidiano, "🛋️", "un mueble", Voluminoso),
            new ObjetoConocido("appliance", "fridge, washer, oven or other household appliance", ContextoObjeto.ObjetoCotidiano, "🧺", "un electrodoméstico", "♻️ Nuestra caneca piloto no recibe electrodomésticos. Si aún funciona, considera repararlo o donarlo; si no, busca un punto autorizado de recolección."),
            new ObjetoConocido("toy", "toy", ContextoObjeto.ObjetoCotidiano, "🧸", "un juguete", Reparar),
            new ObjetoConocido("shoes", "shoes", ContextoObjeto.ObjetoCotidiano, "👟", "unos zapatos", Reparar, "ZAPATOS"),
            new ObjetoConocido("clothes", "clothes", ContextoObjeto.ObjetoCotidiano, "👕", "ropa", Reparar, "ROPA"),
            new ObjetoConocido("tool", "hand tool", ContextoObjeto.ObjetoCotidiano, "🔧", "una herramienta", Reparar),

            // Comida que todavía puede ser alimento: no se asume que sea un residuo. Eco pregunta antes si se va a desechar
            // (ServicioConversacionTelegram.AlimentosPorConfirmar). Pescado cocinado en un plato no es un animal vivo.
            new ObjetoConocido("served_food", "food served on a plate or ready to eat", ContextoObjeto.ObjetoCotidiano, "🍽️", "comida preparada", OrientarComida),
            new ObjetoConocido("served_fish", "cooked fish served as a meal", ContextoObjeto.ObjetoCotidiano, "🍽️", "comida preparada, que incluye pescado y acompañamientos", OrientarComida),
            new ObjetoConocido("raw_food", "raw meat or raw fish", ContextoObjeto.ObjetoCotidiano, "🥩", "carne o pescado crudo", OrientarComida),
            new ObjetoConocido("eggs", "whole eggs", ContextoObjeto.ObjetoCotidiano, "🥚", "huevos", OrientarComida),
            // Prueba real del 12/11/2026: una manzana verde se leyó como «comida preparada». Una fruta o verdura entera no es
            // un plato servido; en el flujo de disposición va a la verde como orgánico.
            new ObjetoConocido("whole_produce", "whole fruit or vegetable, not cooked", ContextoObjeto.ObjetoCotidiano, "🍎", "una fruta o verdura", OrientarComida),
            new ObjetoConocido("food_scraps", "leftovers, peels or food scraps to throw away", ContextoObjeto.ResiduoOrganico, "🍃", "restos de comida", "EcoAccess todavía no dispone de un contenedor adecuado para estos residuos.", "ORGANICO", ObjetoVisible.ResiduoOrganico),

            new ObjetoConocido("plastic_bottle", "plastic drink bottle", ContextoObjeto.ResiduoAprovechable, "♻️", "una botella plástica", SinCanecaPiloto, "PET", ObjetoVisible.BotellaPlastica),
            new ObjetoConocido("glass_bottle", "glass bottle or jar", ContextoObjeto.ResiduoAprovechable, "♻️", "un envase de vidrio", SinCanecaPiloto, "VIDRIO", ObjetoVisible.EnvaseVidrio),
            new ObjetoConocido("can", "metal drink can", ContextoObjeto.ResiduoAprovechable, "♻️", "una lata", SinCanecaPiloto, "LATA_ALUMINIO", ObjetoVisible.Lata),
            new ObjetoConocido("plastic_container", "plastic cup, tub or container", ContextoObjeto.ResiduoAprovechable, "♻️", "un envase plástico", SinCanecaPiloto, "PLASTICO_RIGIDO", ObjetoVisible.EnvasePlasticoRigido),
            new ObjetoConocido("plastic_bag", "plastic bag or film", ContextoObjeto.ResiduoAprovechable, "♻️", "una bolsa plástica", SinCanecaPiloto, "BOLSA_PLASTICA", ObjetoVisible.BolsaPlastica),
            new ObjetoConocido("drink_carton", "milk or juice carton, Tetra Pak", ContextoObjeto.ResiduoAprovechable, "♻️", "un envase de cartón multicapa", SinCanecaPiloto, "MULTICAPA", ObjetoVisible.EnvaseCompuesto),
            new ObjetoConocido("cardboard", "cardboard or box", ContextoObjeto.ResiduoAprovechable, "♻️", "cartón", SinCanecaPiloto, "CARTON", ObjetoVisible.Carton),
            new ObjetoConocido("paper", "paper, newspaper or document", ContextoObjeto.ResiduoAprovechable, "♻️", "papel", SinCanecaPiloto, "PAPEL", ObjetoVisible.Papel),
            new ObjetoConocido("metal_item", "other metal object", ContextoObjeto.ResiduoAprovechable, "♻️", "un objeto metálico", SinCanecaPiloto, "METAL", ObjetoVisible.ObjetoMetalico),
            new ObjetoConocido("snack_wrapper", "chips or candy wrapper, shiny", ContextoObjeto.ResiduoNoAprovechable, "🗑️", "un empaque multicapa", "♻️ Nuestra caneca piloto solo recibe aprovechables. Deposítalo en un recipiente de residuos no aprovechables.", "OBJETO_COMPUESTO", ObjetoVisible.EmpaqueMetalizado),

            new ObjetoConocido("condom", "condom, used or in its wrapper", ContextoObjeto.ResiduoSanitario, "♻️", "un residuo sanitario", Sanitario, "CONDON", ObjetoVisible.ArticuloSanitario),
            new ObjetoConocido("diaper_or_pad", "diaper, sanitary pad or tampon", ContextoObjeto.ResiduoSanitario, "♻️", "un residuo sanitario", Sanitario, "PANAL", ObjetoVisible.ArticuloSanitario),
            new ObjetoConocido("used_tissue", "used napkin, tissue or toilet paper", ContextoObjeto.ResiduoSanitario, "♻️", "un residuo sanitario", Sanitario, "SERVILLETA_USADA", ObjetoVisible.ArticuloSanitario),
            // Prueba real del 12/11/2026: un rollo de papel higiénico se trató como papel usado. Un rollo no permite saber si
            // está usado; no se supone contaminación ni se abre ninguna tapa.
            new ObjetoConocido("toilet_paper_roll", "a roll of toilet paper", ContextoObjeto.ObjetoCotidiano, "🧻", "un rollo de papel higiénico",
                "Un rollo nuevo no es un residuo. Si es papel higiénico usado, va a la caneca NEGRA; si es solo el tubo de cartón limpio, va a la BLANCA. Envíame una foto que muestre claramente cuál de los dos es."),
            new ObjetoConocido("gloves", "disposable gloves", ContextoObjeto.ResiduoSanitario, "♻️", "un residuo sanitario", Sanitario, "OTRO_RESIDUO", ObjetoVisible.ArticuloSanitario),
            // Tapabocas usado: residuo sanitario. Antes no tenía clave y el modelo, que lo nombraba bien, quedaba sin respuesta.
            new ObjetoConocido("face_mask", "used face mask, surgical mask or respirator", ContextoObjeto.ResiduoSanitario, "♻️", "un tapabocas", Sanitario, "OTRO_RESIDUO", ObjetoVisible.ArticuloSanitario),

            new ObjetoConocido(VariosObjetos, "pile of several mixed items, none is the main one", ContextoObjeto.Desconocido, "🤔", "varios objetos", "Envíame una foto de uno solo, de cerca."),
            new ObjetoConocido(NoDistinguible, "blurry, dark, empty, or you cannot tell what it is", ContextoObjeto.Desconocido, "🤔", "nada que pueda distinguir", "Envíame una foto más clara."),
        };

        private static readonly Dictionary<string, ObjetoConocido> PorClave = Todos.ToDictionary(o => o.Clave, StringComparer.Ordinal);

        /// <summary>El objeto de esa clave, o null si la clave no existe: lo que el modelo invente no llega a ninguna parte.</summary>
        public static ObjetoConocido? Buscar(string? clave) => clave is not null && PorClave.TryGetValue(clave, out ObjetoConocido? objeto) ? objeto : null;
    }
}
