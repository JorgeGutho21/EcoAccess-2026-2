using System;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// La lanza un repositorio cuando, al volver a comprobar una regla dentro de su transacción, encuentra que otra
    /// sesión cambió los datos después de la lectura del servicio y la operación ya no está permitida. El repositorio
    /// deshace su transacción; el mensaje es apto para mostrarse a la persona usuaria. También la lanza cuando la
    /// espera de un bloqueo se agota o Oracle detecta un interbloqueo (ORA-00054 u ORA-30006, ORA-00060).
    /// </summary>
    public sealed class CambioConcurrenteException : Exception
    {
        public CambioConcurrenteException(string mensaje)
            : base(mensaje) { }

        public CambioConcurrenteException(string mensaje, Exception causa)
            : base(mensaje, causa) { }
    }
}
