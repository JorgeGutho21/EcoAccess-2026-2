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
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Dashboard: bienvenida con Eco, acciones rápidas, indicadores, gráficos y últimos depósitos. Todo sale de
    /// ServicioPanelAdministrativo y ServicioEstadisticas (Oracle); una consulta fallida muestra «—» y un aviso,
    /// nunca ceros inventados. Se cuentan depósitos, no kilogramos.
    /// </summary>
    internal sealed class PanelResumenAdministrativo : UserControl
    {
        private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        private readonly ServicioPanelAdministrativo _servicio;
        private readonly ServicioEstadisticas _estadisticas;
        private readonly int _actor;
        private readonly Action<string, bool> _estadoConexion;
        private readonly PanelGraficosResumen _graficos = new();
        private readonly FlowLayoutPanel _indicadores = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Margin = Padding.Empty };
        private readonly List<(Label Valor, Label Detalle, string Nombre)> _valores = new List<(Label Valor, Label Detalle, string Nombre)>();
        private readonly Label _actualizado = new() { AutoSize = true, ForeColor = TemaEcoAccess.VerdeHoja, Margin = new Padding(0, 14, 0, 0) };
        private readonly KryptonButton _actualizar = new() { Text = "Actualizar datos", AutoSize = true };
        private readonly DataGridView _actividad = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AccessibleName = "Últimos ocho depósitos registrados",
        };
        private readonly EstadoVacioEco _vacioActividad = new();
        private readonly PanelDesplazable _scroll = new() { Dock = DockStyle.Fill };
        private readonly TableLayoutPanel _estructura = new() { AutoSize = true, ColumnCount = 1, Location = Point.Empty };
        private CancellationTokenSource? _consulta;
        // Al cambiar de módulo se cancela la consulta en curso: eso no es un fallo de Oracle y no se informa como tal.
        private bool _cerrando;

        internal PanelResumenAdministrativo(ServicioPanelAdministrativo servicio, ServicioEstadisticas estadisticas,
            int actor, string nombre, Action<string, bool> estadoConexion, IReadOnlyList<AccionRapida> acciones)
        {
            _servicio = servicio;
            _estadisticas = estadisticas;
            _actor = actor;
            _estadoConexion = estadoConexion;
            Dock = DockStyle.Fill;
            BackColor = TemaEcoAccess.Fondo;
            _scroll.Padding = new Padding(TemaEcoAccess.Px(this, 24), TemaEcoAccess.Px(this, 20), TemaEcoAccess.Px(this, 24), TemaEcoAccess.Px(this, 24));
            _estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _estructura.Controls.Add(CrearBienvenida(nombre));
            _estructura.Controls.Add(CrearAcciones(acciones));
            _estructura.Controls.Add(Encabezado("Indicadores operativos", "Totales históricos de esta base, incluidas pruebas y simulaciones. Consulta Informe institucional para excluirlas."));
            _estructura.Controls.Add(_indicadores);
            _estructura.Controls.Add(Encabezado("Actividad", "Registros de esta base, incluidos ensayos; no acreditan funcionamiento físico ni kilogramos"));
            _estructura.Controls.Add(_graficos);
            _estructura.Controls.Add(CrearActividad());

            var datos = new (string Nombre, string Ayuda, IconChar Icono)[]
            {
                ("Ciudadanos registrados", "Cuentas activas con rol Ciudadano", IconChar.Users),
                ("Solicitudes registradas", "Interacciones iniciadas, con o sin depósito", IconChar.ClipboardList),
                ("Depósitos registrados", "Registros operativos, incluidos ensayos", IconChar.BoxOpen),
                ("Depósitos verificados", "Material revisado por un operario", IconChar.CircleCheck),
                ("Ecopuntos acreditados", "Créditos otorgados antes de reversos", IconChar.Coins),
                ("Ecopuntos reversados", "Anulaciones y correcciones", IconChar.RotateLeft),
                ("Canecas activas", "Estado configurado, no telemetría", IconChar.Trash),
                ("Materiales sugeridos", "Sugerencias de IA; no autorizaciones", IconChar.WandMagicSparkles),
            };
            foreach (var dato in datos) _indicadores.Controls.Add(CrearIndicador(dato.Nombre, dato.Ayuda, dato.Icono));

            _scroll.Controls.Add(_estructura);
            Controls.Add(_scroll);
            _scroll.SizeChanged += (_, _) => Adaptar();
            _actualizar.Click += async (_, _) => await ActualizarAsync();
            Load += async (_, _) => { Adaptar(); await ActualizarAsync(); };
            _actividad.Visible = false;
            _vacioActividad.Mostrar("Consulta los datos para ver los últimos depósitos.", false);
        }

        private Control CrearBienvenida(string nombre)
        {
            // Crece con el texto: en pantallas estrechas el saludo ocupa más líneas y nada se recorta.
            var tarjeta = new TarjetaEco { Relleno = TemaEcoAccess.VerdeOscuro, ColorBorde = TemaEcoAccess.VerdeOscuro, Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(0, TemaEcoAccess.Px(this, 220)), Margin = new Padding(0, 0, 0, 8) };
            var interior = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, BackColor = TemaEcoAccess.VerdeOscuro };
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var textos = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.VerdeOscuro, Padding = new Padding(8, 6, 0, 0) };
            string primerNombre = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;
            var saludo = new Label { Text = $"¡Hola, {primerNombre}! Bienvenido a EcoAccess", AutoSize = true, Font = TemaEcoAccess.Titulo, ForeColor = Color.White, Margin = Padding.Empty };
            var mensaje = new Label { Text = "Juntos transformamos cada residuo en una oportunidad. Revisemos cómo estamos contribuyendo al medioambiente.", AutoSize = true, ForeColor = TemaEcoAccess.VerdeHoja, Font = TemaEcoAccess.Entrada, Margin = new Padding(0, 8, 0, 0) };
            TemaEcoAccess.EstilizarAccion(_actualizar);
            _actualizar.Margin = new Padding(0, 16, 0, 0);
            _actualizar.Values.Image = IconChar.ArrowsRotate.ToBitmap(IconFont.Solid, TemaEcoAccess.Px(this, 16), TemaEcoAccess.VerdeOscuro);
            textos.Controls.AddRange(new Control[] { saludo, mensaje, _actualizar, _actualizado });
            textos.SizeChanged += (_, _) =>
            {
                var limite = new Size(Math.Max(240, textos.ClientSize.Width - 16), 0);
                saludo.MaximumSize = mensaje.MaximumSize = _actualizado.MaximumSize = limite;
            };
            int lado = TemaEcoAccess.Px(this, 168);
            var eco = new ImagenRedondeada { Imagen = RecursosEco.Eco, Circular = false, RadioEsquina = 18, Size = new Size(lado, lado), Margin = new Padding(16, 0, 4, 0), AccessibleName = "Eco, la mascota de EcoAccess, saluda con el pulgar arriba", AccessibleRole = AccessibleRole.Graphic, Anchor = AnchorStyles.Right };
            interior.Controls.Add(textos, 0, 0);
            interior.Controls.Add(eco, 1, 0);
            tarjeta.Controls.Add(interior);
            return tarjeta;
        }

        private Control CrearAcciones(IReadOnlyList<AccionRapida> acciones)
        {
            var fila = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Margin = new Padding(0, 4, 0, 4) };
            foreach (AccionRapida accion in acciones)
            {
                var boton = new KryptonButton { Text = accion.Texto, AutoSize = true, Margin = new Padding(0, 0, 10, 10), AccessibleName = accion.Texto };
                TemaEcoAccess.EstilizarAccion(boton);
                boton.Values.Image = accion.Icono.ToBitmap(IconFont.Solid, TemaEcoAccess.Px(this, 16), TemaEcoAccess.Verde);
                boton.Click += (_, _) => accion.Ejecutar();
                fila.Controls.Add(boton);
            }
            return fila;
        }

        private Control Encabezado(string titulo, string ayuda)
        {
            var fila = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, Margin = new Padding(6, 18, 0, 6) };
            fila.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 12, 0) });
            fila.Controls.Add(new Label { Text = ayuda, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 7, 0, 0) });
            return fila;
        }

        private Control CrearIndicador(string nombre, string ayuda, IconChar icono)
        {
            var tarjeta = new TarjetaEco { Height = TemaEcoAccess.Px(this, 200), Width = TemaEcoAccess.Px(this, 300), Padding = new Padding(TemaEcoAccess.Px(this, 14) + TemaEcoAccess.Px(this, TarjetaEco.Sombra)) };
            var interior = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = TemaEcoAccess.Superficie };
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 3; i++) interior.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int lado = TemaEcoAccess.Px(this, 40);
            var fondoIcono = new TarjetaIcono(icono) { Size = new Size(lado, lado), Margin = new Padding(0, 0, 12, 0) };
            var titulo = new Label { Text = nombre, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Anchor = AnchorStyles.Left, Margin = Padding.Empty };
            var valor = new Label { Text = "—", AutoSize = true, Font = TemaEcoAccess.Valor, ForeColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(0, 8, 0, 0), AccessibleName = nombre };
            var detalle = new Label { Text = ayuda, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = Padding.Empty };
            interior.Controls.Add(fondoIcono, 0, 0);
            interior.Controls.Add(titulo, 1, 0);
            interior.Controls.Add(valor, 0, 1);
            interior.SetColumnSpan(valor, 2);
            interior.Controls.Add(detalle, 0, 2);
            interior.SetColumnSpan(detalle, 2);
            interior.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(120, interior.ClientSize.Width);
                detalle.MaximumSize = new Size(ancho, 0);
                titulo.MaximumSize = new Size(Math.Max(80, ancho - lado - 12), 0);
            };
            tarjeta.Controls.Add(interior);
            _valores.Add((valor, detalle, nombre));
            return tarjeta;
        }

        private Control CrearActividad()
        {
            var tarjeta = new TarjetaEco { Dock = DockStyle.Top, Height = TemaEcoAccess.Px(this, 400), Margin = new Padding(0, 4, 0, 0) };
            var interior = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Superficie };
            interior.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            interior.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            interior.Controls.Add(new Label { Text = "Últimos depósitos", AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 10) }, 0, 0);
            var cuerpo = new Panel { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Superficie };
            TemaEcoAccess.EstilizarTabla(_actividad);
            _actividad.CellFormatting += FormatearEstado;
            cuerpo.Controls.Add(_actividad);
            cuerpo.Controls.Add(_vacioActividad);
            _vacioActividad.Dock = DockStyle.Fill;
            interior.Controls.Add(cuerpo, 0, 1);
            tarjeta.Controls.Add(interior);
            return tarjeta;
        }

        /// <summary>El estado se muestra con texto y color (pendiente en ámbar, verificado en verde).</summary>
        private void FormatearEstado(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || _actividad.Columns[e.ColumnIndex].Name != "Estado" || e.Value is not string estado) return;
            bool verificado = estado.StartsWith("Verificado", StringComparison.Ordinal);
            e.CellStyle!.ForeColor = verificado ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Advertencia;
            e.CellStyle.Font = TemaEcoAccess.Subtitulo;
        }

        private void Adaptar()
        {
            int ancho = Math.Max(TemaEcoAccess.Px(this, 320), _scroll.ClientSize.Width - _scroll.Padding.Horizontal);
            _estructura.MinimumSize = _estructura.MaximumSize = new Size(ancho, 0);
            _estructura.Width = ancho;
            // 4 indicadores por fila en pantallas anchas o medianas, 2 o 1 en estrechas; así las 8 tarjetas forman filas parejas.
            int columnas = ancho >= TemaEcoAccess.Px(this, 900) ? 4 : ancho >= TemaEcoAccess.Px(this, 480) ? 2 : 1;
            int anchoTarjeta = (ancho - 12 * columnas) / columnas;
            foreach (Control tarjeta in _indicadores.Controls) tarjeta.Width = anchoTarjeta;
            foreach (Control control in _estructura.Controls) if (control is not FlowLayoutPanel) control.Width = ancho;
        }

        private async Task ActualizarAsync()
        {
            if (_consulta is not null) return;
            using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _consulta = consulta;
            _actualizar.Enabled = false;
            _actualizado.Text = "Consultando Oracle…";
            foreach (var v in _valores) { v.Valor.Text = "—"; v.Valor.AccessibleName = $"{v.Nombre}: consultando"; }
            _graficos.Limpiar("Consultando datos de Oracle…");
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
                IndicadoresPanel i = resultado.Valor.Indicadores;
                long pendientes = Math.Max(0, i.Depositos - i.Verificados);
                (long Valor, string Detalle)[] datos =
                new (long Valor, string Detalle)[]
    {
        (i.Ciudadanos, "Cuentas activas con rol Ciudadano"),
        (i.Interacciones, "Interacciones iniciadas, con o sin depósito"),
        (i.Depositos, $"{pendientes:N0} pendientes de verificación"),
        (i.Verificados, i.Depositos == 0 ? "Aún no hay depósitos" : $"{100.0 * i.Verificados / i.Depositos:N0} % de los depósitos"),
        (i.CreditosOtorgados, $"Saldo neto tras reversos: {i.SaldoNetoEcopuntos:N0}"),
        (Math.Max(0, i.CreditosOtorgados - i.SaldoNetoEcopuntos), $"Saldo neto: {i.SaldoNetoEcopuntos:N0} ecopuntos"),
        (i.EnServicio, $"de {i.Contenedores:N0} canecas registradas"),
        (i.ClasificacionesIa, $"{i.MaterialesHabilitados:N0} materiales habilitados en el catálogo")
    };
                for (int k = 0; k < datos.Length; k++)
                {
                    _valores[k].Valor.Text = datos[k].Valor.ToString("N0", Colombia);
                    _valores[k].Detalle.Text = datos[k].Detalle;
                    _valores[k].Valor.AccessibleName = $"{_valores[k].Nombre}: {_valores[k].Valor.Text}";
                }
                _graficos.Mostrar(i, porDia.Valor, porMaterial.Valor, primero, hoy);
                _actividad.DataSource = resultado.Valor.Actividad.Select(a => new
                {
                    Depósito = a.IdDeposito,
                    Fecha = HoraColombia.DesdeUtc(a.FechaUtc).ToString("dd/MM/yyyy HH:mm"),
                    a.Material,
                    Caneca = a.Caneca ?? "—",
                    Estado = a.Verificado ? "Verificado" : "Pendiente de verificación",
                }).ToList();
                bool hay = resultado.Valor.Actividad.Count > 0;
                _actividad.Visible = hay;
                _vacioActividad.Visible = !hay;
                if (!hay) _vacioActividad.Mostrar("Todavía no hay depósitos registrados. ¡Comienza la primera acción ambiental escaneando el QR de una caneca!", false);
                string hora = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).ToString("HH:mm");
                _actualizado.Text = $"Datos de Oracle actualizados a las {hora} (hora de Colombia).";
                _estadoConexion($"Oracle consultado a las {hora} · Colombia", false);
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

        private void MostrarFallo(string mensaje, string estado)
        {
            foreach (var v in _valores) { v.Valor.Text = "—"; v.Valor.AccessibleName = $"{v.Nombre}: sin datos verificados"; }
            _graficos.Limpiar("Sin datos verificados para mostrar.");
            _actividad.DataSource = null;
            _actividad.Visible = false;
            _vacioActividad.Visible = true;
            _vacioActividad.Mostrar(mensaje, true);
            _actualizado.Text = mensaje;
            _estadoConexion(estado, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _cerrando = true; _consulta?.Cancel(); }
            base.Dispose(disposing);
        }
    }

    /// <summary>Icono sobre un cuadrado redondeado verde suave (indicadores del dashboard).</summary>
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

    /// <summary>Estado vacío o de error con Eco y un mensaje que explica qué pasa y qué hacer.</summary>
    internal sealed class EstadoVacioEco : TableLayoutPanel
    {
        private readonly Label _mensaje = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.None, Margin = new Padding(0, 12, 0, 0) };

        public EstadoVacioEco()
        {
            ColumnCount = 1;
            RowCount = 4;
            BackColor = TemaEcoAccess.Superficie;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            int lado = TemaEcoAccess.Px(this, 84);
            Controls.Add(new ImagenRedondeada { Imagen = RecursosEco.Eco, Size = new Size(lado, lado), Anchor = AnchorStyles.None, AccessibleName = "Eco", AccessibleRole = AccessibleRole.Graphic }, 0, 1);
            Controls.Add(_mensaje, 0, 2);
            SizeChanged += (_, _) => _mensaje.MaximumSize = new Size(Math.Max(200, ClientSize.Width - 40), 0);
        }

        public void Mostrar(string mensaje, bool error)
        {
            _mensaje.Text = mensaje;
            _mensaje.ForeColor = error ? TemaEcoAccess.Error : TemaEcoAccess.Suave;
            _mensaje.AccessibleName = mensaje;
        }
    }
}
