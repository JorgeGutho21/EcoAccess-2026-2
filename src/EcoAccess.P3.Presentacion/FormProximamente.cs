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

    /// <summary>Evolución de EcoAccess. Todo es contenido educativo: no consulta datos ni crea canecas ni ubicaciones.</summary>
    public sealed class FormProximamente : Form
    {
        private readonly RejillaTarjetas[] _rejillas;
        private readonly PanelDesplazable[] _paginas;
        private readonly Label[] _textos;

        public FormProximamente()
        {
            Text = "Próximamente · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1120, 760);
            MinimumSize = new Size(680, 500);
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            Font = TemaEcoAccess.Cuerpo;

            var evolucion = new RejillaTarjetas(300) { ColumnasMaximas = 2 };
            foreach (ElementoEvolucion elemento in ProyeccionesEcoAccess.Evolucion) evolucion.Controls.Add(new TarjetaEvolucion(elemento));
            var canecas = new RejillaTarjetas(240) { ColumnasMaximas = 4 };
            foreach (CanecaProyectada caneca in ProyeccionesEcoAccess.CanecasFuturas) canecas.Controls.Add(new TarjetaCanecaProyectada(caneca));
            var bloques = new RejillaTarjetas(250) { ColumnasMaximas = 3 };
            foreach (BloqueProyectado bloque in ProyeccionesEcoAccess.Bloques) bloques.Controls.Add(new TarjetaBloqueProyectado(bloque));

            _rejillas = new[] { evolucion, canecas, bloques };
            _textos = new[]
            {
                Texto(ProyeccionesEcoAccess.MensajeInstitucional + " Cada tarjeta indica si la función ya existe, si está en desarrollo o si es una proyección."),
                Texto("Así funcionaría la separación con varias canecas. La caneca piloto actual tiene un solo compartimiento: estas canecas todavía no existen."),
                Texto(ProyeccionesEcoAccess.AvisoBloques + " " + ProyeccionesEcoAccess.CantidadCanecas),
            };
            string[] titulos = { "Evolución de EcoAccess", "Separación inteligente", "Expansión proyectada en la UPC" };

            var pestanas = new TabControl { Dock = DockStyle.Fill };
            _paginas = new PanelDesplazable[3];
            for (int i = 0; i < 3; i++)
            {
                var pagina = new TabPage(titulos[i]) { BackColor = TemaEcoAccess.Fondo };
                var desplazable = new PanelDesplazable { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(TemaEcoAccess.Px(this, 20)) };
                var columna = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo };
                columna.Controls.Add(new Label { Text = titulos[i], AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 6) });
                columna.Controls.Add(_textos[i]);
                columna.Controls.Add(_rejillas[i]);
                desplazable.Controls.Add(columna);
                pagina.Controls.Add(desplazable);
                pestanas.TabPages.Add(pagina);
                _paginas[i] = desplazable;
                desplazable.ClientSizeChanged += (_, _) => Adaptar();
            }
            Controls.Add(pestanas);
            Shown += (_, _) => Adaptar();
            pestanas.SelectedIndexChanged += (_, _) => Adaptar();
        }

        private static Label Texto(string texto) => new() { Text = texto, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 16) };

        private void Adaptar()
        {
            for (int i = 0; i < _paginas.Length; i++)
            {
                int ancho = _paginas[i].ClientSize.Width - _paginas[i].Padding.Horizontal;
                if (ancho <= 0) continue;
                _textos[i].MaximumSize = new Size(ancho, 0);
                _rejillas[i].Adaptar(ancho);
            }
        }
    }

    internal static class EstiloProyeccion
    {
        internal static (string Texto, Color Tono, Color Fondo) Para(EstadoProyeccion estado) => estado switch
        {
            EstadoProyeccion.Actual => ("Funcionalidad actual", TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave),
            EstadoProyeccion.EnDesarrollo => ("En desarrollo", TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave),
            _ => ("Proyección futura", TemaEcoAccess.Info, TemaEcoAccess.InfoSuave),
        };
    }

    internal sealed class TarjetaEvolucion : TarjetaDibujada
    {
        private readonly ElementoEvolucion _elemento;

        public TarjetaEvolucion(ElementoEvolucion elemento) : base(interactiva: false)
        {
            _elemento = elemento;
            AccessibleName = $"{elemento.Titulo}. {EstiloProyeccion.Para(elemento.Estado).Texto}. {elemento.Descripcion}";
        }

        private IconChar Icono => _elemento.Estado switch { EstadoProyeccion.Actual => IconChar.CircleCheck, EstadoProyeccion.EnDesarrollo => IconChar.Screwdriver, _ => IconChar.Seedling };

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            return Relleno + P(28) + P(10) + DibujoEco.AltoTexto(_elemento.Titulo, TemaEcoAccess.Subtitulo, util) + P(4)
                + DibujoEco.AltoTexto(_elemento.Descripcion, TemaEcoAccess.Cuerpo, util) + Relleno + P(6);
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            var estilo = EstiloProyeccion.Para(_elemento.Estado);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujoEco.Icono(g, Icono, estilo.Tono, estilo.Fondo, new Rectangle(x, y, P(28), P(28)), P(8));
            DibujoEco.Pastilla(g, estilo.Texto, TemaEcoAccess.Pequena, estilo.Tono, estilo.Fondo, x + P(38), y + P(1), this);
            y += P(38);
            y += DibujoEco.Texto(g, _elemento.Titulo, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto, x, y, util) + P(4);
            DibujoEco.Texto(g, _elemento.Descripcion, TemaEcoAccess.Cuerpo, TemaEcoAccess.Suave, x, y, util);
        }
    }

    internal sealed class TarjetaCanecaProyectada : TarjetaDibujada
    {
        private readonly CanecaProyectada _caneca;

        public TarjetaCanecaProyectada(CanecaProyectada caneca) : base(interactiva: false)
        {
            _caneca = caneca;
            AccessibleName = $"{caneca.Nombre}. Proyección futura. {caneca.Descripcion} Materiales orientativos: {caneca.MaterialesOrientativos}.";
        }

        private int AltoIlustracion => P(112);

        private string Materiales => "Orientativos: " + _caneca.MaterialesOrientativos;

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            return Relleno + AltoIlustracion + P(10) + P(28) + DibujoEco.AltoTexto(_caneca.Nombre, TemaEcoAccess.Subtitulo, util) + P(4)
                + DibujoEco.AltoTexto(_caneca.Descripcion, TemaEcoAccess.Cuerpo, util) + P(6)
                + DibujoEco.AltoTexto(Materiales, TemaEcoAccess.Pequena, util) + P(6)
                + DibujoEco.AltoTexto(_caneca.Funcionalidad, TemaEcoAccess.Pequena, util) + Relleno + P(6);
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            var (cuerpo, tinta, icono) = _caneca.Categoria switch
            {
                CategoriaMaterial.Aprovechable => (Color.White, Color.FromArgb(0x33, 0x41, 0x55), IconChar.Recycle),
                CategoriaMaterial.OrganicoAprovechable => (TemaEcoAccess.Verde, Color.White, IconChar.Seedling),
                CategoriaMaterial.NoAprovechable => (Color.FromArgb(0x1F, 0x29, 0x37), Color.White, IconChar.TrashCan),
                _ => (Color.FromArgb(0xD9, 0x9A, 0x2B), Color.FromArgb(0x1F, 0x29, 0x37), IconChar.TriangleExclamation),
            };
            g.SmoothingMode = SmoothingMode.AntiAlias;
            IlustracionEco.Caneca(g, new Rectangle(x + (util - P(84)) / 2, y, P(84), AltoIlustracion), cuerpo, tinta, icono, this);
            y += AltoIlustracion + P(10);
            var estilo = EstiloProyeccion.Para(EstadoProyeccion.Futura);
            DibujoEco.Pastilla(g, estilo.Texto, TemaEcoAccess.Pequena, estilo.Tono, estilo.Fondo, x, y, this);
            y += P(28);
            y += DibujoEco.Texto(g, _caneca.Nombre, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto, x, y, util) + P(4);
            y += DibujoEco.Texto(g, _caneca.Descripcion, TemaEcoAccess.Cuerpo, TemaEcoAccess.Suave, x, y, util) + P(6);
            y += DibujoEco.Texto(g, Materiales, TemaEcoAccess.Pequena, TemaEcoAccess.Texto, x, y, util) + P(6);
            DibujoEco.Texto(g, _caneca.Funcionalidad, TemaEcoAccess.Pequena, TemaEcoAccess.Suave, x, y, util);
        }
    }

    internal sealed class TarjetaBloqueProyectado : TarjetaDibujada
    {
        private const string Rotulo = "Ilustración conceptual";
        private readonly BloqueProyectado _bloque;

        public TarjetaBloqueProyectado(BloqueProyectado bloque) : base(interactiva: false)
        {
            _bloque = bloque;
            AccessibleName = $"{bloque.Nombre}. Proyección futura, sin canecas instaladas. {bloque.Objetivo} {ProyeccionesEcoAccess.CantidadCanecas}";
        }

        private int AltoIlustracion => P(120);

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            return Relleno + AltoIlustracion + P(4) + DibujoEco.AltoTexto(Rotulo, TemaEcoAccess.Pequena, util) + P(8) + P(28)
                + DibujoEco.AltoTexto(_bloque.Nombre, TemaEcoAccess.Subtitulo, util) + P(4)
                + DibujoEco.AltoTexto(_bloque.Objetivo, TemaEcoAccess.Cuerpo, util) + P(6)
                + DibujoEco.AltoTexto(_bloque.Integracion, TemaEcoAccess.Pequena, util) + P(6)
                + DibujoEco.AltoTexto(ProyeccionesEcoAccess.CantidadCanecas, TemaEcoAccess.Pequena, util) + Relleno + P(6);
        }

        protected override void PintarContenido(Graphics g)
        {
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            IlustracionEco.Edificio(g, new Rectangle(x, y, util, AltoIlustracion), _bloque, this);
            y += AltoIlustracion + P(4);
            y += DibujoEco.Texto(g, Rotulo, TemaEcoAccess.Pequena, TemaEcoAccess.Suave, x, y, util) + P(8);
            var estilo = EstiloProyeccion.Para(EstadoProyeccion.Futura);
            DibujoEco.Pastilla(g, estilo.Texto, TemaEcoAccess.Pequena, estilo.Tono, estilo.Fondo, x, y, this);
            y += P(28);
            y += DibujoEco.Texto(g, _bloque.Nombre, TemaEcoAccess.Subtitulo, TemaEcoAccess.Texto, x, y, util) + P(4);
            y += DibujoEco.Texto(g, _bloque.Objetivo, TemaEcoAccess.Cuerpo, TemaEcoAccess.Suave, x, y, util) + P(6);
            y += DibujoEco.Texto(g, _bloque.Integracion, TemaEcoAccess.Pequena, TemaEcoAccess.Suave, x, y, util) + P(6);
            DibujoEco.Texto(g, ProyeccionesEcoAccess.CantidadCanecas, TemaEcoAccess.Pequena, TemaEcoAccess.Advertencia, x, y, util);
        }
    }

    /// <summary>Ilustraciones vectoriales conceptuales: sin fotografías, fachadas ni planos reales.</summary>
    internal static class IlustracionEco
    {
        internal static void Caneca(Graphics g, Rectangle a, Color cuerpo, Color tinta, IconChar icono, Control escala)
        {
            int alto = TemaEcoAccess.Px(escala, 14);
            using var borde = new Pen(Color.FromArgb(0x64, 0x74, 0x8B), TemaEcoAccess.Px(escala, 2));
            using var relleno = new SolidBrush(cuerpo);
            using (var tapa = RecursosEco.Redondeado(new RectangleF(a.X, a.Y + TemaEcoAccess.Px(escala, 6), a.Width, alto), alto / 2f))
            {
                g.FillPath(relleno, tapa);
                g.DrawPath(borde, tapa);
            }
            int arriba = a.Y + alto + TemaEcoAccess.Px(escala, 10), recorte = TemaEcoAccess.Px(escala, 8);
            using (var ruta = new GraphicsPath())
            {
                ruta.AddPolygon(new[]
                {
                    new Point(a.X + TemaEcoAccess.Px(escala, 4), arriba), new Point(a.Right - TemaEcoAccess.Px(escala, 4), arriba),
                    new Point(a.Right - recorte - TemaEcoAccess.Px(escala, 4), a.Bottom - 2), new Point(a.X + recorte + TemaEcoAccess.Px(escala, 4), a.Bottom - 2),
                });
                g.FillPath(relleno, ruta);
                g.DrawPath(borde, ruta);
            }
            Bitmap imagen = CacheIconos.Obtener(icono, a.Width / 2, tinta);
            g.DrawImage(imagen, a.X + (a.Width - imagen.Width) / 2, arriba + (a.Bottom - arriba - imagen.Height) / 2);
        }

        internal static void Edificio(Graphics g, Rectangle a, BloqueProyectado bloque, Control escala)
        {
            using (var fondo = RecursosEco.Redondeado(a, TemaEcoAccess.Px(escala, 10)))
            using (var pincel = new SolidBrush(TemaEcoAccess.VerdeSuave))
                g.FillPath(pincel, fondo);
            int base_ = a.Bottom - TemaEcoAccess.Px(escala, 14);
            int ancho = Math.Min(a.Width * 3 / 5, TemaEcoAccess.Px(escala, 190));
            var edificio = new Rectangle(a.X + TemaEcoAccess.Px(escala, 18), a.Y + TemaEcoAccess.Px(escala, 22), ancho, base_ - a.Y - TemaEcoAccess.Px(escala, 22));
            using (var muro = new SolidBrush(Color.White)) g.FillRectangle(muro, edificio);
            using (var linea = new Pen(TemaEcoAccess.Verde, TemaEcoAccess.Px(escala, 2)))
            {
                g.DrawRectangle(linea, edificio);
                g.DrawLine(linea, a.X + TemaEcoAccess.Px(escala, 10), base_, a.Right - TemaEcoAccess.Px(escala, 10), base_);
            }
            if (bloque.EsComedor)
            {
                Bitmap imagen = CacheIconos.Obtener(IconChar.Utensils, edificio.Height / 2, TemaEcoAccess.VerdeOscuro);
                g.DrawImage(imagen, edificio.X + (edificio.Width - imagen.Width) / 2, edificio.Y + (edificio.Height - imagen.Height) / 2);
            }
            else
            {
                int lado = TemaEcoAccess.Px(escala, 13), separacion = TemaEcoAccess.Px(escala, 9);
                using var ventana = new SolidBrush(TemaEcoAccess.VerdeHoja);
                for (int fila = 0; fila < 3; fila++)
                    for (int columna = 0; columna < 5; columna++)
                    {
                        int vx = edificio.X + separacion + columna * (lado + separacion), vy = edificio.Y + separacion + fila * (lado + separacion);
                        if (vx + lado < edificio.Right && vy + lado < edificio.Bottom) g.FillRectangle(ventana, vx, vy, lado, lado);
                    }
                using var letra = new Font("Segoe UI Semibold", 22);
                string inicial = bloque.Nombre.Split(' ').Last();
                Size medida = TextRenderer.MeasureText(inicial, letra);
                var circulo = new Rectangle(edificio.Right - medida.Height / 2, edificio.Y - medida.Height / 3, medida.Height, medida.Height);
                using (var fondo = new SolidBrush(TemaEcoAccess.VerdeOscuro)) g.FillEllipse(fondo, circulo);
                TextRenderer.DrawText(g, inicial, letra, circulo, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            // Tres canecas pequeñas: indican separación, no una posición real ni una cantidad.
            int ancho2 = TemaEcoAccess.Px(escala, 20), alto2 = TemaEcoAccess.Px(escala, 36);
            int x = a.Right - 3 * (ancho2 + TemaEcoAccess.Px(escala, 6)) - TemaEcoAccess.Px(escala, 10);
            foreach (Color color in new[] { Color.White, Color.FromArgb(0x1F, 0x29, 0x37), TemaEcoAccess.Verde })
            {
                var caneca = new Rectangle(x, base_ - alto2, ancho2, alto2);
                using (var relleno = new SolidBrush(color)) g.FillRectangle(relleno, caneca);
                using (var contorno = new Pen(Color.FromArgb(0x64, 0x74, 0x8B))) g.DrawRectangle(contorno, caneca);
                x += ancho2 + TemaEcoAccess.Px(escala, 6);
            }
        }
    }
}
