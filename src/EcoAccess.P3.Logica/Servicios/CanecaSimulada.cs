using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Comportamiento que reproduce la caneca simulada al ejecutar una orden.</summary>
    public enum EscenarioCaneca
    {
        /// <summary>Acuse, apertura, detección y cierre.</summary>
        Normal = 1,

        /// <summary>Recibe el comando pero no envía nada (por ejemplo, se reinició).</summary>
        SinAcuse = 2,

        /// <summary>Acusa recibo pero la tapa no se abre.</summary>
        AcuseSinApertura = 3,

        /// <summary>Se abre y se cierra sin que el sensor detecte un residuo.</summary>
        AperturaSinDeteccion = 4,

        /// <summary>Acusa recibo y luego informa un fallo (tapa trabada, sensor caído).</summary>
        Fallo = 5,

        /// <summary>Como <see cref="Normal"/>, pero cada evento se envía dos veces con el mismo identificador.</summary>
        Duplicados = 6,

        /// <summary>La detección llega antes que la apertura.</summary>
        FueraDeOrden = 7,

        /// <summary>El canal no entrega el comando.</summary>
        Desconectada = 8,

        /// <summary>Caneca sin sensor de ingreso: acuse, apertura y, al terminar la ventana, cierre sin sensor (DECLARADA_SIN_SENSOR).</summary>
        SinSensor = 9
    }

    /// <summary>
    /// Caneca simulada. Hace de canal (<see cref="ICanalDispositivo"/>) y de dispositivo: guarda
    /// los comandos que recibe y, cuando la prueba lo pide con <see cref="EjecutarAsync"/>, envía la secuencia de
    /// eventos del escenario por el mismo contrato que usaría el ESP32
    /// (<see cref="ServicioInteracciones.RegistrarEventoAsync"/>). Es determinista: no usa azar ni hilos propios, y
    /// recibir un comando no emite eventos por sí solo. <b>No identifica el material</b>: como el sensor real, solo
    /// informa que detectó algo y, si se configura, un peso medido. No es un ESP32 y no prueba hardware.
    /// </summary>
    public sealed class CanecaSimulada : ICanalDispositivo
    {
        private readonly int _idContenedor;
        private readonly Func<EventoRecibido, CancellationToken, Task<Resultado<EventoRegistrado>>> _enviarEvento;
        private readonly TimeProvider _reloj;
        private readonly IRegistroEjecucionDispositivo? _registro;
        private readonly object _bloqueo = new();
        private readonly Dictionary<int, ComandoApertura> _comandos = new Dictionary<int, ComandoApertura>();
        private readonly List<ComandoApertura> _ordenLlegada = new List<ComandoApertura>();
        private readonly HashSet<string> _clavesEjecutadas = new(StringComparer.Ordinal);

        /// <param name="enviarEvento">Normalmente <see cref="ServicioInteracciones.RegistrarEventoAsync"/>.</param>
        public CanecaSimulada(
            int idContenedor,
            Func<EventoRecibido, CancellationToken, Task<Resultado<EventoRegistrado>>> enviarEvento,
            TimeProvider reloj,
            IRegistroEjecucionDispositivo? registro = null)
        {
            if (idContenedor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(idContenedor), "La caneca debe tener un identificador positivo.");
            }

            _idContenedor = idContenedor;
            _enviarEvento = enviarEvento ?? throw new ArgumentNullException(nameof(enviarEvento));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _registro = registro;
        }

        public string Canal => "simulador";

        /// <summary>Escenario que se reproduce en la próxima ejecución.</summary>
        public EscenarioCaneca Escenario { get; set; } = EscenarioCaneca.Normal;

        /// <summary>Tiempo con la tapa abierta antes de los siguientes eventos; en una demostración reproduce los 10 segundos.</summary>
        public TimeSpan VentanaDeposito { get; set; } = TimeSpan.Zero;

        /// <summary>Peso que informa la báscula en la detección, si la caneca la tiene; null si no hay báscula.</summary>
        public decimal? PesoMedidoKg { get; set; }

        /// <summary>Comandos recibidos, en el orden en que llegaron (sin repetidos por orden).</summary>
        public IReadOnlyList<ComandoApertura> ComandosRecibidos
        {
            get
            {
                lock (_bloqueo)
                {
                    return _ordenLlegada.ToList();
                }
            }
        }

        public Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(comando, nameof(comando));
            cancellationToken.ThrowIfCancellationRequested();
            if (Escenario == EscenarioCaneca.Desconectada)
            {
                return Task.FromResult(false);
            }

            // Un comando para otra caneca no se acepta: el ESP32 solo obedece órdenes de su propio contenedor.
            if (comando.IdContenedor != _idContenedor || comando.IdOrden <= 0
                || Compat.IsNullOrWhiteSpace(comando.ClaveIdempotencia) || comando.ClaveIdempotencia.Length > 80
                || comando.VenceUtc.Kind != DateTimeKind.Utc || comando.VenceUtc <= _reloj.GetUtcNow().UtcDateTime)
            {
                return Task.FromResult(false);
            }

            lock (_bloqueo)
            {
                if (_comandos.TryGetValue(comando.IdOrden, out var previo))
                {
                    return Task.FromResult(previo == comando);
                }
                if (_comandos.TryAdd(comando.IdOrden, comando))
                {
                    _ordenLlegada.Add(comando);
                }
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Reproduce el escenario para una orden recibida y devuelve lo que respondió el servicio a cada evento. No hace
        /// nada (lista vacía) si la orden no llegó por el canal, si ya se ejecutó (la misma clave de idempotencia no abre
        /// dos veces) o si el comando venció según el reloj de la caneca.
        /// </summary>
        public async Task<IReadOnlyList<Resultado<EventoRegistrado>>> EjecutarAsync(int idOrden, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ComandoApertura? comando;
            lock (_bloqueo)
            {
                if (!_comandos.TryGetValue(idOrden, out comando)
                    || _reloj.GetUtcNow().UtcDateTime >= comando.VenceUtc
                    || !_clavesEjecutadas.Add(comando.ClaveIdempotencia)
                    || (_registro is not null && !_registro.IntentarReservar(comando)))
                {
                    return Array.Empty<Resultado<EventoRegistrado>>();
                }
            }

            if (_reloj.GetUtcNow().UtcDateTime >= comando.VenceUtc)
            {
                return Array.Empty<Resultado<EventoRegistrado>>();
            }

            // Copias locales: un cambio de escenario o de peso desde otro hilo no mezcla dos secuencias.
            EscenarioCaneca escenario = Escenario;
            decimal? pesoMedido = PesoMedidoKg;
            var respuestas = new List<Resultado<EventoRegistrado>>();
            foreach (TipoEventoDispositivo tipo in Secuencia(escenario))
            {
                EventoRecibido evento = Evento(idOrden, tipo, pesoMedido);
                respuestas.Add(await _enviarEvento(evento, cancellationToken));
                if (escenario == EscenarioCaneca.Duplicados)
                {
                    respuestas.Add(await _enviarEvento(evento, cancellationToken));
                }
            
                if (tipo == TipoEventoDispositivo.Apertura && VentanaDeposito > TimeSpan.Zero)
                {
                    await Task.Delay(VentanaDeposito, cancellationToken);
                }
            }

            return respuestas;
        }

        private static TipoEventoDispositivo[] Secuencia(EscenarioCaneca escenario) => escenario switch
        {
            EscenarioCaneca.Normal or EscenarioCaneca.Duplicados =>
                new TipoEventoDispositivo[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.Deteccion, TipoEventoDispositivo.Cierre },
            EscenarioCaneca.AcuseSinApertura => new TipoEventoDispositivo[] { TipoEventoDispositivo.Acuse },
            EscenarioCaneca.AperturaSinDeteccion =>
                new TipoEventoDispositivo[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.Cierre },
            EscenarioCaneca.Fallo => new TipoEventoDispositivo[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Fallo },
            EscenarioCaneca.SinSensor =>
                new TipoEventoDispositivo[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.CierreSinSensor },
            EscenarioCaneca.FueraDeOrden =>
                new TipoEventoDispositivo[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Deteccion, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.Cierre },
            _ => Array.Empty<TipoEventoDispositivo>(),
        };

        /// <summary>Identidad estable por caneca, orden y tipo; reiniciar no causa colisiones entre órdenes distintas.</summary>
        private EventoRecibido Evento(int idOrden, TipoEventoDispositivo tipo, decimal? pesoMedido)
        {
            decimal? peso = tipo == TipoEventoDispositivo.Deteccion ? pesoMedido : null;
            return new EventoRecibido(_idContenedor, $"sim-{_idContenedor}-{idOrden}-{(int)tipo}", tipo, idOrden, _reloj.GetUtcNow().UtcDateTime, peso);
        }
    }
}
