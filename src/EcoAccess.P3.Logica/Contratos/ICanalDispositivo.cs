using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Orden de apertura tal como viaja a la caneca. No lleva datos de la persona ni del material: la caneca solo
    /// necesita saber qué orden abrir y hasta cuándo vale.
    /// </summary>
    /// <param name="ClaveIdempotencia">La caneca la usa para no abrir dos veces si recibe el mismo comando repetido.</param>
    /// <param name="VenceUtc">Después de esta hora la caneca no debe abrir aunque reciba el comando.</param>
    public sealed record ComandoApertura(int IdOrden, int IdContenedor, string ClaveIdempotencia, DateTime VenceUtc);

    /// <summary>
    /// Canal hacia la caneca, independiente del medio (simulador, HTTP local, Serial/USB). Solo
    /// entrega comandos: no tiene acceso a repositorios ni a SQL, y la IA no lo usa. Lo que pase después (acuse,
    /// apertura, detección) llega como eventos por <see cref="Servicios.ServicioInteracciones.RegistrarEventoAsync"/>.
    /// </summary>
    public interface ICanalDispositivo
    {
        /// <summary>Nombre del canal para trazabilidad (por ejemplo «simulador»); no es un secreto.</summary>
        string Canal { get; }

        /// <summary>
        /// True si el canal entregó el comando a la caneca. Entregarlo no significa que la caneca se abrió ni que
        /// recibió un residuo: eso solo lo prueban los eventos del dispositivo.
        /// </summary>
        /// <exception cref="OperationCanceledException">Si se cancela o vence el tiempo.</exception>
        Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default);
    }
}
