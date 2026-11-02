using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Relación entre una cuenta de EcoAccess y una cuenta de Telegram. Es el «contacto
    /// verificado» de RN-02: el identificador numérico lo entrega Telegram en cada mensaje y no se puede escribir
    /// a mano. Una cuenta tiene a lo sumo un vínculo activo y una cuenta de Telegram también; al darlo de baja la
    /// fila se conserva con su fecha de baja, para la auditoría.
    /// </summary>
    public sealed class VinculoTelegram
    {
        public int Id { get; }
        public int IdUsuario { get; }
        public long TelegramUserId { get; }
        public DateTime FechaVinculoUtc { get; }
        public DateTime? FechaBajaUtc { get; }
        public bool Activo => FechaBajaUtc is null;

        public VinculoTelegram(int id, int idUsuario, long telegramUserId, DateTime fechaVinculoUtc, DateTime? fechaBajaUtc = null)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
            IdUsuario = Validacion.IdPositivo(idUsuario, nameof(idUsuario));
            TelegramUserId = ValidarIdTelegram(telegramUserId, nameof(telegramUserId));
            FechaVinculoUtc = Validacion.FechaUtc(fechaVinculoUtc, nameof(fechaVinculoUtc));

            if (fechaBajaUtc is { } baja)
            {
                if (Validacion.FechaUtc(baja, nameof(fechaBajaUtc)) < fechaVinculoUtc)
                {
                    throw new ArgumentException("La baja no puede ser anterior al vínculo.", nameof(fechaBajaUtc));
                }

                FechaBajaUtc = baja;
            }
        }

        /// <summary>Telegram asigna identificadores de usuario positivos; 0 o negativo indica un dato corrupto.</summary>
        internal static long ValidarIdTelegram(long id, string nombreParametro)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nombreParametro, "El identificador de Telegram debe ser positivo.");
            }

            return id;
        }
    }
}
