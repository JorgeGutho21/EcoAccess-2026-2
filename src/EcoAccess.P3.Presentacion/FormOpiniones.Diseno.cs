using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Logica.Contratos;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Valoraciones» del Supervisor (skill eco-ui-supervisor): banner con el resumen del período, indicadores
    /// (promedio con estrellas, total, invitados y días con valoraciones), gráfico del promedio diario y la tabla pulida. Solo
    /// presentación: la consulta sigue en el formulario y todo lo de aquí se calcula con las filas que ya puso en la tabla.
    /// </summary>
    public sealed partial class FormOpiniones
    {
        private static readonly CultureInfo CulturaValoraciones = CultureInfo.GetCultureInfo("es-CO");
        private Label? _avisoValoraciones;

        /// <summary>Arma el módulo y devuelve la tabla y el aviso que llena la consulta.</summary>
        private (DataGridView Tabla, Label Aviso) ConstruirAdministracion()
        {
            var aviso = new Label { Text = "Consultando las valoraciones…", UseMnemonic = false };
            _avisoValoraciones = aviso;
            var banner = new BannerModulo("Valoraciones de la comunidad",
                "Promedios diarios de los últimos 30 días (UTC). Solo agregados: sin comentarios ni identificadores de las personas.", IconChar.Star)
            {
                Estado = aviso.Text,
            };
            aviso.TextChanged += (_, _) => banner.Estado = aviso.Text;
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Star, "Tendencias de valoraciones",
                "Promedios diarios de los últimos 30 días (UTC). Solo agregados: sin comentarios ni identificadores de las personas.", banner);

            var indicadores = new IndicadoresValoraciones();
            var resumen = ModuloEco.Seccion("Resumen del período", icono: IconChar.ChartPie);
            resumen.Agregar(indicadores);
            ModuloEco.Fila(raiz, resumen);

            var grafico = new GraficoPromedios { Height = TemaEcoAccess.Px(this, 250) };
            var marcoGrafico = new Panel { Height = TemaEcoAccess.Px(this, 250), BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            grafico.Dock = DockStyle.Fill;
            marcoGrafico.Controls.Add(grafico);
            var tendencia = ModuloEco.Seccion("Promedio por día", "Cada barra es el promedio de estrellas del día; el color indica qué tan bien se valoró.", icono: IconChar.ChartColumn);
            tendencia.Agregar(marcoGrafico);
            ModuloEco.Fila(raiz, tendencia);

            var tabla = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AccessibleName = "Valoraciones por día",
                Dock = DockStyle.Fill,
            };
            // Alto fijo dentro de una columna que se desplaza: la tabla tiene su barra y el módulo la suya, sin recortes.
            var marcoTabla = new Panel { Height = TemaEcoAccess.Px(this, 420), BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            marcoTabla.Controls.Add(tabla);
            var seccion = ModuloEco.Seccion("Valoraciones por día", icono: IconChar.Table);
            seccion.Agregar(marcoTabla);
            seccion.Agregar(ModuloEco.Mensaje(aviso));
            ModuloEco.Fila(raiz, seccion);

            ModuloEco.TerminarDesplazable(this, raiz, 560);
            PulirTablaValoraciones(tabla);
            tabla.DataBindingComplete += (_, _) =>
            {
                var filas = (tabla.DataSource as IEnumerable<TendenciaOpiniones>)?.ToList() ?? new List<TendenciaOpiniones>();
                indicadores.Mostrar(filas);
                grafico.Mostrar(filas);
            };
            return (tabla, aviso);
        }

        private static void PulirTablaValoraciones(DataGridView tabla)
        {
            DisenoGestion.PulirTabla(tabla, columna => columna switch
            {
                "DiaUtc" => 150, "Cantidad" => 130, "Promedio" => 220, "Invitados" => 140, _ => 0,
            }, "Aún no hay valoraciones en los últimos 30 días.", IconChar.Star);
            tabla.DataBindingComplete += (_, _) =>
            {
                foreach (var (nombre, texto) in new[] { ("DiaUtc", "Día (UTC)"), ("Cantidad", "Valoraciones"), ("Promedio", "Promedio de estrellas"), ("Invitados", "De invitados") })
                    if (tabla.Columns[nombre] is { } columna) columna.HeaderText = texto;
                if (tabla.Columns["DiaUtc"] is { } dia) dia.DefaultCellStyle.Format = "dd/MM/yyyy";
            };
            tabla.CellPainting += (_, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || tabla.Columns[e.ColumnIndex].Name != "Promedio" || e.Value is not decimal promedio) return;
                bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
                e.PaintBackground(e.CellBounds, elegida);
                int px(int n) => TemaEcoAccess.Px(tabla, n);
                int lado = px(16);
                int x = e.CellBounds.Left + px(10), y = e.CellBounds.Top + (e.CellBounds.Height - lado) / 2;
                Estrellas.Dibujar(e.Graphics, x, y, lado, px(3), promedio, tabla);
                string texto = promedio.ToString("0.0", CulturaValoraciones);
                TextRenderer.DrawText(e.Graphics, texto, DibujoDashboard.Semibold(tabla, 13.5f),
                    Rectangle.FromLTRB(x + 5 * (lado + px(3)) + px(6), e.CellBounds.Top, e.CellBounds.Right - px(4), e.CellBounds.Bottom),
                    elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.VerdeOscuro, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                e.Handled = true;
            };
        }
    }

    /// <summary>Cinco estrellas con relleno proporcional al promedio (por ejemplo 4,3 llena cuatro y un tercio de la quinta).</summary>
    internal static class Estrellas
    {
        internal static Color Tono(decimal promedio) =>
            promedio >= 4m ? TemaEcoAccess.Verde : promedio >= 3m ? DibujoDashboard.Serie[2] : promedio >= 2m ? DibujoDashboard.Pendiente : TemaEcoAccess.Error;

        internal static void Dibujar(Graphics g, int x, int y, int lado, int separacion, decimal promedio, Control escala)
        {
            Bitmap vacia = CacheIconos.Obtener(IconChar.Star, lado, TemaEcoAccess.Borde);
            Bitmap llena = CacheIconos.Obtener(IconChar.Star, lado, DibujoDashboard.Pendiente);
            for (int k = 0; k < 5; k++)
            {
                int xi = x + k * (lado + separacion);
                g.DrawImage(vacia, xi, y);
                float parte = (float)Math.Max(0m, Math.Min(1m, promedio - k));
                if (parte <= 0) continue;
                Region anterior = g.Clip;
                g.SetClip(new RectangleF(xi, y, llena.Width * parte, llena.Height), CombineMode.Intersect);
                g.DrawImage(llena, xi, y);
                g.Clip = anterior;
            }
        }
    }

    /// <summary>
    /// Indicadores del período en tarjetas: promedio ponderado por la cantidad de valoraciones (con estrellas), total, cuántas
    /// son de invitados y en cuántos días hubo valoraciones. Cuatro columnas en pantallas anchas y dos en las angostas.
    /// </summary>
    internal sealed class IndicadoresValoraciones : Control
    {
        private (string Titulo, IconChar Icono, string Valor, string Detalle, decimal? Estrellas)[] _tarjetas = Vacias();

        internal IndicadoresValoraciones()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = "Resumen del período";
            Margin = Padding.Empty;
        }

        private static (string, IconChar, string, string, decimal?)[] Vacias() => new (string, IconChar, string, string, decimal?)[]
        {
            ("Promedio del período", IconChar.Star, "—", "Sin valoraciones consultadas.", null),
            ("Valoraciones", IconChar.Comments, "—", "En los últimos 30 días.", null),
            ("De invitados", IconChar.UserSecret, "—", "Sesiones sin cuenta.", null),
            ("Días con valoraciones", IconChar.CalendarDays, "—", "De los últimos 30 días.", null),
        };

        internal void Mostrar(IReadOnlyList<TendenciaOpiniones> filas)
        {
            var cultura = CultureInfo.GetCultureInfo("es-CO");
            int total = filas.Sum(f => f.Cantidad), invitados = filas.Sum(f => f.Invitados), dias = filas.Count(f => f.Cantidad > 0);
            if (total == 0) _tarjetas = Vacias();
            else
            {
                // Promedio del período ponderado por la cantidad de cada día (no el promedio de los promedios).
                decimal promedio = filas.Sum(f => f.Promedio * f.Cantidad) / total;
                _tarjetas = new (string, IconChar, string, string, decimal?)[]
                {
                    ("Promedio del período", IconChar.Star, promedio.ToString("0.0", cultura), "de 5 estrellas, ponderado por día", promedio),
                    ("Valoraciones", IconChar.Comments, total.ToString("N0", cultura), "En los últimos 30 días.", null),
                    ("De invitados", IconChar.UserSecret, invitados.ToString("N0", cultura), $"{(decimal)invitados * 100 / total:0}% del total", null),
                    ("Días con valoraciones", IconChar.CalendarDays, dias.ToString("N0", cultura), "De los últimos 30 días.", null),
                };
            }
            AccessibleDescription = string.Join("; ", _tarjetas.Select(t => $"{t.Titulo}: {t.Valor}, {t.Detalle}"));
            Invalidate();
        }

        private int P(float n) => DibujoDashboard.P(this, n);
        private int Columnas => Width >= P(760) ? 4 : 2;
        private int AltoTarjeta => P(138);
        private int AltoNecesario => (4 / Columnas) * AltoTarjeta + (4 / Columnas - 1) * P(14);

        public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width is > 0 and < int.MaxValue / 2 ? proposedSize.Width : Width, AltoNecesario);

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && Height != AltoNecesario) Height = AltoNecesario;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int columnas = Columnas, separacion = P(14);
            int ancho = (Width - separacion * (columnas - 1)) / columnas;
            const TextFormatFlags Linea = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            for (int i = 0; i < _tarjetas.Length; i++)
            {
                var (titulo, icono, valor, detalle, estrellas) = _tarjetas[i];
                var area = new Rectangle((i % columnas) * (ancho + separacion) + 1, (i / columnas) * (AltoTarjeta + separacion) + 1, ancho - P(4), AltoTarjeta - P(5));
                DibujoDashboard.Tarjeta(g, this, area, TemaEcoAccess.Superficie, TemaEcoAccess.Borde, P(12));
                int lado = P(36), x = area.Left + P(16), y = area.Top + P(14);
                using (var ruta = RecursosEco.Redondeado(new RectangleF(x, y, lado, lado), P(9)))
                using (var fondo = new SolidBrush(TemaEcoAccess.VerdeSuave)) g.FillPath(fondo, ruta);
                Bitmap imagen = CacheIconos.Obtener(icono, lado * 55 / 100, TemaEcoAccess.Verde);
                g.DrawImage(imagen, x + (lado - imagen.Width) / 2, y + (lado - imagen.Height) / 2);
                TextRenderer.DrawText(g, titulo, DibujoDashboard.Semibold(this, 13.5f), new Rectangle(x + lado + P(10), y, area.Right - x - lado - P(20), lado), TemaEcoAccess.Texto,
                    Linea | TextFormatFlags.VerticalCenter);
                Font grande = DibujoDashboard.Semibold(this, 28f);
                int yValor = y + lado + P(8);
                Size medida = TextRenderer.MeasureText(g, valor, grande, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, valor, grande, new Point(x, yValor), TemaEcoAccess.VerdeOscuro, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                if (estrellas is { } promedio)
                {
                    int ladoEstrella = P(17);
                    Estrellas.Dibujar(g, x + medida.Width + P(10), yValor + (medida.Height - ladoEstrella) / 2, ladoEstrella, P(3), promedio, this);
                }
                Font pequena = DibujoDashboard.Fuente(this, 12f);
                TextRenderer.DrawText(g, detalle, pequena, new Rectangle(x, yValor + medida.Height + P(4), area.Right - x - P(12), pequena.Height + 2), TemaEcoAccess.Suave, Linea);
            }
        }
    }

    /// <summary>Barras del promedio de estrellas por día (eje de 0 a 5), coloreadas según el valor, con la fecha debajo.</summary>
    internal sealed class GraficoPromedios : Control
    {
        private TendenciaOpiniones[] _dias = Array.Empty<TendenciaOpiniones>();

        internal GraficoPromedios()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.Chart;
            AccessibleName = "Promedio de estrellas por día";
        }

        internal void Mostrar(IEnumerable<TendenciaOpiniones> dias)
        {
            _dias = dias.Where(d => d.Cantidad > 0).OrderBy(d => d.DiaUtc).ToArray();
            var cultura = CultureInfo.GetCultureInfo("es-CO");
            AccessibleDescription = string.Join("; ", _dias.Select(d => $"{d.DiaUtc:dd/MM}: {d.Promedio.ToString("0.0", cultura)} de 5 con {d.Cantidad} valoraciones"));
            Invalidate();
        }

        private int P(float n) => DibujoDashboard.P(this, n);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Font fuente = DibujoDashboard.Fuente(this, 11.5f);
            if (_dias.Length == 0)
            {
                DibujoDashboard.Texto(g, "Aún no hay valoraciones para graficar en los últimos 30 días.", DibujoDashboard.Fuente(this, 13), TemaEcoAccess.Suave,
                    ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            var cultura = CultureInfo.GetCultureInfo("es-CO");
            int anchoEje = TextRenderer.MeasureText(g, "5 ★", fuente, Size.Empty, TextFormatFlags.NoPadding).Width + P(8);
            int altoEtiqueta = fuente.Height + P(8);
            var trazo = Rectangle.FromLTRB(anchoEje + P(6), fuente.Height + P(6), Width - P(4), Height - altoEtiqueta);
            using (var guia = new Pen(TemaEcoAccess.Borde))
                for (int i = 0; i <= 5; i++)
                {
                    int y = trazo.Bottom - trazo.Height * i / 5;
                    g.DrawLine(guia, trazo.Left, y, trazo.Right, y);
                    TextRenderer.DrawText(g, i == 0 ? "0" : $"{i} ★", fuente, new Rectangle(0, y - fuente.Height / 2, anchoEje, fuente.Height), TemaEcoAccess.Suave, TextFormatFlags.Right | TextFormatFlags.NoPadding);
                }
            int n = _dias.Length;
            float hueco = (float)trazo.Width / n;
            float barra = Math.Max(3f, Math.Min(hueco * 0.55f, P(34)));
            int cadaCuanto = Math.Max(1, (int)Math.Ceiling(TextRenderer.MeasureText("00/00", fuente, Size.Empty, TextFormatFlags.NoPadding).Width * 1.2f / hueco));
            bool cifras = hueco >= TextRenderer.MeasureText("0,0", fuente, Size.Empty, TextFormatFlags.NoPadding).Width + P(4);
            for (int i = 0; i < n; i++)
            {
                TendenciaOpiniones dia = _dias[i];
                float centro = trazo.Left + hueco * (i + 0.5f);
                float alto = Math.Max(P(3), trazo.Height * (float)Math.Min(5m, dia.Promedio) / 5f);
                var rect = new RectangleF(centro - barra / 2, trazo.Bottom - alto, barra, alto);
                using (var ruta = RecursosEco.Redondeado(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height + P(6)), Math.Min(P(6), barra / 2)))
                using (var pincel = new SolidBrush(Estrellas.Tono(dia.Promedio)))
                {
                    g.SetClip(new RectangleF(rect.X - 1, trazo.Top - P(2), rect.Width + 2, trazo.Height + P(2)));
                    g.FillPath(pincel, ruta);
                    g.ResetClip();
                }
                if (cifras)
                    TextRenderer.DrawText(g, dia.Promedio.ToString("0.0", cultura), fuente, new Rectangle((int)(centro - hueco / 2), (int)rect.Y - fuente.Height - P(2), (int)hueco, fuente.Height),
                        TemaEcoAccess.Texto, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                if (i % cadaCuanto == 0)
                    TextRenderer.DrawText(g, dia.DiaUtc.ToString("dd/MM", cultura), fuente, new Rectangle((int)(centro - hueco * cadaCuanto / 2f), trazo.Bottom + P(5), (int)(hueco * cadaCuanto), altoEtiqueta),
                        TemaEcoAccess.Suave, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            }
        }
    }
}
