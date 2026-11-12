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

    public partial class FormUsuarios
    {
        private readonly CajaTexto _busqueda = new() { Width = 250, MaxLength = 254, PlaceholderText = "Nombre o correo electrónico", AccessibleName = "Buscar por nombre o correo" };
        private readonly ComboBox _filtroEstado = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 135, AccessibleName = "Filtrar por estado" };
        private readonly ComboBox _filtroRol = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, AccessibleName = "Filtrar por rol" };
        private readonly Button _buscar = DisenoFormularios.Accion("Buscar");
        private readonly Button _anterior = DisenoFormularios.Accion("Anterior");
        private readonly Button _siguiente = DisenoFormularios.Accion("Siguiente");
        private readonly Button _cancelar = DisenoFormularios.Accion("Cancelar consulta");
        private readonly Label _paginaTexto = DisenoFormularios.Texto("Página sin consultar");
        private readonly Label _seleccion = DisenoFormularios.Texto("Selecciona una cuenta de la tabla.");
        private readonly Label _seleccionAcceso = DisenoFormularios.Texto("Selecciona una cuenta de la tabla.");
        private readonly FlowLayoutPanel _filtros = new() { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        private readonly FlowLayoutPanel _paginacion = new() { AutoSize = true, Dock = DockStyle.Fill };
        private readonly TableLayoutPanel _cuerpoUsuarios = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        private readonly TabControl _edicionUsuarios = new() { Dock = DockStyle.Fill, AccessibleName = "Acciones administrativas de cuentas" };
        private readonly ErrorProvider _erroresUsuarios = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        private int _paginaActual = 1;
        private bool _haySiguiente;
        private bool _datosAutorizados;
        private CancellationTokenSource? _consultaUsuarios;
        private Control? _focoPendiente;
        private bool? _apilarUsuarios;
        private string _textoAplicado = "";
        private bool? _estadoAplicado;
        private CodigoRol? _rolAplicado;

        private void ConstruirDisenoUsuarios()
        {
            SuspendLayout();
            Controls.Clear();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = TemaEcoAccess.Cuerpo;
            MinimumSize = new Size(560, 440);
            _erroresUsuarios.ContainerControl = this;
            Disposed += (_, _) => { _consultaUsuarios?.Cancel(); _erroresUsuarios.Dispose(); };

            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(20), BackColor = TemaEcoAccess.Fondo };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var cabecera = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            cabecera.Controls.Add(new IconoEcoAccess
            {
                IconChar = IconChar.Users,
                IconColor = TemaEcoAccess.Verde,
                Size = new Size(38, 38),
                IconSize = 28,
                TabStop = false,
                Margin = new Padding(0, 4, 12, 0)
            });
            var textos = DisenoFormularios.Columna();
            textos.BackColor = TemaEcoAccess.Fondo;
            textos.Padding = Padding.Empty;
            DisenoFormularios.Agregar(textos, new Label { Text = "Personas y permisos", AutoSize = true, Font = TemaEcoAccess.Titulo, ForeColor = TemaEcoAccess.Texto });
            DisenoFormularios.Agregar(textos, DisenoFormularios.Texto("Administración de cuentas · los permisos se verifican en cada operación"));
            cabecera.Controls.Add(textos);
            cabecera.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(160, cabecera.ClientSize.Width - TemaEcoAccess.Px(this, 60));
                textos.MaximumSize = new Size(ancho, 0);
                foreach (Label etiqueta in textos.Controls.OfType<Label>()) etiqueta.MaximumSize = new Size(ancho, 0);
            };
            raiz.Controls.Add(cabecera, 0, 0);

            _filtroEstado.Items.AddRange(new object[] { "Todos los estados", "Activos", "Inactivos" });
            _filtroEstado.SelectedIndex = 0;
            _filtroRol.Items.AddRange(new object[] { "Todos los roles", "Ciudadano", "Operario", "Supervisor (Administrador del sistema)" });
            _filtroRol.SelectedIndex = 0;
            foreach (Control campo in new Control[] { _busqueda, _filtroEstado, _filtroRol, _buscar, botonActualizar, _cancelar })
            {
                campo.Margin = new Padding(0, 0, 8, 12);
                campo.TabIndex = _filtros.Controls.Count;
                _filtros.Controls.Add(campo);
            }
            botonActualizar.AutoSize = true;
            botonActualizar.Size = new Size(130, 36);
            _cancelar.Visible = false;
            raiz.Controls.Add(_filtros, 0, 1);

            _cuerpoUsuarios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _cuerpoUsuarios.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
            _cuerpoUsuarios.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var listado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 14, 0) };
            listado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            listado.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listado.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            DisenoFormularios.PrepararTabla(tablaUsuarios);
            listado.Controls.Add(tablaUsuarios, 0, 0);
            _paginacion.Controls.AddRange(new Control[] { _anterior, _siguiente, _paginaTexto });
            _paginaTexto.Margin = new Padding(8, 10, 0, 0);
            listado.Controls.Add(_paginacion, 0, 1);
            _cuerpoUsuarios.Controls.Add(listado, 0, 0);

            var registro = CrearPestana("Crear cuenta");
            DisenoFormularios.Agregar(registro, DisenoFormularios.Texto("Alta administrativa", true));
            DisenoFormularios.Agregar(registro, DisenoFormularios.Texto("El registro público crea solo ciudadanos. Los roles de personal (Operario y Supervisor) los asigna aquí un supervisor."));
            DisenoFormularios.Campo(registro, etiquetaNombre, textoNombre);
            DisenoFormularios.Campo(registro, etiquetaCorreo, textoCorreo);
            DisenoFormularios.Campo(registro, etiquetaRolInicial, comboRolInicial);
            DisenoFormularios.Agregar(registro, botonRegistrar);
            var cuenta = CrearPestana("Cuenta y roles");
            DisenoFormularios.Agregar(cuenta, _seleccion);
            DisenoFormularios.Agregar(cuenta, botonActivar);
            DisenoFormularios.Agregar(cuenta, botonDesactivar);
            DisenoFormularios.Agregar(cuenta, DisenoFormularios.Texto("Roles de la cuenta seleccionada", true));
            DisenoFormularios.Agregar(cuenta, comboRol);
            comboRol.AccessibleName = "Rol de la cuenta seleccionada";
            DisenoFormularios.Agregar(cuenta, botonAsignarRol);
            DisenoFormularios.Agregar(cuenta, botonQuitarRol);
            var acceso = CrearPestana("Acceso inicial");
            DisenoFormularios.Agregar(acceso, DisenoFormularios.Texto("Solo para personal sin contraseña", true));
            DisenoFormularios.Agregar(acceso, _seleccionAcceso);
            DisenoFormularios.Agregar(acceso, DisenoFormularios.Texto("Selecciona la cuenta en la tabla. Esta acción no reemplaza una contraseña ya establecida."));
            DisenoFormularios.Campo(acceso, etiquetaContrasena, textoContrasena);
            DisenoFormularios.Campo(acceso, etiquetaConfirmacion, textoConfirmacion);
            DisenoFormularios.Agregar(acceso, botonEstablecerContrasena);
            panelAcciones.Controls.Clear();
            grupoRegistro.Dispose();
            grupoRoles.Dispose();
            grupoContrasena.Dispose();
            panelAcciones.Dock = DockStyle.Fill;
            panelAcciones.AutoScroll = false;
            panelAcciones.Controls.Add(_edicionUsuarios);
            _cuerpoUsuarios.Controls.Add(panelAcciones, 1, 0);
            raiz.Controls.Add(_cuerpoUsuarios, 0, 2);
            etiquetaEstado.Dock = DockStyle.Fill;
            etiquetaEstado.AutoSize = true;
            etiquetaEstado.MinimumSize = new Size(0, 42);
            etiquetaEstado.Padding = new Padding(0, 12, 0, 0);
            raiz.Controls.Add(etiquetaEstado, 0, 3);
            Controls.Add(raiz);
            // En pantallas bajas (1366×768) la cabecera y los filtros dejaban la tabla en una fila y media: el módulo se
            // desplaza y la tabla conserva un alto útil. En 1920×1080 cabe completo y no aparece la barra.
            AutoScroll = true;
            AutoScrollMinSize = new Size(0, TemaEcoAccess.Px(this, 720));
            TemaEcoAccess.Aplicar(this);
            _buscar.Click += async (_, _) =>
            {
                if (_ocupado) return;
                _paginaActual = 1;
                _textoAplicado = _busqueda.Text;
                _estadoAplicado = _filtroEstado.SelectedIndex == 0 ? null : _filtroEstado.SelectedIndex == 1;
                _rolAplicado = _filtroRol.SelectedIndex == 0 ? null : (CodigoRol)_filtroRol.SelectedIndex;
                await EjecutarAsync(CargarAsync, lectura: true);
            };
            _anterior.Click += async (_, _) => { if (_ocupado || _paginaActual <= 1) return; _paginaActual--; await EjecutarAsync(CargarAsync, lectura: true); };
            _siguiente.Click += async (_, _) => { if (_ocupado || !_haySiguiente) return; _paginaActual++; await EjecutarAsync(CargarAsync, lectura: true); };
            _cancelar.Click += (_, _) => _consultaUsuarios?.Cancel();
            _busqueda.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _buscar.PerformClick(); } };
            tablaUsuarios.SelectionChanged += (_, _) => ActualizarSeleccionUsuarios();
            SizeChanged += (_, _) => AdaptarUsuarios();
            _cuerpoUsuarios.SizeChanged += (_, _) => AdaptarUsuarios();
            ResumeLayout(true);
            AdaptarUsuarios();
            ActualizarSeleccionUsuarios();
        }

        private TableLayoutPanel CrearPestana(string titulo)
        {
            var pagina = new TabPage(titulo) { BackColor = Color.White, AutoScroll = true, Padding = new Padding(4) };
            var columna = DisenoFormularios.Columna();
            columna.SizeChanged += (_, _) =>
            {
                foreach (Label label in columna.Controls.OfType<Label>())
                    label.MaximumSize = new Size(Math.Max(100, columna.ClientSize.Width - columna.Padding.Horizontal), 0);
            };
            pagina.Controls.Add(columna);
            _edicionUsuarios.TabPages.Add(pagina);
            return columna;
        }

        private void AdaptarUsuarios()
        {
            bool apilar = _cuerpoUsuarios.ClientSize.Width < TemaEcoAccess.Px(this, 900);
            if (_apilarUsuarios == apilar) return;
            _apilarUsuarios = apilar;
            _cuerpoUsuarios.SuspendLayout();
            _cuerpoUsuarios.ColumnStyles[1].Width = apilar ? 0 : TemaEcoAccess.Px(this, 350);
            _cuerpoUsuarios.RowCount = apilar ? 2 : 1;
            _cuerpoUsuarios.RowStyles.Clear();
            _cuerpoUsuarios.RowStyles.Add(new RowStyle(SizeType.Percent, apilar ? 48 : 100));
            if (apilar) _cuerpoUsuarios.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            _cuerpoUsuarios.SetCellPosition(panelAcciones, new TableLayoutPanelCellPosition(apilar ? 0 : 1, apilar ? 1 : 0));
            _cuerpoUsuarios.ResumeLayout(true);
        }

        private void ActualizarSeleccionUsuarios()
        {
            var fila = tablaUsuarios.CurrentRow?.DataBoundItem as FilaUsuario;
            _seleccion.Text = fila is null ? "Selecciona una cuenta de la tabla." : $"{fila.Nombre}\n{fila.Correo}\n{fila.Estado} · {fila.Roles}";
            _seleccionAcceso.Text = _seleccion.Text;
            bool seleccionValida = !_ocupado && _datosAutorizados && fila is not null;
            botonActivar.Enabled = seleccionValida;
            botonDesactivar.Enabled = seleccionValida;
            botonAsignarRol.Enabled = botonQuitarRol.Enabled = botonEstablecerContrasena.Enabled = seleccionValida;
            botonRegistrar.Enabled = !_ocupado && _datosAutorizados;
            _anterior.Enabled = !_ocupado && _paginaActual > 1;
            _siguiente.Enabled = !_ocupado && _haySiguiente;
        }

        private FiltroUsuarios CapturarFiltroUsuarios() => new(_textoAplicado, _estadoAplicado, _rolAplicado, _paginaActual);

        private void LimpiarConsultaUsuarios()
        {
            _datosAutorizados = false;
            _haySiguiente = false;
            tablaUsuarios.DataSource = null;
            tablaUsuarios.ClearSelection();
            textoContrasena.Clear();
            textoConfirmacion.Clear();
            _paginaTexto.Text = "Sin consulta autorizada disponible";
            ActualizarSeleccionUsuarios();
        }
    }
}
