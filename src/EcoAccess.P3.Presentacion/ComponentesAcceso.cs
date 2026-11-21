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
        private readonly Image? _ilustracionOriginal = RecursosEco.Imagen("eco/eco_portada.png");
        private readonly Image? _campus = RecursosEco.Imagen("eco/campus.jpg");
        private readonly Image? _avatar = RecursosEco.Imagen("eco/eco_saluda.png");
        private Control? _tarjeta;
        private Bitmap? _fondoDifuminado;
        private Bitmap? _ilustracion;

        private static readonly (FontAwesome.Sharp.IconChar Icono, string Texto)[] Beneficios =
        {
            (FontAwesome.Sharp.IconChar.Recycle, "Aprende a separar\ncorrectamente"),
            (FontAwesome.Sharp.IconChar.ChartColumn, "Consulta tu\nhistorial de depósitos"),
            (FontAwesome.Sharp.IconChar.Award, "Acumula\necopuntos"),
            (FontAwesome.Sharp.IconChar.Leaf, "Contribuye a un\ncampus más sostenible"),
        };

        private static readonly (CategoriaMaterial Caneca, string Nombre)[] Canecas =
        {
            (CategoriaMaterial.NoAprovechable, "No\naprovechables"),
            (CategoriaMaterial.OrganicoAprovechable, "Orgánicos\naprovechables"),
            (CategoriaMaterial.Aprovechable, "Aprovechables\n(reciclables)"),
        };

        /// <summary>
        /// Portada del acceso (diseño de referencia ExpoSoftware): el campus difuminado de fondo, ECO a la izquierda (se
        /// funde con el fondo, sin cortes), la marca y el lema «Reciclar hoy, construye un mejor mañana», las tres canecas
        /// con su nombre, la conversación con el bot de Telegram cuando hay ancho y, abajo, la franja de beneficios. La
        /// tarjeta de acceso (<see cref="Tarjeta"/>) se coloca a la derecha. Es decorativa: no concede acceso.
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

        /// <summary>Alto que necesita el contenido de la tarjeta: la tarjeta no se estira más allá (sin hueco blanco abajo).</summary>
        internal Func<int>? AltoContenido { get; set; }

        private int P(int u) => TemaEcoAccess.Px(this, u);
        private int AltoFranja => ConFranja ? P(92) : 0;
        /// <summary>La franja de beneficios solo se muestra si queda alto suficiente para la tarjeta de acceso.</summary>
        private bool ConFranja => Height >= P(700);
        private bool Compacta => Width < P(960);

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_tarjeta is null) return;
            int margen = Height < P(860) ? P(18) : P(28);
            int ancho = Compacta ? Width - 2 * margen : Math.Min(P(520), Math.Max(P(440), Width * 34 / 100));
            int disponible = Height - 2 * margen - (Compacta || !ConFranja ? 0 : AltoFranja + P(8));
            int alto = Math.Max(P(240), disponible);
            int deseado = AltoContenido?.Invoke() ?? 0;
            if (deseado > 0) alto = Math.Min(alto, deseado);
            var limites = new Rectangle(Width - ancho - margen, margen, ancho, alto);
            if (_tarjeta.Bounds != limites)
            {
                _tarjeta.Bounds = limites;
                using var ruta = RecursosEco.Redondeado(new RectangleF(0, 0, _tarjeta.Width, _tarjeta.Height), P(22));
                _tarjeta.Region = new Region(ruta);
            }
            Invalidate();
        }

        /// <summary>Fondo: la parte del campus con edificios y árboles, muy difuminada (se dibuja pequeña y se amplía).</summary>
        private Bitmap? FondoDifuminado()
        {
            if (_fondoDifuminado is not null || _campus is null) return _fondoDifuminado;
            var origen = new Rectangle(0, _campus.Height * 30 / 100, _campus.Width, _campus.Height * 42 / 100);
            var pequeno = new Bitmap(110, 110 * origen.Height / origen.Width);
            using (var g = Graphics.FromImage(pequeno))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(_campus, new Rectangle(0, 0, pequeno.Width, pequeno.Height), origen, GraphicsUnit.Pixel);
            }
            return _fondoDifuminado = pequeno;
        }

        /// <summary>ECO con el pulgar arriba (bordes ya transparentes), escalado al alto del panel una sola vez.</summary>
        private Bitmap? Ilustracion(int alto)
        {
            if (_ilustracionOriginal is null || alto < 10) return null;
            if (_ilustracion is not null && _ilustracion.Height == alto) return _ilustracion;
            _ilustracion?.Dispose();
            int ancho = Math.Max(10, alto * _ilustracionOriginal.Width / _ilustracionOriginal.Height);
            var bmp = new Bitmap(ancho, alto, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(_ilustracionOriginal, new Rectangle(0, 0, ancho, alto));
            }
            return _ilustracion = bmp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Bitmap? fondo = FondoDifuminado();
            if (fondo is not null)
            {
                float k = Math.Max((float)Width / fondo.Width, (float)Height / fondo.Height);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                using var atributos = new System.Drawing.Imaging.ImageAttributes();
                atributos.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY); // sin halo en los bordes al ampliar
                float w = fondo.Width * k, h = fondo.Height * k;
                g.DrawImage(fondo, Rectangle.Round(new RectangleF((Width - w) / 2, (Height - h) / 2, w, h)), 0, 0, fondo.Width, fondo.Height, GraphicsUnit.Pixel, atributos);
            }
            // Velo verde: el lema blanco y las canecas se leen sobre el campus.
            using (var velo = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
                       Color.FromArgb(70, 12, 74, 40), Color.FromArgb(150, 12, 74, 40), 0f))
                g.FillRectangle(velo, 0, 0, Width, Height);
            int anchoEco = 0;
            Bitmap? eco = Ilustracion(Height);
            if (eco is not null)
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor; // ya está a su tamaño final
                g.DrawImageUnscaled(eco, 0, 0);
                anchoEco = eco.Width;
            }
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            PintarOlas(g);
            if (Compacta) return;
            int derecha = _tarjeta is null ? Width : _tarjeta.Left - P(16);
            // Zona libre entre ECO (su parte visible termina antes del degradado) y la tarjeta.
            int izquierda = Math.Min(anchoEco * 78 / 100, Math.Max(0, derecha - P(380)));
            float centro = (izquierda + derecha) / 2f;
            bool reducido = derecha - izquierda < P(560) || Height < P(700);
            var marca = reducido ? _marcaReducida : _marca;
            var lema = reducido ? _lemaReducido : _lema;
            float y = reducido ? P(14) : P(34);
            float finMarca = Centrado(g, "EcoAccess", marca, Color.White, centro, y, out float anchoMarca);
            // Brote verde sobre el final de la marca, como en el diseño de referencia.
            int brote = reducido ? P(30) : P(40);
            g.DrawImage(CacheIconos.Obtener(FontAwesome.Sharp.IconChar.Seedling, brote, Color.FromArgb(0x8D, 0xD8, 0x3A)), centro + anchoMarca / 2f - brote * 1.5f, y - brote * 0.2f);
            y = finMarca - P(6);
            // Trazo verde bajo la marca.
            using (var trazo = new System.Drawing.Drawing2D.GraphicsPath())
            {
                float mitad = anchoMarca * 0.46f;
                trazo.AddBezier(centro - mitad, y + P(8), centro - mitad * 0.3f, y - P(4), centro + mitad * 0.4f, y - P(4), centro + mitad, y - P(2));
                trazo.AddBezier(centro + mitad, y - P(2), centro + mitad * 0.4f, y + P(2), centro - mitad * 0.3f, y + P(4), centro - mitad, y + P(8));
                using var lima = new SolidBrush(Color.FromArgb(0x8D, 0xD8, 0x3A));
                using var borde = new Pen(Color.FromArgb(0x8D, 0xD8, 0x3A), P(3));
                g.FillPath(lima, trazo);
                g.DrawPath(borde, trazo);
            }
            y += P(16);
            float inicioLema = y;
            int interlinea = reducido ? P(6) : P(10);
            y = Centrado(g, "Reciclar hoy,", lema, Color.White, centro, y, out float anchoHoy) - interlinea;
            y = Centrado(g, "construye un", lema, Color.White, centro, y, out _) - interlinea;
            y = Centrado(g, "mejor mañana", lema, Color.FromArgb(0xC6, 0xF2, 0x4E), centro, y, out _);
            // Destellos junto a «hoy,».
            using (var destello = new Pen(Color.FromArgb(0xD9, 0xF2, 0x3A), P(4)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
            {
                float dx = centro + anchoHoy / 2f + P(14), dy = inicioLema + P(10);
                g.DrawLine(destello, dx, dy + P(6), dx + P(10), dy - P(6));
                g.DrawLine(destello, dx + P(8), dy + P(14), dx + P(26), dy + P(8));
            }
            PintarEscena(g, izquierda, derecha, y + P(16));
            if (ConFranja) PintarFranja(g);
        }

        // Olas verde lima en las esquinas inferiores, como en el diseño de referencia.
        private void PintarOlas(Graphics g)
        {
            using var lima = new SolidBrush(Color.FromArgb(190, 0x6C, 0xBF, 0x2E));
            using (var ola = new System.Drawing.Drawing2D.GraphicsPath())
            {
                ola.AddBezier(0, Height * 0.84f, Width * 0.06f, Height * 0.88f, Width * 0.10f, Height * 0.96f, Width * 0.16f, Height);
                ola.AddLine(Width * 0.16f, Height, 0, Height);
                ola.CloseFigure();
                g.FillPath(lima, ola);
            }
            using (var ola = new System.Drawing.Drawing2D.GraphicsPath())
            {
                ola.AddBezier(Width, Height * 0.80f, Width * 0.95f, Height * 0.88f, Width * 0.91f, Height * 0.95f, Width * 0.84f, Height);
                ola.AddLine(Width * 0.84f, Height, Width, Height);
                ola.CloseFigure();
                g.FillPath(lima, ola);
            }
        }

        /// <summary>
        /// Las tres canecas sobre su base, con el nombre escrito en cada una (Resolución 2184), y el teléfono con la
        /// conversación del bot de Telegram apoyado a su derecha cuando hay ancho.
        /// </summary>
        private void PintarEscena(Graphics g, float izquierda, float derecha, float arriba)
        {
            float abajo = Height - AltoFranja - (ConFranja ? P(24) : P(16));
            float alto = abajo - arriba;
            float anchoZona = derecha - izquierda;
            int lado = (int)Math.Min(P(118), Math.Min((alto - P(20)) / 1.5f, anchoZona / 3.5f));
            if (lado < P(54)) return; // sin espacio bajo el lema: no se tapa el texto
            float altoTel = Math.Min(alto, lado * 3.0f);
            float anchoTel = altoTel * 0.52f;
            float anchoCanecas = lado * 3.24f;
            bool telefono = altoTel >= P(250) && anchoZona >= anchoCanecas + anchoTel + P(30);
            float total = anchoCanecas + (telefono ? anchoTel + P(4) : 0);
            float x0 = (izquierda + derecha) / 2f - total / 2f;
            float baseY = abajo - P(16);
            float topeCaneca = baseY - lado * 1.5f;
            // Base oscura con sombra.
            using (var sombra = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                g.FillEllipse(sombra, x0 - P(20), baseY + P(6), anchoCanecas + P(40), P(18));
            using (var peana = new System.Drawing.Drawing2D.LinearGradientBrush(new RectangleF(0, baseY - 1, 10, P(18) + 2), Color.FromArgb(0x3A, 0x40, 0x46), Color.FromArgb(0x1E, 0x22, 0x26), 90f))
            using (var ruta = RecursosEco.Redondeado(new RectangleF(x0 - P(12), baseY - P(2), anchoCanecas + P(24), P(18)), P(5)))
                g.FillPath(peana, ruta);
            for (int i = 0; i < Canecas.Length; i++)
            {
                float x = x0 + i * lado * 1.12f;
                DibujoCaneca.Pintar(g, new RectangleF(x, topeCaneca, lado, lado * 1.5f), Canecas[i].Caneca, Canecas[i].Nombre);
            }
            if (telefono) PintarTelefono(g, new RectangleF(x0 + anchoCanecas + P(4), abajo - altoTel + P(4), anchoTel, altoTel));
        }

        /// <summary>Teléfono con la conversación del bot de Telegram de EcoAccess (ilustrativo, no es un chat real).</summary>
        private void PintarTelefono(Graphics g, RectangleF r)
        {
            float w = r.Width;
            using (var sombra = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            using (var ruta = RecursosEco.Redondeado(new RectangleF(r.X + P(8), r.Y + P(10), r.Width, r.Height), w * 0.15f))
                g.FillPath(sombra, ruta);
            using (var marco = new System.Drawing.Drawing2D.LinearGradientBrush(r, Color.FromArgb(0x3A, 0x3F, 0x44), Color.FromArgb(0x12, 0x14, 0x16), 0f))
            using (var ruta = RecursosEco.Redondeado(r, w * 0.15f))
                g.FillPath(marco, ruta);
            var pantalla = RectangleF.Inflate(r, -w * 0.04f, -w * 0.04f);
            using var rutaPantalla = RecursosEco.Redondeado(pantalla, w * 0.12f);
            var estado = g.Save();
            g.SetClip(rutaPantalla);
            using (var fondo = new System.Drawing.Drawing2D.LinearGradientBrush(pantalla, Color.FromArgb(0xE4, 0xF4, 0xDC), Color.FromArgb(0xCF, 0xEB, 0xC6), 90f))
                g.FillRectangle(fondo, pantalla);
            float tam = Math.Max(8f, w * 0.064f);
            using var fuente = new Font("Segoe UI", tam, GraphicsUnit.Pixel);
            using var pequena = new Font("Segoe UI", tam * 0.72f, GraphicsUnit.Pixel);
            using var fuenteTitulo = new Font("Segoe UI", tam * 1.3f, FontStyle.Bold, GraphicsUnit.Pixel);
            // Barra de estado y cabecera verde con el logo de Telegram.
            float altoEstado = pantalla.Height * 0.045f;
            float altoCabecera = pantalla.Height * 0.11f;
            using (var cabecera = new SolidBrush(Color.FromArgb(0x1F, 0x7A, 0x3A)))
                g.FillRectangle(cabecera, pantalla.X, pantalla.Y, pantalla.Width, altoEstado + altoCabecera);
            TextRenderer.DrawText(g, "9:41", pequena, Rectangle.Round(new RectangleF(pantalla.X + w * 0.1f, pantalla.Y + w * 0.02f, w * 0.4f, altoEstado)), Color.White, TextFormatFlags.NoPadding);
            float yc = pantalla.Y + altoEstado;
            Bitmap atras = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.ArrowLeft, Math.Max(6, (int)(altoCabecera * 0.32f)), Color.White);
            g.DrawImage(atras, pantalla.X + w * 0.05f, yc + (altoCabecera - atras.Height) / 2f);
            float d = altoCabecera * 0.62f;
            float xl = pantalla.X + w * 0.05f + atras.Width + w * 0.04f;
            using (var azul = new SolidBrush(Color.FromArgb(0x2A, 0xA1, 0xDA)))
                g.FillEllipse(azul, xl, yc + (altoCabecera - d) / 2f, d, d);
            Bitmap avion = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.PaperPlane, Math.Max(6, (int)(d * 0.5f)), Color.White);
            g.DrawImage(avion, xl + (d - avion.Width) / 2f - d * 0.04f, yc + (altoCabecera - avion.Height) / 2f);
            TextRenderer.DrawText(g, "Telegram", fuenteTitulo,
                Rectangle.Round(new RectangleF(xl + d + w * 0.04f, yc, pantalla.Right - xl - d, altoCabecera)), Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            float y = yc + altoCabecera + w * 0.08f;
            y = Burbuja(g, fuente, pequena, pantalla, y, "¿Dónde deposito una botella de plástico?", false);
            Burbuja(g, fuente, pequena, pantalla, y, "Va en la caneca blanca (Aprovechables).", true);
            // Barra de escritura.
            float altoBarra = pantalla.Height * 0.085f;
            var barra = new RectangleF(pantalla.X + w * 0.05f, pantalla.Bottom - altoBarra - w * 0.06f, pantalla.Width - w * 0.1f, altoBarra);
            using (var blanco = new SolidBrush(Color.White))
            using (var rutaBarra = RecursosEco.Redondeado(barra, altoBarra / 2))
                g.FillPath(blanco, rutaBarra);
            TextRenderer.DrawText(g, "Escribe un mensaje…", pequena, Rectangle.Round(new RectangleF(barra.X + altoBarra * 0.4f, barra.Y, barra.Width - altoBarra * 1.4f, barra.Height)),
                TemaEcoAccess.Suave, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            float de = altoBarra * 0.8f;
            using (var azul = new SolidBrush(Color.FromArgb(0x2A, 0xA1, 0xDA)))
                g.FillEllipse(azul, barra.Right - de - altoBarra * 0.1f, barra.Y + (altoBarra - de) / 2, de, de);
            Bitmap enviar = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.PaperPlane, Math.Max(6, (int)(de * 0.5f)), Color.White);
            g.DrawImage(enviar, barra.Right - de - altoBarra * 0.1f + (de - enviar.Width) / 2, barra.Y + (altoBarra - enviar.Height) / 2);
            g.Restore(estado);
        }

        // Burbuja de chat con la hora y el doble visto: la del bot va a la izquierda con la cara de ECO.
        private float Burbuja(Graphics g, Font fuente, Font pequena, RectangleF pantalla, float y, string texto, bool bot)
        {
            float w = pantalla.Width;
            float avatar = bot ? w * 0.19f : 0;
            float maximo = w * 0.62f;
            var bandera = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
            Size medida = TextRenderer.MeasureText(g, texto, fuente, new Size((int)maximo, 0), bandera);
            Size hora = TextRenderer.MeasureText(g, "10:24 ✓✓", pequena, Size.Empty, TextFormatFlags.NoPadding);
            float relleno = w * 0.045f;
            var caja = new RectangleF(0, y, Math.Max(medida.Width, hora.Width) + 2 * relleno, medida.Height + hora.Height + 2 * relleno);
            caja.X = bot ? pantalla.X + w * 0.04f + avatar + w * 0.02f : pantalla.Right - w * 0.05f - caja.Width;
            if (caja.Bottom > pantalla.Bottom - pantalla.Height * 0.14f) return y; // no tapa la barra de escritura
            using (var pincel = new SolidBrush(bot ? Color.FromArgb(0xB9, 0xE6, 0x9F) : Color.White))
            using (var ruta = RecursosEco.Redondeado(caja, w * 0.06f))
                g.FillPath(pincel, ruta);
            TextRenderer.DrawText(g, texto, fuente, new Rectangle((int)(caja.X + relleno), (int)(caja.Y + relleno), medida.Width + 2, medida.Height + 2), TemaEcoAccess.Texto, bandera);
            TextRenderer.DrawText(g, "10:24 ✓✓", pequena, new Rectangle((int)(caja.Right - relleno - hora.Width), (int)(caja.Bottom - relleno * 0.6f - hora.Height), hora.Width + 2, hora.Height),
                Color.FromArgb(0x4E, 0x8F, 0x5A), TextFormatFlags.NoPadding);
            if (bot && _avatar is not null)
            {
                var circulo = new RectangleF(pantalla.X + w * 0.04f, caja.Y, avatar, avatar);
                using var ruta = new System.Drawing.Drawing2D.GraphicsPath();
                ruta.AddEllipse(circulo);
                var estado = g.Save();
                g.SetClip(ruta, System.Drawing.Drawing2D.CombineMode.Intersect);
                using (var fondo = new SolidBrush(Color.White)) g.FillEllipse(fondo, circulo);
                g.DrawImage(_avatar, circulo);
                g.Restore(estado);
            }
            return caja.Bottom + w * 0.05f;
        }

        private float Centrado(Graphics g, string texto, Font fuente, Color color, float centro, float y) => Centrado(g, texto, fuente, color, centro, y, out _);

        private float Centrado(Graphics g, string texto, Font fuente, Color color, float centro, float y, out float ancho)
        {
            Size medida = TextRenderer.MeasureText(texto, fuente, Size.Empty, TextFormatFlags.NoPadding);
            ancho = medida.Width;
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
            if (disposing)
            {
                _marca.Dispose(); _lema.Dispose(); _marcaReducida.Dispose(); _lemaReducido.Dispose(); _beneficio.Dispose();
                _fondoDifuminado?.Dispose(); _ilustracion?.Dispose();
            }
        }
    }

    /// <summary>Barra superior del acceso: Universidad Popular del Cesar | EcoAccess y «Sistema de Gestión de Residuos».</summary>
    internal sealed class BarraAccesoEco : Control
    {
        private readonly Font _universidad = new("Segoe UI Semibold", 13);
        private readonly Font _marca = new("Segoe UI", 26, FontStyle.Bold | FontStyle.Italic);
        private readonly Font _sistema = new("Segoe UI", 14, FontStyle.Italic);
        private readonly Image? _logoUpc = RecursosEco.Imagen("eco/upc_blanco.png");

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
            // Onda verde lima en la esquina derecha, como en el diseño de referencia.
            using (var onda = new System.Drawing.Drawing2D.GraphicsPath())
            {
                onda.AddBezier(Width - P(150), Height, Width - P(80), Height - P(10), Width - P(30), Height - P(40), Width, Height - P(80));
                onda.AddLine(Width, Height - P(80), Width, Height);
                onda.CloseFigure();
                using var lima = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(Width - P(150), 0, P(150), Height), Color.FromArgb(0x4C, 0xA8, 0x2C), Color.FromArgb(0x9B, 0xE0, 0x3C), 0f);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillPath(lima, onda);
            }
            int x = P(28);
            var bandera = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
            if (_logoUpc is not null)
            {
                int altoLogo = Height - P(22);
                int anchoLogo = altoLogo * _logoUpc.Width / _logoUpc.Height;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(_logoUpc, x, (Height - altoLogo) / 2, anchoLogo, altoLogo);
                x += anchoLogo + P(20);
            }
            else
            {
                Size u = TextRenderer.MeasureText("Universidad\nPopular del Cesar", _universidad);
                TextRenderer.DrawText(g, "Universidad\nPopular del Cesar", _universidad, new Rectangle(x, 0, u.Width, Height), Color.White, bandera);
                x += u.Width + P(20);
            }
            using (var linea = new Pen(Color.FromArgb(150, 255, 255, 255), 1)) g.DrawLine(linea, x, P(16), x, Height - P(16));
            x += P(22);
            int lado = P(38);
            Bitmap hoja = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.Seedling, lado, Color.FromArgb(0x8D, 0xD8, 0x3A));
            g.DrawImage(hoja, x, (Height - hoja.Height) / 2);
            x += hoja.Width + P(10);
            TextRenderer.DrawText(g, "EcoAccess", _marca, new Rectangle(x, 0, P(320), Height), Color.White, bandera);
            Size s = TextRenderer.MeasureText("Sistema de Gestión de Residuos", _sistema);
            int xs = Width - P(64) - s.Width;
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
