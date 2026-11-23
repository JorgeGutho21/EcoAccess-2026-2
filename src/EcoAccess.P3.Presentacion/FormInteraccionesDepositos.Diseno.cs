using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Depósitos e interacciones» (skill eco-ui-supervisor): encabezado, tarjeta de consulta con los filtros
    /// como campos con icono, y tarjeta de resultados con las pestañas. Reutiliza los controles del Designer; los filtros siguen
    /// dentro de <c>panelFiltros</c> y el botón de cancelar queda fuera, porque se usa mientras los filtros están deshabilitados.
    /// </summary>
    public partial class FormInteraccionesDepositos
    {
        private readonly TabControl _paginasResultados = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Resultados de la auditoría" };

        private void ConstruirDiseno()
        {
            var banner = new BannerModulo("Auditar depósitos",
                "Consulta los depósitos por fecha y caneca, verifica el material observado y revisa las solicitudes y eventos de cada caneca.",
                IconChar.ShieldHalved) { Estado = etiquetaPagina.Text };
            etiquetaPagina.TextChanged += (_, _) => banner.Estado = etiquetaPagina.Text;
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.BoxOpen, "Depósitos e interacciones",
                "Consulta los depósitos por fecha y caneca, registra la verificación del material observado y revisa las solicitudes y eventos de cada caneca.", banner);

            panelFiltros.Controls.Clear();
            panelFiltros.AutoSize = true;
            panelFiltros.WrapContents = true;
            panelFiltros.BackColor = TemaEcoAccess.Superficie;
            panelFiltros.Padding = Padding.Empty;
            panelFiltros.Controls.AddRange(new Control[]
            {
                ModuloEco.Campo(etiquetaDesde, IconChar.CalendarDays, fechaDesde, ancho: 200),
                ModuloEco.Campo(etiquetaHasta, IconChar.CalendarDays, fechaHasta, ancho: 200),
                ModuloEco.Campo(etiquetaFiltroCaneca, IconChar.Trash, comboCaneca, ancho: 260),
                ModuloEco.Campo(etiquetaFiltroVerificacion, IconChar.ClipboardCheck, comboVerificacion, ancho: 220),
                ModuloEco.JuntoACampos(ModuloEco.Boton(botonConsultar, IconChar.MagnifyingGlass)),
            });
            var consulta = ModuloEco.Seccion("Consultar depósitos", "La fecha es la de Colombia. La consulta solo lee; verificar un depósito es la única acción que escribe.",
                icono: IconChar.MagnifyingGlass);
            consulta.Agregar(panelFiltros);
            consulta.Agregar(ModuloEco.Mensaje(etiquetaConsulta));
            consulta.Agregar(ModuloEco.Mensaje(etiquetaCaneca, pequeno: true));
            consulta.Agregar(ModuloEco.Mensaje(etiquetaUltimaConsulta, pequeno: true));
            consulta.Agregar(ModuloEco.Botones(ModuloEco.Boton(botonCancelar, IconChar.Xmark)));
            ModuloEco.Fila(raiz, consulta);

            // Pestaña de depósitos: tabla, paginación y, debajo, la verificación del material observado.
            var paginacion = ModuloEco.Botones(ModuloEco.Boton(botonAnterior, IconChar.ChevronLeft), ModuloEco.Boton(botonSiguiente, IconChar.ChevronRight), etiquetaPagina);
            etiquetaPagina.Margin = new Padding(6, 12, 0, 0);
            etiquetaPagina.ForeColor = TemaEcoAccess.Suave;
            panelDepositos.Controls.Clear();
            // La verificación contiene aviso y campos en varias filas. Un alto automático dentro de la fila que llena la tabla
            // se recortaba sin que la página pudiera desplazarse; se reserva el área y, si se estrecha, esta se desplaza sola.
            panelDepositos.AutoSize = false;
            panelDepositos.AutoScroll = true;
            panelDepositos.WrapContents = true;
            panelDepositos.BackColor = TemaEcoAccess.Superficie;
            panelDepositos.MinimumSize = Size.Empty;
            _conteo.Font = TemaEcoAccess.Subtitulo;
            _conteo.Margin = new Padding(0, TemaEcoAccess.Subtitulo.Height + 18, 14, 14);
            panelDepositos.Controls.AddRange(new Control[]
            {
                new AvisoEco(DisenoFormularios.Texto("La verificación queda registrada y no acredita ecopuntos automáticamente."), "Verificación independiente"),
                ModuloEco.Campo(etiquetaMaterial, IconChar.Leaf, comboMaterial, ancho: 260),
                _conteo,
                ModuloEco.Campo("Unidades contadas", IconChar.Hashtag, _cantidadVerificada, ancho: 170),
                ModuloEco.JuntoACampos(ModuloEco.Boton(botonVerificar, IconChar.CircleCheck)),
            });
            PrepararPaginaDepositos(paginacion);
            ModuloEco.PaginaConTabla(paginaInteracciones, tablaInteracciones, ModuloEco.Mensaje(etiquetaInteracciones));
            ModuloEco.PaginaConTabla(paginaIncidencias, tablaIncidencias, ModuloEco.Mensaje(etiquetaIncidencias));
            while (pestanas.TabPages.Count > 0)
            {
                TabPage pagina = pestanas.TabPages[0];
                pestanas.TabPages.Remove(pagina);
                _paginasResultados.TabPages.Add(pagina);
            }
            pestanas.EnabledChanged += (_, _) => _paginasResultados.Enabled = pestanas.Enabled;

            var resultados = ModuloEco.Seccion("Resultados de auditoría", "Revisa depósitos, solicitudes y eventos sin la franja clásica de Windows.",
                llenar: true, icono: IconChar.ShieldHalved);
            resultados.MinimumSize = new Size(0, 460);
            resultados.Agregar(new SelectorPestanas(_paginasResultados, IconChar.BoxOpen, IconChar.ArrowsRotate, IconChar.TriangleExclamation));
            resultados.Agregar(_paginasResultados, llenar: true);
            ModuloEco.Fila(raiz, resultados, llenar: true);
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(etiquetaEstado));

            PulirTablasAuditoria();
            Disposed += (_, _) => { panelSuperior.Dispose(); panelInferior.Dispose(); };

            // Desplazamiento propio del módulo (como en Estadísticas). Antes el formulario pedía 960 de alto mínimo y la ventana
            // principal lo desplazaba, pero Windows no deja que un formulario sea más alto que la pantalla
            // (SystemInformation.MaxWindowTrackSize): con escala 125 % el final se cortaba, la tabla quedaba aplastada con su
            // propia barra y el mensaje de estado se montaba sobre ella. Ahora un panel interno desplaza la columna, que tiene
            // el alto real del contenido, y Resultados reserva el alto completo de la pestaña de depósitos (sin barra interna).
            _raizAuditoria = raiz;
            _resultadosAuditoria = resultados;
            raiz.Dock = DockStyle.Top;
            _desplazableAuditoria.Controls.Add(raiz);
            MinimumSize = new Size(640, 420);
            Controls.Add(_desplazableAuditoria);
            ResumeLayout(true);
            TemaEcoAccess.Aplicar(this);
            RuedaEco.Encauzar(this);
            foreach (Control fila in new Control[] { banner, consulta, etiquetaEstado, _contenidoDepositos!, _desplazableAuditoria })
                fila.SizeChanged += (_, _) => PedirAltoAuditoria();
            HandleCreated += (_, _) => PedirAltoAuditoria();
            AjustarAltoAuditoria();
        }

        private readonly PanelDesplazable _desplazableAuditoria = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo, AccessibleName = "Auditar depósitos" };
        private TableLayoutPanel? _raizAuditoria, _contenidoDepositos;
        private SeccionEco? _resultadosAuditoria;
        private bool _altoAuditoriaPendiente;

        private void PedirAltoAuditoria()
        {
            if (_altoAuditoriaPendiente || !IsHandleCreated || IsDisposed) return;
            _altoAuditoriaPendiente = true;
            BeginInvoke(new System.Action(() => { _altoAuditoriaPendiente = false; AjustarAltoAuditoria(); }));
        }

        /// <summary>
        /// Resultados = su marco (título, selector, márgenes) + el alto completo de la pestaña de depósitos (tabla, paginación y
        /// verificación); la columna suma las filas automáticas y ese mínimo, o el alto de la vista si sobra espacio.
        /// </summary>
        private void AjustarAltoAuditoria()
        {
            if (_raizAuditoria is null || _resultadosAuditoria is null || _contenidoDepositos is null || IsDisposed) return;
            int marco = _resultadosAuditoria.Height - _paginasResultados.Height;
            int pagina = _contenidoDepositos.Height + paginaDepositos.Padding.Vertical + TemaEcoAccess.Px(this, 4);
            int minimo = System.Math.Max(TemaEcoAccess.Px(this, 460), marco + pagina);
            if (System.Math.Abs(_resultadosAuditoria.MinimumSize.Height - minimo) > 1) _resultadosAuditoria.MinimumSize = new Size(0, minimo);
            int alto = _raizAuditoria.Padding.Vertical;
            foreach (Control fila in _raizAuditoria.Controls)
                alto += (fila == _resultadosAuditoria ? minimo : fila.Height) + fila.Margin.Vertical;
            alto = System.Math.Max(alto, _desplazableAuditoria.ClientSize.Height);
            if (System.Math.Abs(_raizAuditoria.Height - alto) > 1) _raizAuditoria.Height = alto;
        }

        /// <summary>
        /// Conserva tabla, paginación y verificación en una única superficie desplazable. El pie no depende de la
        /// altura disponible de la ventana: al faltar espacio, la propia pestaña muestra su barra vertical.
        /// </summary>
        private void PrepararPaginaDepositos(Control paginacion)
        {
            paginaDepositos.Controls.Clear();
            paginaDepositos.Padding = new Padding(12, 14, 12, 8);
            paginaDepositos.UseVisualStyleBackColor = false;
            paginaDepositos.BackColor = TemaEcoAccess.Superficie;
            paginaDepositos.AutoScroll = true;

            var contenido = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = TemaEcoAccess.Superficie,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                AccessibleName = "Tabla y verificación del depósito",
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // La tabla conserva una altura útil. El resto del contenido puede extenderse y lo recorre la pestaña.
            tablaDepositos.Dock = DockStyle.Top;
            tablaDepositos.Height = TemaEcoAccess.Px(this, 260);
            tablaDepositos.Margin = Padding.Empty;
            paginacion.Dock = DockStyle.Top;
            paginacion.Margin = new Padding(0, 0, 0, 8);
            panelDepositos.AutoSize = true;
            panelDepositos.AutoScroll = false;
            panelDepositos.Dock = DockStyle.Top;
            panelDepositos.Margin = Padding.Empty;

            contenido.Controls.Add(tablaDepositos, 0, 0);
            contenido.Controls.Add(paginacion, 0, 1);
            contenido.Controls.Add(panelDepositos, 0, 2);
            paginaDepositos.Controls.Add(contenido);
            _contenidoDepositos = contenido;

            // No dependemos de que WinForms infiera el área virtual de un control acoplado arriba: se declara de forma
            // explícita. Esto mantiene la rueda y la barra vertical activas hasta el último campo de verificación.
            void ActualizarAreaDesplazable()
            {
                int ancho = System.Math.Max(1, paginaDepositos.ClientSize.Width - paginaDepositos.Padding.Horizontal);
                int alto = System.Math.Max(contenido.Height, contenido.GetPreferredSize(new Size(ancho, 0)).Height);
                Size requerida = new(0, alto + paginaDepositos.Padding.Vertical);
                if (paginaDepositos.AutoScrollMinSize != requerida) paginaDepositos.AutoScrollMinSize = requerida;
            }
            contenido.SizeChanged += (_, _) => ActualizarAreaDesplazable();
            panelDepositos.SizeChanged += (_, _) => ActualizarAreaDesplazable();
            paginaDepositos.ClientSizeChanged += (_, _) => ActualizarAreaDesplazable();
            paginaDepositos.HandleCreated += (_, _) => paginaDepositos.BeginInvoke((System.Action)ActualizarAreaDesplazable);
            ActualizarAreaDesplazable();
        }

        /// <summary>Tablas con filas amplias, encabezado verde y estados vacíos; no cambian sus consultas ni columnas.</summary>
        private void PulirTablasAuditoria()
        {
            DisenoGestion.PulirTabla(tablaDepositos, columna => columna switch
            {
                "Id" => 105, "Fecha" => 175, "Caneca" => 200, "Usuario" => 115, "Declarado" => 210,
                "Verificado" => 210, "KgMedidos" => 135, "KgEstimados" => 170, _ => 0,
            }, "Consulta los filtros para ver depósitos y su verificación.", IconChar.BoxOpen);
            DisenoGestion.PulirTabla(tablaInteracciones, columna => columna switch
            {
                "Id" => 115, "Inicio" => 175, "Usuario" => 115, "Canal" => 135, "Estado" => 150,
                "Material" => 185, "Cierre" => 175, _ => 0,
            }, "Selecciona una caneca y consulta para ver sus solicitudes.", IconChar.ArrowsRotate);
            DisenoGestion.PulirTabla(tablaIncidencias, columna => columna switch
            {
                "Id" => 130, "Caneca" => 115, "Orden" => 115, "Tipo" => 185, "Recibido" => 180,
                "Dispositivo" => 190, "Externo" => 240, _ => 0,
            }, "Consulta el período para ver los eventos en revisión.", IconChar.TriangleExclamation);
            tablaDepositos.CellPainting += PintarCeldaDeposito;
        }

        private void PintarCeldaDeposito(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is not string valor) return;
            string columna = tablaDepositos.Columns[e.ColumnIndex].Name;
            if (columna is not ("Id" or "Verificado")) return;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            e.PaintBackground(e.CellBounds, elegida);
            if (columna == "Verificado")
            {
                bool verificado = valor != "Sin verificar";
                DisenoGestion.Pastilla(e.Graphics, e.CellBounds, valor,
                    verificado ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Advertencia,
                    verificado ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.AdvertenciaSuave,
                    TemaEcoAccess.Borde, verificado ? IconChar.Check : IconChar.None,
                    verificado ? TemaEcoAccess.Verde : TemaEcoAccess.Advertencia, tablaDepositos);
            }
            else
            {
                int px(int n) => TemaEcoAccess.Px(tablaDepositos, n);
                var zona = Rectangle.FromLTRB(e.CellBounds.Left + px(10), e.CellBounds.Top, e.CellBounds.Right - px(6), e.CellBounds.Bottom);
                TextRenderer.DrawText(e.Graphics, valor, DibujoDashboard.Semibold(tablaDepositos, 14f), zona,
                    elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.VerdeOscuro,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            e.Handled = true;
        }
    }
}
