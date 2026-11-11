// Generador de la documentación académica (fase 8). Convierte las fuentes Markdown de docs/entrega/fuentes_md en HTML
// listo para Word. Las directivas {{...}} se reemplazan con tablas sacadas del inventario real (inventario.json), del
// diccionario de Oracle Cloud (oracle_*.txt), del historial de Git (commits.txt) o de fragmentos del código fuente.
// Nada de lo que producen las directivas se escribe a mano: si el código cambia, se vuelve a generar.
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;

static class Documentos
{
    static JsonElement _inv;
    static string _raiz = "";
    static readonly Dictionary<string, List<string>> Cobertura = new(StringComparer.Ordinal);
    static string _docActual = "", _capituloActual = "";

    public static void Generar(string raiz, string fuentes, string salida)
    {
        _raiz = Path.GetFullPath(raiz);
        Directory.CreateDirectory(salida);
        _inv = JsonDocument.Parse(File.ReadAllText(Path.Combine(_raiz, "docs/academico/inventario/inventario.json"))).RootElement;
        var tuberia = new MarkdownPipelineBuilder().UsePipeTables().UseGridTables().UseAutoIdentifiers().UseEmphasisExtras().Build();
        string notas = File.Exists(Path.Combine(fuentes, "_entidades_notas.md")) ? File.ReadAllText(Path.Combine(fuentes, "_entidades_notas.md")) : "";
        var archivos = Directory.GetFiles(fuentes, "*.md").Where(f => !Path.GetFileName(f).StartsWith("_")).OrderBy(f => f, StringComparer.Ordinal).ToList();
        // El documento maestro (00) va al final porque su anexo de cobertura usa lo que documentaron los demás.
        archivos = archivos.Where(f => !Path.GetFileName(f).StartsWith("00")).Concat(archivos.Where(f => Path.GetFileName(f).StartsWith("00"))).ToList();
        foreach (string archivo in archivos)
        {
            string nombre = Path.GetFileNameWithoutExtension(archivo);
            var (meta, cuerpo) = Frente(File.ReadAllText(archivo));
            _docActual = meta.GetValueOrDefault("archivo", nombre);
            string md = Expandir(cuerpo, notas);
            string html = Markdown.ToHtml(md, tuberia);
            File.WriteAllText(Path.Combine(salida, _docActual + ".html"), Envolver(meta, html), new UTF8Encoding(false));
            Console.WriteLine($"{_docActual}: {md.Length / 1000} mil caracteres de Markdown");
        }
    }

    static (Dictionary<string, string>, string) Frente(string texto)
    {
        var meta = new Dictionary<string, string>(StringComparer.Ordinal);
        var m = Regex.Match(texto, @"^---\r?\n(.*?)\r?\n---\r?\n", RegexOptions.Singleline);
        if (!m.Success) return (meta, texto);
        foreach (string linea in m.Groups[1].Value.Split('\n'))
            if (linea.IndexOf(':') is var i and > 0) meta[linea[..i].Trim()] = linea[(i + 1)..].Trim();
        return (meta, texto[m.Length..]);
    }

    static string Expandir(string md, string notas)
    {
        var sb = new StringBuilder();
        foreach (string linea in md.Split('\n'))
        {
            string l = linea.TrimEnd('\r');
            if (l.StartsWith("# ")) _capituloActual = l[2..].Trim();
            var d = Regex.Match(l.Trim(), @"^\{\{([a-z_]+)(?::(.*))?\}\}$");
            if (!d.Success) { sb.AppendLine(l); continue; }
            string arg = d.Groups[2].Value;
            sb.AppendLine(d.Groups[1].Value switch
            {
                "proyectos" => Proyectos(),
                "tipos" => Tipos(arg),
                "detalle" => Detalle(arg),
                "entidades" => Entidades(notas),
                "formularios" => Formularios(arg),
                "oracle_tablas" => OracleTablas(),
                "oracle_columnas" => OracleColumnas(arg),
                "commits" => Commits(),
                "pruebas" => Pruebas(arg),
                "codigo" => Codigo(arg),
                "cobertura" => CoberturaTabla(),
                "conteos" => Conteos(),
                // Incluye un Markdown del repositorio sin su título principal; sus "##" pasan a ser capítulos ("#").
                "incluir" => Incluir(arg),
                // Igual, pero conserva los niveles: sus "##" quedan como secciones dentro del capítulo actual.
                "anidar" => Incluir(arg, subir: false),
                _ => $"**[directiva desconocida: {d.Value}]**"
            });
        }
        return sb.ToString();
    }

