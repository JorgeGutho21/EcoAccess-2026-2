using System;

namespace EcoAccess.P3.Bot
{

    internal static class RegistroTecnicoSeguro
    {
        internal static Action<string> Proteger(Action<string> registrar)
        {
            Compat.ThrowIfNull(registrar, nameof(registrar));
            return mensaje => { try { registrar(mensaje); } catch (Exception) { /* El registro no es una operación de negocio. */ } };
        }
    }
}
