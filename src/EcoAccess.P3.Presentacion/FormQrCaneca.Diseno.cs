using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco del QR de una caneca cuando lo abre el Supervisor desde «Canecas y QR» (skill eco-ui-supervisor). La imagen
    /// del código es un dato, no decoración. La vista del ciudadano conserva su disposición.
    /// </summary>
    public partial class FormQrCaneca
    {
        private void ConstruirDiseno()
        {
            Button[] enlace = Controls.OfType<Button>().Where(b => b != botonGuardar && b != botonImprimir && b != botonCerrar).ToArray();
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Qrcode, _nombreCaneca,
                "Código QR de la caneca. Guárdalo en PNG o imprímelo para pegarlo en ella.");
            var tarjeta = ModuloEco.Seccion("Código QR");
            imagenQr.Size = new Size(280, 280);
            imagenQr.SizeMode = PictureBoxSizeMode.Zoom;
            imagenQr.Dock = DockStyle.None;
            imagenQr.Anchor = AnchorStyles.Top;
            imagenQr.Margin = new Padding(0, 0, 0, 12);
            tarjeta.Agregar(imagenQr);
            imagenQr.Dock = DockStyle.None;
            textoEnlace.ReadOnly = true;
            tarjeta.Agregar(ModuloEco.Campo("Enlace", IconChar.Link, textoEnlace));
            foreach (Button boton in enlace) ModuloEco.Boton(boton, boton.Text.Contains("Copiar") ? IconChar.Copy : IconChar.PaperPlane);
            if (enlace.Length > 0) tarjeta.Agregar(ModuloEco.Rejilla(enlace));
            tarjeta.Agregar(ModuloEco.Rejilla(ModuloEco.Boton(botonGuardar, IconChar.Download), ModuloEco.Boton(botonImprimir, IconChar.Print)));
            tarjeta.Agregar(ModuloEco.Boton(botonCerrar, IconChar.Xmark, ancho: true));
            tarjeta.Agregar(ModuloEco.Mensaje(etiquetaEstado));
            ModuloEco.Fila(raiz, tarjeta);
            raiz.AutoScroll = true;
            etiquetaCaneca.Visible = false;
            ClientSize = new Size(520, 720);
            Disposed += (_, _) => etiquetaCaneca.Dispose();
            ModuloEco.Terminar(this, raiz, new Size(420, 420));
        }
    }
}
