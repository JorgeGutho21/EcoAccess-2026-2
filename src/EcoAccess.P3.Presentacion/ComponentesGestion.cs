using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Piezas de diseño compartidas por los catálogos de gestión (Usuarios y roles, Materiales): ficha del elemento elegido,
    /// botón suave, texto guía en cajas, tabla con filas de 46 px, encabezado verde suave y estado vacío, y pastillas de
    /// estado o de categoría dibujadas en las celdas. Solo cambian el aspecto; datos y eventos siguen en cada formulario.
    /// </summary>
    internal static class DisenoGestion
    {
        /// <summary>Botón de acción frecuente con relleno verde suave e icono, a todo el ancho.</summary>
        internal static Button BotonSuave(Button boton, IconChar icono)
        {
            EstiloControlesEco.Boton(boton, EstiloControlesEco.TipoBoton.Suave);
            ModuloEco.Boton(boton, icono, ancho: true);
            return boton;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr ventana, int mensaje, IntPtr wParam, string lParam);

        /// <summary>Texto guía gris dentro de una caja de una línea vacía (EM_SETCUEBANNER de Windows; también se ve con el foco).</summary>
        internal static void TextoGuia(TextBox caja, string guia)
        {
            const int EmSetCueBanner = 0x1501;
            void Poner() => SendMessage(caja.Handle, EmSetCueBanner, (IntPtr)1, guia);
            if (caja.IsHandleCreated) Poner();
            caja.HandleCreated += (_, _) => Poner();
        }

        /// <summary>
        /// Ficha verde suave con un icono en círculo blanco y el texto del elemento elegido (el formulario sigue escribiendo la
        /// etiqueta). Una sola tabla de tamaño automático; el texto se limita al ancho de la columna que la contiene.
        /// </summary>
        internal static Control Ficha(Label texto, IconChar icono, string nombreIcono)
        {
            var fila = new TableLayoutPanel
            {
                ColumnCount = 2, RowCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = TemaEcoAccess.Superficie,
                Margin = new Padding(0, 4, 0, 12), Padding = new Padding(14, 12, 14, 12),
            };
            fila.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var ruta = RecursosEco.Redondeado(new RectangleF(0.5f, 0.5f, fila.Width - 1.5f, fila.Height - 1.5f), TemaEcoAccess.Px(fila, 12));
                using var fondo = new SolidBrush(TemaEcoAccess.VerdeSuave);
                using var borde = new Pen(TemaEcoAccess.VerdeHoja);
                e.Graphics.FillPath(fondo, ruta);
                e.Graphics.DrawPath(borde, ruta);
            };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.Controls.Add(new InsigniaIcono(icono, TemaEcoAccess.VerdeOscuro, TemaEcoAccess.Superficie)
            {
                Size = new Size(TemaEcoAccess.Px(texto, 40), TemaEcoAccess.Px(texto, 40)), Margin = new Padding(0, 0, 12, 0), Anchor = AnchorStyles.Top, AccessibleName = nombreIcono,
            }, 0, 0);
            texto.AutoSize = true;
            texto.Margin = new Padding(0, 2, 0, 0);
            texto.ForeColor = TemaEcoAccess.Texto;
            texto.BackColor = TemaEcoAccess.VerdeSuave;
            fila.Controls.Add(texto, 1, 0);
            void Limitar()
            {
                if (fila.Parent is not { } columna) return;
                int ancho = Math.Max(80, columna.ClientSize.Width - columna.Padding.Horizontal - fila.Padding.Horizontal - TemaEcoAccess.Px(fila, 60));
                if (texto.MaximumSize.Width != ancho) texto.MaximumSize = new Size(ancho, 0);
            }
            fila.ParentChanged += (_, _) => { if (fila.Parent is { } p) { p.SizeChanged += (_, _) => Limitar(); Limitar(); } };
            return fila;
        }

        /// <summary>
        /// Tabla con el aspecto del dashboard: filas de 46 px (sin autoajuste), encabezado verde suave, anchos mínimos por columna
        /// (aunque la consulta fije anchos menores) y un estado vacío con icono cuando no hay filas.
        /// </summary>
        internal static void PulirTabla(DataGridView tabla, Func<string, int> anchoMinimo, string mensajeVacio, IconChar iconoVacio)
        {
            tabla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            tabla.RowTemplate.Height = TemaEcoAccess.Px(tabla, 46);
            tabla.EnableHeadersVisualStyles = false;
            tabla.ColumnHeadersDefaultCellStyle.BackColor = TemaEcoAccess.VerdeSuave;
            tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = TemaEcoAccess.VerdeSuave;
            tabla.ColumnHeadersDefaultCellStyle.ForeColor = TemaEcoAccess.VerdeOscuro;
            // Desplazar en horizontal movía los píxeles ya pintados y solo repintaba la franja nueva: con celdas dibujadas
            // a mano quedaban líneas verticales que cortaban el texto (Canecas y QR). Con doble búfer y un repintado completo
            // al desplazar, la tabla siempre se ve limpia.
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(tabla, true);
            tabla.Scroll += (_, e) => { if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll) tabla.Invalidate(); };
            tabla.ColumnAdded += (_, e) =>
            {
                int minimo = anchoMinimo(e.Column.Name);
                if (minimo > 0) e.Column.MinimumWidth = TemaEcoAccess.Px(tabla, minimo);
            };
            var vacio = new EstadoVacioEco(conEco: false, icono: iconoVacio) { Visible = false };
            vacio.Mostrar(mensajeVacio, false);
            tabla.Controls.Add(vacio);
            void Ubicar()
            {
                int arriba = tabla.ColumnHeadersVisible && tabla.ColumnCount > 0 ? tabla.ColumnHeadersHeight : 0;
                vacio.Bounds = new Rectangle(1, arriba + 1, Math.Max(0, tabla.ClientSize.Width - 2), Math.Max(0, tabla.ClientSize.Height - arriba - 2));
                vacio.Visible = tabla.Rows.Count == 0;
            }
            tabla.DataBindingComplete += (_, _) => Ubicar();
            tabla.DataSourceChanged += (_, _) => Ubicar();
            tabla.RowsAdded += (_, _) => Ubicar();
            tabla.RowsRemoved += (_, _) => Ubicar();
            tabla.SizeChanged += (_, _) => Ubicar();
            Ubicar();
        }

        /// <summary>Pastilla de estado en una celda: verde con punto si está activo, gris si no.</summary>
        internal static void PastillaEstado(Graphics g, Rectangle celda, string valor, bool activo, Control escala) =>
            Pastilla(g, celda, valor, activo ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave, activo ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Fondo,
                activo ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.Borde, IconChar.None, activo ? TemaEcoAccess.Verde : TemaEcoAccess.Suave, escala);

        /// <summary>
        /// Pastilla redondeada dentro de una celda: icono pequeño (o un punto del color <paramref name="marca"/>) y texto del tono
        /// indicado sobre su fondo suave. Si no cabe, el texto se corta con puntos suspensivos.
        /// </summary>
        internal static void Pastilla(Graphics g, Rectangle celda, string valor, Color tono, Color fondo, Color borde, IconChar icono, Color marca, Control escala)
        {
            int px(int n) => TemaEcoAccess.Px(escala, n);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Font fuente = DibujoDashboard.Semibold(escala, 12f);
            Size medida = TextRenderer.MeasureText(g, valor, fuente, Size.Empty, TextFormatFlags.NoPadding);
            int adorno = icono == IconChar.None ? px(7) : px(13);
            int alto = medida.Height + px(8), ancho = Math.Min(celda.Width - px(12), medida.Width + adorno + px(25));
            // Celda más angosta que la pastilla (columna encogida, otra escala): un arco con ancho ≤ 0 hace fallar a GDI+
            // y la tabla dejaba de pintarse. Entonces solo se escribe el texto.
            if (ancho < alto || celda.Height < alto)
            {
                TextRenderer.DrawText(g, valor, fuente, celda, tono, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }
            var pastilla = new Rectangle(celda.Left + px(8), celda.Top + (celda.Height - alto) / 2, ancho, alto);
            using (var ruta = RecursosEco.Redondeado(pastilla, alto / 2f))
            using (var relleno = new SolidBrush(fondo))
            using (var lapiz = new Pen(borde))
            {
                g.FillPath(relleno, ruta);
                g.DrawPath(lapiz, ruta);
            }
            if (icono == IconChar.None)
                using (var punto = new SolidBrush(marca)) g.FillEllipse(punto, pastilla.Left + px(10), pastilla.Top + (alto - adorno) / 2, adorno, adorno);
            else
            {
                Bitmap imagen = CacheIconos.Obtener(icono, adorno, marca);
                g.DrawImage(imagen, pastilla.Left + px(9), pastilla.Top + (alto - imagen.Height) / 2);
            }
            var texto = Rectangle.FromLTRB(pastilla.Left + px(10) + adorno + px(5), pastilla.Top, pastilla.Right - px(6), pastilla.Bottom);
            TextRenderer.DrawText(g, valor, fuente, texto, tono, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>
    /// Pestañas sin la franja clásica de Windows: las páginas ocupan todo el control y se eligen con un
    /// <see cref="SelectorPestanas"/> (botones tipo píldora con icono). Conserva TabControl, así que el formulario sigue usando
    /// las mismas páginas, el mismo índice y Ctrl+Tab.
    /// </summary>
    internal sealed class PaginasSinPestanas : TabControl
    {
        private const int TcmAdjustRect = 0x1328;

        protected override void WndProc(ref Message m)
        {
            // Sin margen para la franja de pestañas ni para el borde: la página llena el control.
            if (m.Msg == TcmAdjustRect && !DesignMode) { m.Result = (IntPtr)1; return; }
            base.WndProc(ref m);
        }
    }

    /// <summary>
    /// Fila de botones tipo píldora (icono + texto) que eligen la página de un TabControl: la elegida en verde oscuro con texto
    /// blanco y las demás blancas con borde, como en el diseño de Usuarios y roles. Cada botón es accesible como pestaña.
    /// </summary>
    internal sealed class SelectorPestanas : TableLayoutPanel
    {
        private readonly TabControl _paginas;

        internal SelectorPestanas(TabControl paginas, params IconChar[] iconos)
        {
            _paginas = paginas;
            ColumnCount = paginas.TabCount;
            RowCount = 1;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = TemaEcoAccess.Superficie;
            Margin = new Padding(0, 0, 0, 12);
            AccessibleRole = AccessibleRole.PageTabList;
            AccessibleName = paginas.AccessibleName;
            for (int i = 0; i < paginas.TabCount; i++)
            {
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / paginas.TabCount));
                int indice = i;
                var boton = new BotonPestana(paginas.TabPages[i].Text, i < iconos.Length ? iconos[i] : IconChar.None)
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(i == 0 ? 0 : 3, 0, i == paginas.TabCount - 1 ? 0 : 3, 0),
                    TabIndex = i,
                };
                boton.Click += (_, _) => _paginas.SelectedIndex = indice;
                Controls.Add(boton, i, 0);
            }
            paginas.SelectedIndexChanged += (_, _) => Marcar();
            paginas.HandleCreated += (_, _) => Marcar();
            paginas.EnabledChanged += (_, _) => { foreach (Control c in Controls) c.Invalidate(); };
            Marcar();
        }

        private void Marcar()
        {
            for (int i = 0; i < Controls.Count; i++)
                if (Controls[i] is BotonPestana b) b.Elegida = i == Math.Max(0, _paginas.SelectedIndex);
        }
    }

    /// <summary>Botón píldora de <see cref="SelectorPestanas"/>.</summary>
    internal sealed class BotonPestana : Control
    {
        private readonly IconChar _icono;
        private bool _elegida, _encima;

        internal BotonPestana(string texto, IconChar icono)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Text = texto;
            _icono = icono;
            AccessibleRole = AccessibleRole.PageTab;
            AccessibleName = texto;
            Cursor = Cursors.Hand;
            TabStop = true;
            Font = TemaEcoAccess.Pequena;
            Height = TemaEcoAccess.Px(this, 40);
        }

        internal bool Elegida
        {
            get => _elegida;
            set { _elegida = value; AccessibleDescription = value ? "Pestaña actual" : null; Invalidate(); }
        }

        public override Size GetPreferredSize(Size proposedSize) => new(base.GetPreferredSize(proposedSize).Width, TemaEcoAccess.Px(this, 40));

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(EstiloControlesEco.FondoDe(Parent));
            bool activo = Parent?.Enabled ?? true;
            Color fondo = _elegida ? (activo ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave) : _encima ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Superficie;
            Color borde = _elegida ? fondo : _encima ? TemaEcoAccess.Verde : TemaEcoAccess.Borde;
            Color texto = _elegida ? Color.White : activo ? TemaEcoAccess.Texto : TemaEcoAccess.Suave;
            var area = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var ruta = RecursosEco.Redondeado(area, TemaEcoAccess.Px(this, 8)))
            {
                using (var relleno = new SolidBrush(fondo)) g.FillPath(relleno, ruta);
                using (var lapiz = new Pen(borde)) g.DrawPath(lapiz, ruta);
            }
            if (Focused && ShowFocusCues)
                using (var ruta = RecursosEco.Redondeado(RectangleF.Inflate(area, -2, -2), TemaEcoAccess.Px(this, 7)))
                using (var foco = new Pen(_elegida ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.Verde, 2)) g.DrawPath(foco, ruta);
            Font fuente = _elegida ? EstiloControlesEco.FuenteSemibold(Font) : Font;
            Bitmap? icono = _icono == IconChar.None ? null : CacheIconos.Obtener(_icono, TemaEcoAccess.Px(this, 16), texto);
            int separacion = icono is null ? 0 : TemaEcoAccess.Px(this, 6);
            int libre = Width - TemaEcoAccess.Px(this, 10) - (icono?.Width ?? 0) - separacion;
            Size medida = TextRenderer.MeasureText(g, Text, fuente, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            // Si no cabe con icono, se muestra solo el texto (el icono es decorativo).
            if (medida.Width > libre) { icono = null; separacion = 0; libre = Width - TemaEcoAccess.Px(this, 12); }
            int total = (icono?.Width ?? 0) + separacion + Math.Min(medida.Width, libre);
            int x = Math.Max(TemaEcoAccess.Px(this, 6), (Width - total) / 2);
            if (icono is not null) g.DrawImage(icono, x, (Height - icono.Height) / 2);
            var zona = new Rectangle(x + (icono?.Width ?? 0) + separacion, 0, Math.Min(medida.Width, libre) + 2, Height);
            TextRenderer.DrawText(g, Text, fuente, zona, texto, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyUp(e);
        }
    }

    /// <summary>
    /// Aviso en recuadro verde suave redondeado con icono de información: título opcional en semibold y un texto (que puede
    /// ser una etiqueta que el formulario sigue actualizando).
    /// </summary>
    internal sealed class AvisoEco : TableLayoutPanel
    {
        internal AvisoEco(Label texto, string? titulo = null, IconChar icono = IconChar.CircleInfo)
        {
            ColumnCount = 2;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = TemaEcoAccess.Superficie;
            Margin = new Padding(0, 0, 0, 12);
            Padding = new Padding(14, 12, 14, 12);
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int lado = TemaEcoAccess.Px(this, 22);
            var marca = new Label
            {
                Image = CacheIconos.Obtener(icono, lado, TemaEcoAccess.Verde), Size = new Size(lado, lado), Margin = new Padding(0, 2, 12, 0),
                BackColor = TemaEcoAccess.VerdeSuave, AccessibleRole = AccessibleRole.Graphic, AccessibleName = "Información",
            };
            Controls.Add(marca, 0, 0);
            int fila = 0;
            if (titulo is not null)
            {
                Controls.Add(new Label
                {
                    Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro,
                    BackColor = TemaEcoAccess.VerdeSuave, Margin = new Padding(0, 0, 0, 2), UseMnemonic = false,
                }, 1, fila++);
            }
            texto.AutoSize = true;
            texto.Font = titulo is null ? TemaEcoAccess.Cuerpo : TemaEcoAccess.Cuerpo;
            texto.ForeColor = titulo is null ? TemaEcoAccess.Texto : TemaEcoAccess.Suave;
            texto.BackColor = TemaEcoAccess.VerdeSuave;
            texto.Margin = new Padding(0, titulo is null ? 2 : 0, 0, 0);
            Controls.Add(texto, 1, fila);
            if (titulo is not null) SetRowSpan(marca, 2);
            ParentChanged += (_, _) => { if (Parent is { } p) { p.SizeChanged += (_, _) => Limitar(); Limitar(); } };

            void Limitar()
            {
                if (Parent is not { } contenedor) return;
                int ancho = Math.Max(80, contenedor.ClientSize.Width - contenedor.Padding.Horizontal - Padding.Horizontal - lado - TemaEcoAccess.Px(this, 20));
                foreach (Control c in Controls)
                    if (c is Label { AutoSize: true } l && l.MaximumSize.Width != ancho) l.MaximumSize = new Size(ancho, 0);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(EstiloControlesEco.FondoDe(Parent));
            using var ruta = RecursosEco.Redondeado(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), TemaEcoAccess.Px(this, 10));
            using var relleno = new SolidBrush(TemaEcoAccess.VerdeSuave);
            using var borde = new Pen(TemaEcoAccess.VerdeHoja);
            e.Graphics.FillPath(relleno, ruta);
            e.Graphics.DrawPath(borde, ruta);
        }
    }
}
