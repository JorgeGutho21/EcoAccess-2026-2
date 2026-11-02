using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Logica.Dobles
{

    internal sealed class OpinionesEnMemoria : IRepositorioOpiniones
    {
        internal ConcurrentDictionary<(string, DateTime), Opinion> Filas { get; } = new();
        public Task<Opinion?> ObtenerAsync(string participante, DateTime diaUtc, CancellationToken ct = default) =>
            Task.FromResult(Filas.TryGetValue((participante, diaUtc.Date), out var opinion) ? opinion : null);
        public Task AgregarAsync(Opinion opinion, CancellationToken ct = default)
        {
            if (!Filas.TryAdd((opinion.Participante, opinion.FechaUtc.Date), opinion)) throw new ConflictoUnicidadException("Repetida");
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<TendenciaOpiniones>> ResumirAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TendenciaOpiniones>>(Filas.Values.Where(o => o.FechaUtc >= desdeUtc && o.FechaUtc < hastaUtc)
                .GroupBy(o => o.FechaUtc.Date).Select(g => new TendenciaOpiniones(g.Key, g.Count(), g.Average(o => (decimal)o.Estrellas), g.Count(o => o.IdUsuario is null))).ToList());
    }
}
