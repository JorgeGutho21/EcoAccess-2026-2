using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco del «Dashboard institucional» (skill eco-ui-supervisor): banner con los datos que se están viendo,
    /// filtros en rejilla, indicadores y fotos de Echo en secciones con icono, gráficos y tablas con pastillas. Solo
    /// presentación: reutiliza los mismos controles que llena la consulta; ningún dato se calcula aquí.
    /// </summary>
    public sealed partial class FormDashboardInstitucional
    {
        private void ConstruirDiseno(Action? abrirPowerBi)
        {
            var banner = new BannerModulo("Dashboard institucional",
                "Actividad del sistema por período: solicitudes, depósitos, verificaciones y ecopuntos, con comparación frente al período anterior.",
                IconChar.Database) { Estado = "Consultando los datos del período…" };
            _alcanceTexto.TextChanged += (_, _) => banner.Estado = _alcanceTexto.Text;
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.ChartPie, "Dashboard institucional",
                "Actividad del sistema por período: solicitudes, depósitos, verificaciones y ecopuntos, con comparación frente al período anterior.", banner);

            // Filtros: 8 campos en rejilla (4 columnas en pantallas anchas, 2 en medianas, 1 en angostas) y las acciones debajo.
            Control[] campos =
            {
                ModuloEco.Campo("Desde", IconChar.CalendarDays, _desde), ModuloEco.Campo("Hasta", IconChar.CalendarDays, _hasta),
                ModuloEco.Campo("Caneca", IconChar.TrashCan, _caneca), ModuloEco.Campo("Ubicación", IconChar.LocationDot, _ubicacion),
                ModuloEco.Campo("Material", IconChar.Leaf, _material), ModuloEco.Campo("Categoría", IconChar.LayerGroup, _categoria),
                ModuloEco.Campo("Agrupar por", IconChar.CalendarWeek, _periodo), ModuloEco.Campo("Datos", IconChar.Database, _alcance),
            };
            foreach (Control campo in campos) campo.Dock = DockStyle.Fill;
            var rejilla = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            int columnas = 0;
            void Repartir()
            {
                int ancho = rejilla.Parent?.ClientSize.Width ?? 0;
                if (ancho <= 0) return;
                int n = ancho >= TemaEcoAccess.Px(rejilla, 960) ? 4 : ancho >= TemaEcoAccess.Px(rejilla, 480) ? 2 : 1;
                if (n == columnas) return;
                columnas = n;
                rejilla.SuspendLayout();
                rejilla.Controls.Clear();
                rejilla.ColumnStyles.Clear();
                rejilla.RowStyles.Clear();
                rejilla.ColumnCount = n;
                rejilla.RowCount = (campos.Length + n - 1) / n;
                for (int k = 0; k < n; k++) rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / n));
                for (int k = 0; k < rejilla.RowCount; k++) rejilla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                for (int k = 0; k < campos.Length; k++) rejilla.Controls.Add(campos[k], k % n, k / n);
                rejilla.ResumeLayout(true);
            }

            TemaEcoAccess.EstilizarAccion(_consultar, principal: true);
            TemaEcoAccess.EstilizarAccion(_limpiar);
            TemaEcoAccess.EstilizarAccion(_powerBi);
            _consultar.Values.Image = CacheIconos.Obtener(IconChar.ArrowsRotate, TemaEcoAccess.Px(this, 16), Color.White);
            _limpiar.Values.Image = CacheIconos.Obtener(IconChar.Eraser, TemaEcoAccess.Px(this, 16), TemaEcoAccess.VerdeOscuro);
            _powerBi.Values.Image = CacheIconos.Obtener(IconChar.ChartSimple, TemaEcoAccess.Px(this, 16), TemaEcoAccess.VerdeOscuro);
            // Tamaño fijo (texto + icono + márgenes, alto 46): con ajuste automático Krypton estiraba cada botón a otro ancho
            // y la fila de acciones quedaba apilada.
            foreach (KryptonButton boton in new[] { _consultar, _limpiar, _powerBi })
            {
                boton.AutoSize = false;
                boton.MinimumSize = Size.Empty;
                boton.Size = new Size(TextRenderer.MeasureText(boton.Text, TemaEcoAccess.Subtitulo).Width + TemaEcoAccess.Px(this, 76), TemaEcoAccess.Px(this, ModuloEco.AltoCampo));
            }
            _graficos.AltoFila = 500;
            var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 2, 0, 12), BackColor = TemaEcoAccess.Superficie };
            foreach (KryptonButton boton in new[] { _consultar, _limpiar }) { boton.Margin = new Padding(0, 0, 10, 0); acciones.Controls.Add(boton); }
            if (abrirPowerBi is not null)
            {
                _powerBi.Margin = new Padding(0, 0, 10, 0);
                _powerBi.Click += (_, _) => abrirPowerBi();
                acciones.Controls.Add(_powerBi);
            }
            _alcanceTexto.Text = "Consultando los datos del período…";
            var filtros = ModuloEco.Seccion("Filtros", "«Datos» separa la operación real de la demostración y las simulaciones.", icono: IconChar.Filter);
            filtros.Agregar(rejilla);
            filtros.Agregar(acciones);
            filtros.Agregar(new AvisoEco(_alcanceTexto, "Qué datos estás viendo", IconChar.Database) { Margin = new Padding(0, 0, 0, 2) });
            filtros.Cuerpo.SizeChanged += (_, _) => Repartir();
            ModuloEco.Fila(raiz, filtros);

            // Indicadores y fotos de Echo: las tarjetas de siempre, dentro de secciones con título e icono.
            foreach (TarjetaDato dato in Tarjetas()) _indicadores.Controls.Add(dato);
            foreach (TarjetaDato dato in new[] { _visionTotal, _visionDestino, _visionOtraFoto, _visionLatencia }) _visionIndicadores.Controls.Add(dato);
            var indicadores = Seccion("Indicadores del período", null, IconChar.ChartPie, _indicadores);
            ModuloEco.Fila(raiz, indicadores);
            ModuloEco.Fila(raiz, _graficos);
            _graficos.Margin = new Padding(0, 0, 0, 14);
            var vision = Seccion("Fotos consultadas a Echo", "Consultas de reconocimiento por foto: no son depósitos ni suman ecopuntos.", IconChar.Camera, _visionIndicadores);
            ModuloEco.Fila(raiz, vision);

            ModuloEco.Fila(raiz, SeccionTabla("Comparación con el período anterior", "Mismo número de días justo antes del período consultado.", IconChar.ArrowTrendUp, _comparacion, 430));
            ModuloEco.Fila(raiz, SeccionTabla("Actividad por caneca y ubicación", null, IconChar.LocationDot, _grupos, 290));
            ModuloEco.Fila(raiz, SeccionTabla("Estado de las canecas registradas", "La conectividad y el dispositivo no se miden desde este panel.", IconChar.TrashCan, _estadoCanecas, 240));
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(_estado));

            ModuloEco.TerminarDesplazable(this, raiz, 720);
            PulirTablasInstitucional();
        }

        /// <summary>Sección blanca con una rejilla de tarjetas que se reparte al ancho de la sección.</summary>
        private static SeccionEco Seccion(string titulo, string? descripcion, IconChar icono, RejillaTarjetas rejilla)
        {
            var seccion = ModuloEco.Seccion(titulo, descripcion, icono: icono);
            rejilla.BackColor = TemaEcoAccess.Superficie;
            rejilla.Margin = Padding.Empty;
            seccion.Agregar(rejilla);
            seccion.Cuerpo.SizeChanged += (_, _) => { if (seccion.Cuerpo.ClientSize.Width > 0) rejilla.Adaptar(seccion.Cuerpo.ClientSize.Width); };
            return seccion;
        }

        /// <summary>Tabla de alto fijo en su sección: la tabla tiene su barra y el módulo la suya, sin recortes.</summary>
        private static SeccionEco SeccionTabla(string titulo, string? descripcion, IconChar icono, DataGridView tabla, int alto)
        {
            var seccion = ModuloEco.Seccion(titulo, descripcion, icono: icono);
            var marco = new Panel { Height = TemaEcoAccess.Px(seccion, alto), BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            tabla.Dock = DockStyle.Fill;
            tabla.Margin = Padding.Empty;
            marco.Controls.Add(tabla);
            seccion.Agregar(marco);
            return seccion;
        }

        private void PulirTablasInstitucional()
        {
            DisenoGestion.PulirTabla(_comparacion, c => c switch { "Indicador" => 240, "Actual" => 110, "Anterior" => 110, "Diferencia" => 150, "Variación" => 330, _ => 0 },
                "Consulta un período para comparar con el anterior.", IconChar.ArrowTrendUp);
            DisenoGestion.PulirTabla(_grupos, c => c switch { "Dimensión" => 150, "Grupo" => 240, "Depósitos" => 110, "Verificados" => 120, _ => 0 },
                "Sin depósitos por caneca ni ubicación en el período.", IconChar.LocationDot);
            DisenoGestion.PulirTabla(_estadoCanecas, c => c switch { "Caneca" => 220, "Configuración" => 220, "Conectividad" => 260, "Dispositivo" => 220, _ => 0 },
                "Sin canecas registradas para mostrar.", IconChar.TrashCan);
            _comparacion.CellPainting += PintarCeldaInstitucional;
            _grupos.CellPainting += PintarCeldaInstitucional;
            _estadoCanecas.CellPainting += PintarCeldaInstitucional;
        }

        private void PintarCeldaInstitucional(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (sender is not DataGridView tabla || e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is null) return;
            string columna = tabla.Columns[e.ColumnIndex].Name;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            Graphics g = e.Graphics;
            switch (columna)
            {
                case "Indicador" or "Grupo" or "Caneca" when e.Value is string texto:
                    e.PaintBackground(e.CellBounds, elegida);
                    TextRenderer.DrawText(g, texto, DibujoDashboard.Semibold(tabla, 13.5f),
                        Rectangle.FromLTRB(e.CellBounds.Left + TemaEcoAccess.Px(tabla, 10), e.CellBounds.Top, e.CellBounds.Right - TemaEcoAccess.Px(tabla, 4), e.CellBounds.Bottom),
                        elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Texto, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    break;
                case "Diferencia" when e.Value is long diferencia:
                    e.PaintBackground(e.CellBounds, elegida);
                    // Sin juicio de bueno o malo (subir los reversos no es una mejora): solo dirección del cambio.
                    if (diferencia == 0)
                        DisenoGestion.Pastilla(g, e.CellBounds, "Sin cambio", TemaEcoAccess.Suave, TemaEcoAccess.Fondo, TemaEcoAccess.Borde, IconChar.Minus, TemaEcoAccess.Suave, tabla);
                    else
                    {
                        bool sube = diferencia > 0;
                        string texto = (sube ? "+" : "−") + Math.Abs(diferencia).ToString("N0", Colombia);
                        DisenoGestion.Pastilla(g, e.CellBounds, texto, sube ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Info, sube ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.InfoSuave,
                            TemaEcoAccess.Borde, sube ? IconChar.ArrowTrendUp : IconChar.ArrowTrendDown, sube ? TemaEcoAccess.Verde : TemaEcoAccess.Info, tabla);
                    }
                    break;
                case "Dimensión" when e.Value is string dimension:
                    e.PaintBackground(e.CellBounds, elegida);
                    bool caneca = dimension == "Caneca";
                    DisenoGestion.Pastilla(g, e.CellBounds, dimension, TemaEcoAccess.VerdeOscuro, caneca ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.InfoSuave, TemaEcoAccess.Borde,
                        caneca ? IconChar.TrashCan : IconChar.LocationDot, caneca ? TemaEcoAccess.Verde : TemaEcoAccess.Info, tabla);
                    break;
                case "Configuración" when e.Value is string configuracion:
                    e.PaintBackground(e.CellBounds, elegida);
                    DisenoGestion.PastillaEstado(g, e.CellBounds, configuracion, configuracion.StartsWith("Habilitada", StringComparison.Ordinal), tabla);
                    break;
                case "Conectividad" or "Dispositivo" when e.Value is string nota:
                    e.PaintBackground(e.CellBounds, elegida);
                    TextRenderer.DrawText(g, nota, e.CellStyle?.Font ?? tabla.Font,
                        Rectangle.FromLTRB(e.CellBounds.Left + TemaEcoAccess.Px(tabla, 10), e.CellBounds.Top, e.CellBounds.Right - TemaEcoAccess.Px(tabla, 4), e.CellBounds.Bottom),
                        elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Suave, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    break;
                default:
                    return;
            }
            e.Handled = true;
        }
    }
}
