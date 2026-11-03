using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Si la caneca puede atender una orden ahora: el enlace local responde y no hay un fallo físico reciente sin resolver.
    /// Un error al comprobarlo se trata como «no disponible»; nunca como permiso para abrir.
    /// </summary>
    public interface IEstadoCaneca
    {
        Task<bool> DisponibleAsync(int idContenedor, CancellationToken cancellationToken = default);
    }
}
