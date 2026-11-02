namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Ciclo de una orden de apertura (RN-07, RN-08). Del 1 al 5 la orden está vigente y bloquea la caneca;
    /// Cerrada, Vencida y Fallida son definitivos. Solo se avanza, nunca se retrocede.
    /// Los valores son explícitos porque se guardarán como número en Oracle.
    /// </summary>
    public enum EstadoOrdenApertura
    {
        Emitida = 1,
        Enviada = 2,
        Acusada = 3,
        Abierta = 4,
        Detectada = 5,
        Cerrada = 6,
        Vencida = 7,
        Fallida = 8
    }
}
