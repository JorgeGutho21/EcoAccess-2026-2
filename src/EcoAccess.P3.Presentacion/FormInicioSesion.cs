using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Inicio de sesión con correo y contraseña.
    /// El formulario no compara contraseñas ni cuenta intentos: todo eso lo decide <see cref="ServicioAutenticacion"/>.
    /// Si se acepta, deja la sesión en <see cref="Sesion"/> y se cierra con <see cref="DialogResult.OK"/>.
    /// </summary>
    public partial class FormInicioSesion : Form
    {
        private readonly ServicioAutenticacion _servicio;
        private bool _ocupado;
        private readonly ServicioRegistroCiudadanos _registro;
        private readonly TableLayoutPanel _opciones = new() { AutoSize = true, Dock = DockStyle.Fill };
        private readonly Krypton.Toolkit.KryptonButton _accionAcceso = new();
        private CampoEco _campoCorreo = null!;
        private CampoEco _campoClave = null!;
        private readonly SelectorAreaAcceso _selectorArea = new() { TabIndex = 0 };
        public bool AccesoInvitado { get; private set; }

        public SesionActiva? Sesion { get; private set; }

        /// <summary>Área autorizada tras comprobar la contraseña y los roles reales de la cuenta.</summary>
        public AreaAcceso AreaElegida { get; private set; } = AreaAcceso.Ciudadano;

        public FormInicioSesion(ServicioAutenticacion servicio, ServicioRegistroCiudadanos registro)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            InitializeComponent();
            ConstruirBienvenida();
            TemaEcoAccess.Aplicar(this);
            etiquetaTitulo.Text = "Bienvenido a EcoAccess";
            textoCorreo.AccessibleName = "Correo electrónico de la cuenta";
            textoContrasena.AccessibleName = "Contraseña de la cuenta";
            textoCorreo.MaxLength = Usuario.LongitudMaximaCorreo;
            textoContrasena.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            FormClosing += (_, e) => e.Cancel = _ocupado;
        }

        private async void FormInicioSesion_Load(object? sender, EventArgs e)
        {
            // La consulta opcional del bootstrap no bloquea entrar como invitado si Oracle no está disponible.
            enlaceConfiguracionInicial.Visible = false;
            using var plazo = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                bool requerida = await _servicio.RequiereConfiguracionInicialAsync(plazo.Token).WaitAsync(plazo.Token);
                if (!IsDisposed) enlaceConfiguracionInicial.Visible = requerida;
            }
            catch (Exception) { if (!IsDisposed) enlaceConfiguracionInicial.Visible = false; }
        }

        private void ConstruirBienvenida()
        {
            Controls.Clear();
            Text = "EcoAccess · Acceso al sistema";
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = MinimizeBox = true;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(TemaEcoAccess.Px(this, 560), TemaEcoAccess.Px(this, 420));
            ClientSize = new Size(1180, 780);

            // Rediseño ExpoSoftware: barra verde oscura, ilustración de ECO en la UPC con el lema y la tarjeta de acceso
            // blanca a la derecha; abajo, la franja de beneficios. Los controles y su lógica son los mismos de siempre.
            var barra = new BarraAccesoEco { Dock = DockStyle.Top, Height = TemaEcoAccess.Px(this, 76) };
            var portada = new PanelIdentidadEcoAccess { Dock = DockStyle.Fill };
            var tarjeta = new Panel { BackColor = Color.White, Padding = new Padding(28, 22, 28, 18) };
            var desplazamiento = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            var acceso = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = Color.White };
            acceso.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            etiquetaTitulo.Text = "Bienvenido a EcoAccess";
            etiquetaTitulo.Font = _tituloGrande;
            etiquetaTitulo.ForeColor = TemaEcoAccess.VerdeOscuro;
            etiquetaTitulo.AutoSize = true;
            etiquetaTitulo.Anchor = AnchorStyles.None;
            etiquetaTitulo.Margin = new Padding(0, 0, 0, 2);
            acceso.Controls.Add(etiquetaTitulo);
            var introduccion = new Label { Text = "Gestiona tus depósitos, historial y ecopuntos.", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Anchor = AnchorStyles.None, Margin = new Padding(0, 0, 0, 14) };
            acceso.Controls.Add(introduccion);

            panelCampos.Controls.Clear();
            panelCampos.Dock = DockStyle.Fill;
            panelCampos.AutoSize = true;
            panelCampos.AutoSizeMode = AutoSizeMode.GrowAndShrink; // sin alto sobrante bajo «Regístrate»
            panelCampos.TabIndex = 1;
            panelCampos.BackColor = Color.White;
            panelCampos.Padding = Padding.Empty;
            panelCampos.Margin = Padding.Empty;
            var campos = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = Color.White };
            campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var tipoAcceso = new Label { Text = "Tipo de acceso", AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 6) };
            void DescribirArea()
            {
                var area = _selectorArea.Seleccionada;
                _ayudasAcceso.SetToolTip(_selectorArea, SelectorAreaAcceso.Descripciones[area].Resumen + " Se comprueba el rol real de tu cuenta al entrar.");
                _accionAcceso.Values.Text = "Iniciar sesión";
                _accionAcceso.AccessibleName = $"Iniciar sesión como {AreasAcceso.NombreArea(area)}";
                etiquetaEstado.Text = string.Empty;
            }
            _selectorArea.Cambio += (_, _) => DescribirArea();
            campos.Controls.Add(tipoAcceso);
            campos.Controls.Add(_selectorArea);
            // Campos con icono, ojo para mostrar la contraseña y mensaje de error bajo cada uno.
            etiquetaCorreo.Dispose();
            etiquetaContrasena.Dispose();
            _campoCorreo = new CampoEco("Correo electrónico", FontAwesome.Sharp.IconChar.Envelope, textoCorreo) { Dock = DockStyle.Top, TabIndex = 0 };
            _campoClave = new CampoEco("Contraseña", FontAwesome.Sharp.IconChar.Lock, textoContrasena, contrasena: true) { Dock = DockStyle.Top, TabIndex = 1 };
            campos.Controls.Add(_campoCorreo);
            campos.Controls.Add(_campoClave);
            CajaTexto.Sugerir(textoCorreo, "Ingresa tu correo");
            CajaTexto.Sugerir(textoContrasena, "Ingresa tu contraseña");
            textoCorreo.TextChanged += (_, _) => _campoCorreo.MostrarError(null);
            textoContrasena.TextChanged += (_, _) => { if (textoContrasena.Text.Length > 0) _campoClave.MostrarError(null); };
            _accionAcceso.Values.Text = "Iniciar sesión";
            _accionAcceso.Values.Image = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.RightToBracket, TemaEcoAccess.Px(this, 18), Color.White);
            _accionAcceso.Dock = DockStyle.Fill;
            _accionAcceso.Height = TemaEcoAccess.Px(this, 48);
            _accionAcceso.MinimumSize = new Size(0, TemaEcoAccess.Px(this, 48));
            _accionAcceso.TabIndex = 3;
            _accionAcceso.Margin = new Padding(0, 10, 0, 6);
            TemaEcoAccess.EstilizarAccion(_accionAcceso, true);
            _accionAcceso.Click += BotonEntrar_Click;
            AcceptButton = _accionAcceso;
            DescribirArea();
            botonEntrar.Dispose();
            campos.Controls.Add(_accionAcceso);
            // Entrar como invitado: sin usuario ni contraseña, botón con borde verde como en el diseño de referencia.
            var invitado = new Krypton.Toolkit.KryptonButton
            {
                Dock = DockStyle.Fill,
                Height = TemaEcoAccess.Px(this, 48),
                MinimumSize = new Size(0, TemaEcoAccess.Px(this, 48)),
                Margin = new Padding(0, 4, 0, 8),
                TabIndex = 4,
                AccessibleName = "Continuar como invitado, sin usuario ni contraseña",
            };
            TemaEcoAccess.EstilizarAccion(invitado);
            invitado.StateCommon.Border.Color1 = invitado.StateCommon.Border.Color2 = TemaEcoAccess.Verde;
            invitado.OverrideDefault.Border.Color1 = TemaEcoAccess.Verde;
            invitado.StateCommon.Border.Width = 2;
            invitado.Values.Text = "Continuar como invitado";
            invitado.Values.Image = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.Leaf, TemaEcoAccess.Px(this, 18), TemaEcoAccess.Verde);
            invitado.Click += (_, _) => { if (!_ocupado) { AccesoInvitado = true; DialogResult = DialogResult.OK; } };
            _ayudasAcceso.SetToolTip(invitado, "Aprende a separar y deposita en las canecas. Los ecopuntos y la administración requieren una cuenta.");
            campos.Controls.Add(invitado);
            campos.Controls.Add(CrearEnlaceRegistro());
            etiquetaEstado.AutoSize = true;
            etiquetaEstado.Margin = new Padding(0, 6, 0, 4);
            // Sin mensaje no ocupa alto: la tarjeta termina justo bajo «Regístrate».
            etiquetaEstado.Visible = false;
            etiquetaEstado.TextChanged += (_, _) => etiquetaEstado.Visible = etiquetaEstado.Text.Length > 0;
            campos.Controls.Add(etiquetaEstado);
            enlaceConfiguracionInicial.TabIndex = 6;
            enlaceConfiguracionInicial.LinkColor = enlaceConfiguracionInicial.VisitedLinkColor = TemaEcoAccess.VerdeOscuro;
            enlaceConfiguracionInicial.ActiveLinkColor = TemaEcoAccess.Verde;
            campos.Controls.Add(enlaceConfiguracionInicial);
            panelCampos.Controls.Add(campos);
            acceso.Controls.Add(panelCampos);
            _opciones.Visible = false;

            acceso.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(240, acceso.ClientSize.Width - 4);
                etiquetaTitulo.MaximumSize = introduccion.MaximumSize = new Size(ancho, 0);
                // En pantallas pequeñas la tarjeta es más estrecha: el título baja de tamaño para quedar en una línea.
                float tam = TextRenderer.MeasureText(etiquetaTitulo.Text, _tituloGrande).Width <= ancho ? 22f : 18f;
                if (Math.Abs(etiquetaTitulo.Font.Size - tam) > 0.1f) etiquetaTitulo.Font = tam > 20 ? _tituloGrande : _tituloPequeno;
                etiquetaEstado.MaximumSize = new Size(ancho, 0);
                // En tarjetas angostas «Regístrate» pasa a la línea siguiente en vez de salirse.
                if (_filaRegistro is not null) _filaRegistro.MaximumSize = new Size(ancho, 0);
            };
            void AjustarAcceso()
            {
                int ancho = Math.Max(240, desplazamiento.ClientSize.Width);
                acceso.MinimumSize = acceso.MaximumSize = new Size(ancho, 0);
                acceso.Location = Point.Empty;
            }
            desplazamiento.ClientSizeChanged += (_, _) => { AjustarAcceso(); Reajustar(); };
            desplazamiento.Controls.Add(acceso);
            tarjeta.Controls.Add(desplazamiento);
            // Con poco alto (1366×768, 1600×900) la tarjeta se compacta para que «Regístrate» se vea sin desplazar.
            bool? compacta = null;
            portada.SizeChanged += (_, _) =>
            {
                bool baja = portada.Height < TemaEcoAccess.Px(this, 860);
                if (compacta == baja) return;
                compacta = baja;
                int boton = TemaEcoAccess.Px(this, baja ? 42 : 48);
                acceso.SuspendLayout();
                tarjeta.Padding = baja ? new Padding(24, 14, 24, 10) : new Padding(28, 22, 28, 18);
                introduccion.Margin = new Padding(0, 0, 0, baja ? 8 : 14);
                tipoAcceso.Margin = new Padding(0, 0, 0, baja ? 4 : 6);
                _selectorArea.Compactar(baja);
                _campoCorreo.Compactar(baja);
                _campoClave.Compactar(baja);
                _accionAcceso.Height = invitado.Height = boton;
                _accionAcceso.MinimumSize = invitado.MinimumSize = new Size(0, boton);
                _accionAcceso.Margin = new Padding(0, baja ? 4 : 10, 0, baja ? 4 : 6);
                invitado.Margin = new Padding(0, baja ? 2 : 4, 0, baja ? 4 : 8);
                acceso.ResumeLayout(true);
                portada.PerformLayout();
            };
            // Alto real del contenido: el borde inferior del último control visible («Regístrate» o el mensaje de estado).
            portada.AltoContenido = () =>
            {
                int fondo = 0;
                foreach (Control c in campos.Controls)
                    if (c.Visible) fondo = Math.Max(fondo, c.Bottom + c.Margin.Bottom);
                return panelCampos.Top + campos.Top + fondo + tarjeta.Padding.Vertical + TemaEcoAccess.Px(this, 10);
            };
            // Se reajusta después del diseño en curso: dentro de él, el cambio de tamaño del título no llegaba a la tarjeta.
            void Reajustar()
            {
                if (_reajustePendiente || !IsHandleCreated) { if (!IsHandleCreated) portada.PerformLayout(); return; }
                _reajustePendiente = true;
                BeginInvoke(new Action(() => { _reajustePendiente = false; if (!IsDisposed) portada.PerformLayout(); }));
            }
            acceso.SizeChanged += (_, _) => Reajustar();
            campos.SizeChanged += (_, _) => Reajustar();
            portada.Tarjeta = tarjeta;
            portada.Controls.Add(tarjeta);
            Controls.Add(portada);
            Controls.Add(barra);
            Shown += (_, _) => textoCorreo.Focus();
            ForeColor = TemaEcoAccess.Texto;
            WindowState = FormWindowState.Maximized;
            enlaceConfiguracionInicial.Visible = false;
        }

        private readonly ToolTip _ayudasAcceso = new();
        private readonly Font _tituloGrande = new("Segoe UI", 22, FontStyle.Bold);
        private readonly Font _tituloPequeno = new("Segoe UI", 18, FontStyle.Bold);
        private FlowLayoutPanel? _filaRegistro;
        private bool _reajustePendiente;

        /// <summary>«¿Aún no tienes una cuenta? Regístrate» con su icono, centrado al pie de la tarjeta. Abre el registro existente.</summary>
        private Control CrearEnlaceRegistro()
        {
            var fila = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Anchor = AnchorStyles.None, BackColor = Color.White, Margin = new Padding(0, 4, 0, 4) };
            _filaRegistro = fila;
            int lado = TemaEcoAccess.Px(this, 18);
            fila.Controls.Add(new PictureBox { Image = CacheIconos.Obtener(FontAwesome.Sharp.IconChar.UserPlus, lado, TemaEcoAccess.VerdeOscuro), SizeMode = PictureBoxSizeMode.CenterImage, Size = new Size(lado + 6, lado + 8), Margin = new Padding(0, 0, 6, 0), AccessibleName = "Crear cuenta" });
            fila.Controls.Add(new Label { Text = "¿Aún no tienes una cuenta?", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 4, 4, 0) });
            var enlace = new LinkLabel
            {
                Text = "Regístrate",
                AutoSize = true,
                Font = TemaEcoAccess.Subtitulo,
                LinkColor = TemaEcoAccess.VerdeOscuro,
                VisitedLinkColor = TemaEcoAccess.VerdeOscuro,
                ActiveLinkColor = TemaEcoAccess.Verde,
                Margin = new Padding(0, 3, 0, 0),
                TabIndex = 5,
                AccessibleName = "¿Aún no tienes una cuenta? Regístrate como ciudadano",
            };
            enlace.LinkClicked += (_, _) => AbrirRegistro();
            fila.Controls.Add(enlace);
            return fila;
        }

        private void AbrirRegistro()
        {
            if (_ocupado) return;
            using var formulario = new FormRegistroCiudadano(_registro);
            if (formulario.ShowDialog(this) != DialogResult.OK) return;
            // De vuelta al inicio de sesión con el correo ya escrito.
            textoCorreo.Text = formulario.CorreoRegistrado;
            textoContrasena.Clear();
            _campoClave.OcultarContrasena();
            etiquetaEstado.ForeColor = TemaEcoAccess.Verde;
            etiquetaEstado.Text = "Cuenta ciudadana creada. Ya puedes iniciar sesión.";
            textoContrasena.Focus();
        }

        private async void BotonEntrar_Click(object? sender, EventArgs e)
        {
            if (_ocupado) return;
            etiquetaEstado.Text = string.Empty;
            // Validación por campo antes de consultar: no se gasta un intento de acceso con datos incompletos.
            bool sinCorreo = Compat.IsNullOrWhiteSpace(textoCorreo.Text), sinClave = textoContrasena.Text.Length == 0;
            _campoCorreo.MostrarError(sinCorreo ? "Escribe el correo de tu cuenta." : null);
            _campoClave.MostrarError(sinClave ? "Escribe tu contraseña." : null);
            if (sinCorreo || sinClave)
            {
                (sinCorreo ? textoCorreo : textoContrasena).Focus();
                return;
            }
            await EjecutarAsync(async () =>
            {
                // La contraseña sale del control antes de llamar, para que no quede en él aunque falle la conexión.
                string contrasena = textoContrasena.Text;
                textoContrasena.Clear();
                Resultado<SesionActiva> resultado = await _servicio.IniciarSesionPublicaAsync(textoCorreo.Text, contrasena);
                if (!resultado.Exitoso)
                {
                    MostrarError(resultado.Mensaje);
                    return;
                }

                // La contraseña es correcta; ahora el área elegida debe coincidir con un rol real de la cuenta.
                Resultado<AreaAcceso> area = AreasAcceso.Elegir(resultado.Valor, _selectorArea.Seleccionada);
                if (!area.Exitoso)
                {
                    MostrarError(area.Mensaje);
                    return;
                }

                AreaElegida = area.Valor;
                Sesion = resultado.Valor;
            });

            if (Sesion is null) textoContrasena.Focus();

            // Se cierra fuera de EjecutarAsync: con _ocupado activo, FormClosing cancelaría el cierre.
            if (Sesion is not null)
            {
                DialogResult = DialogResult.OK;
            }
        }

        private async void EnlaceConfiguracionInicial_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var formulario = new FormConfiguracionInicial(_servicio))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    textoCorreo.Text = formulario.CorreoConfigurado;
                    etiquetaEstado.ForeColor = Color.DarkGreen;
                    etiquetaEstado.Text = "Contraseña del supervisor guardada. Elige Supervisor e inicia sesión.";
                    textoContrasena.Focus();
                }
            }

            await EjecutarAsync(async () => enlaceConfiguracionInicial.Visible = await _servicio.RequiereConfiguracionInicialAsync());
        }

        // Igual que en FormMateriales: bloquea la ventana durante la operación y no muestra el texto del proveedor.
        private async Task EjecutarAsync(Func<Task> accion)
        {
            if (_ocupado)
            {
                return;
            }

            _ocupado = true;
            UseWaitCursor = true;
            _accionAcceso.Values.Text = "Comprobando acceso…";
            panelCampos.Enabled = false;
            _opciones.Enabled = false;
            try
            {
                await accion();
            }
            catch (Exception)
            {
                MostrarError("No se pudo conectar con la base de datos. Revise que Oracle esté en marcha e inténtelo de nuevo.");
            }
            finally
            {
                _ocupado = false;
                _accionAcceso.Values.Text = $"Iniciar sesión como {AreasAcceso.NombreArea(_selectorArea.Seleccionada)}";
                panelCampos.Enabled = true;
                _opciones.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void MostrarError(string? mensaje)
        {
            etiquetaEstado.ForeColor = Color.DarkRed;
            etiquetaEstado.Text = mensaje ?? "No se pudo completar la operación.";
        }
    }
}
