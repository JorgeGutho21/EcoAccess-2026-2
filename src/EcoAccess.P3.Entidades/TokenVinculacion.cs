using System;
using System.Linq;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Permiso de un solo uso para registrar una cuenta desde Telegram. El bot genera un
    /// token aleatorio y entrega un enlace a la página de registro; aquí se guarda solo su hash SHA-256, de modo
    /// que leer la tabla no permite usar tokens vigentes. Así la persona escribe sus datos en una página HTTPS
    /// y nunca en el chat.
    /// </summary>
    public sealed class TokenVinculacion
    {
        /// <summary>SHA-256 en hexadecimal: 32 bytes son 64 caracteres.</summary>
        public const int LongitudHash = 64;

        /// <summary>Tiempo para completar el registro; después hay que pedir otro enlace al bot.</summary>
        public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);

        public string HashToken { get; }
        public long TelegramUserId { get; }
        public long ChatId { get; }
        public DateTime CreacionUtc { get; }
        public DateTime VenceUtc { get; }
        public DateTime? UsoUtc { get; }

        /// <summary>Token recién emitido: vence <see cref="Vigencia"/> después de crearse.</summary>
        public TokenVinculacion(string hashToken, long telegramUserId, long chatId, DateTime creacionUtc)
            : this(hashToken, telegramUserId, chatId, creacionUtc, Validacion.FechaUtc(creacionUtc, nameof(creacionUtc)) + Vigencia, null)
        {
        }

        /// <summary>Reconstruye un token guardado, con las mismas comprobaciones.</summary>
        public TokenVinculacion(string hashToken, long telegramUserId, long chatId, DateTime creacionUtc, DateTime venceUtc, DateTime? usoUtc)
        {
            HashToken = ValidarHash(hashToken, nameof(hashToken));
            TelegramUserId = VinculoTelegram.ValidarIdTelegram(telegramUserId, nameof(telegramUserId));
            if (chatId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chatId), "El chat de Telegram no puede ser 0.");
            }

            ChatId = chatId;
            CreacionUtc = Validacion.FechaUtc(creacionUtc, nameof(creacionUtc));
            VenceUtc = Validacion.FechaUtc(venceUtc, nameof(venceUtc));
            if (VenceUtc <= CreacionUtc)
            {
                throw new ArgumentException("El vencimiento debe ser posterior a la creación.", nameof(venceUtc));
            }

            UsoUtc = usoUtc is { } uso ? Validacion.FechaUtc(uso, nameof(usoUtc)) : null;
        }

        /// <summary>Sin usar y antes del vencimiento.</summary>
        public bool EstaVigente(DateTime ahoraUtc) =>
            UsoUtc is null && Validacion.FechaUtc(ahoraUtc, nameof(ahoraUtc)) < VenceUtc;

        private static string ValidarHash(string? hash, string nombreParametro)
        {
            if (hash is null || hash.Length != LongitudHash || !hash.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'))
            {
                throw new ArgumentException($"El hash debe tener {LongitudHash} caracteres hexadecimales en minúscula.", nombreParametro);
            }

            return hash;
        }
    }
}
