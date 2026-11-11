using System;
using System.Drawing;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Configuración inicial del primer administrador, para no escribir su contraseña en un script SQL. Tiene dos casos:
    /// si la base no tiene ninguna cuenta con el rol Administrador, pide nombre, correo y contraseña y la crea
    /// (<see cref="ServicioAutenticacion.CrearAdministradorInicialAsync"/>); si ya existe una cuenta de administrador
    /// sin contraseña, solo le da la primera (<see cref="ServicioAutenticacion.ConfigurarAdministradorInicialAsync"/>).
    /// Solo se ofrece mientras ningún administrador tiene contraseña, y el servicio vuelve a comprobarlo al guardar.
    /// </summary>
    public partial class FormConfiguracionInicial : Form
    {
        private readonly ServicioAutenticacion _servicio;
        private bool _ocupado;
        private bool _crearCuenta;
        private int _desplazamiento;

        /// <summary>Correo que quedó configurado, para precargarlo en el inicio de sesión.</summary>
        public string CorreoConfigurado { get; private set; } = string.Empty;

        public FormConfiguracionInicial(ServicioAutenticacion servicio)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            InitializeComponent();
            textoNombre.MaxLength = Usuario.LongitudMaximaNombre;
            textoCorreo.MaxLength = Usuario.LongitudMaximaCorreo;
            textoContrasena.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            textoConfirmacion.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            MostrarModo(crearCuenta: false);
            FormClosing += (_, e) => e.Cancel = _ocupado;
            Shown += async (_, _) =>
            {
                try
                {
                    MostrarModo(await _servicio.FaltaCuentaAdministradorAsync());
                }
                catch (Exception)
                {
                    MostrarError("No se pudo conectar con la base de datos. Revise que Oracle esté en marcha e inténtelo de nuevo.");
                }
            };
        }

        /// <summary>Muestra u oculta el nombre según haya que crear la cuenta o solo darle contraseña.</summary>
        private void MostrarModo(bool crearCuenta)
        {
            _crearCuenta = crearCuenta;
            etiquetaNombre.Visible = crearCuenta;
            textoNombre.Visible = crearCuenta;

            // Sin el nombre, los demás campos suben para no dejar un hueco.
            int desplazamiento = (crearCuenta ? 0 : -textoCorreo.Height - 35) - _desplazamiento;
            if (desplazamiento != 0)
            {
                foreach (Control control in panelCampos.Controls)
                {
                    if (control != etiquetaNombre && control != textoNombre) control.Top += desplazamiento;
                }

                panelCampos.Height += desplazamiento;
                etiquetaEstado.Top += desplazamiento;
                ClientSize = new Size(ClientSize.Width, ClientSize.Height + desplazamiento);
                _desplazamiento += desplazamiento;
            }

            etiquetaExplicacion.Text = crearCuenta
                ? "La base de datos no tiene ninguna cuenta de administrador. Escriba su nombre, su correo y una contraseña " +
                  $"({CredencialAcceso.LongitudMinimaContrasena} caracteres como mínimo). Este paso solo se puede hacer una vez."
                : "Ningún administrador tiene contraseña todavía. Escriba el correo de una cuenta de administrador activa y " +
                  $"su contraseña ({CredencialAcceso.LongitudMinimaContrasena} caracteres como mínimo).";
            (crearCuenta ? (Control)textoNombre : textoCorreo).Select();
        }

        private async void BotonAceptar_Click(object? sender, EventArgs e)
        {
            if (_ocupado)
            {
                return;
            }

            // Comparar las dos copias es una ayuda de la pantalla, no una regla de negocio.
            string contrasena = textoContrasena.Text;
            string confirmacion = textoConfirmacion.Text;
            textoContrasena.Clear();
            textoConfirmacion.Clear();
            if (contrasena != confirmacion)
            {
                MostrarError("Las dos contraseñas no coinciden.");
                return;
            }

            _ocupado = true;
            UseWaitCursor = true;
            panelCampos.Enabled = false;
            try
            {
                Resultado resultado = _crearCuenta
                    ? await _servicio.CrearAdministradorInicialAsync(textoNombre.Text, textoCorreo.Text, contrasena)
                    : await _servicio.ConfigurarAdministradorInicialAsync(textoCorreo.Text, contrasena);
                if (resultado.Exitoso)
                {
                    CorreoConfigurado = textoCorreo.Text.Trim();
                    _ocupado = false;
                    DialogResult = DialogResult.OK;
                    return;
                }

                MostrarError(resultado.Mensaje);
            }
            catch (Exception)
            {
                MostrarError("No se pudo conectar con la base de datos. Revise que Oracle esté en marcha e inténtelo de nuevo.");
            }
            finally
            {
                _ocupado = false;
                panelCampos.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void MostrarError(string? mensaje)
        {
            etiquetaEstado.ForeColor = Color.DarkRed;
            etiquetaEstado.Text = mensaje ?? "No se pudo completar la operación.";
        }
    }
}
