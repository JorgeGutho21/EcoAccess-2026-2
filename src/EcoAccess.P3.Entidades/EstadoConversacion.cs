namespace EcoAccess.P3.Entidades
{

    /// <summary>Paso en que va el diálogo del bot con una persona. Los valores se guardan en Oracle.</summary>
    public enum EstadoConversacion
    {
        /// <summary>Todavía no escaneó un QR, o la solicitud anterior terminó.</summary>
        SinCaneca = 1,

        /// <summary>Escaneó una caneca en servicio y se le ofrecieron sus materiales.</summary>
        CanecaIdentificada = 2,

        /// <summary>Eligió un material y falta que confirme.</summary>
        EsperandoConfirmacion = 3,

        /// <summary>Se emitió la orden de apertura; el siguiente QR empieza de nuevo.</summary>
        OrdenEmitida = 4,
    }
}
