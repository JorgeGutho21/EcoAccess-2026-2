using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Registro asistido: el operador busca al ciudadano, elige la caneca y el residuo, solicita la apertura, sigue la
    /// operación y confirma la recepción. Toda regla vive en la lógica; este formulario solo la presenta.
    /// </summary>
    public sealed class FormRegistroManual : Form
    {
        private static readonly TimeSpan Espera = TimeSpan.FromSeconds(2);

        private readonly ServiciosAplicacion _servicios;
        private readonly int _idOperador;
        private readonly CajaTexto _buscarCiudadano = new CajaTexto { PlaceholderText = "Nombre o correo del ciudadano", AccessibleName = "Buscar ciudadano" };
        private readonly ListBox _ciudadanos = new ListBox { DisplayMember = nameof(Opcion.Texto), Height = 110, AccessibleName = "Ciudadanos encontrados" };
        private readonly ComboBox _canecas = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Opcion.Texto), AccessibleName = "Caneca" };
        private readonly CajaTexto _buscarResiduo = new CajaTexto { PlaceholderText = "Escribe el residuo: botella, cartón, pila…", AccessibleName = "Buscar residuo" };
        private readonly ListBox _residuos = new ListBox { DisplayMember = nameof(Opcion.Texto), Height = 110, AccessibleName = "Materiales del catálogo" };
        private readonly Label _orientacion = new Label { AutoSize = true, ForeColor = TemaEcoAccess.Suave };
        private readonly KryptonButton _abrir = new KryptonButton { Text = "Solicitar apertura" };
        private readonly KryptonButton _registrar = new KryptonButton { Text = "Registrar ciudadano nuevo" };
        private readonly Label _estado = new Label { AutoSize = true, Font = TemaEcoAccess.Subtitulo };
        private readonly NumericUpDown _cantidad = new NumericUpDown { Minimum = 1, Maximum = 1000, Value = 1, Width = 90, AccessibleName = "Cantidad recibida" };
        private readonly KryptonButton _confirmar = new KryptonButton { Text = "Confirmar recepción", Enabled = false };
        private readonly CancellationTokenSource _cierre = new CancellationTokenSource();
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

            var tabla = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(16) };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tabla.Controls.Add(Seccion("1. Ciudadano", _buscarCiudadano, _ciudadanos, _registrar), 0, 0);
            tabla.Controls.Add(Seccion("2. Caneca", _canecas), 1, 0);
            tabla.Controls.Add(Seccion("3. Residuo", _buscarResiduo, _residuos, _orientacion), 0, 1);
            var cantidad = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
            cantidad.Controls.Add(new Label { Text = "Cantidad recibida:", AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
            cantidad.Controls.Add(_cantidad);
            tabla.Controls.Add(Seccion("4. Apertura y recepción", _abrir, _estado, cantidad, _confirmar), 1, 1);
            Controls.Add(tabla);

            _buscarCiudadano.TextChanged += async (_, _) => await BuscarCiudadanosAsync();
            _buscarResiduo.TextChanged += async (_, _) => await BuscarResiduosAsync();
            _residuos.SelectedIndexChanged += async (_, _) => await MostrarOrientacionAsync();
            _canecas.SelectedIndexChanged += async (_, _) => await MostrarOrientacionAsync();
            _abrir.Click += async (_, _) => await SolicitarAperturaAsync();
            _confirmar.Click += async (_, _) => await ConfirmarRecepcionAsync();
            _registrar.Click += (_, _) => RegistrarCiudadano();
            Load += async (_, _) => await CargarAsync();
            FormClosed += (_, _) => { _cierre.Cancel(); _cierre.Dispose(); };
        }

        private sealed record Opcion(int Id, string Texto);

        private static Control Seccion(string titulo, params Control[] controles)
        {
            var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Superficie, Padding = new Padding(14), Margin = new Padding(8) };
            panel.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(0, 0, 0, 8) });
            foreach (Control control in controles)
            {
                if (control is not Label) control.Width = 380;
                control.Margin = new Padding(0, 0, 0, 8);
                panel.Controls.Add(control);
            }
            return panel;
        }

        private async Task CargarAsync()
        {
            IReadOnlyList<Contenedor> canecas = await _servicios.Contenedores.ListarAsync(_cierre.Token);
            _canecas.DataSource = canecas.Where(c => c.EstaEnServicio).Select(c => new Opcion(c.Id, c.Nombre)).ToList();
            await BuscarCiudadanosAsync();
            await BuscarResiduosAsync();
        }

        private async Task BuscarCiudadanosAsync()
        {
            string texto = _buscarCiudadano.Text;
            var r = await _servicios.Usuarios.BuscarCiudadanosAsync(_idOperador, texto, _cierre.Token);
            if (texto != _buscarCiudadano.Text) return;
            if (r.Exitoso) _ciudadanos.DataSource = r.Valor.Filas.Select(u => new Opcion(u.Id, $"{u.Nombre} · {u.CorreoElectronico}")).ToList();
            else MostrarEstado(r.Mensaje!, TemaEcoAccess.Error);
        }

        private async Task BuscarResiduosAsync()
        {
            string texto = _buscarResiduo.Text;
            IReadOnlyList<Material> materiales = Compat.IsNullOrWhiteSpace(texto)
                ? await _servicios.CatalogoAmbiental.ListarHabilitadosAsync(_cierre.Token)
                : await _servicios.CatalogoAmbiental.BuscarAsync(texto, _cierre.Token);
            if (texto == _buscarResiduo.Text) _residuos.DataSource = materiales.Select(m => new Opcion(m.Id, m.Nombre)).ToList();
        }

        /// <summary>Reconocer un material no autoriza depositarlo: la lógica informa si esta caneca lo admite.</summary>
        private async Task MostrarOrientacionAsync()
        {
            if (_residuos.SelectedItem is not Opcion material || _canecas.SelectedItem is not Opcion caneca) return;
            var r = await _servicios.CatalogoAmbiental.ConsultarAsync(material.Id, caneca.Id, _cierre.Token);
            bool admitido = r.Exitoso && r.Valor.Admision == AdmisionMaterial.Admitido;
            _abrir.Enabled = admitido;
            _orientacion.MaximumSize = new System.Drawing.Size(380, 0);
            _orientacion.Text = !r.Exitoso ? r.Mensaje : admitido ? "Esta caneca admite el material." : "Esta caneca no lo admite. " + r.Valor.Material.Descripcion;
        }

        private void RegistrarCiudadano()
        {
            using var registro = new FormRegistroCiudadano(_servicios.RegistroCiudadanos);
            registro.ShowDialog(this);
            _ = BuscarCiudadanosAsync();
        }

        private async Task SolicitarAperturaAsync()
        {
            if (_ciudadanos.SelectedItem is not Opcion ciudadano || _canecas.SelectedItem is not Opcion caneca || _residuos.SelectedItem is not Opcion material)
            {
                MostrarEstado("Selecciona ciudadano, caneca y material.", TemaEcoAccess.Advertencia);
                return;
            }

            _abrir.Enabled = false;
            _idDeposito = null;
            _confirmar.Enabled = false;
            var orden = await _servicios.Interacciones.RegistrarAsistidoAsync(_idOperador, ciudadano.Id, caneca.Id, material.Id, _cierre.Token);
            if (!orden.Exitoso)
            {
                _abrir.Enabled = true;
                MostrarEstado(orden.Mensaje!, TemaEcoAccess.Error);
                return;
            }

            MostrarEstado("Orden emitida. Esperando a la caneca…", TemaEcoAccess.Info);
            try { await SeguirAsync(orden.Valor); }
            catch (OperationCanceledException) { }
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
                        MostrarEstado($"La caneca está abierta. Ventana de {MensajesTelegram.VentanaDepositoSegundos} segundos.", TemaEcoAccess.Verde);
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
            if (_idDeposito is not int id || _residuos.SelectedItem is not Opcion material) return;
            _confirmar.Enabled = false;
            var r = await _servicios.Interacciones.VerificarDepositoAsync(_idOperador, id, material.Id, _cierre.Token, (int)_cantidad.Value);
            MostrarEstado(r.Exitoso ? "Recepción confirmada. Los ecopuntos se acreditan desde el módulo Ecopuntos." : r.Mensaje!, r.Exitoso ? TemaEcoAccess.Verde : TemaEcoAccess.Error);
            if (!r.Exitoso) _confirmar.Enabled = true; else _abrir.Enabled = true;
        }

        private void MostrarEstado(string texto, System.Drawing.Color color)
        {
            _estado.MaximumSize = new System.Drawing.Size(380, 0);
            _estado.Text = texto;
            _estado.ForeColor = color;
        }
    }
}
