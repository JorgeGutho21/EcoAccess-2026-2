using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Asistente de depósito en la caneca DEMO: residuo, caneca, confirmación, proceso y resultado. Solo crea la
    /// solicitud y la orden en Oracle mediante <see cref="ServicioInteracciones"/>; el bridge local entrega la orden a la
    /// caneca simulada y sus eventos (acuse, apertura, detección) son los que el asistente muestra. Nunca da por hecho
    /// un paso que Oracle no registró, ni acredita ecopuntos: eso ocurre solo cuando un administrador verifica.
    /// </summary>
    public sealed class FormAsistenteDeposito : Form
    {
        private static readonly TimeSpan Espera = ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(20);

        private readonly ServiciosAplicacion _servicios;
        private readonly SesionActiva? _sesion;
        private readonly IndicadorPasos _pasos = new("Residuo", "Caneca", "Confirmar", "Proceso", "Resultado");
        private readonly PanelDesplazable _contenido = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(32, 8, 32, 8) };
        private readonly KryptonButton _atras = new() { Text = "Atrás", AutoSize = true };
        private readonly KryptonButton _siguiente = new() { Text = "Continuar", AutoSize = true };
        private readonly Label _aviso = new() { AutoSize = true, ForeColor = TemaEcoAccess.Error, Margin = new Padding(0, 8, 0, 0) };
        private readonly LineaHitos _hitos = new();
        private readonly CancellationTokenSource _cierre = new();
        private CanecaPublica? _demo;
        private IReadOnlyList<Material> _catalogo = Array.Empty<Material>();
        private int _paginaSeleccion = 1;
        private string _textoSeleccion = "";
        private bool _hayMasSeleccion;
        private bool _cargandoSeleccion;
        private Material? _material;
        private int _cantidadDeclarada = 1;
        private int _paso;
        private int _idInteraccion;
        private string? _clave;
        private EstadoOrdenApertura _maximo;
        private bool _procesando;

        /// <summary>True si Oracle registró un depósito en esta operación.</summary>
        public bool DepositoRegistrado { get; private set; }

        /// <summary>El visitante pidió crear su cuenta desde el resultado.</summary>
        public bool SolicitarRegistro { get; private set; }

        public FormAsistenteDeposito(ServiciosAplicacion servicios, SesionActiva? sesion, Material? preseleccion)
        {
            _servicios = servicios;
            _sesion = sesion;
            _material = preseleccion;
            Text = "Depositar residuo · EcoAccess";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            MinimizeBox = false;
            Size = new Size(TemaEcoAccess.Px(this, 1000), TemaEcoAccess.Px(this, 740));
            MinimumSize = new Size(TemaEcoAccess.Px(this, 720), TemaEcoAccess.Px(this, 560));

            var cabecera = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(32, 24, 32, 8) };
            cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cabecera.Controls.Add(new Label { Text = "Depositar residuo", AutoSize = true, Font = TemaEcoAccess.Titulo, Margin = Padding.Empty });
            cabecera.Controls.Add(new Label
            {
                Text = sesion is null
                    ? "Como invitado puedes probar la simulación, sin sumar ecopuntos."
                    : $"Participas como {sesion.Nombre}. Registrar una solicitud no acredita puntos; se requieren verificación y tarifa vigente.",
                AutoSize = true,
                ForeColor = TemaEcoAccess.Suave,
                Margin = new Padding(0, 4, 0, 18),
            });
            _pasos.Height = TemaEcoAccess.Px(this, 66);
            _pasos.Dock = DockStyle.Top;
            _pasos.Margin = Padding.Empty;
            cabecera.Controls.Add(_pasos);

            // Pie: aviso a la izquierda y acciones a la derecha. El panel de flujo se reacomoda solo cuando un botón
            // cambia de texto («Continuar», «Procesando…», «Finalizar»), sin recortarlo.
            var pie = new Panel { Dock = DockStyle.Bottom, Height = TemaEcoAccess.Px(this, 76), BackColor = TemaEcoAccess.Superficie, Padding = new Padding(32, 14, 32, 14) };
            var botones = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            TemaEcoAccess.EstilizarAccion(_atras);
            TemaEcoAccess.EstilizarAccion(_siguiente, principal: true);
            _atras.Margin = new Padding(0, 0, 10, 0);
            _siguiente.Margin = Padding.Empty;
            // Tamaño calculado a partir del texto: el ajuste automático de Krypton no se recalcula al cambiarlo.
            foreach (KryptonButton boton in new[] { _atras, _siguiente })
            {
                boton.AutoSize = false;
                void Medir() => boton.Size = new Size(TextRenderer.MeasureText(boton.Text, TemaEcoAccess.Subtitulo).Width + TemaEcoAccess.Px(this, 44), TemaEcoAccess.Px(this, 46));
                boton.TextChanged += (_, _) => Medir();
                Medir();
            }
            botones.Controls.Add(_siguiente);
            botones.Controls.Add(_atras);
            _aviso.Dock = DockStyle.Fill;
            _aviso.AutoSize = false;
            _aviso.TextAlign = ContentAlignment.MiddleLeft;
            pie.Controls.Add(_aviso);
            pie.Controls.Add(botones);
            pie.Paint += (_, e) => { using var borde = new Pen(TemaEcoAccess.Borde); e.Graphics.DrawLine(borde, 0, 0, pie.Width, 0); };

            Controls.Add(_contenido);
            Controls.Add(pie);
            Controls.Add(cabecera);
            _atras.Click += (_, _) => { if (_paso > 0 && _paso < 3) MostrarPaso(_paso - 1); };
            _siguiente.Click += async (_, _) => await AvanzarAsync();
            _contenido.ClientSizeChanged += (_, _) => AjustarContenido();
            CancelButton = null;
            KeyPreview = true;
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape && !_procesando) Close(); };
            Shown += async (_, _) => await CargarAsync();
            FormClosing += (_, _) => _cierre.Cancel();
        }

        private async Task CargarAsync()
        {
            MostrarMensaje(IconChar.Spinner, "Consultando la caneca piloto…", null, false);
            _siguiente.Enabled = _atras.Enabled = false;
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
                limite.CancelAfter(TimeSpan.FromSeconds(20));
                var demo = await _servicios.CatalogoAmbiental.ConsultarPilotoAsync(limite.Token).WaitAsync(limite.Token);
                var pagina = await _servicios.CatalogoAmbiental.ConsultarPaginaPublicaAsync("", cancellationToken: limite.Token).WaitAsync(limite.Token);
                if (!pagina.Exitoso) throw new InvalidOperationException("No se pudo consultar la página pública.");
                _catalogo = pagina.Valor.Filas;
                _hayMasSeleccion = pagina.Valor.HaySiguiente;
                if (IsDisposed) return;
                if (!demo.Exitoso)
                {
                    MostrarMensaje(IconChar.CircleInfo, "La caneca piloto no está disponible.",
                        "Puedes consultar la guía ambiental y volver a intentarlo más tarde.", true);
                    return;
                }
                _demo = demo.Valor;
                if (_material is not null && !_demo.Admitidos.Any(m => m.Id == _material.Id)) _material = null;
                MostrarPaso(_material is null ? 0 : 1);
            }
            catch (Exception) when (!_cierre.IsCancellationRequested && !IsDisposed)
            {
                MostrarMensaje(IconChar.PlugCircleXmark, "El servicio no está disponible.", "Vuelve a abrir el asistente en unos segundos.", true);
            }
            catch (Exception) when (_cierre.IsCancellationRequested || IsDisposed) { }
        }

        private void MostrarPaso(int paso)
        {
            _paso = paso;
            _pasos.Actual = paso;
            _aviso.Text = "";
            _contenido.SuspendLayout();
            LimpiarContenido();
            Control vista = paso switch
            {
                0 => VistaResiduo(),
                1 => VistaCaneca(),
                _ => VistaConfirmar(),
            };
            _contenido.Controls.Add(vista);
            _contenido.ResumeLayout(true);
            _atras.Visible = paso is 1 or 2;
            _atras.Enabled = true;
            _siguiente.Visible = true;
            _siguiente.Enabled = paso switch { 0 => _material is not null, 1 => _demo!.Caneca.EstaEnServicio, _ => true };
            _siguiente.Text = paso == 2 ? "Confirmar y abrir caneca" : "Continuar";
            AjustarContenido();
            AcceptButton = _siguiente;
        }

        // Todo el contenido ocupa el ancho disponible; las rejillas reparten columnas y las tarjetas crecen en alto.
        private void AjustarContenido()
        {
            int ancho = Math.Max(TemaEcoAccess.Px(this, 300), _contenido.ClientSize.Width - _contenido.Padding.Horizontal);
            foreach (Control vista in _contenido.Controls)
            {
                vista.MinimumSize = vista.MaximumSize = new Size(ancho, 0);
                vista.Location = new Point(_contenido.Padding.Left + _contenido.AutoScrollPosition.X, _contenido.Padding.Top + _contenido.AutoScrollPosition.Y);
                foreach (Control parte in vista.Controls)
                {
                    if (parte is RejillaTarjetas rejilla) rejilla.Adaptar(ancho);
                    else parte.MinimumSize = parte.MaximumSize = new Size(ancho, 0);
                }
            }
        }

        /// <summary>Vacía el área de contenido conservando la línea de hitos, que se reutiliza entre vistas.</summary>
        private void LimpiarContenido()
        {
            _hitos.Parent?.Controls.Remove(_hitos);
            foreach (Control anterior in _contenido.Controls.Cast<Control>().ToList()) anterior.Dispose();
        }

        // Paso 1: tarjetas de material. Solo se pueden elegir los que la caneca DEMO recibe según Oracle.
        private Control VistaResiduo()
        {
            var vista = Columna();
            vista.Controls.Add(Encabezado("¿Qué vas a depositar?", "Elige entre los materiales admitidos. Los demás muestran orientación sin permitir continuar el depósito."));
            var buscar = new TextBox { Text = _textoSeleccion, MaxLength = 100, Width = 500, AccessibleName = "Buscar residuo por nombre o sinónimo" };
            var botonBuscar = new Button { Text = "Buscar residuo", AutoSize = true };
            TemaEcoAccess.EstilizarBoton(botonBuscar);
            botonBuscar.Click += async (_, _) => await CargarSeleccionAsync(buscar.Text, 1);
            buscar.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await CargarSeleccionAsync(buscar.Text, 1); } };
            vista.Controls.Add(buscar); vista.Controls.Add(botonBuscar);
            var rejilla = new RejillaTarjetas(200) { ColumnasMaximas = 4 };
            var disponibles = _demo!.Admitidos;
            foreach (Material material in _catalogo.Take(ServicioCatalogoAmbiental.TamanoPaginaPublica))
            {
                bool disponible = disponibles.Any(d => d.Id == material.Id);
                var opcion = new TarjetaMaterialOpcion(material, disponible) { Seleccionada = _material?.Id == material.Id };
                opcion.Click += (_, _) =>
                {
                    if (!disponible) return;
                    _material = material;
                    foreach (TarjetaMaterialOpcion otra in rejilla.Controls.OfType<TarjetaMaterialOpcion>()) otra.Seleccionada = otra == opcion;
                    _siguiente.Enabled = true;
                };
                opcion.DoubleClick += async (_, _) => { if (disponible) await AvanzarAsync(); };
                rejilla.Controls.Add(opcion);
            }
            vista.Controls.Add(rejilla);
            var paginas = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            var anterior = new Button { Text = "Anterior", AutoSize = true, Enabled = _paginaSeleccion > 1 };
            var siguiente = new Button { Text = "Siguiente", AutoSize = true, Enabled = _hayMasSeleccion };
            anterior.Click += async (_, _) => await CargarSeleccionAsync(_textoSeleccion, _paginaSeleccion - 1);
            siguiente.Click += async (_, _) => await CargarSeleccionAsync(_textoSeleccion, _paginaSeleccion + 1);
            paginas.Controls.AddRange(new Control[] { anterior, new Label { Text = $"Página {_paginaSeleccion} · {_catalogo.Count} resultados", AutoSize = true }, siguiente });
            vista.Controls.Add(paginas);
            return vista;
        }

        private async Task CargarSeleccionAsync(string texto, int pagina)
        {
            if (_cargandoSeleccion || _procesando || _paso != 0) return;
            _cargandoSeleccion = true;
            _siguiente.Enabled = _atras.Enabled = false;
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
                limite.CancelAfter(TimeSpan.FromSeconds(20));
                var resultado = await _servicios.CatalogoAmbiental.ConsultarPaginaPublicaAsync(texto, pagina, cancellationToken: limite.Token);
                if (IsDisposed) return;
                if (!resultado.Exitoso) { _aviso.Text = resultado.Mensaje; return; }
                _paginaSeleccion = resultado.Valor.Pagina; _textoSeleccion = texto;
                _catalogo = resultado.Valor.Filas; _hayMasSeleccion = resultado.Valor.HaySiguiente;
                _material = null;
                MostrarPaso(0);
            }
            catch (OperationCanceledException) when (_cierre.IsCancellationRequested) { }
            catch (Exception) { if (!IsDisposed) _aviso.Text = "No pudimos consultar los materiales. Intenta de nuevo."; }
            finally { _cargandoSeleccion = false; if (!IsDisposed) { _atras.Enabled = true; _siguiente.Enabled = _material is not null; } }
        }

        // Paso 2: la caneca tal como está en Oracle; no se inventan otras canecas.
        private Control VistaCaneca()
        {
            Contenedor caneca = _demo!.Caneca;
            var vista = Columna();
            vista.Controls.Add(Encabezado(ServicioCatalogoAmbiental.NombreCanecaPiloto, "Una sola caneca y un compartimiento. Por ahora recibe cualquier residuo."));
            var tarjeta = Tarjeta();
            var filas = Filas(tarjeta);
            Fila(filas, "Nombre", ServicioCatalogoAmbiental.NombreCanecaPiloto);
            Fila(filas, "Estado", caneca.EstaEnServicio ? "En servicio" : "Fuera de servicio", caneca.EstaEnServicio ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Error);
            Fila(filas, "Materiales permitidos", "Cualquier residuo");
            Fila(filas, "Tipo", "Simulación por software, sin confirmación física de apertura o ingreso.");
            vista.Controls.Add(tarjeta);
            if (!caneca.EstaEnServicio) _aviso.Text = "La caneca no está en servicio; no se puede continuar.";
            return vista;
        }

        // Paso 3: resumen antes de crear nada en Oracle.
        private Control VistaConfirmar()
        {
            var vista = Columna();
            vista.Controls.Add(Encabezado("Confirma tu depósito", "Al confirmar se crea una orden única de apertura con vigencia de dos minutos."));
            var tarjeta = Tarjeta();
            var filas = Filas(tarjeta);
            Fila(filas, "Material", $"{_material!.Nombre} · {GuiaSeparacion.Para(_material.Categoria).Nombre}");
            Fila(filas, "Cómo entregarlo", GuiaSeparacion.Orientacion(_material));
            Fila(filas, "Caneca", ServicioCatalogoAmbiental.NombreCanecaPiloto);
            Fila(filas, "Tipo de operación", "Simulación de software");
            Fila(filas, "Usuario", _sesion is null ? "Invitado · este depósito no suma ecopuntos" : $"{_sesion.Nombre} · ciudadano registrado");
            vista.Controls.Add(tarjeta);
            if (_sesion is not null)
            {
                vista.Controls.Add(new Label { AutoSize = true, Text = "Cantidad declarada (unidades)", Font = TemaEcoAccess.Subtitulo });
                var cantidad = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 1000,
                    Value = _cantidadDeclarada,
                    Width = 150,
                    AccessibleName = "Cantidad declarada, sin verificación física"
                };
                var potenciales = new Label
                {
                    AutoSize = true,
                    MaximumSize = new Size(850, 0),
                    ForeColor = TemaEcoAccess.Suave,
                    Text = "Consulta la tarifa vigente para ver los puntos potenciales. Acreditados: 0."
                };
                cantidad.ValueChanged += (_, _) => { _cantidadDeclarada = (int)cantidad.Value; potenciales.Text = "Cantidad modificada. Consulta de nuevo los puntos potenciales. Acreditados: 0."; };
                vista.Controls.Add(cantidad);
                var cotizar = new Button { Text = "Consultar puntos potenciales", AutoSize = true };
                TemaEcoAccess.EstilizarBoton(cotizar);
                cotizar.Click += async (_, _) =>
                {
                    cotizar.Enabled = cantidad.Enabled = false;
                    try
                    {
                        var respuesta = await _servicios.Ecopuntos.ConsultarPotencialesAsync(_sesion.IdUsuario, _material!.Id, _cantidadDeclarada, _cierre.Token);
                        if (potenciales.IsDisposed) return;
                        potenciales.Text = respuesta.Exitoso
                            ? $"Tarifa: {respuesta.Valor.Tarifa} puntos por {(respuesta.Valor.PorUnidad ? "unidad verificada" : "depósito verificado")}. " +
                              $"Potenciales: {respuesta.Valor.PuntosPotenciales}. Acreditados: 0. Estimación sujeta a verificación y tarifa vigente al acreditar."
                            : respuesta.Mensaje;
                    }
                    catch (OperationCanceledException) when (_cierre.IsCancellationRequested) { }
                    catch (Exception) { if (!potenciales.IsDisposed) potenciales.Text = "No pudimos consultar la tarifa. No se acreditaron puntos."; }
                    finally { if (!cotizar.IsDisposed) cotizar.Enabled = cantidad.Enabled = true; }
                };
                vista.Controls.Add(cotizar);
                vista.Controls.Add(potenciales);
            }
            vista.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = TemaEcoAccess.Suave,
                MaximumSize = new Size(850, 0),
                Text = "La cantidad es una declaración. Registrar esta solicitud acredita 0 ecopuntos. La verificación y las tarifas se consultan por separado."
            });
            return vista;
        }

        private async Task AvanzarAsync()
        {
            if (_procesando || _cargandoSeleccion) return;
            if (_paso == 0 && _material is not null) MostrarPaso(1);
            else if (_paso == 1) MostrarPaso(2);
            else if (_paso == 2) await ProcesarAsync();
            else if (_paso >= 3) Close();
        }

        // Paso 4: crea la solicitud y la orden; luego sigue los hechos que la caneca registra en Oracle.
        private async Task ProcesarAsync()
        {
            _procesando = true;
            _paso = 3;
            _pasos.Actual = 3;
            _atras.Visible = false;
            _siguiente.Enabled = false;
            _siguiente.Text = "Procesando…";
            _aviso.Text = "";
            _contenido.SuspendLayout();
            LimpiarContenido();
            var vista = Columna();
            vista.Controls.Add(Encabezado("Siguiendo tu operación", "Cada paso se marca únicamente al recibir su confirmación. Este recorrido es una simulación."));
            var tarjeta = Tarjeta();
            tarjeta.Controls.Add(_hitos);
            _hitos.Dock = DockStyle.Top;
            vista.Controls.Add(tarjeta);
            _contenido.Controls.Add(vista);
            _contenido.ResumeLayout(true);
            AjustarContenido();
            _maximo = 0;
            MostrarHitos(null, null);

            try
            {
                // La misma clave se reutiliza si hay que reintentar: Oracle nunca emite dos órdenes por ella.
                _clave ??= "app-" + Guid.NewGuid().ToString("N");
                if (_idInteraccion == 0)
                {
                    var inicio = _sesion is null
                        ? await _servicios.Interacciones.IniciarVisitanteAsync(_demo!.Caneca.Id, _cierre.Token)
                        : await _servicios.Interacciones.IniciarConCantidadAsync(_sesion.IdUsuario, _demo!.Caneca.Id, CanalInteraccion.Simulador, _cantidadDeclarada, _cierre.Token);
                    if (!inicio.Exitoso) { Terminar(false, "No se pudo registrar la solicitud", inicio.Mensaje ?? "Oracle rechazó la solicitud."); return; }
                    _idInteraccion = inicio.Valor;
                }
                var orden = _sesion is null
                    ? await _servicios.Interacciones.AutorizarAperturaVisitanteAsync(_idInteraccion, _material!.Id, _clave, _cierre.Token)
                    : await _servicios.Interacciones.AutorizarAperturaAsync(_sesion.IdUsuario, _idInteraccion, _material!.Id, _clave, _cierre.Token);
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
                    if (operacion.Deposito is not null) { await MostrarExitoAsync(operacion); return; }
                    switch (operacion.Orden.Estado)
                    {
                        case EstadoOrdenApertura.Vencida:
                            Terminar(false, "La orden venció", "La caneca no completó la operación en los dos minutos de vigencia. No se registró ningún depósito.");
                            return;
                        case EstadoOrdenApertura.Fallida:
                            Terminar(false, "La caneca informó un fallo", "No se registró ningún depósito. Puedes intentarlo de nuevo.");
                            return;
                        case EstadoOrdenApertura.Cerrada:
                            Terminar(false, "La caneca se cerró sin detectar un residuo", "No se registró ningún depósito.");
                            return;
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1), _cierre.Token);
            }
            if (!_cierre.IsCancellationRequested)
                Terminar(false, "Sin confirmación todavía", "La caneca no respondió a tiempo. Si el bridge local está detenido, la orden vencerá sin depósito.");
        }

        // Hitos de la operación según el estado más avanzado que Oracle registró; nada se marca antes de tiempo.
        private void MostrarHitos(OrdenApertura? orden, Deposito? deposito)
        {
            if (orden is not null && orden.Estado <= EstadoOrdenApertura.Detectada && orden.Estado > _maximo) _maximo = orden.Estado;
            bool terminadaMal = orden?.Estado is EstadoOrdenApertura.Vencida or EstadoOrdenApertura.Fallida
                || (orden?.Estado == EstadoOrdenApertura.Cerrada && deposito is null);
            EstadoOrdenApertura alcanzado = deposito is not null ? EstadoOrdenApertura.Detectada : _maximo;

            EstadoHito Hito(EstadoOrdenApertura requerido)
            {
                if (alcanzado >= requerido) return EstadoHito.Hecho;
                bool siguiente = requerido == (alcanzado == 0 ? EstadoOrdenApertura.Emitida : alcanzado + 1);
                if (siguiente) return terminadaMal ? EstadoHito.Fallo : EstadoHito.EnCurso;
                return EstadoHito.Pendiente;
            }

            _hitos.Mostrar(
            new (string Titulo, string Detalle, EstadoHito Estado)[]
    {
        ("Orden creada", orden is null ? "Registrando tu solicitud…" : "Tu solicitud quedó registrada.", Hito(EstadoOrdenApertura.Emitida)),
        ("Solicitud enviada a la caneca", "Esperando la entrega de la orden autorizada al simulador.", Hito(EstadoOrdenApertura.Enviada)),
        ("Recepción confirmada", "La caneca acusa recibo de la orden.", Hito(EstadoOrdenApertura.Acusada)),
        ("Apertura confirmada", "La caneca informa que abrió la tapa.", Hito(EstadoOrdenApertura.Abierta)),
        ("Depósito detectado", deposito is null ? "Esperando el evento de ingreso simulado." : "Depósito de simulación registrado.", Hito(EstadoOrdenApertura.Detectada)),
        ("Verificación", _sesion is null ? "Los invitados no suman ecopuntos." : "La verificación independiente y la tarifa vigente son necesarias para acreditar puntos.", deposito?.FechaVerificacionUtc is not null ? EstadoHito.Hecho : EstadoHito.Pendiente)
    });
            AjustarContenido();
        }

        // Paso 5: resultado real. Al ciudadano se le muestra su saldo actual, sin sumar puntos pendientes.
        private async Task MostrarExitoAsync(OperacionApertura operacion)
        {
            DepositoRegistrado = true;
            string detalle;
            if (_sesion is null)
            {
                detalle = "Tu depósito quedó registrado. Regístrate para comenzar a acumular ecopuntos en tus próximos depósitos verificados.";
            }
            else
            {
                string saldo = "—";
                try
                {
                    var resumen = await _servicios.Ecopuntos.ConsultarMiosAsync(_sesion.IdUsuario, _cierre.Token);
                    if (resumen.Exitoso) saldo = resumen.Valor.Saldo.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("es-CO"));
                }
                catch (Exception) when (!_cierre.IsCancellationRequested) { }
                detalle = $"El depósito de simulación está pendiente de verificación. Los puntos requieren verificación independiente y una tarifa vigente. Saldo actual: {saldo} ecopuntos.";
            }
            Terminar(true, "¡Gracias por contribuir al cuidado del planeta!", detalle);
        }

        private void Terminar(bool exito, string titulo, string detalle, bool reintentar = false)
        {
            if (IsDisposed) return;
            _procesando = false;
            _paso = 4;
            _pasos.Actual = 4;
            var vista = Columna();
            var tarjeta = Tarjeta();
            var interior = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = TemaEcoAccess.Superficie };
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int lado = TemaEcoAccess.Px(this, 96);
            interior.Controls.Add(exito
                ? new ImagenRedondeada { Imagen = RecursosEco.Eco, Size = new Size(lado, lado), Margin = new Padding(0, 0, 20, 0), AccessibleName = "Eco", AccessibleRole = AccessibleRole.Graphic }
                : new TarjetaIcono(IconChar.CircleExclamation) { Size = new Size(lado / 2 + 8, lado / 2 + 8), Margin = new Padding(0, 0, 20, 0) }, 0, 0);
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
                // La línea de hitos queda debajo del resultado: muestra hasta dónde llegó la operación.
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
            if (_accionAtras is null) boton.Click += (_, _) => { if (_paso == 4) _accionAtras?.Invoke(); };
            _accionAtras = accion;
        }

        private void MostrarMensaje(IconChar icono, string titulo, string? detalle, bool error)
        {
            _contenido.SuspendLayout();
            LimpiarContenido();
            var vista = Columna();
            vista.Controls.Add(Encabezado(titulo, detalle ?? ""));
            _contenido.Controls.Add(vista);
            _contenido.ResumeLayout(true);
            AjustarContenido();
            if (error) _aviso.Text = titulo;
            _siguiente.Visible = !error;
            _atras.Visible = false;
        }

        private static TableLayoutPanel Columna() =>
            new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo, Location = Point.Empty, Margin = Padding.Empty };

        private static Control Encabezado(string titulo, string texto)
        {
            var bloque = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.Fondo, Margin = new Padding(0, 8, 0, 16) };
            bloque.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, Margin = Padding.Empty });
            if (texto.Length > 0) bloque.Controls.Add(new Label { Text = texto, AutoSize = true, MaximumSize = new Size(900, 0), ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 4, 0, 0) });
            return bloque;
        }

        private static TarjetaEco Tarjeta() =>
            new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 12) };

        private static TableLayoutPanel Filas(TarjetaEco tarjeta)
        {
            var filas = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, BackColor = TemaEcoAccess.Superficie };
            filas.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            filas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tarjeta.Controls.Add(filas);
            filas.SizeChanged += (_, _) =>
            {
                foreach (Label valor in filas.Controls.OfType<Label>().Where(l => filas.GetColumn(l) == 1))
                    valor.MaximumSize = new Size(Math.Max(160, filas.Width - 230), 0);
            };
            return filas;
        }

        private static void Fila(TableLayoutPanel filas, string nombre, string valor, Color? color = null)
        {
            int fila = filas.RowCount++;
            filas.Controls.Add(new Label { Text = nombre, AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 8, 12, 8) }, 0, fila);
            filas.Controls.Add(new Label { Text = valor, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = color ?? TemaEcoAccess.Texto, Margin = new Padding(0, 8, 0, 8), AccessibleName = $"{nombre}: {valor}" }, 1, fila);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed) { _cierre.Cancel(); _cierre.Dispose(); _hitos.Dispose(); }
            base.Dispose(disposing);
        }
    }

    /// <summary>Opción de material en el asistente: seleccionable si la caneca la recibe; si no, atenuada y explicada.</summary>
    internal sealed class TarjetaMaterialOpcion : TarjetaDibujada
    {
        private readonly Material _material;
        private readonly bool _disponible;
        private bool _seleccionada;

        public TarjetaMaterialOpcion(Material material, bool disponible) : base(disponible)
        {
            _material = material;
            _disponible = disponible;
            if (!disponible) Superficie = Color.FromArgb(0xF8, 0xFA, 0xF9);
            AccessibleName = $"{material.Nombre}. {(disponible ? "Disponible en la caneca piloto" : "No disponible en esta caneca")}";
            AccessibleRole = disponible ? AccessibleRole.RadioButton : AccessibleRole.StaticText;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal bool Seleccionada
        {
            get => _seleccionada;
            set
            {
                _seleccionada = value;
                AccessibleName = $"{_material.Nombre}. {(value ? "Seleccionado" : _disponible ? "Disponible en la caneca piloto" : "No disponible en esta caneca")}";
                Invalidate();
            }
        }

        private string Estado => _disponible ? (_seleccionada ? "Seleccionado" : "Disponible") : "No disponible en esta caneca";

        private string Indicacion => GuiaSeparacion.Orientacion(_material);

        internal override int MedirAlto(int ancho)
        {
            int util = ancho - 2 * Relleno;
            return Relleno + P(40) + P(12) + DibujoEco.AltoTexto(_material.Nombre, TemaEcoAccess.Subtitulo, util) + P(4)
                + DibujoEco.AltoTexto(Indicacion, TemaEcoAccess.Pequena, util) + P(12) + P(26) + Relleno + P(6);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_seleccionada) return;
            // Selección visible sin depender solo del color: borde grueso y marca de verificación.
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var ruta = RecursosEco.Redondeado(new RectangleF(1, 1, Width - 3, Height - P(4) - 2), P(TemaEcoAccess.Radio));
            using var borde = new Pen(TemaEcoAccess.VerdeOscuro, P(2));
            e.Graphics.DrawPath(borde, ruta);
            Bitmap marca = CacheIconos.Obtener(IconChar.CircleCheck, P(22), TemaEcoAccess.VerdeOscuro);
            e.Graphics.DrawImage(marca, Width - Relleno - marca.Width, Relleno);
        }

        protected override void PintarContenido(Graphics g)
        {
            var (tono, fondo, icono, _) = EstiloCategoria.Para(_material.Categoria);
            int x = Relleno, y = Relleno, util = Width - 2 * Relleno;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujoEco.Icono(g, icono, _disponible ? tono : TemaEcoAccess.Suave, _disponible ? fondo : TemaEcoAccess.Borde, new Rectangle(x, y, P(40), P(40)), P(20));
            y += P(40) + P(12);
            y += DibujoEco.Texto(g, _material.Nombre, TemaEcoAccess.Subtitulo, _disponible ? TemaEcoAccess.Texto : TemaEcoAccess.Suave, x, y, util) + P(4);
            y += DibujoEco.Texto(g, Indicacion, TemaEcoAccess.Pequena, TemaEcoAccess.Suave, x, y, util) + P(12);
            DibujoEco.Pastilla(g, Estado, TemaEcoAccess.Pequena,
                _disponible ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave, _disponible ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Borde, x, y, this);
        }
    }
}
