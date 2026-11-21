using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>Colores e icono de cada una de las tres canecas del prototipo (Resolución 2184 de 2019).</summary>
    internal static class EstiloCaneca
    {
        internal sealed record Estilo(Color Cuerpo, Color Tapa, Color Simbolo, Color Fondo, Color Titulo, Color Detalle, IconChar Icono);

        internal static Estilo Para(CategoriaMaterial caneca) => caneca switch
        {
            CategoriaMaterial.OrganicoAprovechable => new Estilo(Color.FromArgb(0x2E, 0xA0, 0x43), Color.FromArgb(0x1F, 0x7A, 0x30), Color.White,
                Color.FromArgb(0x1E, 0x8E, 0x3E), Color.White, Color.FromArgb(0xDC, 0xF5, 0xE2), IconChar.Seedling),
            CategoriaMaterial.Aprovechable => new Estilo(Color.FromArgb(0xF4, 0xF6, 0xF5), Color.FromArgb(0xD5, 0xDB, 0xD8), Color.FromArgb(0x1E, 0x8E, 0x3E),
                Color.White, TemaEcoAccess.VerdeOscuro, TemaEcoAccess.Suave, IconChar.Recycle),
            _ => new Estilo(Color.FromArgb(0x2B, 0x30, 0x36), Color.FromArgb(0x16, 0x1A, 0x1E), Color.White,
                Color.FromArgb(0x33, 0x39, 0x40), Color.White, Color.FromArgb(0xD3, 0xD8, 0xDD), IconChar.TrashCan),
        };
    }

    /// <summary>Dibujo vectorial de una caneca (tapa, cuerpo y símbolo): nítido a cualquier escala y con el color correcto.</summary>
    internal static class DibujoCaneca
    {
        /// <param name="nombre">Si se indica, el nombre va escrito en el cuerpo, bajo el símbolo (portada del acceso).</param>
        internal static void Pintar(Graphics g, RectangleF area, CategoriaMaterial caneca, string? nombre = null)
        {
            var e = EstiloCaneca.Para(caneca);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = area.Width, h = area.Height, x = area.X, y = area.Y;
            float altoTapa = h * 0.16f;
            var tapa = new RectangleF(x, y, w, altoTapa);
            // Cuerpo ligeramente más angosto abajo, como una caneca real.
            using (var cuerpo = new GraphicsPath())
            {
                float arriba = y + altoTapa + h * 0.02f, abajo = y + h, margenAbajo = w * 0.09f;
                cuerpo.AddLine(x + w * 0.04f, arriba, x + w * 0.96f, arriba);
                cuerpo.AddLine(x + w * 0.96f, arriba, x + w - margenAbajo, abajo - h * 0.05f);
                cuerpo.AddArc(x + w - margenAbajo - h * 0.1f, abajo - h * 0.1f, h * 0.1f, h * 0.1f, 0, 90);
                cuerpo.AddArc(x + margenAbajo, abajo - h * 0.1f, h * 0.1f, h * 0.1f, 90, 90);
                cuerpo.CloseFigure();
                using var relleno = new LinearGradientBrush(new RectangleF(x, arriba, w, h), Claro(e.Cuerpo, 18), e.Cuerpo, LinearGradientMode.Horizontal);
                g.FillPath(relleno, cuerpo);
                using var borde = new Pen(Color.FromArgb(60, 0, 0, 0), Math.Max(1f, w / 90f));
                g.DrawPath(borde, cuerpo);
            }
            using (var ruta = RecursosEco.Redondeado(tapa, altoTapa * 0.35f))
            using (var pincel = new SolidBrush(e.Tapa))
            using (var borde = new Pen(Color.FromArgb(60, 0, 0, 0), Math.Max(1f, w / 90f)))
            {
                g.FillPath(pincel, ruta);
                g.DrawPath(borde, ruta);
            }
            // Ranura de la tapa.
            using (var ranura = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                g.FillRectangle(ranura, x + w * 0.32f, y + altoTapa * 0.38f, w * 0.36f, altoTapa * 0.24f);
            int lado = Math.Max(8, (int)(w * 0.42f));
            Bitmap simbolo = CacheIconos.Obtener(e.Icono, lado, e.Simbolo);
            if (nombre is null)
            {
                g.DrawImage(simbolo, x + (w - simbolo.Width) / 2f, y + altoTapa + (h - altoTapa - simbolo.Height) / 2f);
                return;
            }
            float cuerpoArriba = y + altoTapa, cuerpoAlto = h - altoTapa;
            g.DrawImage(simbolo, x + (w - simbolo.Width) / 2f, cuerpoArriba + cuerpoAlto * 0.12f);
            Color texto = caneca == CategoriaMaterial.Aprovechable ? TemaEcoAccess.VerdeOscuro : Color.White;
            using var fuente = new Font("Segoe UI", Math.Max(7f, w * 0.1f), FontStyle.Bold, GraphicsUnit.Pixel);
            var caja = Rectangle.Round(new RectangleF(x + w * 0.04f, cuerpoArriba + cuerpoAlto * 0.12f + simbolo.Height + w * 0.04f, w * 0.92f, cuerpoAlto * 0.4f));
            TextRenderer.DrawText(g, nombre, fuente, caja, texto, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        }

        private static Color Claro(Color c, int cuanto) =>
            Color.FromArgb(c.A, Math.Min(255, c.R + cuanto), Math.Min(255, c.G + cuanto), Math.Min(255, c.B + cuanto));
    }

    /// <summary>
    /// Tarjeta grande y seleccionable de una caneca: dibujo, nombre, descripción, ejemplos y la acción de ver sus
    /// residuos. Responde a clic, Enter y Espacio; la seleccionada lleva borde de foco y marca, no solo color.
    /// </summary>
    internal sealed class TarjetaCaneca : TarjetaDibujada
    {
        private readonly EstiloCaneca.Estilo _estilo;
        private readonly string _titulo, _descripcion, _ejemplos;
        private bool _seleccionada;

        public TarjetaCaneca(CategoriaMaterial caneca, string titulo, string descripcion) : base(interactiva: true)
        {
            Caneca = caneca;
            _estilo = EstiloCaneca.Para(caneca);
            _titulo = titulo;
            _descripcion = descripcion;
            _ejemplos = string.Join(", ", CatalogoPrototipo.DeCaneca(caneca).Take(3).Select(r => r.Nombre.Split(',')[0].ToLowerInvariant()));
            Superficie = _estilo.Fondo;
            AccessibleRole = AccessibleRole.RadioButton;
            AccessibleName = $"{titulo}: {descripcion}. Ver sus residuos ({_ejemplos}…).";
        }

        internal CategoriaMaterial Caneca { get; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal bool Seleccionada
        {
            get => _seleccionada;
            set { _seleccionada = value; AccessibleDescription = value ? "Seleccionada" : null; Invalidate(); }
        }

        private int LadoCaneca => P(84);
        internal static readonly Font FuenteTitulo = new("Segoe UI Semibold", 20);
        internal static readonly Font FuenteDescripcion = new("Segoe UI", 12);

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno - LadoCaneca - P(18) - P(40);
            int texto = DibujoEco.AltoTexto(_titulo, FuenteTitulo, util) + P(2) + DibujoEco.AltoTexto(_descripcion, FuenteDescripcion, util);
            return Relleno + Math.Max(LadoCaneca + P(16), texto) + Relleno + P(6);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_seleccionada) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var ruta = RecursosEco.Redondeado(new RectangleF(P(2), P(2), Width - P(5), Height - P(4) - P(4)), P(TemaEcoAccess.Radio));
            using var borde = new Pen(Caneca == CategoriaMaterial.Aprovechable ? TemaEcoAccess.Verde : Color.FromArgb(0xB8, 0xF0, 0x4A), P(3));
            e.Graphics.DrawPath(borde, ruta);
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Relleno, y = Relleno;
            int alto = Height - P(4) - 2 * Relleno;
            DibujoCaneca.Pintar(g, new RectangleF(x, y + (alto - LadoCaneca - P(16)) / 2f, LadoCaneca, LadoCaneca + P(16)), Caneca);
            int xt = x + LadoCaneca + P(22), util = Width - xt - Relleno - P(40);
            int total = DibujoEco.AltoTexto(_titulo, FuenteTitulo, util) + P(2) + DibujoEco.AltoTexto(_descripcion, FuenteDescripcion, util);
            int yt = y + Math.Max(0, (alto - total) / 2);
            yt += DibujoEco.Texto(g, _titulo, FuenteTitulo, _estilo.Titulo, xt, yt, util) + P(2);
            DibujoEco.Texto(g, _descripcion, FuenteDescripcion, _estilo.Detalle, xt, yt, util);
            // Flecha circular: la tarjeta lleva a sus residuos.
            int lado = P(36), cx = Width - Relleno - lado, cy = (Height - P(4) - lado) / 2;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color circulo = Caneca == CategoriaMaterial.Aprovechable ? TemaEcoAccess.VerdeSuave : Color.FromArgb(60, 255, 255, 255);
            using (var pincel = new SolidBrush(_seleccionada ? Color.FromArgb(0xB8, 0xF0, 0x4A) : circulo)) g.FillEllipse(pincel, cx, cy, lado, lado);
            Bitmap flecha = CacheIconos.Obtener(_seleccionada ? IconChar.Check : IconChar.ChevronRight, P(14),
                _seleccionada ? TemaEcoAccess.VerdeOscuro : _estilo.Titulo);
            g.DrawImage(flecha, cx + (lado - flecha.Width) / 2, cy + (lado - flecha.Height) / 2);
        }
    }

    /// <summary>Residuo del prototipo: ilustración oficial de la mano de ECO, nombre y la caneca a la que va.</summary>
    internal sealed class TarjetaResiduo : TarjetaDibujada
    {
        private bool _seleccionada;

        public TarjetaResiduo(ResiduoPrototipo residuo) : base(interactiva: true)
        {
            Residuo = residuo;
            AccessibleRole = AccessibleRole.RadioButton;
            AccessibleName = $"{residuo.Nombre}. Va en la {CatalogoPrototipo.NombreCaneca(residuo.Caneca).ToLowerInvariant()}.";
            Margin = new Padding(0, 0, 12, 12);
        }

        internal ResiduoPrototipo Residuo { get; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal bool Seleccionada
        {
            get => _seleccionada;
            set { _seleccionada = value; AccessibleDescription = value ? "Seleccionado" : null; Invalidate(); }
        }

        private int Interior => P(10);

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Interior;
            return Interior + util * 2 / 3 + P(8) + DibujoEco.AltoTexto(Residuo.Nombre, TemaEcoAccess.Subtitulo, util) + P(6) + P(24) + Interior + P(6);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_seleccionada) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var ruta = RecursosEco.Redondeado(new RectangleF(1, 1, Width - 3, Height - P(4) - 2), P(TemaEcoAccess.Radio));
            using var borde = new Pen(TemaEcoAccess.VerdeOscuro, P(3));
            e.Graphics.DrawPath(borde, ruta);
            Bitmap marca = CacheIconos.Obtener(IconChar.CircleCheck, P(22), TemaEcoAccess.VerdeOscuro);
            e.Graphics.DrawImage(marca, Width - Interior - marca.Width - P(2), Interior + P(2));
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Interior, y = Interior, util = Width - 2 * Interior;
            var area = new Rectangle(x, y, util, util * 2 / 3);
            Image? imagen = RecursosEco.Residuo(Residuo);
            if (imagen is not null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var recorte = RecursosEco.Redondeado(area, P(10));
                g.SetClip(recorte);
                // Recorte centrado (cover): la imagen es cuadrada y el área 3:2; la mano y el residuo quedan al centro.
                float k = Math.Max((float)area.Width / imagen.Width, (float)area.Height / imagen.Height);
                float w = imagen.Width * k, h = imagen.Height * k;
                g.DrawImage(imagen, area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
                g.ResetClip();
            }
            y += area.Height + P(8);
            y += DibujoEco.Texto(g, Residuo.Nombre, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto, x, y, util) + P(6);
            var e = EstiloCaneca.Para(Residuo.Caneca);
            Color tono = Residuo.Caneca == CategoriaMaterial.Aprovechable ? TemaEcoAccess.VerdeOscuro : Color.White;
            Color fondo = Residuo.Caneca == CategoriaMaterial.Aprovechable ? Color.FromArgb(0xEE, 0xF2, 0xF0) : e.Cuerpo;
            DibujoEco.Pastilla(g, CatalogoPrototipo.NombreCaneca(Residuo.Caneca), TemaEcoAccess.Pequena, tono, fondo, x, y, this);
        }
    }

    /// <summary>
    /// Indicador personal compacto (mis ecopuntos, depósitos…): icono a la izquierda y, a su lado, título, valor real de
    /// Oracle y una línea de detalle. Ocupa la mitad de alto que <see cref="TarjetaDato"/> para que el inicio quepa en pantalla.
    /// </summary>
    internal sealed class TarjetaIndicador : TarjetaDibujada
    {
        private readonly IconChar _icono;
        private readonly string _titulo;
        private string _valor = "—";
        private string _detalle = "Consultando Oracle…";
        internal static readonly Font FuenteValor = new("Segoe UI Semibold", 22);

        public TarjetaIndicador(IconChar icono, string titulo, bool interactiva = false) : base(interactiva)
        {
            _icono = icono;
            _titulo = titulo;
            AccessibleName = $"{titulo}: consultando";
        }

        internal void Mostrar(string valor, string detalle)
        {
            _valor = valor;
            _detalle = detalle;
            AccessibleName = $"{_titulo}: {valor}. {detalle}";
            Height = MedirAlto(Width);
            Invalidate();
            Parent?.PerformLayout();
        }

        private int LadoIcono => P(46);
        private int Interior => P(16);

        internal override int MedirAlto(int ancho)
        {
            int util = Math.Max(1, ancho - 2 * Interior - LadoIcono - P(14));
            int texto = DibujoEco.AltoTexto(_titulo, TemaEcoAccess.Subtitulo, util) + DibujoEco.AltoTexto(_valor, FuenteValor, util)
                + DibujoEco.AltoTexto(_detalle, TemaEcoAccess.Pequena, util);
            return Interior + Math.Max(LadoIcono, texto) + Interior + P(6);
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Interior, y = Interior;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujoEco.Icono(g, _icono, TemaEcoAccess.Verde, TemaEcoAccess.VerdeSuave, new Rectangle(x, y, LadoIcono, LadoIcono), P(12));
            int xt = x + LadoIcono + P(14), util = Math.Max(1, Width - xt - Interior);
            y += DibujoEco.Texto(g, _titulo, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto, xt, y, util);
            y += DibujoEco.Texto(g, _valor, FuenteValor, TemaEcoAccess.VerdeOscuro, xt, y, util);
            DibujoEco.Texto(g, _detalle, TemaEcoAccess.Pequena, TemaEcoAccess.Suave, xt, y, util);
        }
    }

    /// <summary>Imagen escalada sin deformar (contener), centrada sobre el fondo del contenedor.</summary>
    internal sealed class ImagenAjustada : Control
    {
        private Image? _imagen;

        public ImagenAjustada()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;
            AccessibleRole = AccessibleRole.Graphic;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal Image? Imagen { get => _imagen; set { _imagen = value; Invalidate(); } }

        /// <summary>Alineación vertical de la imagen dentro del control (abajo para personajes que «se apoyan» en el borde).</summary>
        internal bool AlinearAbajo { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_imagen is null || Width <= 0 || Height <= 0) return;
            float k = Math.Min((float)Width / _imagen.Width, (float)Height / _imagen.Height);
            float w = _imagen.Width * k, h = _imagen.Height * k;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(_imagen, (Width - w) / 2, AlinearAbajo ? Height - h : (Height - h) / 2, w, h);
        }
    }

    /// <summary>
    /// Portada de bienvenida a lo ancho: fondo verde suave con hojas, saludo personalizado, texto breve y ECO con su
    /// tablet a la derecha. El alto lo fija quien la usa; el texto se reacomoda al ancho.
    /// </summary>
    internal sealed class PortadaEco : Control
    {
        private readonly string _titulo, _texto;
        private readonly Image? _eco = RecursosEco.Imagen("eco/eco_tablet.png");

        public PortadaEco(string titulo, string texto)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            _titulo = titulo;
            _texto = texto;
            BackColor = TemaEcoAccess.Fondo;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = $"{titulo} {texto}";
            TabStop = false;
        }

        private int P(int u) => TemaEcoAccess.Px(this, u);

        internal static readonly Font FuenteTitulo = new("Segoe UI", 30, FontStyle.Bold | FontStyle.Italic);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            var area = new RectangleF(0, 0, Width - 1, Height - P(6));
            using (var ruta = RecursosEco.Redondeado(area, P(22)))
            {
                using (var fondo = new LinearGradientBrush(area, Color.FromArgb(0xD9, 0xF2, 0xC8), Color.FromArgb(0xF3, 0xFB, 0xEE), LinearGradientMode.Horizontal))
                    g.FillPath(fondo, ruta);
                g.SetClip(ruta);
                // Formas orgánicas: dos olas verdes en la base y hojas difusas, sin cargar imágenes extra.
                using (var ola = new GraphicsPath())
                {
                    ola.AddBezier(0, area.Height * 0.78f, area.Width * 0.3f, area.Height * 0.55f, area.Width * 0.55f, area.Height * 1.05f, area.Width, area.Height * 0.7f);
                    ola.AddLine(area.Width, area.Height * 0.7f, area.Width, area.Height);
                    ola.AddLine(area.Width, area.Height, 0, area.Height);
                    ola.CloseFigure();
                    using var pincel = new SolidBrush(Color.FromArgb(70, 0x8D, 0xC6, 0x3F));
                    g.FillPath(pincel, ola);
                }
                foreach (var (fx, fy, tam, giro) in new[] { (0.03f, 0.12f, 70, -30), (0.46f, 0.08f, 46, 20), (0.6f, 0.62f, 40, 60), (0.9f, 0.1f, 52, -60) })
                {
                    Bitmap hoja = CacheIconos.Obtener(IconChar.Leaf, P(tam), Color.FromArgb(55, 0x6C, 0xBF, 0x2E));
                    var estado = g.Save();
                    g.TranslateTransform(area.Width * fx + hoja.Width / 2f, area.Height * fy + hoja.Height / 2f);
                    g.RotateTransform(giro);
                    g.DrawImage(hoja, -hoja.Width / 2f, -hoja.Height / 2f);
                    g.Restore(estado);
                }
                g.ResetClip();
            }
            // ECO a la derecha, apoyado en el borde inferior.
            int altoEco = (int)(area.Height - P(8));
            int anchoEco = 0;
            if (_eco is not null)
            {
                anchoEco = altoEco * _eco.Width / _eco.Height;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_eco, area.Width - anchoEco - P(36), P(8), anchoEco, altoEco);
            }
            int x = P(44), util = (int)area.Width - x - anchoEco - P(80);
            int altoTitulo = DibujoEco.AltoTexto(_titulo, FuenteTitulo, util);
            int altoTexto = DibujoEco.AltoTexto(_texto, TemaEcoAccess.Entrada, Math.Min(util, P(640)));
            int y = (int)((area.Height - altoTitulo - P(8) - altoTexto) / 2);
            y += DibujoEco.Texto(g, _titulo, FuenteTitulo, TemaEcoAccess.VerdeOscuro, x, y, util) + P(8);
            DibujoEco.Texto(g, _texto, TemaEcoAccess.Entrada, TemaEcoAccess.Texto, x, y, Math.Min(util, P(640)));
        }
    }
}
