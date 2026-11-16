// Inventario verificable del código de EcoAccess P3 (fase 8.1).
// Uso: dotnet run -c Release -- <raíz del repositorio> <carpeta de salida> <commit>
// Lee cada .cs propio (src, tests), sin bin/obj, y produce inventario.json más tablas CSV. No compila ni ejecuta el código.
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length > 0 && args[0] == "docs") { Documentos.Generar(args[1], args[2], args[3]); return; }
string raiz = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
string salida = Path.GetFullPath(args.Length > 1 ? args[1] : "inventario");
string commit = args.Length > 2 ? args[2].Trim() : "";
Directory.CreateDirectory(salida);

var archivos = new[] { "src", "tests" }.Select(d => Path.Combine(raiz, d)).Where(Directory.Exists)
    .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
    .Where(f => !Regex.IsMatch(f, @"[\\/](bin|obj|TestResults)[\\/]"))
    .OrderBy(f => f, StringComparer.Ordinal).ToList();

var tipos = new List<Tipo>();
var formularios = new Dictionary<string, Formulario>(StringComparer.Ordinal);
var archivosInfo = new List<ArchivoInfo>();

foreach (string archivo in archivos)
{
    string relativo = Path.GetRelativePath(raiz, archivo).Replace('\\', '/');
    string proyecto = Proyecto(relativo);
    string texto = File.ReadAllText(archivo);
    var unidad = CSharpSyntaxTree.ParseText(texto, new CSharpParseOptions(LanguageVersion.Latest)).GetCompilationUnitRoot();
    bool generado = relativo.EndsWith(".Designer.cs") || relativo.EndsWith(".g.cs") || relativo.Contains("/Properties/");
    archivosInfo.Add(new ArchivoInfo(proyecto, relativo, texto.Split('\n').Length, generado));

    foreach (var decl in unidad.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
    {
        string espacio = decl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
        string nombre = decl.Identifier.Text;
        if (decl.Parent is BaseTypeDeclarationSyntax padre) nombre = padre.Identifier.Text + "." + nombre;
        string clase = decl switch
        {
            InterfaceDeclarationSyntax => "interface",
            EnumDeclarationSyntax => "enum",
            RecordDeclarationSyntax r => r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record",
            StructDeclarationSyntax => "struct",
            _ => "class"
        };
        var mods = decl.Modifiers.Select(m => m.Text).ToList();
        var tipo = new Tipo
        {
            Proyecto = proyecto, Archivo = relativo, Linea = Linea(decl), Espacio = espacio, Nombre = nombre, Clase = clase,
            Modificadores = string.Join(' ', mods), Bases = decl.BaseList?.Types.Select(t => t.ToString()).ToList() ?? new(),
            Resumen = Resumen(decl), Parcial = mods.Contains("partial"), Capa = Capa(proyecto, relativo),
        };
        if (decl is EnumDeclarationSyntax e)
            tipo.Valores = e.Members.Select(m => m.Identifier.Text + (m.EqualsValue is null ? "" : " = " + m.EqualsValue.Value)).ToList();
        if (decl is TypeDeclarationSyntax td)
        {
            if (td.ParameterList is { } pl) tipo.Parametros = pl.Parameters.Select(p => p.ToString()).ToList();
            foreach (var miembro in td.Members)
                if (Miembro(miembro) is { } m) tipo.Miembros.Add(m);
        }
        tipos.Add(tipo);
    }

    // Formularios: controles, textos, eventos y servicios que llaman los métodos.
    if (proyecto != "EcoAccess.P3.Presentacion") continue;
    foreach (var cls in unidad.DescendantNodes().OfType<ClassDeclarationSyntax>())
    {
        string n = cls.Identifier.Text;
        bool esFormulario = n.StartsWith("Form") || cls.BaseList?.Types.Any(t => t.ToString() is "Form" or "UserControl") == true;
        if (!esFormulario) continue;
        if (!formularios.TryGetValue(n, out var f)) formularios[n] = f = new Formulario { Nombre = n };
        f.Archivos.Add(relativo);
        foreach (var asig in cls.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            string izq = asig.Left.ToString().Replace("this.", "");
            if (asig.IsKind(SyntaxKind.AddAssignmentExpression) && izq.Contains('.'))
            {
                string control = izq[..izq.LastIndexOf('.')];
                string evento = izq[(izq.LastIndexOf('.') + 1)..];
                string manejador = asig.Right is IdentifierNameSyntax id ? id.Identifier.Text : Compactar(asig.Right.ToString(), 160);
                f.Eventos.Add(new Evento(control, evento, manejador, relativo, Linea(asig)));
            }
            else if (asig.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                string manejador = asig.Right is IdentifierNameSyntax id ? id.Identifier.Text : Compactar(asig.Right.ToString(), 160);
                f.Eventos.Add(new Evento("(formulario)", izq, manejador, relativo, Linea(asig)));
            }
            else if (izq.EndsWith(".Text") && asig.Right is LiteralExpressionSyntax lit)
                f.Textos[izq[..^5]] = lit.Token.ValueText;
            if (asig.Right is BaseObjectCreationExpressionSyntax creado && !izq.Contains('(') && !izq.Contains('.'))
                f.Controles.TryAdd(izq, creado is ObjectCreationExpressionSyntax o ? o.Type.ToString() : "?");
        }
        foreach (var fld in cls.Members.OfType<FieldDeclarationSyntax>())
            foreach (var v in fld.Declaration.Variables)
                if (fld.Declaration.Type.ToString() is { } t && Regex.IsMatch(t, "(Button|Label|TextBox|ComboBox|DataGridView|Panel|CheckBox|LinkLabel|NumericUpDown|DateTimePicker|ListBox|PictureBox|TabControl|Chart|Krypton|Icon|Tarjeta|Grafico|Menu|ToolStrip|RadioButton|FlowLayoutPanel|TableLayoutPanel|GroupBox)"))
                    f.Controles[v.Identifier.Text] = t;
        foreach (var met in cls.Members.OfType<MethodDeclarationSyntax>())
            f.Metodos[met.Identifier.Text] = met.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(i => i.Expression.ToString()).Where(s => s.StartsWith("_") && s.Contains('.'))
                .Select(s => Compactar(s, 90)).Distinct().Take(12).ToList();
    }
}

// Pruebas: qué tipos de producción nombra cada clase de prueba (por identificadores; verificable a mano).
var produccion = tipos.Where(t => t.Capa != "Pruebas").Select(t => t.Nombre.Split('.')[0]).ToHashSet(StringComparer.Ordinal);
var pruebasPorTipo = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
var clasesPrueba = new List<ClasePrueba>();
foreach (string archivo in archivos.Where(a => Path.GetRelativePath(raiz, a).Replace('\\', '/').StartsWith("tests/")))
{
    var unidad = CSharpSyntaxTree.ParseText(File.ReadAllText(archivo)).GetCompilationUnitRoot();
    foreach (var cls in unidad.DescendantNodes().OfType<ClassDeclarationSyntax>())
    {
        int metodos = cls.Members.OfType<MethodDeclarationSyntax>().Count(m => m.AttributeLists.SelectMany(l => l.Attributes)
            .Any(at => at.Name.ToString() is "TestMethod" or "DataTestMethod"));
        if (metodos == 0) continue;
        var usados = cls.DescendantNodes().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.Text).Where(produccion.Contains).ToHashSet();
        clasesPrueba.Add(new ClasePrueba(cls.Identifier.Text, Path.GetRelativePath(raiz, archivo).Replace('\\', '/'), metodos, usados.OrderBy(x => x).ToList()));
        foreach (string u in usados)
        {
            if (!pruebasPorTipo.TryGetValue(u, out var set)) pruebasPorTipo[u] = set = new(StringComparer.Ordinal);
            set.Add(cls.Identifier.Text);
        }
    }
}
foreach (var t in tipos)
    if (pruebasPorTipo.TryGetValue(t.Nombre.Split('.')[0], out var s)) t.Pruebas = s.ToList();

