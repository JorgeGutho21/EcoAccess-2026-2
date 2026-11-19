using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Pantalla táctil de la estación EcoAccess (modo <c>--estacion</c>). Observación del profesor Patiño: es la misma
    /// aplicación y la misma base de datos, con las demás opciones ocultas; un solo botón grande y luego «escoge».
    /// Flujo: «Voy a botar algo» → grupo → residuo → caneca. Solo orienta: no abre canecas ni registra depósitos (eso lo
    /// hace el flujo de Telegram con QR y presencia), así que nunca muestra un depósito que no ocurrió.
    /// </summary>
    public sealed class FormEstacionTactil : Form
    {
        private static readonly TimeSpan EsperaInactiva = TimeSpan.FromSeconds(45);
        private static readonly Color Blanca = Color.White;
        private static readonly Color Negra = Color.FromArgb(0x1F, 0x29, 0x37);

        private readonly ServicioCatalogoAmbiental _catalogo;
        private readonly Panel _vistas = new() { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo };
        private readonly Label _estado = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 13), Anchor = AnchorStyles.Right };
        private readonly System.Windows.Forms.Timer _inactividad = new();
        private IReadOnlyList<Material> _materiales = Array.Empty<Material>();
        private IconButton? _botonPrincipal;
        private Label? _avisoInicio;
        private bool _cargando;

        public FormEstacionTactil(ServicioCatalogoAmbiental catalogo)
        {
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
            Text = "EcoAccess - Estación";
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = TemaEcoAccess.Fondo;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1024, 700);
            KeyPreview = true;
            DoubleBuffered = true;

            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = TemaEcoAccess.Fondo };
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 96)));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.Controls.Add(CrearEncabezado(), 0, 0);
            raiz.Controls.Add(_vistas, 0, 1);
            Controls.Add(raiz);

            _inactividad.Interval = (int)EsperaInactiva.TotalMilliseconds;
            _inactividad.Tick += (_, _) => MostrarInicio();
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            Shown += async (_, _) => await CargarCatalogoAsync();
            MostrarInicio();
        }

        private Control CrearEncabezado()
        {
            var barra = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = TemaEcoAccess.VerdeOscuro, Padding = new Padding(TemaEcoAccess.Px(this, 28), 0, TemaEcoAccess.Px(this, 28), 0) };
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var marca = new Label { Text = "EcoAccess", Font = new Font("Segoe UI Semibold", 30), ForeColor = Color.White, AutoSize = true, Anchor = AnchorStyles.Left };
            _estado.ForeColor = Color.White;
            barra.Controls.Add(marca, 0, 0);
            barra.Controls.Add(_estado, 1, 0);
            return barra;
        }

        // ── Datos ──────────────────────────────────────────────────────────────────────────────────────────────

        private async Task CargarCatalogoAsync()
        {
            if (_cargando) return;
            _cargando = true;
            Estado("Conectando…", true);
            try
            {
                using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                _materiales = await _catalogo.ListarHabilitadosAsync(limite.Token);
                Estado(_materiales.Count > 0 ? "● Sistema disponible" : "Catálogo vacío", _materiales.Count > 0);
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                // No se muestra el error de Oracle (puede citar host o servicio): solo que no hay conexión.
                _materiales = Array.Empty<Material>();
                Estado("● Sin conexión", false);
            }
            finally
            {
                _cargando = false;
                ActualizarInicio();
            }
        }

        private void Estado(string texto, bool correcto)
        {
            _estado.Text = texto;
            _estado.ForeColor = correcto ? Color.White : Color.FromArgb(0xFF, 0xD7, 0xD2);
        }

        // ── Vistas ─────────────────────────────────────────────────────────────────────────────────────────────

        private void Mostrar(Control vista, bool vigilarInactividad)
        {
            _vistas.SuspendLayout();
            foreach (Control anterior in _vistas.Controls.Cast<Control>().ToList()) { _vistas.Controls.Remove(anterior); anterior.Dispose(); }
            vista.Dock = DockStyle.Fill;
            _vistas.Controls.Add(vista);
            _vistas.ResumeLayout();
            _inactividad.Stop();
            if (vigilarInactividad) _inactividad.Start();
        }

        /// <summary>Vista 1: un solo botón grande.</summary>
        private void MostrarInicio()
        {
            var vista = Rejilla(1, 4);
            vista.RowStyles[0] = new RowStyle(SizeType.Percent, 34);
            vista.RowStyles[1] = new RowStyle(SizeType.Percent, 14);
            vista.RowStyles[2] = new RowStyle(SizeType.Percent, 32);
            vista.RowStyles[3] = new RowStyle(SizeType.Percent, 20);

            var eco = new PictureBox { Image = RecursosEco.Eco, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill, Margin = new Padding(0, TemaEcoAccess.Px(this, 20), 0, 0) };
            var pregunta = Texto("¿Vas a botar algo?", 34, TemaEcoAccess.Texto);
            _botonPrincipal = Boton("Voy a botar algo", IconChar.HandPointer, 40, TemaEcoAccess.Verde, Color.White, 96);
            _botonPrincipal.Dock = DockStyle.None;
            _botonPrincipal.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
            _botonPrincipal.Width = TemaEcoAccess.Px(this, 720);
            Navegar(_botonPrincipal, MostrarGrupos);
            _avisoInicio = Texto("", 18, TemaEcoAccess.Suave);

            vista.Controls.Add(eco, 0, 0);
            vista.Controls.Add(pregunta, 0, 1);
            vista.Controls.Add(_botonPrincipal, 0, 2);
            vista.Controls.Add(_avisoInicio, 0, 3);
            Mostrar(vista, vigilarInactividad: false);
            ActualizarInicio();
        }

        private void ActualizarInicio()
        {
            if (_botonPrincipal is null || _avisoInicio is null || _botonPrincipal.IsDisposed) return;
            bool listo = _materiales.Count > 0;
            _botonPrincipal.Enabled = listo;
            _botonPrincipal.BackColor = listo ? TemaEcoAccess.Verde : Color.FromArgb(0x9C, 0xA3, 0xAF);
            _avisoInicio.Text = listo ? "Toca el botón y te decimos en qué caneca va."
                : _cargando ? "Cargando el catálogo…"
                : "No hay conexión con la base de datos. Pide ayuda al personal. Se reintentará sola.";
            if (!listo && !_cargando) _ = ReintentarAsync();
        }

        private async Task ReintentarAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            if (!IsDisposed && _materiales.Count == 0) await CargarCatalogoAsync();
        }

        /// <summary>Vista 2: «escoge» entre seis grupos grandes.</summary>
        private void MostrarGrupos()
        {
            var vista = Rejilla(1, 3);
            vista.RowStyles[0] = new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 80));
            vista.RowStyles[1] = new RowStyle(SizeType.Percent, 100);
            vista.RowStyles[2] = new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 128));
            vista.Controls.Add(Texto("¿Qué vas a botar?", 30, TemaEcoAccess.Texto), 0, 0);

            var grupos = Rejilla(3, 2);
            foreach (GrupoEstacion grupo in GuiaEstacionTactil.Grupos)
            {
                IReadOnlyList<Material> lista = GuiaEstacionTactil.MaterialesDe(grupo, _materiales);
                bool especial = grupo == GrupoEstacion.Especiales;
                IconButton boton = Boton(GuiaEstacionTactil.Nombre(grupo), IconoDe(grupo), 24,
                    Color.White, especial ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro, 88, vertical: true);
                boton.Enabled = lista.Count > 0;
                Navegar(boton, () => MostrarMateriales(grupo));
                grupos.Controls.Add(boton);
            }
            vista.Controls.Add(grupos, 0, 1);
            vista.Controls.Add(BotonVolver("Volver al inicio", MostrarInicio), 0, 2);
            Mostrar(vista, vigilarInactividad: true);
        }

        /// <summary>Vista 3: los residuos del grupo, tal como están en el catálogo de Oracle.</summary>
        private void MostrarMateriales(GrupoEstacion grupo)
        {
            IReadOnlyList<Material> lista = GuiaEstacionTactil.MaterialesDe(grupo, _materiales);
            var vista = Rejilla(1, 3);
            vista.RowStyles[0] = new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 80));
            vista.RowStyles[1] = new RowStyle(SizeType.Percent, 100);
            vista.RowStyles[2] = new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 128));
            vista.Controls.Add(Texto(GuiaEstacionTactil.Nombre(grupo) + ": toca el tuyo", 28, TemaEcoAccess.Texto), 0, 0);

            int columnas = lista.Count <= 4 ? 2 : 3;
            var rejilla = Rejilla(columnas, (lista.Count + columnas - 1) / columnas);
            foreach (Material material in lista)
            {
                IconButton boton = Boton(material.Nombre, IconoDe(grupo), lista.Count > 9 ? 17 : 20,
                    Color.White, TemaEcoAccess.Texto, lista.Count > 9 ? 40 : 56, vertical: false);
                Navegar(boton, () => MostrarResultado(GuiaEstacionTactil.Orientar(material)));
                rejilla.Controls.Add(boton);
            }
            vista.Controls.Add(rejilla, 0, 1);
            vista.Controls.Add(BotonVolver("Volver", MostrarGrupos), 0, 2);
            Mostrar(vista, vigilarInactividad: true);
        }

        /// <summary>Vista 4: la caneca, grande y con su color; lo especial no abre ni indica ninguna de las tres.</summary>
        private void MostrarResultado(OrientacionEstacion destino)
        {
            (Color fondo, Color texto, IconChar icono) = destino.Destino switch
            {
                DestinoResiduo.Blanca => (Blanca, Negra, IconChar.Recycle),
                DestinoResiduo.Verde => (TemaEcoAccess.VerdeOscuro, Color.White, IconChar.Seedling),
                DestinoResiduo.Negra => (Negra, Color.White, IconChar.TrashCan),
                _ => (TemaEcoAccess.Error, Color.White, IconChar.TriangleExclamation),
            };

            var vista = Rejilla(1, 3);
            vista.RowStyles[0] = new RowStyle(SizeType.Percent, 62);
            vista.RowStyles[1] = new RowStyle(SizeType.Percent, 20);
            vista.RowStyles[2] = new RowStyle(SizeType.Absolute, TemaEcoAccess.Px(this, 128));

            var tarjeta = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = fondo, Margin = new Padding(TemaEcoAccess.Px(this, 60), TemaEcoAccess.Px(this, 20), TemaEcoAccess.Px(this, 60), 0) };
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            tarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            if (destino.Destino == DestinoResiduo.Blanca) tarjeta.Padding = new Padding(TemaEcoAccess.Px(this, 6));
            var dibujo = new IconPictureBox { IconChar = icono, IconColor = texto, BackColor = fondo, IconSize = TemaEcoAccess.Px(this, 120), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill };
            var titulo = Texto(destino.Titulo.ToUpperInvariant(), 48, texto);
            var material = Texto(destino.Material.Nombre, 22, texto);
            tarjeta.Controls.Add(dibujo, 0, 0);
            tarjeta.Controls.Add(titulo, 0, 1);
            tarjeta.Controls.Add(material, 0, 2);
            if (destino.Destino == DestinoResiduo.Blanca)
                tarjeta.Paint += (_, e) => { using var borde = new Pen(Negra, TemaEcoAccess.Px(this, 4)); e.Graphics.DrawRectangle(borde, 2, 2, tarjeta.Width - 4, tarjeta.Height - 4); };

            vista.Controls.Add(tarjeta, 0, 0);
            vista.Controls.Add(Texto(destino.Indicacion, 18, TemaEcoAccess.Texto), 0, 1);
            vista.Controls.Add(BotonVolver("Listo", MostrarInicio, principal: true), 0, 2);
            Mostrar(vista, vigilarInactividad: true);
        }

        // ── Piezas ─────────────────────────────────────────────────────────────────────────────────────────────

        private TableLayoutPanel Rejilla(int columnas, int filas)
        {
            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = columnas, RowCount = filas, BackColor = TemaEcoAccess.Fondo, Padding = new Padding(TemaEcoAccess.Px(this, 24)) };
            for (int c = 0; c < columnas; c++) tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnas));
            for (int f = 0; f < filas; f++) tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / filas));
            return tabla;
        }

        private static Label Texto(string texto, float puntos, Color color) => new()
        {
            Text = texto, Font = new Font("Segoe UI Semibold", puntos), ForeColor = color, BackColor = Color.Transparent,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, UseMnemonic = false
        };

        private IconButton Boton(string texto, IconChar icono, float puntos, Color fondo, Color color, int icono96, bool vertical = false)
        {
            var boton = new IconButton
            {
                Text = texto, IconChar = icono, IconColor = color, IconSize = TemaEcoAccess.Px(this, icono96),
                Font = new Font("Segoe UI Semibold", puntos), ForeColor = color, BackColor = fondo,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Cursor = Cursors.Hand, UseMnemonic = false,
                Margin = new Padding(TemaEcoAccess.Px(this, 10)),
                TextImageRelation = vertical ? TextImageRelation.ImageAboveText : TextImageRelation.ImageBeforeText,
                ImageAlign = vertical ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                TextAlign = vertical ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                Padding = new Padding(TemaEcoAccess.Px(this, 16)),
            };
            boton.FlatAppearance.BorderColor = TemaEcoAccess.Borde;
            boton.FlatAppearance.BorderSize = fondo == Color.White ? 2 : 0;
            boton.Click += (_, _) => { if (_inactividad.Enabled) { _inactividad.Stop(); _inactividad.Start(); } };
            return boton;
        }

        private IconButton BotonVolver(string texto, Action accion, bool principal = false)
        {
            IconButton boton = Boton(texto, principal ? IconChar.Check : IconChar.ArrowLeft, 22,
                principal ? TemaEcoAccess.Verde : Color.White, principal ? Color.White : TemaEcoAccess.VerdeOscuro, 40);
            boton.Dock = DockStyle.None;
            boton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
            boton.Width = TemaEcoAccess.Px(this, 420);
            boton.Padding = new Padding(TemaEcoAccess.Px(this, 6));
            boton.TextImageRelation = TextImageRelation.ImageBeforeText;
            boton.ImageAlign = ContentAlignment.MiddleCenter;
            boton.TextAlign = ContentAlignment.MiddleCenter;
            Navegar(boton, accion);
            return boton;
        }

        /// <summary>
        /// Cambia de vista después del clic, no durante: la vista nueva libera los botones de la anterior y liberar el botón
        /// pulsado dentro de su propio evento Click deja a Windows Forms trabajando sobre un control ya destruido.
        /// </summary>
        private void Navegar(Control boton, Action accion) => boton.Click += (_, _) => BeginInvoke(accion);

        private static IconChar IconoDe(GrupoEstacion grupo) => grupo switch
        {
            GrupoEstacion.Plastico => IconChar.BottleWater,
            GrupoEstacion.PapelCarton => IconChar.Newspaper,
            GrupoEstacion.VidrioMetal => IconChar.WineBottle,
            GrupoEstacion.Comida => IconChar.AppleWhole,
            GrupoEstacion.Otros => IconChar.ToiletPaper,
            _ => IconChar.BatteryFull,
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inactividad.Dispose();
            base.Dispose(disposing);
        }
    }
}