    static IEnumerable<JsonElement> TiposProduccion() => _inv.GetProperty("Tipos").EnumerateArray().Where(t => S(t, "Capa") != "Pruebas");
    static string S(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    static string Celda(string s) => s.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Replace("<", "&lt;").Replace(">", "&gt;");
    static string Cod(string s) => string.IsNullOrEmpty(s) ? "" : "`" + s.Replace("`", "'").Replace("|", "\\|") + "`";
    static void Registrar(string elemento) { if (!Cobertura.TryGetValue(elemento, out var l)) Cobertura[elemento] = l = new(); string r = $"{_docActual} › {_capituloActual}"; if (!l.Contains(r)) l.Add(r); }
    static string Ruta(JsonElement t) => $"{S(t, "Archivo")}:{t.GetProperty("Linea").GetInt32()}";

    static string Incluir(string ruta, bool subir = true)
    {
        var lineas = File.ReadAllLines(Path.Combine(_raiz, ruta)).ToList();
        if (lineas.Count > 0 && lineas[0].StartsWith("# ")) lineas.RemoveAt(0);
        bool enCodigo = false;
        var sb = new StringBuilder($"> Fuente: `{ruta}`.\n\n");
        foreach (string l in lineas)
        {
            if (l.TrimStart().StartsWith("```")) enCodigo = !enCodigo;
            if (enCodigo) sb.AppendLine(l);
            else if (subir) sb.AppendLine(l.StartsWith("##") ? l[1..] : l);
            else sb.AppendLine(l.StartsWith("# ") ? "#" + l : l);
        }
        return sb.ToString();
    }

    static string Conteos()
    {
        var tipos = TiposProduccion().ToList();
        var formularios = _inv.GetProperty("Formularios").EnumerateArray().ToList();
        var pruebas = _inv.GetProperty("ClasesPrueba").EnumerateArray().ToList();
        return "| Elemento | Cantidad |\n|---|---|\n" +
            $"| Archivos C# (src y tests) | {_inv.GetProperty("Archivos").GetArrayLength()} |\n" +
            $"| Tipos de producción (clases, interfaces, enumeraciones, records) | {tipos.Count} |\n" +
            $"| Clases | {tipos.Count(t => S(t, "Clase") == "class")} |\n| Interfaces | {tipos.Count(t => S(t, "Clase") == "interface")} |\n" +
            $"| Enumeraciones | {tipos.Count(t => S(t, "Clase") == "enum")} |\n| Records | {tipos.Count(t => S(t, "Clase").StartsWith("record"))} |\n" +
            $"| Clases abstractas | {tipos.Count(t => S(t, "Modificadores").Contains("abstract"))} |\n" +
            $"| Formularios y paneles de Windows Forms | {formularios.Count} |\n| Eventos conectados en formularios | {formularios.Sum(f => f.GetProperty("Eventos").GetArrayLength())} |\n" +
            $"| Clases de prueba | {pruebas.Count} |\n| Métodos de prueba ([TestMethod]) | {pruebas.Sum(p => p.GetProperty("Metodos").GetInt32())} |\n" +
            $"| Commit del inventario | {S(_inv, "Commit")} |\n";
    }

    static string Proyectos()
    {
        var sb = new StringBuilder("| Proyecto | Archivos .cs | Líneas | Tipos | Capa |\n|---|---|---|---|---|\n");
        foreach (var g in _inv.GetProperty("Archivos").EnumerateArray().GroupBy(a => S(a, "Proyecto")).OrderBy(g => g.Key))
        {
            var tipos = _inv.GetProperty("Tipos").EnumerateArray().Where(t => S(t, "Proyecto") == g.Key).ToList();
            sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Sum(a => a.GetProperty("Lineas").GetInt32())} | {tipos.Count} | {tipos.Select(t => S(t, "Capa")).FirstOrDefault()} |");
        }
        return sb.ToString();
    }

    static string Tipos(string capa)
    {
        var sb = new StringBuilder("| Tipo | Clase | Propósito (comentario del código) | Archivo |\n|---|---|---|---|\n");
        foreach (var t in TiposProduccion().Where(t => S(t, "Capa").StartsWith(capa)).OrderBy(t => S(t, "Nombre")))
        {
            Registrar(S(t, "Nombre"));
            string resumen = S(t, "Resumen");
            sb.AppendLine($"| **{Celda(S(t, "Nombre"))}** | {S(t, "Modificadores").Replace("public", "").Trim()} {S(t, "Clase")} | {Celda(resumen.Length > 260 ? resumen[..260] + "…" : resumen)} | {Cod(Ruta(t))} |");
        }
        return sb.ToString();
    }

