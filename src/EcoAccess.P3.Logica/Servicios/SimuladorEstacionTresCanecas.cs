using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Falla que el simulador reproduce en la próxima orden de una caneca.</summary>
    public enum FallaSimulada
    {
        Ninguna = 0,
        NoAbre = 1,
        NoCierra = 2,
        SinRespuesta = 3,
    }

    /// <summary>
    /// ESP32 simulado de una estación de tres canecas (blanca, negra, verde), sin puertos, sin red y sin motores. Reproduce:
    /// conexión y desconexión, reinicio, orden recibida y autorizada, apertura, ventana abierta, cierre, fallos de apertura y
    /// de cierre, falta de respuesta, comandos duplicados por clave de idempotencia, órdenes vencidas o de otra caneca, y
    /// la regla física de la estación real: una sola tapa se mueve a la vez (comparten el mismo COM). La ventana se mide
    /// con el <see cref="TimeProvider"/> recibido, así que las pruebas no esperan diez segundos reales.
    /// <para>Una prueba con este simulador no verifica nada mecánico: ni ángulos, ni GPIO, ni la fuente de los servos.</para>
    /// </summary>
    public sealed class SimuladorEstacionTresCanecas : IEjecutorAperturas
    {
        /// <summary>GPIO configurado en la estación física (commit 1fc2443): blanca 19, negra 18, verde 23. Solo se informa.</summary>
        public static readonly IReadOnlyDictionary<DestinoResiduo, int> GpioConfigurado = new Dictionary<DestinoResiduo, int>
        {
            [DestinoResiduo.Blanca] = 19, [DestinoResiduo.Negra] = 18, [DestinoResiduo.Verde] = 23,
        };

        private readonly IReadOnlyDictionary<int, DestinoResiduo> _tapas;
        private readonly TimeProvider _reloj;
        private readonly TimeSpan _ventana;
        private readonly SemaphoreSlim _unaTapaALaVez = new(1, 1);
        private readonly object _bloqueo = new();
        private readonly HashSet<string> _ejecutadas = new(StringComparer.Ordinal);
        private readonly Dictionary<int, FallaSimulada> _fallas = new();
        private readonly List<(int IdContenedor, TipoEventoDispositivo Tipo, DateTimeOffset Cuando)> _bitacora = new();
        private int _enMovimiento;
        private int _maximoSimultaneo;

        /// <param name="tapas">Contenedor de cada tapa y su color. Debe haber exactamente una por color.</param>
        /// <param name="ventana">Tiempo con la tapa abierta; 10 s en la estación real.</param>
        public SimuladorEstacionTresCanecas(IReadOnlyDictionary<int, DestinoResiduo> tapas, TimeProvider reloj, TimeSpan? ventana = null)
        {
            Compat.ThrowIfNull(tapas, nameof(tapas));
            if (tapas.Count != 3 || tapas.Values.Distinct().Count() != 3
                || tapas.Values.Any(d => d is not (DestinoResiduo.Blanca or DestinoResiduo.Negra or DestinoResiduo.Verde)))
                throw new ArgumentException("La estación tiene exactamente una caneca blanca, una negra y una verde.", nameof(tapas));
            _tapas = tapas;
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _ventana = ventana ?? TimeSpan.FromSeconds(10);
        }

        public bool EsSimulado => true;

        public bool Conectada { get; private set; } = true;

        /// <summary>Veces que la estación se reinició.</summary>
        public int Reinicios { get; private set; }

        /// <summary>Máximo de tapas en movimiento al mismo tiempo durante toda la vida del simulador. Debe ser 1.</summary>
        public int MaximoTapasSimultaneas => Volatile.Read(ref _maximoSimultaneo);

        public IReadOnlyList<(int IdContenedor, TipoEventoDispositivo Tipo, DateTimeOffset Cuando)> Bitacora
        {
            get { lock (_bloqueo) return _bitacora.ToList(); }
        }

        public void Desconectar() => Conectada = false;

        public void Reconectar() => Conectada = true;

        /// <summary>Reinicio del ESP32: pierde las fallas programadas, conserva la memoria de órdenes ejecutadas (NVS).</summary>
        public void Reiniciar()
        {
            lock (_bloqueo) { _fallas.Clear(); Reinicios++; }
            Conectada = true;
        }

        /// <summary>La próxima orden de esa caneca fallará así.</summary>
        public void ProgramarFalla(int idContenedor, FallaSimulada falla)
        {
            lock (_bloqueo) _fallas[idContenedor] = falla;
        }

        public async Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(comando, nameof(comando));
            cancellationToken.ThrowIfCancellationRequested();
            if (!Conectada) return new(EstadoEjecucionApertura.Desconectada, Array.Empty<TipoEventoDispositivo>());
            if (!_tapas.ContainsKey(comando.IdContenedor) || comando.IdOrden <= 0 || string.IsNullOrWhiteSpace(comando.ClaveIdempotencia)
                || comando.VenceUtc.Kind != DateTimeKind.Utc || comando.VenceUtc <= _reloj.GetUtcNow().UtcDateTime)
                return new(EstadoEjecucionApertura.Rechazada, Array.Empty<TipoEventoDispositivo>());

            await _unaTapaALaVez.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                FallaSimulada falla;
                lock (_bloqueo)
                {
                    // Como la reserva en NVS del firmware: la misma clave nunca abre dos veces, ni después de reiniciar.
                    if (!_ejecutadas.Add(comando.ClaveIdempotencia))
                        return new(EstadoEjecucionApertura.Duplicada, Array.Empty<TipoEventoDispositivo>());
                    falla = _fallas.TryGetValue(comando.IdContenedor, out FallaSimulada f) ? f : FallaSimulada.Ninguna;
                    _fallas.Remove(comando.IdContenedor);
                }
                if (falla == FallaSimulada.SinRespuesta) return new(EstadoEjecucionApertura.SinRespuesta, Array.Empty<TipoEventoDispositivo>());

                int moviendo = Interlocked.Increment(ref _enMovimiento);
                ActualizarMaximo(moviendo);
                try
                {
                    var eventos = new List<TipoEventoDispositivo> { Registrar(comando.IdContenedor, TipoEventoDispositivo.Acuse) };
                    if (falla == FallaSimulada.NoAbre)
                    {
                        eventos.Add(Registrar(comando.IdContenedor, TipoEventoDispositivo.Fallo));
                        return new(EstadoEjecucionApertura.FalloApertura, eventos);
                    }
                    eventos.Add(Registrar(comando.IdContenedor, TipoEventoDispositivo.Apertura));
                    // La ventana es lógica: el cierre se fecha al final de la ventana sin esperar diez segundos reales.
                    await Task.Yield();
                    if (falla == FallaSimulada.NoCierra)
                    {
                        eventos.Add(Registrar(comando.IdContenedor, TipoEventoDispositivo.Fallo));
                        return new(EstadoEjecucionApertura.FalloCierre, eventos);
                    }
                    eventos.Add(Registrar(comando.IdContenedor, TipoEventoDispositivo.CierreSinSensor, _ventana));
                    return new(EstadoEjecucionApertura.Completada, eventos);
                }
                finally { Interlocked.Decrement(ref _enMovimiento); }
            }
            finally { _unaTapaALaVez.Release(); }
        }

        private TipoEventoDispositivo Registrar(int idContenedor, TipoEventoDispositivo tipo, TimeSpan despues = default)
        {
            lock (_bloqueo) _bitacora.Add((idContenedor, tipo, _reloj.GetUtcNow() + despues));
            return tipo;
        }

        private void ActualizarMaximo(int actual)
        {
            int previo;
            do { previo = Volatile.Read(ref _maximoSimultaneo); }
            while (actual > previo && Interlocked.CompareExchange(ref _maximoSimultaneo, actual, previo) != previo);
        }
    }
}
