using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    public partial class FormMateriales
    {
        private readonly CajaTexto _busquedaMaterial = new()
        {
            Width = 230,
            MaxLength = Material.LongitudMaximaNombre,
            PlaceholderText = "Código o nombre del material",
            AccessibleName = "Buscar material por código o nombre"
        };
        private readonly ComboBox _filtroCategoria = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, AccessibleName = "Filtrar categoría ambiental" };
        private readonly Button _buscarMaterial = DisenoFormularios.Accion("Buscar");
        private readonly Button _anteriorMaterial = DisenoFormularios.Accion("Anterior");
        private readonly Button _siguienteMaterial = DisenoFormularios.Accion("Siguiente");
        private readonly Button _cancelarMaterial = DisenoFormularios.Accion("Cancelar consulta");
        private readonly Label _paginaMaterial = DisenoFormularios.Texto("Página sin consultar");
        private readonly Label _detalleMaterial = DisenoFormularios.Texto("Selecciona un material de la tabla.");
        private readonly FlowLayoutPanel _filtrosMaterial = new() { AutoSize = true, WrapContents = true };
        private readonly FlowLayoutPanel _paginacionMaterial = new() { AutoSize = true, WrapContents = true };
        private readonly ErrorProvider _erroresMaterial = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        private readonly TabControl _pestanasMaterial = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Gestión del catálogo ambiental" };
        private int _numeroPaginaMaterial = 1;
        private bool _haySiguienteMaterial;
        private bool _lecturaAutorizadaMaterial;
        private string _textoAplicadoMaterial = "";
        private CategoriaMaterial? _categoriaAplicadaMaterial;
        private bool _soloHabilitadosAplicado;
        private CancellationTokenSource? _consultaMaterial;
        private Control? _focoMaterial;
        private bool _escrituraMaterial;
        private bool _cambioMaterialConfirmado;

        private void ConstruirDisenoMateriales()
        {
            SuspendLayout();
            Controls.Clear();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = TemaEcoAccess.Cuerpo;
            MinimumSize = new Size(560, 440);
            _erroresMaterial.ContainerControl = this;
            Disposed += (_, _) => { _consultaMaterial?.Cancel(); _erroresMaterial.Dispose(); };
            _filtroCategoria.Items.Add("Todas las categorías");
            foreach (OpcionCategoria opcion in TextosCategoria.Opciones) _filtroCategoria.Items.Add(opcion.Texto);
            _filtroCategoria.SelectedIndex = 0;
            // Filtros como campos Eco; siguen siendo hijos de _filtrosMaterial, que el formulario deshabilita mientras consulta.
            checkSoloHabilitados.AutoSize = true;
            checkSoloHabilitados.Font = TemaEcoAccess.Subtitulo;
            checkSoloHabilitados.Margin = new Padding(4, TemaEcoAccess.Subtitulo.Height + 18, 18, 14);
            foreach (Control control in new Control[]
                     {
                         ModuloEco.Campo("Código o nombre", IconChar.MagnifyingGlass, _busquedaMaterial, ancho: 240),
                         ModuloEco.Campo("Categoría", IconChar.LayerGroup, _filtroCategoria, ancho: 230),
                         checkSoloHabilitados,
                         ModuloEco.JuntoACampos(ModuloEco.Boton(_buscarMaterial, IconChar.MagnifyingGlass)),
                         ModuloEco.JuntoACampos(ModuloEco.Boton(botonActualizar, IconChar.ArrowsRotate)),
                         ModuloEco.JuntoACampos(ModuloEco.Boton(_cancelarMaterial, IconChar.Xmark)),
                     })
            {
                control.TabIndex = _filtrosMaterial.Controls.Count;
                _filtrosMaterial.Controls.Add(control);
            }
            _filtrosMaterial.BackColor = TemaEcoAccess.Superficie;
            _cancelarMaterial.Visible = false;
            _paginacionMaterial.Controls.AddRange(new Control[] { _anteriorMaterial, _siguienteMaterial, _paginaMaterial });
            _paginacionMaterial.BackColor = TemaEcoAccess.Superficie;
            _paginaMaterial.Margin = new Padding(8, 12, 0, 0);

            // Gestión del material como en el diseño aprobado (igual que Usuarios y roles): píldoras con icono, aviso verde,
            // cajas con texto guía, ficha del material elegido y botones suaves. Mismos controles y eventos del Designer.
            var registro = CrearPestanaMaterial("Crear material", botonRegistrar);
            DisenoFormularios.Agregar(registro, new AvisoEco(DisenoFormularios.Texto("La categoría orienta la separación. La admisión depende también de cada caneca."), "Amplía el catálogo ambiental"));
            DisenoFormularios.Campo(registro, etiquetaCodigo, textoCodigo, IconChar.Barcode);
            DisenoFormularios.Campo(registro, etiquetaNombre, textoNombre, IconChar.Tag);
            DisenoFormularios.Campo(registro, etiquetaCategoria, comboCategoria, IconChar.LayerGroup);
            textoDescripcion.ScrollBars = ScrollBars.Vertical;
            textoDescripcion.Height = 96;
            DisenoFormularios.Campo(registro, etiquetaDescripcion, textoDescripcion, IconChar.AlignLeft);
            DisenoGestion.TextoGuia(textoCodigo, "Ejemplo: PET");
            DisenoGestion.TextoGuia(textoNombre, "Ingresa el nombre del material");
            var estado = CrearPestanaMaterial("Estado y detalle");
            DisenoFormularios.Agregar(estado, DisenoFormularios.Texto("Material seleccionado", true));
            DisenoFormularios.Agregar(estado, DisenoGestion.Ficha(_detalleMaterial, IconChar.Leaf, "Material"));
            DisenoFormularios.Agregar(estado, DisenoGestion.BotonSuave(botonHabilitar, IconChar.CircleCheck));
            DisenoFormularios.Agregar(estado, DisenoGestion.BotonSuave(botonDeshabilitar, IconChar.CircleXmark));
            DisenoFormularios.Agregar(estado, new AvisoEco(DisenoFormularios.Texto("Deshabilitar conserva el historial y retira el material de nuevas operaciones. No se permite dejar una caneca en servicio sin materiales habilitados.")));
            panelAcciones.Controls.Clear();
            grupoRegistro.Dispose();
            panelAcciones.Dock = DockStyle.Fill;
            panelAcciones.AutoScroll = false;
            panelAcciones.BackColor = TemaEcoAccess.Superficie;
            var gestion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            gestion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gestion.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gestion.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            gestion.Controls.Add(new SelectorPestanas(_pestanasMaterial, IconChar.Plus, IconChar.CircleInfo) { Dock = DockStyle.Fill }, 0, 0);
            gestion.Controls.Add(_pestanasMaterial, 0, 1);
            panelAcciones.Controls.Add(gestion);
            // Encabezado con la estética del banner del dashboard y la información actual del módulo (la página consultada).
            var banner = new BannerModulo("Materiales con propósito",
                "Catálogo ambiental: código, categoría y estado de cada material que reciben las canecas. Administración con permisos verificados.", IconChar.LayerGroup);
            banner.Estado = _paginaMaterial.Text;
            _paginaMaterial.TextChanged += (_, _) => banner.Estado = _paginaMaterial.Text;
            Controls.Add(new VistaGestionCatalogo("Materiales con propósito", "Catálogo ambiental · administración con permisos verificados",
                IconChar.Leaf, _filtrosMaterial, tablaMateriales, _paginacionMaterial, panelAcciones, etiquetaEstado,
                tituloLista: "Catálogo de materiales", tituloEditor: "Gestión del material", tituloBusqueda: "Buscar materiales", anchoLateral: 470,
                banner: banner, iconoBusqueda: IconChar.MagnifyingGlass, iconoLista: IconChar.LayerGroup, iconoEditor: IconChar.Gear));
            PulirTablaMateriales();

            _buscarMaterial.Click += async (_, _) =>
            {
                if (_ocupado) return;
                _numeroPaginaMaterial = 1;
                _textoAplicadoMaterial = _busquedaMaterial.Text;
                _categoriaAplicadaMaterial = _filtroCategoria.SelectedIndex <= 0 ? null : TextosCategoria.Opciones[_filtroCategoria.SelectedIndex - 1].Valor;
                _soloHabilitadosAplicado = checkSoloHabilitados.Checked;
                await EjecutarAsync(CargarAsync, lectura: true);
            };
            _anteriorMaterial.Click += async (_, _) =>
            {
                if (_ocupado || _numeroPaginaMaterial <= 1) return;
                _numeroPaginaMaterial--;
                await EjecutarAsync(CargarAsync, lectura: true);
            };
            _siguienteMaterial.Click += async (_, _) =>
            {
                if (_ocupado || !_haySiguienteMaterial) return;
                _numeroPaginaMaterial++;
                await EjecutarAsync(CargarAsync, lectura: true);
            };
            _cancelarMaterial.Click += (_, _) => _consultaMaterial?.Cancel();
            _busquedaMaterial.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _buscarMaterial.PerformClick(); } };
            tablaMateriales.SelectionChanged += (_, _) => ActualizarSeleccionMaterial();
            ResumeLayout(true);
            ActualizarSeleccionMaterial();
        }

        /// <summary>
        /// Tabla con el aspecto del dashboard (DisenoGestion.PulirTabla): el código en semibold verde, la categoría en una pastilla
        /// con el color y el icono de su caneca (EstiloCategoria) y el estado en pastilla. Solo cambia el dibujo.
        /// </summary>
        private void PulirTablaMateriales()
        {
            DisenoGestion.PulirTabla(tablaMateriales, columna => columna switch
            {
                "Codigo" => 170, "Nombre" => 220, "Categoria" => 240, "Estado" => 150, "Descripcion" => 300, _ => 0,
            }, "Busca por código, nombre o categoría, o pulsa «Actualizar lista» para ver el catálogo.", IconChar.LayerGroup);
            tablaMateriales.CellPainting += PintarCeldaMaterial;
        }

        private void PintarCeldaMaterial(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is not string valor) return;
            string columna = tablaMateriales.Columns[e.ColumnIndex].Name;
            if (columna is not ("Codigo" or "Categoria" or "Estado")) return;
            Graphics g = e.Graphics;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            e.PaintBackground(e.CellBounds, elegida);
            if (columna == "Codigo")
            {
                var zona = Rectangle.FromLTRB(e.CellBounds.Left + TemaEcoAccess.Px(tablaMateriales, 10), e.CellBounds.Top, e.CellBounds.Right - TemaEcoAccess.Px(tablaMateriales, 4), e.CellBounds.Bottom);
                TextRenderer.DrawText(g, valor, DibujoDashboard.Semibold(tablaMateriales, 14f), zona, elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.VerdeOscuro,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else if (columna == "Categoria")
            {
                // El texto de la fila es el de TextosCategoria; de ahí sale la categoría y su estilo (color e icono de la caneca).
                OpcionCategoria? opcion = TextosCategoria.Opciones.FirstOrDefault(o => o.Texto == valor);
                if (opcion is null)
                    TextRenderer.DrawText(g, valor, e.CellStyle?.Font ?? tablaMateriales.Font, e.CellBounds, TemaEcoAccess.Texto, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                else
                {
                    var (tono, fondo, icono, _) = EstiloCategoria.Para(opcion.Valor);
                    DisenoGestion.Pastilla(g, e.CellBounds, valor, tono, fondo, TemaEcoAccess.Borde, icono, tono, tablaMateriales);
                }
            }
            else
                DisenoGestion.PastillaEstado(g, e.CellBounds, valor, valor == "Habilitado", tablaMateriales);
            e.Handled = true;
        }

        private TableLayoutPanel CrearPestanaMaterial(string titulo, Button? accionFija = null)
        {
            var pagina = new TabPage(titulo) { BackColor = Color.White, Padding = Padding.Empty };
            var estructura = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = accionFija is null ? 1 : 2 };
            estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var desplazable = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var columna = DisenoFormularios.Columna();
            columna.Padding = new Padding(0, 4, 6, 4);
            columna.SizeChanged += (_, _) =>
            {
                foreach (Label label in columna.Controls.OfType<Label>())
                    label.MaximumSize = new Size(Math.Max(100, columna.ClientSize.Width - columna.Padding.Horizontal), 0);
            };
            desplazable.Controls.Add(columna);
            estructura.Controls.Add(desplazable, 0, 0);
            if (accionFija is not null)
            {
                estructura.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
                ModuloEco.Boton(accionFija, ModuloEco.IconoBoton(accionFija), ancho: true);
                accionFija.Dock = DockStyle.Fill;
                accionFija.MinimumSize = new Size(0, ModuloEco.AltoBoton);
                accionFija.Height = ModuloEco.AltoBoton;
                accionFija.Margin = new Padding(0, 8, 6, 4);
                accionFija.TabIndex = 1;
                estructura.Controls.Add(accionFija, 0, 1);
            }
            pagina.Controls.Add(estructura);
            _pestanasMaterial.TabPages.Add(pagina);
            return columna;
        }

        private void ActualizarSeleccionMaterial()
        {
            var fila = tablaMateriales.CurrentRow?.DataBoundItem as FilaMaterial;
            _detalleMaterial.Text = fila is null ? "Selecciona un material de la tabla."
                : $"{fila.Codigo} · {fila.Nombre}\n{fila.Categoria}\n{fila.Estado}\n\n{fila.Descripcion ?? "Sin descripción adicional."}";
            bool seleccionado = !_ocupado && _lecturaAutorizadaMaterial && fila is not null;
            botonHabilitar.Enabled = botonDeshabilitar.Enabled = seleccionado;
            botonRegistrar.Enabled = !_ocupado && _lecturaAutorizadaMaterial;
            _anteriorMaterial.Enabled = !_ocupado && _numeroPaginaMaterial > 1;
            _siguienteMaterial.Enabled = !_ocupado && _haySiguienteMaterial;
        }

        private FiltroMateriales CapturarFiltroMaterial() => new(_textoAplicadoMaterial,
            _soloHabilitadosAplicado ? true : null, _categoriaAplicadaMaterial, _numeroPaginaMaterial);

        private void LimpiarConsultaMaterial()
        {
            _lecturaAutorizadaMaterial = false;
            _haySiguienteMaterial = false;
            tablaMateriales.DataSource = null;
            tablaMateriales.ClearSelection();
            _paginaMaterial.Text = "Sin consulta autorizada disponible";
            ActualizarSeleccionMaterial();
        }

        private void ErrorCampoMaterial(Control control, string mensaje)
        {
            _erroresMaterial.SetError(control, mensaje);
            _focoMaterial = control;
            MostrarEstado(mensaje, true);
        }
    }
}
