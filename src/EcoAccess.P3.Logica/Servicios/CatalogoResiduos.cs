using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Un residuo consultable desde la aplicación de escritorio. Une dos fuentes que ya existían en el proyecto:
    /// <list type="bullet">
    /// <item><see cref="CatalogoPrototipo"/>: residuos con material habilitado en Oracle. Son los ÚNICOS que pueden pedir la
    /// apertura de una caneca (<see cref="PermiteApertura"/>), y aun así Oracle confirma que la caneca esté en servicio y lo admita.</item>
    /// <item><see cref="CatalogoObjetos"/> (base y ampliado colombiano) con la ruta de <see cref="MatrizDecisionResiduos"/>: la misma
    /// que usa ECO en Telegram. Solo orientan (caneca, manejo especial, reutilizar o «depende de su estado»); nunca abren.</item>
    /// </list>
    /// No hace falta una imagen: <see cref="Imagen"/> es opcional y la interfaz muestra un icono del material cuando falta.
    /// </summary>
    public sealed record ResiduoCatalogo(
        string Id,
        string Nombre,
        RutaDisposicion Ruta,
        string Material,
        string Descripcion,
        string Instrucciones,
        string? Precauciones,
        IReadOnlyList<string> Sinonimos,
        string Fuente,
        ResiduoPrototipo? Prototipo = null,
        bool Vigente = true)
    {
        /// <summary>Caneca de EcoAccess a la que va, o null si su ruta no es ninguna de las tres.</summary>
        public CategoriaMaterial? Caneca => CatalogoResiduos.CanecaDe(Ruta);

        /// <summary>Solo un residuo del piloto con material de Oracle puede iniciar un depósito con apertura.</summary>
        public bool PermiteApertura => Vigente && Prototipo is { Disponible: true } && Caneca is not null;

        /// <summary>Pilas, RAEE, medicamentos, bombillos, peligrosos, cortopunzantes o aceite: entrega diferenciada.</summary>
        public bool RequiereManejoEspecial => CatalogoResiduos.EsManejoEspecial(Ruta);

        /// <summary>El destino depende de algo que el nombre no dice (contenido, limpieza, uso): se advierte y no se abre.</summary>
        public bool DependeDeSuEstado => Ruta == RutaDisposicion.Evaluar;

        /// <summary>Ruta relativa de la ilustración en assets/imagenes/residuos (sin extensión), o null si no tiene.</summary>
        public string? Imagen => Prototipo is { Imagen: { Length: > 0 } imagen } ? imagen : null;
    }

    /// <summary>Qué tan segura es una coincidencia. Una aproximada solo sugiere: la persona confirma antes de depositar.</summary>
    public enum CalidadCoincidencia
    {
        Aproximada = 1,
        Parcial = 2,
        Exacta = 3,
    }

    public sealed record CoincidenciaResiduo(ResiduoCatalogo Residuo, int Puntaje, CalidadCoincidencia Calidad);

    /// <summary>Resultado de buscar un residuo por texto libre.</summary>
    public sealed record ResultadoBusquedaResiduo(string Consulta, IReadOnlyList<CoincidenciaResiduo> Coincidencias, string Mensaje)
    {
        public ResiduoCatalogo? Principal => Coincidencias.Count > 0 ? Coincidencias[0].Residuo : null;

        public bool SinResultados => Coincidencias.Count == 0;

        /// <summary>El mejor resultado solo se parece (error de escritura): se pregunta «¿quisiste decir…?».</summary>
        public bool EsAproximada => Coincidencias.Count > 0 && Coincidencias[0].Calidad == CalidadCoincidencia.Aproximada;

        /// <summary>
        /// Hay varias interpretaciones igual de buenas con destinos distintos (por ejemplo «vaso»: limpio a la blanca,
        /// sucio a la negra). La persona elige; no se asume ninguna.
        /// </summary>
        public bool EsAmbigua => Alternativas.Count > 0;

        /// <summary>Otras interpretaciones cercanas a la principal que van a un destino distinto.</summary>
        public IReadOnlyList<ResiduoCatalogo> Alternativas
        {
            get
            {
                if (Coincidencias.Count < 2) return Array.Empty<ResiduoCatalogo>();
                CoincidenciaResiduo primera = Coincidencias[0];
                return Coincidencias.Skip(1)
                    .Where(c => c.Residuo.Ruta != primera.Residuo.Ruta && (
                        // Igual de buenas con destino distinto.
                        (c.Calidad == primera.Calidad && primera.Puntaje - c.Puntaje <= CatalogoResiduos.MargenAmbiguedad)
                        // O dos depositables que comparten la palabra buscada y cuyo estado cambia la caneca («vaso», «papel», «caja»).
                        || (primera.Residuo.PermiteApertura && c.Residuo.PermiteApertura && c.Calidad >= CalidadCoincidencia.Parcial
                            && primera.Puntaje - c.Puntaje <= CatalogoResiduos.MargenEstado)))
                    .Select(c => c.Residuo).Take(3).ToList();
            }
        }
    }

    /// <summary>
    /// Catálogo unificado y buscador tolerante del escritorio. Determinista y sin IA: normaliza tildes y mayúsculas, quita
    /// artículos, reconoce plurales y diminutivos («botellita»), sinónimos y nombres populares («guineo», «chuspa»),
    /// coincidencias parciales y errores de escritura razonables («servileta»). La búsqueda flexible solo SUGIERE: abrir
    /// una caneca exige un residuo del piloto elegido por la persona y la confirmación del asistente de depósito.
    /// </summary>
    public static class CatalogoResiduos
    {
        internal const int MargenAmbiguedad = 8, MargenEstado = 45;

        private static readonly HashSet<string> Vacias = new(StringComparer.Ordinal)
        {
            "de", "del", "la", "el", "los", "las", "un", "una", "unos", "unas", "mi", "mis", "y", "o", "con", "para", "por", "en",
            "al", "que", "donde", "boto", "tiro", "echo", "va", "van", "pongo", "deposito", "esta", "este", "esto", "es", "lo",
        };

        public const string FuentePiloto = "Catálogo del piloto EcoAccess: material habilitado en Oracle (Resolución 2184 de 2019).";
        public static readonly string FuenteMatriz = $"Matriz de decisión EcoAccess {MatrizDecisionResiduos.Version} (Resolución 2184 de 2019 y programas posconsumo); la misma que usa ECO en Telegram.";

        /// <summary>Todos los residuos consultables: primero los del piloto (pueden depositarse) y luego los de orientación.</summary>
        public static IReadOnlyList<ResiduoCatalogo> Todos => Datos.Todos;

        // Clase anidada: se inicializa al primer uso, cuando los diccionarios de esta clase ya existen.
        private static class Datos
        {
            internal static readonly IReadOnlyList<ResiduoCatalogo> Todos = Construir();
            internal static readonly Dictionary<string, ResiduoCatalogo> PorId = Todos.ToDictionary(r => r.Id, StringComparer.Ordinal);
            internal static readonly IReadOnlyList<(ResiduoCatalogo Residuo, string[][] Claves)> Indice =
                Todos.Select(r => (r, new[] { r.Nombre }.Concat(r.Sinonimos).Select(Tokens).Where(t => t.Length > 0).Distinct(new ComparadorTokens()).ToArray())).ToList();
        }

        public static ResiduoCatalogo? Buscar(string? id) => id is not null && Datos.PorId.TryGetValue(id, out ResiduoCatalogo? r) ? r : null;

        public static IReadOnlyList<ResiduoCatalogo> DeCaneca(CategoriaMaterial caneca) => Todos.Where(r => r.Caneca == caneca).ToList();

        /// <summary>Residuos de la caneca que se pueden depositar con apertura (los del piloto).</summary>
        public static IReadOnlyList<ResiduoCatalogo> DepositablesDe(CategoriaMaterial caneca) => Todos.Where(r => r.Caneca == caneca && r.PermiteApertura).ToList();

        public static IReadOnlyList<ResiduoCatalogo> ManejoEspecial => Todos.Where(r => r.RequiereManejoEspecial).ToList();

        public static CategoriaMaterial? CanecaDe(RutaDisposicion ruta) => ruta switch
        {
            RutaDisposicion.Verde => CategoriaMaterial.OrganicoAprovechable,
            RutaDisposicion.Blanca => CategoriaMaterial.Aprovechable,
            RutaDisposicion.Negra => CategoriaMaterial.NoAprovechable,
            _ => null,
        };

        public static bool EsManejoEspecial(RutaDisposicion ruta) => ruta is RutaDisposicion.Raee or RutaDisposicion.PilasBaterias
            or RutaDisposicion.Medicamentos or RutaDisposicion.Bombillos or RutaDisposicion.Peligroso or RutaDisposicion.Cortopunzante
            or RutaDisposicion.AceiteUsado;

        /// <summary>Nombre corto del destino para la interfaz.</summary>
        public static string NombreDestino(RutaDisposicion ruta) => ruta switch
        {
            RutaDisposicion.Verde => "Caneca verde",
            RutaDisposicion.Blanca => "Caneca blanca",
            RutaDisposicion.Negra => "Caneca negra",
            RutaDisposicion.Raee => "Residuo eléctrico o electrónico (RAEE)",
            RutaDisposicion.PilasBaterias => "Pilas y baterías: punto posconsumo",
            RutaDisposicion.Medicamentos => "Medicamentos: punto posconsumo",
            RutaDisposicion.Bombillos => "Bombillos: punto posconsumo",
            RutaDisposicion.Peligroso => "Residuo peligroso: entrega especial",
            RutaDisposicion.Cortopunzante => "Cortopunzante: recipiente rígido y entrega especial",
            RutaDisposicion.AceiteUsado => "Aceite usado: punto de recolección",
            RutaDisposicion.ReutilizacionDonacion => "Reutiliza, repara o dona",
            RutaDisposicion.NoEsResiduo => "No es un residuo",
            _ => "Depende de su estado",
        };

        public const string MensajeManejoEspecial =
            "Este residuo requiere una disposición especial y no debe depositarse en las tres canecas de EcoAccess.";

        /// <summary>Busca por texto libre. El texto vacío no devuelve nada (la interfaz muestra las canecas).</summary>
        public static ResultadoBusquedaResiduo Consultar(string? texto, int maximo = 8)
        {
            string[] consulta = Tokens(texto);
            if (consulta.Length == 0) return new ResultadoBusquedaResiduo("", Array.Empty<CoincidenciaResiduo>(), "Escribe el nombre de un residuo.");
            var encontrados = new List<CoincidenciaResiduo>();
            foreach (var (residuo, claves) in Datos.Indice)
            {
                CoincidenciaResiduo? mejor = null;
                foreach (string[] clave in claves)
                {
                    CoincidenciaResiduo? c = Evaluar(residuo, consulta, clave);
                    if (c is not null && (mejor is null || c.Puntaje > mejor.Puntaje)) mejor = c;
                }
                if (mejor is not null) encontrados.Add(mejor);
            }
            // Con coincidencias exactas o parciales, las aproximadas sobran: solo confundirían.
            CalidadCoincidencia mejorCalidad = encontrados.Count == 0 ? CalidadCoincidencia.Aproximada : encontrados.Max(c => c.Calidad);
            List<CoincidenciaResiduo> lista = encontrados
                .Where(c => mejorCalidad == CalidadCoincidencia.Aproximada || c.Calidad != CalidadCoincidencia.Aproximada)
                .OrderByDescending(c => c.Calidad).ThenByDescending(c => c.Puntaje).ThenBy(c => c.Residuo.PermiteApertura ? 0 : 1)
                .ThenBy(c => c.Residuo.Nombre.Length).ThenBy(c => c.Residuo.Id, StringComparer.Ordinal)
                .Take(Math.Max(1, maximo)).ToList();
            string mensaje = lista.Count == 0
                ? "No encontramos ese residuo en el catálogo de EcoAccess. Revisa la escritura o prueba con otro nombre (por ejemplo «botella», «cáscara» o «servilleta»)."
                : lista[0].Calidad == CalidadCoincidencia.Aproximada ? $"¿Quisiste decir «{lista[0].Residuo.Nombre}»?"
                : lista.Count == 1 ? "1 residuo encontrado." : $"{lista.Count} residuos encontrados.";
            return new ResultadoBusquedaResiduo(string.Join(" ", consulta), lista, mensaje);
        }

        /// <summary>Sugerencias mientras se escribe (desde 2 letras).</summary>
        public static IReadOnlyList<ResiduoCatalogo> Sugerir(string? texto, int maximo = 6)
        {
            string normal = CatalogoPrototipo.Normalizar(texto);
            if (normal.Replace(" ", "").Length < 2) return Array.Empty<ResiduoCatalogo>();
            return Consultar(texto, maximo).Coincidencias.Select(c => c.Residuo).ToList();
        }

        // ── Comparación ─────────────────────────────────────────────────────────────────────────────────────────────

        private static CoincidenciaResiduo? Evaluar(ResiduoCatalogo residuo, string[] consulta, string[] clave)
        {
            int extra = residuo.PermiteApertura ? 3 : 0;
            // La coincidencia literal gana a la de raíz: «pastillas» (medicamento) por encima de cualquier parecido.
            if (consulta.SequenceEqual(clave, StringComparer.Ordinal))
                return new CoincidenciaResiduo(residuo, 110 + extra, CalidadCoincidencia.Exacta);
            if (consulta.Select(Raiz).SequenceEqual(clave.Select(Raiz), StringComparer.Ordinal))
                return new CoincidenciaResiduo(residuo, 100 + extra, CalidadCoincidencia.Exacta);
            int exactas = 0, aproximadas = 0;
            var usadas = new HashSet<int>();
            for (int i = 0; i < consulta.Length; i++)
            {
                string q = consulta[i];
                bool ultima = i == consulta.Length - 1;
                int encontrada = -1;
                for (int k = 0; k < clave.Length && encontrada < 0; k++)
                    if (!usadas.Contains(k) && CoincideToken(q, clave[k], ultima)) encontrada = k;
                if (encontrada >= 0) { exactas++; usadas.Add(encontrada); continue; }
                for (int k = 0; k < clave.Length && encontrada < 0; k++)
                    if (!usadas.Contains(k) && Parecido(q, clave[k])) encontrada = k;
                if (encontrada >= 0) { aproximadas++; usadas.Add(encontrada); }
            }
            int coinciden = exactas + aproximadas;
            if (coinciden == 0) return null;
            // Cuánto de la clave quedó cubierto: «botella» se parece más a «botella» que a «botella de vidrio».
            int cobertura = 20 * usadas.Count / clave.Length;
            if (coinciden == consulta.Length)
                return aproximadas == 0
                    ? new CoincidenciaResiduo(residuo, 60 + cobertura + extra, CalidadCoincidencia.Parcial)
                    : new CoincidenciaResiduo(residuo, 30 + cobertura + extra - 5 * aproximadas, CalidadCoincidencia.Aproximada);
            // Palabras de sobra en la consulta («botella de agua grande»): vale si al menos la mitad coincide y no es solo una sílaba.
            if (2 * coinciden >= consulta.Length && usadas.Any(k => clave[k].Length >= 4))
                return new CoincidenciaResiduo(residuo, 10 + cobertura * coinciden / consulta.Length + extra, CalidadCoincidencia.Aproximada);
            return null;
        }

        private static bool CoincideToken(string q, string k, bool ultima)
        {
            if (q == k) return true;
            string rq = Raiz(q), rk = Raiz(k);
            if (rq == rk && rq.Length >= 2) return true;
            // La última palabra puede estar a medio escribir («bot» → «botella»).
            if (ultima && q.Length >= 3 && k.StartsWith(q, StringComparison.Ordinal)) return true;
            return rq.Length >= 4 && rk.Length >= 4 && (rk.StartsWith(rq, StringComparison.Ordinal) || rq.StartsWith(rk, StringComparison.Ordinal)) && Math.Abs(rq.Length - rk.Length) <= 2;
        }

        /// <summary>Error de escritura razonable: 1 letra en palabras de 5 a 7 letras, 2 desde 8. Las cortas deben coincidir.</summary>
        internal static bool Parecido(string q, string k)
        {
            int largo = Math.Min(q.Length, k.Length);
            if (largo < 5) return false;
            int permitido = largo >= 8 ? 2 : 1;
            if (Math.Abs(q.Length - k.Length) > permitido) return false;
            return Distancia(q, k, permitido) <= permitido || Distancia(Raiz(q), Raiz(k), permitido) <= permitido && Raiz(q).Length >= 4;
        }

        /// <summary>Distancia de Damerau-Levenshtein (transposiciones incluidas), cortada en <paramref name="limite"/> + 1.</summary>
        internal static int Distancia(string a, string b, int limite = int.MaxValue)
        {
            int n = a.Length, m = b.Length;
            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
            {
                int minimoFila = int.MaxValue;
                for (int j = 1; j <= m; j++)
                {
                    int costo = a[i - 1] == b[j - 1] ? 0 : 1;
                    int v = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + costo);
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) v = Math.Min(v, d[i - 2, j - 2] + 1);
                    d[i, j] = v;
                    minimoFila = Math.Min(minimoFila, v);
                }
                if (minimoFila > limite) return limite + 1;
            }
            return d[n, m];
        }

        /// <summary>Palabras normalizadas sin artículos ni preposiciones.</summary>
        internal static string[] Tokens(string? texto)
        {
            string normal = CatalogoPrototipo.Normalizar(texto);
            if (normal.Length > 100) normal = normal.Substring(0, 100);
            return normal.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(t => !Vacias.Contains(t)).ToArray();
        }

        /// <summary>Raíz simple en español: quita diminutivo, plural y vocal final («botellitas» → «botell»).</summary>
        internal static string Raiz(string palabra)
        {
            string p = palabra;
            // Sin «-illa»: «pastilla», «colilla» y «semilla» no son diminutivos (y «pastillas» → «pasta» mandaba un medicamento a la verde).
            foreach (string sufijo in new[] { "citos", "citas", "cito", "cita", "itos", "itas", "ito", "ita" })
                if (p.EndsWith(sufijo, StringComparison.Ordinal) && p.Length - sufijo.Length >= 3) { p = p.Substring(0, p.Length - sufijo.Length); break; }
            if (p.Length > 4 && p.EndsWith("es", StringComparison.Ordinal) && !"aeiou".Contains(p[p.Length - 3])) p = p.Substring(0, p.Length - 2);
            else if (p.Length > 3 && p.EndsWith("s", StringComparison.Ordinal)) p = p.Substring(0, p.Length - 1);
            if (p.Length >= 4 && "aeo".Contains(p[p.Length - 1])) p = p.Substring(0, p.Length - 1);
            return p;
        }

        private sealed class ComparadorTokens : IEqualityComparer<string[]>
        {
            public bool Equals(string[]? x, string[]? y) => x is not null && y is not null && x.SequenceEqual(y, StringComparer.Ordinal);
            public int GetHashCode(string[] obj) => string.Join(" ", obj).GetHashCode();
        }

        // ── Construcción del catálogo ───────────────────────────────────────────────────────────────────────────────

        private static IReadOnlyList<ResiduoCatalogo> Construir()
        {
            var lista = new List<ResiduoCatalogo>();
            foreach (ResiduoPrototipo p in CatalogoPrototipo.Todos)
            {
                RutaDisposicion ruta = p.Caneca switch
                {
                    CategoriaMaterial.OrganicoAprovechable => RutaDisposicion.Verde,
                    CategoriaMaterial.Aprovechable => RutaDisposicion.Blanca,
                    _ => RutaDisposicion.Negra,
                };
                lista.Add(new ResiduoCatalogo(p.Id, p.Nombre, ruta, p.CodigoMaterial, p.Descripcion, p.Instrucciones, null, p.Sinonimos, FuentePiloto, p));
            }
            var nombres = new HashSet<string>(lista.Select(r => CatalogoPrototipo.Normalizar(r.Nombre)), StringComparer.Ordinal);
            foreach (ObjetoConocido o in CatalogoObjetos.Todos)
            {
                if (o.Contexto is ContextoObjeto.Desconocido or ContextoObjeto.Persona or ContextoObjeto.AnimalVivo or ContextoObjeto.PlantaViva) continue;
                Espanol.TryGetValue(o.Clave, out var es);
                string nombre = es.Nombre ?? Capitalizar(SinArticulo(o.Frase));
                if (!nombres.Add(CatalogoPrototipo.Normalizar(nombre))) nombre += " (orientación)";
                RutaDisposicion ruta = MatrizDecisionResiduos.Ruta(o);
                string explicacion = SinEmoji(o.Explicacion);
                string? precauciones = EsManejoEspecial(ruta) || ruta == RutaDisposicion.Cortopunzante ? explicacion : null;
                lista.Add(new ResiduoCatalogo("obj-" + o.Clave, nombre, ruta, o.CodigoMaterial ?? MaterialDe(o.Contexto),
                    $"{MaterialDe(o.Contexto)} · {NombreDestino(ruta)}.", explicacion, precauciones,
                    (es.Sinonimos ?? "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries), FuenteMatriz));
            }
            return lista;
        }

        private static string MaterialDe(ContextoObjeto contexto) => contexto switch
        {
            ContextoObjeto.ResiduoAprovechable => "Aprovechable",
            ContextoObjeto.ResiduoOrganico => "Orgánico",
            ContextoObjeto.ResiduoSanitario => "Sanitario",
            ContextoObjeto.ResiduoElectronico => "Eléctrico o electrónico",
            ContextoObjeto.ResiduoEspecial => "Especial",
            ContextoObjeto.ResiduoNoAprovechable => "No aprovechable",
            _ => "Objeto de uso cotidiano",
        };

        private static string SinArticulo(string frase)
        {
            foreach (string a in new[] { "unos ", "unas ", "un ", "una ", "el ", "la ", "los ", "las " })
                if (frase.StartsWith(a, StringComparison.OrdinalIgnoreCase)) return frase.Substring(a.Length);
            return frase;
        }

        private static string Capitalizar(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);

        /// <summary>Quita los emojis del inicio: el escritorio dibuja sus propios iconos.</summary>
        private static string SinEmoji(string texto)
        {
            int i = 0;
            while (i < texto.Length && !char.IsLetterOrDigit(texto[i]) && texto[i] != '«' && texto[i] != '¿' && texto[i] != '¡') i++;
            return texto.Substring(i).Trim();
        }

        /// <summary>
        /// Nombre en español y sinónimos populares de las claves del catálogo de objetos (que nacieron para el modelo de
        /// visión en inglés). Sin entrada, el nombre sale de la frase en español que ya usa ECO.
        /// </summary>
        private static readonly Dictionary<string, (string? Nombre, string? Sinonimos)> Espanol = new(StringComparer.Ordinal)
        {
            ["battery"] = ("Pila o batería", "pila|pilas|bateria|baterias|pila aa|pila aaa|pila doble a|pila triple a|pila alcalina|bateria de celular|pila recargable"),
            ["lithium_battery"] = (null, "bateria de litio|bateria de portatil|bateria de computador|power bank|banco de energia|bateria de celular"),
            ["button_battery"] = (null, "pila de reloj|pila boton|pila de boton|pila redonda|pila plana"),
            ["car_battery"] = (null, "bateria de moto|bateria de carro|acumulador"),
            ["swollen_battery"] = (null, "bateria inflada|bateria hinchada|bateria danada"),
            ["medicine"] = ("Medicamento vencido o sin uso", "medicamento|medicamentos|remedio|remedios|pastilla|pastillas|pasta|tableta|capsula|jarabe|droga vencida|medicina"),
            ["blister_empty"] = (null, "blister|empaque de pastillas"),
            ["medicine_bottle"] = (null, "frasco de jarabe|frasco de medicina"),
            ["bulb"] = ("Bombillo", "bombillo|bombillos|bombilla|foco|lampara|ahorrador|bombillo ahorrador"),
            ["fluorescent_tube"] = (null, "tubo fluorescente|lampara fluorescente|tubo de luz|bombillo ahorrador"),
            ["led_bulb"] = (null, "bombillo led|foco led"),
            ["syringe"] = ("Jeringa, aguja u otro cortopunzante", "jeringa|jeringas|aguja|agujas|cortopunzante|lanceta|cuchilla usada"),
            ["broken_glass"] = ("Vidrio roto", "vidrio roto|vidrios rotos|vaso roto|botella rota|pedazo de vidrio"),
            ["cooking_oil"] = ("Aceite de cocina usado", "aceite|aceite usado|aceite de cocina|aceite de freir|manteca usada"),
            ["motor_oil"] = (null, "aceite de motor|aceite de carro|aceite de moto|lubricante"),
            ["aerosol"] = ("Aerosol o spray", "aerosol|spray|desodorante en aerosol|lata de spray|ambientador"),
            ["chemical_container"] = ("Envase de químicos (cloro, solventes, plaguicidas)", "quimico|quimicos|cloro|varsol|thinner|solvente|veneno|limpiador con producto|sustancia quimica|acido"),
            ["pesticide_container"] = (null, "plaguicida|insecticida|veneno para ratas|matarratas|fumigante|herbicida"),
            ["paint_can"] = (null, "pintura|tarro de pintura|galon de pintura|pintura en aerosol"),
            ["thermometer_mercury"] = (null, "termometro|termometro de mercurio|mercurio"),
            ["phone"] = ("Celular", "celular|telefono|smartphone|movil|telefono celular"),
            ["smartphone_broken"] = (null, "celular danado|celular roto|telefono viejo"),
            ["laptop"] = ("Computador o portátil", "computador|portatil|laptop|pc|computadora"),
            ["charger"] = ("Cargador", "cargador|cargador de celular|adaptador|transformador"),
            ["cable"] = ("Cable", "cable|cables|alambre electrico|extension"),
            ["headphones"] = ("Audífonos", "audifonos|auriculares|manos libres|cascos"),
            ["computer_mouse"] = (null, "mouse|raton"),
            ["keyboard"] = (null, "teclado"),
            ["monitor"] = (null, "monitor|pantalla|televisor|tv"),
            ["printer"] = (null, "impresora"),
            ["electronic_other"] = ("Aparato eléctrico o electrónico", "aparato electronico|electronico|electronicos|raee|aparato electrico|tarjeta electronica|circuito"),
            ["appliance"] = ("Electrodoméstico", "electrodomestico|nevera|lavadora|estufa|horno|aire acondicionado"),
            ["ink_cartridge"] = (null, "cartucho|cartucho de tinta|toner"),
            ["vape"] = (null, "vape|vapeador|cigarrillo electronico|vaper"),
            ["lighter"] = (null, "encendedor|yesquero|mechera"),
            ["gas_cylinder"] = (null, "pipeta|pipeta de gas|cilindro|extintor|gas"),
            ["fireworks"] = (null, "polvora|totes|cohetes|voladores"),
            ["x_ray"] = (null, "radiografia|placa de rayos x"),
            ["disposable_razor"] = (null, "cuchilla|cuchilla de afeitar|maquina de afeitar|gillette|rasuradora"),
            ["kitchen_knife"] = (null, "cuchillo"),
            ["scalpel"] = (null, "bisturi|bisturí|cuchilla de bisturi|cortador|bisturi usado"),
            ["lancet"] = (null, "lanceta|lancetas"),
            ["insulin_pen"] = (null, "pluma de insulina|aguja de insulina"),
            ["nail_polish"] = (null, "esmalte|quitaesmalte|removedor de esmalte|acetona"),
            ["tire"] = (null, "llanta|llantas|neumatico|caucho de carro"),
            ["food_scraps"] = ("Restos de comida (orientación)", "sobras|restos de comida|desperdicio de comida"),
            ["plant_waste"] = ("Restos vegetales y de poda", "poda|ramas|hojarasca|pasto cortado|cesped cortado"),
            ["plastic_bottle"] = ("Botella plástica (orientación)", "botella plastica"),
            ["glass_bottle"] = ("Envase de vidrio (orientación)", "envase de vidrio"),
            ["can"] = ("Lata (orientación)", "lata"),
            ["plastic_container"] = ("Envase plástico (orientación)", "envase plastico"),
            ["plastic_bag"] = ("Bolsa plástica", "bolsa|bolsas|bolsa plastica|chuspa|bolsa de supermercado|talega"),
            ["drink_carton"] = ("Envase multicapa (orientación)", "tetrapak|caja de leche"),
            ["cardboard"] = ("Cartón (orientación)", "carton"),
            ["paper"] = ("Papel (orientación)", "papel"),
            ["metal_item"] = ("Objeto metálico pequeño", "metal|chatarra|objeto metalico|pedazo de metal"),
            ["snack_wrapper"] = ("Empaque metalizado de mecato (orientación)", "paquete|paquete de papas|empaque de mecato|bolsa de papitas"),
            ["condom"] = ("Condón o preservativo usado (orientación)", "condon|preservativo"),
            ["diaper_or_pad"] = ("Pañal, toalla higiénica o tampón (orientación)", "panal|toalla higienica|tampon"),
            ["used_tissue"] = ("Servilleta, pañuelo o papel higiénico usado (orientación)", "panuelo usado|kleenex|papel de bano usado"),
            ["toilet_paper_roll"] = ("Rollo de papel higiénico", "rollo de papel higienico|rollo de papel|tubo de papel higienico"),
            ["gloves"] = ("Guantes desechables usados", "guantes|guantes desechables|guante de latex|guante de nitrilo"),
            ["face_mask"] = ("Tapabocas usado", "tapabocas|mascarilla|cubrebocas|barbijo"),
            ["styrofoam"] = ("Icopor (orientación)", "icopor|poliestireno|anime"),
            ["cigarette_butt"] = ("Colilla de cigarrillo (orientación)", "colilla|cigarrillo|pucho"),
            ["greasy_paper"] = ("Papel o cartón con grasa (orientación)", "caja de pizza|carton con grasa|papel engrasado"),
            ["thermal_paper"] = ("Recibo o tiquete de papel térmico", "recibo|tiquete|factura|voucher|papel termico|tirilla"),
            ["disposable_cutlery"] = ("Cubiertos y pitillos desechables (orientación)", "pitillo|pitillos|pajilla|cubiertos desechables|tenedor desechable|cuchara desechable"),
            ["mirror_or_ceramic"] = ("Espejo o cerámica", "espejo|ceramica|porcelana|taza rota|pocillo"),
            ["wood"] = ("Madera", "madera|palo|tabla"),
            ["served_food"] = ("Comida preparada", "comida|almuerzo|plato de comida"),
            ["raw_food"] = ("Carne o pescado crudo", "carne|pescado|pollo crudo"),
            ["eggs"] = ("Huevos", "huevo|huevos"),
            ["whole_produce"] = ("Fruta o verdura entera", "fruta entera|verdura entera"),
            ["shoes"] = ("Zapatos", "zapatos|tenis|zapatillas|calzado"),
            ["clothes"] = ("Ropa", "ropa|camisa|pantalon|camiseta|prenda"),
            ["toy"] = ("Juguete", "juguete|juguetes|muneco"),
            ["tool"] = ("Herramienta", "herramienta|martillo|destornillador|alicate"),
            ["furniture"] = ("Mueble", "mueble|sofa|cama|colchon|estante"),
            ["chair"] = ("Silla", "silla|asiento"),
            ["table"] = ("Mesa o escritorio", "mesa|escritorio"),
            ["tablet"] = ("Tableta", "tablet|tableta|ipad"),
            ["milk_bag"] = (null, "bolsa de leche"),
            ["water_bag"] = (null, "bolsa de agua|agua en bolsa"),
            ["beer_can"] = (null, "lata de cerveza|lata de bebida energizante"),
            ["tuna_can"] = (null, "lata de atun|lata de sardinas|lata de conserva|enlatado"),
            ["glass_jar_food"] = (null, "frasco|frasco de vidrio|frasco de mermelada|frasco de salsa"),
            ["chewing_gum"] = (null, "chicle|goma de mascar"),
            ["candy_wrapper"] = (null, "envoltura de dulce|papel de dulce|envoltura de colombina|palito de colombina"),
            ["wet_wipes"] = (null, "panitos|panitos humedos|toallitas humedas"),
            ["toothbrush"] = (null, "cepillo de dientes"),
            ["pet_waste"] = (null, "popo de perro|excremento|heces|caca de perro"),
            ["construction_debris"] = (null, "escombro|escombros|ladrillo|cemento|baldosa"),
            ["book"] = (null, "libro|libros"),
            ["notebook"] = (null, "cuaderno usado|libreta"),
            ["eyeglasses"] = (null, "gafas|lentes|anteojos"),
            ["cd_dvd"] = (null, "cd|dvd|disco"),
        };
    }
}
