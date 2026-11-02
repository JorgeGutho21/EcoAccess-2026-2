using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{

    public sealed partial class ServicioInteracciones
    {
        internal async Task<bool> EsInvitadaTelegramAsync(int interaccion, CancellationToken ct) =>
            await _interacciones.ObtenerInteraccionAsync(interaccion, ct) is { IdUsuario: null, Canal: CanalInteraccion.Telegram or CanalInteraccion.TelegramSimulado };

        // La sesión privada persistida se exige en ServicioInvitadosTelegram antes de cada operación.
        // El canal se recupera del registro inmutable; ningún callback lo proporciona.
        private async Task<CanalInteraccion> CanalSesionAsync(int interaccion, CancellationToken ct) =>
            await _interacciones.ObtenerInteraccionAsync(interaccion, ct) is { IdUsuario: null, Canal: CanalInteraccion.TelegramSimulado }
                ? CanalInteraccion.TelegramSimulado : CanalInteraccion.Telegram;

        // Solo ServicioInvitadosTelegram llama estas operaciones tras comprobar la sesión persistida.
        internal Task<Resultado<int>> IniciarInvitadoTelegramAsync(int contenedor, CancellationToken ct, bool simulador = false) =>
            IniciarDeAsync(null, contenedor, simulador ? CanalInteraccion.TelegramSimulado : CanalInteraccion.Telegram, ct);

        internal async Task<Resultado> ValidarInvitadoTelegramAsync(int interaccion, CancellationToken ct)
        {
            var (_, rechazo) = await CargarPropiaIniciadaAsync(null, interaccion, ct, canalInvitado: await CanalSesionAsync(interaccion, ct));
            return rechazo ?? Resultado.Exito();
        }

        internal async Task<Resultado<int>> AutorizarInvitadoTelegramAsync(int interaccion, int material, string clave, CancellationToken ct) =>
            await AutorizarAperturaDeAsync(null, interaccion, material, clave, ct, await CanalSesionAsync(interaccion, ct));

        internal async Task<Resultado<OperacionApertura>> ConsultarInvitadoTelegramAsync(int interaccion, string clave, CancellationToken ct) =>
            await ConsultarOperacionDeAsync(null, interaccion, clave, ct, await CanalSesionAsync(interaccion, ct));

        internal async Task<Resultado> SugerirInvitadoTelegramAsync(int interaccion, int material, decimal confianza, CancellationToken ct) =>
            await RegistrarSugerenciaDeAsync(null, interaccion, material, confianza, ct, await CanalSesionAsync(interaccion, ct));

        internal async Task<Resultado> RechazarInvitadoTelegramAsync(int interaccion, string motivo, CancellationToken ct) =>
            await RechazarDeAsync(null, interaccion, motivo, ct, await CanalSesionAsync(interaccion, ct));
    }
}