    static string Miembros(JsonElement t, bool soloPublicos)
    {
        var sb = new StringBuilder();
        if (t.TryGetProperty("Valores", out var vals) && vals.ValueKind == JsonValueKind.Array)
            sb.AppendLine("Valores: " + string.Join(", ", vals.EnumerateArray().Select(v => Cod(v.GetString() ?? ""))) + "\n");
        if (t.TryGetProperty("Parametros", out var ps) && ps.ValueKind == JsonValueKind.Array && ps.GetArrayLength() > 0)
            sb.AppendLine("Parámetros del record: " + string.Join(", ", ps.EnumerateArray().Select(v => Cod(v.GetString() ?? ""))) + "\n");
        var ms = t.GetProperty("Miembros").EnumerateArray().Where(m => !soloPublicos || S(m, "Modificadores").Contains("public") || S(m, "Modificadores").Contains("internal") || S(m, "Tipo") == "constructor").ToList();
        if (ms.Count == 0) return sb.ToString();
        sb.AppendLine("| Miembro | Clase de miembro | Firma | Qué hace |\n|---|---|---|---|");
        foreach (var m in ms)
        {
            string firma = S(m, "Tipo") is "método" or "constructor"
                ? $"{S(m, "Retorno")} {S(m, "Nombre")}({string.Join(", ", m.GetProperty("Parametros").EnumerateArray().Select(p => p.GetString()))})".Trim()
                : $"{S(m, "Retorno")} {S(m, "Nombre")}";
            sb.AppendLine($"| {Celda(S(m, "Nombre"))} | {S(m, "Tipo")} | {Cod(firma.Length > 150 ? firma[..150] + "…" : firma)} | {Celda(S(m, "Resumen"))} |");
        }
        return sb.ToString();
    }

    static string Detalle(string capa)
    {
        var sb = new StringBuilder();
        foreach (var t in TiposProduccion().Where(t => S(t, "Capa").StartsWith(capa)).OrderBy(t => S(t, "Archivo")).ThenBy(t => t.GetProperty("Linea").GetInt32()))
        {
            Registrar(S(t, "Nombre"));
            sb.AppendLine($"### {S(t, "Nombre")}\n");
            sb.AppendLine($"- **Archivo:** {Cod(Ruta(t))}\n- **Espacio de nombres:** {Cod(S(t, "Espacio"))}\n- **Declaración:** {Cod((S(t, "Modificadores") + " " + S(t, "Clase")).Trim())}");
            var bases = t.GetProperty("Bases").EnumerateArray().Select(b => b.GetString()).ToList();
            if (bases.Count > 0) sb.AppendLine($"- **Hereda o implementa:** {string.Join(", ", bases.Select(b => Cod(b!)))}");
            var pr = t.GetProperty("Pruebas").EnumerateArray().Select(p => p.GetString()!).ToList();
            sb.AppendLine($"- **Pruebas que lo usan:** {(pr.Count == 0 ? "ninguna clase de prueba lo nombra directamente" : string.Join(", ", pr.Take(10)) + (pr.Count > 10 ? $" y {pr.Count - 10} más" : ""))}\n");
            if (S(t, "Resumen") is { Length: > 0 } r) sb.AppendLine($"**Propósito según el código:** {Celda(r)}\n");
            sb.AppendLine(Miembros(t, soloPublicos: true));
        }
        return sb.ToString();
    }

    static string Snake(string pascal) => Regex.Replace(pascal, "(?<=[a-z0-9])([A-Z])", "_$1").ToUpperInvariant();

