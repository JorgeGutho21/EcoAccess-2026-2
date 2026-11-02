using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Registro público sin selector de roles; la lógica y Datos imponen Ciudadano. La validación inmediata solo orienta
    /// mientras se escribe: las reglas definitivas (correo único, largo de la contraseña, consentimiento) las decide
    /// <see cref="ServicioRegistroCiudadanos"/>, y la contraseña se guarda solo como hash PBKDF2.
    /// </summary>
    public sealed class FormRegistroCiudadano : Form
    {
        private readonly ServicioRegistroCiudadanos _servicio;
        private readonly CampoEco _nombre = new("Nombre", IconChar.User, new TextBox { MaxLength = Usuario.LongitudMaximaNombre });
        private readonly CampoEco _correo = new("Correo electrónico", IconChar.Envelope, new TextBox { MaxLength = Usuario.LongitudMaximaCorreo });
        private readonly CampoEco _clave = new("Contraseña", IconChar.Lock, new TextBox { MaxLength = CredencialAcceso.LongitudMaximaContrasena }, contrasena: true);
        private readonly CampoEco _confirmacion = new("Confirma tu contraseña", IconChar.Lock, new TextBox { MaxLength = CredencialAcceso.LongitudMaximaContrasena }, contrasena: true);
        private readonly MedidorContrasena _medidor = new() { Margin = new Padding(0, 0, 0, 16) };
        private readonly CheckBox _consentimiento = new() { Text = "Acepto el tratamiento de mi nombre y correo para gestionar mi cuenta y mi participación en EcoAccess.", AutoSize = true, Margin = new Padding(0, 4, 0, 6) };
        private readonly Label _estado = new() { AutoSize = true, ForeColor = TemaEcoAccess.Error, Margin = new Padding(0, 10, 0, 0) };
        private readonly KryptonButton _crear = new() { Text = "Crear mi cuenta", AccessibleName = "Crear mi cuenta ciudadana" };
        private readonly LinkLabel _volver = new() { Text = "¿Ya tienes cuenta? Volver al inicio", AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
        private readonly TableLayoutPanel _formulario = new() { AutoSize = true, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie };
        private bool _ocupado;
        public string CorreoRegistrado { get; private set; } = "";

        public FormRegistroCiudadano(ServicioRegistroCiudadanos servicio)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            Text = "Crear cuenta ciudadana · EcoAccess";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            MinimizeBox = false;
            Size = new Size(TemaEcoAccess.Px(this, 1060), TemaEcoAccess.Px(this, 760));
            MinimumSize = new Size(TemaEcoAccess.Px(this, 480), TemaEcoAccess.Px(this, 520));

            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = TemaEcoAccess.Fondo };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var identidad = CrearIdentidad();
            var desplazamiento = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(32, 28, 32, 28) };
            var tarjeta = new TarjetaEco { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Location = new Point(32, 28) };
            _formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _formulario.Dock = DockStyle.Top;
            _formulario.Controls.Add(new Label { Text = "Crea tu cuenta", AutoSize = true, Font = TemaEcoAccess.Titulo, Margin = Padding.Empty });
            _formulario.Controls.Add(new Label { Text = "Es gratis y toma un minuto. Tu cuenta es de ciudadano: no concede permisos administrativos.", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 6, 0, 22) });
            _clave.Ayuda("Entre 12 y 128 caracteres. Una frase larga es fácil de recordar y difícil de adivinar.");
            foreach (CampoEco campo in new[] { _nombre, _correo, _clave, _confirmacion }) campo.Dock = DockStyle.Top;
            foreach (CampoEco campo in new[] { _nombre, _correo, _clave }) _formulario.Controls.Add(campo);
            _formulario.Controls.Add(_medidor);
            _formulario.Controls.Add(_confirmacion);
            // La casilla va sin texto y el texto en una etiqueta que sí se ajusta al ancho; pulsarla también marca la casilla.
            var textoConsentimiento = new Label { Text = _consentimiento.Text, AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(0, 4, 0, 6) };
            textoConsentimiento.Click += (_, _) => { _consentimiento.Checked = !_consentimiento.Checked; _consentimiento.Focus(); };
            _consentimiento.AccessibleName = _consentimiento.Text;
            _consentimiento.Text = "";
            _consentimiento.Margin = new Padding(0, 6, 8, 6);
            var filaConsentimiento = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Top, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            filaConsentimiento.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filaConsentimiento.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filaConsentimiento.Controls.Add(_consentimiento, 0, 0);
            filaConsentimiento.Controls.Add(textoConsentimiento, 1, 0);
            filaConsentimiento.SizeChanged += (_, _) => textoConsentimiento.MaximumSize = new Size(Math.Max(120, filaConsentimiento.Width - TemaEcoAccess.Px(this, 32)), 0);
            _formulario.Controls.Add(filaConsentimiento);
            _formulario.Controls.Add(new Label { Text = "Guardamos la versión y la fecha de tu aceptación. El registro no verifica la titularidad del correo ni vincula Telegram.", AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 18) });
            TemaEcoAccess.EstilizarAccion(_crear, principal: true);
            _crear.Height = TemaEcoAccess.Px(this, 48);
            _crear.Dock = DockStyle.Top;
            _crear.Margin = Padding.Empty;
            _formulario.Controls.Add(_crear);
            _formulario.Controls.Add(_estado);
            _volver.LinkColor = _volver.VisitedLinkColor = TemaEcoAccess.VerdeOscuro;
            _volver.ActiveLinkColor = TemaEcoAccess.Verde;
            _volver.LinkClicked += (_, _) => { if (!_ocupado) { DialogResult = DialogResult.Cancel; } };
            _formulario.Controls.Add(_volver);
            tarjeta.Controls.Add(_formulario);
            desplazamiento.Controls.Add(tarjeta);
            raiz.Controls.Add(identidad, 0, 0);
            raiz.Controls.Add(desplazamiento, 1, 0);
            Controls.Add(raiz);

            _medidor.Height = TemaEcoAccess.Px(this, 30);
            _medidor.Dock = DockStyle.Top;
            for (int i = 0; i < _formulario.Controls.Count; i++) _formulario.Controls[i].TabIndex = i;

            // Validación inmediata: al salir de cada campo y, una vez mostrado un error, mientras se corrige.
            _nombre.Campo.Leave += (_, _) => ValidarNombre();
            _correo.Campo.Leave += (_, _) => ValidarCorreo();
            _clave.Campo.TextChanged += (_, _) => { _medidor.Evaluar(_clave.Campo.Text); if (_confirmacion.Campo.Text.Length > 0) ValidarConfirmacion(); };
            _clave.Campo.Leave += (_, _) => ValidarClave();
            _confirmacion.Campo.TextChanged += (_, _) => ValidarConfirmacion();
            _crear.Click += async (_, _) => await RegistrarAsync();
            AcceptButton = _crear;
            CancelButton = null;
            KeyPreview = true;
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape && !_ocupado) DialogResult = DialogResult.Cancel; };
            FormClosing += (_, e) => e.Cancel = _ocupado;

            void Adaptar()
            {
                bool compacta = ClientSize.Width < TemaEcoAccess.Px(this, 860);
                identidad.Visible = !compacta;
                raiz.ColumnStyles[0].SizeType = compacta ? SizeType.Absolute : SizeType.Percent;
                raiz.ColumnStyles[0].Width = compacta ? 0 : 42;
                raiz.ColumnStyles[1].Width = compacta ? 100 : 58;
                int ancho = Math.Max(TemaEcoAccess.Px(this, 300), Math.Min(TemaEcoAccess.Px(this, 560), desplazamiento.ClientSize.Width - desplazamiento.Padding.Horizontal));
                tarjeta.MinimumSize = tarjeta.MaximumSize = new Size(ancho, 0);
                tarjeta.Location = new Point(Math.Max(desplazamiento.Padding.Left, (desplazamiento.ClientSize.Width - ancho) / 2) + desplazamiento.AutoScrollPosition.X,
                    desplazamiento.Padding.Top + desplazamiento.AutoScrollPosition.Y);
                int interior = ancho - tarjeta.Padding.Horizontal;
                foreach (Control control in _formulario.Controls)
                    if (control is Label or CheckBox or LinkLabel) control.MaximumSize = new Size(interior, 0);
            }
            SizeChanged += (_, _) => Adaptar();
            desplazamiento.ClientSizeChanged += (_, _) => Adaptar();
            Shown += (_, _) => { Adaptar(); _nombre.Campo.Focus(); };
        }

        // Panel de marca: por qué conviene registrarse, sin prometer canjes que todavía no existen.
        private static Control CrearIdentidad()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = TemaEcoAccess.VerdeSuave, Padding = new Padding(44, 40, 36, 32) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int alto = TemaEcoAccess.Px(panel, 44);
            panel.Controls.Add(new LogoEcoAccess { Size = new Size((int)(alto * (double)RecursosEco.Logo.Width / RecursosEco.Logo.Height), alto), Margin = new Padding(0, 0, 0, 30) });
            var titulo = new Label { Text = "Únete a EcoAccess y convierte tus acciones ambientales en ecopuntos", AutoSize = true, Font = new Font("Segoe UI Semibold", 20), ForeColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(0, 0, 0, 12) };
            var texto = new Label { Text = "Consulta tus depósitos, sigue tu participación y prepárate para futuras recompensas.", AutoSize = true, Font = TemaEcoAccess.Entrada, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 0, 0, 22) };
            panel.Controls.Add(titulo);
            panel.Controls.Add(texto);
            var beneficios = new List<Label>();
            foreach (string beneficio in new[] { "Historial de todos tus depósitos", "Ecopuntos por cada depósito verificado", "Tus datos solo se usan para gestionar tu cuenta" })
            {
                var fila = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = TemaEcoAccess.VerdeSuave, Margin = new Padding(0, 0, 0, 10) };
                int lado = TemaEcoAccess.Px(panel, 20);
                fila.Controls.Add(new PictureBox { Image = IconChar.CircleCheck.ToBitmap(IconFont.Solid, lado, TemaEcoAccess.VerdeOscuro), Size = new Size(lado, lado), Margin = new Padding(0, 2, 10, 0) });
                var etiqueta = new Label { Text = beneficio, AutoSize = true, Font = TemaEcoAccess.Cuerpo, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 2, 0, 0) };
                beneficios.Add(etiqueta);
                fila.Controls.Add(etiqueta);
                panel.Controls.Add(fila);
            }
            int lado2 = TemaEcoAccess.Px(panel, 120);
            panel.Controls.Add(new ImagenRedondeada { Imagen = RecursosEco.Eco, Size = new Size(lado2, lado2), Margin = new Padding(0, 24, 0, 0), AccessibleName = "Eco", AccessibleRole = AccessibleRole.Graphic });
            panel.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(160, panel.ClientSize.Width - panel.Padding.Horizontal);
                titulo.MaximumSize = texto.MaximumSize = new Size(ancho, 0);
                foreach (Label b in beneficios) b.MaximumSize = new Size(ancho - TemaEcoAccess.Px(panel, 32), 0);
            };
            return panel;
        }

        private bool ValidarNombre()
        {
            bool valido = !Compat.IsNullOrWhiteSpace(_nombre.Campo.Text);
            _nombre.MostrarError(valido ? null : "Escribe tu nombre.");
            return valido;
        }

        // Formato básico; la lógica de negocio valida el correo definitivo y que no esté registrado.
        private bool ValidarCorreo()
        {
            string correo = _correo.Campo.Text.Trim();
            int arroba = correo.IndexOf('@');
            bool valido = arroba > 0 && correo.LastIndexOf('.') > arroba + 1 && !correo.EndsWith('.') && !correo.Contains(' ');
            _correo.MostrarError(valido ? null : correo.Length == 0 ? "Escribe tu correo electrónico." : "Revisa el correo: debe tener la forma nombre@dominio.com.");
            return valido;
        }

        private bool ValidarClave()
        {
            bool valido = _clave.Campo.Text.Length >= 12;
            _clave.MostrarError(valido ? null : "La contraseña debe tener al menos 12 caracteres.");
            return valido;
        }

        private bool ValidarConfirmacion()
        {
            if (_confirmacion.Campo.Text.Length == 0)
            {
                _confirmacion.MostrarError("Repite la contraseña para confirmarla.");
                return false;
            }
            bool coincide = _confirmacion.Campo.Text == _clave.Campo.Text;
            if (coincide) _confirmacion.MostrarValido("Las contraseñas coinciden.");
            else _confirmacion.MostrarError("Las contraseñas no coinciden.");
            return coincide;
        }

        private async Task RegistrarAsync()
        {
            if (_ocupado) return;
            _estado.Text = "";
            bool[] validos = new bool[] { ValidarNombre(), ValidarCorreo(), ValidarClave(), ValidarConfirmacion() };
            if (!_consentimiento.Checked) _estado.Text = "Para crear la cuenta debes aceptar el tratamiento de tus datos.";
            if (validos.Contains(false) || !_consentimiento.Checked)
            {
                (new Control[] { _nombre.Campo, _correo.Campo, _clave.Campo, _confirmacion.Campo }.Zip(validos, (Control control, bool valido) => (First: control, Second: valido)).FirstOrDefault(x => !x.Second).First ?? _consentimiento).Focus();
                return;
            }

            _ocupado = true;
            _formulario.Enabled = false;
            _crear.Text = "Creando tu cuenta…";
            UseWaitCursor = true;
            bool creado = false;
            // La contraseña sale de los controles antes de llamar, para que no quede en ellos si algo falla.
            string clave = _clave.Campo.Text;
            string confirmacion = _confirmacion.Campo.Text;
            _clave.Campo.Clear();
            _confirmacion.Campo.Clear();
            _clave.OcultarContrasena();
            _confirmacion.OcultarContrasena();
            try
            {
                var resultado = await _servicio.RegistrarAsync(_nombre.Campo.Text, _correo.Campo.Text, clave, confirmacion, _consentimiento.Checked);
                if (resultado.Exitoso)
                {
                    CorreoRegistrado = _correo.Campo.Text.Trim().ToLowerInvariant();
                    creado = true;
                }
                else _estado.Text = resultado.Mensaje;
            }
            catch (Exception) { _estado.Text = "No se pudo confirmar el registro. Revisa la conexión y prueba iniciar sesión antes de repetirlo."; }
            finally
            {
                clave = confirmacion = "";
                _ocupado = false;
                _formulario.Enabled = true;
                _crear.Text = "Crear mi cuenta";
                UseWaitCursor = false;
            }
            if (creado) DialogResult = DialogResult.OK;
            else _clave.Campo.Focus();
        }
    }
}
