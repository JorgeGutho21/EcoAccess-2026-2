using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Presentacion;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Capturas
{
    /// <summary>
    /// Capturas auténticas de la aplicación de escritorio: formularios reales, servicios reales y Oracle real, dibujados fuera
    /// de la pantalla (DrawToBitmap) para no tomar el escritorio de nadie. Para las secciones administrativas crea un
    /// administrador FICTICIO (correo captura.*@example.invalid) y lo borra al terminar, aunque algo falle.
    /// Uso: EcoAccess.P3.Capturas.exe &lt;carpeta-salida&gt; [ancho alto]
    /// </summary>
    internal static class Program
    {
        private static string _salida = "";
        private static readonly List<string> Informe = new();

        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Un error de un formulario se anota y la captura sigue: el borrado del administrador ficticio debe ejecutarse siempre.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => Informe.Add("ERROR_INTERFAZ: " + e.Exception);
            _salida = args.Length > 0 ? args[0] : Path.Combine(Environment.CurrentDirectory, "capturas");
            var tamano = args.Length >= 3 ? new Size(int.Parse(args[1]), int.Parse(args[2])) : new Size(1366, 768);
            Directory.CreateDirectory(_salida);

            string plantilla = System.Configuration.ConfigurationManager.ConnectionStrings[CadenaConexionOracle.Nombre]?.ConnectionString
                ?? "Data Source=localhost:1522/PRUEBAP3;User Id=ECOACCESS_P3_APP";
            IConfiguration configuracion = new ConfigurationBuilder().AddUserSecrets("ecoaccess-p3-framework").AddEnvironmentVariables("ECOACCESS_").Build();
            string cadena = CadenaConexionOracle.Resolver(configuracion.GetConnectionString(CadenaConexionOracle.Nombre), plantilla)
                ?? throw new InvalidOperationException("Sin cadena de conexión.");
            ServiciosAplicacion servicios = EcoAccess.P3.Presentacion.Program.ComponerServicios(cadena, configuracion["Telegram:NombreBot"]);
            var fabrica = new FabricaConexionesOracle(cadena);
            string sufijo = $"{tamano.Width}x{tamano.Height}";

            Capturar("01_inicio_y_acceso", () => new FormInicioSesion(servicios.Autenticacion, servicios.RegistroCiudadanos), null);
            Capturar($"02_ciudadano_invitado_{sufijo}", () => new FormExplorarEcoAccess(servicios, null), tamano);

            var usuarios = new RepositorioUsuariosOracle(fabrica);
            string correo = "captura." + Guid.NewGuid().ToString("N").Substring(0, 12) + "@example.invalid";
            int id = Task.Run(() => usuarios.AgregarAsync(new Usuario("Administrador ficticio de capturas", correo, CodigoRol.Administrador, DateTime.UtcNow))).GetAwaiter().GetResult();
            Informe.Add("ADMIN_FICTICIO=" + id);
            try
            {
                var sesion = new SesionActiva(id, "Administrador ficticio de capturas", new[] { CodigoRol.Administrador }, DateTime.UtcNow);
                // Cada sección del menú lateral, con su propio botón; se omite Power BI (abre otro programa) y lo que abre diálogos.
                string[] secciones = { "Dashboard", "Ciudadanos y usuarios", "Materiales", "Canecas y QR", "Puntos de instalación", "Depósitos",
                    "Ecopuntos", "Estadísticas", "Valoraciones", "Dashboard institucional", "Sistema y Telegram", "Próximamente" };
                CapturarSecciones(() => new FormPrincipal(servicios, sesion), tamano, secciones, sufijo);
            }
            finally
            {
                using var c = new OracleConnection(cadena); c.Open();
                using var tx = c.BeginTransaction();
                foreach (string sql in new[] { "DELETE FROM usuario_rol WHERE id_usuario = :id", "DELETE FROM usuario WHERE id_usuario = :id AND correo_electronico LIKE 'captura.%@example.invalid'" })
                {
                    using var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; cmd.BindByName = true; cmd.Parameters.Add("id", id); cmd.ExecuteNonQuery();
                }
                tx.Commit();
                Informe.Add("ADMIN_FICTICIO_BORRADO=" + id);
                File.WriteAllLines(Path.Combine(_salida, $"informe_{sufijo}.txt"), Informe);
            }
            return 0;
        }

        /// <summary>Deja correr el bucle de mensajes: las cargas asíncronas desde Oracle terminan en el hilo de la interfaz.</summary>
        private static void Esperar(int milisegundos)
        {
            var reloj = Stopwatch.StartNew();
            while (reloj.ElapsedMilliseconds < milisegundos) { Application.DoEvents(); Thread.Sleep(30); }
        }

        private static Form Mostrar(Func<Form> crear, Size? tamano)
        {
            Form f = crear();
            f.StartPosition = FormStartPosition.Manual;
            f.ShowInTaskbar = false;
            // Fuera de la pantalla visible: no tapa las ventanas de la persona que usa el equipo.
            f.Location = new Point(-5000, -5000);
            f.Show();
            Esperar(600);
            // El formulario arma su diseño al cargar (y se maximiza); el tamaño de la pantalla simulada se aplica después.
            f.WindowState = FormWindowState.Normal;
            if (tamano is { } t) f.Size = t;
            f.Location = new Point(-f.Width - 200, -f.Height - 200);
            return f;
        }

        private static void Guardar(Form f, string nombre, Stopwatch carga)
        {
            using var bmp = new Bitmap(f.Width, f.Height);
            f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.Size));
            string ruta = Path.Combine(_salida, nombre + ".png");
            bmp.Save(ruta, ImageFormat.Png);
            Informe.Add($"{nombre}: {f.Width}x{f.Height} px a {f.DeviceDpi} ppp ({f.DeviceDpi * 100 / 96} %), listo en {carga.ElapsedMilliseconds} ms, cortes={Cortes(f)}");
        }

        /// <summary>Controles visibles que se salen de su contenedor (botones o textos cortados): pista para revisar a ojo.</summary>
        private static int Cortes(Control raiz)
        {
            int n = 0;
            foreach (Control c in Todos(raiz))
            {
                if (!c.Visible || c.Parent is null || c.Parent is ScrollableControl { AutoScroll: true }) continue;
                if (c is ButtonBase or Label && (c.Right > c.Parent.ClientSize.Width + 1 || c.Bottom > c.Parent.ClientSize.Height + 1)) n++;
                // Texto más grande que su propia etiqueta: se ve recortado aunque la etiqueta quepa en su contenedor.
                if (c is Label { AutoSize: true } etiqueta && etiqueta.MaximumSize.Width == 0)
                {
                    Size pref = etiqueta.PreferredSize;
                    if (pref.Width > etiqueta.Width + 2 || pref.Height > etiqueta.Height + 2)
                    {
                        n++;
                        Informe.Add($"    recortada: «{Recorte(etiqueta.Text)}» {etiqueta.Width}x{etiqueta.Height} necesita {pref.Width}x{pref.Height} en {etiqueta.Parent?.GetType().Name}");
                    }
                }
            }
            return n;
        }

        private static string Recorte(string texto) => texto.Length <= 40 ? texto : texto.Substring(0, 40) + "…";

        private static IEnumerable<Control> Todos(Control raiz) => raiz.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Todos(c)));

        private static void Capturar(string nombre, Func<Form> crear, Size? tamano)
        {
            var carga = Stopwatch.StartNew();
            using Form f = Mostrar(crear, tamano);
            Esperar(4000);
            Guardar(f, nombre, carga);
            f.Close();
        }

        private static void CapturarSecciones(Func<Form> crear, Size tamano, string[] secciones, string sufijo)
        {
            using Form f = Mostrar(crear, tamano);
            Esperar(5000);
            int i = 3;
            foreach (string seccion in secciones)
            {
                var boton = Todos(f).OfType<IButtonControl>().Cast<Control>().FirstOrDefault(c => c.Text == seccion && c.Visible);
                if (boton is null) { Informe.Add($"{seccion}: no hay botón visible para este rol"); continue; }
                var carga = Stopwatch.StartNew();
                string archivo = $"{i++:00}_{Nombre(seccion)}_{sufijo}";
                try
                {
                    ((IButtonControl)boton).PerformClick();
                    Esperar(seccion is "Dashboard institucional" or "Estadísticas" or "Depósitos" ? 8000 : 4500);
                    Guardar(f, archivo, carga);
                    // Lo que queda debajo del pliegue: se desplaza el panel principal de la sección y se captura otra vez.
                    var desplazable = Todos(f).OfType<ScrollableControl>()
                        .Where(c => c.Visible && c.AutoScroll && c.VerticalScroll.Visible && c.Height > f.Height / 2)
                        .OrderByDescending(c => c.Height).FirstOrDefault();
                    if (desplazable is not null)
                    {
                        desplazable.AutoScrollPosition = new Point(0, desplazable.VerticalScroll.Maximum);
                        Esperar(800);
                        Guardar(f, archivo + "_abajo", carga);
                        // Las tarjetas de fotos consultadas a Echo (V023) quedan a media página del dashboard institucional.
                        if (Todos(desplazable).OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Fotos consultadas a Echo")) is { } echo)
                        {
                            desplazable.AutoScrollPosition = new Point(0, 0);
                            desplazable.AutoScrollPosition = new Point(0, Math.Max(0, desplazable.PointToClient(echo.PointToScreen(Point.Empty)).Y - 40));
                            Esperar(800);
                            Guardar(f, archivo + "_echo", carga);
                        }
                    }
                }
                catch (Exception e) { Informe.Add($"ERROR {archivo}: {e}"); }
            }
            f.Close();
        }

        private static string Nombre(string texto) => new string(texto.ToLowerInvariant()
            .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ñ', 'n')
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
    }
}
