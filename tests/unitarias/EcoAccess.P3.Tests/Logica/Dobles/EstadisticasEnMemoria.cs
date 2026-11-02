using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Logica.Dobles
{

    /// <summary>
    /// Repositorio de estadísticas que devuelve respuestas fijas y anota el filtro recibido. Sirve para probar que el
    /// servicio valida antes de delegar; el SQL de agregación lo cubren las pruebas de integración contra Oracle.
    /// </summary>
    internal sealed class RepositorioEstadisticasEnMemoria : IRepositorioEstadisticas
    {
        public static readonly ResumenActividad ResumenFijo = new(
            new ConteoInteracciones(5, 0, 1, 2, 1, 1, 3, 1, 1),
            new ConteoOrdenes(4, 1, 2, 1, 0, 3, 2),
            new ConteoEventos(6, 5, 1, 0),
            new ResumenDepositos(2, 1, 1, 1, 0.5m, 1, 0.3m));

        public int Llamadas { get; private set; }
        public List<FiltroEstadisticas> Filtros { get; } = new List<FiltroEstadisticas>();
        public Action? AlConsultar { get; set; }
        public FiltroEstadisticas? UltimoFiltro { get; private set; }
        public AgrupacionDepositos? UltimaAgrupacion { get; private set; }
        public (int Pagina, int Tamano)? UltimaPagina { get; private set; }

        /// <summary>Depósitos que pagina <see cref="ListarDepositosAsync"/>; vacío por defecto.</summary>
        public List<FilaDeposito> Depositos { get; } = new List<FilaDeposito>();

        public Task<ResumenActividad> ObtenerResumenAsync(FiltroEstadisticas filtro, CancellationToken cancellationToken = default)
        {
            Anotar(filtro, cancellationToken);
            return Task.FromResult(ResumenFijo);
        }

        public Task<IReadOnlyList<FilaAgrupada>> AgruparDepositosAsync(
            FiltroEstadisticas filtro,
            AgrupacionDepositos agrupacion,
            CancellationToken cancellationToken = default)
        {
            Anotar(filtro, cancellationToken);
            UltimaAgrupacion = agrupacion;
            IReadOnlyList<FilaAgrupada> filas = new FilaAgrupada[] { new FilaAgrupada(1, null, "PET", 2, 1, 1, 0.5m, 1, 0.3m) };
            return Task.FromResult(filas);
        }

        public Task<IReadOnlyList<FilaFallosContenedor>> ListarFallosPorContenedorAsync(
            FiltroEstadisticas filtro,
            CancellationToken cancellationToken = default)
        {
            Anotar(filtro, cancellationToken);
            IReadOnlyList<FilaFallosContenedor> filas = new FilaFallosContenedor[] { new FilaFallosContenedor(7, "Caneca 7", 1, 2, 0, 1) };
            return Task.FromResult(filas);
        }

        public Task<PaginaDepositos> ListarDepositosAsync(
            FiltroEstadisticas filtro,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
        {
            Anotar(filtro, cancellationToken);
            UltimaPagina = (pagina, tamanoPagina);
            // El salto se calcula en long, igual que en Oracle, para que una página enorme no desborde.
            long salto = (long)(pagina - 1) * tamanoPagina;
            IReadOnlyList<FilaDeposito> filas = salto >= Depositos.Count ? new List<FilaDeposito>() : Depositos.Skip((int)salto).Take(tamanoPagina).ToList();
            return Task.FromResult(new PaginaDepositos(filas, Depositos.Count, pagina, tamanoPagina));
        }

        private void Anotar(FiltroEstadisticas filtro, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Llamadas++;
            Filtros.Add(filtro);
            UltimoFiltro = filtro;
            AlConsultar?.Invoke();
        }
    }
}
