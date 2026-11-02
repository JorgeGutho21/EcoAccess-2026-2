using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Acceso a las credenciales de inicio de sesión. Como el resto de repositorios, cada cambio
    /// tiene su propio método y escribe solo sus columnas: así un intento fallido no puede deshacer un cambio
    /// de contraseña hecho al mismo tiempo.
    /// </summary>
    public interface IRepositorioCredenciales
    {
        /// <summary>Devuelve la credencial de la cuenta con ese correo (ya normalizado en minúsculas), o null.</summary>
        Task<CredencialAcceso?> ObtenerPorCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default);

        /// <summary>Devuelve la credencial de la cuenta, o null si todavía no tiene contraseña.</summary>
        Task<CredencialAcceso?> ObtenerPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda la primera credencial de una cuenta. Lanza <see cref="ConflictoUnicidadException"/> si ya tenía una
        /// (por ejemplo, si dos administradores la establecen a la vez).
        /// </summary>
        Task AgregarAsync(CredencialAcceso credencial, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reemplaza hash, sal, iteraciones y fecha de cambio, y reinicia intentos y bloqueo.
        /// Lanza <see cref="InvalidOperationException"/> si la cuenta no tenía credencial.
        /// </summary>
        Task ReemplazarSecretoAsync(CredencialAcceso credencial, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suma un intento fallido en una sola sentencia atómica. Si con él se llega a <paramref name="maximoIntentos"/>,
        /// bloquea la credencial hasta <paramref name="bloquearHastaUtc"/> y deja el contador en 0, para que tras el
        /// bloqueo la persona vuelva a tener todos sus intentos. Si la cuenta no tiene credencial, no hace nada.
        /// </summary>
        Task RegistrarFalloAsync(int idUsuario, int maximoIntentos, DateTime bloquearHastaUtc, CancellationToken cancellationToken = default);

        /// <summary>Deja los intentos en 0 y quita el bloqueo; se llama tras un inicio de sesión correcto.</summary>
        Task ReiniciarIntentosAsync(int idUsuario, CancellationToken cancellationToken = default);
    }
}
