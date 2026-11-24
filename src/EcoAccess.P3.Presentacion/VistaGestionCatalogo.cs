using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Marco visual de los catálogos de gestión (Usuarios, Materiales) con la estructura Eco (skill eco-ui-supervisor):
    /// encabezado con icono, tarjeta de búsqueda, tarjeta con la tabla y su paginación, y tarjeta lateral con el editor.
    /// Recibe controles existentes y no conoce servicios ni permisos.
    /// </summary>
    internal sealed class VistaGestionCatalogo : UserControl
    {
        private readonly TableLayoutPanel _raiz;

        // Misma regla que PanelDesplazable: al cambiar de pestaña o restaurar, la vista no salta al control con foco.
        protected override Point ScrollToControl(Control activeControl) =>
            EstabilizadorVentana.NavegandoConTeclado() ? base.ScrollToControl(activeControl) : DisplayRectangle.Location;

        internal VistaGestionCatalogo(string titulo, string descripcion, IconChar icono,
            Control filtros, DataGridView tabla, Control paginacion, Control editor, Label estado,
            string tituloLista = "Resultados", string tituloEditor = "Acciones", string tituloBusqueda = "Buscar", int anchoLateral = 420,
            Control? banner = null, IconChar iconoBusqueda = IconChar.None, IconChar iconoLista = IconChar.None, IconChar iconoEditor = IconChar.None)
        {
            Dock = DockStyle.Fill;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            AutoScroll = true;
            _raiz = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, Padding = new Padding(24, 18, 24, 18), BackColor = TemaEcoAccess.Fondo };
            _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            // Encabezado normal o, si el módulo lo trae, un banner con la estética del dashboard (BannerModulo).
            ModuloEco.Fila(_raiz, banner ?? ModuloEco.Encabezado(icono, titulo, descripcion));

            var busqueda = ModuloEco.Seccion(tituloBusqueda, icono: iconoBusqueda);
            filtros.TabIndex = 0;
            busqueda.Agregar(filtros);
            ModuloEco.Fila(_raiz, busqueda);

            var lista = ModuloEco.Seccion(tituloLista, llenar: true, icono: iconoLista);
            DisenoFormularios.PrepararTabla(tabla);
            tabla.TabIndex = 0;
            lista.Agregar(tabla, llenar: true);
            paginacion.TabIndex = 1;
            lista.Agregar(paginacion);
            var lateral = ModuloEco.Seccion(tituloEditor, llenar: true, icono: iconoEditor);
            editor.TabIndex = 1;
            lateral.Agregar(editor, llenar: true);
            var cuerpo = ModuloEco.DosColumnas(lista, lateral, anchoLateral);
            cuerpo.MinimumSize = new Size(0, 520);
            ModuloEco.Fila(_raiz, cuerpo, llenar: true);
            ModuloEco.Fila(_raiz, ModuloEco.Mensaje(estado));

            estado.TextChanged += (_, _) => AjustarAltura();
            SizeChanged += (_, _) =>
            {
                estado.MaximumSize = new Size(Math.Max(160, ClientSize.Width - TemaEcoAccess.Px(this, 60)), 0);
                AjustarAltura();
            };
            cuerpo.SizeChanged += (_, _) => AjustarAltura();
            Controls.Add(_raiz);
            TemaEcoAccess.Aplicar(this);
        }

        /// <summary>La raíz llena el alto visible; si el contenido no cabe, crece y la vista se desplaza.</summary>
        private void AjustarAltura()
        {
            int preferida = _raiz.GetPreferredSize(new Size(Math.Max(1, ClientSize.Width - SystemInformation.VerticalScrollBarWidth), 0)).Height;
            int altura = Math.Max(ClientSize.Height, preferida);
            if (_raiz.Height != altura) _raiz.Height = altura;
        }
    }
}
