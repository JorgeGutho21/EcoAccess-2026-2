namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Por dónde llegó la solicitud de apertura. Manual es la apertura que registra un operario (CU-17).
    /// Los valores son explícitos porque se guardarán como número en Oracle.
    /// </summary>
    public enum CanalInteraccion
    {
        Telegram = 1,
        Manual = 2,
        Simulador = 3,
        // Mantiene el aislamiento de la sesión Telegram frente al visitante de escritorio.
        TelegramSimulado = 4
    }
}
