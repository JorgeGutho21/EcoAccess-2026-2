using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.Presentacion
{

    public static class Program
    {
        /// <summary>Destino Oracle leído de App.config; solo se usa en mensajes, nunca contiene la contraseña.</summary>
        private static string _destino = CadenaConexionOracle.DestinoPredeterminado;
        /// <summary>
        ///  Punto de entrada de la aplicación de escritorio.
        /// </summary>
        /// <remarks>
        ///  Este método es el único lugar donde la presentación crea clases de EcoAccess.P3.Datos
        ///  (punto de composición): aquí se entregan los repositorios Oracle a los servicios de la lógica.
        ///  Los formularios nunca usan Datos directamente.
        /// </remarks>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Laboratorio de propiedades de Windows Forms (material de clase): solo se abre si se pide de forma explícita,
            // por ejemplo en Visual Studio > Propiedades del proyecto > Depurar > Argumentos: --laboratorio-propiedades.
            // Sin el argumento, EcoAccess arranca con su flujo normal (acceso directo, instalador y compañeros).
            if (Array.Exists(args, a => string.Equals(a, "--laboratorio-propiedades", StringComparison.OrdinalIgnoreCase)))
            {
                Application.Run(new FormEjemploPropiedades());
                return;
            }

            // Red de seguridad: una excepción no capturada muestra un aviso genérico, sin el texto del
            // proveedor (puede citar host o servicio), en lugar del diálogo de .NET con la traza.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                RegistrarError(e.Exception);
                MessageBox.Show(
                    "Ocurrió un error inesperado. La operación no se completó.",
                    "EcoAccess P3",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            // App.config (ConfigurationManager) aporta la parte pública: destino y usuario del esquema.
            // La cadena completa contiene la contraseña: se lee del perfil privado de Windows (User Secrets) o de
            // ECOACCESS_ConnectionStrings__OracleP3, tiene prioridad y nunca se muestra ni se versiona.
            const string IdSecretos = "ecoaccess-p3-framework";
            string? plantilla = System.Configuration.ConfigurationManager.ConnectionStrings[CadenaConexionOracle.Nombre]?.ConnectionString;
            string DestinoPredeterminado = CadenaConexionOracle.Destino(plantilla);
            _destino = DestinoPredeterminado;
            IConfiguration configuracion = new ConfigurationBuilder().AddUserSecrets(IdSecretos).AddEnvironmentVariables("ECOACCESS_").Build();
            string? cadena = CadenaConexionOracle.Resolver(configuracion.GetConnectionString(CadenaConexionOracle.Nombre), plantilla);
            // Con el alias de Oracle Cloud, ODP.NET necesita TNS_ADMIN: si el acceso directo no lo hereda, se toma la carpeta
            // del wallet instalada por el instalador. Docker no es necesario para la base oficial.
            CadenaConexionOracle.PrepararWallet(cadena ?? plantilla);

            // Comprobación real antes de abrir: si falta la conexión, Oracle no responde o la contraseña dejó de servir,
            // se abre la configuración inicial en lugar de cerrar la aplicación con un error. Con solo la plantilla de
            // App.config (sin contraseña) no se intenta el inicio de sesión: se pide la contraseña directamente.
            EstadoConexionOracle estado = CadenaConexionOracle.TieneContrasena(cadena) ? Comprobar(cadena) : EstadoConexionOracle.CadenaInvalida;
            if (estado != EstadoConexionOracle.Correcta)
            {
                string? actual = cadena;
                using var activacion = new FormActivacionOracle(DestinoPredeterminado, UsuarioDe(actual) ?? "ECOACCESS_P3_APP",
                    estado is EstadoConexionOracle.CadenaInvalida or EstadoConexionOracle.CredencialesInvalidas,
                    CadenaConexionOracle.CarpetaWallet,
                    async (credenciales, ct) =>
                    {
                        CadenaConexionOracle.PrepararWallet(actual ?? plantilla);
                        string? prueba = credenciales is null ? actual : ConCredenciales(actual, DestinoPredeterminado, credenciales);
                        return Explicar(await VerificadorConexionOracle.ComprobarAsync(prueba, ct));
                    });
                if (activacion.ShowDialog() != DialogResult.OK) return;
                if (activacion.Usuario is not null)
                {
                    cadena = ConCredenciales(actual, DestinoPredeterminado, activacion.Usuario + "\n" + activacion.Contrasena);
                    // Solo después de que Oracle la aceptó: queda en el perfil de Windows, fuera del repositorio.
                    ConfiguracionLocal.Guardar(IdSecretos, new Dictionary<string, string> { ["ConnectionStrings:OracleP3"] = cadena });
                }
            }

            ServiciosAplicacion servicios;
            try
            {
                // Aquí siempre hay cadena: la configuración inicial no deja continuar sin una conexión comprobada.
                servicios = ComponerServicios(cadena!, configuracion["Telegram:NombreBot"]);
            }
            catch (ArgumentException)
            {
                // El mensaje de la excepción podría citar parte de la cadena: no se muestra.
                MessageBox.Show(
                    "La cadena de conexión ConnectionStrings:OracleP3 no es válida. Revísela en los User Secrets «ecoaccess-p3-framework».",
                    "EcoAccess P3",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            // Estación táctil (observación del profesor Patiño): la misma aplicación y la misma base, con un solo botón grande
            // y las demás opciones ocultas. Se abre con el argumento --estacion (acceso directo de la pantalla táctil).
            // También si un supervisor configuró este equipo como estación (Configuración → Apariencia → EcoAccess Táctil);
            // --escritorio abre el escritorio normal aunque el equipo sea estación.
            bool pideEstacion = Array.Exists(args, a => string.Equals(a, "--estacion", StringComparison.OrdinalIgnoreCase));
            bool pideEscritorio = Array.Exists(args, a => string.Equals(a, "--escritorio", StringComparison.OrdinalIgnoreCase));
            if (pideEstacion || (!pideEscritorio && AlmacenApariencia.Leer().EsEstacion))
            {
                Application.Run(new FormEstacionTactil(servicios.CatalogoAmbiental));
                return;
            }

            EjecutarSesiones(servicios, args);
        }

        /// <summary>
        /// Punto de composición del escritorio: entrega los repositorios Oracle a los servicios de la lógica. Es público para
        /// que las pruebas y el arnés de capturas usen exactamente la misma composición que la aplicación.
        /// </summary>
        public static ServiciosAplicacion ComponerServicios(string cadena, string? nombreBotTelegram)
        {
                var fabrica = new FabricaConexionesOracle(cadena);
                var usuarios = new RepositorioUsuariosOracle(fabrica);
                var materiales = new RepositorioMaterialesOracle(fabrica);
                var puntos = new RepositorioPuntosInstalacionOracle(fabrica);
                var contenedores = new RepositorioContenedoresOracle(fabrica);
                return new ServiciosAplicacion(
                    new ServicioMateriales(usuarios, materiales, contenedores),
                    new ServicioAutenticacion(
                        new RepositorioCredencialesOracle(fabrica),
                        usuarios,
                        new HasherPbkdf2(),
                        TimeProvider.System),
                    new ServicioPuntosInstalacion(usuarios, puntos, contenedores),
                    new ServicioContenedores(
                        usuarios,
                        contenedores,
                        puntos,
                        materiales,
                        new GeneradorIdentificadorQrAleatorio(),
                        TimeProvider.System),
                    new ServicioUsuarios(usuarios, TimeProvider.System),
                    new ServicioInteracciones(
                        usuarios,
                        new RepositorioInteraccionesOracle(fabrica),
                        contenedores,
                        materiales,
                        TimeProvider.System),
                    new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(fabrica)),
                    // El nombre del bot es público (va impreso en el QR); se lee de la misma configuración.
                    nombreBotTelegram?.Trim() is { Length: > 0 } nombreBot ? nombreBot : null,
                    new ServicioEcopuntos(usuarios, new RepositorioInteraccionesOracle(fabrica), materiales,
                        new RepositorioEcopuntosOracle(fabrica), TimeProvider.System),
                    new ServicioDiagnosticoOracle(usuarios, new RepositorioDiagnosticoOracle(fabrica), TimeProvider.System),
                    new ServicioPanelAdministrativo(usuarios, new RepositorioPanelAdministrativoOracle(fabrica)),
                    new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(fabrica), new HasherPbkdf2(), TimeProvider.System),
                    new ServicioCatalogoAmbiental(materiales, contenedores),
                    new ServicioOpiniones(usuarios, new RepositorioOpinionesOracle(fabrica), TimeProvider.System),
                    new ServicioInformeInstitucional(usuarios, new RepositorioInformeInstitucionalOracle(fabrica), new RepositorioConsultasVisionOracle(fabrica)),
                    // Lectura del panel del ciudadano (diseño Moderno). Reutiliza los mismos servicios; no agrega consultas propias.
                    new ServicioPanelCiudadano(
                        new ServicioEstadisticas(usuarios, new RepositorioEstadisticasOracle(fabrica)),
                        new ServicioEcopuntos(usuarios, new RepositorioInteraccionesOracle(fabrica), materiales, new RepositorioEcopuntosOracle(fabrica), TimeProvider.System),
                        contenedores, new RepositorioEstacionesOracle(fabrica), new RepositorioVinculosTelegramOracle(fabrica), TimeProvider.System));
        }

        private static void EjecutarSesiones(ServiciosAplicacion servicios, string[] args)
        {
            // Entrada pública para kiosco/revisión: usa exactamente la sesión invitada existente, sin aceptar actor ni credenciales por argumentos.
            bool explorarDirectamente = args.Length == 1 && args[0] == "--explorar";
            // Inicio de sesión y panel en bucle: cerrar sesión vuelve al inicio; cancelar o cerrar la ventana termina.
            // El diseño de cada ventana sale de la preferencia de apariencia (archivo local) y, si falta o no corresponde
            // al perfil, del predeterminado. El diseño nunca decide el rol: lo decide Oracle en el inicio de sesión.
            while (true)
            {
                SesionActiva? sesion;
                bool invitado;
                AreaAcceso area = AreaAcceso.Ciudadano;
                if (explorarDirectamente)
                {
                    sesion = null;
                    invitado = true;
                    explorarDirectamente = false;
                }
                else using (IVentanaAcceso inicio = CrearVentanaAcceso(servicios))
                {
                    if (inicio.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    sesion = inicio.Sesion;
                    invitado = inicio.AccesoInvitado;
                    area = inicio.AreaElegida;
                }

                if (!invitado && sesion is null) return;
                if (invitado) sesion = null;
                if (!AbrirPaneles(servicios, sesion, invitado ? AreaAcceso.Ciudadano : area)) return;
            }
        }

        /// <summary>Ventana de acceso del diseño elegido en este equipo (antes de saber quién entra).</summary>
        public static IVentanaAcceso CrearVentanaAcceso(ServiciosAplicacion servicios) =>
            CatalogoApariencias.Resolver(AlmacenApariencia.Leer().Equipo, PerfilApariencia.Invitado).Id == CatalogoApariencias.Clasico
                ? new FormInicioSesion(servicios.Autenticacion, servicios.RegistroCiudadanos)
                : new FormAccesoModerno(servicios.Autenticacion, servicios.RegistroCiudadanos);

        /// <summary>
        /// Abre el panel de la sesión con su diseño. Si la persona aplica otro diseño, se reabre con la misma sesión y el mismo
        /// rol, sin volver a pedir la contraseña ni tocar Oracle. Devuelve true si hay que volver a la ventana de acceso.
        /// </summary>
        private static bool AbrirPaneles(ServiciosAplicacion servicios, SesionActiva? sesion, AreaAcceso area)
        {
            while (true)
            {
                PerfilApariencia perfil = CatalogoApariencias.PerfilDe(sesion, area);
                string diseno = CatalogoApariencias.Resolver(AlmacenApariencia.Leer().Para(sesion), perfil).Id;

                if (perfil is PerfilApariencia.Invitado or PerfilApariencia.Ciudadano)
                {
                    if (diseno == CatalogoApariencias.Clasico)
                    {
                        using var explorar = new FormExplorarEcoAccess(servicios, sesion);
                        Application.Run(explorar);
                        if (explorar.CambioAparienciaSolicitado) continue;
                        return explorar.SolicitarInicioSesion;
                    }
                    using var panel = new FormPanelCiudadano(servicios, sesion);
                    Application.Run(panel);
                    if (panel.CambioAparienciaSolicitado) continue;
                    return panel.SolicitarInicioSesion;
                }

                // Operario o Supervisor: el menú de gestión (los dos diseños comparten este panel por ahora). Cambiar de área
                // reabre el menú con la otra área ya autorizada; FormPrincipal la vuelve a comprobar con AreasAcceso.Elegir.
                AreaAcceso? siguiente = area;
                bool cerrarSesion = false, cambioApariencia = false;
                while (siguiente is { } actual)
                {
                    using var principal = new FormPrincipal(servicios, sesion!, actual);
                    Application.Run(principal);
                    siguiente = principal.AreaSolicitada;
                    cerrarSesion = principal.CerrarSesionSolicitado;
                    cambioApariencia = principal.CambioAparienciaSolicitado;
                    if (siguiente is { } otra) area = otra;
                    if (cambioApariencia) break;
                }
                if (cambioApariencia) continue;
                return cerrarSesion;
            }
        }

        /// <summary>
        /// Deja en %LOCALAPPDATA%\EcoAccessP3\errores.log el tipo y la pila de un error no controlado, para diagnosticarlo.
        /// No guarda el mensaje: el de Oracle puede citar host, servicio o usuario.
        /// </summary>
        private static void RegistrarError(Exception error)
        {
            try
            {
                string carpeta = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoAccessP3");
                System.IO.Directory.CreateDirectory(carpeta);
                System.IO.File.AppendAllText(System.IO.Path.Combine(carpeta, "errores.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + error.GetType().FullName + Environment.NewLine + error.StackTrace + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException) { }
        }

        // Un solo intento con tiempo límite; Main aún no tiene bucle de mensajes, así que esperar aquí no congela ventanas.
        private static EstadoConexionOracle Comprobar(string? cadena)
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try { return Task.Run(() => VerificadorConexionOracle.ComprobarAsync(cadena, limite.Token)).GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { return EstadoConexionOracle.ServicioNoDisponible; }
        }

        private static string? UsuarioDe(string? cadena)
        {
            if (cadena is null) return null;
            try { return new System.Data.Common.DbConnectionStringBuilder { ConnectionString = cadena }["User Id"] as string; }
            catch (Exception e) when (e is ArgumentException or KeyNotFoundException) { return null; }
        }

        /// <summary>La cadena actual (o una nueva) con el destino fijo y el usuario y la contraseña indicados.</summary>
        private static string ConCredenciales(string? actual, string destino, string credenciales)
        {
            var partes = new System.Data.Common.DbConnectionStringBuilder();
            try { if (actual is not null) partes.ConnectionString = actual; }
            catch (ArgumentException) { partes.Clear(); }
            string[] valores = credenciales.Split('\n', 2);
            partes["User Id"] = valores[0];
            partes["Password"] = valores.Length == 2 ? valores[1] : "";
            partes["Data Source"] = destino;
            return partes.ConnectionString;
        }

        /// <summary>Mensaje sencillo para la persona; nunca incluye la cadena ni el error interno de Oracle.</summary>
        private static (bool Correcta, bool PedirContrasena, string Mensaje) Explicar(EstadoConexionOracle estado) => estado switch
        {
            EstadoConexionOracle.Correcta => (true, false, ""),
            EstadoConexionOracle.CredencialesInvalidas => (false, true, "Oracle rechazó el usuario o la contraseña. Revísalos con calma: varios intentos fallidos pueden bloquear la cuenta."),
            EstadoConexionOracle.CuentaBloqueada => (false, false, "La cuenta del esquema está bloqueada en Oracle. Debe desbloquearla el administrador de la base."),
            EstadoConexionOracle.EsquemaIncompleto => (false, false, "La conexión funciona, pero ese esquema no tiene las tablas de EcoAccess. Revisa el usuario."),
            EstadoConexionOracle.CadenaInvalida => (false, true, "Falta la conexión en este equipo: escribe la contraseña del esquema."),
            _ => (false, false, $"Oracle no respondió en {_destino}. Comprueba que el contenedor esté en marcha y vuelve a comprobar."),
        };
    }
}
