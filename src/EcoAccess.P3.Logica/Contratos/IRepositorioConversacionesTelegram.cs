using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Estado del diálogo con el bot (CONVERSACION_TELEGRAM) y registro de mensajes ya procesados (UPDATE_TELEGRAM).
    /// </summary>
    public interface IRepositorioConversacionesTelegram
    {
        /// <summary>Conversación de esa cuenta de Telegram, o null si nunca escribió.</summary>
        Task<ConversacionTelegram?> ObtenerAsync(long telegramUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserta la conversación si su versión es 0; si no, la actualiza solo si la versión guardada sigue siendo la
        /// misma y la incrementa.
        /// </summary>
        /// <exception cref="CambioConcurrenteException">Si otro mensaje la cambió antes, o ya existía al insertarla.</exception>
        Task GuardarAsync(ConversacionTelegram conversacion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Anota el identificador del mensaje de Telegram. Devuelve false si ya estaba: Telegram reenvía un mensaje
        /// cuando no recibe confirmación, y el bot no debe procesarlo dos veces.
        /// </summary>
        Task<bool> RegistrarUpdateAsync(long updateId, DateTime recibidoUtc, CancellationToken cancellationToken = default);
    }
}
