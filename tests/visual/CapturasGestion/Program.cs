using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Presentacion;

// Capturas de las áreas de gestión (Supervisor u Operario) con la ventana principal REAL y cada módulo del menú, para revisar
// la estética con la skill eco-chequeo-estetica. Oracle es inalcanzable a propósito (127.0.0.1:1): no usa credenciales ni datos
// reales. La ventana queda casi entera fuera de la pantalla (1 px dentro, para tomar la escala del monitor) y se dibuja con
// DrawToBitmap, así que no tapa el escritorio.
// Uso: CapturasGestion.exe <carpeta de salida> [supervisor|operario] [ancho alto]
static class Program
{
    static void Bombear(int ms)
    {
        var fin = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < fin) { Application.DoEvents(); Thread.Sleep(15); }
    }

    static IEnumerable<Control> Todos(Control c) { foreach (Control h in c.Controls) { yield return h; foreach (var n in Todos(h)) yield return n; } }

    static void Capturar(Form f, string carpeta, string nombre)
    {
        // Volcado opcional de posiciones (CAPTURAS_VOLCAR=1) para depurar desplazamientos del diseño fijo.
        if (Environment.GetEnvironmentVariable("CAPTURAS_VOLCAR") == "1")
            File.WriteAllLines(Path.Combine(carpeta, nombre + ".txt"), Todos(f).Where(c => c.Visible)
                .Select(c => $"{c.GetType().Name} {c.Name} {c.Bounds} pad {c.Padding} dpi {c.DeviceDpi} en {c.Parent?.GetType().Name} {c.Parent?.Name}"), new UTF8Encoding(false));
        using var bmp = new Bitmap(f.Width, f.Height);
        f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
        bmp.Save(Path.Combine(carpeta, nombre + ".png"), ImageFormat.Png);
    }

    static IEnumerable<Form> Dialogos(ServiciosAplicacion servicios, SesionActiva sesion)
    {
        yield return new FormCambioContrasena(servicios.Autenticacion, sesion);
        using var png = new MemoryStream();
        using (var muestra = new Bitmap(64, 64))
        {
            using (Graphics g = Graphics.FromImage(muestra)) { g.Clear(Color.White); g.FillRectangle(Brushes.Black, 8, 8, 20, 20); g.FillRectangle(Brushes.Black, 36, 36, 20, 20); }
            muestra.Save(png, ImageFormat.Png);
        }
        var qr = new FormQrCaneca("Caneca de prueba", new Uri("https://t.me/EcoAccessP3_JG_bot?start=prueba"), png.ToArray());
        TemaEcoAccess_Aplicar(qr);
        yield return qr;
    }

    // Igual que FormContenedores antes de mostrar el QR (el tema es interno; se aplica por reflexión).
    static void TemaEcoAccess_Aplicar(Form formulario) =>
        typeof(FormPrincipal).Assembly.GetType("EcoAccess.P3.Presentacion.TemaEcoAccess")!
            .GetMethod("Aplicar", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { formulario });

    [STAThread]
    static int Main(string[] args)
    {
        string salida = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "capturas_gestion");
        Directory.CreateDirectory(salida);
        var area = args.Length > 1 && args[1] == "operario" ? AreaAcceso.Operario : AreaAcceso.Supervisor;
        var cliente = args.Length > 3 ? new Size(int.Parse(args[2]), int.Parse(args[3])) : new Size(1366, 728);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var errores = new List<string>();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => errores.Add(e.Exception.ToString());

        var servicios = EcoAccess.P3.Presentacion.Program.ComponerServicios("User Id=capturas;Data Source=127.0.0.1:1/ninguno;Connection Timeout=2", "EcoAccessP3_JG_bot");
        var sesion = new SesionActiva(1, "Supervisora Demo", new[] { CodigoRol.Administrador, CodigoRol.Operario }, DateTime.UtcNow);
        using var principal = new FormPrincipal(servicios, sesion, area) { StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        principal.Location = new Point(1 - principal.Width, 0);
        principal.Show();
        principal.WindowState = FormWindowState.Normal;
        principal.Location = new Point(1 - principal.Width, 0);
        principal.ClientSize = cliente;
        // Toca 1 px del monitor principal: así usa su escala real (fuera de toda pantalla Windows la toma como 100 %).
        principal.Location = new Point(1 - principal.Width, 0);
        Bombear(2500);
        string prefijo = area == AreaAcceso.Supervisor ? "sup" : "ope";
        Capturar(principal, salida, $"{prefijo}_00_Dashboard");

        var botones = Todos(principal).Where(c => c.GetType().Name == "BotonNavegacion" && c.Visible).Select(c => c.Text)
            .Where(t => t is not ("Dashboard" or "Contraer menú" or "Análisis en Power BI" or "Perfil y contraseña" or "Cerrar sesión") && !t.StartsWith("Cambiar", StringComparison.Ordinal))
            .Where(t => Environment.GetEnvironmentVariable("CAPTURAS_SOLO") is not { Length: > 0 } solo || solo.Split('|').Contains(t))
            .Distinct().ToList();
        int i = 1;
        foreach (string texto in botones)
        {
            if (principal.IsDisposed) break;
            Control boton = Todos(principal).First(c => c.GetType().Name == "BotonNavegacion" && c.Text == texto);
            typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(boton, new object[] { EventArgs.Empty });
            Bombear(5000);
            string nombre = $"{prefijo}_{i++:00}_" + new string(texto.Where(char.IsLetterOrDigit).ToArray());
            Capturar(principal, salida, nombre);
            int p = 0;
            foreach (TabControl pestanas in Todos(principal).OfType<TabControl>().Where(t => t.Visible).ToList())
                for (int k = 1; k < pestanas.TabCount; k++)
                {
                    pestanas.SelectedIndex = k; Bombear(600);
                    Capturar(principal, salida, $"{nombre}_pestana{++p}");
                }
        }
        // Diálogos del personal: perfil y contraseña, y el QR de una caneca (con un PNG de prueba, no el QR real).
        if (Environment.GetEnvironmentVariable("CAPTURAS_SOLO") is not { Length: > 0 })
        {
            foreach (Form dialogo in Dialogos(servicios, sesion))
                using (dialogo)
                {
                    dialogo.StartPosition = FormStartPosition.Manual;
                    dialogo.ShowInTaskbar = false;
                    dialogo.Show();
                    dialogo.Location = new Point(1 - dialogo.Width, 0);
                    Bombear(1500);
                    Capturar(dialogo, salida, $"{prefijo}_dialogo_{new string(dialogo.GetType().Name.Where(char.IsLetter).ToArray())}");
                    dialogo.Hide();
                }
        }
        File.WriteAllLines(Path.Combine(salida, "errores.txt"), errores, new UTF8Encoding(false));
        return errores.Count == 0 ? 0 : 1;
    }
}
