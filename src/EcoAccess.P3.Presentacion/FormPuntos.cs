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
    /// Puntos de instalación: consultar, registrar, activar y desactivar. Las reglas (nombre único, no desactivar
    /// un punto con canecas en servicio) las decide <see cref="ServicioPuntosInstalacion"/>; aquí solo se muestran.
    /// </summary>
    public partial class FormPuntos : Form
    {
        private readonly ServicioPuntosInstalacion _servicio;
        private readonly int _idActor;
        private bool _ocupado;

        public FormPuntos(ServicioPuntosInstalacion servicio, int idActor)
        {
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _idActor = idActor;
            InitializeComponent();
            textoNombre.MaxLength = PuntoInstalacion.LongitudMaximaNombre;
            textoReferencia.MaxLength = PuntoInstalacion.LongitudMaximaReferencia;
            FormClosing += (_, e) => e.Cancel = _ocupado;
        }

        private async void FormPuntos_Load(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync);

        private async void BotonActualizar_Click(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync);

        private async void BotonRegistrar_Click(object? sender, EventArgs e) => await EjecutarAsync(async () =>
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(_idActor, textoNombre.Text, textoReferencia.Text);
            if (Informar(resultado, "Punto registrado."))
            {
                textoNombre.Clear();
                textoReferencia.Clear();
                await CargarAsync();
            }
        });

        private async void BotonActivar_Click(object? sender, EventArgs e) =>
            await CambiarEstadoAsync(id => _servicio.ActivarAsync(_idActor, id), "Punto activado.");

        private async void BotonDesactivar_Click(object? sender, EventArgs e) =>
            await CambiarEstadoAsync(id => _servicio.DesactivarAsync(_idActor, id), "Punto desactivado.");

        private async Task CambiarEstadoAsync(Func<int, Task<Resultado>> operacion, string mensajeExito) => await EjecutarAsync(async () =>
        {
            if (tablaPuntos.CurrentRow?.DataBoundItem is not FilaPunto fila)
            {
                MostrarEstado("Error: seleccione un punto de la tabla.", esError: true);
                return;
            }

            if (Informar(await operacion(fila.Id), mensajeExito))
            {
                await CargarAsync();
            }
        });

        private async Task CargarAsync()
        {
            int? idSeleccionado = (tablaPuntos.CurrentRow?.DataBoundItem as FilaPunto)?.Id;
            IReadOnlyList<PuntoInstalacion> puntos = await _servicio.ListarAsync();
            tablaPuntos.DataSource = puntos.Select(FilaPunto.Desde).ToList();
            Columna(nameof(FilaPunto.Id)).Visible = false;
            Columna(nameof(FilaPunto.Referencia)).HeaderText = "Referencia de ubicación";
            SeleccionGrid.Restaurar(tablaPuntos, (FilaPunto f) => f.Id == idSeleccionado, nameof(FilaPunto.Nombre));
            MostrarEstado($"{puntos.Count} puntos de instalación.", esError: false);
        }

        private DataGridViewColumn Columna(string nombre) =>
            tablaPuntos.Columns[nombre] ?? throw new InvalidOperationException($"Falta la columna {nombre}.");

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
            etiquetaEstado.ForeColor = TemaEcoAccess.ColorEstado(esError);
            etiquetaEstado.Text = mensaje;
        }

        private sealed record FilaPunto(int Id, string Nombre, string? Referencia, string Estado)
        {
            public static FilaPunto Desde(PuntoInstalacion p) => new(p.Id, p.Nombre, p.ReferenciaUbicacion, p.Activo ? "Activo" : "Inactivo");
        }
    }
}
