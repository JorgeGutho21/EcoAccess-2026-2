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

    /// <summary>Reutiliza las gráficas existentes con agregados privados autorizados por el servicio.</summary>
    public sealed class FormEstadisticasPersonales : Form
    {
        private readonly CancellationTokenSource _cierre = new();

        public FormEstadisticasPersonales(ServicioEstadisticas servicio, int actor)
        {
            Text = "Mi actividad · EcoAccess";
            Size = new Size(1150, 780);
            MinimumSize = new Size(680, 500);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = TemaEcoAccess.Fondo; ForeColor = TemaEcoAccess.Texto; Font = TemaEcoAccess.Cuerpo;
            StartPosition = FormStartPosition.CenterParent;
            var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(20) };
            var periodo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, AccessibleName = "Período de mi actividad" };
            periodo.Items.AddRange(new object[] { "Últimos 7 días", "Últimos 30 días", "Últimos 90 días" }); periodo.SelectedIndex = 1;
            var semanal = new CheckBox { Text = "Agrupar por semana", AutoSize = true };
            var consultar = new Button { Text = "Consultar mi actividad", AutoSize = true };
            TemaEcoAccess.EstilizarBoton(consultar);
            filtros.Controls.AddRange(new Control[] { periodo, semanal, consultar });
            var resumen = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(20, 8, 20, 12), Text = "Consultando tu actividad…" };
            var scroll = new PanelDesplazable { Dock = DockStyle.Fill, Padding = new Padding(20) };
            var graficas = new PanelGraficosResumen();
            scroll.Controls.Add(graficas);
            var pestanas = new TabControl { Dock = DockStyle.Fill };
            var resumenTab = new TabPage("Mis gráficas"); resumenTab.Controls.Add(scroll);
            var historialTab = new TabPage("Mis depósitos");
            var historial = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            var navegacion = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            var anterior = new Button { Text = "Anterior", AutoSize = true, Enabled = false };
            var siguiente = new Button { Text = "Siguiente", AutoSize = true, Enabled = false };
            var paginaTexto = new Label { Text = "Sin consulta", AutoSize = true };
            navegacion.Controls.AddRange(new Control[] { anterior, paginaTexto, siguiente });
            historialTab.Controls.Add(historial); historialTab.Controls.Add(navegacion);
            pestanas.TabPages.AddRange(new TabPage[] { resumenTab, historialTab });
            Controls.Add(pestanas); Controls.Add(resumen); Controls.Add(filtros);
            EstadisticasPersonales? datos = null;
            bool ocupado = false;
            int paginaActual = 1;
            async Task CargarDetalleAsync(int pagina)
            {
                if (datos is null) return;
                anterior.Enabled = siguiente.Enabled = false;
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
                limite.CancelAfter(TimeSpan.FromSeconds(30));
                var resultado = await servicio.ListarMiosAsync(actor, datos.Periodo.DesdeUtc, datos.Periodo.HastaUtc, pagina, 20, limite.Token);
                if (IsDisposed) return;
                if (!resultado.Exitoso) { historial.DataSource = null; paginaTexto.Text = resultado.Mensaje; return; }
                paginaActual = resultado.Valor.Pagina;
                historial.DataSource = resultado.Valor.Filas.Select(f => new
                {
                    FechaColombia = HoraColombia.DesdeUtc(f.FechaDeteccionUtc).ToString("dd/MM/yyyy HH:mm"),
                    MaterialDeclarado = f.MaterialDeclarado,
                    MaterialVerificado = f.MaterialVerificado ?? "Pendiente de verificación",
                    PesoMedidoKg = f.PesoMedidoKg?.ToString("0.###") ?? "Sin medición"
                }).ToList();
                paginaTexto.Text = resultado.Valor.Total == 0 ? "Sin depósitos en este período" :
                    $"Página {paginaActual} de {resultado.Valor.TotalPaginas} · {resultado.Valor.Total} depósitos";
                anterior.Enabled = paginaActual > 1;
                siguiente.Enabled = paginaActual < resultado.Valor.TotalPaginas;
            }
            async Task CambiarPaginaAsync(int pagina)
            {
                if (ocupado) return;
                ocupado = true; consultar.Enabled = false;
                try { await CargarDetalleAsync(pagina); }
                catch (OperationCanceledException) when (_cierre.IsCancellationRequested) { }
                catch (Exception) { if (!IsDisposed) { historial.DataSource = null; paginaTexto.Text = "No pudimos consultar el historial. Usa Consultar mi actividad para reintentar."; } }
                finally { ocupado = false; if (!IsDisposed) consultar.Enabled = true; }
            }
            anterior.Click += async (_, _) => await CambiarPaginaAsync(paginaActual - 1);
            siguiente.Click += async (_, _) => await CambiarPaginaAsync(paginaActual + 1);
            semanal.CheckedChanged += (_, _) => { if (datos is not null) graficas.MostrarPersonal(datos, semanal.Checked); };
            async Task ConsultarAsync()
            {
                if (ocupado) return;
                ocupado = true; consultar.Enabled = periodo.Enabled = false;
                anterior.Enabled = siguiente.Enabled = false;
                try
                {
                    int dias = periodo.SelectedIndex switch { 0 => 7, 2 => 90, _ => 30 };
                    // Días completos de Colombia, incluyendo hoy; el servicio fija el actor en cada consulta.
                    DateTime hasta = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-5).Date.AddDays(1).AddHours(5), DateTimeKind.Utc);
                    using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
                    limite.CancelAfter(TimeSpan.FromSeconds(30));
                    var resultado = await servicio.ObtenerMiasAsync(actor, hasta.AddDays(-dias), hasta, limite.Token);
                    if (IsDisposed) return;
                    if (!resultado.Exitoso) { datos = null; historial.DataSource = null; graficas.Limpiar("Consulta no disponible para esta cuenta."); resumen.Text = resultado.Mensaje; return; }
                    datos = resultado.Valor;
                    resumen.Text = $"Mis depósitos: {datos.Actual.Depositos.Total:N0} · Verificados: {datos.Actual.Depositos.Verificados:N0}" +
                        $" · Período anterior: {datos.Anterior.Depositos.Total:N0} depósitos. Las solicitudes pendientes no son depósitos verificados.";
                    graficas.MostrarPersonal(datos, semanal.Checked);
                    await CargarDetalleAsync(1);
                }
                catch (OperationCanceledException) when (_cierre.IsCancellationRequested) { }
                catch (Exception)
                {
                    if (!IsDisposed)
                    {
                        datos = null; historial.DataSource = null; paginaTexto.Text = "Consulta incompleta";
                        graficas.Limpiar("No pudimos consultar tu actividad."); resumen.Text = "Intenta de nuevo en unos segundos.";
                    }
                }
                finally { ocupado = false; if (!IsDisposed) consultar.Enabled = periodo.Enabled = true; }
            }
            consultar.Click += async (_, _) => await ConsultarAsync();
            Shown += async (_, _) => await ConsultarAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed) { _cierre.Cancel(); _cierre.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