var opciones = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
File.WriteAllText(Path.Combine(salida, "inventario.json"), JsonSerializer.Serialize(new
{
    Commit = commit, FechaUtc = DateTime.UtcNow, Archivos = archivosInfo, Tipos = tipos,
    Formularios = formularios.Values.OrderBy(f => f.Nombre).ToList(), ClasesPrueba = clasesPrueba
}, opciones), new UTF8Encoding(false));

var csv = new StringBuilder("PROYECTO;ARCHIVO;CLASE;TIPO;CAPA;DEPENDENCIAS;METODOS_PUBLICOS;PRUEBAS\n");
foreach (var t in tipos.OrderBy(t => t.Proyecto).ThenBy(t => t.Archivo).ThenBy(t => t.Linea))
{
    var deps = t.Miembros.Where(m => m.Tipo == "constructor").SelectMany(m => m.Parametros).Select(p => p.Split(' ')[0]).Distinct();
    var pub = t.Miembros.Where(m => m.Tipo == "método" && m.Modificadores.Contains("public")).Select(m => m.Nombre).Distinct();
    csv.AppendLine(Limpio(string.Join(';', t.Proyecto, t.Archivo, t.Nombre, t.Clase, t.Capa, string.Join(", ", deps), string.Join(", ", pub), string.Join(", ", t.Pruebas))));
}
File.WriteAllText(Path.Combine(salida, "matriz_clases.csv"), csv.ToString(), new UTF8Encoding(true));

