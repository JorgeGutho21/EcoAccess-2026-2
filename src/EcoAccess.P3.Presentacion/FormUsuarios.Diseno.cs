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
        private readonly TabControl _edicionUsuarios = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Acciones administrativas de cuentas" };
        private readonly ErrorProvider _erroresUsuarios = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        private int _paginaActual = 1;

        // Misma regla que PanelDesplazable: al cambiar de pestaña o de tamaño, el módulo no salta al control con foco.
        protected override Point ScrollToControl(Control activeControl) =>
            EstabilizadorVentana.NavegandoConTeclado() ? base.ScrollToControl(activeControl) : DisplayRectangle.Location;
        private bool _haySiguiente;
        private bool _datosAutorizados;
        private CancellationTokenSource? _consultaUsuarios;
        private Control? _focoPendiente;
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

            _filtroEstado.Items.AddRange(new object[] { "Todos los estados", "Activos", "Inactivos" });
            _filtroEstado.SelectedIndex = 0;
            _filtroRol.Items.AddRange(new object[] { "Todos los roles", "Ciudadano", "Operario", "Supervisor (Administrador del sistema)" });
            _filtroRol.SelectedIndex = 0;
            // Filtros como campos Eco (etiqueta arriba, caja de 46 px con icono). Siguen siendo hijos de _filtros: el formulario
            // los deshabilita juntos mientras consulta.
            foreach (Control campo in new Control[]
                     {
                         ModuloEco.Campo("Nombre o correo", IconChar.MagnifyingGlass, _busqueda, ancho: 240),
                         ModuloEco.Campo("Estado", IconChar.ToggleOn, _filtroEstado, ancho: 190),
                         ModuloEco.Campo("Rol", IconChar.UserTag, _filtroRol, ancho: 195),
                         ModuloEco.JuntoACampos(ModuloEco.Boton(_buscar, IconChar.MagnifyingGlass)),
                         ModuloEco.JuntoACampos(ModuloEco.Boton(botonActualizar, IconChar.ArrowsRotate)),
                         ModuloEco.JuntoACampos(ModuloEco.Boton(_cancelar, IconChar.Xmark)),
                     })
            {
                campo.TabIndex = _filtros.Controls.Count;
                _filtros.Controls.Add(campo);
            }
            _filtros.Dock = DockStyle.Fill;
            _filtros.BackColor = TemaEcoAccess.Superficie;
            _cancelar.Visible = false;
            _paginacion.Controls.AddRange(new Control[] { _anterior, _siguiente, _paginaTexto });
            _paginacion.BackColor = TemaEcoAccess.Superficie;
            _paginaTexto.Margin = new Padding(8, 12, 0, 0);

            // Gestión de la cuenta como en el diseño aprobado: píldoras con icono para elegir la página, aviso verde con icono de
            // información, cajas con texto guía y botones suaves. Mismos controles y eventos del Designer.
            var registro = CrearPestana("Crear cuenta");
            DisenoFormularios.Agregar(registro, new AvisoEco(DisenoFormularios.Texto("El registro público crea solo ciudadanos. Los roles de personal (Operario y Supervisor) los asigna aquí un supervisor.")));
            DisenoFormularios.Campo(registro, etiquetaNombre, textoNombre, IconChar.User);
            DisenoFormularios.Campo(registro, etiquetaCorreo, textoCorreo, IconChar.Envelope);
            DisenoFormularios.Campo(registro, etiquetaRolInicial, comboRolInicial, IconChar.UserTag);
            DisenoFormularios.Agregar(registro, ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));
            var cuenta = CrearPestana("Cuenta y roles");
            DisenoFormularios.Agregar(cuenta, DisenoFormularios.Texto("Cuenta seleccionada", true));
            DisenoFormularios.Agregar(cuenta, DisenoGestion.Ficha(_seleccion, IconChar.UserCheck, "Cuenta"));
            DisenoFormularios.Agregar(cuenta, DisenoGestion.BotonSuave(botonActivar, IconChar.CircleCheck));
            DisenoFormularios.Agregar(cuenta, DisenoGestion.BotonSuave(botonDesactivar, IconChar.CircleXmark));
            DisenoFormularios.Agregar(cuenta, DisenoFormularios.Texto("Roles de la cuenta", true));
            DisenoFormularios.Campo(cuenta, new Label { Text = "Rol" }, comboRol, IconChar.UserTag);
            comboRol.AccessibleName = "Rol de la cuenta seleccionada";
            DisenoFormularios.Agregar(cuenta, ModuloEco.Rejilla(DisenoGestion.BotonSuave(botonAsignarRol, IconChar.UserPlus), DisenoGestion.BotonSuave(botonQuitarRol, IconChar.UserMinus)));
            var acceso = CrearPestana("Acceso inicial");
            DisenoFormularios.Agregar(acceso, new AvisoEco(_seleccionAcceso, "Solo para personal sin contraseña"));
            DisenoFormularios.Agregar(acceso, DisenoFormularios.Texto("Selecciona la cuenta en la tabla. Esta acción no reemplaza una contraseña ya establecida."));
            DisenoFormularios.Campo(acceso, etiquetaContrasena, textoContrasena, IconChar.Lock);
            DisenoFormularios.Campo(acceso, etiquetaConfirmacion, textoConfirmacion, IconChar.Lock);
            DisenoFormularios.Agregar(acceso, ModuloEco.Boton(botonEstablecerContrasena, IconChar.Lock, ancho: true));
            DisenoGestion.TextoGuia(textoNombre, "Ingresa el nombre completo");
            DisenoGestion.TextoGuia(textoCorreo, "Ingresa el correo electrónico");
            DisenoGestion.TextoGuia(textoContrasena, "Ingresa la contraseña");
            DisenoGestion.TextoGuia(textoConfirmacion, "Confirma la contraseña");
            panelAcciones.Controls.Clear();
            grupoRegistro.Dispose();
            grupoRoles.Dispose();
            grupoContrasena.Dispose();
            panelAcciones.Dock = DockStyle.Fill;
            panelAcciones.AutoScroll = false;
            panelAcciones.BackColor = TemaEcoAccess.Superficie;
            var gestion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            gestion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gestion.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            gestion.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            gestion.Controls.Add(new SelectorPestanas(_edicionUsuarios, IconChar.UserPlus, IconChar.Users, IconChar.Lock) { Dock = DockStyle.Fill }, 0, 0);
            gestion.Controls.Add(_edicionUsuarios, 0, 1);
            panelAcciones.Controls.Add(gestion);
            // Encabezado con la estética del banner del dashboard y la información actual del módulo (la página consultada).
            var banner = new BannerModulo("Personas y permisos", "Administra cuentas, roles y accesos del sistema de forma segura. Los permisos se verifican en cada operación.", IconChar.UsersGear);
            banner.Estado = _paginaTexto.Text;
            _paginaTexto.TextChanged += (_, _) => banner.Estado = _paginaTexto.Text;
            Controls.Add(new VistaGestionCatalogo("Personas y permisos", "Administración de cuentas · los permisos se verifican en cada operación",
                IconChar.Users, _filtros, tablaUsuarios, _paginacion, panelAcciones, etiquetaEstado,
                tituloLista: "Cuentas", tituloEditor: "Gestión de la cuenta", tituloBusqueda: "Buscar cuentas", anchoLateral: 500,
                banner: banner, iconoBusqueda: IconChar.MagnifyingGlass, iconoLista: IconChar.Users, iconoEditor: IconChar.Gear));
            PulirTablaUsuarios();
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
            ResumeLayout(true);
            ActualizarSeleccionUsuarios();
        }

        /// <summary>
        /// Tabla con el aspecto del dashboard (DisenoGestion.PulirTabla): la persona con sus iniciales en un círculo, el estado
        /// en pastilla y el consentimiento con un visto. Solo cambia el dibujo; datos y eventos son los mismos.
        /// </summary>
        private void PulirTablaUsuarios()
        {
            DisenoGestion.PulirTabla(tablaUsuarios, columna => columna switch
            {
                "Nombre" => 230, "Correo" => 260, "Roles" => 200, "Estado" => 125, "Consentimiento" => 150, "Registro" => 190, _ => 0,
            }, "Busca por nombre, estado o rol, o pulsa «Actualizar lista» para ver las cuentas.", IconChar.UsersViewfinder);
            tablaUsuarios.CellPainting += PintarCeldaUsuario;
            // El estado se lee junto al nombre (sin desplazar la tabla); solo cambia el orden en pantalla, no los datos.
            tablaUsuarios.DataBindingComplete += (_, _) =>
            {
                if (tablaUsuarios.Columns["Estado"] is { } estado && tablaUsuarios.Columns["Nombre"] is { } nombre && estado.DisplayIndex != nombre.DisplayIndex + 1)
                    estado.DisplayIndex = nombre.DisplayIndex + 1;
            };
        }

        private void PintarCeldaUsuario(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is not string valor) return;
            string columna = tablaUsuarios.Columns[e.ColumnIndex].Name;
            if (columna is not ("Nombre" or "Estado" or "Consentimiento")) return;
            Graphics g = e.Graphics;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            e.PaintBackground(e.CellBounds, elegida);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int px(int n) => TemaEcoAccess.Px(tablaUsuarios, n);
            if (columna == "Consentimiento")
            {
                // «Sí (versión)» con un visto verde delante; «No» en gris.
                bool si = valor.StartsWith("Sí", StringComparison.Ordinal);
                int x = e.CellBounds.Left + px(8);
                if (si)
                {
                    Bitmap visto = CacheIconos.Obtener(IconChar.Check, px(14), TemaEcoAccess.Verde);
                    g.DrawImage(visto, x, e.CellBounds.Top + (e.CellBounds.Height - visto.Height) / 2);
                    x += visto.Width + px(6);
                }
                TextRenderer.DrawText(g, valor, e.CellStyle?.Font ?? tablaUsuarios.Font, Rectangle.FromLTRB(x, e.CellBounds.Top, e.CellBounds.Right - px(4), e.CellBounds.Bottom),
                    elegida ? e.CellStyle!.SelectionForeColor : si ? TemaEcoAccess.Texto : TemaEcoAccess.Suave,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else if (columna == "Nombre")
            {
                int lado = px(30);
                var circulo = new Rectangle(e.CellBounds.Left + px(8), e.CellBounds.Top + (e.CellBounds.Height - lado) / 2, lado, lado);
                using (var fondo = new SolidBrush(elegida ? TemaEcoAccess.Superficie : TemaEcoAccess.VerdeSuave)) g.FillEllipse(fondo, circulo);
                string iniciales = string.Concat(valor.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
                Font fuente = DibujoDashboard.Semibold(tablaUsuarios, 11.5f);
                TextRenderer.DrawText(g, iniciales, fuente, circulo, TemaEcoAccess.VerdeOscuro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                var texto = Rectangle.FromLTRB(circulo.Right + px(10), e.CellBounds.Top, e.CellBounds.Right - px(6), e.CellBounds.Bottom);
                TextRenderer.DrawText(g, valor, e.CellStyle?.Font ?? tablaUsuarios.Font, texto, elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Texto,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
                DisenoGestion.PastillaEstado(g, e.CellBounds, valor, valor == "Activo", tablaUsuarios);
            e.Handled = true;
        }

        private TableLayoutPanel CrearPestana(string titulo)
        {
            var pagina = new TabPage(titulo) { BackColor = Color.White, AutoScroll = true, Padding = Padding.Empty };
            var columna = DisenoFormularios.Columna();
            columna.Padding = new Padding(0, 4, 6, 4);
            columna.SizeChanged += (_, _) =>
            {
                foreach (Label label in columna.Controls.OfType<Label>())
                    label.MaximumSize = new Size(Math.Max(100, columna.ClientSize.Width - columna.Padding.Horizontal), 0);
            };
            pagina.Controls.Add(columna);
            _edicionUsuarios.TabPages.Add(pagina);
            return columna;
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
