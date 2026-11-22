using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Puntos de instalación» (skill eco-ui-supervisor). Reutiliza los controles del Designer; las acciones
    /// siguen dentro de <c>panelAcciones</c>, que el formulario deshabilita mientras trabaja.
    /// </summary>
    public partial class FormPuntos
    {
        private void ConstruirDiseno()
        {
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.LocationDot, "Puntos de instalación",
                "Los lugares del campus donde se instalan las canecas. Solo los puntos activos reciben canecas nuevas.");

            var lista = ModuloEco.Seccion("Puntos registrados", "Selecciona un punto de la tabla para activarlo o desactivarlo.", llenar: true);
            lista.Agregar(tablaPuntos, llenar: true);
            lista.Agregar(ModuloEco.Mensaje(etiquetaEstado));

            var seleccion = ModuloEco.Seccion("Punto seleccionado", "Acciones sobre la fila elegida en la tabla.");
            seleccion.Agregar(ModuloEco.Rejilla(
                ModuloEco.Boton(botonActivar, IconChar.CirclePlay),
                ModuloEco.Boton(botonDesactivar, IconChar.CirclePause)));
            seleccion.Agregar(ModuloEco.Boton(botonActualizar, IconChar.ArrowsRotate, ancho: true));

            var registro = ModuloEco.Seccion(grupoRegistro.Text, "Crea un punto nuevo; queda disponible para registrar canecas en él.");
            registro.Agregar(ModuloEco.Campo(etiquetaNombre, IconChar.LocationDot, textoNombre));
            registro.Agregar(ModuloEco.Campo(etiquetaReferencia, IconChar.MapLocationDot, textoReferencia, alto: textoReferencia.Multiline ? 96 : ModuloEco.AltoCampo));
            registro.Agregar(ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));

            ModuloEco.Columna(panelAcciones, seleccion, registro);
            ModuloEco.Fila(raiz, ModuloEco.DosColumnas(lista, panelAcciones, 440), llenar: true);
            Disposed += (_, _) => grupoRegistro.Dispose();
            ModuloEco.Terminar(this, raiz, new Size(640, 560));
        }
    }
}
