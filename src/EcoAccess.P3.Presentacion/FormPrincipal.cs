using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Estructura de la aplicación: barra lateral contraíble, barra superior con estado y sesión, y el área de
    /// contenido donde se abren los módulos. Los servicios revalidan la cuenta y el rol en cada operación.
    /// </summary>
    public partial class FormPrincipal : Form
    {
        private const int AnchoLateral = 276; // 236 cortaba «Puntos de instalación», «Dashboard institucional» y «Análisis en Power BI»
        private const int AnchoContraido = 76;

        private readonly ServiciosAplicacion _servicios;
        private readonly SesionActiva _sesion;
        private readonly PanelDesplazable _contenido = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo };
        private readonly Label _tituloModulo = new() { AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.Texto, Margin = new Padding(0, 2, 0, 0) };
        private readonly Label _fecha = new() { AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 2, 0, 0) };
        private readonly IconoSimple _iconoModulo = new(IconChar.House) { AccessibleName = "Icono del módulo" };
        private readonly PastillaEstado _estado = new() { Margin = new Padding(0, 0, 16, 0), Anchor = AnchorStyles.Right };
        private readonly ToolTip _ayudas = new();
        private EstabilizadorVentana? _estabilizador;
        private readonly List<BotonNavegacion> _navegacion = new List<BotonNavegacion>();
        private BotonNavegacion? _registroManual;
        private readonly bool _admin;
        private TableLayoutPanel? _raiz;
        private Label? _lema;
        private Image? _hojasInferior;
        private Label? _areaActual;
        private LogoEcoAccess? _logo;
        private BotonNavegacion? _contraer;
        private Form? _modulo;
        private BotonNavegacion? _inicio;
        private BotonNavegacion? _materiales, _canecas, _usuarios, _depositos, _estadisticas;

        private readonly AreaAcceso _area;

        public bool CerrarSesionSolicitado { get; private set; }

        /// <summary>Otra área de gestión que la persona pidió abrir (solo una que su cuenta ya tiene).</summary>
        public AreaAcceso? AreaSolicitada { get; private set; }

        public FormPrincipal(ServiciosAplicacion servicios, SesionActiva sesion, AreaAcceso area)
        {
            _servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            // Defensa en profundidad: aunque se construya directamente, el área debe ser un rol real de gestión de la cuenta.
            if (area == AreaAcceso.Ciudadano || !AreasAcceso.Elegir(sesion, area).Exitoso)
                throw new ArgumentException("La cuenta no tiene acceso a esa área de gestión.", nameof(area));
            _area = area;
            _admin = area == AreaAcceso.Supervisor;
            InitializeComponent();
            ConstruirEstructura();
            MostrarEstado("Oracle · sin comprobar", false);
            MostrarInicio();
            FormClosing += (_, e) => { if (!CerrarContenido()) e.Cancel = true; };
            Disposed += (_, _) => _ayudas.Dispose();
        }

        private void ConstruirEstructura()
        {
            SuspendLayout();
            Controls.Clear();
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            Font = TemaEcoAccess.Cuerpo;
            Text = "EcoAccess · Gestión ambiental";
            BackColor = TemaEcoAccess.Fondo;
            // Cabe en 1366×768 al 125 % (unos 1093×580 lógicos útiles); el contenido se desplaza si hace falta.
            MinimumSize = new Size(900, 540);
            ClientSize = new Size(1366, 768);
            WindowState = FormWindowState.Maximized;

            _raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, AnchoLateral)));
            _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _raiz.Controls.Add(ConstruirLateral(), 0, 0);
            _raiz.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Borde, Margin = Padding.Empty }, 1, 0);

            var centro = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
            centro.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centro.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
            centro.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            centro.Controls.Add(ConstruirCabecera(), 0, 0);
            centro.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Borde, Margin = Padding.Empty }, 0, 1);
            centro.Controls.Add(_contenido, 0, 2);
            _raiz.Controls.Add(centro, 2, 0);
            Controls.Add(_raiz);
            _contenido.SizeChanged += (_, _) => AjustarModulo();
            _estabilizador ??= new EstabilizadorVentana(this, _contenido);
            ResumeLayout(true);
        }

        private Control ConstruirLateral()
        {
            var lateral = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Superficie, ColumnCount = 1, RowCount = 3, Padding = new Padding(14, 18, 14, 14), Margin = Padding.Empty };
            lateral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // el menú sigue el ancho de la barra (también contraída)
            lateral.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            lateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            lateral.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Marca: logotipo oficial de EcoAccess (Eco, la mascota, vive en Telegram y en el saludo del dashboard).
            var marca = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 26), BackColor = Color.Transparent };
            int anchoLogo = TemaEcoAccess.Px(this, 196);
            _logo = new LogoEcoAccess { Size = new Size(anchoLogo, LogoEcoAccess.AltoPara(anchoLogo)), Margin = new Padding(4, 0, 0, 4) };
            _lema = new Label { Text = "Gestión ambiental · ¡Cada residuo cuenta!", AutoSize = true, MaximumSize = new Size(anchoLogo, 0), ForeColor = TemaEcoAccess.Suave, Font = TemaEcoAccess.Pequena, Margin = new Padding(6, 0, 0, 0) };
            marca.Controls.Add(_logo, 0, 0);
            marca.Controls.Add(_lema, 0, 1); lateral.Controls.Add(marca, 0, 0);

            // Fondo transparente: las hojas inferiores de la barra se ven detrás del menú, como en el diseño.
            var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.Transparent, Margin = Padding.Empty };
            menu.Scroll += (_, _) => menu.Invalidate(true);
            menu.MouseWheel += (_, _) => menu.Invalidate(true);
            lateral.Controls.Add(menu, 0, 1);
            // Cada área tiene su propio menú. Ocultar opciones es comodidad; la autorización real está en los servicios.
            var areaActual = new Label
            {
                Text = _admin ? "ÁREA SUPERVISOR" : "ÁREA OPERARIO", AutoSize = true, Font = TemaEcoAccess.Cuerpo,
                ForeColor = TemaEcoAccess.Verde, Margin = new Padding(6, 0, 0, 0), Anchor = AnchorStyles.Left, AccessibleName = $"Área actual: {AreasAcceso.NombreArea(_area)}",
            };
            // «Contraer menú» es un icono pequeño a la derecha del rótulo del área (el diseño no reserva una fila para él), así
            // el espacio inferior queda para las hojas.
            _contraer = new BotonNavegacion("Contraer menú", IconChar.AnglesLeft)
            {
                Contraido = true, Size = new Size(TemaEcoAccess.Px(this, 36), TemaEcoAccess.Px(this, 32)), Margin = Padding.Empty, Anchor = AnchorStyles.Right,
            };
            _contraer.Click += (_, _) => Contraer(_contraer.Text == "Contraer menú");
            _ayudas.SetToolTip(_contraer, "Contraer menú");
            var filaArea = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Height = TemaEcoAccess.Px(this, 34), BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 8) };
            filaArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filaArea.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filaArea.Controls.Add(areaActual, 0, 0);
            filaArea.Controls.Add(_contraer, 1, 0);
            _areaActual = areaActual;
            menu.Controls.Add(filaArea);
            _inicio = Agregar(menu, "Dashboard", IconChar.House, MostrarInicio);
            if (_admin)
            {
                _usuarios = Agregar(menu, "Usuarios y roles", IconChar.Users, () => BotonUsuarios_Click(this, EventArgs.Empty));
                _materiales = Agregar(menu, "Materiales", IconChar.Leaf, () => BotonMateriales_Click(this, EventArgs.Empty));
                _canecas = Agregar(menu, "Canecas y QR", IconChar.Qrcode, () => BotonCanecas_Click(this, EventArgs.Empty));
                Agregar(menu, "Puntos de instalación", IconChar.LocationDot, () => BotonPuntos_Click(this, EventArgs.Empty));
                Agregar(menu, "Tarifas y ecopuntos", IconChar.Coins, AbrirEcopuntos);
                _depositos = Agregar(menu, "Auditar depósitos", IconChar.ShieldHalved, AbrirDepositos);
                _estadisticas = Agregar(menu, "Estadísticas", IconChar.ChartColumn, () => BotonEstadisticas_Click(this, EventArgs.Empty));
                if (_servicios.Opiniones is { } opiniones)
                    Agregar(menu, "Valoraciones", IconChar.Star, () => AbrirModulo("Tendencias de valoraciones",
                        () => new FormOpiniones(opiniones, _sesion.IdUsuario, administracion: true), _navegacion.FirstOrDefault(b => b.Text == "Valoraciones")));
                if (_servicios.InformeInstitucional is not null) Agregar(menu, "Dashboard institucional", IconChar.TableCellsLarge, AbrirInstitucional);
                Agregar(menu, "Análisis en Power BI", IconChar.ChartLine, AbrirPowerBi);
                Agregar(menu, "Sistema y Telegram", IconChar.PaperPlane, AbrirDiagnostico);
            }
            else
            {
                Agregar(menu, "Consultar", IconChar.MagnifyingGlass, AbrirConsultaOperario);
                _depositos = Agregar(menu, "Depósitos", IconChar.BoxOpen, AbrirDepositos);
                _registroManual = Agregar(menu, "Registro manual", IconChar.PenToSquare, AbrirRegistroManual);
                Agregar(menu, "Ecopuntos", IconChar.Coins, AbrirEcopuntos);
                _estadisticas = Agregar(menu, "Estadísticas", IconChar.ChartColumn, () => BotonEstadisticas_Click(this, EventArgs.Empty));
                if (_servicios.InformeInstitucional is not null) Agregar(menu, "Dashboard institucional", IconChar.TableCellsLarge, AbrirInstitucional);
                Agregar(menu, "Análisis en Power BI", IconChar.ChartLine, AbrirPowerBi);
            }
            // Sin desplazamiento horizontal: los elementos se ajustan al ancho útil, descontando la barra vertical si aparece.
            // ClientSizeChanged también se dispara cuando aparece la barra vertical (SizeChanged no).
            menu.AutoScroll = false;
            menu.HorizontalScroll.Maximum = 0;
            menu.HorizontalScroll.Enabled = false;
            menu.HorizontalScroll.Visible = false;
            menu.AutoScroll = true;
            menu.ClientSizeChanged += (_, _) =>
            {
                int ancho = menu.ClientSize.Width - 4;
                foreach (Control c in menu.Controls) if (c.Width != ancho) c.Width = Math.Max(1, ancho);
            };

            // Hojas decorativas del diseño (assets/imagenes/eco/hojas_superior.png y hojas_inferior.png). Si no están, no ocupan lugar.
            // Se pintan sobre la propia barra, al ras de los bordes: arriba a la derecha detrás del logotipo, y abajo a la
            // izquierda, a todo el ancho, solo en el espacio libre debajo de la última opción: nunca detrás del texto del menú.
            // Si falta alto se aplana un poco (como en el diseño, ~45 % del ancho) y, si ni así cabe, no se dibuja.
            _hojasInferior = RecursosEco.Imagen("eco/hojas_inferior.png"); // eco-estetica: permitido hojas del diseño aprobado del Dashboard
            Image? hojasSuperior = RecursosEco.Imagen("eco/hojas_superior.png"); // eco-estetica: permitido hojas del diseño aprobado del Dashboard
            lateral.SizeChanged += (_, _) => lateral.Invalidate();
            menu.Layout += (_, _) => lateral.Invalidate();
            lateral.Paint += (_, e) =>
            {
                if (_logo is { Visible: false }) return; // contraída: sin decoración
                int ancho = lateral.ClientSize.Width;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                if (hojasSuperior is not null)
                {
                    int lado = ancho * 27 / 100, alto = hojasSuperior.Height * lado / hojasSuperior.Width;
                    e.Graphics.DrawImage(CacheEscalado.Obtener(hojasSuperior, lado, Math.Max(1, alto)), ancho - lado, 0);
                }
                if (_hojasInferior is not null && !menu.VerticalScroll.Visible)
                {
                    int finMenu = menu.Top + menu.Controls.Cast<Control>().Where(c => c.Visible).Select(c => c.Bottom + c.Margin.Bottom).DefaultIfEmpty(0).Max();
                    int libre = lateral.ClientSize.Height - finMenu - TemaEcoAccess.Px(this, 4);
                    int alto = Math.Min(_hojasInferior.Height * ancho / _hojasInferior.Width, libre);
                    if (alto >= ancho * 40 / 100)
                        e.Graphics.DrawImage(CacheEscalado.Obtener(_hojasInferior, ancho, alto), 0, lateral.ClientSize.Height - alto);
                }
            };
            return lateral;
        }

        private BotonNavegacion Agregar(FlowLayoutPanel menu, string texto, IconChar icono, Action accion)
        {
            var boton = new BotonNavegacion(texto, icono) { Width = TemaEcoAccess.Px(this, AnchoLateral - 30), TabIndex = _navegacion.Count };
            boton.Click += (_, _) => accion();
            _ayudas.SetToolTip(boton, texto);
            menu.Controls.Add(boton);
            _navegacion.Add(boton);
            return boton;
        }

        /// <summary>Contraída, la barra muestra solo iconos (con su nombre en la ayuda emergente) y deja más espacio al contenido.</summary>
        private void Contraer(bool contraer)
        {
            if (_raiz is null || _contraer is null) return;
            _raiz.ColumnStyles[0].Width = TemaEcoAccess.Px(this, contraer ? AnchoContraido : AnchoLateral);
            foreach (BotonNavegacion boton in _navegacion) boton.Contraido = contraer;
            _contraer.Text = _contraer.AccessibleName = contraer ? "Expandir menú" : "Contraer menú";
            _contraer.Icono = contraer ? IconChar.AnglesRight : IconChar.AnglesLeft;
            _ayudas.SetToolTip(_contraer, _contraer.Text);
            _logo!.Visible = _lema!.Visible = !contraer;
            if (_areaActual is not null) _areaActual.Visible = !contraer;
            _raiz.Controls[0].Invalidate(true);
        }

        /// <summary>
        /// Cabecera del diseño aprobado: icono y título del módulo con «Área · fecha» debajo; a la derecha el estado de Oracle,
        /// un separador, la persona en sesión (avatar, nombre y área) con su menú (perfil y cambio de área) y el botón de salir.
        /// </summary>
        private Control ConstruirCabecera()
        {
            var cabecera = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Superficie, Padding = new Padding(24, 12, 20, 12), ColumnCount = 6, RowCount = 1, Margin = Padding.Empty };
            cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++) cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _iconoModulo.Size = new Size(TemaEcoAccess.Px(this, 30), TemaEcoAccess.Px(this, 30));
            _iconoModulo.Margin = new Padding(0, 0, 14, 0);
            _iconoModulo.Anchor = AnchorStyles.Left;
            cabecera.Controls.Add(_iconoModulo, 0, 0);
            var titulos = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty, Anchor = AnchorStyles.Left };
            string fecha = HoraColombia.DesdeUtc(DateTime.UtcNow).ToString("dddd d 'de' MMMM 'de' yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-CO"));
            _fecha.Text = $"Área {AreasAcceso.NombreArea(_area)} · {fecha}";
            _fecha.Margin = new Padding(0, 1, 0, 0);
            titulos.Controls.Add(_tituloModulo);
            titulos.Controls.Add(_fecha);
            cabecera.Controls.Add(titulos, 1, 0);
            cabecera.Controls.Add(_estado, 2, 0);
            cabecera.Controls.Add(new Panel { BackColor = TemaEcoAccess.Borde, Size = new Size(1, TemaEcoAccess.Px(this, 40)), Margin = new Padding(4, 0, 22, 0), Anchor = AnchorStyles.None }, 3, 0);

            // Persona en sesión: avatar, nombre y área; al pulsarla (o la flecha) se abre su menú.
            var persona = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, RowCount = 2, Margin = Padding.Empty, Anchor = AnchorStyles.Right, Cursor = Cursors.Hand };
            var avatar = new AvatarIniciales(_sesion.Nombre) { Size = new Size(TemaEcoAccess.Px(this, 40), TemaEcoAccess.Px(this, 40)), Margin = new Padding(0, 0, 10, 0) };
            etiquetaSesion.Text = _sesion.Nombre;
            etiquetaSesion.AutoSize = true;
            etiquetaSesion.Font = TemaEcoAccess.Subtitulo;
            etiquetaSesion.ForeColor = TemaEcoAccess.Texto;
            etiquetaSesion.Margin = Padding.Empty;
            etiquetaSesion.MaximumSize = new Size(TemaEcoAccess.Px(this, 220), 0);
            var rol = new Label
            {
                Text = $"Área {AreasAcceso.NombreArea(_area)}", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = Padding.Empty,
                AccessibleDescription = $"Roles de la cuenta: {string.Join(", ", _sesion.Roles.Select(AreasAcceso.NombreRol))}",
            };
            _ayudas.SetToolTip(rol, rol.AccessibleDescription);
            var menuPersona = new ContextMenuStrip { ShowImageMargin = true, Font = TemaEcoAccess.Cuerpo, BackColor = TemaEcoAccess.Superficie, ForeColor = TemaEcoAccess.Texto };
            menuPersona.Items.Add(new ToolStripMenuItem("Perfil y contraseña", CacheIconos.Obtener(IconChar.UserGear, TemaEcoAccess.Px(this, 16), TemaEcoAccess.Verde),
                (_, _) => BotonCambiarContrasena_Click(this, EventArgs.Empty)));
            // Cambiar de área solo aparece si la cuenta tiene AMBOS roles de gestión: nunca abre un área no concedida.
            AreaAcceso? otra = AreasAcceso.DeGestion(_sesion).Where(a => a != _area).Select(a => (AreaAcceso?)a).FirstOrDefault();
            if (otra is { } destino)
                menuPersona.Items.Add(new ToolStripMenuItem($"Cambiar al área {AreasAcceso.NombreArea(destino)}", CacheIconos.Obtener(IconChar.ArrowsRotate, TemaEcoAccess.Px(this, 16), TemaEcoAccess.Verde),
                    (_, _) => CambiarArea(destino)));
            menuPersona.Items.Add(new ToolStripSeparator());
            menuPersona.Items.Add(new ToolStripMenuItem("Cerrar sesión", CacheIconos.Obtener(IconChar.RightFromBracket, TemaEcoAccess.Px(this, 16), TemaEcoAccess.Verde),
                (_, _) => BotonCerrarSesion_Click(this, EventArgs.Empty)));
            Disposed += (_, _) => menuPersona.Dispose();
            var desplegar = new BotonNavegacion("Opciones de la cuenta", IconChar.ChevronDown) { Contraido = true, Size = new Size(TemaEcoAccess.Px(this, 30), TemaEcoAccess.Px(this, 36)), Margin = new Padding(8, 0, 0, 0) };
            _ayudas.SetToolTip(desplegar, "Opciones de la cuenta: perfil y contraseña" + (otra is null ? "" : " y cambio de área"));
            void Abrir() => menuPersona.Show(desplegar, new Point(desplegar.Width - menuPersona.PreferredSize.Width, desplegar.Height));
            desplegar.Click += (_, _) => Abrir();
            foreach (Control c in new Control[] { avatar, etiquetaSesion, rol }) { c.Cursor = Cursors.Hand; c.Click += (_, _) => Abrir(); }
            persona.Controls.Add(avatar, 0, 0);
            persona.SetRowSpan(avatar, 2);
            persona.Controls.Add(etiquetaSesion, 1, 0);
            persona.Controls.Add(rol, 1, 1);
            persona.Controls.Add(desplegar, 2, 0);
            persona.SetRowSpan(desplegar, 2);
            cabecera.Controls.Add(persona, 4, 0);
            var salir = BotonIcono("Cerrar sesión", IconChar.RightFromBracket, () => BotonCerrarSesion_Click(this, EventArgs.Empty));
            salir.Margin = new Padding(20, 0, 0, 0);
            salir.Anchor = AnchorStyles.Right;
            cabecera.Controls.Add(salir, 5, 0);

            // La cabecera mide su ancho por su contenido: si no cabe, ensancharía toda la columna y cortaría el módulo por la
            // derecha. En anchos menores se oculta el nombre (queda el avatar con su menú) y la fecha; salir siempre se ve.
            void Compactar()
            {
                int disponible = cabecera.Parent?.ClientSize.Width ?? cabecera.Width;
                if (disponible <= 0) return;
                bool sinNombre = disponible < TemaEcoAccess.Px(this, 1100);
                etiquetaSesion.Visible = rol.Visible = !sinNombre;
                _fecha.Text = disponible < TemaEcoAccess.Px(this, 900) ? $"Área {AreasAcceso.NombreArea(_area)}" : $"Área {AreasAcceso.NombreArea(_area)} · {fecha}";
            }
            Control? padre = null;
            cabecera.ParentChanged += (_, _) =>
            {
                if (padre is not null) padre.SizeChanged -= AlCambiar;
                padre = cabecera.Parent;
                if (padre is not null) padre.SizeChanged += AlCambiar;
                Compactar();
            };
            void AlCambiar(object? s, EventArgs e) => Compactar();
            return cabecera;
        }

        private Control BotonIcono(string texto, IconChar icono, Action accion)
        {
            var boton = new BotonNavegacion(texto, icono) { Contraido = true, Size = new Size(TemaEcoAccess.Px(this, 40), TemaEcoAccess.Px(this, 40)), Margin = new Padding(12, 0, 0, 0) };
            boton.Click += (_, _) => accion();
            _ayudas.SetToolTip(boton, texto);
            return boton;
        }

        private bool CerrarContenido()
        {
            if (_modulo is { IsDisposed: false })
            {
                _modulo.Close();
                if (!_modulo.IsDisposed)
                {
                    MostrarEstado("Espere a que termine la operación o cancele la consulta antes de cambiar de módulo", true);
                    return false;
                }
            }
            _modulo = null;
            foreach (Control control in _contenido.Controls.Cast<Control>().ToArray()) control.Dispose();
            _contenido.Controls.Clear();
            _contenido.AutoScrollMinSize = Size.Empty;
            _contenido.AutoScrollPosition = Point.Empty;
            return true;
        }

        private void MostrarInicio()
        {
            if (!CerrarContenido()) return;
            _tituloModulo.Text = "Dashboard";
            Seleccionar(_inicio);
            var acciones = new List<AccionRapida>();
            if (_admin)
            {
                acciones.Add(new("Crear cuenta o asignar rol", IconChar.UserPlus, () => BotonUsuarios_Click(this, EventArgs.Empty)));
                acciones.Add(new("Gestionar materiales", IconChar.Leaf, () => BotonMateriales_Click(this, EventArgs.Empty)));
                acciones.Add(new("Canecas y QR", IconChar.Qrcode, () => BotonCanecas_Click(this, EventArgs.Empty)));
                acciones.Add(new("Tarifas de ecopuntos", IconChar.Coins, AbrirEcopuntos));
                acciones.Add(new("Auditar depósitos", IconChar.ShieldHalved, AbrirDepositos));
            }
            else
            {
                acciones.Add(new("Consultar ciudadanos y canecas", IconChar.MagnifyingGlass, AbrirConsultaOperario));
                acciones.Add(new("Verificar depósitos", IconChar.ClipboardCheck, AbrirDepositos));
                acciones.Add(new("Registro manual", IconChar.PenToSquare, AbrirRegistroManual));
            }
            acciones.Add(new("Estadísticas", IconChar.ChartColumn, () => BotonEstadisticas_Click(this, EventArgs.Empty)));
            if (_servicios.InformeInstitucional is not null) acciones.Add(new("Dashboard institucional", IconChar.BuildingColumns, AbrirInstitucional));
            acciones.Add(new("Análisis en Power BI", IconChar.ChartSimple, AbrirPowerBi));
            var resumen = new PanelResumenAdministrativo(_servicios.PanelAdministrativo, _servicios.Estadisticas, _sesion.IdUsuario, _sesion.Nombre, MostrarEstado, acciones, AbrirDepositos);
            EstabilizadorVentana.PermitirEncoger(resumen);
            _contenido.Controls.Add(resumen);
        }

        private void AbrirInstitucional() => AbrirModulo("Dashboard institucional",
            () => new FormDashboardInstitucional(_servicios.InformeInstitucional!, _sesion.IdUsuario, _servicios.Contenedores, _servicios.Puntos, _servicios.Materiales, AbrirPowerBi),
            _navegacion.FirstOrDefault(b => b.Text == "Dashboard institucional"));

        /// <summary>Abre el informe local si existe. Si falta algo, lo explica: el dashboard interno no depende de Power BI.</summary>
        private void AbrirPowerBi()
        {
            string? raiz = AccesoPowerBi.BuscarRaizProyecto(Application.StartupPath);
            var estado = AccesoPowerBi.Consultar(raiz,
                AccesoPowerBi.ProgramasConocidos(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                System.IO.File.Exists);
            if (estado.SePuedeAbrir)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(estado.Programa!, $"\"{estado.Informe}\"") { UseShellExecute = false }); }
                catch (Exception) { MessageBox.Show(this, "No se pudo abrir Power BI Desktop. El dashboard de EcoAccess sigue disponible.", "Análisis en Power BI", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                return;
            }
            MessageBox.Show(this, AccesoPowerBi.Explicar(estado), "Análisis en Power BI", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AbrirConsultaOperario() => AbrirModulo("Consultar",
            () => new FormConsultaOperario(_servicios.Usuarios, _servicios.Contenedores, _servicios.Materiales, _sesion.IdUsuario),
            _navegacion.FirstOrDefault(b => b.Text == "Consultar"));

        private void CambiarArea(AreaAcceso destino)
        {
            if (!AreasAcceso.Elegir(_sesion, destino).Exitoso || !CerrarContenido()) return;
            AreaSolicitada = destino;
            Close();
        }

        private void AbrirRegistroManual() => AbrirModulo("Registro manual", () => new FormRegistroManual(_servicios, _sesion.IdUsuario), _registroManual);
        private void AbrirDepositos() => AbrirModulo("Depósitos e interacciones", () => new FormInteraccionesDepositos(_servicios.Interacciones, _servicios.Estadisticas, _servicios.Contenedores, _servicios.Materiales, _sesion.IdUsuario), _depositos);
        private void AbrirEcopuntos() => AbrirModulo(_admin ? "Tarifas y ecopuntos" : "Ecopuntos", () => new FormEcopuntos(_servicios.Ecopuntos, _servicios.Materiales, _sesion.IdUsuario, _admin),
            _navegacion.FirstOrDefault(b => b.Text is "Ecopuntos" or "Tarifas y ecopuntos"));
        private void AbrirDiagnostico() => AbrirModulo("Sistema, Oracle y Telegram", () => new FormDiagnosticoOracle(_servicios.DiagnosticoOracle, _sesion.IdUsuario), _navegacion.FirstOrDefault(b => b.Text == "Sistema y Telegram"));

        private void AbrirModulo(string titulo, Func<Form> crear, BotonNavegacion? origen)
        {
            if (!CerrarContenido()) return;
            _modulo = crear();
            TemaEcoAccess.Aplicar(_modulo);
            EstabilizadorVentana.PermitirEncoger(_modulo);
            _modulo.TopLevel = false;
            _modulo.FormBorderStyle = FormBorderStyle.None;
            _modulo.Dock = DockStyle.Fill;
            _modulo.Location = Point.Empty;
            _modulo.Padding = new Padding(Math.Max(_modulo.Padding.Left, 12));
            _tituloModulo.Text = titulo;
            Seleccionar(origen);
            _contenido.Controls.Add(_modulo);
            // Los formularios del Designer (posiciones fijas) se reacomodan con la fuente real ya heredada del menú.
            if (_modulo.AutoScaleMode == AutoScaleMode.Font)
            {
                DisenoFormularios.AjustarDisenoFijo(_modulo);
                // Más angosto que esto, un control de posición fija quedaba cortado: la ventana desplaza el módulo en horizontal.
                int ancho = DisenoFormularios.AnchoNecesario(_modulo);
                if (ancho > _modulo.MinimumSize.Width) _modulo.MinimumSize = new Size(ancho, _modulo.MinimumSize.Height);
            }
            AjustarModulo();
            _modulo.Show();
            AjustarModulo();
        }

        private void AjustarModulo()
        {
            if (_modulo is null || _modulo.IsDisposed) return;
            // Dock ocupa el área disponible; el área virtual solo crece por el mínimo del formulario.
            _contenido.AutoScrollMinSize = _modulo.MinimumSize;
        }

        private void Seleccionar(BotonNavegacion? actual)
        {
            foreach (BotonNavegacion boton in _navegacion) boton.Activo = boton == actual;
            // El dashboard usa en la cabecera el icono de panel (como el diseño); los módulos, el de su botón del menú.
            _iconoModulo.Icono = actual is null || actual == _inicio ? IconChar.TableCellsLarge : actual.Icono;
        }

        private void MostrarEstado(string mensaje, bool error)
        {
            if (IsDisposed) return;
            // La pastilla resume; el texto completo queda en la ayuda emergente y para lectores de pantalla.
            string corto = error ? "Revisar conexión" : mensaje.StartsWith("Oracle consultado", StringComparison.Ordinal) ? "Oracle conectado" : mensaje.Length > 34 ? mensaje[..34] + "…" : mensaje;
            _estado.Mostrar(corto, error ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro, error ? TemaEcoAccess.ErrorSuave : TemaEcoAccess.VerdeSuave);
            _estado.AccessibleDescription = mensaje;
            _ayudas.SetToolTip(_estado, mensaje);
        }

        private void BotonMateriales_Click(object? sender, EventArgs e) => AbrirModulo("Catálogo de materiales", () => new FormMateriales(_servicios.Materiales, _sesion.IdUsuario), _materiales);
        private void BotonPuntos_Click(object? sender, EventArgs e) => AbrirModulo("Puntos de instalación", () => new FormPuntos(_servicios.Puntos, _sesion.IdUsuario), _navegacion.FirstOrDefault(b => b.Text == "Puntos de instalación"));
        private void BotonCanecas_Click(object? sender, EventArgs e) => AbrirModulo("Canecas y códigos QR", () => new FormContenedores(_servicios.Contenedores, _servicios.Puntos, _servicios.Materiales, _sesion.IdUsuario, _servicios.NombreBotTelegram), _canecas);
        private void BotonUsuarios_Click(object? sender, EventArgs e) => AbrirModulo("Ciudadanos, usuarios y roles", () => new FormUsuarios(_servicios.Usuarios, _servicios.Autenticacion, _sesion.IdUsuario), _usuarios);
        private void BotonEstadisticas_Click(object? sender, EventArgs e) => AbrirModulo("Estadísticas y reportes", () => new FormEstadisticas(_servicios.Estadisticas, _servicios.Contenedores, _servicios.Materiales, _sesion.IdUsuario), _estadisticas);

        private void BotonCambiarContrasena_Click(object? sender, EventArgs e)
        {
            using var formulario = new FormCambioContrasena(_servicios.Autenticacion, _sesion);
            TemaEcoAccess.Aplicar(formulario);
            formulario.ShowDialog(this);
        }

        private void BotonCerrarSesion_Click(object? sender, EventArgs e)
        {
            if (!CerrarContenido()) return;
            CerrarSesionSolicitado = true;
            Close();
        }
    }

    /// <summary>Atajo del dashboard hacia un módulo existente (no duplica su funcionalidad).</summary>
    internal sealed record AccionRapida(string Texto, IconChar Icono, Action Ejecutar);
}
