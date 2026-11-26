using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Registro asistido: el operador busca al ciudadano y el residuo, verifica la caneca, solicita la apertura, sigue la
    /// operación y confirma la recepción. Toda regla vive en la lógica; este formulario solo la presenta.
    /// </summary>
    public sealed partial class FormRegistroManual : Form
    {
        private static readonly TimeSpan Espera = TimeSpan.FromSeconds(2);
        private const int AnchoColumna = 560;

        private readonly ServiciosAplicacion _servicios;
        private readonly int _idOperador;
        private readonly CajaTexto _buscarCiudadano = new CajaTexto { PlaceholderText = "Nombre o correo del ciudadano", AccessibleName = "Buscar ciudadano" };
        private readonly ListBox _ciudadanos = new ListBox { DisplayMember = nameof(Opcion.Texto), Height = 110, AccessibleName = "Ciudadanos encontrados" };
        private readonly ComboBox _canecas = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Opcion.Texto), AccessibleName = "Caneca" };
        private readonly CajaTexto _buscarResiduo = new CajaTexto { PlaceholderText = "Escribe el residuo y pulsa Enter: botella, cartón, pila…", AccessibleName = "Buscar residuo" };
        private readonly KryptonButton _botonBuscar = new KryptonButton { Text = "Buscar" };
        private readonly ListBox _residuos = new ListBox { DrawMode = DrawMode.OwnerDrawFixed, Height = 220, IntegralHeight = false, AccessibleName = "Materiales encontrados" };
        private readonly Label _estadoBusqueda = new Label { AutoSize = true, ForeColor = TemaEcoAccess.Suave };
        private readonly Label _orientacion = new Label { AutoSize = true, ForeColor = TemaEcoAccess.Suave };
        private readonly Label _resumen = new Label { AutoSize = true, ForeColor = TemaEcoAccess.Texto, Text = "Selecciona ciudadano, caneca y material." };
        private readonly KryptonButton _abrir = new KryptonButton { Text = "Solicitar apertura", Enabled = false };
        private readonly KryptonButton _registrar = new KryptonButton { Text = "Registrar ciudadano nuevo" };
        private readonly Label _estado = new Label { AutoSize = true, Font = TemaEcoAccess.Subtitulo };
        private readonly NumericUpDown _cantidad = new NumericUpDown { Minimum = 1, Maximum = 1000, Value = 1, Width = 90, AccessibleName = "Cantidad recibida" };
        private readonly KryptonButton _confirmar = new KryptonButton { Text = "Confirmar recepción", Enabled = false };
        private readonly System.Windows.Forms.Timer _esperaResiduo = new System.Windows.Forms.Timer { Interval = 350 };
        private readonly System.Windows.Forms.Timer _esperaCiudadano = new System.Windows.Forms.Timer { Interval = 350 };
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
        private readonly Dictionary<int, Contenedor> _datosCanecas = new Dictionary<int, Contenedor>();
        /// <summary>Catálogo habilitado (id → material): con él se sabe qué tipo de caneca es cada una y qué residuos recibe.</summary>
        private readonly Dictionary<int, Material> _catalogo = new Dictionary<int, Material>();
        private CancellationTokenSource? _consultaResiduo;
        private int _busquedaResiduo;
        private CancellationTokenSource? _consultaCiudadano;
        private int? _idDeposito;

        public FormRegistroManual(ServiciosAplicacion servicios, int idOperador)
        {
            _servicios = servicios ?? throw new ArgumentNullException(nameof(servicios));
            _idOperador = idOperador;
            Text = "Registro manual";
            BackColor = TemaEcoAccess.Fondo;
            Font = TemaEcoAccess.Cuerpo;
            AutoScroll = true;
            TemaEcoAccess.EstilizarAccion(_abrir, true);
            TemaEcoAccess.EstilizarAccion(_registrar, false);
            TemaEcoAccess.EstilizarAccion(_confirmar, true);
            TemaEcoAccess.EstilizarAccion(_botonBuscar, true);
            _residuos.ItemHeight = TemaEcoAccess.Px(this, 46);
            // Altura suficiente para el texto del botón con cualquier escala.
            foreach (KryptonButton boton in new[] { _abrir, _registrar, _confirmar, _botonBuscar }) boton.Height = TemaEcoAccess.Px(this, 44);

            // Disposición Eco (banner, selector visual de canecas, residuo, ciudadano y confirmación): FormRegistroManual.Diseno.cs.
            ConstruirDiseno();

            _residuos.DrawItem += DibujarResiduo;
            _esperaResiduo.Tick += async (_, _) => { _esperaResiduo.Stop(); await BuscarResiduosAsync(); };
            _esperaCiudadano.Tick += async (_, _) => { _esperaCiudadano.Stop(); await BuscarCiudadanosAsync(); };
            _buscarResiduo.TextChanged += (_, _) => { _esperaResiduo.Stop(); _esperaResiduo.Start(); };
            _buscarCiudadano.TextChanged += (_, _) => { _esperaCiudadano.Stop(); _esperaCiudadano.Start(); };
            _buscarResiduo.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _esperaResiduo.Stop();
                    await BuscarResiduosAsync();
                }
                else if (e.KeyCode == Keys.Down && _residuos.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    _residuos.Focus();
                    if (_residuos.SelectedIndex < 0) _residuos.SelectedIndex = 0;
                }
            };
            _residuos.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                _buscarCiudadano.Focus();
            };
            _botonBuscar.Click += async (_, _) => { _esperaResiduo.Stop(); await BuscarResiduosAsync(); };
            _buscarCiudadano.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                _esperaCiudadano.Stop();
                await BuscarCiudadanosAsync();
            };
            _residuos.SelectedIndexChanged += (_, _) => MostrarOrientacion();
            // Al cambiar de caneca la lista muestra solo lo que esa caneca recibe (la búsqueda se repite con el mismo texto).
            _canecas.SelectedIndexChanged += async (_, _) => { _residuos.Invalidate(); MostrarOrientacion(); if (_catalogo.Count > 0) await BuscarResiduosAsync(); };
            _ciudadanos.SelectedIndexChanged += (_, _) => MostrarOrientacion();
            _abrir.Click += async (_, _) => await SolicitarAperturaAsync();
            _confirmar.Click += async (_, _) => await ConfirmarRecepcionAsync();
            _registrar.Click += (_, _) => RegistrarCiudadano();
            Load += async (_, _) => await CargarAsync();
            FormClosed += (_, _) =>
            {
                _esperaResiduo.Stop();
                _esperaCiudadano.Stop();
                _esperaResiduo.Dispose();
                _esperaCiudadano.Dispose();
                _consultaResiduo?.Cancel();
                _consultaCiudadano?.Cancel();
                _cierre.Cancel();
                _cierre.Dispose();
            };
        }

        private sealed record Opcion(int Id, string Texto);

        private async Task CargarAsync()
        {
            try
            {
                // Primero el catálogo: define el tipo de cada caneca (verde, blanca o negra) y los residuos que se ofrecen.
                foreach (Material material in await _servicios.CatalogoAmbiental.ListarHabilitadosAsync(_cierre.Token)) _catalogo[material.Id] = material;
                IReadOnlyList<Contenedor> canecas = await _servicios.Contenedores.ListarAsync(_cierre.Token);
                foreach (Contenedor caneca in canecas) _datosCanecas[caneca.Id] = caneca;
                _canecas.DataSource = canecas.Where(c => c.EstaEnServicio).Select(c => new Opcion(c.Id, c.Nombre)).ToList();
            }
            catch (OperationCanceledException) { return; }
            catch (Exception)
            {
                // Si el módulo ya se cerró (el operario cambió de módulo), no hay nada que mostrar; antes el error escapaba y
                // llegaba a la aplicación.
                if (IsDisposed) return;
                MostrarEstado("No pudimos consultar las canecas. Comprueba la conexión con Oracle y vuelve a abrir este módulo.", TemaEcoAccess.Error);
            }
            if (IsDisposed) return;
            await BuscarCiudadanosAsync();
            await BuscarResiduosAsync();
        }

        private async Task BuscarCiudadanosAsync()
        {
            _consultaCiudadano?.Cancel();
            using var consulta = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
            _consultaCiudadano = consulta;
            try
            {
                var r = await _servicios.Usuarios.BuscarCiudadanosAsync(_idOperador, _buscarCiudadano.Text, consulta.Token);
                if (consulta.IsCancellationRequested) return;
                if (r.Exitoso) _ciudadanos.DataSource = r.Valor.Filas.Select(u => new Opcion(u.Id, $"{u.Nombre} · {u.CorreoElectronico}")).ToList();
                else MostrarEstado(r.Mensaje!, TemaEcoAccess.Error);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { if (!IsDisposed) MostrarEstado("No pudimos buscar ciudadanos. Vuelve a intentarlo.", TemaEcoAccess.Error); }
            finally { if (ReferenceEquals(_consultaCiudadano, consulta)) _consultaCiudadano = null; }
        }

        /// <summary>Cada búsqueda cancela la anterior: una respuesta lenta nunca reemplaza a una más reciente.</summary>
        private async Task BuscarResiduosAsync()
        {
            _consultaResiduo?.Cancel();
            using var consulta = CancellationTokenSource.CreateLinkedTokenSource(_cierre.Token);
            _consultaResiduo = consulta;
            // Número de esta búsqueda: si al volver ya empezó otra (por ejemplo, al cambiar de caneca rápido), esta no toca la
            // lista ni el mensaje. La cancelación sola no bastaba: el catálogo a veces responde sin atenderla.
            int numero = ++_busquedaResiduo;
            Opcion? elegida = _canecas.SelectedItem as Opcion;
            string texto = _buscarResiduo.Text;
            _estadoBusqueda.ForeColor = TemaEcoAccess.Suave;
            _estadoBusqueda.Text = "Consultando el catálogo…";
            try
            {
                IReadOnlyList<Material> encontrados = Compat.IsNullOrWhiteSpace(texto)
                    ? await _servicios.CatalogoAmbiental.ListarHabilitadosAsync(consulta.Token)
                    : await _servicios.CatalogoAmbiental.BuscarAsync(texto, consulta.Token);
                // La respuesta solo vale si sigue siendo la última búsqueda y la caneca no cambió mientras se esperaba.
                if (consulta.IsCancellationRequested || numero != _busquedaResiduo || IsDisposed || (_canecas.SelectedItem as Opcion)?.Id != elegida?.Id) return;
                // Solo residuos de las canecas verde, blanca y negra: sin peligrosos y sin los que ninguna caneca en servicio
                // recibe. Con una caneca elegida, solo los que esa caneca admite (antes aparecían como «No admitido»).
                List<Material> materiales = encontrados.Where(m => SeOfreceEnRegistro(m, elegida)).ToList();
                int? anterior = (_residuos.SelectedItem as Material)?.Id;
                _residuos.BeginUpdate();
                _residuos.Items.Clear();
                _residuos.Items.AddRange(materiales.Cast<object>().ToArray());
                _residuos.EndUpdate();
                int conservado = anterior is int id ? materiales.FindIndex(m => m.Id == id) : -1;
                if (materiales.Count > 0) _residuos.SelectedIndex = Math.Max(0, conservado);
                string dondeBusca = elegida is not null ? $"que recibe {elegida.Texto}" : "que reciben las canecas";
                _estadoBusqueda.Text = materiales.Count == 0
                    ? (Compat.IsNullOrWhiteSpace(texto) ? $"No hay residuos {dondeBusca}." : $"Sin coincidencias para «{texto.Trim()}» entre los residuos {dondeBusca}. Prueba con otro nombre, como «botella» o «cartón».")
                    : Compat.IsNullOrWhiteSpace(texto)
                        ? $"{materiales.Count} {(materiales.Count == 1 ? "residuo" : "residuos")} {dondeBusca}. Flecha abajo para recorrerlos."
                        : $"{materiales.Count} {(materiales.Count == 1 ? "coincidencia" : "coincidencias")} {dondeBusca}. Flecha abajo para recorrerlas.";
                if (materiales.Count == 0) MostrarOrientacion();
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (IsDisposed) return;
                _estadoBusqueda.ForeColor = TemaEcoAccess.Error;
                _estadoBusqueda.Text = "El catálogo no respondió. Pulsa Buscar para reintentar.";
            }
            finally { if (ReferenceEquals(_consultaResiduo, consulta)) _consultaResiduo = null; }
        }

        /// <summary>
        /// Residuo que se puede registrar aquí: no peligroso (esos van por ruta especial, no a las canecas) y admitido por la
        /// caneca elegida o, sin caneca elegida, por alguna caneca en servicio.
        /// </summary>
        private bool SeOfreceEnRegistro(Material material, Opcion? caneca)
        {
            if (material.Categoria == CategoriaMaterial.Peligroso) return false;
            if (caneca is not null && _datosCanecas.TryGetValue(caneca.Id, out Contenedor? elegida)) return elegida.AdmiteMaterial(material.Id);
            return _datosCanecas.Values.Any(c => c.EstaEnServicio && c.AdmiteMaterial(material.Id));
        }

        private bool? AdmiteCaneca(Material material) =>
            _canecas.SelectedItem is Opcion caneca && _datosCanecas.TryGetValue(caneca.Id, out Contenedor? datos) ? datos.AdmiteMaterial(material.Id) : (bool?)null;

        private void DibujarResiduo(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || _residuos.Items[e.Index] is not Material material) return;
            bool seleccionado = (e.State & DrawItemState.Selected) != 0;
            using (var fondo = new SolidBrush(seleccionado ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Superficie)) e.Graphics.FillRectangle(fondo, e.Bounds);
            var estilo = EstiloCategoria.Para(material.Categoria);
            int lado = e.Bounds.Height - TemaEcoAccess.Px(this, 12);
            var icono = new Rectangle(e.Bounds.X + TemaEcoAccess.Px(this, 6), e.Bounds.Y + TemaEcoAccess.Px(this, 6), lado, lado);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            DibujoEco.Icono(e.Graphics, estilo.Icono, estilo.Tono, estilo.Fondo, icono, TemaEcoAccess.Px(this, 8));
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            int x = icono.Right + TemaEcoAccess.Px(this, 10), ancho = e.Bounds.Right - x - TemaEcoAccess.Px(this, 6);
            TextFormatFlags corte = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            int altoLinea = e.Bounds.Height / 2;
            TextRenderer.DrawText(e.Graphics, material.Nombre, TemaEcoAccess.Subtitulo, new Rectangle(x, e.Bounds.Y + TemaEcoAccess.Px(this, 3), ancho, altoLinea), TemaEcoAccess.Texto, corte);
            bool? admitido = AdmiteCaneca(material);
            string admision = admitido is null ? "Selecciona una caneca" : admitido.Value ? "Admitido en esta caneca" : "No admitido en esta caneca";
            Color tono = admitido == false ? TemaEcoAccess.Error : admitido == true ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Suave;
            TextRenderer.DrawText(e.Graphics, $"{estilo.Destino} · {admision}", TemaEcoAccess.Pequena, new Rectangle(x, e.Bounds.Y + altoLinea, ancho, altoLinea), tono, corte);
            e.DrawFocusRectangle();
        }

        /// <summary>Reconocer un material no autoriza depositarlo: la caneca elegida define si se puede continuar.</summary>
        private void MostrarOrientacion()
        {
            _orientacion.MaximumSize = new Size(TemaEcoAccess.Px(this, AnchoColumna), 0);
            if (_residuos.SelectedItem is not Material material || _canecas.SelectedItem is not Opcion caneca)
            {
                _orientacion.Text = "";
                _abrir.Enabled = false;
                _resumen.Text = "Selecciona ciudadano, caneca y material.";
                return;
            }
            bool admitido = AdmiteCaneca(material) == true;
            _abrir.Enabled = admitido;
            _orientacion.ForeColor = admitido ? TemaEcoAccess.VerdeOscuro : TemaEcoAccess.Error;
            _orientacion.Text = admitido
                ? (material.Descripcion ?? "Esta caneca admite el material.")
                : $"Esta caneca no lo admite, así que no se abrirá. {material.Descripcion}";
            string ciudadano = _ciudadanos.SelectedItem is Opcion persona ? persona.Texto : "Sin ciudadano seleccionado";
            _resumen.MaximumSize = new Size(TemaEcoAccess.Px(this, AnchoColumna), 0);
            _resumen.Text = $"{material.Nombre} · {caneca.Texto}\n{ciudadano}";
        }

        private void RegistrarCiudadano()
        {
            using var registro = new FormRegistroCiudadano(_servicios.RegistroCiudadanos);
            registro.ShowDialog(this);
            _ = BuscarCiudadanosAsync();
        }

        private async Task SolicitarAperturaAsync()
        {
            if (_ciudadanos.SelectedItem is not Opcion ciudadano || _canecas.SelectedItem is not Opcion caneca || _residuos.SelectedItem is not Material material)
            {
                MostrarEstado("Selecciona ciudadano, caneca y material.", TemaEcoAccess.Advertencia);
                return;
            }

            _abrir.Enabled = false;
            _idDeposito = null;
            _confirmar.Enabled = false;
            Resultado<int> orden;
            try { orden = await _servicios.Interacciones.RegistrarAsistidoAsync(_idOperador, ciudadano.Id, caneca.Id, material.Id, _cierre.Token); }
            catch (OperationCanceledException) { return; }
            catch (Exception)
            {
                if (IsDisposed) return;
                _abrir.Enabled = true;
                MostrarEstado("No se pudo confirmar la solicitud. Revisa los depósitos antes de repetirla.", TemaEcoAccess.Error);
                return;
            }
            if (!orden.Exitoso)
            {
                _abrir.Enabled = true;
                MostrarEstado(orden.Mensaje!, TemaEcoAccess.Error);
                return;
            }

            MostrarEstado("Solicitud registrada. Abriendo caneca…", TemaEcoAccess.Info);
            // La orden se ejecuta en el servicio central de hardware (panel ECOACCESS ESP32). Antes solo quedaba en Oracle
            // esperando a un bridge que ya no existe y vencía sin mover la tapa.
            if (_servicios.EjecutorEstacion is { } crearEjecutor)
            {
                ResultadoEjecucionOrden ejecucion;
                try { ejecucion = (await (await crearEjecutor()).EjecutarAsync(orden.Valor, "operario", $"operario {_idOperador}", material.Nombre, _cierre.Token)).Resultado; }
                catch (OperationCanceledException) { return; }
                catch (Exception error)
                {
                    if (IsDisposed) return;
                    _abrir.Enabled = true;
                    MostrarEstado("No fue posible abrir la caneca (" + error.GetType().Name + "). La solicitud quedó registrada para revisión.", TemaEcoAccess.Error);
                    return;
                }
                if (IsDisposed) return;
                if (!ejecucion.ComandoConfirmado)
                {
                    _abrir.Enabled = true;
                    MostrarEstado("No fue posible abrir la caneca: " + ejecucion.Mensaje, TemaEcoAccess.Error);
                    return;
                }
                MostrarEstado(ejecucion.Mensaje + " (el ESP32 confirmó la orden)", TemaEcoAccess.Verde);
            }
            try { await SeguirAsync(orden.Valor); }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (IsDisposed) return;
                _abrir.Enabled = true;
                MostrarEstado("Se perdió la consulta del estado. Revisa la orden en Depósitos antes de repetirla.", TemaEcoAccess.Error);
            }
        }

        /// <summary>Solo muestra hechos registrados en Oracle por el servicio de dispositivos.</summary>
        private async Task SeguirAsync(int idOrden)
        {
            DateTime limite = DateTime.UtcNow + ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(40);
            while (DateTime.UtcNow < limite)
            {
                await Task.Delay(Espera, _cierre.Token);
                var r = await _servicios.Interacciones.ConsultarOperacionAsync(_idOperador, idOrden, _cierre.Token);
                if (!r.Exitoso) { MostrarEstado(r.Mensaje!, TemaEcoAccess.Error); return; }
                if (r.Valor.Deposito is { } deposito)
                {
                    _idDeposito = deposito.Id;
                    _confirmar.Enabled = !deposito.EstaVerificado;
                    MostrarEstado("La tapa se cerró. Depósito pendiente de verificación: confirma lo recibido.", TemaEcoAccess.Advertencia);
                    return;
                }
                switch (r.Valor.Orden.Estado)
                {
                    case EstadoOrdenApertura.Abierta:
                        MostrarEstado($"Caneca abierta. Puedes desechar tu residuo. Ventana de {MensajesTelegram.VentanaDepositoSegundos} segundos; se cerrará sola.", TemaEcoAccess.Verde);
                        break;
                    case EstadoOrdenApertura.Fallida:
                        MostrarEstado("La caneca informó un fallo. No se registró ningún depósito.", TemaEcoAccess.Error);
                        _abrir.Enabled = true;
                        return;
                    case EstadoOrdenApertura.Cerrada:
                        MostrarEstado("La caneca se cerró sin registrar un depósito.", TemaEcoAccess.Advertencia);
                        _abrir.Enabled = true;
                        return;
                }
            }
            MostrarEstado("La caneca no respondió a tiempo. Comprueba el servicio de dispositivos.", TemaEcoAccess.Error);
            _abrir.Enabled = true;
        }

        private async Task ConfirmarRecepcionAsync()
        {
            if (_idDeposito is not int id || _residuos.SelectedItem is not Material material) return;
            _confirmar.Enabled = false;
            try
            {
                var r = await _servicios.Interacciones.VerificarDepositoAsync(_idOperador, id, material.Id, _cierre.Token, (int)_cantidad.Value);
                MostrarEstado(r.Exitoso ? "Recepción confirmada. Los ecopuntos se acreditan desde el módulo Ecopuntos." : r.Mensaje!, r.Exitoso ? TemaEcoAccess.Verde : TemaEcoAccess.Error);
                if (!r.Exitoso) _confirmar.Enabled = true; else _abrir.Enabled = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (IsDisposed) return;
                _confirmar.Enabled = true;
                MostrarEstado("No se pudo confirmar la recepción. Revisa el depósito antes de repetirla.", TemaEcoAccess.Error);
            }
        }

        private void MostrarEstado(string texto, Color color)
        {
            _estado.MaximumSize = new Size(TemaEcoAccess.Px(this, AnchoColumna), 0);
            _estado.Text = texto;
            _estado.ForeColor = color;
        }
    }
}
