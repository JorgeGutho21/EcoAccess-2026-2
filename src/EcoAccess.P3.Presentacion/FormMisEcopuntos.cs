using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Ecopuntos del ciudadano. Muestra lo que informa el servicio: la interfaz no calcula saldos ni otorga puntos. Las
    /// vistas por período agrupan los movimientos recibidos, que son los más recientes del libro.
    /// </summary>
    public sealed class FormMisEcopuntos : Form
    {
        private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        private readonly ServicioEcopuntos _servicio;
        private readonly ServicioEstadisticas? _estadisticas;
        private readonly int _actor;
        private readonly CancellationTokenSource _cierre = new();
        private readonly KryptonButton _actualizar = new() { Text = "Actualizar mis ecopuntos", AutoSize = true };
        private readonly Label _estado = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Text = "Consultando Oracle…" };
        private readonly RejillaTarjetas _indicadores = new(200) { ColumnasMaximas = 4 };
        private readonly TarjetaDato _saldo = new(IconChar.Coins, "Saldo actual");
        private readonly TarjetaDato _obtenidos = new(IconChar.CirclePlus, "Puntos obtenidos");
        private readonly TarjetaDato _reversados = new(IconChar.RotateLeft, "Puntos reversados");
        private readonly TarjetaDato _netos = new(IconChar.ScaleBalanced, "Puntos netos");
        private readonly ComboBox _periodo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, AccessibleName = "Agrupar la evolución por" };
        private readonly CartesianChart _evolucion = new() { Dock = DockStyle.Fill, BackColor = Color.White, EasingFunction = null };
        private readonly EstadoVacioEco _vacioEvolucion = new() { Dock = DockStyle.Fill };
        private readonly DataGridView _recientes = Tabla("Últimos movimientos");
        private readonly DataGridView _historial = Tabla("Historial de movimientos");
        private readonly EstadoVacioEco _vacioRecientes = new() { Dock = DockStyle.Fill };
        private readonly EstadoVacioEco _vacioHistorial = new() { Dock = DockStyle.Fill };
        private readonly RejillaTarjetas _semana = new(200) { ColumnasMaximas = 3 };
        private readonly TarjetaDato _estaSemana = new(IconChar.CalendarWeek, "Esta semana");
        private readonly TarjetaDato _semanaAnterior = new(IconChar.CalendarDays, "Semana anterior");
        private readonly TarjetaDato _diferencia = new(IconChar.ArrowRightArrowLeft, "Diferencia");
        private readonly CartesianChart _graficoSemana = new() { Dock = DockStyle.Fill, BackColor = Color.White, EasingFunction = null };
        private readonly EstadoVacioEco _vacioSemana = new() { Dock = DockStyle.Fill };
        private readonly List<(PanelDesplazable Pagina, Action<int> Ajustar)> _ajustes = new();
        private IReadOnlyList<MovimientoEcopuntos> _movimientos = Array.Empty<MovimientoEcopuntos>();
        private bool _ocupado;

        public FormMisEcopuntos(ServicioEcopuntos servicio, ServicioEstadisticas? estadisticas, int actor)
        {
            _servicio = servicio;
            _estadisticas = estadisticas;
            _actor = actor;
            Text = "Mis ecopuntos · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1120, 760);
            MinimumSize = new Size(700, 520);
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            Font = TemaEcoAccess.Cuerpo;
            TemaEcoAccess.EstilizarAccion(_actualizar, principal: true);
            _actualizar.Values.Image = CacheIconos.Obtener(IconChar.ArrowsRotate, TemaEcoAccess.Px(this, 16), Color.White);
            _periodo.Items.AddRange(new object[] { "Día", "Semana", "Mes" });
            _periodo.SelectedIndex = 0;
            foreach (TarjetaDato dato in new[] { _saldo, _obtenidos, _reversados, _netos }) _indicadores.Controls.Add(dato);
            foreach (TarjetaDato dato in new[] { _estaSemana, _semanaAnterior, _diferencia }) _semana.Controls.Add(dato);

            var pestanas = new TabControl { Dock = DockStyle.Fill };
            pestanas.TabPages.Add(PaginaResumen());
            pestanas.TabPages.Add(PaginaTabla("Mi actividad", "Tus movimientos más recientes.", _recientes, _vacioRecientes));
            pestanas.TabPages.Add(PaginaSemana());
            pestanas.TabPages.Add(PaginaTabla("Historial", "Movimientos más recientes de tu libro de ecopuntos, en hora de Colombia.", _historial, _vacioHistorial));
            pestanas.TabPages.Add(PaginaComoGanar());
            pestanas.TabPages.Add(PaginaProximamente());
            Controls.Add(pestanas);
            Controls.Add(CrearEncabezado());

            _actualizar.Click += async (_, _) => await ConsultarAsync();
            _periodo.SelectedIndexChanged += (_, _) => MostrarEvolucion();
            Shown += async (_, _) => { Adaptar(); await ConsultarAsync(); };
            pestanas.SelectedIndexChanged += (_, _) => Adaptar();
            FormClosing += (_, _) => _cierre.Cancel();
        }

        private Control CrearEncabezado()
        {
            var barra = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(TemaEcoAccess.Px(this, 16), TemaEcoAccess.Px(this, 12), TemaEcoAccess.Px(this, 16), TemaEcoAccess.Px(this, 8)), BackColor = TemaEcoAccess.Superficie };
            barra.Controls.Add(_actualizar);
            _estado.Margin = new Padding(12, 8, 12, 0);
            barra.Controls.Add(_estado);
            if (_estadisticas is { } estadisticas)
            {
                var actividad = new KryptonButton { Text = "Mis depósitos y actividad", AutoSize = true, Margin = new Padding(0) };
                TemaEcoAccess.EstilizarAccion(actividad);
                actividad.Click += (_, _) =>
                {
                    using var ventana = new FormEstadisticasPersonales(estadisticas, _actor);
                    ventana.ShowDialog(this);
                };
                barra.Controls.Add(actividad);
            }
            return barra;
        }

        private static DataGridView Tabla(string nombre)
        {
            var tabla = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AccessibleName = nombre,
            };
            TemaEcoAccess.EstilizarTabla(tabla);
            return tabla;
        }

        private TabPage PaginaResumen()
        {
            var (pagina, columna, desplazable) = Pagina("Resumen");
            columna.Controls.Add(Titulo("Tu saldo", "Valores de Oracle. Los puntos se acreditan después de verificar un depósito."));
            columna.Controls.Add(_indicadores);
            var cabecera = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 8, 0, 8) };
            cabecera.Controls.Add(new Label { Text = "Evolución", AutoSize = true, Font = TemaEcoAccess.TituloSeccion, Margin = new Padding(0, 0, 12, 0) });
            cabecera.Controls.Add(new Label { Text = "Agrupar por:", AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
            cabecera.Controls.Add(_periodo);
            columna.Controls.Add(cabecera);
            var lienzo = Lienzo(_evolucion, _vacioEvolucion);
            columna.Controls.Add(lienzo);
            _ajustes.Add((desplazable, ancho => { _indicadores.Adaptar(ancho); lienzo.Width = ancho; }));
            return pagina;
        }

        private TabPage PaginaSemana()
        {
            var (pagina, columna, desplazable) = Pagina("Esta semana");
            columna.Controls.Add(Titulo("Esta semana", "De lunes a domingo, en hora de Colombia, frente a la semana anterior."));
            columna.Controls.Add(_semana);
            var lienzo = Lienzo(_graficoSemana, _vacioSemana);
            columna.Controls.Add(lienzo);
            _ajustes.Add((desplazable, ancho => { _semana.Adaptar(ancho); lienzo.Width = ancho; }));
            return pagina;
        }

        private TabPage PaginaTabla(string titulo, string ayuda, DataGridView tabla, EstadoVacioEco vacio)
        {
            var pagina = new TabPage(titulo) { BackColor = TemaEcoAccess.Fondo, Padding = new Padding(TemaEcoAccess.Px(this, 16)) };
            var area = new Panel { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Superficie };
            area.Controls.Add(tabla);
            area.Controls.Add(vacio);
            tabla.Visible = false;
            vacio.Mostrar("Consultando tus movimientos…", false);
            pagina.Controls.Add(area);
            pagina.Controls.Add(new Label { Text = ayuda, Dock = DockStyle.Top, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Padding = new Padding(0, 0, 0, 10) });
            return pagina;
        }

        private TabPage PaginaComoGanar()
        {
            var (pagina, columna, desplazable) = Pagina("Cómo ganar ecopuntos");
            columna.Controls.Add(Titulo("Cómo se ganan", "Reglas actuales de EcoAccess. Una foto o una solicitud de apertura no otorgan puntos por sí solas."));
            var pasos = new RejillaTarjetas(240) { ColumnasMaximas = 2 };
            foreach (var (icono, titulo, texto) in new (IconChar, string, string)[]
            {
                (IconChar.UserCheck, "1. Usa tu cuenta", "Los depósitos como invitado no acumulan ecopuntos."),
                (IconChar.BoxOpen, "2. Registra tu residuo", "Solo se abre si la caneca admite el material y confirmas la solicitud."),
                (IconChar.ClipboardCheck, "3. Espera la verificación", "Un operario o administrador confirma el material depositado."),
                (IconChar.Coins, "4. Se acreditan los puntos", "Se aplica la tarifa vigente del material, y cada depósito se acredita una sola vez."),
            }) pasos.Controls.Add(new TarjetaAccionRapida(icono, titulo, texto, interactiva: false));
            columna.Controls.Add(pasos);
            _ajustes.Add((desplazable, pasos.Adaptar));
            return pagina;
        }

        private TabPage PaginaProximamente()
        {
            var (pagina, columna, desplazable) = Pagina("Próximamente");
            columna.Controls.Add(Titulo("Lo que viene", "Son proyecciones: hoy no se pueden reclamar ni canjear."));
            var planes = new RejillaTarjetas(240) { ColumnasMaximas = 3 };
            foreach (var (titulo, texto) in new[]
            {
                ("Recompensas", "Beneficios a cambio de ecopuntos. Todavía no están disponibles."),
                ("Retos", "Metas ambientales individuales o por grupos. Todavía no están disponibles."),
                ("Clasificaciones", "Participación comparada, sin exponer datos personales. Todavía no están disponibles."),
            }) planes.Controls.Add(new TarjetaAccionRapida(IconChar.Seedling, titulo, texto, interactiva: false));
            columna.Controls.Add(planes);
            _ajustes.Add((desplazable, planes.Adaptar));
            return pagina;
        }

        private (TabPage Pagina, TableLayoutPanel Columna, PanelDesplazable Desplazable) Pagina(string titulo)
        {
            var pagina = new TabPage(titulo) { BackColor = TemaEcoAccess.Fondo };
            var desplazable = new PanelDesplazable { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(TemaEcoAccess.Px(this, 16)) };
            var columna = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo };
            desplazable.Controls.Add(columna);
            pagina.Controls.Add(desplazable);
            desplazable.ClientSizeChanged += (_, _) => Adaptar();
            return (pagina, columna, desplazable);
        }

        private static Control Titulo(string titulo, string ayuda)
        {
            var fila = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 0, 12) };
            fila.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, Margin = Padding.Empty });
            fila.Controls.Add(new Label { Text = ayuda, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 4, 0, 0) });
            return fila;
        }

        private Panel Lienzo(Control grafico, EstadoVacioEco vacio)
        {
            var lienzo = new Panel { Height = TemaEcoAccess.Px(this, 280), BackColor = TemaEcoAccess.Superficie, Margin = new Padding(0, 0, 0, 8) };
            lienzo.Controls.Add(grafico);
            lienzo.Controls.Add(vacio);
            grafico.Visible = false;
            vacio.Mostrar("Consultando tus movimientos…", false);
            return lienzo;
        }

        private void Adaptar()
        {
            foreach (var (pagina, ajustar) in _ajustes)
            {
                int ancho = pagina.ClientSize.Width - pagina.Padding.Horizontal;
                if (ancho > 0) ajustar(ancho);
            }
        }

        /// <summary>Solo vuelve a leer Oracle: no cambia ningún depósito ni saldo.</summary>
        private async Task ConsultarAsync()
        {
            if (_ocupado) return;
            _ocupado = true;
            _actualizar.Enabled = false;
            _estado.ForeColor = TemaEcoAccess.Suave;
            _estado.Text = "Consultando Oracle…";
            try
            {
                var r = await _servicio.ConsultarMiosAsync(_actor, _cierre.Token);
                if (IsDisposed) return;
                if (!r.Exitoso) { MostrarFallo(r.Mensaje ?? "Consulta no disponible para esta cuenta."); return; }
                ResumenEcopuntos datos = r.Valor;
                _movimientos = datos.Movimientos;
                _saldo.Mostrar(datos.Saldo.ToString("N0", Colombia), "Según el libro de ecopuntos de Oracle.");
                _obtenidos.Mostrar(datos.TotalAcreditado.ToString("N0", Colombia), "Acumulados por depósitos verificados.");
                _reversados.Mostrar(datos.TotalRevertido.ToString("N0", Colombia), "Anulaciones y correcciones registradas.");
                _netos.Mostrar(datos.Saldo.ToString("N0", Colombia), "Obtenidos menos reversados.");
                MostrarMovimientos();
                MostrarEvolucion();
                MostrarSemana();
                Adaptar();
                _estado.Text = $"Actualizado a las {HoraColombia.DesdeUtc(DateTime.UtcNow):HH:mm} · hora de Colombia.";
            }
            catch (OperationCanceledException) when (_cierre.IsCancellationRequested) { }
            catch (Exception) when (!IsDisposed) { MostrarFallo("No pudimos consultar Oracle. Pulsa Actualizar para reintentar."); }
            finally
            {
                _ocupado = false;
                if (!IsDisposed) _actualizar.Enabled = true;
            }
        }

        private void MostrarFallo(string mensaje)
        {
            _estado.ForeColor = TemaEcoAccess.Error;
            _estado.Text = mensaje;
            foreach (TarjetaDato dato in new[] { _saldo, _obtenidos, _reversados, _netos, _estaSemana, _semanaAnterior, _diferencia }) dato.Mostrar("—", "Sin datos verificados.");
            foreach (var (grafico, vacio) in new (Control, EstadoVacioEco)[] { (_evolucion, _vacioEvolucion), (_graficoSemana, _vacioSemana), (_recientes, _vacioRecientes), (_historial, _vacioHistorial) })
            {
                grafico.Visible = false;
                vacio.Visible = true;
                vacio.Mostrar(mensaje, true);
            }
            Adaptar();
        }

        private void MostrarMovimientos()
        {
            const string sinDatos = "Todavía no tienes movimientos. Los ecopuntos se acreditan cuando se verifica uno de tus depósitos.";
            var filas = _movimientos.OrderByDescending(m => m.FechaUtc).Select(m => new
            {
                Fecha = HoraColombia.DesdeUtc(m.FechaUtc).ToString("dd/MM/yyyy HH:mm", Colombia),
                Tipo = m.Tipo == TipoMovimientoEcopuntos.Credito ? "Acreditación" : "Reverso",
                Puntos = m.Puntos,
                Detalle = m.Motivo,
            }).ToList();
            foreach (var (tabla, vacio, datos) in new[] { (_recientes, _vacioRecientes, filas.Take(10).ToList()), (_historial, _vacioHistorial, filas) })
            {
                tabla.DataSource = datos;
                tabla.Visible = datos.Count > 0;
                vacio.Visible = datos.Count == 0;
                vacio.Mostrar(sinDatos, false);
            }
        }

        private static DateTime InicioSemana(DateTime dia) => dia.Date.AddDays(-(((int)dia.DayOfWeek + 6) % 7));

        private void MostrarEvolucion()
        {
            Func<DateTime, DateTime> clave = _periodo.SelectedIndex switch
            {
                1 => InicioSemana,
                2 => d => new DateTime(d.Year, d.Month, 1),
                _ => d => d.Date,
            };
            string formato = _periodo.SelectedIndex == 2 ? "MM/yyyy" : "dd/MM";
            var grupos = _movimientos.GroupBy(m => clave(HoraColombia.DesdeUtc(m.FechaUtc))).OrderBy(g => g.Key).ToList();
            MostrarColumnas(_evolucion, _vacioEvolucion, grupos.Select(g => g.Key.ToString(formato, Colombia)).ToArray(),
                grupos.Select(g => g.Where(m => m.Puntos > 0).Sum(m => (long)m.Puntos)).ToArray(),
                grupos.Select(g => g.Where(m => m.Puntos < 0).Sum(m => (long)m.Puntos)).ToArray(),
                "Sin movimientos todavía. Aquí verás la evolución cuando se acredite tu primer depósito verificado.");
        }

        private void MostrarSemana()
        {
            DateTime hoy = HoraColombia.DesdeUtc(DateTime.UtcNow).Date, lunes = InicioSemana(hoy);
            long Puntos(DateTime desde, DateTime hasta) => _movimientos.Where(m => HoraColombia.DesdeUtc(m.FechaUtc) >= desde && HoraColombia.DesdeUtc(m.FechaUtc) < hasta).Sum(m => (long)m.Puntos);
            long actual = Puntos(lunes, lunes.AddDays(7)), anterior = Puntos(lunes.AddDays(-7), lunes);
            _estaSemana.Mostrar(actual.ToString("N0", Colombia), $"Desde el lunes {lunes:dd/MM}.");
            _semanaAnterior.Mostrar(anterior.ToString("N0", Colombia), $"Semana del {lunes.AddDays(-7):dd/MM}.");
            _diferencia.Mostrar((actual - anterior).ToString("+#,0;-#,0;0", Colombia), "Puntos netos frente a la semana anterior.");
            var dias = Enumerable.Range(0, 7).Select(i => lunes.AddDays(i)).ToArray();
            MostrarColumnas(_graficoSemana, _vacioSemana, dias.Select(d => d.ToString("ddd dd", Colombia)).ToArray(),
                dias.Select(d => _movimientos.Where(m => HoraColombia.DesdeUtc(m.FechaUtc).Date == d && m.Puntos > 0).Sum(m => (long)m.Puntos)).ToArray(),
                dias.Select(d => _movimientos.Where(m => HoraColombia.DesdeUtc(m.FechaUtc).Date == d && m.Puntos < 0).Sum(m => (long)m.Puntos)).ToArray(),
                "Sin actividad esta semana. Registra un residuo y, cuando se verifique, tus puntos aparecerán aquí.");
        }

        private static void MostrarColumnas(CartesianChart grafico, EstadoVacioEco vacio, string[] etiquetas, long[] acreditados, long[] reversos, string sinDatos)
        {
            bool hay = acreditados.Any(v => v != 0) || reversos.Any(v => v != 0);
            grafico.Visible = hay;
            vacio.Visible = !hay;
            vacio.Mostrar(sinDatos, false);
            if (!hay) return;
            grafico.Series = new ISeries[]
            {
                new ColumnSeries<long> { Name = "Acreditados", Values = acreditados, Fill = new SolidColorPaint(new SKColor(0x16, 0x8A, 0x55)), MaxBarWidth = 36 },
                new ColumnSeries<long> { Name = "Reversos", Values = reversos, Fill = new SolidColorPaint(new SKColor(0xD9, 0x9A, 0x2B)), MaxBarWidth = 36 },
            };
            grafico.XAxes = new[] { new Axis { Labels = etiquetas, TextSize = 12, LabelsPaint = new SolidColorPaint(new SKColor(0x55, 0x62, 0x74)) } };
            grafico.YAxes = new[] { new Axis { TextSize = 12, MinStep = 1, LabelsPaint = new SolidColorPaint(new SKColor(0x55, 0x62, 0x74)) } };
            grafico.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed) { _cierre.Cancel(); _cierre.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
