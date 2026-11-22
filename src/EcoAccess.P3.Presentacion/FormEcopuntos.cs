using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
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
        private readonly EstadoVacioEco _vacio = new(conEco: false, icono: IconChar.Coins);
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
            StartPosition = FormStartPosition.CenterParent; Size = new Size(1180, 760);
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Coins, mostrarAdministracion ? "Tarifas y ecopuntos" : "Ecopuntos",
                mostrarAdministracion
                    ? "Consulta el saldo de un ciudadano, acredita depósitos verificados y administra las tarifas de puntos por material."
                    : "Consulta el saldo de un ciudadano y acredita los depósitos que ya verificaste.");
            // El módulo se desplaza: las tarjetas conservan su altura natural y nada se solapa en pantallas bajas.
            AutoScroll = true;

            // Saldo: icono, cifra destacada y la consulta del ciudadano.
            var saldo = ModuloEco.Seccion("Saldo de ecopuntos");
            var cifra = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, BackColor = TemaEcoAccess.Superficie, Margin = new Padding(0, 0, 0, 12) };
            cifra.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cifra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cifra.Controls.Add(new InsigniaIcono(IconChar.Coins, TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave, circular: false) { Size = new Size(64, 64), Margin = new Padding(0, 0, 16, 0), AccessibleName = "Saldo de ecopuntos" }, 0, 0);
            var textosSaldo = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            _saldo.Font = TemaEcoAccess.Valor; _saldo.ForeColor = TemaEcoAccess.VerdeOscuro; _saldo.Text = "—"; _saldo.Margin = Padding.Empty;
            _totales.ForeColor = TemaEcoAccess.Suave; _totales.Font = TemaEcoAccess.Cuerpo;
            textosSaldo.Controls.Add(_saldo);
            textosSaldo.Controls.Add(_totales);
            cifra.Controls.Add(textosSaldo, 1, 0);
            saldo.Agregar(cifra);
            if (vistaCiudadana)
            {
                var propias = ModuloEco.Botones(Boton("Actualizar mi saldo", ConsultarAsync, IconChar.ArrowsRotate));
                if (estadisticas is not null)
                    propias.Controls.Add(Boton("Mi actividad diaria y semanal", () =>
                    {
                        using var actividad = new FormEstadisticasPersonales(estadisticas, actor);
                        actividad.ShowDialog(this);
                        return Task.CompletedTask;
                    }, IconChar.ChartColumn));
                saldo.Agregar(propias);
            }
            else saldo.Agregar(ModuloEco.FilaCampos(
                ModuloEco.Campo("Ciudadano (ID)", IconChar.User, _ciudadano, ancho: 200),
                ModuloEco.JuntoACampos(Boton("Consultar saldo e historial", ConsultarAsync, IconChar.MagnifyingGlass))));
            ModuloEco.Fila(raiz, saldo);

            // Historial con estado vacío explicativo.
            var historial = ModuloEco.Seccion("Historial de movimientos", "Hora de Colombia. Se muestran los últimos 50 movimientos.");
            var area = new Panel { Height = 320, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            TemaEcoAccess.EstilizarTabla(_historial);
            area.Controls.Add(_historial);
            area.Controls.Add(_vacio);
            _vacio.Dock = DockStyle.Fill;
            _vacio.Mostrar("Consulta un ciudadano para ver su saldo e historial. Los ecopuntos se acreditan solo después de verificar un depósito.", false);
            _historial.Visible = false;
            historial.Agregar(area);
            ModuloEco.Fila(raiz, historial);

            var operacion = ModuloEco.Seccion("Acreditar, anular o corregir", "Usa el ID del depósito verificado o del crédito activo y la versión de la regla.");
            var anular = ModuloEco.JuntoACampos(Boton("Anular crédito", () => OperarAsync(TipoOperacionEcopuntos.Revertir), IconChar.Ban)); anular.Visible = mostrarAdministracion;
            var corregir = ModuloEco.JuntoACampos(Boton("Corregir con nueva regla", () => OperarAsync(TipoOperacionEcopuntos.Corregir), IconChar.PenToSquare)); corregir.Visible = mostrarAdministracion;
            operacion.Agregar(ModuloEco.FilaCampos(
                ModuloEco.Campo("Depósito (ID)", IconChar.BoxOpen, _deposito, ancho: 190),
                ModuloEco.Campo("Regla", IconChar.ListCheck, _regla, ancho: 440),
                ModuloEco.JuntoACampos(Boton("Acreditar depósito", () => OperarAsync(TipoOperacionEcopuntos.Acreditar), IconChar.CircleCheck))));
            operacion.Agregar(ModuloEco.FilaCampos(
                ModuloEco.Campo("Crédito activo (ID)", IconChar.Coins, _credito, ancho: 190),
                ModuloEco.Campo("Motivo", IconChar.AlignLeft, _motivo, ancho: 440),
                anular, corregir));
            operacion.Visible = !vistaCiudadana;
            ModuloEco.Fila(raiz, operacion);

            var reglas = ModuloEco.Seccion("Reglas de puntos por material", "Cada cambio crea una versión nueva desde ahora; los movimientos anteriores conservan su tarifa.");
            _porUnidad.Font = TemaEcoAccess.Subtitulo;
            _porUnidad.Margin = new Padding(0, TemaEcoAccess.Subtitulo.Height + 18, 14, 14);
            reglas.Agregar(ModuloEco.FilaCampos(
                ModuloEco.Campo("Material", IconChar.Leaf, _material, ancho: 260),
                ModuloEco.Campo("Tarifa en puntos", IconChar.Coins, _puntos, ancho: 190),
                _porUnidad,
                ModuloEco.Campo("Descripción de la nueva versión", IconChar.AlignLeft, _descripcion, ancho: 360),
                ModuloEco.JuntoACampos(Boton("Crear regla desde ahora", CrearReglaAsync, IconChar.Plus))));
            reglas.Visible = mostrarAdministracion;
            ModuloEco.Fila(raiz, reglas);
            _estado.MaximumSize = Size.Empty;
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(_estado));
            Load += async (_, _) => await EjecutarAsync(CargarAsync);
            FormClosing += (_, e) => e.Cancel = _ocupado;
            ModuloEco.Terminar(this, raiz, new Size(640, mostrarAdministracion ? 1240 : 980));
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
        private Button Boton(string texto, Func<Task> accion, IconChar icono = IconChar.None)
        {
            var boton = ModuloEco.Boton(new Button { Text = texto, AccessibleName = texto }, icono);
            boton.Click += async (_, _) => await EjecutarAsync(accion);
            return boton;
        }

        private static NumericUpDown Numero() => new() { Minimum = 1, Maximum = int.MaxValue, Width = 105 };
    }
}
