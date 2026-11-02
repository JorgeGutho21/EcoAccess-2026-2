using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.Sockets;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// «EcoAccess — Configuración inicial». Comprueba Docker, el contenedor de Oracle y el puerto, y si falta (o dejó de
    /// servir) la contraseña del esquema la pide una sola vez. Program la valida con una consulta real y solo entonces la
    /// guarda en el perfil de Windows. No hace intentos automáticos con credenciales: cada intento lo pide la persona y
    /// hay un máximo por ventana, para no bloquear la cuenta en Oracle.
    /// </summary>
    public sealed class FormActivacionOracle : Form
    {
        private const int MaximoIntentos = 3;
        private const string Contenedor = "oracle-free-p3";

        private readonly string _destino;
        private readonly Func<string?, CancellationToken, Task<(bool Correcta, bool PedirContrasena, string Mensaje)>> _probar;
        private readonly TableLayoutPanel _lista = new() { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Top, BackColor = TemaEcoAccess.Superficie };
        private readonly Dictionary<string, (PictureBox Icono, Label Texto)> _filas = new Dictionary<string, (PictureBox Icono, Label Texto)>();
        private readonly CampoEco _usuario;
        private readonly CampoEco _clave;
        private readonly KryptonButton _guardar = new() { Text = "Comprobar y guardar" };
        private readonly KryptonButton _reintentar = new() { Text = "Volver a comprobar" };
        private readonly Label _estado = new() { AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        private int _intentos;
        private bool _ocupado;

        /// <summary>Usuario y contraseña validados, listos para que Program los guarde.</summary>
        public string? Usuario { get; private set; }
        public string? Contrasena { get; private set; }

        /// <param name="destino">Host, puerto y servicio fijos, por ejemplo «localhost:1522/PRUEBAP3».</param>
        /// <param name="probar">Prueba real con Oracle: recibe la contraseña (o null para probar la configuración guardada).</param>
        public FormActivacionOracle(string destino, string usuarioSugerido, bool pedirContrasena,
            Func<string?, CancellationToken, Task<(bool Correcta, bool PedirContrasena, string Mensaje)>> probar)
        {
            _destino = destino;
            _probar = probar;
            Text = "EcoAccess — Configuración inicial";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            MinimizeBox = MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(TemaEcoAccess.Px(this, 620), TemaEcoAccess.Px(this, 700));
            AutoScroll = true;

            _usuario = new CampoEco("Usuario del esquema", IconChar.User, new TextBox { Text = usuarioSugerido, MaxLength = 30 }) { Dock = DockStyle.Top };
            _clave = new CampoEco("Contraseña del esquema", IconChar.Lock, new TextBox { MaxLength = 128 }, contrasena: true) { Dock = DockStyle.Top };
            _clave.Ayuda("Se comprueba con Oracle y se guarda solo en tu perfil de Windows, nunca en el proyecto.");

            var tarjeta = new TarjetaEco { Dock = DockStyle.Fill };
            var columna = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie };
            columna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int alto = TemaEcoAccess.Px(this, 34);
            columna.Controls.Add(new LogoEcoAccess { Size = new Size((int)(alto * (double)RecursosEco.Logo.Width / RecursosEco.Logo.Height), alto), Margin = new Padding(0, 0, 0, 16) });
            columna.Controls.Add(new Label { Text = "Estamos preparando la conexión de EcoAccess", AutoSize = true, Font = TemaEcoAccess.TituloSeccion, Margin = Padding.Empty });
            columna.Controls.Add(new Label { Text = $"Base de datos: {destino}. Solo se hace una vez en este equipo.", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 6, 0, 16) });
            _lista.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _lista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (string nombre in new[] { "Docker", "Oracle", "Puerto 1522", "Base de datos", "Credenciales", "EcoAccess" }) AgregarFila(nombre);
            columna.Controls.Add(_lista);
            var separador = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = TemaEcoAccess.Borde, Margin = new Padding(0, 14, 0, 16) };
            columna.Controls.Add(separador);
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

        private async Task ComprobarEntornoAsync(bool probarGuardada)
        {
            if (_ocupado) return;
            _ocupado = true;
            _reintentar.Enabled = _guardar.Enabled = false;
            _estado.ForeColor = TemaEcoAccess.Suave;
            _estado.Text = "Comprobando…";
            try
            {
                string? docker = await EstadoContenedorAsync();
                Marcar("Docker", docker is not null, docker is null ? "no responde. Abre Docker Desktop y vuelve a comprobar." : "conectado");
                bool sano = docker is "running healthy" or "running";
                Marcar("Oracle", docker is null ? false : sano,
                    docker is null ? "sin información" : sano ? "disponible" : docker.StartsWith("running", StringComparison.Ordinal) ? "iniciándose; espera un minuto" : $"el contenedor {Contenedor} está detenido");
                bool puerto = await PuertoAbiertoAsync();
                Marcar("Puerto 1522", puerto, puerto ? "abierto" : "sin respuesta");
                if (!puerto)
                {
                    Marcar("Base de datos", false, "no disponible todavía");
                    _estado.Text = "Oracle aún no acepta conexiones. Cuando Docker muestre el contenedor como healthy, pulsa «Volver a comprobar».";
                    return;
                }
                if (probarGuardada)
                {
                    var (correcta, pedir, mensaje) = await _probar(null, CancellationToken.None);
                    Concluir(correcta, pedir, mensaje);
                }
                else
                {
                    Marcar("Base de datos", true, "accesible");
                    Marcar("Credenciales", null, "pendientes");
                    _estado.Text = "Escribe la contraseña del esquema para terminar.";
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
            _intentos++;
            _guardar.Enabled = _reintentar.Enabled = false;
            _estado.ForeColor = TemaEcoAccess.Suave;
            _estado.Text = "Comprobando con Oracle…";
            string clave = _clave.Campo.Text;
            try
            {
                var (correcta, pedir, mensaje) = await _probar(usuario + "\n" + clave, CancellationToken.None);
                if (correcta) { Usuario = usuario; Contrasena = clave; }
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

        /// <summary>«estado salud» del contenedor según Docker, o null si Docker no responde. No inicia ni modifica nada.</summary>
        private static async Task<string?> EstadoContenedorAsync()
        {
            try
            {
                var info = new ProcessStartInfo("docker", $"inspect -f \"{{{{.State.Status}}}} {{{{if .State.Health}}}}{{{{.State.Health.Status}}}}{{{{end}}}}\" {Contenedor}")
                { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using var proceso = Process.Start(info)!;
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                string salida = await proceso.StandardOutput.ReadToEndAsync().WaitAsync(limite.Token);
                await proceso.WaitForExitAsync(limite.Token);
                return proceso.ExitCode == 0 ? salida.Trim() : null;
            }
            catch (Exception) { return null; }
        }

        private static async Task<bool> PuertoAbiertoAsync()
        {
            try
            {
                using var cliente = new TcpClient();
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await cliente.ConnectAsync("127.0.0.1", 1522).WaitAsync(limite.Token);
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
