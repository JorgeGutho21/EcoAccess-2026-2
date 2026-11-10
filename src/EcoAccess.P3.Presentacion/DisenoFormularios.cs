using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Composición visual reutilizable; no decide validaciones, permisos ni operaciones.</summary>
    internal static class DisenoFormularios
    {
        /// <summary>
        /// Repara formularios del Designer con posiciones fijas cuando el texto real (fuente del tema, escala de Windows)
        /// no cabe: cada botón o etiqueta crece hasta su texto (en dos líneas si hace falta) y las filas siguientes bajan.
        /// Conserva los mismos controles y eventos; no cambia anchos ni lógica. Se probó al 100 % en 1366×768, donde los
        /// formularios diseñados al 125 % cortaban «Poner en servicio», «Registrar» o «Consultar».
        /// </summary>
        internal static void AjustarDisenoFijo(Control raiz)
        {
            foreach (Control hijo in raiz.Controls)
            {
                if (EsContenedorLibre(hijo)) AjustarDisenoFijo(hijo);
                else if (hijo is FlowLayoutPanel or TableLayoutPanel) AjustarAltosEnFlujo(hijo);
            }
            var libres = new System.Collections.Generic.List<Control>();
            foreach (Control c in raiz.Controls)
                if (c.Dock == DockStyle.None && (c.Anchor & AnchorStyles.Bottom) == 0) libres.Add(c);
            libres.Sort((a, b) => a.Top != b.Top ? a.Top.CompareTo(b.Top) : a.Left.CompareTo(b.Left));
            int desplazamiento = 0, fondoAnterior = int.MinValue, i = 0;
            int separacion = TemaEcoAccess.Px(raiz, 3);
            while (i < libres.Count)
            {
                int topeOriginal = libres[i].Top;
                var fila = new System.Collections.Generic.List<Control>();
                while (i < libres.Count && libres[i].Top < topeOriginal + TemaEcoAccess.Px(raiz, 8)) fila.Add(libres[i++]);
                if (fondoAnterior != int.MinValue && topeOriginal + desplazamiento < fondoAnterior + separacion)
                    desplazamiento = fondoAnterior + separacion - topeOriginal;
                foreach (Control c in fila)
                {
                    c.Top += desplazamiento;
                    int necesario = AltoNecesario(c);
                    if (necesario > c.Height) c.Height = necesario;
                }
                foreach (Control c in fila) fondoAnterior = System.Math.Max(fondoAnterior, c.Bottom);
            }
            // Un grupo o panel crece lo que creció su contenido; el formulario raíz se desplaza (AutoScroll del menú).
            if (raiz is GroupBox or Panel && raiz is not Form && fondoAnterior != int.MinValue)
            {
                int falta = fondoAnterior + TemaEcoAccess.Px(raiz, 8) - raiz.ClientSize.Height;
                if (falta <= 0) return;
                // Un panel anclado arriba y abajo no puede crecer (lo fija la ventana): se desplaza en lugar de esconder campos.
                if (raiz is Panel panel && (panel.Anchor & AnchorStyles.Bottom) != 0 && (panel.Anchor & AnchorStyles.Top) != 0)
                {
                    panel.AutoScroll = true;
                    panel.AutoScrollMinSize = new Size(0, fondoAnterior + TemaEcoAccess.Px(raiz, 8));
                }
                else raiz.Height += falta;
            }
        }

        /// <summary>En paneles de flujo o tabla solo hace falta la altura: el propio panel reacomoda las filas.</summary>
        private static void AjustarAltosEnFlujo(Control panel)
        {
            foreach (Control c in panel.Controls)
            {
                if (c is FlowLayoutPanel or TableLayoutPanel) AjustarAltosEnFlujo(c);
                else if (EsContenedorLibre(c)) AjustarDisenoFijo(c);
                else if (c is ButtonBase { AutoSize: false } or Label { AutoSize: false })
                {
                    // En un panel de flujo sobra espacio a lo ancho: un botón crece a lo ancho y su texto queda en una línea.
                    if (c is Button && !string.IsNullOrEmpty(c.Text))
                    {
                        int ancho = TextRenderer.MeasureText(c.Text, c.Font).Width + TemaEcoAccess.Px(c, 18);
                        if (ancho > c.Width) c.Width = ancho;
                    }
                    int necesario = AltoNecesario(c) + c.Margin.Vertical;
                    if (necesario - c.Margin.Vertical > c.Height) c.Height = necesario - c.Margin.Vertical;
                }
            }
            // Una fila de filtros con alto fijo crece hasta su contenido en vez de esconder el botón «Consultar».
            if (panel is FlowLayoutPanel { AutoSize: false } flujo && flujo.Dock is DockStyle.Top or DockStyle.None)
            {
                int preferido = flujo.GetPreferredSize(new Size(flujo.ClientSize.Width, 0)).Height;
                if (preferido > flujo.Height) flujo.Height = preferido;
            }
            // Si la fila de filtros ocupa una celda de alto fijo de una tabla, crece la fila (y la tabla anclada arriba).
            else if (panel is FlowLayoutPanel { AutoSize: false, Dock: DockStyle.Fill } celda && celda.Parent is TableLayoutPanel tabla)
            {
                int fila = tabla.GetRow(celda);
                int preferido = celda.GetPreferredSize(new Size(celda.ClientSize.Width, 0)).Height;
                int falta = preferido - celda.Height;
                if (falta > 0 && fila >= 0 && fila < tabla.RowStyles.Count && tabla.RowStyles[fila].SizeType == SizeType.Absolute)
                {
                    tabla.RowStyles[fila].Height += falta;
                    if (tabla.Dock == DockStyle.Top && !tabla.AutoSize) tabla.Height += falta;
                }
            }
        }

        private static bool EsContenedorLibre(Control c) =>
            c is GroupBox or TabPage or Panel && c is not TableLayoutPanel and not FlowLayoutPanel and not SplitterPanel
            || c is TabControl or SplitContainer;

        private static int AltoNecesario(Control c)
        {
            if (c is TabControl or SplitContainer or DataGridView or ListBox or TextBoxBase { Multiline: true }) return c.Height;
            if (c is Label { AutoSize: true }) return c.PreferredSize.Height;
            // Un botón cuyo texto ya cabe a lo ancho (según su propio cálculo) conserva su alto: no se parte en dos líneas.
            if (c is Button && !string.IsNullOrEmpty(c.Text))
            {
                Size propio = c.GetPreferredSize(Size.Empty);
                if (propio.Width <= c.Width) return System.Math.Max(c.Height, propio.Height);
            }
            if (c is ButtonBase or Label && !string.IsNullOrEmpty(c.Text))
            {
                int margen = c is ButtonBase and not CheckBox and not RadioButton ? TemaEcoAccess.Px(c, 14) : TemaEcoAccess.Px(c, 4);
                int ancho = System.Math.Max(1, c.Width - (c is ButtonBase ? TemaEcoAccess.Px(c, 12) : 0) - (c is CheckBox or RadioButton ? TemaEcoAccess.Px(c, 20) : 0));
                // Primero en una línea; solo si no cabe a lo ancho se mide en varias.
                Size unaLinea = TextRenderer.MeasureText(c.Text, c.Font);
                if (unaLinea.Width <= ancho) return unaLinea.Height + margen;
                Size medido = TextRenderer.MeasureText(c.Text, c.Font, new Size(ancho, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                return medido.Height + margen;
            }
            return c.Height;
        }

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
