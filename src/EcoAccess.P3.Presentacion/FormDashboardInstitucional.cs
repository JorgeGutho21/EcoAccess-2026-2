using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Presenta agregados autorizados; no ejecuta SQL ni acredita puntos.</summary>
    public sealed class FormDashboardInstitucional : Form
    {
        private readonly ServicioInformeInstitucional _servicio;
        private readonly int _actor;
        private readonly DateTimePicker _desde = new() { Width = 135, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-29) };
        private readonly DateTimePicker _hasta = new() { Width = 135, Format = DateTimePickerFormat.Short };
        private readonly NumericUpDown _caneca = new() { Minimum = 0, Maximum = int.MaxValue, Width = 85 };
        private readonly NumericUpDown _ubicacion = new() { Minimum = 0, Maximum = int.MaxValue, Width = 85 };
        private readonly ComboBox _periodo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        private readonly ComboBox _alcance = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 270 };
        private readonly Button _consultar = new() { Text = "Actualizar", AutoSize = true };
        private readonly Label _estado = new() { AutoSize = true, MaximumSize = new Size(1100, 0) };
        private readonly Label _indicadores = new() { AutoSize = true, MaximumSize = new Size(1100, 0), Padding = new Padding(12) };
        private readonly PanelGraficosResumen _graficos = new();
        private readonly DataGridView _grupos = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly CancellationTokenSource _cierre = new();
        private InformeInstitucional? _datos;
        private bool _ocupado;
        private bool _demostracionConsultada;
        private DateOnly _desdeConsultado;
        private DateOnly _hastaConsultado;

        public FormDashboardInstitucional(ServicioInformeInstitucional servicio, int actor)
        {
            _servicio = servicio; _actor = actor;
            Text = "EcoAccess · dashboard institucional";
            Font = TemaEcoAccess.Cuerpo; BackColor = TemaEcoAccess.Fondo; ForeColor = TemaEcoAccess.Texto;
            AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(1200, 850); MinimumSize = new Size(720, 560);
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };
            var columna = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
            var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
            _periodo.Items.AddRange(new object[] { "Día", "Semana", "Mes" }); _periodo.SelectedIndex = 0;
            _alcance.Items.AddRange(new object[] { "Sin marcas de ensayo", "Demostración y simulaciones" }); _alcance.SelectedIndex = 0;
            foreach (var item in new (string Texto, Control Control)[] { ("Desde",_desde),("Hasta",_hasta),
                ("Caneca (0=todas)",_caneca),("Ubicación (0=todas)",_ubicacion),("Agrupar",_periodo),("Datos",_alcance) })
            { filtros.Controls.Add(new Label { Text = item.Texto, AutoSize = true, Margin = new Padding(8) }); filtros.Controls.Add(item.Control); }
            filtros.Controls.Add(_consultar);
            columna.Controls.Add(new Label { Text = "Actividad institucional", Font = TemaEcoAccess.TituloSeccion, AutoSize = true });
            columna.Controls.Add(filtros); columna.Controls.Add(_indicadores); columna.Controls.Add(_graficos);
            var tabla = new Panel { Dock = DockStyle.Top, Height = 260 }; tabla.Controls.Add(_grupos); columna.Controls.Add(tabla);
            columna.Controls.Add(_estado); scroll.Controls.Add(columna); Controls.Add(scroll);
            scroll.SizeChanged += (_, _) => { columna.Width = Math.Max(620, scroll.ClientSize.Width - 40); _graficos.Width = columna.Width; };
            _consultar.Click += async (_, _) => await ConsultarAsync();
            _periodo.SelectedIndexChanged += (_, _) => Mostrar();
            Shown += async (_, _) => await ConsultarAsync();
            FormClosed += (_, _) => _cierre.Cancel();
        }
        private async Task ConsultarAsync()
        {
            if (_ocupado) return;
            _ocupado = true; _consultar.Enabled = false; _datos = null;
            _graficos.Limpiar("Consultando…"); _grupos.DataSource = null; _indicadores.Text = "Consultando…";
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
            limite.CancelAfter(TimeSpan.FromSeconds(45));
            try
            {
                var periodo = HoraColombia.PeriodoUtc(DateOnly.FromDateTime(_desde.Value), DateOnly.FromDateTime(_hasta.Value));
                bool demostracion = _alcance.SelectedIndex == 1;
                var r = await _servicio.ConsultarAsync(_actor, new(periodo.DesdeUtc, periodo.HastaUtc,
                    _caneca.Value == 0 ? null : (int)_caneca.Value, _ubicacion.Value == 0 ? null : (int)_ubicacion.Value, demostracion), limite.Token);
                if (IsDisposed) return;
                if (!r.Exitoso) { _estado.Text = r.Mensaje; _indicadores.Text = "Datos no disponibles"; return; }
                _demostracionConsultada = demostracion;
                _desdeConsultado = DateOnly.FromDateTime(periodo.DesdeUtc.AddHours(-5));
                _hastaConsultado = DateOnly.FromDateTime(periodo.HastaUtc.AddHours(-5)).AddDays(-1);
                _datos = r.Valor; Mostrar();
            }
            catch (OperationCanceledException) { if (!IsDisposed) _estado.Text = "Consulta cancelada o tiempo agotado. Puedes reintentar."; }
            catch (Exception) { if (!IsDisposed) _estado.Text = "No se pudo consultar el informe. Solicita revisión de la conexión y de las vistas de reporte."; }
            finally { _ocupado = false; if (!IsDisposed) _consultar.Enabled = true; }
        }
        private void Mostrar()
        {
            if (_datos is not { } d) return;
            var i = d.Actual;
            _indicadores.Text = $"{(_demostracionConsultada ? "DEMOSTRACIÓN / SIMULACIONES" : "SIN MARCAS DE ENSAYO (no certifica actividad física)")}\nSolicitudes: {i.Solicitudes:N0} · Depósitos: {i.Depositos:N0} · verificados: {i.Verificados:N0} · ciudadanos participantes únicos: {i.Ciudadanos:N0}\n" +
                $"Unidades verificadas: {i.UnidadesVerificadas:N0} · verificados históricos sin conteo: {i.VerificadosSinConteo:N0}\n" +
                $"Puntos acreditados: {i.Acreditados:N0} · reversos: {i.Reversos:N0} · netos: {i.Netos:N0}\n" +
                $"Opiniones globales del período: {i.Opiniones:N0} · promedio: {(i.Promedio is { } media ? media.ToString("N2") : "sin valoraciones")} / 5\n" +
                $"Período anterior de igual duración: {d.Anterior.Depositos:N0} depósitos · diferencia: {i.Depositos - d.Anterior.Depositos:+0;-0;0}";
            var dias = d.Dias;
            if (_periodo.SelectedIndex > 0)
                dias = d.Dias.Where(f => f.Dia is not null).GroupBy(f => _periodo.SelectedIndex == 1
                    ? f.Dia!.Value.AddDays(-(((int)f.Dia.Value.DayOfWeek + 6) % 7)) : new DateOnly(f.Dia!.Value.Year, f.Dia.Value.Month, 1))
                    .Select(g => new FilaAgrupada(null, g.Key, g.Key.ToString("yyyy-MM-dd"), g.Sum(f => f.Depositos), g.Sum(f => f.Verificados), g.Sum(f => f.ConPesoMedido), g.Sum(f => f.KgMedidos), 0, 0)).ToArray();
            _graficos.MostrarInforme(d, dias, _desdeConsultado, _hastaConsultado);
            _grupos.DataSource = d.Canecas.Select(f => new { Dimension = "Caneca", Identificador = f.Id, Grupo = f.Etiqueta, f.Depositos, f.Verificados })
                .Concat(d.Ubicaciones.Select(f => new { Dimension = "Ubicación", Identificador = f.Id, Grupo = f.Etiqueta, f.Depositos, f.Verificados })).ToList();
            _estado.Text = (_demostracionConsultada ? "Ensayos y simulaciones separados del informe operativo. No son actividad física. " : "Se excluyen simulaciones y fixtures conocidos; su ausencia no certifica actividad física. ") +
                "Opiniones globales: no se atribuyen a caneca o ubicación. Las consultas son sucesivas; semanas y meses extremos pueden ser parciales. " +
                "Bloques A/B/C/H/I/P y comedor: proyección, sin cifras inventadas.";
        }
    }
}
