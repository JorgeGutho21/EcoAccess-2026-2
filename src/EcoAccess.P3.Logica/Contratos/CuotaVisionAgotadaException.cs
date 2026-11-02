using System;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>El proveedor o el límite local gratuito impiden otro intento; no autoriza usar un servicio de pago.</summary>
    public sealed class CuotaVisionAgotadaException : Exception
    {
        public CuotaVisionAgotadaException() : base("Se agotó o bloqueó la cuota gratuita de clasificación.")
        {
        }
    }
}
