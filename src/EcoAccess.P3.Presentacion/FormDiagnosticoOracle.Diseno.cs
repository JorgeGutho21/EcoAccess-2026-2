using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Sistema, Oracle y Telegram» (skill eco-ui-supervisor): banner con el estado de Oracle, fichas del
    /// esquema (tiempo de consulta, tablas e incidencias), tabla de migraciones con pastillas y los servicios de la
    /// demostración como tarjetas con su estado. Solo presentación: las fichas y tarjetas se leen de los mismos textos que
    /// escribe el formulario; si su formato no se reconoce, se muestra el texto tal cual.
    /// </summary>
    public sealed partial class FormDiagnosticoOracle
    {
        private readonly RejillaIndicadores _fichasOracle = new();
        private readonly Label _notaOracle = new() { AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 2, 0, 12), UseMnemonic = false };
        private readonly ListaServicios _listaServicios = new();
        private readonly Label _ultimaOperacion = new() { UseMnemonic = false };
        private readonly Label _errores = new() { UseMnemonic = false };
        private AvisoEco? _avisoErrores;

        private void ConstruirDiseno()
        {
            const string Descripcion = "Estado de la base de datos y de los servicios de la demostración en este equipo. Consulta de solo lectura: no modifica registros ni aplica migraciones.";
            var banner = new BannerModulo("Sistema, Oracle y Telegram", Descripcion, IconChar.Server) { Estado = "Verificando Oracle…" };
            _estado.TextChanged += (_, _) => banner.Estado = _estado.Text;
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Server, "Sistema, Oracle y Telegram", Descripcion, banner);

            var oracle = ModuloEco.Seccion("Base de datos Oracle", "Verifica la conexión y el estado estructural de las migraciones.", icono: IconChar.Database);
            oracle.Agregar(ModuloEco.Botones(ModuloEco.Boton(_consultar, IconChar.Plug), ModuloEco.Boton(_cancelar, IconChar.Xmark)));
            oracle.Agregar(ModuloEco.Mensaje(_estado));
            _estado.Font = TemaEcoAccess.Subtitulo;
            oracle.Agregar(_fichasOracle);
            oracle.Agregar(_notaOracle);
            oracle.Agregar(ModuloEco.Mensaje(_detalle));
            var marcoMigraciones = new Panel { Height = TemaEcoAccess.Px(this, 260), BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            _migraciones.Margin = Padding.Empty;
            marcoMigraciones.Controls.Add(_migraciones);
            oracle.Agregar(marcoMigraciones);
            ModuloEco.Fila(raiz, oracle);

            var servicios = ModuloEco.Seccion("Servicios de la demostración", "Procesos iniciados en este equipo. Un proceso activo no acredita que el sensor físico responda.", icono: IconChar.PaperPlane);
            servicios.Agregar(_listaServicios);
            servicios.Agregar(new AvisoEco(_ultimaOperacion, "Última operación del bridge", IconChar.ClockRotateLeft) { Margin = new Padding(0, 6, 0, 10) });
            _avisoErrores = new AvisoEco(_errores, "Errores recientes", IconChar.TriangleExclamation) { Margin = new Padding(0, 0, 0, 2) };
            servicios.Agregar(_avisoErrores);
            servicios.Agregar(ModuloEco.Mensaje(_servicios));
            ModuloEco.Fila(raiz, servicios);

            ModuloEco.TerminarDesplazable(this, raiz, 640);
            _servicios.MaximumSize = _estado.MaximumSize = _detalle.MaximumSize = Size.Empty;
            ModuloEco.AjustarTextos(raiz);

            DisenoGestion.PulirTabla(_migraciones, c => c switch { "Versión" => 140, "Estado" => 220, "Faltantes" => 320, _ => 0 },
                "Verifica la conexión para ver el estado de las migraciones.", IconChar.Database);
            _migraciones.CellPainting += PintarCeldaMigracion;
            _detalle.TextChanged += (_, _) => MostrarFichasOracle();
            _servicios.TextChanged += (_, _) => MostrarServiciosEnTarjetas();
            MostrarFichasOracle();
            MostrarServiciosEnTarjetas();
        }

        /// <summary>Fichas del esquema leídas del detalle que escribe el formulario (o «—» si no hay resultado verificado).</summary>
        private void MostrarFichasOracle()
        {
            string texto = _detalle.Text;
            Match esquema = Regex.Match(texto, @"Esquema:\s*(.+?)\s·\sPDB:\s*(.+?)\s·\sConsulta:\s*([\d.,]+)\s*ms");
            Match tablas = Regex.Match(texto, @"Tablas:\s*(\d+)");
            Match restricciones = Regex.Match(texto, @"no validadas:\s*(\d+)");
            Match objetos = Regex.Match(texto, @"Objetos inválidos:\s*(\d+)");
            Match indices = Regex.Match(texto, @"Índices inválidos:\s*(\d+)");
            bool leido = esquema.Success && tablas.Success && restricciones.Success && objetos.Success && indices.Success;
            if (!leido)
            {
                _fichasOracle.Mostrar(new[]
                {
                    new Indicador("Esquema", IconChar.Database, "—", "Sin resultado verificado."),
                    new Indicador("Tiempo de consulta", IconChar.Stopwatch, "—", "Milisegundos de la consulta."),
                    new Indicador("Tablas", IconChar.Table, "—", "Encontradas en el esquema."),
                    new Indicador("Incidencias estructurales", IconChar.ShieldHalved, "—", "Restricciones, objetos e índices."),
                });
                _notaOracle.Visible = false;
                _detalle.Visible = texto.Length > 0;
                return;
            }
            int incidencias = int.Parse(restricciones.Groups[1].Value) + int.Parse(objetos.Groups[1].Value) + int.Parse(indices.Groups[1].Value);
            _fichasOracle.Mostrar(new[]
            {
                new Indicador("Esquema", IconChar.Database, esquema.Groups[1].Value, "PDB: " + esquema.Groups[2].Value, Pequeno: true),
                new Indicador("Tiempo de consulta", IconChar.Stopwatch, esquema.Groups[3].Value + " ms", "Una consulta de lectura."),
                new Indicador("Tablas", IconChar.Table, tablas.Groups[1].Value, "Encontradas en el esquema."),
                new Indicador("Incidencias estructurales", incidencias == 0 ? IconChar.CircleCheck : IconChar.TriangleExclamation, incidencias.ToString(),
                    $"Restricciones {restricciones.Groups[1].Value} · objetos {objetos.Groups[1].Value} · índices {indices.Groups[1].Value}", Tono: incidencias == 0 ? null : TemaEcoAccess.Error),
            });
            // Lo que no va en las fichas (hora de la verificación y la advertencia) queda como nota.
            _notaOracle.Text = string.Join("  ", texto.Split('\n').Skip(2).Select(l => l.Trim()).Where(l => l.Length > 0));
            _notaOracle.Visible = _notaOracle.Text.Length > 0;
            _detalle.Visible = false;
        }

        /// <summary>Tarjetas de los servicios leídas del texto que escribe el formulario (● activo / ○ no disponible).</summary>
        private void MostrarServiciosEnTarjetas()
        {
            string[] lineas = _servicios.Text.Replace("\r", "").Split('\n');
            var lista = new List<(string Nombre, bool Activo, string Detalle)>();
            foreach (string linea in lineas)
            {
                Match m = Regex.Match(linea, @"^\s*([●○])\s+(.*?):\s(activo|no disponible)\s·\s(.*)$");
                if (m.Success) lista.Add((m.Groups[2].Value, m.Groups[1].Value == "●", m.Groups[4].Value.Trim()));
            }
            string? ultima = lineas.FirstOrDefault(l => l.StartsWith("Última operación del bridge:", StringComparison.Ordinal));
            int inicioErrores = Array.FindIndex(lineas, l => l.StartsWith("Errores recientes", StringComparison.Ordinal));
            bool leido = lista.Count > 0 && ultima is not null && inicioErrores >= 0;
            _listaServicios.Visible = leido;
            _ultimaOperacion.Parent!.Visible = leido;
            _servicios.Visible = !leido && _servicios.Text.Length > 0;
            if (!leido) { _avisoErrores!.Visible = false; return; }
            _listaServicios.Mostrar(lista);
            _ultimaOperacion.Text = ultima!.Substring("Última operación del bridge:".Length).Trim();
            string[] errores = lineas.Skip(inicioErrores + 1).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            _avisoErrores!.Visible = errores.Length > 0;
            _errores.Text = string.Join(Environment.NewLine, errores);
        }

        private void PintarCeldaMigracion(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is not string valor) return;
            string columna = _migraciones.Columns[e.ColumnIndex].Name;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            if (columna == "Estado")
            {
                e.PaintBackground(e.CellBounds, elegida);
                bool presente = valor == "Estructura presente";
                DisenoGestion.Pastilla(e.Graphics, e.CellBounds, valor, presente ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Error, presente ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.ErrorSuave,
                    TemaEcoAccess.Borde, presente ? IconChar.CircleCheck : IconChar.TriangleExclamation, presente ? TemaEcoAccess.Verde : TemaEcoAccess.Error, _migraciones);
            }
            else if (columna == "Versión" || (columna == "Faltantes" && valor.Length == 0))
            {
                e.PaintBackground(e.CellBounds, elegida);
                bool version = columna == "Versión";
                TextRenderer.DrawText(e.Graphics, version ? valor : "Ninguno", version ? DibujoDashboard.Semibold(_migraciones, 13.5f) : e.CellStyle?.Font ?? _migraciones.Font,
                    Rectangle.FromLTRB(e.CellBounds.Left + TemaEcoAccess.Px(_migraciones, 10), e.CellBounds.Top, e.CellBounds.Right - TemaEcoAccess.Px(_migraciones, 4), e.CellBounds.Bottom),
                    elegida ? e.CellStyle!.SelectionForeColor : version ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else return;
            e.Handled = true;
        }
    }

    /// <summary>Una ficha: título con icono, cifra grande y detalle; <see cref="Tono"/> cambia el color de la cifra (p. ej. error).</summary>
    internal sealed record Indicador(string Titulo, IconChar Icono, string Valor, string Detalle, Color? Tono = null, bool Pequeno = false);

    /// <summary>Fichas en 4 columnas (2 en pantallas angostas) con el estilo de los indicadores del dashboard.</summary>
    internal sealed class RejillaIndicadores : Control
    {
        private Indicador[] _fichas = Array.Empty<Indicador>();

        internal RejillaIndicadores()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.Grouping;
            Margin = new Padding(0, 4, 0, 6);
        }

        internal void Mostrar(Indicador[] fichas)
        {
            _fichas = fichas;
            AccessibleDescription = string.Join("; ", fichas.Select(f => $"{f.Titulo}: {f.Valor}, {f.Detalle}"));
            OnSizeChanged(EventArgs.Empty);
            Invalidate();
        }

        private int P(float n) => DibujoDashboard.P(this, n);
        // Hasta 4 por fila en pantallas anchas (3 fichas quedan en una sola fila); 2 en las angostas.
        private int Columnas => Width >= P(1000) ? Math.Max(1, Math.Min(4, _fichas.Length))
            : _fichas.Length == 3 && Width >= P(640) ? 3 : Math.Min(2, Math.Max(1, _fichas.Length));
        private int AltoFicha => P(132);
        private int AltoNecesario => _fichas.Length == 0 ? 0 : (_fichas.Length + Columnas - 1) / Columnas * (AltoFicha + P(14)) - P(14);

        public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width is > 0 and < int.MaxValue / 2 ? proposedSize.Width : Width, AltoNecesario);

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && Height != AltoNecesario) Height = AltoNecesario;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int columnas = Columnas, separacion = P(14), ancho = (Width - separacion * (columnas - 1)) / columnas;
            const TextFormatFlags Linea = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            for (int i = 0; i < _fichas.Length; i++)
            {
                Indicador f = _fichas[i];
                var area = new Rectangle((i % columnas) * (ancho + separacion) + 1, (i / columnas) * (AltoFicha + separacion) + 1, ancho - P(4), AltoFicha - P(5));
                DibujoDashboard.Tarjeta(g, this, area, TemaEcoAccess.Superficie, TemaEcoAccess.Borde, P(12));
                int lado = P(36), x = area.Left + P(16), y = area.Top + P(14);
                Color tono = f.Tono ?? TemaEcoAccess.Verde;
                using (var ruta = RecursosEco.Redondeado(new RectangleF(x, y, lado, lado), P(9)))
                using (var fondo = new SolidBrush(f.Tono is null ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.ErrorSuave)) g.FillPath(fondo, ruta);
                Bitmap icono = CacheIconos.Obtener(f.Icono, lado * 55 / 100, tono);
                g.DrawImage(icono, x + (lado - icono.Width) / 2, y + (lado - icono.Height) / 2);
                TextRenderer.DrawText(g, f.Titulo, DibujoDashboard.Semibold(this, 13.5f), new Rectangle(x + lado + P(10), y, area.Right - x - lado - P(20), lado), TemaEcoAccess.Texto, Linea | TextFormatFlags.VerticalCenter);
                Font grande = DibujoDashboard.Semibold(this, f.Pequeno ? 20f : 28f);
                int yValor = y + lado + P(f.Pequeno ? 14 : 8);
                TextRenderer.DrawText(g, f.Valor, grande, new Rectangle(x, yValor, area.Right - x - P(12), grande.Height), f.Tono ?? TemaEcoAccess.VerdeOscuro, Linea);
                Font pequena = DibujoDashboard.Fuente(this, 12f);
                int yDetalle = f.Pequeno ? area.Bottom - P(14) - pequena.Height : yValor + grande.Height + P(2);
                TextRenderer.DrawText(g, f.Detalle, pequena, new Rectangle(x, yDetalle, area.Right - x - P(12), pequena.Height + 2), TemaEcoAccess.Suave, Linea);
            }
        }
    }

    /// <summary>Servicios de la demostración en filas: icono, nombre, detalle y pastilla «Activo» / «No disponible».</summary>
    internal sealed class ListaServicios : Control
    {
        private (string Nombre, bool Activo, string Detalle)[] _servicios = Array.Empty<(string, bool, string)>();

        internal ListaServicios()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            AccessibleRole = AccessibleRole.List;
            AccessibleName = "Servicios de la demostración";
            Margin = new Padding(0, 4, 0, 4);
        }

        internal void Mostrar(IEnumerable<(string Nombre, bool Activo, string Detalle)> servicios)
        {
            _servicios = servicios.ToArray();
            AccessibleDescription = string.Join("; ", _servicios.Select(s => $"{s.Nombre}: {(s.Activo ? "activo" : "no disponible")}, {s.Detalle}"));
            OnSizeChanged(EventArgs.Empty);
            Invalidate();
        }

        private int P(float n) => DibujoDashboard.P(this, n);
        private int AltoFila => P(66);
        private int AltoNecesario => _servicios.Length * (AltoFila + P(10));

        public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width is > 0 and < int.MaxValue / 2 ? proposedSize.Width : Width, AltoNecesario);

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && Height != AltoNecesario) Height = AltoNecesario;
        }

        private static IconChar IconoDe(string nombre) =>
            nombre.IndexOf("Telegram", StringComparison.OrdinalIgnoreCase) >= 0 ? IconChar.PaperPlane
            : nombre.StartsWith("IA", StringComparison.Ordinal) ? IconChar.Robot
            : nombre.IndexOf("bridge", StringComparison.OrdinalIgnoreCase) >= 0 || nombre.IndexOf("sensor", StringComparison.OrdinalIgnoreCase) >= 0 ? IconChar.Microchip
            : nombre.IndexOf("simulador", StringComparison.OrdinalIgnoreCase) >= 0 ? IconChar.Gamepad
            : IconChar.Gear;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            const TextFormatFlags Linea = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            for (int i = 0; i < _servicios.Length; i++)
            {
                var (nombre, activo, detalle) = _servicios[i];
                var area = new Rectangle(1, i * (AltoFila + P(10)) + 1, Width - P(4), AltoFila - P(4));
                DibujoDashboard.Tarjeta(g, this, area, TemaEcoAccess.Superficie, TemaEcoAccess.Borde, P(12));
                int lado = P(38), x = area.Left + P(14), y = area.Top + (area.Height - lado) / 2;
                using (var ruta = RecursosEco.Redondeado(new RectangleF(x, y, lado, lado), P(10)))
                using (var fondo = new SolidBrush(activo ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Fondo)) g.FillPath(fondo, ruta);
                Bitmap icono = CacheIconos.Obtener(IconoDe(nombre), lado * 55 / 100, activo ? TemaEcoAccess.Verde : TemaEcoAccess.Suave);
                g.DrawImage(icono, x + (lado - icono.Width) / 2, y + (lado - icono.Height) / 2);

                // Pastilla de estado a la derecha; nombre y detalle ocupan el resto.
                string estado = activo ? "Activo" : "No disponible";
                Font fuentePastilla = DibujoDashboard.Semibold(this, 12f);
                int anchoPastilla = TextRenderer.MeasureText(g, estado, fuentePastilla, Size.Empty, TextFormatFlags.NoPadding).Width + P(36);
                var celda = new Rectangle(area.Right - anchoPastilla - P(18), area.Top, anchoPastilla + P(12), area.Height);
                if (activo) DisenoGestion.PastillaEstado(g, celda, estado, true, this);
                else DisenoGestion.Pastilla(g, celda, estado, TemaEcoAccess.Suave, TemaEcoAccess.Fondo, TemaEcoAccess.Borde, IconChar.None, TemaEcoAccess.Suave, this);

                int xTexto = x + lado + P(12), anchoTexto = celda.Left - xTexto - P(12);
                Font fuenteNombre = DibujoDashboard.Semibold(this, 14f), fuenteDetalle = DibujoDashboard.Fuente(this, 12.5f);
                int altoTextos = fuenteNombre.Height + P(2) + fuenteDetalle.Height, yTexto = area.Top + (area.Height - altoTextos) / 2;
                TextRenderer.DrawText(g, nombre, fuenteNombre, new Rectangle(xTexto, yTexto, anchoTexto, fuenteNombre.Height), TemaEcoAccess.Texto, Linea);
                TextRenderer.DrawText(g, detalle, fuenteDetalle, new Rectangle(xTexto, yTexto + fuenteNombre.Height + P(2), anchoTexto, fuenteDetalle.Height), TemaEcoAccess.Suave, Linea);
            }
        }
    }
}
