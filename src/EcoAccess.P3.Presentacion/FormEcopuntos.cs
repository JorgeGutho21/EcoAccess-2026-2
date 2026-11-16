using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Administra reglas y movimientos mediante Logica, sin SQL ni decisiones de autorización.</summary>
    public sealed class FormEcopuntos : Form
    {
        private readonly ServicioEcopuntos _servicio;
        private readonly ServicioMateriales _materiales;
        private readonly int _actor;
        private readonly bool _vistaCiudadana;
        private readonly NumericUpDown _ciudadano = Numero();
        private readonly NumericUpDown _deposito = Numero();
        private readonly NumericUpDown _credito = Numero();
        private readonly NumericUpDown _puntos = new() { Minimum = 1, Maximum = 1_000_000, Width = 120 };
        private readonly CheckBox _porUnidad = new() { Text = "Por unidad verificada", AutoSize = true };
        private readonly ComboBox _regla = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 400 };
        private readonly ComboBox _material = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
        private readonly TextBox _motivo = new() { MaxLength = 300, Width = 420, AccessibleName = "Motivo de corrección o anulación" };
        private readonly TextBox _descripcion = new() { MaxLength = 200, Width = 300, AccessibleName = "Descripción de la nueva regla" };
        private readonly Label _estado = new() { AutoSize = true, MaximumSize = new Size(1100, 0), Text = "Consulta el saldo o selecciona una operación." };
        private readonly Label _saldo = new() { AutoSize = true, Text = "—", Margin = new Padding(0, 4, 0, 0) };
        private readonly Label _totales = new() { AutoSize = true, Text = "Pendiente de consulta", Margin = new Padding(0, 0, 0, 6) };
        private readonly EstadoVacioEco _vacio = new();
        private readonly DataGridView _historial = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            AccessibleName = "Historial auditable de ecopuntos",
        };
        private bool _ocupado;
        private string? _firma;
        private string? _clave;

        public FormEcopuntos(ServicioEcopuntos servicio, ServicioMateriales materiales, int actor, bool mostrarAdministracion,
            bool vistaCiudadana = false, ServicioEstadisticas? estadisticas = null)
        {
            _servicio = servicio; _materiales = materiales; _actor = actor;
            _vistaCiudadana = vistaCiudadana;
            Text = vistaCiudadana ? "Mis ecopuntos e historial" : "Ecopuntos — saldos, movimientos y reglas";
            StartPosition = FormStartPosition.CenterParent; Size = new Size(1180, 760); // Alto mínimo suficiente para todas las tarjetas: en pantallas bajas el contenedor se desplaza y nada se solapa.
            MinimumSize = new Size(720, 400);
            // Las tarjetas se apilan con su altura natural y el formulario se desplaza: nada se solapa en pantallas bajas.
            AutoScroll = true;
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = TemaEcoAccess.Fondo;
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 5, Padding = new Padding(12) };
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Tarjeta de saldo: Eco, el saldo destacado y la consulta del ciudadano.
            var tarjetaSaldo = new TarjetaEco { Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 8) };
            var cuerpoSaldo = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, BackColor = TemaEcoAccess.Superficie };
            cuerpoSaldo.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cuerpoSaldo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cuerpoSaldo.Controls.Add(new ImagenRedondeada { Imagen = RecursosEco.Eco, Size = new Size(96, 96), Margin = new Padding(0, 0, 18, 0), AccessibleName = "Eco", AccessibleRole = AccessibleRole.Graphic }, 0, 0);
            var textosSaldo = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            _saldo.Font = TemaEcoAccess.Valor; _saldo.ForeColor = TemaEcoAccess.VerdeOscuro; _saldo.Text = "—";
            _totales.ForeColor = TemaEcoAccess.Suave;
            var consulta = Fila();
            consulta.Dock = DockStyle.None;
            consulta.BackColor = TemaEcoAccess.Superficie;
            Agregar(consulta, "Ciudadano (ID)", _ciudadano);
            consulta.Controls.Add(Boton("Consultar saldo e historial", ConsultarAsync));
            textosSaldo.Controls.Add(new Label { Text = "Saldo de ecopuntos", AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = Padding.Empty });
            textosSaldo.Controls.Add(_saldo);
            textosSaldo.Controls.Add(_totales);
            textosSaldo.Controls.Add(consulta);
            consulta.Visible = !vistaCiudadana;
            if (vistaCiudadana) textosSaldo.Controls.Add(Boton("Actualizar mi saldo", ConsultarAsync));
            if (vistaCiudadana && estadisticas is not null)
                textosSaldo.Controls.Add(Boton("Mi actividad diaria y semanal", () =>
                {
                    using var actividad = new FormEstadisticasPersonales(estadisticas, actor);
                    actividad.ShowDialog(this);
                    return Task.CompletedTask;
                }));
            cuerpoSaldo.Controls.Add(textosSaldo, 1, 0);
            tarjetaSaldo.Controls.Add(cuerpoSaldo);
            // La altura sigue al contenido real (con escalado por fuente el ajuste automático del panel dejaba espacio vacío).
            cuerpoSaldo.SizeChanged += (_, _) => tarjetaSaldo.Height = cuerpoSaldo.Height + tarjetaSaldo.Padding.Vertical;
            layout.Controls.Add(tarjetaSaldo, 0, 0);

            // Historial con estado vacío explicativo.
            var tarjetaHistorial = new TarjetaEco { Dock = DockStyle.Top, Height = 340, Margin = new Padding(0, 0, 0, 8) };
            var cuerpoHistorial = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Superficie };
            cuerpoHistorial.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cuerpoHistorial.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cuerpoHistorial.Controls.Add(new Label
            {
                AutoSize = true,
                Font = TemaEcoAccess.Subtitulo,
                ForeColor = TemaEcoAccess.Texto,
                Margin = new Padding(0, 0, 0, 8),
                Text = "Historial de movimientos · hora de Colombia"
            }, 0, 0);
            var area = new Panel { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Superficie };
            TemaEcoAccess.EstilizarTabla(_historial);
            area.Controls.Add(_historial);
            area.Controls.Add(_vacio);
            _vacio.Dock = DockStyle.Fill;
            _vacio.Mostrar("Consulta un ciudadano para ver su saldo e historial. Los ecopuntos se acreditan solo después de verificar un depósito.", false);
            _historial.Visible = false;
            cuerpoHistorial.Controls.Add(area, 0, 1);
            tarjetaHistorial.Controls.Add(cuerpoHistorial);
            layout.Controls.Add(tarjetaHistorial, 0, 1);

            var operacion = Fila();
            Agregar(operacion, "Depósito (ID)", _deposito); Agregar(operacion, "Regla", _regla);
            operacion.Controls.Add(Boton("Acreditar depósito", () => OperarAsync(TipoOperacionEcopuntos.Acreditar)));
            Agregar(operacion, "Crédito activo (ID)", _credito); Agregar(operacion, "Motivo", _motivo);
            var anular = Boton("Anular crédito", () => OperarAsync(TipoOperacionEcopuntos.Revertir)); anular.Visible = mostrarAdministracion;
            var corregir = Boton("Corregir con nueva regla", () => OperarAsync(TipoOperacionEcopuntos.Corregir)); corregir.Visible = mostrarAdministracion;
            operacion.Controls.Add(anular); operacion.Controls.Add(corregir);
            var seccionOperacion = Seccion("Acreditar, anular o corregir", operacion);
            seccionOperacion.Visible = !vistaCiudadana;
            layout.Controls.Add(seccionOperacion, 0, 2);
            var reglas = Fila();
            Agregar(reglas, "Material", _material); Agregar(reglas, "Tarifa en puntos", _puntos); reglas.Controls.Add(_porUnidad);
            Agregar(reglas, "Descripción de nueva versión", _descripcion);
            reglas.Controls.Add(Boton("Crear regla desde ahora", CrearReglaAsync));
            var seccionReglas = Seccion("Reglas de puntos por material", reglas); seccionReglas.Visible = mostrarAdministracion;
            layout.Controls.Add(seccionReglas, 0, 3);
            layout.Controls.Add(_estado, 0, 4); Controls.Add(layout); Load += async (_, _) => await EjecutarAsync(CargarAsync);
            DisenoFormularios.AjustarEtiquetasAlAncho(layout, _estado);
            FormClosing += (_, e) => e.Cancel = _ocupado;
        }

        private async Task CargarAsync()
        {
            if (_vistaCiudadana) { await ConsultarAsync(); return; }
            var reglas = await _servicio.ListarReglasAsync(_actor);
            if (!reglas.Exitoso) { _estado.Text = reglas.Mensaje; return; }
            _regla.DisplayMember = "Texto"; _regla.ValueMember = "Id";
            _regla.DataSource = reglas.Valor.Select(r => new { r.Id, Texto = $"Versión {r.Id} · material {r.IdMaterial} · {r.Puntos} puntos por {(r.PorUnidad ? "unidad verificada" : "depósito")} · {r.Descripcion}" }).ToList();
            _material.DisplayMember = nameof(Material.Nombre); _material.ValueMember = nameof(Material.Id);
            _material.DataSource = (await _materiales.ListarAsync(true)).ToList();
            if (reglas.Valor.Count == 0) _estado.Text = "No hay reglas configuradas. No se acreditarán puntos hasta crear una regla aprobada.";
        }

        private async Task ConsultarAsync()
        {
            var r = _vistaCiudadana ? await _servicio.ConsultarMiosAsync(_actor) : await _servicio.ConsultarAsync(_actor, (int)_ciudadano.Value);
            if (!r.Exitoso) { _estado.Text = r.Mensaje; return; }
            _saldo.Text = $"{r.Valor.Saldo:N0} ecopuntos";
            _totales.Text = $"Acreditados: {r.Valor.TotalAcreditado:N0} · revertidos: {r.Valor.TotalRevertido:N0}";
            _historial.Visible = r.Valor.Movimientos.Count > 0;
            _vacio.Visible = !_historial.Visible;
            _vacio.Mostrar("Este ciudadano todavía no tiene ecopuntos. Se acreditan cuando un operario verifica uno de sus depósitos.", false);
            _historial.DataSource = r.Valor.Movimientos.Select(m => new
            {
                CreditoOMovimiento = m.Id,
                Deposito = m.IdDeposito,
                Regla = m.IdRegla,
                m.Tipo,
                m.Puntos,
                Fecha = HoraColombia.DesdeUtc(m.FechaUtc),
                Actor = m.IdActor,
                m.Motivo,
            }).ToList();
            if (_vistaCiudadana)
                foreach (string nombre in new[] { "CreditoOMovimiento", "Deposito", "Regla", "Actor" })
                    if (_historial.Columns[nombre] is { } columna) columna.Visible = false;
            _estado.Text = "Saldo de todo el libro; se muestran los últimos 50 movimientos.";
        }

        private async Task OperarAsync(TipoOperacionEcopuntos tipo)
        {
            int regla = _regla.SelectedValue is int id ? id : 0;
            string firma = tipo switch
            {
                TipoOperacionEcopuntos.Acreditar => $"{tipo}|{_deposito.Value}|{regla}",
                TipoOperacionEcopuntos.Revertir => $"{tipo}|{_credito.Value}|{_motivo.Text.Trim()}",
                _ => $"{tipo}|{_credito.Value}|{regla}|{_motivo.Text.Trim()}",
            };
            if (firma != _firma) { _firma = firma; _clave = "gui-" + Guid.NewGuid().ToString("N"); }
            Resultado<IReadOnlyList<MovimientoEcopuntos>> r = tipo switch
            {
                TipoOperacionEcopuntos.Acreditar => await _servicio.AcreditarAsync(_actor, (int)_deposito.Value, regla, _clave!),
                TipoOperacionEcopuntos.Revertir => await _servicio.RevertirAsync(_actor, (int)_credito.Value, _clave!, _motivo.Text),
                _ => await _servicio.CorregirAsync(_actor, (int)_credito.Value, regla, _clave!, _motivo.Text),
            };
            _estado.Text = r.Exitoso ? "Operación registrada. Consulta el saldo del ciudadano para ver el historial." : r.Mensaje;
        }

        private async Task CrearReglaAsync()
        {
            int material = _material.SelectedValue is int id ? id : 0;
            var r = await _servicio.RegistrarReglaAsync(_actor, material, (int)_puntos.Value, _descripcion.Text, DateTime.UtcNow, porUnidad: _porUnidad.Checked);
            if (r.Exitoso) { await CargarAsync(); _estado.Text = $"Se creó la versión {r.Valor}. Los movimientos anteriores conservan su tarifa."; }
            else _estado.Text = r.Mensaje;
        }

        private async Task EjecutarAsync(Func<Task> accion)
        {
            if (_ocupado) return;
            _ocupado = true; UseWaitCursor = true;
            try { await accion(); }
            catch (Exception) { _estado.Text = "No se pudo confirmar el resultado. Comprueba la conexión y consulta el historial antes de repetir la operación."; }
            finally { UseWaitCursor = false; _ocupado = false; }
        }
        private Button Boton(string texto, Func<Task> accion)
        {
            var boton = new Button { AutoSize = true, Text = texto, AccessibleName = texto, Margin = new Padding(6) };
            boton.Click += async (_, _) => await EjecutarAsync(accion); return boton;
        }
        /// <summary>Grupo de acciones en una tarjeta con título.</summary>
        private static TarjetaEco Seccion(string titulo, Control contenido)
        {
            var tarjeta = new TarjetaEco { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 8), Padding = new Padding(18 + TarjetaEco.Sombra, 12 + TarjetaEco.Sombra, 18 + TarjetaEco.Sombra, 12 + TarjetaEco.Sombra) };
            var cuerpo = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie };
            cuerpo.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(6, 0, 0, 0) });
            contenido.BackColor = TemaEcoAccess.Superficie;
            // Una fila que se ajusta necesita su ancho máximo para calcular cuántas líneas ocupa; si no, se recorta.
            contenido.Dock = DockStyle.None;
            cuerpo.SizeChanged += (_, _) => contenido.MaximumSize = contenido.MinimumSize = new Size(Math.Max(200, cuerpo.ClientSize.Width - 6), 0);
            cuerpo.Controls.Add(contenido);
            tarjeta.Controls.Add(cuerpo);
            return tarjeta;
        }

        private static FlowLayoutPanel Fila() => new() { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(0, 6, 0, 6) };
        private static NumericUpDown Numero() => new() { Minimum = 1, Maximum = int.MaxValue, Width = 105 };
        private static void Agregar(FlowLayoutPanel panel, string texto, Control control)
        {
            panel.Controls.Add(new Label { Text = texto, AutoSize = true, Margin = new Padding(6, 8, 6, 4) });
            control.AccessibleName = texto; panel.Controls.Add(control);
        }
    }
}
