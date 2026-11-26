using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Canecas: registrar en un punto, elegir qué materiales admiten y cambiar su estado. Las reglas (punto activo,
    /// material habilitado, no quedar en servicio sin materiales, baja definitiva) las decide
    /// <see cref="ServicioContenedores"/>; las listas de puntos y materiales solo sirven para elegir y mostrar nombres.
    /// «Ver QR» abre la etiqueta imprimible de la caneca seleccionada (<see cref="FormQrCaneca"/>).
    /// </summary>
    public partial class FormContenedores : Form
    {
        private readonly ServicioContenedores _contenedores;
        private readonly ServicioPuntosInstalacion _puntos;
        private readonly ServicioMateriales _materiales;
        private readonly int _idActor;
        private readonly string? _nombreBot;
        private Dictionary<int, Contenedor> _canecas = new Dictionary<int, Contenedor>();
        private Dictionary<int, Material> _catalogo = new Dictionary<int, Material>();
        private bool _ocupado;

        public FormContenedores(ServicioContenedores contenedores, ServicioPuntosInstalacion puntos, ServicioMateriales materiales, int idActor, string? nombreBot)
        {
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _puntos = puntos ?? throw new ArgumentNullException(nameof(puntos));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _idActor = idActor;
            _nombreBot = nombreBot;
            InitializeComponent();
            textoNombre.MaxLength = Contenedor.LongitudMaximaNombre;
            comboPunto.DisplayMember = nameof(Opcion.Texto);
            comboMaterial.DisplayMember = nameof(Opcion.Texto);
            listaAdmitidos.DisplayMember = nameof(Opcion.Texto);
            ConstruirDiseno();
            FormClosing += (_, e) => e.Cancel = _ocupado;
        }

        private async void FormContenedores_Load(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync);

        private async void BotonActualizar_Click(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync);

        private async void BotonVerQr_Click(object? sender, EventArgs e)
        {
            if (CanecaSeleccionada() is not { } seleccionada)
            {
                MostrarEstado("Error: seleccione una caneca de la tabla.", esError: true);
                return;
            }

            if (_nombreBot is null)
            {
                MostrarEstado("Error: falta Telegram:NombreBot en los User Secrets del perfil en uso; sin él el QR no abre el bot.", esError: true);
                return;
            }

            Contenedor? caneca = null;
            Uri? enlace = null;
            // Se relee la caneca: otra persona pudo darla de baja después de cargar la tabla.
            await EjecutarAsync(async () =>
            {
                Resultado<Contenedor> actual = await _contenedores.ObtenerAsync(seleccionada.Id);
                if (!Informar(actual, "QR generado."))
                {
                    return;
                }

                Resultado<Uri> resultado = CodigoQrCaneca.Enlace(_nombreBot, actual.Valor);
                if (Informar(resultado, "QR generado."))
                {
                    caneca = actual.Valor;
                    enlace = resultado.Valor;
                }
            });
            if (caneca is null || enlace is null)
            {
                return;
            }

            using var formulario = new FormQrCaneca(caneca.Nombre, enlace, CodigoQrCaneca.Png(enlace));
            TemaEcoAccess.Aplicar(formulario);
            formulario.ShowDialog(this);
            MostrarEstado(string.Empty, esError: false);
        }

        private void TablaCanecas_SelectionChanged(object? sender, EventArgs e) => MostrarMaterialesAdmitidos();

        private async void BotonRegistrar_Click(object? sender, EventArgs e) => await EjecutarAsync(async () =>
        {
            if (comboPunto.SelectedItem is not Opcion punto)
            {
                MostrarEstado("Error: seleccione un punto de instalación activo.", esError: true);
                return;
            }

            Resultado<int> resultado = await _contenedores.RegistrarAsync(_idActor, punto.Id, textoNombre.Text);
            if (Informar(resultado, "Caneca registrada fuera de servicio. Asígnele materiales y póngala en servicio."))
            {
                textoNombre.Clear();
                await CargarAsync(idSeleccionar: resultado.Valor);
            }
        });

        private async void BotonAdmitir_Click(object? sender, EventArgs e)
        {
            if (comboMaterial.SelectedItem is not Opcion material)
            {
                MostrarEstado("Error: seleccione un material habilitado.", esError: true);
                return;
            }

            await ModificarSeleccionadaAsync(id => _contenedores.AdmitirMaterialAsync(_idActor, id, material.Id), "Material admitido.");
        }

        private async void BotonQuitar_Click(object? sender, EventArgs e)
        {
            if (listaAdmitidos.SelectedItem is not Opcion material)
            {
                MostrarEstado("Error: seleccione un material de la lista de admitidos.", esError: true);
                return;
            }

            await ModificarSeleccionadaAsync(id => _contenedores.QuitarMaterialAsync(_idActor, id, material.Id), "Material quitado.");
        }

        private async void BotonEnServicio_Click(object? sender, EventArgs e) =>
            await ModificarSeleccionadaAsync(id => _contenedores.PonerEnServicioAsync(_idActor, id), "Caneca en servicio.");

        private async void BotonFueraDeServicio_Click(object? sender, EventArgs e) =>
            await ModificarSeleccionadaAsync(id => _contenedores.PonerFueraDeServicioAsync(_idActor, id), "Caneca fuera de servicio.");

        private async void BotonDarDeBaja_Click(object? sender, EventArgs e)
        {
            if (_ocupado || CanecaSeleccionada() is not { } caneca || caneca.Estado == EstadoContenedor.DadoDeBaja)
            {
                // Sin selección, o ya dada de baja: la operación informa por sí misma sin preguntar.
                await ModificarSeleccionadaAsync(id => _contenedores.DarDeBajaAsync(_idActor, id), "Caneca dada de baja.");
                return;
            }

            // La baja es definitiva (la caneca no vuelve a servicio): se confirma antes de pedirla.
            DialogResult respuesta = MessageBox.Show(
                this,
                $"¿Dar de baja la caneca «{caneca.Nombre}»? La baja es definitiva: no podrá volver a servicio.",
                "Dar de baja",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (respuesta == DialogResult.Yes)
            {
                await ModificarSeleccionadaAsync(id => _contenedores.DarDeBajaAsync(_idActor, id), "Caneca dada de baja.");
            }
        }

        private async Task ModificarSeleccionadaAsync(Func<int, Task<Resultado>> operacion, string mensajeExito) => await EjecutarAsync(async () =>
        {
            if (CanecaSeleccionada() is not { } caneca)
            {
                MostrarEstado("Error: seleccione una caneca de la tabla.", esError: true);
                return;
            }

            if (Informar(await operacion(caneca.Id), mensajeExito))
            {
                await CargarAsync(idSeleccionar: caneca.Id);
            }
        });

        private async Task CargarAsync() => await CargarAsync(idSeleccionar: CanecaSeleccionada()?.Id);

        /// <summary>Estado OPERATIVO en vivo del controlador central (el mismo que consulta Telegram), distinto del estado
        /// administrativo de Oracle de cada caneca. Evita que la app diga «En servicio» mientras la estación está en pausa.</summary>
        private string _estadoEstacion = "";

        internal static string TextoEstadoOperativo(EcoAccess.P3.Logica.Vision35.Expo.EstadoEstacionExpo e) =>
            !e.Disponible ? "Estación: el controlador no responde"
            : !e.Conectado ? "Estación: desconectada (sin ESP32)"
            : e.ControlActivo ? "Estación: en servicio"
            : "Estación: en pausa (mantenimiento) · reanúdala en ECOACCESS ESP32";

        private static async Task<string> EstadoOperativoEstacionAsync()
        {
            try
            {
                var e = await new EcoAccess.P3.Logica.Vision35.Expo.ClienteEstacionPanel().EstadoAsync();
                return TextoEstadoOperativo(e);
            }
            catch (Exception) { return "Estación: sin dato del controlador"; }
        }

        private async Task CargarAsync(int? idSeleccionar)
        {
            IReadOnlyList<PuntoInstalacion> puntos = await _puntos.ListarAsync();
            IReadOnlyList<Material> materiales = await _materiales.ListarAsync(soloHabilitados: false);
            IReadOnlyList<Contenedor> canecas = await _contenedores.ListarAsync();
            _estadoEstacion = await EstadoOperativoEstacionAsync();

            var nombresPunto = puntos.ToDictionary(p => p.Id, p => p.Nombre);
            _catalogo = materiales.ToDictionary(m => m.Id);
            _canecas = canecas.ToDictionary(c => c.Id);

            // Solo se ofrece lo que el servicio aceptaría; si cambia antes de enviar, el servicio rechaza igual.
            Enlazar(comboPunto, puntos.Where(p => p.Activo).Select(p => new Opcion(p.Id, p.Nombre)).ToList());
            Enlazar(comboMaterial, materiales.Where(m => m.Habilitado).Select(m => new Opcion(m.Id, $"{m.Codigo} — {m.Nombre}")).ToList());

            tablaCanecas.DataSource = canecas.Select(c => new FilaCaneca(
                c.Id,
                c.Nombre,
                nombresPunto.GetValueOrDefault(c.IdPuntoInstalacion, $"Punto {c.IdPuntoInstalacion}"),
                TextosContenedor.Estado(c.Estado),
                string.Join(", ", c.MaterialesAdmitidos.Select(NombreMaterial).Order()),
                c.IdentificadorQr,
                c.FechaAltaUtc.ToLocalTime().ToString("g"))).ToList();
            Columna(nameof(FilaCaneca.Id)).Visible = false;
            Columna(nameof(FilaCaneca.IdentificadorQr)).HeaderText = "Identificador QR";
            Columna(nameof(FilaCaneca.Alta)).HeaderText = "Alta";
            SeleccionGrid.Restaurar(tablaCanecas, (FilaCaneca f) => f.Id == idSeleccionar, nameof(FilaCaneca.Nombre));
            MostrarMaterialesAdmitidos();
            MostrarEstado($"{canecas.Count} canecas.", esError: false);
        }

        private void MostrarMaterialesAdmitidos()
        {
            Contenedor? caneca = CanecaSeleccionada();
            grupoMateriales.Text = caneca is null ? "Materiales admitidos" : $"Materiales admitidos — {caneca.Nombre}";
            listaAdmitidos.DataSource = caneca?.MaterialesAdmitidos
                .Select(id => new Opcion(id, NombreMaterial(id)))
                .OrderBy(o => o.Texto)
                .ToList() ?? new List<Opcion>();
            // Sin selección previa: un segundo clic en «Quitar» no debe quitar el primero de la lista sin elegirlo.
            listaAdmitidos.ClearSelected();
        }

        /// <summary>Recarga un combo conservando la opción elegida si sigue disponible.</summary>
        private static void Enlazar(ComboBox combo, List<Opcion> opciones)
        {
            int? idElegido = (combo.SelectedItem as Opcion)?.Id;
            combo.DataSource = opciones;
            Opcion? anterior = opciones.Find(o => o.Id == idElegido);
            if (anterior is not null)
            {
                combo.SelectedItem = anterior;
            }
        }

        private Contenedor? CanecaSeleccionada() =>
            tablaCanecas.CurrentRow?.DataBoundItem is FilaCaneca fila ? _canecas.GetValueOrDefault(fila.Id) : null;

        private string NombreMaterial(int id) => _catalogo.TryGetValue(id, out Material? m)
            ? (m.Habilitado ? m.Nombre : $"{m.Nombre} (deshabilitado)")
            : $"Material {id}";

        private DataGridViewColumn Columna(string nombre) =>
            tablaCanecas.Columns[nombre] ?? throw new InvalidOperationException($"Falta la columna {nombre}.");

        // Igual que en FormMateriales: bloquea la ventana durante la operación y no muestra el texto del proveedor.
        private async Task EjecutarAsync(Func<Task> accion)
        {
            if (_ocupado)
            {
                return;
            }

            _ocupado = true;
            UseWaitCursor = true;
            panelAcciones.Enabled = false;
            try
            {
                await accion();
            }
            catch (Exception)
            {
                MostrarEstado("Error: no se pudo completar la operación. Revise que Oracle esté en marcha e inténtelo de nuevo.", esError: true);
            }
            finally
            {
                _ocupado = false;
                panelAcciones.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private bool Informar(Resultado resultado, string mensajeExito)
        {
            MostrarEstado(resultado.Exitoso ? mensajeExito : $"Error: {resultado.Mensaje ?? "no se pudo completar la operación."}", esError: !resultado.Exitoso);
            return resultado.Exitoso;
        }

        private void MostrarEstado(string mensaje, bool esError)
        {
            etiquetaEstado.ForeColor = esError ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro;
            etiquetaEstado.Text = mensaje;
        }

        /// <summary>Elemento de combo o lista: el id que se envía al servicio y el texto que se muestra.</summary>
        private sealed record Opcion(int Id, string Texto);

        private sealed record FilaCaneca(int Id, string Nombre, string Punto, string Estado, string Materiales, string IdentificadorQr, string Alta);
    }
}
