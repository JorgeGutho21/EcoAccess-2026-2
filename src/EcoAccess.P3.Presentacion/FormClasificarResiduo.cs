using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// «Clasificar residuo» del panel Moderno: grupo → residuo → caneca. Usa las mismas reglas que la estación táctil
    /// (<see cref="GuiaEstacionTactil"/>) y la categoría guardada en Oracle; solo orienta, no abre canecas ni registra nada.
    /// </summary>
    public sealed class FormClasificarResiduo : Form
    {
        private readonly IReadOnlyList<Material> _catalogo;
        private readonly TextBox _buscar = new() { Dock = DockStyle.Fill, Font = TemaEcoAccess.Entrada, AccessibleName = "Buscar residuo por nombre" };
        private readonly ListBox _lista = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12), IntegralHeight = false, BorderStyle = BorderStyle.None, DisplayMember = nameof(Material.Nombre) };
        private readonly TableLayoutPanel _resultado = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        private GrupoEstacion? _grupo;

        public FormClasificarResiduo(IReadOnlyList<Material> catalogo, GrupoEstacion? grupo)
        {
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
            _grupo = grupo;
            Text = "Clasificar residuo";
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1040, 600);
            MinimumSize = new Size(860, 520);
            MinimizeBox = false;

            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(TemaEcoAccess.Px(this, 16)) };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 230)));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 40)));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var grupos = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            grupos.Controls.Add(BotonGrupo("Todos", IconChar.List, null));
            foreach (GrupoEstacion g in GuiaEstacionTactil.Grupos) grupos.Controls.Add(BotonGrupo(GuiaEstacionTactil.Nombre(g), Icono(g), g));
            raiz.Controls.Add(new Label { Text = "Tipo", Dock = DockStyle.Fill, Font = TemaEcoAccess.Subtitulo, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            raiz.Controls.Add(grupos, 0, 1);
            raiz.Controls.Add(_buscar, 1, 0);
            raiz.Controls.Add(_lista, 1, 1);
            raiz.Controls.Add(new Label { Text = "Dónde va", Dock = DockStyle.Fill, Font = TemaEcoAccess.Subtitulo, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) }, 2, 0);
            _resultado.Margin = new Padding(TemaEcoAccess.Px(this, 12), 3, 0, 0);
            raiz.Controls.Add(_resultado, 2, 1);
            Controls.Add(raiz);

            _buscar.TextChanged += (_, _) => Filtrar();
            _lista.SelectedIndexChanged += (_, _) => { if (_lista.SelectedItem is Material m) Mostrar(GuiaEstacionTactil.Orientar(m)); };
            Filtrar();
            Mostrar(null);
        }

        private Button BotonGrupo(string texto, IconChar icono, GrupoEstacion? grupo)
        {
            var boton = new IconButton
            {
                Text = "  " + texto, IconChar = icono, IconSize = TemaEcoAccess.Px(this, 22), IconColor = grupo == GrupoEstacion.Especiales ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro,
                TextImageRelation = TextImageRelation.ImageBeforeText, ImageAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft,
                Width = TemaEcoAccess.Px(this, 210), Height = TemaEcoAccess.Px(this, 44), FlatStyle = FlatStyle.Flat, BackColor = TemaEcoAccess.Superficie,
                ForeColor = TemaEcoAccess.Texto, Font = TemaEcoAccess.Cuerpo, Margin = new Padding(0, 0, 0, 6), Cursor = Cursors.Hand,
            };
            boton.FlatAppearance.BorderColor = TemaEcoAccess.Borde;
            boton.Click += (_, _) => { _grupo = grupo; Filtrar(); };
            return boton;
        }

        private void Filtrar()
        {
            string texto = _buscar.Text.Trim();
            IEnumerable<Material> materiales = _grupo is { } g ? GuiaEstacionTactil.MaterialesDe(g, _catalogo) : _catalogo.OrderBy(m => m.Nombre);
            if (texto.Length > 0) materiales = materiales.Where(m => m.Nombre.IndexOf(texto, StringComparison.CurrentCultureIgnoreCase) >= 0);
            _lista.BeginUpdate();
            _lista.Items.Clear();
            foreach (Material m in materiales) _lista.Items.Add(m);
            _lista.EndUpdate();
        }

        private void Mostrar(OrientacionEstacion? orientacion)
        {
            _resultado.SuspendLayout();
            _resultado.Controls.Clear();
            _resultado.RowStyles.Clear();
            if (orientacion is null)
            {
                _resultado.Controls.Add(new Label { Text = "Elige un residuo de la lista para ver en qué caneca va.", Dock = DockStyle.Fill, ForeColor = TemaEcoAccess.Suave, TextAlign = ContentAlignment.MiddleCenter });
                _resultado.ResumeLayout();
                return;
            }
            (Color fondo, Color texto, IconChar icono) = orientacion.Destino switch
            {
                DestinoResiduo.Blanca => (Color.White, Color.FromArgb(0x1F, 0x29, 0x37), IconChar.Recycle),
                DestinoResiduo.Verde => (TemaEcoAccess.VerdeOscuro, Color.White, IconChar.Seedling),
                DestinoResiduo.Negra => (Color.FromArgb(0x1F, 0x29, 0x37), Color.White, IconChar.TrashCan),
                _ => (TemaEcoAccess.Error, Color.White, IconChar.TriangleExclamation),
            };
            _resultado.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            _resultado.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            _resultado.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            _resultado.Controls.Add(new IconPictureBox { IconChar = icono, IconColor = texto, BackColor = fondo, IconSize = TemaEcoAccess.Px(this, 96), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = Padding.Empty }, 0, 0);
            _resultado.Controls.Add(new Label { Text = orientacion.Titulo.ToUpperInvariant(), Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 22), ForeColor = texto, BackColor = fondo, TextAlign = ContentAlignment.MiddleCenter, Margin = Padding.Empty }, 0, 1);
            _resultado.Controls.Add(new Label { Text = orientacion.Material.Nombre + "\n\n" + orientacion.Indicacion, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12), ForeColor = TemaEcoAccess.Texto, Padding = new Padding(8), UseMnemonic = false }, 0, 2);
            _resultado.ResumeLayout();
        }

        private static IconChar Icono(GrupoEstacion grupo) => grupo switch
        {
            GrupoEstacion.Plastico => IconChar.BottleWater,
            GrupoEstacion.PapelCarton => IconChar.Newspaper,
            GrupoEstacion.VidrioMetal => IconChar.WineBottle,
            GrupoEstacion.Comida => IconChar.AppleWhole,
            GrupoEstacion.Otros => IconChar.ToiletPaper,
            _ => IconChar.BatteryFull,
        };
    }
}
