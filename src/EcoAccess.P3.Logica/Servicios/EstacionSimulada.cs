using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Adaptador ESP32 simulado para una estación de tres canecas: una <see cref="CanecaSimulada"/> por tapa, cada una
    /// con su propio identificador de dispositivo. Una orden se entrega solo a la tapa de su contenedor; una orden para
    /// la blanca nunca abre la verde ni la negra. No carga firmware ni mueve motores: es software.
    /// </summary>
    public sealed class EstacionSimulada : ICanalDispositivo
    {
        private readonly IReadOnlyDictionary<int, (string Dispositivo, CanecaSimulada Tapa)> _tapas;

        public EstacionSimulada(IEnumerable<(int IdContenedor, string Dispositivo, CanecaSimulada Tapa)> tapas)
        {
            Compat.ThrowIfNull(tapas, nameof(tapas));
            var lista = tapas.ToList();
            if (lista.Count == 0) throw new ArgumentException("La estación necesita al menos una tapa.", nameof(tapas));
            if (lista.Select(t => t.IdContenedor).Distinct().Count() != lista.Count
                || lista.Select(t => t.Dispositivo).Distinct(StringComparer.Ordinal).Count() != lista.Count)
                throw new ArgumentException("Cada tapa tiene un contenedor y un ESP32 propios.", nameof(tapas));
            _tapas = lista.ToDictionary(t => t.IdContenedor, t => (t.Dispositivo, t.Tapa));
        }

        public string Canal => "simulador-estacion";

        /// <summary>El ESP32 que recibiría la orden de ese contenedor; null si la estación no lo tiene.</summary>
        public string? DispositivoDe(int idContenedor) => _tapas.TryGetValue(idContenedor, out var t) ? t.Dispositivo : null;

        public Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(comando, nameof(comando));
            return _tapas.TryGetValue(comando.IdContenedor, out var t)
                ? t.Tapa.EnviarAperturaAsync(comando, cancellationToken)
                : Task.FromResult(false);
        }

        /// <summary>Reproduce la orden en la tapa que la recibió; las demás no hacen nada.</summary>
        public async Task<IReadOnlyList<Resultado<EventoRegistrado>>> EjecutarAsync(int idOrden, CancellationToken cancellationToken = default)
        {
            foreach (var (_, tapa) in _tapas.Values)
            {
                if (tapa.ComandosRecibidos.Any(c => c.IdOrden == idOrden))
                    return await tapa.EjecutarAsync(idOrden, cancellationToken);
            }
            return Array.Empty<Resultado<EventoRegistrado>>();
        }
    }
}
