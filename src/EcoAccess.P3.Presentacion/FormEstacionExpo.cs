using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35;
using EcoAccess.P3.Logica.Vision35.Expo;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>Servicios de la estación compartidos por las pantallas del PC (se componen en Program.cs).</summary>
    internal static class EstacionPc
    {
        /// <summary>Repositorio de OPERACION_ESTACION; null si no hay conexión a Oracle configurada.</summary>
        internal static IRegistroOperacionesEstacion? Oracle { get; set; }

        /// <summary>Consulta de estadísticas de la estación (la conecta Program.cs con el repositorio Oracle).</summary>
        internal static Func<DateTime, DateTime, System.Threading.CancellationToken, Task<IReadOnlyList<FilaEstadisticaEstacion>>>? Estadisticas { get; set; }

        private static ServicioEstacionExpo? _servicio;
        private static readonly ReconocedorExpo9 _reconocedor = new();

        internal static ReconocedorExpo9 Reconocedor => _reconocedor;

        /// <summary>
        /// Servicio central de hardware: el panel de este equipo si es el anfitrión, o el del anfitrión de la red configurado con
        /// Configurar-Anfitrion.ps1. Sin ninguno de los dos, simulación declarada (nunca se presenta como apertura real).
        /// </summary>
        internal static async Task<ServicioEstacionExpo> ServicioAsync()
        {
            if (_servicio is not null) return _servicio;
            var panel = new ClienteEstacionPanel();
            var estado = await panel.EstadoAsync().ConfigureAwait(false);
            // Con un anfitrión de red configurado nunca se cae a simulación: si no responde, la apertura falla y se dice.
            IEstacionCanecasExpo estacion = estado.Disponible || AnfitrionEstacion.EsRemoto(panel.Url) ? panel : new EstacionExpoSimulada();
            _servicio = new ServicioEstacionExpo(estacion, new RegistroOperacionesConCola(Oracle));
            return _servicio;
        }
    }

    /// <summary>
    /// Estación ExpoSoftware en el PC. MODO AUTOMÁTICO: foto (cámara de Windows o archivo) → una inferencia local →
    /// 9 materiales → canecas → apertura 7/14/21 s. MODO MANUAL: la persona elige 1, 2 o 3 canecas (sin Ollama).
    /// Ambos usan la misma estación (panel local, único dueño del ESP32) y el mismo registro en Oracle que Telegram.
    /// </summary>
    internal sealed class FormEstacionExpo : Form
    {
        private readonly int? _idUsuario;
        private readonly PictureBox _foto = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        private readonly Label _resultado = new() { Dock = DockStyle.Fill, Font = TemaEcoAccess.Cuerpo, ForeColor = TemaEcoAccess.Texto, AutoSize = false };
        private readonly Label _cuenta = new() { AutoSize = true, Font = TemaEcoAccess.Valor, ForeColor = TemaEcoAccess.VerdeOscuro, Text = "" };
        private readonly PastillaEstado _estado = new();
        private readonly ListView _historial = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        private readonly BotonEco _btnCamara = new("Tomar foto", IconChar.Camera, BotonEco.Estilo.Principal) { AccessibleName = "Tomar foto con la cámara" };
        private readonly BotonEco _btnArchivo = new("Cargar imagen", IconChar.Image, BotonEco.Estilo.Secundario) { AccessibleName = "Cargar imagen del computador" };
        private readonly BotonEco _btnAbrir = new("Abrir canecas", IconChar.BoxOpen, BotonEco.Estilo.Principal) { Enabled = false, AccessibleName = "Abrir las canecas recomendadas" };
        private readonly Dictionary<string, Panel> _tarjetas = new();
        private readonly System.Windows.Forms.Timer _reloj = new() { Interval = 250 };
        private FileSystemWatcher? _vigilante;
        private ResultadoExpo9? _ultimo;
        private DateTime _finUtc;
        private CancellationTokenSource? _cts;

        public FormEstacionExpo(int? idUsuario = null)
        {
            _idUsuario = idUsuario;
            Text = "Estación ExpoSoftware · EcoAccess";
            AccessibleName = Text;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = TemaEcoAccess.Fondo;
            Font = TemaEcoAccess.Cuerpo;
            Size = new Size(TemaEcoAccess.Px(this, 1180), TemaEcoAccess.Px(this, 740));
            MinimumSize = new Size(TemaEcoAccess.Px(this, 960), TemaEcoAccess.Px(this, 620));

            var cab = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(TemaEcoAccess.Px(this, 16)), BackColor = TemaEcoAccess.Superficie, WrapContents = true };
            cab.Controls.Add(new Label { Text = "Estación de tres canecas", AutoSize = true, Font = TemaEcoAccess.Titulo, ForeColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(0, 0, 16, 0) });
            _estado.Margin = new Padding(0, 10, 16, 0);
            cab.Controls.Add(_estado);
            foreach (var b in new[] { _btnCamara, _btnArchivo, _btnAbrir }) { b.Margin = new Padding(0, 4, 8, 0); b.Size = b.GetPreferredSize(Size.Empty); cab.Controls.Add(b); }

            var manual = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(TemaEcoAccess.Px(this, 16), 6, 16, 6), BackColor = TemaEcoAccess.Fondo };
            manual.Controls.Add(new Label { Text = "Modo manual (sin reconocimiento):", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 10, 8, 0) });
            foreach (string c in new[] { "V", "B", "N", "VB", "VN", "BN", "VBN" })
            {
                var b = new BotonEco(Nombres(c) + " · " + PoliticaTiemposApertura.DuracionMs(c.Length) / 1000 + " s", IconChar.None, BotonEco.Estilo.Secundario) { AccessibleName = "Abrir " + Nombres(c), Margin = new Padding(0, 4, 6, 0) };
                b.Size = b.GetPreferredSize(Size.Empty);
                string letras = c;
                b.Click += async (_, _) => await AbrirAsync(Desde(letras), TipoOperacionEstacion.APERTURA_MANUAL, null);
                manual.Controls.Add(b);
            }

            var cuerpo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(TemaEcoAccess.Px(this, 16)) };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 62)); cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
            var marcoFoto = new TarjetaEco { Dock = DockStyle.Fill, Padding = new Padding(10) }; marcoFoto.Controls.Add(_foto);
            cuerpo.Controls.Add(marcoFoto, 0, 0);

            var derecha = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            derecha.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 120)));
            derecha.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var filaCanecas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            for (int i = 0; i < 3; i++) filaCanecas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            foreach (var (letra, nombre, cat) in new[] { ("V", "VERDE", CategoriaMaterial.OrganicoAprovechable), ("B", "BLANCA", CategoriaMaterial.Aprovechable), ("N", "NEGRA", CategoriaMaterial.NoAprovechable) })
            {
                var t = new TarjetaEco { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 8), Padding = new Padding(12) };
                t.Controls.Add(new Label { Text = nombre + "\n" + ResultadoExpo9.Categoria(cat), Dock = DockStyle.Fill, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Suave, TextAlign = ContentAlignment.MiddleCenter });
                _tarjetas[letra] = t; filaCanecas.Controls.Add(t);
            }
            derecha.Controls.Add(filaCanecas, 0, 0);
            derecha.Controls.Add(_cuenta, 0, 1);
            var marcoRes = new TarjetaEco { Dock = DockStyle.Fill, Padding = new Padding(14) }; marcoRes.Controls.Add(_resultado);
            derecha.Controls.Add(marcoRes, 0, 2);
            cuerpo.Controls.Add(derecha, 1, 0);

            _historial.Columns.Add("Hora", 110); _historial.Columns.Add("Canal", 80); _historial.Columns.Add("Tipo", 110);
            _historial.Columns.Add("Canecas", 150); _historial.Columns.Add("Tiempo", 60); _historial.Columns.Add("Estado", 140); _historial.Columns.Add("Materiales", 330);
            var grupo = new GroupBox { Text = "Historial compartido (Oracle Cloud: PC, Telegram y web)", Dock = DockStyle.Fill };
            grupo.Controls.Add(_historial);
            cuerpo.Controls.Add(grupo, 0, 1); cuerpo.SetColumnSpan(grupo, 2);

            Controls.Add(cuerpo); Controls.Add(manual); Controls.Add(cab);
            _resultado.Text = "Toma o carga una foto de los residuos. Reconozco 9 materiales: manzana, banano y papa (verde); papel kraft o cartón, botella plástica y frasco de vidrio (blanca); papel higiénico, servilleta y envoltura metalizada (negra).";

            _btnArchivo.Click += async (_, _) => await CargarArchivoAsync();
            _btnCamara.Click += (_, _) => TomarFoto();
            _btnAbrir.Click += async (_, _) => { if (_ultimo is { } r) await AbrirAsync(r.Canecas, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, r); };
            _reloj.Tick += (_, _) => Contar();
            Shown += async (_, _) => { await RefrescarEstadoAsync(); await RefrescarHistorialAsync(); _ = EstacionPc.Reconocedor.PrecalentarAsync(); };
            FormClosing += (_, _) => { _reloj.Stop(); _vigilante?.Dispose(); _cts?.Cancel(); };
            TemaEcoAccess.Aplicar(this);
        }

        private async Task RefrescarEstadoAsync()
        {
            var servicio = await EstacionPc.ServicioAsync();
            var e = await servicio.EstadoAsync();
            if (servicio.Estacion.EsSimulada) _estado.Mostrar("● Simulación: este equipo no tiene canecas", TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave);
            else if (e.Conectado && e.ControlActivo) _estado.Mostrar($"● ESP32 en {e.Puerto} · control activo", TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave);
            else if (e.Conectado) _estado.Mostrar($"● ESP32 en {e.Puerto} · activa el control en el panel", TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave);
            else _estado.Mostrar("● " + e.Texto, TemaEcoAccess.Error, TemaEcoAccess.ErrorSuave);
        }

        /// <summary>Abre la app Cámara de Windows y analiza sola la foto nueva que aparezca en «Álbum de cámara».</summary>
        private void TomarFoto()
        {
            string carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Camera Roll");
            Directory.CreateDirectory(carpeta);
            _vigilante?.Dispose();
            _vigilante = new FileSystemWatcher(carpeta) { Filter = "*.jpg", EnableRaisingEvents = true };
            _vigilante.Created += (_, a) => BeginInvoke(new Action(async () =>
            {
                _vigilante?.Dispose(); _vigilante = null;
                for (int i = 0; i < 20; i++) { try { using (File.Open(a.FullPath, FileMode.Open, FileAccess.Read, FileShare.None)) break; } catch (IOException) { await Task.Delay(150); } }
                Activate();
                await AnalizarAsync(File.ReadAllBytes(a.FullPath));
            }));
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("microsoft.windows.camera:") { UseShellExecute = true }); }
            catch (Exception) { _resultado.Text = "No se pudo abrir la cámara de Windows. Usa «Cargar imagen»."; }
            _resultado.Text = "Toma la foto en la app Cámara; la analizo en cuanto se guarde.";
        }

        private async Task CargarArchivoAsync()
        {
            using var dlg = new OpenFileDialog { Title = "Elegir foto de residuos", Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.webp;*.bmp" };
            if (dlg.ShowDialog(this) == DialogResult.OK) await AnalizarAsync(File.ReadAllBytes(dlg.FileName));
        }

        private async Task AnalizarAsync(byte[] bytes)
        {
            try { using var ms = new MemoryStream(bytes); var img = Image.FromStream(ms); _foto.Image?.Dispose(); _foto.Image = new Bitmap(img); }
            catch (Exception) { _resultado.Text = "El archivo no es una imagen válida."; return; }
            _btnAbrir.Enabled = false; _resultado.Text = "Identificando residuos…"; Resaltar(Array.Empty<CategoriaMaterial>());
            _cts?.Cancel(); _cts = new CancellationTokenSource();
            ResultadoExpo9 r = await Task.Run(() => EstacionPc.Reconocedor.ReconocerAsync(bytes, _cts.Token));
            if (IsDisposed) return;
            _ultimo = r;
            Resaltar(r.Canecas);
            _resultado.Text = r.Resumen() + $"\n\n(Reconocimiento local {r.Modelo}: {r.Medicion.TotalMs} ms{(r.Medicion.DesdeCache ? ", desde caché" : "")})";
            _btnAbrir.Enabled = r.Canecas.Count > 0 && r.Estado != EstadoReconocimientoExpo.GestionEspecial;
            if (r.PuedeAbrirAutomaticamente) await AbrirAsync(r.Canecas, TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, r);
            else if (_btnAbrir.Enabled) _resultado.Text += "\nHay un objeto que no reconocí con seguridad: revisa y pulsa «Abrir canecas» para confirmar.";
        }

        private async Task AbrirAsync(IReadOnlyList<CategoriaMaterial> canecas, TipoOperacionEstacion tipo, ResultadoExpo9? rec)
        {
            var servicio = await EstacionPc.ServicioAsync();
            string clave = "pc:" + Guid.NewGuid().ToString("N").Substring(0, 16);
            _btnAbrir.Enabled = false;
            var res = await servicio.EjecutarAsync(clave, CanalEstacion.PC, tipo, canecas, _idUsuario, _idUsuario is int u ? "usuario:" + u : "pc", rec);
            Resaltar(res.Canecas);
            string linea = res.AperturaEnviada
                ? $"{(res.Simulada ? "Simulación: " : "")}Abriendo {Nombres(ClienteEstacionPanel.Letras(res.Canecas))}{(res.Canecas.Count > 1 ? (res.Modalidad == "simultanea" ? " a la vez" : " una tras otra") : "")} durante {res.DuracionMs / 1000} s."
                  + (res.Simulada ? "" : " La apertura física se confirma mirando la tapa.")
                : "No se abrió ninguna caneca: " + res.Mensaje;
            linea += res.RegistradaEnOracle ? "\nOperación registrada en Oracle Cloud." : "\nRegistro pendiente de sincronización con Oracle.";
            _resultado.Text = (rec is null ? "" : rec.Resumen() + "\n\n") + linea;
            if (res.AperturaEnviada)
            {
                int total = res.Modalidad == "simultanea" ? res.DuracionMs + (res.Canecas.Count - 1) * 250 : res.Canecas.Count * (res.DuracionMs + 1200);
                _finUtc = DateTime.UtcNow.AddMilliseconds(total); _reloj.Start();
                _ = Task.Run(async () =>
                {
                    await Task.Delay(total + 1500);
                    await servicio.MarcarCicloCompletadoAsync(res, CanalEstacion.PC, tipo, _idUsuario, "pc", rec, false);
                    if (!IsDisposed) BeginInvoke(new Action(async () => await RefrescarHistorialAsync()));
                });
            }
            await RefrescarHistorialAsync();
            await RefrescarEstadoAsync();
        }

        private void Contar()
        {
            double s = (_finUtc - DateTime.UtcNow).TotalSeconds;
            if (s <= 0) { _cuenta.Text = "Tapas cerrando ✓"; _reloj.Stop(); Resaltar(Array.Empty<CategoriaMaterial>()); return; }
            _cuenta.Text = $"Cierre en {Math.Ceiling(s):0} s";
        }

        private async Task RefrescarHistorialAsync()
        {
            if (EstacionPc.Oracle is null) { _historial.Items.Clear(); _historial.Items.Add(new ListViewItem(new[] { "—", "", "", "", "", "Sin conexión a Oracle configurada", "" })); return; }
            try
            {
                var ops = await EstacionPc.Oracle.RecientesAsync(null, 20);
                _historial.BeginUpdate(); _historial.Items.Clear();
                foreach (var o in ops)
                    _historial.Items.Add(new ListViewItem(new[] { o.FechaUtc.ToLocalTime().ToString("dd/MM HH:mm:ss"), o.Canal.ToString(), o.Tipo.ToString().Replace('_', ' ').ToLowerInvariant(),
                        (o.Canecas ?? "").ToLowerInvariant().Replace(",", " + "), (o.DuracionMs / 1000) + " s", o.Estado.ToString().Replace('_', ' ').ToLowerInvariant(), o.Materiales ?? "" }));
                _historial.EndUpdate();
            }
            catch (Exception) { _historial.Items.Clear(); _historial.Items.Add(new ListViewItem(new[] { "—", "", "", "", "", "Oracle no respondió o falta la migración V030", "" })); }
        }

        private void Resaltar(IReadOnlyCollection<CategoriaMaterial> activas)
        {
            foreach (var (letra, t) in _tarjetas.Select(k => (k.Key, k.Value)))
            {
                bool on = Desde(letra).All(activas.Contains);
                if (t is TarjetaEco te) te.Relleno = on ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Superficie;
                t.Controls[0].ForeColor = on ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
                t.Invalidate();
            }
        }

        private static IReadOnlyList<CategoriaMaterial> Desde(string letras) => letras.Select(c => c switch
        {
            'V' => CategoriaMaterial.OrganicoAprovechable, 'B' => CategoriaMaterial.Aprovechable, _ => CategoriaMaterial.NoAprovechable,
        }).ToList();

        private static string Nombres(string letras) => string.Join(" + ", letras.Select(c => c switch { 'V' => "Verde", 'B' => "Blanca", _ => "Negra" }));
    }
}
