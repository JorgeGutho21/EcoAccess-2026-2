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
    /// Inicio del visitante y del ciudadano con el diseño de referencia de ExpoSoftware: portada con el saludo, los cuatro
    /// indicadores reales y ECO; «¿Dónde va tu residuo?» con buscador, las tres canecas, el resultado y «Así de fácil».
    /// Todo funciona: las canecas abren sus residuos, el resultado deposita, los indicadores abren ecopuntos y actividad, y
    /// los datos se actualizan solos (cada 30 s, al volver a la ventana y al terminar un depósito). No recibe servicios
    /// administrativos: todo pasa por la lógica de negocio.
    /// </summary>
    public sealed class FormExplorarEcoAccess : Form
    {
        private const int AnchoMaximo = 1800;
        internal static TimeSpan IntervaloSincronizacion => PoliticaSincronizacion.Intervalo;

        private static readonly Color FondoSeccion = Color.FromArgb(0xEE, 0xF8, 0xE8);

        private readonly ServiciosAplicacion _servicios;
        private readonly SesionActiva? _sesion;
        private readonly string _sesionOpinion = Guid.NewGuid().ToString("N");
        private readonly PanelDesplazable _scroll = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo };
        private readonly Panel _pagina = new() { BackColor = TemaEcoAccess.Fondo };
        private readonly PortadaInicio _portada;
        private readonly TarjetaIndicador _saldo = new(IconChar.Coins, "Mis ecopuntos", interactiva: true);
        private readonly TarjetaIndicador _depositos = new(IconChar.Recycle, "Mis depósitos", interactiva: true);
        private readonly TarjetaIndicador _ultima = new(IconChar.ClockRotateLeft, "Última actividad", interactiva: true);
        private readonly TarjetaIndicador _participacion = new(IconChar.Users, "Mi participación", interactiva: true);
        private readonly FlowLayoutPanel _filaEstado = new() { AutoSize = true, WrapContents = false, BackColor = TemaEcoAccess.Fondo };
        private readonly KryptonButton _actualizar = new() { AutoSize = true, AccessibleName = "Actualizar mi estado ahora (también se actualiza solo)" };
        private readonly Label _estadoActualizacion = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(14, 12, 0, 0) };
        private readonly System.Windows.Forms.Timer _sincronizacion = new();
        private readonly Panel _seccion = new() { BackColor = FondoSeccion };
        private readonly CajaBusqueda _busqueda = new() { AccessibleName = "Buscar un residuo del prototipo" };
        private readonly KryptonButton _buscar = new() { AutoSize = false, AccessibleName = "Buscar residuo" };
        private readonly Label _titulo = new() { Text = "¿Dónde va tu residuo?", AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold | FontStyle.Italic), ForeColor = TemaEcoAccess.VerdeOscuro, BackColor = FondoSeccion };
        private readonly Label _subtitulo = new() { Text = "Escribe el nombre de tu residuo y consulta en qué caneca entregarlo.", AutoSize = true, ForeColor = TemaEcoAccess.Texto, Font = TemaEcoAccess.Entrada, BackColor = FondoSeccion };
        private readonly List<TarjetaCaneca> _canecas = new();
        private readonly PanelResultado _resultado = new();
        private readonly PasosFacil _pasos = new();
        private readonly ToolTip _ayudas = new();
        private ResiduoPrototipo _residuoElegido = CatalogoPrototipo.Buscar("botella-agua")!;
        private IReadOnlyList<ResiduoPrototipo> _coincidencias = Array.Empty<ResiduoPrototipo>();
        private bool _actualizando;
        private int _fallosSeguidos;
        private DateTime _ultimaActualizacionUtc = DateTime.MinValue;
        private CanecaPublica? _demo;
        private bool _cerrando;

        public bool SolicitarInicioSesion { get; private set; }

        public FormExplorarEcoAccess(ServiciosAplicacion servicios, SesionActiva? sesion)
        {
            _servicios = servicios;
            _sesion = sesion;
            Text = sesion is null ? "EcoAccess · Explora y deposita como invitado" : $"EcoAccess · {sesion.Nombre}";
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            Font = TemaEcoAccess.Cuerpo;
            Size = new Size(1280, 850);
            MinimumSize = new Size(900, 600);
            WindowState = FormWindowState.Maximized;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;

            string nombre = sesion?.Nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            _portada = new PortadaInicio(
                sesion is null ? "¡Bienvenido a EcoAccess!" : $"¡Hola, {nombre}!",
                sesion is null
                    ? "Separa bien tus residuos: búscalo o elige una caneca. Como invitado puedes depositar, sin acumular ecopuntos."
                    : "Gestiona tus residuos, consulta tu actividad y sigue acumulando ecopuntos por tus acciones sostenibles.",
                sesion is null ? Array.Empty<Control>() : new Control[] { _saldo, _depositos, _ultima, _participacion },
                sesion is null ? CrearInvitacion() : null);
            _saldo.Click += (_, _) => MostrarMisEcopuntos();
            _participacion.Click += (_, _) => MostrarMisEcopuntos();
            _depositos.Click += (_, _) => MostrarMiActividad();
            _ultima.Click += (_, _) => MostrarMiActividad();
            foreach (var t in new[] { _saldo, _depositos, _ultima, _participacion }) _ayudas.SetToolTip(t, t == _saldo || t == _participacion ? "Abrir mis ecopuntos" : "Abrir mi actividad");

            _pagina.Controls.Add(_portada);
            _pagina.Controls.Add(CrearFilaEstado());
            _pagina.Controls.Add(_seccion);
            CrearSeccion();
            _scroll.Controls.Add(_pagina);
            Controls.Add(_scroll);
            Controls.Add(CrearBarraSuperior());

            _resultado.Mostrar(_residuoElegido, _sesion is null, 1);
            _resultado.Depositar += (_, _) => AbrirAsistente(_residuoElegido);
            _resultado.VerMas += (_, _) => ElegirEntre($"Resultados de la búsqueda ({_coincidencias.Count})", "Elige el residuo que tienes en la mano.", _coincidencias);
            _buscar.Click += (_, _) => Buscar(_busqueda.Campo.Text);
            _busqueda.Campo.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                Buscar(_busqueda.Campo.Text);
            };
            _scroll.ClientSizeChanged += (_, _) => Adaptar();

            // Sincronización: periódica, al volver a la ventana y al terminar un depósito. Nunca dos consultas a la vez.
            _sincronizacion.Interval = (int)IntervaloSincronizacion.TotalMilliseconds;
            _sincronizacion.Tick += async (_, _) => await ActualizarMiEstadoAsync(automatica: true);
            Activated += async (_, _) =>
            {
                if (_sesion is not null && DateTime.UtcNow - _ultimaActualizacionUtc > TimeSpan.FromSeconds(15) && _fallosSeguidos == 0)
                    await ActualizarMiEstadoAsync(automatica: true);
            };
            Shown += async (_, _) => { Adaptar(); await CargarAsync(); if (_sesion is not null) _sincronizacion.Start(); };
            FormClosing += (_, _) => { _cerrando = true; _sincronizacion.Stop(); };
        }

        private int P(int u) => TemaEcoAccess.Px(this, u);

        // Barra superior como la referencia: logotipo a la izquierda; rol, usuario y «Cerrar sesión» a la derecha.
        private Control CrearBarraSuperior()
        {
            var barra = new TableLayoutPanel { Dock = DockStyle.Top, Height = P(68), ColumnCount = 2, BackColor = TemaEcoAccess.Superficie, Padding = new Padding(28, 0, 24, 0) };
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            barra.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            int altoLogo = P(40);
            barra.Controls.Add(new LogoEcoAccess { Size = new Size((int)(altoLogo * (double)RecursosEco.Logo.Width / RecursosEco.Logo.Height), altoLogo), Anchor = AnchorStyles.Left }, 0, 0);
            var derecha = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, WrapContents = false, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            var pastilla = new PastillaEstado { Margin = new Padding(0, 8, 18, 0) };
            if (_sesion is null)
            {
                pastilla.Mostrar("Modo invitado", TemaEcoAccess.Suave, TemaEcoAccess.Fondo);
                derecha.Controls.Add(pastilla);
                derecha.Controls.Add(Boton("Iniciar sesión", false, IconChar.RightToBracket, (_, _) => VolverAlInicio()));
                derecha.Controls.Add(Boton("Crear cuenta", true, IconChar.UserPlus, (_, _) => CrearCuenta()));
            }
            else
            {
                pastilla.Mostrar("Ciudadano", TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave);
                derecha.Controls.Add(pastilla);
                derecha.Controls.Add(new Label { Text = _sesion.Nombre, AutoSize = true, Font = new Font("Segoe UI", 13), Margin = new Padding(0, 9, 18, 0) });
                derecha.Controls.Add(new Label { AutoSize = false, Size = new Size(1, P(28)), BackColor = TemaEcoAccess.Borde, Margin = new Padding(0, 9, 18, 0) });
                derecha.Controls.Add(Boton("Cerrar sesión", false, IconChar.RightFromBracket, (_, _) => VolverAlInicio()));
            }
            barra.Controls.Add(derecha, 1, 0);
            barra.Paint += (_, e) =>
            {
                using var borde = new Pen(TemaEcoAccess.Borde);
                e.Graphics.DrawLine(borde, 0, barra.Height - 1, barra.Width, barra.Height - 1);
            };
            return barra;
        }

        private KryptonButton Boton(string texto, bool principal, IconChar icono, EventHandler accion)
        {
            var boton = new KryptonButton { AutoSize = true, Margin = new Padding(8, 0, 0, 0), AccessibleName = texto };
            TemaEcoAccess.EstilizarAccion(boton, principal);
            boton.Values.Text = texto;
            boton.Values.Image = CacheIconos.Obtener(icono, P(16), principal ? Color.White : TemaEcoAccess.VerdeOscuro);
            boton.Click += accion;
            return boton;
        }

        // Invitado: en el lugar de los indicadores, la invitación a crear la cuenta (sin prometer canjes que no existen).
        private Control CrearInvitacion()
        {
            var tarjeta = new TarjetaAccionRapida(IconChar.Award, "¿Sabías que puedes acumular ecopuntos?",
                "Crea tu cuenta para llevar el historial de tus depósitos y sumar ecopuntos cuando se verifiquen. Pulsa aquí para registrarte.");
            tarjeta.Click += (_, _) => CrearCuenta();
            return new ContenedorAltoTarjeta(tarjeta);
        }

        // Debajo de la portada: «Actualizar mi estado» (opcional: también se actualiza solo), la hora y accesos secundarios.
        private Control CrearFilaEstado()
        {
            if (_sesion is not null)
            {
                TemaEcoAccess.EstilizarAccion(_actualizar);
                _actualizar.Values.Text = "Actualizar mi estado";
                _actualizar.Values.Image = CacheIconos.Obtener(IconChar.ClockRotateLeft, P(18), TemaEcoAccess.VerdeOscuro);
                _actualizar.Click += async (_, _) => await ActualizarMiEstadoAsync(automatica: false);
                _filaEstado.Controls.Add(_actualizar);
                _filaEstado.Controls.Add(new Label { AutoSize = false, Size = new Size(1, P(28)), BackColor = TemaEcoAccess.Borde, Margin = new Padding(18, 9, 0, 0) });
                _estadoActualizacion.Text = "Tus datos se actualizan solos cada 30 segundos.";
                _filaEstado.Controls.Add(_estadoActualizacion);
            }
            foreach (var (texto, icono, accion) in new (string, IconChar, Action)[]
            {
                ("QR de la estación", IconChar.Qrcode, MostrarQrPiloto),
                ("Tu opinión", IconChar.Star, MostrarOpiniones),
            })
            {
                if (texto == "Tu opinión" && _servicios.Opiniones is null) continue;
                var enlace = new LinkLabel { Text = texto, AutoSize = true, LinkColor = TemaEcoAccess.Verde, ActiveLinkColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(22, 12, 0, 0), AccessibleName = texto };
                enlace.LinkClicked += (_, _) => accion();
                _filaEstado.Controls.Add(enlace);
            }
            return _filaEstado;
        }

        private void CrearSeccion()
        {
            _seccion.Controls.Add(_titulo);
            _seccion.Controls.Add(_subtitulo);
            _busqueda.Height = P(50);
            _busqueda.Campo.MaxLength = 60;
            _seccion.Controls.Add(_busqueda);
            TemaEcoAccess.EstilizarAccion(_buscar, principal: true);
            _buscar.Values.Text = "Buscar";
            _buscar.Values.Image = CacheIconos.Obtener(IconChar.MagnifyingGlass, P(18), Color.White);
            _seccion.Controls.Add(_buscar);
            foreach (var (caneca, titulo, descripcion) in CatalogoPrototipo.Canecas)
            {
                var tarjeta = new TarjetaCaneca(caneca, titulo, descripcion) { BackColor = FondoSeccion };
                tarjeta.Click += (_, _) => ElegirCaneca(caneca);
                _ayudas.SetToolTip(tarjeta, $"Ver los residuos de la {titulo.ToLowerInvariant()}");
                _canecas.Add(tarjeta);
                _seccion.Controls.Add(tarjeta);
            }
            _seccion.Controls.Add(_resultado);
            _seccion.Controls.Add(_pasos);
            _seccion.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(TemaEcoAccess.Fondo);
                using (var ruta = RecursosEco.Redondeado(new RectangleF(0, 0, _seccion.Width - 1, _seccion.Height - 1), P(24)))
                using (var fondo = new SolidBrush(FondoSeccion))
                    e.Graphics.FillPath(fondo, ruta);
                Bitmap hoja = CacheIconos.Obtener(IconChar.Leaf, P(54), TemaEcoAccess.Verde);
                e.Graphics.DrawImage(hoja, P(22), P(24));
                Bitmap hojaDerecha = CacheIconos.Obtener(IconChar.Leaf, P(40), Color.FromArgb(150, 0x6C, 0xBF, 0x2E));
                e.Graphics.DrawImage(hojaDerecha, _seccion.Width - hojaDerecha.Width - P(8), P(8));
            };
        }

        /// <summary>Coloca todo a lo ancho disponible: sin espacio vacío a un lado y, en escritorio, sin desplazamiento.</summary>
        private void Adaptar()
        {
            int margen = P(24);
            int ancho = Math.Min(P(AnchoMaximo), Math.Max(P(860), _scroll.ClientSize.Width - 2 * margen));
            int x = Math.Max(margen, (_scroll.ClientSize.Width - ancho) / 2);
            _pagina.SuspendLayout();
            _portada.Bounds = new Rectangle(0, 0, _scroll.ClientSize.Width, _portada.MedirAlto(_scroll.ClientSize.Width));
            int y = _portada.Bottom + P(6);
            // Tamaño explícito: con AutoSize la fila quedaba más alta de lo medido y tapaba el borde del panel verde.
            _filaEstado.AutoSize = false;
            _filaEstado.Size = _filaEstado.GetPreferredSize(Size.Empty);
            _filaEstado.Location = new Point(x + P(4), y);
            y = _filaEstado.Bottom + P(6);
            _seccion.Bounds = new Rectangle(x, y, ancho, AdaptarSeccion(ancho));
            _pagina.Size = new Size(_scroll.ClientSize.Width, _seccion.Bottom + P(8));
            _pagina.ResumeLayout(true);
            _pagina.Location = new Point(_scroll.AutoScrollPosition.X, _scroll.AutoScrollPosition.Y);
        }

        private int AdaptarSeccion(int ancho)
        {
            int m = P(28), espacio = P(18);
            int y = P(22);
            int xTexto = P(96);
            int anchoBuscar = P(150);
            int anchoCaja = Math.Max(P(260), ancho * 45 / 100);
            _buscar.Bounds = new Rectangle(ancho - m - anchoBuscar, y + P(10), anchoBuscar, P(50));
            _busqueda.Bounds = new Rectangle(_buscar.Left - P(14) - anchoCaja, y + P(10), anchoCaja, P(50));
            int anchoTexto = Math.Max(P(200), _busqueda.Left - xTexto - P(16));
            _titulo.MaximumSize = _subtitulo.MaximumSize = new Size(anchoTexto, 0);
            _titulo.Location = new Point(xTexto, y);
            _subtitulo.Location = new Point(xTexto + P(2), _titulo.Bottom + P(2));
            y = Math.Max(_subtitulo.Bottom, _busqueda.Bottom) + P(18);
            int anchoCaneca = (ancho - 2 * m - 2 * espacio) / 3;
            int altoCaneca = _canecas.Max(c => c.MedirAlto(anchoCaneca));
            for (int i = 0; i < _canecas.Count; i++)
                _canecas[i].Bounds = new Rectangle(m + i * (anchoCaneca + espacio), y, anchoCaneca, altoCaneca);
            y += altoCaneca + P(12);
            int anchoResultado = (ancho - 2 * m - espacio) * 58 / 100;
            int altoInferior = Math.Max(P(170), _resultado.MedirAlto(anchoResultado));
            _resultado.Bounds = new Rectangle(m, y, anchoResultado, altoInferior);
            _pasos.Bounds = new Rectangle(_resultado.Right + espacio, y, ancho - m - _resultado.Right - espacio, altoInferior);
            return y + altoInferior + P(22);
        }

        // ── Canecas, búsqueda y residuos ─────────────────────────────────────────────────────────────────────────────

        private void ElegirCaneca(CategoriaMaterial caneca)
        {
            foreach (TarjetaCaneca t in _canecas) t.Seleccionada = t.Caneca == caneca;
            var lista = CatalogoPrototipo.DeCaneca(caneca);
            string nombre = CatalogoPrototipo.NombreCaneca(caneca);
            string descripcion = CatalogoPrototipo.Canecas.First(c => c.Caneca == caneca).Descripcion;
            ElegirEntre($"{nombre}: {descripcion} ({lista.Count})", "Elige el residuo que tienes para ver cómo depositarlo.", lista);
        }

        private void ElegirEntre(string titulo, string subtitulo, IReadOnlyList<ResiduoPrototipo> residuos)
        {
            if (residuos.Count == 0) return;
            using var ventana = new FormResiduosCaneca(titulo, subtitulo, residuos);
            if (ventana.ShowDialog(this) == DialogResult.OK && ventana.Elegido is { } elegido) MostrarResiduo(elegido, 1);
        }

        private void Buscar(string? texto)
        {
            BusquedaPrototipo b = CatalogoPrototipo.Consultar(texto);
            if (b.FueraDeCatalogo)
            {
                // Lo especial o peligroso no se asigna a ninguna caneca: ninguna se resalta y no se ofrece depositar.
                foreach (TarjetaCaneca t in _canecas) t.Seleccionada = false;
                _resultado.MostrarAdvertencia(texto ?? "");
                Adaptar();
                return;
            }
            if (Compat.IsNullOrWhiteSpace(texto)) { _busqueda.Campo.Focus(); return; }
            if (b.Residuos.Count == 0)
            {
                _subtitulo.ForeColor = TemaEcoAccess.Error;
                _subtitulo.Text = b.Mensaje;
                return;
            }
            _subtitulo.ForeColor = TemaEcoAccess.Texto;
            _subtitulo.Text = "Escribe el nombre de tu residuo y consulta en qué caneca entregarlo.";
            _coincidencias = b.Residuos;
            MostrarResiduo(b.Residuos[0], b.Residuos.Count);
        }

        private void MostrarResiduo(ResiduoPrototipo residuo, int coincidencias)
        {
            _residuoElegido = residuo;
            foreach (TarjetaCaneca t in _canecas) t.Seleccionada = t.Caneca == residuo.Caneca;
            _resultado.Mostrar(residuo, _sesion is null, coincidencias);
            Adaptar();
        }

        // ── Datos reales del ciudadano y sincronización ─────────────────────────────────────────────────────────────

        private async Task CargarAsync()
        {
            try
            {
                using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var demo = await _servicios.CatalogoAmbiental.ConsultarPilotoAsync(consulta.Token).WaitAsync(consulta.Token);
                _demo = demo.Exitoso ? demo.Valor : null;
            }
            catch (Exception) when (!_cerrando && !IsDisposed) { _demo = null; }
            catch (Exception) when (_cerrando || IsDisposed) { return; }
            if (_sesion is not null) await ActualizarMiEstadoAsync(automatica: true);
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
            var colombia = System.Globalization.CultureInfo.GetCultureInfo("es-CO");
            if (saldo.Exitoso) _saldo.Mostrar(saldo.Valor.Saldo.ToString("N0", colombia), "Verificados.");
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
                _depositos.Mostrar(total.ToString("N0", colombia), $"Últimos 30 días: {verificados:N0} verificados");
                _participacion.Mostrar(verificados.ToString("N0", colombia), "Depósitos verificados · Últimos 30 días.");
                if (ultimos.Exitoso && ultimos.Valor.Filas.Count > 0)
                {
                    var ultimo = ultimos.Valor.Filas[0];
                    _ultima.Mostrar(HoraColombia.DesdeUtc(ultimo.FechaDeteccionUtc).ToString("dd/MM/yyyy", colombia),
                        $"{ultimo.MaterialDeclarado} · {(ultimo.MaterialVerificado is null ? "pendiente de verificación" : "verificado")}.");
                }
                else _ultima.Mostrar("—", ultimos.Exitoso ? "Sin depósitos en los últimos 30 días." : "No disponible para esta cuenta.");
            }
            Adaptar();
        }

        /// <summary>
        /// Vuelve a leer Oracle (no modifica ningún depósito). Una sola consulta a la vez; con la ventana minimizada no se
        /// consulta. Si falla, espera el doble antes de reintentar (hasta 2 minutos) y conserva los últimos valores.
        /// </summary>
        private async Task ActualizarMiEstadoAsync(bool automatica)
        {
            if (_actualizando || _sesion is null || _cerrando || IsDisposed) return;
            if (automatica && WindowState == FormWindowState.Minimized) return;
            _actualizando = true;
            _actualizar.Enabled = false;
            if (!automatica) { _estadoActualizacion.ForeColor = TemaEcoAccess.Suave; _estadoActualizacion.Text = "Consultando Oracle…"; }
            using var consulta = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try
            {
                await CargarDatosCiudadanoAsync(consulta.Token);
                if (IsDisposed) return;
                _fallosSeguidos = 0;
                _ultimaActualizacionUtc = DateTime.UtcNow;
                _sincronizacion.Interval = (int)IntervaloSincronizacion.TotalMilliseconds;
                _estadoActualizacion.ForeColor = TemaEcoAccess.Suave;
                _estadoActualizacion.Text = $"Actualizado a las {HoraColombia.DesdeUtc(DateTime.UtcNow):HH:mm} · hora de Colombia · se actualiza solo.";
            }
            catch (Exception) when (!_cerrando && !IsDisposed)
            {
                _fallosSeguidos++;
                TimeSpan espera = EsperaTrasFallos(_fallosSeguidos);
                _sincronizacion.Interval = (int)espera.TotalMilliseconds;
                _estadoActualizacion.ForeColor = TemaEcoAccess.Advertencia;
                _estadoActualizacion.Text = $"Sin conexión con Oracle: se muestran los últimos datos; reintento automático en {espera.TotalSeconds:0} s.";
            }
            catch (Exception) when (_cerrando || IsDisposed) { }
            finally
            {
                _actualizando = false;
                if (!IsDisposed) { _actualizar.Enabled = true; Adaptar(); }
            }
        }

        internal static TimeSpan EsperaTrasFallos(int fallos) => PoliticaSincronizacion.EsperaTrasFallos(fallos);

        // ── Navegación a otras ventanas ─────────────────────────────────────────────────────────────────────────────

        private void MostrarQrPiloto()
        {
            // Siempre el QR oficial y definitivo del repositorio, nunca uno generado: es el que está impreso en la estación.
            var enlace = EnlaceTelegram.ParaCaneca(EnlaceTelegram.NombreBotPiloto, EnlaceTelegram.IdentificadorQrOficial);
            if (_demo is null || !_demo.Caneca.EstaEnServicio || _demo.Caneca.IdentificadorQr != EnlaceTelegram.IdentificadorQrOficial)
                MessageBox.Show(this, "Este es el QR oficial de la estación. En la base conectada ahora la caneca piloto no está en servicio con ese QR, así que al escanearlo Eco solo orientará y no abrirá la caneca.", "EcoAccess");
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

        private void AbrirAsistente(ResiduoPrototipo? residuo)
        {
            using var asistente = new FormAsistenteDeposito(_servicios, _sesion, residuo);
            asistente.ShowDialog(this);
            if (asistente.SolicitarRegistro) { CrearCuenta(); return; }
            // Refresco al terminar una operación: el depósito nuevo aparece sin esperar al siguiente ciclo.
            if (_sesion is not null) _ = ActualizarMiEstadoAsync(automatica: true);
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
            if (disposing) { _sincronizacion.Dispose(); _ayudas.Dispose(); }
            base.Dispose(disposing);
        }
    }

    /// <summary>Envuelve una tarjeta dibujada para que su alto siga a su ancho dentro de un contenedor de posición manual.</summary>
    internal sealed class ContenedorAltoTarjeta : Panel
    {
        private readonly TarjetaDibujada _tarjeta;

        public ContenedorAltoTarjeta(TarjetaDibujada tarjeta)
        {
            _tarjeta = tarjeta;
            tarjeta.Margin = Padding.Empty;
            BackColor = Color.Transparent;
            Controls.Add(tarjeta);
        }

        public override Size GetPreferredSize(Size proposedSize) =>
            new(proposedSize.Width, _tarjeta.MedirAlto(Math.Max(1, proposedSize.Width)));

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            _tarjeta.Bounds = new Rectangle(0, 0, Width, _tarjeta.MedirAlto(Math.Max(1, Width)));
        }
    }
}
