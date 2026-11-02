using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Marco visual para catálogos; recibe controles existentes y no conoce servicios ni permisos.</summary>
    internal sealed class VistaGestionCatalogo : UserControl
    {
        private readonly TableLayoutPanel _cuerpo = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        private readonly Control _editor;
        private readonly TableLayoutPanel _raiz;
        private bool? _apilado;

        internal VistaGestionCatalogo(string titulo, string descripcion, IconChar icono,
            Control filtros, DataGridView tabla, Control paginacion, Control editor, Label estado)
        {
            Dock = DockStyle.Fill;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = TemaEcoAccess.Cuerpo;
            AutoScroll = true;
            _editor = editor;
            var raiz = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20),
                BackColor = TemaEcoAccess.Fondo
            };
            _raiz = raiz;
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var cabecera = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            cabecera.Controls.Add(new IconoEcoAccess
            {
                IconChar = icono,
                IconColor = TemaEcoAccess.Verde,
                Size = new Size(38, 38),
                IconSize = 28,
                TabStop = false,
                Margin = new Padding(0, 4, 12, 0)
            });
            var textos = DisenoFormularios.Columna();
            textos.Padding = Padding.Empty;
            textos.BackColor = TemaEcoAccess.Fondo;
            DisenoFormularios.Agregar(textos, new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.Titulo, ForeColor = TemaEcoAccess.Texto });
            DisenoFormularios.Agregar(textos, DisenoFormularios.Texto(descripcion));
            cabecera.Controls.Add(textos);
            cabecera.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(160, cabecera.ClientSize.Width - TemaEcoAccess.Px(this, 60));
                textos.MaximumSize = new Size(ancho, 0);
                foreach (Label etiqueta in textos.Controls.OfType<Label>()) etiqueta.MaximumSize = new Size(ancho, 0);
            };
            raiz.Controls.Add(cabecera, 0, 0);
            filtros.Dock = DockStyle.Fill;
            filtros.TabIndex = 0;
            raiz.Controls.Add(filtros, 0, 1);
            _cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
            _cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var listado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 14, 0) };
            listado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            listado.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listado.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            DisenoFormularios.PrepararTabla(tabla);
            tabla.TabIndex = 0;
            listado.Controls.Add(tabla, 0, 0);
            paginacion.Dock = DockStyle.Fill;
            paginacion.TabIndex = 1;
            listado.Controls.Add(paginacion, 0, 1);
            _cuerpo.Controls.Add(listado, 0, 0);
            editor.Dock = DockStyle.Fill;
            editor.TabIndex = 1;
            _cuerpo.Controls.Add(editor, 1, 0);
            raiz.Controls.Add(_cuerpo, 0, 2);
            estado.Dock = DockStyle.Fill;
            estado.AutoSize = true;
            estado.MinimumSize = new Size(0, 42);
            estado.Padding = new Padding(0, 12, 0, 0);
            raiz.Controls.Add(estado, 0, 3);
            estado.TextChanged += (_, _) => AjustarAltura();
            SizeChanged += (_, _) =>
            {
                estado.MaximumSize = new Size(Math.Max(160, ClientSize.Width - TemaEcoAccess.Px(this, 40)), 0);
                AjustarAltura();
            };
            _cuerpo.SizeChanged += (_, _) => Adaptar();
            Controls.Add(raiz);
            TemaEcoAccess.Aplicar(this);
            Adaptar();
        }

        private void Adaptar()
        {
            bool apilar = _cuerpo.ClientSize.Width < TemaEcoAccess.Px(this, 900);
            if (_apilado == apilar) return;
            _apilado = apilar;
            _cuerpo.SuspendLayout();
            _cuerpo.ColumnStyles[1].Width = apilar ? 0 : TemaEcoAccess.Px(this, 350);
            // En modo apilado se conserva espacio útil para ambas áreas; el marco permite desplazarse.
            _cuerpo.MinimumSize = new Size(0, TemaEcoAccess.Px(this, apilar ? 560 : 280));
            _cuerpo.RowCount = apilar ? 2 : 1;
            _cuerpo.RowStyles.Clear();
            _cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, apilar ? 48 : 100));
            if (apilar) _cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            _cuerpo.SetCellPosition(_editor, new TableLayoutPanelCellPosition(apilar ? 0 : 1, apilar ? 1 : 0));
            _cuerpo.ResumeLayout(true);
            AjustarAltura();
        }

        private void AjustarAltura()
        {
            int preferida = _raiz.GetPreferredSize(new Size(Math.Max(1, ClientSize.Width - SystemInformation.VerticalScrollBarWidth), 0)).Height;
            int altura = Math.Max(ClientSize.Height, preferida);
            if (_raiz.Height != altura) _raiz.Height = altura;
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            _apilado = null;
            Adaptar();
        }
    }
}
