using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{
    /// <summary>
    /// Motor visual que puede nombrar varios objetos de una foto (lista corta en inglés). La lista nunca se muestra: la
    /// interpreta <see cref="Servicios.AnalisisMultiobjeto"/>. Solo la implementa el modelo local; una foto no sale del equipo
    /// para listar objetos.
    /// </summary>
    public interface IListadorObjetosVisibles
    {
        /// <summary>La lista que nombró el motor, o null si no pudo.</summary>
        Task<string?> ListarAsync(ImagenResiduo imagen, CancellationToken cancellationToken = default);
    }
}
