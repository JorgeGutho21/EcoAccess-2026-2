using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Canecas y sus materiales admitidos (tablas CONTENEDOR y CONTENEDOR_MATERIAL), tratadas como
    /// un solo agregado: el repositorio siempre lee y guarda la caneca junto con su lista de materiales.
    /// </summary>
    public interface IRepositorioContenedores
    {
        /// <summary>Devuelve la caneca con sus materiales admitidos, o null si no existe.</summary>
        Task<Contenedor?> ObtenerPorIdAsync(int idContenedor, CancellationToken cancellationToken = default);

        /// <summary>Caneca cuyo QR contiene ese identificador (lo que llega en el enlace del bot), o null.</summary>
        Task<Contenedor?> ObtenerPorIdentificadorQrAsync(string identificadorQr, CancellationToken cancellationToken = default);

        /// <summary>Canecas ordenadas por nombre.</summary>
        Task<IReadOnlyList<Contenedor>> ListarAsync(CancellationToken cancellationToken = default);

        /// <summary>Canecas instaladas en un punto, en cualquier estado.</summary>
        Task<IReadOnlyList<Contenedor>> ListarPorPuntoAsync(int idPunto, CancellationToken cancellationToken = default);

        /// <summary>Canecas que admiten un material, en cualquier estado.</summary>
        Task<IReadOnlyList<Contenedor>> ListarPorMaterialAsync(int idMaterial, CancellationToken cancellationToken = default);

        /// <summary>Guarda una caneca nueva con sus materiales y devuelve el identificador asignado.</summary>
        /// <exception cref="ConflictoUnicidadException">Si el identificador del QR ya existe.</exception>
        Task<int> AgregarAsync(Contenedor contenedor, CancellationToken cancellationToken = default);

        // Escrituras puntuales: cada una bloquea la fila de la caneca, vuelve a comprobar sus reglas con los datos
        // confirmados y cambia solo lo suyo, para no pisar lo que otra sesión guardó entre la lectura y la escritura.
        // Orden de bloqueo común con IRepositorioMateriales: primero el material, después la caneca.

        /// <summary>Agrega un material admitido. Repetirlo no cambia nada.</summary>
        /// <exception cref="CambioConcurrenteException">Si la caneca está dada de baja o el material está deshabilitado.</exception>
        /// <exception cref="InvalidOperationException">Si la caneca o el material no existen.</exception>
        Task AdmitirMaterialAsync(int idContenedor, int idMaterial, CancellationToken cancellationToken = default);

        /// <summary>Quita un material admitido. Si la caneca no lo admitía, no cambia nada.</summary>
        /// <exception cref="CambioConcurrenteException">
        /// Si la caneca está dada de baja, o está en servicio y se quedaría sin materiales habilitados.
        /// </exception>
        /// <exception cref="InvalidOperationException">Si la caneca no existe.</exception>
        Task QuitarMaterialAsync(int idContenedor, int idMaterial, CancellationToken cancellationToken = default);

        /// <summary>Cambia el estado. Una caneca dada de baja no vuelve a otro estado.</summary>
        /// <exception cref="CambioConcurrenteException">
        /// Si la caneca está dada de baja y se pide otro estado, o se pide en servicio sin materiales habilitados
        /// o con su punto de instalación inactivo.
        /// </exception>
        /// <exception cref="InvalidOperationException">Si la caneca no existe.</exception>
        Task CambiarEstadoAsync(int idContenedor, EstadoContenedor estado, CancellationToken cancellationToken = default);
    }
}
