using System;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Límite de frecuencia por clave (por ejemplo, por cuenta de Telegram), contra el abuso del bot. Devuelve
    /// false cuando la clave superó su cupo en la ventana vigente.
    /// </summary>
    public interface ILimitadorUso
    {
        bool Permitir(string clave, DateTime ahoraUtc);
    }
}
