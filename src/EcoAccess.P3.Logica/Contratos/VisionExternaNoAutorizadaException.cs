using System;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>El análisis solo podría continuar enviando la foto a un servicio externo y la persona no lo ha autorizado.</summary>
    public sealed class VisionExternaNoAutorizadaException : Exception
    {
        public VisionExternaNoAutorizadaException() : base("La foto no se envió: falta la autorización para el servicio externo.") { }
    }
}
