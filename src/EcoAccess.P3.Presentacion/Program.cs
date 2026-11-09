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

    static class Program
    {
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
// Red de seguridad: una excepción no capturada muestra un aviso genérico, sin el texto del
            // proveedor (puede citar host o servicio), en lugar del diálogo de .NET con la traza.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, _) => MessageBox.Show(
                "Ocurrió un error inesperado. La operación no se completó.",
                "EcoAccess P3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            // Base central: Oracle Autonomous Database en Oracle Cloud (mTLS con wallet). Ya no se usa Docker ni un
            // Oracle local. La cadena contiene la contraseña: vive en los User Secrets «ecoaccess-p3-framework» del
            // perfil de Windows y nunca se muestra. ECOACCESS_ConnectionStrings__OracleP3 la sustituye si existe.
            const string IdSecretos = "ecoaccess-p3-framework";
            IConfiguration configuracion = new ConfigurationBuilder().AddUserSecrets(IdSecretos).AddEnvironmentVariables("ECOACCESS_").Build();
            string? cadena = configuracion.GetConnectionString("OracleP3");

            // Comprobación real antes de abrir: si falta la conexión, Oracle no responde o la contraseña dejó de servir,
            // se abre la configuración inicial en lugar de cerrar la aplicación con un error.
            EstadoConexionOracle estado = Comprobar(cadena);
            if (estado != EstadoConexionOracle.Correcta)
            {
                string? actual = cadena;
                var (aliasActual, usuarioActual) = WalletOracleNube.Describir(actual);
                using var activacion = new FormActivacionOracle(
                    WalletOracleNube.CarpetaDe(actual) ?? configuracion["EcoAccess:Oracle:CarpetaWallet"] ?? WalletOracleNube.CarpetaPredeterminada,
                    (WalletOracleNube.UsaWallet(actual) ? aliasActual : null) ?? configuracion["EcoAccess:Oracle:Alias"] ?? WalletOracleNube.AliasPredeterminado,
                    usuarioActual ?? "ECOACCESS_P3_APP",
                    estado is EstadoConexionOracle.CadenaInvalida or EstadoConexionOracle.CredencialesInvalidas || !WalletOracleNube.UsaWallet(actual),
                    async (carpeta, alias, credenciales, ct) =>
                    {
                        string? prueba = credenciales is null ? actual : CadenaNube(carpeta, alias, credenciales);
                        return Explicar(await VerificadorConexionOracle.ComprobarAsync(prueba, ct));
                    });
                if (activacion.ShowDialog() != DialogResult.OK) return;
                if (activacion.Usuario is not null)
                {
                    cadena = CadenaNube(activacion.CarpetaWallet!, activacion.Alias!, activacion.Usuario + "\n" + activacion.Contrasena);
                    // Solo después de que Oracle la aceptó: queda en el perfil de Windows, fuera del repositorio.
                    ConfiguracionLocal.Guardar(IdSecretos, new Dictionary<string, string>
                    {
                        ["ConnectionStrings:OracleP3"] = cadena,
                        ["EcoAccess:Oracle:CarpetaWallet"] = activacion.CarpetaWallet!,
                        ["EcoAccess:Oracle:Alias"] = activacion.Alias!,
                    });
                }
            }

            if (string.IsNullOrWhiteSpace(cadena)) return;

            ServiciosAplicacion servicios;
            try
            {
                // Aquí siempre hay cadena: la configuración inicial no deja continuar sin una conexión comprobada.
                var fabrica = new FabricaConexionesOracle(cadena!);
                var usuarios = new RepositorioUsuariosOracle(fabrica);
                var materiales = new RepositorioMaterialesOracle(fabrica);
                var puntos = new RepositorioPuntosInstalacionOracle(fabrica);
                var contenedores = new RepositorioContenedoresOracle(fabrica);
                servicios = new ServiciosAplicacion(
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
                    configuracion["Telegram:NombreBot"]?.Trim() is { Length: > 0 } nombreBot ? nombreBot : null,
                    new ServicioEcopuntos(usuarios, new RepositorioInteraccionesOracle(fabrica), materiales,
                        new RepositorioEcopuntosOracle(fabrica), TimeProvider.System),
                    new ServicioDiagnosticoOracle(usuarios, new RepositorioDiagnosticoOracle(fabrica), TimeProvider.System),
                    new ServicioPanelAdministrativo(usuarios, new RepositorioPanelAdministrativoOracle(fabrica)),
                    new ServicioRegistroCiudadanos(new RepositorioRegistroCiudadanosOracle(fabrica), new HasherPbkdf2(), TimeProvider.System),
                    new ServicioCatalogoAmbiental(materiales, contenedores),
                    new ServicioOpiniones(usuarios, new RepositorioOpinionesOracle(fabrica), TimeProvider.System),
                    new ServicioInformeInstitucional(usuarios, new RepositorioInformeInstitucionalOracle(fabrica)));
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

            // Entrada pública para kiosco/revisión: usa exactamente la sesión invitada existente, sin aceptar actor ni credenciales por argumentos.
            bool explorarDirectamente = args.Length == 1 && args[0] == "--explorar";
            // Inicio de sesión y menú en bucle: cerrar sesión vuelve al inicio; cancelar o cerrar la ventana termina.
            while (true)
            {
                SesionActiva? sesion;
                bool invitado;
                if (explorarDirectamente)
                {
                    sesion = null;
                    invitado = true;
                    explorarDirectamente = false;
                }
                else using (var inicio = new FormInicioSesion(servicios.Autenticacion, servicios.RegistroCiudadanos))
                {
                    if (inicio.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    sesion = inicio.Sesion;
                    invitado = inicio.AccesoInvitado;
                }

                if (!invitado && sesion is null) return;
                if (invitado || !(sesion!.TieneRol(CodigoRol.Administrador) || sesion.TieneRol(CodigoRol.Operario)))
                {
                    using var explorar = new FormExplorarEcoAccess(servicios, sesion);
                    Application.Run(explorar);
                    if (!explorar.SolicitarInicioSesion) return;
                    continue;
                }

                using var principal = new FormPrincipal(servicios, sesion!);
                Application.Run(principal);
                if (!principal.CerrarSesionSolicitado)
                {
                    return;
                }
            }
        }

        // Un solo intento con tiempo límite; Main aún no tiene bucle de mensajes, así que esperar aquí no congela ventanas.
        private static EstadoConexionOracle Comprobar(string? cadena)
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try { return Task.Run(() => VerificadorConexionOracle.ComprobarAsync(cadena, limite.Token)).GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { return EstadoConexionOracle.ServicioNoDisponible; }
        }

        /// <summary>
        /// Cadena de Oracle Cloud con el wallet de la carpeta indicada. Si el wallet no sirve devuelve una cadena vacía,
        /// que el verificador informa como «cadena inválida» sin intentar autenticar.
        /// </summary>
        private static string CadenaNube(string carpetaWallet, string alias, string credenciales)
        {
            WalletOracleNube? wallet = WalletOracleNube.Abrir(carpetaWallet, out _);
            string[] valores = credenciales.Split(new[] { '\n' }, 2);
            if (wallet is null || !wallet.TryObtenerDestino(alias, out _, out _)) return "";
            return wallet.CrearCadena(alias, valores[0], valores.Length == 2 ? valores[1] : "");
        }

        /// <summary>Mensaje sencillo para la persona; nunca incluye la cadena ni el error interno de Oracle.</summary>
        private static (bool Correcta, bool PedirContrasena, string Mensaje) Explicar(EstadoConexionOracle estado) => estado switch
        {
            EstadoConexionOracle.Correcta => (true, false, ""),
            EstadoConexionOracle.CredencialesInvalidas => (false, true, "Oracle rechazó el usuario o la contraseña. Revísalos con calma: varios intentos fallidos pueden bloquear la cuenta."),
            EstadoConexionOracle.CuentaBloqueada => (false, false, "La cuenta del esquema está bloqueada en Oracle. Debe desbloquearla el administrador de la base."),
            EstadoConexionOracle.EsquemaIncompleto => (false, false, "La conexión funciona, pero ese esquema no tiene las tablas de EcoAccess. Revisa el usuario."),
            EstadoConexionOracle.CadenaInvalida => (false, true, "Falta la conexión en este equipo: escribe la contraseña del esquema."),
            _ => (false, false, "Oracle Cloud no respondió. Comprueba que la base EcoAccess esté «Available» en la consola de Oracle Cloud, que el wallet sea el actual y que haya Internet; luego vuelve a comprobar."),
        };
    }
}
