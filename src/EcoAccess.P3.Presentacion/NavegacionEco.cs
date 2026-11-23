using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Elemento de la barra lateral: fondo redondeado, icono vectorial y texto. La sección activa se marca con fondo
    /// verde suave y texto verde oscuro (no solo con color: también cambia el peso de la letra). Funciona con teclado
    /// (Tab, Enter, Espacio) y muestra un anillo de foco.
    /// </summary>
    internal sealed class BotonNavegacion : Control, IButtonControl
    {
        private IconChar _icono;

        /// <summary>Icono del módulo (lo repite la cabecera de la ventana principal); «Contraer menú» lo cambia al girar.</summary>
        internal IconChar Icono
        {
            get => _icono;
            set { if (_icono == value) return; _icono = value; _imagenNormal?.Dispose(); _imagenNormal = null; Invalidate(); }
        }
        private Bitmap? _imagenNormal;
        private Bitmap? _imagenActiva;
        private bool _activo;
        private bool _encima;
        private bool _contraido;

        public BotonNavegacion(string texto, IconChar icono)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.StandardClick | ControlStyles.SupportsTransparentBackColor, true);
            Text = texto;
            _icono = icono;
            AccessibleName = texto;
            AccessibleRole = AccessibleRole.PushButton;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent; // deja ver la barra (y sus hojas) detrás; eco-estetica: permitido fondo transparente
            Font = TemaEcoAccess.Cuerpo;
            Height = TemaEcoAccess.Px(this, 42);
            Margin = new Padding(0, 1, 0, 1);
            TabStop = true;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public DialogResult DialogResult { get; set; }
        public void NotifyDefault(bool value) { }
        public void PerformClick() => OnClick(EventArgs.Empty);

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Activo
        {
            get => _activo;
            set { _activo = value; AccessibleDescription = value ? "Sección actual" : null; Invalidate(); }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Contraido
        {
            get => _contraido;
            set { _contraido = value; Invalidate(); }
        }

        private void PrepararIconos()
        {
            int lado = TemaEcoAccess.Px(this, 19);
            if (_imagenNormal is not null && _imagenNormal.Width == lado) return;
            _imagenNormal?.Dispose();
            _imagenActiva?.Dispose();
            _imagenNormal = _icono.ToBitmap(IconFont.Solid, lado, TemaEcoAccess.Texto);
            _imagenActiva = _icono.ToBitmap(IconFont.Solid, lado, TemaEcoAccess.Verde);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PrepararIconos();
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Sin Clear: el fondo transparente ya pintó la barra lateral (con sus hojas) detrás del botón.
            var area = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color? fondo = _activo ? TemaEcoAccess.VerdeSuave : _encima ? Color.FromArgb(0xF1, 0xF5, 0xF3) : null;
            if (fondo is { } color)
            {
                using var ruta = RecursosEco.Redondeado(area, TemaEcoAccess.Px(this, 10));
                using var pincel = new SolidBrush(color);
                g.FillPath(pincel, ruta);
            }
            if (Focused && ShowFocusCues && _focoConTeclado)
            {
                using var ruta = RecursosEco.Redondeado(RectangleF.Inflate(area, -1, -1), TemaEcoAccess.Px(this, 10));
                using var anillo = new Pen(TemaEcoAccess.Verde, 2);
                g.DrawPath(anillo, ruta);
            }
            Bitmap icono = _activo ? _imagenActiva! : _imagenNormal!;
            int x = _contraido ? (Width - icono.Width) / 2 : TemaEcoAccess.Px(this, 14);
            g.DrawImage(icono, x, (Height - icono.Height) / 2);
            if (_contraido) return;
            using Font fuente = new(_activo ? "Segoe UI Semibold" : "Segoe UI", 11);
            var texto = new Rectangle(x + icono.Width + TemaEcoAccess.Px(this, 12), 0, Width - x - icono.Width - TemaEcoAccess.Px(this, 16), Height);
            TextRenderer.DrawText(g, Text, fuente, texto, TemaEcoAccess.Texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { Height = TemaEcoAccess.Px(this, 42); Invalidate(); base.OnDpiChangedAfterParent(e); }

        // El anillo de foco aparece solo cuando se navega con teclado (Tab llega como KeyUp al botón que recibe el foco); con
        // el ratón o al abrir la ventana no se dibuja, como en el diseño.
        private static bool _focoConTeclado;
        protected override void OnMouseDown(MouseEventArgs e) { _focoConTeclado = false; Invalidate(); base.OnMouseDown(e); }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (!_focoConTeclado) { _focoConTeclado = true; Invalidate(); }
            if (e.KeyCode is Keys.Enter or Keys.Space) { PerformClick(); e.Handled = true; }
            base.OnKeyUp(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _imagenNormal?.Dispose(); _imagenActiva?.Dispose(); }
            base.Dispose(disposing);
        }
    }

    /// <summary>Círculo con las iniciales de la persona en sesión (sin foto ni datos adicionales).</summary>
    internal sealed class AvatarIniciales : Control
    {
        public AvatarIniciales(string nombre)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Text = string.Concat(nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
            Font = new Font("Segoe UI Semibold", 11);
            TabStop = false;
            AccessibleRole = AccessibleRole.Graphic;
            AccessibleName = "Iniciales de " + nombre;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fondo = new SolidBrush(TemaEcoAccess.VerdeOscuro);
            e.Graphics.FillEllipse(fondo, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
