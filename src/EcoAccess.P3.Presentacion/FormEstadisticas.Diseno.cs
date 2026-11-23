using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Estadísticas y reportes» (skill eco-ui-supervisor): banner con el período y los depósitos consultados,
    /// tarjeta de filtros, resumen en tarjetas con cifras y barras proporcionales, y resultados con pestañas tipo píldora
    /// (gráfico de los grupos sobre su tabla). Reutiliza los controles del Designer y no cambia la lógica: el resumen se lee
    /// del mismo texto que escribe el formulario en <c>textoResumen</c>, y las tablas se pintan sobre los datos ya cargados.
    /// </summary>
    public partial class FormEstadisticas
    {
        private static readonly CultureInfo CulturaDiseno = CultureInfo.GetCultureInfo("es-CO");
        private readonly BannerModulo _banner = new("Estadísticas y reportes",
            "Filtra los depósitos por fecha, caneca, material, persona y verificación; agrúpalos y exporta la página consultada.", IconChar.CalendarDays)
        {
            Estado = "Consulta un período para ver el resumen.",
        };
        private readonly ResumenEstadisticas _tarjetasResumen = new();
        private readonly Label _aclaracionResumen = new() { UseMnemonic = false };
        private readonly GraficoGrupos _graficoGrupos = new() { Dock = DockStyle.Fill };
        private readonly GraficoBarras _graficoDias = new() { Dock = DockStyle.Fill, Visible = false, AccessibleName = "Depósitos por día" };
        private readonly PanelDesplazable _desplazable = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo, AccessibleName = "Estadísticas y reportes" };
        private SeccionEco? _resultados;
        private TableLayoutPanel? _raiz;
        private bool _altoPendiente;

        private void ConstruirDiseno()
        {
            // Pestañas sin la franja de Windows (se eligen con píldoras). Se cambia el control del Designer por uno igual sin
            // franja, con las mismas páginas: el formulario sigue usando «pestanas» (lo deshabilita mientras consulta).
            var paginas = new PaginasSinPestanas { Name = pestanas.Name, TabIndex = pestanas.TabIndex, AccessibleName = "Resultados de la consulta" };
            foreach (TabPage pagina in pestanas.TabPages.Cast<TabPage>().ToArray())
            {
                pestanas.TabPages.Remove(pagina);
                paginas.TabPages.Add(pagina);
            }
            TabControl original = pestanas;
            pestanas = paginas;

            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.ChartColumn, "Estadísticas y reportes", "", _banner);
            original.Dispose();
            _raiz = raiz;

            // Filtros en rejilla: 8 campos + «Consultar» = 9 celdas, en 5 columnas (2 filas) en pantallas anchas, 3 × 3 en las
            // medianas y 1 columna en las angostas. Los campos llenan su columna, así nunca queda un botón solo en una fila.
            Control[] celdas =
            {
                ModuloEco.Campo(etiquetaDesde, IconChar.CalendarDays, fechaDesde),
                ModuloEco.Campo(etiquetaHasta, IconChar.CalendarDays, fechaHasta),
                ModuloEco.Campo(etiquetaCaneca, IconChar.TrashCan, comboCaneca),
                ModuloEco.Campo(etiquetaMaterial, IconChar.Leaf, comboMaterial),
                ModuloEco.Campo(etiquetaUsuario, IconChar.User, numeroUsuario),
                ModuloEco.Campo(etiquetaVerificacion, IconChar.ClipboardCheck, comboVerificacion),
                ModuloEco.Campo(etiquetaCriterio, IconChar.ListCheck, comboCriterio),
                ModuloEco.Campo(etiquetaAgrupacion, IconChar.LayerGroup, comboAgrupacion),
                ModuloEco.JuntoACampos(ModuloEco.Boton(botonConsultar, IconChar.MagnifyingGlass)),
            };
            foreach (Control campo in celdas.Take(8)) campo.Dock = DockStyle.Fill;
            botonConsultar.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            var campos = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            int columnas = 0;
            void Repartir()
            {
                int ancho = panelFiltros.ClientSize.Width;
                if (ancho <= 0) return;
                int n = ancho >= TemaEcoAccess.Px(panelFiltros, 1250) ? 5 : ancho >= TemaEcoAccess.Px(panelFiltros, 620) ? 3 : 1;
                if (n == columnas) return;
                columnas = n;
                campos.SuspendLayout();
                campos.Controls.Clear();
                campos.ColumnStyles.Clear();
                campos.RowStyles.Clear();
                campos.ColumnCount = n;
                campos.RowCount = (celdas.Length + n - 1) / n;
                for (int k = 0; k < n; k++) campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / n));
                for (int k = 0; k < campos.RowCount; k++) campos.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                for (int k = 0; k < celdas.Length; k++) campos.Controls.Add(celdas[k], k % n, k / n);
                campos.ResumeLayout(true);
            }
            // panelFiltros (del Designer) se conserva como contenedor: sigue al alto de la rejilla de campos.
            panelFiltros.Controls.Clear();
            panelFiltros.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelFiltros.BackColor = TemaEcoAccess.Superficie;
            panelFiltros.BorderStyle = BorderStyle.None;
            panelFiltros.Controls.Add(campos);
            campos.SizeChanged += (_, _) => panelFiltros.Height = campos.Height;
            panelFiltros.SizeChanged += (_, _) => Repartir();
            Repartir();
            var filtros = ModuloEco.Seccion("Filtros", "La fecha es la de Colombia. «Id usuario» en 0 incluye a todas las personas.", icono: IconChar.Filter);
            filtros.Agregar(panelFiltros);
            ModuloEco.Fila(raiz, filtros);

            // Resumen: tarjetas dibujadas a partir del texto que escribe el formulario. textoResumen queda fuera de la vista
            // (el formulario lo sigue llenando) y es la fuente de las tarjetas y de la descripción accesible.
            textoResumen.TextChanged += (_, _) => MostrarResumenEnTarjetas();
            Disposed += (_, _) => textoResumen.Dispose();
            _aclaracionResumen.Text = "El material y la verificación solo filtran depósitos; la caneca, el usuario y las fechas filtran todo.";
            var resumen = ModuloEco.Seccion("Resumen de la consulta", icono: IconChar.ChartPie);
            resumen.Agregar(_tarjetasResumen);
            resumen.Agregar(new AvisoEco(_aclaracionResumen) { Margin = new Padding(0, 4, 0, 2) });
            ModuloEco.Fila(raiz, resumen);

            ConstruirResultados(raiz);
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(etiquetaEstado));

            // Desplazamiento propio del módulo. Antes el formulario pedía un alto mínimo y la ventana principal lo desplazaba,
            // pero Windows no deja que un formulario sea más alto que la pantalla (SystemInformation.MaxWindowTrackSize): con
            // escala 125 % el módulo medía 1 102 px de los 2 373 que necesitaba y lo de abajo se cortaba aunque la barra siguiera.
            // Ahora el formulario ocupa solo la vista y un panel interno desplaza la columna, que tiene el alto real del contenido.
            raiz.Dock = DockStyle.Top;
            _desplazable.Controls.Add(raiz);
            _desplazable.SizeChanged += (_, _) => PedirAltoMinimo();
            // Lo mismo que ModuloEco.Terminar, con el panel desplazable en lugar de la columna.
            MinimumSize = new Size(640, 420);
            Controls.Add(_desplazable);
            ResumeLayout(true);
            TemaEcoAccess.Aplicar(this);
            RuedaEco.Encauzar(this);
            // El alto sigue al contenido real (filtros en 2 o 3 filas, tarjetas en 1, 2 o 3 columnas): así la página se
            // desplaza entera y la tabla de resultados nunca queda aplastada ni recortada.
            foreach (Control fila in new Control[] { _banner, filtros, resumen, etiquetaEstado })
                fila.SizeChanged += (_, _) => PedirAltoMinimo();
            HandleCreated += (_, _) => PedirAltoMinimo();
            AjustarAltoMinimo();
        }

        private void ConstruirResultados(TableLayoutPanel raiz)
        {
            Button? exportar = panelPaginas.Controls.OfType<Button>().FirstOrDefault(b => b != botonAnterior && b != botonSiguiente);
            panelPaginas.Controls.Clear();
            DisenoGestion.BotonSuave(botonAnterior, IconChar.ChevronLeft).Dock = DockStyle.None;
            DisenoGestion.BotonSuave(botonSiguiente, IconChar.ChevronRight).Dock = DockStyle.None;
            var paginacion = ModuloEco.Botones(botonAnterior, botonSiguiente, etiquetaPagina);
            foreach (Button boton in new[] { botonAnterior, botonSiguiente }) boton.Margin = new Padding(0, 0, 10, 0);
            etiquetaPagina.AutoSize = true;
            etiquetaPagina.Margin = new Padding(6, 12, 14, 0);
            etiquetaPagina.ForeColor = TemaEcoAccess.Suave;
            if (exportar is not null)
            {
                ModuloEco.Boton(exportar, IconChar.FileCsv);
                exportar.Margin = new Padding(0, 0, 10, 0);
                paginacion.Controls.Add(exportar);
            }
            Disposed += (_, _) => panelPaginas.Dispose();

            // Página «Depósitos agrupados»: gráfico de los grupos (o de los días) arriba y la tabla debajo.
            var graficos = new Panel { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Superficie, Margin = new Padding(0, 2, 0, 12) };
            graficos.Controls.Add(_graficoGrupos);
            graficos.Controls.Add(_graficoDias);
            Pagina(pestanaAgrupado, tablaAgrupada, graficos, 230);
            Pagina(pestanaFallos, tablaFallos, null, 0);
            Pagina(pestanaDetalle, tablaDepositos, null, 0, paginacion);

            PulirTablas();

            var selector = new SelectorPestanas(pestanas, IconChar.LayerGroup, IconChar.TriangleExclamation, IconChar.ListUl) { Dock = DockStyle.Fill };
            _resultados = ModuloEco.Seccion("Resultados", llenar: true, icono: IconChar.Table);
            _resultados.MinimumSize = new Size(0, TemaEcoAccess.Px(this, 640));
            _resultados.Agregar(selector);
            _resultados.Agregar(pestanas, llenar: true);
            ModuloEco.Fila(raiz, _resultados, llenar: true);
        }

        /// <summary>Página blanca con un bloque opcional arriba (de alto fijo), la tabla al resto y una fila de acciones abajo.</summary>
        private static void Pagina(TabPage pagina, DataGridView tabla, Control? arriba, int altoArriba, params Control[] pie)
        {
            pagina.Controls.Clear();
            pagina.Padding = new Padding(0, 6, 0, 0);
            pagina.UseVisualStyleBackColor = false;
            pagina.BackColor = TemaEcoAccess.Superficie;
            var columna = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            columna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (arriba is not null)
            {
                columna.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(pagina, altoArriba)));
                columna.Controls.Add(arriba, 0, columna.RowCount++);
            }
            ModuloEco.Fila(columna, tabla, llenar: true);
            tabla.Margin = new Padding(0, 0, 0, pie.Length > 0 ? 12 : 2);
            foreach (Control c in pie) ModuloEco.Fila(columna, c);
            pagina.Controls.Add(columna);
        }

        private void PulirTablas()
        {
            DisenoGestion.PulirTabla(tablaAgrupada, columna => columna switch
            {
                "Grupo" => 220, "Depositos" => 110, "Verificados" => 120, "ConPesoMedido" => 150, "KgMedidos" => 120, "ConPesoEstimado" => 160, "KgEstimados" => 130, _ => 0,
            }, "Sin grupos para mostrar. Elige el período y pulsa «Consultar».", IconChar.LayerGroup);
            DisenoGestion.PulirTabla(tablaFallos, columna => columna switch
            {
                "Caneca" => 240, "EventosFallo" => 150, "EventosEnRevision" => 200, "OrdenesFallidas" => 160, "OrdenesVencidas" => 160, "Incidencias" => 140, _ => 0,
            }, "Sin incidencias en las canecas para el filtro consultado.", IconChar.CircleCheck);
            DisenoGestion.PulirTabla(tablaDepositos, columna => columna switch
            {
                "Id" => 110, "Detectado" => 190, "Caneca" => 220, "IdUsuario" => 110, "Declarado" => 190, "Verificado" => 200, "KgMedidos" => 115, "KgEstimados" => 125, _ => 0,
            }, "Sin depósitos para el filtro consultado.", IconChar.Database);
            tablaAgrupada.CellPainting += PintarCeldaAgrupada;
            tablaFallos.CellPainting += PintarCeldaFallos;
            tablaDepositos.CellPainting += PintarCeldaDeposito;
            tablaAgrupada.DataBindingComplete += (_, _) =>
            {
                // Encabezados que el formulario deja con el nombre de la propiedad (sin tilde).
                if (tablaAgrupada.Columns["Depositos"] is { HeaderText: "Depositos" } depositos) depositos.HeaderText = "Depósitos";
                MostrarGrafico();
            };
        }

        /// <summary>Convierte el texto del resumen en tarjetas y actualiza la línea del banner con el período consultado.</summary>
        private void MostrarResumenEnTarjetas()
        {
            LectorResumenEstadisticas.Lectura? lectura = LectorResumenEstadisticas.Leer(textoResumen.Text);
            _tarjetasResumen.Mostrar(lectura?.Indicadores, textoResumen.Text);
            if (lectura?.Aclaracion is { } aclaracion) _aclaracionResumen.Text = aclaracion;
            if (lectura is null)
            {
                _banner.Estado = string.IsNullOrWhiteSpace(textoResumen.Text) ? "Consulta un período para ver el resumen." : "Resumen de la última consulta.";
                return;
            }
            string periodo = $"Del {fechaDesde.Value.ToString("dd/MM/yyyy", CulturaDiseno)} al {fechaHasta.Value.ToString("dd/MM/yyyy", CulturaDiseno)}";
            _banner.Estado = $"{periodo} · {Contar(lectura.Depositos, "depósito", "depósitos")} · {lectura.Verificados.ToString("N0", CulturaDiseno)} verificados";
        }

        private static string Contar(int n, string uno, string varios) => $"{n.ToString("N0", CulturaDiseno)} {(n == 1 ? uno : varios)}";

        /// <summary>Gráfico de la página agrupada con las mismas filas de la tabla: barras por día o los grupos con más depósitos.</summary>
        private void MostrarGrafico()
        {
            var filas = (tablaAgrupada.DataSource as IEnumerable<FilaVistaAgrupada>)?.ToList() ?? new List<FilaVistaAgrupada>();
            // La agrupación por día trae fechas en la etiqueta: se ve mejor como serie en el tiempo.
            var dias = filas.Select(f => DateTime.TryParse(f.Grupo, CulturaDiseno, DateTimeStyles.None, out DateTime d)
                || DateTime.TryParse(f.Grupo, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d : (DateTime?)null).ToList();
            bool porDia = filas.Count > 0 && dias.All(d => d is not null);
            _graficoDias.Visible = porDia;
            _graficoGrupos.Visible = !porDia;
            if (porDia)
            {
                var orden = filas.Select((f, i) => (Fila: f, Dia: dias[i]!.Value)).OrderBy(x => x.Dia).ToList();
                _graficoDias.Mostrar(orden.Select(x => x.Dia.ToString("dd/MM", CulturaDiseno)).ToArray(), orden.Select(x => x.Fila.Depositos).ToArray(), orden.Select(x => x.Fila.Verificados).ToArray());
            }
            else
                _graficoGrupos.Mostrar(filas.Select(f => (f.Grupo, f.Depositos, f.Verificados)));
        }

        private void PintarCeldaAgrupada(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null) return;
            string columna = tablaAgrupada.Columns[e.ColumnIndex].Name;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            if (columna == "Grupo" && e.Value is string grupo)
                Texto(e, tablaAgrupada, grupo, DibujoDashboard.Semibold(tablaAgrupada, 13.5f), elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.VerdeOscuro);
            else if (columna == "Verificados" && e.Value is int verificados)
            {
                e.PaintBackground(e.CellBounds, elegida);
                Color tono = verificados > 0 ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
                DisenoGestion.Pastilla(e.Graphics, e.CellBounds, verificados.ToString("N0", CulturaDiseno), tono, verificados > 0 ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Fondo,
                    verificados > 0 ? TemaEcoAccess.VerdeHoja : TemaEcoAccess.Borde, IconChar.CircleCheck, verificados > 0 ? TemaEcoAccess.Verde : TemaEcoAccess.Suave, tablaAgrupada);
                e.Handled = true;
            }
            else if (columna is "KgMedidos" or "KgEstimados" && e.Value is string kg && kg == 0m.ToString("0.000", CulturaDiseno))
                Texto(e, tablaAgrupada, kg, e.CellStyle?.Font ?? tablaAgrupada.Font, TemaEcoAccess.Suave);
        }

        private void PintarCeldaFallos(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null) return;
            string columna = tablaFallos.Columns[e.ColumnIndex].Name;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            if (columna == "Caneca" && e.Value is string caneca)
            {
                e.PaintBackground(e.CellBounds, elegida);
                Bitmap icono = CacheIconos.Obtener(IconChar.TrashCan, TemaEcoAccess.Px(tablaFallos, 16), TemaEcoAccess.Verde);
                e.Graphics.DrawImage(icono, e.CellBounds.Left + TemaEcoAccess.Px(tablaFallos, 10), e.CellBounds.Top + (e.CellBounds.Height - icono.Height) / 2);
                var zona = Rectangle.FromLTRB(e.CellBounds.Left + TemaEcoAccess.Px(tablaFallos, 34), e.CellBounds.Top, e.CellBounds.Right - TemaEcoAccess.Px(tablaFallos, 4), e.CellBounds.Bottom);
                TextRenderer.DrawText(e.Graphics, caneca, DibujoDashboard.Semibold(tablaFallos, 13.5f), zona, elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Texto,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                e.Handled = true;
                return;
            }
            if (e.Value is not int cantidad) return;
            e.PaintBackground(e.CellBounds, elegida);
            string texto = cantidad.ToString("N0", CulturaDiseno);
            if (cantidad == 0)
                DisenoGestion.Pastilla(e.Graphics, e.CellBounds, texto, TemaEcoAccess.Suave, TemaEcoAccess.Fondo, TemaEcoAccess.Borde, IconChar.None, TemaEcoAccess.VerdeHoja, tablaFallos);
            else if (columna is "EventosEnRevision" or "OrdenesVencidas")
                DisenoGestion.Pastilla(e.Graphics, e.CellBounds, texto, TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave, TemaEcoAccess.Borde, IconChar.Clock, DibujoDashboard.Pendiente, tablaFallos);
            else
                DisenoGestion.Pastilla(e.Graphics, e.CellBounds, texto, TemaEcoAccess.Error, TemaEcoAccess.ErrorSuave, TemaEcoAccess.Borde, IconChar.TriangleExclamation, TemaEcoAccess.Error, tablaFallos);
            e.Handled = true;
        }

        private void PintarCeldaDeposito(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null) return;
            string columna = tablaDepositos.Columns[e.ColumnIndex].Name;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            if (columna == "Id" && e.Value is int id)
                Texto(e, tablaDepositos, "#" + id.ToString(CulturaDiseno), DibujoDashboard.Semibold(tablaDepositos, 13.5f), elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.VerdeOscuro);
            else if (columna == "Verificado" && e.Value is string verificado)
            {
                e.PaintBackground(e.CellBounds, elegida);
                if (verificado == "Sin verificar")
                    DisenoGestion.Pastilla(e.Graphics, e.CellBounds, verificado, TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave, TemaEcoAccess.Borde, IconChar.Clock, DibujoDashboard.Pendiente, tablaDepositos);
                else
                    DisenoGestion.Pastilla(e.Graphics, e.CellBounds, verificado, TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave, TemaEcoAccess.VerdeHoja, IconChar.CircleCheck, TemaEcoAccess.Verde, tablaDepositos);
                e.Handled = true;
            }
            else if (e.Value is "—" && !elegida)
                Texto(e, tablaDepositos, "—", e.CellStyle?.Font ?? tablaDepositos.Font, TemaEcoAccess.Suave);
        }

        private static void Texto(DataGridViewCellPaintingEventArgs e, DataGridView tabla, string texto, Font fuente, Color color)
        {
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            e.PaintBackground(e.CellBounds, elegida);
            var zona = Rectangle.FromLTRB(e.CellBounds.Left + TemaEcoAccess.Px(tabla, 10), e.CellBounds.Top, e.CellBounds.Right - TemaEcoAccess.Px(tabla, 4), e.CellBounds.Bottom);
            TextRenderer.DrawText(e.Graphics!, texto, fuente, zona, color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            e.Handled = true;
        }

        private void PedirAltoMinimo()
        {
            if (_altoPendiente || !IsHandleCreated || IsDisposed) return;
            _altoPendiente = true;
            BeginInvoke(new Action(() => { _altoPendiente = false; AjustarAltoMinimo(); }));
        }

        /// <summary>
        /// Alto de la columna = filas de alto automático (banner, filtros, resumen, estado) + el mínimo de Resultados, o el
        /// alto de la vista si sobra espacio (entonces Resultados crece y no hay barra). El panel interno desplaza la columna.
        /// </summary>
        private void AjustarAltoMinimo()
        {
            if (_raiz is null || _resultados is null || IsDisposed) return;
            int alto = _raiz.Padding.Vertical;
            foreach (Control fila in _raiz.Controls)
                alto += (fila == _resultados ? _resultados.MinimumSize.Height : fila.Height) + fila.Margin.Vertical;
            alto = Math.Max(alto, _desplazable.ClientSize.Height);
            if (Math.Abs(_raiz.Height - alto) > 1) _raiz.Height = alto;
        }
    }
}
