using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Recursos gráficos incrustados en el ensamblado (sin rutas externas que puedan faltar al ejecutar).</summary>
    internal static class RecursosEco
    {
        private static Image? _eco;

        /// <summary>Imagen oficial de Eco (assets/branding/Eco_perfil_Telegram.jpg). Se carga una vez y se comparte.</summary>
        internal static Image Eco
        {
            get
            {
                if (_eco is not null) return _eco;
                using Stream flujo = typeof(RecursosEco).Assembly.GetManifestResourceStream("EcoAccess.Eco.jpg")
                    ?? throw new InvalidOperationException("Falta el recurso de la mascota Eco.");
                using Image leida = Image.FromStream(flujo);
                _eco = new Bitmap(leida);
                return _eco;
            }
        }

        /// <summary>
        /// Bytes del QR oficial de la estación (assets/identidad/QR_Caneca_piloto.png) tal como está en el repositorio:
        /// se muestra, guarda e imprime sin regenerarlo.
        /// </summary>
        internal static byte[] QrOficialPng()
        {
            using Stream flujo = typeof(RecursosEco).Assembly.GetManifestResourceStream("EcoAccess.QrOficial.png")
                ?? throw new InvalidOperationException("Falta el recurso del QR oficial.");
            using var copia = new MemoryStream();
            flujo.CopyTo(copia);
            return copia.ToArray();
        }

        private static Image? _logo;

        /// <summary>Logotipo oficial de EcoAccess (assets/branding/Logo_EcoAccess.png, fondo transparente) para la aplicación C#.</summary>
        internal static Image Logo
        {
            get
            {
                if (_logo is not null) return _logo;
                using Stream flujo = typeof(RecursosEco).Assembly.GetManifestResourceStream("EcoAccess.Logo.png")
                    ?? throw new InvalidOperationException("Falta el recurso del logotipo de EcoAccess.");
                using Image leida = Image.FromStream(flujo);
                _logo = new Bitmap(leida);
                return _logo;
            }
        }

        private static readonly Dictionary<string, Image?> Imagenes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Ilustración incrustada de assets/imagenes (ruta relativa con «/», por ejemplo «eco/eco_tablet.png»). Se decodifica
        /// una vez y se comparte; si el recurso no existe devuelve null y la pantalla sigue sin imagen en vez de fallar.
        /// </summary>
        internal static Image? Imagen(string ruta)
        {
            lock (Imagenes)
            {
                if (Imagenes.TryGetValue(ruta, out Image? guardada)) return guardada;
                string nombre = "EcoAccess.Img." + ruta.Replace('/', '\\');
                using Stream? flujo = typeof(RecursosEco).Assembly.GetManifestResourceStream(nombre);
                Image? imagen = null;
                if (flujo is not null) using (Image leida = Image.FromStream(flujo)) imagen = new Bitmap(leida);
                Imagenes[ruta] = imagen;
                return imagen;
            }
        }

        /// <summary>Ilustración de un residuo del prototipo: la mano oficial de ECO sosteniendo el material.</summary>
        internal static Image? Residuo(EcoAccess.P3.Logica.Servicios.ResiduoPrototipo residuo) =>
            Imagen("residuos/" + residuo.Imagen.Replace('.', '/') + ".jpg");

        internal static GraphicsPath Redondeado(RectangleF r, float radio)
        {
            float d = Math.Max(1, Math.Min(radio * 2, Math.Min(r.Width, r.Height)));
            var ruta = new GraphicsPath();
            ruta.AddArc(r.X, r.Y, d, d, 180, 90);
            ruta.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            ruta.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            ruta.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            ruta.CloseFigure();
            return ruta;
        }
    }

    /// <summary>
    /// Iconos ya convertidos en imagen. Dibujar un icono FontAwesome cuesta mucho más que copiarlo, y las tarjetas se
    /// repintan al desplazar: se genera una vez por icono, tamaño y color. Las imágenes se comparten: no se liberan.
    /// </summary>
    internal static class CacheIconos
    {
        private static readonly Dictionary<(IconChar, IconFont, int, int), Bitmap> Imagenes = new();

        internal static Bitmap Obtener(IconChar icono, int lado, Color color, IconFont fuente = IconFont.Solid)
        {
            var clave = (icono, fuente, Math.Max(1, lado), color.ToArgb());
            if (!Imagenes.TryGetValue(clave, out Bitmap? imagen)) Imagenes[clave] = imagen = icono.ToBitmap(fuente, clave.Item3, color);
            return imagen;
        }
    }

    /// <summary>
    /// Panel desplazable de los módulos: dibuja sin parpadeo y no salta a otra posición cuando un clic da el foco a un
    /// control hijo. Con el teclado sí acerca el control enfocado, para que Tab nunca lo deje fuera de la vista.
    /// </summary>
    internal class PanelDesplazable : Panel
    {
        public PanelDesplazable()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoScroll = true;
        }

        // Antes solo se evitaba el salto con el ratón presionado; al cambiar de pestaña o restaurar la ventana también saltaba.
        protected override Point ScrollToControl(Control activeControl) =>
            EstabilizadorVentana.NavegandoConTeclado() ? base.ScrollToControl(activeControl) : DisplayRectangle.Location;
    }

    /// <summary>
    /// Tarjeta blanca con esquinas redondeadas y sombra suave desplazada hacia abajo. La sombra se dibuja dentro de
    /// sus propios límites (Margen de sombra), así que los hijos se colocan con Padding y no la tapan.
    /// </summary>
    internal class TarjetaEco : Panel
    {
        internal const int Sombra = 6;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Color Relleno { get; set; } = TemaEcoAccess.Superficie;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Color ColorBorde { get; set; } = TemaEcoAccess.Borde;

        public TarjetaEco()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = TemaEcoAccess.Fondo;
            Padding = new Padding(20 + Sombra);
            Margin = new Padding(0, 0, 12, 12);
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            if (Parent is not null) BackColor = Parent.BackColor;
        }

        private Bitmap? _fondo;
        private (int, int, int, int, int) _claveFondo;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _fondo?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // La sombra y el borde suavizados se dibujan una sola vez por tamaño y color; repintar solo los copia.
            var clave = (Width, Height, Relleno.ToArgb(), ColorBorde.ToArgb(), BackColor.ToArgb());
            if (_fondo is null || clave != _claveFondo)
            {
                _fondo?.Dispose();
                _fondo = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
                using (Graphics lienzo = Graphics.FromImage(_fondo))
                {
                    lienzo.Clear(BackColor);
                    PintarFondo(lienzo);
                }
                _claveFondo = clave;
            }
            e.Graphics.DrawImageUnscaled(_fondo, 0, 0);
            base.OnPaint(e);
        }

        private void PintarFondo(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int s = TemaEcoAccess.Px(this, Sombra);
            float radio = TemaEcoAccess.Px(this, TemaEcoAccess.Radio);
            var cuerpo = new RectangleF(s / 2f, s / 4f, Width - s - 1, Height - s - 1);
            // Sombra difusa: capas concéntricas de baja opacidad, desplazadas 2 px hacia abajo.
            for (int capa = s; capa >= 1; capa--)
            {
                var r = RectangleF.Inflate(cuerpo, capa / 2f, capa / 2f);
                r.Offset(0, 2);
                using var ruta = RecursosEco.Redondeado(r, radio + capa / 2f);
                using var pincel = new SolidBrush(Color.FromArgb(Math.Max(2, 12 - capa * 2), 16, 40, 30));
                g.FillPath(pincel, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(cuerpo, radio))
            using (var relleno = new SolidBrush(Relleno))
            using (var borde = new Pen(ColorBorde))
            {
                g.FillPath(relleno, ruta);
                g.DrawPath(borde, ruta);
            }
        }
    }

    /// <summary>Imagen recortada en círculo o rectángulo redondeado con bordes suavizados (sin dientes de sierra).</summary>
    internal sealed class ImagenRedondeada : Control
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public Image? Imagen { get; set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public bool Circular { get; set; } = true;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public float RadioEsquina { get; set; } = 16;

        public ImagenRedondeada()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;
        }

        private Bitmap? _escalada;
        private (int, int, bool, Image?) _claveEscalada;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _escalada?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Imagen is null || Width <= 1 || Height <= 1) return;
            // Reducir la imagen con la máxima calidad en cada repintado es lo que más frenaba el desplazamiento.
            var clave = (Width, Height, Circular, Imagen);
            if (_escalada is null || clave != _claveEscalada)
            {
                _escalada?.Dispose();
                _escalada = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics lienzo = Graphics.FromImage(_escalada)) PintarImagen(lienzo);
                _claveEscalada = clave;
            }
            e.Graphics.DrawImageUnscaled(_escalada, 0, 0);
        }

        private void PintarImagen(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var destino = new RectangleF(0, 0, Width - 1, Height - 1);
            // Recorte tipo "cover": llena el área conservando la proporción y centrando la imagen.
            Image imagen = Imagen!;
            float escala = Math.Max(destino.Width / imagen.Width, destino.Height / imagen.Height);
            float w = imagen.Width * escala, h = imagen.Height * escala;
            using var ruta = new GraphicsPath();
            if (Circular) ruta.AddEllipse(destino);
            else ruta.AddPath(RecursosEco.Redondeado(destino, TemaEcoAccess.Px(this, (int)RadioEsquina)), false);
            using var textura = new TextureBrush(imagen, WrapMode.Clamp);
            textura.ScaleTransform(escala, escala);
            textura.TranslateTransform((destino.Width - w) / 2f / escala, (destino.Height - h) / 2f / escala, MatrixOrder.Prepend);
            g.FillPath(textura, ruta);
        }
    }

    /// <summary>Pastilla de estado: punto de color y texto sobre fondo suave del mismo tono (no depende solo del color).</summary>
    internal sealed class PastillaEstado : Control
    {
        private Color _tono = TemaEcoAccess.VerdeOscuro;
        private Color _fondo = TemaEcoAccess.VerdeSuave;

        public PastillaEstado()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI Semibold", 9.5f);
            TabStop = false;
        }

        public void Mostrar(string texto, Color tono, Color fondo)
        {
            Text = texto;
            AccessibleName = texto;
            _tono = tono;
            _fondo = fondo;
            Size medida = TextRenderer.MeasureText(texto, Font);
            Size = new Size(medida.Width + TemaEcoAccess.Px(this, 34), medida.Height + TemaEcoAccess.Px(this, 10));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var ruta = RecursosEco.Redondeado(new RectangleF(0, 0, Width - 1, Height - 1), Height / 2f))
            using (var fondo = new SolidBrush(_fondo))
                g.FillPath(fondo, ruta);
            int punto = TemaEcoAccess.Px(this, 8);
            using (var tono = new SolidBrush(_tono))
                g.FillEllipse(tono, TemaEcoAccess.Px(this, 10), (Height - punto) / 2f, punto, punto);
            var area = new Rectangle(TemaEcoAccess.Px(this, 24), 0, Width - TemaEcoAccess.Px(this, 28), Height);
            TextRenderer.DrawText(g, Text, Font, area, _tono, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>Logotipo de EcoAccess escalado sin deformarse (se ajusta al alto o al ancho disponible) y alineado a la izquierda.</summary>
    internal sealed class LogoEcoAccess : Control
    {
        public LogoEcoAccess()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;
            AccessibleRole = AccessibleRole.Graphic;
            AccessibleName = "Logotipo de EcoAccess";
        }

        /// <summary>Alto que conserva la proporción del logotipo para un ancho dado.</summary>
        public static int AltoPara(int ancho) => (int)Math.Round(ancho * (double)RecursosEco.Logo.Height / RecursosEco.Logo.Width);

        protected override void OnPaint(PaintEventArgs e)
        {
            Image logo = RecursosEco.Logo;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float escala = Math.Min((float)Width / logo.Width, (float)Height / logo.Height);
            float w = logo.Width * escala, h = logo.Height * escala;
            e.Graphics.DrawImage(logo, new RectangleF(0, (Height - h) / 2f, w, h));
        }
    }
}
