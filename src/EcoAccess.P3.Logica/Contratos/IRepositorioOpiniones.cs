using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    public sealed record TendenciaOpiniones(DateTime DiaUtc, int Cantidad, decimal Promedio, int Invitados);

    /// <summary>Una opinión por participante y día UTC, garantizada también con concurrencia en Oracle.</summary>
    public interface IRepositorioOpiniones
    {
        Task<Opinion?> ObtenerAsync(string participante, DateTime diaUtc, CancellationToken ct = default);
        /// <exception cref="ConflictoUnicidadException">Ya existe una opinión para ese participante y día.</exception>
        Task AgregarAsync(Opinion opinion, CancellationToken ct = default);
        Task<IReadOnlyList<TendenciaOpiniones>> ResumirAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
    }
}
