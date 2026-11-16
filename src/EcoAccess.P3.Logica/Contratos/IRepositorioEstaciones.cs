using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Logica.Contratos
{
    /// <summary>Estación de tres canecas (V021, CONFIGURACION_CANECA). Solo lectura.</summary>
    public interface IRepositorioEstaciones
    {
        /// <summary>
        /// Las canecas configuradas de la misma estación (punto de instalación) que la caneca indicada, incluida ella si
        /// está configurada. Vacío si la estación no tiene configuración: entonces funciona como caneca única.
        /// </summary>
        Task<IReadOnlyList<CanecaEstacion>> ListarEstacionDeAsync(int idContenedor, CancellationToken ct = default);
    }
}
