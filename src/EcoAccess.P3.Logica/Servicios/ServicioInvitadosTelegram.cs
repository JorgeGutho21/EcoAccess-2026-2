using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Invitado sin usuario ficticio. El adaptador autentica el remitente de Telegram; la conversación persistida
    /// delimita su interacción. Ni el cliente ni los botones pueden escoger una interacción ajena.
    /// No consulta históricos ni acredita puntos; un vínculo posterior no cambia el titular NULL.
    /// </summary>
    public sealed class ServicioInvitadosTelegram
    {
        private readonly IRepositorioConversacionesTelegram _conversaciones;
        private readonly ServicioInteracciones _interacciones;
        private readonly TimeProvider _reloj;

        public ServicioInvitadosTelegram(IRepositorioConversacionesTelegram conversaciones, ServicioInteracciones interacciones, TimeProvider reloj)
        {
            _conversaciones = conversaciones;
            _interacciones = interacciones;
            _reloj = reloj;
        }

        public async Task<bool> EsSesionInvitadaAsync(long cuenta, int interaccion, CancellationToken ct = default) =>
            (await ComprobarAsync(cuenta, interaccion, ct, vigente: false)).Exitoso
            && await _interacciones.EsInvitadaTelegramAsync(interaccion, ct);

        public Task<Resultado<int>> IniciarAsync(long cuenta, long chat, int contenedor, CancellationToken ct = default, bool simulador = false) =>
            cuenta > 0 && chat == cuenta
                ? _interacciones.IniciarInvitadoTelegramAsync(contenedor, ct, simulador)
                : Task.FromResult(Resultado<int>.Rechazo(MotivoRechazo.SinPermiso, "Utiliza tu conversación privada con Eco."));

        private async Task<Resultado> ComprobarAsync(long cuenta, int interaccion, CancellationToken ct, bool vigente = true)
        {
            var sesion = cuenta > 0 ? await _conversaciones.ObtenerAsync(cuenta, ct) : null;
            if (sesion is null || sesion.ChatId != cuenta || sesion.IdInteraccion != interaccion
                || (vigente && _reloj.GetUtcNow().UtcDateTime - sesion.FechaActualizacionUtc > ServicioConversacionTelegram.VigenciaConversacion))
                return Resultado.Rechazo(MotivoRechazo.SinPermiso, "La solicitud no pertenece a este diálogo o ya venció. Escanea de nuevo el QR.");
            return Resultado.Exito();
        }

        public async Task<Resultado> ValidarAsync(long cuenta, int interaccion, CancellationToken ct = default)
        {
            var permiso = await ComprobarAsync(cuenta, interaccion, ct);
            return permiso.Exitoso ? await _interacciones.ValidarInvitadoTelegramAsync(interaccion, ct) : permiso;
        }

        public async Task<Resultado<int>> AutorizarAsync(long cuenta, int interaccion, int material, string clave, CancellationToken ct = default)
        {
            var permiso = await ComprobarAsync(cuenta, interaccion, ct);
            return permiso.Exitoso ? await _interacciones.AutorizarInvitadoTelegramAsync(interaccion, material, clave, ct)
                : Resultado<int>.DesdeRechazo(permiso);
        }

        public async Task<Resultado<OperacionApertura>> ConsultarAsync(long cuenta, int interaccion, string clave, CancellationToken ct = default)
        {
            var permiso = await ComprobarAsync(cuenta, interaccion, ct);
            return permiso.Exitoso ? await _interacciones.ConsultarInvitadoTelegramAsync(interaccion, clave, ct)
                : Resultado<OperacionApertura>.DesdeRechazo(permiso);
        }

        public async Task<Resultado> RegistrarSugerenciaAsync(long cuenta, int interaccion, int material, decimal confianza, CancellationToken ct = default)
        {
            var permiso = await ComprobarAsync(cuenta, interaccion, ct);
            return permiso.Exitoso ? await _interacciones.SugerirInvitadoTelegramAsync(interaccion, material, confianza, ct) : permiso;
        }

        public async Task<Resultado> CancelarAsync(long cuenta, int interaccion, string motivo, CancellationToken ct = default)
        {
            // Se permite cancelar una solicitud vencida, conservando siempre su pertenencia al diálogo.
            var permiso = await ComprobarAsync(cuenta, interaccion, ct, vigente: false);
            return permiso.Exitoso ? await _interacciones.RechazarInvitadoTelegramAsync(interaccion, motivo, ct) : permiso;
        }
    }
}