var csvF = new StringBuilder("FORMULARIO;CONTROL;TIPO;TEXTO;EVENTO;METODO;SERVICIOS_LLAMADOS;RUTA\n");
foreach (var f in formularios.Values.OrderBy(f => f.Nombre))
    foreach (var e in f.Eventos)
    {
        f.Textos.TryGetValue(e.Control, out string? txt);
        f.Controles.TryGetValue(e.Control, out string? tc);
        f.Metodos.TryGetValue(e.Manejador, out var ll);
        csvF.AppendLine(Limpio(string.Join(';', f.Nombre, e.Control, tc ?? "", txt ?? "", e.Nombre, e.Manejador.Replace(';', ','), string.Join(", ", ll ?? new()), $"{e.Archivo}:{e.Linea}")));
    }
File.WriteAllText(Path.Combine(salida, "matriz_formularios.csv"), csvF.ToString(), new UTF8Encoding(true));

Console.WriteLine($"Archivos: {archivos.Count}; tipos: {tipos.Count} ({tipos.Count(t => t.Capa != "Pruebas")} de producción); " +
    $"formularios: {formularios.Count}; eventos: {formularios.Values.Sum(f => f.Eventos.Count)}; clases de prueba: {clasesPrueba.Count}; métodos de prueba: {clasesPrueba.Sum(c => c.Metodos)}");

static int Linea(SyntaxNode n) => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
static string Limpio(string s) => s.Replace("\r", "").Replace("\n", " ");
static string Proyecto(string rel) { var p = rel.Split('/'); return p[0] == "src" ? p[1] : p.Length > 2 ? p[2] : p[0]; }
static string Capa(string proyecto, string rel) => rel.StartsWith("tests/") ? "Pruebas" : proyecto switch
{
    "EcoAccess.P3.Entidades" => "Entidades",
    "EcoAccess.P3.Logica" => rel.Contains("/Contratos/") ? "Lógica (contratos)" : "Lógica",
    "EcoAccess.P3.Datos" => "Datos",
    "EcoAccess.P3.Presentacion" => "Presentación (Windows Forms)",
    "EcoAccess.P3.Bot" => "Presentación (bot de Telegram)",
    "EcoAccess.P3.Compatibilidad" => "Compatibilidad net48",
    _ => proyecto
};

