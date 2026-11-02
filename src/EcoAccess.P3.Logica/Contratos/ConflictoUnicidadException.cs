using System;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// La lanza un repositorio cuando la base de datos rechaza un valor repetido en una columna única
    /// (en Oracle, ORA-00001). El servicio ya consulta antes de guardar, pero dos usuarios pueden guardar
    /// a la vez; la restricción UNIQUE es la defensa final y esta excepción le permite al servicio
    /// responder <see cref="MotivoRechazo.Duplicado"/> sin conocer los códigos de Oracle.
    /// </summary>
    public sealed class ConflictoUnicidadException : Exception
    {
        public ConflictoUnicidadException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }
}
