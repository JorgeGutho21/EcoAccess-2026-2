using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>Disposición Eco del cambio de contraseña del personal (skill eco-ui-supervisor). Reutiliza los controles del Designer.</summary>
    public partial class FormCambioContrasena
    {
        private void ConstruirDiseno()
        {
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.UserGear, "Perfil y contraseña", etiquetaExplicacion.Text);
            var tarjeta = ModuloEco.Seccion("Cambiar contraseña", "Escribe la actual y la nueva dos veces. La sesión sigue abierta.");
            foreach ((Label etiqueta, TextBox caja) in new[] { (etiquetaActual, textoActual), (etiquetaNueva, textoNueva), (etiquetaConfirmacion, textoConfirmacion) })
            {
                etiqueta.Text = etiqueta.Text.TrimEnd(':');
                tarjeta.Agregar(ModuloEco.Campo(etiqueta, IconChar.Lock, caja));
            }
            tarjeta.Agregar(ModuloEco.Botones(ModuloEco.Boton(botonAceptar, IconChar.Key), ModuloEco.Boton(botonCancelar, IconChar.Xmark)));
            tarjeta.Agregar(ModuloEco.Mensaje(etiquetaEstado));
            ModuloEco.Fila(raiz, tarjeta);
            raiz.AutoScroll = true;
            ClientSize = new Size(520, 640);
            Disposed += (_, _) => { panelCampos.Dispose(); etiquetaExplicacion.Dispose(); };
            ModuloEco.Terminar(this, raiz, new Size(420, 420));
        }
    }
}
