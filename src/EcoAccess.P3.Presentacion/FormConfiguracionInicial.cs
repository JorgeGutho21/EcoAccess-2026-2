using System;
using System.Drawing;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Primera contraseña del administrador inicial, para no escribirla en un script SQL. Solo se ofrece
    /// mientras ningún administrador activo tiene contraseña, y el servicio vuelve a comprobarlo al guardar.
    /// </summary>
    public partial class FormConfiguracionInicial : Form
    {
        private readonly ServicioAutenticacion _servicio;
        private bool _ocupado;

        /// <summary>Correo que quedó configurado, para precargarlo en el inicio de sesión.</summary>
        public string CorreoConfigurado { get; private set; } = string.Empty;

        public FormConfiguracionInicial(ServicioAutenticacion servicio)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            InitializeComponent();
            etiquetaExplicacion.Text = "Ningún administrador tiene contraseña todavía. Escriba el correo de una cuenta de " +
                $"administrador activa y su contraseña ({CredencialAcceso.LongitudMinimaContrasena} caracteres como mínimo).";
            textoCorreo.MaxLength = Usuario.LongitudMaximaCorreo;
            textoContrasena.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            textoConfirmacion.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            FormClosing += (_, e) => e.Cancel = _ocupado;
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
                Resultado resultado = await _servicio.ConfigurarAdministradorInicialAsync(textoCorreo.Text, contrasena);
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

        private void botonAceptar_Click_1(object sender, EventArgs e)
        {

        }
    }
}
