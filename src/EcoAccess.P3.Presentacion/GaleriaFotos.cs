using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Galería de las fotos de los residuos de una caneca (<see cref="CatalogoFotos.DeCaneca"/>): miniaturas con el nombre
    /// del residuo, sin láminas completas ni fotos pendientes. Clic o Intro abre la foto en grande. Las imágenes salen del
    /// caché de recursos incrustados: no hay rutas del equipo.
    /// </summary>
    internal sealed class GaleriaFotosCaneca : FlowLayoutPanel
    {
        internal GaleriaFotosCaneca(CategoriaMaterial caneca)
        {
            Caneca = caneca;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            WrapContents = true;
            Margin = Padding.Empty;
            BackColor = Color.Transparent;
            AccessibleName = "Fotos de la " + CatalogoPrototipo.NombreCaneca(caneca).ToLowerInvariant();
            foreach (FotoCaneca foto in CatalogoFotos.DeCaneca(caneca)) Controls.Add(new MiniaturaFoto(foto));
        }

        internal CategoriaMaterial Caneca { get; }

        internal int Cantidad => Controls.Count;

        /// <summary>Ajusta el ancho disponible (las miniaturas pasan a la línea siguiente sin desplazamiento horizontal).</summary>
        internal void Adaptar(int ancho) => MaximumSize = MinimumSize = new Size(Math.Max(100, ancho), 0);
    }

    /// <summary>Miniatura de una foto con su título; clic, Intro o Espacio la abren en grande.</summary>
    internal sealed class MiniaturaFoto : Control
    {
        private readonly FotoCaneca _foto;
        private bool _encima;

        internal MiniaturaFoto(FotoCaneca foto)
        {
            _foto = foto;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(TemaEcoAccess.Px(this, 150), TemaEcoAccess.Px(this, 188));
            Margin = new Padding(0, 0, TemaEcoAccess.Px(this, 10), TemaEcoAccess.Px(this, 10));
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = foto.Tipo == TipoFoto.PorClasificar ? "Foto por clasificar: " + foto.Nota : "Foto: " + foto.Titulo;
            MouseEnter += (_, _) => { _encima = true; Invalidate(); };
            MouseLeave += (_, _) => { _encima = false; Invalidate(); };
            GotFocus += (_, _) => Invalidate();
            LostFocus += (_, _) => Invalidate();
            Click += (_, _) => Abrir();
            KeyDown += (_, e) => { if (e.KeyCode is Keys.Enter or Keys.Space) { Abrir(); e.Handled = true; } };
        }

        private void Abrir()
        {
            using var ventana = new FormFoto(_foto);
            ventana.ShowDialog(FindForm());
        }

        internal static Image? Imagen(string clave) => RecursosEco.Imagen("residuos/" + clave.Replace('.', '/') + ".jpg");

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // El contenedor es transparente: se pinta el fondo de la ventana para que las esquinas redondeadas no salgan negras.
            Color fondoVentana = TemaEcoAccess.Fondo;
            for (Control? c = Parent; c is not null; c = c.Parent)
                if (c.BackColor.A == 255) { fondoVentana = c.BackColor; break; }
            g.Clear(fondoVentana);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int p = TemaEcoAccess.Px(this, 6);
            var tarjeta = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var ruta = RecursosEco.Redondeado(tarjeta, TemaEcoAccess.Px(this, 10)))
            using (var fondo = new SolidBrush(Color.White))
            using (var borde = new Pen(Focused || _encima ? TemaEcoAccess.Verde : TemaEcoAccess.Borde, Focused ? 2 : 1))
            {
                g.FillPath(fondo, ruta);
                g.DrawPath(borde, ruta);
            }
            var area = new Rectangle(p, p, Width - 2 * p, Width - 2 * p);
            if (Imagen(_foto.Clave) is { } imagen)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using var recorte = RecursosEco.Redondeado(area, TemaEcoAccess.Px(this, 8));
                var estado = g.Save();
                g.SetClip(recorte);
                float k = Math.Max((float)area.Width / imagen.Width, (float)area.Height / imagen.Height);
                float w = imagen.Width * k, h = imagen.Height * k;
                g.DrawImage(imagen, area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
                g.Restore(estado);
            }
            Color color = _foto.Tipo switch
            {
                TipoFoto.PorClasificar => Color.FromArgb(0xB4, 0x5F, 0x06),
                _ => TemaEcoAccess.Texto,
            };
            var texto = new Rectangle(p, area.Bottom + p / 2, Width - 2 * p, Height - area.Bottom - p);
            TextRenderer.DrawText(g, _foto.Titulo, TemaEcoAccess.Pequena, texto, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Foto en grande con su título y, si está por clasificar, la razón.</summary>
    internal sealed class FormFoto : Form
    {
        internal FormFoto(FotoCaneca foto)
        {
            Text = foto.Titulo + " · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = TemaEcoAccess.Cuerpo;
            MinimizeBox = MaximizeBox = false;
            KeyPreview = true;
            Size = new Size(TemaEcoAccess.Px(this, 640), TemaEcoAccess.Px(this, 720));
            var imagen = new ImagenAjustada { Dock = DockStyle.Fill, Imagen = MiniaturaFoto.Imagen(foto.Clave), AccessibleName = foto.Titulo };
            var pie = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = false,
                Height = TemaEcoAccess.Px(this, foto.Nota is null ? 48 : 84),
                Padding = new Padding(16, 8, 16, 8),
                Font = foto.Nota is null ? TemaEcoAccess.Subtitulo : TemaEcoAccess.Cuerpo,
                ForeColor = foto.Tipo == TipoFoto.PorClasificar ? Color.FromArgb(0xB4, 0x5F, 0x06) : TemaEcoAccess.VerdeOscuro,
                Text = foto.Nota is null ? foto.Titulo : "Por clasificar (no se deposita todavía): " + foto.Nota,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            Controls.Add(imagen);
            Controls.Add(pie);
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }
    }

    /// <summary>Ventana con la galería completa de una caneca (desde el catálogo de materiales).</summary>
    internal sealed class FormGaleriaCaneca : Form
    {
        internal FormGaleriaCaneca(CategoriaMaterial caneca)
        {
            var galeria = new GaleriaFotosCaneca(caneca);
            Text = $"{CatalogoPrototipo.NombreCaneca(caneca)}: todas las fotos ({galeria.Cantidad}) · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = TemaEcoAccess.Fondo;
            Font = TemaEcoAccess.Cuerpo;
            MinimizeBox = false;
            KeyPreview = true;
            Size = new Size(TemaEcoAccess.Px(this, 1080), TemaEcoAccess.Px(this, 760));
            var desplazable = new PanelDesplazable { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 12, 16), BackColor = TemaEcoAccess.Fondo };
            desplazable.Controls.Add(galeria);
            desplazable.ClientSizeChanged += (_, _) => galeria.Adaptar(desplazable.ClientSize.Width - desplazable.Padding.Horizontal);
            Controls.Add(desplazable);
            Shown += (_, _) => { galeria.Adaptar(desplazable.ClientSize.Width - desplazable.Padding.Horizontal); galeria.Controls.Cast<Control>().FirstOrDefault()?.Focus(); };
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }
    }
}
