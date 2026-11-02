using System.Collections.Generic;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Lectura privada de un ciudadano. Solicitudes, depósitos y verificación siguen siendo hechos distintos.
    /// Las consultas individuales no forman una instantánea transaccional frente a nuevas escrituras concurrentes.</summary>
    public sealed record EstadisticasPersonales(
        FiltroEstadisticas Periodo,
        FiltroEstadisticas PeriodoAnterior,
        ResumenActividad Actual,
        ResumenActividad Anterior,
        IReadOnlyList<FilaAgrupada> Materiales,
        IReadOnlyList<FilaAgrupada> Evolucion);
}
