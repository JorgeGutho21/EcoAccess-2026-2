using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Inicio del ciudadano y del visitante (diseño aprobado de EcoAccess): barra con logo y sesión; portada con el saludo,
    /// los cuatro indicadores reales de Oracle, ECO con la botella y el campus; «¿Dónde va tu residuo?» con buscador
    /// tolerante y sugerencias, las tres canecas oficiales pulsables, el resultado con la disponibilidad real de la estación
    /// y «Así de fácil» con el avance del usuario.
    /// <para>
    /// SIN DESPLAZAMIENTO: todo se compone sobre un lienzo de referencia de 1600×830 px escalado al tamaño físico de la
    /// ventana (las fuentes van en píxeles), así cabe igual a 1366×768, 1600×900 y 1920×1080 y a 100 %, 125 % o 150 % de
    /// escala. El alto sobrante se reparte en la zona central; nada se oculta ni se superpone.
    /// </para>
    /// <para>
    /// Los datos se actualizan solos (cada <see cref="PoliticaSincronizacion.Intervalo"/>, al volver a la ventana y al terminar
    /// un depósito) en segundo plano y sin bloquear la interfaz; si Oracle no responde se dice y los valores viejos se marcan.
    /// Buscar nunca suma ecopuntos ni abre nada: abrir una caneca pasa por el asistente de depósito y por Oracle.
    /// </para>
    /// </summary>
    public sealed class FormExplorarEcoAccess : Form
    {
        private const float AnchoReferencia = 1600f, AltoReferencia = 830f;
        internal static TimeSpan IntervaloSincronizacion => PoliticaSincronizacion.Intervalo;

        private static readonly Color FondoSeccion = Color.FromArgb(0xEC, 0xF7, 0xE6);
        private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        private readonly ServiciosAplicacion _servicios;
        private readonly SesionActiva? _sesion;
        private readonly string _sesionOpinion = Guid.NewGuid().ToString("N");
        private readonly LienzoInicio _lienzo = new() { Dock = DockStyle.Fill };
        private readonly ToolTip _ayudas = new();
        private readonly CancellationTokenSource _cierre = new();

        // Barra superior y portada.
        private readonly BotonEco _salir;
        private readonly BotonEco? _crearCuenta;
        private readonly BotonEco _catalogo = new("Catálogo", IconChar.Images) { AccessibleName = "Catálogo de materiales con fotografías" };
        private readonly IndicadorInicio _saldo = new(IconChar.Coins, "Mis ecopuntos");
        private readonly IndicadorInicio _depositos = new(IconChar.Recycle, "Mis depósitos");
        private readonly IndicadorInicio _ultima = new(IconChar.ClockRotateLeft, "Última actividad");
        private readonly IndicadorInicio _participacion = new(IconChar.Users, "Mi participación");
        private readonly IndicadorInicio? _invitacion;
        private readonly string _saludo, _textoSaludo;

        // Fila de estado.
        private readonly BotonEco _actualizar = new("Actualizar mi estado", IconChar.ArrowsRotate, BotonEco.Estilo.Secundario);
        private readonly BotonEco _qr = new("QR de la estación", IconChar.Qrcode, BotonEco.Estilo.Enlace);
        private readonly BotonEco _opinion = new("Tu opinión", IconChar.CommentDots, BotonEco.Estilo.Enlace);
        private string _estadoActualizacion = "Consultando Oracle…";
        private Color _colorEstado = TemaEcoAccess.Suave;
        private string _estadoOracle = "Oracle · comprobando";
        private bool? _oracleConectado;
        /// <summary>Modo de la estación en este equipo (supervisor de desarrollo): simulación, bridge físico o sin dato.</summary>
        private string? _modoEstacion;
        private bool _estacionSimulada;
        private const string TextoSimulada = "● Estación simulada (demo)", TextoFisica = "● Bridge físico activo";

        // Sección «¿Dónde va tu residuo?».
        private readonly CajaTexto _campo = new() { BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = TemaEcoAccess.Texto, MaxLength = 60, AccessibleName = "Escribe un residuo" };
        private readonly BotonEco _buscar = new("Buscar", IconChar.MagnifyingGlass, BotonEco.Estilo.Principal);
        private readonly ListBox _sugerencias = new() { DrawMode = DrawMode.OwnerDrawFixed, BorderStyle = BorderStyle.FixedSingle, Visible = false, IntegralHeight = false, TabStop = false, AccessibleName = "Sugerencias" };
        private readonly System.Windows.Forms.Timer _esperaSugerir = new() { Interval = 160 };
        private readonly List<ZonaCaneca> _zonas = new();
        private readonly BotonEco _accionPrincipal = new("Depositar este residuo", IconChar.ChevronRight, BotonEco.Estilo.Principal) { IconoDerecha = true };
        private readonly BotonEco _accionSecundaria = new("Ver los residuos de esta caneca", IconChar.List, BotonEco.Estilo.Enlace);
        private readonly List<BotonEco> _alternativas = new();

        // Estado de la pantalla.
        private enum Modo { Residuo, Caneca, SinResultados }
        private Modo _modo = Modo.Residuo;
        private ResiduoCatalogo _residuo = CatalogoResiduos.Buscar("botella-agua")!;
        private ResultadoBusquedaResiduo? _busqueda;
        private CategoriaMaterial? _canecaElegida;
        private string _textoSinResultados = "";
        private string? _disponibilidad;
        private bool? _disponible;
        private CancellationTokenSource? _consultaDisponibilidad;
        private int _paso = 1;
        private bool _actualizando, _cerrando;
        private int _fallosSeguidos;
        private DateTime _ultimaActualizacionUtc = DateTime.MinValue;
        private CanecaPublica? _demo;
        private readonly System.Windows.Forms.Timer _sincronizacion = new();
        private readonly System.Windows.Forms.Timer _reloj = new() { Interval = 5000 };

        // Geometría calculada en Adaptar().
        private float _s = 1f;
        private Rectangle _rBarra, _rPortada, _rEstado, _rSeccion, _rCaja, _rCanecas, _rImagenCanecas, _rResultado, _rPasos, _rEco, _rTextoEstado;
        private Rectangle _rIlustracion, _rTextoResultado;

        public bool SolicitarInicioSesion { get; private set; }

        public FormExplorarEcoAccess(ServiciosAplicacion servicios, SesionActiva? sesion)
        {
            _servicios = servicios;
            _sesion = sesion;
            Text = sesion is null ? "EcoAccess · Explora como invitado" : $"EcoAccess · {sesion.Nombre}";
            // Sin escalado automático: el lienzo se compone en píxeles a partir del tamaño físico de la ventana.
            AutoScaleMode = AutoScaleMode.None;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            Size = new Size(1366, 768);
            MinimumSize = new Size(960, 600);
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;

            string nombre = sesion?.Nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            _saludo = sesion is null ? "¡Bienvenido a EcoAccess!" : $"¡Hola, {nombre}!";
            _textoSaludo = sesion is null
                ? "Busca tu residuo o toca una caneca. Como invitado no acumulas ecopuntos."
                : "Gestiona tus residuos, consulta tu actividad y suma ecopuntos por tus acciones sostenibles.";

            _salir = new BotonEco(sesion is null ? "Iniciar sesión" : "Cerrar sesión", sesion is null ? IconChar.RightToBracket : IconChar.RightFromBracket);
            _salir.Click += (_, _) => VolverAlInicio();
            _lienzo.Controls.Add(_salir);
            _catalogo.Click += (_, _) => VerCatalogo();
            _lienzo.Controls.Add(_catalogo);
            if (sesion is null)
            {
                _crearCuenta = new BotonEco("Crear cuenta", IconChar.UserPlus, BotonEco.Estilo.Principal);
                _crearCuenta.Click += (_, _) => CrearCuenta();
                _lienzo.Controls.Add(_crearCuenta);
                _invitacion = new IndicadorInicio(IconChar.Award, "¿Sabías que puedes acumular ecopuntos?");
                _invitacion.Mostrar("Crea tu cuenta", "Lleva el historial de tus depósitos y suma ecopuntos cuando se verifiquen. Pulsa aquí para registrarte.");
                _invitacion.Click += (_, _) => CrearCuenta();
                _lienzo.Controls.Add(_invitacion);
            }
            else
            {
                foreach (IndicadorInicio t in new[] { _saldo, _depositos, _ultima, _participacion }) _lienzo.Controls.Add(t);
                _saldo.Click += (_, _) => MostrarMisEcopuntos();
                _participacion.Click += (_, _) => MostrarMisEcopuntos();
                _depositos.Click += (_, _) => MostrarMiActividad();
                _ultima.Click += (_, _) => MostrarMiActividad();
                _ayudas.SetToolTip(_saldo, "Abrir mis ecopuntos");
                _ayudas.SetToolTip(_participacion, "Abrir mis ecopuntos");
                _ayudas.SetToolTip(_depositos, "Abrir mi actividad");
                _ayudas.SetToolTip(_ultima, "Abrir mi actividad");
                _actualizar.Click += async (_, _) => await ActualizarMiEstadoAsync(automatica: false);
                _ayudas.SetToolTip(_actualizar, "Opcional: tus datos también se actualizan solos.");
                _lienzo.Controls.Add(_actualizar);
            }
            _qr.Click += (_, _) => MostrarQrPiloto();
            _opinion.Click += (_, _) => MostrarOpiniones();
            _lienzo.Controls.Add(_qr);
            if (_servicios.Opiniones is not null) _lienzo.Controls.Add(_opinion);

            CrearSeccion();
            Controls.Add(_lienzo);
            _lienzo.Paint += (_, e) => Pintar(e.Graphics);
            _lienzo.Resize += (_, _) => Adaptar();
            // Lo que no cupo en la tarjeta de resultado se lee al pasar el ratón sobre ella (y nunca tapa otras zonas).
            _lienzo.MouseMove += (_, e) =>
            {
                string ayuda = _rResultado.Contains(e.Location) ? _textoOmitido : "";
                if (_ayudas.GetToolTip(_lienzo) != ayuda) _ayudas.SetToolTip(_lienzo, ayuda);
            };

            MostrarResiduo(_residuo, null, consultarEstacion: false);
            _paso = 1; // Ejemplo inicial: todavía no ha buscado nada.

            // Sincronización: periódica, al volver a la ventana y al terminar un depósito. Nunca dos consultas a la vez.
            _sincronizacion.Interval = (int)IntervaloSincronizacion.TotalMilliseconds;
            _sincronizacion.Tick += async (_, _) => await ActualizarMiEstadoAsync(automatica: true);
            _reloj.Tick += (_, _) => RevisarVigencia();
            Activated += async (_, _) =>
            {
                if (DateTime.UtcNow - _ultimaActualizacionUtc > TimeSpan.FromSeconds(10) && _fallosSeguidos == 0 && _ultimaActualizacionUtc != DateTime.MinValue)
                    await ActualizarMiEstadoAsync(automatica: true);
            };
            Shown += async (_, _) => { Adaptar(); _campo.Focus(); _sincronizacion.Start(); _reloj.Start(); await CargarAsync(); };
            FormClosing += (_, _) => { _cerrando = true; _sincronizacion.Stop(); _reloj.Stop(); _cierre.Cancel(); _consultaDisponibilidad?.Cancel(); };
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape && _sugerencias.Visible) { _sugerencias.Visible = false; e.Handled = true; } };
        }

        private int S(float v) => (int)Math.Round(v * _s);

        private void CrearSeccion()
        {
            _campo.PlaceholderText = "Escribe un residuo: botella, cartón, servilleta, cáscara...";
            _lienzo.Controls.Add(_campo);
            _lienzo.Controls.Add(_buscar);
            _lienzo.Controls.Add(_sugerencias);
            _buscar.Click += (_, _) => Buscar(_campo.Text);
            _campo.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (_sugerencias.Visible && _sugerencias.SelectedItem is ResiduoCatalogo elegido) ElegirSugerencia(elegido);
                    else Buscar(_campo.Text);
                }
                else if (e.KeyCode == Keys.Down && _sugerencias.Visible && _sugerencias.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    _sugerencias.SelectedIndex = Math.Min(_sugerencias.Items.Count - 1, _sugerencias.SelectedIndex + 1);
                }
                else if (e.KeyCode == Keys.Up && _sugerencias.Visible && _sugerencias.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    _sugerencias.SelectedIndex = Math.Max(0, _sugerencias.SelectedIndex - 1);
                }
                else if (e.KeyCode == Keys.Escape) _sugerencias.Visible = false;
            };
            _campo.TextChanged += (_, _) => { _esperaSugerir.Stop(); _esperaSugerir.Start(); };
            _campo.GotFocus += (_, _) => _lienzo.Invalidate(_rCaja);
            _campo.LostFocus += (_, _) =>
            {
                _lienzo.Invalidate(_rCaja);
                // Se cierra al salir, salvo que el foco pase a la propia lista.
                BeginInvoke(new Action(() => { if (!_sugerencias.Focused && !_campo.Focused) _sugerencias.Visible = false; }));
            };
            _esperaSugerir.Tick += (_, _) => { _esperaSugerir.Stop(); ActualizarSugerencias(); };
            _sugerencias.DrawItem += DibujarSugerencia;
            _sugerencias.MouseClick += (_, _) => { if (_sugerencias.SelectedItem is ResiduoCatalogo r) ElegirSugerencia(r); };
            _sugerencias.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter && _sugerencias.SelectedItem is ResiduoCatalogo r) { e.SuppressKeyPress = true; ElegirSugerencia(r); } };

            foreach (var (caneca, titulo, descripcion) in CatalogoPrototipo.Canecas)
            {
                var zona = new ZonaCaneca(caneca, titulo, descripcion);
                zona.Click += (_, _) => ElegirCaneca(caneca);
                _ayudas.SetToolTip(zona, $"{titulo}: {descripcion}. Pulsa para ver qué recibe.");
                _zonas.Add(zona);
                _lienzo.Controls.Add(zona);
            }
            _accionPrincipal.Click += (_, _) => AccionPrincipal();
            _accionSecundaria.Click += (_, _) => AccionSecundaria();
            _lienzo.Controls.Add(_accionPrincipal);
            _lienzo.Controls.Add(_accionSecundaria);
            for (int i = 0; i < 3; i++)
            {
                var alternativa = new BotonEco("", IconChar.ArrowRight, BotonEco.Estilo.Suave) { Visible = false };
                alternativa.Click += (_, _) => { if (alternativa.Tag is ResiduoCatalogo r) MostrarResiduo(r, null); };
                _alternativas.Add(alternativa);
                _lienzo.Controls.Add(alternativa);
            }
        }

        // ── Composición ─────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>Coloca todo a la escala de la ventana. Ningún control queda fuera del lienzo y no hay barras de desplazamiento.</summary>
        private void Adaptar()
        {
            int W = _lienzo.ClientSize.Width, H = _lienzo.ClientSize.Height;
            if (W < 50 || H < 50) return;
            _s = Math.Max(0.5f, Math.Min(1.6f, Math.Min(W / AnchoReferencia, H / AltoReferencia)));
            int sobra = Math.Max(0, H - (int)(AltoReferencia * _s));
            _lienzo.SuspendLayout();

            // Barra superior.
            _rBarra = new Rectangle(0, 0, W, S(64));
            Font botones = FuentesInicio.Semibold(15 * _s);
            foreach (BotonEco b in new[] { _salir, _crearCuenta, _catalogo, _actualizar, _buscar, _accionPrincipal }.Where(b => b is not null)!) b!.Font = botones;
            _salir.Size = _salir.GetPreferredSize(Size.Empty);
            _salir.Location = new Point(W - S(28) - _salir.Width, (_rBarra.Height - _salir.Height) / 2);
            if (_crearCuenta is not null)
            {
                _crearCuenta.Size = _crearCuenta.GetPreferredSize(Size.Empty);
                _crearCuenta.Location = new Point(_salir.Left - S(12) - _crearCuenta.Width, _salir.Top);
            }
            _catalogo.Size = _catalogo.GetPreferredSize(Size.Empty);
            _catalogo.Location = new Point((_crearCuenta?.Left ?? _salir.Left) - S(12) - _catalogo.Width, _salir.Top);

            // Portada: saludo, indicadores, ECO y campus.
            _rPortada = new Rectangle(0, _rBarra.Bottom, W, S(238) + sobra / 6);
            int m = S(36), g = S(16), eco = S(250);
            int ancho = (W - 2 * m - 2 * g - eco) / 4;
            int yTarjetas = _rPortada.Bottom - S(14) - S(108);
            var tarjetas = new[] { _saldo, _depositos, _ultima };
            for (int i = 0; i < 3; i++) tarjetas[i].Bounds = new Rectangle(m + i * (ancho + g), yTarjetas, ancho, S(108));
            _participacion.Bounds = new Rectangle(W - m - ancho, yTarjetas, ancho, S(108));
            foreach (IndicadorInicio t in new[] { _saldo, _depositos, _ultima, _participacion }) t.Escala = _s;
            _rEco = new Rectangle(m + 3 * ancho + 2 * g, _rPortada.Y + S(6), W - 2 * m - 4 * ancho - 2 * g, _rPortada.Height - S(8));
            if (_invitacion is not null)
            {
                _invitacion.Escala = _s;
                _invitacion.Bounds = new Rectangle(m, yTarjetas, 3 * ancho + 2 * g, S(108));
            }

            // Fila de estado.
            _rEstado = new Rectangle(0, _rPortada.Bottom, W, S(46));
            int x = m;
            Font enlaces = FuentesInicio.Semibold(14 * _s);
            if (_sesion is not null)
            {
                _actualizar.Font = FuentesInicio.Semibold(14 * _s);
                Size p = _actualizar.GetPreferredSize(Size.Empty);
                _actualizar.Bounds = new Rectangle(x, _rEstado.Y + (_rEstado.Height - S(36)) / 2, p.Width, S(36));
                x = _actualizar.Right + S(16);
            }
            // Derecha: pastilla de Oracle; a su izquierda los enlaces; el texto de estado ocupa el resto.
            Font pastillas = FuentesInicio.Semibold(13 * _s);
            int derechaEnlaces = W - S(36) - TextRenderer.MeasureText("● Sin conexión con Oracle", pastillas).Width - S(26) - S(18)
                - (_modoEstacion is null ? 0 : TextRenderer.MeasureText(_modoEstacion, pastillas).Width + S(26) + S(10));
            foreach (BotonEco enlace in new[] { _opinion, _qr })
            {
                if (enlace.Parent is null) continue;
                enlace.Font = enlaces;
                Size p = enlace.GetPreferredSize(Size.Empty);
                enlace.Bounds = new Rectangle(derechaEnlaces - p.Width, _rEstado.Y + (_rEstado.Height - p.Height) / 2, p.Width, p.Height);
                derechaEnlaces = enlace.Left - S(10);
            }
            _rTextoEstado = Rectangle.FromLTRB(x, _rEstado.Y, Math.Max(x + S(120), derechaEnlaces - S(8)), _rEstado.Bottom);

            // Sección.
            _rSeccion = new Rectangle(S(24), _rEstado.Bottom, W - S(48), H - _rEstado.Bottom - S(14));
            int pad = S(22);
            int yCabecera = _rSeccion.Y + S(14);
            int anchoBoton = S(150), altoCaja = S(52);
            int anchoCaja = Math.Max(S(300), (int)(_rSeccion.Width * 0.42));
            _buscar.Bounds = new Rectangle(_rSeccion.Right - pad - anchoBoton, yCabecera + S(4), anchoBoton, altoCaja);
            _rCaja = new Rectangle(_buscar.Left - S(14) - anchoCaja, yCabecera + S(4), anchoCaja, altoCaja);
            _campo.Font = FuentesInicio.Obtener(17 * _s);
            int altoCampo = _campo.PreferredHeight;
            _campo.Bounds = new Rectangle(_rCaja.X + S(50), _rCaja.Y + (_rCaja.Height - altoCampo) / 2, _rCaja.Width - S(64), altoCampo);
            _sugerencias.Font = FuentesInicio.Obtener(15 * _s);
            _sugerencias.ItemHeight = Math.Max(18, S(38));
            int filas = Math.Max(1, Math.Min(6, _sugerencias.Items.Count));
            _sugerencias.Bounds = new Rectangle(_rCaja.X, _rCaja.Bottom + S(4), _rCaja.Width, filas * _sugerencias.ItemHeight + 4);

            _rPasos = new Rectangle(_rSeccion.X + pad, _rSeccion.Bottom - S(14) - S(84), _rSeccion.Width - 2 * pad, S(84));
            int yMedio = _rCaja.Bottom + S(16);
            int altoMedio = _rPasos.Top - S(14) - yMedio;
            int anchoCanecas = (int)((_rSeccion.Width - 2 * pad - S(20)) * 0.5);
            _rCanecas = new Rectangle(_rSeccion.X + pad - S(6), yMedio, anchoCanecas, altoMedio);
            Image? grupo = RecursosEco.Imagen("inicio/canecas_tres.png");
            _rImagenCanecas = grupo is null ? _rCanecas : CacheEscalado.Contener(grupo, new Rectangle(_rCanecas.X, _rCanecas.Y, _rCanecas.Width, _rCanecas.Height - S(22)), abajo: true);
            // Posición de cada caneca dentro de la ilustración (1335×562): verde, blanca y negra.
            float kx = _rImagenCanecas.Width / 1335f, ky = _rImagenCanecas.Height / 562f;
            int[] izquierdas = { 131, 511, 893 }, derechas = { 466, 847, 1232 };
            for (int i = 0; i < _zonas.Count; i++)
            {
                _zonas[i].Escala = _s;
                _zonas[i].Bounds = Rectangle.FromLTRB(_rImagenCanecas.X + (int)(izquierdas[i] * kx) - S(8), _rImagenCanecas.Y - S(6),
                    _rImagenCanecas.X + (int)(derechas[i] * kx) + S(8), _rImagenCanecas.Y + (int)(505 * ky) + S(6));
            }
            _rResultado = Rectangle.FromLTRB(_rCanecas.Right + S(20), yMedio, _rSeccion.Right - pad, _rPasos.Top - S(14));
            AdaptarResultado();
            _lienzo.ResumeLayout(false);
            _lienzo.Invalidate();
        }

        /// <summary>Ilustración a la izquierda; textos, opciones y acciones a la derecha. Los botones nunca tapan texto.</summary>
        private Rectangle _rEtiqueta, _rRotuloOpciones;
        private string _textoOmitido = "";
        private const string RotuloOpciones = "Según su estado:";

        /// <summary>
        /// Composición por prioridades: arriba la etiqueta y el enlace secundario en la misma fila; abajo la acción principal;
        /// encima de ella, en UNA fila, las opciones de desambiguación. Si el alto no alcanza para el nombre y la caneca, las
        /// opciones se retiran primero (siguen en «Ver los resultados»). Nada se superpone.
        /// </summary>
        private void AdaptarResultado()
        {
            Rectangle r = _rResultado;
            int pad = S(18);
            int lado = Math.Max(S(90), Math.Min(r.Height - 2 * pad, (int)(r.Width * 0.28)));
            _rIlustracion = new Rectangle(r.X + pad, r.Y + (r.Height - lado) / 2, lado, lado);
            int xt = _rIlustracion.Right + S(22);
            int anchoTexto = Math.Max(S(120), r.Right - pad - xt);

            // Fila superior: etiqueta a la izquierda y enlace secundario a la derecha.
            int altoFila = S(30);
            _rEtiqueta = new Rectangle(xt, r.Y + pad, anchoTexto, altoFila);
            _accionSecundaria.Font = FuentesInicio.Semibold(13.5f * _s);
            Size ps = _accionSecundaria.GetPreferredSize(Size.Empty);
            int anchoSecundaria = Math.Min(ps.Width, Math.Max(S(60), anchoTexto - S(190)));
            _accionSecundaria.Bounds = new Rectangle(xt + anchoTexto - anchoSecundaria, _rEtiqueta.Y + (altoFila - ps.Height) / 2, anchoSecundaria, ps.Height);

            // Acción principal abajo.
            _accionPrincipal.Font = FuentesInicio.Semibold(16.5f * _s);
            int altoBoton = S(50);
            _accionPrincipal.Bounds = new Rectangle(xt, r.Bottom - pad - altoBoton, Math.Min(anchoTexto, Math.Max(S(300), _accionPrincipal.GetPreferredSize(Size.Empty).Width + S(30))), altoBoton);
            int inferior = (_accionPrincipal.Visible ? _accionPrincipal.Top : r.Bottom - pad) - S(8);

            // Opciones de desambiguación en una fila, con su rótulo a la izquierda.
            Font nombre = FuentesInicio.Obtener(25 * _s, FontStyle.Bold), fuerte = FuentesInicio.Semibold(16 * _s);
            int minimoTexto = nombre.Height + fuerte.Height + S(10);
            int superior = _rEtiqueta.Bottom + S(6);
            var conOpcion = _alternativas.Where(a => a.Tag is not null).ToList();
            int altoOpcion = S(32);
            bool caben = conOpcion.Count > 0 && inferior - altoOpcion - S(6) - superior >= minimoTexto;
            _rRotuloOpciones = Rectangle.Empty;
            if (caben)
            {
                int yOpciones = inferior - altoOpcion;
                Font rotulo = FuentesInicio.Semibold(13 * _s);
                int anchoRotulo = TextRenderer.MeasureText(RotuloOpciones, rotulo).Width;
                _rRotuloOpciones = new Rectangle(xt, yOpciones, anchoRotulo, altoOpcion);
                int xo = xt + anchoRotulo + S(6);
                int mostradas = 0;
                foreach (BotonEco a in _alternativas)
                {
                    a.Visible = false;
                    if (a.Tag is null) continue;
                    a.Font = FuentesInicio.Semibold(13 * _s);
                    int w = a.GetPreferredSize(Size.Empty).Width;
                    if (xo + w > xt + anchoTexto)
                    {
                        // Sin espacio para otra completa: la primera se recorta con puntos suspensivos; las demás quedan en la lista.
                        if (mostradas > 0) continue;
                        w = xt + anchoTexto - xo;
                    }
                    a.Bounds = new Rectangle(xo, yOpciones, w, altoOpcion);
                    a.Visible = true;
                    mostradas++;
                    xo += w + S(8);
                }
                inferior = yOpciones - S(6);
            }
            else foreach (BotonEco a in _alternativas) a.Visible = false;
            _rTextoResultado = Rectangle.FromLTRB(xt, superior, xt + anchoTexto, Math.Max(superior + nombre.Height, inferior));
        }

        // ── Pintura ─────────────────────────────────────────────────────────────────────────────────────────────────

        private void Pintar(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(TemaEcoAccess.Fondo);
            PintarPortada(g);
            PintarBarra(g);
            PintarEstado(g);
            PintarSeccion(g);
        }

        private void PintarBarra(Graphics g)
        {
            using (var blanco = new SolidBrush(Color.White)) g.FillRectangle(blanco, _rBarra);
            using (var borde = new Pen(TemaEcoAccess.Borde)) g.DrawLine(borde, 0, _rBarra.Bottom - 1, _rBarra.Width, _rBarra.Bottom - 1);
            Image logo = RecursosEco.Logo;
            int alto = S(42);
            int anchoLogo = alto * logo.Width / logo.Height;
            g.DrawImage(CacheEscalado.Obtener(logo, anchoLogo, alto), S(32), (_rBarra.Height - alto) / 2);
            // Rol y nombre de la persona a la izquierda del botón de sesión.
            int derecha = _catalogo.Left - S(18);
            Font fNombre = FuentesInicio.Obtener(18 * _s);
            if (_sesion is not null)
            {
                Size nombre = TextRenderer.MeasureText(_sesion.Nombre, fNombre);
                int xNombre = derecha - nombre.Width;
                TextRenderer.DrawText(g, _sesion.Nombre, fNombre, new Point(xNombre, (_rBarra.Height - nombre.Height) / 2), TemaEcoAccess.Texto);
                PintarPastilla(g, "● Ciudadano", xNombre - S(14), _rBarra.Height / 2, TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave, derecha: true);
            }
            else PintarPastilla(g, "Modo invitado", derecha, _rBarra.Height / 2, TemaEcoAccess.Suave, TemaEcoAccess.Fondo, derecha: true);
        }

        /// <summary>Pastilla centrada verticalmente en <paramref name="yCentro"/>; con <paramref name="derecha"/> la x es su borde derecho.</summary>
        private Rectangle PintarPastilla(Graphics g, string texto, int x, int yCentro, Color tono, Color fondo, bool derecha = false, float tamano = 14)
        {
            Font f = FuentesInicio.Semibold(tamano * _s);
            Size medida = TextRenderer.MeasureText(texto, f, Size.Empty, TextFormatFlags.NoPadding);
            var area = new Rectangle(0, yCentro - (medida.Height + S(12)) / 2, medida.Width + S(26), medida.Height + S(12));
            area.X = derecha ? x - area.Width : x;
            using (var ruta = RecursosEco.Redondeado(area, area.Height / 2f))
            using (var pincel = new SolidBrush(fondo))
                g.FillPath(pincel, ruta);
            TextRenderer.DrawText(g, texto, f, area, tono, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            return area;
        }

        private void PintarPortada(Graphics g)
        {
            Rectangle p = _rPortada;
            using (var fondo = new LinearGradientBrush(p, Color.FromArgb(0xDF, 0xF3, 0xD2), Color.FromArgb(0xF7, 0xFC, 0xF3), 0f))
                g.FillRectangle(fondo, p);
            // Campus de la UPC a la derecha, difuminado hacia la izquierda y hacia abajo.
            if (RecursosEco.Imagen("eco/campus.jpg") is { } campus)
            {
                var destino = new Rectangle(p.Right - p.Width * 30 / 100, p.Y, p.Width * 30 / 100, p.Height);
                var franja = new RectangleF(0, campus.Height * 0.34f, campus.Width, campus.Height * 0.30f);
                float k = Math.Max(destino.Width / franja.Width, destino.Height / franja.Height);
                var origen = new RectangleF(franja.X + (franja.Width - destino.Width / k) / 2, franja.Y, destino.Width / k, destino.Height / k);
                g.DrawImage(campus, destino, origen, GraphicsUnit.Pixel);
                using var difuminado = new LinearGradientBrush(destino, Color.FromArgb(255, 0xEE, 0xF8, 0xE7), Color.FromArgb(30, 0xEE, 0xF8, 0xE7), 0f);
                g.FillRectangle(difuminado, destino);
                using var abajo = new LinearGradientBrush(new Rectangle(destino.X, destino.Bottom - S(90), destino.Width, S(90)), Color.FromArgb(0, 0xEE, 0xF8, 0xE7), Color.FromArgb(235, 0xEE, 0xF8, 0xE7), 90f);
                g.FillRectangle(abajo, destino.X, destino.Bottom - S(90), destino.Width, S(91));
            }
            using (var ola = new GraphicsPath())
            {
                ola.AddBezier(0, p.Y + p.Height * 0.55f, p.Width * 0.25f, p.Y + p.Height * 0.35f, p.Width * 0.45f, p.Y + p.Height * 0.95f, p.Width * 0.72f, p.Y + p.Height * 0.62f);
                ola.AddLine(p.Width * 0.72f, p.Y + p.Height * 0.62f, p.Width * 0.72f, p.Bottom);
                ola.AddLine(p.Width * 0.72f, p.Bottom, 0, p.Bottom);
                ola.CloseFigure();
                using var pincel = new SolidBrush(Color.FromArgb(55, 0x8D, 0xC6, 0x3F));
                g.FillPath(pincel, ola);
            }
            // Hoja del saludo y saludo.
            Bitmap hoja = CacheIconos.Obtener(IconChar.Leaf, S(66), Color.FromArgb(0x2E, 0xA0, 0x43));
            g.DrawImage(hoja, S(34), p.Y + S(18));
            Font titulo = FuentesInicio.Obtener(38 * _s, FontStyle.Bold | FontStyle.Italic);
            Font texto = FuentesInicio.Obtener(17 * _s);
            int x = S(112), anchoTexto = Math.Max(S(300), _rEco.Left - x - S(20));
            int yTitulo = p.Y + S(10);
            TextRenderer.DrawText(g, _saludo, titulo, new Rectangle(x, yTitulo, anchoTexto, titulo.Height + S(4)), Color.FromArgb(0x0E, 0x4D, 0x2C),
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            int yTexto = yTitulo + titulo.Height + S(2);
            int limite = _saldo.Visible && _sesion is not null ? _saldo.Top - S(6) : (_invitacion?.Top ?? p.Bottom) - S(6);
            TextRenderer.DrawText(g, _textoSaludo, texto, Rectangle.FromLTRB(x, yTexto, x + Math.Min(anchoTexto, S(760)), Math.Max(yTexto + texto.Height, limite)), TemaEcoAccess.Texto,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.TextBoxControl);
            // ECO con la botella, entre la tercera y la cuarta tarjeta, apoyado en el borde inferior de la portada.
            if (RecursosEco.Imagen("eco/eco_botella.png") is { } ecoImagen)
            {
                var area = new Rectangle(_rEco.X - S(30), _rEco.Y, _rEco.Width + S(60), _rEco.Height);
                Rectangle destino = CacheEscalado.Contener(ecoImagen, area, abajo: true);
                g.DrawImage(CacheEscalado.Obtener(ecoImagen, destino.Width, destino.Height), destino.Location);
            }
        }

        private void PintarEstado(Graphics g)
        {
            Font f = FuentesInicio.Obtener(14 * _s);
            Bitmap reloj = CacheIconos.Obtener(_oracleConectado == false ? IconChar.TriangleExclamation : IconChar.Clock, S(16), _colorEstado);
            g.DrawImage(reloj, _rTextoEstado.X, _rTextoEstado.Y + (_rTextoEstado.Height - reloj.Height) / 2);
            TextRenderer.DrawText(g, _estadoActualizacion, f, Rectangle.FromLTRB(_rTextoEstado.X + S(22), _rTextoEstado.Y, _rTextoEstado.Right, _rTextoEstado.Bottom), _colorEstado,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            // Conexión con Oracle a la derecha, siempre visible.
            Color tono = _oracleConectado switch { true => TemaEcoAccess.VerdeOscuro, false => TemaEcoAccess.Error, _ => TemaEcoAccess.Suave };
            Color fondo = _oracleConectado switch { true => TemaEcoAccess.VerdeSuave, false => TemaEcoAccess.ErrorSuave, _ => Color.White };
            int derecha = _rEstado.Right - S(36);
            Rectangle oracle = PintarPastilla(g, "● " + _estadoOracle, derecha, _rEstado.Y + _rEstado.Height / 2, tono, fondo, derecha: true, tamano: 13);
            // Una estación simulada se identifica siempre: sus aperturas no son físicas.
            if (_modoEstacion is not null)
                PintarPastilla(g, _modoEstacion, oracle.Left - S(10), _rEstado.Y + _rEstado.Height / 2,
                    _estacionSimulada ? TemaEcoAccess.Advertencia : TemaEcoAccess.VerdeOscuro, _estacionSimulada ? TemaEcoAccess.AdvertenciaSuave : TemaEcoAccess.VerdeSuave, derecha: true, tamano: 13);
        }

        private void PintarSeccion(Graphics g)
        {
            Rectangle sec = _rSeccion;
            using (var ruta = RecursosEco.Redondeado(sec, S(26)))
            {
                using var fondo = new SolidBrush(FondoSeccion);
                g.FillPath(fondo, ruta);
                using var borde = new Pen(Color.FromArgb(0xD8, 0xEC, 0xD0));
                g.DrawPath(borde, ruta);
            }
            Bitmap hoja = CacheIconos.Obtener(IconChar.Leaf, S(52), Color.FromArgb(0x2E, 0xA0, 0x43));
            g.DrawImage(hoja, sec.X + S(20), sec.Y + S(16));
            Bitmap hojaDerecha = CacheIconos.Obtener(IconChar.Leaf, S(34), Color.FromArgb(150, 0x6C, 0xBF, 0x2E));
            g.DrawImage(hojaDerecha, sec.Right - hojaDerecha.Width - S(6), sec.Y + S(2));

            // Título y subtítulo a la izquierda del buscador.
            int x = sec.X + S(84), ancho = Math.Max(S(160), _rCaja.Left - x - S(16));
            Font titulo = FuentesInicio.Obtener(32 * _s, FontStyle.Bold | FontStyle.Italic);
            Font sub = FuentesInicio.Obtener(16 * _s);
            TextRenderer.DrawText(g, "¿Dónde va tu residuo?", titulo, new Rectangle(x, sec.Y + S(10), ancho, titulo.Height + 2), Color.FromArgb(0x0E, 0x4D, 0x2C),
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, "Escribe el nombre de tu residuo y consulta en qué caneca entregarlo.", sub,
                new Rectangle(x + S(2), sec.Y + S(12) + titulo.Height, ancho, sub.Height * 2 + 2), TemaEcoAccess.Texto,
                TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.TextBoxControl);

            // Caja del buscador.
            using (var ruta = RecursosEco.Redondeado(_rCaja, S(14)))
            {
                using var blanco = new SolidBrush(Color.White);
                g.FillPath(blanco, ruta);
                using var borde = new Pen(_campo.Focused ? TemaEcoAccess.Verde : Color.FromArgb(0xCF, 0xDD, 0xD4), _campo.Focused ? Math.Max(1.5f, 2 * _s) : 1);
                g.DrawPath(borde, ruta);
            }
            Bitmap lupa = CacheIconos.Obtener(IconChar.MagnifyingGlass, S(20), _campo.Focused ? TemaEcoAccess.Verde : TemaEcoAccess.Suave);
            g.DrawImage(lupa, _rCaja.X + S(18), _rCaja.Y + (_rCaja.Height - lupa.Height) / 2);

            // Canecas oficiales y su indicación.
            if (RecursosEco.Imagen("inicio/canecas_tres.png") is { } grupo)
                g.DrawImage(CacheEscalado.Obtener(grupo, _rImagenCanecas.Width, _rImagenCanecas.Height), _rImagenCanecas.Location);
            Font ayuda = FuentesInicio.Obtener(13 * _s, FontStyle.Italic);
            TextRenderer.DrawText(g, "Toca una caneca para ver qué residuos recibe.", ayuda,
                new Rectangle(_rCanecas.X, _rImagenCanecas.Bottom + S(2), _rCanecas.Width, S(20)), TemaEcoAccess.Suave, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            // Flecha discreta sobre la caneca de destino del residuo mostrado.
            if (_modo == Modo.Residuo && _residuo.Caneca is { } destino && _zonas.FirstOrDefault(z => z.Caneca == destino) is { } zona)
            {
                int cx = zona.Left + zona.Width / 2, arriba = Math.Max(_rCaja.Bottom + S(2), zona.Top - S(16));
                Bitmap flecha = CacheIconos.Obtener(IconChar.CaretDown, S(26), Color.FromArgb(0x6C, 0xBF, 0x2E));
                g.DrawImage(flecha, cx - flecha.Width / 2, arriba);
            }

            PintarResultado(g);
            PintarPasos(g);
        }

        private static void Tarjeta(Graphics g, Rectangle r, float radio, Color relleno)
        {
            for (int capa = 4; capa >= 1; capa--)
            {
                var sombra = r; sombra.Offset(0, capa);
                using var ruta = RecursosEco.Redondeado(sombra, radio);
                using var pincel = new SolidBrush(Color.FromArgb(9, 16, 60, 30));
                g.FillPath(pincel, ruta);
            }
            using (var ruta = RecursosEco.Redondeado(r, radio))
            using (var pincel = new SolidBrush(relleno))
                g.FillPath(pincel, ruta);
        }

        private void PintarResultado(Graphics g)
        {
            Rectangle r = _rResultado;
            Tarjeta(g, r, S(20), Color.White);
            // Ilustración: fondo verde suave redondeado con la imagen del residuo, la caneca o el icono del material.
            using (var ruta = RecursosEco.Redondeado(_rIlustracion, S(18)))
            using (var suave = new SolidBrush(_modo == Modo.Residuo && (_residuo.RequiereManejoEspecial || _residuo.DependeDeSuEstado) ? TemaEcoAccess.AdvertenciaSuave : Color.FromArgb(0xEE, 0xF8, 0xEA)))
                g.FillPath(suave, ruta);
            Image? imagen = null;
            IconChar icono = IconChar.CircleQuestion;
            Color tonoIcono = TemaEcoAccess.Verde;
            if (_modo == Modo.Caneca && _canecaElegida is { } c) imagen = RecursosEco.Imagen($"inicio/caneca_{NombreArchivo(c)}.png");
            else if (_modo == Modo.Residuo)
            {
                imagen = _residuo.Id is "botella-agua" ? RecursosEco.Imagen("inicio/botella_agua.png") : IconosMaterial.Ilustracion(_residuo);
                icono = _residuo.RequiereManejoEspecial ? IconChar.TriangleExclamation : IconosMaterial.Para(_residuo);
                if (_residuo.RequiereManejoEspecial || _residuo.DependeDeSuEstado) tonoIcono = TemaEcoAccess.Advertencia;
            }
            else icono = IconChar.MagnifyingGlass;
            if (imagen is not null)
            {
                var area = Rectangle.Inflate(_rIlustracion, -S(8), -S(8));
                Rectangle destino = CacheEscalado.Contener(imagen, area);
                if (IconosMaterial.Ilustracion(_residuo) == imagen && _modo == Modo.Residuo)
                {
                    using var ruta = RecursosEco.Redondeado(destino, S(14));
                    g.SetClip(ruta);
                    g.DrawImage(CacheEscalado.Obtener(imagen, destino.Width, destino.Height), destino.Location);
                    g.ResetClip();
                }
                else g.DrawImage(CacheEscalado.Obtener(imagen, destino.Width, destino.Height), destino.Location);
            }
            else
            {
                Bitmap dibujo = CacheIconos.Obtener(icono, _rIlustracion.Width * 45 / 100, tonoIcono);
                g.DrawImage(dibujo, _rIlustracion.X + (_rIlustracion.Width - dibujo.Width) / 2, _rIlustracion.Y + (_rIlustracion.Height - dibujo.Height) / 2);
            }
            // Separador vertical.
            using (var linea = new Pen(Color.FromArgb(0xD8, 0xEC, 0xD0), Math.Max(1, S(2))))
                g.DrawLine(linea, _rIlustracion.Right + S(11), r.Y + S(24), _rIlustracion.Right + S(11), r.Bottom - S(24));

            var bloques = new List<(string Texto, Font Fuente, Color Color, int Antes, bool Linea)>();
            Rectangle t = _rTextoResultado;
            int y = t.Y;
            // Etiqueta superior.
            string etiqueta;
            Color tonoEtiqueta = Color.White, fondoEtiqueta = Color.FromArgb(0x17, 0x6B, 0x3A);
            if (_modo == Modo.Caneca) etiqueta = "Caneca seleccionada";
            else if (_modo == Modo.SinResultados) { etiqueta = "Sin resultados"; fondoEtiqueta = TemaEcoAccess.Suave; }
            else if (_busqueda?.EsAproximada == true) { etiqueta = "¿Quisiste decir…?"; fondoEtiqueta = TemaEcoAccess.Advertencia; }
            else if (_residuo.RequiereManejoEspecial) { etiqueta = "Manejo especial"; fondoEtiqueta = TemaEcoAccess.Advertencia; }
            else etiqueta = _busqueda is null ? "Ejemplo de resultado" : "Resultado de la búsqueda";
            PintarPastilla(g, etiqueta, _rEtiqueta.X, _rEtiqueta.Y + _rEtiqueta.Height / 2, tonoEtiqueta, fondoEtiqueta, tamano: 13);

            Font nombre = FuentesInicio.Obtener(25 * _s, FontStyle.Bold);
            Font cuerpo = FuentesInicio.Obtener(15 * _s);
            Font fuerte = FuentesInicio.Semibold(16 * _s);
            Font pequena = FuentesInicio.Obtener(13.5f * _s);
            if (_modo == Modo.Caneca && _canecaElegida is { } caneca)
            {
                var info = CatalogoPrototipo.Canecas.First(x => x.Caneca == caneca);
                var depositables = CatalogoResiduos.DepositablesDe(caneca);
                bloques.Add((info.Nombre, nombre, TemaEcoAccess.Texto, 0, false));
                bloques.Add((info.Descripcion, fuerte, ColorCaneca(caneca), S(2), false));
                bloques.Add(("Recibe, entre otros: " + string.Join(", ", depositables.Take(7).Select(d => d.Nombre.Split('(')[0].Split(',')[0].Trim().ToLower(Colombia))) + ".", cuerpo, TemaEcoAccess.Texto, S(8), false));
                bloques.Add((TextoNoRecibe(caneca), pequena, TemaEcoAccess.Suave, S(6), false));
            }
            else if (_modo == Modo.SinResultados)
            {
                bloques.Add(($"No encontramos «{_textoSinResultados}»", nombre, TemaEcoAccess.Texto, 0, false));
                bloques.Add(("No inventamos una clasificación. Revisa la escritura, prueba con otro nombre (por ejemplo «botella», «cáscara» o «servilleta») o toca una de las canecas para ver lo que reciben.", cuerpo, TemaEcoAccess.Texto, S(8), false));
                bloques.Add(("Si es un residuo especial (pilas, medicamentos, aceite, aparatos), escríbelo así y te diremos adónde llevarlo.", pequena, TemaEcoAccess.Suave, S(6), false));
            }
            else
            {
                ResiduoCatalogo res = _residuo;
                bloques.Add((res.Nombre, nombre, TemaEcoAccess.Texto, 0, false));
                if (res.Caneca is { } destino)
                    bloques.Add(($"Deposítalo en la caneca {NombreArchivo(destino)} · {CatalogoPrototipo.Canecas.First(x => x.Caneca == destino).Descripcion.ToLower(Colombia)}", fuerte, ColorCaneca(destino), S(4), true));
                else
                    bloques.Add((CatalogoResiduos.NombreDestino(res.Ruta), fuerte, TemaEcoAccess.Advertencia, S(4), true));
                // Prioridad: disponibilidad real de la estación, aviso especial, instrucciones y, si cabe, el motivo.
                if (res.PermiteApertura && _disponibilidad is not null)
                    bloques.Add(((_disponible == true ? "● " : _disponible == false ? "● " : "○ ") + _disponibilidad, pequena,
                        _disponible == true ? TemaEcoAccess.VerdeOscuro : _disponible == false ? TemaEcoAccess.Error : TemaEcoAccess.Suave, S(6), false));
                if (res.RequiereManejoEspecial) bloques.Add((CatalogoResiduos.MensajeManejoEspecial, fuerte, TemaEcoAccess.Error, S(6), false));
                if (res.Caneca is not null && !res.PermiteApertura)
                    bloques.Add(("La apertura desde EcoAccess aún no está habilitada para este residuo: deposítalo tú mismo en esa caneca o elige uno equivalente.", pequena, TemaEcoAccess.Suave, S(6), false));
                bloques.Add((res.Instrucciones, cuerpo, TemaEcoAccess.Texto, S(6), false));
                if (!res.RequiereManejoEspecial && !string.IsNullOrWhiteSpace(res.Descripcion)) bloques.Add(("Por qué: " + res.Descripcion, cuerpo, TemaEcoAccess.Suave, S(6), false));
            }

            // Se dibuja en orden mientras quepa: el nombre siempre (al menos una línea); lo demás se recorta por líneas y lo
            // que no cabe queda en la ayuda emergente. Nunca se dibuja encima de los botones.
            var omitido = new List<string>();
            bool primero = true;
            foreach (var (texto, fuente, color, antes, lineaCaneca) in bloques)
            {
                if (!primero) y += antes;
                int disponible = t.Bottom - y;
                int alto = DibujoEco.AltoTexto(texto, fuente, t.Width);
                int lineasNombre = t.Height >= nombre.Height * 2 + fuerte.Height * 2 + S(8) ? 2 : 1;
                int maximo = fuente == nombre ? Math.Min(alto, fuente.Height * lineasNombre + 2) : alto;
                if (maximo > disponible)
                {
                    if (disponible >= fuente.Height || primero) { maximo = Math.Max(1, disponible / fuente.Height) * fuente.Height; omitido.Add(texto); }
                    else { omitido.Add(texto); continue; }
                }
                primero = false;
                int xTexto = t.X;
                if (lineaCaneca && _modo == Modo.Residuo && _residuo.Caneca is { } cc && RecursosEco.Imagen($"inicio/caneca_{NombreArchivo(cc)}.png") is { } mini)
                {
                    int altoMini = Math.Max(fuente.Height + S(10), S(34));
                    int anchoMini = altoMini * mini.Width / mini.Height;
                    g.DrawImage(CacheEscalado.Obtener(mini, anchoMini, altoMini), t.X, y - S(4));
                    xTexto += anchoMini + S(8);
                    alto = DibujoEco.AltoTexto(texto, fuente, t.Right - xTexto);
                    maximo = Math.Min(Math.Max(alto, altoMini - S(4)), Math.Max(fuente.Height, disponible));
                }
                TextRenderer.DrawText(g, texto, fuente, Rectangle.FromLTRB(xTexto, y, t.Right, y + maximo), color, DibujoEco.Parrafo | TextFormatFlags.EndEllipsis);
                y += maximo;
            }
            if (_alternativas.Any(a => a.Visible) && !_rRotuloOpciones.IsEmpty)
                TextRenderer.DrawText(g, RotuloOpciones, FuentesInicio.Semibold(13 * _s), _rRotuloOpciones, TemaEcoAccess.VerdeOscuro,
                    TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            _textoOmitido = string.Join("\n", omitido);
        }

        private void PintarPasos(Graphics g)
        {
            Rectangle r = _rPasos;
            Tarjeta(g, r, S(20), Color.White);
            Rectangle pastilla = PintarPastilla(g, "Así de fácil:", r.X + S(22), r.Y + r.Height / 2, Color.White, Color.FromArgb(0x17, 0x6B, 0x3A), tamano: 16);
            var pasos = new (string Imagen, string Titulo, string Texto)[]
            {
                ("inicio/paso_qr.png", "Identifica", "tu residuo"),
                ("inicio/paso_lupa.png", "Consulta", "con ECO o aquí"),
                ("inicio/paso_caneca.png", "Deposítalo", "en la caneca correcta"),
                ("inicio/paso_listo.png", "¡Listo!", "suma ecopuntos al verificarse"),
            };
            int inicio = pastilla.Right + S(24);
            int ancho = (r.Right - S(16) - inicio) / pasos.Length;
            Font titulo = FuentesInicio.Semibold(16 * _s), texto = FuentesInicio.Obtener(13.5f * _s);
            for (int i = 0; i < pasos.Length; i++)
            {
                int x = inicio + i * ancho;
                bool hecho = i + 1 < _paso || (i == 3 && _paso > 4), actual = i + 1 == _paso;
                int lado = Math.Min(S(64), r.Height - S(20));
                var icono = new Rectangle(x, r.Y + (r.Height - lado) / 2, lado, lado);
                if (actual)
                    using (var halo = new SolidBrush(Color.FromArgb(70, 0xB8, 0xF0, 0x4A))) g.FillEllipse(halo, Rectangle.Inflate(icono, S(5), S(5)));
                if (RecursosEco.Imagen(pasos[i].Imagen) is { } img)
                {
                    Rectangle destino = CacheEscalado.Contener(img, icono);
                    g.DrawImage(CacheEscalado.Obtener(img, destino.Width, destino.Height), destino.Location);
                }
                // Número (o marca si ya se cumplió).
                int n = S(24);
                var circulo = new Rectangle(icono.X - S(6), icono.Y - S(4), n, n);
                using (var pincel = new SolidBrush(hecho ? Color.FromArgb(0x6C, 0xBF, 0x2E) : Color.FromArgb(0x17, 0x6B, 0x3A))) g.FillEllipse(pincel, circulo);
                if (hecho) { Bitmap check = CacheIconos.Obtener(IconChar.Check, n * 3 / 5, Color.White); g.DrawImage(check, circulo.X + (n - check.Width) / 2, circulo.Y + (n - check.Height) / 2); }
                else TextRenderer.DrawText(g, (i + 1).ToString(CultureInfo.InvariantCulture), FuentesInicio.Semibold(13 * _s), circulo, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                int xt = icono.Right + S(10), anchoTexto = Math.Max(S(40), x + ancho - S(34) - xt);
                int altoBloque = titulo.Height + DibujoEco.AltoTexto(pasos[i].Texto, texto, anchoTexto);
                int yt = r.Y + Math.Max(S(4), (r.Height - Math.Min(altoBloque, r.Height - S(8))) / 2);
                TextRenderer.DrawText(g, pasos[i].Titulo, titulo, new Rectangle(xt, yt, anchoTexto, titulo.Height), actual ? Color.FromArgb(0x0E, 0x4D, 0x2C) : TemaEcoAccess.Texto,
                    TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                TextRenderer.DrawText(g, pasos[i].Texto, texto, Rectangle.FromLTRB(xt, yt + titulo.Height, xt + anchoTexto, r.Bottom - S(6)), TemaEcoAccess.Suave,
                    DibujoEco.Parrafo | TextFormatFlags.EndEllipsis);
                if (i < pasos.Length - 1)
                {
                    Bitmap flecha = CacheIconos.Obtener(IconChar.ChevronRight, S(20), Color.FromArgb(0x6C, 0xBF, 0x2E));
                    g.DrawImage(flecha, x + ancho - S(26), r.Y + (r.Height - flecha.Height) / 2);
                }
            }
        }

        private static string NombreArchivo(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => "verde",
            CategoriaMaterial.Aprovechable => "blanca",
            _ => "negra",
        };

        private static Color ColorCaneca(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => Color.FromArgb(0x1E, 0x8E, 0x3E),
            CategoriaMaterial.Aprovechable => Color.FromArgb(0x17, 0x6B, 0x3A),
            _ => Color.FromArgb(0x2B, 0x30, 0x36),
        };

        private static string TextoNoRecibe(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => "No recibe bolsas, empaques, servilletas ni residuos especiales.",
            CategoriaMaterial.Aprovechable => "Solo limpio y seco. Lo sucio o con grasa va a la negra; pilas, medicamentos y aparatos tienen entrega especial.",
            _ => "No recibe reciclables limpios, restos de comida ni residuos especiales (pilas, medicamentos, aceite, cortopunzantes).",
        };

        // ── Búsqueda, sugerencias, canecas y resultado ──────────────────────────────────────────────────────────────

        private void ActualizarSugerencias()
        {
            if (_cerrando || IsDisposed) return;
            IReadOnlyList<ResiduoCatalogo> lista = _campo.Focused ? CatalogoResiduos.Sugerir(_campo.Text) : Array.Empty<ResiduoCatalogo>();
            _sugerencias.BeginUpdate();
            _sugerencias.Items.Clear();
            foreach (ResiduoCatalogo r in lista) _sugerencias.Items.Add(r);
            _sugerencias.EndUpdate();
            _sugerencias.Visible = lista.Count > 0;
            if (lista.Count > 0)
            {
                _sugerencias.Height = Math.Min(6, lista.Count) * _sugerencias.ItemHeight + 4;
                _sugerencias.BringToFront();
            }
        }

        private void DibujarSugerencia(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || _sugerencias.Items[e.Index] is not ResiduoCatalogo r) return;
            bool elegido = (e.State & DrawItemState.Selected) != 0;
            using (var fondo = new SolidBrush(elegido ? TemaEcoAccess.VerdeSuave : Color.White)) e.Graphics.FillRectangle(fondo, e.Bounds);
            int lado = e.Bounds.Height * 55 / 100;
            Bitmap icono = CacheIconos.Obtener(IconosMaterial.Para(r), lado, r.RequiereManejoEspecial ? TemaEcoAccess.Advertencia : TemaEcoAccess.Verde);
            e.Graphics.DrawImage(icono, e.Bounds.X + S(12), e.Bounds.Y + (e.Bounds.Height - icono.Height) / 2);
            string destino = r.Caneca is { } c ? CatalogoPrototipo.NombreCaneca(c) : CatalogoResiduos.NombreDestino(r.Ruta).Split(':')[0];
            Font pequena = FuentesInicio.Obtener(13 * _s);
            Size medida = TextRenderer.MeasureText(destino, pequena);
            int xTexto = e.Bounds.X + S(12) + lado + S(12);
            TextRenderer.DrawText(e.Graphics, r.Nombre, _sugerencias.Font, Rectangle.FromLTRB(xTexto, e.Bounds.Y, e.Bounds.Right - medida.Width - S(16), e.Bounds.Bottom), TemaEcoAccess.Texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, destino, pequena, Rectangle.FromLTRB(e.Bounds.Right - medida.Width - S(10), e.Bounds.Y, e.Bounds.Right - S(6), e.Bounds.Bottom),
                r.Caneca is { } cc ? ColorCaneca(cc) : TemaEcoAccess.Advertencia, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        private void ElegirSugerencia(ResiduoCatalogo r)
        {
            _sugerencias.Visible = false;
            _busqueda = CatalogoResiduos.Consultar(r.Nombre);
            MostrarResiduo(r, _busqueda.Principal?.Id == r.Id ? _busqueda : null);
            _campo.Focus();
        }

        internal void Buscar(string? texto)
        {
            _sugerencias.Visible = false;
            if (string.IsNullOrWhiteSpace(texto)) { _campo.Focus(); return; }
            ResultadoBusquedaResiduo b = CatalogoResiduos.Consultar(texto);
            _busqueda = b;
            if (b.SinResultados)
            {
                _modo = Modo.SinResultados;
                _textoSinResultados = texto!.Trim();
                _canecaElegida = null;
                foreach (ZonaCaneca z in _zonas) z.Seleccionada = false;
                ConfigurarAcciones();
                _paso = 2;
                Adaptar();
                return;
            }
            MostrarResiduo(b.Principal!, b);
        }

        private void MostrarResiduo(ResiduoCatalogo residuo, ResultadoBusquedaResiduo? busqueda, bool consultarEstacion = true)
        {
            _modo = Modo.Residuo;
            _residuo = residuo;
            _busqueda = busqueda ?? _busqueda;
            if (busqueda is null && _busqueda?.Principal?.Id != residuo.Id) _busqueda = null;
            _canecaElegida = null;
            foreach (ZonaCaneca z in _zonas) z.Seleccionada = z.Caneca == residuo.Caneca;
            // Desambiguación: otras interpretaciones con destino distinto, para que la persona elija según su caso.
            IReadOnlyList<ResiduoCatalogo> opciones = _busqueda?.Principal?.Id == residuo.Id ? _busqueda.Alternativas : Array.Empty<ResiduoCatalogo>();
            for (int i = 0; i < _alternativas.Count; i++)
            {
                ResiduoCatalogo? o = i < opciones.Count ? opciones[i] : null;
                _alternativas[i].Tag = o;
                _alternativas[i].Text = o is null ? "" : $"{Corto(o.Nombre)} → {(o.Caneca is { } c ? NombreArchivo(c) : "especial")}";
            }
            _paso = residuo.PermiteApertura ? 3 : 2;
            _disponibilidad = null;
            _disponible = null;
            ConfigurarAcciones();
            Adaptar();
            if (consultarEstacion && residuo.PermiteApertura) _ = ConsultarDisponibilidadAsync();
        }

        private static string Corto(string nombre)
        {
            string s = nombre.Split('(')[0].Trim().TrimEnd(',');
            return s.Length > 30 ? s.Substring(0, 29).TrimEnd() + "…" : s;
        }

        private void ConfigurarAcciones()
        {
            bool invitado = _sesion is null;
            _accionPrincipal.Visible = true;
            _accionSecundaria.Visible = true;
            _accionPrincipal.Enabled = true;
            switch (_modo)
            {
                case Modo.Caneca:
                    _accionPrincipal.Text = $"Ver los {CatalogoResiduos.DepositablesDe(_canecaElegida!.Value).Count} residuos que recibe";
                    _accionPrincipal.Icono = IconChar.ChevronRight;
                    _accionSecundaria.Visible = false;
                    break;
                case Modo.SinResultados:
                    _accionPrincipal.Visible = false;
                    _accionSecundaria.Text = "Ver residuos especiales";
                    _accionSecundaria.Icono = IconChar.TriangleExclamation;
                    break;
                default:
                    if (_residuo.PermiteApertura)
                    {
                        _accionPrincipal.Text = _busqueda?.EsAproximada == true ? "Sí, es este: depositarlo" : invitado ? "Depositar como invitado" : "Depositar este residuo";
                        _accionPrincipal.Icono = IconChar.ChevronRight;
                        _accionPrincipal.Enabled = _disponible != false;
                        _accionSecundaria.Text = _busqueda is { Coincidencias: { Count: > 1 } } ? $"Ver los {_busqueda.Coincidencias.Count} resultados" : "Ver más residuos de esta caneca";
                        _accionSecundaria.Icono = IconChar.List;
                    }
                    else if (_residuo.RequiereManejoEspecial || _residuo.Ruta == RutaDisposicion.ReutilizacionDonacion)
                    {
                        _accionPrincipal.Text = "¿Dónde puedo llevarlo en Valledupar?";
                        _accionPrincipal.Icono = IconChar.LocationDot;
                        _accionSecundaria.Visible = _busqueda is { Coincidencias: { Count: > 1 } };
                        _accionSecundaria.Text = $"Ver los {_busqueda?.Coincidencias.Count ?? 0} resultados";
                        _accionSecundaria.Icono = IconChar.List;
                    }
                    else
                    {
                        _accionPrincipal.Visible = _residuo.Caneca is not null;
                        _accionPrincipal.Text = "Ver residuos habilitados de esta caneca";
                        _accionPrincipal.Icono = IconChar.ChevronRight;
                        _accionSecundaria.Visible = _busqueda is { Coincidencias: { Count: > 1 } };
                        _accionSecundaria.Text = $"Ver los {_busqueda?.Coincidencias.Count ?? 0} resultados";
                        _accionSecundaria.Icono = IconChar.List;
                    }
                    break;
            }
        }

        private void AccionPrincipal()
        {
            switch (_modo)
            {
                case Modo.Caneca:
                    VerResiduosDe(_canecaElegida!.Value);
                    break;
                case Modo.Residuo when _residuo.PermiteApertura:
                    AbrirAsistente(_residuo.Prototipo!);
                    break;
                case Modo.Residuo when _residuo.Caneca is { } c && !_residuo.RequiereManejoEspecial && _residuo.Ruta != RutaDisposicion.ReutilizacionDonacion:
                    VerResiduosDe(c);
                    break;
                case Modo.Residuo:
                    _ = MostrarPuntosAsync(_residuo);
                    break;
            }
        }

        private void AccionSecundaria()
        {
            if (_modo == Modo.SinResultados) { MostrarEspeciales(); return; }
            if (_busqueda is { Coincidencias: { Count: > 1 } } b) { ElegirEntre($"Resultados de «{b.Consulta}» ({b.Coincidencias.Count})", b.Coincidencias.Select(c => c.Residuo).ToList()); return; }
            if (_residuo.Caneca is { } caneca) VerResiduosDe(caneca);
        }

        private void ElegirCaneca(CategoriaMaterial caneca)
        {
            _modo = Modo.Caneca;
            _canecaElegida = caneca;
            foreach (ZonaCaneca z in _zonas) z.Seleccionada = z.Caneca == caneca;
            foreach (BotonEco a in _alternativas) a.Tag = null;
            _paso = Math.Max(_paso, 2);
            ConfigurarAcciones();
            Adaptar();
        }

        /// <summary>Catálogo visual completo; si el usuario elige un residuo, se muestra su destino como en una búsqueda.</summary>
        private void VerCatalogo()
        {
            using var ventana = new FormCatalogoMateriales(_canecaElegida);
            if (ventana.ShowDialog(this) == DialogResult.OK && ventana.Elegido is { } elegido && CatalogoResiduos.Buscar(elegido.Id) is { } r)
            {
                _busqueda = null;
                MostrarResiduo(r, null);
            }
        }

        private void VerResiduosDe(CategoriaMaterial caneca)
        {
            // Catálogo visual: una tarjeta por residuo único con imagen; el número sale del catálogo, no está escrito a mano.
            var lista = CatalogoPrototipo.VisiblesDe(caneca);
            var info = CatalogoPrototipo.Canecas.First(c => c.Caneca == caneca);
            using var ventana = new FormResiduosCaneca($"{info.Nombre} — {info.Descripcion}", "Elige el residuo que tienes para ver cómo depositarlo.", lista, caneca);
            if (ventana.ShowDialog(this) == DialogResult.OK && ventana.Elegido is { } elegido && CatalogoResiduos.Buscar(elegido.Id) is { } r)
            {
                _busqueda = null;
                MostrarResiduo(r, null);
            }
        }

        /// <summary>Lista de resultados de una búsqueda (los del piloto con su ilustración y los de orientación con su icono).</summary>
        private void ElegirEntre(string titulo, IReadOnlyList<ResiduoCatalogo> residuos)
        {
            using var ventana = new FormResultadosResiduo(titulo, residuos);
            if (ventana.ShowDialog(this) == DialogResult.OK && ventana.Elegido is { } elegido) MostrarResiduo(elegido, _busqueda?.Principal?.Id == elegido.Id ? _busqueda : null);
        }

        private void MostrarEspeciales() =>
            ElegirEntre("Residuos de manejo especial: no van en las tres canecas", CatalogoResiduos.ManejoEspecial);

        private async Task MostrarPuntosAsync(ResiduoCatalogo residuo)
        {
            IReadOnlyList<PuntoDisposicion> puntos;
            try
            {
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
                limite.CancelAfter(TimeSpan.FromSeconds(15));
                puntos = await (_servicios.Orientacion ?? new ServicioOrientacionEspecial()).PuntosAsync(residuo.Ruta, limite.Token);
            }
            catch (Exception) when (!_cerrando && !IsDisposed) { puntos = await new ServicioOrientacionEspecial().PuntosAsync(residuo.Ruta); }
            if (_cerrando || IsDisposed) return;
            using var ventana = new FormPuntosDisposicion(residuo.Nombre, residuo.Ruta, puntos, residuo.Precauciones ?? residuo.Instrucciones);
            ventana.ShowDialog(this);
        }

        /// <summary>Consulta en Oracle si la caneca de la estación que corresponde al residuo está en servicio y lo admite.</summary>
        private async Task ConsultarDisponibilidadAsync()
        {
            if (_residuo.Prototipo is not { } prototipo || _cerrando) return;
            _consultaDisponibilidad?.Cancel();
            var consulta = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
            _consultaDisponibilidad = consulta;
            consulta.CancelAfter(TimeSpan.FromSeconds(15));
            string nombreCaneca = CatalogoPrototipo.NombreCaneca(prototipo.Caneca).ToLower(Colombia);
            if (_disponibilidad is null) { _disponibilidad = $"Comprobando la {nombreCaneca} en Oracle…"; _lienzo.Invalidate(_rResultado); }
            try
            {
                var destino = await _servicios.CatalogoAmbiental.ResolverPrototipoAsync(prototipo, consulta.Token).WaitAsync(consulta.Token);
                if (consulta.IsCancellationRequested || _residuo.Prototipo != prototipo || IsDisposed) return;
                _disponible = destino.Exitoso;
                _disponibilidad = destino.Exitoso
                    ? $"Estación disponible: la {nombreCaneca} está en servicio y admite este residuo (Oracle, {HoraColombia.DesdeUtc(DateTime.UtcNow):HH:mm})."
                    + (_estacionSimulada ? " Modo demostración: la apertura será simulada." : "")
                    : (destino.Mensaje ?? "La caneca no está disponible.") + " No se enviará ninguna orden.";
                _oracleConectado = true;
            }
            catch (Exception) when (!_cierre.IsCancellationRequested && !IsDisposed)
            {
                if (_residuo.Prototipo != prototipo) return;
                _disponible = null;
                _disponibilidad = "No se pudo comprobar la estación: sin conexión con Oracle. El asistente lo volverá a intentar.";
            }
            catch (Exception) when (_cierre.IsCancellationRequested || IsDisposed) { return; }
            ConfigurarAcciones();
            AdaptarResultado();
            _lienzo.Invalidate();
        }

        // ── Datos reales del ciudadano y sincronización ─────────────────────────────────────────────────────────────

        /// <summary>Lee (sin iniciar ni detener nada) si en este equipo corre el simulador o el bridge físico de la estación.</summary>
        private async Task RevisarModoEstacionAsync()
        {
            try
            {
                var resumen = await Task.Run(() => EstadoServiciosDemostracion.Consultar());
                if (_cerrando || IsDisposed) return;
                bool simulador = resumen.Servicios.Any(x => x.Nombre == "Simulador" && x.Activo);
                bool bridge = resumen.Servicios.Any(x => x.Nombre == "Bridge" && x.Activo);
                string? modo = simulador ? TextoSimulada : bridge ? TextoFisica : null;
                _estacionSimulada = simulador;
                if (modo != _modoEstacion) { _modoEstacion = modo; Adaptar(); }
            }
            catch (Exception) when (!_cerrando) { /* Sin manifiesto o sin permisos: no se afirma ningún modo. */ }
        }

        private async Task CargarAsync()
        {
            await RevisarModoEstacionAsync();
            try
            {
                using var consulta = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
                consulta.CancelAfter(TimeSpan.FromSeconds(20));
                var demo = await _servicios.CatalogoAmbiental.ConsultarPilotoAsync(consulta.Token).WaitAsync(consulta.Token);
                _demo = demo.Exitoso ? demo.Valor : null;
                MarcarOracle(true);
            }
            catch (Exception) when (!_cerrando && !IsDisposed) { _demo = null; MarcarOracle(false); }
            catch (Exception) when (_cerrando || IsDisposed) { return; }
            if (_residuo.PermiteApertura) _ = ConsultarDisponibilidadAsync();
            await ActualizarMiEstadoAsync(automatica: true);
        }

        private void MarcarOracle(bool conectado)
        {
            _oracleConectado = conectado;
            _estadoOracle = conectado ? "Oracle conectado" : "Sin conexión con Oracle";
            if (_sesion is null)
            {
                _estadoActualizacion = conectado
                    ? $"Catálogo y estación consultados a las {HoraColombia.DesdeUtc(DateTime.UtcNow):HH:mm} · hora de Colombia."
                    : "Sin conexión con Oracle: puedes consultar el catálogo, pero no depositar hasta que vuelva.";
                _colorEstado = conectado ? TemaEcoAccess.Suave : TemaEcoAccess.Advertencia;
            }
            _lienzo.Invalidate(_rEstado);
        }

        // Saldo de ecopuntos, depósitos y su verificación. Nunca se inventan valores.
        private async Task CargarDatosCiudadanoAsync(CancellationToken ct)
        {
            int id = _sesion!.IdUsuario;
            var saldo = await _servicios.Ecopuntos.ConsultarMiosAsync(id, ct).WaitAsync(ct);
            DateTime hasta = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-5).Date.AddDays(1).AddHours(5), DateTimeKind.Utc);
            var actividad = await _servicios.Estadisticas.ObtenerMiasAsync(id, hasta.AddDays(-30), hasta, ct).WaitAsync(ct);
            var ultimos = await _servicios.Estadisticas.ListarMiosAsync(id, hasta.AddDays(-30), hasta, 1, 1, ct).WaitAsync(ct);
            if (IsDisposed) return;
            if (saldo.Exitoso) _saldo.Mostrar(saldo.Valor.Saldo.ToString("N0", Colombia), "Verificados.");
            else _saldo.Mostrar("—", "No disponible para esta cuenta.");
            if (!actividad.Exitoso)
            {
                foreach (var t in new[] { _depositos, _ultima, _participacion }) t.Mostrar("—", "No disponible para esta cuenta.");
                return;
            }
            var datos = actividad.Valor;
            int total = datos.Actual.Depositos.Total;
            int verificados = datos.Actual.Depositos.Verificados;
            _depositos.Mostrar(total.ToString("N0", Colombia), $"Últimos 30 días: {verificados:N0} verificados.");
            _participacion.Mostrar(verificados.ToString("N0", Colombia), "Depósitos verificados · Últimos 30 días.");
            if (ultimos.Exitoso && ultimos.Valor.Filas.Count > 0)
            {
                var ultimo = ultimos.Valor.Filas[0];
                // Último DEPÓSITO registrado (no la última consulta a Oracle), con su estado de verificación.
                _ultima.Mostrar(HoraColombia.DesdeUtc(ultimo.FechaDeteccionUtc).ToString("dd/MM/yyyy HH:mm", Colombia),
                    $"{ultimo.MaterialDeclarado} · {(ultimo.MaterialVerificado is null ? "pendiente de verificación" : "verificado")}.");
            }
            else _ultima.Mostrar("—", ultimos.Exitoso ? "Sin depósitos en los últimos 30 días." : "No disponible para esta cuenta.");
        }

        /// <summary>
        /// Vuelve a leer Oracle (no modifica ningún depósito). Una sola consulta a la vez, en segundo plano; con la ventana
        /// minimizada no se consulta. Si falla, espera el doble antes de reintentar (hasta 2 minutos), conserva los últimos
        /// valores y, pasado su tiempo de vigencia, los marca como desactualizados.
        /// </summary>
        private async Task ActualizarMiEstadoAsync(bool automatica)
        {
            if (_actualizando || _cerrando || IsDisposed) return;
            if (automatica && WindowState == FormWindowState.Minimized) return;
            if (_sesion is null)
            {
                // El visitante no tiene datos personales: solo se refresca la disponibilidad de la estación.
                if (_modo == Modo.Residuo && _residuo.PermiteApertura) await ConsultarDisponibilidadAsync();
                return;
            }
            _actualizando = true;
            _actualizar.Enabled = false;
            _actualizar.Text = "Actualizando…";
            if (!automatica) { _estadoActualizacion = "Consultando Oracle…"; _colorEstado = TemaEcoAccess.Suave; _lienzo.Invalidate(_rEstado); }
            using var consulta = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
            consulta.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                await CargarDatosCiudadanoAsync(consulta.Token);
                if (IsDisposed) return;
                _fallosSeguidos = 0;
                _ultimaActualizacionUtc = DateTime.UtcNow;
                _sincronizacion.Interval = (int)IntervaloSincronizacion.TotalMilliseconds;
                MarcarOracle(true);
                _estadoActualizacion = $"Actualizado a las {HoraColombia.DesdeUtc(DateTime.UtcNow):HH:mm:ss} · hora de Colombia · se actualiza solo cada {IntervaloSincronizacion.TotalSeconds:0} s.";
                _colorEstado = TemaEcoAccess.Suave;
                if (_modo == Modo.Residuo && _residuo.PermiteApertura) _ = ConsultarDisponibilidadAsync();
                _ = RevisarModoEstacionAsync();
            }
            catch (Exception) when (!_cerrando && !IsDisposed)
            {
                _fallosSeguidos++;
                TimeSpan espera = EsperaTrasFallos(_fallosSeguidos);
                _sincronizacion.Interval = (int)espera.TotalMilliseconds;
                MarcarOracle(false);
                if (_ultimaActualizacionUtc == DateTime.MinValue)
                    foreach (IndicadorInicio t in new[] { _saldo, _depositos, _ultima, _participacion }) t.Mostrar("—", "Sin datos: Oracle no responde.");
                _estadoActualizacion = _ultimaActualizacionUtc == DateTime.MinValue
                    ? $"Sin conexión con Oracle: no hay datos que mostrar todavía. Reintento automático en {espera.TotalSeconds:0} s."
                    : $"Sin conexión con Oracle: datos de las {HoraColombia.DesdeUtc(_ultimaActualizacionUtc):HH:mm}. Reintento en {espera.TotalSeconds:0} s.";
                _colorEstado = TemaEcoAccess.Advertencia;
            }
            catch (Exception) when (_cerrando || IsDisposed) { }
            finally
            {
                _actualizando = false;
                if (!IsDisposed)
                {
                    _actualizar.Enabled = true;
                    _actualizar.Text = "Actualizar mi estado";
                    RevisarVigencia();
                    _lienzo.Invalidate();
                }
            }
        }

        /// <summary>Marca los indicadores como desactualizados si la última lectura correcta ya no es vigente (sin consultar Oracle).</summary>
        private void RevisarVigencia()
        {
            if (_sesion is null || IsDisposed) return;
            bool viejo = _ultimaActualizacionUtc != DateTime.MinValue && PoliticaSincronizacion.EstaDesactualizado(_ultimaActualizacionUtc, DateTime.UtcNow);
            foreach (IndicadorInicio t in new[] { _saldo, _depositos, _ultima, _participacion }) t.Desactualizado = viejo;
        }

        internal static TimeSpan EsperaTrasFallos(int fallos) => PoliticaSincronizacion.EsperaTrasFallos(fallos);

        // ── Navegación a otras ventanas ─────────────────────────────────────────────────────────────────────────────

        private void MostrarQrPiloto()
        {
            // Siempre el QR oficial y definitivo del repositorio, nunca uno generado: es el que está impreso en la estación.
            var enlace = EnlaceTelegram.ParaCaneca(EnlaceTelegram.NombreBotPiloto, EnlaceTelegram.IdentificadorQrOficial);
            if (_demo is null || !_demo.Caneca.EstaEnServicio || _demo.Caneca.IdentificadorQr != EnlaceTelegram.IdentificadorQrOficial)
                MessageBox.Show(this, "Este es el QR oficial de la estación. En la base conectada ahora la caneca piloto no está en servicio con ese QR, así que al escanearlo ECO solo orientará y no abrirá la caneca.", "EcoAccess");
            using var qr = new FormQrCaneca(ServicioCatalogoAmbiental.NombreCanecaPiloto, enlace, RecursosEco.QrOficialPng(), vistaCiudadana: true);
            qr.ShowDialog(this); // El QR no crea ni autoriza una solicitud.
        }

        private void MostrarOpiniones()
        {
            if (_servicios.Opiniones is not { } opiniones) return;
            using var formulario = new FormOpiniones(opiniones, _sesion?.IdUsuario, sesionInvitada: _sesionOpinion);
            formulario.ShowDialog(this);
        }

        private void MostrarMisEcopuntos()
        {
            if (_sesion is null) { MessageBox.Show(this, "Inicia sesión para consultar tu historial personal. Los invitados no acumulan ecopuntos.", "EcoAccess"); return; }
            using var ventana = new FormMisEcopuntos(_servicios.Ecopuntos, _servicios.Estadisticas, _sesion.IdUsuario);
            ventana.ShowDialog(this);
            _ = ActualizarMiEstadoAsync(automatica: true);
        }

        private void MostrarMiActividad()
        {
            if (_sesion is null) return;
            using var ventana = new FormEstadisticasPersonales(_servicios.Estadisticas, _sesion.IdUsuario);
            ventana.ShowDialog(this);
        }

        private void AbrirAsistente(ResiduoPrototipo residuo)
        {
            using var asistente = new FormAsistenteDeposito(_servicios, _sesion, residuo);
            asistente.ShowDialog(this);
            if (asistente.SolicitarRegistro) { CrearCuenta(); return; }
            // Solo un depósito registrado en Oracle completa «Así de fácil»; un intento fallido no.
            if (asistente.DepositoRegistrado) _paso = 5;
            _lienzo.Invalidate();
            // Refresco inmediato al terminar una operación: el depósito nuevo aparece sin esperar al siguiente ciclo.
            _ = ActualizarMiEstadoAsync(automatica: true);
        }

        private void CrearCuenta()
        {
            using var registro = new FormRegistroCiudadano(_servicios.RegistroCiudadanos);
            if (registro.ShowDialog(this) == DialogResult.OK) VolverAlInicio();
        }

        private void VolverAlInicio()
        {
            SolicitarInicioSesion = true;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sincronizacion.Dispose();
                _reloj.Dispose();
                _esperaSugerir.Dispose();
                _ayudas.Dispose();
                _consultaDisponibilidad?.Dispose();
                _cierre.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>Resultados de una búsqueda o de los residuos especiales, en una lista con desplazamiento propio.</summary>
    public sealed class FormResultadosResiduo : Form
    {
        public ResiduoCatalogo? Elegido { get; private set; }

        public FormResultadosResiduo(string titulo, IReadOnlyList<ResiduoCatalogo> residuos)
        {
            Text = titulo + " · EcoAccess";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            MinimizeBox = false;
            KeyPreview = true;
            Size = new Size(TemaEcoAccess.Px(this, 760), TemaEcoAccess.Px(this, 640));
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            var cabecera = new Label { Text = titulo, Dock = DockStyle.Top, AutoSize = false, Height = TemaEcoAccess.Px(this, 56), Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro, Padding = new Padding(20, 0, 20, 0), TextAlign = ContentAlignment.MiddleLeft };
            var lista = new ListBox { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = TemaEcoAccess.Px(this, 58), BorderStyle = BorderStyle.None, IntegralHeight = false, Font = TemaEcoAccess.Entrada, AccessibleName = titulo };
            foreach (ResiduoCatalogo r in residuos) lista.Items.Add(r);
            lista.DrawItem += (_, e) =>
            {
                if (e.Index < 0 || lista.Items[e.Index] is not ResiduoCatalogo r) return;
                bool sel = (e.State & DrawItemState.Selected) != 0;
                using (var fondo = new SolidBrush(sel ? TemaEcoAccess.VerdeSuave : e.Index % 2 == 0 ? Color.White : Color.FromArgb(0xFA, 0xFC, 0xFB))) e.Graphics.FillRectangle(fondo, e.Bounds);
                int lado = e.Bounds.Height - TemaEcoAccess.Px(this, 14);
                var area = new Rectangle(e.Bounds.X + TemaEcoAccess.Px(this, 14), e.Bounds.Y + (e.Bounds.Height - lado) / 2, lado, lado);
                if (IconosMaterial.Ilustracion(r) is { } img) e.Graphics.DrawImage(CacheEscalado.Obtener(img, lado, lado), area.Location);
                else DibujoEco.Icono(e.Graphics, IconosMaterial.Para(r), r.RequiereManejoEspecial ? TemaEcoAccess.Advertencia : TemaEcoAccess.Verde,
                    r.RequiereManejoEspecial ? TemaEcoAccess.AdvertenciaSuave : TemaEcoAccess.VerdeSuave, area, 10);
                int x = area.Right + TemaEcoAccess.Px(this, 14);
                string destino = (r.Caneca is { } c ? CatalogoPrototipo.NombreCaneca(c) : CatalogoResiduos.NombreDestino(r.Ruta)) + (r.PermiteApertura ? " · se puede depositar en la estación" : "");
                TextRenderer.DrawText(e.Graphics, r.Nombre, TemaEcoAccess.Subtitulo, new Rectangle(x, e.Bounds.Y + TemaEcoAccess.Px(this, 8), e.Bounds.Right - x - 10, TemaEcoAccess.Px(this, 22)), TemaEcoAccess.Texto, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(e.Graphics, destino, TemaEcoAccess.Cuerpo, new Rectangle(x, e.Bounds.Y + TemaEcoAccess.Px(this, 30), e.Bounds.Right - x - 10, TemaEcoAccess.Px(this, 20)),
                    r.RequiereManejoEspecial ? TemaEcoAccess.Advertencia : TemaEcoAccess.Suave, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            };
            void Elegir() { if (lista.SelectedItem is ResiduoCatalogo r) { Elegido = r; DialogResult = DialogResult.OK; } }
            lista.DoubleClick += (_, _) => Elegir();
            lista.MouseClick += (_, _) => Elegir();
            lista.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Elegir(); } };
            Controls.Add(lista);
            Controls.Add(cabecera);
        }
    }
}
