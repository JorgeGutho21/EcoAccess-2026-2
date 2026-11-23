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
    public sealed partial class FormEcopuntos : Form
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
            // Disposición Eco (banner, fichas del saldo, historial con pastillas y secciones con icono): FormEcopuntos.Diseno.cs.
            ConstruirDiseno(mostrarAdministracion, vistaCiudadana, estadisticas, actor);
            Load += async (_, _) => await EjecutarAsync(CargarAsync);
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
        private Button Boton(string texto, Func<Task> accion, IconChar icono = IconChar.None)
        {
            var boton = ModuloEco.Boton(new Button { Text = texto, AccessibleName = texto }, icono);
            boton.Click += async (_, _) => await EjecutarAsync(accion);
            return boton;
        }

        private static NumericUpDown Numero() => new() { Minimum = 1, Maximum = int.MaxValue, Width = 105 };
    }
}
