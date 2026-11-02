using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    public sealed record IndicadoresPanel(long Contenedores, long EnServicio, long MaterialesHabilitados,
        long Depositos, long Verificados, long Interacciones, long EventosConIncidencia, long SaldoNetoEcopuntos,
        long CreditosOtorgados, long Ciudadanos = 0, long ClasificacionesIa = 0);

    public sealed record ActividadPanel(int IdDeposito, DateTime FechaUtc, string Material, bool Verificado, string? Caneca = null);

    public sealed record ResumenPanelAdministrativo(IndicadoresPanel Indicadores, IReadOnlyList<ActividadPanel> Actividad);

    /// <summary>Resumen global de catálogos y hechos; no contiene identidades de ciudadanos.</summary>
    public interface IRepositorioPanelAdministrativo
    {
        Task<ResumenPanelAdministrativo> ConsultarAsync(CancellationToken cancellationToken = default);
    }
}
