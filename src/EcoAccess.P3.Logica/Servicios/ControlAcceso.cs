using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Recarga al actor en cada operación y comprueba que siga activo y tenga el rol pedido (RN-15).
    /// No se confía en lo que la GUI recuerde de la sesión: si un administrador desactiva una cuenta, la
    /// siguiente operación de esa cuenta ya se rechaza. Es interna porque solo la usan los servicios.
    /// </summary>
    internal sealed class ControlAcceso
    {
        internal const string MensajeSinConsentimiento =
            "La cuenta no ha aceptado el tratamiento de datos; no puede solicitar aperturas.";

        private readonly IRepositorioUsuarios _usuarios;

        public ControlAcceso(IRepositorioUsuarios usuarios)
        {
            _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
        }

        /// <summary>Devuelve null si el actor puede operar, o el rechazo que corresponde.</summary>
        public Task<Resultado?> ExigirRolAsync(int idActor, CodigoRol rol, CancellationToken cancellationToken) =>
            ExigirAlgunRolAsync(idActor, cancellationToken, rol);

        /// <summary>Como <see cref="ExigirRolAsync"/>, pero basta con tener uno de los roles.</summary>
        public async Task<Resultado?> ExigirAlgunRolAsync(int idActor, CancellationToken cancellationToken, params CodigoRol[] roles) =>
            (await CargarAsync(idActor, roles, cancellationToken)).Rechazo;

        /// <summary>
        /// Ciudadano activo que aceptó el tratamiento de datos (RN-02). Es parcial: el contacto verificado se
        /// comprobará cuando exista el vínculo con Telegram.
        /// </summary>
        public async Task<Resultado?> ExigirCiudadanoHabilitadoAsync(int idActor, CancellationToken cancellationToken)
        {
            (Usuario? actor, Resultado? rechazo) = await CargarAsync(idActor, new CodigoRol[] { CodigoRol.Ciudadano }, cancellationToken);
            if (rechazo is not null)
            {
                return rechazo;
            }

            return actor!.TieneConsentimiento ? null : Resultado.Rechazo(MotivoRechazo.SinPermiso, MensajeSinConsentimiento);
        }

        private async Task<(Usuario? Actor, Resultado? Rechazo)> CargarAsync(
            int idActor,
            CodigoRol[] roles,
            CancellationToken cancellationToken)
        {
            Usuario? actor = idActor > 0 ? await _usuarios.ObtenerPorIdAsync(idActor, cancellationToken) : null;

            if (actor is null)
            {
                return (null, Resultado.Rechazo(MotivoRechazo.ActorNoEncontrado, "No se encontró la cuenta que intenta realizar la operación."));
            }

            if (!actor.Activo)
            {
                return (actor, Resultado.Rechazo(MotivoRechazo.ActorInactivo, "La cuenta está desactivada."));
            }

            if (!roles.Any(actor.TieneRol))
            {
                return (actor, Resultado.Rechazo(MotivoRechazo.SinPermiso, $"La operación requiere el rol {string.Join(" o ", roles)}."));
            }

            return (actor, null);
        }
    }
}
