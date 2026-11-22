using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Catálogo de materiales: consultar, registrar, habilitar y deshabilitar. El formulario no
    /// decide reglas: llama a <see cref="ServicioMateriales"/> y muestra el mensaje del resultado.
    /// </summary>
    public partial class FormMateriales : Form
    {
        private readonly ServicioMateriales _servicio;
        private readonly int _idActor;
        private bool _ocupado;

        public FormMateriales(ServicioMateriales servicio, int idActor)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _idActor = idActor;
            InitializeComponent();
            comboCategoria.DataSource = TextosCategoria.Opciones;
            comboCategoria.DisplayMember = nameof(OpcionCategoria.Texto);
            comboCategoria.ValueMember = nameof(OpcionCategoria.Valor);
            // Los límites vienen de la entidad para que el campo no trunque distinto que el dominio.
            textoCodigo.MaxLength = Material.LongitudMaximaCodigo;
            textoNombre.MaxLength = Material.LongitudMaximaNombre;
            textoDescripcion.MaxLength = Material.LongitudMaximaDescripcion;
            ConstruirDisenoMateriales();
            FormClosing += (_, e) =>
            {
                if (_ocupado && _escrituraMaterial) e.Cancel = true;
                else _consultaMaterial?.Cancel();
            };
            Text = "Catálogo de materiales";
        }

        private async void FormMateriales_Load(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync, lectura: true);

        private void CheckSoloHabilitados_CheckedChanged(object? sender, EventArgs e)
        {
            if (!_ocupado) MostrarEstado("Pulsa Buscar para aplicar el estado junto con los demás filtros.", false);
        }

        private async void BotonActualizar_Click(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync, lectura: true);

        private async void BotonRegistrar_Click(object? sender, EventArgs e) => await EjecutarAsync(async () =>
        {
            if (Compat.IsNullOrWhiteSpace(textoCodigo.Text)) { ErrorCampoMaterial(textoCodigo, "Escribe un código para el material."); return; }
            if (Compat.IsNullOrWhiteSpace(textoNombre.Text)) { ErrorCampoMaterial(textoNombre, "Escribe el nombre del material."); return; }
            if (comboCategoria.SelectedItem is not OpcionCategoria opcion)
            {
                MostrarEstado("Error: seleccione una categoría.", esError: true);
                return;
            }

            Resultado<int> resultado = await _servicio.RegistrarAsync(
                _idActor, textoCodigo.Text, textoNombre.Text, opcion.Valor, textoDescripcion.Text);
            if (Informar(resultado, "Material registrado."))
            {
                textoCodigo.Clear();
                textoNombre.Clear();
                textoDescripcion.Clear();
                await CargarAsync(resultado.Valor);
                if (_lecturaAutorizadaMaterial)
                    MostrarEstado("Material guardado en Oracle. Si no aparece en esta página o filtro, búscalo por su código.", false);
            }
        });

        private async void BotonHabilitar_Click(object? sender, EventArgs e) =>
            await CambiarEstadoAsync(id => _servicio.HabilitarAsync(_idActor, id), "Material habilitado.");

        private async void BotonDeshabilitar_Click(object? sender, EventArgs e) =>
            await CambiarEstadoAsync(id => _servicio.DeshabilitarAsync(_idActor, id), "Material deshabilitado.", confirmar: true);

        private async Task CambiarEstadoAsync(Func<int, Task<Resultado>> operacion, string mensajeExito, bool confirmar = false) => await EjecutarAsync(async () =>
        {
            if (tablaMateriales.CurrentRow?.DataBoundItem is not FilaMaterial fila)
            {
                MostrarEstado("Error: seleccione un material de la tabla.", esError: true);
                return;
            }

            if (confirmar && MessageBox.Show(this,
                $"Deshabilitar {fila.Codigo} · {fila.Nombre}\n\nSe retirará de nuevas operaciones; su historial se conserva.\n¿Deseas continuar?",
                "Confirmar estado del material", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                MostrarEstado("Operación cancelada, no se guardaron cambios.", false);
                return;
            }
            if (Informar(await operacion(fila.Id), mensajeExito))
            {
                await CargarAsync();
                if (_lecturaAutorizadaMaterial)
                    MostrarEstado(mensajeExito + " La selección queda vacía si ya no coincide con el filtro.", false);
            }
        });

        private Task CargarAsync() => CargarAsync(idSeleccionar: null);

        private async Task CargarAsync(int? idSeleccionar)
        {
            int? idSeleccionado = idSeleccionar ?? (tablaMateriales.CurrentRow?.DataBoundItem as FilaMaterial)?.Id;
            LimpiarConsultaMaterial();
            using var limiteRecarga = _consultaMaterial is null ? new CancellationTokenSource(TimeSpan.FromSeconds(20)) : null;
            if (limiteRecarga is not null)
            {
                _escrituraMaterial = false;
                _consultaMaterial = limiteRecarga;
                _cancelarMaterial.Visible = _cancelarMaterial.Enabled = true;
            }
            Resultado<PaginaMateriales> resultado;
            try
            {
                resultado = await _servicio.ConsultarPaginaAsync(_idActor, CapturarFiltroMaterial(), _consultaMaterial!.Token);
            }
            finally
            {
                if (limiteRecarga is not null) _consultaMaterial = null;
            }
            if (IsDisposed) return;
            if (!resultado.Exitoso)
            {
                Informar(resultado, "");
                if (_cambioMaterialConfirmado)
                    MostrarEstado("El cambio se guardó. La consulta posterior fue rechazada; no se muestran filas. " + resultado.Mensaje, true);
                return;
            }
            var pagina = resultado.Valor;
            _lecturaAutorizadaMaterial = true;
            _haySiguienteMaterial = pagina.HaySiguiente;
            tablaMateriales.DataSource = pagina.Filas.Select(FilaMaterial.Desde).ToList();
            PrepararColumnas();
            SeleccionGrid.Restaurar(tablaMateriales, (FilaMaterial f) => f.Id == idSeleccionado, nameof(FilaMaterial.Codigo));
            _paginaMaterial.Text = $"Página {_numeroPaginaMaterial} · {pagina.Filas.Count} materiales · hasta 25 por página";
            MostrarEstado(pagina.Filas.Count == 0 ? "No hay materiales que coincidan con los filtros aplicados."
                : "Consulta autorizada en Oracle · orden por código e identificador.", false);
            ActualizarSeleccionMaterial();
        }

        private void PrepararColumnas()
        {
            Columna(nameof(FilaMaterial.Id)).Visible = false;
            Columna(nameof(FilaMaterial.Codigo)).HeaderText = "Código";
            Columna(nameof(FilaMaterial.Categoria)).HeaderText = "Categoría";
            Columna(nameof(FilaMaterial.Descripcion)).HeaderText = "Descripción";
            foreach (DataGridViewColumn columna in tablaMateriales.Columns)
            {
                columna.SortMode = DataGridViewColumnSortMode.NotSortable;
                columna.Width = TemaEcoAccess.Px(this, columna.Name switch
                {
                    nameof(FilaMaterial.Descripcion) => 300,
                    nameof(FilaMaterial.Nombre) or nameof(FilaMaterial.Categoria) => 230,
                    _ => 155
                });
            }
        }

        // El enlace a datos crea una columna por propiedad de FilaMaterial; si falta una, es un error de programación.
        private DataGridViewColumn Columna(string nombre) =>
            tablaMateriales.Columns[nombre] ?? throw new InvalidOperationException($"Falta la columna {nombre}.");

        // Deshabilita la ventana mientras dura la operación, impide cerrarla o reentrar, y convierte las
        // fallas de infraestructura (Oracle detenido, red) en un mensaje genérico: el texto del proveedor
        // puede citar host o servicio, así que no se muestra. Los rechazos de negocio llegan como Resultado.
        private async Task EjecutarAsync(Func<Task> accion, bool lectura = false)
        {
            if (_ocupado)
            {
                return;
            }

            _ocupado = true;
            _escrituraMaterial = !lectura;
            _cambioMaterialConfirmado = false;
            UseWaitCursor = true;
            panelAcciones.Enabled = false;
            tablaMateriales.Enabled = false;
            _paginacionMaterial.Enabled = false;
            foreach (Control control in _filtrosMaterial.Controls) control.Enabled = false;
            _erroresMaterial.Clear();
            MostrarEstado(lectura ? "Consultando catálogo en Oracle…" : "Procesando la operación…", false);
            using var consulta = lectura ? new CancellationTokenSource(TimeSpan.FromSeconds(20)) : null;
            _consultaMaterial = consulta;
            _cancelarMaterial.Visible = _cancelarMaterial.Enabled = lectura;
            try
            {
                await accion();
            }
            catch (OperationCanceledException) when (lectura || !_escrituraMaterial)
            {
                if (!IsDisposed)
                {
                    LimpiarConsultaMaterial();
                    MostrarEstado(_cambioMaterialConfirmado ? "El cambio se guardó. La consulta posterior se canceló o agotó el tiempo; vuelve a buscar."
                        : "Consulta cancelada o tiempo de espera agotado. Puedes volver a buscar.", true);
                }
            }
            catch (Exception)
            {
                if (!IsDisposed)
                {
                    LimpiarConsultaMaterial();
                    MostrarEstado(_cambioMaterialConfirmado ? "El cambio se guardó, pero no se pudo actualizar la tabla. Vuelve a buscar."
                        : lectura ? "No se pudo consultar Oracle. Revisa la conexión y vuelve a buscar."
                        : "No se pudo confirmar el resultado. Consulta antes de repetir la operación para evitar duplicados.", true);
                }
            }
            finally
            {
                _ocupado = false;
                _escrituraMaterial = false;
                _consultaMaterial = null;
                if (!IsDisposed)
                {
                    panelAcciones.Enabled = _lecturaAutorizadaMaterial;
                    tablaMateriales.Enabled = true;
                    _paginacionMaterial.Enabled = true;
                    foreach (Control control in _filtrosMaterial.Controls) control.Enabled = true;
                    _cancelarMaterial.Visible = false;
                    UseWaitCursor = false;
                    ActualizarSeleccionMaterial();
                    _focoMaterial?.Focus();
                    _focoMaterial = null;
                }
            }
        }

        private bool Informar(Resultado resultado, string mensajeExito)
        {
            if (resultado.Exitoso && _escrituraMaterial) _cambioMaterialConfirmado = true;
            if (resultado.Motivo is MotivoRechazo.SinPermiso or MotivoRechazo.ActorInactivo or MotivoRechazo.ActorNoEncontrado)
                LimpiarConsultaMaterial();
            if (resultado.Motivo is MotivoRechazo.DatosInvalidos or MotivoRechazo.Duplicado)
            {
                Control? campo = resultado.Motivo == MotivoRechazo.Duplicado || resultado.Mensaje?.StartsWith("codigo:", StringComparison.OrdinalIgnoreCase) == true ? textoCodigo
                    : resultado.Mensaje?.StartsWith("nombre:", StringComparison.OrdinalIgnoreCase) == true ? textoNombre
                    : resultado.Mensaje?.StartsWith("descripcion:", StringComparison.OrdinalIgnoreCase) == true ? textoDescripcion : null;
                if (campo is not null) { _erroresMaterial.SetError(campo, resultado.Mensaje); _focoMaterial = campo; }
            }
            MostrarEstado(resultado.Exitoso ? mensajeExito : $"Error: {resultado.Mensaje}", esError: !resultado.Exitoso);
            return resultado.Exitoso;
        }

        private void MostrarEstado(string mensaje, bool esError)
        {
            etiquetaEstado.ForeColor = esError ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro;
            etiquetaEstado.Text = mensaje;
        }

        /// <summary>Lo que muestra cada fila de la tabla; la entidad no se enlaza directamente.</summary>
        private sealed record FilaMaterial(int Id, string Codigo, string Nombre, string Categoria, string Estado, string? Descripcion)
        {
            public static FilaMaterial Desde(Material m) => new(
                m.Id, m.Codigo, m.Nombre, TextosCategoria.Texto(m.Categoria), m.Habilitado ? "Habilitado" : "Deshabilitado", m.Descripcion);
        }
    }
}
