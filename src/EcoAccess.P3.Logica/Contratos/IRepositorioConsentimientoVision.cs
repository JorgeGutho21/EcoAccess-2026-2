using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Autorización de una cuenta de Telegram para enviar sus fotos a un servicio externo de reconocimiento. Se guarda
    /// por cuenta y por versión del aviso: un aviso nuevo exige autorizar de nuevo y revocar la deja sin efecto.
    /// </summary>
    public interface IRepositorioConsentimientoVision
    {
        Task<bool> EstaVigenteAsync(long telegramUserId, string versionAviso, CancellationToken cancellationToken = default);

        Task RegistrarAsync(long telegramUserId, string versionAviso, DateTime fechaUtc, CancellationToken cancellationToken = default);

        /// <returns>true si había una autorización vigente y quedó revocada.</returns>
        Task<bool> RevocarAsync(long telegramUserId, DateTime fechaUtc, CancellationToken cancellationToken = default);
    }

    /// <summary>Papel del servicio externo de reconocimiento en la conversación.</summary>
    public enum PoliticaVisionExterna
    {
        /// <summary>Las fotos nunca salen del equipo.</summary>
        Ninguna = 0,

        /// <summary>El servicio externo es el motor elegido: se pide autorización antes de la primera foto.</summary>
        Principal = 1,

        /// <summary>Primero el modelo local; el externo solo si el local falla o si la persona pide una segunda opinión.</summary>
        Respaldo = 2
    }
}
