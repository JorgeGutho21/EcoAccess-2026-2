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
        private readonly Font _beneficio = new("Segoe UI Semibold", 12);
        private readonly Image? _ilustracionOriginal = RecursosEco.Imagen("eco/eco_upc.jpg");
        private readonly Image? _logo = RecursosEco.Imagen("eco/logo_ecoaccess.png");
        private readonly Image? _lema = RecursosEco.Imagen("eco/lema_portada.png");
        private readonly Image? _canecas = RecursosEco.Imagen("eco/canecas_portada.png");
        private Control? _tarjeta;
        private Bitmap? _ilustracion;
        /// <summary>Zona donde se pintaron las tres canecas: un clic ahí abre el sitio web de EcoAccess.</summary>
        private RectangleF _zonaCanecas = RectangleF.Empty;
        private readonly ToolTip _ayuda = new();

        /// <summary>Sitio web público de EcoAccess (ECOACCESS WEB, GitHub Pages).</summary>
        internal const string SitioWeb = "https://jorgegutho21.github.io/EcoAccess-2026-2/";

        private static readonly (FontAwesome.Sharp.IconChar Icono, string Texto)[] Beneficios =
        {
            (FontAwesome.Sharp.IconChar.Recycle, "Aprende a separar\ncorrectamente"),
            (FontAwesome.Sharp.IconChar.ChartColumn, "Consulta tu\nhistorial de depósitos"),
            (FontAwesome.Sharp.IconChar.Award, "Acumula\necopuntos"),
            (FontAwesome.Sharp.IconChar.Leaf, "Contribuye a un\ncampus más sostenible"),
        };

        /// <summary>
        /// Portada del acceso (diseño de referencia ExpoSoftware): a la izquierda ECO en la UPC; a la derecha un panel verde
        /// con borde curvo que lleva el logotipo de EcoAccess, el lema «Reciclar hoy, construye un mejor mañana.» y las tres
        /// canecas; abajo, la franja de beneficios. La tarjeta de acceso (<see cref="Tarjeta"/>) va a la derecha.
        /// No concede acceso; un clic en las canecas abre el sitio web de EcoAccess en el navegador.
        /// </summary>
        internal PanelIdentidadEcoAccess()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.VerdeOscuro;
            TabStop = false;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = "EcoAccess. Reciclar hoy, construye un mejor mañana.";
            AccessibleDescription = "Haz clic en las canecas para abrir el sitio web de EcoAccess.";
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool sobre = _zonaCanecas.Contains(e.Location);
            Cursor nuevo = sobre ? Cursors.Hand : Cursors.Default;
            if (Cursor != nuevo)
            {
                Cursor = nuevo;
                _ayuda.SetToolTip(this, sobre ? "Abrir el sitio web de EcoAccess" : null);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            _ayuda.SetToolTip(this, null);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || !_zonaCanecas.Contains(e.Location)) return;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(SitioWeb) { UseShellExecute = true })?.Dispose(); }
            catch (Exception) { MessageBox.Show(this, "No se pudo abrir el navegador. Sitio: " + SitioWeb, "EcoAccess", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }


        /// <summary>Tarjeta blanca de inicio de sesión: a la derecha, sobre el panel verde y por encima de la franja.</summary>
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
            int ancho = Compacta ? Width - 2 * margen : Math.Min(P(520), Math.Max(P(440), Width * 31 / 100));
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

        /// <summary>ECO en la UPC escalado al alto del panel una sola vez (se recalcula si cambia el alto).</summary>
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
            _zonaCanecas = RectangleF.Empty;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Bitmap? eco = Ilustracion(Height);
            int derecha = _tarjeta is null ? Width : _tarjeta.Left;
            // El panel verde empieza justo después del logotipo de la UPC (termina al 96 % del ancho de la ilustración).
            // Si falta espacio, la ilustración se corre a la izquierda (recorta el árbol, no a ECO ni el logotipo).
            int anchoEco = eco?.Width ?? Width / 2;
            float finLogo = anchoEco * 0.96f, limite = derecha - P(400);
            float corrimiento = Math.Min(Height * 0.14f, Math.Max(Height * 0.05f, finLogo - P(30) - limite));
            float curva = Math.Min(finLogo - corrimiento - P(30), limite);
            if (eco is not null) g.DrawImageUnscaled(eco, -(int)corrimiento, 0);
            if (Compacta) { if (ConFranja) PintarFranja(g); return; }

            using (var panel = new System.Drawing.Drawing2D.GraphicsPath())
            {
                panel.AddBezier(curva + P(70), 0, curva - P(30), Height * 0.30f, curva - P(60), Height * 0.65f, curva + P(20), Height);
                panel.AddLine(curva + P(20), Height, Width, Height);
                panel.AddLine(Width, Height, Width, 0);
                panel.CloseFigure();
                var zona = new RectangleF(curva - P(80), 0, Math.Max(1, Width - curva + P(80)), Math.Max(1, Height));
                using (var verde = new System.Drawing.Drawing2D.LinearGradientBrush(zona, Color.Black, Color.Black, 25f))
                {
                    verde.InterpolationColors = new System.Drawing.Drawing2D.ColorBlend
                    {
                        Colors = new[] { Color.FromArgb(0x4C, 0xB0, 0x4A), Color.FromArgb(0x23, 0x8A, 0x3C), Color.FromArgb(0x12, 0x6A, 0x30), Color.FromArgb(0x0E, 0x58, 0x2A) },
                        Positions = new[] { 0f, 0.25f, 0.6f, 1f },
                    };
                    g.FillPath(verde, panel);
                }
                // Brillo suave detrás del logotipo y de las canecas.
                float cx = (curva + derecha) / 2f;
                using (var brillo = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    brillo.AddEllipse(cx - P(320), Height * 0.05f, P(640), Height * 0.9f);
                    using var luz = new System.Drawing.Drawing2D.PathGradientBrush(brillo)
                    {
                        CenterColor = Color.FromArgb(70, 0xB8, 0xF0, 0x6A),
                        SurroundColors = new[] { Color.FromArgb(0, 0x23, 0x8A, 0x3C) },
                    };
                    var estado = g.Save();
                    g.SetClip(panel);
                    g.FillPath(luz, brillo);
                    g.Restore(estado);
                }
                // Borde luminoso de la curva.
                using (var borde = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    borde.AddBezier(curva + P(70), 0, curva - P(30), Height * 0.30f, curva - P(60), Height * 0.65f, curva + P(20), Height);
                    using var halo = new Pen(Color.FromArgb(90, 0xB8, 0xF0, 0x4A), P(10));
                    using var linea = new Pen(Color.FromArgb(170, 0xD8, 0xFF, 0x8A), P(2));
                    g.DrawPath(halo, borde);
                    g.DrawPath(linea, borde);
                }
            }
            PintarOlas(g);
            PintarContenido(g, curva + P(36), derecha - P(24)); // la curva llega hasta curva+70 arriba: el logotipo no la pisa
            if (ConFranja) PintarFranja(g);
        }

        /// <summary>Logotipo, lema y canecas centrados en el panel verde; se reducen juntos si falta alto.</summary>
        private void PintarContenido(Graphics g, float izquierda, float derecha)
        {
            float ancho = derecha - izquierda;
            if (ancho < P(220) || _logo is null || _lema is null) return;
            float arriba = Height < P(700) ? P(14) : P(36);
            float abajo = Height - AltoFranja - (ConFranja ? P(22) : P(10));
            float disponible = abajo - arriba;
            float wLogo = Math.Min(ancho * 0.78f, P(400)), wLema = Math.Min(ancho * 0.96f, P(430)), wCanecas = Math.Min(ancho * 1.02f, P(500));
            float hLogo = wLogo * _logo.Height / _logo.Width, hLema = wLema * _lema.Height / _lema.Width;
            float hCanecas = _canecas is null ? 0 : wCanecas * _canecas.Height / _canecas.Width;
            float separacion = P(14);
            float total = hLogo + separacion + hLema + separacion + hCanecas;
            float escala = Math.Min(1f, disponible / total);
            if (escala < 0.62f && _canecas is not null)
            {
                // Sin alto para todo: se quitan las canecas antes que reducir el lema hasta que no se lea.
                hCanecas = 0;
                total = hLogo + separacion + hLema;
                escala = Math.Min(1f, disponible / total);
            }
            float libre = disponible - total * escala;
            float hueco = separacion * escala + Math.Min(libre * 0.25f, P(34));
            float y = arriba + Math.Max(0, libre - 2 * (hueco - separacion * escala)) * 0.35f;
            float centro = (izquierda + derecha) / 2f;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            y = Dibujar(g, _logo, centro, y, wLogo * escala) + hueco;
            y = Dibujar(g, _lema, centro, y, wLema * escala) + hueco;
            if (hCanecas > 0 && _canecas is not null)
            {
                float w = wCanecas * escala, h = w * _canecas.Height / _canecas.Width;
                _zonaCanecas = new RectangleF(centro - w / 2f, Math.Max(y, abajo - h), w, h);
                g.DrawImage(_canecas, _zonaCanecas);
            }
        }

        private static float Dibujar(Graphics g, Image imagen, float centro, float y, float ancho)
        {
            float alto = ancho * imagen.Height / imagen.Width;
            g.DrawImage(imagen, centro - ancho / 2f, y, ancho, alto);
            return y + alto;
        }

        // Olas verde lima en las esquinas inferiores, como en el diseño de referencia.
        private void PintarOlas(Graphics g)
        {
            using var lima = new SolidBrush(Color.FromArgb(170, 0x6C, 0xBF, 0x2E));
            using (var ola = new System.Drawing.Drawing2D.GraphicsPath())
            {
                ola.AddBezier(0, Height * 0.86f, Width * 0.05f, Height * 0.90f, Width * 0.09f, Height * 0.96f, Width * 0.14f, Height);
                ola.AddLine(Width * 0.14f, Height, 0, Height);
                ola.CloseFigure();
                g.FillPath(lima, ola);
            }
            using (var ola = new System.Drawing.Drawing2D.GraphicsPath())
            {
                ola.AddBezier(Width, Height * 0.82f, Width * 0.95f, Height * 0.89f, Width * 0.91f, Height * 0.95f, Width * 0.85f, Height);
                ola.AddLine(Width * 0.85f, Height, Width, Height);
                ola.CloseFigure();
                g.FillPath(lima, ola);
            }
        }

        // Franja blanca inferior con los cuatro beneficios.
        private void PintarFranja(Graphics g)
        {
            int margen = P(90);
            var franja = new RectangleF(margen, Height - AltoFranja - P(14), Width - 2 * margen, AltoFranja);
            using (var sombra = RecursosEco.Redondeado(new RectangleF(franja.X, franja.Y + P(4), franja.Width, franja.Height), P(30)))
            using (var gris = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                g.FillPath(gris, sombra);
            using (var ruta = RecursosEco.Redondeado(franja, P(30)))
            using (var blanco = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
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
            if (disposing) { _beneficio.Dispose(); _ilustracion?.Dispose(); _ayuda.Dispose(); }
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
