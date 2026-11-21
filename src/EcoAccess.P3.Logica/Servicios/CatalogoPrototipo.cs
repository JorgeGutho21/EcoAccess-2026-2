using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Un residuo del stand de ExpoSoftware: identificador estable, nombre, caneca, descripción, instrucciones, imagen,
    /// sinónimos de búsqueda, código del material en Oracle con el que se registra el depósito y disponibilidad.
    /// </summary>
    public sealed record ResiduoPrototipo(
        string Id,
        string Nombre,
        CategoriaMaterial Caneca,
        string Descripcion,
        string Instrucciones,
        string Imagen,
        string CodigoMaterial,
        IReadOnlyList<string> Sinonimos,
        bool Disponible = true);

    /// <summary>Resultado de buscar un residuo en el catálogo del prototipo.</summary>
    public sealed record BusquedaPrototipo(IReadOnlyList<ResiduoPrototipo> Residuos, bool FueraDeCatalogo, string Mensaje);

    /// <summary>
    /// Residuos DEPOSITABLES del piloto: los 15 del stand de ExpoSoftware (con ilustración) más la ampliación del
    /// 20/11/2026 (sin ilustración), repartidos en las tres canecas de la Resolución 2184 de 2019. Cada uno se registra
    /// con un material que ya existe en los scripts de Oracle con la categoría de su caneca (ORGANICO, PET, PAPEL, CARTON,
    /// VIDRIO, LATA_ALUMINIO, MULTICAPA, PLASTICO_RIGIDO, SERVILLETA_USADA, PLASTICO_SIN_ID, ICOPOR_CONTAMINADO, CONDON,
    /// TOALLA_HIGIENICA, TAMPON, PANAL, COLILLA), así que no hace falta migrar la base; Oracle decide en cada depósito si
    /// la caneca está en servicio y lo admite. No hay cuarta categoría: lo especial o peligroso no se asigna a ninguna
    /// caneca (<see cref="CatalogoResiduos"/> lo orienta). Para ampliar basta añadir una entrada; la imagen es opcional.
    /// </summary>
    public static class CatalogoPrototipo
    {
        public const string Verde = "verde", Blanca = "blanca", Negra = "negra";

        /// <summary>
        /// Todos los residuos depositables. Los que no tienen ilustración propia toman como imagen principal la primera
        /// fotografía integrada de su residuo (<see cref="CatalogoFotos"/>); si no hay ninguna, la interfaz usa el icono.
        /// </summary>
        public static IReadOnlyList<ResiduoPrototipo> Todos { get; } = ConFotos(new[]
        {
            // Caneca verde: orgánicos aprovechables (6).
            R("cascara-banano", "Cáscara de banano", CategoriaMaterial.OrganicoAprovechable, "Resto orgánico de fruta.",
                "Deposítala sin bolsa ni empaque en la caneca verde.", "verde.banano", "ORGANICO",
                "banano", "cascara de banano", "cascara de platano", "platano", "guineo", "cascara"),
            R("cascara-naranja", "Cáscara de naranja", CategoriaMaterial.OrganicoAprovechable, "Resto orgánico de fruta cítrica.",
                "Deposítala sin bolsa ni empaque en la caneca verde.", "verde.naranja", "ORGANICO",
                "naranja", "cascara de naranja", "piel de naranja"),
            R("hojas-secas", "Hojas secas de árboles", CategoriaMaterial.OrganicoAprovechable, "Residuo vegetal de jardín.",
                "Deposítalas sin ramas grandes, tierra ni bolsa en la caneca verde.", "verde.hojas", "ORGANICO",
                "hojas", "hojas secas", "hoja seca", "hojarasca"),
            R("mitad-kiwi", "Mitad de un kiwi", CategoriaMaterial.OrganicoAprovechable, "Resto orgánico de fruta.",
                "Deposítala sin empaque en la caneca verde.", "verde.kiwi", "ORGANICO",
                "kiwi", "mitad de kiwi", "fruta"),
            R("cascara-mandarina", "Cáscara de mandarina", CategoriaMaterial.OrganicoAprovechable, "Resto orgánico de fruta cítrica.",
                "Deposítala sin bolsa ni empaque en la caneca verde.", "verde.mandarina", "ORGANICO",
                "mandarina", "cascara de mandarina"),
            R("mitad-limon", "Mitad de un limón", CategoriaMaterial.OrganicoAprovechable, "Resto orgánico de fruta cítrica.",
                "Deposítala sin empaque en la caneca verde.", "verde.limon", "ORGANICO",
                "limon", "mitad de limon"),

            // Caneca blanca: aprovechables limpios y secos (5).
            R("botella-agua", "Botella plástica pequeña de agua", CategoriaMaterial.Aprovechable, "Envase PET reciclable.",
                "Vacíala por completo, tápala y deposítala limpia y seca en la caneca blanca.", "blanca.botella-agua", "PET",
                "botella", "botella de agua", "botella plastica", "pet", "agua", "envase"),
            R("botella-gaseosa", "Botella plástica pequeña de gaseosa", CategoriaMaterial.Aprovechable, "Envase PET reciclable.",
                "Vacíala por completo, tápala y deposítala limpia y seca en la caneca blanca.", "blanca.botella-gaseosa", "PET",
                "gaseosa", "botella de gaseosa", "refresco", "soda", "pet"),
            R("papel-cuaderno", "Bola de papel de cuaderno arrugado", CategoriaMaterial.Aprovechable, "Papel limpio y seco.",
                "Solo si está limpio y seco: deposítalo en la caneca blanca.", "blanca.papel", "PAPEL",
                "papel", "papel arrugado", "hoja arrugada", "hoja de cuaderno", "cuaderno", "bola de papel"),
            R("caja-jugo", "Caja de jugo tipo Hit, vacía y limpia", CategoriaMaterial.Aprovechable, "Envase multicapa (cartón, plástico y aluminio).",
                "Vacíala, aplástala y deposítala limpia en la caneca blanca.", "blanca.caja-jugo", "MULTICAPA",
                "caja de jugo", "jugo", "hit", "tetra pak", "tetrapak", "multicapa", "caja"),
            R("vasos-plasticos", "Vasos plásticos reciclables, limpios y secos", CategoriaMaterial.Aprovechable, "Envase plástico rígido.",
                "Solo limpios y secos, sin restos de bebida: deposítalos en la caneca blanca.", "blanca.vasos", "PLASTICO_RIGIDO",
                "vaso", "vasos", "vaso plastico", "vasos plasticos"),

            // Caneca negra: no aprovechables (4).
            R("papel-higienico", "Papel higiénico usado", CategoriaMaterial.NoAprovechable, "Residuo sanitario no aprovechable.",
                "Deposítalo en la caneca negra; no va a la blanca aunque sea papel.", "negra.papel-higienico", "SERVILLETA_USADA",
                "papel higienico", "higienico", "papel de bano"),
            R("servilletas-usadas", "Servilletas usadas", CategoriaMaterial.NoAprovechable, "Papel contaminado con grasa o comida.",
                "Deposítalas en la caneca negra; no van a la blanca.", "negra.servilletas", "SERVILLETA_USADA",
                "servilleta", "servilletas", "servilleta sucia", "servilleta usada", "pañuelo"),
            R("vaso-contaminado", "Vaso desechable contaminado con comida", CategoriaMaterial.NoAprovechable, "Desechable sucio, no aprovechable.",
                "Con restos de comida o bebida va a la caneca negra.", "negra.vaso-contaminado", "PLASTICO_SIN_ID",
                "vaso desechable", "vaso sucio", "vaso contaminado", "vaso de cafe", "desechable"),
            R("plato-icopor", "Plato de icopor con restos de comida", CategoriaMaterial.NoAprovechable, "Icopor contaminado, no aprovechable.",
                "Retira los restos de comida si puedes y deposita el plato en la caneca negra.", "negra.plato-icopor", "ICOPOR_CONTAMINADO",
                "icopor", "plato de icopor", "plato desechable", "poliestireno", "plato"),

            // ── Ampliación del 20/11/2026 (sin ilustración: la interfaz usa el icono del material). Solo códigos que ya
            //    existen en los scripts de Oracle (01_catalogos, 03_catalogo_ambiental_piloto) con la categoría de su caneca.
            //    Oracle sigue decidiendo si la caneca concreta está en servicio y admite el material.

            // Verde: ORGANICO (categoría 2).
            R("restos-fruta", "Restos de frutas", CategoriaMaterial.OrganicoAprovechable, "Pulpa, corazones y trozos de fruta.",
                "Sin bolsa, sin empaque ni stickers: deposítalos en la caneca verde.", "", "ORGANICO",
                "fruta", "frutas", "restos de fruta", "manzana", "pera", "mango", "papaya", "patilla", "sandia", "pina", "guayaba", "corazon de manzana", "fruta danada"),
            R("restos-verdura", "Restos de verduras y hortalizas", CategoriaMaterial.OrganicoAprovechable, "Residuos vegetales de cocina.",
                "Sin bolsa ni empaque: deposítalos en la caneca verde.", "", "ORGANICO",
                "verdura", "verduras", "hortaliza", "residuos vegetales", "cebolla", "tomate", "lechuga", "zanahoria", "repollo", "cilantro", "cascara de papa", "papa"),
            R("cascaras-alimentos", "Cáscaras de alimentos (huevo, papa, yuca, aguacate)", CategoriaMaterial.OrganicoAprovechable, "Cáscaras orgánicas de cocina.",
                "Sin bolsa ni empaque: deposítalas en la caneca verde.", "", "ORGANICO",
                "cascaras", "cascara de huevo", "cascaras de huevo", "cascara de yuca", "cascara de aguacate", "cascara de mango", "cascara de pina", "cascara de patilla"),
            R("restos-comida", "Restos de comida sin empaque", CategoriaMaterial.OrganicoAprovechable, "Sobras de alimentos preparados.",
                "Escurre los líquidos y deposita solo la comida, sin la coca, el plato ni la bolsa, en la caneca verde.", "", "ORGANICO",
                "comida", "sobras", "restos de comida", "arroz", "frijoles", "lentejas", "pasta", "arepa", "almuerzo", "desperdicio de comida"),
            R("poda-jardin", "Restos de poda, pasto y jardín", CategoriaMaterial.OrganicoAprovechable, "Residuos de jardín y poda pequeña.",
                "Ramas pequeñas, pasto y flores sin tierra, piedras ni bolsa: caneca verde.", "", "ORGANICO",
                "poda", "restos de poda", "pasto", "cesped", "grama", "ramas", "rama", "flores marchitas", "residuos de jardin", "maleza"),
            R("cafe-te", "Borra de café y bolsitas de té o aromática", CategoriaMaterial.OrganicoAprovechable, "Residuo orgánico de bebidas.",
                "Escurre el líquido y deposítalo en la caneca verde (sin la cápsula plástica).", "", "ORGANICO",
                "borra de cafe", "cafe", "cuncho", "te", "aromatica", "bolsita de te", "filtro de cafe"),
            R("semillas-tusas", "Semillas, pepas y tusas de mazorca", CategoriaMaterial.OrganicoAprovechable, "Partes duras de frutas y vegetales.",
                "Deposítalas sin bolsa en la caneca verde.", "", "ORGANICO",
                "semilla", "semillas", "pepa", "pepas", "pepa de mango", "pepa de aguacate", "tusa", "tusa de mazorca", "mazorca"),

            // Blanca: aprovechables (categoría 1).
            R("botella-pet", "Botella plástica PET de otras bebidas", CategoriaMaterial.Aprovechable, "Envase PET reciclable (jugo, té, bebida hidratante).",
                "Vacíala, tápala y entrégala limpia y seca en la caneca blanca.", "", "PET",
                "botella pet", "botella de jugo", "botella de te", "envase de agua", "plastico de bebida", "envase de bebida", "botella de suero"),
            R("envase-plastico", "Envase plástico rígido limpio (yogur, champú o detergente vacío)", CategoriaMaterial.Aprovechable, "Plástico rígido aprovechable.",
                "Solo vacío, enjuagado y seco, con su tapa: caneca blanca. Con producto dentro no se recicla.", "", "PLASTICO_RIGIDO",
                "envase plastico", "envases plasticos", "tarro plastico", "frasco plastico", "envase de yogur", "coca plastica", "recipiente plastico", "tapa plastica", "tapas", "envase de champu"),
            R("carton-limpio", "Cartón limpio y seco", CategoriaMaterial.Aprovechable, "Cajas y empaques de cartón sin grasa.",
                "Aplástalo y entrégalo seco y sin restos de comida en la caneca blanca. Con grasa va a la negra.", "", "CARTON",
                "carton", "caja de carton", "cajas", "empaque de carton", "cubeta de huevos", "tubo de papel higienico", "caja de cereal", "cartulina"),
            R("papel-limpio", "Papel limpio y seco (hojas, fotocopias, periódico)", CategoriaMaterial.Aprovechable, "Papel aprovechable.",
                "Seco, sin grasa y sin grapas grandes: caneca blanca. Mojado o sucio va a la negra.", "", "PAPEL",
                "hojas", "hoja", "hojas impresas", "fotocopia", "fotocopias", "periodico", "revista", "sobre de papel", "papel bond", "papel de oficina"),
            R("vidrio", "Botella o frasco de vidrio sin contenido", CategoriaMaterial.Aprovechable, "Envase de vidrio aprovechable.",
                "Vacío y sin tapa metálica (sepárala): caneca blanca. El vidrio roto requiere entrega especial.", "", "VIDRIO",
                "vidrio", "botella de vidrio", "frasco de vidrio", "botella de cerveza", "frasco de mermelada", "envase de vidrio", "frasco de compota"),
            R("lata-aluminio", "Lata de aluminio (gaseosa, cerveza, bebida energizante)", CategoriaMaterial.Aprovechable, "Metal aprovechable.",
                "Vacíala y entrégala en la caneca blanca; si puedes, aplástala.", "", "LATA_ALUMINIO",
                "lata", "latas", "lata de gaseosa", "lata de cerveza", "aluminio", "lata de aluminio", "lata de bebida"),
            R("caja-leche", "Caja de leche u otro envase multicapa vacío", CategoriaMaterial.Aprovechable, "Envase multicapa (cartón, plástico y aluminio).",
                "Vacíala, enjuágala, aplástala y deposítala en la caneca blanca.", "", "MULTICAPA",
                "caja de leche", "leche", "tetra pak de leche", "envase de leche", "caja de crema de leche"),

            // Negra: no aprovechables (categoría 3).
            R("papel-contaminado", "Papel o cartón con grasa o comida (caja de pizza, pañuelos)", CategoriaMaterial.NoAprovechable, "Papel contaminado, no aprovechable.",
                "Va en la caneca negra. Si una parte está limpia, sepárala para la blanca.", "", "SERVILLETA_USADA",
                "papel sucio", "papel engrasado", "caja de pizza", "carton con grasa", "papel contaminado", "panuelo usado", "toalla de papel", "papel mojado"),
            R("envolturas", "Envolturas y paquetes metalizados de mecato", CategoriaMaterial.NoAprovechable, "Empaque multicapa no aprovechable.",
                "Vacíalo de migas y deposítalo en la caneca negra.", "", "PLASTICO_SIN_ID",
                "envoltura", "envolturas", "envoltorio", "paquete de papas", "bolsa de papas", "empaque de galletas", "empaque de dulce", "papel metalizado", "chuspa de mecato", "empaque de mecato"),
            R("cubiertos-desechables", "Pitillos y cubiertos desechables usados", CategoriaMaterial.NoAprovechable, "Plástico desechable no aprovechable.",
                "Deposítalos en la caneca negra.", "", "PLASTICO_SIN_ID",
                "pitillo", "pitillos", "pajilla", "cubiertos desechables", "tenedor plastico", "cuchara desechable", "cuchara plastica", "mezclador"),
            R("icopor-sucio", "Coca, vaso o bandeja de icopor sucios", CategoriaMaterial.NoAprovechable, "Icopor contaminado, no aprovechable.",
                "Retira los restos de comida (van a la verde) y deposita el icopor en la caneca negra.", "", "ICOPOR_CONTAMINADO",
                "coca de icopor", "caja de icopor", "bandeja de icopor", "vaso de icopor", "icopor sucio", "icopor contaminado"),
            R("condon-usado", "Condón usado, envuelto en papel", CategoriaMaterial.NoAprovechable, "Residuo sanitario doméstico no aprovechable (no es residuo hospitalario).",
                "Envuélvelo en papel higiénico o en su empaque y deposítalo en la caneca negra. Nunca al inodoro.", "", "CONDON",
                "condon", "condon usado", "preservativo", "preservativo usado"),
            R("toalla-higienica", "Toalla higiénica o protector diario usado", CategoriaMaterial.NoAprovechable, "Residuo sanitario doméstico no aprovechable.",
                "Envuélvela en su empaque o en papel y deposítala en la caneca negra. Nunca al inodoro.", "", "TOALLA_HIGIENICA",
                "toalla higienica", "toallas higienicas", "protector diario", "toalla sanitaria"),
            R("tampon-usado", "Tampón usado", CategoriaMaterial.NoAprovechable, "Residuo sanitario doméstico no aprovechable.",
                "Envuélvelo en papel y deposítalo en la caneca negra. Nunca al inodoro.", "", "TAMPON",
                "tampon", "tampones"),
            R("panal-usado", "Pañal usado", CategoriaMaterial.NoAprovechable, "Residuo sanitario doméstico no aprovechable.",
                "Enróllalo, ciérralo con su cinta y deposítalo en la caneca negra.", "", "PANAL",
                "panal", "panales", "panal desechable", "panal de bebe"),
            R("colilla", "Colilla de cigarrillo apagada", CategoriaMaterial.NoAprovechable, "Residuo no aprovechable.",
                "Apágala por completo y deposítala en la caneca negra.", "", "COLILLA",
                "colilla", "colillas", "cigarrillo", "pucho"),
        });

        private static IReadOnlyList<ResiduoPrototipo> ConFotos(ResiduoPrototipo[] residuos) =>
            residuos.Select(r => r.Imagen.Length > 0 ? r : r with { Imagen = CatalogoFotos.De(r.Id).FirstOrDefault() ?? string.Empty }).ToList();

        /// <summary>Todas las imágenes de un residuo: su ilustración (si la tiene) y luego sus fotografías, sin repetir.</summary>
        public static IReadOnlyList<string> Imagenes(ResiduoPrototipo residuo) =>
            new[] { residuo.Imagen }.Concat(CatalogoFotos.De(residuo.Id)).Where(i => i.Length > 0).Distinct(StringComparer.Ordinal).ToList();

        /// <summary>Palabras que señalan un residuo especial o peligroso: no pertenece a ninguna de las tres canecas.</summary>
        private static readonly string[] FueraDeAlcance =
        {
            "pila", "pilas", "bateria", "baterias", "medicamento", "medicamentos", "pastilla", "pastillas", "blister", "aceite",
            "jeringa", "aguja", "bombillo", "bombilla", "celular", "cargador", "electronico", "aerosol", "insecticida", "veneno",
            "quimico", "cloro", "pintura", "termometro", "vidrio roto", "toner", "cartucho",
        };

        public const string MensajeFueraDeCatalogo =
            "Este residuo no pertenece al catálogo de depósito del prototipo y no va en ninguna de las tres canecas. Entrégalo en un punto de recolección especializado.";

        private static ResiduoPrototipo R(string id, string nombre, CategoriaMaterial caneca, string descripcion, string instrucciones,
            string imagen, string codigo, params string[] sinonimos) =>
            new(id, nombre, caneca, descripcion, instrucciones, imagen, codigo, sinonimos);

        public static ResiduoPrototipo? Buscar(string? id) => Todos.FirstOrDefault(r => r.Id == id);

        public static IReadOnlyList<ResiduoPrototipo> DeCaneca(CategoriaMaterial caneca) => Todos.Where(r => r.Caneca == caneca).ToList();

        /// <summary>Nombre, color y texto corto de cada caneca, en el orden en que se muestran.</summary>
        public static IReadOnlyList<(CategoriaMaterial Caneca, string Nombre, string Descripcion)> Canecas { get; } = new[]
        {
            (CategoriaMaterial.OrganicoAprovechable, "Caneca verde", "Orgánicos aprovechables"),
            (CategoriaMaterial.Aprovechable, "Caneca blanca", "Aprovechables / reciclables"),
            (CategoriaMaterial.NoAprovechable, "Caneca negra", "No aprovechables"),
        };

        public static string NombreCaneca(CategoriaMaterial caneca) =>
            Canecas.Where(c => c.Caneca == caneca).Select(c => c.Nombre).DefaultIfEmpty("Fuera del prototipo").First();

        /// <summary>
        /// Busca sin distinguir mayúsculas ni tildes, por nombre y sinónimos. Si el texto nombra un residuo especial o
        /// peligroso y no coincide con ninguno del catálogo, lo marca como fuera de catálogo en vez de asignarle una caneca.
        /// </summary>
        public static BusquedaPrototipo Consultar(string? texto)
        {
            string consulta = Normalizar(texto);
            if (consulta.Length == 0) return new BusquedaPrototipo(Todos, false, $"{Todos.Count} residuos que puedes depositar en la estación.");
            string[] palabras = consulta.Split(' ');
            var exactos = Todos.Where(r => Claves(r).Any(c => c == consulta)).ToList();
            var parciales = Todos.Where(r => !exactos.Contains(r) && Claves(r).Any(c => Contiene(c, palabras))).ToList();
            var encontrados = exactos.Concat(parciales).ToList();
            bool peligroso = FueraDeAlcance.Any(p => Contiene(consulta, p.Split(' ')));
            if (peligroso && exactos.Count == 0)
                return new BusquedaPrototipo(Array.Empty<ResiduoPrototipo>(), true, MensajeFueraDeCatalogo);
            if (encontrados.Count == 0)
                return new BusquedaPrototipo(encontrados, false, "No encontramos ese residuo en el catálogo del prototipo. Prueba con «botella», «cáscara» o «servilleta».");
            return new BusquedaPrototipo(encontrados, false, encontrados.Count == 1 ? "1 residuo encontrado." : $"{encontrados.Count} residuos encontrados.");
        }

        private static IEnumerable<string> Claves(ResiduoPrototipo r) => new[] { Normalizar(r.Nombre) }.Concat(r.Sinonimos.Select(Normalizar));

        // Todas las palabras de la consulta deben aparecer como palabra (o inicio de palabra) en la clave.
        private static bool Contiene(string clave, string[] palabras)
        {
            string[] partes = clave.Split(' ');
            return palabras.All(p => partes.Any(c => c.StartsWith(p, StringComparison.Ordinal) || (p.Length >= 4 && c.StartsWith(p.TrimEnd('s'), StringComparison.Ordinal))));
        }

        /// <summary>Minúsculas, sin tildes ni signos, con un solo espacio entre palabras.</summary>
        public static string Normalizar(string? texto)
        {
            if (texto is null) return "";
            var sb = new StringBuilder();
            foreach (char c in texto.Normalize(NormalizationForm.FormD))
            {
                UnicodeCategory tipo = CharUnicodeInfo.GetUnicodeCategory(c);
                if (tipo == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }
            return string.Join(" ", sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
