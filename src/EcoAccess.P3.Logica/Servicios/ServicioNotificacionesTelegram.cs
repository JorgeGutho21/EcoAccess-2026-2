using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Avisos ya enviados (clave → fecha del hecho) y fallos de entrega por clave.</summary>
    public sealed record EstadoNotificaciones(Dictionary<string, DateTime> Enviadas, Dictionary<string, int> Fallos);

    /// <summary>El chat no existe o bloqueó al bot: reintentar el mismo aviso no sirve.</summary>
    public sealed class DestinoNotificacionNoDisponibleException
        : Exception
    {
        public DestinoNotificacionNoDisponibleException(Exception? causa = null) : base("El destinatario del aviso no está disponible.", causa)
        {
        }
    }

    /// <summary>Persistencia local de los avisos enviados; sobrevive al reinicio del bot.</summary>
    public interface IRegistroNotificacionesEnviadas
    {
        EstadoNotificaciones? Cargar();
        void Guardar(EstadoNotificaciones estado);
    }

    /// <summary>
    /// Avisa al ciudadano cuando Oracle registra un cambio de su operación: apertura, fallo, cierre sin depósito,
    /// depósito, verificación y ecopuntos. Solo informa hechos ya persistidos por el bridge o el operador; nunca los
    /// anticipa ni actúa sobre la caneca. Entrega «al menos una vez»: la clave se guarda después de enviar, así que un
    /// corte justo entre ambos pasos puede repetir un aviso, nunca perderlo. Un aviso que falla
    /// <see cref="IntentosMaximos"/> veces se descarta y queda registrado; el depósito no depende de Telegram.
    /// </summary>
    public sealed class ServicioNotificacionesTelegram
    {
        /// <summary>Solo se revisan hechos de este periodo; los anteriores ya no son noticia.</summary>
        public static readonly TimeSpan Ventana = TimeSpan.FromHours(24);
        public const int IntentosMaximos = 5;

        private readonly IRepositorioNotificacionesTelegram _repositorio;
        private readonly IRegistroNotificacionesEnviadas _registro;
        private readonly TimeProvider _reloj;
        private readonly Func<long, string?>? _detecciones;
        private EstadoNotificaciones? _estado;

        /// <param name="detecciones">Lo detectado en la foto de cada persona, para anunciar la apertura; sin esto, el aviso no afirma una detección.</param>
        public ServicioNotificacionesTelegram(IRepositorioNotificacionesTelegram repositorio, IRegistroNotificacionesEnviadas registro, TimeProvider reloj, Func<long, string?>? detecciones = null)
        {
            _detecciones = detecciones;
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        /// <summary>Envía los avisos pendientes y devuelve cuántos se entregaron.</summary>
        public async Task<int> RevisarAsync(Func<RespuestaBot, CancellationToken, Task> enviar, Action<string> registrar, CancellationToken cancellationToken)
        {
            Compat.ThrowIfNull(enviar, nameof(enviar));
            Compat.ThrowIfNull(registrar, nameof(registrar));
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            IReadOnlyList<HechoNotificable> hechos = await _repositorio.ListarDesdeAsync(ahora - Ventana, cancellationToken);
            if (_estado is null)
            {
                _estado = _registro.Cargar();
                if (_estado is null)
                {
                    // Primer arranque: lo ocurrido antes no se envía de golpe; solo lo nuevo desde ahora.
                    _estado = new EstadoNotificaciones(hechos.ToDictionary(h => h.Clave, h => h.FechaUtc), new Dictionary<string, int>());
                    _registro.Guardar(_estado);
                    return 0;
                }
            }

            foreach (string vieja in _estado.Enviadas.Where(e => e.Value < ahora - Ventana - Ventana).Select(e => e.Key).ToArray())
                _estado.Enviadas.Remove(vieja);

            int entregados = 0;
            foreach (HechoNotificable hecho in hechos)
            {
                if (_estado.Enviadas.ContainsKey(hecho.Clave)) continue;
                try
                {
                    await enviar(Mensaje(hecho.Tipo == TipoHechoNotificable.Apertura && _detecciones is not null
                        ? hecho with { FraseDeteccion = _detecciones(hecho.TelegramUserId) } : hecho, _reloj.GetUtcNow().UtcDateTime), cancellationToken);
                    entregados++;
                    _estado.Fallos.Remove(hecho.Clave);
                    _estado.Enviadas[hecho.Clave] = hecho.FechaUtc;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (DestinoNotificacionNoDisponibleException)
                {
                    _estado.Fallos.Remove(hecho.Clave);
                    _estado.Enviadas[hecho.Clave] = hecho.FechaUtc;
                    registrar($"Aviso {hecho.Clave}: el chat no existe o bloqueó al bot; se descarta sin reintentar.");
                }
                catch (Exception error)
                {
                    int fallos = _estado.Fallos.GetValueOrDefault(hecho.Clave) + 1;
                    _estado.Fallos[hecho.Clave] = fallos;
                    registrar($"Aviso {hecho.Clave}: no se pudo entregar ({error.GetType().Name}), intento {fallos} de {IntentosMaximos}.");
                    if (fallos >= IntentosMaximos)
                    {
                        _estado.Fallos.Remove(hecho.Clave);
                        _estado.Enviadas[hecho.Clave] = hecho.FechaUtc;
                        registrar($"Aviso {hecho.Clave}: descartado; el ciudadano puede consultarlo en el menú.");
                    }
                }
                _registro.Guardar(_estado);
            }

            return entregados;
        }

        public static RespuestaBot Mensaje(HechoNotificable hecho) => Mensaje(hecho, null);

        /// <param name="ahoraUtc">Hora real del envío: el aviso de apertura informa los segundos que de verdad quedan.</param>
        public static RespuestaBot Mensaje(HechoNotificable hecho, DateTime? ahoraUtc)
        {
            string prefijo = ServicioConversacionTelegram.PrefijoMenu;
            IReadOnlyList<BotonBot> botones = hecho.Tipo is TipoHechoNotificable.EcopuntosAcreditados or TipoHechoNotificable.EcopuntosRevertidos
                ? new BotonBot[] { new BotonBot("🌱 Mis ecopuntos", prefijo + "/ecopuntos") }
                : hecho.Tipo is TipoHechoNotificable.DepositoRegistrado or TipoHechoNotificable.CicloCerrado
                ? ServicioConversacionTelegram.BotonesValoracion
                : hecho.Tipo == TipoHechoNotificable.OrdenVencida ? new BotonBot[] { new BotonBot("🏠 Menú", prefijo + "inicio") }
                : hecho.Tipo == TipoHechoNotificable.Apertura ? Array.Empty<BotonBot>()
                : new BotonBot[] { new BotonBot("🧾 Mis depósitos", prefijo + "/depositos"), new BotonBot("🏠 Menú principal", prefijo + "inicio") };
            return new RespuestaBot(hecho.TelegramUserId, MensajesTelegram.Notificacion(hecho, ahoraUtc), botones);
        }
    }
}
