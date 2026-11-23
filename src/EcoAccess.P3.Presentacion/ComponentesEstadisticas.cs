using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>Una parte de un desglose (nombre, cantidad y color de la serie).</summary>
    internal sealed record ParteResumen(string Nombre, int Valor, Color Color);

    /// <summary>
    /// Desglose de un indicador: una barra proporcional (solo si las partes suman exactamente el total, para no sugerir un
    /// reparto que no existe) y la leyenda con las cantidades.
    /// </summary>
    internal sealed record DesgloseResumen(string Titulo, ParteResumen[] Partes, int? Total);

    /// <summary>Cifra grande de una tarjeta; <see cref="Estimada"/> la marca como aproximada (no es una medición).</summary>
    internal sealed record CifraResumen(string Valor, string Unidad, string? Rotulo = null, string? Detalle = null, bool Estimada = false);

    internal sealed record IndicadorResumen(string Titulo, IconChar Icono, CifraResumen[] Cifras, DesgloseResumen[] Desgloses, string? Nota = null, bool NotaAviso = false);

    /// <summary>
    /// Lee el resumen que escribe <c>FormEstadisticas</c> (una línea por categoría) y lo convierte en indicadores para las
    /// tarjetas. Solo presentación: los números salen del mismo texto que ya calculó el formulario. Si el formato no se
    /// reconoce, devuelve null y la vista muestra el texto tal cual.
    /// </summary>
    internal static class LectorResumenEstadisticas
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

        // Serie: logrado (verde), en curso (verde medio / pizarra), pendiente (ámbar) y fallido (rojo), todos del tema.
        private static Color Logrado => TemaEcoAccess.Verde;
        private static Color Oscuro => TemaEcoAccess.VerdeOscuro;
        private static Color Medio => DibujoDashboard.Serie[2];
        private static Color Pizarra => DibujoDashboard.Serie[4];
        private static Color Pendiente => DibujoDashboard.Pendiente;
        private static Color Fallido => TemaEcoAccess.Error;

        internal sealed record Lectura(IndicadorResumen[] Indicadores, string? Aclaracion, int Depositos, int Verificados, int Solicitudes);

        internal static Lectura? Leer(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            string[] lineas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string? Linea(string inicio) => lineas.FirstOrDefault(l => l.StartsWith(inicio, StringComparison.Ordinal));
            string? li = Linea("Interacciones"), lo = Linea("Órdenes"), le = Linea("Eventos"), ld = Linea("Depósitos"), lp = Linea("Peso");
            if (li is null || lo is null || le is null || ld is null || lp is null) return null;
            try
            {
                int iTotal = N(li, "Interacciones (solicitudes):"), iIni = N(li, "Iniciadas"), iAut = N(li, "autorizadas"), iCon = N(li, "con depósito"),
                    iSin = N(li, "sin depósito"), iRec = N(li, "rechazadas"), iTel = N(li, "Por Telegram"), iMan = N(li, "manuales"), iSim = N(li, "simulador");
                int oTotal = N(lo, "Órdenes de apertura emitidas:"), oVig = N(lo, "Vigentes"), oCer = N(lo, "cerradas"), oVen = N(lo, "vencidas"), oFal = N(lo, "fallidas"),
                    oConf = N(lo, "Apertura confirmada por el sensor:"), oSinConf = N(lo, "sin confirmar:"), oDep = N(lo, "Con depósito:");
                int eTotal = N(le, "Eventos de las canecas:"), eApl = N(le, "Aplicados"), eRev = N(le, "en revisión"), eFal = N(le, "fallos");
                int dTotal = N(ld, "Depósitos detectados:"), dVer = N(ld, "Verificados"), dCoi = N(ld, "coinciden"), dDis = N(ld, "discrepan"), dSin = N(ld, "sin verificar");
                Match medido = Regex.Match(lp, @"Peso medido con báscula:\s*([\d.,]+)\s*kg en (\d+)");
                Match estimado = Regex.Match(lp, @"Peso estimado \(no es una medición\):\s*([\d.,]+)\s*kg en (\d+)");
                if (!medido.Success || !estimado.Success) return null;
                string? aclaracion = lineas.LastOrDefault(l => l.StartsWith("El material", StringComparison.Ordinal));

                var indicadores = new[]
                {
                    new IndicadorResumen("Interacciones (solicitudes)", IconChar.Comments,
                        new[] { new CifraResumen(F(iTotal), iTotal == 1 ? "solicitud" : "solicitudes") },
                        new[]
                        {
                            new DesgloseResumen("Estado de la solicitud", new[]
                            {
                                new ParteResumen("Con depósito", iCon, Logrado), new ParteResumen("Autorizadas", iAut, Oscuro),
                                new ParteResumen("Iniciadas", iIni, Pizarra), new ParteResumen("Sin depósito", iSin, Pendiente),
                                new ParteResumen("Rechazadas", iRec, Fallido),
                            }, iTotal),
                            new DesgloseResumen("Canal", new[]
                            {
                                new ParteResumen("Telegram", iTel, Logrado), new ParteResumen("Simulador", iSim, Medio), new ParteResumen("Manuales", iMan, Oscuro),
                            }, iTotal),
                        }),
                    new IndicadorResumen("Órdenes de apertura emitidas", IconChar.Key,
                        new[] { new CifraResumen(F(oTotal), oTotal == 1 ? "orden" : "órdenes", Detalle: $"{F(oDep)} con depósito") },
                        new[]
                        {
                            new DesgloseResumen("Estado de la orden", new[]
                            {
                                new ParteResumen("Cerradas", oCer, Logrado), new ParteResumen("Vigentes", oVig, Medio),
                                new ParteResumen("Vencidas", oVen, Pendiente), new ParteResumen("Fallidas", oFal, Fallido),
                            }, oTotal),
                            new DesgloseResumen("Apertura confirmada por el sensor", new[]
                            {
                                new ParteResumen("Confirmada", oConf, Logrado), new ParteResumen("Sin confirmar", oSinConf, Pendiente),
                            }, oTotal),
                        }),
                    new IndicadorResumen("Eventos de las canecas", IconChar.TrashCan,
                        new[] { new CifraResumen(F(eTotal), eTotal == 1 ? "evento" : "eventos") },
                        new[]
                        {
                            new DesgloseResumen("Aplicados + en revisión = total", new[]
                            {
                                new ParteResumen("Aplicados", eApl, Logrado), new ParteResumen("En revisión", eRev, Pendiente),
                            }, eTotal),
                        },
                        eFal == 0 ? "Sin eventos de fallo en el período." : $"{F(eFal)} {(eFal == 1 ? "fallo" : "fallos")}: pueden estar aplicados o en revisión.",
                        NotaAviso: eFal > 0),
                    new IndicadorResumen("Depósitos detectados", IconChar.Database,
                        new[] { new CifraResumen(F(dTotal), dTotal == 1 ? "depósito" : "depósitos") },
                        new[]
                        {
                            new DesgloseResumen("Verificación", new[]
                            {
                                new ParteResumen("Verificados", dVer, Logrado), new ParteResumen("Sin verificar", dSin, Pendiente),
                            }, dTotal),
                            new DesgloseResumen("Entre los verificados", new[]
                            {
                                new ParteResumen("Coinciden", dCoi, Oscuro), new ParteResumen("Discrepan", dDis, Fallido),
                            }, dVer),
                        }),
                    new IndicadorResumen("Peso de los depósitos", IconChar.ScaleBalanced,
                        new[]
                        {
                            new CifraResumen(medido.Groups[1].Value, "kg", "Medido con báscula", $"en {F(int.Parse(medido.Groups[2].Value, Cultura))} depósitos"),
                            new CifraResumen(estimado.Groups[1].Value, "kg", "Estimado", $"en {F(int.Parse(estimado.Groups[2].Value, Cultura))} depósitos", Estimada: true),
                        },
                        Array.Empty<DesgloseResumen>(),
                        "El peso estimado no es una medición y nunca se suma al medido."),
                };
                return new Lectura(indicadores, aclaracion, dTotal, dVer, iTotal);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static string F(int n) => n.ToString("N0", Cultura);

        private static int N(string linea, string clave)
        {
            Match m = Regex.Match(linea, Regex.Escape(clave) + @"\s*(\d+)");
            if (!m.Success) throw new FormatException(clave);
            return int.Parse(m.Groups[1].Value, Cultura);
        }

        /// <summary>Indicadores vacíos (antes de la primera consulta): la misma estructura con «—».</summary>
        internal static IndicadorResumen[] Vacios() => new[]
        {
            new IndicadorResumen("Interacciones (solicitudes)", IconChar.Comments, new[] { new CifraResumen("—", "solicitudes") }, Array.Empty<DesgloseResumen>()),
            new IndicadorResumen("Órdenes de apertura emitidas", IconChar.Key, new[] { new CifraResumen("—", "órdenes") }, Array.Empty<DesgloseResumen>()),
            new IndicadorResumen("Eventos de las canecas", IconChar.TrashCan, new[] { new CifraResumen("—", "eventos") }, Array.Empty<DesgloseResumen>()),
            new IndicadorResumen("Depósitos detectados", IconChar.Database, new[] { new CifraResumen("—", "depósitos") }, Array.Empty<DesgloseResumen>()),
            new IndicadorResumen("Peso de los depósitos", IconChar.ScaleBalanced,
                new[] { new CifraResumen("—", "kg", "Medido con báscula"), new CifraResumen("—", "kg", "Estimado", Estimada: true) }, Array.Empty<DesgloseResumen>()),
        };
    }

    /// <summary>
    /// Tarjetas del resumen de Estadísticas dibujadas en un solo control: encabezado verde suave con icono, cifra grande,
    /// barras proporcionales y leyenda con cantidades. Se reparten en 3 + 2 columnas (ancho), 2 (medio) o 1 (angosto) y el
    /// control toma el alto que necesitan, así el módulo se desplaza entero en lugar de recortar.
    /// </summary>
    internal sealed class ResumenEstadisticas : Control
    {
        private IndicadorResumen[] _indicadores = LectorResumenEstadisticas.Vacios();
        private string? _textoLibre;
        private string _notaVacia = "Consulta un período para ver el resumen.";

        internal ResumenEstadisticas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = "Resumen de la consulta";
            Margin = Padding.Empty;
        }

        /// <summary>Muestra los indicadores leídos; con null y un texto, el texto tal cual (formato no reconocido).</summary>
        internal void Mostrar(IndicadorResumen[]? indicadores, string textoOriginal)
        {
            _indicadores = indicadores ?? LectorResumenEstadisticas.Vacios();
            _textoLibre = indicadores is null && !string.IsNullOrWhiteSpace(textoOriginal) ? textoOriginal : null;
            AccessibleDescription = string.IsNullOrWhiteSpace(textoOriginal) ? _notaVacia : textoOriginal;
            Ajustar();
            Invalidate();
        }

        private int P(float n) => DibujoDashboard.P(this, n);

        public override Size GetPreferredSize(Size proposedSize)
        {
            int ancho = proposedSize.Width is > 0 and < int.MaxValue / 2 ? proposedSize.Width : Width;
            return new Size(ancho, AltoPara(ancho));
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Ajustar();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); Ajustar(); Invalidate(); }

        private void Ajustar()
        {
            if (Width <= 0) return;
            int alto = AltoPara(Width);
            if (alto != Height) Height = alto;
        }

        private int Separacion => P(14);

        /// <summary>Filas de tarjetas según el ancho: índices de <see cref="_indicadores"/> por fila.</summary>
        private int[][] Filas(int ancho)
        {
            int n = _indicadores.Length;
            if (ancho >= P(1000) && n == 5) return new[] { new[] { 0, 1, 2 }, new[] { 3, 4 } };
            if (ancho >= P(620)) return Enumerable.Range(0, (n + 1) / 2).Select(f => Enumerable.Range(f * 2, Math.Min(2, n - f * 2)).ToArray()).ToArray();
            return Enumerable.Range(0, n).Select(i => new[] { i }).ToArray();
        }

        private IEnumerable<(int Indice, Rectangle Area)> Distribuir(int ancho, Func<int, int, int> altoTarjeta)
        {
            int y = 0;
            foreach (int[] fila in Filas(ancho))
            {
                int anchoTarjeta = (ancho - Separacion * (fila.Length - 1)) / fila.Length;
                int alto = fila.Max(i => altoTarjeta(i, anchoTarjeta));
                for (int k = 0; k < fila.Length; k++)
                    yield return (fila[k], new Rectangle(k * (anchoTarjeta + Separacion), y, k == fila.Length - 1 ? ancho - k * (anchoTarjeta + Separacion) : anchoTarjeta, alto));
                y += alto + Separacion;
            }
        }

        private int AltoPara(int ancho)
        {
            if (ancho <= 0) return P(200);
            if (_textoLibre is not null)
                return TextRenderer.MeasureText(_textoLibre, DibujoDashboard.Fuente(this, 13.5f), new Size(ancho - P(32), int.MaxValue), TextFormatFlags.WordBreak).Height + P(36);
            int fondo = 0;
            foreach (var (indice, area) in Distribuir(ancho, (i, w) => Tarjeta(null, _indicadores[i], new Rectangle(0, 0, w, 0))))
                fondo = Math.Max(fondo, area.Bottom);
            return fondo + P(4);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_textoLibre is not null)
            {
                DibujoDashboard.Texto(g, _textoLibre, DibujoDashboard.Fuente(this, 13.5f), TemaEcoAccess.Texto, Rectangle.FromLTRB(P(16), P(16), Width - P(16), Height));
                return;
            }
            foreach (var (indice, area) in Distribuir(Width, (i, w) => Tarjeta(null, _indicadores[i], new Rectangle(0, 0, w, 0))))
                Tarjeta(g, _indicadores[indice], Rectangle.FromLTRB(area.Left + 1, area.Top + 1, area.Right - P(4), area.Bottom - P(4)));
        }

        /// <summary>Mide (sin <paramref name="g"/>) o dibuja una tarjeta; devuelve su alto.</summary>
        private int Tarjeta(Graphics? g, IndicadorResumen indicador, Rectangle area)
        {
            const TextFormatFlags Linea = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            int margen = P(18), anchoInterior = area.Width - 2 * margen;
            int altoEncabezado = P(50), radio = P(12);
            if (g is not null)
            {
                DibujoDashboard.Tarjeta(g, this, area, TemaEcoAccess.Superficie, TemaEcoAccess.Borde, radio);
                // Encabezado verde suave con las esquinas de arriba redondeadas (el mismo trazo de la tarjeta, recortado).
                using (var ruta = RecursosEco.Redondeado(area, radio))
                using (var fondo = new SolidBrush(TemaEcoAccess.VerdeSuave))
                using (var borde = new Pen(TemaEcoAccess.Borde))
                {
                    Region anterior = g.Clip;
                    g.SetClip(new Rectangle(area.Left, area.Top, area.Width + 1, altoEncabezado));
                    g.FillPath(fondo, ruta);
                    g.DrawPath(borde, ruta);
                    g.Clip = anterior;
                    g.DrawLine(borde, area.Left, area.Top + altoEncabezado, area.Right, area.Top + altoEncabezado);
                }
                Bitmap icono = CacheIconos.Obtener(indicador.Icono, P(20), TemaEcoAccess.Verde);
                g.DrawImage(icono, area.Left + margen, area.Top + (altoEncabezado - icono.Height) / 2);
                var titulo = Rectangle.FromLTRB(area.Left + margen + icono.Width + P(10), area.Top, area.Right - margen, area.Top + altoEncabezado);
                TextRenderer.DrawText(g, indicador.Titulo, DibujoDashboard.Semibold(this, 15f), titulo, TemaEcoAccess.VerdeOscuro, Linea | TextFormatFlags.VerticalCenter);
            }
            int y = area.Top + altoEncabezado + P(14);

            // Cifras grandes, una al lado de la otra (el peso medido y el estimado nunca se suman).
            int columnas = indicador.Cifras.Length;
            int anchoCifra = (anchoInterior - P(16) * (columnas - 1)) / Math.Max(1, columnas);
            int altoCifras = 0;
            for (int k = 0; k < columnas; k++)
            {
                CifraResumen cifra = indicador.Cifras[k];
                int x = area.Left + margen + k * (anchoCifra + P(16)), yc = y;
                if (g is not null && k > 0)
                    using (var separador = new Pen(TemaEcoAccess.Borde)) g.DrawLine(separador, x - P(8), y + P(2), x - P(8), y + altoCifras - P(2));
                if (cifra.Rotulo is not null)
                {
                    Font fuenteRotulo = DibujoDashboard.Semibold(this, 12f);
                    int altoRotulo = fuenteRotulo.Height;
                    if (g is not null)
                    {
                        if (cifra.Estimada)
                        {
                            // Pastilla ámbar «Estimado · no es una medición» para que no se lea como peso real.
                            string texto = cifra.Rotulo + " · no es una medición";
                            Size medida = TextRenderer.MeasureText(g, texto, fuenteRotulo, Size.Empty, TextFormatFlags.NoPadding);
                            var pastilla = new Rectangle(x, yc, Math.Min(anchoCifra, medida.Width + P(16)), altoRotulo + P(4));
                            using (var ruta = RecursosEco.Redondeado(pastilla, pastilla.Height / 2f))
                            using (var fondo = new SolidBrush(TemaEcoAccess.AdvertenciaSuave))
                                g.FillPath(fondo, ruta);
                            TextRenderer.DrawText(g, texto, fuenteRotulo, Rectangle.Inflate(pastilla, -P(8), 0), TemaEcoAccess.Advertencia, Linea | TextFormatFlags.VerticalCenter);
                        }
                        else
                            TextRenderer.DrawText(g, cifra.Rotulo, fuenteRotulo, new Rectangle(x, yc + P(2), anchoCifra, altoRotulo), TemaEcoAccess.Suave, Linea);
                    }
                    yc += altoRotulo + P(8);
                }
                Font grande = DibujoDashboard.Semibold(this, cifra.Estimada ? 28f : 30f), unidad = DibujoDashboard.Fuente(this, 14f);
                string valor = (cifra.Estimada && cifra.Valor != "—" ? "≈ " : "") + cifra.Valor;
                Size medidaValor = g is null ? TextRenderer.MeasureText(valor, grande, Size.Empty, TextFormatFlags.NoPadding)
                    : TextRenderer.MeasureText(g, valor, grande, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                if (g is not null)
                {
                    TextRenderer.DrawText(g, valor, grande, new Point(x, yc), cifra.Estimada ? TemaEcoAccess.Texto : TemaEcoAccess.VerdeOscuro, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    Size medidaUnidad = TextRenderer.MeasureText(g, cifra.Unidad, unidad, Size.Empty, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, cifra.Unidad, unidad, new Rectangle(x + medidaValor.Width + P(6), yc + medidaValor.Height - medidaUnidad.Height - P(4), Math.Max(0, anchoCifra - medidaValor.Width - P(6)), medidaUnidad.Height),
                        TemaEcoAccess.Suave, Linea);
                }
                yc += medidaValor.Height + P(2);
                if (cifra.Detalle is not null)
                {
                    Font fuenteDetalle = DibujoDashboard.Fuente(this, 12.5f);
                    if (g is not null) TextRenderer.DrawText(g, cifra.Detalle, fuenteDetalle, new Rectangle(x, yc, anchoCifra, fuenteDetalle.Height), TemaEcoAccess.Suave, Linea);
                    yc += fuenteDetalle.Height;
                }
                altoCifras = Math.Max(altoCifras, yc - y);
            }
            y += altoCifras + P(14);

            foreach (DesgloseResumen desglose in indicador.Desgloses)
                y = Desglose(g, desglose, area.Left + margen, y, anchoInterior) + P(12);

            if (indicador.Nota is not null || (indicador.Desgloses.Length == 0 && indicador.Cifras.All(c => c.Valor == "—")))
            {
                string nota = indicador.Nota ?? _notaVacia;
                Font fuente = DibujoDashboard.Fuente(this, 12.5f);
                int lado = P(15);
                int ancho = anchoInterior - lado - P(8);
                int alto = TextRenderer.MeasureText(nota, fuente, new Size(ancho, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
                if (g is not null)
                {
                    Color tono = indicador.NotaAviso ? TemaEcoAccess.Advertencia : TemaEcoAccess.Suave;
                    Bitmap icono = CacheIconos.Obtener(indicador.NotaAviso ? IconChar.TriangleExclamation : IconChar.CircleInfo, lado, indicador.NotaAviso ? DibujoDashboard.Pendiente : TemaEcoAccess.Verde);
                    g.DrawImage(icono, area.Left + margen, y + P(1));
                    TextRenderer.DrawText(g, nota, fuente, new Rectangle(area.Left + margen + lado + P(8), y, ancho, alto), tono, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }
                y += alto + P(12);
            }
            return y - area.Top + P(6);
        }

        /// <summary>Título, barra proporcional (si las partes suman el total) y leyenda que se parte en líneas.</summary>
        private int Desglose(Graphics? g, DesgloseResumen desglose, int x, int y, int ancho)
        {
            Font fuenteTitulo = DibujoDashboard.Semibold(this, 12f), fuente = DibujoDashboard.Fuente(this, 12.5f), cifra = DibujoDashboard.Semibold(this, 12.5f);
            if (g is not null) TextRenderer.DrawText(g, desglose.Titulo, fuenteTitulo, new Rectangle(x, y, ancho, fuenteTitulo.Height), TemaEcoAccess.Suave,
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            y += fuenteTitulo.Height + P(6);

            int suma = desglose.Partes.Sum(p => p.Valor);
            bool barra = desglose.Total is { } total && suma == total;
            if (barra)
            {
                int alto = P(10);
                if (g is not null)
                {
                    var pista = new RectangleF(x, y, ancho, alto);
                    using var ruta = RecursosEco.Redondeado(pista, alto / 2f);
                    using (var fondo = new SolidBrush(TemaEcoAccess.Fondo)) g.FillPath(fondo, ruta);
                    if (suma > 0)
                    {
                        Region anterior = g.Clip;
                        g.SetClip(ruta, CombineMode.Intersect);
                        float xi = x;
                        ParteResumen[] visibles = desglose.Partes.Where(p => p.Valor > 0).ToArray();
                        for (int k = 0; k < visibles.Length; k++)
                        {
                            float w = k == visibles.Length - 1 ? x + ancho - xi : (float)ancho * visibles[k].Valor / suma;
                            using var pincel = new SolidBrush(visibles[k].Color);
                            g.FillRectangle(pincel, xi, y, w, alto);
                            // Separación blanca fina entre segmentos para distinguirlos.
                            if (k > 0) using (var corte = new Pen(TemaEcoAccess.Superficie, P(2))) g.DrawLine(corte, xi, y, xi, y + alto);
                            xi += w;
                        }
                        g.Clip = anterior;
                    }
                    else
                        using (var borde = new Pen(TemaEcoAccess.Borde)) g.DrawPath(borde, ruta);
                }
                y += alto + P(8);
            }

            // Leyenda: punto de color + nombre + cantidad; pasa a la línea siguiente cuando no cabe.
            int punto = P(9), hueco = P(16), xl = x, altoLinea = Math.Max(fuente.Height, cifra.Height) + P(4);
            foreach (ParteResumen parte in desglose.Partes)
            {
                string valor = parte.Valor.ToString("N0", CultureInfo.GetCultureInfo("es-CO"));
                int anchoNombre = TextRenderer.MeasureText(parte.Nombre, fuente, Size.Empty, TextFormatFlags.NoPadding).Width;
                int anchoValor = TextRenderer.MeasureText(valor, cifra, Size.Empty, TextFormatFlags.NoPadding).Width;
                int anchoItem = punto + P(6) + anchoNombre + P(5) + anchoValor;
                if (xl > x && xl + anchoItem > x + ancho) { xl = x; y += altoLinea; }
                if (g is not null)
                {
                    using (var pincel = new SolidBrush(parte.Valor > 0 || !barra ? parte.Color : TemaEcoAccess.Borde))
                        g.FillEllipse(pincel, xl, y + (fuente.Height - punto) / 2f + P(1), punto, punto);
                    TextRenderer.DrawText(g, parte.Nombre, fuente, new Point(xl + punto + P(6), y), TemaEcoAccess.Texto, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, valor, cifra, new Point(xl + punto + P(6) + anchoNombre + P(5), y), parte.Valor > 0 ? TemaEcoAccess.Texto : TemaEcoAccess.Suave, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }
                xl += anchoItem + hueco;
            }
            return y + altoLinea;
        }
    }

    /// <summary>
    /// Barras horizontales de los grupos con más depósitos (material, caneca, punto o usuario): la barra clara es el total de
    /// depósitos y la oscura, encima, los verificados. Muestra los primeros <see cref="Maximo"/> y dice cuántos hay en total.
    /// </summary>
    internal sealed class GraficoGrupos : Control
    {
        private (string Nombre, int Depositos, int Verificados)[] _grupos = Array.Empty<(string, int, int)>();
        private int _totalGrupos;
        private string _vacio = "Consulta un período para ver los grupos.";
        internal const int Maximo = 6;

        internal GraficoGrupos()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.Chart;
            AccessibleName = "Grupos con más depósitos";
        }

        internal void Mostrar(IEnumerable<(string Nombre, int Depositos, int Verificados)> grupos)
        {
            var todos = grupos.ToList();
            _totalGrupos = todos.Count;
            _grupos = todos.OrderByDescending(g => g.Depositos).ThenBy(g => g.Nombre, StringComparer.CurrentCulture).Take(Maximo).ToArray();
            _vacio = "Sin depósitos para el filtro consultado.";
            AccessibleDescription = string.Join("; ", _grupos.Select(g => $"{g.Nombre}: {g.Depositos} depósitos, {g.Verificados} verificados"));
            Invalidate();
        }

        private int P(float n) => DibujoDashboard.P(this, n);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            const TextFormatFlags Linea = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            Font fuente = DibujoDashboard.Fuente(this, 12.5f), semibold = DibujoDashboard.Semibold(this, 12.5f), pequena = DibujoDashboard.Fuente(this, 11.5f);

            // Leyenda arriba a la derecha y el alcance a la izquierda.
            int altoCabecera = P(26);
            string alcance = _totalGrupos > Maximo ? $"Los {Maximo} con más depósitos de {_totalGrupos} grupos" : _totalGrupos == 0 ? "" : $"{_totalGrupos} {(_totalGrupos == 1 ? "grupo" : "grupos")}";
            TextRenderer.DrawText(g, alcance, pequena, new Rectangle(0, 0, Width / 2, altoCabecera), TemaEcoAccess.Suave, Linea);
            int xl = Width;
            foreach (var (nombre, color) in new[] { ("Verificados", TemaEcoAccess.Verde), ("Depósitos", TemaEcoAccess.VerdeHoja) })
            {
                int ancho = TextRenderer.MeasureText(nombre, pequena, Size.Empty, TextFormatFlags.NoPadding).Width;
                xl -= ancho;
                TextRenderer.DrawText(g, nombre, pequena, new Rectangle(xl, 0, ancho + 2, altoCabecera), TemaEcoAccess.Suave, Linea);
                xl -= P(16);
                using (var ruta = RecursosEco.Redondeado(new RectangleF(xl, (altoCabecera - P(10)) / 2f, P(10), P(10)), P(3)))
                using (var pincel = new SolidBrush(color)) g.FillPath(pincel, ruta);
                xl -= P(18);
            }

            if (_grupos.Length == 0 || _grupos.All(x => x.Depositos == 0))
            {
                DibujoDashboard.Texto(g, _grupos.Length == 0 ? _vacio : "Sin depósitos en el período consultado.", DibujoDashboard.Fuente(this, 13), TemaEcoAccess.Suave,
                    Rectangle.FromLTRB(0, altoCabecera, Width, Height), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            int maximo = Math.Max(1, _grupos.Max(x => x.Depositos));
            int anchoNombre = Math.Min(P(230), Width * 30 / 100);
            int anchoValor = TextRenderer.MeasureText("0000 · 0000 verif.", fuente, Size.Empty, TextFormatFlags.NoPadding).Width + P(10);
            int alto = Math.Min(P(34), (Height - altoCabecera - P(6)) / Maximo);
            int y = altoCabecera + P(6);
            foreach (var (nombre, depositos, verificados) in _grupos)
            {
                TextRenderer.DrawText(g, nombre, semibold, new Rectangle(0, y, anchoNombre - P(12), alto), TemaEcoAccess.Texto, Linea);
                int grosor = Math.Max(P(8), alto * 42 / 100);
                var pista = new RectangleF(anchoNombre, y + (alto - grosor) / 2f, Math.Max(P(20), Width - anchoNombre - anchoValor), grosor);
                using (var ruta = RecursosEco.Redondeado(pista, grosor / 2f))
                using (var fondo = new SolidBrush(TemaEcoAccess.Fondo)) g.FillPath(fondo, ruta);
                foreach ((int valor, Color color) in new[] { (depositos, TemaEcoAccess.VerdeHoja), (Math.Min(verificados, depositos), TemaEcoAccess.Verde) })
                {
                    if (valor <= 0) continue;
                    float w = Math.Max(grosor, pista.Width * valor / maximo);
                    using var ruta = RecursosEco.Redondeado(new RectangleF(pista.Left, pista.Top, w, pista.Height), grosor / 2f);
                    using var pincel = new SolidBrush(color);
                    g.FillPath(pincel, ruta);
                }
                string cifras = $"{depositos:N0} · {verificados:N0} verif.";
                TextRenderer.DrawText(g, cifras, fuente, new Rectangle((int)pista.Right + P(10), y, anchoValor - P(10), alto), TemaEcoAccess.Texto, Linea);
                y += alto;
            }
        }
    }

    /// <summary>
    /// Rueda del ratón en los módulos con desplazamiento: sobre una lista desplegable, una fecha o un número la rueda mueve
    /// la página en lugar de cambiar el filtro sin querer; sobre una tabla la desplaza a ella y, cuando ya llegó al principio
    /// o al final (o no tiene barra), sigue moviendo la página. No cambia ningún valor ni evento del formulario.
    /// </summary>
    internal static class RuedaEco
    {
        private const int WmMouseWheel = 0x020A;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr ventana, int mensaje, IntPtr wParam, IntPtr lParam);

        internal static void Encauzar(Control raiz)
        {
            Conectar(raiz);
            foreach (Control c in EstabilizadorVentana.Descendientes(raiz)) Conectar(c);
        }

        private static void Conectar(Control c)
        {
            switch (c)
            {
                case ComboBox lista:
                    lista.MouseWheel += (_, e) => { if (!lista.DroppedDown) Pasar(lista, e); };
                    break;
                case UpDownBase or DateTimePicker:
                    c.MouseWheel += (_, e) => Pasar(c, e);
                    break;
                case DataGridView tabla:
                    tabla.MouseWheel += (_, e) => { if (!PuedeDesplazar(tabla, e.Delta)) Pasar(tabla, e); };
                    break;
            }
        }

        private static bool PuedeDesplazar(DataGridView tabla, int delta)
        {
            VScrollBar? barra = tabla.Controls.OfType<VScrollBar>().FirstOrDefault();
            if (barra is null || !barra.Visible || !barra.Enabled) return false;
            return delta > 0 ? barra.Value > barra.Minimum : barra.Value < barra.Maximum - barra.LargeChange + 1;
        }

        /// <summary>Anula la rueda en el control y la entrega al primer contenedor con barra vertical visible.</summary>
        private static void Pasar(Control origen, MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs manejado) manejado.Handled = true;
            for (Control? p = origen.Parent; p is not null; p = p.Parent)
            {
                if (p is ScrollableControl { AutoScroll: true } desplazable && desplazable.VerticalScroll.Visible && desplazable.IsHandleCreated)
                {
                    Point cursor = Control.MousePosition;
                    var wParam = (IntPtr)(e.Delta << 16);
                    var lParam = (IntPtr)((cursor.Y << 16) | (cursor.X & 0xFFFF));
                    SendMessage(desplazable.Handle, WmMouseWheel, wParam, lParam);
                    return;
                }
            }
        }
    }
}
