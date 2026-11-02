using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Lugares físicos donde se instalan canecas (tabla PUNTO_INSTALACION).</summary>
    public interface IRepositorioPuntosInstalacion
    {
        Task<PuntoInstalacion?> ObtenerPorIdAsync(int idPunto, CancellationToken cancellationToken = default);

        /// <summary>Compara el nombre exacto (ya recortado), igual que la restricción UNIQUE de Oracle.</summary>
        Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default);

        /// <summary>Puntos ordenados por nombre.</summary>
        Task<IReadOnlyList<PuntoInstalacion>> ListarAsync(CancellationToken cancellationToken = default);

        /// <exception cref="ConflictoUnicidadException">Si el nombre ya existe.</exception>
        Task<int> AgregarAsync(PuntoInstalacion punto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda el estado activo de un punto existente. Al desactivar, vuelve a comprobar dentro de la escritura que
        /// el punto no tenga canecas en servicio, porque otra sesión pudo poner una en servicio después de la lectura.
        /// </summary>
        /// <exception cref="CambioConcurrenteException">Si al desactivar el punto tiene canecas en servicio.</exception>
        /// <exception cref="InvalidOperationException">Si el punto no existe.</exception>
        Task CambiarActivoAsync(int idPunto, bool activo, CancellationToken cancellationToken = default);
    }
}
