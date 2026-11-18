using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Presentacion;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.RecorridoInterfaces
{
    /// <summary>
    /// Recorre la estación táctil real con Oracle real (solo lectura del catálogo) a 1366×768 y 1920×1080, pulsando los
    /// mismos botones que tocaría una persona. Cada paso comprueba que apareció el texto esperado y guarda una captura.
    /// Uso: EcoAccess.P3.RecorridoInterfaces.exe &lt;carpeta-salida&gt;. Código de salida 0 si todos los pasos pasan.
    /// </summary>
    internal static class Program
    {
        private static readonly List<string> Informe = new();
        private static int _fallos;

        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => { _fallos++; Informe.Add("ERROR_INTERFAZ: " + e.Exception.GetType().Name + "\n" + e.Exception.StackTrace); };
            string salida = args.Length > 0 ? args[0] : Path.Combine(Environment.CurrentDirectory, "recorrido");
            Directory.CreateDirectory(salida);

            string? plantilla = System.Configuration.ConfigurationManager.ConnectionStrings[CadenaConexionOracle.Nombre]?.ConnectionString;
            IConfiguration configuracion = new ConfigurationBuilder().AddUserSecrets("ecoaccess-p3-framework").AddEnvironmentVariables("ECOACCESS_").Build();
            string cadena = CadenaConexionOracle.Resolver(configuracion.GetConnectionString(CadenaConexionOracle.Nombre), plantilla)
                ?? throw new InvalidOperationException("Sin cadena de conexión.");
            CadenaConexionOracle.PrepararWallet(cadena);
            ServiciosAplicacion servicios = EcoAccess.P3.Presentacion.Program.ComponerServicios(cadena, null);

            // Las preferencias de apariencia de la prueba van a un archivo temporal: no se toca la configuración real del equipo.
            AlmacenApariencia.Ruta = Path.Combine(salida, "apariencia_prueba.txt");
            if (File.Exists(AlmacenApariencia.Ruta)) File.Delete(AlmacenApariencia.Ruta);
            int idCiudadano = args.Length > 1 ? int.Parse(args[1]) : 0;

            foreach (Size tamano in new[] { new Size(1366, 768), new Size(1920, 1080) })
            {
                RecorrerEstacion(servicios, tamano, salida);
                RecorrerAcceso(servicios, tamano, salida);
                RecorrerPanel(servicios, null, tamano, salida);
                if (idCiudadano > 0) RecorrerPanel(servicios, Ciudadano(idCiudadano), tamano, salida);
            }
            if (idCiudadano > 0) RecorrerApariencia(servicios, Ciudadano(idCiudadano), salida);

            File.WriteAllLines(Path.Combine(salida, "informe.txt"), Informe);
            Console.WriteLine(string.Join(Environment.NewLine, Informe));
            return _fallos == 0 ? 0 : 1;
        }

        private static void RecorrerEstacion(ServiciosAplicacion servicios, Size tamano, string salida)
        {
            string sufijo = tamano.Width + "x" + tamano.Height;
            using var form = new FormEstacionTactil(servicios.CatalogoAmbiental)
            {
                WindowState = FormWindowState.Normal, StartPosition = FormStartPosition.Manual,
                Location = new Point(-(tamano.Width + 200), 0), ShowInTaskbar = false,
            };
            form.Show();
            form.MinimumSize = Size.Empty;
            form.Bounds = new Rectangle(new Point(-(tamano.Width + 200), 0), tamano);
            Esperar(form, () => Hay(form, "Sistema disponible") || Hay(form, "Sin conexión"), 30);
            Paso(form, "Estación lista (Oracle)", Hay(form, "Sistema disponible"), salida, $"estacion_1_inicio_{sufijo}");

            Pulsar(form, "Voy a botar algo");
            Paso(form, "Un toque abre los grupos", Hay(form, "¿Qué vas a botar?"), salida, $"estacion_2_grupos_{sufijo}");

            Pulsar(form, "Plástico");
            Paso(form, "Grupo Plástico con residuos del catálogo", Hay(form, "Botella plástica PET"), salida, $"estacion_3_plastico_{sufijo}");

            Pulsar(form, "Botella plástica PET");
            Paso(form, "PET → caneca blanca", Hay(form, "CANECA BLANCA"), salida, $"estacion_4_pet_{sufijo}");

            Pulsar(form, "Listo");
            Paso(form, "«Listo» vuelve al inicio", Hay(form, "Voy a botar algo"), salida, null);

            Pulsar(form, "Voy a botar algo");
            Pulsar(form, "Pilas y medicinas");
            Pulsar(form, "Pila o batería");
            Paso(form, "Pila → ninguna caneca", Hay(form, "NO VA EN ESTAS CANECAS"), salida, $"estacion_5_pila_{sufijo}");

            Pulsar(form, "Listo");
            Pulsar(form, "Voy a botar algo");
            Pulsar(form, "Comida");
            Pulsar(form, "Restos de comida");
            Paso(form, "Comida → caneca verde", Hay(form, "CANECA VERDE"), salida, $"estacion_6_comida_{sufijo}");

            Pulsar(form, "Listo");
            Pulsar(form, "Voy a botar algo");
            Pulsar(form, "Papel y cartón");
            Pulsar(form, "Servilleta o papel higiénico usado");
            Paso(form, "Servilleta usada → caneca negra", Hay(form, "CANECA NEGRA"), salida, $"estacion_7_servilleta_{sufijo}");
            form.Close();
        }

        private static SesionActiva Ciudadano(int id) => new(id, "Ciudadano de simulación", new[] { EcoAccess.P3.Entidades.CodigoRol.Ciudadano }, DateTime.UtcNow);

        private static T Abrir<T>(T form, Size tamano) where T : Form
        {
            form.StartPosition = FormStartPosition.Manual;
            form.ShowInTaskbar = false;
            form.WindowState = FormWindowState.Normal;
            form.Location = new Point(-(tamano.Width + 200), 0);
            form.Show();
            form.WindowState = FormWindowState.Normal;
            form.MinimumSize = Size.Empty;
            form.Bounds = new Rectangle(new Point(-(tamano.Width + 200), 0), tamano);
            Esperar(form, () => false, 0.5);
            return form;
        }

        private static void RecorrerAcceso(ServiciosAplicacion servicios, Size tamano, string salida)
        {
            string sufijo = tamano.Width + "x" + tamano.Height;
            using var ventana = (Form)EcoAccess.P3.Presentacion.Program.CrearVentanaAcceso(servicios);
            Anotar("Sin preferencia, el inicio de sesión es el Moderno", ventana is FormAccesoModerno);
            Abrir(ventana, tamano);
            bool completo = Hay(ventana, "Bienvenido a EcoAccess") && Hay(ventana, "Ciudadano") && Hay(ventana, "Operario") && Hay(ventana, "Supervisor")
                && Hay(ventana, "Iniciar sesión") && Hay(ventana, "Continuar como invitado") && Hay(ventana, "Regístrate");
            Paso(ventana, "Inicio Moderno: roles, correo, contraseña, invitado y registro discreto", completo, salida, $"acceso_moderno_{sufijo}");
            Control? registro = Todos(ventana).FirstOrDefault(c => c is LinkLabel && c.Text == "Regístrate");
            bool sinDesplazamiento = registro is not null
                && ventana.PointToClient(registro.PointToScreen(new Point(0, registro.Height))).Y <= ventana.ClientSize.Height;
            Paso(ventana, "Todo el formulario cabe sin desplazamiento", sinDesplazamiento, salida, null);
            ventana.Close();
        }

        private static void RecorrerPanel(ServiciosAplicacion servicios, SesionActiva? sesion, Size tamano, string salida)
        {
            string sufijo = (sesion is null ? "invitado_" : "ciudadano_") + tamano.Width + "x" + tamano.Height;
            using var panel = Abrir(new FormPanelCiudadano(servicios, sesion), tamano);
            Esperar(panel, () => Hay(panel, "Actualizado") || Hay(panel, "Sin conexión") || Hay(panel, "No se pudieron"), 40);
            Paso(panel, "Panel Moderno carga datos de Oracle (" + (sesion is null ? "invitado" : "ciudadano") + ")", Hay(panel, "Actualizado"), salida, $"panel_{sufijo}");
            Paso(panel, "Panel muestra las tres canecas reales", Hay(panel, "Caneca blanca") && Hay(panel, "Caneca verde") && Hay(panel, "Caneca negra"), salida, null);
            if (sesion is not null) Paso(panel, "Panel del ciudadano muestra su saldo de ecopuntos", Hay(panel, "Ecopuntos acumulados"), salida, null);
            bool sinAdministracion = !Hay(panel, "Usuarios y roles") && !Hay(panel, "Base de datos") && !Hay(panel, "Sistema y Telegram") && !Hay(panel, "Auditar depósitos");
            Paso(panel, "El panel no muestra opciones administrativas", sinAdministracion, salida, null);
            panel.Close();
        }

        private static void RecorrerApariencia(ServiciosAplicacion servicios, SesionActiva sesion, string salida)
        {
            var tamano = new Size(1366, 768);
            using (var selector = Abrir(new FormSeleccionInterfaz(sesion, PerfilApariencia.Ciudadano, CatalogoApariencias.Moderno), tamano))
            {
                Paso(selector, "El selector lista los diseños del ciudadano", Hay(selector, "EcoAccess Moderno") && Hay(selector, "EcoAccess Clásico"), salida, "selector_ciudadano_1366x768");
                Paso(selector, "El ciudadano no ve el modo táctil de estación", !Hay(selector, "EcoAccess Táctil"), salida, null);
                Paso(selector, "El predeterminado está marcado", Hay(selector, "Predeterminada"), salida, null);
                Paso(selector, "Hay vista previa real de cada diseño", Todos(selector).OfType<Button>().Count(b => b.Text.Trim() == "Vista previa" && b.Enabled) >= 2, salida, null);
                PulsarEn(selector, "EcoAccess Clásico", "Aplicar interfaz");
            }
            Anotar("Aplicar guarda la preferencia de la cuenta", AlmacenApariencia.Leer().DeUsuario(sesion.IdUsuario) == CatalogoApariencias.Clasico);
            Anotar("La preferencia sobrevive a un reinicio (se relee del archivo)",
                CatalogoApariencias.Resolver(AlmacenApariencia.Leer().Para(sesion), PerfilApariencia.Ciudadano).Id == CatalogoApariencias.Clasico);
            using (var acceso = (Form)EcoAccess.P3.Presentacion.Program.CrearVentanaAcceso(servicios))
                Anotar("Tras aplicar, el inicio de sesión del equipo usa el Clásico", acceso is FormInicioSesion);
            using (var explorar = Abrir(new FormExplorarEcoAccess(servicios, sesion), tamano))
            {
                Esperar(explorar, () => false, 6);
                Paso(explorar, "El diseño Clásico abre con la misma sesión y conserva las funciones", Hay(explorar, "Mis ecopuntos") && Hay(explorar, "Apariencia"), salida, "panel_clasico_ciudadano_1366x768");
                explorar.Close();
            }
            using (var selector = Abrir(new FormSeleccionInterfaz(sesion, PerfilApariencia.Ciudadano, CatalogoApariencias.Clasico), tamano))
                Pulsar(selector, "Restablecer diseño predeterminado");
            Anotar("Restablecer vuelve al Moderno", CatalogoApariencias.Resolver(AlmacenApariencia.Leer().Para(sesion), PerfilApariencia.Ciudadano).Id == CatalogoApariencias.Moderno);

            File.WriteAllText(AlmacenApariencia.Ruta, "equipo=<roto>\nusuario.x=1\n\u0000basura");
            using (var acceso = (Form)EcoAccess.P3.Presentacion.Program.CrearVentanaAcceso(servicios))
                Anotar("Un archivo de apariencia dañado no impide iniciar (usa el Moderno)", acceso is FormAccesoModerno);
            File.Delete(AlmacenApariencia.Ruta);

            var supervisor = new SesionActiva(1, "Supervisor de prueba", new[] { EcoAccess.P3.Entidades.CodigoRol.Administrador }, DateTime.UtcNow);
            using (var selector = Abrir(new FormSeleccionInterfaz(supervisor, PerfilApariencia.Supervisor, CatalogoApariencias.Moderno), tamano))
            {
                Paso(selector, "El supervisor ve además el modo táctil de estación", Hay(selector, "EcoAccess Táctil") && Hay(selector, "Usar este equipo como estación"), salida, "selector_supervisor_1366x768");
                selector.Close();
            }
        }

        private static void Anotar(string nombre, bool correcto)
        {
            if (!correcto) _fallos++;
            Informe.Add((correcto ? "OK    " : "FALLA ") + nombre);
        }

        private static void PulsarEn(Control raiz, string tarjeta, string texto)
        {
            Control? contenedor = Todos(raiz).OfType<TableLayoutPanel>().Where(t => Todos(t).Any(c => c is Label && c.Text == tarjeta) && Todos(t).OfType<Button>().Any(b => b.Text.Trim() == texto)).OrderBy(t => Todos(t).Count()).FirstOrDefault();
            Button? boton = contenedor is null ? null : Todos(contenedor).OfType<Button>().FirstOrDefault(b => b.Text.Trim() == texto);
            if (boton is null || !boton.Enabled) { _fallos++; Informe.Add("FALLA no se pudo pulsar «" + texto + "» en «" + tarjeta + "»"); return; }
            boton.PerformClick();
            Application.DoEvents();
        }

        private static void Paso(Form form, string nombre, bool correcto, string salida, string? captura)
        {
            if (!correcto) _fallos++;
            Informe.Add((correcto ? "OK    " : "FALLA ") + nombre + " [" + form.Width + "x" + form.Height + "]");
            if (captura is null) return;
            using var imagen = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(imagen, new Rectangle(Point.Empty, form.Size));
            imagen.Save(Path.Combine(salida, captura + ".png"), ImageFormat.Png);
        }

        private static void Pulsar(Form form, string texto)
        {
            Button? boton = Todos(form).OfType<Button>().FirstOrDefault(b => b.Visible && b.Text.Trim() == texto.Trim());
            if (boton is null) { _fallos++; Informe.Add("FALLA no se encontró el botón «" + texto + "»"); return; }
            if (!boton.Enabled) { _fallos++; Informe.Add("FALLA botón deshabilitado «" + texto + "»"); return; }
            boton.PerformClick();
            Esperar(form, () => false, 0.4);
        }

        // Las tarjetas dibujadas a mano exponen su título en AccessibleName (lo mismo que lee un lector de pantalla).
        private static bool Hay(Control raiz, string texto) => Todos(raiz).Any(c => c.Visible && (c.Text.Contains(texto) || (c.AccessibleName?.Contains(texto) ?? false)));

        private static IEnumerable<Control> Todos(Control raiz)
        {
            foreach (Control hijo in raiz.Controls)
            {
                yield return hijo;
                foreach (Control nieto in Todos(hijo)) yield return nieto;
            }
        }

        private static void Esperar(Form form, Func<bool> listo, double segundos)
        {
            var reloj = Stopwatch.StartNew();
            while (reloj.Elapsed.TotalSeconds < segundos && !listo())
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(30);
            }
            Application.DoEvents();
        }
    }
}
