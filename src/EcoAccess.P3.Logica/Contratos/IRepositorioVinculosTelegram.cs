using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Vinculación de una cuenta EXISTENTE (P-15, tabla VINCULACION_PENDIENTE de V007). La página autenticada la
    /// prepara para el token del enlace; el bot la confirma cuando el mismo Telegram escribe el código mostrado en la
    /// página. Solo se guarda el hash del código. Una por token: preparar de nuevo reemplaza la anterior.
    /// </summary>
    public sealed record VinculacionPendiente(string HashToken, long TelegramUserId, int IdUsuario, string HashCodigo,
        DateTime CreacionUtc, DateTime VenceUtc, int Intentos);

    /// <summary>
    /// Vínculos entre cuentas y Telegram, y tokens de registro (tablas VINCULO_TELEGRAM y TOKEN_VINCULACION).
    /// El registro desde Telegram crea la cuenta, la vincula y gasta el token en una sola transacción: si algo
    /// falla no queda una cuenta sin vínculo ni un token usado sin cuenta.
    /// </summary>
    public interface IRepositorioVinculosTelegram
    {
        /// <summary>Vínculo vigente de esa cuenta de Telegram, o null.</summary>
        Task<VinculoTelegram?> ObtenerVinculoActivoPorTelegramAsync(long telegramUserId, CancellationToken cancellationToken = default);

        /// <summary>Vínculo vigente de esa cuenta de EcoAccess, o null.</summary>
        Task<VinculoTelegram?> ObtenerVinculoActivoPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

        /// <summary>Guarda un token recién emitido.</summary>
        /// <exception cref="ConflictoUnicidadException">Si el hash ya existe (prácticamente imposible con 256 bits).</exception>
        Task AgregarTokenAsync(TokenVinculacion token, CancellationToken cancellationToken = default);

        /// <summary>Token por su hash, o null.</summary>
        Task<TokenVinculacion?> ObtenerTokenAsync(string hashToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea la cuenta (con su consentimiento), crea el vínculo y marca el token como usado, todo junto.
        /// Devuelve el id de la cuenta. Bloquea el token y vuelve a comprobar que siga vigente.
        /// </summary>
        /// <exception cref="CambioConcurrenteException">Si el token ya se usó o venció entre la lectura y la escritura.</exception>
        /// <exception cref="ConflictoUnicidadException">Si el correo ya existe o esa cuenta de Telegram ya tiene un vínculo activo.</exception>
        Task<int> RegistrarCiudadanoYVincularAsync(
            Usuario usuario,
            string hashToken,
            long telegramUserId,
            DateTime ahoraUtc,
            CancellationToken cancellationToken = default);

        /// <summary>Cierra el vínculo activo de la cuenta. Si no tenía, no hace nada.</summary>
        Task DarDeBajaAsync(int idUsuario, DateTime fechaUtc, CancellationToken cancellationToken = default);

        /// <summary>Guarda o reemplaza la vinculación pendiente de ese token, con los intentos en cero.</summary>
        Task GuardarVinculacionPendienteAsync(VinculacionPendiente pendiente, CancellationToken cancellationToken = default);

        /// <summary>
        /// La vinculación pendiente más reciente de esa cuenta de Telegram que siga vigente en <paramref name="ahoraUtc"/>,
        /// con su token sin usar; null si no hay.
        /// </summary>
        Task<VinculacionPendiente?> ObtenerVinculacionPendienteAsync(long telegramUserId, DateTime ahoraUtc,
            CancellationToken cancellationToken = default);

        /// <summary>Suma un intento fallido de forma atómica y devuelve el total.</summary>
        Task<int> RegistrarIntentoFallidoAsync(string hashToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// En una transacción: bloquea token y pendiente, exige que la pendiente siga siendo la verificada (mismo hash de
        /// código y misma cuenta, para que una preparación simultánea no la sustituya), vuelve a comprobar vigencia,
        /// intentos, cuenta activa con rol Ciudadano y consentimiento, crea el vínculo, marca el token como usado por esa
        /// cuenta y borra la pendiente. No agrega ni quita roles. Devuelve el id de la cuenta.
        /// </summary>
        /// <exception cref="CambioConcurrenteException">Si algo dejó de cumplirse entre la lectura y la escritura.</exception>
        /// <exception cref="ConflictoUnicidadException">Si la cuenta o ese Telegram ya tienen un vínculo activo.</exception>
        Task<int> ConfirmarVinculacionPendienteAsync(VinculacionPendiente verificada, DateTime ahoraUtc,
            CancellationToken cancellationToken = default);
    }
}
