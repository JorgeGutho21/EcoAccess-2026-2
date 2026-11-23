using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Pantalla de reconocimiento visual multirresiduo ECOACCESS P3 (Sección 13).
    /// Integra: captura/carga de fotografía, etiquetas visuales sobre vista previa, lista de objetos detectados,
    /// canecas recomendadas (1, 2 o 3 sin duplicar), estado de la máquina de estados (11 estados) y apertura
    /// secuencial guiada en modo simulado seguro, búsqueda escrita diferenciada de la detección visual, acceso
    /// al catálogo fotográfico, clasificación manual/confirmación, cancelación asíncrona sin congelar la UI
    /// e historial local de operaciones con tolerancia a desconexiones de Oracle.
    /// </summary>
    public sealed class FormReconocimientoVisual : Form
    {
        private ClienteVisionOllamaMultirresiduo _clienteVision = new();
        private readonly SimuladorEstacionTresCanecas _simuladorEstacion = new(
            new Dictionary<int, DestinoResiduo>
            {
                [1093] = DestinoResiduo.Blanca,
                [1400] = DestinoResiduo.Verde,
                [1401] = DestinoResiduo.Negra,
            },
            TimeProvider.System,
            TimeSpan.Zero);
        private GestorSesionMultirresiduo _gestor;
        private readonly ColaLocalOperaciones _colaLocal = new();

        private readonly ComboBox _selectorModelo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 155 };
        private readonly ComboBox _selectorApertura = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 185 };

        private readonly PictureBox _vistaPrevia = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(245, 248, 245) };
        private readonly Label _estadoMaquina = new() { AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.VerdeOscuro };
        private readonly Label _contador = new() { AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave };
        private readonly Label _bannerError = new() { Dock = DockStyle.Top, AutoSize = true, ForeColor = TemaEcoAccess.Error, Padding = new Padding(8, 6, 8, 6), Visible = false };
        private readonly Label _etiquetaModo = new() { AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = TemaEcoAccess.VerdeOscuro, Text = "Modo: Esperando fotografía o búsqueda escrita" };

        private readonly BotonEco _btnCargarFoto = new("Cargar foto / captura", IconChar.Camera, BotonEco.Estilo.Principal);
        private readonly BotonEco _btnRepetir = new("Repetir fotografía", IconChar.RotateLeft, BotonEco.Estilo.Secundario) { Enabled = false };
        private readonly BotonEco _btnCancelar = new("Cancelar análisis", IconChar.Stop, BotonEco.Estilo.Secundario) { Enabled = false };
        private readonly BotonEco _btnCatalogo = new("Catálogo fotográfico", IconChar.Images, BotonEco.Estilo.Secundario);
        private readonly BotonEco _btnManual = new("Clasificación manual", IconChar.HandPointer, BotonEco.Estilo.Secundario);
        private readonly BotonEco _btnConfirmarAbrir = new("Abrir canecas (demo simulada)", IconChar.BoxOpen, BotonEco.Estilo.Principal) { Enabled = false };

        private readonly CajaTexto _cajaBusqueda = new() { PlaceholderText = "Búsqueda escrita (ej. cáscara de aguacate, botella, pitillo)..." };
        private readonly BotonEco _btnBuscarTexto = new("Buscar texto", IconChar.MagnifyingGlass, BotonEco.Estilo.Secundario);

        private readonly ListView _listaObjetos = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
        private readonly ListView _listaHistorial = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };

        private readonly Panel _tarjetaVerde;
        private readonly Panel _tarjetaBlanca;
        private readonly Panel _tarjetaNegra;
        private readonly Label _detalleTexto = new() { Dock = DockStyle.Fill, AutoSize = false, Font = TemaEcoAccess.Cuerpo, ForeColor = TemaEcoAccess.Texto };

        private CancellationTokenSource? _ctsAnalisis;
        private byte[]? _bytesActuales;
        private ResultadoAnalisisMultirresiduo? _ultimoAnalisis;
        private Image? _imagenActual;

        public FormReconocimientoVisual()
        {
            Text = "Reconocimiento visual multirresiduo · EcoAccess P3";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Font = TemaEcoAccess.Cuerpo;
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            Size = new Size(TemaEcoAccess.Px(this, 1260), TemaEcoAccess.Px(this, 780));
            MinimumSize = new Size(TemaEcoAccess.Px(this, 960), TemaEcoAccess.Px(this, 620));

            _gestor = new GestorSesionMultirresiduo(
                _clienteVision,
                new MotorReglasMultirresiduo(),
                _simuladorEstacion,
                validadorCalidad: null,
                reloj: TimeProvider.System);

            // Cabecera con barra de herramientas y búsqueda escrita diferenciada.
            var cabecera = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1,
                BackColor = TemaEcoAccess.Superficie, Padding = new Padding(16, 12, 16, 10),
            };
            cabecera.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var filaTitulo = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = Padding.Empty };
            filaTitulo.Controls.Add(new Label { Text = "Reconocimiento visual y clasificación de los 35 residuos", AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(0, 0, 16, 4) });
            filaTitulo.Controls.Add(_estadoMaquina);
            cabecera.Controls.Add(filaTitulo);

            var filaAcciones = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 6, 0, 6) };
            foreach (BotonEco b in new[] { _btnCargarFoto, _btnRepetir, _btnCancelar, _btnManual, _btnCatalogo, _btnConfirmarAbrir })
            {
                b.Margin = new Padding(0, 0, 8, 4);
                b.Size = b.GetPreferredSize(Size.Empty);
                filaAcciones.Controls.Add(b);
            }
            _selectorModelo.Items.AddRange(new object[] { "lfm3-vl:latest", "qwen3-vl:4b", "qwen3-vl:2b", "qwen3-vl:8b" });
            int idxMod = _selectorModelo.Items.IndexOf(_clienteVision.Modelo);
            _selectorModelo.SelectedIndex = idxMod >= 0 ? idxMod : 0;
            _selectorModelo.SelectedIndexChanged += (_, _) => CambiarModeloVisual(_selectorModelo.SelectedItem?.ToString() ?? ClienteVisionOllamaMultirresiduo.ModeloPredeterminado);
            _selectorApertura.Items.AddRange(new object[] { "Secuencial guiada (segura)", "Simultánea (supervisada)" });
            _selectorApertura.SelectedIndex = 0;
            filaAcciones.Controls.Add(new Label { Text = "Modelo IA:", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(6, 9, 4, 0) });
            filaAcciones.Controls.Add(_selectorModelo);
            filaAcciones.Controls.Add(new Label { Text = "Apertura:", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(8, 9, 4, 0) });
            filaAcciones.Controls.Add(_selectorApertura);
            cabecera.Controls.Add(filaAcciones);

            var filaBusqueda = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Margin = new Padding(0, 2, 0, 0) };
            filaBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filaBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filaBusqueda.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filaBusqueda.Controls.Add(new Label { Text = "Búsqueda escrita (no es detección visual):", AutoSize = true, ForeColor = TemaEcoAccess.Suave, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) }, 0, 0);
            _cajaBusqueda.Dock = DockStyle.Fill;
            filaBusqueda.Controls.Add(_cajaBusqueda, 1, 0);
            _btnBuscarTexto.Size = _btnBuscarTexto.GetPreferredSize(Size.Empty);
            filaBusqueda.Controls.Add(_btnBuscarTexto, 2, 0);
            cabecera.Controls.Add(filaBusqueda);

            // Cuerpo principal dividido: izquierda imagen + canecas, derecha objetos + detalles + historial.
            var cuerpo = new SplitContainer
            {
                Dock = DockStyle.Fill, Orientation = Orientation.Vertical,
                SplitterDistance = TemaEcoAccess.Px(this, 520), BackColor = TemaEcoAccess.Fondo, Padding = new Padding(12),
            };

            // Panel izquierdo: vista previa con etiquetas visuales + 3 canecas recomendadas.
            var izq = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo };
            izq.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            izq.RowStyles.Add(new RowStyle(SizeType.Percent, 72));
            izq.RowStyles.Add(new RowStyle(SizeType.Percent, 28));

            var barraInfo = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            barraInfo.Controls.Add(_etiquetaModo);
            barraInfo.Controls.Add(_contador);
            izq.Controls.Add(barraInfo, 0, 0);

            var marcoFoto = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6), Margin = new Padding(0, 6, 0, 8) };
            marcoFoto.Controls.Add(_vistaPrevia);
            _vistaPrevia.Paint += DibujarEtiquetasVisuales;
            izq.Controls.Add(marcoFoto, 0, 1);

            var panelCanecas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = TemaEcoAccess.Fondo };
            panelCanecas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            panelCanecas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            panelCanecas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            _tarjetaVerde = CrearTarjetaCaneca("VERDE", "Orgánicos aprovechables", Color.FromArgb(46, 125, 50));
            _tarjetaBlanca = CrearTarjetaCaneca("BLANCA", "Aprovechables limpios", Color.FromArgb(90, 110, 130));
            _tarjetaNegra = CrearTarjetaCaneca("NEGRA", "No aprovechables", Color.FromArgb(45, 48, 52));
            panelCanecas.Controls.Add(_tarjetaVerde, 0, 0);
            panelCanecas.Controls.Add(_tarjetaBlanca, 1, 0);
            panelCanecas.Controls.Add(_tarjetaNegra, 2, 0);
            izq.Controls.Add(panelCanecas, 0, 2);
            cuerpo.Panel1.Controls.Add(izq);

            // Panel derecho: objetos detectados, mensaje/detalle e historial local.
            var der = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo };
            der.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
            der.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
            der.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            _listaObjetos.Columns.Add("#", 30);
            _listaObjetos.Columns.Add("Código", 55);
            _listaObjetos.Columns.Add("Residuo / Objeto", 190);
            _listaObjetos.Columns.Add("Estado", 105);
            _listaObjetos.Columns.Add("Caneca", 75);
            _listaObjetos.Columns.Add("Cant.", 42);
            _listaObjetos.Columns.Add("Validación / Motivo", 210);

            var grupoObjetos = new GroupBox { Text = "Objetos identificados (catálogo 35)", Dock = DockStyle.Fill };
            grupoObjetos.Controls.Add(_listaObjetos);
            der.Controls.Add(grupoObjetos, 0, 0);

            var grupoDetalle = new GroupBox { Text = "Dictamen del motor de reglas y estado de apertura", Dock = DockStyle.Fill };
            _detalleTexto.Padding = new Padding(8);
            grupoDetalle.Controls.Add(_detalleTexto);
            der.Controls.Add(grupoDetalle, 0, 1);

            _listaHistorial.Columns.Add("Hora UTC", 80);
            _listaHistorial.Columns.Add("Modo", 65);
            _listaHistorial.Columns.Add("Materiales", 140);
            _listaHistorial.Columns.Add("Canecas", 130);
            _listaHistorial.Columns.Add("Estado", 140);
            var grupoHistorial = new GroupBox { Text = "Historial de registros locales (respaldo offline sin PII)", Dock = DockStyle.Fill };
            grupoHistorial.Controls.Add(_listaHistorial);
            der.Controls.Add(grupoHistorial, 0, 2);

            cuerpo.Panel2.Controls.Add(der);

            Controls.Add(cuerpo);
            Controls.Add(_bannerError);
            Controls.Add(cabecera);

            // Eventos
            _btnCargarFoto.Click += async (_, _) => await SeleccionarYAnalizarAsync();
            _btnRepetir.Click += async (_, _) => await RepetirFotoAsync();
            _btnCancelar.Click += (_, _) => CancelarAnalisisEnCurso();
            _btnCatalogo.Click += (_, _) => { using var f = new FormCatalogoMateriales(); f.ShowDialog(this); };
            _btnManual.Click += (_, _) => ClasificarManualmente();
            _btnConfirmarAbrir.Click += async (_, _) => await EjecutarAperturaSimuladaAsync();
            _btnBuscarTexto.Click += (_, _) => EjecutarBusquedaEscrita();
            _cajaBusqueda.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; EjecutarBusquedaEscrita(); } };

            FormClosing += (_, _) =>
            {
                _ctsAnalisis?.Cancel();
                _ctsAnalisis?.Dispose();
                _clienteVision.Dispose();
                _imagenActual?.Dispose();
            };

            ActualizarEstadoUi();
            RefrescarHistorial();
        }

        private async Task SeleccionarYAnalizarAsync()
        {
            using var dialogo = new OpenFileDialog
            {
                Title = "Seleccionar fotografía de residuo(s)",
                Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            byte[] bytes = File.ReadAllBytes(dialogo.FileName);
            await AnalizarBytesAsync(bytes, dialogo.FileName);
        }

        private async Task RepetirFotoAsync()
        {
            _gestor.CancelarDeposito("Usuario solicitó repetir fotografía");
            _ultimoAnalisis = null;
            _bytesActuales = null;
            _vistaPrevia.Image = null;
            _imagenActual?.Dispose();
            _imagenActual = null;
            _listaObjetos.Items.Clear();
            ResaltarCanecas(Array.Empty<CategoriaMaterial>());
            ActualizarEstadoUi();
            await SeleccionarYAnalizarAsync();
        }

        private async Task AnalizarBytesAsync(byte[] bytes, string nombreArchivo)
        {
            _ctsAnalisis?.Cancel();
            _ctsAnalisis?.Dispose();
            _ctsAnalisis = new CancellationTokenSource();

            _bytesActuales = bytes;
            _ultimoAnalisis = null;
            _bannerError.Visible = false;
            _etiquetaModo.Text = "Modo: DETECCIÓN VISUAL (" + Path.GetFileName(nombreArchivo) + ")";
            _btnCargarFoto.Enabled = false;
            _btnRepetir.Enabled = false;
            _btnConfirmarAbrir.Enabled = false;
            _btnCancelar.Enabled = true;

            try
            {
                using var ms = new MemoryStream(bytes);
                using var tmp = Image.FromStream(ms);
                _imagenActual?.Dispose();
                _imagenActual = new Bitmap(tmp);
                _vistaPrevia.Image = _imagenActual;
            }
            catch (Exception)
            {
                _vistaPrevia.Image = null;
            }

            _detalleTexto.Text = "Analizando imagen de forma asíncrona con Ollama... la interfaz permanece receptiva y puedes cancelar en cualquier momento.";
            ActualizarEstadoUi();

            try
            {
                // Ejecución asíncrona fuera del hilo UI para evitar congelamiento durante inferencias largas.
                ResultadoAnalisisMultirresiduo resultado = await Task.Run(
                    () => _gestor.AnalizarFotografiaAsync(bytes, _ctsAnalisis.Token),
                    _ctsAnalisis.Token);

                if (IsDisposed) return;
                _ultimoAnalisis = resultado;
                MostrarResultadoVisual(resultado);
            }
            catch (OperationCanceledException)
            {
                if (IsDisposed) return;
                _detalleTexto.Text = "Análisis cancelado por el usuario. No se emitió ninguna orden de apertura.";
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                MostrarError("Error controlado en el análisis visual: " + ex.Message);
            }
            finally
            {
                if (!IsDisposed)
                {
                    _btnCargarFoto.Enabled = true;
                    _btnRepetir.Enabled = _bytesActuales is not null;
                    _btnCancelar.Enabled = false;
                    ActualizarEstadoUi();
                }
            }
        }

        private void CancelarAnalisisEnCurso()
        {
            _ctsAnalisis?.Cancel();
            _gestor.CancelarDeposito("Cancelado desde botón de interfaz");
            _btnCancelar.Enabled = false;
            ActualizarEstadoUi();
        }

        private void MostrarResultadoVisual(ResultadoAnalisisMultirresiduo res)
        {
            _listaObjetos.Items.Clear();
            foreach (ObjetoClasificado35 o in res.Objetos)
            {
                var fila = new ListViewItem(o.Ordinal.ToString());
                fila.SubItems.Add(o.Material35?.Codigo ?? "—");
                fila.SubItems.Add(o.Material35?.Nombre ?? o.Observacion.NombreDetectado);
                fila.SubItems.Add(o.Estado.ToString());
                fila.SubItems.Add(o.CanecaAsignada.HasValue ? MotorReglasMultirresiduo.NombreCortoCaneca(o.CanecaAsignada.Value) : "NINGUNA");
                fila.SubItems.Add(o.Cantidad.ToString());
                fila.SubItems.Add(o.Motivo);
                _listaObjetos.Items.Add(fila);
            }

            _contador.Text = $"Objetos detectados: {res.TotalObjetosDetectados} · Canecas únicas requeridas: {res.CanecasRequeridas.Count} ({res.Combinacion}) · {res.LatenciaMs} ms";
            _detalleTexto.Text = res.MensajeUsuario;
            ResaltarCanecas(res.CanecasRequeridas);

            if (res.EstadoGlobal is EstadoGlobalAnalisis.ModoSeguroError or EstadoGlobalAnalisis.RequiereRepeticion)
                MostrarError(res.MensajeUsuario);
            else
                _bannerError.Visible = false;

            _btnConfirmarAbrir.Enabled = res.CanecasRequeridas.Count > 0 &&
                (_gestor.Maquina.EstadoActual is EstadoSesionDeposito.CLASIFICADO or EstadoSesionDeposito.REQUIERE_CONFIRMACION);
            _btnConfirmarAbrir.Text = _gestor.Maquina.EstadoActual == EstadoSesionDeposito.REQUIERE_CONFIRMACION
                ? "Confirmar y abrir (demo simulada)"
                : "Abrir canecas (demo simulada)";

            _vistaPrevia.Invalidate();
            ActualizarEstadoUi();

            if (!res.PuedeAbrirCanecas)
            {
                RegistrarOperacionLocal("Visual", res, Array.Empty<string>(),
                    res.EstadoGlobal == EstadoGlobalAnalisis.ModoSeguroError
                        ? EstadoOperacionLocal.FalloVision
                        : EstadoOperacionLocal.ClasificacionSoloOrientacion);
            }
        }

        private void EjecutarBusquedaEscrita()
        {
            ResultadoBusquedaPrioritaria busqueda = CatalogoPrioritario35.BuscarPorTexto(_cajaBusqueda.Text);
            _etiquetaModo.Text = "Modo: BÚSQUEDA ESCRITA (catálogo textual — no es detección visual)";
            _bannerError.Visible = false;
            _ultimoAnalisis = null;
            _listaObjetos.Items.Clear();

            int ord = 1;
            foreach (MaterialPrioritario35 m in busqueda.Materiales)
            {
                var fila = new ListViewItem(ord++.ToString());
                fila.SubItems.Add(m.Codigo);
                fila.SubItems.Add(m.Nombre);
                fila.SubItems.Add("Búsqueda escrita");
                fila.SubItems.Add(m.NombreCaneca);
                fila.SubItems.Add("1");
                _listaObjetos.Items.Add(fila);
            }

            MaterialPrioritario35? primero = busqueda.Materiales.FirstOrDefault();
            if (primero is not null)
            {
                ResaltarCanecas(new[] { primero.Caneca });
                IReadOnlyList<string> imgs = CatalogoPrioritario35.ImagenesValidasDe(primero);
                if (imgs.Count > 0)
                {
                    Image? img = RecursosEco.Imagen("residuos/" + imgs[0].Replace('.', '/') + ".jpg");
                    _vistaPrevia.Image = img;
                }
                else
                {
                    // Nunca inventar fotografía ni mostrar marco roto cuando falta recurso gráfico.
                    _vistaPrevia.Image = null;
                }
                _detalleTexto.Text = $"{busqueda.Mensaje}\n\n{primero.Descripcion}\nCondición: {primero.CondicionDisposicion}\n" +
                                     (imgs.Count > 0 ? $"Imágenes válidas disponibles: {imgs.Count}." : "Sin fotografía propia registrada todavía (no se inventa imagen).");
            }
            else
            {
                ResaltarCanecas(Array.Empty<CategoriaMaterial>());
                _vistaPrevia.Image = null;
                _detalleTexto.Text = busqueda.Mensaje;
            }

            _contador.Text = $"Búsqueda escrita · Coincidencias: {busqueda.Materiales.Count}";
            _btnConfirmarAbrir.Enabled = false;
        }

        private void ClasificarManualmente()
        {
            using var dialogo = new FormCatalogoMateriales();
            if (dialogo.ShowDialog(this) == DialogResult.OK && dialogo.Elegido is { } elegido)
            {
                _etiquetaModo.Text = "Modo: CLASIFICACIÓN MANUAL";
                ResaltarCanecas(new[] { elegido.Caneca });
                _detalleTexto.Text = $"Selección manual: {elegido.Nombre} → {CatalogoPrototipo.NombreCaneca(elegido.Caneca)}.\n{elegido.Instrucciones}";
                _colaLocal.Registrar(new OperacionPendienteLocal(
                    "manual-" + Guid.NewGuid().ToString("N"),
                    DateTime.UtcNow,
                    "Manual",
                    new[] { elegido.Id },
                    new[] { MotorReglasMultirresiduo.NombreCortoCaneca(elegido.Caneca) },
                    Array.Empty<string>(),
                    EstadoOperacionLocal.ClasificacionSoloOrientacion,
                    "Clasificación manual elegida por el usuario."));
                RefrescarHistorial();
            }
        }

        private async Task EjecutarAperturaSimuladaAsync()
        {
            if (_ultimoAnalisis is null || _ultimoAnalisis.CanecasRequeridas.Count == 0) return;

            if (_gestor.Maquina.EstadoActual == EstadoSesionDeposito.REQUIERE_CONFIRMACION)
            {
                var confirmacion = MessageBox.Show(
                    "Existen objetos que requieren confirmación humana sobre su estado (limpio/vacío).\n\n¿Confirmas que los residuos cumplen las condiciones para abrir en modo SIMULADO?",
                    "Confirmación requerida · EcoAccess",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirmacion != DialogResult.Yes) return;
                _gestor.ConfirmarManualmente(_ultimoAnalisis.CanecasRequeridas, "Confirmado por usuario en WinForms");
            }

            if (_gestor.Maquina.EstadoActual != EstadoSesionDeposito.CLASIFICADO) return;

            _btnConfirmarAbrir.Enabled = false;
            var estacionSimulada = new[]
            {
                new CanecaEstacion(1093, "Blanca (simulada)", CategoriaMaterial.Aprovechable, true, null, null),
                new CanecaEstacion(1400, "Verde (simulada)", CategoriaMaterial.OrganicoAprovechable, true, null, null),
                new CanecaEstacion(1401, "Negra (simulada)", CategoriaMaterial.NoAprovechable, true, null, null),
            };

            string idOp = "ui-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            ModoAperturaCanecas modo = _selectorApertura.SelectedIndex == 1
                ? ModoAperturaCanecas.SimultaneaSupervisada
                : ModoAperturaCanecas.SecuencialGuiada;
            var aperturas = await _gestor.EjecutarAperturaSecuencialAsync(idOp, estacionSimulada, permitirSoloSimulador: true, modoApertura: modo);

            string[] confirmadas = aperturas.Where(a => a.AperturaConfirmada).Select(a => MotorReglasMultirresiduo.NombreCortoCaneca(a.Caneca)).ToArray();
            _detalleTexto.Text = $"Apertura ({modo} · SIMULADOR MODO 1 — sin movimiento físico):\n" +
                                 string.Join("\n", aperturas.Select(a => $"- {MotorReglasMultirresiduo.NombreCortoCaneca(a.Caneca)}: {a.Mensaje}"));

            RegistrarOperacionLocal("Visual", _ultimoAnalisis, confirmadas,
                confirmadas.Length > 0 ? EstadoOperacionLocal.AperturaConfirmadaSimulada : EstadoOperacionLocal.FalloHardware);
            ActualizarEstadoUi();
        }

        private void CambiarModeloVisual(string modeloNuevo)
        {
            _ctsAnalisis?.Cancel();
            var anterior = _clienteVision;
            _clienteVision = new ClienteVisionOllamaMultirresiduo(modelo: modeloNuevo);
            _gestor = new GestorSesionMultirresiduo(_clienteVision, new MotorReglasMultirresiduo(), _simuladorEstacion, reloj: TimeProvider.System);
            anterior.Dispose();
            _detalleTexto.Text = $"Modelo visual seleccionado: {modeloNuevo}. Las fotografías permanecen locales en Ollama (127.0.0.1).";
            ActualizarEstadoUi();
        }

        private void RegistrarOperacionLocal(string modo, ResultadoAnalisisMultirresiduo res, IReadOnlyList<string> confirmadas, EstadoOperacionLocal estado)
        {
            var codigos = res.Objetos.Where(o => o.Material35 is not null).Select(o => o.Material35!.Codigo).Distinct().ToList();
            var solicitadas = res.CanecasRequeridas.Select(MotorReglasMultirresiduo.NombreCortoCaneca).ToList();
            _colaLocal.Registrar(new OperacionPendienteLocal(
                "op-" + Guid.NewGuid().ToString("N").Substring(0, 16),
                DateTime.UtcNow,
                modo,
                codigos,
                solicitadas,
                confirmadas,
                estado,
                $"Comb={res.Combinacion}; Modelo={res.ModeloUtilizado}"));
            RefrescarHistorial();
        }

        private void RefrescarHistorial()
        {
            _listaHistorial.Items.Clear();
            foreach (OperacionPendienteLocal op in _colaLocal.ListarTodas().OrderByDescending(o => o.FechaUtc).Take(25))
            {
                var item = new ListViewItem(op.FechaUtc.ToString("HH:mm:ss"));
                item.SubItems.Add(op.ModoOrigen);
                item.SubItems.Add(op.CodigosMaterial35.Count > 0 ? string.Join(",", op.CodigosMaterial35) : "—");
                item.SubItems.Add(op.CanecasSolicitadas.Count > 0 ? string.Join("+", op.CanecasSolicitadas) : "NINGUNA");
                item.SubItems.Add(op.SincronizadoOracle ? $"{op.Estado} (Oracle)" : $"{op.Estado} (Local)");
                _listaHistorial.Items.Add(item);
            }
        }

        private void DibujarEtiquetasVisuales(object? sender, PaintEventArgs e)
        {
            if (_ultimoAnalisis is null || _ultimoAnalisis.Objetos.Count == 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int y = 10;
            foreach (ObjetoClasificado35 o in _ultimoAnalisis.Objetos.Take(6))
            {
                string etiqueta = $"{(o.Material35?.Codigo ?? "?")} · {(o.Material35?.Nombre ?? o.Observacion.NombreDetectado)} → {(o.CanecaAsignada.HasValue ? MotorReglasMultirresiduo.NombreCortoCaneca(o.CanecaAsignada.Value) : o.Estado.ToString())}";
                SizeF sz = e.Graphics.MeasureString(etiqueta, TemaEcoAccess.Pequena);
                var rect = new RectangleF(10, y, Math.Min(_vistaPrevia.Width - 20, sz.Width + 16), sz.Height + 8);
                Color fondo = o.CanecaAsignada switch
                {
                    CategoriaMaterial.OrganicoAprovechable => Color.FromArgb(215, 46, 125, 50),
                    CategoriaMaterial.Aprovechable => Color.FromArgb(225, 55, 71, 79),
                    CategoriaMaterial.NoAprovechable => Color.FromArgb(225, 33, 33, 33),
                    _ => Color.FromArgb(220, 183, 28, 28),
                };
                using var brocha = new SolidBrush(fondo);
                e.Graphics.FillRectangle(brocha, rect);
                e.Graphics.DrawString(etiqueta, TemaEcoAccess.Pequena, Brushes.White, rect.X + 8, rect.Y + 4);
                y += (int)rect.Height + 6;
            }
        }

        private void ResaltarCanecas(IReadOnlyCollection<CategoriaMaterial> activas)
        {
            AplicarEstiloCaneca(_tarjetaVerde, activas.Contains(CategoriaMaterial.OrganicoAprovechable), Color.FromArgb(46, 125, 50));
            AplicarEstiloCaneca(_tarjetaBlanca, activas.Contains(CategoriaMaterial.Aprovechable), Color.FromArgb(55, 71, 79));
            AplicarEstiloCaneca(_tarjetaNegra, activas.Contains(CategoriaMaterial.NoAprovechable), Color.FromArgb(33, 33, 33));
        }

        private static Panel CrearTarjetaCaneca(string titulo, string subtitulo, Color acento)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4), Padding = new Padding(10), BackColor = Color.White };
            var l1 = new Label { Text = titulo, Dock = DockStyle.Top, AutoSize = true, Font = TemaEcoAccess.Subtitulo, ForeColor = acento };
            var l2 = new Label { Text = subtitulo, Dock = DockStyle.Top, AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave };
            var l3 = new Label { Name = "estado", Text = "Inactiva", Dock = DockStyle.Bottom, AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave };
            p.Controls.Add(l3);
            p.Controls.Add(l2);
            p.Controls.Add(l1);
            return p;
        }

        private static void AplicarEstiloCaneca(Panel tarjeta, bool activa, Color colorActivo)
        {
            tarjeta.BackColor = activa ? colorActivo : Color.White;
            foreach (Control c in tarjeta.Controls)
            {
                c.ForeColor = activa ? Color.White : (c.Name == "estado" ? TemaEcoAccess.Suave : TemaEcoAccess.Texto);
                if (c.Name == "estado")
                    c.Text = activa ? "✔ REQUERIDA" : "Inactiva";
            }
        }

        private void MostrarError(string mensaje)
        {
            _bannerError.Text = "⚠️ " + mensaje;
            _bannerError.Visible = true;
        }

        private void ActualizarEstadoUi()
        {
            _estadoMaquina.Text = $"Estado sesión: {_gestor.Maquina.EstadoActual}";
        }
    }
}
