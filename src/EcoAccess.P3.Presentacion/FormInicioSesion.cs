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
            Text = "EcoAccess · Bienvenida";
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = MinimizeBox = true;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(TemaEcoAccess.Px(this, 560), TemaEcoAccess.Px(this, 360));
            ClientSize = new Size(1180, 780);
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var identidad = new PanelIdentidadEcoAccess { Dock = DockStyle.Fill };
            var desplazamiento = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(32, 24, 32, 24) };
            var acceso = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 5 };
            acceso.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) acceso.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            etiquetaTitulo.Text = "Bienvenido a EcoAccess";
            etiquetaTitulo.Font = TemaEcoAccess.Titulo;
            etiquetaTitulo.AutoSize = true;
            etiquetaTitulo.Margin = new Padding(0, 0, 0, 8);
            acceso.Controls.Add(etiquetaTitulo, 0, 0);
            var introduccion = new Label { Text = "Entra sin cuenta como invitado, o inicia sesión o regístrate para tus funciones personales.", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 24) };
            acceso.Controls.Add(introduccion, 0, 1);

            panelCampos.Controls.Clear();
            panelCampos.Dock = DockStyle.Fill;
            panelCampos.AutoSize = true;
            panelCampos.TabIndex = 1;
            panelCampos.BackColor = Color.White;
            panelCampos.Padding = new Padding(24);
            panelCampos.Margin = new Padding(0, 0, 0, 16);
            var campos = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
            campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var tituloAcceso = new Label { Text = "Iniciar sesión", AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto };
            var detalle = new Label { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 4, 0, 16) };
            var tipoAcceso = new Label { Text = "Tipo de acceso", AutoSize = true, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 12, 0, 6) };
            void DescribirArea()
            {
                var area = _selectorArea.Seleccionada;
                detalle.Text = SelectorAreaAcceso.Descripciones[area].Resumen + " Se comprueba el rol real de tu cuenta al entrar.";
                _accionAcceso.Values.Text = $"Iniciar sesión como {AreasAcceso.NombreArea(area)}";
                _accionAcceso.AccessibleName = _accionAcceso.Values.Text;
                etiquetaEstado.Text = string.Empty;
            }
            _selectorArea.Cambio += (_, _) => DescribirArea();
            campos.Controls.Add(tituloAcceso);
            campos.Controls.Add(tipoAcceso);
            campos.Controls.Add(_selectorArea);
            campos.Controls.Add(detalle);
            // Campos con icono, ojo para mostrar la contraseña y mensaje de error bajo cada uno.
            etiquetaCorreo.Dispose();
            etiquetaContrasena.Dispose();
            _campoCorreo = new CampoEco("Correo electrónico", FontAwesome.Sharp.IconChar.Envelope, textoCorreo) { Dock = DockStyle.Top, TabIndex = 0 };
            _campoClave = new CampoEco("Contraseña", FontAwesome.Sharp.IconChar.Lock, textoContrasena, contrasena: true) { Dock = DockStyle.Top, TabIndex = 1 };
            campos.Controls.Add(_campoCorreo);
            campos.Controls.Add(_campoClave);
            textoCorreo.TextChanged += (_, _) => _campoCorreo.MostrarError(null);
            textoContrasena.TextChanged += (_, _) => { if (textoContrasena.Text.Length > 0) _campoClave.MostrarError(null); };
            _accionAcceso.Values.Text = "Iniciar sesión";
            _accionAcceso.AccessibleName = "Iniciar sesión con mi cuenta";
            _accionAcceso.Dock = DockStyle.Fill;
            _accionAcceso.Height = TemaEcoAccess.Px(this, 44);
            _accionAcceso.MinimumSize = new Size(0, TemaEcoAccess.Px(this, 44));
            _accionAcceso.TabIndex = 3;
            _accionAcceso.Margin = new Padding(0, 8, 0, 8);
            TemaEcoAccess.EstilizarAccion(_accionAcceso, true);
            _accionAcceso.Click += BotonEntrar_Click;
            AcceptButton = _accionAcceso;
            DescribirArea();
            botonEntrar.Dispose();
            campos.Controls.Add(_accionAcceso);
            etiquetaEstado.AutoSize = true;
            etiquetaEstado.Margin = new Padding(0, 8, 0, 8);
            campos.Controls.Add(etiquetaEstado);
            enlaceConfiguracionInicial.TabIndex = 4;
            enlaceConfiguracionInicial.LinkColor = enlaceConfiguracionInicial.VisitedLinkColor = TemaEcoAccess.VerdeOscuro;
            enlaceConfiguracionInicial.ActiveLinkColor = TemaEcoAccess.Verde;
            campos.Controls.Add(enlaceConfiguracionInicial);
            panelCampos.Controls.Add(campos);
            acceso.Controls.Add(panelCampos, 0, 3);

            _opciones.Controls.Clear();
            _opciones.ColumnCount = 1;
            _opciones.RowCount = 2;
            _opciones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _opciones.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _opciones.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _opciones.AutoSize = true;
            _opciones.Dock = DockStyle.Fill;
            _opciones.Margin = Padding.Empty;
            _opciones.TabIndex = 2;
            var crear = new TarjetaRutaAcceso("Registrarse", "¿Es tu primera vez? Crea tu cuenta de ciudadano para registrar depósitos y sumar ecopuntos.", "Registrarme", FontAwesome.Sharp.IconChar.UserPlus);
            // Entrada directa, sin usuario ni contraseña: va primero. Lo personal y lo administrativo exigen iniciar sesión.
            var invitado = new TarjetaRutaAcceso("Continuar como invitado", "Entra ya, sin usuario ni contraseña: aprende a separar y deposita en la caneca piloto. Los ecopuntos y la administración requieren una cuenta.", "Entrar como invitado", FontAwesome.Sharp.IconChar.Leaf);
            TemaEcoAccess.EstilizarAccion(invitado.Accion, true);
            crear.Accion.Click += (_, _) =>
            {
                if (_ocupado) return;
                using var formulario = new FormRegistroCiudadano(_registro);
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    textoCorreo.Text = formulario.CorreoRegistrado;
                    textoContrasena.Clear();
                    _campoClave.OcultarContrasena();
                    etiquetaEstado.ForeColor = TemaEcoAccess.Verde;
                    etiquetaEstado.Text = "Cuenta ciudadana creada. Ya puedes iniciar sesión.";
                    textoContrasena.Focus();
                }
            };
            invitado.Accion.Click += (_, _) => { if (!_ocupado) { AccesoInvitado = true; DialogResult = DialogResult.OK; } };
            _opciones.Controls.Add(crear);
            crear.Dock = invitado.Dock = DockStyle.Fill;
            crear.TabIndex = 0;
            invitado.TabIndex = 0;
            acceso.Controls.Add(invitado, 0, 2);
            acceso.Controls.Add(_opciones, 0, 4);

            acceso.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(260, acceso.ClientSize.Width - 8);
                etiquetaTitulo.MaximumSize = introduccion.MaximumSize = new Size(ancho, 0);
                etiquetaEstado.MaximumSize = new Size(Math.Max(240, ancho - panelCampos.Padding.Horizontal), 0);
                detalle.MaximumSize = new Size(Math.Max(240, ancho - 48), 0);
            };
            void AjustarAnchoAcceso()
            {
                int ancho = Math.Max(260, Math.Min(TemaEcoAccess.Px(this, 720), desplazamiento.ClientSize.Width - desplazamiento.Padding.Horizontal));
                acceso.MinimumSize = new Size(ancho, 0);
                acceso.MaximumSize = new Size(ancho, 0);
                acceso.Width = ancho;
                acceso.Left = Math.Max(0, (desplazamiento.ClientSize.Width - ancho) / 2);
                acceso.Top = desplazamiento.Padding.Top;
            }
            desplazamiento.SizeChanged += (_, _) => AjustarAnchoAcceso();
            desplazamiento.Controls.Add(acceso);
            raiz.Controls.Add(identidad, 0, 0);
            raiz.Controls.Add(desplazamiento, 1, 0);
            bool? compactaAnterior = null;
            void Adaptar()
            {
                bool compacta = ClientSize.Width < TemaEcoAccess.Px(this, 960);
                if (compactaAnterior == compacta) return;
                compactaAnterior = compacta;
                identidad.Visible = !compacta;
                raiz.ColumnStyles[0].SizeType = compacta ? SizeType.Absolute : SizeType.Percent;
                raiz.ColumnStyles[0].Width = compacta ? 0 : 38;
                raiz.ColumnStyles[1].Width = compacta ? 100 : 62;
            }
            SizeChanged += (_, _) => Adaptar();
            Shown += (_, _) => { Adaptar(); textoCorreo.Focus(); };
            Controls.Add(raiz);
            ForeColor = TemaEcoAccess.Texto;
            WindowState = FormWindowState.Maximized;
            enlaceConfiguracionInicial.Visible = false;
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
