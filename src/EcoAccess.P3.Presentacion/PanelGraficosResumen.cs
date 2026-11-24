using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Logica.Contratos;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Gráficos del dashboard. Solo representa respuestas ya autorizadas; no consulta ni modifica la base de datos.
    /// Cada gráfico tiene texto alternativo con los mismos datos para lectores de pantalla.
    /// </summary>
    internal sealed class PanelGraficosResumen : UserControl
    {
        // Tonos de la paleta Eco para series: verde de marca, verde oscuro, hoja, menta, gris pizarra y ámbar.
        private static readonly SKColor[] Serie =
        new SKColor[]
    {
        new(0x16, 0x8A, 0x55),
        new(0x10, 0x5C, 0x3B),
        new(0x5F, 0xB8, 0x8A),
        new(0xB7, 0xE4, 0xCB),
        new(0x64, 0x74, 0x8B),
        new(0xD9, 0x9A, 0x2B)
    };

        // Radio interior del anillo en píxeles: con 56 y un lienzo bajo el anillo quedaba sin grosor y parecía vacío.
        private const int RadioAnillo = 28;

        private readonly TableLayoutPanel _estructura = new() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        private readonly CartesianChart _dias = new() { Dock = DockStyle.Fill, BackColor = Color.White, EasingFunction = null, TooltipPosition = TooltipPosition.Top };
        private readonly PieChart _materiales = new() { Dock = DockStyle.Fill, BackColor = Color.White, EasingFunction = null, LegendPosition = LegendPosition.Bottom };
        private readonly PieChart _verificacion = new() { Dock = DockStyle.Fill, BackColor = Color.White, EasingFunction = null, LegendPosition = LegendPosition.Bottom };
        private readonly Label _estadoDias = Estado(), _estadoMateriales = Estado(), _estadoVerificacion = Estado();
        private readonly Label _detalleDias = Detalle(), _detalleMateriales = Detalle(), _detalleVerificacion = Detalle();
        private readonly TarjetaEco _tarjetaDias, _tarjetaMateriales, _tarjetaVerificacion;
        private int _modo = -1;

        /// <summary>Alto de cada fila de tarjetas (360 por defecto). El dashboard institucional usa más para que el anillo de
        /// materiales no quede aplastado por su leyenda.</summary>
        internal int AltoFila { get; set; } = 360;

        internal PanelGraficosResumen()
        {
            Dock = DockStyle.Top;
            BackColor = TemaEcoAccess.Fondo;
            _tarjetaDias = CrearTarjeta("Actividad por día o semana", _dias, _estadoDias, _detalleDias);
            _tarjetaMateriales = CrearTarjeta("Depósitos por material", _materiales, _estadoMateriales, _detalleMateriales);
            _tarjetaVerificacion = CrearTarjeta("Estado de verificación", _verificacion, _estadoVerificacion, _detalleVerificacion);
            foreach (PieChart grafico in new[] { _materiales, _verificacion })
            {
                grafico.LegendTextPaint = new SolidColorPaint(new SKColor(0x1E, 0x29, 0x3B)) { SKTypeface = SKTypeface.FromFamilyName("Segoe UI") };
                grafico.LegendTextSize = 13;
            }
            Controls.Add(_estructura);
            SizeChanged += (_, _) => Adaptar();
            Limpiar("Consulta los datos para ver los gráficos.");
        }

        private static Label Estado() => new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = TemaEcoAccess.Suave, BackColor = Color.White };
        private static Label Detalle() => new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, BackColor = Color.White, Margin = new Padding(0, 8, 0, 0) };

        private TarjetaEco CrearTarjeta(string titulo, Control grafico, Label estado, Label detalle)
        {
            var tarjeta = new TarjetaEco { Dock = DockStyle.Fill };
            var interior = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.White };
            interior.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            interior.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            interior.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            interior.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
            var lienzo = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            grafico.AccessibleName = titulo;
            grafico.TabStop = false;
            lienzo.Controls.Add(grafico);
            lienzo.Controls.Add(estado);
            interior.Controls.Add(lienzo, 0, 1);
            interior.Controls.Add(detalle, 0, 2);
            interior.SizeChanged += (_, _) => detalle.MaximumSize = new Size(Math.Max(150, interior.ClientSize.Width), 0);
            tarjeta.Controls.Add(interior);
            return tarjeta;
        }

        /// <summary>Tres columnas en pantallas anchas; en estrechas, cada gráfico ocupa su propia fila.</summary>
        private void Adaptar()
        {
            int modo = ClientSize.Width >= TemaEcoAccess.Px(this, 1100) ? 3 : ClientSize.Width >= TemaEcoAccess.Px(this, 700) ? 2 : 1;
            if (modo == _modo) return;
            _modo = modo;
            SuspendLayout();
            _estructura.SuspendLayout();
            _estructura.Controls.Clear();
            // La expansión de columnas persiste entre modos: sin reiniciarla, el modo de tres columnas pierde una celda.
            _estructura.SetColumnSpan(_tarjetaDias, 1);
            _estructura.ColumnStyles.Clear();
            _estructura.RowStyles.Clear();
            int alto = TemaEcoAccess.Px(this, AltoFila);
            if (modo == 3)
            {
                _estructura.ColumnCount = 3; _estructura.RowCount = 1;
                foreach (float p in new[] { 46f, 27f, 27f }) _estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, p));
                _estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                _estructura.Controls.Add(_tarjetaDias, 0, 0); _estructura.Controls.Add(_tarjetaMateriales, 1, 0); _estructura.Controls.Add(_tarjetaVerificacion, 2, 0);
                Height = alto;
            }
            else if (modo == 2)
            {
                _estructura.ColumnCount = 2; _estructura.RowCount = 2;
                _estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); _estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                _estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); _estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                _estructura.Controls.Add(_tarjetaDias, 0, 0); _estructura.SetColumnSpan(_tarjetaDias, 2);
                _estructura.Controls.Add(_tarjetaMateriales, 0, 1); _estructura.Controls.Add(_tarjetaVerificacion, 1, 1);
                Height = alto * 2;
            }
            else
            {
                _estructura.ColumnCount = 1; _estructura.RowCount = 3;
                _estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                for (int i = 0; i < 3; i++) _estructura.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
                _estructura.Controls.Add(_tarjetaDias, 0, 0); _estructura.Controls.Add(_tarjetaMateriales, 0, 1); _estructura.Controls.Add(_tarjetaVerificacion, 0, 2);
                Height = alto * 3;
            }
            _estructura.ResumeLayout(true);
            ResumeLayout(true);
        }

        internal void Limpiar(string mensaje)
        {
            foreach (var (grafico, estado, detalle) in new (Control, Label, Label)[] { (_dias, _estadoDias, _detalleDias), (_materiales, _estadoMateriales, _detalleMateriales), (_verificacion, _estadoVerificacion, _detalleVerificacion) })
            {
                grafico.AccessibleDescription = string.Empty;
                grafico.Visible = false;
                estado.Visible = true;
                estado.Text = mensaje;
                detalle.Text = string.Empty;
            }
            _dias.Series = Array.Empty<ISeries>();
            _materiales.Series = Array.Empty<ISeries>();
            _verificacion.Series = Array.Empty<ISeries>();
        }

        internal void Mostrar(IndicadoresPanel indicadores, IReadOnlyList<FilaAgrupada> dias, IReadOnlyList<FilaAgrupada> materiales, DateOnly desde, DateOnly hasta)
            => MostrarDatos(indicadores.Depositos, indicadores.Verificados, dias, materiales, desde, hasta);

        internal void MostrarInforme(InformeInstitucional informe, IReadOnlyList<FilaAgrupada> periodos, DateOnly desde, DateOnly hasta)
            => MostrarDatos(informe.Actual.Depositos, informe.Actual.Verificados, periodos, informe.Materiales, desde, hasta);

        internal void MostrarPersonal(EstadisticasPersonales datos, bool semanal)
        {
            IReadOnlyList<FilaAgrupada> filas = datos.Evolucion;
            if (semanal)
                filas = filas.Where(f => f.Dia is not null)
                    .GroupBy(f => f.Dia!.Value.AddDays(-(((int)f.Dia.Value.DayOfWeek + 6) % 7)))
                    .Select(g => new FilaAgrupada(null, g.Key, "Semana " + g.Key.ToString("dd/MM"), g.Sum(f => f.Depositos),
                        g.Sum(f => f.Verificados), g.Sum(f => f.ConPesoMedido), g.Sum(f => f.KgMedidos),
                        g.Sum(f => f.ConPesoEstimado), g.Sum(f => f.KgEstimados))).ToList();
            DateOnly desde = DateOnly.FromDateTime(datos.Periodo.DesdeUtc.AddHours(-5));
            DateOnly hasta = DateOnly.FromDateTime(datos.Periodo.HastaUtc.AddTicks(-1).AddHours(-5));
            MostrarDatos(datos.Actual.Depositos.Total, datos.Actual.Depositos.Verificados, filas, datos.Materiales, desde, hasta);
            _detalleDias.Text = $"{desde:dd/MM}–{hasta:dd/MM/yyyy} · actividad personal por {(semanal ? "semana (lunes)" : "día")} de Colombia. Las semanas extremas pueden ser parciales.";
            _detalleMateriales.Text = $"Material declarado o verificado · {datos.Actual.Depositos.Total:N0} depósitos en el período personal.";
        }

        private void MostrarDatos(long depositos, long verificados, IReadOnlyList<FilaAgrupada> dias,
            IReadOnlyList<FilaAgrupada> materiales, DateOnly desde, DateOnly hasta)
        {
            // Depósitos por día (solo días con registros).
            var filas = dias.OrderBy(d => d.Dia).ToArray();
            _detalleDias.Text = $"{desde:dd/MM}–{hasta:dd/MM/yyyy} · hora de Colombia. Cuenta depósitos detectados, no aperturas ni peso.";
            Alternar(_dias, _estadoDias, filas.Length > 0, "Sin depósitos en el período consultado. El gráfico aparecerá con el primer registro.");
            var etiquetas = new SolidColorPaint(new SKColor(0x55, 0x62, 0x74)) { SKTypeface = SKTypeface.FromFamilyName("Segoe UI") };
            var guias = new SolidColorPaint(new SKColor(0xE2, 0xE8, 0xE5)) { StrokeThickness = 1 };
            _dias.XAxes = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[] { new Axis { Labels = filas.Select(d => d.Dia?.ToString("dd/MM") ?? d.Etiqueta).ToArray(), LabelsPaint = etiquetas, TextSize = 12, MinStep = 1, SeparatorsPaint = null } };
            _dias.YAxes = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[] { new Axis { MinLimit = 0, MinStep = 1, LabelsPaint = etiquetas, TextSize = 12, SeparatorsPaint = guias } };
            _dias.Series = filas.Length == 0 ? Array.Empty<ISeries>() :
            new ISeries[]
    {
        new ColumnSeries<int> { Name = "Depósitos", Values = filas.Select(d => d.Depositos).ToArray(), Fill = new SolidColorPaint(Serie[0]), Rx = 6, Ry = 6, MaxBarWidth = 28 },
        new ColumnSeries<int> { Name = "Verificados", Values = filas.Select(d => d.Verificados).ToArray(), Fill = new SolidColorPaint(Serie[3]), Rx = 6, Ry = 6, MaxBarWidth = 28 }
    };
            _dias.AccessibleDescription = string.Join("; ", filas.Select(d => $"{d.Etiqueta}: {d.Depositos} depósitos, {d.Verificados} verificados"));

            // Distribución por material (los seis más frecuentes; el resto se agrupa como «Otros»).
            var orden = materiales.Where(m => m.Depositos > 0).OrderByDescending(m => m.Depositos).ToList();
            var visibles = orden.Take(5).Select(m => (m.Etiqueta, m.Depositos)).ToList();
            int otros = orden.Skip(5).Sum(m => m.Depositos);
            if (otros > 0) visibles.Add(("Otros", otros));
            Alternar(_materiales, _estadoMateriales, visibles.Count > 0, "Sin depósitos registrados todavía.");
            _materiales.Series = visibles.Select((m, i) => (ISeries)new PieSeries<int> { Name = m.Etiqueta, Values = new int[] { m.Depositos }, InnerRadius = RadioAnillo, Fill = new SolidColorPaint(Serie[i % Serie.Length]) }).ToArray();
            _detalleMateriales.Text = $"Material declarado o verificado · total consultado: {orden.Sum(m => m.Depositos):N0} depósitos.";
            _materiales.AccessibleDescription = string.Join("; ", visibles.Select(m => $"{m.Etiqueta}: {m.Depositos}"));

            // Verificados frente a pendientes.
            long pendientes = Math.Max(0, depositos - verificados);
            Alternar(_verificacion, _estadoVerificacion, depositos > 0, "Sin depósitos para verificar.");
            _verificacion.Series = depositos == 0 ? Array.Empty<ISeries>() :
            new ISeries[]
    {
        new PieSeries<long> { Name = "Verificados", Values = new long[] { verificados }, InnerRadius = RadioAnillo, Fill = new SolidColorPaint(Serie[0]) },
        new PieSeries<long> { Name = "Pendientes", Values = new long[] { pendientes }, InnerRadius = RadioAnillo, Fill = new SolidColorPaint(Serie[5]) }
    };
            _detalleVerificacion.Text = "Un operario o administrador confirma el material antes de otorgar ecopuntos.";
            _verificacion.AccessibleDescription = $"Verificados: {verificados}; pendientes: {pendientes}";
        }

        private static void Alternar(Control grafico, Label estado, bool hayDatos, string vacio)
        {
            grafico.Visible = hayDatos;
            estado.Visible = !hayDatos;
            estado.Text = vacio;
        }
    }
}