    static string Entidades(string notas)
    {
        var tablas = File.ReadAllLines(Path.Combine(_raiz, "docs/academico/inventario/oracle_tablas.txt")).Select(l => l.Split('|')).Where(p => p.Length >= 2).ToDictionary(p => p[0], p => p[1]);
        var notasPorEntidad = Regex.Split(notas, @"^## ", RegexOptions.Multiline).Skip(1)
            .ToDictionary(b => b[..b.IndexOf('\n')].Trim(), b => b[(b.IndexOf('\n') + 1)..], StringComparer.Ordinal);
        var sb = new StringBuilder();
        int n = 0;
        foreach (var t in TiposProduccion().Where(t => S(t, "Capa") == "Entidades").OrderBy(t => S(t, "Clase") == "enum").ThenBy(t => S(t, "Nombre")))
        {
            string nombre = S(t, "Nombre");
            Registrar(nombre);
            n++;
            sb.AppendLine($"## {n}. {nombre}\n");
            string tabla = Snake(nombre);
            string tablaOracle = tablas.ContainsKey(tabla) ? $"{Cod(tabla)}: {Celda(tablas[tabla])}" : S(t, "Clase") == "enum" ? "Se guarda como número en las columnas que lo usan (no tiene tabla propia)." : "No tiene tabla propia con ese nombre.";
            notasPorEntidad.TryGetValue(nombre, out string? nota);
            sb.AppendLine("| Aspecto | Detalle |\n|---|---|");
            sb.AppendLine($"| 1. Nombre | {Cod(nombre)} ({S(t, "Clase")}) |\n| 2. Ruta | {Cod(Ruta(t))} |\n| 3. Propósito (comentario del código) | {Celda(S(t, "Resumen"))} |");
            sb.AppendLine($"| 13. Tabla de Oracle | {tablaOracle} |");
            var pr = t.GetProperty("Pruebas").EnumerateArray().Select(p => p.GetString()!).ToList();
            sb.AppendLine($"| 17. Pruebas | {(pr.Count == 0 ? "—" : string.Join(", ", pr))} |\n");
            sb.AppendLine("**5–9. Propiedades, constructores, métodos y validaciones**\n");
            sb.AppendLine(Miembros(t, soloPublicos: true));
            if (tablas.ContainsKey(tabla)) sb.AppendLine($"**Columnas de {Cod(tabla)} en Oracle Cloud**\n\n" + OracleColumnas(tabla));
            if (nota is not null) sb.AppendLine(nota.Replace("\n### ", "\n#### "));
            else if (S(t, "Clase") == "enum")
                sb.AppendLine($"**Explicación sencilla.** Una enumeración es una lista cerrada de opciones con nombre. En lugar de escribir el número {(t.TryGetProperty("Valores", out var v) && v.GetArrayLength() > 0 ? v[0].GetString() : "")} en el código, se escribe su nombre, y el compilador impide usar una opción que no exista. Los números son explícitos porque Oracle guarda el número: cambiar el orden de la lista no debe cambiar los datos ya guardados.\n");
        }
        return sb.ToString();
    }

