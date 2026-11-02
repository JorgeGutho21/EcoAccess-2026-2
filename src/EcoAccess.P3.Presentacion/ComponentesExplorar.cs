using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Color, icono y destino de cada categoría. Sigue el código de colores de la Resolución 2184 de 2019 (blanca,
    /// verde y negra); los peligrosos no van en esas canecas y se marcan en rojo de advertencia.
    /// </summary>
    internal static class EstiloCategoria
    {
        internal static (Color Tono, Color Fondo, IconChar Icono, string Destino) Para(CategoriaMaterial categoria) => categoria switch
        {
            CategoriaMaterial.Aprovechable => (Color.FromArgb(0x33, 0x41, 0x55), Color.FromArgb(0xEE, 0xF2, 0xF6), IconChar.Recycle, "Caneca blanca"),
            CategoriaMaterial.OrganicoAprovechable => (TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave, IconChar.Seedling, "Caneca verde"),
            CategoriaMaterial.NoAprovechable => (Color.FromArgb(0x1F, 0x29, 0x37), Color.FromArgb(0xEC, 0xEE, 0xF1), IconChar.TrashCan, "Caneca negra"),
            _ => (TemaEcoAccess.Error, TemaEcoAccess.ErrorSuave, IconChar.TriangleExclamation, "Punto posconsumo"),
        };
    }

    /// <summary>Utilidades de dibujo compartidas por las tarjetas dibujadas a mano.</summary>
    internal static class DibujoEco
    {
        internal const TextFormatFlags Parrafo = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.TextBoxControl;

        internal static int AltoTexto(string texto, Font fuente, int ancho) =>
            Compat.IsNullOrEmpty(texto) ? 0 : TextRenderer.MeasureText(texto, fuente, new Size(Math.Max(1, ancho), int.MaxValue), Parrafo).Height;

        internal static int Texto(Graphics g, string texto, Font fuente, Color color, int x, int y, int ancho)
        {
            int alto = AltoTexto(texto, fuente, ancho);
            TextRenderer.DrawText(g, texto, fuente, new Rectangle(x, y, ancho, alto), color, Parrafo);
            return alto;
        }

        /// <summary>Icono FontAwesome centrado sobre un cuadrado redondeado del color de fondo indicado.</summary>
        internal static void Icono(Graphics g, IconChar icono, Color tono, Color fondo, Rectangle area, float radio)
        {
            using (var ruta = RecursosEco.Redondeado(area, radio))
            using (var pincel = new SolidBrush(fondo))
                g.FillPath(pincel, ruta);
            int lado = Math.Max(8, area.Width * 11 / 20);
            using Bitmap imagen = icono.ToBitmap(IconFont.Solid, lado, tono);
            g.DrawImage(imagen, area.X + (area.Width - imagen.Width) / 2, area.Y + (area.Height - imagen.Height) / 2);
        }

        /// <summary>Pastilla de texto corto (categoría, destino) sobre fondo suave del mismo tono.</summary>
        internal static Size Pastilla(Graphics g, string texto, Font fuente, Color tono, Color fondo, int x, int y, Control escala)
        {
            Size medida = TextRenderer.MeasureText(texto, fuente, Size.Empty, TextFormatFlags.NoPadding);
            var area = new Rectangle(x, y, medida.Width + TemaEcoAccess.Px(escala, 20), medida.Height + TemaEcoAccess.Px(escala, 8));
            using (var ruta = RecursosEco.Redondeado(area, area.Height / 2f))
            using (var pincel = new SolidBrush(fondo))
                g.FillPath(pincel, ruta);
            TextRenderer.DrawText(g, texto, fuente, area, tono, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return area.Size;
        }
    }

    /// <summary>
    /// Tarjeta dibujada a mano: superficie blanca redondeada con sombra corta. Calcula su alto a partir del ancho
    /// (<see cref="AjustarAncho"/>), así el contenedor reparte columnas sin recortar textos a ningún escalado.
    /// Si es interactiva se comporta como un botón: foco visible, Enter o Espacio la activan y responde al ratón.
    /// </summary>
    internal abstract class TarjetaDibujada : Control
    {
        private bool _encima;

        protected TarjetaDibujada(bool interactiva)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, interactiva);
            Interactiva = interactiva;
            TabStop = interactiva;
            Cursor = interactiva ? Cursors.Hand : Cursors.Default;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            Font = TemaEcoAccess.Cuerpo;
            Margin = new Padding(0, 0, 16, 16);
            AccessibleRole = interactiva ? AccessibleRole.PushButton : AccessibleRole.Grouping;
        }

        protected bool Interactiva { get; }

        /// <summary>Color de la superficie; las tarjetas destacadas usan el verde oscuro de marca.</summary>
        protected Color Superficie { get; set; } = TemaEcoAccess.Superficie;

        protected bool Resaltada => Interactiva && (_encima || Focused);

        protected int P(int unidades) => TemaEcoAccess.Px(this, unidades);

        /// <summary>Margen interior entre el borde de la tarjeta y su contenido.</summary>
        protected int Relleno => P(20);

        /// <summary>Alto que necesita la tarjeta para mostrar todo su contenido con ese ancho.</summary>
        internal abstract int MedirAlto(int ancho);

        internal void AjustarAncho(int ancho)
        {
            Width = ancho;
            Height = MedirAlto(ancho);
        }

        protected abstract void PintarContenido(Graphics g);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float radio = P(TemaEcoAccess.Radio);
            var cuerpo = new RectangleF(1, 1, Width - 3, Height - P(4) - 2);
            // Sombra corta: dos capas suaves desplazadas hacia abajo; al pasar el ratón se eleva un poco.
            int capas = Resaltada ? 4 : 2;
            for (int capa = capas; capa >= 1; capa--)
            {
                var r = cuerpo;
                r.Offset(0, capa);
                using var ruta = RecursosEco.Redondeado(r, radio);
                using var pincel = new SolidBrush(Color.FromArgb(Resaltada ? 14 : 10, 16, 40, 30));
                g.FillPath(pincel, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(cuerpo, radio))
            using (var relleno = new SolidBrush(Superficie))
            using (var borde = new Pen(Focused ? TemaEcoAccess.Verde : Resaltada ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.Borde, Focused ? P(2) : 1))
            {
                g.FillPath(relleno, ruta);
                g.DrawPath(borde, ruta);
            }
            g.SmoothingMode = SmoothingMode.None;
            PintarContenido(g);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _encima = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _encima = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override bool IsInputKey(Keys keyData) => Interactiva && keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Interactiva && e.KeyCode is Keys.Enter or Keys.Space)
            {
                e.Handled = true;
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Interactiva) Focus();
        }
    }

    /// <summary>Acceso rápido: icono, título y una línea que dice qué se puede hacer. La destacada va en verde de marca.</summary>
    internal sealed class TarjetaAccionRapida : TarjetaDibujada
    {
        private readonly IconChar _icono;
        private readonly string _titulo;
        private readonly string _texto;
        private readonly bool _destacada;

        public TarjetaAccionRapida(IconChar icono, string titulo, string texto, bool destacada = false, bool interactiva = true) : base(interactiva)
        {
            _icono = icono;
            _titulo = titulo;
            _texto = texto;
            _destacada = destacada;
            Superficie = destacada ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Superficie;
            AccessibleName = titulo;
            AccessibleDescription = texto;
        }

        private int LadoIcono => P(44);

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            return Relleno + LadoIcono + P(14) + DibujoEco.AltoTexto(_titulo, TemaEcoAccess.Subtitulo, util) + P(4)
                + DibujoEco.AltoTexto(_texto, TemaEcoAccess.Cuerpo, util) + Relleno + P(6);
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            Color tono = _destacada ? Color.White : TemaEcoAccess.VerdeOscuro;
            Color fondo = _destacada ? Color.FromArgb(0x1B, 0x6E, 0x49) : TemaEcoAccess.VerdeSuave;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujoEco.Icono(g, _icono, tono, fondo, new Rectangle(x, y, LadoIcono, LadoIcono), P(10));
            // Flecha: indica que la tarjeta lleva a algún sitio; avanza al resaltarse.
            int flecha = P(16);
            if (Interactiva)
            {
                using Bitmap imagen = IconChar.ArrowRight.ToBitmap(IconFont.Solid, flecha, _destacada ? TemaEcoAccess.VerdeHoja : Resaltada ? TemaEcoAccess.Verde : TemaEcoAccess.Suave);
                g.DrawImage(imagen, Width - Relleno - flecha - (Resaltada ? 0 : P(4)), y + (LadoIcono - flecha) / 2);
            }
            y += LadoIcono + P(14);
            y += DibujoEco.Texto(g, _titulo, TemaEcoAccess.Subtitulo, _destacada ? Color.White : TemaEcoAccess.Texto, x, y, util) + P(4);
            DibujoEco.Texto(g, _texto, TemaEcoAccess.Cuerpo, _destacada ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.Suave, x, y, util);
        }
    }

    /// <summary>Dato personal del ciudadano (saldo, depósitos, actividad) con su valor real de Oracle o «—» si no hay.</summary>
    internal sealed class TarjetaDato : TarjetaDibujada
    {
        private readonly IconChar _icono;
        private readonly string _titulo;
        private string _valor = "—";
        private string _detalle = "Consultando Oracle…";

        public TarjetaDato(IconChar icono, string titulo) : base(interactiva: false)
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
        }

        private int LadoIcono => P(40);

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            return Relleno + LadoIcono + P(12) + DibujoEco.AltoTexto(_valor, TemaEcoAccess.Valor, util)
                + P(2) + DibujoEco.AltoTexto(_detalle, TemaEcoAccess.Pequena, util) + Relleno + P(6);
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujoEco.Icono(g, _icono, TemaEcoAccess.Verde, TemaEcoAccess.VerdeSuave, new Rectangle(x, y, LadoIcono, LadoIcono), P(10));
            int xt = x + LadoIcono + P(12);
            TextRenderer.DrawText(g, _titulo, TemaEcoAccess.Subtitulo, new Rectangle(xt, y, util - LadoIcono - P(12), LadoIcono), TemaEcoAccess.Texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            y += LadoIcono + P(12);
            y += DibujoEco.Texto(g, _valor, TemaEcoAccess.Valor, TemaEcoAccess.VerdeOscuro, x, y, util) + P(2);
            DibujoEco.Texto(g, _detalle, TemaEcoAccess.Pequena, TemaEcoAccess.Suave, x, y, util);
        }
    }

    /// <summary>
    /// Categoría de separación con su color de caneca. Al activarla despliega ejemplos reales del catálogo de Oracle
    /// con la indicación específica de cada material.
    /// </summary>
    internal sealed class TarjetaCategoria : TarjetaDibujada
    {
        private readonly IndicacionSeparacion _guia;
        private IReadOnlyList<Material> _ejemplos = Array.Empty<Material>();
        private bool _abierta;

        public event EventHandler? AltoCambiado;

        public TarjetaCategoria(IndicacionSeparacion guia) : base(interactiva: true)
        {
            _guia = guia;
            AccessibleName = $"{guia.Nombre}. {guia.Indicacion}";
            AccessibleDescription = "Pulsa Enter para ver ejemplos del catálogo";
            Click += (_, _) => Alternar();
        }

        internal CategoriaMaterial Categoria => _guia.Categoria;

        internal void MostrarEjemplos(IReadOnlyList<Material> ejemplos)
        {
            _ejemplos = ejemplos;
            if (_abierta) Reajustar();
        }

        internal void Abrir()
        {
            if (!_abierta) Alternar();
        }

        private void Alternar()
        {
            _abierta = !_abierta;
            AccessibleDescription = _abierta ? "Pulsa Enter para ocultar los ejemplos" : "Pulsa Enter para ver ejemplos del catálogo";
            Reajustar();
        }

        private void Reajustar()
        {
            Height = MedirAlto(Width);
            Invalidate();
            AltoCambiado?.Invoke(this, EventArgs.Empty);
        }

        private int LadoIcono => P(48);

        private string TextoEjemplos()
        {
            if (_ejemplos.Count == 0) return "Aún no hay materiales de esta categoría en el catálogo.";
            return string.Join("\n", _ejemplos.Take(5).Select(m => Compat.IsNullOrWhiteSpace(m.Descripcion) ? $"• {m.Nombre}" : $"• {m.Nombre}: {m.Descripcion}"));
        }

        private string Enlace => _abierta ? "Ocultar ejemplos" : "Ver ejemplos";

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            int alto = Relleno + LadoIcono + P(14) + DibujoEco.AltoTexto(_guia.Nombre, TemaEcoAccess.Subtitulo, util) + P(10)
                + P(26) + P(10) + DibujoEco.AltoTexto(_guia.Indicacion, TemaEcoAccess.Cuerpo, util) + P(14)
                + DibujoEco.AltoTexto(Enlace, TemaEcoAccess.Subtitulo, util);
            if (_abierta) alto += P(12) + DibujoEco.AltoTexto(TextoEjemplos(), TemaEcoAccess.Pequena, util);
            return alto + Relleno + P(6);
        }

        protected override void PintarContenido(Graphics g)
        {
            var (tono, fondo, icono, destino) = EstiloCategoria.Para(_guia.Categoria);
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujoEco.Icono(g, icono, tono, fondo, new Rectangle(x, y, LadoIcono, LadoIcono), LadoIcono / 2f);
            y += LadoIcono + P(14);
            y += DibujoEco.Texto(g, _guia.Nombre, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto, x, y, util) + P(10);
            y += DibujoEco.Pastilla(g, destino, TemaEcoAccess.Pequena, tono, fondo, x, y, this).Height + P(10);
            y += DibujoEco.Texto(g, _guia.Indicacion, TemaEcoAccess.Cuerpo, TemaEcoAccess.Suave, x, y, util) + P(14);
            y += DibujoEco.Texto(g, (_abierta ? "▴ " : "▾ ") + Enlace, TemaEcoAccess.Subtitulo, TemaEcoAccess.VerdeOscuro, x, y, util);
            if (_abierta) DibujoEco.Texto(g, TextoEjemplos(), TemaEcoAccess.Pequena, TemaEcoAccess.Texto, x, y + P(12), util);
        }
    }

    /// <summary>
    /// Resultado de búsqueda: el material con su categoría, la indicación específica guardada en Oracle y si la caneca
    /// DEMO lo recibe. Solo entonces ofrece depositarlo.
    /// </summary>
    internal sealed class TarjetaResultadoMaterial : TarjetaDibujada
    {
        private readonly Material _material;
        private readonly bool? _recibeDemo;
        private readonly KryptonButton? _depositar;

        public TarjetaResultadoMaterial(Material material, bool? recibeDemo, Action<Material>? depositar) : base(interactiva: false)
        {
            _material = material;
            _recibeDemo = recibeDemo;
            IndicacionSeparacion guia = GuiaSeparacion.Para(material.Categoria);
            AccessibleName = $"{material.Nombre}. {guia.Nombre}. {Indicacion}. {TextoDemo}";
            if (recibeDemo == true && depositar is not null)
            {
                _depositar = new KryptonButton { Text = "Depositar este material", AutoSize = true, AccessibleName = $"Depositar {material.Nombre} en la caneca piloto" };
                TemaEcoAccess.EstilizarAccion(_depositar, principal: true);
                _depositar.Click += (_, _) => depositar(material);
                Controls.Add(_depositar);
            }
        }

        private string Indicacion => GuiaSeparacion.Orientacion(_material);

        private string TextoDemo => _recibeDemo switch
        {
            true => "La caneca piloto recibe este material.",
            false => "La caneca piloto no recibe este material.",
            null => "No se pudo comprobar la admisión en la caneca piloto.",
        };

        private int LadoIcono => P(40);

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            int texto = util - LadoIcono - P(14);
            int alto = Relleno + Math.Max(LadoIcono, DibujoEco.AltoTexto(_material.Nombre, TemaEcoAccess.Subtitulo, texto) + P(6) + P(26))
                + P(14) + DibujoEco.AltoTexto("Cómo entregarlo", TemaEcoAccess.Pequena, util) + P(2)
                + DibujoEco.AltoTexto(Indicacion, TemaEcoAccess.Cuerpo, util) + P(12)
                + DibujoEco.AltoTexto(TextoDemo, TemaEcoAccess.Pequena, util - P(22));
            if (_depositar is not null) alto += P(14) + _depositar.GetPreferredSize(Size.Empty).Height;
            return alto + Relleno + P(6);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_depositar is null) return;
            Size medida = _depositar.GetPreferredSize(Size.Empty);
            _depositar.SetBounds(Relleno, Height - Relleno - P(6) - medida.Height, medida.Width, medida.Height);
        }

        protected override void PintarContenido(Graphics g)
        {
            var (tono, fondo, icono, destino) = EstiloCategoria.Para(_material.Categoria);
            IndicacionSeparacion guia = GuiaSeparacion.Para(_material.Categoria);
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujoEco.Icono(g, icono, tono, fondo, new Rectangle(x, y, LadoIcono, LadoIcono), LadoIcono / 2f);
            int xt = x + LadoIcono + P(14), texto = util - LadoIcono - P(14);
            int yt = y + DibujoEco.Texto(g, _material.Nombre, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto, xt, y, texto) + P(6);
            Size pastilla = DibujoEco.Pastilla(g, guia.Nombre, TemaEcoAccess.Pequena, tono, fondo, xt, yt, this);
            DibujoEco.Pastilla(g, destino, TemaEcoAccess.Pequena, TemaEcoAccess.Suave, TemaEcoAccess.Fondo, xt + pastilla.Width + P(6), yt, this);
            y += Math.Max(LadoIcono, yt + pastilla.Height - y) + P(14);
            y += DibujoEco.Texto(g, "Cómo entregarlo", TemaEcoAccess.Pequena, TemaEcoAccess.Suave, x, y, util) + P(2);
            y += DibujoEco.Texto(g, Indicacion, TemaEcoAccess.Cuerpo, TemaEcoAccess.Texto, x, y, util) + P(12);
            IconChar marca = _recibeDemo == true ? IconChar.CircleCheck : IconChar.CircleInfo;
            Color color = _recibeDemo == true ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
            using (Bitmap imagen = marca.ToBitmap(IconFont.Solid, P(16), color))
                g.DrawImage(imagen, x, y + P(1));
            DibujoEco.Texto(g, TextoDemo, TemaEcoAccess.Pequena, color, x + P(22), y, util - P(22));
        }
    }

    /// <summary>Campo de búsqueda grande con lupa y borde redondeado; el borde se pone verde con el foco.</summary>
    internal sealed class CajaBusqueda : Panel
    {
        internal TextBox Campo { get; } = new CajaTexto()
        {
            BorderStyle = BorderStyle.None,
            Font = TemaEcoAccess.Entrada,
            ForeColor = TemaEcoAccess.Texto,
            BackColor = TemaEcoAccess.Superficie,
            PlaceholderText = "Escribe un residuo: botella, cartón, pila, servilleta…",
        };

        public CajaBusqueda()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            Controls.Add(Campo);
            Campo.GotFocus += (_, _) => Invalidate();
            Campo.LostFocus += (_, _) => Invalidate();
            Cursor = Cursors.IBeam;
            Click += (_, _) => Campo.Focus();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int izquierda = TemaEcoAccess.Px(this, 48);
            int alto = Campo.PreferredHeight;
            Campo.SetBounds(izquierda, (Height - alto) / 2, Math.Max(10, Width - izquierda - TemaEcoAccess.Px(this, 16)), alto);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? TemaEcoAccess.Fondo);
            bool foco = Campo.Focused;
            using (var ruta = RecursosEco.Redondeado(new RectangleF(1, 1, Width - 3, Height - 3), TemaEcoAccess.Px(this, 12)))
            using (var relleno = new SolidBrush(TemaEcoAccess.Superficie))
            using (var borde = new Pen(foco ? TemaEcoAccess.Verde : TemaEcoAccess.Borde, foco ? TemaEcoAccess.Px(this, 2) : 1))
            {
                g.FillPath(relleno, ruta);
                g.DrawPath(borde, ruta);
            }
            int lado = TemaEcoAccess.Px(this, 18);
            using Bitmap lupa = IconChar.MagnifyingGlass.ToBitmap(IconFont.Solid, lado, foco ? TemaEcoAccess.Verde : TemaEcoAccess.Suave);
            g.DrawImage(lupa, TemaEcoAccess.Px(this, 18), (Height - lupa.Height) / 2);
        }
    }

    /// <summary>Indicador horizontal de pasos del asistente: hechos con marca, el actual resaltado y los pendientes en gris.</summary>
    internal sealed class IndicadorPasos : Control
    {
        private readonly string[] _pasos;
        private int _actual;

        public IndicadorPasos(params string[] pasos)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            _pasos = pasos;
            BackColor = TemaEcoAccess.Fondo;
            Font = TemaEcoAccess.Pequena;
            TabStop = false;
            AccessibleRole = AccessibleRole.ProgressBar;
            Actualizar();
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal int Actual
        {
            get => _actual;
            set { _actual = Compat.Clamp(value, 0, _pasos.Length - 1); Actualizar(); Invalidate(); }
        }

        private void Actualizar() => AccessibleName = $"Paso {_actual + 1} de {_pasos.Length}: {_pasos[_actual]}";

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int n = _pasos.Length, diametro = TemaEcoAccess.Px(this, 30);
            float tramo = (float)Width / n;
            for (int i = 0; i < n; i++)
            {
                float cx = tramo * i + tramo / 2f;
                if (i < n - 1)
                {
                    using var linea = new Pen(i < _actual ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Borde, TemaEcoAccess.Px(this, 2));
                    g.DrawLine(linea, cx + diametro / 2f + 4, diametro / 2f + 1, cx + tramo - diametro / 2f - 4, diametro / 2f + 1);
                }
                var circulo = new RectangleF(cx - diametro / 2f, 1, diametro, diametro);
                bool hecho = i < _actual, actual = i == _actual;
                using (var relleno = new SolidBrush(hecho || actual ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Superficie))
                    g.FillEllipse(relleno, circulo);
                using (var borde = new Pen(hecho || actual ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Borde, TemaEcoAccess.Px(this, 2)))
                    g.DrawEllipse(borde, circulo);
                if (hecho)
                {
                    using Bitmap marca = IconChar.Check.ToBitmap(IconFont.Solid, diametro / 2, Color.White);
                    g.DrawImage(marca, cx - marca.Width / 2f, 1 + (diametro - marca.Height) / 2f);
                }
                else
                {
                    TextRenderer.DrawText(g, (i + 1).ToString(), TemaEcoAccess.Subtitulo, Rectangle.Round(circulo),
                        actual ? Color.White : TemaEcoAccess.Suave, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                var etiqueta = new Rectangle((int)(tramo * i), diametro + TemaEcoAccess.Px(this, 8), (int)tramo, Height - diametro - TemaEcoAccess.Px(this, 8));
                TextRenderer.DrawText(g, _pasos[i], actual ? TemaEcoAccess.Subtitulo : Font, etiqueta,
                    actual || hecho ? TemaEcoAccess.Texto : TemaEcoAccess.Suave, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }
        }
    }

    /// <summary>Estado de un evento de la operación tal como lo registra Oracle.</summary>
    internal enum EstadoHito { Pendiente, EnCurso, Hecho, Fallo }

    /// <summary>Lista vertical de los hitos de una operación (orden, envío, apertura, detección, verificación).</summary>
    internal sealed class LineaHitos : Control
    {
        private readonly List<(string Titulo, string Detalle, EstadoHito Estado)> _hitos = new List<(string Titulo, string Detalle, EstadoHito Estado)>();

        public LineaHitos()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            TabStop = false;
            AccessibleRole = AccessibleRole.List;
            AccessibleName = "Progreso de la operación";
        }

        internal void Mostrar(IEnumerable<(string Titulo, string Detalle, EstadoHito Estado)> hitos)
        {
            _hitos.Clear();
            _hitos.AddRange(hitos);
            AccessibleDescription = string.Join(". ", _hitos.Select(h => $"{h.Titulo}: {Nombre(h.Estado)}"));
            Height = MedirAlto(Width);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int alto = MedirAlto(Width);
            if (alto != Height) Height = alto;
        }

        private static string Nombre(EstadoHito estado) => estado switch
        {
            EstadoHito.Hecho => "completado",
            EstadoHito.EnCurso => "en curso",
            EstadoHito.Fallo => "no se completó",
            _ => "pendiente",
        };

        private int Fila(int ancho, int i) =>
            Math.Max(TemaEcoAccess.Px(this, 28), DibujoEco.AltoTexto(_hitos[i].Titulo, TemaEcoAccess.Subtitulo, ancho)
                + DibujoEco.AltoTexto(_hitos[i].Detalle, TemaEcoAccess.Pequena, ancho)) + TemaEcoAccess.Px(this, 14);

        internal int MedirAlto(int ancho)
        {
            int texto = ancho - TemaEcoAccess.Px(this, 44), alto = 0;
            for (int i = 0; i < _hitos.Count; i++) alto += Fila(texto, i);
            return Math.Max(alto, TemaEcoAccess.Px(this, 20));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int lado = TemaEcoAccess.Px(this, 24), x = TemaEcoAccess.Px(this, 44), texto = Width - x, y = 0;
            for (int i = 0; i < _hitos.Count; i++)
            {
                var (titulo, detalle, estado) = _hitos[i];
                int fila = Fila(texto, i);
                if (i < _hitos.Count - 1)
                {
                    using var linea = new Pen(estado == EstadoHito.Hecho ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.Borde, TemaEcoAccess.Px(this, 2));
                    g.DrawLine(linea, lado / 2f, y + lado + 2, lado / 2f, y + fila - 2);
                }
                (IconChar icono, Color color) = estado switch
                {
                    EstadoHito.Hecho => (IconChar.CircleCheck, TemaEcoAccess.VerdeOscuro),
                    EstadoHito.EnCurso => (IconChar.CircleDot, TemaEcoAccess.Info),
                    EstadoHito.Fallo => (IconChar.CircleXmark, TemaEcoAccess.Error),
                    _ => (IconChar.Circle, TemaEcoAccess.Borde),
                };
                using (Bitmap imagen = icono.ToBitmap(estado == EstadoHito.Pendiente ? IconFont.Regular : IconFont.Solid, lado, color))
                    g.DrawImage(imagen, 0, y);
                int alto = DibujoEco.Texto(g, titulo, TemaEcoAccess.Subtitulo, estado == EstadoHito.Pendiente ? TemaEcoAccess.Suave : TemaEcoAccess.Texto, x, y + TemaEcoAccess.Px(this, 1), texto);
                DibujoEco.Texto(g, detalle, TemaEcoAccess.Pequena, estado == EstadoHito.Fallo ? TemaEcoAccess.Error : TemaEcoAccess.Suave, x, y + alto + TemaEcoAccess.Px(this, 1), texto);
                y += fila;
            }
        }
    }

    /// <summary>
    /// Rejilla adaptable: reparte sus tarjetas en 4, 2 o 1 columnas según el ancho disponible y ajusta el alto de
    /// cada una a su contenido. Así ninguna tarjeta se recorta al cambiar el tamaño de la ventana o el escalado.
    /// </summary>
    internal sealed class RejillaTarjetas : FlowLayoutPanel
    {
        private readonly int _anchoMinimo;

        public RejillaTarjetas(int anchoMinimo = 250)
        {
            DoubleBuffered = true;
            _anchoMinimo = anchoMinimo;
            WrapContents = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Margin = Padding.Empty;
            BackColor = TemaEcoAccess.Fondo;
        }

        /// <summary>Máximo de columnas: 4 en los accesos rápidos; 2 en los resultados de búsqueda.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] internal int ColumnasMaximas { get; init; } = 4;

        internal void Adaptar(int ancho)
        {
            int separacion = TemaEcoAccess.Px(this, 16);
            int columnas = ColumnasMaximas;
            while (columnas > 1 && (ancho - separacion * (columnas - 1)) / columnas < TemaEcoAccess.Px(this, _anchoMinimo)) columnas = columnas > 2 ? columnas / 2 : 1;
            int anchoTarjeta = (ancho - separacion * (columnas - 1)) / columnas;
            int indice = 0;
            SuspendLayout();
            foreach (Control control in Controls)
            {
                if (control is not TarjetaDibujada tarjeta) continue;
                bool ultimaDeFila = ++indice % columnas == 0;
                tarjeta.Margin = new Padding(0, 0, ultimaDeFila ? 0 : separacion, separacion);
                tarjeta.AjustarAncho(anchoTarjeta);
            }
            MaximumSize = new Size(ancho, 0);
            MinimumSize = new Size(ancho, 0);
            ResumeLayout(true);
        }
    }
}
