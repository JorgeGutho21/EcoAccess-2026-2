using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Qué pasó al intentar entregar una orden de apertura a la caneca.</summary>
    public enum ResultadoDespacho
    {
        /// <summary>El canal entregó el comando y la orden quedó Enviada. La caneca todavía no se ha abierto.</summary>
        Enviada = 1,

        /// <summary>La orden ya estaba Enviada o más adelante (por ejemplo, el acuse llegó primero); no se reenvió.</summary>
        YaEnviada = 2,

        /// <summary>
        /// El canal falló, no respondió a tiempo o dijo que no entregó, o entregó pero la orden no pudo marcarse. La orden
        /// sigue Emitida y puede reintentarse: el comando pudo llegar igual, y la caneca no abre dos veces la misma clave.
        /// </summary>
        NoEntregada = 3,

        /// <summary>La orden pasó su hora de vencimiento, aunque el barrido ya la haya marcado Vencida; no se reenvía (RN-07).</summary>
        Vencida = 4,

        /// <summary>La orden no existe o terminó (cerrada o fallida).</summary>
        NoPermitida = 5
    }

    /// <summary>
    /// Entrega las órdenes de apertura a la caneca por un <see cref="ICanalDispositivo"/>. No confía en el
    /// canal: le impone un tiempo máximo, trata cualquier fallo como «no entregada» y solo marca la orden como Enviada.
    /// Nunca la marca como Abierta ni crea depósitos: eso lo deciden los eventos del dispositivo en
    /// <see cref="ServicioInteracciones.RegistrarEventoAsync"/>.
    /// </summary>
    public sealed class ServicioDespachoOrdenes
    {
        /// <summary>Tiempo máximo de espera al canal si no se indica otro.</summary>
        public static readonly TimeSpan TiempoMaximoPredeterminado = TimeSpan.FromSeconds(5);

        private readonly IRepositorioInteracciones _interacciones;
        private readonly ServicioInteracciones _servicio;
        private readonly ICanalDispositivo _canal;
        private readonly TimeProvider _reloj;
        private readonly TimeSpan _tiempoMaximo;

        public ServicioDespachoOrdenes(
            IRepositorioInteracciones interacciones,
            ServicioInteracciones servicio,
            ICanalDispositivo canal,
            TimeProvider reloj,
            TimeSpan? tiempoMaximo = null)
        {
            _interacciones = interacciones ?? throw new ArgumentNullException(nameof(interacciones));
            _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            _canal = canal ?? throw new ArgumentNullException(nameof(canal));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _tiempoMaximo = tiempoMaximo ?? TiempoMaximoPredeterminado;
            if (_tiempoMaximo <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(tiempoMaximo), "El tiempo máximo debe ser positivo.");
            }
        }

        /// <summary>Nombre del canal en uso, para trazabilidad.</summary>
        public string Canal => _canal.Canal;

        private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

        /// <summary>
        /// Entrega la orden si está Emitida y vigente. Reintentar después de <see cref="ResultadoDespacho.NoEntregada"/>
        /// es seguro: el comando lleva la clave de idempotencia y la caneca no abre dos veces.
        /// </summary>
        /// <exception cref="OperationCanceledException">Si quien llama cancela; el vencimiento del tiempo no lanza.</exception>
        public async Task<ResultadoDespacho> DespacharAsync(int idOrden, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OrdenApertura? orden = idOrden > 0 ? await _interacciones.ObtenerOrdenAsync(idOrden, cancellationToken) : null;
            if (orden is null)
            {
                return ResultadoDespacho.NoPermitida;
            }

            if (PasoSuHora(orden))
            {
                return ResultadoDespacho.Vencida;
            }

            if (!orden.EsVigente)
            {
                return ResultadoDespacho.NoPermitida;
            }

            if (orden.Estado != EstadoOrdenApertura.Emitida)
            {
                return ResultadoDespacho.YaEnviada;
            }

            var comando = new ComandoApertura(orden.Id, orden.IdContenedor, orden.ClaveIdempotencia, orden.FechaVencimientoUtc);
            // Un comando no debe salir hacia la caneca si quien llama ya canceló, aunque el repositorio no lo haya notado.
            cancellationToken.ThrowIfCancellationRequested();
            if (!await EntregarAsync(comando, cancellationToken))
            {
                return ResultadoDespacho.NoEntregada;
            }

            // El comando ya salió: el registro no se cancela, para que la orden refleje lo que pasó con la caneca.
            Resultado marcado = await _servicio.MarcarEnviadaAsync(orden.Id, CancellationToken.None);
            return marcado.Exitoso ? ResultadoDespacho.Enviada : await ClasificarAsync(orden.Id, CancellationToken.None);
        }

        /// <summary>La hora de vencimiento pasó, o el barrido ya la marcó Vencida.</summary>
        private bool PasoSuHora(OrdenApertura orden) =>
            orden.Estado == EstadoOrdenApertura.Vencida || orden.EstaVencida(Ahora);

        /// <summary>
        /// La orden no pudo pasar a Enviada después de entregarla: o el acuse llegó antes (sigue vigente y más
        /// adelante), o venció o terminó mientras se entregaba.
        /// </summary>
        private async Task<ResultadoDespacho> ClasificarAsync(int idOrden, CancellationToken cancellationToken)
        {
            OrdenApertura? actual = await _interacciones.ObtenerOrdenAsync(idOrden, cancellationToken);
            if (actual is null)
            {
                return ResultadoDespacho.NoPermitida;
            }

            if (actual.EsVigente && actual.Estado > EstadoOrdenApertura.Emitida)
            {
                return ResultadoDespacho.YaEnviada;
            }

            if (PasoSuHora(actual))
            {
                return ResultadoDespacho.Vencida;
            }

            return actual.EsVigente ? ResultadoDespacho.NoEntregada : ResultadoDespacho.NoPermitida;
        }

        /// <summary>
        /// True solo si el canal dijo que entregó dentro del tiempo máximo. El tiempo se impone también a un canal que
        /// ignore el token. La cancelación pedida por quien llama se propaga como <see cref="OperationCanceledException"/>,
        /// aunque el canal reaccione con otra excepción.
        /// </summary>
        private async Task<bool> EntregarAsync(ComandoApertura comando, CancellationToken cancellationToken)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(_tiempoMaximo);
            try
            {
                return await _canal.EnviarAperturaAsync(comando, limite.Token).WaitAsync(_tiempoMaximo, cancellationToken);
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                if (ex is OperationCanceledException)
                {
                    throw;
                }

                throw new OperationCanceledException("El despacho se canceló.", ex, cancellationToken);
            }
            catch (Exception)
            {
                // WaitAsync puede vencer antes que CancelAfter: se cancela aquí para que el canal siempre reciba la
                // señal antes de liberar el token.
                limite.Cancel();

                // Un canal caído (cable suelto, red local, dispositivo apagado) deja la orden Emitida para reintentar.
                return false;
            }
        }
    }
}
