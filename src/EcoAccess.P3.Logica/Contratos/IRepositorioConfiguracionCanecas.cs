using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{
    /// <summary>
    /// Configuración de las canecas de una estación (tabla CONFIGURACION_CANECA, V021) y cambio de nombre de una caneca.
    /// Es un contrato aparte de <see cref="IRepositorioContenedores"/> para no obligar a cambiar sus implementaciones
    /// (principio de segregación de interfaces).
    /// </summary>
    public interface IRepositorioConfiguracionCanecas
    {
        Task<ConfiguracionCaneca?> ObtenerAsync(int idContenedor, CancellationToken cancellationToken = default);

        /// <summary>Configuraciones de las canecas de una estación (punto de instalación).</summary>
        Task<IReadOnlyList<ConfiguracionCaneca>> ListarPorPuntoAsync(int idPunto, CancellationToken cancellationToken = default);

        /// <summary>Inserta o reemplaza la configuración. Lanza <see cref="ConflictoUnicidadException"/> si el dispositivo ya está asignado.</summary>
        Task GuardarAsync(ConfiguracionCaneca configuracion, CancellationToken cancellationToken = default);

        /// <summary>Cambia el nombre visible de la caneca (CONTENEDOR.nombre).</summary>
        Task RenombrarAsync(int idContenedor, string nombre, CancellationToken cancellationToken = default);
    }
}
