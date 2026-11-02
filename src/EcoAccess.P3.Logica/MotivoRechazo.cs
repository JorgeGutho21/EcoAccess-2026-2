namespace EcoAccess.P3.Logica
{

    /// <summary>
    /// Razones por las que un servicio dice «no» a una operación. La GUI y el bot traducen
    /// cada valor a un mensaje para la persona; el texto de <see cref="Resultado.Mensaje"/> da el detalle.
    /// </summary>
    public enum MotivoRechazo
    {
        /// <summary>El identificador del actor no corresponde a ninguna cuenta.</summary>
        ActorNoEncontrado = 1,

        /// <summary>La cuenta del actor existe pero está desactivada (RN-15).</summary>
        ActorInactivo = 2,

        /// <summary>El actor no tiene el rol que exige la operación (matriz de permisos).</summary>
        SinPermiso = 3,

        /// <summary>El registro sobre el que se quiere operar no existe.</summary>
        NoEncontrado = 4,

        /// <summary>Ya existe un registro con ese valor único (código, nombre o identificador).</summary>
        Duplicado = 5,

        /// <summary>Los datos recibidos no cumplen el formato o las longitudes de la entidad.</summary>
        DatosInvalidos = 6,

        /// <summary>El estado actual del registro no permite la operación.</summary>
        EstadoNoPermitido = 7,

        /// <summary>
        /// El inicio de sesión o la verificación de la contraseña actual falló. Agrupa a propósito correo inexistente,
        /// contraseña errada, cuenta inactiva o sin rol y credencial bloqueada, para no revelar qué cuentas existen.
        /// </summary>
        CredencialesInvalidas = 8
    }
}