    static string Formularios(string filtro)
    {
        var sb = new StringBuilder();
        foreach (var f in _inv.GetProperty("Formularios").EnumerateArray().Where(f => filtro == "" || S(f, "Nombre") == filtro))
        {
            string nombre = S(f, "Nombre");
            Registrar(nombre);
            var tipo = _inv.GetProperty("Tipos").EnumerateArray().FirstOrDefault(t => S(t, "Nombre") == nombre && S(t, "Resumen") != "");
            sb.AppendLine($"### {nombre}\n");
            sb.AppendLine($"- **Archivos:** {string.Join(", ", f.GetProperty("Archivos").EnumerateArray().Select(a => Cod(a.GetString()!)))}");
            if (tipo.ValueKind == JsonValueKind.Object) sb.AppendLine($"- **Propósito según el código:** {Celda(S(tipo, "Resumen"))}");
            var controles = f.GetProperty("Controles").EnumerateObject().ToList();
            sb.AppendLine($"- **Controles declarados:** {controles.Count}\n");
            var textos = f.GetProperty("Textos").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
            var metodos = f.GetProperty("Metodos").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(x => x.GetString()!).ToList());
            var eventos = f.GetProperty("Eventos").EnumerateArray().ToList();
            if (eventos.Count > 0)
            {
                sb.AppendLine("| Control | Texto | Evento | Método que se ejecuta | Servicios que llama | Ruta |\n|---|---|---|---|---|---|");
                foreach (var e in eventos)
                {
                    string control = S(e, "Control"), man = S(e, "Manejador");
                    textos.TryGetValue(control, out string? txt);
                    metodos.TryGetValue(man, out var ll);
                    string manejador = man.Length > 70 ? man[..70] + "…" : man;
                    sb.AppendLine($"| {Cod(control)} | {Celda(txt ?? "")} | {S(e, "Nombre")} | {Cod(manejador)} | {Celda(string.Join(", ", ll ?? new()))} | {Cod($"{S(e, "Archivo").Split('/').Last()}:{e.GetProperty("Linea").GetInt32()}")} |");
                }
                sb.AppendLine();
            }
            if (controles.Count > 0)
                sb.AppendLine("Controles: " + string.Join(", ", controles.Take(60).Select(c => $"{Cod(c.Name)} ({c.Value.GetString()})")) + (controles.Count > 60 ? $" y {controles.Count - 60} más." : ".") + "\n");
        }
        return sb.ToString();
    }

    static string OracleTablas()
    {
        var cols = File.ReadAllLines(Path.Combine(_raiz, "docs/academico/inventario/oracle_columnas.txt")).Select(l => l.Split('|')[0]).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var sb = new StringBuilder("| Tabla | Columnas | Comentario en Oracle |\n|---|---|---|\n");
        foreach (var p in File.ReadAllLines(Path.Combine(_raiz, "docs/academico/inventario/oracle_tablas.txt")).Select(l => l.Split('|')).Where(p => p.Length >= 2))
            sb.AppendLine($"| {Cod(p[0])} | {cols.GetValueOrDefault(p[0])} | {Celda(p[1])} |");
        return sb.ToString();
    }

    static string OracleColumnas(string tabla)
    {
        var sb = new StringBuilder();
        foreach (var g in File.ReadAllLines(Path.Combine(_raiz, "docs/academico/inventario/oracle_columnas.txt")).Select(l => l.Split('|')).Where(p => p.Length >= 7 && (tabla == "" || p[0] == tabla)).GroupBy(p => p[0]))
        {
            if (tabla == "") { sb.AppendLine($"### {g.Key}\n"); Registrar("tabla " + g.Key); }
            sb.AppendLine("| Columna | Tipo | ¿Nulo? | Clave | Referencia | Comentario |\n|---|---|---|---|---|---|");
            foreach (var p in g)
            {
                string clave = string.Join(", ", p[4].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(k => k switch { "P" => "PK", "R" => "FK", "U" => "UNIQUE", _ => k }));
                sb.AppendLine($"| {Cod(p[1])} | {p[2]} | {(p[3] == "Y" ? "Sí" : "No")} | {clave} | {p[5]} | {Celda(p[6])} |");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    static string Commits()
    {
        var sb = new StringBuilder();
        foreach (var g in File.ReadAllLines(Path.Combine(_raiz, "docs/academico/inventario/commits.txt")).Select(l => l.Split('|', 4)).Where(p => p.Length == 4).GroupBy(p => p[0][..10]))
        {
            sb.AppendLine($"**{g.Key}** ({g.Count()} commits)\n\n| Hora | Commit | Autor | Mensaje |\n|---|---|---|---|");
            foreach (var p in g) sb.AppendLine($"| {p[0][11..]} | {Cod(p[1])} | {Celda(p[2])} | {Celda(p[3].Length > 180 ? p[3][..180] + "…" : p[3])} |");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    static string Pruebas(string filtro)
    {
        var sb = new StringBuilder("| Clase de prueba | Métodos | Archivo | Tipos de producción que usa |\n|---|---|---|---|\n");
        foreach (var c in _inv.GetProperty("ClasesPrueba").EnumerateArray().Where(c => filtro == "" || S(c, "Archivo").Contains(filtro)).OrderBy(c => S(c, "Archivo")))
            sb.AppendLine($"| {S(c, "Clase")} | {c.GetProperty("Metodos").GetInt32()} | {Cod(S(c, "Archivo").Replace("tests/", ""))} | {Celda(string.Join(", ", c.GetProperty("Tipos").EnumerateArray().Select(x => x.GetString()).Take(12)))} |");
        return sb.ToString();
    }

    // {{codigo:ruta/al/archivo.cs:desde:hasta}} copia esas líneas exactas del archivo, con su ruta.
    static string Codigo(string arg)
    {
        var p = arg.Split(':');
        string[] lineas = File.ReadAllLines(Path.Combine(_raiz, p[0]));
        int desde = int.Parse(p[1]), hasta = Math.Min(int.Parse(p[2]), lineas.Length);
        var cuerpo = lineas.Skip(desde - 1).Take(hasta - desde + 1).ToList();
        int sangria = cuerpo.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return $"*Código real: {Cod($"{p[0]}, líneas {desde}–{hasta}")}*\n\n```csharp\n{string.Join("\n", cuerpo.Select(l => l.Length >= sangria ? l[sangria..] : l))}\n```\n";
    }

    static string CoberturaTabla()
    {
        var sb = new StringBuilder("| Elemento del código | Clase | Documentado | Dónde | Evidencia (archivo) |\n|---|---|---|---|---|\n");
        int si = 0, total = 0;
        foreach (var t in TiposProduccion().OrderBy(t => S(t, "Capa")).ThenBy(t => S(t, "Nombre")))
        {
            total++;
            bool doc = Cobertura.TryGetValue(S(t, "Nombre"), out var donde);
            if (doc) si++;
            sb.AppendLine($"| {Cod(S(t, "Nombre"))} | {S(t, "Clase")} | {(doc ? "Sí" : "No")} | {Celda(doc ? string.Join("; ", donde!.Take(3)) : "—")} | {Cod(Ruta(t))} |");
        }
        foreach (var f in _inv.GetProperty("Formularios").EnumerateArray())
        {
            total++;
            bool doc = Cobertura.TryGetValue(S(f, "Nombre"), out var donde);
            if (doc) si++;
            sb.AppendLine($"| {Cod(S(f, "Nombre"))} (formulario) | formulario | {(doc ? "Sí" : "No")} | {Celda(doc ? string.Join("; ", donde!.Take(3)) : "—")} | {Cod(f.GetProperty("Archivos")[0].GetString()!)} |");
        }
        return $"Elementos documentados: **{si} de {total}** ({100.0 * si / total:F1} %).\n\n" + sb;
    }

    static string Envolver(Dictionary<string, string> meta, string cuerpo)
    {
        string t(string k) => WebUtility.HtmlEncode(meta.GetValueOrDefault(k, ""));
        string estilo = """
            body{font-family:Calibri,Arial,sans-serif;font-size:11pt;color:#1d2b22;line-height:1.35}
            h1{color:#1f6b3a;font-size:18pt;border-bottom:1.5px solid #9cc5a8;margin-top:22pt;page-break-after:avoid}
            h2{color:#1f6b3a;font-size:14pt;margin-top:16pt;page-break-after:avoid}
            h3{color:#2d5a3b;font-size:12pt;margin-top:12pt;page-break-after:avoid}
            h4{color:#2d5a3b;font-size:11pt;margin-top:10pt;page-break-after:avoid}
            table{border-collapse:collapse;width:100%;margin:6pt 0}
            th{background:#e3f0e6;text-align:left}
            td,th{border:1px solid #b9cfbf;padding:2pt 4pt;font-size:9pt;vertical-align:top}
            code{font-family:Consolas,monospace;font-size:8.5pt;color:#24492f}
            pre{background:#f4f8f5;border:1px solid #d5e4d9;padding:6pt;font-size:8.5pt;white-space:pre-wrap}
            pre code{color:#1d2b22}
            blockquote{border-left:3px solid #9cc5a8;margin-left:0;padding-left:8pt;color:#3b4a40}
            .portada{text-align:center;margin-top:90pt}
            .titulo-portada{font-size:28pt;color:#1f6b3a;font-weight:bold;text-align:center;margin-top:30pt}
            .sub{font-size:14pt;color:#3b4a40;text-align:center}
            .id{font-size:11pt;text-align:center;margin-top:40pt}
            .indice{page-break-before:always}
            """;
        string portada = $"""
            <div class="portada"><p class="sub">Universidad Popular del Cesar</p><p class="sub">Programación de Computadores III (SS462) y Base de Datos · Proyecto de aula</p>
            <p class="titulo-portada">{t("titulo")}</p><p class="sub">{t("subtitulo")}</p>
            <p class="id">Integrantes: Jorge Gutiérrez Thomas · Heyder Canchila · Saray Cadena</p>
            <p class="id">EcoAccess P3 · Documento {t("codigo")}<br/>Autor de los commits del repositorio según Git: Jorge Alfonso Gutiérrez Thomas<br/>Rama {Cod2("desarrollo/telegram-doble-ia")}, PR #11 · commit {WebUtility.HtmlEncode(S(_inv, "Commit"))}<br/>Noviembre de 2026</p></div>
            <p class="indice">[[INDICE]]</p>
            """;
        return $"<!doctype html><html lang='es'><head><meta charset='utf-8'><title>{t("titulo")}</title><style>{estilo}</style></head><body>{portada}{cuerpo}</body></html>";
        static string Cod2(string s) => $"<code>{s}</code>";
    }
}
