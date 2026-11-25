using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Logica.Vision35.Expo;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Estadísticas de la estación de tres canecas desde Oracle Cloud (OPERACION_ESTACION y su detalle V031): totales,
    /// invitados frente a registrados, Telegram frente a web, residuos más frecuentes, por caneca, fecha y hora, aperturas
    /// ejecutadas y fallidas, y el historial. Filtros por fecha, material, caneca, canal y tipo de usuario. Sin datos
    /// ficticios: si Oracle no responde, la pantalla lo dice y no muestra cifras.
    /// </summary>
    internal sealed class FormEstadisticasEstacion : Form
    {
        private readonly DateTimePicker _desde = new() { Format = DateTimePickerFormat.Short, AccessibleName = "Desde" };
        private readonly DateTimePicker _hasta = new() { Format = DateTimePickerFormat.Short, AccessibleName = "Hasta" };
        private readonly ComboBox _caneca = Combo("Caneca", "Todas", "VERDE", "BLANCA", "NEGRA");
        private readonly ComboBox _canal = Combo("Canal", "Todos", "WEB", "TELEGRAM", "PC");
        private readonly ComboBox _usuario = Combo("Tipo de usuario", "Todos", "INVITADO", "REGISTRADO");
        private readonly ComboBox _material = Combo("Material", "Todos");
        private readonly Button _botonConsultar = new() { Text = "Consultar", AutoSize = true, AccessibleName = "Consultar estadísticas" };
        private readonly Label _estado = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Font = TemaEcoAccess.Pequena };
        private readonly FlowLayoutPanel _indicadores = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 6, 0, 6) };
        private readonly DataGridView _resumen = Tabla("Resumen por categoría");
        private readonly DataGridView _historial = Tabla("Historial de operaciones");
        private IReadOnlyList<FilaEstadisticaEstacion> _filas = Array.Empty<FilaEstadisticaEstacion>();

        public FormEstadisticasEstacion()
        {
            Text = "EcoAccess · Operaciones de la estación";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1180, 760);
            MinimumSize = new Size(900, 600);
            BackColor = TemaEcoAccess.Fondo;
            _desde.Value = DateTime.Today.AddDays(-30);
            _hasta.Value = DateTime.Today;

            var titulo = new Label { Text = "Operaciones de la estación", Font = TemaEcoAccess.Titulo, ForeColor = TemaEcoAccess.Texto, AutoSize = true };
            var subtitulo = new Label { Text = "Datos de Oracle Cloud (OPERACION_ESTACION). Las tapas no tienen sensores: «depósito declarado» es lo que confirma la persona.", Font = TemaEcoAccess.Cuerpo, ForeColor = TemaEcoAccess.Suave, AutoSize = true };
            var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 8, 0, 4) };
            foreach (var (etq, c) in new (string, Control)[] { ("Desde", _desde), ("Hasta", _hasta), ("Caneca", _caneca), ("Canal", _canal), ("Usuario", _usuario), ("Material", _material) })
            {
                var caja = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 0, 12, 0) };
                caja.Controls.Add(new Label { Text = etq, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, AutoSize = true });
                c.Width = etq == "Material" ? 220 : 140;
                caja.Controls.Add(c);
                filtros.Controls.Add(caja);
            }
            _botonConsultar.Margin = new Padding(0, 22, 12, 0);
            filtros.Controls.Add(_botonConsultar);
            _estado.Margin = new Padding(0, 30, 0, 0);
            filtros.Controls.Add(_estado);

            var division = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 420, BackColor = TemaEcoAccess.Fondo };
            division.Panel1.Controls.Add(_resumen);
            division.Panel2.Controls.Add(_historial);
            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16) };
            cuerpo.Controls.Add(division);
            cuerpo.Controls.Add(_indicadores);
            cuerpo.Controls.Add(filtros);
            cuerpo.Controls.Add(subtitulo);
            cuerpo.Controls.Add(titulo);
            titulo.Dock = DockStyle.Top; subtitulo.Dock = DockStyle.Top;
            Controls.Add(cuerpo);

            _botonConsultar.Click += async (_, _) => await ConsultarAsync();
            foreach (var c in new[] { _caneca, _canal, _usuario, _material }) c.SelectedIndexChanged += (_, _) => Pintar();
            Shown += async (_, _) => await ConsultarAsync();
            TemaEcoAccess.Aplicar(this);
        }

        private static ComboBox Combo(string nombre, params string[] opciones)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = nombre };
            c.Items.AddRange(opciones); c.SelectedIndex = 0;
            return c;
        }

        private static DataGridView Tabla(string nombre)
        {
            var t = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AccessibleName = nombre };
            TemaEcoAccess.EstilizarTabla(t);
            return t;
        }

        private async Task ConsultarAsync()
        {
            if (EstacionPc.Estadisticas is not { } consultar) { _estado.Text = "Oracle no está configurado en este equipo."; return; }
            _botonConsultar.Enabled = false; _estado.Text = "Consultando Oracle Cloud…";
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                _filas = await consultar(_desde.Value.Date, _hasta.Value.Date.AddDays(1), cts.Token);
                string actual = _material.SelectedItem as string ?? "Todos";
                _material.Items.Clear(); _material.Items.Add("Todos");
                foreach (var m in _filas.Select(f => f.NombreResiduo).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().OrderBy(n => n)) _material.Items.Add(m!);
                _material.SelectedItem = _material.Items.Contains(actual) ? actual : "Todos";
                _estado.Text = $"{_filas.Select(f => f.Clave).Distinct().Count()} operaciones en Oracle · actualizado {DateTime.Now:HH:mm:ss}";
                Pintar();
            }
            catch (Exception e)
            {
                _filas = Array.Empty<FilaEstadisticaEstacion>(); Pintar();
                _estado.Text = "No se pudo consultar Oracle (" + e.GetType().Name + "). Revisa la conexión y vuelve a intentar.";
            }
            finally { _botonConsultar.Enabled = true; }
        }

        private void Pintar()
        {
            string? Sel(ComboBox c) => c.SelectedItem is string s && !s.StartsWith("Tod", StringComparison.Ordinal) ? s : null;
            var filtradas = EstadisticasEstacion.Filtrar(_filas, new FiltroEstadisticasEstacion(Sel(_caneca), Sel(_canal), Sel(_usuario), Sel(_material)));
            var r = EstadisticasEstacion.Calcular(filtradas);

            _indicadores.SuspendLayout();
            _indicadores.Controls.Clear();
            void Indicador(string t, int v, string nota)
            {
                var caja = new Panel { Width = 170, Height = 84, BackColor = Color.White, Margin = new Padding(0, 0, 10, 10), Padding = new Padding(12, 8, 12, 8) };
                caja.Controls.Add(new Label { Text = nota, Dock = DockStyle.Top, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, AutoSize = false, Height = 18 });
                caja.Controls.Add(new Label { Text = v.ToString("N0"), Dock = DockStyle.Top, Font = TemaEcoAccess.Valor, ForeColor = TemaEcoAccess.VerdeOscuro, AutoSize = false, Height = 40 });
                caja.Controls.Add(new Label { Text = t, Dock = DockStyle.Top, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, AutoSize = false, Height = 22 });
                _indicadores.Controls.Add(caja);
            }
            Indicador("Operaciones", r.Operaciones, "Total en el periodo");
            Indicador("Invitados", r.Invitados, "Sin cuenta EcoAccess");
            Indicador("Registrados", r.Registrados, "Con cuenta vinculada");
            Indicador("Residuos", r.Residuos, "Identificados con detalle");
            Indicador("Aperturas ejecutadas", r.AperturasEjecutadas, "Orden confirmada por el ESP32");
            Indicador("Aperturas fallidas", r.AperturasFallidas, "Rechazadas o sin respuesta");
            Indicador("Depósitos declarados", r.DisposicionesDeclaradas, $"Verificados por sensor: {r.DisposicionesVerificadas}");
            _indicadores.ResumeLayout();

            _resumen.Columns.Clear(); _resumen.Rows.Clear();
            _resumen.Columns.Add("dim", "Categoría"); _resumen.Columns.Add("clave", "Valor"); _resumen.Columns.Add("n", "Cantidad");
            void Bloque(string dim, IEnumerable<(string Clave, int Cantidad)> datos) { foreach (var (c, n) in datos) _resumen.Rows.Add(dim, c, n); }
            Bloque("Canal", r.PorCanal); Bloque("Caneca", r.PorCaneca); Bloque("Material", r.Materiales); Bloque("Método", r.PorMetodo);
            Bloque("Fecha", r.PorDia); Bloque("Hora", r.PorHora);

            _historial.Columns.Clear(); _historial.Rows.Clear();
            foreach (var (n, t) in new[] { ("f", "Fecha"), ("c", "Canal"), ("u", "Usuario"), ("m", "Método"), ("r", "Residuo"), ("k", "Caneca"), ("d", "Tiempo"), ("e", "Estado") })
                _historial.Columns.Add(n, t);
            foreach (var f in filtradas.OrderByDescending(x => x.FechaLocal).Take(500))
                _historial.Rows.Add(f.FechaLocal.ToString("dd/MM/yyyy HH:mm"), f.Canal, f.TipoUsuario == "INVITADO" ? "Invitado" : "Registrado", f.Metodo ?? "—",
                    f.NombreResiduo ?? "—", (f.Caneca ?? f.CanecasOperacion ?? "—").ToLowerInvariant().Replace(",", " + "), f.DuracionMs / 1000 + " s",
                    f.EstadoOperacion == "DISPOSICION_CONFIRMADA" ? "Depósito declarado" : f.EstadoOperacion == "ERROR" ? "No se abrió" : "Registrada");
        }
    }
}
