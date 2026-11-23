using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using FontAwesome.Sharp;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Dashboard de gestión con el diseño aprobado: banner de bienvenida, accesos rápidos, ocho indicadores operativos y la fila
    /// «Actividad y verificación» (barras por día o semana, depósitos por material, estado de verificación y últimos depósitos).
    /// Todo sale de ServicioPanelAdministrativo y ServicioEstadisticas (Oracle); una consulta fallida muestra «—» y un aviso,
    /// nunca ceros inventados. Se cuentan depósitos, no kilogramos.
    /// </summary>
    internal sealed class PanelResumenAdministrativo : UserControl
    {
        private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        private readonly ServicioPanelAdministrativo _servicio;
        private readonly ServicioEstadisticas _estadisticas;
        private readonly int _actor;
        private readonly Action<string, bool> _estadoConexion;
        private readonly PanelDesplazable _scroll = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo };
        private readonly Panel _lienzo = new() { BackColor = TemaEcoAccess.Fondo, Location = Point.Empty };
        private readonly Button _actualizar = new() { Text = "Actualizar datos", AccessibleName = "Actualizar datos de Oracle" };
        private readonly BannerBienvenida _banner;
        private readonly List<AccesoRapido> _accesos = new();
        private readonly EncabezadoSeccion _encabezadoIndicadores = new("Indicadores operativos", "Resumen del estado actual del sistema y las principales operaciones.", IconChar.ChartColumn);
        private readonly EncabezadoSeccion _encabezadoActividad = new("Actividad y verificación", "Comportamiento de los depósitos y su estado de validación.", IconChar.ChartLine);
        private readonly IndicadorDashboard[] _indicadores =
        {
            new("Ciudadanos registrados", IconChar.Users),
            new("Solicitudes registradas", IconChar.ClipboardList),
            new("Depósitos registrados", IconChar.Cube),
            new("Depósitos verificados", IconChar.CircleCheck),
            new("Ecopuntos acreditados", IconChar.Coins),
            new("Ecopuntos reversados", IconChar.RotateLeft),
            new("Canecas activas", IconChar.TrashCan),
            new("Materiales habilitados", IconChar.Leaf),
        };
        private readonly GraficoBarras _barras = new();
        private readonly SelectorSegmentado _periodo = new("Día", "Semana");
        private readonly GraficoAnillo _materiales = new(leyendaDerecha: true) { AccessibleName = "Depósitos por material" };
        private readonly GraficoAnillo _verificacion = new(leyendaDerecha: false) { AccessibleName = "Estado de verificación", ValoresEnLeyenda = true };
        private readonly DataGridView _actividad = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            AccessibleName = "Últimos depósitos registrados",
        };
        private readonly EstadoVacioEco _vacioActividad = new(conEco: false, icono: IconChar.ClockRotateLeft);
        private readonly TarjetaDashboard _tarjetaBarras, _tarjetaMateriales, _tarjetaVerificacion, _tarjetaUltimos;
        private IReadOnlyList<FilaAgrupada> _porDia = Array.Empty<FilaAgrupada>();
        private DateOnly _desde, _hasta;
        private CancellationTokenSource? _consulta;
        // Al cambiar de módulo se cancela la consulta en curso: eso no es un fallo de Oracle y no se informa como tal.
        private bool _cerrando;

        internal PanelResumenAdministrativo(ServicioPanelAdministrativo servicio, ServicioEstadisticas estadisticas,
            int actor, string nombre, Action<string, bool> estadoConexion, IReadOnlyList<AccionRapida> acciones, Action? verDepositos = null)
        {
            _servicio = servicio;
            _estadisticas = estadisticas;
            _actor = actor;
            _estadoConexion = estadoConexion;
            Dock = DockStyle.Fill;
            BackColor = TemaEcoAccess.Fondo;

            _actualizar.Font = DibujoDashboard.FuenteBoton;
            EstiloControlesEco.Boton(_actualizar, EstiloControlesEco.TipoBoton.Secundario);
            EstiloControlesEco.IconoBoton(_actualizar, IconChar.ArrowsRotate);
            _actualizar.Click += async (_, _) => await ActualizarAsync();
            string primerNombre = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;
            _banner = new BannerBienvenida(_actualizar)
            {
                Saludo = $"¡Hola, {primerNombre}! Bienvenido a EcoAccess",
                Mensaje = "Juntos transformamos cada residuo en una oportunidad. Revisa el estado del sistema, gestiona recursos y verifica cómo estamos contribuyendo al medioambiente.",
                Estado = "Consulta los datos para ver el estado del sistema.",
            };

            foreach (AccionRapida accion in acciones)
            {
                var acceso = new AccesoRapido(accion.Texto, accion.Icono);
                acceso.Click += (_, _) => accion.Ejecutar();
                _accesos.Add(acceso);
            }

            _tarjetaBarras = new TarjetaDashboard("Actividad por día o semana", IconChar.ChartColumn, _barras, _periodo, IconChar.CalendarDays);
            _tarjetaMateriales = new TarjetaDashboard("Depósitos por material", IconChar.Recycle, _materiales, iconoNota: IconChar.Cube);
            _tarjetaVerificacion = new TarjetaDashboard("Estado de verificación", IconChar.ClipboardCheck, _verificacion, iconoNota: IconChar.ShieldHalved);
            var tabla = new Panel { BackColor = TemaEcoAccess.Superficie };
            TemaEcoAccess.EstilizarTabla(_actividad);
            EstiloControlesEco.MarcoTabla(_actividad);
            _actividad.ColumnHeadersDefaultCellStyle.Font = DibujoDashboard.FuenteEncabezadoTabla;
            _actividad.ColumnHeadersDefaultCellStyle.ForeColor = TemaEcoAccess.Texto;
            _actividad.DefaultCellStyle.Font = TemaEcoAccess.Pequena;
            _actividad.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _actividad.DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
            _actividad.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            _actividad.CellPainting += PintarEstado;
            // Como en el diseño, la tabla aparece sin una fila marcada; la selección es solo para quien la recorre.
            _actividad.DataBindingComplete += (_, _) => _actividad.ClearSelection();
            tabla.Controls.Add(_actividad);
            tabla.Controls.Add(_vacioActividad);
            _vacioActividad.Dock = DockStyle.Fill;
            var verTodos = new EnlacePastilla("Ver todos") { Visible = verDepositos is not null };
            if (verDepositos is not null) verTodos.Click += (_, _) => verDepositos();
            _tarjetaUltimos = new TarjetaDashboard("Últimos depósitos", IconChar.Coins, tabla, verTodos, tituloGrande: true);
            _tarjetaVerificacion.Nota = "Un operario o administrador confirma el material antes de otorgar ecopuntos.";
            _periodo.Cambio += (_, _) => MostrarBarras();

            _lienzo.Controls.Add(_banner);
            foreach (AccesoRapido acceso in _accesos) _lienzo.Controls.Add(acceso);
            _lienzo.Controls.Add(_encabezadoIndicadores);
            foreach (IndicadorDashboard indicador in _indicadores) _lienzo.Controls.Add(indicador);
            _lienzo.Controls.Add(_encabezadoActividad);
            foreach (Control tarjeta in new Control[] { _tarjetaBarras, _tarjetaMateriales, _tarjetaVerificacion, _tarjetaUltimos }) _lienzo.Controls.Add(tarjeta);
            _scroll.Controls.Add(_lienzo);
            Controls.Add(_scroll);
            // Solo desplazamiento vertical: el lienzo se ajusta al ancho visible (mismo truco que el menú lateral).
            _scroll.AutoScroll = false;
            _scroll.HorizontalScroll.Maximum = 0;
            _scroll.HorizontalScroll.Enabled = false;
            _scroll.HorizontalScroll.Visible = false;
            _scroll.AutoScroll = true;
            _scroll.ClientSizeChanged += (_, _) => Adaptar();
            Load += async (_, _) => { Adaptar(); await ActualizarAsync(); };
            Limpiar("Consulta los datos para ver los gráficos.");
            _actividad.Visible = false;
            _vacioActividad.Mostrar("Consulta los datos para ver los últimos depósitos.", false);
        }

        private int P(float unidades) => DibujoDashboard.P(this, unidades);

        /// <summary>
        /// Distribución del diseño: en pantallas anchas los accesos y los indicadores van en una fila y las cuatro tarjetas de
        /// actividad en otra; en pantallas angostas pasan a dos filas (o a una columna) sin cortar nada. El módulo se desplaza.
        /// </summary>
        private void Adaptar()
        {
            int margen = P(22), separacion = P(12);
            // La tabla usa fuentes en píxeles de la escala real, igual que las piezas dibujadas del dashboard.
            _actividad.DefaultCellStyle.Font = DibujoDashboard.Fuente(this, 11.5f);
            _actividad.ColumnHeadersDefaultCellStyle.Font = DibujoDashboard.Semibold(this, 11.5f);
            int ancho = Math.Max(P(320), _scroll.ClientSize.Width - 2 * margen);
            _lienzo.SuspendLayout();
            int y = P(14);
            _banner.SetBounds(margen, y, ancho, Math.Max(_banner.Height, P(186)));
            _banner.Height = _banner.AltoNecesario();
            y += _banner.Height + P(14);

            // Una fila si cada tarjeta tiene al menos 150 px (el texto cabe en dos líneas); si no, dos filas.
            y = Rejilla(_accesos, margen, y, ancho, Columnas(_accesos.Count, ancho, P(150), P(10)), P(70), P(10));
            _encabezadoIndicadores.SetBounds(margen, y + P(10), ancho, P(44));
            y = _encabezadoIndicadores.Bottom + P(6);
            int columnasIndicadores = Columnas(_indicadores.Length, ancho, P(158), P(10));
            y = Rejilla(_indicadores, margen, y, ancho, columnasIndicadores, P(164), P(10));
            _encabezadoActividad.SetBounds(margen, y + P(12), ancho, P(44));
            y = _encabezadoActividad.Bottom + P(8);

            int alto = P(330);
            Control[] tarjetas = { _tarjetaBarras, _tarjetaMateriales, _tarjetaVerificacion, _tarjetaUltimos };
            if (ancho >= P(1100))
            {
                // Proporciones del diseño: barras 25 %, materiales 17 %, verificación 15 % y la tabla el resto.
                int util = ancho - 3 * separacion;
                int[] anchos = { util * 255 / 1000, util * 165 / 1000, util * 150 / 1000, 0 };
                anchos[3] = util - anchos[0] - anchos[1] - anchos[2];
                int x = margen;
                for (int i = 0; i < 4; i++) { tarjetas[i].SetBounds(x, y, anchos[i], alto); x += anchos[i] + separacion; }
                y += alto;
            }
            else if (ancho >= P(760))
            {
                int mitad = (ancho - separacion) / 2;
                _tarjetaBarras.SetBounds(margen, y, mitad, alto);
                _tarjetaUltimos.SetBounds(margen + mitad + separacion, y, ancho - mitad - separacion, alto);
                y += alto + separacion;
                _tarjetaMateriales.SetBounds(margen, y, mitad, alto);
                _tarjetaVerificacion.SetBounds(margen + mitad + separacion, y, ancho - mitad - separacion, alto);
                y += alto;
            }
            else
            {
                foreach (Control tarjeta in tarjetas)
                {
                    tarjeta.SetBounds(margen, y, ancho, alto);
                    y += alto + separacion;
                }
                y -= separacion;
            }
            _lienzo.Size = new Size(ancho + 2 * margen, y + P(24));
            _lienzo.ResumeLayout(true);
            // El lienzo ya mide lo que cabe a lo ancho: se recalcula el desplazamiento para no dejar una barra horizontal vieja.
            _scroll.PerformLayout();
        }

        /// <summary>
        /// Todas en una fila si cada tarjeta mide al menos <paramref name="minimo"/>; si no, la mitad por fila (y, en pantallas muy
        /// angostas, las que quepan). Así las filas quedan parejas: 8 → 8, 4 o 2.
        /// </summary>
        private static int Columnas(int cantidad, int ancho, int minimo, int separacion)
        {
            int caben = Math.Max(1, (ancho + separacion) / (minimo + separacion));
            if (caben >= cantidad) return cantidad;
            int mitad = (int)Math.Ceiling(cantidad / 2.0);
            return caben >= mitad ? mitad : Math.Max(1, Math.Min(caben, (int)Math.Ceiling(cantidad / 4.0)));
        }

        /// <summary>Coloca controles en una rejilla de columnas iguales y devuelve la posición vertical siguiente.</summary>
        private static int Rejilla(IReadOnlyList<Control> controles, int x, int y, int ancho, int columnas, int alto, int separacion)
        {
            if (controles.Count == 0) return y;
            columnas = Math.Max(1, Math.Min(columnas, controles.Count));
            int anchoCelda = (ancho - (columnas - 1) * separacion) / columnas;
            for (int i = 0; i < controles.Count; i++)
            {
                int fila = i / columnas, columna = i % columnas;
                int celda = columna == columnas - 1 ? ancho - columna * (anchoCelda + separacion) : anchoCelda;
                controles[i].SetBounds(x + columna * (anchoCelda + separacion), y + fila * (alto + separacion), celda, alto);
            }
            int filas = (int)Math.Ceiling(controles.Count / (double)columnas);
            return y + filas * alto + (filas - 1) * separacion;
        }

        /// <summary>El estado del depósito va en una pastilla: ámbar «Pendiente de verificación» o verde «Verificado».</summary>
        private void PintarEstado(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _actividad.Columns[e.ColumnIndex].Name != "Estado" || e.Value is not string estado || e.Graphics is null) return;
            e.PaintBackground(e.CellBounds, false);
            bool verificado = estado.StartsWith("Verificado", StringComparison.Ordinal);
            Font fuente = DibujoDashboard.Semibold(this, 10);
            Size medida = TextRenderer.MeasureText(e.Graphics, estado, fuente, Size.Empty, TextFormatFlags.NoPadding);
            int alto = medida.Height + P(8), ancho = Math.Min(e.CellBounds.Width - P(8), medida.Width + P(20));
            var pastilla = new Rectangle(e.CellBounds.Left + P(4), e.CellBounds.Top + (e.CellBounds.Height - alto) / 2, ancho, alto);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var ruta = RecursosEco.Redondeado(pastilla, alto / 2f))
            using (var fondo = new SolidBrush(verificado ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.AdvertenciaSuave))
                e.Graphics.FillPath(fondo, ruta);
            TextRenderer.DrawText(e.Graphics, estado, fuente, pastilla, verificado ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Advertencia,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            e.Handled = true;
        }

        private async Task ActualizarAsync()
        {
            if (_consulta is not null) return;
            using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _consulta = consulta;
            _actualizar.Enabled = false;
            _banner.Estado = "Consultando Oracle…";
            foreach (IndicadorDashboard indicador in _indicadores) indicador.Mostrar("—", "Consultando…");
            Limpiar("Consultando datos de Oracle…");
            try
            {
                var resultado = await _servicio.ConsultarAsync(_actor, consulta.Token).WaitAsync(consulta.Token);
                if (IsDisposed) return;
                if (!resultado.Exitoso)
                {
                    MostrarFallo("Tu cuenta no tiene acceso a este resumen: " + resultado.Mensaje, "Resumen no autorizado");
                    return;
                }
                DateOnly hoy = DateOnly.FromDateTime(HoraColombia.DesdeUtc(DateTime.UtcNow));
                DateOnly primero = hoy.AddDays(-29);
                var periodo = HoraColombia.PeriodoUtc(primero, hoy);
                var porDia = await _estadisticas.AgruparDepositosAsync(_actor, new FiltroEstadisticas(periodo.DesdeUtc, periodo.HastaUtc), AgrupacionDepositos.Dia, consulta.Token).WaitAsync(consulta.Token);
                var historico = HoraColombia.PeriodoUtc(new DateOnly(2020, 1, 1), hoy);
                var porMaterial = await _estadisticas.AgruparDepositosAsync(_actor, new FiltroEstadisticas(historico.DesdeUtc, historico.HastaUtc), AgrupacionDepositos.Material, consulta.Token).WaitAsync(consulta.Token);
                if (IsDisposed) return;
                if (!porDia.Exitoso || !porMaterial.Exitoso)
                {
                    MostrarFallo("Tu cuenta no tiene acceso al detalle de estadísticas.", "Detalle no autorizado");
                    return;
                }
                MostrarResumen(resultado.Valor, porDia.Valor, porMaterial.Valor, primero, hoy);
            }
            catch (Exception) when (!IsDisposed && !_cerrando)
            {
                MostrarFallo("No pudimos conectar con Oracle. Revisa que Docker y la base estén en marcha y pulsa «Actualizar datos».", "Oracle sin respuesta · vuelve a intentar");
            }
            catch (Exception) when (IsDisposed || _cerrando) { }
            finally
            {
                _consulta = null;
                if (!IsDisposed) _actualizar.Enabled = true;
            }
        }

        /// <summary>
        /// Muestra un resumen ya autorizado: indicadores, barras, anillos y últimos depósitos. Separado de la consulta para que el
        /// arnés de capturas pueda dibujar el dashboard con datos de ejemplo sin Oracle.
        /// </summary>
        internal void MostrarResumen(ResumenPanelAdministrativo resumen, IReadOnlyList<FilaAgrupada> porDiaValor, IReadOnlyList<FilaAgrupada> porMaterialValor, DateOnly primero, DateOnly hoy)
        {
            IndicadoresPanel i = resumen.Indicadores;
            string N(long valor) => valor.ToString("N0", Colombia);
            _indicadores[0].Mostrar(N(i.Ciudadanos), "Cuentas activas con rol Ciudadano");
            _indicadores[1].Mostrar(N(i.Interacciones), "Interacciones iniciadas, con o sin depósito");
            _indicadores[2].Mostrar(N(i.Depositos), "Total de depósitos en el sistema");
            _indicadores[3].Mostrar(N(i.Verificados), i.Depositos == 0 ? "Aún no hay depósitos registrados" : $"{100.0 * i.Verificados / i.Depositos:N0} % de los depósitos registrados");
            _indicadores[4].Mostrar(N(i.CreditosOtorgados), $"Saldo neto tras reversos: {N(i.SaldoNetoEcopuntos)}");
            _indicadores[5].Mostrar(N(Math.Max(0, i.CreditosOtorgados - i.SaldoNetoEcopuntos)), $"Saldo neto: {N(i.SaldoNetoEcopuntos)} ecopuntos");
            _indicadores[6].Mostrar(N(i.EnServicio), "Canecas registradas en el sistema", $"de {N(i.Contenedores)}");
            _indicadores[7].Mostrar(N(i.MaterialesHabilitados), "Materiales habilitados en el catálogo");

            // Barras: todos los días del período (los días sin registros cuentan 0, así el eje no salta fechas).
            _porDia = porDiaValor;
            _desde = primero;
            _hasta = hoy;
            MostrarBarras();

            var orden = porMaterialValor.Where(m => m.Depositos > 0).OrderByDescending(m => m.Depositos).ToList();
            var visibles = orden.Take(5).Select((m, k) => (m.Etiqueta, (long)m.Depositos, DibujoDashboard.Serie[k])).ToList();
            long otros = orden.Skip(5).Sum(m => (long)m.Depositos);
            if (otros > 0) visibles.Add(("Otros", otros, DibujoDashboard.Serie[5]));
            long totalMateriales = orden.Sum(m => (long)m.Depositos);
            if (totalMateriales > 0) _materiales.Mostrar(visibles, totalMateriales); else _materiales.Vaciar("Sin depósitos registrados todavía.");
            _tarjetaMateriales.Nota = $"Material declarado o verificado · total consultado: {N(totalMateriales)} depósitos.";

            long pendientes = Math.Max(0, i.Depositos - i.Verificados);
            if (i.Depositos > 0)
                _verificacion.Mostrar(new[] { ("Verificados", i.Verificados, TemaEcoAccess.Verde), ("Pendientes", pendientes, DibujoDashboard.Pendiente) }, i.Depositos);
            else _verificacion.Vaciar("Sin depósitos para verificar.");

            _actividad.DataSource = resumen.Actividad.Select(a => new
            {
                Depósito = a.IdDeposito,
                Fecha = HoraColombia.DesdeUtc(a.FechaUtc).ToString("dd/MM/yyyy HH:mm"),
                a.Material,
                Caneca = a.Caneca ?? "—",
                Estado = a.Verificado ? "Verificado" : "Pendiente de verificación",
            }).ToList();
            AjustarColumnas();
            _actividad.ClearSelection();
            bool hay = resumen.Actividad.Count > 0;
            _actividad.Visible = hay;
            _vacioActividad.Visible = !hay;
            if (!hay) _vacioActividad.Mostrar("Todavía no hay depósitos registrados. ¡Comienza la primera acción ambiental escaneando el QR de una caneca!", false);
            string hora = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).ToString("HH:mm");
            _banner.Estado = $"Datos de Oracle actualizados a las {hora} (hora de Colombia).";
            _estadoConexion($"Oracle consultado a las {hora} · Colombia", false);
        }

        /// <summary>Barras por día (todos los días del período) o por semana (lunes a domingo; las semanas extremas pueden ser parciales).</summary>
        private void MostrarBarras()
        {
            if (_desde == default) return;
            var porDia = _porDia.Where(f => f.Dia is not null).ToDictionary(f => f.Dia!.Value);
            var dias = Enumerable.Range(0, _hasta.DayNumber - _desde.DayNumber + 1).Select(k => _desde.AddDays(k)).ToList();
            int Depositos(DateOnly d) => porDia.TryGetValue(d, out FilaAgrupada? f) ? f.Depositos : 0;
            int Verificados(DateOnly d) => porDia.TryGetValue(d, out FilaAgrupada? f) ? f.Verificados : 0;
            string periodo = $"{_desde:dd/MM}–{_hasta:dd/MM/yyyy} · hora de Colombia.";
            if (_periodo.Elegida == 0)
            {
                _barras.Mostrar(dias.Select(d => d.ToString("dd/MM")).ToArray(), dias.Select(Depositos).ToArray(), dias.Select(Verificados).ToArray());
                _tarjetaBarras.Nota = periodo + "\nCuenta depósitos detectados, no aperturas ni peso.";
                return;
            }
            var semanas = dias.GroupBy(d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7))).ToList();
            _barras.Mostrar(semanas.Select(s => s.Key.ToString("dd/MM")).ToArray(),
                semanas.Select(s => s.Sum(Depositos)).ToArray(), semanas.Select(s => s.Sum(Verificados)).ToArray());
            _tarjetaBarras.Nota = periodo + "\nSemanas de lunes a domingo; las de los extremos pueden ser parciales.";
        }

        /// <summary>Anchos relativos de las columnas de «Últimos depósitos», como en el diseño.</summary>
        private void AjustarColumnas()
        {
            foreach (var (nombre, peso) in new (string, float)[] { ("Depósito", 13), ("Fecha", 22), ("Material", 19), ("Caneca", 20), ("Estado", 26) })
                if (_actividad.Columns[nombre] is { } columna)
                {
                    columna.FillWeight = peso;
                    columna.SortMode = DataGridViewColumnSortMode.NotSortable;
                    // Solo el material y la caneca se parten en dos líneas; la fecha y el número van en una.
                    columna.DefaultCellStyle.WrapMode = nombre is "Material" or "Caneca" ? DataGridViewTriState.True : DataGridViewTriState.False;
                }
        }

        private void Limpiar(string mensaje)
        {
            _barras.Vaciar(mensaje);
            _materiales.Vaciar(mensaje);
            _verificacion.Vaciar(mensaje);
            _tarjetaBarras.Nota = "Cuenta depósitos detectados, no aperturas ni peso.";
            _tarjetaMateriales.Nota = "Material declarado o verificado.";
        }

        private void MostrarFallo(string mensaje, string estado)
        {
            foreach (IndicadorDashboard indicador in _indicadores) indicador.Mostrar("—", "Sin datos verificados.");
            Limpiar("Sin datos verificados para mostrar.");
            _actividad.DataSource = null;
            _actividad.Visible = false;
            _vacioActividad.Visible = true;
            _vacioActividad.Mostrar(mensaje, true);
            _banner.Estado = mensaje;
            _estadoConexion(estado, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _cerrando = true; _consulta?.Cancel(); }
            base.Dispose(disposing);
        }
    }

    /// <summary>Icono sobre un cuadrado redondeado verde suave (estado de error del asistente de depósito).</summary>
    internal sealed class TarjetaIcono : Control
    {
        private readonly IconChar _icono;
        private Bitmap? _imagen;

        public TarjetaIcono(IconChar icono)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            _icono = icono;
            BackColor = TemaEcoAccess.Superficie;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);
            using (var ruta = RecursosEco.Redondeado(new RectangleF(0, 0, Width - 1, Height - 1), TemaEcoAccess.Px(this, 10)))
            using (var fondo = new SolidBrush(TemaEcoAccess.VerdeSuave))
                e.Graphics.FillPath(fondo, ruta);
            int lado = Width / 2;
            if (_imagen is null || _imagen.Width != lado)
            {
                _imagen?.Dispose();
                _imagen = _icono.ToBitmap(IconFont.Solid, lado, TemaEcoAccess.Verde);
            }
            e.Graphics.DrawImage(_imagen, (Width - _imagen.Width) / 2, (Height - _imagen.Height) / 2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _imagen?.Dispose();
            base.Dispose(disposing);
        }
    }
}
