using System;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// La lanza un repositorio cuando quitar un rol dejaría la cuenta sin ninguno (RN-01). El servicio ya lo impide,
    /// pero dos administradores que quitan a la vez roles distintos de la misma cuenta podrían pasar esa comprobación;
    /// el repositorio lo detecta dentro de su transacción, la deshace y lanza esta excepción.
    /// </summary>
    public sealed class UltimoRolException : Exception
    {
        public UltimoRolException()
            : base("No se puede quitar el último rol de una cuenta (RN-01).") { }
    }
}
