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

// Capturas del rediseño ExpoSoftware con las ventanas REALES de EcoAccess, en varios tamaños de ventana.
// Oracle es inalcanzable a propósito (127.0.0.1:1): no usa credenciales ni datos reales, así que los indicadores muestran
// «—» o el aviso de conexión. Se captura la pantalla (no DrawToBitmap) y se mide si hay controles fuera de su contenedor.
// Uso: CapturasExpo.exe <carpeta de salida>
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
        f.TopMost = true; f.Activate(); Bombear(500);
        using var bmp = new Bitmap(f.Width, f.Height);
        using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(f.Location, Point.Empty, f.Size);
        bmp.Save(Path.Combine(_salida, nombre + ".png"));
        f.TopMost = false;
        // Controles visibles que se salen por la derecha de su contenedor (texto o botón cortado).
        var fuera = Todos(f).Where(c => c.Visible && c.Parent is { } p && !(p is ScrollableControl { AutoScroll: true }) && c.Dock == DockStyle.None
            && c.Width > 0 && c.Right > p.ClientSize.Width + 2).Select(c => $"{c.GetType().Name} «{(c.Text.Length > 30 ? c.Text.Substring(0, 30) : c.Text)}» {c.Bounds} en {c.Parent!.ClientSize}").ToList();
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
                if (nombre == "1920x1080")
                {
                    Llamar(inicio, "Buscar", "botella"); Bombear(800);
                    Capturar(inicio, "03_busqueda_botella_1920x1080");
                    Llamar(inicio, "Buscar", "pila"); Bombear(800);
                    Capturar(inicio, "05_busqueda_pila_fuera_de_catalogo_1920x1080");
                }
                inicio.Hide();
            }
        }
        using (var invitado = new FormExplorarEcoAccess(servicios, null))
        {
            invitado.Show(); Tamano(invitado, tamanos[0].Fisico); Bombear(3000);
            Capturar(invitado, "06_inicio_invitado_1920x1080");
            invitado.Hide();
        }
        using (var residuos = new FormResiduosCaneca("Caneca verde: Orgánicos aprovechables (6)", "Elige el residuo que tienes para ver cómo depositarlo.",
                   EcoAccess.P3.Logica.Servicios.CatalogoPrototipo.DeCaneca(CategoriaMaterial.OrganicoAprovechable)))
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
}
