namespace EcoAccess.P3.Entidades
{
    /// <summary>
    /// Qué tan comprobado está un depósito. Se deduce de los datos del depósito (no se guarda aparte): una sola fuente de
    /// verdad. Un depósito sin sensor nunca se presenta como ingreso detectado ni como pesaje real.
    /// </summary>
    public enum EstadoRegistroDeposito
    {
        /// <summary>DECLARADA_SIN_SENSOR: la tapa se abrió y cerró; la persona declaró el residuo; nadie lo comprobó.</summary>
        DeclaradaSinSensor = 1,

        /// <summary>DETECTADA_POR_SENSOR: el sensor de la caneca registró un ingreso dentro de la ventana.</summary>
        DetectadaPorSensor = 2,

        /// <summary>VERIFICADA: un operario distinto de quien depositó confirmó el material.</summary>
        Verificada = 3,
    }

    public static class EstadosRegistroDeposito
    {
        /// <summary>Código estable para informes, CSV, SQL y Power BI.</summary>
        public static string Codigo(EstadoRegistroDeposito estado) => estado switch
        {
            EstadoRegistroDeposito.DeclaradaSinSensor => "DECLARADA_SIN_SENSOR",
            EstadoRegistroDeposito.DetectadaPorSensor => "DETECTADA_POR_SENSOR",
            EstadoRegistroDeposito.Verificada => "VERIFICADA",
            _ => "DESCONOCIDO",
        };
    }
}
