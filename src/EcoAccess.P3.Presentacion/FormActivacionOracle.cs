using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Datos.Oracle;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// «EcoAccess — Configuración inicial» para Oracle Cloud (Autonomous Database con mTLS). Comprueba, en este orden,
    /// el wallet descargado, la red hasta Oracle Cloud, la base de datos y las credenciales del esquema. Si falta la
    /// contraseña (o dejó de servir) la pide una sola vez; Program la valida con una consulta real y solo entonces la
    /// guarda en el perfil de Windows (User Secrets), nunca en el repositorio.
    /// </summary>
    /// <remarks>
    /// No hace intentos automáticos con credenciales: cada intento lo pide la persona y hay un máximo por ventana,
    /// para no bloquear la cuenta en Oracle. Ya no depende de Docker ni de un Oracle instalado en el equipo.
    /// </remarks>
    public sealed class FormActivacionOracle : Form
    {
        private const int MaximoIntentos = 3;

        private readonly Func<string, string, string?, CancellationToken, Task<(bool Correcta, bool PedirContrasena, string Mensaje)>> _probar;
        private readonly TableLayoutPanel _lista = new() { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Top, BackColor = TemaEcoAccess.Superficie };
        private readonly Dictionary<string, (PictureBox Icono, Label Texto)> _filas = new Dictionary<string, (PictureBox Icono, Label Texto)>();
        private readonly CampoEco _wallet;
        private readonly CampoEco _alias;
        private readonly CampoEco _usuario;
        private readonly CampoEco _clave;
        private readonly KryptonButton _guardar = new() { Text = "Comprobar y guardar" };
        private readonly KryptonButton _reintentar = new() { Text = "Volver a comprobar" };
        private readonly Label _estado = new() { AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        private int _intentos;
        private bool _ocupado;

        /// <summary>Datos validados por Oracle, listos para que Program arme y guarde la cadena.</summary>
        public string? CarpetaWallet { get; private set; }
        public string? Alias { get; private set; }
        public string? Usuario { get; private set; }
        public string? Contrasena { get; private set; }

        /// <param name="carpetaWallet">Carpeta privada del wallet (por defecto %USERPROFILE%\.oracle\wallets\EcoAccess).</param>
        /// <param name="alias">Alias de tnsnames.ora, normalmente «ecoaccess_tp».</param>
        /// <param name="probar">
        /// Prueba real con Oracle: recibe carpeta, alias y «usuario\ncontraseña» (o null para probar la configuración
        /// guardada) y devuelve si funcionó, si conviene pedir la contraseña y un mensaje sin datos sensibles.
        /// </param>
        public FormActivacionOracle(string carpetaWallet, string alias, string usuarioSugerido, bool pedirContrasena,
            Func<string, string, string?, CancellationToken, Task<(bool Correcta, bool PedirContrasena, string Mensaje)>> probar)
        {
            _probar = probar;
            Text = "EcoAccess — Configuración inicial (Oracle Cloud)";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            MinimizeBox = MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(TemaEcoAccess.Px(this, 640), TemaEcoAccess.Px(this, 820));
            AutoScroll = true;

            _wallet = new CampoEco("Carpeta del wallet", IconChar.FolderOpen, new TextBox { Text = carpetaWallet, MaxLength = 260 }) { Dock = DockStyle.Top };
            _wallet.Ayuda("Descomprime aquí el ZIP «Wallet_EcoAccess» descargado de Oracle Cloud. Nunca dentro del repositorio.");
            _alias = new CampoEco("Servicio (alias de tnsnames.ora)", IconChar.Server, new TextBox { Text = alias, MaxLength = 64 }) { Dock = DockStyle.Top };
            _usuario = new CampoEco("Usuario del esquema", IconChar.User, new TextBox { Text = usuarioSugerido, MaxLength = 128 }) { Dock = DockStyle.Top };
            _clave = new CampoEco("Contraseña del esquema", IconChar.Lock, new TextBox { MaxLength = 128 }, contrasena: true) { Dock = DockStyle.Top };
            _clave.Ayuda("Se comprueba con Oracle y se guarda solo en tu perfil de Windows, nunca en el proyecto.");

            var tarjeta = new TarjetaEco { Dock = DockStyle.Fill };
            var columna = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie };
            columna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int alto = TemaEcoAccess.Px(this, 34);
            columna.Controls.Add(new LogoEcoAccess { Size = new Size((int)(alto * (double)RecursosEco.Logo.Width / RecursosEco.Logo.Height), alto), Margin = new Padding(0, 0, 0, 16) });
            columna.Controls.Add(new Label { Text = "Conectemos EcoAccess con Oracle Cloud", AutoSize = true, Font = TemaEcoAccess.TituloSeccion, Margin = Padding.Empty });
            columna.Controls.Add(new Label { Text = "Base compartida del equipo (Autonomous Database, mTLS). Solo se hace una vez en este equipo.", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 6, 0, 16) });
            _lista.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (string nombre in new[] { "Wallet", "Red", "Base de datos", "Credenciales", "EcoAccess" }) AgregarFila(nombre);
            columna.Controls.Add(_lista);
            columna.Controls.Add(new Panel { Height = 1, Dock = DockStyle.Top, BackColor = TemaEcoAccess.Borde, Margin = new Padding(0, 14, 0, 16) });
            columna.Controls.Add(_wallet);
            columna.Controls.Add(_alias);
            columna.Controls.Add(_usuario);
            columna.Controls.Add(_clave);
            var botones = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            TemaEcoAccess.EstilizarAccion(_guardar, principal: true);
            TemaEcoAccess.EstilizarAccion(_reintentar);
            _guardar.AutoSize = _reintentar.AutoSize = true;
            _reintentar.Margin = new Padding(10, 0, 0, 0);
            botones.Controls.Add(_guardar);
            botones.Controls.Add(_reintentar);
            columna.Controls.Add(botones);
            columna.Controls.Add(_estado);
            tarjeta.Controls.Add(columna);
            var marco = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), BackColor = TemaEcoAccess.Fondo };
            marco.Controls.Add(tarjeta);
            Controls.Add(marco);
            columna.SizeChanged += (_, _) => _estado.MaximumSize = new Size(Math.Max(200, columna.Width - 10), 0);

            MostrarCampos(pedirContrasena);
            AcceptButton = _guardar;
            _guardar.Click += async (_, _) => await GuardarAsync();
            _reintentar.Click += async (_, _) => await ComprobarEntornoAsync(probarGuardada: !_clave.Visible);
            FormClosing += (_, e) => e.Cancel = _ocupado;
            Shown += async (_, _) => await ComprobarEntornoAsync(probarGuardada: !pedirContrasena);
        }

        private void AgregarFila(string nombre)
        {
            int lado = TemaEcoAccess.Px(this, 18);
            var icono = new PictureBox { Size = new Size(lado, lado), Margin = new Padding(0, 4, 10, 4) };
            var texto = new Label { AutoSize = true, Margin = new Padding(0, 4, 0, 4), Text = $"{nombre}: pendiente", ForeColor = TemaEcoAccess.Suave };
            _filas[nombre] = (icono, texto);
            _lista.Controls.Add(icono);
            _lista.Controls.Add(texto);
            Marcar(nombre, null, "pendiente");
        }

        /// <summary>Marca una comprobación: true correcta, false con problema, null pendiente. Icono y texto, no solo color.</summary>
        private void Marcar(string nombre, bool? correcta, string detalle)
        {
            var (icono, texto) = _filas[nombre];
            (IconChar figura, Color tono) = correcta switch
            {
                true => (IconChar.CircleCheck, TemaEcoAccess.VerdeOscuro),
                false => (IconChar.CircleXmark, TemaEcoAccess.Error),
                null => (IconChar.Circle, TemaEcoAccess.Suave),
            };
            icono.Image?.Dispose();
            icono.Image = figura.ToBitmap(correcta is null ? IconFont.Regular : IconFont.Solid, icono.Width, tono);
            texto.Text = $"{nombre}: {detalle}";
            texto.ForeColor = correcta == false ? TemaEcoAccess.Error : TemaEcoAccess.Texto;
            texto.AccessibleName = texto.Text;
        }

        private void MostrarCampos(bool visibles)
        {
            _usuario.Visible = _clave.Visible = _guardar.Visible = visibles;
            if (visibles) _clave.Campo.Focus();
        }

        /// <summary>Wallet y red; devuelve false (con el motivo ya mostrado) si todavía no se puede llegar a Oracle.</summary>
        private async Task<bool> ComprobarWalletYRedAsync()
        {
            WalletOracleNube? wallet = WalletOracleNube.Abrir(_wallet.Campo.Text.Trim(), out string motivo);
            string alias = _alias.Campo.Text.Trim();
            if (wallet is null)
            {
                Marcar("Wallet", false, motivo);
                _estado.Text = "Descarga el wallet de la base EcoAccess en Oracle Cloud, descomprímelo en la carpeta indicada y pulsa «Volver a comprobar».";
                return false;
            }

            if (!wallet.TryObtenerDestino(alias, out string host, out int puerto))
            {
                Marcar("Wallet", false, $"no contiene el servicio «{alias}». Disponibles: {string.Join(", ", wallet.Alias)}");
                return false;
            }

            Marcar("Wallet", true, $"correcto ({alias})");
            bool red = await PuertoAbiertoAsync(host, puerto);
            Marcar("Red", red, red ? $"Oracle Cloud responde en el puerto {puerto}" : $"sin respuesta del puerto {puerto}; revisa Internet o el firewall de la red");
            if (!red) Marcar("Base de datos", false, "no disponible todavía");
            return red;
        }

        private async Task ComprobarEntornoAsync(bool probarGuardada)
        {
            if (_ocupado) return;
            _ocupado = true;
            _reintentar.Enabled = _guardar.Enabled = false;
            _estado.ForeColor = TemaEcoAccess.Suave;
            _estado.Text = "Comprobando…";
            try
            {
                if (!await ComprobarWalletYRedAsync()) return;
                if (probarGuardada)
                {
                    var (correcta, pedir, mensaje) = await _probar(_wallet.Campo.Text.Trim(), _alias.Campo.Text.Trim(), null, CancellationToken.None);
                    Concluir(correcta, pedir, mensaje);
                }
                else
                {
                    Marcar("Base de datos", null, "pendiente de autenticar");
                    Marcar("Credenciales", null, "pendientes");
                    _estado.Text = "Escribe el usuario y la contraseña del esquema para terminar.";
                }
            }
            finally
            {
                _ocupado = false;
                _reintentar.Enabled = true;
                _guardar.Enabled = _intentos < MaximoIntentos;
            }
        }

        private async Task GuardarAsync()
        {
            if (_ocupado || !_guardar.Visible) return;
            string usuario = _usuario.Campo.Text.Trim();
            if (usuario.Length == 0) { _usuario.MostrarError("Escribe el usuario del esquema."); return; }
            if (_clave.Campo.Text.Length == 0) { _clave.MostrarError("Escribe la contraseña."); return; }
            if (_intentos >= MaximoIntentos) return;
            _ocupado = true;
            _guardar.Enabled = _reintentar.Enabled = false;
            _estado.ForeColor = TemaEcoAccess.Suave;
            _estado.Text = "Comprobando con Oracle Cloud…";
            string clave = _clave.Campo.Text;
            try
            {
                if (!await ComprobarWalletYRedAsync()) return;
                _intentos++;
                string carpeta = _wallet.Campo.Text.Trim(), alias = _alias.Campo.Text.Trim();
                var (correcta, pedir, mensaje) = await _probar(carpeta, alias, usuario + "\n" + clave, CancellationToken.None);
                if (correcta) { CarpetaWallet = Path.GetFullPath(carpeta); Alias = alias; Usuario = usuario; Contrasena = clave; }
                Concluir(correcta, pedir, mensaje);
            }
            finally
            {
                clave = "";
                _clave.Campo.Clear();
                _ocupado = false;
                _reintentar.Enabled = true;
                _guardar.Enabled = _intentos < MaximoIntentos && Usuario is null;
                if (_intentos >= MaximoIntentos && Usuario is null)
                    _estado.Text += " Por seguridad no se permiten más intentos en esta ventana; revisa la contraseña y vuelve a abrir EcoAccess.";
            }
        }

        private void Concluir(bool correcta, bool pedirContrasena, string mensaje)
        {
            Marcar("Base de datos", correcta || pedirContrasena, correcta || pedirContrasena ? "accesible" : mensaje);
            Marcar("Credenciales", correcta ? true : pedirContrasena ? false : null, correcta ? "válidas" : pedirContrasena ? mensaje : "pendientes");
            Marcar("EcoAccess", correcta, correcta ? "listo para iniciar" : "en espera");
            _estado.ForeColor = correcta ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Error;
            _estado.Text = correcta ? "Conexión comprobada. Abriendo EcoAccess…" : mensaje;
            if (pedirContrasena && !correcta) MostrarCampos(true);
            if (correcta)
            {
                var cierre = new System.Windows.Forms.Timer { Interval = 900 };
                cierre.Tick += (_, _) => { cierre.Dispose(); DialogResult = DialogResult.OK; };
                cierre.Start();
            }
        }

        /// <summary>Conexión TCP de prueba (sin TLS ni credenciales) para distinguir «sin red» de «credenciales».</summary>
        private static async Task<bool> PuertoAbiertoAsync(string host, int puerto)
        {
            try
            {
                using var cliente = new TcpClient();
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                await cliente.ConnectAsync(host, puerto).WaitAsync(limite.Token);
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
