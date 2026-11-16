using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Diagnóstico administrativo de solo lectura; no recibe ni muestra SQL o credenciales.</summary>
    public sealed class FormDiagnosticoOracle : Form
    {
        private readonly ServicioDiagnosticoOracle _servicio;
        private readonly int _idActor;
        private readonly Label _estado = new() { AutoSize = true, MaximumSize = new Size(1000, 0) };
        private readonly Label _detalle = new() { AutoSize = true, MaximumSize = new Size(1000, 0) };
        private readonly Label _servicios = new() { AutoSize = true, MaximumSize = new Size(940, 0), Margin = new Padding(0, 12, 0, 12), AccessibleName = "Estado de los servicios de la demostración" };
        private readonly Button _consultar = new() { Text = "&Verificar conexión", AutoSize = true, AccessibleName = "Verificar el esquema Oracle" };
        private readonly Button _cancelar = new() { Text = "&Cancelar consulta", AutoSize = true, Enabled = false };
        private readonly DataGridView _migraciones = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AccessibleName = "Estado estructural de las migraciones Oracle",
        };
        private CancellationTokenSource? _consulta;

        public FormDiagnosticoOracle(ServicioDiagnosticoOracle servicio, int idActor)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _idActor = idActor;
            Text = "Diagnóstico Oracle · EcoAccess";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1000, 600);
            // Con 1366×768 al 125 % el detalle de servicios se cortaba 4 px: bajo este alto, la ventana principal desplaza el módulo.
            MinimumSize = new Size(640, 520);
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(245, 247, 249);
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
            for (int i = 0; i < 6; i++) panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label { Text = "Estado de la base de datos", AutoSize = true, Font = new Font(Font.FontFamily, 20, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) }, 0, 0);
            panel.Controls.Add(new Label { Text = "Consulta de solo lectura del esquema actual. No modifica registros ni aplica migraciones.", AutoSize = true, MaximumSize = new Size(900, 0), Margin = new Padding(0, 0, 0, 12) }, 0, 1);
            var acciones = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
            acciones.Controls.AddRange(new Control[] { _consultar, _cancelar });
            panel.Controls.Add(acciones, 0, 2);
            panel.Controls.Add(_estado, 0, 3);
            panel.Controls.Add(_detalle, 0, 4);
            panel.Controls.Add(_servicios, 0, 5);
            panel.Controls.Add(_migraciones, 0, 6);
            Controls.Add(panel);
            DisenoFormularios.AjustarEtiquetasAlAncho(panel, _estado, _detalle, _servicios);
            _consultar.Click += async (_, _) => await ConsultarAsync();
            _cancelar.Click += (_, _) => _consulta?.Cancel();
            Shown += async (_, _) => await ConsultarAsync();
            FormClosing += (_, e) => { if (_consulta is not null) { e.Cancel = true; _consulta.Cancel(); } };
        }

        /// <summary>Procesos que inició el supervisor en este equipo; un proceso activo no acredita un sensor físico.</summary>
        private void MostrarServicios()
        {
            var resumen = EstadoServiciosDemostracion.Consultar();
            var texto = new System.Text.StringBuilder("Servicios de la demostración (este equipo):\n");
            foreach (var s in resumen.Servicios)
                texto.AppendLine($"  {(s.Activo ? "●" : "○")} {s.Nombre}: {(s.Activo ? "activo" : "no disponible")} · {s.Detalle}");
            texto.AppendLine("Última operación del bridge: " + (resumen.UltimaOperacion ?? "sin registros en esta sesión"));
            texto.Append(resumen.Errores.Count == 0 ? "Errores recientes: ninguno registrado." : "Errores recientes:\n  " + string.Join("\n  ", resumen.Errores));
            _servicios.Text = texto.ToString();
            _servicios.ForeColor = resumen.Errores.Count == 0 ? Color.FromArgb(40, 55, 65) : Color.DarkRed;
        }

        private async Task ConsultarAsync()
        {
            if (_consulta is not null) return;
            using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _consulta = consulta;
            _consultar.Enabled = false;
            _cancelar.Enabled = true;
            _estado.Text = "Verificando Oracle…";
            _estado.ForeColor = Color.FromArgb(40, 55, 65);
            _detalle.Text = "";
            _migraciones.DataSource = null;
            MostrarServicios();
            try
            {
                var resultado = await _servicio.ConsultarAsync(_idActor, consulta.Token).WaitAsync(consulta.Token);
                if (!resultado.Exitoso)
                {
                    _estado.Text = "Acceso rechazado: " + resultado.Mensaje;
                    _estado.ForeColor = Color.Firebrick;
                    return;
                }
                DiagnosticoOracle diagnostico = resultado.Valor;
                EstadoEsquemaOracle esquema = diagnostico.Esquema;
                _estado.Text = diagnostico.SinIncidencias ? "Oracle disponible · sin incidencias estructurales detectadas" : "Oracle disponible · revisar las incidencias estructurales";
                _estado.ForeColor = diagnostico.SinIncidencias ? Color.DarkGreen : Color.DarkRed;
                _detalle.Text = $"Esquema: {esquema.Esquema} · PDB: {esquema.Contenedor} · Consulta: {esquema.LatenciaMilisegundos:F1} ms\n" +
                    $"Tablas: {esquema.Tablas.Count} · Restricciones deshabilitadas/no validadas: {esquema.RestriccionesProblematicas} · Objetos inválidos: {esquema.ObjetosInvalidos} · Índices inválidos: {esquema.IndicesInvalidos}\n" +
                    $"Última verificación: {diagnostico.VerificadoUtc.ToOffset(TimeSpan.FromHours(-5)):yyyy-MM-dd HH:mm:ss} (Colombia)\n" +
                    "La presencia de tablas no acredita por sí sola el historial ni el contenido completo de una migración.";
                _migraciones.DataSource = diagnostico.Migraciones.Select(m => new { Versión = m.Version, m.Estado, Faltantes = m.ElementosFaltantes }).ToList();
            }
            catch (OperationCanceledException)
            {
                _estado.Text = "Consulta cancelada o tiempo de espera agotado. Puede volver a verificar.";
            }
            catch (Exception)
            {
                _estado.Text = "Error: no se pudo verificar Oracle. Revise el servicio y la configuración local, y vuelva a intentar.";
                _estado.ForeColor = Color.Firebrick;
            }
            finally
            {
                _detalle.Text = Compat.IsNullOrEmpty(_detalle.Text)
                    ? $"Último intento: {DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)):yyyy-MM-dd HH:mm:ss} (Colombia). Sin resultado verificado."
                    : _detalle.Text;
                _consulta = null;
                _consultar.Enabled = true;
                _cancelar.Enabled = false;
            }
        }
    }
}
