namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Estado operativo de una caneca. Solo EnServicio recibe órdenes de apertura (RN-05).
    /// DadoDeBaja es definitivo: la caneca deja de usarse pero su historial se conserva.
    /// Los valores son explícitos porque se guardarán como número en Oracle.
    /// </summary>
    public enum EstadoContenedor
    {
        EnServicio = 1,
        FueraDeServicio = 2,
        DadoDeBaja = 3
    }
}
