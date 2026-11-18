using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Panel del ciudadano del diseño «EcoAccess Moderno» (predeterminado), según la segunda imagen de referencia. Todos los
    /// números salen de <see cref="ServicioPanelCiudadano"/> (Oracle); no hay valores de ejemplo. Respecto a la imagen:
    /// las canecas son las tres reales (blanca, verde y negra) y sin porcentaje de llenado, porque no hay sensor de nivel;
    /// y no hay bloque «Base de datos», porque el ciudadano no debe ver información interna. Las acciones abren las mismas
    /// ventanas que el diseño Clásico (asistente de depósito, ecopuntos, actividad, QR oficial).
    /// </summary>
    public sealed class FormPanelCiudadano : Form, IPanelConApariencia
    {
        private readonly ServiciosAplicacion _servicios;
        private readonly SesionActiva? _sesion;
        private readonly TableLayoutPanel _tarjetas = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        private readonly GraficaSemana _grafica = new() { Dock = DockStyle.Fill };
        private readonly ListView _historial = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HeaderStyle = ColumnHeaderStyle.Nonclickable, BorderStyle = BorderStyle.None, MultiSelect = false, Font = TemaEcoAccess.Pequena, HideSelection = true };
        private readonly TableLayoutPanel _canecas = new() { Dock = DockStyle.Fill, ColumnCount = 1 };
        private readonly Label _telegram = new() { Dock = DockStyle.Fill, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Texto, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label _aviso = new() { Dock = DockStyle.Fill, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, TextAlign = ContentAlignment.MiddleRight };
        private IReadOnlyList<Material> _catalogo = Array.Empty<Material>();

        public bool CambioAparienciaSolicitado { get; private set; }
        /// <summary>Cerrar sesión (cuenta) o «Iniciar sesión» (invitado): el programa vuelve a la ventana de acceso.</summary>
        public bool SolicitarInicioSesion { get; private set; }

        public FormPanelCiudadano(ServiciosAplicacion servicios, SesionActiva? sesion)
        {
            _servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _sesion = sesion;
            Text = "EcoAccess - Sistema de Gestión de Residuos";
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1360, 760);
            MinimumSize = new Size(1100, 680);
            WindowState = FormWindowState.Maximized;

            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = TemaEcoAccess.Fondo };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 232)));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.Controls.Add(MenuLateral(), 0, 0);
            raiz.Controls.Add(Contenido(), 1, 0);
            Controls.Add(raiz);
            _historial.SizeChanged += (_, _) => AjustarColumnasHistorial();
            Shown += async (_, _) => await CargarAsync();
        }

        // ── Menú lateral ───────────────────────────────────────────────────────────────────────────────────────

        private Control MenuLateral()
        {
            var menu = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty, Padding = new Padding(TemaEcoAccess.Px(this, 12)) };
            void Agregar(Control c, SizeType tipo = SizeType.AutoSize, float alto = 0) { menu.RowStyles.Add(new RowStyle(tipo, alto)); c.Dock = DockStyle.Fill; menu.Controls.Add(c, 0, menu.RowStyles.Count - 1); }

            Agregar(new PictureBox { Image = RecursosEco.Logo, SizeMode = PictureBoxSizeMode.Zoom, Height = TemaEcoAccess.Px(this, 70), Margin = new Padding(4, 4, 4, 16) }, SizeType.Absolute, TemaEcoAccess.Px(this, 86));
            Agregar(Navegacion("Inicio", IconChar.House, () => { }, activo: true));
            Agregar(Navegacion("Registrar depósito", IconChar.CirclePlus, AbrirAsistente));
            Agregar(Navegacion("Clasificar residuo", IconChar.Recycle, () => Clasificar(null)));
            Agregar(Navegacion("Contenedores", IconChar.TrashCan, MostrarCanecas));
            if (_sesion is not null)
            {
                Agregar(Navegacion("Historial", IconChar.ClockRotateLeft, MostrarActividad));
                Agregar(Navegacion("Ecopuntos", IconChar.Trophy, MostrarEcopuntos));
            }
            Agregar(Navegacion("Telegram", IconChar.PaperPlane, MostrarQr));
            Agregar(Navegacion("Configuración", IconChar.Gear, AbrirApariencia));
            Agregar(Navegacion(_sesion is null ? "Iniciar sesión" : "Cerrar sesión", _sesion is null ? IconChar.RightToBracket : IconChar.RightFromBracket,
                () => { SolicitarInicioSesion = true; Close(); }));
            Agregar(new PictureBox { Image = RecursosEco.Eco, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(8, 16, 8, 0) }, SizeType.Percent, 100);
            Agregar(new Label { Text = "Conectamos tecnología y sostenibilidad.", Font = TemaEcoAccess.Pequena, ForeColor = Color.White, BackColor = TemaEcoAccess.VerdeOscuro, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(8), Margin = Padding.Empty }, SizeType.Absolute, TemaEcoAccess.Px(this, 76));
            return menu;
        }

        private IconButton Navegacion(string texto, IconChar icono, Action accion, bool activo = false)
        {
            var boton = new IconButton
            {
                Text = "  " + texto, IconChar = icono, IconSize = TemaEcoAccess.Px(this, 20), TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, Height = TemaEcoAccess.Px(this, 40),
                Font = new Font("Segoe UI", 10.5f), AutoEllipsis = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 1, 0, 1), Padding = new Padding(8, 0, 0, 0),
                BackColor = activo ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Superficie, ForeColor = activo ? Color.White : TemaEcoAccess.Texto,
                IconColor = activo ? Color.White : TemaEcoAccess.VerdeOscuro,
            };
            boton.FlatAppearance.BorderSize = 0;
            boton.Click += (_, _) => BeginInvoke(accion);
            return boton;
        }

        // ── Contenido ──────────────────────────────────────────────────────────────────────────────────────────

        private Control Contenido()
        {
            var contenido = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(TemaEcoAccess.Px(this, 20), TemaEcoAccess.Px(this, 14), TemaEcoAccess.Px(this, 20), TemaEcoAccess.Px(this, 10)) };
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 74)));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 104)));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 64)));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            contenido.Controls.Add(Saludo(), 0, 0);
            contenido.Controls.Add(_tarjetas, 0, 1);
            contenido.Controls.Add(AccionesRapidas(), 0, 2);

            var fila1 = Columnas(3);
            fila1.Controls.Add(Tarjeta("Depósitos de la semana", IconChar.ChartColumn, _grafica), 0, 0);
            fila1.Controls.Add(Tarjeta("Historial reciente", IconChar.ClockRotateLeft, _historial), 1, 0);
            fila1.Controls.Add(Tarjeta("Estado de las canecas", IconChar.TrashCan, _canecas), 2, 0);
            contenido.Controls.Add(fila1, 0, 3);

            var fila2 = Columnas(3);
            fila2.Controls.Add(Tarjeta("Clasificación de residuos", IconChar.Recycle, Clasificacion()), 0, 0);
            fila2.Controls.Add(Tarjeta("Vinculación con Telegram", IconChar.PaperPlane, Telegram()), 1, 0);
            fila2.Controls.Add(Tarjeta("Cómo depositar", IconChar.CircleInfo, ComoDepositar()), 2, 0);
            contenido.Controls.Add(fila2, 0, 4);
            return contenido;
        }

        private Control Saludo()
        {
            var saludo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
            saludo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 56)));
            saludo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            saludo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0));
            saludo.ColumnStyles[2] = new ColumnStyle(SizeType.AutoSize);
            saludo.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            saludo.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            var icono = new IconPictureBox { IconChar = IconChar.CircleUser, IconColor = TemaEcoAccess.VerdeOscuro, IconSize = TemaEcoAccess.Px(this, 48), BackColor = TemaEcoAccess.Fondo, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage };
            saludo.Controls.Add(icono, 0, 0);
            saludo.SetRowSpan(icono, 2);
            string nombre = _sesion is null ? "invitado" : PrimerNombre(_sesion.Nombre);
            saludo.Controls.Add(new Label { Text = "¡Bienvenido, " + nombre + "!", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 20), ForeColor = TemaEcoAccess.Texto, TextAlign = ContentAlignment.BottomLeft, UseMnemonic = false }, 1, 0);
            saludo.Controls.Add(new Label { Text = _sesion is null ? "Consulta dónde va cada residuo. Crea una cuenta para llevar tu historial y tus ecopuntos." : "Gestiona tus depósitos, ecopuntos y consulta la actividad de EcoAccess.", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11), ForeColor = TemaEcoAccess.Suave, TextAlign = ContentAlignment.TopLeft }, 1, 1);
            var rol = new Label { Text = _sesion is null ? "Invitado" : "Ciudadano", AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.VerdeOscuro, BackColor = TemaEcoAccess.VerdeSuave, Padding = new Padding(14, 6, 14, 6), Anchor = AnchorStyles.Right };
            saludo.Controls.Add(rol, 2, 0);
            saludo.Controls.Add(_aviso, 2, 1);
            return saludo;
        }

        private Control AccionesRapidas()
        {
            var fila = Columnas(_sesion is null ? 3 : 5);
            int i = 0;
            void Agregar(string texto, IconChar icono, Action accion, bool principal = false)
            {
                var boton = new IconButton
                {
                    Text = " " + texto, IconChar = icono, IconSize = TemaEcoAccess.Px(this, 20), TextImageRelation = TextImageRelation.ImageBeforeText,
                    ImageAlign = ContentAlignment.MiddleRight, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 10.5f), Cursor = Cursors.Hand, Margin = new Padding(0, 6, TemaEcoAccess.Px(this, 10), 6), AutoEllipsis = true,
                    BackColor = principal ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Superficie, ForeColor = principal ? Color.White : TemaEcoAccess.Texto,
                    IconColor = principal ? Color.White : TemaEcoAccess.VerdeOscuro,
                };
                boton.FlatAppearance.BorderColor = principal ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Borde;
                boton.Click += (_, _) => BeginInvoke(accion);
                fila.Controls.Add(boton, i++, 0);
            }
            Agregar("Clasificar residuo", IconChar.Recycle, () => Clasificar(null), principal: true);
            Agregar("Registrar depósito", IconChar.CirclePlus, AbrirAsistente);
            if (_sesion is not null)
            {
                Agregar("Historial", IconChar.ChartColumn, MostrarActividad);
                Agregar("Ecopuntos", IconChar.Trophy, MostrarEcopuntos);
            }
            Agregar("Contenedores", IconChar.TrashCan, MostrarCanecas);
            return fila;
        }

        private Control Clasificacion()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 3 };
            for (int i = 0; i < 5; i++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 44)));
            var ayuda = new Label { Text = "Toca un tipo para saber dónde va.", AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 4) };
            panel.Controls.Add(ayuda, 0, 0);
            panel.SetColumnSpan(ayuda, 5);
            (string Texto, IconChar Icono, GrupoEstacion Grupo)[] grupos =
            {
                ("Plástico", IconChar.BottleWater, GrupoEstacion.Plastico), ("Papel", IconChar.Newspaper, GrupoEstacion.PapelCarton),
                ("Vidrio", IconChar.WineBottle, GrupoEstacion.VidrioMetal), ("Metal", IconChar.Gear, GrupoEstacion.VidrioMetal),
                ("Orgánico", IconChar.AppleWhole, GrupoEstacion.Comida),
            };
            for (int i = 0; i < grupos.Length; i++)
            {
                var g = grupos[i];
                var boton = new IconButton
                {
                    Text = g.Texto, IconChar = g.Icono, IconSize = TemaEcoAccess.Px(this, 20), IconColor = TemaEcoAccess.VerdeOscuro, TextImageRelation = TextImageRelation.ImageAboveText,
                    ImageAlign = ContentAlignment.BottomCenter, TextAlign = ContentAlignment.TopCenter, Padding = Padding.Empty,
                    Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = TemaEcoAccess.Superficie, ForeColor = TemaEcoAccess.Texto, Font = new Font("Segoe UI", 8f),
                    Margin = new Padding(0, 0, 4, 4), Cursor = Cursors.Hand, AccessibleName = "Clasificar " + g.Texto,
                };
                boton.FlatAppearance.BorderColor = TemaEcoAccess.Borde;
                boton.Click += (_, _) => BeginInvoke(new Action(() => Clasificar(g.Grupo)));
                panel.Controls.Add(boton, i, 1);
            }
            var todo = new IconButton { Text = "Clasificar residuo  →", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = TemaEcoAccess.VerdeOscuro, ForeColor = Color.White, Font = TemaEcoAccess.Subtitulo, Cursor = Cursors.Hand, Margin = new Padding(0, 2, 6, 0) };
            todo.FlatAppearance.BorderSize = 0;
            todo.Click += (_, _) => BeginInvoke(new Action(() => Clasificar(null)));
            panel.Controls.Add(todo, 0, 2);
            panel.SetColumnSpan(todo, 5);
            return panel;
        }

        private Control Telegram()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 44)));
            panel.RowStyles[0] = new RowStyle(SizeType.Percent, 40);
            panel.RowStyles[1] = new RowStyle(SizeType.Percent, 60);
            panel.Controls.Add(new Label { Text = "Escanea el QR y envía la foto a Eco.", Dock = DockStyle.Fill, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave }, 0, 0);
            _telegram.Text = _sesion is null ? "Como invitado puedes usar el bot; para acumular ecopuntos, vincula una cuenta." : "Consultando tu vinculación…";
            panel.Controls.Add(_telegram, 0, 1);
            var qr = new IconButton { Text = "  Ver QR oficial", IconChar = IconChar.Qrcode, IconSize = TemaEcoAccess.Px(this, 22), IconColor = TemaEcoAccess.VerdeOscuro, TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = TemaEcoAccess.Superficie, ForeColor = TemaEcoAccess.VerdeOscuro, Font = TemaEcoAccess.Subtitulo, Cursor = Cursors.Hand, Margin = new Padding(0, 2, 0, 0) };
            qr.FlatAppearance.BorderColor = TemaEcoAccess.Verde;
            qr.Click += (_, _) => BeginInvoke(new Action(MostrarQr));
            panel.Controls.Add(qr, 0, 2);
            return panel;
        }

        private Control ComoDepositar()
        {
            var lista = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
            string[] pasos =
            {
                "1. Escanea el QR de la estación.",
                "2. Envía a Eco la foto de un residuo.",
                "3. Se abre la caneca correcta 10 s.",
                "4. Ecopuntos al verificar el depósito.",
            };
            foreach (string paso in pasos)
            {
                lista.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
                lista.Controls.Add(new Label { Text = paso, Dock = DockStyle.Fill, ForeColor = TemaEcoAccess.Texto, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true });
            }
            return lista;
        }

        private TableLayoutPanel Columnas(int n)
        {
            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = n, RowCount = 1, Margin = Padding.Empty };
            for (int i = 0; i < n; i++) tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / n));
            return tabla;
        }

        private Control Tarjeta(string titulo, IconChar icono, Control contenido)
        {
            var tarjeta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Superficie, Margin = new Padding(0, TemaEcoAccess.Px(this, 8), TemaEcoAccess.Px(this, 12), TemaEcoAccess.Px(this, 4)), Padding = new Padding(TemaEcoAccess.Px(this, 14), TemaEcoAccess.Px(this, 10), TemaEcoAccess.Px(this, 14), TemaEcoAccess.Px(this, 10)) };
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 36)));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tarjeta.Paint += (_, e) => { using var borde = new Pen(TemaEcoAccess.Borde); e.Graphics.DrawRectangle(borde, 0, 0, tarjeta.Width - 1, tarjeta.Height - 1); };
            var encabezado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty, BackColor = TemaEcoAccess.Superficie };
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 30)));
            encabezado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            encabezado.Controls.Add(new IconPictureBox { IconChar = icono, IconColor = TemaEcoAccess.Verde, BackColor = TemaEcoAccess.Superficie, IconSize = TemaEcoAccess.Px(this, 20), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill, Margin = Padding.Empty }, 0, 0);
            encabezado.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Fill, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.Texto, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, UseMnemonic = false, Margin = Padding.Empty }, 1, 0);
            tarjeta.Controls.Add(encabezado, 0, 0);
            contenido.Dock = DockStyle.Fill;
            tarjeta.Controls.Add(contenido, 0, 1);
            return tarjeta;
        }

        private Control Indicador(string titulo, string valor, string? detalle, IconChar icono)
        {
            var tarjeta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = TemaEcoAccess.Superficie, Margin = new Padding(0, 0, TemaEcoAccess.Px(this, 12), 0), Padding = new Padding(TemaEcoAccess.Px(this, 12), TemaEcoAccess.Px(this, 8), TemaEcoAccess.Px(this, 10), TemaEcoAccess.Px(this, 6)) };
            tarjeta.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 64)));
            tarjeta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 3; i++) tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, i == 1 ? 46 : 27));
            tarjeta.Paint += (_, e) => { using var borde = new Pen(TemaEcoAccess.Borde); e.Graphics.DrawRectangle(borde, 0, 0, tarjeta.Width - 1, tarjeta.Height - 1); };
            var dibujo = new IconPictureBox { IconChar = icono, IconColor = TemaEcoAccess.VerdeOscuro, BackColor = TemaEcoAccess.VerdeSuave, IconSize = TemaEcoAccess.Px(this, 34), Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage, Margin = new Padding(0, 4, 8, 4) };
            tarjeta.Controls.Add(dibujo, 0, 0);
            tarjeta.SetRowSpan(dibujo, 3);
            tarjeta.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Fill, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = true }, 1, 0);
            tarjeta.Controls.Add(new Label { Text = valor, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 17), ForeColor = TemaEcoAccess.Texto, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, UseMnemonic = false }, 1, 1);
            tarjeta.Controls.Add(new Label { Text = detalle ?? "", Dock = DockStyle.Fill, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.VerdeOscuro, TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true }, 1, 2);
            return tarjeta;
        }

        // ── Datos ──────────────────────────────────────────────────────────────────────────────────────────────

        private async Task CargarAsync()
        {
            _aviso.Text = "Actualizando…";
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            try
            {
                _catalogo = await _servicios.CatalogoAmbiental.ListarHabilitadosAsync(limite.Token);
                ServicioPanelCiudadano? panel = _servicios.PanelCiudadano;
                if (_sesion is null || panel is null)
                {
                    MostrarIndicadoresInvitado();
                    MostrarCanecas(panel is null ? Array.Empty<CanecaEstacion>() : await panel.CanecasEstacionAsync(limite.Token));
                    MostrarHistorial(null);
                }
                else
                {
                    Resultado<PanelCiudadano> datos = await panel.ObtenerAsync(_sesion.IdUsuario, limite.Token);
                    if (!datos.Exitoso) { _aviso.Text = datos.Mensaje ?? "No se pudieron cargar tus datos."; return; }
                    MostrarIndicadores(datos.Valor);
                    _grafica.Dias = datos.Valor.Semana;
                    MostrarHistorial(datos.Valor.Recientes);
                    MostrarCanecas(datos.Valor.Canecas);
                    _telegram.Text = datos.Valor.TelegramVinculado switch
                    {
                        true => "✔ Cuenta vinculada: tus depósitos por el bot suman aquí.",
                        false => "Aún no vinculada. En el bot usa «Vincular cuenta».",
                        null => "No se pudo consultar la vinculación en este momento.",
                    };
                }
                _aviso.Text = "Actualizado " + DateTime.Now.ToString("h:mm tt", new CultureInfo("es-CO"));
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // Sin texto del proveedor: puede citar host o servicio.
                _aviso.Text = "Sin conexión con la base de datos.";
            }
        }

        private void MostrarIndicadores(PanelCiudadano datos)
        {
            _tarjetas.Controls.Clear();
            _tarjetas.ColumnStyles.Clear();
            for (int i = 0; i < 4; i++) _tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _tarjetas.Controls.Add(Indicador("Depósitos (90 días)", datos.Depositos.ToString("N0", new CultureInfo("es-CO")), datos.Verificados + " verificados", IconChar.TrashCan), 0, 0);
            _tarjetas.Controls.Add(Indicador("Ecopuntos acumulados", datos.Saldo.ToString("N0", new CultureInfo("es-CO")), "Saldo actual", IconChar.Trophy), 1, 0);
            _tarjetas.Controls.Add(Indicador("Residuos clasificados", datos.MaterialesDistintos + (datos.MaterialesDistintos == 1 ? " tipo" : " tipos"), "En los últimos 90 días", IconChar.Recycle), 2, 0);
            _tarjetas.Controls.Add(Indicador("Último depósito", datos.UltimoDepositoUtc is { } fecha ? Dia(fecha) : "Aún no hay", datos.UltimoDepositoUtc is { } hora ? Hora(hora) : null, IconChar.Leaf), 3, 0);
        }

        private void MostrarIndicadoresInvitado()
        {
            _tarjetas.Controls.Clear();
            _tarjetas.ColumnStyles.Clear();
            for (int i = 0; i < 3; i++) _tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            _tarjetas.Controls.Add(Indicador("Materiales en el catálogo", _catalogo.Count.ToString(CultureInfo.InvariantCulture), "Consulta dónde va cada uno", IconChar.Recycle), 0, 0);
            _tarjetas.Controls.Add(Indicador("Canecas de la estación", "3", "Blanca, verde y negra", IconChar.TrashCan), 1, 0);
            _tarjetas.Controls.Add(Indicador("Ecopuntos", "Con una cuenta", "Los invitados no acumulan", IconChar.Trophy), 2, 0);
        }

        private void MostrarHistorial(IReadOnlyList<FilaDeposito>? filas)
        {
            _historial.BeginUpdate();
            _historial.Columns.Clear();
            _historial.Items.Clear();
            foreach (string columna in new[] { "Fecha", "Residuo", "Peso", "Estado" }) _historial.Columns.Add(columna);
            if (filas is null || filas.Count == 0)
                _historial.Items.Add(new ListViewItem(new[] { "", filas is null ? "Inicia sesión para ver tu historial." : "Sin depósitos en 90 días.", "", "" }) { ForeColor = TemaEcoAccess.Suave });
            else
                foreach (FilaDeposito deposito in filas.Take(5))
                {
                    decimal? peso = deposito.PesoMedidoKg ?? deposito.PesoEstimadoKg;
                    bool verificado = deposito.MaterialVerificado is not null;
                    var item = new ListViewItem(new[]
                    {
                        (deposito.FechaDeteccionUtc + HoraColombia.Desfase).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture),
                        deposito.MaterialVerificado ?? deposito.MaterialDeclarado,
                        peso is { } kg ? kg.ToString("0.00", CultureInfo.InvariantCulture) + " kg" : "—",
                        verificado ? "Verificado" : "Pendiente",
                    }) { UseItemStyleForSubItems = false };
                    item.SubItems[3].ForeColor = verificado ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Advertencia;
                    _historial.Items.Add(item);
                }
            AjustarColumnasHistorial();
            _historial.EndUpdate();
        }

        private void AjustarColumnasHistorial()
        {
            if (_historial.Columns.Count != 4) return;
            int ancho = Math.Max(200, _historial.ClientSize.Width - 4);
            float[] partes = { 0.25f, 0.37f, 0.15f, 0.23f };
            for (int i = 0; i < 4; i++) _historial.Columns[i].Width = (int)(ancho * partes[i]);
        }

        private void MostrarCanecas(IReadOnlyList<CanecaEstacion> canecas)
        {
            _canecas.SuspendLayout();
            _canecas.Controls.Clear();
            _canecas.RowStyles.Clear();
            if (canecas.Count == 0)
            {
                _canecas.Controls.Add(new Label { Text = "No hay canecas configuradas en la base conectada.", Dock = DockStyle.Fill, ForeColor = TemaEcoAccess.Suave, TextAlign = ContentAlignment.MiddleCenter });
                _canecas.ResumeLayout();
                return;
            }
            foreach (CanecaEstacion caneca in canecas)
            {
                (string color, Color tono, Color texto) = ResolutorDestino.PorCategoria(caneca.Categoria) switch
                {
                    DestinoResiduo.Blanca => ("Caneca blanca", Color.White, Color.FromArgb(0x1F, 0x29, 0x37)),
                    DestinoResiduo.Verde => ("Caneca verde", TemaEcoAccess.VerdeOscuro, Color.White),
                    DestinoResiduo.Negra => ("Caneca negra", Color.FromArgb(0x1F, 0x29, 0x37), Color.White),
                    _ => (TextosCategoria.Texto(caneca.Categoria), TemaEcoAccess.Error, Color.White),
                };
                _canecas.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / canecas.Count));
                var fila = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = new Padding(0, 3, 0, 3), BackColor = TemaEcoAccess.Fondo };
                fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 46)));
                fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                var muestra = new IconPictureBox { IconChar = IconChar.TrashCan, IconColor = texto, BackColor = tono, IconSize = TemaEcoAccess.Px(this, 24), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill, Margin = new Padding(6), BorderStyle = BorderStyle.FixedSingle };
                fila.Controls.Add(muestra, 0, 0);
                fila.Controls.Add(new Label { Text = color, Dock = DockStyle.Fill, Font = TemaEcoAccess.Cuerpo, ForeColor = TemaEcoAccess.Texto, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }, 1, 0);
                fila.Controls.Add(new Label { Text = caneca.EnServicio ? "En servicio" : "Fuera de servicio", AutoSize = true, Anchor = AnchorStyles.Right, Font = TemaEcoAccess.Pequena, ForeColor = caneca.EnServicio ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Error, BackColor = caneca.EnServicio ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.ErrorSuave, Padding = new Padding(8, 3, 8, 3) }, 2, 0);
                _canecas.Controls.Add(fila);
            }
            _canecas.ResumeLayout();
        }

        // ── Acciones (reutilizan las ventanas existentes) ───────────────────────────────────────────────────────

        private void AbrirAsistente()
        {
            using var asistente = new FormAsistenteDeposito(_servicios, _sesion, null);
            asistente.ShowDialog(this);
            if (asistente.DepositoRegistrado) _ = CargarAsync();
        }

        private void Clasificar(GrupoEstacion? grupo)
        {
            using var ventana = new FormClasificarResiduo(_catalogo, grupo);
            ventana.ShowDialog(this);
        }

        private void MostrarCanecas()
        {
            MessageBox.Show(this, "La estación piloto tiene tres canecas: blanca (aprovechables), verde (orgánicos) y negra (no aprovechables). " +
                "Lo peligroso (pilas, medicamentos, aparatos) no va en ninguna: Eco te orienta a un punto posconsumo. " +
                "El estado de cada una aparece en «Estado de las canecas».", "Contenedores", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MostrarActividad()
        {
            if (_sesion is null) return;
            using var ventana = new FormEstadisticasPersonales(_servicios.Estadisticas, _sesion.IdUsuario);
            ventana.ShowDialog(this);
        }

        private void MostrarEcopuntos()
        {
            if (_sesion is null) return;
            using var ventana = new FormMisEcopuntos(_servicios.Ecopuntos, _servicios.Estadisticas, _sesion.IdUsuario);
            ventana.ShowDialog(this);
        }

        private void MostrarQr()
        {
            // Siempre el QR oficial del repositorio; nunca se genera otro.
            var enlace = EnlaceTelegram.ParaCaneca(EnlaceTelegram.NombreBotPiloto, EnlaceTelegram.IdentificadorQrOficial);
            using var qr = new FormQrCaneca(ServicioCatalogoAmbiental.NombreCanecaPiloto, enlace, RecursosEco.QrOficialPng(), vistaCiudadana: true);
            qr.ShowDialog(this);
        }

        private void AbrirApariencia()
        {
            using var selector = new FormSeleccionInterfaz(_sesion, CatalogoApariencias.PerfilDe(_sesion, AreaAcceso.Ciudadano), CatalogoApariencias.Moderno);
            if (selector.ShowDialog(this) == DialogResult.OK && selector.Aplicado)
            {
                CambioAparienciaSolicitado = true;
                Close();
            }
        }

        // ── Utilidades ─────────────────────────────────────────────────────────────────────────────────────────

        private static string PrimerNombre(string nombre) => nombre.Trim().Split(' ').FirstOrDefault() ?? nombre;

        private static string Dia(DateTime utc)
        {
            DateTime local = (utc + HoraColombia.Desfase).Date;
            DateTime hoy = (DateTime.UtcNow + HoraColombia.Desfase).Date;
            return local == hoy ? "Hoy" : local == hoy.AddDays(-1) ? "Ayer" : local.ToString("d MMM", new CultureInfo("es-CO"));
        }

        private static string Hora(DateTime utc) => (utc + HoraColombia.Desfase).ToString("h:mm tt", new CultureInfo("es-CO"));

        /// <summary>Gráfica de barras de los depósitos por día. Dibujada a mano: sin datos inventados ni escala falsa.</summary>
        private sealed class GraficaSemana : Control
        {
            private IReadOnlyList<DepositosDia> _dias = Array.Empty<DepositosDia>();

            public GraficaSemana() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true); BackColor = TemaEcoAccess.Superficie; }

            public IReadOnlyList<DepositosDia> Dias { get => _dias; set { _dias = value ?? Array.Empty<DepositosDia>(); Invalidate(); } }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (_dias.Count == 0)
                {
                    TextRenderer.DrawText(g, "Sin datos todavía.", TemaEcoAccess.Cuerpo, ClientRectangle, TemaEcoAccess.Suave, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    return;
                }
                int maximo = Math.Max(1, _dias.Max(d => d.Depositos));
                int abajo = Height - 22, arriba = 18, ancho = Width / _dias.Count;
                var cultura = new CultureInfo("es-CO");
                using var barra = new SolidBrush(TemaEcoAccess.Verde);
                using var linea = new Pen(TemaEcoAccess.Borde);
                g.DrawLine(linea, 0, abajo, Width, abajo);
                for (int i = 0; i < _dias.Count; i++)
                {
                    DepositosDia dia = _dias[i];
                    int alto = (int)((abajo - arriba) * (dia.Depositos / (double)maximo));
                    var area = new Rectangle(i * ancho + ancho / 4, abajo - alto, ancho / 2, alto);
                    if (alto > 0) g.FillRectangle(barra, area);
                    TextRenderer.DrawText(g, dia.Depositos.ToString(cultura), TemaEcoAccess.Pequena, new Rectangle(i * ancho, abajo - alto - 18, ancho, 16), TemaEcoAccess.Texto, TextFormatFlags.HorizontalCenter);
                    string etiqueta = cultura.TextInfo.ToTitleCase(dia.Dia.ToDateTime(TimeOnly.MinValue).ToString("ddd", cultura).TrimEnd('.'));
                    TextRenderer.DrawText(g, etiqueta, TemaEcoAccess.Pequena, new Rectangle(i * ancho, abajo + 3, ancho, 18), TemaEcoAccess.Suave, TextFormatFlags.HorizontalCenter);
                }
            }
        }
    }
}
