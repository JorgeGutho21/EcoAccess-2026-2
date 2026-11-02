using System;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// La lanza un repositorio cuando guardar una cuenta dejaría al sistema sin ningún administrador activo (RN-23).
    /// El servicio ya lo impide con sus comprobaciones, pero dos administradores que se quitan el rol el uno al otro
    /// a la vez podrían pasarlas; el repositorio lo detecta dentro de su transacción, la deshace y lanza esta
    /// excepción para que el servicio responda <see cref="MotivoRechazo.EstadoNoPermitido"/>.
    /// </summary>
    public sealed class UltimoAdministradorException : Exception
    {
        public UltimoAdministradorException()
            : base("RN-23: el sistema debe conservar al menos un administrador activo.") { }
    }
}
