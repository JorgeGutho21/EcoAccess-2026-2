using System;
using System.Drawing;
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
    /// Inicio de sesión del diseño «EcoAccess Moderno» (predeterminado), según la imagen de referencia aprobada: Eco a la
    /// izquierda y el formulario compacto a la derecha, sin desplazamiento a 1366×768. La autenticación es la misma del
    /// diseño Clásico: <see cref="ServicioAutenticacion.IniciarSesionPublicaAsync"/> y luego <see cref="AreasAcceso.Elegir"/>
    /// comprueba en Oracle que la cuenta tenga el rol del botón elegido; el botón por sí solo no concede nada.
    /// El registro no ocupa la pantalla: es un enlace discreto que abre su propia ventana.
    /// </summary>
    public sealed class FormAccesoModerno : Form, IVentanaAcceso
    {
        private readonly ServicioAutenticacion _servicio;
        private readonly ServicioRegistroCiudadanos _registro;
        private readonly TextBox _correo = new() { BorderStyle = BorderStyle.None, MaxLength = Usuario.LongitudMaximaCorreo, AccessibleName = "Correo electrónico de la cuenta" };
        private readonly TextBox _clave = new() { BorderStyle = BorderStyle.None, MaxLength = CredencialAcceso.LongitudMaximaContrasena, UseSystemPasswordChar = true, AccessibleName = "Contraseña de la cuenta" };
        private readonly CampoEco _campoCorreo;
        private readonly CampoEco _campoClave;
        private readonly Label _estado = new() { AutoSize = true, ForeColor = TemaEcoAccess.Error, Font = TemaEcoAccess.Cuerpo, Margin = new Padding(0, 4, 0, 4) };
        private readonly IconButton _entrar;
        private readonly IconButton _invitado;
        private readonly LinkLabel _configuracionInicial = new() { AutoSize = true, Text = "Configurar la contraseña del supervisor", Visible = false, Font = TemaEcoAccess.Pequena };
        private readonly RadioButton[] _areas;
        private bool _ocupado;

        public SesionActiva? Sesion { get; private set; }
        public bool AccesoInvitado { get; private set; }
        public AreaAcceso AreaElegida { get; private set; } = AreaAcceso.Ciudadano;

        public FormAccesoModerno(ServicioAutenticacion servicio, ServicioRegistroCiudadanos registro)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            Text = "EcoAccess - Acceso al Sistema";
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1180, 700);
            MinimumSize = new Size(900, 620);
            KeyPreview = true;

            _campoCorreo = new CampoEco("Correo electrónico", IconChar.Envelope, _correo) { Dock = DockStyle.Top };
            _campoClave = new CampoEco("Contraseña", IconChar.Lock, _clave, contrasena: true) { Dock = DockStyle.Top };
            _areas = new[] { Area("Ciudadano", IconChar.User, AreaAcceso.Ciudadano), Area("Operario", IconChar.ClipboardCheck, AreaAcceso.Operario), Area("Supervisor", IconChar.UserGroup, AreaAcceso.Supervisor) };
            _areas[0].Checked = true;
            _entrar = Accion("Iniciar sesión", IconChar.RightToBracket, principal: true);
            _invitado = Accion("Continuar como invitado", IconChar.Leaf, principal: false);
            _entrar.Click += async (_, _) => await EntrarAsync();
            _invitado.Click += (_, _) => { if (_ocupado) return; AccesoInvitado = true; DialogResult = DialogResult.OK; };
            _configuracionInicial.LinkClicked += async (_, _) => await ConfigurarSupervisorAsync();
            AcceptButton = _entrar;

            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Fondo };
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 72)));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.Controls.Add(Encabezado(), 0, 0);
            raiz.Controls.Add(Cuerpo(), 0, 1);
            Controls.Add(raiz);

            FormClosing += (_, e) => e.Cancel = _ocupado;
            Shown += async (_, _) => { _correo.Focus(); await ConsultarConfiguracionInicialAsync(); };
        }

        private Control Encabezado()
        {
            var barra = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = TemaEcoAccess.VerdeOscuro, Margin = Padding.Empty, Padding = new Padding(TemaEcoAccess.Px(this, 32), 0, 0, 0) };
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            barra.Controls.Add(new Label { Text = "EcoAccess", AutoSize = true, Font = new Font("Segoe UI Semibold", 24), ForeColor = Color.White, Anchor = AnchorStyles.Left }, 0, 0);
            barra.Controls.Add(new Label { Text = "|   Sistema de Gestión de Residuos", AutoSize = true, Font = new Font("Segoe UI", 14), ForeColor = Color.FromArgb(0xE8, 0xF6, 0xEE), Anchor = AnchorStyles.Left, Margin = new Padding(TemaEcoAccess.Px(this, 18), 0, 0, 0) }, 1, 0);
            return barra;
        }

        private Control Cuerpo()
        {
            var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(TemaEcoAccess.Px(this, 28)) };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

            // Columna de Eco: imagen oficial y lema, sobre verde suave.
            var identidad = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = TemaEcoAccess.VerdeSuave, Margin = new Padding(0, 0, TemaEcoAccess.Px(this, 24), 0) };
            identidad.RowStyles.Add(new RowStyle(SizeType.Percent, 74));
            identidad.RowStyles.Add(new RowStyle(SizeType.Percent, 26));
            identidad.Controls.Add(new PictureBox { Image = RecursosEco.Eco, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill, Margin = new Padding(TemaEcoAccess.Px(this, 18)) }, 0, 0);
            identidad.Controls.Add(new Label { Text = "Conectamos tecnología\ny sostenibilidad", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 18), ForeColor = Color.White, BackColor = TemaEcoAccess.VerdeOscuro, Margin = Padding.Empty }, 0, 1);

            // Formulario compacto.
            var tarjeta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie, Padding = new Padding(TemaEcoAccess.Px(this, 40), TemaEcoAccess.Px(this, 24), TemaEcoAccess.Px(this, 40), TemaEcoAccess.Px(this, 16)), AutoScroll = false };
            tarjeta.Paint += (_, e) => { using var borde = new Pen(TemaEcoAccess.Borde); e.Graphics.DrawRectangle(borde, 0, 0, tarjeta.Width - 1, tarjeta.Height - 1); };
            void Fila(Control control, SizeType tipo = SizeType.AutoSize, float alto = 0)
            {
                tarjeta.RowStyles.Add(new RowStyle(tipo, alto));
                control.Dock = DockStyle.Fill;
                tarjeta.Controls.Add(control, 0, tarjeta.RowStyles.Count - 1);
            }

            Fila(new Label { Text = "Bienvenido a EcoAccess", AutoSize = true, Font = new Font("Segoe UI Semibold", 24), ForeColor = TemaEcoAccess.Texto });
            Fila(new Label { Text = "Entra a tu cuenta para gestionar depósitos, historial y ecopuntos.", AutoSize = true, Font = new Font("Segoe UI", 11), ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, TemaEcoAccess.Px(this, 14)) });
            Fila(new Label { Text = "Tipo de acceso", AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto });
            var areas = new TableLayoutPanel { ColumnCount = 3, RowCount = 1, Height = TemaEcoAccess.Px(this, 54), Margin = new Padding(0, 4, 0, TemaEcoAccess.Px(this, 10)) };
            for (int i = 0; i < 3; i++) { areas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f)); areas.Controls.Add(_areas[i], i, 0); }
            Fila(areas, SizeType.Absolute, TemaEcoAccess.Px(this, 68));
            Fila(_campoCorreo);
            Fila(_campoClave);
            Fila(_estado);
            Fila(_entrar, SizeType.Absolute, TemaEcoAccess.Px(this, 58));
            Fila(_invitado, SizeType.Absolute, TemaEcoAccess.Px(this, 58));

            var registro = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Anchor = AnchorStyles.None, Margin = new Padding(0, TemaEcoAccess.Px(this, 8), 0, 0) };
            registro.Controls.Add(new Label { Text = "¿Aún no tienes una cuenta?", AutoSize = true, Font = TemaEcoAccess.Cuerpo, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 3, 4, 0) });
            var enlace = new LinkLabel { Text = "Regístrate", AutoSize = true, Font = TemaEcoAccess.Subtitulo, LinkColor = TemaEcoAccess.VerdeOscuro, ActiveLinkColor = TemaEcoAccess.Verde, AccessibleName = "Registrarse como ciudadano" };
            enlace.LinkClicked += (_, _) => Registrarse();
            registro.Controls.Add(enlace);
            registro.Controls.Add(_configuracionInicial);
            tarjeta.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tarjeta.Controls.Add(registro, 0, tarjeta.RowStyles.Count - 1);
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            cuerpo.Controls.Add(identidad, 0, 0);
            cuerpo.Controls.Add(tarjeta, 1, 0);

            // En ventanas estrechas se oculta Eco y el formulario ocupa todo el ancho: nunca hace falta desplazarse.
            cuerpo.SizeChanged += (_, _) =>
            {
                bool estrecha = cuerpo.Width < TemaEcoAccess.Px(this, 980);
                identidad.Visible = !estrecha;
                cuerpo.ColumnStyles[0].Width = estrecha ? 0 : 38;
                cuerpo.ColumnStyles[1].Width = estrecha ? 100 : 62;
            };
            return cuerpo;
        }

        private RadioButton Area(string texto, IconChar icono, AreaAcceso area)
        {
            var opcion = new RadioButton
            {
                Text = "  " + texto, Appearance = Appearance.Button, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleCenter,
                Image = CacheIconos.Obtener(icono, TemaEcoAccess.Px(this, 26), TemaEcoAccess.VerdeOscuro), TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleCenter, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, BackColor = TemaEcoAccess.Superficie,
                Margin = new Padding(0, 0, TemaEcoAccess.Px(this, 10), 0), Tag = area, Cursor = Cursors.Hand, AccessibleName = "Acceso de " + texto,
            };
            opcion.FlatAppearance.BorderColor = TemaEcoAccess.Borde;
            opcion.FlatAppearance.CheckedBackColor = TemaEcoAccess.VerdeSuave;
            opcion.CheckedChanged += (_, _) => opcion.FlatAppearance.BorderColor = opcion.Checked ? TemaEcoAccess.Verde : TemaEcoAccess.Borde;
            return opcion;
        }

        private IconButton Accion(string texto, IconChar icono, bool principal)
        {
            var boton = new IconButton
            {
                Text = "  " + texto, IconChar = icono, IconSize = TemaEcoAccess.Px(this, 26), TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleCenter, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 13),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, TemaEcoAccess.Px(this, 6), 0, TemaEcoAccess.Px(this, 4)),
                BackColor = principal ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Superficie,
                ForeColor = principal ? Color.White : TemaEcoAccess.VerdeOscuro,
                IconColor = principal ? Color.White : TemaEcoAccess.Verde,
            };
            boton.FlatAppearance.BorderColor = principal ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Verde;
            boton.FlatAppearance.BorderSize = principal ? 0 : 2;
            return boton;
        }

        private AreaAcceso AreaSeleccionada()
        {
            foreach (RadioButton opcion in _areas) if (opcion.Checked) return (AreaAcceso)opcion.Tag!;
            return AreaAcceso.Ciudadano;
        }

        private async Task EntrarAsync()
        {
            if (_ocupado) return;
            _estado.Text = "";
            bool sinCorreo = Compat.IsNullOrWhiteSpace(_correo.Text), sinClave = _clave.Text.Length == 0;
            _campoCorreo.MostrarError(sinCorreo ? "Escribe el correo de tu cuenta." : null);
            _campoClave.MostrarError(sinClave ? "Escribe tu contraseña." : null);
            if (sinCorreo || sinClave) { (sinCorreo ? _correo : _clave).Focus(); return; }

            Ocupado(true);
            try
            {
                // La contraseña sale del control antes de llamar, para que no quede en él aunque falle la conexión.
                string contrasena = _clave.Text;
                _clave.Clear();
                Resultado<SesionActiva> resultado = await _servicio.IniciarSesionPublicaAsync(_correo.Text, contrasena);
                if (!resultado.Exitoso) { _estado.Text = resultado.Mensaje; return; }
                Resultado<AreaAcceso> area = AreasAcceso.Elegir(resultado.Valor, AreaSeleccionada());
                if (!area.Exitoso) { _estado.Text = area.Mensaje; return; }
                AreaElegida = area.Valor;
                Sesion = resultado.Valor;
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // Nunca se muestra el texto del proveedor: puede citar host, servicio o usuario.
                _estado.Text = "No se pudo conectar con la base de datos. Inténtalo de nuevo en un momento.";
            }
            finally { Ocupado(false); }

            if (Sesion is null) _clave.Focus();
            else DialogResult = DialogResult.OK;
        }

        private void Ocupado(bool ocupado)
        {
            _ocupado = ocupado;
            UseWaitCursor = ocupado;
            _entrar.Text = ocupado ? "  Comprobando acceso…" : "  Iniciar sesión";
            foreach (Control control in new Control[] { _correo, _clave, _entrar, _invitado }) control.Enabled = !ocupado;
            foreach (RadioButton opcion in _areas) opcion.Enabled = !ocupado;
        }

        private void Registrarse()
        {
            if (_ocupado) return;
            using var formulario = new FormRegistroCiudadano(_registro);
            if (formulario.ShowDialog(this) == DialogResult.OK)
            {
                _correo.Text = formulario.CorreoRegistrado;
                _areas[0].Checked = true;
                _estado.ForeColor = TemaEcoAccess.VerdeOscuro;
                _estado.Text = "Cuenta creada. Escribe tu contraseña para entrar.";
                _clave.Focus();
            }
        }

        private async Task ConsultarConfiguracionInicialAsync()
        {
            // Solo aparece si todavía no existe la contraseña del supervisor; si Oracle no responde, no bloquea el acceso.
            using var plazo = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { bool requerida = await _servicio.RequiereConfiguracionInicialAsync(plazo.Token); if (!IsDisposed) _configuracionInicial.Visible = requerida; }
            catch (Exception e) when (e is not OutOfMemoryException) { }
        }

        private async Task ConfigurarSupervisorAsync()
        {
            using (var formulario = new FormConfiguracionInicial(_servicio))
            {
                if (formulario.ShowDialog(this) == DialogResult.OK)
                {
                    _correo.Text = formulario.CorreoConfigurado;
                    _areas[2].Checked = true;
                    _estado.ForeColor = TemaEcoAccess.VerdeOscuro;
                    _estado.Text = "Contraseña del supervisor guardada. Inicia sesión.";
                }
            }
            await ConsultarConfiguracionInicialAsync();
        }
    }
}
