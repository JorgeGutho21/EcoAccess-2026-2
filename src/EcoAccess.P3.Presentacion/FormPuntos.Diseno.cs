using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Puntos de instalación»: banner con el estado actual, tabla pulida, ficha de la fila elegida y
    /// acciones laterales. Reutiliza los controles del Designer; las acciones siguen dentro de <c>panelAcciones</c>, que el
    /// formulario deshabilita mientras trabaja.
    /// </summary>
    public partial class FormPuntos
    {
        private readonly TabControl _paginasPuntos = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Gestión del punto de instalación" };
        private readonly Label _fichaPunto = DisenoFormularios.Texto("Selecciona un punto de la tabla.");

        private void ConstruirDiseno()
        {
            var banner = new BannerModulo("Puntos de instalación",
                "Administra los lugares del campus donde se instalan las canecas. Solo los puntos activos reciben canecas nuevas.",
                IconChar.LocationDot) { Estado = "Sin puntos consultados todavía." };
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.LocationDot, "Puntos de instalación",
                "Los lugares del campus donde se instalan las canecas. Solo los puntos activos reciben canecas nuevas.", banner);

            var lista = ModuloEco.Seccion("Puntos registrados", "Selecciona un punto de la tabla para activarlo o desactivarlo.",
                llenar: true, icono: IconChar.LocationDot);
            lista.Agregar(tablaPuntos, llenar: true);
            lista.Agregar(ModuloEco.Mensaje(etiquetaEstado));

            // Página «Punto»: las acciones sobre la fila elegida no compiten visualmente con el formulario de registro.
            TableLayoutPanel punto = CrearPestanaPunto("Punto");
            DisenoFormularios.Agregar(punto, DisenoFormularios.Texto("Punto seleccionado", true));
            DisenoFormularios.Agregar(punto, DisenoGestion.Ficha(_fichaPunto, IconChar.LocationDot, "Punto de instalación"));
            DisenoFormularios.Agregar(punto, ModuloEco.Rejilla(
                DisenoGestion.BotonSuave(botonActivar, IconChar.CircleCheck),
                DisenoGestion.BotonSuave(botonDesactivar, IconChar.CircleXmark)));
            DisenoFormularios.Agregar(punto, DisenoGestion.BotonSuave(botonActualizar, IconChar.ArrowsRotate));

            // Página «Registrar»: alta independiente, como en la referencia de Canecas y QR.
            TableLayoutPanel registro = CrearPestanaPunto("Registrar");
            DisenoFormularios.Agregar(registro, new AvisoEco(DisenoFormularios.Texto("Activa el punto cuando esté listo para recibir canecas nuevas."), "Nuevo punto"));
            DisenoFormularios.Agregar(registro, ModuloEco.Campo(etiquetaNombre, IconChar.LocationDot, textoNombre));
            DisenoGestion.TextoGuia(textoNombre, "Ej. Bloque A, primer piso");
            DisenoFormularios.Agregar(registro, ModuloEco.Campo(etiquetaReferencia, IconChar.MapLocationDot, textoReferencia, alto: textoReferencia.Multiline ? 96 : ModuloEco.AltoCampo));
            DisenoFormularios.Agregar(registro, ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));

            var gestion = ModuloEco.Seccion("Gestión del punto", "Consulta la ubicación elegida, cambia su disponibilidad o registra un punto nuevo.",
                llenar: true, icono: IconChar.Gear);
            gestion.Agregar(new SelectorPestanas(_paginasPuntos, IconChar.LocationDot, IconChar.Plus));
            gestion.Agregar(_paginasPuntos, llenar: true);
            panelAcciones.Controls.Clear();
            panelAcciones.AutoScroll = false;
            panelAcciones.BorderStyle = BorderStyle.None;
            panelAcciones.Padding = Padding.Empty;
            panelAcciones.BackColor = TemaEcoAccess.Fondo;
            gestion.Dock = DockStyle.Fill;
            gestion.Margin = Padding.Empty;
            panelAcciones.Controls.Add(gestion);
            ModuloEco.Fila(raiz, ModuloEco.DosColumnas(lista, panelAcciones, 440), llenar: true);
            PulirTablaPuntos(banner);
            Disposed += (_, _) => grupoRegistro.Dispose();
            ModuloEco.Terminar(this, raiz, new Size(640, 560));
        }

        private TableLayoutPanel CrearPestanaPunto(string titulo)
        {
            var pagina = new TabPage(titulo) { BackColor = Color.White, AutoScroll = true, Padding = Padding.Empty };
            var columna = DisenoFormularios.Columna();
            columna.Padding = new Padding(0, 4, 6, 4);
            int anchoAplicado = -1;
            columna.SizeChanged += (_, _) =>
            {
                int ancho = System.Math.Max(100, columna.ClientSize.Width - columna.Padding.Horizontal);
                if (ancho == anchoAplicado) return;
                anchoAplicado = ancho;
                foreach (Control control in columna.Controls)
                    if (control is Label etiqueta) etiqueta.MaximumSize = new Size(ancho, 0);
                // Las filas de alto automático se vuelven a medir cuando ya existe la ventana; antes no hay un handle válido.
                if (columna.IsHandleCreated) columna.BeginInvoke(new System.Action(() => { if (!columna.IsDisposed) columna.PerformLayout(); }));
            };
            pagina.Controls.Add(columna);
            _paginasPuntos.TabPages.Add(pagina);
            return columna;
        }

        /// <summary>
        /// Tabla con el aspecto del dashboard: el punto se identifica con su icono y el estado se presenta en una pastilla. La
        /// ficha y la línea del banner se alimentan exclusivamente de la tabla existente, sin consultas ni cambios de lógica.
        /// </summary>
        private void PulirTablaPuntos(BannerModulo banner)
        {
            DisenoGestion.PulirTabla(tablaPuntos, columna => columna switch
            {
                "Nombre" => 270, "Referencia" => 320, "Estado" => 135, _ => 0,
            }, "Aún no hay puntos para mostrar. Registra el primero en el panel lateral.", IconChar.LocationDot);
            tablaPuntos.CellPainting += PintarCeldaPunto;
            tablaPuntos.SelectionChanged += (_, _) => MostrarFichaPunto();
            tablaPuntos.CurrentCellChanged += (_, _) => MostrarFichaPunto();
            tablaPuntos.DataBindingComplete += (_, _) =>
            {
                int total = tablaPuntos.Rows.Count;
                banner.Estado = total == 0 ? "No hay puntos de instalación registrados." : $"{total} puntos de instalación registrados.";
                MostrarFichaPunto();
            };
        }

        private void MostrarFichaPunto()
        {
            _fichaPunto.Text = tablaPuntos.CurrentRow?.DataBoundItem is FilaPunto fila
                ? string.IsNullOrWhiteSpace(fila.Referencia)
                    ? $"{fila.Nombre}\n{fila.Estado}"
                    : $"{fila.Nombre}\n{fila.Referencia}\n{fila.Estado}"
                : "Selecciona un punto de la tabla.";
        }

        private void PintarCeldaPunto(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is not string valor) return;
            string columna = tablaPuntos.Columns[e.ColumnIndex].Name;
            if (columna is not ("Nombre" or "Estado")) return;

            Graphics g = e.Graphics;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            e.PaintBackground(e.CellBounds, elegida);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int px(int n) => TemaEcoAccess.Px(tablaPuntos, n);
            Color textoFila = elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Texto;
            const TextFormatFlags Linea = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

            if (columna == "Nombre")
            {
                int lado = px(30);
                var cuadro = new Rectangle(e.CellBounds.Left + px(8), e.CellBounds.Top + (e.CellBounds.Height - lado) / 2, lado, lado);
                DibujoEco.Icono(g, IconChar.LocationDot, TemaEcoAccess.Verde, TemaEcoAccess.VerdeSuave, cuadro, px(8));
                TextRenderer.DrawText(g, valor, e.CellStyle?.Font ?? tablaPuntos.Font,
                    Rectangle.FromLTRB(cuadro.Right + px(10), e.CellBounds.Top, e.CellBounds.Right - px(6), e.CellBounds.Bottom), textoFila, Linea);
            }
            else
            {
                DisenoGestion.PastillaEstado(g, e.CellBounds, valor, valor == "Activo", tablaPuntos);
            }

            e.Handled = true;
        }
    }
}
