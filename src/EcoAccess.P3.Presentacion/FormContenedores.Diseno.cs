using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Canecas y QR» (skill eco-ui-supervisor): encabezado, tabla en tarjeta y columna lateral con las acciones,
    /// los materiales admitidos y el registro. Reutiliza los controles del Designer, así los eventos y la lógica no cambian;
    /// las acciones siguen dentro de <c>panelAcciones</c>, que el formulario deshabilita mientras trabaja.
    /// </summary>
    public partial class FormContenedores
    {
        private void ConstruirDiseno()
        {
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Trash, "Canecas y códigos QR",
                "Registra las canecas de cada punto de instalación, controla si están en servicio y qué materiales admiten.");

            var lista = ModuloEco.Seccion("Canecas registradas", "Selecciona una caneca de la tabla para ver su QR, cambiar su estado o sus materiales.", llenar: true);
            lista.Agregar(tablaCanecas, llenar: true);
            lista.Agregar(ModuloEco.Mensaje(etiquetaEstado));

            var seleccion = ModuloEco.Seccion("Caneca seleccionada", "Acciones sobre la fila elegida en la tabla.");
            seleccion.Agregar(ModuloEco.Rejilla(
                ModuloEco.Boton(botonVerQr, IconChar.Qrcode),
                ModuloEco.Boton(botonActualizar, IconChar.ArrowsRotate),
                ModuloEco.Boton(botonEnServicio, IconChar.CirclePlay),
                ModuloEco.Boton(botonFueraDeServicio, IconChar.CirclePause)));
            seleccion.Agregar(ModuloEco.Boton(botonDarDeBaja, IconChar.TrashCan, ancho: true));

            var materiales = ModuloEco.Seccion(grupoMateriales.Text, "Lo que la caneca seleccionada recibe.");
            grupoMateriales.TextChanged += (_, _) => materiales.Titulo.Text = grupoMateriales.Text;
            listaAdmitidos.Height = 112;
            listaAdmitidos.Margin = new Padding(2, 2, 2, 12);
            materiales.Agregar(listaAdmitidos);
            materiales.Agregar(ModuloEco.Boton(botonQuitar, IconChar.Xmark, ancho: true));
            materiales.Agregar(ModuloEco.Campo("Material para admitir", IconChar.Leaf, comboMaterial));
            materiales.Agregar(ModuloEco.Boton(botonAdmitir, IconChar.Plus, ancho: true));

            var registro = ModuloEco.Seccion(grupoRegistro.Text, "Crea una caneca en un punto de instalación activo.");
            registro.Agregar(ModuloEco.Campo(etiquetaPunto, IconChar.LocationDot, comboPunto));
            registro.Agregar(ModuloEco.Campo(etiquetaNombre, IconChar.Tag, textoNombre));
            registro.Agregar(ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));

            ModuloEco.Columna(panelAcciones, seleccion, materiales, registro);
            ModuloEco.Fila(raiz, ModuloEco.DosColumnas(lista, panelAcciones, 440), llenar: true);
            Disposed += (_, _) => { grupoMateriales.Dispose(); grupoRegistro.Dispose(); };
            ModuloEco.Terminar(this, raiz, new Size(640, 600));
        }
    }
}
