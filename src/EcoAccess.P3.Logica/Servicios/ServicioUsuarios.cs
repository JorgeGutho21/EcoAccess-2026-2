using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Gestión de cuentas y roles (CU-12) y consentimiento de datos personales (RN-02, Ley 1581 de 2012).
    /// Gestionar cuentas es solo para administradores y nunca puede dejar al sistema sin uno activo (RN-23).
    /// El consentimiento lo da o lo retira la propia persona, no un administrador en su nombre.
    /// </summary>
    /// <remarks>
    /// RN-23 se garantiza así: quien opera es un administrador activo (ControlAcceso) y no puede quitarse
    /// ese rol ni desactivarse; por eso siempre queda al menos él. Si dos administradores se quitan el rol
    /// el uno al otro al mismo tiempo, ambos pasarían esa comprobación; el repositorio Oracle lo detecta con un
    /// bloqueo de fila y lanza <see cref="UltimoAdministradorException"/>, que aquí se responde como
    /// <see cref="MotivoRechazo.EstadoNoPermitido"/>.
    /// El registro de auditoría de RN-24 se agregará cuando exista su tabla.
    /// </remarks>
    public sealed class ServicioUsuarios
    {
        private readonly ControlAcceso _acceso;
        private readonly IRepositorioUsuarios _usuarios;
        private readonly TimeProvider _reloj;

        public ServicioUsuarios(IRepositorioUsuarios usuarios, TimeProvider reloj)
        {
            _acceso = new ControlAcceso(usuarios);
            _usuarios = usuarios;
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        /// <summary>Registra una cuenta activa con un primer rol y devuelve su identificador.</summary>
        public async Task<Resultado<int>> RegistrarAsync(
            int idActor,
            string nombre,
            string correoElectronico,
            CodigoRol rolInicial,
            CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            Usuario usuario;
            try
            {
                usuario = new Usuario(nombre, correoElectronico, rolInicial, _reloj.GetUtcNow().UtcDateTime);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo<int>(error);
            }

            if (await _usuarios.ExisteCorreoAsync(usuario.CorreoElectronico, cancellationToken))
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, "Ya existe una cuenta con ese correo.");
            }

            try
            {
                return Resultado<int>.Exito(await _usuarios.AgregarAsync(usuario, cancellationToken));
            }
            catch (ConflictoUnicidadException)
            {
                // Otro administrador registró el mismo correo entre la consulta y la inserción.
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, "Ya existe una cuenta con ese correo.");
            }
        }

        /// <summary>Devuelve una cuenta. Exige administrador porque contiene datos personales (RN-25).</summary>
        public async Task<Resultado<Usuario>> ObtenerAsync(int idActor, int idUsuario, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return Resultado<Usuario>.DesdeRechazo(rechazo);
            }

            Usuario? usuario = await _usuarios.ObtenerPorIdAsync(idUsuario, cancellationToken);
            return usuario is null
                ? Resultado<Usuario>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa cuenta.")
                : Resultado<Usuario>.Exito(usuario);
        }

        /// <summary>Lista las cuentas. Exige administrador por la misma razón que <see cref="ObtenerAsync"/>.</summary>
        public async Task<Resultado<IReadOnlyList<Usuario>>> ListarAsync(int idActor, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return Resultado<IReadOnlyList<Usuario>>.DesdeRechazo(rechazo);
            }

            return Resultado<IReadOnlyList<Usuario>>.Exito(await _usuarios.ListarAsync(cancellationToken));
        }

        /// <summary>Para atender a una persona desde el escritorio: el personal solo busca ciudadanos activos, sin administrar cuentas.</summary>
        public async Task<Resultado<PaginaUsuarios>> BuscarCiudadanosAsync(int idActor, string? texto, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return Resultado<PaginaUsuarios>.DesdeRechazo(rechazo);
            }

            return Resultado<PaginaUsuarios>.Exito(await _usuarios.ConsultarPaginaAsync(
                new FiltroUsuarios(texto, activo: true, rol: CodigoRol.Ciudadano, tamanoPagina: 20), cancellationToken));
        }


        /// <summary>Consulta acotada de cuentas; revalida permiso administrativo en cada lectura.</summary>
        public async Task<Resultado<PaginaUsuarios>> ConsultarPaginaAsync(int idActor, FiltroUsuarios filtro,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            cancellationToken.ThrowIfCancellationRequested();
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
                return Resultado<PaginaUsuarios>.DesdeRechazo(rechazo);
            return Resultado<PaginaUsuarios>.Exito(await _usuarios.ConsultarPaginaAsync(filtro, cancellationToken));
        }

        /// <summary>Agrega un rol. Si la cuenta ya lo tiene, responde éxito sin guardar.</summary>
        public Task<Resultado> AsignarRolAsync(int idActor, int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idUsuario, usuario =>
            {
                if (!Compat.IsDefined(rol))
                {
                    return Resultado.Rechazo(MotivoRechazo.DatosInvalidos, "rol: el rol no existe.");
                }

                if (usuario.TieneRol(rol))
                {
                    return Resultado.Exito();
                }

                usuario.AsignarRol(rol);
                return null;
            }, usuario => _usuarios.AgregarRolAsync(usuario.Id, rol, cancellationToken), cancellationToken);

        /// <summary>Quita un rol. No deja la cuenta sin roles (RN-01) ni deja a un administrador quitarse el suyo (RN-23).</summary>
        public Task<Resultado> QuitarRolAsync(int idActor, int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idUsuario, usuario =>
            {
                if (!usuario.TieneRol(rol))
                {
                    return Resultado.Exito();
                }

                if (rol == CodigoRol.Administrador && usuario.Id == idActor)
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "Un administrador no puede quitarse su propio rol (RN-23).");
                }

                if (usuario.Roles.Count == 1)
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "No se puede quitar el último rol de una cuenta (RN-01).");
                }

                usuario.QuitarRol(rol);
                return null;
            }, usuario => _usuarios.QuitarRolAsync(usuario.Id, rol, cancellationToken), cancellationToken);

        public Task<Resultado> ActivarAsync(int idActor, int idUsuario, CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idUsuario, usuario =>
            {
                if (usuario.Activo)
                {
                    return Resultado.Exito();
                }

                usuario.Activar();
                return null;
            }, usuario => _usuarios.CambiarActivoAsync(usuario.Id, true, cancellationToken), cancellationToken);

        /// <summary>Desactiva una cuenta; su historial se conserva (RN-03). Nadie puede desactivarse a sí mismo (RN-23).</summary>
        public Task<Resultado> DesactivarAsync(int idActor, int idUsuario, CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idUsuario, usuario =>
            {
                if (usuario.Id == idActor)
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "Un administrador no puede desactivar su propia cuenta (RN-23).");
                }

                if (!usuario.Activo)
                {
                    return Resultado.Exito();
                }

                usuario.Desactivar();
                return null;
            }, usuario => _usuarios.CambiarActivoAsync(usuario.Id, false, cancellationToken), cancellationToken);

        /// <summary>
        /// La propia persona acepta una versión de la política de datos; la fecha la pone el reloj del sistema.
        /// Una cuenta inactiva no puede aceptar, porque no puede usar el sistema (RN-03).
        /// </summary>
        public async Task<Resultado> RegistrarConsentimientoAsync(int idActor, string version, CancellationToken cancellationToken = default)
        {
            Usuario? actor = idActor > 0 ? await _usuarios.ObtenerPorIdAsync(idActor, cancellationToken) : null;
            if (actor is null)
            {
                return Resultado.Rechazo(MotivoRechazo.ActorNoEncontrado, "No se encontró la cuenta que intenta realizar la operación.");
            }

            if (!actor.Activo)
            {
                return Resultado.Rechazo(MotivoRechazo.ActorInactivo, "La cuenta está desactivada.");
            }

            try
            {
                actor.RegistrarConsentimiento(version, _reloj.GetUtcNow().UtcDateTime);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo(error);
            }

            // El repositorio solo guarda si la cuenta sigue activa: un administrador pudo desactivarla después de leerla.
            try
            {
                if (!await _usuarios.RegistrarConsentimientoAsync(actor.Id, actor.VersionConsentimiento!, actor.FechaConsentimientoUtc!.Value, cancellationToken))
                {
                    return Resultado.Rechazo(MotivoRechazo.ActorInactivo, "La cuenta está desactivada.");
                }
            }
            catch (CambioConcurrenteException ex)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
            }

            return Resultado.Exito();
        }

        /// <summary>
        /// La propia persona retira su consentimiento. Se permite aunque la cuenta esté inactiva: revocar es un
        /// derecho del titular y no debe depender del estado de la cuenta. Sin consentimiento vigente, responde éxito sin guardar.
        /// </summary>
        public async Task<Resultado> RevocarConsentimientoAsync(int idActor, CancellationToken cancellationToken = default)
        {
            Usuario? actor = idActor > 0 ? await _usuarios.ObtenerPorIdAsync(idActor, cancellationToken) : null;
            if (actor is null)
            {
                return Resultado.Rechazo(MotivoRechazo.ActorNoEncontrado, "No se encontró la cuenta que intenta realizar la operación.");
            }

            if (actor.TieneConsentimiento)
            {
                try
                {
                    await _usuarios.RevocarConsentimientoAsync(actor.Id, cancellationToken);
                }
                catch (CambioConcurrenteException ex)
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
                }
            }

            return Resultado.Exito();
        }

        /// <summary>
        /// Plantilla de las operaciones de administrador sobre otra cuenta: exige el rol, carga la cuenta y aplica el
        /// cambio. Si el cambio devuelve un resultado (rechazo o éxito idempotente), se responde sin guardar; si devuelve
        /// null, <paramref name="guardar"/> escribe solo esa operación, para no pisar lo que otra cambió a la vez.
        /// </summary>
        private async Task<Resultado> ModificarAsync(
            int idActor,
            int idUsuario,
            Func<Usuario, Resultado?> cambio,
            Func<Usuario, Task> guardar,
            CancellationToken cancellationToken)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            Usuario? usuario = await _usuarios.ObtenerPorIdAsync(idUsuario, cancellationToken);
            if (usuario is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa cuenta.");
            }

            if (cambio(usuario) is { } respuesta)
            {
                return respuesta;
            }

            try
            {
                await guardar(usuario);
            }
            catch (Exception error) when (error is UltimoAdministradorException or UltimoRolException or CambioConcurrenteException)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, error.Message);
            }

            return Resultado.Exito();
        }
    }
}
