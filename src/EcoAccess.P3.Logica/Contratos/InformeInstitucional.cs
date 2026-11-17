using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    public sealed record FiltroInforme(DateTime DesdeUtc, DateTime HastaUtc, int? IdContenedor = null, int? IdUbicacion = null, bool SoloDemostracion = false,
        string? CodigoMaterial = null, EcoAccess.P3.Entidades.CategoriaMaterial? Categoria = null);
    public sealed record IndicadoresInforme(long Depositos, long Verificados, long Ciudadanos,
        long Acreditados, long Netos, long Opiniones, decimal? Promedio, long Solicitudes = 0, long UnidadesVerificadas = 0, long Reversos = 0, long VerificadosSinConteo = 0);
    public sealed record InformeInstitucional(IndicadoresInforme Actual, IndicadoresInforme Anterior,
        IReadOnlyList<FilaAgrupada> Dias, IReadOnlyList<FilaAgrupada> Materiales,
        IReadOnlyList<FilaAgrupada> Canecas, IReadOnlyList<FilaAgrupada> Ubicaciones);

    /// <summary>Informes sin escritura ni identidades personales; agregación en Oracle.</summary>
    public interface IRepositorioInformeInstitucional
    {
        Task<IndicadoresInforme> ConsultarAsync(FiltroInforme filtro, CancellationToken ct = default);
        Task<IReadOnlyList<FilaAgrupada>> AgruparAsync(FiltroInforme filtro, AgrupacionDepositos agrupacion, CancellationToken ct = default);
    }
}
