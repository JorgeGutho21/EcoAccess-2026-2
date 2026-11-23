using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Piezas dibujadas del dashboard de gestión (diseño aprobado del Dashboard del Supervisor): banner de bienvenida, accesos
    /// rápidos, indicadores, encabezados de sección, tarjetas de gráfico, barras, anillos y selector Día/Semana. Todas las medidas
    /// se calculan al pintar con la escala real de la pantalla (<see cref="TemaEcoAccess.Px"/>).
    /// </summary>
    internal static class DibujoDashboard
    {
        /// <summary>Texto del botón «Actualizar datos» del banner (semibold 12,5 pt, como en el diseño).</summary>
        internal static readonly Font FuenteBoton = new("Segoe UI Semibold", 12.5f);
        /// <summary>Encabezados de la tabla «Últimos depósitos» y texto de sus pastillas de estado.</summary>
        internal static readonly Font FuenteEncabezadoTabla = new("Segoe UI Semibold", 9f);
        /// <summary>Texto de las pastillas de estado de la tabla («Pendiente de verificación»).</summary>
        internal static readonly Font FuentePastilla = new("Segoe UI Semibold", 8.25f);

        /// <summary>Serie del anillo de materiales: verde de marca, verde oscuro, verde medio, verde hoja, gris pizarra y ámbar («Otros»).</summary>
        internal static readonly Color[] Serie =
        {
            TemaEcoAccess.Verde, TemaEcoAccess.VerdeOscuro, Color.FromArgb(0x5F, 0xB8, 0x8A), TemaEcoAccess.VerdeHoja,
            Color.FromArgb(0x64, 0x74, 0x8B), Color.FromArgb(0xD9, 0x9A, 0x2B),
        };

        /// <summary>Ámbar de los depósitos pendientes de verificación.</summary>
        internal static readonly Color Pendiente = Color.FromArgb(0xD9, 0x9A, 0x2B);

        internal static int P(Control c, float unidades) => (int)Math.Round(unidades * c.DeviceDpi / 96f);
        internal static Font Fuente(Control c, float px) => FuentesInicio.Obtener(px * c.DeviceDpi / 96f);
        internal static Font Semibold(Control c, float px) => FuentesInicio.Semibold(px * c.DeviceDpi / 96f);

        /// <summary>Tarjeta blanca redondeada con sombra corta (la misma de las tarjetas de ciudadano).</summary>
        internal static void Tarjeta(Graphics g, Control c, Rectangle area, Color relleno, Color borde, float radio)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int capa = 4; capa >= 1; capa--)
            {
                var sombra = RectangleF.Inflate(area, capa / 2f, capa / 2f);
                sombra.Offset(0, 2);
                using var ruta = RecursosEco.Redondeado(sombra, radio + capa / 2f);
                using var pincel = new SolidBrush(Color.FromArgb(Math.Max(2, 10 - capa * 2), 16, 40, 30)); // eco-estetica: permitido sombra translúcida
                g.FillPath(pincel, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(area, radio))
            {
                using (var pincel = new SolidBrush(relleno)) g.FillPath(pincel, ruta);
                using (var lapiz = new Pen(borde)) g.DrawPath(lapiz, ruta);
            }
        }

        internal static void Texto(Graphics g, string texto, Font fuente, Color color, Rectangle area, TextFormatFlags extra = 0) =>
            TextRenderer.DrawText(g, texto, fuente, area, color, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | extra);
    }

    /// <summary>Control con foco y teclado (Enter/Espacio) que se comporta como botón; lo usan los accesos y el «Ver todos».</summary>
    internal abstract class BotonDibujado : Control, IButtonControl
    {
        protected bool Encima, Pulsado;

        protected BotonDibujado()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.StandardClick | ControlStyles.SupportsTransparentBackColor, true);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public DialogResult DialogResult { get; set; }
        public void NotifyDefault(bool value) { }
        public void PerformClick() => OnClick(EventArgs.Empty);

        protected override void OnMouseEnter(EventArgs e) { Encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { Encima = Pulsado = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { Pulsado = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { Pulsado = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space && Enabled) { e.Handled = true; OnClick(EventArgs.Empty); }
            base.OnKeyDown(e);
        }

        protected override void OnTextChanged(EventArgs e) { AccessibleName = Text; Invalidate(); base.OnTextChanged(e); }

        protected bool MostrarFoco => Focused && ShowFocusCues;
    }

    /// <summary>
    /// Banner de bienvenida: degradado verde, Eco con la tableta a la izquierda, el campus difuminado y las tres canecas a la
    /// derecha, saludo, mensaje, botón «Actualizar datos» y hora de la última consulta.
    /// </summary>
    internal sealed class BannerBienvenida : Control
    {
        private string _saludo = "", _mensaje = "", _estado = "";

        internal BannerBienvenida(Button actualizar)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Actualizar = actualizar;
            BackColor = ColorBoton;
            Controls.Add(actualizar);
            AccessibleRole = AccessibleRole.Grouping;
        }

        internal Button Actualizar { get; }

        // Tonos del degradado medidos sobre el diseño aprobado: verde bosque a la izquierda (detrás de Eco), más oscuro detrás
        // del texto y un verde más claro y frío hacia el campus.
        private static readonly Color[] Tonos =
        {
            Color.FromArgb(0x17, 0x5C, 0x2E), // eco-estetica: permitido tono del banner (diseño aprobado)
            Color.FromArgb(0x12, 0x53, 0x2D), // eco-estetica: permitido tono del banner (diseño aprobado)
            Color.FromArgb(0x1A, 0x58, 0x3A), // eco-estetica: permitido tono del banner (diseño aprobado)
            Color.FromArgb(0x25, 0x68, 0x48), // eco-estetica: permitido tono del banner (diseño aprobado)
            Color.FromArgb(0x2A, 0x72, 0x50), // eco-estetica: permitido tono del banner (diseño aprobado)
            Color.FromArgb(0x2A, 0x72, 0x50), // eco-estetica: permitido tono del banner (diseño aprobado)
        };
        private static readonly float[] Posiciones = { 0f, 0.17f, 0.32f, 0.46f, 0.60f, 1f };
        /// <summary>Color del degradado detrás del botón: las esquinas redondeadas del botón se funden con él.</summary>
        private static Color ColorBoton => Tonos[1];
        /// <summary>Color del degradado en la fracción <paramref name="t"/> del ancho (para fundir el campus sin corte).</summary>
        private static Color TonoEn(float t)
        {
            for (int k = 1; k < Posiciones.Length; k++)
                if (t <= Posiciones[k])
                    return Mezclar(Tonos[k - 1], Tonos[k], (t - Posiciones[k - 1]) / (Posiciones[k] - Posiciones[k - 1]));
            return Tonos[Tonos.Length - 1];
        }

        private static Color Mezclar(Color a, Color b, float t) =>
            Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); // eco-estetica: permitido mezcla de dos tokens

        internal string Saludo { get => _saludo; set { _saludo = value; AccessibleName = value; Invalidate(); } }
        internal string Mensaje { get => _mensaje; set { _mensaje = value; AccessibleDescription = value; Invalidate(); } }
        internal string Estado { get => _estado; set { _estado = value; Invalidate(); } }

        private int Izquierda => Ancho(0.20f, 250);
        private int Ancho(float fraccion, int maximo) => Math.Min((int)(Width * fraccion), DibujoDashboard.P(this, maximo));
        private int FinTexto => Width - Ancho(0.27f, 390);

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            using Graphics g = CreateGraphics();
            Rectangle texto = AreaTexto(g, out int yBoton);
            Size preferido = Actualizar.GetPreferredSize(Size.Empty);
            Actualizar.SetBounds(texto.Left, yBoton, Math.Max(preferido.Width, DibujoDashboard.P(this, 200)), DibujoDashboard.P(this, 42));
        }

        /// <summary>Área del saludo y el mensaje; devuelve dónde va la fila del botón.</summary>
        private Rectangle AreaTexto(Graphics g, out int yBoton)
        {
            int x = Izquierda + DibujoDashboard.P(this, 16), ancho = Math.Max(DibujoDashboard.P(this, 200), FinTexto - x - DibujoDashboard.P(this, 16));
            int y = DibujoDashboard.P(this, 22);
            Size titulo = TextRenderer.MeasureText(g, _saludo + "   ", DibujoDashboard.Semibold(this, 29), new Size(ancho, int.MaxValue), TextFormatFlags.WordBreak);
            Size mensaje = TextRenderer.MeasureText(g, _mensaje, DibujoDashboard.Fuente(this, 16), new Size(ancho, int.MaxValue), TextFormatFlags.WordBreak);
            yBoton = y + titulo.Height + DibujoDashboard.P(this, 8) + mensaje.Height + DibujoDashboard.P(this, 16);
            return new Rectangle(x, y, ancho, titulo.Height);
        }

        /// <summary>Alto que necesita el banner para su ancho (crece si el texto ocupa más líneas).</summary>
        internal int AltoNecesario()
        {
            if (!IsHandleCreated) return DibujoDashboard.P(this, 186);
            using Graphics g = CreateGraphics();
            AreaTexto(g, out int yBoton);
            return Math.Max(DibujoDashboard.P(this, 186), yBoton + DibujoDashboard.P(this, 42 + 24));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            PintarFondo(g, this, Izquierda, Ancho(0.42f, 600), conCanecas: true);
            Rectangle texto = AreaTexto(g, out int yBoton);
            Font fuenteTitulo = DibujoDashboard.Semibold(this, 29);
            TextRenderer.DrawText(g, _saludo, fuenteTitulo, texto, Color.White, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            // Hoja después del saludo (al final de su última línea).
            Size ultima = TextRenderer.MeasureText(g, _saludo.Split('\n').Last(), fuenteTitulo, new Size(texto.Width, int.MaxValue), TextFormatFlags.WordBreak);
            if (ultima.Width + DibujoDashboard.P(this, 40) < texto.Width && TextRenderer.MeasureText(g, _saludo, fuenteTitulo).Width <= texto.Width)
            {
                Bitmap hoja = CacheIconos.Obtener(IconChar.Leaf, DibujoDashboard.P(this, 26), TemaEcoAccess.VerdeHoja);
                g.DrawImage(hoja, texto.Left + ultima.Width + DibujoDashboard.P(this, 6), texto.Top + (texto.Height - hoja.Height) / 2);
            }
            var mensaje = new Rectangle(texto.Left, texto.Bottom + DibujoDashboard.P(this, 8), texto.Width, yBoton - texto.Bottom);
            TextRenderer.DrawText(g, _mensaje, DibujoDashboard.Fuente(this, 16), mensaje, Color.FromArgb(240, Color.White), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix); // eco-estetica: permitido blanco translúcido sobre verde
            // Separador vertical y hora de la última consulta, en la fila del botón.
            int xSeparador = Actualizar.Right + DibujoDashboard.P(this, 22);
            using (var linea = new Pen(Color.FromArgb(150, Color.White), Math.Max(1, DibujoDashboard.P(this, 1)))) // eco-estetica: permitido separador translúcido
                g.DrawLine(linea, xSeparador, Actualizar.Top + DibujoDashboard.P(this, 4), xSeparador, Actualizar.Bottom - DibujoDashboard.P(this, 4));
            var estado = Rectangle.FromLTRB(xSeparador + DibujoDashboard.P(this, 20), Actualizar.Top - DibujoDashboard.P(this, 4), texto.Right, Actualizar.Bottom + DibujoDashboard.P(this, 4));
            TextRenderer.DrawText(g, _estado, DibujoDashboard.Fuente(this, 14), estado, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }


        /// <summary>
        /// Fondo de los banners Eco (dashboard y encabezados de módulo): degradado medido sobre el diseño, campus difuminado a la
        /// derecha fundido sin corte, las tres canecas (opcional) y Eco con la tableta a la izquierda.
        /// </summary>
        internal static void PintarFondo(Graphics g, Control c, int anchoEco, int anchoCampus, bool conCanecas)
        {
            int Width = c.Width, Height = c.Height;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(c.Parent?.BackColor ?? TemaEcoAccess.Fondo);
            var area = new RectangleF(0, 0, Width - 1, Height - 3);
            float radio = DibujoDashboard.P(c, 16);
            using var ruta = RecursosEco.Redondeado(area, radio);
            using (var degradado = new LinearGradientBrush(area, Tonos[0], Tonos[Tonos.Length - 1], 0f))
            {
                degradado.InterpolationColors = new ColorBlend { Colors = Tonos, Positions = Posiciones };
                g.FillPath(degradado, ruta);
            }
            Region? anterior = g.Clip;
            g.SetClip(ruta, CombineMode.Intersect);
            // Campus difuminado a la derecha (solo la franja de edificios de la foto), fundido con el degradado.
            if (RecursosEco.Imagen("eco/campus.jpg") is { } campus)
            {

                var destino = new Rectangle(Width - anchoCampus, 0, anchoCampus, Height);
                var origen = new Rectangle(0, (int)(campus.Height * 0.33f), campus.Width, (int)(campus.Width * (float)Height / anchoCampus));
                using var atributos = new System.Drawing.Imaging.ImageAttributes();
                atributos.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.85f });
                g.DrawImage(campus, destino, origen.X, origen.Y, origen.Width, Math.Min(origen.Height, campus.Height - origen.Y), GraphicsUnit.Pixel, atributos);
                // Fundido suave desde el color exacto del degradado en ese punto (sin borde visible) hasta la foto.
                Color borde = TonoEn((float)destino.X / Math.Max(1, Width));
                var franja = new Rectangle(destino.X - 1, 0, (int)(destino.Width * 0.6f) + 2, Height);
                using var fundido = new LinearGradientBrush(franja, borde, Color.FromArgb(0, borde), 0f); // eco-estetica: permitido fundido a transparente
                fundido.WrapMode = WrapMode.TileFlipX; // sin repetir el color sólido al borde del rectángulo
                fundido.Blend = new Blend { Positions = new[] { 0f, 0.15f, 0.55f, 1f }, Factors = new[] { 0f, 0.1f, 0.6f, 1f } };
                g.FillRectangle(fundido, franja);
                // Velo verde ligero sobre toda la foto para que los ladrillos no se salgan de la paleta.
                using var velo = new SolidBrush(Color.FromArgb(70, Tonos[Tonos.Length - 1])); // eco-estetica: permitido velo del tono del banner
                g.FillRectangle(velo, destino);
            }
            // Las tres canecas sobre su base, abajo a la derecha.
            if (conCanecas && RecursosEco.Imagen("eco/canecas_portada.png") is { } canecas)
            {
                int alto = (int)(Height * 0.86f);
                int ancho = canecas.Width * alto / canecas.Height;
                int disponible = Math.Min((int)(Width * 0.27f), DibujoDashboard.P(c, 390));
                if (ancho > disponible) { ancho = disponible; alto = canecas.Height * ancho / canecas.Width; }
                g.DrawImage(CacheEscalado.Obtener(canecas, ancho, alto), Width - ancho - DibujoDashboard.P(c, 10), Height - alto - DibujoDashboard.P(c, 4));
            }
            // Eco con la tableta, a la izquierda y apoyado en el borde inferior.
            // eco_tablet_banner.png = eco_tablet.png sin el velo blanco y con los cuadros de fondo atenuados para este verde.
            if ((RecursosEco.Imagen("eco/eco_tablet_banner.png") ?? RecursosEco.Imagen("eco/eco_tablet.png")) is { } eco)
            {
                int alto = Height - DibujoDashboard.P(c, 6);
                int ancho = eco.Width * alto / eco.Height;
                if (ancho > anchoEco) { ancho = anchoEco; alto = eco.Height * ancho / eco.Width; }
                g.DrawImage(CacheEscalado.Obtener(eco, ancho, alto), DibujoDashboard.P(c, 14), Height - alto - DibujoDashboard.P(c, 3));
            }
            g.Clip = anterior;
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); PerformLayout(); Invalidate(); }
    }

    /// <summary>
    /// Encabezado de un módulo con la estética del banner del dashboard: mismo degradado, Eco con la tableta y el campus
    /// difuminado (sin las canecas, que son del dashboard), el nombre del módulo en grande con su hoja, la descripción y una
    /// línea de estado con icono que el módulo actualiza con su información actual (por ejemplo, la página consultada).
    /// </summary>
    internal sealed class BannerModulo : Control
    {
        private string _titulo, _descripcion, _estado = "";
        private readonly IconChar _iconoEstado;

        internal BannerModulo(string titulo, string descripcion, IconChar iconoEstado = IconChar.CircleInfo)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            _titulo = titulo;
            _descripcion = descripcion;
            _iconoEstado = iconoEstado;
            AccessibleRole = AccessibleRole.Grouping;
            AccessibleName = titulo;
            AccessibleDescription = descripcion;
            Margin = new Padding(0, 0, 0, 14);
            Height = DibujoDashboard.P(this, 160);
        }

        /// <summary>Información actual del módulo (una línea, bajo la descripción).</summary>
        internal string Estado { get => _estado; set { _estado = value ?? ""; Invalidate(); } }

        private int AnchoEco => Math.Min((int)(Width * 0.17f), DibujoDashboard.P(this, 210));
        private int AnchoCampus => Math.Min((int)(Width * 0.45f), DibujoDashboard.P(this, 640));

        private Rectangle AreaTexto => Rectangle.FromLTRB(AnchoEco + DibujoDashboard.P(this, 34), DibujoDashboard.P(this, 22),
            Math.Max(AnchoEco + DibujoDashboard.P(this, 240), Width - (int)(AnchoCampus * 0.55f)), Height - DibujoDashboard.P(this, 16));

        /// <summary>Alto para el ancho actual: crece si la descripción ocupa más líneas.</summary>
        private int AltoNecesario()
        {
            Rectangle area = AreaTexto;
            int titulo = TextRenderer.MeasureText(_titulo, DibujoDashboard.Semibold(this, 29), new Size(area.Width, int.MaxValue), TextFormatFlags.WordBreak).Height;
            int descripcion = TextRenderer.MeasureText(_descripcion, DibujoDashboard.Fuente(this, 16), new Size(area.Width, int.MaxValue), TextFormatFlags.WordBreak).Height;
            int estado = _estado.Length == 0 ? 0 : DibujoDashboard.P(this, 12) + DibujoDashboard.Fuente(this, 14).Height;
            return Math.Max(DibujoDashboard.P(this, 150), area.Top + titulo + DibujoDashboard.P(this, 6) + descripcion + estado + DibujoDashboard.P(this, 26));
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && AltoNecesario() is var alto && alto != Height) Height = alto;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            BannerBienvenida.PintarFondo(g, this, AnchoEco, AnchoCampus, conCanecas: false);
            Rectangle area = AreaTexto;
            Font fuenteTitulo = DibujoDashboard.Semibold(this, 29);
            Size titulo = TextRenderer.MeasureText(g, _titulo, fuenteTitulo, new Size(area.Width, int.MaxValue), TextFormatFlags.WordBreak);
            TextRenderer.DrawText(g, _titulo, fuenteTitulo, new Rectangle(area.Left, area.Top, area.Width, titulo.Height), Color.White, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            if (titulo.Width + DibujoDashboard.P(this, 40) < area.Width)
            {
                Bitmap hoja = CacheIconos.Obtener(IconChar.Leaf, DibujoDashboard.P(this, 26), TemaEcoAccess.VerdeHoja);
                g.DrawImage(hoja, area.Left + titulo.Width + DibujoDashboard.P(this, 8), area.Top + (titulo.Height - hoja.Height) / 2);
            }
            int y = area.Top + titulo.Height + DibujoDashboard.P(this, 6);
            Font fuenteDescripcion = DibujoDashboard.Fuente(this, 16);
            Size descripcion = TextRenderer.MeasureText(g, _descripcion, fuenteDescripcion, new Size(area.Width, int.MaxValue), TextFormatFlags.WordBreak);
            TextRenderer.DrawText(g, _descripcion, fuenteDescripcion, new Rectangle(area.Left, y, area.Width, descripcion.Height),
                Color.FromArgb(240, Color.White), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix); // eco-estetica: permitido blanco translúcido sobre verde
            if (_estado.Length == 0) return;
            y += descripcion.Height + DibujoDashboard.P(this, 12);
            Font fuenteEstado = DibujoDashboard.Fuente(this, 14);
            Bitmap icono = CacheIconos.Obtener(_iconoEstado, DibujoDashboard.P(this, 16), TemaEcoAccess.VerdeHoja);
            g.DrawImage(icono, area.Left, y + (fuenteEstado.Height - icono.Height) / 2);
            TextRenderer.DrawText(g, _estado, fuenteEstado, new Rectangle(area.Left + icono.Width + DibujoDashboard.P(this, 8), y, area.Width - icono.Width, fuenteEstado.Height + 4),
                Color.White, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); OnSizeChanged(e); Invalidate(); }
    }

    /// <summary>Acceso rápido: tarjeta blanca con el icono verde a la izquierda y el nombre en dos líneas.</summary>
    internal sealed class AccesoRapido : BotonDibujado
    {
        private readonly IconChar _icono;

        internal AccesoRapido(string texto, IconChar icono)
        {
            _icono = icono;
            Text = texto;
            BackColor = TemaEcoAccess.Fondo;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? TemaEcoAccess.Fondo);
            int margen = DibujoDashboard.P(this, 3);
            var area = new Rectangle(margen, margen, Width - 2 * margen - 1, Height - 2 * margen - 3);
            DibujoDashboard.Tarjeta(g, this, area, Pulsado ? TemaEcoAccess.VerdeHoja : Encima ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Superficie,
                Encima || MostrarFoco ? TemaEcoAccess.Verde : TemaEcoAccess.Borde, DibujoDashboard.P(this, 12));
            if (MostrarFoco)
                using (var ruta = RecursosEco.Redondeado(RectangleF.Inflate(area, -2, -2), DibujoDashboard.P(this, 10)))
                using (var foco = new Pen(TemaEcoAccess.Verde, 2)) g.DrawPath(foco, ruta);
            int lado = DibujoDashboard.P(this, 28);
            Bitmap icono = CacheIconos.Obtener(_icono, lado, TemaEcoAccess.Verde);
            int x = area.Left + DibujoDashboard.P(this, 16);
            g.DrawImage(icono, x, area.Top + (area.Height - icono.Height) / 2);
            var texto = Rectangle.FromLTRB(x + lado + DibujoDashboard.P(this, 12), area.Top, area.Right - DibujoDashboard.P(this, 8), area.Bottom);
            DibujoDashboard.Texto(g, Text, DibujoDashboard.Semibold(this, 13.5f), TemaEcoAccess.Texto, texto, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    /// <summary>Indicador: icono en cuadro verde suave y título arriba, cifra grande en verde y detalle centrados.</summary>
    internal sealed class IndicadorDashboard : Control
    {
        private readonly IconChar _icono;
        private string _valor = "—", _sufijo = "", _detalle = "";

        internal IndicadorDashboard(string titulo, IconChar icono)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Text = titulo;
            _icono = icono;
            BackColor = TemaEcoAccess.Fondo;
            AccessibleRole = AccessibleRole.StaticText;
            Actualizar();
        }

        internal void Mostrar(string valor, string detalle, string sufijo = "")
        {
            _valor = valor; _detalle = detalle; _sufijo = sufijo;
            Actualizar();
            Invalidate();
        }

        private void Actualizar()
        {
            AccessibleName = $"{Text}: {_valor}{(_sufijo.Length > 0 ? " " + _sufijo : "")}";
            AccessibleDescription = _detalle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? TemaEcoAccess.Fondo);
            int m = DibujoDashboard.P(this, 3);
            var area = new Rectangle(m, m, Width - 2 * m - 1, Height - 2 * m - 3);
            DibujoDashboard.Tarjeta(g, this, area, TemaEcoAccess.Superficie, TemaEcoAccess.Borde, DibujoDashboard.P(this, 12));
            int lado = DibujoDashboard.P(this, 36), x = area.Left + DibujoDashboard.P(this, 14), y = area.Top + DibujoDashboard.P(this, 14);
            using (var ruta = RecursosEco.Redondeado(new RectangleF(x, y, lado, lado), DibujoDashboard.P(this, 9)))
            using (var fondo = new SolidBrush(TemaEcoAccess.VerdeSuave)) g.FillPath(fondo, ruta);
            Bitmap icono = CacheIconos.Obtener(_icono, lado * 55 / 100, TemaEcoAccess.Verde);
            g.DrawImage(icono, x + (lado - icono.Width) / 2, y + (lado - icono.Height) / 2);
            var titulo = Rectangle.FromLTRB(x + lado + DibujoDashboard.P(this, 10), y - DibujoDashboard.P(this, 3), area.Right - DibujoDashboard.P(this, 6), y + lado + DibujoDashboard.P(this, 5));
            DibujoDashboard.Texto(g, Text, DibujoDashboard.Semibold(this, 13.5f), TemaEcoAccess.Texto, titulo, TextFormatFlags.VerticalCenter);

            // Cifra y sufijo («6 de 9») centrados como un solo bloque.
            Font grande = DibujoDashboard.Semibold(this, 30), pequena = DibujoDashboard.Semibold(this, 19);
            int yValor = y + lado + DibujoDashboard.P(this, 8);
            Size valor = TextRenderer.MeasureText(g, _valor, grande, Size.Empty, TextFormatFlags.NoPadding);
            Size sufijo = _sufijo.Length == 0 ? Size.Empty : TextRenderer.MeasureText(g, " " + _sufijo, pequena, Size.Empty, TextFormatFlags.NoPadding);
            int xValor = area.Left + (area.Width - valor.Width - sufijo.Width) / 2;
            TextRenderer.DrawText(g, _valor, grande, new Point(xValor, yValor), TemaEcoAccess.VerdeOscuro, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            if (_sufijo.Length > 0)
                TextRenderer.DrawText(g, " " + _sufijo, pequena, new Point(xValor + valor.Width, yValor + valor.Height - sufijo.Height - DibujoDashboard.P(this, 3)), TemaEcoAccess.VerdeOscuro, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            var detalle = Rectangle.FromLTRB(area.Left + DibujoDashboard.P(this, 8), yValor + valor.Height + DibujoDashboard.P(this, 6), area.Right - DibujoDashboard.P(this, 8), area.Bottom - DibujoDashboard.P(this, 6));
            DibujoDashboard.Texto(g, _detalle, DibujoDashboard.Fuente(this, 11.5f), TemaEcoAccess.Suave, detalle, TextFormatFlags.HorizontalCenter);
        }
    }

    /// <summary>Encabezado de sección: icono verde, título y una descripción corta en la misma línea.</summary>
    internal sealed class EncabezadoSeccion : Control
    {
        private readonly IconChar _icono;
        private readonly string _descripcion;

        internal EncabezadoSeccion(string titulo, string descripcion, IconChar icono)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Text = titulo;
            _descripcion = descripcion;
            _icono = icono;
            BackColor = TemaEcoAccess.Fondo;
            AccessibleRole = AccessibleRole.StaticText;
            AccessibleName = titulo;
            AccessibleDescription = descripcion;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            Bitmap icono = CacheIconos.Obtener(_icono, DibujoDashboard.P(this, 24), TemaEcoAccess.Verde);
            g.DrawImage(icono, DibujoDashboard.P(this, 6), (Height - icono.Height) / 2);
            Font titulo = DibujoDashboard.Semibold(this, 23);
            int x = DibujoDashboard.P(this, 6) + icono.Width + DibujoDashboard.P(this, 12);
            Size medida = TextRenderer.MeasureText(g, Text, titulo, Size.Empty, TextFormatFlags.NoPadding);
            int y = (Height - medida.Height) / 2;
            TextRenderer.DrawText(g, Text, titulo, new Point(x, y), TemaEcoAccess.Texto, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            Font descripcion = DibujoDashboard.Fuente(this, 13.5f);
            Size medidaDescripcion = TextRenderer.MeasureText(g, _descripcion, descripcion, Size.Empty, TextFormatFlags.NoPadding);
            var area = new Rectangle(x + medida.Width + DibujoDashboard.P(this, 20), y + medida.Height - medidaDescripcion.Height - DibujoDashboard.P(this, 2),
                Math.Max(1, Width - x - medida.Width - DibujoDashboard.P(this, 24)), medidaDescripcion.Height);
            TextRenderer.DrawText(g, _descripcion, descripcion, area, TemaEcoAccess.Suave, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>
    /// Tarjeta de un gráfico o tabla del dashboard: encabezado (icono y título, y un control opcional a la derecha), contenido y
    /// una nota al pie en un recuadro gris con icono.
    /// </summary>
    internal sealed class TarjetaDashboard : Panel
    {
        private readonly IconChar _icono;
        private readonly Control? _derecha;
        private readonly Control _contenido;
        private readonly IconChar _iconoNota;
        private string _nota = "";

        internal TarjetaDashboard(string titulo, IconChar icono, Control contenido, Control? derecha = null, IconChar iconoNota = IconChar.CircleInfo, bool tituloGrande = false)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Text = titulo;
            _icono = icono;
            _contenido = contenido;
            _derecha = derecha;
            _iconoNota = iconoNota;
            TituloGrande = tituloGrande;
            BackColor = TemaEcoAccess.Fondo;
            AccessibleName = titulo;
            Controls.Add(contenido);
            if (derecha is not null) Controls.Add(derecha);
        }

        internal bool TituloGrande { get; }

        internal string Nota
        {
            get => _nota;
            set { _nota = value; PerformLayout(); Invalidate(); }
        }

        private int Margen => DibujoDashboard.P(this, 3);
        private int Relleno => DibujoDashboard.P(this, 14);
        private int AltoEncabezado => DibujoDashboard.P(this, TituloGrande ? 46 : 42);

        private int AltoNota()
        {
            if (_nota.Length == 0) return 0;
            using Graphics g = CreateGraphics();
            int ancho = Math.Max(1, Width - 2 * (Margen + Relleno) - DibujoDashboard.P(this, 40));
            return TextRenderer.MeasureText(g, _nota, DibujoDashboard.Fuente(this, 11.5f), new Size(ancho, int.MaxValue), TextFormatFlags.WordBreak).Height + DibujoDashboard.P(this, 14);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int x = Margen + Relleno, ancho = Width - 2 * x;
            int y = Margen + DibujoDashboard.P(this, 10) + AltoEncabezado;
            int nota = AltoNota();
            int fondo = Height - Margen - 3 - Relleno - (nota > 0 ? nota + DibujoDashboard.P(this, 10) : 0);
            _contenido.SetBounds(x, y, Math.Max(1, ancho), Math.Max(1, fondo - y));
            if (_derecha is not null)
            {
                Size preferido = _derecha.GetPreferredSize(Size.Empty);
                int alto = Math.Min(preferido.Height, AltoEncabezado);
                _derecha.SetBounds(Width - x - preferido.Width, Margen + DibujoDashboard.P(this, 10) + (AltoEncabezado - alto) / 2, preferido.Width, alto);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? TemaEcoAccess.Fondo);
            var area = new Rectangle(Margen, Margen, Width - 2 * Margen - 1, Height - 2 * Margen - 3);
            DibujoDashboard.Tarjeta(g, this, area, TemaEcoAccess.Superficie, TemaEcoAccess.Borde, DibujoDashboard.P(this, 12));
            int x = area.Left + Relleno, y = area.Top + DibujoDashboard.P(this, 10);
            Bitmap icono = CacheIconos.Obtener(_icono, DibujoDashboard.P(this, TituloGrande ? 22 : 17), TemaEcoAccess.Verde);
            g.DrawImage(icono, x, y + (AltoEncabezado - icono.Height) / 2);
            int limite = _derecha is null ? area.Right - Relleno : _derecha.Left - DibujoDashboard.P(this, 8);
            var titulo = Rectangle.FromLTRB(x + icono.Width + DibujoDashboard.P(this, 10), y, limite, y + AltoEncabezado);
            // El título va en una línea: si no cabe, la letra baja hasta 10,5 px antes de recortarlo con «…».
            float tamano = TituloGrande ? 19 : 12.5f;
            while (tamano > 10.5f && TextRenderer.MeasureText(g, Text, DibujoDashboard.Semibold(this, tamano), Size.Empty, TextFormatFlags.NoPadding).Width > titulo.Width) tamano -= 0.5f;
            DibujoDashboard.Texto(g, Text, DibujoDashboard.Semibold(this, tamano), TemaEcoAccess.Texto, titulo, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            int nota = AltoNota();
            if (nota == 0) return;
            var recuadro = new Rectangle(area.Left + Relleno, area.Bottom - Relleno - nota, area.Width - 2 * Relleno, nota);
            using (var ruta = RecursosEco.Redondeado(recuadro, DibujoDashboard.P(this, 8)))
            using (var fondo = new SolidBrush(TemaEcoAccess.Fondo)) g.FillPath(fondo, ruta);
            Bitmap iconoNota = CacheIconos.Obtener(_iconoNota, DibujoDashboard.P(this, 14), TemaEcoAccess.Verde);
            g.DrawImage(iconoNota, recuadro.Left + DibujoDashboard.P(this, 11), recuadro.Top + (recuadro.Height - iconoNota.Height) / 2);
            var texto = Rectangle.FromLTRB(recuadro.Left + DibujoDashboard.P(this, 34), recuadro.Top, recuadro.Right - DibujoDashboard.P(this, 6), recuadro.Bottom);
            DibujoDashboard.Texto(g, _nota, DibujoDashboard.Fuente(this, 11.5f), TemaEcoAccess.Suave, texto, TextFormatFlags.VerticalCenter);
        }
    }

    /// <summary>Selector de dos opciones en forma de pastilla (Día / Semana); la elegida en verde oscuro con texto blanco.</summary>
    internal sealed class SelectorSegmentado : Control
    {
        private readonly string[] _opciones;
        private int _elegida;

        internal SelectorSegmentado(params string[] opciones)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            _opciones = opciones;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PageTabList;
            AccessibleName = "Agrupar la actividad por " + string.Join(" o ", opciones);
        }

        internal event EventHandler? Cambio;

        internal int Elegida
        {
            get => _elegida;
            set { if (value == _elegida || value < 0 || value >= _opciones.Length) return; _elegida = value; AccessibleDescription = _opciones[value]; Invalidate(); Cambio?.Invoke(this, EventArgs.Empty); }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int ancho = _opciones.Sum(o => TextRenderer.MeasureText(o, DibujoDashboard.Semibold(this, 11)).Width + DibujoDashboard.P(this, 12));
            return new Size(ancho + DibujoDashboard.P(this, 6), DibujoDashboard.P(this, 28));
        }

        private Rectangle Opcion(int i)
        {
            int ancho = (Width - DibujoDashboard.P(this, 6)) / _opciones.Length;
            return new Rectangle(DibujoDashboard.P(this, 3) + i * ancho, DibujoDashboard.P(this, 3), ancho, Height - DibujoDashboard.P(this, 6));
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            for (int i = 0; i < _opciones.Length; i++) if (Opcion(i).Contains(e.Location)) Elegida = i;
            Focus();
            base.OnMouseClick(e);
        }

        protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) { Elegida = Math.Max(0, _elegida - 1); e.Handled = true; }
            if (e.KeyCode == Keys.Right) { Elegida = Math.Min(_opciones.Length - 1, _elegida + 1); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(TemaEcoAccess.Superficie);
            using (var ruta = RecursosEco.Redondeado(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), (Height - 1) / 2f))
            using (var fondo = new SolidBrush(TemaEcoAccess.Fondo))
            using (var borde = new Pen(Focused && ShowFocusCues ? TemaEcoAccess.Verde : TemaEcoAccess.Borde))
            {
                g.FillPath(fondo, ruta);
                g.DrawPath(borde, ruta);
            }
            for (int i = 0; i < _opciones.Length; i++)
            {
                Rectangle r = Opcion(i);
                if (i == _elegida)
                    using (var ruta = RecursosEco.Redondeado(r, r.Height / 2f))
                    using (var relleno = new SolidBrush(TemaEcoAccess.VerdeOscuro)) g.FillPath(relleno, ruta);
                TextRenderer.DrawText(g, _opciones[i], DibujoDashboard.Semibold(this, 11), r, i == _elegida ? Color.White : TemaEcoAccess.Texto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }

    /// <summary>Botón en pastilla verde suave con flecha (por ejemplo «Ver todos ›»).</summary>
    internal sealed class EnlacePastilla : BotonDibujado
    {
        internal EnlacePastilla(string texto)
        {
            Text = texto;
            BackColor = TemaEcoAccess.Superficie;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size texto = TextRenderer.MeasureText(Text, DibujoDashboard.Semibold(this, 13));
            return new Size(texto.Width + DibujoDashboard.P(this, 40), DibujoDashboard.P(this, 30));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(TemaEcoAccess.Superficie);
            using (var ruta = RecursosEco.Redondeado(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), (Height - 1) / 2f))
            {
                using (var fondo = new SolidBrush(Pulsado ? TemaEcoAccess.VerdeHoja : Encima ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.VerdeSuave)) g.FillPath(fondo, ruta);
                if (MostrarFoco) using (var foco = new Pen(TemaEcoAccess.Verde, 2)) g.DrawPath(foco, ruta);
            }
            Font fuente = DibujoDashboard.Semibold(this, 13);
            Size medida = TextRenderer.MeasureText(g, Text, fuente, Size.Empty, TextFormatFlags.NoPadding);
            Bitmap flecha = CacheIconos.Obtener(IconChar.ChevronRight, DibujoDashboard.P(this, 10), TemaEcoAccess.VerdeOscuro);
            int x = (Width - medida.Width - DibujoDashboard.P(this, 8) - flecha.Width) / 2;
            TextRenderer.DrawText(g, Text, fuente, new Point(x, (Height - medida.Height) / 2), TemaEcoAccess.VerdeOscuro, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            g.DrawImage(flecha, x + medida.Width + DibujoDashboard.P(this, 8), (Height - flecha.Height) / 2);
        }
    }

    /// <summary>Columnas por día o semana: depósitos en verde y verificados en verde hoja, con guías horizontales.</summary>
    internal sealed class GraficoBarras : Control
    {
        private string[] _etiquetas = Array.Empty<string>();
        private int[] _depositos = Array.Empty<int>(), _verificados = Array.Empty<int>();
        private string _vacio = "Consulta los datos para ver la actividad.";

        internal GraficoBarras()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.Chart;
            AccessibleName = "Depósitos por día o semana";
        }

        internal void Mostrar(string[] etiquetas, int[] depositos, int[] verificados)
        {
            _etiquetas = etiquetas; _depositos = depositos; _verificados = verificados;
            AccessibleDescription = string.Join("; ", etiquetas.Select((e, i) => $"{e}: {depositos[i]} depósitos, {verificados[i]} verificados"));
            Invalidate();
        }

        internal void Vaciar(string mensaje)
        {
            _etiquetas = Array.Empty<string>(); _depositos = _verificados = Array.Empty<int>(); _vacio = mensaje;
            AccessibleDescription = mensaje;
            Invalidate();
        }

        /// <summary>Máximo del eje redondeado a 5, 10, 25… para que las guías caigan en números limpios.</summary>
        private static int Escala(int maximo)
        {
            foreach (int paso in new[] { 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2500, 5000 })
                if (maximo <= paso * 5) return paso;
            return (int)Math.Ceiling(maximo / 5.0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Font fuente = DibujoDashboard.Fuente(this, 10.5f);
            if (_depositos.Length == 0 || _depositos.All(d => d == 0) && _verificados.All(v => v == 0))
            {
                DibujoDashboard.Texto(g, _depositos.Length == 0 ? _vacio : "Sin depósitos en el período consultado.", DibujoDashboard.Fuente(this, 13), TemaEcoAccess.Suave,
                    ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            int paso = Escala(Math.Max(1, Math.Max(_depositos.Max(), _verificados.Max())));
            int maximo = paso * 5;
            int anchoEje = TextRenderer.MeasureText(g, maximo.ToString(), fuente).Width + DibujoDashboard.P(this, 2);
            int altoEtiqueta = fuente.Height + DibujoDashboard.P(this, 6);
            var trazo = Rectangle.FromLTRB(anchoEje + DibujoDashboard.P(this, 6), fuente.Height / 2, Width - DibujoDashboard.P(this, 4), Height - altoEtiqueta);
            using (var guia = new Pen(TemaEcoAccess.Borde))
                for (int i = 0; i <= 5; i++)
                {
                    int y = trazo.Bottom - trazo.Height * i / 5;
                    g.DrawLine(guia, trazo.Left, y, trazo.Right, y);
                    TextRenderer.DrawText(g, (paso * i).ToString(), fuente, new Rectangle(0, y - fuente.Height / 2, anchoEje, fuente.Height), TemaEcoAccess.Suave,
                        TextFormatFlags.Right | TextFormatFlags.NoPadding);
                }
            int n = _depositos.Length;
            float hueco = (float)trazo.Width / n;
            float barra = Math.Max(1.5f, Math.Min(hueco * 0.32f, DibujoDashboard.P(this, 12)));
            int cadaCuanto = Math.Max(1, (int)Math.Ceiling(TextRenderer.MeasureText("00/00", fuente, Size.Empty, TextFormatFlags.NoPadding).Width * 1.1f / hueco));
            using var depositos = new SolidBrush(TemaEcoAccess.Verde);
            using var verificados = new SolidBrush(TemaEcoAccess.VerdeHoja);
            for (int i = 0; i < n; i++)
            {
                float centro = trazo.Left + hueco * (i + 0.5f);
                foreach ((int valor, Brush pincel, float x) in new[] { (_depositos[i], (Brush)depositos, centro - barra - 1), (_verificados[i], verificados, centro + 1) })
                {
                    if (valor <= 0) continue;
                    float alto = Math.Max(2f, trazo.Height * (float)valor / maximo);
                    using var ruta = RecursosEco.Redondeado(new RectangleF(x, trazo.Bottom - alto, barra, alto + barra / 2), barra / 2);
                    g.SetClip(new RectangleF(x - 1, trazo.Top - 2, barra + 2, trazo.Height + 2));
                    g.FillPath(pincel, ruta);
                    g.ResetClip();
                }
                if (i % cadaCuanto == 0)
                    TextRenderer.DrawText(g, _etiquetas[i], fuente, new Rectangle((int)(centro - hueco * cadaCuanto / 2f), trazo.Bottom + DibujoDashboard.P(this, 5), (int)(hueco * cadaCuanto), altoEtiqueta),
                        TemaEcoAccess.Suave, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            }
        }
    }

    /// <summary>Anillo con el total al centro y la leyenda a la derecha o debajo.</summary>
    internal sealed class GraficoAnillo : Control
    {
        private (string Nombre, long Valor, Color Color)[] _partes = Array.Empty<(string, long, Color)>();
        private string _centro = "—", _subtitulo = "Depósitos", _vacio = "Consulta los datos para ver el gráfico.";

        internal GraficoAnillo(bool leyendaDerecha)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            LeyendaDerecha = leyendaDerecha;
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.Chart;
        }

        internal bool LeyendaDerecha { get; }

        /// <summary>Con <paramref name="valoresEnLeyenda"/> la leyenda muestra la cantidad: «Verificados (58)».</summary>
        internal bool ValoresEnLeyenda { get; set; }

        internal void Mostrar(IEnumerable<(string Nombre, long Valor, Color Color)> partes, long total, string subtitulo = "Depósitos")
        {
            _partes = partes.Where(p => p.Valor > 0).ToArray();
            _centro = total.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-CO"));
            _subtitulo = subtitulo;
            AccessibleDescription = $"{_centro} {subtitulo}: " + string.Join("; ", _partes.Select(p => $"{p.Nombre} {p.Valor}"));
            Invalidate();
        }

        internal void Vaciar(string mensaje)
        {
            _partes = Array.Empty<(string, long, Color)>(); _vacio = mensaje; AccessibleDescription = mensaje;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            if (_partes.Length == 0)
            {
                DibujoDashboard.Texto(g, _vacio, DibujoDashboard.Fuente(this, 13), TemaEcoAccess.Suave, ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            Font fuenteLeyenda = DibujoDashboard.Fuente(this, 10.5f);
            string Rotulo((string Nombre, long Valor, Color Color) p) => ValoresEnLeyenda ? $"{p.Nombre} ({p.Valor:N0})" : p.Nombre;
            int punto = DibujoDashboard.P(this, 9), sangria = punto + DibujoDashboard.P(this, 7), espacio = DibujoDashboard.P(this, 6);
            Rectangle anillo, leyenda;
            int anchoTexto;
            if (LeyendaDerecha)
            {
                // La leyenda toma hasta la mitad del ancho y cada nombre se parte en líneas si no cabe («Papel de oficina y cuadernos»).
                int anchoNatural = _partes.Max(p => TextRenderer.MeasureText(g, Rotulo(p), fuenteLeyenda, Size.Empty, TextFormatFlags.NoPadding).Width) + sangria;
                int anchoLeyenda = Math.Min((int)(Width * 0.5f), anchoNatural);
                anchoTexto = Math.Max(1, anchoLeyenda - sangria);
                int lado = Math.Min(Width - anchoLeyenda - DibujoDashboard.P(this, 8), Height);
                anillo = new Rectangle(0, (Height - lado) / 2, lado, lado);
                int altoLeyenda = AltoLeyenda(g, fuenteLeyenda, anchoTexto, espacio, Rotulo);
                leyenda = new Rectangle(anillo.Right + DibujoDashboard.P(this, 8), Math.Max(0, (Height - altoLeyenda) / 2), anchoLeyenda, altoLeyenda);
            }
            else
            {
                anchoTexto = Math.Max(1, Math.Min(Width - sangria, _partes.Max(p => TextRenderer.MeasureText(g, Rotulo(p), fuenteLeyenda, Size.Empty, TextFormatFlags.NoPadding).Width)));
                int altoLeyenda = AltoLeyenda(g, fuenteLeyenda, anchoTexto, espacio, Rotulo);
                int lado = Math.Min(Width, Height - altoLeyenda - DibujoDashboard.P(this, 10));
                anillo = new Rectangle((Width - lado) / 2, 0, lado, lado);
                leyenda = new Rectangle(Math.Max(0, (Width - anchoTexto - sangria) / 2), anillo.Bottom + DibujoDashboard.P(this, 10), anchoTexto + sangria, altoLeyenda);
            }
            if (anillo.Width > DibujoDashboard.P(this, 30))
            {
                var circulo = RectangleF.Inflate(anillo, -DibujoDashboard.P(this, 4), -DibujoDashboard.P(this, 4));
                float grosor = circulo.Width * 0.17f;
                var trazo = RectangleF.Inflate(circulo, -grosor / 2, -grosor / 2);
                long total = Math.Max(1, _partes.Sum(p => p.Valor));
                float inicio = -90;
                foreach (var parte in _partes)
                {
                    float barrido = 360f * parte.Valor / total;
                    using var lapiz = new Pen(parte.Color, grosor);
                    g.DrawArc(lapiz, trazo, inicio, Math.Max(0.5f, barrido - (_partes.Length > 1 ? 1.2f : 0)));
                    inicio += barrido;
                }
                Font cifra = DibujoDashboard.Semibold(this, Math.Max(13, circulo.Width * 0.16f * 96f / DeviceDpi));
                Font sub = DibujoDashboard.Fuente(this, Math.Max(10, circulo.Width * 0.075f * 96f / DeviceDpi));
                Size medidaCifra = TextRenderer.MeasureText(g, _centro, cifra, Size.Empty, TextFormatFlags.NoPadding);
                Size medidaSub = TextRenderer.MeasureText(g, _subtitulo, sub, Size.Empty, TextFormatFlags.NoPadding);
                float cy = circulo.Top + circulo.Height / 2f - (medidaCifra.Height + medidaSub.Height) / 2f;
                TextRenderer.DrawText(g, _centro, cifra, new Point((int)(circulo.Left + (circulo.Width - medidaCifra.Width) / 2), (int)cy), TemaEcoAccess.Texto, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, _subtitulo, sub, new Point((int)(circulo.Left + (circulo.Width - medidaSub.Width) / 2), (int)cy + medidaCifra.Height), TemaEcoAccess.Texto, TextFormatFlags.NoPadding);
            }
            int yLinea = leyenda.Top;
            foreach (var parte in _partes)
            {
                Size medida = TextRenderer.MeasureText(g, Rotulo(parte), fuenteLeyenda, new Size(anchoTexto, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.WordBreak);
                using (var pincel = new SolidBrush(parte.Color)) g.FillEllipse(pincel, leyenda.Left, yLinea + (fuenteLeyenda.Height - punto) / 2f, punto, punto);
                TextRenderer.DrawText(g, Rotulo(parte), fuenteLeyenda, new Rectangle(leyenda.Left + sangria, yLinea, anchoTexto, medida.Height),
                    TemaEcoAccess.Texto, TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                yLinea += medida.Height + espacio;
            }
        }

        private int AltoLeyenda(Graphics g, Font fuente, int anchoTexto, int espacio, Func<(string Nombre, long Valor, Color Color), string> rotulo) =>
            _partes.Sum(p => TextRenderer.MeasureText(g, rotulo(p), fuente, new Size(anchoTexto, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.WordBreak).Height + espacio) - espacio;
    }

    /// <summary>Icono FontAwesome suelto, del color indicado (encabezado de la ventana principal).</summary>
    internal sealed class IconoSimple : Control
    {
        private IconChar _icono;

        internal IconoSimple(IconChar icono)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            _icono = icono;
            TabStop = false;
            AccessibleRole = AccessibleRole.Graphic;
        }

        internal IconChar Icono { get => _icono; set { _icono = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? TemaEcoAccess.Superficie);
            Bitmap imagen = CacheIconos.Obtener(_icono, Math.Min(Width, Height), TemaEcoAccess.Verde);
            e.Graphics.DrawImage(imagen, (Width - imagen.Width) / 2, (Height - imagen.Height) / 2);
        }
    }

    /// <summary>Imagen decorativa alineada a una esquina (hojas de la barra lateral). Si el archivo no existe, no ocupa lugar.</summary>
    internal sealed class DecoracionImagen : Control
    {
        private readonly Image? _imagen;
        private readonly ContentAlignment _alineacion;

        internal DecoracionImagen(string ruta, ContentAlignment alineacion)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            _imagen = RecursosEco.Imagen(ruta);
            _alineacion = alineacion;
            Visible = _imagen is not null;
            TabStop = false;
            Enabled = false;
            AccessibleRole = AccessibleRole.Graphic;
            AccessibleName = "Decoración";
        }

        internal bool HayImagen => _imagen is not null;

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? TemaEcoAccess.Superficie);
            if (_imagen is null) return;
            float escala = Math.Min((float)Width / _imagen.Width, (float)Height / _imagen.Height);
            int ancho = (int)(_imagen.Width * escala), alto = (int)(_imagen.Height * escala);
            int x = _alineacion is ContentAlignment.TopRight or ContentAlignment.BottomRight or ContentAlignment.MiddleRight ? Width - ancho : 0;
            int y = _alineacion is ContentAlignment.BottomLeft or ContentAlignment.BottomRight or ContentAlignment.BottomCenter ? Height - alto : 0;
            e.Graphics.DrawImage(CacheEscalado.Obtener(_imagen, Math.Max(1, ancho), Math.Max(1, alto)), x, y);
        }
    }
}
