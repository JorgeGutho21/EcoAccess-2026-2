using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Depósito simplificado (ExpoSoftware): 1) elegir uno de los 15 residuos, 2) confirmar la caneca que EcoAccess
    /// identifica, 3) depositar y 4) resultado. La caneca es la de la estación del QR oficial que corresponde al residuo
    /// (blanca 1093, verde 1400 o negra 1401); si Oracle la tiene fuera de servicio o no admite el material, no se envía
    /// nada. Solo crea la solicitud y la orden mediante <see cref="ServicioInteracciones"/>; los hitos que se muestran son
    /// los eventos que la caneca (física o simulada) registra en Oracle. El contador usa la ventana del firmware
    /// (ABIERTO_MS = 7 s) desde que Oracle registra la apertura. Nunca acredita ecopuntos: eso exige verificación.
    /// </summary>
    public sealed class FormAsistenteDeposito : Form
    {
        private static readonly TimeSpan Espera = ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(20);

        private readonly ServiciosAplicacion _servicios;
        private readonly SesionActiva? _sesion;
        private readonly IndicadorPasos _pasos = new("Residuo", "Caneca", "Depositar", "Resultado");
        private readonly PanelDesplazable _contenido = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(32, 8, 32, 8) };
        private readonly KryptonButton _atras = new() { Text = "Atrás", AutoSize = true };
        private readonly KryptonButton _siguiente = new() { Text = "Continuar", AutoSize = true };
        private readonly Label _aviso = new() { AutoSize = true, ForeColor = TemaEcoAccess.Error, Margin = new Padding(0, 8, 0, 0) };
        private readonly LineaHitos _hitos = new();
        private readonly Label _contador = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 16), ForeColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(0, 0, 0, 10), Visible = false };
        private readonly System.Windows.Forms.Timer _reloj = new() { Interval = 250 };
        private readonly CancellationTokenSource _cierre = new();
        private string _textoBusqueda = "";
        private ResiduoPrototipo? _residuo;
        private DestinoPrototipo? _destino;
        private string? _problemaDestino;
        private int _cantidadDeclarada = 1;
        private int _paso;
        private int _idInteraccion;
        private string? _clave;
        private EstadoOrdenApertura _maximo;
        private DateTime? _aperturaVistaUtc;
        private bool _procesando;
        private bool _resolviendo;

        /// <summary>True si Oracle registró un depósito en esta operación.</summary>
        public bool DepositoRegistrado { get; private set; }

        /// <summary>El visitante pidió crear su cuenta desde el resultado.</summary>
        public bool SolicitarRegistro { get; private set; }

        public FormAsistenteDeposito(ServiciosAplicacion servicios, SesionActiva? sesion, ResiduoPrototipo? preseleccion)
        {
            _servicios = servicios;
            _sesion = sesion;
            _residuo = preseleccion;
            Text = "Depositar residuo · EcoAccess";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            MinimizeBox = false;
            Size = new Size(TemaEcoAccess.Px(this, 1100), TemaEcoAccess.Px(this, 760));
            MinimumSize = new Size(TemaEcoAccess.Px(this, 760), TemaEcoAccess.Px(this, 560));

            var cabecera = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(32, 20, 32, 6) };
            cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cabecera.Controls.Add(new Label { Text = "Depositar residuo", AutoSize = true, Font = TemaEcoAccess.Titulo, Margin = Padding.Empty });
            cabecera.Controls.Add(new Label
            {
                Text = sesion is null
                    ? "Como invitado puedes depositar, pero no suma ecopuntos."
                    : $"Participas como {sesion.Nombre}. Los ecopuntos se acreditan solo cuando se verifica el depósito.",
                AutoSize = true,
                ForeColor = TemaEcoAccess.Suave,
                Margin = new Padding(0, 4, 0, 14),
            });
            _pasos.Height = TemaEcoAccess.Px(this, 66);
            _pasos.Dock = DockStyle.Top;
            _pasos.Margin = Padding.Empty;
            cabecera.Controls.Add(_pasos);

            var pie = new Panel { Dock = DockStyle.Bottom, Height = TemaEcoAccess.Px(this, 76), BackColor = TemaEcoAccess.Superficie, Padding = new Padding(32, 14, 32, 14) };
            var botones = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = false, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            TemaEcoAccess.EstilizarAccion(_atras);
            TemaEcoAccess.EstilizarAccion(_siguiente, principal: true);
            _atras.Margin = new Padding(0, 0, 10, 0);
            _siguiente.Margin = Padding.Empty;
            botones.Controls.Add(_atras);
            botones.Controls.Add(_siguiente);
            // El ancho del panel se calcula con los botones visibles y su texto actual: antes quedaba con el ancho de
            // «Continuar» y, al aparecer «Cambiar residuo», el botón «Depositar» se cortaba a la derecha.
            void Medir()
            {
                foreach (KryptonButton b in new[] { _atras, _siguiente })
                    b.Size = new Size(TextRenderer.MeasureText(b.Text, TemaEcoAccess.Subtitulo).Width + TemaEcoAccess.Px(this, 44), TemaEcoAccess.Px(this, 46));
                botones.Width = new[] { _atras, _siguiente }.Where(b => b.Visible || !IsHandleCreated).Sum(b => b.Width + b.Margin.Horizontal) + TemaEcoAccess.Px(this, 4);
                pie.PerformLayout();
            }
            foreach (KryptonButton boton in new[] { _atras, _siguiente })
            {
                boton.AutoSize = false;
                boton.TextChanged += (_, _) => Medir();
                boton.VisibleChanged += (_, _) => Medir();
            }
            Medir();
            _aviso.Dock = DockStyle.Fill;
            _aviso.AutoSize = false;
            _aviso.TextAlign = ContentAlignment.MiddleLeft;
            pie.Controls.Add(_aviso);
            pie.Controls.Add(botones);
            pie.Paint += (_, e) => { using var borde = new Pen(TemaEcoAccess.Borde); e.Graphics.DrawLine(borde, 0, 0, pie.Width, 0); };

            Controls.Add(_contenido);
            Controls.Add(pie);
            Controls.Add(cabecera);
            _atras.Click += (_, _) => { if (_paso is 1) MostrarPaso(0); };
            _siguiente.Click += async (_, _) => await AvanzarAsync();
            _contenido.ClientSizeChanged += (_, _) => AjustarContenido();
            _reloj.Tick += (_, _) => ActualizarContador();
            KeyPreview = true;
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape && !_procesando) Close(); };
            Shown += async (_, _) =>
            {
                if (_residuo is null) MostrarPaso(0);
                else await ElegirYResolverAsync(_residuo);
            };
            FormClosing += (_, _) => { _cierre.Cancel(); _reloj.Stop(); };
        }

        private void MostrarPaso(int paso)
        {
            _paso = paso;
            _pasos.Actual = paso;
            _aviso.Text = "";
            _contenido.SuspendLayout();
            LimpiarContenido();
            _contenido.Controls.Add(paso == 0 ? VistaResiduo() : VistaCaneca());
            _contenido.ResumeLayout(true);
            _atras.Visible = paso == 1;
            _atras.Text = "Cambiar residuo";
            _atras.Enabled = true;
            _siguiente.Visible = true;
            _siguiente.Enabled = paso == 0 ? _residuo is not null : _destino is not null;
            _siguiente.Text = paso == 0 ? "Continuar" : "Depositar";
            AjustarContenido();
            AcceptButton = _siguiente;
        }

        private void AjustarContenido()
        {
            int ancho = Math.Max(TemaEcoAccess.Px(this, 300), _contenido.ClientSize.Width - _contenido.Padding.Horizontal);
            foreach (Control vista in _contenido.Controls)
            {
                vista.MinimumSize = vista.MaximumSize = new Size(ancho, 0);
                vista.Location = new Point(_contenido.Padding.Left + _contenido.AutoScrollPosition.X, _contenido.Padding.Top + _contenido.AutoScrollPosition.Y);
                foreach (Control parte in vista.Controls)
                {
                    int disponible = Math.Max(TemaEcoAccess.Px(this, 200), ancho - parte.Margin.Horizontal - TemaEcoAccess.Px(this, 24));
                    if (parte is RejillaTarjetas rejilla) rejilla.Adaptar(disponible);
                    else parte.MinimumSize = parte.MaximumSize = new Size(disponible, 0);
                }
            }
        }

        private void LimpiarContenido()
        {
            _hitos.Parent?.Controls.Remove(_hitos);
            _contador.Parent?.Controls.Remove(_contador);
            foreach (Control anterior in _contenido.Controls.Cast<Control>().ToList()) anterior.Dispose();
        }

        // Paso 1: los 15 residuos del prototipo con su ilustración; el buscador filtra sin tildes y con sinónimos.
        private Control VistaResiduo()
        {
            var vista = Columna();
            vista.Controls.Add(Encabezado("¿Qué vas a depositar?", "Elige uno de los residuos del prototipo o búscalo por su nombre."));
            var fila = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, BackColor = TemaEcoAccess.Fondo, Margin = new Padding(0, 0, 0, 10) };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var buscar = new CajaBusqueda { Dock = DockStyle.Fill, Height = TemaEcoAccess.Px(this, 44), Margin = new Padding(0, 0, 10, 0), AccessibleName = "Buscar residuo" };
            buscar.Campo.Text = _textoBusqueda;
            buscar.Campo.MaxLength = 60;
            var botonBuscar = new KryptonButton { Text = "Buscar", AutoSize = true, MinimumSize = new Size(TemaEcoAccess.Px(this, 110), TemaEcoAccess.Px(this, 44)) };
            TemaEcoAccess.EstilizarAccion(botonBuscar, principal: true);
            fila.Controls.Add(buscar, 0, 0);
            fila.Controls.Add(botonBuscar, 1, 0);
            vista.Controls.Add(fila);
            BusquedaPrototipo resultado = CatalogoPrototipo.Consultar(_textoBusqueda);
            var estado = new Label { Text = resultado.Mensaje, AutoSize = true, ForeColor = resultado.FueraDeCatalogo ? TemaEcoAccess.Advertencia : TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 10) };
            vista.Controls.Add(estado);
            var rejilla = new RejillaTarjetas(150) { ColumnasMaximas = 5 };
            foreach (ResiduoPrototipo r in resultado.Residuos)
            {
                var opcion = new TarjetaResiduo(r) { Seleccionada = r == _residuo };
                opcion.Click += (_, _) =>
                {
                    _residuo = r;
                    _destino = null;
                    foreach (TarjetaResiduo otra in rejilla.Controls.OfType<TarjetaResiduo>()) otra.Seleccionada = otra == opcion;
                    _siguiente.Enabled = true;
                };
                opcion.DoubleClick += async (_, _) => await ElegirYResolverAsync(r);
                rejilla.Controls.Add(opcion);
            }
            vista.Controls.Add(rejilla);
            void Buscar() { _textoBusqueda = buscar.Campo.Text; _residuo = null; MostrarPaso(0); }
            botonBuscar.Click += (_, _) => Buscar();
            buscar.Campo.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Buscar(); } };
            return vista;
        }

        // Paso 2: EcoAccess identifica la caneca y muestra su estado en Oracle; solo continúa si puede recibir el residuo.
        private Control VistaCaneca()
        {
            ResiduoPrototipo r = _residuo!;
            var vista = Columna();
            vista.Controls.Add(Encabezado($"Va en la {CatalogoPrototipo.NombreCaneca(r.Caneca).ToLowerInvariant()}", "EcoAccess identificó la caneca que corresponde a tu residuo."));
            var tarjeta = Tarjeta();
            var interior = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, BackColor = TemaEcoAccess.Superficie };
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 170)));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 120)));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            interior.Controls.Add(new ImagenAjustada { Imagen = RecursosEco.Residuo(r), Size = new Size(TemaEcoAccess.Px(this, 160), TemaEcoAccess.Px(this, 160)), Margin = new Padding(0, 0, 10, 0), AccessibleName = r.Nombre }, 0, 0);
            interior.Controls.Add(new DibujoCanecaControl(r.Caneca) { Size = new Size(TemaEcoAccess.Px(this, 100), TemaEcoAccess.Px(this, 130)), Anchor = AnchorStyles.None }, 1, 0);
            var textos = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Dock = DockStyle.Fill, Margin = new Padding(16, 0, 0, 0) };
            var instrucciones = new Label { Text = r.Instrucciones, AutoSize = true, Font = TemaEcoAccess.Entrada, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 6, 0, 8) };
            textos.Controls.Add(new Label { Text = r.Nombre, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, Margin = Padding.Empty });
            textos.Controls.Add(instrucciones);
            string estado = _destino is not null
                ? $"{_destino.Caneca.Nombre} · en servicio en Oracle."
                : _problemaDestino ?? "Consultando la caneca en Oracle…";
            textos.Controls.Add(new Label { Text = estado, AutoSize = true, ForeColor = _destino is not null ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Error, Font = TemaEcoAccess.Subtitulo, Margin = new Padding(0, 0, 0, 6) });
            textos.Controls.Add(new Label
            {
                Text = "Al depositar se envía una orden única a esta caneca. La tapa se abre 7 segundos (ventana del controlador) y se cierra sola. " +
                       "Si la caneca no confirma la orden, no se registra ningún depósito.",
                AutoSize = true,
                ForeColor = TemaEcoAccess.Suave,
                Margin = Padding.Empty,
            });
            textos.SizeChanged += (_, _) => { foreach (Label l in textos.Controls.OfType<Label>()) l.MaximumSize = new Size(Math.Max(200, textos.Width - 8), 0); };
            interior.Controls.Add(textos, 2, 0);
            tarjeta.Controls.Add(interior);
            vista.Controls.Add(tarjeta);
            if (_sesion is not null && _destino is not null)
            {
                var fila = new FlowLayoutPanel { AutoSize = true, BackColor = TemaEcoAccess.Fondo, WrapContents = false, Margin = new Padding(0, 4, 0, 0) };
                fila.Controls.Add(new Label { Text = "Unidades que vas a depositar", AutoSize = true, Font = TemaEcoAccess.Subtitulo, Margin = new Padding(0, 6, 12, 0) });
                var cantidad = new NumericUpDown { Minimum = 1, Maximum = 20, Value = _cantidadDeclarada, Width = TemaEcoAccess.Px(this, 80), AccessibleName = "Cantidad declarada, sin verificación física" };
                cantidad.ValueChanged += (_, _) => _cantidadDeclarada = (int)cantidad.Value;
                fila.Controls.Add(cantidad);
                vista.Controls.Add(fila);
            }
            if (_destino is null && _problemaDestino is not null) _aviso.Text = _problemaDestino;
            return vista;
        }

        /// <summary>Pasa al paso 2 resolviendo en Oracle la caneca y el material de ese residuo.</summary>
        private async Task ElegirYResolverAsync(ResiduoPrototipo residuo)
        {
            if (_resolviendo || _procesando) return;
            _resolviendo = true;
            _residuo = residuo;
            _destino = null;
            _problemaDestino = null;
            MostrarPaso(1);
            _siguiente.Enabled = false;
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
                limite.CancelAfter(TimeSpan.FromSeconds(20));
                var destino = await _servicios.CatalogoAmbiental.ResolverPrototipoAsync(residuo, limite.Token).WaitAsync(limite.Token);
                if (IsDisposed) return;
                if (destino.Exitoso) _destino = destino.Valor;
                else _problemaDestino = destino.Mensaje ?? "La caneca no está disponible.";
            }
            catch (Exception) when (!_cierre.IsCancellationRequested && !IsDisposed)
            {
                _problemaDestino = "No pudimos consultar la caneca en Oracle. Revisa la conexión e inténtalo de nuevo.";
            }
            catch (Exception) when (_cierre.IsCancellationRequested || IsDisposed) { return; }
            finally { _resolviendo = false; }
            MostrarPaso(1);
        }

        private async Task AvanzarAsync()
        {
            if (_procesando || _resolviendo) return;
            if (_paso == 0 && _residuo is not null) await ElegirYResolverAsync(_residuo);
            else if (_paso == 1 && _destino is not null) await ProcesarAsync();
            else if (_paso >= 2 && !_procesando) Close();
        }

        // Paso 3: crea la solicitud y la orden; luego sigue los hechos que la caneca registra en Oracle.
        private async Task ProcesarAsync()
        {
            _procesando = true;
            _paso = 2;
            _pasos.Actual = 2;
            _atras.Visible = false;
            _siguiente.Enabled = false;
            _siguiente.Text = "Procesando…";
            _aviso.Text = "";
            _contenido.SuspendLayout();
            LimpiarContenido();
            var vista = Columna();
            vista.Controls.Add(Encabezado("Siguiendo tu depósito", "Cada paso se marca solo cuando la caneca lo confirma en Oracle."));
            vista.Controls.Add(_contador);
            var tarjeta = Tarjeta();
            tarjeta.Controls.Add(_hitos);
            _hitos.Dock = DockStyle.Top;
            vista.Controls.Add(tarjeta);
            _contenido.Controls.Add(vista);
            _contenido.ResumeLayout(true);
            AjustarContenido();
            _maximo = 0;
            _aperturaVistaUtc = null;
            MostrarHitos(null, null);

            try
            {
                // La misma clave se reutiliza si hay que reintentar: Oracle nunca emite dos órdenes por ella.
                _clave ??= "app-" + Guid.NewGuid().ToString("N");
                int caneca = _destino!.Caneca.Id;
                if (_idInteraccion == 0)
                {
                    var inicio = _sesion is null
                        ? await _servicios.Interacciones.IniciarVisitanteAsync(caneca, _cierre.Token)
                        : await _servicios.Interacciones.IniciarConCantidadAsync(_sesion.IdUsuario, caneca, CanalInteraccion.Simulador, _cantidadDeclarada, _cierre.Token);
                    if (!inicio.Exitoso) { Terminar(false, "No se pudo registrar la solicitud", inicio.Mensaje ?? "Oracle rechazó la solicitud."); return; }
                    _idInteraccion = inicio.Valor;
                }
                var orden = _sesion is null
                    ? await _servicios.Interacciones.AutorizarAperturaVisitanteAsync(_idInteraccion, _destino.Material.Id, _clave, _cierre.Token)
                    : await _servicios.Interacciones.AutorizarAperturaAsync(_sesion.IdUsuario, _idInteraccion, _destino.Material.Id, _clave, _cierre.Token);
                if (!orden.Exitoso) { Terminar(false, "La caneca no pudo recibir la orden", orden.Mensaje ?? "Oracle rechazó la orden."); return; }
                await SeguirAsync();
            }
            catch (Exception) when (!_cierre.IsCancellationRequested && !IsDisposed)
            {
                Terminar(false, "No se pudo completar la operación", "Puede deberse a la conexión con Oracle. Si la orden quedó registrada, reintentar usa la misma clave y no crea otra orden.", reintentar: true);
            }
            catch (Exception) when (_cierre.IsCancellationRequested || IsDisposed) { }
        }

        private async Task SeguirAsync()
        {
            DateTime limite = DateTime.UtcNow + Espera;
            while (!_cierre.IsCancellationRequested && DateTime.UtcNow < limite)
            {
                var consulta = _sesion is null
                    ? await _servicios.Interacciones.ConsultarOperacionVisitanteAsync(_idInteraccion, _clave!, _cierre.Token)
                    : await _servicios.Interacciones.ConsultarOperacionAsync(_sesion.IdUsuario, _idInteraccion, _clave!, _cierre.Token);
                if (IsDisposed) return;
                if (consulta.Exitoso)
                {
                    OperacionApertura operacion = consulta.Valor;
                    MostrarHitos(operacion.Orden, operacion.Deposito);
                    if (operacion.Deposito is not null && operacion.Orden.Estado is EstadoOrdenApertura.Cerrada or EstadoOrdenApertura.Detectada)
                    {
                        // Con depósito registrado se espera el cierre para confirmarlo; si ya cerró, se termina.
                        if (operacion.Orden.Estado == EstadoOrdenApertura.Cerrada || DateTime.UtcNow - (_aperturaVistaUtc ?? DateTime.UtcNow) > TimeSpan.FromSeconds(MensajesTelegram.VentanaDepositoSegundos + 5))
                        { await MostrarExitoAsync(operacion); return; }
                    }
                    else if (operacion.Deposito is not null) { await MostrarExitoAsync(operacion); return; }
                    switch (operacion.Orden.Estado)
                    {
                        case EstadoOrdenApertura.Vencida:
                            Terminar(false, "La caneca no respondió", "La orden venció sin confirmación de la caneca. No se registró ningún depósito.");
                            return;
                        case EstadoOrdenApertura.Fallida:
                            Terminar(false, "La caneca informó un fallo", "No se registró ningún depósito. Puedes intentarlo de nuevo.");
                            return;
                        case EstadoOrdenApertura.Cerrada when operacion.Deposito is null:
                            Terminar(false, "La caneca se cerró sin detectar un residuo", "No se registró ningún depósito.");
                            return;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1), _cierre.Token);
            }
            if (!_cierre.IsCancellationRequested)
                Terminar(false, "Sin confirmación de la caneca", "La caneca no confirmó la orden a tiempo: puede estar desconectada o su servicio detenido. La orden vence sola y no se registra depósito.");
        }

        // Hitos según el estado más avanzado que Oracle registró; nada se marca antes de tiempo.
        private void MostrarHitos(OrdenApertura? orden, Deposito? deposito)
        {
            if (orden is not null && orden.Estado <= EstadoOrdenApertura.Cerrada && orden.Estado > _maximo) _maximo = orden.Estado;
            if (_maximo >= EstadoOrdenApertura.Abierta && _aperturaVistaUtc is null)
            {
                _aperturaVistaUtc = DateTime.UtcNow;
                _reloj.Start();
            }
            bool terminadaMal = orden?.Estado is EstadoOrdenApertura.Vencida or EstadoOrdenApertura.Fallida
                || (orden?.Estado == EstadoOrdenApertura.Cerrada && deposito is null);
            EstadoOrdenApertura alcanzado = _maximo;

            EstadoHito Hito(params EstadoOrdenApertura[] requeridos)
            {
                EstadoOrdenApertura requerido = requeridos.Min();
                if (alcanzado >= requerido || (requerido == EstadoOrdenApertura.Detectada && deposito is not null)) return EstadoHito.Hecho;
                bool siguiente = requerido == (alcanzado == 0 ? EstadoOrdenApertura.Emitida : alcanzado + 1);
                if (siguiente) return terminadaMal ? EstadoHito.Fallo : EstadoHito.EnCurso;
                return EstadoHito.Pendiente;
            }

            _hitos.Mostrar(new (string Titulo, string Detalle, EstadoHito Estado)[]
            {
                ("Solicitud enviada", orden is null ? "Registrando tu solicitud en Oracle…" : "Tu solicitud quedó registrada.", Hito(EstadoOrdenApertura.Emitida)),
                ("Preparando apertura", "La orden autorizada se entrega a la caneca y ella acusa recibo.", Hito(EstadoOrdenApertura.Acusada)),
                ("Caneca disponible para depósito", "La caneca confirmó que abrió la tapa.", Hito(EstadoOrdenApertura.Abierta)),
                ("Depósito registrado", deposito is null ? "Esperando el registro del depósito." : "Depósito registrado, pendiente de verificación.", deposito is not null ? EstadoHito.Hecho : Hito(EstadoOrdenApertura.Detectada)),
                ("Cierre confirmado", "La caneca informa que la tapa se cerró.", Hito(EstadoOrdenApertura.Cerrada)),
            });
            AjustarContenido();
        }

        // Contador de la ventana: desde que Oracle registra la apertura, 7 s del controlador (aproximado al segundo).
        private void ActualizarContador()
        {
            if (_aperturaVistaUtc is not { } inicio || _contador.IsDisposed) { _reloj.Stop(); return; }
            int quedan = MensajesTelegram.VentanaDepositoSegundos - (int)Math.Floor((DateTime.UtcNow - inicio).TotalSeconds);
            _contador.Visible = true;
            if (_maximo >= EstadoOrdenApertura.Cerrada || quedan <= 0)
            {
                _contador.Text = _maximo >= EstadoOrdenApertura.Cerrada ? "La tapa se cerró." : "La tapa se está cerrando…";
                if (_maximo >= EstadoOrdenApertura.Cerrada) _reloj.Stop();
                return;
            }
            _contador.Text = $"Deposita ahora: quedan unos {quedan} s";
        }

        // Paso 4: resultado real. Al ciudadano se le muestra su saldo actual, sin sumar puntos pendientes.
        private async Task MostrarExitoAsync(OperacionApertura operacion)
        {
            DepositoRegistrado = true;
            string detalle;
            if (_sesion is null)
                detalle = "Tu depósito quedó registrado. Crea tu cuenta para sumar ecopuntos en tus próximos depósitos verificados.";
            else
            {
                string saldo = "—";
                try
                {
                    var resumen = await _servicios.Ecopuntos.ConsultarMiosAsync(_sesion.IdUsuario, _cierre.Token);
                    if (resumen.Exitoso) saldo = resumen.Valor.Saldo.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-CO"));
                }
                catch (Exception) when (!_cierre.IsCancellationRequested) { }
                detalle = $"Tu depósito está pendiente de verificación. Los ecopuntos se acreditan cuando un operario o administrador lo verifica. Saldo verificado actual: {saldo} ecopuntos.";
            }
            Terminar(true, "¡Gracias por cuidar el planeta!", detalle);
        }

        private void Terminar(bool exito, string titulo, string detalle, bool reintentar = false)
        {
            if (IsDisposed) return;
            _procesando = false;
            _reloj.Stop();
            _paso = 3;
            _pasos.Actual = 3;
            var vista = Columna();
            var tarjeta = Tarjeta();
            var interior = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = TemaEcoAccess.Superficie };
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int lado = TemaEcoAccess.Px(this, 130);
            interior.Controls.Add(exito
                ? new ImagenAjustada { Imagen = RecursosEco.Imagen("eco/eco_celebra.png"), Size = new Size(lado, lado), Margin = new Padding(0, 0, 20, 0), AccessibleName = "ECO celebra" }
                : new TarjetaIcono(IconChar.CircleExclamation) { Size = new Size(lado / 2, lado / 2), Margin = new Padding(0, 0, 20, 0) }, 0, 0);
            var textos = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.Superficie };
            var etiquetaDetalle = new Label { Text = detalle, AutoSize = true, Font = TemaEcoAccess.Entrada, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 8, 0, 0) };
            textos.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = exito ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Error, Margin = Padding.Empty });
            textos.Controls.Add(etiquetaDetalle);
            textos.SizeChanged += (_, _) => etiquetaDetalle.MaximumSize = new Size(Math.Max(200, textos.Width - 10), 0);
            interior.Controls.Add(textos, 1, 0);
            tarjeta.Controls.Add(interior);
            vista.Controls.Add(tarjeta);
            bool hayProgreso = _hitos.Parent is not null;
            _contenido.SuspendLayout();
            LimpiarContenido();
            if (hayProgreso)
            {
                var progreso = Tarjeta();
                progreso.Controls.Add(_hitos);
                vista.Controls.Add(progreso);
            }
            _contenido.Controls.Add(vista);
            _contenido.ResumeLayout(true);
            AjustarContenido();

            _atras.Visible = true;
            _atras.Enabled = true;
            _siguiente.Enabled = true;
            _siguiente.Text = "Finalizar";
            if (exito && _sesion is null)
            {
                _atras.Text = "Crear mi cuenta";
                ReemplazarClick(_atras, () => { SolicitarRegistro = true; Close(); });
            }
            else if (!exito)
            {
                _atras.Text = reintentar ? "Reintentar" : "Intentar de nuevo";
                ReemplazarClick(_atras, async () =>
                {
                    if (!reintentar) { _idInteraccion = 0; _clave = null; }
                    await ProcesarAsync();
                });
            }
            else _atras.Visible = false;
        }

        private Action? _accionAtras;

        private void ReemplazarClick(KryptonButton boton, Action accion)
        {
            if (_accionAtras is null) boton.Click += (_, _) => { if (_paso == 3) _accionAtras?.Invoke(); };
            _accionAtras = accion;
        }

        private static TableLayoutPanel Columna() =>
            new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo, Location = Point.Empty, Margin = Padding.Empty };

        private static Control Encabezado(string titulo, string texto)
        {
            var bloque = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.Fondo, Margin = new Padding(0, 8, 0, 14) };
            bloque.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, Margin = Padding.Empty });
            if (texto.Length > 0) bloque.Controls.Add(new Label { Text = texto, AutoSize = true, MaximumSize = new Size(900, 0), ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 4, 0, 0) });
            return bloque;
        }

        private static TarjetaEco Tarjeta() =>
            new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 12) };

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed) { _cierre.Cancel(); _cierre.Dispose(); _hitos.Dispose(); _reloj.Dispose(); _contador.Dispose(); }
            base.Dispose(disposing);
        }
    }

    /// <summary>Caneca dibujada como control (paso 2 del depósito).</summary>
    internal sealed class DibujoCanecaControl : Control
    {
        private readonly CategoriaMaterial _caneca;

        public DibujoCanecaControl(CategoriaMaterial caneca)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            _caneca = caneca;
            BackColor = Color.Transparent;
            AccessibleRole = AccessibleRole.Graphic;
            AccessibleName = CatalogoPrototipo.NombreCaneca(caneca);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DibujoCaneca.Pintar(e.Graphics, new RectangleF(2, 2, Width - 4, Height - 4), _caneca);
        }
    }
}
