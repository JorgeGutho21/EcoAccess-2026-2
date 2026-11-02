using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Catálogo de materiales (tabla MATERIAL).</summary>
    public interface IRepositorioMateriales
    {
        Task<Material?> ObtenerPorIdAsync(int idMaterial, CancellationToken cancellationToken = default);

        /// <summary>Compara con el código ya normalizado en mayúsculas.</summary>
        Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default);

        /// <summary>Materiales ordenados por código; con <paramref name="soloHabilitados"/> omite los deshabilitados.</summary>
        Task<IReadOnlyList<Material>> ListarAsync(bool soloHabilitados, CancellationToken cancellationToken = default);

        /// <summary>Consulta acotada para la administración; no sustituye el catálogo completo.</summary>
        Task<PaginaMateriales> ConsultarPaginaAsync(FiltroMateriales filtro, CancellationToken cancellationToken = default);

        /// <summary>Guarda un material nuevo y devuelve el identificador asignado.</summary>
        /// <exception cref="ConflictoUnicidadException">Si el código ya existe.</exception>
        Task<int> AgregarAsync(Material material, CancellationToken cancellationToken = default);

        /// <summary>Marca el material como habilitado. Repetirlo no cambia nada.</summary>
        /// <exception cref="InvalidOperationException">Si el material no existe.</exception>
        Task HabilitarAsync(int idMaterial, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marca el material como deshabilitado tras comprobar, en la misma transacción y con las canecas que lo
        /// admiten bloqueadas, que ninguna caneca en servicio se queda sin otro material habilitado.
        /// </summary>
        /// <exception cref="CambioConcurrenteException">Si es el único material habilitado de una caneca en servicio.</exception>
        /// <exception cref="InvalidOperationException">Si el material no existe.</exception>
        Task DeshabilitarAsync(int idMaterial, CancellationToken cancellationToken = default);
    }
}