static string Resumen(SyntaxNode n)
{
    var doc = n.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
    if (doc is null) return "";
    var sum = doc.Content.OfType<XmlElementSyntax>().FirstOrDefault(x => x.StartTag.Name.ToString() == "summary");
    if (sum is null) return "";
    string texto = string.Concat(sum.Content.Select(Texto));
    return Compactar(Regex.Replace(texto, @"(^|\n)\s*///\s?", " "), 700);

    static string Texto(XmlNodeSyntax c) => c switch
    {
        XmlEmptyElementSyntax v => v.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault()?.Cref.ToString()
            ?? v.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.ToString() ?? "",
        XmlElementSyntax el => string.Concat(el.Content.Select(Texto)),
        XmlTextSyntax t => string.Concat(t.TextTokens.Select(k => k.Text)),
        _ => ""
    };
}

static string Compactar(string s, int max) { s = Regex.Replace(s, @"\s+", " ").Trim(); return s.Length > max ? s[..max] + "…" : s; }

static MiembroInfo? Miembro(MemberDeclarationSyntax m)
{
    string mods = string.Join(' ', m.Modifiers.Select(x => x.Text));
    int linea = Linea(m);
    return m switch
    {
        ConstructorDeclarationSyntax c => new MiembroInfo("constructor", c.Identifier.Text, mods, "", c.ParameterList.Parameters.Select(p => p.ToString()).ToList(), Resumen(c), linea),
        MethodDeclarationSyntax me => new MiembroInfo("método", me.Identifier.Text, mods, me.ReturnType.ToString(), me.ParameterList.Parameters.Select(p => p.ToString()).ToList(), Resumen(me), linea),
        PropertyDeclarationSyntax p => new MiembroInfo("propiedad", p.Identifier.Text, mods, p.Type.ToString(), new(), Resumen(p), linea),
        FieldDeclarationSyntax f when f.Modifiers.Any(x => x.Text is "public" or "internal" or "protected" or "const") =>
            new MiembroInfo(f.Modifiers.Any(x => x.Text == "const") ? "constante" : "campo", string.Join(", ", f.Declaration.Variables.Select(v => v.Identifier.Text)), mods, f.Declaration.Type.ToString(), new(), Resumen(f), linea),
        EventFieldDeclarationSyntax ev => new MiembroInfo("evento", string.Join(", ", ev.Declaration.Variables.Select(v => v.Identifier.Text)), mods, ev.Declaration.Type.ToString(), new(), Resumen(ev), linea),
        _ => null
    };
}

sealed record ArchivoInfo(string Proyecto, string Archivo, int Lineas, bool Generado);
sealed record MiembroInfo(string Tipo, string Nombre, string Modificadores, string Retorno, List<string> Parametros, string Resumen, int Linea);
sealed record Evento(string Control, string Nombre, string Manejador, string Archivo, int Linea);
sealed record ClasePrueba(string Clase, string Archivo, int Metodos, List<string> Tipos);
sealed class Tipo
{
    public string Proyecto { get; set; } = ""; public string Archivo { get; set; } = ""; public int Linea { get; set; }
    public string Espacio { get; set; } = ""; public string Nombre { get; set; } = ""; public string Clase { get; set; } = "";
    public string Modificadores { get; set; } = ""; public List<string> Bases { get; set; } = new(); public string Resumen { get; set; } = "";
    public bool Parcial { get; set; }
    public string Capa { get; set; } = ""; public List<string>? Valores { get; set; } public List<string>? Parametros { get; set; }
    public List<MiembroInfo> Miembros { get; set; } = new(); public List<string> Pruebas { get; set; } = new();
}
sealed class Formulario
{
    public string Nombre { get; set; } = ""; public SortedSet<string> Archivos { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Controles { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Textos { get; set; } = new(StringComparer.Ordinal);
    public List<Evento> Eventos { get; set; } = new();
    public Dictionary<string, List<string>> Metodos { get; set; } = new(StringComparer.Ordinal);
}
