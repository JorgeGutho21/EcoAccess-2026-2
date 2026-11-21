using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Fuentes del inicio en PÍXELES, calculadas con la escala de la ventana y no con el DPI: así la composición depende solo
    /// del tamaño físico disponible y cabe igual a 100 %, 125 % o 150 % de escala de Windows. Se comparten y no se liberan.
    /// </summary>
    internal static class FuentesInicio
    {
        private static readonly Dictionary<(float, FontStyle, string), Font> Cache = new();

        internal static Font Obtener(float pixeles, FontStyle estilo = FontStyle.Regular, string familia = "Segoe UI")
        {
            float px = Math.Max(9f, (float)Math.Round(pixeles * 2) / 2f);
            lock (Cache)
            {
                if (!Cache.TryGetValue((px, estilo, familia), out Font? fuente))
                    Cache[(px, estilo, familia)] = fuente = new Font(familia, px, estilo, GraphicsUnit.Pixel);
                return fuente;
            }
        }

        internal static Font Semibold(float pixeles) => Obtener(pixeles, FontStyle.Regular, "Segoe UI Semibold");
    }

    /// <summary>Imágenes ya escaladas a un tamaño: escalar el PNG de las canecas en cada repintado costaba decenas de ms.</summary>
    internal static class CacheEscalado
    {
        private static readonly Dictionary<(Image, int, int), Bitmap> Cache = new();

        internal static Bitmap Obtener(Image origen, int ancho, int alto)
        {
            ancho = Math.Max(1, ancho);
            alto = Math.Max(1, alto);
            lock (Cache)
            {
                if (Cache.TryGetValue((origen, ancho, alto), out Bitmap? listo)) return listo;
                // Se conservan pocas escalas por imagen: al redimensionar la ventana no se acumulan copias.
                foreach (var vieja in Cache.Keys.Where(k => k.Item1 == origen).ToList())
                {
                    Cache[vieja].Dispose();
                    Cache.Remove(vieja);
                }
                var bmp = new Bitmap(ancho, alto, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.DrawImage(origen, new Rectangle(0, 0, ancho, alto));
                }
                Cache[(origen, ancho, alto)] = bmp;
                return bmp;
            }
        }

        /// <summary>Rectángulo con la proporción de la imagen, centrado (o apoyado abajo) dentro de <paramref name="area"/>.</summary>
        internal static Rectangle Contener(Image imagen, Rectangle area, bool abajo = false)
        {
            float k = Math.Min((float)area.Width / imagen.Width, (float)area.Height / imagen.Height);
            int w = Math.Max(1, (int)(imagen.Width * k)), h = Math.Max(1, (int)(imagen.Height * k));
            return new Rectangle(area.X + (area.Width - w) / 2, abajo ? area.Bottom - h : area.Y + (area.Height - h) / 2, w, h);
        }
    }

    /// <summary>Icono del material para los residuos sin ilustración: nunca se muestra una imagen rota.</summary>
    internal static class IconosMaterial
    {
        internal static IconChar Para(ResiduoCatalogo r)
        {
            switch (r.Ruta)
            {
                case RutaDisposicion.PilasBaterias: return IconChar.BatteryFull;
                case RutaDisposicion.Medicamentos: return IconChar.Pills;
                case RutaDisposicion.Bombillos: return IconChar.Lightbulb;
                case RutaDisposicion.Cortopunzante: return IconChar.Syringe;
                case RutaDisposicion.AceiteUsado: return IconChar.OilCan;
                case RutaDisposicion.Peligroso: return IconChar.SkullCrossbones;
                case RutaDisposicion.Raee: return IconChar.Plug;
                case RutaDisposicion.ReutilizacionDonacion: return IconChar.HandHoldingHeart;
                case RutaDisposicion.Evaluar: return IconChar.CircleQuestion;
                case RutaDisposicion.NoEsResiduo: return IconChar.Heart;
            }
            switch (r.Material)
            {
                case "PET": return IconChar.BottleWater;
                case "PLASTICO_RIGIDO": case "PEAD": return IconChar.Jar;
                case "PAPEL": return IconChar.FileLines;
                case "CARTON": case "MULTICAPA": return IconChar.BoxOpen;
                case "VIDRIO": return IconChar.WineBottle;
                case "LATA_ALUMINIO": return IconChar.Beer;
                case "ORGANICO": return IconChar.AppleWhole;
                case "CONDON": case "TOALLA_HIGIENICA": case "TAMPON": case "PANAL": case "SERVILLETA_USADA": return IconChar.Toilet;
                case "COLILLA": return IconChar.SmokingBan;
            }
            return r.Caneca switch
            {
                CategoriaMaterial.OrganicoAprovechable => IconChar.Seedling,
                CategoriaMaterial.Aprovechable => IconChar.Recycle,
                _ => IconChar.TrashCan,
            };
        }

        internal static Image? Ilustracion(ResiduoCatalogo r) => r.Prototipo is { } p ? RecursosEco.Residuo(p) : null;
    }

    /// <summary>
    /// Botón dibujado del inicio: principal (verde oscuro), secundario (blanco con borde) o enlace. Hover, pulsado, foco
    /// visible y teclado (Enter y Espacio). La fuente la fija quien lo coloca, según la escala de la ventana.
    /// </summary>
    internal sealed class BotonEco : Control
    {
        internal enum Estilo { Principal, Secundario, Enlace, Suave }

        private bool _encima, _pulsado;

        public BotonEco(string texto, IconChar icono = IconChar.None, Estilo estilo = Estilo.Secundario)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Text = texto;
            Icono = icono;
            Tipo = estilo;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = texto;
            Font = FuentesInicio.Semibold(15);
        }

        internal IconChar Icono { get; set; }
        internal Estilo Tipo { get; set; }

        /// <summary>Icono a la derecha del texto (flecha de «continuar»).</summary>
        internal bool IconoDerecha { get; set; }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); AccessibleName = Text; Invalidate(); }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size texto = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
            int icono = Icono == IconChar.None ? 0 : Font.Height + Font.Height / 2;
            int relleno = Tipo == Estilo.Enlace ? Font.Height / 4 : Font.Height;
            return new Size(texto.Width + icono + 2 * relleno, Tipo == Estilo.Enlace ? texto.Height + Font.Height / 3 : (int)(Font.Height * 2.3));
        }

        protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _encima = _pulsado = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pulsado = true; Focus(); Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pulsado = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }

        protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space && Enabled) { e.Handled = true; OnClick(EventArgs.Empty); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var area = new RectangleF(1, 1, Width - 3, Height - 3);
            Color fondo, texto, borde;
            switch (Tipo)
            {
                case Estilo.Principal:
                    fondo = !Enabled ? TemaEcoAccess.Borde : _pulsado ? Color.FromArgb(0x0A, 0x3E, 0x27) : _encima ? TemaEcoAccess.Hover : Color.FromArgb(0x17, 0x6B, 0x3A);
                    texto = Enabled ? Color.White : TemaEcoAccess.Suave;
                    borde = fondo;
                    break;
                case Estilo.Suave:
                    fondo = _pulsado ? TemaEcoAccess.VerdeHoja : _encima ? Color.FromArgb(0xD6, 0xF0, 0xDF) : TemaEcoAccess.VerdeSuave;
                    texto = Enabled ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
                    borde = fondo;
                    break;
                case Estilo.Enlace:
                    fondo = _encima ? Color.FromArgb(40, 0x16, 0x8A, 0x55) : Color.Transparent;
                    texto = Enabled ? TemaEcoAccess.Verde : TemaEcoAccess.Suave;
                    borde = Color.Transparent;
                    break;
                default:
                    fondo = _pulsado ? TemaEcoAccess.VerdeHoja : _encima ? TemaEcoAccess.VerdeSuave : Color.White;
                    texto = Enabled ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
                    borde = _encima ? TemaEcoAccess.Verde : Color.FromArgb(0xCF, 0xDD, 0xD4);
                    break;
            }
            float radio = Tipo == Estilo.Enlace ? Height / 4f : Math.Min(Height / 2f, Font.Height * 0.7f);
            if (Tipo != Estilo.Enlace)
            {
                // Sombra corta debajo del botón.
                var sombra = area; sombra.Offset(0, 2);
                using (var ruta = RecursosEco.Redondeado(sombra, radio))
                using (var pincel = new SolidBrush(Color.FromArgb(Tipo == Estilo.Principal ? 40 : 16, 10, 50, 30)))
                    g.FillPath(pincel, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(area, radio))
            {
                if (fondo.A > 0) using (var pincel = new SolidBrush(fondo)) g.FillPath(pincel, ruta);
                if (borde.A > 0 && Tipo == Estilo.Secundario) using (var lapiz = new Pen(borde)) g.DrawPath(lapiz, ruta);
                if (Focused && ShowFocusCues)
                    using (var foco = new Pen(Color.FromArgb(0xB8, 0xF0, 0x4A), 2.5f)) g.DrawPath(foco, ruta);
            }
            Size medida = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
            int ladoIcono = Icono == IconChar.None ? 0 : (int)(Font.Height * 1.05);
            int separacion = ladoIcono == 0 ? 0 : Font.Height / 2;
            int total = medida.Width + ladoIcono + separacion;
            int x = Math.Max(2, (Width - total) / 2);
            int xIcono = IconoDerecha ? x + medida.Width + separacion : x;
            int xTexto = IconoDerecha ? x : x + ladoIcono + separacion;
            if (ladoIcono > 0)
            {
                if (IconoDerecha && Tipo == Estilo.Principal)
                {
                    // Flecha en un círculo blanco, como el diseño aprobado.
                    int c = (int)(ladoIcono * 1.35);
                    using (var blanco = new SolidBrush(Color.White)) g.FillEllipse(blanco, xIcono, (Height - c) / 2f, c, c);
                    Bitmap flecha = CacheIconos.Obtener(Icono, ladoIcono * 3 / 4, fondo);
                    g.DrawImage(flecha, xIcono + (c - flecha.Width) / 2f, (Height - flecha.Height) / 2f);
                }
                else
                {
                    Bitmap imagen = CacheIconos.Obtener(Icono, ladoIcono, texto);
                    g.DrawImage(imagen, xIcono, (Height - imagen.Height) / 2f);
                }
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(xTexto, 0, Math.Max(1, Width - xTexto), Height), texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }
    }

    /// <summary>Indicador personal (ecopuntos, depósitos…): valor real de Oracle, detalle y aviso de datos desactualizados.</summary>
    internal sealed class IndicadorInicio : Control
    {
        private readonly IconChar _icono;
        private string _valor = "—", _detalle = "Consultando Oracle…";
        private bool _encima, _desactualizado;

        public IndicadorInicio(IconChar icono, string titulo)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            _icono = icono;
            Titulo = titulo;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = $"{titulo}: consultando";
        }

        internal string Titulo { get; }
        internal float Escala { get; set; } = 1f;
        internal bool Resaltado { get; set; }

        internal void Mostrar(string valor, string detalle)
        {
            _valor = valor;
            _detalle = detalle;
            AccessibleName = $"{Titulo}: {valor}. {detalle}";
            Invalidate();
        }

        /// <summary>Los datos en pantalla son viejos (Oracle no respondió): se atenúan y se dice.</summary>
        internal bool Desactualizado
        {
            get => _desactualizado;
            set { if (_desactualizado != value) { _desactualizado = value; Invalidate(); } }
        }

        internal string Valor => _valor;

        protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space) { e.Handled = true; OnClick(EventArgs.Empty); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Escala;
            float radio = 16 * s;
            var area = new RectangleF(2, 2, Width - 5, Height - 7);
            for (int capa = 4; capa >= 1; capa--)
            {
                var sombra = area; sombra.Offset(0, capa);
                using var ruta = RecursosEco.Redondeado(sombra, radio);
                using var pincel = new SolidBrush(Color.FromArgb(9, 16, 60, 30));
                g.FillPath(pincel, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(area, radio))
            {
                using (var blanco = new SolidBrush(Color.FromArgb(_encima ? 255 : 250, 255, 255, 255))) g.FillPath(blanco, ruta);
                bool marcado = Resaltado || Focused || _encima;
                using var borde = new Pen(marcado ? TemaEcoAccess.Verde : Color.FromArgb(0xE3, 0xEE, 0xE6), marcado ? Math.Max(1.5f, 2 * s) : 1);
                g.DrawPath(borde, ruta);
            }
            // Icono y título arriba, valor debajo del título y el detalle en una línea al pie (el texto completo va en AccessibleName).
            int pad = (int)(14 * s);
            Font titulo = FuentesInicio.Semibold(17 * s), valor = FuentesInicio.Obtener(28 * s, FontStyle.Bold), detalle = FuentesInicio.Obtener(13 * s);
            int lado = (int)Math.Min(44 * s, Height * 0.40);
            var cuadro = new Rectangle(pad, pad, lado, lado);
            DibujoEco.Icono(g, _icono, TemaEcoAccess.Verde, TemaEcoAccess.VerdeSuave, cuadro, 12 * s);
            int xt = cuadro.Right + (int)(12 * s), ancho = Math.Max(10, Width - xt - pad);
            Color tono = _desactualizado ? TemaEcoAccess.Suave : TemaEcoAccess.VerdeOscuro;
            int yDetalle = Height - (int)(12 * s) - detalle.Height - 5;
            TextRenderer.DrawText(g, Titulo, titulo, new Rectangle(xt, pad - (int)(2 * s), ancho, titulo.Height + 2), TemaEcoAccess.Texto,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int yValor = pad - (int)(2 * s) + titulo.Height;
            TextRenderer.DrawText(g, _valor, valor, Rectangle.FromLTRB(xt, yValor, xt + ancho, Math.Max(yValor + 4, yDetalle)), tono,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            string linea = _desactualizado ? "Sin actualizar · " + _detalle : _detalle;
            TextRenderer.DrawText(g, linea, detalle, new Rectangle(pad, yDetalle, Width - 2 * pad, detalle.Height + 2), _desactualizado ? TemaEcoAccess.Advertencia : TemaEcoAccess.Suave,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }

    /// <summary>
    /// Zona pulsable de una caneca sobre la ilustración oficial de las tres canecas. No dibuja la caneca (la ilustración es
    /// una sola imagen con plantas y base): solo el resaltado al pasar, al seleccionarla y el foco del teclado.
    /// </summary>
    internal sealed class ZonaCaneca : Control
    {
        private bool _encima, _seleccionada;

        public ZonaCaneca(CategoriaMaterial caneca, string nombre, string descripcion)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Caneca = caneca;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.RadioButton;
            AccessibleName = $"{nombre}: {descripcion}. Ver qué residuos recibe.";
        }

        internal CategoriaMaterial Caneca { get; }
        internal float Escala { get; set; } = 1f;

        internal bool Seleccionada
        {
            get => _seleccionada;
            set { if (_seleccionada != value) { _seleccionada = value; AccessibleDescription = value ? "Seleccionada" : null; Invalidate(); } }
        }

        protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space) { e.Handled = true; OnClick(EventArgs.Empty); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_seleccionada && !_encima && !Focused) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Escala;
            var area = new RectangleF(3, 3, Width - 7, Height - 7);
            Color lima = Color.FromArgb(0x9B, 0xE0, 0x3A);
            using (var ruta = RecursosEco.Redondeado(area, 22 * s))
            {
                if (_seleccionada)
                {
                    // Halo lima suave alrededor de la caneca elegida.
                    for (int i = 3; i >= 1; i--)
                        using (var halo = new Pen(Color.FromArgb(38, lima), i * 4 * s)) g.DrawPath(halo, ruta);
                    using var borde = new Pen(lima, Math.Max(2f, 3.2f * s));
                    g.DrawPath(borde, ruta);
                }
                else
                {
                    using var borde = new Pen(Color.FromArgb(Focused ? 230 : 150, TemaEcoAccess.Verde), Math.Max(1.5f, 2f * s)) { DashStyle = Focused ? DashStyle.Dash : DashStyle.Solid };
                    g.DrawPath(borde, ruta);
                }
            }
            if (_seleccionada)
            {
                // Marca de verificación arriba a la derecha.
                int lado = (int)(30 * s);
                var circulo = new Rectangle(Width - lado - (int)(8 * s), (int)(8 * s), lado, lado);
                using (var verde = new SolidBrush(TemaEcoAccess.Verde)) g.FillEllipse(verde, circulo);
                using (var blanco = new Pen(Color.White, Math.Max(1.5f, 2 * s))) g.DrawEllipse(blanco, circulo);
                Bitmap check = CacheIconos.Obtener(IconChar.Check, lado * 3 / 5, Color.White);
                g.DrawImage(check, circulo.X + (lado - check.Width) / 2f, circulo.Y + (lado - check.Height) / 2f);
            }
        }
    }

    /// <summary>Panel con doble búfer en el que el inicio dibuja su fondo, la portada, la sección y las tarjetas.</summary>
    internal sealed class LienzoInicio : Panel
    {
        public LienzoInicio()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            DoubleBuffered = true;
            AutoScroll = false; // El inicio cabe siempre: se compone a la escala de la ventana.
        }
    }

    /// <summary>
    /// «¿Dónde puedo llevarlo en Valledupar?»: puntos y programas verificados (tabla PUNTO_DISPOSICION de Oracle o la
    /// semilla verificada del 12/11/2026). Solo informa: no genera ninguna orden. No inventa direcciones ni horarios.
    /// </summary>
    public sealed class FormPuntosDisposicion : Form
    {
        public FormPuntosDisposicion(string residuo, RutaDisposicion ruta, IReadOnlyList<PuntoDisposicion> puntos, string? precauciones)
        {
            Text = "¿Dónde puedo llevarlo en Valledupar? · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            MinimizeBox = false;
            KeyPreview = true;
            Size = new Size(TemaEcoAccess.Px(this, 760), TemaEcoAccess.Px(this, 640));
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            var lista = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(24, 16, 24, 16), BackColor = TemaEcoAccess.Fondo };
            int ancho() => Math.Max(200, lista.ClientSize.Width - lista.Padding.Horizontal - 8);
            Label Etiqueta(string texto, Font fuente, Color color, int arriba = 0) =>
                new Label { Text = texto, AutoSize = true, Font = fuente, ForeColor = color, Margin = new Padding(0, arriba, 0, 4) };

            lista.Controls.Add(Etiqueta("¿Dónde puedo llevarlo en Valledupar?", TemaEcoAccess.Titulo, TemaEcoAccess.VerdeOscuro));
            lista.Controls.Add(Etiqueta($"{residuo} · {CatalogoResiduos.NombreDestino(ruta)}", TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto));
            lista.Controls.Add(Etiqueta(CatalogoResiduos.EsManejoEspecial(ruta)
                ? CatalogoResiduos.MensajeManejoEspecial
                : "EcoAccess no recibe este objeto en sus canecas.", TemaEcoAccess.Entrada, TemaEcoAccess.Advertencia, 6));
            if (!string.IsNullOrWhiteSpace(precauciones))
                lista.Controls.Add(Etiqueta("Precauciones: " + precauciones, TemaEcoAccess.Cuerpo, TemaEcoAccess.Texto, 4));

            var conDireccion = puntos.Where(p => p.Direccion is not null).ToList();
            if (conDireccion.Count == 0)
                lista.Controls.Add(Etiqueta("No encontramos un punto de entrega verificado en Valledupar para este residuo. No mostramos direcciones que no estén comprobadas.",
                    TemaEcoAccess.Entrada, TemaEcoAccess.Texto, 12));
            foreach (PuntoDisposicion p in puntos)
            {
                var tarjeta = new TarjetaEco { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 12, 0, 0) };
                var interior = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Top, BackColor = TemaEcoAccess.Superficie };
                interior.Controls.Add(Etiqueta(p.Nombre, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto));
                interior.Controls.Add(Etiqueta("Recibe: " + p.Recibe, TemaEcoAccess.Cuerpo, TemaEcoAccess.Texto));
                interior.Controls.Add(Etiqueta(p.Direccion is null ? "Programa sin dirección local publicada: consulta su buscador oficial." : $"Dirección: {p.Direccion}, {p.Municipio}", TemaEcoAccess.Cuerpo, TemaEcoAccess.Texto));
                interior.Controls.Add(Etiqueta("Horario: " + (p.Horario ?? "no verificado"), TemaEcoAccess.Cuerpo, TemaEcoAccess.Suave));
                interior.Controls.Add(Etiqueta(p.Estado switch
                {
                    EstadoVerificacionPunto.Confirmado => "Confirmado por el equipo. Antes de ir, confirma que recibe tu residuo.",
                    EstadoVerificacionPunto.ListadoSinConfirmar => "Aparece en la lista oficial del programa; no hemos confirmado que siga activo. Confírmalo antes de desplazarte.",
                    _ => "Programa nacional: el punto exacto se consulta en su buscador.",
                }, TemaEcoAccess.Pequena, p.Estado == EstadoVerificacionPunto.Confirmado ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Advertencia));
                if (p.EnlaceMapa is { } mapa) interior.Controls.Add(Enlace("Ver ubicación en el mapa", mapa));
                interior.Controls.Add(Enlace($"Fuente (consultada el {p.FechaVerificacion:dd/MM/yyyy})", p.Fuente));
                tarjeta.Controls.Add(interior);
                lista.Controls.Add(tarjeta);
            }
            lista.Controls.Add(Etiqueta("No uses una caneca roja genérica como solución: cada residuo especial tiene su propio programa de entrega.", TemaEcoAccess.Pequena, TemaEcoAccess.Suave, 14));
            lista.ClientSizeChanged += (_, _) =>
            {
                foreach (Control c in lista.Controls)
                {
                    c.MaximumSize = new Size(ancho(), 0);
                    if (c is TarjetaEco t) { t.MinimumSize = new Size(ancho(), 0); foreach (Control i in t.Controls) foreach (Control l in i.Controls) l.MaximumSize = new Size(ancho() - 40, 0); }
                }
            };
            var cerrar = new BotonEco("Cerrar", IconChar.None, BotonEco.Estilo.Principal) { Font = TemaEcoAccess.Subtitulo, Dock = DockStyle.Bottom, Height = TemaEcoAccess.Px(this, 48) };
            cerrar.Click += (_, _) => Close();
            Controls.Add(lista);
            Controls.Add(cerrar);
        }

        private static LinkLabel Enlace(string texto, string url)
        {
            var enlace = new LinkLabel { Text = texto, AutoSize = true, LinkColor = TemaEcoAccess.Verde, Margin = new Padding(0, 2, 0, 2), AccessibleName = texto };
            enlace.LinkClicked += (_, _) =>
            {
                // Solo enlaces https de la tabla de puntos (fuente pública o búsqueda en el mapa).
                if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return;
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
                catch (Exception) { MessageBox.Show("No se pudo abrir el navegador. Enlace: " + url, "EcoAccess"); }
            };
            return enlace;
        }
    }
}
