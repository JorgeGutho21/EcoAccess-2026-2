using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Composición visual reutilizable; no decide validaciones, permisos ni operaciones.</summary>
    internal static class DisenoFormularios
    {
        internal static TableLayoutPanel Columna()
        {
            var panel = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                ColumnCount = 1,
                Padding = new Padding(16),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 12)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }

        internal static void Agregar(TableLayoutPanel panel, Control control)
        {
            int fila = panel.Controls.Count;
            panel.RowCount = fila + 1;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Fill;
            control.TabIndex = fila;
            control.Margin = new Padding(0, 0, 0, 12);
            panel.Controls.Add(control, 0, fila);
        }

        internal static void Campo(TableLayoutPanel panel, Label etiqueta, Control entrada)
        {
            etiqueta.AutoSize = true;
            etiqueta.ForeColor = TemaEcoAccess.Texto;
            etiqueta.Margin = new Padding(0, 4, 0, 5);
            Agregar(panel, etiqueta);
            etiqueta.Margin = new Padding(0, 4, 0, 5);
            entrada.AccessibleName = etiqueta.Text;
            entrada.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Agregar(panel, entrada);
        }

        internal static Label Texto(string texto, bool destacado = false) => new()
        {
            Text = texto,
            AutoSize = true,
            ForeColor = destacado ? TemaEcoAccess.Texto : TemaEcoAccess.Suave,
            Font = destacado ? TemaEcoAccess.Subtitulo : TemaEcoAccess.Cuerpo,
            Margin = new Padding(0, 0, 0, 12)
        };

        internal static void PrepararTabla(DataGridView tabla)
        {
            tabla.Dock = DockStyle.Fill;
            tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tabla.DefaultCellStyle.Padding = new Padding(8, 5, 8, 5);
            tabla.ColumnHeadersDefaultCellStyle.Padding = new Padding(8);
            tabla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            tabla.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            tabla.RowHeadersVisible = false;
            tabla.AccessibleName = "Resultados de la consulta";
            TemaEcoAccess.Aplicar(tabla);
        }

        internal static Button Accion(string texto) => new()
        {
            Text = texto,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(100, 36),
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 8)
        };
    }
}
