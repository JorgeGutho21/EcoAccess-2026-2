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

// Reproduce minimizar, restaurar, maximizar y cambiar de pestaña con la ventana principal REAL y cada módulo, con Oracle
// inalcanzable (127.0.0.1:1). Compara la geometría de todos los controles visibles antes y después de cada acción.
static class Program
{
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumThreadWindows(uint id, EnumProc p, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder b, int n);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    static uint _hilo;
    static readonly List<string> Log = new();
    static string _salida = "";

    static void Cerrar(object? _)
    {
        EnumThreadWindows(_hilo, (h, l) =>
        {
            var c = new StringBuilder(64); GetClassName(h, c, 64);
            if (c.ToString() == "#32770") { lock (Log) Log.Add("  (diálogo cerrado automáticamente)"); PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); }
            return true;
        }, IntPtr.Zero);
    }

    static IEnumerable<Control> Todos(Control c) { foreach (Control h in c.Controls) { yield return h; foreach (var n in Todos(h)) yield return n; } }

    static void Bombear(int ms = 400)
    {
        var fin = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < fin) { Application.DoEvents(); Thread.Sleep(15); }
    }

    static string Ruta(Control c) { var p = new List<string>(); for (Control? x = c; x is not null; x = x.Parent) p.Add($"{x.GetType().Name}[{x.Parent?.Controls.GetChildIndex(x) ?? 0}]"); p.Reverse(); return string.Join("/", p); }

    static Dictionary<string, Rectangle> Firma(Form f) =>
        Todos(f).Where(c => c.Visible && c.Width > 0).GroupBy(Ruta).ToDictionary(g => g.Key, g => g.First().Bounds);

    // Controles hermanos visibles que se pisan (excluye los que llenan al padre y las pestañas ocultas).
    static List<string> Superpuestos(Form f)
    {
        var r = new List<string>();
        foreach (Control padre in Todos(f).Prepend(f))
        {
            if (!padre.Visible || padre is TabControl) continue;
            var hijos = padre.Controls.Cast<Control>().Where(c => c.Visible && c.Dock != DockStyle.Fill && c.Width > 0 && c.Height > 0).ToList();
            for (int i = 0; i < hijos.Count; i++)
                for (int j = i + 1; j < hijos.Count; j++)
                {
                    var a = Rectangle.Intersect(hijos[i].Bounds, hijos[j].Bounds);
                    if (a.Width > 2 && a.Height > 2) r.Add($"{Ruta(hijos[i])} ∩ {hijos[j].GetType().Name}[{padre.Controls.GetChildIndex(hijos[j])}] = {a.Width}x{a.Height}");
                }
        }
        return r;
    }

    static int Diferencias(string etiqueta, Dictionary<string, Rectangle> antes, Dictionary<string, Rectangle> despues)
    {
        var dif = antes.Keys.Union(despues.Keys).Where(k => !antes.TryGetValue(k, out var a) || !despues.TryGetValue(k, out var b) || a != b).ToList();
        Log.Add($"  {etiqueta}: {(dif.Count == 0 ? "sin cambios" : dif.Count + " controles cambiaron")}");
        foreach (string k in dif.Take(8))
            Log.Add($"     {k}: {(antes.TryGetValue(k, out var a) ? a.ToString() : "(no estaba)")} → {(despues.TryGetValue(k, out var b) ? b.ToString() : "(desapareció)")}");
        return dif.Count;
    }

    static void Captura(Form f, string nombre)
    {
        using var bmp = new Bitmap(f.Width, f.Height);
        f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
        bmp.Save(Path.Combine(_salida, nombre + ".png"));
    }

    static string Menu(Form f) { var raiz = f.Controls.OfType<TableLayoutPanel>().FirstOrDefault(); if (raiz is null) return "sin raíz"; var lat = raiz.GetControlFromPosition(0, 0); var cen = raiz.GetControlFromPosition(2, 0); return $"raíz {raiz.Size} filas [{string.Join(",", raiz.GetRowHeights())}] menú {lat?.Size} centro {cen?.Size} estado {f.WindowState} cliente {f.ClientSize}"; }
    static void Estado(Form f, FormWindowState e) { f.WindowState = e; Bombear(); Log.Add($"    → {e}: {Menu(f)} módulo {string.Join(",", Todos(f).Where(x => x.Parent?.GetType().Name == "PanelDesplazable" && x.Parent.Parent is TableLayoutPanel).Select(x => x.GetType().Name))}"); if (f.WindowState != e) Log.Add($"  ¡pedido {e} pero quedó {f.WindowState}! tamaño {f.Size}"); }

    [STAThread]
    static int Main(string[] args)
    {
        _salida = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "arnes_ventanas");
        Directory.CreateDirectory(_salida);
        _hilo = GetCurrentThreadId();
        using var vigia = new System.Threading.Timer(Cerrar, null, 300, 300);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Oracle inalcanzable a propósito: ninguna credencial real y ningún dato de producción.
        var servicios = EcoAccess.P3.Presentacion.Program.ComponerServicios("User Id=arnes;Password=arnes;Data Source=127.0.0.1:1/ninguno;Connection Timeout=2", "EcoAccessP3_JG_bot");
        var sesion = new SesionActiva(1, "Arnés de ventanas", new[] { CodigoRol.Administrador, CodigoRol.Operario }, DateTime.UtcNow);
        var area = args.Length > 1 && args[1] == "operario" ? AreaAcceso.Operario : AreaAcceso.Supervisor;
        using var principal = new FormPrincipal(servicios, sesion, area);
        principal.Show();
        Estado(principal, FormWindowState.Normal);
        principal.ClientSize = new Size(1280, 760);
        Bombear(1500);

        var botones = Todos(principal).Where(c => c.GetType().Name == "BotonNavegacion" && c.Visible).Select(c => c.Text).Distinct().Where(t => Environment.GetEnvironmentVariable("ARNES_SOLO") is not { } solo || solo.Split('|').Contains(t)).Where(t => !t.Contains("Área") && !t.Contains("Ciudadano") && !t.Contains("Cerrar") && !t.Contains("Power BI")).ToList();
        Log.Add($"Área {area}; módulos en el menú: {string.Join(", ", botones)}");
        int total = 0; var superpuestos = new HashSet<string>();
        foreach (string texto in botones)
        {
            Control boton = Todos(principal).First(c => c.GetType().Name == "BotonNavegacion" && c.Text == texto);
            typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(boton, new object[] { EventArgs.Empty });
            Bombear(3500);
            Log.Add($"    → abierto {texto}: {Menu(principal)}");
            if (principal.WindowState != FormWindowState.Normal) { Log.Add($"  ¡AVISO! la ventana quedó {principal.WindowState} al abrir {texto} sin que el arnés lo pidiera; se restaura antes de medir"); Estado(principal, FormWindowState.Normal); Bombear(800); }
            if (principal.IsDisposed) { Log.Add($"[{texto}] cerró la ventana principal (cambio de área): se omite"); break; }
            string nombre = new string(texto.Where(char.IsLetterOrDigit).ToArray());
            Log.Add($"[{texto}]");
            var s0 = Firma(principal);
            foreach (var x in Superpuestos(principal)) if (superpuestos.Add(x)) Log.Add($"  superpuestos al abrir: {x}");
            Captura(principal, nombre + "_0_normal");
            Estado(principal, FormWindowState.Minimized); Estado(principal, FormWindowState.Normal); Bombear(600);
            total += Diferencias("minimizar → restaurar", s0, Firma(principal));
            Captura(principal, nombre + "_1_restaurado");
            Estado(principal, FormWindowState.Maximized); Estado(principal, FormWindowState.Normal); Bombear(600);
            total += Diferencias("maximizar → restaurar", s0, Firma(principal));
            foreach (TabControl pestanas in Todos(principal).OfType<TabControl>().Where(t => t.Visible).ToList())
            {
                int original = pestanas.SelectedIndex;
                for (int i = 0; i < pestanas.TabCount; i++) { pestanas.SelectedIndex = i; Bombear(250); }
                Estado(principal, FormWindowState.Minimized);
                pestanas.SelectedIndex = original; Bombear(200);
                Estado(principal, FormWindowState.Normal); Bombear(600);
                total += Diferencias($"pestañas ({pestanas.TabCount}) recorridas, vuelta a la original con la ventana minimizada", s0, Firma(principal));
                Captura(principal, nombre + "_2_pestanas");
            }
            principal.ClientSize = new Size(960, 600); Bombear(600); principal.ClientSize = new Size(1280, 760); Bombear(600);
            total += Diferencias("achicar a 960×600 → volver", s0, Firma(principal));
            File.WriteAllLines(Path.Combine(_salida, $"informe_{area}.txt"), Log, new UTF8Encoding(false));
        }
        Log.Add($"TOTAL de controles que no volvieron a su lugar: {total}");
        File.WriteAllLines(Path.Combine(_salida, $"informe_{area}.txt"), Log, new UTF8Encoding(false));
        principal.Hide();
        return total == 0 ? 0 : 1;
    }
}
