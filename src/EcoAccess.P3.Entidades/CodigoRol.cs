namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Roles de negocio aprobados. Los valores numéricos son explícitos porque
    /// coinciden con la columna CODIGO del catálogo ROL en Oracle: reordenar la lista no debe cambiarlos.
    /// </summary>
    public enum CodigoRol
    {
        Ciudadano = 1,
        Operario = 2,
        Administrador = 3
    }
}
