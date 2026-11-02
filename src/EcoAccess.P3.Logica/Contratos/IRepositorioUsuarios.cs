using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Acceso a las cuentas y sus roles. Lo usan el control de acceso y <c>ServicioUsuarios</c>.
    /// No guarda contraseñas: la autenticación se resolverá aparte.
    /// </summary>
    public interface IRepositorioUsuarios
    {
        /// <summary>Devuelve la cuenta con sus roles, o null si no existe.</summary>
        Task<Usuario?> ObtenerPorIdAsync(int idUsuario, CancellationToken cancellationToken = default);

        /// <summary>Indica si existe una cuenta con ese correo; se espera el correo ya normalizado en minúsculas.</summary>
        Task<bool> ExisteCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default);

        /// <summary>Todas las cuentas con sus roles, ordenadas por nombre.</summary>
        Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default);

        /// <summary>Página de cuentas completas con todos sus roles; conserva el listado interno anterior.</summary>
        Task<PaginaUsuarios> ConsultarPaginaAsync(FiltroUsuarios filtro, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda una cuenta nueva con sus roles y devuelve su identificador.
        /// Lanza <see cref="ConflictoUnicidadException"/> si el correo ya existe.
        /// </summary>
        Task<int> AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

        // Cada cambio de una cuenta existente tiene su propio método y solo escribe las columnas o filas que le
        // corresponden: así una operación no puede devolver a su valor anterior lo que otra cambió a la vez
        // (por ejemplo, que aceptar el consentimiento reactive una cuenta recién desactivada).

        /// <summary>
        /// Activa o desactiva la cuenta. Lanza <see cref="UltimoAdministradorException"/> si desactivarla dejaría al
        /// sistema sin administradores activos (RN-23) e <see cref="InvalidOperationException"/> si la cuenta no existe.
        /// </summary>
        Task CambiarActivoAsync(int idUsuario, bool activo, CancellationToken cancellationToken = default);

        /// <summary>Agrega un rol; si la cuenta ya lo tiene, no cambia nada. Lanza <see cref="InvalidOperationException"/> si la cuenta no existe.</summary>
        Task AgregarRolAsync(int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default);

        /// <summary>
        /// Quita un rol; si la cuenta no lo tiene, no cambia nada. Lanza <see cref="UltimoRolException"/> si la cuenta
        /// quedaría sin roles (RN-01), <see cref="UltimoAdministradorException"/> si el sistema quedaría sin
        /// administradores activos (RN-23) e <see cref="InvalidOperationException"/> si la cuenta no existe.
        /// </summary>
        Task QuitarRolAsync(int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda la versión aceptada y su fecha solo si la cuenta existe y está activa en ese momento (RN-03).
        /// Devuelve false si no se guardó.
        /// </summary>
        Task<bool> RegistrarConsentimientoAsync(int idUsuario, string version, DateTime fechaUtc, CancellationToken cancellationToken = default);

        /// <summary>Borra el consentimiento vigente; no toca el estado ni los roles. Si la cuenta no existe, no hace nada.</summary>
        Task RevocarConsentimientoAsync(int idUsuario, CancellationToken cancellationToken = default);
    }
}
