using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Presentacion;
using Catalogo = EcoAccess.P3.Logica.Servicios.CatalogoPrototipo;
using Residuo = EcoAccess.P3.Logica.Servicios.ResiduoPrototipo;

// Capturas del rediseño ExpoSoftware con las ventanas REALES de EcoAccess, en varios tamaños de ventana.
// Oracle es inalcanzable a propósito (127.0.0.1:1): no usa credenciales ni datos reales, así que los indicadores muestran
// «—» o el aviso de conexión. Se captura la pantalla (no DrawToBitmap) y se mide si hay controles fuera de su contenedor.
// Uso: CapturasExpo.exe <carpeta de salida> [catalogo]   («catalogo»: solo las ventanas de residuos por caneca)
static class Program
{
    static readonly List<string> Informe = new();
    static string _salida = "";

    static void Bombear(int ms)
    {
        var fin = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < fin) { Application.DoEvents(); Thread.Sleep(15); }
    }

    static IEnumerable<Control> Todos(Control c) { foreach (Control h in c.Controls) { yield return h; foreach (var n in Todos(h)) yield return n; } }

    static void Capturar(Form f, string nombre)
    {
        // El ratón fuera del contenido: una ayuda emergente no debe tapar lo que se revisa.
        Cursor.Position = new Point(f.Left + 4, f.Top + 4);
        f.TopMost = true; f.Activate(); Bombear(500);
        using var bmp = new Bitmap(f.Width, f.Height);
        using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(f.Location, Point.Empty, f.Size);
        bmp.Save(Path.Combine(_salida, nombre + ".png"));
        f.TopMost = false;
        // Controles visibles que se salen por la derecha de su contenedor (texto o botón cortado).
        var fuera = Todos(f).Where(c => c.Visible && c.Parent is { } p && !(p is ScrollableControl { AutoScroll: true }) && c.Dock == DockStyle.None
            && c.Width > 0 && (c.Right > p.ClientSize.Width + 2 || c.Bottom > p.ClientSize.Height + 2 || c.Left < -2 || c.Top < -2)).Select(c => $"{c.GetType().Name} «{(c.Text.Length > 30 ? c.Text.Substring(0, 30) : c.Text)}» {c.Bounds} en {c.Parent!.ClientSize}").ToList();
        var barras = Todos(f).OfType<ScrollableControl>().Where(s => s.Visible && s.AutoScroll && s.HorizontalScroll.Visible).Select(s => s.GetType().Name).ToList();
        var vertical = Todos(f).OfType<ScrollableControl>().Where(s => s.Visible && s.AutoScroll && s.VerticalScroll.Visible).Select(s => s.GetType().Name).ToList();
        Informe.Add($"{nombre}: ventana {f.Size} cliente {f.ClientSize} DPI {f.DeviceDpi} · fuera del contenedor {fuera.Count} · desplazamiento horizontal [{string.Join(",", barras)}] · vertical [{string.Join(",", vertical)}]");
        foreach (string x in fuera.Take(5)) Informe.Add("    " + x);
    }

    static void Tamano(Form f, Size fisico)
    {
        for (int intento = 0; intento < 3 && (f.WindowState != FormWindowState.Normal || f.Size != fisico); intento++)
        {
            f.WindowState = FormWindowState.Normal;
            f.Location = Point.Empty;
            f.Size = fisico;
            f.Activate();
            Bombear(1200);
        }
    }

    static object? Llamar(object o, string metodo, params object?[] args) =>
        o.GetType().GetMethod(metodo, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(o, args);

    [STAThread]
    static int Main(string[] args)
    {
        _salida = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "capturas_expo");
        Directory.CreateDirectory(_salida);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (args.Length > 1 && args[1] == "catalogo") return CapturasCatalogo();
        var servicios = EcoAccess.P3.Presentacion.Program.ComponerServicios("User Id=capturas;Data Source=127.0.0.1:1/ninguno;Connection Timeout=2", "EcoAccessP3_JG_bot");
        var sesion = new SesionActiva(1, "Jorgito Demo", new[] { CodigoRol.Ciudadano }, DateTime.UtcNow);
        // Tamaños físicos de ventana: 1920×1080 (con barra de tareas), 1600×900 y 1366×768. Con la escala del equipo.
        var tamanos = new (string Nombre, Size Fisico)[] { ("1920x1080", new Size(1920, 1032)), ("1600x900", new Size(1600, 860)), ("1366x768", new Size(1366, 728)) };

        foreach (var (nombre, fisico) in tamanos)
        {
            using (var acceso = new FormInicioSesion(servicios.Autenticacion, servicios.RegistroCiudadanos))
            {
                acceso.Show(); Tamano(acceso, fisico); Bombear(1500);
                Capturar(acceso, $"01_login_{nombre}");
                acceso.Hide();
            }
            using (var inicio = new FormExplorarEcoAccess(servicios, sesion))
            {
                inicio.Show(); Tamano(inicio, fisico); Bombear(4000);
                Capturar(inicio, $"02_inicio_ciudadano_{nombre}");
                // Búsquedas en todas las resoluciones: el resultado tampoco puede desbordar ni pedir desplazamiento.
                foreach (var (texto, archivo) in new[] { ("botellita", "03a_busqueda_botellita"), ("servileta", "03b_busqueda_error_ortografico"),
                             ("vaso", "03c_busqueda_ambigua_vaso"), ("pila", "05_busqueda_pila_manejo_especial"), ("helicoptero", "05b_sin_resultados"),
                             ("cascara de banano", "03d_busqueda_verde"), ("condon", "03e_busqueda_negra") })
                {
                    Llamar(inicio, "Buscar", texto); Bombear(900);
                    Capturar(inicio, $"{archivo}_{nombre}");
                }
                Llamar(inicio, "ElegirCaneca", CategoriaMaterial.NoAprovechable); Bombear(600);
                Capturar(inicio, $"03f_caneca_negra_seleccionada_{nombre}");
                inicio.Hide();
            }
            if (nombre != "1600x900")
                using (var catalogo = new FormCatalogoMateriales())
                {
                    catalogo.Show(); Tamano(catalogo, new Size(Math.Min(fisico.Width, 1280), Math.Min(fisico.Height, 800))); Bombear(1500);
                    Capturar(catalogo, $"10_catalogo_todas_{nombre}");
                    if (nombre == "1920x1080")
                    {
                        Llamar(catalogo, "Filtrar", (CategoriaMaterial?)CategoriaMaterial.NoAprovechable); Bombear(800);
                        Capturar(catalogo, "11_catalogo_negra");
                        Llamar(catalogo, "Filtrar", (CategoriaMaterial?)null); Bombear(300);
                        var caja = Todos(catalogo).OfType<TextBox>().First(); caja.Text = "carton"; Bombear(800);
                        Capturar(catalogo, "12_catalogo_busqueda_carton");
                    }
                    catalogo.Hide();
                }
        }
        using (var invitado = new FormExplorarEcoAccess(servicios, null))
        {
            invitado.Show(); Tamano(invitado, tamanos[0].Fisico); Bombear(3000);
            Capturar(invitado, "06_inicio_invitado_1920x1080");
            Tamano(invitado, tamanos[2].Fisico); Bombear(1500);
            Capturar(invitado, "06_inicio_invitado_1366x768");
            invitado.Hide();
        }
        using (var residuos = new FormResiduosCaneca("Caneca verde — Orgánicos aprovechables", "Elige el residuo que tienes para ver cómo depositarlo.",
                   Catalogo.VisiblesDe(CategoriaMaterial.OrganicoAprovechable), CategoriaMaterial.OrganicoAprovechable))
        {
            residuos.Show(); Bombear(1500);
            Capturar(residuos, "04_residuos_caneca_verde");
            residuos.Hide();
        }
        using (var asistente = new FormAsistenteDeposito(servicios, sesion, null))
        {
            asistente.Show(); Bombear(1500);
            Capturar(asistente, "07_deposito_paso1_residuo");
            Llamar(asistente, "ElegirYResolverAsync", EcoAccess.P3.Logica.Servicios.CatalogoPrototipo.Buscar("botella-agua")); Bombear(6000);
            Capturar(asistente, "08_deposito_paso2_caneca_sin_oracle");
            asistente.Hide();
        }
        using (var puntos = new FormMisEcopuntos(servicios.Ecopuntos, servicios.Estadisticas, 1))
        {
            puntos.Show(); Bombear(6000);
            Capturar(puntos, "09_mis_ecopuntos_sin_oracle");
            puntos.Hide();
        }
        File.WriteAllLines(Path.Combine(_salida, "informe.txt"), Informe, new UTF8Encoding(false));
        return 0;
    }

    /// <summary>
    /// Ventana de residuos de las tres canecas y de listas de 0, 4, 7, 10, 13 y 20 residuos (más nombres largos, búsqueda
    /// y ventana maximizada). Mide: tarjetas = contador, tarjetas superpuestas y el espacio vacío bajo la última fila
    /// cuando no hay desplazamiento. Escribe informe_catalogo.txt; devuelve 1 si algo no cumple.
    /// </summary>
    static int CapturasCatalogo()
    {
        var todos = Catalogo.Todos.Where(Catalogo.TieneImagen).ToList();
        var largos = todos.Take(6).Select(r => r with { Nombre = r.Nombre + " con un nombre bastante largo para comprobar que ocupa dos o tres líneas sin cortarse" }).ToList();
        var casos = new List<(string Nombre, IReadOnlyList<Residuo> Lista, CategoriaMaterial? Caneca, string? Buscar, bool Maximizar)>
        {
            ("20_verde", Catalogo.VisiblesDe(CategoriaMaterial.OrganicoAprovechable), CategoriaMaterial.OrganicoAprovechable, null, false),
            ("21_blanca", Catalogo.VisiblesDe(CategoriaMaterial.Aprovechable), CategoriaMaterial.Aprovechable, null, false),
            ("22_negra", Catalogo.VisiblesDe(CategoriaMaterial.NoAprovechable), CategoriaMaterial.NoAprovechable, null, false),
            ("23_verde_busqueda_cascara", Catalogo.VisiblesDe(CategoriaMaterial.OrganicoAprovechable), CategoriaMaterial.OrganicoAprovechable, "cascara", false),
            ("24_verde_maximizada", Catalogo.VisiblesDe(CategoriaMaterial.OrganicoAprovechable), CategoriaMaterial.OrganicoAprovechable, null, true),
            ("25_lista_4", todos.Take(4).ToList(), null, null, false),
            ("26_lista_7", todos.Take(7).ToList(), null, null, false),
            ("27_lista_10", todos.Take(10).ToList(), null, null, false),
            ("28_lista_13", todos.Take(13).ToList(), null, null, false),
            ("29_lista_20", todos.Take(20).ToList(), null, null, false),
            ("30_vacia", new List<Residuo>(), CategoriaMaterial.NoAprovechable, null, false),
            ("31_nombres_largos", largos, null, null, false),
            ("32_negra_sin_imagen_excluidos", Catalogo.DeCaneca(CategoriaMaterial.NoAprovechable), CategoriaMaterial.NoAprovechable, null, false),
        };
        int fallas = 0;
        foreach (var caso in casos)
        {
            using var f = new FormResiduosCaneca(caso.Nombre, "Elige el residuo que tienes para ver cómo depositarlo.", caso.Lista, caso.Caneca);
            f.Show(); Bombear(1200);
            if (caso.Maximizar) { f.WindowState = FormWindowState.Maximized; Bombear(1200); }
            if (caso.Buscar is { } texto) { Todos(f).OfType<TextBox>().First().Text = texto; Bombear(800); }
            Capturar(f, caso.Nombre);
            var tarjetas = Todos(f).Where(c => c.GetType().Name == "TarjetaResiduo").ToList();
            var panel = Todos(f).OfType<ScrollableControl>().First(c => c.GetType().Name == "PanelDesplazable");
            int esperadas = caso.Lista.Where(r => caso.Caneca is null || Catalogo.TieneImagen(r)).Select(r => r.Id).Distinct().Count();
            string contador = Todos(f).OfType<Label>().Select(l => l.Text).FirstOrDefault(t => t.Contains("residuo") && t.Length < 40) ?? "";
            int superpuestas = 0;
            var cajas = tarjetas.Select(t => f.RectangleToClient(t.RectangleToScreen(t.ClientRectangle))).ToList();
            for (int i = 0; i < cajas.Count; i++)
                for (int j = i + 1; j < cajas.Count; j++)
                    if (cajas[i].IntersectsWith(cajas[j])) superpuestas++;
            int vacio = 0;
            if (tarjetas.Count > 0 && !panel.VerticalScroll.Visible)
                vacio = panel.ClientSize.Height - tarjetas.Max(t => panel.RectangleToClient(t.RectangleToScreen(t.ClientRectangle)).Bottom);
            int limite = (int)(80 * f.DeviceDpi / 96.0);
            bool bien = superpuestas == 0 && (caso.Buscar is not null || tarjetas.Count == esperadas) && (caso.Maximizar || caso.Buscar is not null || vacio <= limite);
            if (!bien) fallas++;
            Informe.Add($"    {(bien ? "OK" : "REVISAR")} tarjetas {tarjetas.Count} (esperadas {esperadas}) · contador «{contador}» · superpuestas {superpuestas} · vacío bajo la última fila {vacio}px (límite {limite}) · desplazamiento {panel.VerticalScroll.Visible}");
            f.Hide();
        }
        File.WriteAllLines(Path.Combine(_salida, "informe_catalogo.txt"), Informe, new UTF8Encoding(false));
        return fallas == 0 ? 0 : 1;
    }
}
