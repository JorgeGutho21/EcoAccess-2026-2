namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Mensajes de las reglas del catálogo que se comprueban dos veces: en el servicio, con la lectura previa, y en el
    /// repositorio, dentro de la transacción. Compartirlos garantiza que la persona usuaria lea el mismo texto en ambos casos.
    /// </summary>
    public static class MensajesCatalogo
    {
        public const string CanecaDadaDeBaja = "La caneca está dada de baja; no admite cambios.";

        public const string CanecaQuedariaSinMateriales =
            "La caneca en servicio quedaría sin materiales habilitados; póngala fuera de servicio primero.";

        public const string CanecaSinMaterialParaServicio =
            "La caneca no admite ningún material habilitado; no puede ponerse en servicio.";

        public const string PuntoDeCanecaInactivo = "El punto de instalación de la caneca está inactivo.";

        public const string PuntoConCanecasEnServicio =
            "El punto tiene canecas en servicio; póngalas fuera de servicio antes de desactivarlo.";

        public static string MaterialDeshabilitado(string nombre) => $"El material «{nombre}» está deshabilitado.";

        public static string UnicoMaterialDeCanecaEnServicio(string nombreCaneca) =>
            $"Es el único material habilitado de la caneca «{nombreCaneca}», que está en servicio; póngala fuera de servicio primero.";
    }
}
