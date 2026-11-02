namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Lo que reporta la caneca o su simulador sobre una orden.
    /// Los valores son explícitos porque se guardarán como número en Oracle.
    /// </summary>
    public enum TipoEventoDispositivo
    {
        Acuse = 1,
        Apertura = 2,
        Deteccion = 3,
        Cierre = 4,
        Fallo = 5,

        /// <summary>
        /// La tapa terminó su ventana y se cerró en una caneca sin sensor de ingreso: no se sabe si entró un residuo.
        /// El depósito queda pendiente de que un operario lo verifique.
        /// </summary>
        CierreSinSensor = 6
    }
}
