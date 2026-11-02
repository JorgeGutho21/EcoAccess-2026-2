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
        private readonly TabControl _pestanasMaterial = new() { Dock = DockStyle.Fill, AccessibleName = "Gestión del catálogo ambiental" };
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
            foreach (Control control in new Control[] { _busquedaMaterial, _filtroCategoria, checkSoloHabilitados, _buscarMaterial, botonActualizar, _cancelarMaterial })
            {
                control.TabIndex = _filtrosMaterial.Controls.Count;
                control.Margin = new Padding(0, 0, 8, 12);
                _filtrosMaterial.Controls.Add(control);
            }
            checkSoloHabilitados.AutoSize = true;
            botonActualizar.AutoSize = true;
            botonActualizar.Size = new Size(130, 36);
            _cancelarMaterial.Visible = false;
            _paginacionMaterial.Controls.AddRange(new Control[] { _anteriorMaterial, _siguienteMaterial, _paginaMaterial });
            _paginaMaterial.Margin = new Padding(8, 10, 0, 0);

            var registro = CrearPestanaMaterial("Crear material", botonRegistrar);
            DisenoFormularios.Agregar(registro, DisenoFormularios.Texto("Amplía el catálogo ambiental", true));
            DisenoFormularios.Agregar(registro, DisenoFormularios.Texto("La categoría orienta la separación. La admisión depende también de cada caneca."));
            DisenoFormularios.Campo(registro, etiquetaCodigo, textoCodigo);
            DisenoFormularios.Campo(registro, etiquetaNombre, textoNombre);
            DisenoFormularios.Campo(registro, etiquetaCategoria, comboCategoria);
            textoDescripcion.ScrollBars = ScrollBars.Vertical;
            textoDescripcion.Height = 86;
            DisenoFormularios.Campo(registro, etiquetaDescripcion, textoDescripcion);
            var estado = CrearPestanaMaterial("Estado y detalle");
            DisenoFormularios.Agregar(estado, _detalleMaterial);
            DisenoFormularios.Agregar(estado, botonHabilitar);
            DisenoFormularios.Agregar(estado, botonDeshabilitar);
            DisenoFormularios.Agregar(estado, DisenoFormularios.Texto("Deshabilitar conserva el historial y retira el material de nuevas operaciones. No se permite dejar una caneca en servicio sin materiales habilitados."));
            panelAcciones.Controls.Clear();
            grupoRegistro.Dispose();
            panelAcciones.Dock = DockStyle.Fill;
            panelAcciones.AutoScroll = false;
            panelAcciones.Controls.Add(_pestanasMaterial);
            Controls.Add(new VistaGestionCatalogo("Materiales con propósito", "Catálogo ambiental · administración con permisos verificados",
                IconChar.Leaf, _filtrosMaterial, tablaMateriales, _paginacionMaterial, panelAcciones, etiquetaEstado));

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

        private TableLayoutPanel CrearPestanaMaterial(string titulo, Button? accionFija = null)
        {
            var pagina = new TabPage(titulo) { BackColor = Color.White, Padding = new Padding(4) };
            var estructura = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = accionFija is null ? 1 : 2 };
            estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var desplazable = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var columna = DisenoFormularios.Columna();
            columna.SizeChanged += (_, _) =>
            {
                foreach (Label label in columna.Controls.OfType<Label>())
                    label.MaximumSize = new Size(Math.Max(100, columna.ClientSize.Width - columna.Padding.Horizontal), 0);
            };
            desplazable.Controls.Add(columna);
            estructura.Controls.Add(desplazable, 0, 0);
            if (accionFija is not null)
            {
                estructura.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
                accionFija.Dock = DockStyle.Fill;
                accionFija.AutoSize = false;
                accionFija.MinimumSize = new Size(0, 38);
                accionFija.Height = 38;
                accionFija.Margin = new Padding(12, 6, 12, 8);
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
