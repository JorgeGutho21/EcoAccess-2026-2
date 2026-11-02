using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{

    public sealed partial class ServicioInteracciones
    {
        public async Task<Resultado<Interaccion>> ConsultarSolicitudPropiaAsync(int actor, int solicitud, CancellationToken ct = default)
        {
            var (interaccion, rechazo) = await CargarPropiaAsync(actor, solicitud, ct);
            return rechazo is not null ? Resultado<Interaccion>.DesdeRechazo(rechazo) : Resultado<Interaccion>.Exito(interaccion!);
        }

        public async Task<Resultado> DeclararCantidadAsync(int actor, int solicitud, int cantidad, CancellationToken ct = default)
        {
            var (interaccion, rechazo) = await CargarPropiaIniciadaAsync(actor, solicitud, ct);
            if (rechazo is not null) return rechazo;
            try { Interaccion.ValidarCantidad(cantidad); }
            catch (ArgumentException error) { return MensajesValidacion.Rechazo(error); }
            if (interaccion!.CantidadDeclarada == cantidad) return Resultado.Exito();
            return await _interacciones.CambiarCantidadDeclaradaAsync(actor, solicitud, interaccion.CantidadDeclarada, cantidad, ct)
                ? Resultado.Exito()
                : Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "La solicitud cambió mientras declarabas la cantidad. Consulta su estado antes de reintentar.");
        }

        /// <summary>El botón confirma exactamente la declaración mostrada, incluida la ausencia histórica de cantidad.</summary>
        public Task<Resultado<int>> AutorizarDeclaracionAsync(int actor, int solicitud, int material, int? cantidad, string clave, CancellationToken ct = default) =>
            AutorizarAperturaDeAsync(actor, solicitud, material, clave, ct, exigirDeclaracion: true, cantidadEsperada: cantidad);
    }
}
