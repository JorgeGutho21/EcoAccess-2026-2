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
        /// <summary>
        /// Las etiquetas de texto largo tenían un ancho máximo fijo (900 a 1100 px) y en pantallas angostas (1366×768 al 125 %)
        /// se salían por la derecha. Cada vez que el contenedor cambia de tamaño, su ancho máximo pasa a ser el disponible.
        /// </summary>
        internal static void AjustarEtiquetasAlAncho(Control contenedor, params Label[] etiquetas)
        {
            void Ajustar()
            {
                foreach (Label etiqueta in etiquetas)
                {
                    int disponible = contenedor.ClientSize.Width - contenedor.Padding.Horizontal - etiqueta.Margin.Horizontal - TemaEcoAccess.Px(contenedor, 12);
                    etiqueta.MaximumSize = new Size(System.Math.Max(TemaEcoAccess.Px(contenedor, 200), disponible), 0);
                }
            }
            contenedor.SizeChanged += (_, _) => Ajustar();
            Ajustar();
        }

        /// <summary>
        /// Ancho mínimo de un formulario del Designer para que ningún control de posición fija quede fuera de su panel. En
        /// «Estadísticas», a 1366×768 con escala del 125 %, los filtros de la derecha se cortaban (cierre de software, 15/11/2026).
        /// Recorre los paneles que se estiran con la ventana: su ancho cambia lo mismo que el del formulario.
        /// </summary>
        internal static int AnchoNecesario(Control raiz)
        {
            int necesario = 0, margen = TemaEcoAccess.Px(raiz, 8);
            void Recorrer(Control contenedor, int holgura)
            {
                foreach (Control hijo in contenedor.Controls)
                {
                    if (hijo.Dock == DockStyle.None && (hijo.Anchor & AnchorStyles.Right) == 0)
                        necesario = System.Math.Max(necesario, hijo.Right + margen + holgura);
                    bool seEstira = hijo.Dock is DockStyle.Fill or DockStyle.Top or DockStyle.Bottom
                        || (hijo.Dock == DockStyle.None && (hijo.Anchor & (AnchorStyles.Left | AnchorStyles.Right)) == (AnchorStyles.Left | AnchorStyles.Right));
                    if (seEstira && hijo is GroupBox or Panel && hijo is not (FlowLayoutPanel or TableLayoutPanel or TabPage or MarcoEntrada)
                        && !(hijo is ScrollableControl { AutoScroll: true }))
                        Recorrer(hijo, raiz.ClientSize.Width - hijo.ClientSize.Width);
                }
            }
            Recorrer(raiz, 0);
            return necesario;
        }

        internal static void AjustarDisenoFijo(Control raiz)
        {
            foreach (Control hijo in raiz.Controls)
            {
                if (EsContenedorLibre(hijo)) AjustarDisenoFijo(hijo);
                else if (hijo is FlowLayoutPanel or TableLayoutPanel) AjustarAltosEnFlujo(hijo);
            }
            // Cierre de software (15/11/2026): una etiqueta automática más ancha que su grupo («Nombre (ej. Caneca entrada norte)»)
            // se salía por la derecha. Se limita al ancho disponible y pasa a dos líneas; las filas siguientes bajan abajo.
            if (raiz is GroupBox or Panel && raiz is not Form)
            {
                int margen = TemaEcoAccess.Px(raiz, 6);
                foreach (Control c in raiz.Controls)
                    if (c is Label { AutoSize: true } etiqueta && etiqueta.Dock == DockStyle.None && etiqueta.Right > raiz.ClientSize.Width - margen
                        && raiz.ClientSize.Width - etiqueta.Left - margen > TemaEcoAccess.Px(raiz, 40))
                        etiqueta.MaximumSize = new Size(raiz.ClientSize.Width - etiqueta.Left - margen, 0);
            }
            // La línea de estado anclada abajo («Error: no se pudo completar la operación…») debe mostrar dos líneas completas: en
            // 1366×768 al 125 % le faltaban 5 px. Crece hacia arriba y lo que está estirado encima se acorta lo mismo.
            foreach (Control c in raiz.Controls)
            {
                if (c is not Label { AutoSize: false } estado || (estado.Anchor & AnchorStyles.Bottom) == 0 || (estado.Anchor & AnchorStyles.Top) != 0) continue;
                int dosLineas = 2 * TextRenderer.MeasureText("Ág", estado.Font).Height + estado.Padding.Vertical + TemaEcoAccess.Px(raiz, 2);
                int crecer = dosLineas - estado.Height;
                if (crecer <= 0) continue;
                estado.Top -= crecer;
                estado.Height += crecer;
                foreach (Control encima in raiz.Controls)
                    if (encima != estado && (encima.Anchor & (AnchorStyles.Top | AnchorStyles.Bottom)) == (AnchorStyles.Top | AnchorStyles.Bottom)
                        && encima.Top < estado.Top && encima.Bottom > estado.Top - TemaEcoAccess.Px(raiz, 4))
                        encima.Height = System.Math.Max(TemaEcoAccess.Px(raiz, 120), estado.Top - TemaEcoAccess.Px(raiz, 4) - encima.Top);
            }
            var libres = new System.Collections.Generic.List<Control>();
            foreach (Control c in raiz.Controls)
                if (c.Dock == DockStyle.None && (c.Anchor & AnchorStyles.Bottom) == 0) libres.Add(c);
            libres.Sort((a, b) => a.Top != b.Top ? a.Top.CompareTo(b.Top) : a.Left.CompareTo(b.Left));
            var topesOriginales = new System.Collections.Generic.Dictionary<Control, int>();
            foreach (Control c in libres) topesOriginales[c] = c.Top;
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
            // Misión maestra (14/11/2026): un control estirado arriba y abajo (pestañas, tablas) no estaba en la lista y no bajaba
            // cuando bajaban los de encima. En Estadísticas el resumen quedaba 16 px encima de las pestañas. Ahora baja y encoge lo
            // mismo, sin mover su borde inferior (lo sigue fijando la ventana).
            foreach (Control c in raiz.Controls)
            {
                if (c.Dock != DockStyle.None || (c.Anchor & (AnchorStyles.Top | AnchorStyles.Bottom)) != (AnchorStyles.Top | AnchorStyles.Bottom)) continue;
                int fondoEncima = int.MinValue;
                foreach (var par in topesOriginales)
                    if (par.Value < c.Top && par.Key.Right > c.Left && par.Key.Left < c.Right) fondoEncima = System.Math.Max(fondoEncima, par.Key.Bottom);
                int bajar = fondoEncima == int.MinValue ? 0 : fondoEncima + separacion - c.Top;
                if (bajar <= 0) continue;
                int fondo = c.Bottom;
                c.Top += bajar;
                c.Height = System.Math.Max(TemaEcoAccess.Px(raiz, 120), fondo - c.Top);
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
                    // La barra vertical le quita su ancho al panel y el contenido se salía por la derecha (botones de «Canecas y QR»
                    // cortados y desplazamiento horizontal, cierre de software 15/11/2026). Un panel anclado a la derecha se ensancha
                    // hacia la izquierda lo que mide la barra, y el vecino estirado de su izquierda se angosta lo mismo.
                    int barra = SystemInformation.VerticalScrollBarWidth;
                    if ((panel.Anchor & AnchorStyles.Right) != 0 && (panel.Anchor & AnchorStyles.Left) == 0 && panel.Parent is { } contenedor)
                    {
                        int bordeAnterior = panel.Left;
                        foreach (Control vecino in contenedor.Controls)
                            if (vecino != panel && (vecino.Anchor & (AnchorStyles.Left | AnchorStyles.Right)) == (AnchorStyles.Left | AnchorStyles.Right)
                                && vecino.Right <= bordeAnterior && vecino.Right > bordeAnterior - TemaEcoAccess.Px(raiz, 40)
                                && vecino.Top < panel.Bottom && vecino.Bottom > panel.Top && vecino.Width > barra + TemaEcoAccess.Px(raiz, 200))
                                vecino.Width -= barra;
                        panel.Left -= barra;
                        panel.Width += barra;
                    }
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
            c is GroupBox or TabPage or Panel && c is not TableLayoutPanel and not FlowLayoutPanel and not SplitterPanel and not MarcoEntrada
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

        /// <summary>Agrega un control a una columna. Los botones quedan con el estilo Eco, a todo el ancho y con su icono.</summary>
        internal static void Agregar(TableLayoutPanel panel, Control control)
        {
            int fila = panel.Controls.Count;
            panel.RowCount = fila + 1;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Si el módulo ya le dio icono (y estilo), se respeta; si no, se elige por su nombre o texto.
            if (control is Button boton) ModuloEco.Boton(boton, EstiloControlesEco.TieneIcono(boton) ? FontAwesome.Sharp.IconChar.None : ModuloEco.IconoBoton(boton), ancho: true);
            control.Dock = DockStyle.Fill;
            control.TabIndex = fila;
            control.Margin = new Padding(0, 0, 0, control is Button ? 10 : 12);
            panel.Controls.Add(control, 0, fila);
        }

        /// <summary>Campo Eco (skill eco-estilo-base): etiqueta semibold arriba y caja de 46 px con icono, como el inicio de sesión.</summary>
        internal static void Campo(TableLayoutPanel panel, Label etiqueta, Control entrada, FontAwesome.Sharp.IconChar? icono = null)
        {
            entrada.AccessibleName = etiqueta.Text.Replace("&", "");
            bool multilinea = entrada is TextBoxBase { Multiline: true };
            var campo = ModuloEco.Campo(etiqueta, icono ?? ModuloEco.IconoCampo(etiqueta.Text + " " + entrada.Name), entrada,
                alto: multilinea ? System.Math.Max(96, entrada.Height) : ModuloEco.AltoCampo);
            Agregar(panel, campo);
            campo.Margin = new Padding(0, 0, 0, 14);
        }

        /// <summary>Texto de una columna; el destacado es un subtítulo de sección en verde oscuro (semibold 14).</summary>
        internal static Label Texto(string texto, bool destacado = false) => new()
        {
            Text = texto,
            AutoSize = true,
            ForeColor = destacado ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave,
            Font = destacado ? TemaEcoAccess.TituloSeccion : TemaEcoAccess.Cuerpo,
            Margin = new Padding(0, 0, 0, 12),
            UseMnemonic = false,
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

        /// <summary>Botón de acción con el estilo Eco: semibold 11, alto 42 e icono según su texto.</summary>
        internal static Button Accion(string texto)
        {
            var boton = new Button { Text = texto };
            return ModuloEco.Boton(boton, ModuloEco.IconoBoton(boton));
        }
    }
}
