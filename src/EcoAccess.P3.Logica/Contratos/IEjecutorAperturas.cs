using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{
    /// <summary>Cómo terminó una apertura pedida a una estación.</summary>
    public enum EstadoEjecucionApertura
    {
        /// <summary>Acuse, apertura y cierre informados.</summary>
        Completada = 1,

        /// <summary>La estación rechazó la orden (otra caneca, vencida o mal formada).</summary>
        Rechazada = 2,

        /// <summary>La tapa no abrió.</summary>
        FalloApertura = 3,

        /// <summary>La tapa abrió y no informó el cierre.</summary>
        FalloCierre = 4,

        /// <summary>No hubo respuesta dentro del plazo.</summary>
        SinRespuesta = 5,

        /// <summary>La misma clave de idempotencia ya se ejecutó: no se vuelve a abrir.</summary>
        Duplicada = 6,

        /// <summary>La estación no está conectada.</summary>
        Desconectada = 7,
    }

    /// <summary>Resultado de una apertura, con los eventos que informó la estación, en orden.</summary>
    public sealed record ResultadoEjecucionApertura(EstadoEjecucionApertura Estado, IReadOnlyList<TipoEventoDispositivo> Eventos);

    /// <summary>
    /// Ejecuta aperturas de una estación de tres canecas, una a la vez. La misión nocturna del 12/11/2026 solo tiene
    /// implementación simulada (<see cref="Servicios.SimuladorEstacionTresCanecas"/>); <see cref="EsSimulado"/> lo declara
    /// y <see cref="Servicios.CoordinadorAperturasMultiples"/> rechaza cualquier ejecutor que no lo sea.
    /// </summary>
    public interface IEjecutorAperturas
    {
        bool EsSimulado { get; }

        Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken cancellationToken = default);
    }
}
