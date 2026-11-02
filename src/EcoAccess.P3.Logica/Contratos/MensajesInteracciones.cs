namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Mensajes de las reglas de apertura que se comprueban en el servicio y otra vez en el repositorio, dentro de la
    /// transacción que emite la orden. Compartirlos hace que la persona lea el mismo texto en ambos casos.
    /// </summary>
    public static class MensajesInteracciones
    {
        public const string CanecaNoEnServicio = "La caneca no está en servicio; no puede abrirse (RN-05).";

        public const string CanecaNoAdmiteMaterial = "La caneca no admite ese material (RN-06).";

        public const string CanecaOcupada = "La caneca está atendiendo otra solicitud; intente de nuevo en unos minutos (RN-16).";

        public const string SolicitudVencida = "La solicitud venció. Escanee de nuevo el QR para iniciar otra.";

        public const string InteraccionNoIniciada = "La solicitud ya fue autorizada, rechazada o cerrada.";

        public const string OrdenNoVigente = "La orden de apertura de esta solicitud ya venció o se cerró.";

        public const string DepositoYaVerificado = "El depósito ya fue verificado.";
    }
}
