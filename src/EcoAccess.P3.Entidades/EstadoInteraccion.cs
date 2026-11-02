namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Ciclo de una interacción: Iniciada → Autorizada → ConDeposito o SinDeposito, o Iniciada → Rechazada.
    /// Los tres últimos son definitivos. Los valores son explícitos porque se guardarán como número en Oracle.
    /// </summary>
    public enum EstadoInteraccion
    {
        Iniciada = 1,
        Autorizada = 2,
        ConDeposito = 3,
        SinDeposito = 4,
        Rechazada = 5
    }
}
