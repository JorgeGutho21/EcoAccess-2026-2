using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Dispositivos
{

    public enum ResultadoComandoDispositivo
    {
        Rechazado,
        NoAutorizado,
        YaReservadoOIncierto,
        AccionCompletada,
        AutorizacionNoDisponible
    }

    /// <summary>Frontera de actuación propuesta. Autenticar no autoriza ni demuestra apertura física.</summary>
    public sealed class EjecutorComandosAutenticados
    {
        private readonly ProtocoloDispositivoHmac _protocolo;
        private readonly IRegistroEjecucionDispositivo _registro;
        private readonly Func<ComandoApertura, CancellationToken, Task<bool>> _sigueAutorizado;
        private readonly Func<ComandoApertura, CancellationToken, Task> _actuar;

        public EjecutorComandosAutenticados(ProtocoloDispositivoHmac protocolo, IRegistroEjecucionDispositivo registro, Func<ComandoApertura, CancellationToken, Task<bool>> sigueAutorizado, Func<ComandoApertura, CancellationToken, Task> actuar)
        {
            _protocolo = protocolo;
            _registro = registro;
            _sigueAutorizado = sigueAutorizado;
            _actuar = actuar;
        }

        public async Task<ResultadoComandoDispositivo> ProcesarAsync(byte[] paquete, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            // La red puede reutilizar su buffer mientras se espera la autorización: conservar una copia privada.
            if (paquete is null || paquete.Length is 0 or > ProtocoloDispositivoHmac.MaximoPaquete)
                return ResultadoComandoDispositivo.Rechazado;
            byte[] copia = paquete.ToArray();
            var autenticado = _protocolo.ValidarComando(copia);
            if (!autenticado.Exitoso) return ResultadoComandoDispositivo.Rechazado;
            var comando = autenticado.Valor;
            // Requiere consulta autoritativa vigente. Si no hay conexión o la consulta falla, no se actúa.
            bool autorizado;
            try { autorizado = await _sigueAutorizado(comando, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception) { return ResultadoComandoDispositivo.AutorizacionNoDisponible; }
            if (!autorizado) return ResultadoComandoDispositivo.NoAutorizado;
            ct.ThrowIfCancellationRequested();
            // La consulta pudo tardar hasta después del vencimiento.
            if (!_protocolo.ValidarComando(copia).Exitoso) return ResultadoComandoDispositivo.Rechazado;
            if (!_registro.IntentarReservar(comando)) return ResultadoComandoDispositivo.YaReservadoOIncierto;
            // La escritura durable también puede superar el TTL. La reserva se conserva aunque ya no se actúe.
            if (!_protocolo.ValidarComando(copia).Exitoso) return ResultadoComandoDispositivo.Rechazado;
            // Una excepción/cancelación posterior conserva la reserva. Nunca reejecutar automáticamente.
            ct.ThrowIfCancellationRequested();
            await _actuar(comando, ct);
            return ResultadoComandoDispositivo.AccionCompletada;
        }
    }
}
