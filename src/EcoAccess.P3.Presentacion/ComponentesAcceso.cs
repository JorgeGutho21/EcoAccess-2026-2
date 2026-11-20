using System;
using EcoAccess.P3.Entidades;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Ruta pública reutilizable: explica una opción y expone únicamente su acción visual.</summary>
    internal sealed class TarjetaRutaAcceso : TableLayoutPanel
    {
        internal KryptonButton Accion { get; } = new();

        internal TarjetaRutaAcceso(string titulo, string descripcion, string accion, IconChar icono)
        {
            AutoSize = true;
            ColumnCount = 2;
            RowCount = 3;
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 3; i++) RowStyles.Add(new RowStyle(SizeType.AutoSize));
            BackColor = Color.White;
            Padding = new Padding(24);
            Margin = new Padding(0, 0, 0, 16);
            var simbolo = new IconoEcoAccess { IconChar = icono, IconColor = TemaEcoAccess.Verde, IconSize = 28, Size = new Size(36, 36), BackColor = Color.White, TabStop = false };
            var cabecera = new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 8) };
            var detalle = new Label { Text = descripcion, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 12) };
            Accion.Values.Text = accion;
            Accion.AccessibleName = accion;
            Accion.AutoSize = false;
            Accion.Dock = DockStyle.Top;
            Accion.Height = 42;
            Accion.MinimumSize = new Size(180, 42);
            Accion.Margin = Padding.Empty;
            Accion.TabIndex = 0;
            TemaEcoAccess.EstilizarAccion(Accion);
            Controls.Add(simbolo, 0, 0);
            SetRowSpan(simbolo, 3);
            Controls.Add(cabecera, 1, 0);
            Controls.Add(detalle, 1, 1);
            Controls.Add(Accion, 1, 2);
            SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(200, ClientSize.Width - Padding.Horizontal - 48);
                cabecera.MaximumSize = detalle.MaximumSize = new Size(ancho, 0);
            };
        }
    }

    /// <summary>
    /// Identidad de la bienvenida: logotipo oficial, propósito y Eco (la mascota) sobre blanco, para que el logotipo
    /// conserve sus colores. Es decorativa: no
    /// muestra métricas ni concede acceso.
    /// </summary>
    internal sealed class PanelIdentidadEcoAccess : Control
    {
        private readonly Font _marca = new("Segoe UI", 50, FontStyle.Bold | FontStyle.Italic);
        private readonly Font _lema = new("Segoe UI", 30, FontStyle.Bold | FontStyle.Italic);
        private readonly Font _marcaReducida = new("Segoe UI", 38, FontStyle.Bold | FontStyle.Italic);
        private readonly Font _lemaReducido = new("Segoe UI", 23, FontStyle.Bold | FontStyle.Italic);
        private readonly Font _beneficio = new("Segoe UI Semibold", 12);
        private readonly Image? _fondo = RecursosEco.Imagen("eco/eco_upc.jpg");
        private Control? _tarjeta;
        private Bitmap? _difuminado;

        private static Bitmap Difuminar(Image origen)
        {
            var pequena = new Bitmap(48, 48 * origen.Height / origen.Width);
            using (var g = Graphics.FromImage(pequena))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(origen, 0, 0, pequena.Width, pequena.Height);
            }
            return pequena;
        }

        private static readonly (FontAwesome.Sharp.IconChar Icono, string Texto)[] Beneficios =
        {
            (FontAwesome.Sharp.IconChar.Recycle, "Aprende a separar\ncorrectamente"),
            (FontAwesome.Sharp.IconChar.ChartColumn, "Consulta tu\nhistorial de depósitos"),
            (FontAwesome.Sharp.IconChar.Award, "Acumula\necopuntos"),
            (FontAwesome.Sharp.IconChar.Leaf, "Contribuye a un\ncampus más sostenible"),
        };

        /// <summary>
        /// Portada del acceso (diseño de referencia ExpoSoftware): ECO en la UPC a todo el ancho, la marca y el lema «Reciclar
        /// hoy, construye un mejor mañana», las tres canecas con su color y, abajo, la franja de beneficios. La tarjeta de
        /// acceso (<see cref="Tarjeta"/>) se coloca a la derecha con esquinas redondeadas. Es decorativa: no concede acceso.
        /// </summary>
        internal PanelIdentidadEcoAccess()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.VerdeOscuro;
            TabStop = false;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = "EcoAccess. Reciclar hoy, construye un mejor mañana.";
        }

        /// <summary>Tarjeta blanca de inicio de sesión: a la derecha, sobre la ilustración y por encima de la franja.</summary>
        internal Control? Tarjeta
        {
            get => _tarjeta;
            set { _tarjeta = value; PerformLayout(); }
        }

        private int P(int u) => TemaEcoAccess.Px(this, u);
        private int AltoFranja => ConFranja ? P(92) : 0;
        /// <summary>La franja de beneficios solo se muestra si queda alto suficiente para la tarjeta de acceso.</summary>
        private bool ConFranja => Height >= P(700);
        private bool Compacta => Width < P(960);

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_tarjeta is null) return;
            int margen = P(28);
            int ancho = Compacta ? Width - 2 * margen : Math.Min(P(520), Math.Max(P(440), Width * 34 / 100));
            int alto = Height - 2 * margen - (Compacta || !ConFranja ? 0 : AltoFranja + P(8));
            _tarjeta.Bounds = new Rectangle(Width - ancho - margen, margen, ancho, Math.Max(P(240), alto));
            using var ruta = RecursosEco.Redondeado(new RectangleF(0, 0, _tarjeta.Width, _tarjeta.Height), P(22));
            _tarjeta.Region = new Region(ruta);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int anchoEco = 0;
            if (_fondo is not null)
            {
                // Fondo: la misma ilustración muy difuminada (dibujada pequeña y ampliada) cubre todo el panel.
                _difuminado ??= Difuminar(_fondo);
                float k = Math.Max((float)Width / _difuminado.Width, (float)Height / _difuminado.Height);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.DrawImage(_difuminado, 0, (Height - _difuminado.Height * k) / 2, _difuminado.Width * k, _difuminado.Height * k);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                // Encima, la ilustración nítida a la izquierda, ajustada al alto: ECO completo y sin deformar.
                anchoEco = Height * _fondo.Width / _fondo.Height;
                g.DrawImage(_fondo, 0, 0, anchoEco, Height);
                // Fundido ancho hacia el fondo difuminado, sin un corte visible.
                var borde = new Rectangle(anchoEco - P(300), 0, P(300) + 2, Height);
                // El pincel es un poco más ancho que la franja: GDI+ repite el degradado y dejaba una línea en el borde.
                var pincelArea = new RectangleF(borde.X - 2, 0, borde.Width + 4, Height);
                using (var fundido = new System.Drawing.Drawing2D.LinearGradientBrush(pincelArea, Color.FromArgb(0, 22, 70, 40), Color.FromArgb(175, 22, 70, 40), 0f) { WrapMode = System.Drawing.Drawing2D.WrapMode.TileFlipX })
                    g.FillRectangle(fundido, borde);
                using (var resto = new SolidBrush(Color.FromArgb(175, 22, 70, 40)))
                    g.FillRectangle(resto, anchoEco, 0, Math.Max(0, Width - anchoEco), Height);
            }
            // Velo suave para que el lema y la tarjeta se lean sobre la ilustración.
            using (var velo = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
                       Color.FromArgb(10, 10, 60, 30), Color.FromArgb(120, 10, 60, 30), 0f))
                g.FillRectangle(velo, 0, 0, Width, Height);
            // Ola verde lima en las esquinas inferiores, como en el diseño de referencia.
            using (var ola = new System.Drawing.Drawing2D.GraphicsPath())
            {
                ola.AddBezier(0, Height * 0.80f, Width * 0.08f, Height * 0.86f, Width * 0.12f, Height * 0.95f, Width * 0.2f, Height);
                ola.AddLine(Width * 0.2f, Height, 0, Height);
                ola.CloseFigure();
                using var lima = new SolidBrush(Color.FromArgb(200, 0x6C, 0xBF, 0x2E));
                g.FillPath(lima, ola);
            }
            if (Compacta) return;
            int derecha = _tarjeta is null ? Width : _tarjeta.Left - P(16);
            // Lema entre ECO (mitad izquierda de la ilustración) y la tarjeta.
            // Si hay espacio libre entre la ilustración y la tarjeta, ahí; si no, sobre el borde derecho de la ilustración.
            bool libre = derecha - anchoEco >= P(440);
            float centro = libre ? (anchoEco + derecha) / 2f : Math.Max(derecha * 0.55f, derecha - P(230));
            // Sin hueco libre el lema va sobre la ilustración: más abajo (deja ver el logo de la UPC) y algo más pequeño.
            bool reducido = !libre || Height < P(700);
            float y = libre ? Height * 0.10f : Height * 0.30f;
            var marca = reducido ? _marcaReducida : _marca;
            var lema = reducido ? _lemaReducido : _lema;
            y = Centrado(g, "EcoAccess", marca, Color.White, centro, y) + P(6);
            using (var linea = new Pen(Color.FromArgb(0xB8, 0xF0, 0x4A), P(4)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                g.DrawArc(linea, centro - P(170), y - P(40), P(340), P(40), 20, 140);
            y += P(14);
            y = Centrado(g, "Reciclar hoy,", lema, Color.White, centro, y);
            y = Centrado(g, "construye un", lema, Color.White, centro, y);
            y = Centrado(g, "mejor mañana", lema, Color.FromArgb(0xC6, 0xF2, 0x4E), centro, y);
            // Las tres canecas del prototipo con el color correcto (Resolución 2184), sobre una base.
            int lado = Math.Max(P(56), Math.Min(P(96), (int)(Height * 0.12f)));
            float baseY = Height - AltoFranja - P(40) - lado * 1.3f;
            if (baseY < y + P(16)) { if (ConFranja) PintarFranja(g); return; } // sin espacio bajo el lema: no se tapa el texto
            float x0 = centro - lado * 1.65f;
            var canecas = new[] { CategoriaMaterial.NoAprovechable, CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable };
            using (var basePeana = new SolidBrush(Color.FromArgb(200, 0x2B, 0x30, 0x36)))
                g.FillRectangle(basePeana, x0 - P(10), baseY + lado * 1.3f, lado * 3.3f + P(20), P(10));
            for (int i = 0; i < 3; i++)
                DibujoCaneca.Pintar(g, new RectangleF(x0 + i * lado * 1.1f, baseY, lado, lado * 1.3f), canecas[i]);
            if (ConFranja) PintarFranja(g);
        }

        private float Centrado(Graphics g, string texto, Font fuente, Color color, float centro, float y)
        {
            Size medida = TextRenderer.MeasureText(texto, fuente, Size.Empty, TextFormatFlags.NoPadding);
            var area = new Rectangle((int)(centro - medida.Width / 2f), (int)y, medida.Width, medida.Height);
            var sombra = area; sombra.Offset(P(2), P(2));
            TextRenderer.DrawText(g, texto, fuente, sombra, Color.FromArgb(90, 0, 0, 0), TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, texto, fuente, area, color, TextFormatFlags.NoPadding);
            return y + medida.Height;
        }

        // Franja blanca inferior con los cuatro beneficios.
        private void PintarFranja(Graphics g)
        {
            int margen = P(110);
            var franja = new RectangleF(margen, Height - AltoFranja - P(14), Width - 2 * margen, AltoFranja);
            using (var ruta = RecursosEco.Redondeado(franja, P(30)))
            using (var blanco = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                g.FillPath(blanco, ruta);
            float ancho = franja.Width / Beneficios.Length;
            for (int i = 0; i < Beneficios.Length; i++)
            {
                float x = franja.X + i * ancho;
                if (i > 0)
                    using (var linea = new Pen(TemaEcoAccess.Borde, 1))
                        g.DrawLine(linea, x, franja.Y + P(20), x, franja.Bottom - P(20));
                int lado = P(40);
                Bitmap icono = CacheIconos.Obtener(Beneficios[i].Icono, lado, TemaEcoAccess.Verde);
                Size texto = TextRenderer.MeasureText(Beneficios[i].Texto, _beneficio);
                float total = lado + P(14) + texto.Width;
                float xi = x + Math.Max(P(8), (ancho - total) / 2);
                g.DrawImage(icono, xi, franja.Y + (franja.Height - lado) / 2);
                TextRenderer.DrawText(g, Beneficios[i].Texto, _beneficio,
                    new Rectangle((int)(xi + lado + P(14)), (int)(franja.Y + (franja.Height - texto.Height) / 2), texto.Width + 4, texto.Height), TemaEcoAccess.Texto, TextFormatFlags.NoPadding);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { _marca.Dispose(); _lema.Dispose(); _marcaReducida.Dispose(); _lemaReducido.Dispose(); _beneficio.Dispose(); _difuminado?.Dispose(); }
        }
    }

    /// <summary>Barra superior del acceso: Universidad Popular del Cesar | EcoAccess y «Sistema de Gestión de Residuos».</summary>
    internal sealed class BarraAccesoEco : Control
    {
        private readonly Font _universidad = new("Segoe UI Semibold", 13);
        private readonly Font _marca = new("Segoe UI", 26, FontStyle.Bold | FontStyle.Italic);
        private readonly Font _sistema = new("Segoe UI", 14, FontStyle.Italic);

        internal BarraAccesoEco()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            TabStop = false;
            AccessibleRole = AccessibleRole.TitleBar;
            AccessibleName = "Universidad Popular del Cesar. EcoAccess, Sistema de Gestión de Residuos.";
        }

        private int P(int u) => TemaEcoAccess.Px(this, u);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var fondo = new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle.IsEmpty ? new Rectangle(0, 0, 1, 1) : ClientRectangle,
                       Color.FromArgb(0x0C, 0x4A, 0x2F), Color.FromArgb(0x16, 0x6B, 0x3E), 0f))
                g.FillRectangle(fondo, ClientRectangle);
            int x = P(28);
            var bandera = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
            Size u = TextRenderer.MeasureText("Universidad\nPopular del Cesar", _universidad);
            TextRenderer.DrawText(g, "Universidad\nPopular del Cesar", _universidad, new Rectangle(x, 0, u.Width, Height), Color.White, bandera);
            x += u.Width + P(20);
            using (var linea = new Pen(Color.FromArgb(150, 255, 255, 255), 1)) g.DrawLine(linea, x, P(16), x, Height - P(16));
            x += P(22);
            int lado = P(38);
            Bitmap hoja = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.Leaf, lado, Color.FromArgb(0xB8, 0xF0, 0x4A));
            g.DrawImage(hoja, x, (Height - hoja.Height) / 2);
            x += hoja.Width + P(10);
            TextRenderer.DrawText(g, "EcoAccess", _marca, new Rectangle(x, 0, P(320), Height), Color.White, bandera);
            Size s = TextRenderer.MeasureText("Sistema de Gestión de Residuos", _sistema);
            int xs = Width - P(32) - s.Width;
            if (xs > x + P(300))
            {
                Bitmap hojita = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.Leaf, P(22), Color.White);
                g.DrawImage(hojita, xs - hojita.Width - P(10), (Height - hojita.Height) / 2);
                TextRenderer.DrawText(g, "Sistema de Gestión de Residuos", _sistema, new Rectangle(xs, 0, s.Width + 4, Height), Color.White, bandera);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { _universidad.Dispose(); _marca.Dispose(); _sistema.Dispose(); }
        }
    }
}
