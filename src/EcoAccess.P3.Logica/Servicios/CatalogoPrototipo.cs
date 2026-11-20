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
    /// Catálogo definitivo del prototipo (ExpoSoftware 2026): exactamente 15 residuos físicos repartidos en las tres
    /// canecas de la Resolución 2184 de 2019 (6 verdes, 5 blancos y 4 negros). Cada uno se registra con un material
    /// que esa caneca ya admite en Oracle (ORGANICO, PET, PAPEL, MULTICAPA, PLASTICO_RIGIDO, SERVILLETA_USADA,
    /// PLASTICO_SIN_ID, ICOPOR_CONTAMINADO), así que no hace falta migrar la base. No hay cuarta categoría: un
    /// residuo especial o peligroso (pila, medicamento, aceite…) no se asigna a ninguna caneca; solo se advierte.
    /// Para ampliar el catálogo basta añadir una entrada: los formularios lo recorren sin cambios.
    /// </summary>
    public static class CatalogoPrototipo
    {
        public const string Verde = "verde", Blanca = "blanca", Negra = "negra";

        public static IReadOnlyList<ResiduoPrototipo> Todos { get; } = new[]
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
        };

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
            if (consulta.Length == 0) return new BusquedaPrototipo(Todos, false, $"{Todos.Count} residuos del prototipo.");
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
