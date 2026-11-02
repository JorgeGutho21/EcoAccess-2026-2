using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>
    /// Update ya traducido. <see cref="Mensaje"/> es null cuando el update no interesa al bot (mensaje editado,
    /// sticker, grupo sin texto…): aun así su id cuenta para avanzar el offset. <see cref="IdBoton"/> es el id del
    /// callback_query que hay que contestar para que Telegram quite el reloj del botón.
    /// </summary>
    public sealed record UpdateRecibido(long UpdateId, MensajeEntrante? Mensaje, string? IdBoton);

    /// <summary>
    /// Adaptador de Telegram. El bot solo habla con esta interfaz, de modo que el cliente HTTP real se
    /// cambia por <see cref="ClienteTelegramSimulado"/> o por el chat de consola sin tocar la lógica.
    /// </summary>
    public interface IClienteTelegram
    {
        /// <summary>Long polling: espera hasta <paramref name="espera"/> por updates con id mayor o igual a <paramref name="offset"/>.</summary>
        Task<IReadOnlyList<UpdateRecibido>> ObtenerUpdatesAsync(long offset, TimeSpan espera, CancellationToken cancellationToken);

        Task EnviarAsync(RespuestaBot respuesta, CancellationToken cancellationToken);

        Task ResponderBotonAsync(string idBoton, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Falla al hablar con Telegram (red, HTTP o respuesta ilegible). El mensaje nunca incluye el token ni la URL.
    /// <see cref="EsperaSugerida"/> trae el retry_after de una respuesta 429.
    /// </summary>
    public sealed class ErrorTelegramException
        : Exception
    {
        public ErrorTelegramException(string message, TimeSpan? esperaSugerida = null, bool esConflicto = false, bool esTokenRechazado = false, bool esDestinoNoDisponible = false) : base(message)
        {
            EsperaSugerida = esperaSugerida;
            EsConflicto = esConflicto;
            EsTokenRechazado = esTokenRechazado;
            EsDestinoNoDisponible = esDestinoNoDisponible;
        }

        public TimeSpan? EsperaSugerida { get; }

        /// <summary>409: hay un webhook activo u otra instancia haciendo polling con el mismo token.</summary>
        public bool EsConflicto { get; }

        /// <summary>401 o 404: el token fue revocado o no existe; reintentar no sirve.</summary>
        public bool EsTokenRechazado { get; }

        /// <summary>400/403 al enviar un mensaje: el chat no existe o bloqueó al bot.</summary>
        public bool EsDestinoNoDisponible { get; }
    }
}
