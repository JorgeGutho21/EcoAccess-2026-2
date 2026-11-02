using System;
using System.Drawing;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// La persona con sesión cambia su propia contraseña dando la actual. El servicio cuenta el error
    /// en la actual como intento fallido, así que esta pantalla no sirve para adivinarla.
    /// </summary>
    public partial class FormCambioContrasena : Form
    {
        private readonly ServicioAutenticacion _servicio;
        private readonly SesionActiva _sesion;
        private bool _ocupado;

        public FormCambioContrasena(ServicioAutenticacion servicio, SesionActiva sesion)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            InitializeComponent();
            etiquetaExplicacion.Text = $"Cuenta: {sesion.Nombre}. La contraseña nueva debe tener entre " +
                $"{CredencialAcceso.LongitudMinimaContrasena} y {CredencialAcceso.LongitudMaximaContrasena} caracteres.";
            textoActual.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            textoNueva.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            textoConfirmacion.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            FormClosing += (_, e) => e.Cancel = _ocupado;
        }

        private async void BotonAceptar_Click(object? sender, EventArgs e)
        {
            if (_ocupado)
            {
                return;
            }

            // Las contraseñas salen de los controles antes de llamar, para que no queden en ellos pase lo que pase.
            string actual = textoActual.Text;
            string nueva = textoNueva.Text;
            string confirmacion = textoConfirmacion.Text;
            textoActual.Clear();
            textoNueva.Clear();
            textoConfirmacion.Clear();
            if (nueva != confirmacion)
            {
                MostrarError("Las dos copias de la contraseña nueva no coinciden.");
                return;
            }

            _ocupado = true;
            UseWaitCursor = true;
            panelCampos.Enabled = false;
            try
            {
                Resultado resultado = await _servicio.CambiarContrasenaAsync(_sesion.IdUsuario, actual, nueva);
                if (resultado.Exitoso)
                {
                    _ocupado = false;
                    MessageBox.Show(this, "Contraseña cambiada.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
