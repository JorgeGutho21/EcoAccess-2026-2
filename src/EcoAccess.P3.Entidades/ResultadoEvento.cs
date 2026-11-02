namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Aplicado: el evento hizo avanzar una orden vigente. Revision: se guardó para auditoría sin cambiar nada
    /// (tardío, repetido, fuera de orden o sin orden vigente; RN-09). Los valores son explícitos porque se guardarán
    /// como número en Oracle.
    /// </summary>
    public enum ResultadoEvento
    {
        Aplicado = 1,
        Revision = 2
    }
}
