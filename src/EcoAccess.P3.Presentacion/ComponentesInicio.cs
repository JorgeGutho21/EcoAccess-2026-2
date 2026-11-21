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
