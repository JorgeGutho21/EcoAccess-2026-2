using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Portada del inicio (diseño de referencia ExpoSoftware): fondo verde con hojas, el campus a la derecha, el saludo y,
    /// encajados abajo, los indicadores personales con ECO sosteniendo una botella entre el tercero y el cuarto. Los
    /// indicadores son controles hijos (datos reales de Oracle y pulsables); la portada solo los coloca y pinta el fondo.
    /// </summary>
    internal sealed class PortadaInicio : Panel
    {
        private readonly string _titulo, _texto;
        private readonly Image? _eco = RecursosEco.Imagen("eco/eco_botella.png");
        private readonly Image? _campus = RecursosEco.Imagen("eco/campus.jpg");
        private readonly IReadOnlyList<Control> _indicadores;
        private readonly Control? _invitacion;
        internal static readonly Font FuenteTitulo = new("Segoe UI", 34, FontStyle.Bold | FontStyle.Italic);
        internal static readonly Font FuenteTexto = new("Segoe UI", 14);

        public PortadaInicio(string titulo, string texto, IReadOnlyList<Control> indicadores, Control? invitacion)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            _titulo = titulo;
            _texto = texto;
            _indicadores = indicadores;
            _invitacion = invitacion;
            BackColor = Color.FromArgb(0xEE, 0xF8, 0xE8);
            foreach (Control c in indicadores) Controls.Add(c);
            if (invitacion is not null) Controls.Add(invitacion);
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = $"{titulo} {texto}";
        }

        private int P(int u) => TemaEcoAccess.Px(this, u);

        /// <summary>Alto que necesita la portada con ese ancho.</summary>
        internal int MedirAlto(int ancho)
        {
            int cabecera = P(176);
            int tarjetas = _indicadores.Count > 0 ? _indicadores.Max(c => c is TarjetaDibujada t ? t.MedirAlto(AnchoTarjeta(ancho)) : c.Height) : (_invitacion?.GetPreferredSize(new Size(3 * AnchoTarjeta(ancho), 0)).Height ?? 0);
            return cabecera + tarjetas + P(18);
        }

        private int AnchoTarjeta(int ancho) => Math.Max(P(180), (ancho - 2 * P(24) - 3 * P(16)) / 5);

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int margen = P(24), espacio = P(16), y = P(176);
            int ancho = AnchoTarjeta(Width);
            // Tres a la izquierda, el hueco de ECO y la cuarta a la derecha.
            for (int i = 0; i < _indicadores.Count; i++)
            {
                int x = i < 3 ? margen + i * (ancho + espacio) : Width - margen - ancho;
                Control c = _indicadores[i];
                c.Bounds = new Rectangle(x, y, ancho, c is TarjetaDibujada t ? t.MedirAlto(ancho) : c.Height);
            }
            if (_invitacion is not null)
            {
                int anchoInv = Math.Min(Width - 2 * margen, 3 * ancho + 2 * espacio);
                _invitacion.Bounds = new Rectangle(margen, y, anchoInv, _invitacion.GetPreferredSize(new Size(anchoInv, 0)).Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using (var fondo = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), Color.FromArgb(0xE2, 0xF4, 0xD6), Color.FromArgb(0xF6, 0xFB, 0xF2), 0f))
                g.FillRectangle(fondo, ClientRectangle);
            // Campus a la derecha, difuminado hacia la izquierda.
            if (_campus is not null)
            {
                int anchoFoto = Width * 26 / 100, altoFoto = P(260);
                var destino = new Rectangle(Width - anchoFoto, 0, anchoFoto, altoFoto);
                // Solo la franja de edificios de la foto (entre el 36 % y el 62 % de su alto), sin logotipos ni personajes.
                var franja = new RectangleF(0, _campus.Height * 0.36f, _campus.Width, _campus.Height * 0.26f);
                float k = Math.Max(destino.Width / franja.Width, destino.Height / franja.Height);
                var origen = new RectangleF(franja.X + (franja.Width - destino.Width / k) / 2, franja.Y, destino.Width / k, destino.Height / k);
                g.DrawImage(_campus, destino, origen, GraphicsUnit.Pixel);
                using var difuminado = new LinearGradientBrush(destino, Color.FromArgb(255, 0xEC, 0xF7, 0xE4), Color.FromArgb(40, 0xEC, 0xF7, 0xE4), 0f);
                g.FillRectangle(difuminado, destino);
                using var abajo = new LinearGradientBrush(new Rectangle(destino.X, destino.Bottom - P(80), destino.Width, P(80)), Color.FromArgb(0, 0xEE, 0xF8, 0xE8), Color.FromArgb(255, 0xEE, 0xF8, 0xE8), 90f);
                g.FillRectangle(abajo, destino.X, destino.Bottom - P(80), destino.Width, P(81));
            }
            // Ola y hojas.
            using (var ola = new GraphicsPath())
            {
                ola.AddBezier(0, P(150), Width * 0.25f, P(95), Width * 0.45f, P(235), Width * 0.7f, P(170));
                ola.AddLine(Width * 0.7f, P(170), Width * 0.7f, P(260));
                ola.AddLine(Width * 0.7f, P(260), 0, P(260));
                ola.CloseFigure();
                using var pincel = new SolidBrush(Color.FromArgb(70, 0x8D, 0xC6, 0x3F));
                g.FillPath(pincel, ola);
            }
            foreach (var (fx, fy, tam, giro) in new[] { (0.005f, 0.06f, 110, -35), (0.53f, 0.24f, 58, 25), (0.36f, 0.06f, 34, 60) })
            {
                Bitmap hoja = CacheIconos.Obtener(IconChar.Leaf, P(tam), Color.FromArgb(120, 0x6C, 0xBF, 0x2E));
                var estado = g.Save();
                g.TranslateTransform(Width * fx + hoja.Width / 2f, Height * fy + hoja.Height / 2f);
                g.RotateTransform(giro);
                g.DrawImage(hoja, -hoja.Width / 2f, -hoja.Height / 2f);
                g.Restore(estado);
            }
            // Saludo.
            int x = Math.Min(P(150), Width / 9);
            int ancho = Math.Max(P(240), Width * 55 / 100 - x);
            int y = P(26);
            y += DibujoEco.Texto(g, _titulo, FuenteTitulo, TemaEcoAccess.VerdeOscuro, x, y, ancho) + P(2);
            DibujoEco.Texto(g, _texto, FuenteTexto, TemaEcoAccess.Texto, x, y, Math.Min(ancho, P(780)));
            // ECO con la botella entre la tercera y la cuarta tarjeta, de arriba hasta el borde inferior de las tarjetas.
            if (_eco is not null)
            {
                int anchoTarjeta = AnchoTarjeta(Width);
                int izquierda = P(24) + 3 * (anchoTarjeta + P(16));
                int derecha = Width - P(24) - anchoTarjeta - P(8);
                int altoEco = Height - P(4);
                int anchoEco = altoEco * _eco.Width / _eco.Height;
                if (anchoEco > derecha - izquierda + P(80)) { anchoEco = derecha - izquierda + P(80); altoEco = anchoEco * _eco.Height / _eco.Width; }
                g.DrawImage(_eco, izquierda + (derecha - izquierda - anchoEco) / 2, Height - altoEco - P(4), anchoEco, altoEco);
            }
        }
    }

    /// <summary>
    /// Resultado de la búsqueda (diseño de referencia): ilustración del residuo, nombre, caneca en su color, flecha y la
    /// caneca dibujada; debajo, la acción de depositar. Para algo fuera del catálogo muestra la advertencia y no ofrece caneca.
    /// </summary>
    internal sealed class PanelResultado : Panel
    {
        private readonly ImagenAjustada _imagen = new();
        private readonly Label _etiqueta = new() { Text = "Resultado de la búsqueda", AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Verde, BackColor = Color.White };
        private readonly Label _nombre = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 20), ForeColor = TemaEcoAccess.Texto, BackColor = Color.White };
        private readonly RichTextBox _detalle = new() { BorderStyle = BorderStyle.None, ReadOnly = true, BackColor = Color.White, ScrollBars = RichTextBoxScrollBars.None, TabStop = false, Font = TemaEcoAccess.Entrada, Cursor = Cursors.Default };
        private readonly KryptonButton _depositar = new() { AutoSize = true };
        private readonly LinkLabel _verMas = new() { AutoSize = true, LinkColor = TemaEcoAccess.Verde, ActiveLinkColor = TemaEcoAccess.VerdeOscuro, BackColor = Color.White };
        private CategoriaMaterial? _caneca;
        private bool _advertencia;
        private readonly ToolTip _ayudaDetalle = new();

        internal event EventHandler? Depositar;
        internal event EventHandler? VerMas;

        public PanelResultado()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(0xEE, 0xF8, 0xE8);
            TemaEcoAccess.EstilizarAccion(_depositar, principal: true);
            _depositar.Click += (_, _) => Depositar?.Invoke(this, EventArgs.Empty);
            _verMas.LinkClicked += (_, _) => VerMas?.Invoke(this, EventArgs.Empty);
            Controls.AddRange(new Control[] { _imagen, _etiqueta, _nombre, _detalle, _depositar, _verMas });
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = "Resultado de la búsqueda";
        }

        private int P(int u) => TemaEcoAccess.Px(this, u);

        internal void Mostrar(ResiduoPrototipo residuo, bool invitado, int coincidencias)
        {
            _advertencia = false;
            _caneca = residuo.Caneca;
            _imagen.Imagen = RecursosEco.Residuo(residuo);
            _imagen.AccessibleName = residuo.Nombre;
            _nombre.Text = residuo.Nombre;
            string caneca = CatalogoPrototipo.NombreCaneca(residuo.Caneca).ToLowerInvariant();
            string descripcion = CatalogoPrototipo.Canecas.First(c => c.Caneca == residuo.Caneca).Descripcion.ToLowerInvariant();
            _detalle.Clear();
            _detalle.SelectionColor = TemaEcoAccess.Texto;
            _detalle.AppendText("Deposítalo en la ");
            _detalle.SelectionFont = new Font(TemaEcoAccess.Entrada, FontStyle.Bold);
            _detalle.SelectionColor = residuo.Caneca == CategoriaMaterial.Aprovechable ? TemaEcoAccess.Verde : EstiloCaneca.Para(residuo.Caneca).Cuerpo;
            _detalle.AppendText(caneca);
            _detalle.SelectionFont = TemaEcoAccess.Entrada;
            _detalle.SelectionColor = TemaEcoAccess.Texto;
            _detalle.AppendText($" ({descripcion}).");
            _ayudaDetalle.SetToolTip(_detalle, residuo.Instrucciones);
            _depositar.Values.Text = invitado ? "Depositar como invitado" : "Depositar este residuo";
            _depositar.Visible = true;
            _verMas.Text = coincidencias > 1 ? $"Ver los {coincidencias} resultados" : "";
            _verMas.Visible = coincidencias > 1;
            AccessibleName = $"Resultado: {residuo.Nombre}, va en la {caneca}.";
            PerformLayout();
            Invalidate();
        }

        internal void MostrarAdvertencia(string texto)
        {
            _advertencia = true;
            _caneca = null;
            _imagen.Imagen = null;
            _nombre.Text = $"«{texto.Trim()}» no va en estas canecas";
            _detalle.Clear();
            _detalle.SelectionColor = TemaEcoAccess.Texto;
            _detalle.AppendText(CatalogoPrototipo.MensajeFueraDeCatalogo);
            _depositar.Visible = false;
            _verMas.Visible = false;
            AccessibleName = _nombre.Text;
            PerformLayout();
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int alto = Height - P(6), lado = Math.Min(P(150), alto - P(28));
            _imagen.Bounds = new Rectangle(P(26), (alto - lado) / 2, lado, lado);
            int x = P(26) + lado + P(34);
            int ancho = Math.Max(P(160), Width - x - (_caneca is null ? P(24) : P(250)));
            int y = P(20);
            _etiqueta.Location = new Point(x, y); y += _etiqueta.Height + P(2);
            _nombre.MaximumSize = new Size(ancho, 0);
            _nombre.Location = new Point(x, y); y += _nombre.Height + P(4);
            int altoDetalle = DibujoEco.AltoTexto(_detalle.Text, TemaEcoAccess.Entrada, ancho) + P(4);
            _detalle.Bounds = new Rectangle(x, y, ancho, altoDetalle); y += altoDetalle + P(6);
            _depositar.Location = new Point(x, y);
            _verMas.Location = new Point(x + _depositar.Width + P(16), y + (_depositar.Height - _verMas.Height) / 2);
            _etiqueta.Visible = !_advertencia;
            _etiqueta.ForeColor = TemaEcoAccess.Verde;
        }

        internal int MedirAlto(int ancho)
        {
            int x = P(26) + P(150) + P(34);
            int util = Math.Max(P(160), ancho - x - P(250));
            return P(20) + _etiqueta.Height + _nombre.GetPreferredSize(new Size(util, 0)).Height + DibujoEco.AltoTexto(_detalle.Text, TemaEcoAccess.Entrada, util)
                + P(16) + (_depositar.Visible ? _depositar.Height : 0) + P(26);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var cuerpo = new RectangleF(1, 1, Width - 3, Height - P(6));
            for (int capa = 3; capa >= 1; capa--)
            {
                var r = cuerpo; r.Offset(0, capa);
                using var ruta = RecursosEco.Redondeado(r, P(18));
                using var sombra = new SolidBrush(Color.FromArgb(12, 16, 40, 30));
                g.FillPath(sombra, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(cuerpo, P(18)))
            using (var blanco = new SolidBrush(Color.White))
                g.FillPath(blanco, ruta);
            // Fondo verde suave detrás de la ilustración y separador.
            using (var ruta = RecursosEco.Redondeado(new RectangleF(_imagen.Left - P(6), _imagen.Top - P(6), _imagen.Width + P(12), _imagen.Height + P(12)), P(16)))
            using (var suave = new SolidBrush(TemaEcoAccess.VerdeSuave))
                g.FillPath(suave, ruta);
            if (_advertencia)
            {
                Bitmap aviso = CacheIconos.Obtener(IconChar.TriangleExclamation, P(70), TemaEcoAccess.Advertencia);
                g.DrawImage(aviso, _imagen.Left + (_imagen.Width - aviso.Width) / 2, _imagen.Top + (_imagen.Height - aviso.Height) / 2);
            }
            using (var linea = new Pen(TemaEcoAccess.Verde, P(2)))
                g.DrawLine(linea, _imagen.Right + P(18), P(26), _imagen.Right + P(18), Height - P(32));
            if (_caneca is { } caneca)
            {
                // Flecha verde hacia la caneca.
                int cx = Width - P(130), cy = Height / 2;
                using (var flecha = new Pen(Color.FromArgb(0x6C, 0xBF, 0x2E), P(8)) { EndCap = LineCap.ArrowAnchor, StartCap = LineCap.Round })
                using (var ruta = new GraphicsPath())
                {
                    ruta.AddBezier(cx - P(130), cy + P(10), cx - P(100), cy - P(30), cx - P(70), cy - P(30), cx - P(40), cy - P(14));
                    g.DrawPath(flecha, ruta);
                }
                float lado = Math.Min(P(96), Height - P(46));
                DibujoCaneca.Pintar(g, new RectangleF(cx - lado * 0.2f, cy - lado * 0.6f, lado, lado * 1.25f), caneca);
            }
        }
    }

    /// <summary>«Así de fácil»: identifica, búscalo en EcoAccess y deposítalo en la caneca correcta.</summary>
    internal sealed class PasosFacil : Control
    {
        private static readonly (IconChar Icono, string Texto)[] Pasos =
        {
            (IconChar.BottleWater, "Identifica\ntu residuo"),
            (IconChar.MagnifyingGlass, "Búscalo\nen EcoAccess"),
            (IconChar.TrashCan, "Deposítalo\nen la caneca correcta"),
        };

        public PasosFacil()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(0xEE, 0xF8, 0xE8);
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = "Así de fácil: identifica tu residuo, búscalo en EcoAccess y deposítalo en la caneca correcta.";
            TabStop = false;
        }

        private int P(int u) => TemaEcoAccess.Px(this, u);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var cuerpo = new RectangleF(1, 1, Width - 3, Height - P(6));
            for (int capa = 3; capa >= 1; capa--)
            {
                var r = cuerpo; r.Offset(0, capa);
                using var ruta = RecursosEco.Redondeado(r, P(18));
                using var sombra = new SolidBrush(Color.FromArgb(12, 16, 40, 30));
                g.FillPath(sombra, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(cuerpo, P(18)))
            using (var blanco = new SolidBrush(Color.White))
                g.FillPath(blanco, ruta);
            // Pastilla «Así de fácil:».
            Size medida = TextRenderer.MeasureText("Así de fácil:", TemaEcoAccess.Subtitulo);
            var pastilla = new RectangleF(P(26), P(16), medida.Width + P(26), medida.Height + P(10));
            using (var ruta = RecursosEco.Redondeado(pastilla, pastilla.Height / 2))
            using (var verde = new SolidBrush(TemaEcoAccess.Verde))
                g.FillPath(verde, ruta);
            TextRenderer.DrawText(g, "Así de fácil:", TemaEcoAccess.Subtitulo, Rectangle.Round(pastilla), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            float inicio = Math.Max(P(150), Width * 0.18f), ancho = (Width - inicio - P(20)) / Pasos.Length;
            int lado = Math.Min(P(64), (int)(ancho * 0.5f));
            for (int i = 0; i < Pasos.Length; i++)
            {
                float cx = inicio + ancho * i + ancho / 2;
                float cy = P(26) + P(14) + lado / 2f;
                using (var circulo = new SolidBrush(TemaEcoAccess.VerdeSuave)) g.FillEllipse(circulo, cx - lado / 2f, cy - lado / 2f, lado, lado);
                Bitmap icono = CacheIconos.Obtener(Pasos[i].Icono, lado * 55 / 100, TemaEcoAccess.Verde);
                g.DrawImage(icono, cx - icono.Width / 2f, cy - icono.Height / 2f);
                // Número.
                int n = P(24);
                using (var numero = new SolidBrush(TemaEcoAccess.VerdeOscuro)) g.FillEllipse(numero, cx - n / 2f, cy - lado / 2f - n * 0.7f, n, n);
                TextRenderer.DrawText(g, (i + 1).ToString(), TemaEcoAccess.Pequena, new Rectangle((int)(cx - n / 2f), (int)(cy - lado / 2f - n * 0.7f), n, n), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, Pasos[i].Texto, TemaEcoAccess.Cuerpo, new Rectangle((int)(cx - ancho / 2), (int)(cy + lado / 2f + P(6)), (int)ancho, P(48)), TemaEcoAccess.Texto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
                if (i < Pasos.Length - 1)
                {
                    Bitmap flecha = CacheIconos.Obtener(IconChar.ArrowRight, P(18), TemaEcoAccess.Verde);
                    g.DrawImage(flecha, cx + ancho / 2 - flecha.Width / 2f, cy - flecha.Height / 2f);
                }
            }
        }
    }

    /// <summary>Residuos de una caneca (o resultados de una búsqueda) con su ilustración; al elegir uno se cierra.</summary>
    public sealed class FormResiduosCaneca : Form
    {
        public ResiduoPrototipo? Elegido { get; private set; }

        public FormResiduosCaneca(string titulo, string subtitulo, IReadOnlyList<ResiduoPrototipo> residuos)
        {
            Text = titulo + " · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            MinimizeBox = false;
            KeyPreview = true;
            int columnas = Math.Min(3, Math.Max(2, residuos.Count));
            Size = new Size(TemaEcoAccess.Px(this, 140 + columnas * 230), TemaEcoAccess.Px(this, 720));
            var cabecera = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(28, 20, 28, 8), BackColor = TemaEcoAccess.Fondo };
            cabecera.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro });
            cabecera.Controls.Add(new Label { Text = subtitulo, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(3, 4, 3, 0) });
            var desplazable = new PanelDesplazable { Dock = DockStyle.Fill, Padding = new Padding(28, 8, 28, 20), BackColor = TemaEcoAccess.Fondo };
            var rejilla = new RejillaTarjetas(190) { ColumnasMaximas = columnas };
            foreach (ResiduoPrototipo r in residuos)
            {
                var tarjeta = new TarjetaResiduo(r);
                tarjeta.Click += (_, _) => { Elegido = r; DialogResult = DialogResult.OK; };
                rejilla.Controls.Add(tarjeta);
            }
            desplazable.Controls.Add(rejilla);
            desplazable.ClientSizeChanged += (_, _) => rejilla.Adaptar(Math.Max(TemaEcoAccess.Px(this, 300), desplazable.ClientSize.Width - desplazable.Padding.Horizontal));
            Controls.Add(desplazable);
            Controls.Add(cabecera);
            Shown += (_, _) => { rejilla.Adaptar(Math.Max(TemaEcoAccess.Px(this, 300), desplazable.ClientSize.Width - desplazable.Padding.Horizontal)); rejilla.Controls.OfType<TarjetaResiduo>().FirstOrDefault()?.Focus(); };
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }
    }
}
