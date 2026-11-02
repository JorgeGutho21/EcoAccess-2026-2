using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Estado del diálogo de una cuenta de Telegram con el bot. Guarda solo identificadores, nunca el
    /// texto de los mensajes. A diferencia de otras entidades es mutable: el servicio aplica una transición y el
    /// repositorio guarda la fila completa comparando <see cref="Version"/>, para que dos mensajes procesados a la
    /// vez no se pisen.
    /// </summary>
    public sealed class ConversacionTelegram
    {
        public long TelegramUserId { get; }
        public long ChatId { get; }
        public EstadoConversacion Estado { get; private set; }
        public int? IdContenedor { get; private set; }
        public int? IdInteraccion { get; private set; }
        public int? IdMaterialPropuesto { get; private set; }
        public DateTime FechaActualizacionUtc { get; private set; }

        /// <summary>0 mientras no se ha guardado; el repositorio la incrementa en cada escritura.</summary>
        public int Version { get; }

        /// <summary>Conversación nueva, sin caneca.</summary>
        public ConversacionTelegram(long telegramUserId, long chatId, DateTime ahoraUtc)
            : this(telegramUserId, chatId, EstadoConversacion.SinCaneca, null, null, null, ahoraUtc, 0)
        {
        }

        /// <summary>Reconstruye una conversación guardada y comprueba que los datos correspondan al estado.</summary>
        public ConversacionTelegram(
            long telegramUserId,
            long chatId,
            EstadoConversacion estado,
            int? idContenedor,
            int? idInteraccion,
            int? idMaterialPropuesto,
            DateTime fechaActualizacionUtc,
            int version)
        {
            TelegramUserId = VinculoTelegram.ValidarIdTelegram(telegramUserId, nameof(telegramUserId));
            if (chatId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chatId), "El chat de Telegram no puede ser 0.");
            }

            if (version < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version), "La versión no puede ser negativa.");
            }

            ChatId = chatId;
            Estado = Validacion.EnumDefinido(estado, nameof(estado));
            IdContenedor = IdOpcional(idContenedor, nameof(idContenedor));
            IdInteraccion = IdOpcional(idInteraccion, nameof(idInteraccion));
            IdMaterialPropuesto = IdOpcional(idMaterialPropuesto, nameof(idMaterialPropuesto));
            FechaActualizacionUtc = Validacion.FechaUtc(fechaActualizacionUtc, nameof(fechaActualizacionUtc));
            Version = version;

            bool coherente = estado switch
            {
                EstadoConversacion.SinCaneca => idContenedor is null && idInteraccion is null && idMaterialPropuesto is null,
                EstadoConversacion.CanecaIdentificada => idContenedor is not null && idMaterialPropuesto is null,
                _ => idContenedor is not null && idInteraccion is not null && idMaterialPropuesto is not null,
            };

            if (!coherente)
            {
                throw new ArgumentException("Los datos de la conversación no corresponden a su estado.", nameof(estado));
            }
        }

        /// <summary>
        /// Escaneó una caneca. La interacción es opcional porque un invitado (sin vínculo) también recibe orientación,
        /// pero no puede iniciar una solicitud. Se permite desde cualquier estado: un QR nuevo reemplaza al anterior.
        /// </summary>
        public void IdentificarCaneca(int idContenedor, int? idInteraccion, DateTime ahoraUtc)
        {
            int contenedor = Validacion.IdPositivo(idContenedor, nameof(idContenedor));
            int? interaccion = IdOpcional(idInteraccion, nameof(idInteraccion));
            DateTime ahora = Validacion.FechaUtc(ahoraUtc, nameof(ahoraUtc));

            Estado = EstadoConversacion.CanecaIdentificada;
            IdContenedor = contenedor;
            IdInteraccion = interaccion;
            IdMaterialPropuesto = null;
            FechaActualizacionUtc = ahora;
        }

        /// <summary>Eligió un material; solo tiene sentido con una solicitud iniciada.</summary>
        public void ProponerMaterial(int idMaterial, DateTime ahoraUtc)
        {
            int material = Validacion.IdPositivo(idMaterial, nameof(idMaterial));
            DateTime ahora = Validacion.FechaUtc(ahoraUtc, nameof(ahoraUtc));
            if (Estado is not (EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion) || IdInteraccion is null)
            {
                throw new InvalidOperationException("No hay una solicitud iniciada a la que proponer un material.");
            }

            Estado = EstadoConversacion.EsperandoConfirmacion;
            IdMaterialPropuesto = material;
            FechaActualizacionUtc = ahora;
        }

        /// <summary>La orden de apertura quedó emitida.</summary>
        public void MarcarOrdenEmitida(DateTime ahoraUtc)
        {
            DateTime ahora = Validacion.FechaUtc(ahoraUtc, nameof(ahoraUtc));
            if (Estado != EstadoConversacion.EsperandoConfirmacion)
            {
                throw new InvalidOperationException("Solo se emite la orden después de confirmar el material.");
            }

            Estado = EstadoConversacion.OrdenEmitida;
            FechaActualizacionUtc = ahora;
        }

        /// <summary>Vuelve al inicio: cancelación, rechazo o error que obliga a escanear de nuevo.</summary>
        public void Reiniciar(DateTime ahoraUtc)
        {
            DateTime ahora = Validacion.FechaUtc(ahoraUtc, nameof(ahoraUtc));
            Estado = EstadoConversacion.SinCaneca;
            IdContenedor = null;
            IdInteraccion = null;
            IdMaterialPropuesto = null;
            FechaActualizacionUtc = ahora;
        }

        private static int? IdOpcional(int? id, string nombreParametro) =>
            id is { } valor ? Validacion.IdPositivo(valor, nombreParametro) : null;
    }
}
