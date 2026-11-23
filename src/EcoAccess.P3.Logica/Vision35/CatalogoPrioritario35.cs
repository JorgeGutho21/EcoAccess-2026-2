using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>
    /// Ficha administrable de uno de los 35 materiales prioritarios de ECOACCESS (Resolución 2184 de 2019).
    /// Vincula el código único (V01–V10, B01–B13, N01–N12) con su caneca, código de material en Oracle,
    /// referencia opcional al catálogo fotográfico del prototipo y reglas de seguridad para demostración y visión.
    /// </summary>
    public sealed record MaterialPrioritario35(
        string Codigo,
        string Nombre,
        CategoriaMaterial Caneca,
        string CodigoOracle,
        string? IdCatalogoPrototipo,
        string Descripcion,
        string CondicionDisposicion,
        IReadOnlyList<string> Sinonimos,
        IReadOnlyList<string> ClavesVision,
        bool RequiereValidacionEstado = false,
        bool RequiereRutaAprovechamiento = false,
        bool EsSanitarioSimuladoEnStand = false,
        bool Activo = true)
    {
        /// <summary>Color oficial de la caneca en texto legible (VERDE, BLANCA o NEGRA).</summary>
        [JsonIgnore]
        public string NombreCaneca => Caneca switch
        {
            CategoriaMaterial.OrganicoAprovechable => "VERDE",
            CategoriaMaterial.Aprovechable => "BLANCA",
            CategoriaMaterial.NoAprovechable => "NEGRA",
            _ => "NO_APLICA",
        };
    }

    /// <summary>Resultado estructurado de una búsqueda escrita en el catálogo prioritario (distinta de la detección visual).</summary>
    public enum TipoResultadoBusquedaTexto
    {
        Encontrado = 1,
        FueraDeCatalogoEspecial = 2,
        ReutilizableNoResiduo = 3,
        SinCoincidencia = 4,
    }

    /// <summary>Respuesta de búsqueda textual por nombre o sinónimo sobre el catálogo de 35 residuos.</summary>
    public sealed record ResultadoBusquedaPrioritaria(
        TipoResultadoBusquedaTexto Tipo,
        IReadOnlyList<MaterialPrioritario35> Materiales,
        string Mensaje,
        bool EsBusquedaTextual = true);

    /// <summary>Contenedor serializable para versionamiento y administración del catálogo de 35 materiales.</summary>
    public sealed record DocumentoCatalogo35(
        string Version,
        string NormaReferencia,
        DateTime FechaActualizacionUtc,
        IReadOnlyList<MaterialPrioritario35> Materiales);

    /// <summary>
    /// Catálogo único, versionado y administrable de los 35 residuos prioritarios de ECOACCESS P3:
    /// 10 VERDE (V01–V10), 13 BLANCA (B01–B13) y 12 NEGRA (N01–N12).
    /// Conserva compatibilidad total con <see cref="CatalogoPrototipo"/> y los códigos de Oracle Cloud.
    /// </summary>
    public static class CatalogoPrioritario35
    {
        public const string Version = "2026.11.22-35";
        public const string EtiquetaFaltaImagen = "FALTA IMAGEN";
        public const string NormaReferencia = "Resolución 2184 de 2019 (Colombia) · Estación piloto Valledupar UPC";

        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>Los 35 materiales prioritarios exactos definidos por la directiva de ingeniería.</summary>
        public static IReadOnlyList<MaterialPrioritario35> Todos { get; } = new[]
        {
            // ═══════════════════════════════════════════════════════════════════════
            // CANECA VERDE — 10 TIPOS (V01–V10): Orgánicos aprovechables
            // ═══════════════════════════════════════════════════════════════════════
            M("V01", "Cáscara de banano", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "cascara-banano",
              "Resto orgánico biodegradable de banano.",
              "Depositar sin bolsa ni empaque plástico en la caneca VERDE.",
              new[] { "cascara de banano", "banano", "platano maduro", "guineo", "piel de banano" },
              new[] { "banana peel", "banana skin", "cascara de banano", "banana" }),

            M("V02", "Cáscara de limón", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "cascara-limon",
              "Resto orgánico biodegradable de cítrico (limón o lima).",
              "Depositar sin bolsa ni empaque en la caneca VERDE.",
              new[] { "cascara de limon", "cascara de lima", "limon exprimido", "piel de limon" },
              new[] { "lemon peel", "lime peel", "lemon rind", "cascara de limon" }),

            M("V03", "Cáscara de papa", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "cascara-papa",
              "Resto orgánico vegetal proveniente del pelado de papa.",
              "Depositar sin tierra excesiva ni bolsa en la caneca VERDE.",
              new[] { "cascara de papa", "cascaras de papa", "papa", "patata", "piel de papa" },
              new[] { "potato peel", "potato skin", "potato peels", "cascara de papa" }),

            M("V04", "Cáscara de aguacate", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "cascara-aguacate",
              "Resto orgánico biodegradable de aguacate.",
              "Depositar sin empaque ni etiqueta adhesiva en la caneca VERDE.",
              new[] { "cascara de aguacate", "aguacate", "palta", "piel de aguacate" },
              new[] { "avocado peel", "avocado skin", "avocado shell", "cascara de aguacate", "avocado" }),

            M("V05", "Medio limón", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "mitad-limon",
              "Fracción de limón o cítrico destinado a desecho orgánico.",
              "Escurrir exceso de líquido y depositar en la caneca VERDE.",
              new[] { "medio limon", "mitad de limon", "mitad de un limon", "limon cortado", "limon" },
              new[] { "half lemon", "cut lemon", "sliced lemon", "lemon half", "medio limon", "lemon" }),

            M("V06", "Restos de tomate crudo", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "restos-tomate",
              "Trozos o restos de tomate crudo no consumible.",
              "Depositar sin bolsa ni recipiente en la caneca VERDE.",
              new[] { "restos de tomate crudo", "restos de tomate", "tomate crudo", "tomate", "tomate danado" },
              new[] { "tomato scraps", "raw tomato", "cut tomato", "tomato slice", "tomato", "restos de tomate" }),

            M("V07", "Medio kiwi", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "mitad-kiwi",
              "Fracción de kiwi destinada a desecho orgánico.",
              "Depositar sin sticker ni empaque en la caneca VERDE.",
              new[] { "medio kiwi", "mitad de kiwi", "mitad de un kiwi", "kiwi", "cascara de kiwi" },
              new[] { "half kiwi", "cut kiwi", "kiwi", "kiwi peel", "medio kiwi" }),

            M("V08", "Hojas secas de árbol", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "hojas-secas",
              "Material vegetal seco de árboles y jardines.",
              "Depositar sin piedras, ramas gruesas ni bolsas en la caneca VERDE.",
              new[] { "hojas secas de arbol", "hojas secas", "hoja seca", "hojarasca", "hojas de arbol" },
              new[] { "dry leaves", "dried leaves", "dead leaves", "fallen leaves", "leaf", "leaves", "hojas secas" }),

            M("V09", "Restos de césped o poda vegetal", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "poda-jardin",
              "Recortes pequeños de pasto, césped o poda vegetal.",
              "Depositar solo material vegetal pequeño, sin tierra ni bolsas, en la caneca VERDE.",
              new[] { "restos de cesped", "poda vegetal", "cesped", "pasto", "poda de jardin", "ramas pequenas", "grama" },
              new[] { "grass clippings", "pruning waste", "cut grass", "twigs", "plant trimmings", "poda vegetal", "cesped" }),

            M("V10", "Cáscara de naranja", CategoriaMaterial.OrganicoAprovechable, "ORGANICO", "cascara-naranja",
              "Resto orgánico biodegradable de fruta cítrica (naranja).",
              "Depositar sin bolsa ni empaque en la caneca VERDE.",
              new[] { "cascara de naranja", "naranja", "piel de naranja", "cascaras de naranja" },
              new[] { "orange peel", "orange rind", "orange skin", "cascara de naranja", "orange" }),

            // ═══════════════════════════════════════════════════════════════════════
            // CANECA BLANCA — 13 TIPOS (B01–B13): Aprovechables limpios y secos
            // ═══════════════════════════════════════════════════════════════════════
            M("B01", "Botella plástica pequeña de agua, limpia y vacía", CategoriaMaterial.Aprovechable, "PET", "botella-agua",
              "Envase PET transparente pequeño de agua, limpio y sin líquido.",
              "Debe estar vacía, limpia y seca. Si contiene líquido o está sucia, requiere confirmación.",
              new[] { "botella plastica pequena de agua", "botella de agua", "botella plastica limpia", "botella pet agua", "botella plastica", "botella" },
              new[] { "plastic water bottle", "water bottle", "clear plastic bottle", "pet bottle", "plastic bottle", "botella de agua" },
              requiereValidacionEstado: true),

            M("B02", "Botella plástica pequeña de gaseosa, limpia y vacía", CategoriaMaterial.Aprovechable, "PET", "botella-gaseosa",
              "Envase PET pequeño de gaseosa o refresco, limpio y vacío.",
              "Vaciar completamente, enjuagar si tiene azúcar y depositar seca en la caneca BLANCA.",
              new[] { "botella plastica pequena de gaseosa", "botella de gaseosa", "botella refresco", "botella soda", "gaseosa pet" },
              new[] { "plastic soda bottle", "soda bottle", "soft drink bottle", "cola bottle", "botella de gaseosa" },
              requiereValidacionEstado: true),

            M("B03", "Tapa plástica aprovechable", CategoriaMaterial.Aprovechable, "PLASTICO_RIGIDO", "envase-plastico",
              "Tapa de polipropileno o HDPE de botella o envase, limpia.",
              "Depositar limpia y seca en la caneca BLANCA.",
              new[] { "tapa plastica aprovechable", "tapa plastica", "tapa de botella", "tapita plastica", "tapa" },
              new[] { "plastic cap", "bottle cap", "plastic lid", "tapa plastica" },
              requiereValidacionEstado: true),

            M("B04", "Vaso plástico limpio y seco", CategoriaMaterial.Aprovechable, "PLASTICO_RIGIDO", "vasos-plasticos",
              "Vaso plástico sin restos de bebida, grasa ni alimentos.",
              "Solo va a la BLANCA si está limpio y seco; si está contaminado corresponde a la NEGRA (N09).",
              new[] { "vaso plastico limpio y seco", "vaso plastico limpio", "vaso plastico", "vasos plasticos" },
              new[] { "clean plastic cup", "clear plastic cup", "plastic cup", "vaso plastico limpio" },
              requiereValidacionEstado: true),

            M("B05", "Envase plástico pequeño, limpio y vacío", CategoriaMaterial.Aprovechable, "PLASTICO_RIGIDO", "envase-plastico",
              "Recipiente o tarro plástico pequeño rígido, limpio y vacío (yogur, crema, champú).",
              "Confirmar que está vacío, enjuagado y libre de sustancias químicas peligrosas.",
              new[] { "envase plastico pequeno", "envase plastico limpio", "tarro plastico", "recipiente plastico limpio", "envase de yogur limpio" },
              new[] { "plastic container", "plastic tub", "plastic jar", "yogurt cup", "clean plastic container", "envase plastico" },
              requiereValidacionEstado: true),

            M("B06", "Frasco de vidrio limpio", CategoriaMaterial.Aprovechable, "VIDRIO", "vidrio",
              "Frasco de vidrio entero, vacío y limpio (mermelada, compota, salsa).",
              "Debe estar entero, vacío y limpio; el vidrio roto es cortopunzante y no va en las canecas.",
              new[] { "frasco de vidrio limpio", "frasco de vidrio", "tarro de vidrio", "frasco compota", "frasco mermelada" },
              new[] { "glass jar", "clean glass jar", "empty glass jar", "frasco de vidrio" },
              requiereValidacionEstado: true),

            M("B07", "Botella pequeña de vidrio, vacía", CategoriaMaterial.Aprovechable, "VIDRIO", "vidrio",
              "Botella de vidrio pequeña, entera, limpia y vacía.",
              "Entregar entera y vacía en la caneca BLANCA. No introducir vidrio roto.",
              new[] { "botella pequena de vidrio", "botella de vidrio vacia", "botella de vidrio", "envase de vidrio" },
              new[] { "glass bottle", "empty glass bottle", "small glass bottle", "botella de vidrio" },
              requiereValidacionEstado: true),

            M("B08", "Bola de papel de cuaderno limpio", CategoriaMaterial.Aprovechable, "PAPEL", "papel-cuaderno",
              "Papel de cuaderno u oficina arrugado o entero, limpio y seco.",
              "Solo si está seco y sin grasa ni comida; el papel contaminado va en la NEGRA.",
              new[] { "bola de papel de cuaderno limpio", "bola de papel", "papel de cuaderno", "papel limpio", "hoja de cuaderno", "papel arrugado" },
              new[] { "crumpled paper", "paper ball", "clean paper", "notebook paper", "sheet of paper", "paper", "papel limpio" },
              requiereValidacionEstado: true),

            M("B09", "Cartón pequeño limpio y seco", CategoriaMaterial.Aprovechable, "CARTON", "carton-limpio",
              "Fragmento o caja pequeña de cartón limpio y seco, sin grasa.",
              "Debe estar seco y libre de grasa; el cartón engrasado corresponde a la NEGRA (N03).",
              new[] { "carton pequeno limpio y seco", "carton limpio", "carton seco", "caja de carton limpia", "carton" },
              new[] { "clean cardboard", "cardboard box", "cardboard", "corrugated cardboard", "carton limpio" },
              requiereValidacionEstado: true),

            M("B10", "Caja de jugo HIT o similar, vacía y escurrida", CategoriaMaterial.Aprovechable, "MULTICAPA", "caja-jugo",
              "Envase multicapa pequeño tipo Tetra Pak de jugo, vacío y escurrido.",
              "Vaciar, escurrir y aplastar antes de depositar en la caneca BLANCA.",
              new[] { "caja de jugo hit", "caja de jugo vacia", "caja de jugo", "tetrapak de jugo", "jugo hit", "envase multicapa jugo" },
              new[] { "juice box", "juice carton", "drink carton", "tetra pak", "caja de jugo" },
              requiereValidacionEstado: true),

            M("B11", "Caja de leche multicapa, vacía y escurrida", CategoriaMaterial.Aprovechable, "MULTICAPA", "caja-leche",
              "Envase multicapa de leche, vacío, escurrido y limpio.",
              "Escurrir y enjuagar ligeramente antes de depositar en la caneca BLANCA.",
              new[] { "caja de leche multicapa", "caja de leche vacia", "caja de leche", "tetrapak de leche", "envase de leche" },
              new[] { "milk carton", "milk box", "tetra pak milk", "caja de leche" },
              requiereValidacionEstado: true),

            M("B12", "Lata pequeña de bebida, limpia y vacía", CategoriaMaterial.Aprovechable, "LATA_ALUMINIO", "lata-aluminio",
              "Lata metálica de aluminio de bebida (gaseosa, té, cerveza), vacía y limpia.",
              "Vaciar completamente y verificar que no sea un aerosol presurizado.",
              new[] { "lata pequena de bebida", "lata limpia", "lata de aluminio", "lata de gaseosa", "lata vacia", "lata" },
              new[] { "aluminum can", "soda can", "drink can", "beverage can", "metal can", "can", "lata de bebida" },
              requiereValidacionEstado: true),

            M("B13", "Película plástica limpia aceptada por la ruta de aprovechamiento", CategoriaMaterial.Aprovechable, "BOLSA_PLASTICA", null,
              "Bolsa o película plástica flexible limpia y seca, condicionada a la disponibilidad de la ruta local.",
              "Solo va a la BLANCA si está completamente limpia, seca y aceptada por la ruta de aprovechamiento local; si tiene grasa o comida va a la NEGRA.",
              new[] { "pelicula plastica limpia", "film plastico limpio", "bolsa plastica limpia", "plastico flexible limpio", "vinipel limpio" },
              new[] { "clean plastic film", "plastic film", "clean plastic bag", "clear plastic wrap", "pelicula plastica limpia" },
              requiereValidacionEstado: true,
              requiereRutaAprovechamiento: true),

            // ═══════════════════════════════════════════════════════════════════════
            // CANECA NEGRA — 12 TIPOS (N01–N12): No aprovechables
            // ═══════════════════════════════════════════════════════════════════════
            M("N01", "Servilleta usada o contaminada", CategoriaMaterial.NoAprovechable, "SERVILLETA_USADA", "servilletas-usadas",
              "Servilleta de papel usada o impregnada de grasa, humedad o alimentos.",
              "Depositar en la caneca NEGRA; no es reciclable aunque sea de papel.",
              new[] { "servilleta usada", "servilleta contaminada", "servilleta sucia", "servilletas usadas", "panuelo usado" },
              new[] { "used napkin", "dirty napkin", "soiled napkin", "used tissue", "crumpled napkin", "napkin", "servilleta usada" }),

            M("N02", "Papel higiénico usado", CategoriaMaterial.NoAprovechable, "SERVILLETA_USADA", "papel-higienico",
              "Residuo sanitario doméstico no aprovechable. En pruebas de stand usar representación limpia artificial.",
              "Depositar en la caneca NEGRA. Para demostraciones físicas usar únicamente simulación segura y limpia.",
              new[] { "papel higienico usado", "papel higienico", "papel de bano usado" },
              new[] { "used toilet paper", "toilet paper", "dirty toilet paper", "papel higienico usado" },
              esSanitarioSimuladoEnStand: true),

            M("N03", "Cartón impregnado con grasa de comida", CategoriaMaterial.NoAprovechable, "SERVILLETA_USADA", "papel-contaminado",
              "Cartón manchado con aceite, grasa o restos de alimentos (ej. caja de pizza engrasada).",
              "El cartón con grasa pierde aprovechabilidad y va a la caneca NEGRA.",
              new[] { "carton impregnado con grasa", "carton con grasa", "caja de pizza con grasa", "carton sucio", "carton contaminado" },
              new[] { "greasy cardboard", "pizza box", "greasy pizza box", "stained cardboard", "oily cardboard", "dirty cardboard", "carton con grasa" }),

            M("N04", "Colilla de cigarrillo completamente apagada", CategoriaMaterial.NoAprovechable, "COLILLA", "colilla",
              "Filtro y resto de cigarrillo totalmente apagado y frío.",
              "Verificar que esté completamente apagada antes de depositar en la caneca NEGRA.",
              new[] { "colilla de cigarrillo", "colilla apagada", "colilla", "pucho" },
              new[] { "cigarette butt", "cigarette", "extinguished cigarette", "colilla de cigarrillo" }),

            M("N05", "Condón usado, correctamente envuelto", CategoriaMaterial.NoAprovechable, "CONDON", "condon-usado",
              "Residuo sanitario doméstico envuelto. En pruebas de stand usar representación artificial limpia y segura.",
              "Depositar envuelto en la caneca NEGRA. En el stand usar solo modelo artificial seguro.",
              new[] { "condon usado correctamente envuelto", "condon usado", "condon", "preservativo usado" },
              new[] { "used condom", "condom", "wrapped condom", "condon usado" },
              esSanitarioSimuladoEnStand: true),

            M("N06", "Pitillo usado no aprovechable", CategoriaMaterial.NoAprovechable, "PLASTICO_SIN_ID", "cubiertos-desechables",
              "Pitillo o pajilla plástica usada de un solo uso.",
              "Depositar en la caneca NEGRA.",
              new[] { "pitillo usado no aprovechable", "pitillo usado", "pitillo", "pajilla usada", "popote usado" },
              new[] { "used straw", "plastic straw", "drinking straw", "straw", "pitillo usado" }),

            M("N07", "Bandeja pequeña de icopor contaminada", CategoriaMaterial.NoAprovechable, "ICOPOR_CONTAMINADO", "icopor-sucio",
              "Bandeja o empaque de poliestireno expandido (icopor) con restos o grasa.",
              "Retirar sobras sólidas de comida y depositar el icopor en la caneca NEGRA.",
              new[] { "bandeja pequena de icopor contaminada", "bandeja de icopor sucia", "bandeja de icopor", "icopor contaminado", "icopor sucio", "plato de icopor" },
              new[] { "styrofoam tray", "dirty styrofoam", "foam tray", "foam container", "styrofoam", "icopor sucio", "bandeja de icopor" }),

            M("N08", "Envoltura contaminada de alimentos", CategoriaMaterial.NoAprovechable, "PLASTICO_SIN_ID", "envolturas",
              "Empaque o envoltura metalizada/plástica de mecato o alimentos con migas o grasa.",
              "Depositar en la caneca NEGRA.",
              new[] { "envoltura contaminada de alimentos", "envoltura contaminada", "envoltura de alimentos", "paquete de papas", "empaque metalizado", "envoltura de mecato" },
              new[] { "snack wrapper", "food wrapper", "candy wrapper", "chips bag", "dirty wrapper", "foil wrapper", "envoltura contaminada" }),

            M("N09", "Vaso plástico contaminado no aprovechable", CategoriaMaterial.NoAprovechable, "PLASTICO_SIN_ID", "vaso-contaminado",
              "Vaso desechable plástico o plastificado con restos de bebida, café o comida.",
              "Al estar contaminado no es aprovechable; depositar en la caneca NEGRA.",
              new[] { "vaso plastico contaminado", "vaso contaminado", "vaso desechable sucio", "vaso sucio", "vaso de cafe usado" },
              new[] { "dirty plastic cup", "used plastic cup", "stained cup", "dirty disposable cup", "contaminated cup", "vaso contaminado" }),

            M("N10", "Chicle usado", CategoriaMaterial.NoAprovechable, "OTRO_RESIDUO", null,
              "Goma de mascar usada, preferiblemente envuelta en papel.",
              "Envolver en un trozo pequeño de papel y depositar en la caneca NEGRA.",
              new[] { "chicle usado", "chicle", "goma de mascar usada", "goma de mascar" },
              new[] { "chewing gum", "used gum", "bubble gum", "gum", "chicle usado" }),

            M("N11", "Toallita húmeda usada", CategoriaMaterial.NoAprovechable, "TOALLA_HIGIENICA", null,
              "Toallita húmeda desechable usada. En pruebas físicas usar representación limpia artificial.",
              "Depositar en la caneca NEGRA; nunca arrojar al inodoro ni a la caneca blanca.",
              new[] { "toallita humeda usada", "toallita humeda", "panito humedo usado", "panito humedo", "wet wipe" },
              new[] { "wet wipe", "used wet wipe", "baby wipe", "sanitizing wipe", "toallita humeda" },
              esSanitarioSimuladoEnStand: true),

            M("N12", "Tapabocas desechable usado", CategoriaMaterial.NoAprovechable, "OTRO_RESIDUO", null,
              "Mascarilla o tapabocas desechable usado. En pruebas físicas usar muestra limpia sin uso real.",
              "Cortar las tiras si es posible y depositar en la caneca NEGRA.",
              new[] { "tapabocas desechable usado", "tapabocas usado", "tapabocas", "mascarilla desechable", "mascarilla usada" },
              new[] { "face mask", "surgical mask", "used face mask", "disposable mask", "mask", "tapabocas usado" },
              esSanitarioSimuladoEnStand: true),
        };

        private static readonly Dictionary<string, MaterialPrioritario35> PorCodigo =
            Todos.ToDictionary(m => m.Codigo, StringComparer.OrdinalIgnoreCase);

        /// <summary>Objetos reutilizables que NO deben clasificarse automáticamente como basura (Sección 4).</summary>
        private static readonly string[] PatronesReutilizables =
        {
            "cubeta de hielo", "cubeta plastica para hielo", "bandeja de hielo", "molde de hielo", "ice cube tray", "ice tray",
            "termo reutilizable", "botella termica", "lonchera reutilizable", "tupperware reutilizable", "reusable container",
        };

        /// <summary>Residuos peligrosos o especiales que nunca van en las tres canecas.</summary>
        private static readonly string[] PatronesEspeciales =
        {
            "pila", "pilas", "bateria", "medicamento", "pastilla", "blister", "aceite usado", "jeringa", "aguja",
            "bombillo", "celular", "cargador", "aerosol", "insecticida", "cloro", "pintura", "vidrio roto", "cortopunzante",
        };

        private static readonly HashSet<string> PalabrasVacias = new(StringComparer.Ordinal)
        {
            "de", "del", "la", "las", "el", "los", "lo", "con", "y", "e", "en", "un", "una", "unos", "unas", "al", "a",
            "para", "por", "que", "o", "u", "su", "sus", "tipo", "similar",
        };

        public static MaterialPrioritario35? BuscarPorCodigo(string? codigo) =>
            !string.IsNullOrWhiteSpace(codigo) && PorCodigo.TryGetValue(codigo!.Trim(), out var material) ? material : null;

        public static IReadOnlyList<MaterialPrioritario35> DeCaneca(CategoriaMaterial caneca) =>
            Todos.Where(m => m.Caneca == caneca && m.Activo).ToList();

        /// <summary>Imágenes válidas disponibles en el catálogo del prototipo para un material prioritario (vacío si aún no tiene foto).</summary>
                /// <summary>Devuelve "DISPONIBLE" o "FALTA IMAGEN" según la cobertura visual real del material.</summary>
        public static string EstadoImagenDe(MaterialPrioritario35 material) =>
            ImagenesValidasDe(material).Count > 0 ? "DISPONIBLE" : EtiquetaFaltaImagen;

        public static IReadOnlyList<string> ImagenesValidasDe(MaterialPrioritario35 material)
        {
            if (material is null || string.IsNullOrWhiteSpace(material.IdCatalogoPrototipo))
                return Array.Empty<string>();
            ResiduoPrototipo? proto = CatalogoPrototipo.Buscar(material.IdCatalogoPrototipo);
            return proto is null ? Array.Empty<string>() : CatalogoPrototipo.Imagenes(proto);
        }

        /// <summary>
        /// Búsqueda escrita por nombre, código (V01–N12) o sinónimo.
        /// Diferencia explícitamente objetos reutilizables (como una cubeta plástica para hielo) y residuos especiales.
        /// </summary>
        public static ResultadoBusquedaPrioritaria BuscarPorTexto(string? consulta)
        {
            string norm = CatalogoPrototipo.Normalizar(consulta);
            if (norm.Length == 0)
            {
                return new ResultadoBusquedaPrioritaria(
                    TipoResultadoBusquedaTexto.Encontrado,
                    Todos,
                    $"Catálogo prioritario v{Version}: {Todos.Count} residuos (10 verdes, 13 blancos, 12 negros).");
            }

            if (PorCodigo.TryGetValue(norm.ToUpperInvariant(), out var porCodigo))
            {
                return new ResultadoBusquedaPrioritaria(
                    TipoResultadoBusquedaTexto.Encontrado,
                    new[] { porCodigo },
                    $"Búsqueda escrita: {porCodigo.Codigo} — {porCodigo.Nombre} ({porCodigo.NombreCaneca}).");
            }

            if (PatronesReutilizables.Any(p => norm.Contains(CatalogoPrototipo.Normalizar(p))))
            {
                return new ResultadoBusquedaPrioritaria(
                    TipoResultadoBusquedaTexto.ReutilizableNoResiduo,
                    Array.Empty<MaterialPrioritario35>(),
                    "Este objeto parece reutilizable (por ejemplo, una cubeta plástica para hielo aún útil) y no debe clasificarse automáticamente como basura. Si está roto y ya no sirve, confirma su disposición manualmente.");
            }

            string[] tokens = norm.Split(' ').Where(t => !PalabrasVacias.Contains(t)).DefaultIfEmpty(norm).ToArray();
            var exactos = Todos.Where(m => ClavesBusqueda(m).Any(c => c == norm)).ToList();
            if (exactos.Count > 0)
            {
                return new ResultadoBusquedaPrioritaria(
                    TipoResultadoBusquedaTexto.Encontrado,
                    exactos,
                    exactos.Count == 1
                        ? $"Búsqueda escrita: {exactos[0].Codigo} — {exactos[0].Nombre} → Caneca {exactos[0].NombreCaneca}."
                        : $"Búsqueda escrita: {exactos.Count} materiales coincidentes.");
            }

            if (PatronesEspeciales.Any(p => norm.Contains(CatalogoPrototipo.Normalizar(p))))
            {
                return new ResultadoBusquedaPrioritaria(
                    TipoResultadoBusquedaTexto.FueraDeCatalogoEspecial,
                    Array.Empty<MaterialPrioritario35>(),
                    CatalogoPrototipo.MensajeFueraDeCatalogo);
            }

            var parciales = Todos
                .Select(m => (Material: m, Puntaje: CalcularPuntaje(m, tokens)))
                .Where(x => x.Puntaje > 0)
                .OrderByDescending(x => x.Puntaje)
                .ThenBy(x => x.Material.Codigo, StringComparer.Ordinal)
                .Select(x => x.Material)
                .ToList();

            if (parciales.Count > 0)
            {
                return new ResultadoBusquedaPrioritaria(
                    TipoResultadoBusquedaTexto.Encontrado,
                    parciales,
                    parciales.Count == 1
                        ? $"Búsqueda escrita: {parciales[0].Codigo} — {parciales[0].Nombre} → Caneca {parciales[0].NombreCaneca}."
                        : $"Búsqueda escrita: {parciales.Count} residuos encontrados. Verifica el estado real del residuo.");
            }

            return new ResultadoBusquedaPrioritaria(
                TipoResultadoBusquedaTexto.SinCoincidencia,
                Array.Empty<MaterialPrioritario35>(),
                "No se encontró ese residuo en el catálogo prioritario de 35 materiales. Usa otro término o clasificación manual.");
        }

        /// <summary>Exporta el catálogo versionado a JSON legible para auditoría, respaldo o administración.</summary>
        public static string ExportarJson()
        {
            var documento = new DocumentoCatalogo35(Version, NormaReferencia, DateTime.UtcNow, Todos);
            return JsonSerializer.Serialize(documento, OpcionesJson);
        }

        /// <summary>Carga y valida un documento JSON del catálogo administrable (rechaza códigos duplicados o vacíos).</summary>
        public static DocumentoCatalogo35 CargarDesdeJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("El JSON del catálogo no puede estar vacío.");
            DocumentoCatalogo35? doc = JsonSerializer.Deserialize<DocumentoCatalogo35>(json, OpcionesJson);
            if (doc is null || string.IsNullOrWhiteSpace(doc.Version) || doc.Materiales is null || doc.Materiales.Count == 0)
                throw new InvalidDataException("El documento del catálogo está incompleto.");
            var codigos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (MaterialPrioritario35 m in doc.Materiales)
            {
                if (string.IsNullOrWhiteSpace(m.Codigo) || string.IsNullOrWhiteSpace(m.Nombre) || string.IsNullOrWhiteSpace(m.CodigoOracle))
                    throw new InvalidDataException($"Entrada de catálogo inválida: {m.Codigo}.");
                if (!codigos.Add(m.Codigo))
                    throw new InvalidDataException($"Código duplicado en el catálogo: {m.Codigo}.");
            }
            return doc;
        }

        private static MaterialPrioritario35 M(
            string codigo,
            string nombre,
            CategoriaMaterial caneca,
            string codigoOracle,
            string? idPrototipo,
            string descripcion,
            string condicion,
            string[] sinonimos,
            string[] clavesVision,
            bool requiereValidacionEstado = false,
            bool requiereRutaAprovechamiento = false,
            bool esSanitarioSimuladoEnStand = false) =>
            new(codigo, nombre, caneca, codigoOracle, idPrototipo, descripcion, condicion, sinonimos, clavesVision,
                requiereValidacionEstado, requiereRutaAprovechamiento, esSanitarioSimuladoEnStand, Activo: true);

        private static IEnumerable<string> ClavesBusqueda(MaterialPrioritario35 m) =>
            new[] { CatalogoPrototipo.Normalizar(m.Nombre) }
                .Concat(m.Sinonimos.Select(CatalogoPrototipo.Normalizar))
                .Concat(m.ClavesVision.Select(CatalogoPrototipo.Normalizar));

        private static int CalcularPuntaje(MaterialPrioritario35 m, string[] tokens)
        {
            int mejor = 0;
            foreach (string clave in ClavesBusqueda(m))
            {
                string[] partes = clave.Split(' ');
                int coincidencias = tokens.Count(t =>
                    partes.Any(p => p.StartsWith(t, StringComparison.Ordinal) ||
                                    (t.Length >= 4 && p.StartsWith(t.TrimEnd('s'), StringComparison.Ordinal))));
                if (coincidencias == tokens.Length)
                    mejor = Math.Max(mejor, coincidencias * 10 + (partes.Length == tokens.Length ? 5 : 0));
                else if (tokens.Length >= 2 && coincidencias >= 2 && coincidencias * 2 >= tokens.Length)
                    mejor = Math.Max(mejor, coincidencias * 4);
            }
            return mejor;
        }
    }
}
