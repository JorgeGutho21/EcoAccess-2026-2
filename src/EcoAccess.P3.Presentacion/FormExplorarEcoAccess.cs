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
    /// Inicio del visitante y del ciudadano. Separa dos cosas: explorar (guía, catálogo y cómo funciona, sin cuenta) y
    /// depositar en la caneca DEMO con el asistente. El visitante deposita, pero no acumula ecopuntos; el ciudadano ve
    /// sus datos reales de Oracle. No recibe servicios administrativos: todo pasa por la lógica de negocio.
    /// </summary>
    public sealed class FormExplorarEcoAccess : Form
    {
        private const int AnchoMaximo = 1180;

        private readonly ServiciosAplicacion _servicios;
        private readonly SesionActiva? _sesion;
        private readonly string _sesionOpinion = Guid.NewGuid().ToString("N");
        private readonly string? _qrDemo;
        private readonly Panel _scroll = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(32, 28, 32, 24) };
        private readonly TableLayoutPanel _columna = new() { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo };
        private readonly RejillaTarjetas _acciones = new(200);
        private readonly RejillaTarjetas _datos = new(220) { ColumnasMaximas = 4 };
        private readonly RejillaTarjetas _categorias = new(200);
        private readonly RejillaTarjetas _resultados = new(360) { ColumnasMaximas = 2 };
        private readonly RejillaTarjetas _pasos = new(200);
        private readonly CajaBusqueda _busqueda = new() { AccessibleName = "Buscar un residuo en el catálogo" };
        private readonly KryptonButton _buscar = new() { Text = "Buscar", AutoSize = true, AccessibleName = "Buscar material" };
        private readonly Label _estadoBusqueda = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave };
        private readonly KryptonButton _anterior = new() { Text = "Anterior", AutoSize = true, Enabled = false };
        private readonly KryptonButton _proxima = new() { Text = "Siguiente", AutoSize = true, Enabled = false };
        private int _paginaCatalogo = 1;
        private string _textoCatalogo = "";
        private bool _haySiguiente;
        private readonly Label _seccionCategorias = Titulo("Aprende a separar");
        private readonly Label _seccionBusqueda = Titulo("¿Dónde va tu residuo?");
        private readonly Label _seccionPasos = Titulo("Cómo funciona EcoAccess");
        private readonly List<Control> _anchoCompleto = new List<Control>();
        private readonly TarjetaDato _saldo = new(IconChar.Coins, "Mis ecopuntos");
        private readonly TarjetaDato _depositos = new(IconChar.Recycle, "Mis depósitos");
        private readonly TarjetaDato _ultima = new(IconChar.ClockRotateLeft, "Última actividad");
        private readonly TarjetaDato _participacion = new(IconChar.CircleCheck, "Mi participación");
        private CanecaPublica? _demo;
        private CancellationTokenSource? _consulta;
        private bool _cerrando;

        public bool SolicitarInicioSesion { get; private set; }

        public FormExplorarEcoAccess(ServiciosAplicacion servicios, SesionActiva? sesion, string? qrCanecaDemo)
        {
            _servicios = servicios;
            _sesion = sesion;
            _qrDemo = qrCanecaDemo;
            Text = sesion is null ? "EcoAccess · Explora y deposita como invitado" : $"EcoAccess · {sesion.Nombre}";
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            Font = TemaEcoAccess.Cuerpo;
            Size = new Size(1280, 850);
            MinimumSize = new Size(760, 560);
            WindowState = FormWindowState.Maximized;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;

            _scroll.Controls.Add(_columna);
            Controls.Add(_scroll);
            Controls.Add(CrearBarraSuperior());

            Agregar(CrearBienvenida(), new Padding(0, 0, 0, 20));
            if (sesion is null)
            {
                Agregar(CrearBeneficios(), new Padding(0, 0, 0, 20));
            }
            else
            {
                foreach (TarjetaDato dato in new[] { _saldo, _depositos, _ultima, _participacion }) _datos.Controls.Add(dato);
                _columna.Controls.Add(_datos);
                _datos.Margin = new Padding(0, 0, 0, 4);
            }

            CrearAcciones();
            _columna.Controls.Add(_acciones);
            _acciones.Margin = new Padding(0, 0, 0, 4);

            if (_servicios.Opiniones is { } opiniones)
                Agregar(Boton("Tu experiencia con EcoAccess", false, (_, _) =>
                {
                    using var formulario = new FormOpiniones(opiniones, _sesion?.IdUsuario, sesionInvitada: _sesionOpinion);
                    formulario.ShowDialog(this);
                }), new Padding(0, 12, 0, 4));

            Agregar(_seccionCategorias, new Padding(0, 20, 0, 4));
            Agregar(Ayuda("Conoce el código nacional de colores. Nuestra caneca piloto tiene un solo compartimiento y recibe únicamente sus materiales admitidos."), new Padding(0, 0, 0, 14));
            foreach (IndicacionSeparacion guia in GuiaSeparacion.Todas)
            {
                var tarjeta = new TarjetaCategoria(guia);
                tarjeta.AltoCambiado += (_, _) => _categorias.PerformLayout();
                _categorias.Controls.Add(tarjeta);
            }
            _columna.Controls.Add(_categorias);
            _categorias.Margin = new Padding(0, 0, 0, 4);

            Agregar(_seccionBusqueda, new Padding(0, 20, 0, 4));
            Agregar(Ayuda("Busca por nombre: consulta cómo entregarlo y si la caneca piloto lo recibe."), new Padding(0, 0, 0, 14));
            Agregar(CrearFilaBusqueda(), Padding.Empty);
            Agregar(_estadoBusqueda, new Padding(0, 10, 0, 14));
            _estadoBusqueda.Text = "Prueba con «botella», «cartón», «pila» o «servilleta».";
            _columna.Controls.Add(_resultados);
            _resultados.Margin = new Padding(0, 0, 0, 4);
            var navegacion = new FlowLayoutPanel { AutoSize = true, BackColor = TemaEcoAccess.Fondo, Margin = new Padding(0, 0, 0, 16) };
            TemaEcoAccess.EstilizarAccion(_anterior); TemaEcoAccess.EstilizarAccion(_proxima);
            navegacion.Controls.Add(_anterior); navegacion.Controls.Add(_proxima);
            Agregar(navegacion, Padding.Empty);

            Agregar(_seccionPasos, new Padding(0, 20, 0, 4));
            Agregar(Ayuda("Puedes seguir los estados de tu solicitud sin confundirla con un depósito verificado."), new Padding(0, 0, 0, 14));
            CrearPasos();
            _columna.Controls.Add(_pasos);
            Agregar(Ayuda("El registro manual disponible aquí es una simulación de software. Sus eventos no demuestran movimiento físico ni ingreso real."), new Padding(0, 4, 0, 36));

            _buscar.Click += async (_, _) => await BuscarAsync(_busqueda.Campo.Text);
            _anterior.Click += async (_, _) => await CargarPaginaAsync(_textoCatalogo, _paginaCatalogo - 1);
            _proxima.Click += async (_, _) => await CargarPaginaAsync(_textoCatalogo, _paginaCatalogo + 1);
            _busqueda.Campo.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                await BuscarAsync(_busqueda.Campo.Text);
            };
            _scroll.ClientSizeChanged += (_, _) => Adaptar();
            Shown += async (_, _) => { Adaptar(); await CargarAsync(); };
            FormClosing += (_, _) => { _cerrando = true; _consulta?.Cancel(); };
        }

        private static Label Titulo(string texto) =>
            new() { Text = texto, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.Texto };

        private static Label Ayuda(string texto) =>
            new() { Text = texto, AutoSize = true, ForeColor = TemaEcoAccess.Suave };

        private void Agregar(Control control, Padding margen)
        {
            control.Margin = margen;
            _anchoCompleto.Add(control);
            _columna.Controls.Add(control);
        }

        // Barra superior: logotipo y, según quien entra, crear cuenta e iniciar sesión, o el nombre y cerrar sesión.
        private Control CrearBarraSuperior()
        {
            var barra = new TableLayoutPanel { Dock = DockStyle.Top, Height = TemaEcoAccess.Px(this, 68), ColumnCount = 2, BackColor = TemaEcoAccess.Superficie, Padding = new Padding(24, 0, 24, 0) };
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            barra.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            int altoLogo = TemaEcoAccess.Px(this, 30);
            barra.Controls.Add(new LogoEcoAccess { Size = new Size((int)(altoLogo * (double)RecursosEco.Logo.Width / RecursosEco.Logo.Height), altoLogo), Anchor = AnchorStyles.Left }, 0, 0);
            var derecha = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            if (_sesion is null)
            {
                var pastilla = new PastillaEstado { Margin = new Padding(0, 7, 14, 0) };
                pastilla.Mostrar("Modo invitado", TemaEcoAccess.Suave, TemaEcoAccess.Fondo);
                derecha.Controls.Add(pastilla);
                derecha.Controls.Add(Boton("Iniciar sesión", false, (_, _) => VolverAlInicio()));
                derecha.Controls.Add(Boton("Crear cuenta", true, (_, _) => CrearCuenta()));
            }
            else
            {
                var pastilla = new PastillaEstado { Margin = new Padding(0, 7, 14, 0) };
                pastilla.Mostrar("Ciudadano", TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave);
                derecha.Controls.Add(pastilla);
                derecha.Controls.Add(new Label { Text = _sesion.Nombre, AutoSize = true, Font = TemaEcoAccess.Subtitulo, Margin = new Padding(0, 9, 16, 0) });
                derecha.Controls.Add(Boton("Cerrar sesión", false, (_, _) => VolverAlInicio()));
            }
            barra.Controls.Add(derecha, 1, 0);
            barra.Paint += (_, e) =>
            {
                using var borde = new Pen(TemaEcoAccess.Borde);
                e.Graphics.DrawLine(borde, 0, barra.Height - 1, barra.Width, barra.Height - 1);
            };
            return barra;
        }

        private static KryptonButton Boton(string texto, bool principal, EventHandler accion)
        {
            var boton = new KryptonButton { Text = texto, AutoSize = true, Margin = new Padding(8, 0, 0, 0), AccessibleName = texto };
            TemaEcoAccess.EstilizarAccion(boton, principal);
            boton.Click += accion;
            return boton;
        }

        private Control CrearBienvenida()
        {
            var tarjeta = new TarjetaEco { Relleno = TemaEcoAccess.VerdeSuave, ColorBorde = TemaEcoAccess.VerdeHoja, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var interior = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, BackColor = TemaEcoAccess.VerdeSuave };
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var textos = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.VerdeSuave, Padding = new Padding(10, 8, 0, 4) };
            string nombre = _sesion?.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var titulo = new Label { Text = _sesion is null ? "¡Bienvenido a EcoAccess!" : $"¡Hola, {nombre}!", AutoSize = true, Font = new Font("Segoe UI Semibold", 24), ForeColor = TemaEcoAccess.VerdeOscuro, Margin = Padding.Empty };
            var texto = new Label
            {
                Text = _sesion is null
                    ? "Explora cómo separar tus residuos y conoce la caneca piloto. Como invitado puedes probar la simulación, sin acumular ecopuntos."
                    : "Cada residuo cuenta. Registra tu residuo, consulta tu actividad y sigue tus ecopuntos verificados.",
                AutoSize = true,
                Font = TemaEcoAccess.Entrada,
                ForeColor = TemaEcoAccess.Texto,
                Margin = new Padding(0, 10, 0, 18),
            };
            textos.Controls.AddRange(new Control[] { titulo, texto });
            textos.SizeChanged += (_, _) => texto.MaximumSize = new Size(Math.Max(200, textos.Width - 30), 0);
            int lado = TemaEcoAccess.Px(this, 150);
            var eco = new ImagenRedondeada { Imagen = RecursosEco.Eco, Size = new Size(lado, lado), Anchor = AnchorStyles.None, Margin = new Padding(16, 4, 16, 4), AccessibleName = "Eco, la mascota de EcoAccess", AccessibleRole = AccessibleRole.Graphic };
            interior.Controls.Add(textos, 0, 0);
            interior.Controls.Add(eco, 1, 0);
            tarjeta.Controls.Add(interior);
            tarjeta.SizeChanged += (_, _) => eco.Visible = tarjeta.Width > TemaEcoAccess.Px(this, 640);
            return tarjeta;
        }

        // Invitación natural a registrarse: qué gana, sin prometer canjes que todavía no existen.
        private Control CrearBeneficios()
        {
            var tarjeta = new TarjetaEco { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var interior = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, BackColor = TemaEcoAccess.Superficie };
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            interior.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            int lado = TemaEcoAccess.Px(this, 52);
            interior.Controls.Add(new TarjetaIcono(IconChar.Award) { Size = new Size(lado, lado), Anchor = AnchorStyles.Top, Margin = new Padding(4, 2, 16, 0) }, 0, 0);
            var textos = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            var detalle = new Label { Text = "Crea tu cuenta para llevar un historial de tus depósitos y obtener puntos por tus acciones verificadas. Como invitado puedes depositar, pero esos depósitos no suman ecopuntos.", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 6, 0, 0) };
            textos.Controls.Add(new Label { Text = "¿Sabías que puedes acumular ecopuntos?", AutoSize = true, Font = TemaEcoAccess.Subtitulo, Margin = Padding.Empty });
            textos.Controls.Add(detalle);
            textos.SizeChanged += (_, _) => detalle.MaximumSize = new Size(Math.Max(200, textos.Width - 10), 0);
            interior.Controls.Add(textos, 1, 0);
            tarjeta.Controls.Add(interior);
            return tarjeta;
        }

        private void CrearAcciones()
        {
            TarjetaAccionRapida Accion(IconChar icono, string titulo, string texto, Action accion, bool destacada = false)
            {
                var tarjeta = new TarjetaAccionRapida(icono, titulo, texto, destacada);
                tarjeta.Click += (_, _) => accion();
                return tarjeta;
            }

            _acciones.Controls.Add(Accion(IconChar.Qrcode, "Continuar con QR", "Escanea el QR de la caneca piloto y continúa desde tu teléfono.", MostrarQrPiloto, destacada: true));
            _acciones.Controls.Add(Accion(IconChar.BoxOpen, "Registrar residuo manualmente", "Elige un material; la solicitud se verifica antes del depósito.", () => AbrirAsistente(null)));
            _acciones.Controls.Add(Accion(IconChar.Coins, "Mis ecopuntos e historial", "Consulta tu saldo y sus movimientos verificados.", MostrarMisEcopuntos));
            _acciones.Controls.Add(Accion(IconChar.Seedling, "Próximamente", "Conoce los planes de crecimiento de EcoAccess en la UPC.", MostrarProximamente));
        }

        private void MostrarQrPiloto()
        {
            if (_demo is null || !_demo.Caneca.EstaEnServicio)
            {
                MessageBox.Show(this, "La caneca piloto no está disponible en este momento. Vuelve a intentarlo.", "EcoAccess"); return;
            }
            var enlace = EnlaceTelegram.ParaCaneca(_servicios.NombreBotTelegram ?? EnlaceTelegram.NombreBotPiloto, _demo.Caneca.IdentificadorQr);
            using var qr = new FormQrCaneca(ServicioCatalogoAmbiental.NombreCanecaPiloto, enlace, CodigoQrCaneca.Png(enlace), vistaCiudadana: true);
            qr.ShowDialog(this); // El inicio queda abierto; el QR no crea ni autoriza una solicitud.
        }

        private void MostrarMisEcopuntos()
        {
            if (_sesion is null) { MessageBox.Show(this, "Vincula o inicia sesión para consultar tu historial personal. Los invitados no acumulan ecopuntos.", "EcoAccess"); return; }
            using var historial = new FormEcopuntos(_servicios.Ecopuntos, _servicios.Materiales, _sesion.IdUsuario, mostrarAdministracion: false,
                vistaCiudadana: true, estadisticas: _servicios.Estadisticas);
            historial.ShowDialog(this);
        }

        private void MostrarProximamente()
        {
            using var ventana = new Form
            {
                Text = "Próximamente · EcoAccess",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(900, 700),
                MinimumSize = new Size(650, 500),
                BackColor = TemaEcoAccess.Fondo,
                Font = TemaEcoAccess.Cuerpo,
                AutoScaleMode = AutoScaleMode.Dpi
            };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24), BackColor = TemaEcoAccess.Fondo };
            var planes = new RejillaTarjetas(280) { ColumnasMaximas = 2, Dock = DockStyle.Top };
            foreach (var plan in ProyeccionesEcoAccess.Todas)
                planes.Controls.Add(new TarjetaAccionRapida(IconChar.Seedling, plan.Titulo, plan.Descripcion, interactiva: false));
            var estructura = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
            estructura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            estructura.Controls.Add(planes);
            estructura.Controls.Add(new Label
            {
                AutoSize = true,
                Font = TemaEcoAccess.Subtitulo,
                Text = "Proyección por bloques · sin instalaciones activas",
                Margin = new Padding(0, 20, 0, 10)
            });
            var campus = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 4, Padding = new Padding(0, 0, 0, 20) };
            campus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            for (int i = 0; i < 3; i++) campus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            foreach (string bloque in new[] { "Bloque A", "Bloque B", "Bloque C", "Bloque H", "Bloque I", "Bloque P", "Comedor universitario" })
            {
                int fila = campus.RowCount++;
                campus.Controls.Add(new Label { Text = bloque, AutoSize = true, Margin = new Padding(0, 8, 8, 8) }, 0, fila);
                int columna = 1;
                foreach (var color in new[] { (Texto: "Blanca", Fondo: Color.White, Tinta: TemaEcoAccess.Texto),
                    (Texto: "Negra", Fondo: Color.FromArgb(31, 41, 55), Tinta: Color.White),
                    (Texto: "Verde", Fondo: TemaEcoAccess.VerdeOscuro, Tinta: Color.White) })
                    campus.Controls.Add(new Label
                    {
                        Text = color.Texto,
                        BackColor = color.Fondo,
                        ForeColor = color.Tinta,
                        BorderStyle = BorderStyle.FixedSingle,
                        Dock = DockStyle.Fill,
                        AutoSize = true,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Padding = new Padding(8),
                        Margin = new Padding(4),
                        AccessibleName = $"{bloque}, caneca {color.Texto}, proyección futura"
                    }, columna++, fila);
            }
            estructura.Controls.Add(campus);
            scroll.Controls.Add(estructura); ventana.Controls.Add(scroll);
            ventana.Shown += (_, _) => planes.Adaptar(Math.Max(500, scroll.ClientSize.Width - 64));
            ventana.ClientSizeChanged += (_, _) => planes.Adaptar(Math.Max(500, scroll.ClientSize.Width - 64));
            ventana.ShowDialog(this);
        }

        private void CrearPasos()
        {
            (IconChar Icono, string Titulo, string Texto)[] pasos =
            new (IconChar Icono, string Titulo, string Texto)[]
    {
        (IconChar.HandPointer, "1. Elige tu residuo", "Búscalo o selecciónalo en el asistente. Solo se habilitan los materiales que la caneca recibe."),
        (IconChar.Qrcode, "2. Confirma la caneca", "El QR identifica la caneca piloto; por sí solo no autentica ni autoriza una apertura."),
        (IconChar.DoorOpen, "3. Sigue tu operación", "La orden debe estar autorizada. Solo se muestran los eventos recibidos; una simulación se identifica como tal."),
        (IconChar.ClipboardCheck, "4. Verificación y ecopuntos", "La verificación independiente y una tarifa vigente son necesarias para acreditar puntos. Declarar una cantidad no los concede.")
    };
            foreach (var paso in pasos) _pasos.Controls.Add(new TarjetaAccionRapida(paso.Icono, paso.Titulo, paso.Texto, interactiva: false));
        }

        private Control CrearFilaBusqueda()
        {
            var fila = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, BackColor = TemaEcoAccess.Fondo };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _busqueda.Height = TemaEcoAccess.Px(this, 50);
            _busqueda.Dock = DockStyle.Fill;
            _busqueda.Margin = new Padding(0, 0, 12, 0);
            _busqueda.Campo.MaxLength = ServicioCatalogoAmbiental.LargoMaximoBusqueda;
            _busqueda.Campo.AccessibleName = "Buscar un residuo en el catálogo";
            TemaEcoAccess.EstilizarAccion(_buscar, principal: true);
            _buscar.MinimumSize = new Size(TemaEcoAccess.Px(this, 120), TemaEcoAccess.Px(this, 50));
            _buscar.Margin = Padding.Empty;
            fila.Controls.Add(_busqueda, 0, 0);
            fila.Controls.Add(_buscar, 1, 0);
            return fila;
        }

        private void Adaptar()
        {
            int margen = TemaEcoAccess.Px(this, 32);
            int ancho = Math.Min(TemaEcoAccess.Px(this, AnchoMaximo), Math.Max(TemaEcoAccess.Px(this, 320), _scroll.ClientSize.Width - 2 * margen));
            _columna.SuspendLayout();
            _columna.MinimumSize = _columna.MaximumSize = new Size(ancho, 0);
            foreach (Control control in _anchoCompleto)
            {
                int disponible = Math.Max(200, ancho - control.Margin.Horizontal);
                if (control is Label etiqueta) etiqueta.MaximumSize = new Size(disponible, 0);
                else control.MinimumSize = control.MaximumSize = new Size(disponible, 0);
            }
            foreach (RejillaTarjetas rejilla in new[] { _datos, _acciones, _categorias, _resultados, _pasos }) rejilla.Adaptar(Math.Max(200, ancho - rejilla.Margin.Horizontal));
            _columna.ResumeLayout(true);
            // Dock/AutoScroll conservan el desplazamiento vertical sin realimentar una posición virtual horizontal.
        }

        /// <summary>Desplaza la página hasta una sección y, si se indica, deja el foco en el control.</summary>
        private void IrA(Control seccion, Control? foco)
        {
            int destino = seccion.Top + _columna.Top - _scroll.AutoScrollPosition.Y - TemaEcoAccess.Px(this, 12);
            _scroll.AutoScrollPosition = new Point(0, Math.Max(0, destino));
            foco?.Focus();
        }

        private async Task CargarAsync()
        {
            using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try
            {
                // Ejemplos reales de cada categoría: primero los que tienen una indicación específica.
                var primera = await _servicios.CatalogoAmbiental.ConsultarPaginaPublicaAsync("", cancellationToken: consulta.Token).WaitAsync(consulta.Token);
                if (!primera.Exitoso) throw new InvalidOperationException("No se pudo cargar el catálogo público.");
                var catalogo = primera.Valor.Filas;
                if (IsDisposed) return;
                _haySiguiente = primera.Valor.HaySiguiente; _proxima.Enabled = _haySiguiente;
                _estadoBusqueda.Text = $"Página 1 · {catalogo.Count} materiales" + (_haySiguiente ? " · Hay más resultados" : "");
                foreach (TarjetaCategoria tarjeta in _categorias.Controls.OfType<TarjetaCategoria>())
                {
                    tarjeta.MostrarEjemplos(catalogo.Where(m => m.Categoria == tarjeta.Categoria)
                        .OrderBy(m => Compat.IsNullOrWhiteSpace(m.Descripcion)).ThenBy(m => m.Nombre, StringComparer.CurrentCulture).ToList());
                }
                var demo = await _servicios.CatalogoAmbiental.ConsultarCanecaAsync(_qrDemo, consulta.Token).WaitAsync(consulta.Token);
                _demo = demo.Exitoso ? demo.Valor : null;
                MostrarResultados(catalogo);
                if (_sesion is not null) await CargarDatosCiudadanoAsync(consulta.Token);
            }
            catch (Exception) when (!_cerrando && !IsDisposed)
            {
                _estadoBusqueda.Text = "No pudimos consultar el catálogo en este momento. La guía de separación sigue disponible.";
                _estadoBusqueda.ForeColor = TemaEcoAccess.Error;
            }
            catch (Exception) when (_cerrando || IsDisposed) { }
        }

        // Datos reales del ciudadano: saldo de ecopuntos, depósitos y su verificación. Nunca se inventan valores.
        private async Task CargarDatosCiudadanoAsync(CancellationToken ct)
        {
            int id = _sesion!.IdUsuario;
            var saldo = await _servicios.Ecopuntos.ConsultarMiosAsync(id, ct).WaitAsync(ct);
            DateTime hasta = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-5).Date.AddDays(1).AddHours(5), DateTimeKind.Utc);
            var actividad = await _servicios.Estadisticas.ObtenerMiasAsync(id, hasta.AddDays(-30), hasta, ct).WaitAsync(ct);
            if (IsDisposed) return;
            var colombia = System.Globalization.CultureInfo.GetCultureInfo("es-CO");
            if (saldo.Exitoso) _saldo.Mostrar(saldo.Valor.Saldo.ToString("N0", colombia), "Se acreditan cuando un administrador verifica tu depósito.");
            else _saldo.Mostrar("—", "No disponible para esta cuenta.");
            if (!actividad.Exitoso)
            {
                foreach (var t in new[] { _depositos, _ultima, _participacion }) t.Mostrar("—", "No disponible para esta cuenta.");
            }
            else
            {
                var datos = actividad.Valor;
                int total = datos.Actual.Depositos.Total;
                int verificados = datos.Actual.Depositos.Verificados;
                _depositos.Mostrar(total.ToString("N0", colombia), $"Últimos 30 días · {total - verificados:N0} pendientes de verificación");
                _participacion.Mostrar(verificados.ToString("N0", colombia), "Depósitos verificados · últimos 30 días.");
                var ultimaFecha = datos.Evolucion.Where(f => f.Depositos > 0 && f.Dia is not null).MaxBy(f => f.Dia);
                if (ultimaFecha is null)
                {
                    _ultima.Mostrar("—", "Sin depósitos en los últimos 30 días.");
                }
                else
                {
                    _ultima.Mostrar(ultimaFecha.Dia!.Value.ToString("dd/MM/yyyy", colombia), "Último día con depósitos en el período · Colombia.");
                }
            }
            Adaptar();
        }

        private async Task BuscarAsync(string? texto)
        {
            if (_consulta is not null) return;
            if (Compat.IsNullOrWhiteSpace(texto))
            {
                _estadoBusqueda.ForeColor = TemaEcoAccess.Advertencia;
                _estadoBusqueda.Text = "Escribe el nombre de un residuo, por ejemplo «botella» o «pila».";
                _busqueda.Campo.Focus();
                return;
            }
            await CargarPaginaAsync(texto, 1);
        }

        private Task ListarCatalogoAsync() =>
            CargarPaginaAsync("", 1);

        private async Task CargarPaginaAsync(string texto, int pagina)
        {
            if (_consulta is not null) return;
            using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            _consulta = consulta;
            _buscar.Enabled = false;
            _anterior.Enabled = _proxima.Enabled = false;
            _estadoBusqueda.ForeColor = TemaEcoAccess.Suave;
            _estadoBusqueda.Text = "Consultando materiales…";
            try
            {
                var resultado = await _servicios.CatalogoAmbiental.ConsultarPaginaPublicaAsync(texto, pagina, cancellationToken: consulta.Token).WaitAsync(consulta.Token);
                if (IsDisposed) return;
                if (!resultado.Exitoso) { _estadoBusqueda.Text = resultado.Mensaje; return; }
                var datos = resultado.Valor;
                _paginaCatalogo = datos.Pagina; _textoCatalogo = texto; _haySiguiente = datos.HaySiguiente;
                MostrarResultados(datos.Filas);
                _estadoBusqueda.Text = datos.Filas.Count == 0 ? "No encontramos materiales para esta búsqueda." :
                    $"Página {datos.Pagina} · {datos.Filas.Count} materiales" + (_haySiguiente ? " · Hay más resultados" : "");
            }
            catch (Exception) when (!_cerrando && !IsDisposed)
            {
                MostrarResultados(Array.Empty<Material>());
                _haySiguiente = false;
                _estadoBusqueda.ForeColor = TemaEcoAccess.Error;
                _estadoBusqueda.Text = "El catálogo no responde en este momento. Vuelve a intentarlo en unos segundos.";
            }
            catch (Exception) when (_cerrando || IsDisposed) { }
            finally
            {
                _consulta = null;
                if (!IsDisposed)
                {
                    _buscar.Enabled = true; _anterior.Enabled = _paginaCatalogo > 1; _proxima.Enabled = _haySiguiente;
                }
            }
        }

        private void MostrarResultados(IReadOnlyList<Material> materiales)
        {
            _resultados.SuspendLayout();
            foreach (Control anterior in _resultados.Controls.Cast<Control>().ToList()) anterior.Dispose();
            foreach (Material material in materiales.Take(ServicioCatalogoAmbiental.TamanoPaginaPublica))
            {
                bool? recibe = _demo is null ? null : _demo.Caneca.EstaEnServicio && _demo.Admitidos.Any(m => m.Id == material.Id);
                _resultados.Controls.Add(new TarjetaResultadoMaterial(material, recibe, AbrirAsistente));
            }
            _resultados.ResumeLayout();
            Adaptar();
        }

        private void AbrirAsistente(Material? material)
        {
            using var asistente = new FormAsistenteDeposito(_servicios, _sesion, _qrDemo, material);
            asistente.ShowDialog(this);
            if (asistente.SolicitarRegistro) { CrearCuenta(); return; }
            if (_sesion is not null && asistente.DepositoRegistrado) _ = RecargarCiudadanoAsync();
        }

        private async Task RecargarCiudadanoAsync()
        {
            using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try { await CargarDatosCiudadanoAsync(consulta.Token); }
            catch (Exception) when (!_cerrando && !IsDisposed) { }
            catch (Exception) when (_cerrando || IsDisposed) { }
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
    }
}
