using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Inicio de sesión del personal y de la bienvenida ciudadana, cambio de contraseña y primera contraseña
    ///. 
    /// </summary>
    /// <remarks>
    /// Cualquier fallo del inicio de sesión responde lo mismo, <see cref="MensajeFallo"/> con
    /// <see cref="MotivoRechazo.CredencialesInvalidas"/>: correo inexistente, contraseña errada, cuenta inactiva,
    /// cuenta sin un rol permitido por la entrada elegida y credencial bloqueada. Si no hay credencial que comprobar, se
    /// calcula igual un hash de relleno para que el tiempo de respuesta no delate si el correo existe.
    /// La sesión que se devuelve no autoriza nada: cada servicio sigue revisando la cuenta en cada operación.
    /// </remarks>
    public sealed class ServicioAutenticacion
    {
        public const string MensajeFallo = "Correo o contraseña incorrectos.";

        /// <summary>Fallos seguidos que bloquean la credencial (valor provisional hasta calibrar P-10).</summary>
        public const int MaximoIntentos = 5;

        /// <summary>Duración del bloqueo temporal (valor provisional hasta calibrar P-10).</summary>
        public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

        private static readonly CodigoRol[] RolesConAccesoGui = new CodigoRol[] { CodigoRol.Operario, CodigoRol.Administrador };

        private readonly IRepositorioCredenciales _credenciales;
        private readonly IRepositorioUsuarios _usuarios;
        private readonly IHasherContrasenas _hasher;
        private readonly TimeProvider _reloj;
        private readonly ControlAcceso _acceso;

        // Se calcula la primera vez que hace falta, para no pagar las iteraciones al arrancar la aplicación.
        private readonly Lazy<HashContrasena> _relleno;

        public ServicioAutenticacion(
            IRepositorioCredenciales credenciales,
            IRepositorioUsuarios usuarios,
            IHasherContrasenas hasher,
            TimeProvider reloj)
        {
            _credenciales = credenciales ?? throw new ArgumentNullException(nameof(credenciales));
            _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _acceso = new ControlAcceso(usuarios);
            _relleno = new Lazy<HashContrasena>(() => _hasher.Calcular("relleno-sin-cuenta-asociada"));
        }

        /// <summary>Comprueba correo y contraseña y, si son correctos, devuelve la sesión de la cuenta.</summary>
        public Task<Resultado<SesionActiva>> IniciarSesionAsync(
            string? correoElectronico,
            string? contrasena,
            CancellationToken cancellationToken = default) =>
            IniciarSesionConRolesAsync(correoElectronico, contrasena, RolesConAccesoGui, cancellationToken);

        /// <summary>Entrada de bienvenida: el ciudadano recibe sesión, pero sus permisos no cambian.</summary>
        public Task<Resultado<SesionActiva>> IniciarSesionPublicaAsync(string? correoElectronico, string? contrasena,
            CancellationToken cancellationToken = default) =>
            IniciarSesionConRolesAsync(correoElectronico, contrasena,
                new CodigoRol[] { CodigoRol.Ciudadano, CodigoRol.Operario, CodigoRol.Administrador }, cancellationToken);

        private async Task<Resultado<SesionActiva>> IniciarSesionConRolesAsync(string? correoElectronico, string? contrasena,
            CodigoRol[] rolesPermitidos, CancellationToken cancellationToken)
        {
            if (Compat.IsNullOrWhiteSpace(correoElectronico) || Compat.IsNullOrEmpty(contrasena))
            {
                return Resultado<SesionActiva>.Rechazo(MotivoRechazo.DatosInvalidos, "Escriba el correo y la contraseña.");
            }

            if (contrasena.Length > CredencialAcceso.LongitudMaximaContrasena)
            {
                return Fallo<SesionActiva>();
            }

            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            string correo = correoElectronico.Trim().ToLowerInvariant();
            CredencialAcceso? credencial = await _credenciales.ObtenerPorCorreoAsync(correo, cancellationToken);

            if (credencial is null || credencial.EstaBloqueada(ahora))
            {
                VerificarRelleno(contrasena);
                return Fallo<SesionActiva>();
            }

            if (!Verificar(contrasena, credencial))
            {
                await _credenciales.RegistrarFalloAsync(credencial.IdUsuario, MaximoIntentos, ahora + DuracionBloqueo, cancellationToken);
                return Fallo<SesionActiva>();
            }

            Usuario? usuario = await _usuarios.ObtenerPorIdAsync(credencial.IdUsuario, cancellationToken);
            if (usuario is null || !usuario.Activo || !rolesPermitidos.Any(usuario.TieneRol))
            {
                return Fallo<SesionActiva>();
            }

            if (credencial.IntentosFallidos > 0 || credencial.BloqueadaHastaUtc is not null)
            {
                await _credenciales.ReiniciarIntentosAsync(credencial.IdUsuario, cancellationToken);
            }

            return Resultado<SesionActiva>.Exito(new SesionActiva(usuario.Id, usuario.Nombre, usuario.Roles, ahora));
        }

        /// <summary>
        /// La propia persona cambia su contraseña dando la actual. Un error en la actual cuenta como intento
        /// fallido, igual que en el inicio de sesión, para que esta operación no sirva para adivinarla.
        /// </summary>
        public async Task<Resultado> CambiarContrasenaAsync(
            int idUsuario,
            string? contrasenaActual,
            string? contrasenaNueva,
            CancellationToken cancellationToken = default)
        {
            if (await ExigirAccesoGuiAsync(idUsuario, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            if (ValidarNueva(contrasenaNueva) is { } invalida)
            {
                return invalida;
            }

            if (Compat.IsNullOrEmpty(contrasenaActual) || contrasenaActual.Length > CredencialAcceso.LongitudMaximaContrasena)
            {
                return Resultado.Rechazo(MotivoRechazo.CredencialesInvalidas, "La contraseña actual no es correcta.");
            }

            if (contrasenaNueva == contrasenaActual)
            {
                return Resultado.Rechazo(MotivoRechazo.DatosInvalidos, "La contraseña nueva debe ser distinta de la actual.");
            }

            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            CredencialAcceso? credencial = await _credenciales.ObtenerPorUsuarioAsync(idUsuario, cancellationToken);
            if (credencial is null)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "La cuenta todavía no tiene contraseña; la establece un administrador.");
            }

            if (credencial.EstaBloqueada(ahora))
            {
                return Resultado.Rechazo(MotivoRechazo.CredencialesInvalidas, "Demasiados intentos fallidos; intente más tarde.");
            }

            if (!Verificar(contrasenaActual, credencial))
            {
                await _credenciales.RegistrarFalloAsync(idUsuario, MaximoIntentos, ahora + DuracionBloqueo, cancellationToken);
                return Resultado.Rechazo(MotivoRechazo.CredencialesInvalidas, "La contraseña actual no es correcta.");
            }

            await _credenciales.ReemplazarSecretoAsync(Crear(idUsuario, contrasenaNueva!, ahora), cancellationToken);
            return Resultado.Exito();
        }

        /// <summary>
        /// Un administrador da la primera contraseña a una cuenta de operario o administrador que aún no tiene.
        /// No sirve para cambiar una existente: eso lo hace su titular con <see cref="CambiarContrasenaAsync"/>.
        /// </summary>
        public async Task<Resultado> EstablecerContrasenaInicialAsync(
            int idActor,
            int idUsuario,
            string? contrasena,
            CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            if (ValidarNueva(contrasena) is { } invalida)
            {
                return invalida;
            }

            Usuario? usuario = idUsuario > 0 ? await _usuarios.ObtenerPorIdAsync(idUsuario, cancellationToken) : null;
            if (usuario is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe la cuenta indicada.");
            }

            if (!RolesConAccesoGui.Any(usuario.TieneRol))
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "Solo los operarios y administradores inician sesión en la aplicación.");
            }

            if (await _credenciales.ObtenerPorUsuarioAsync(idUsuario, cancellationToken) is not null)
            {
                return Resultado.Rechazo(MotivoRechazo.Duplicado, "La cuenta ya tiene contraseña; solo su titular puede cambiarla.");
            }

            try
            {
                await _credenciales.AgregarAsync(Crear(idUsuario, contrasena!, _reloj.GetUtcNow().UtcDateTime), cancellationToken);
            }
            catch (ConflictoUnicidadException)
            {
                // Otro administrador la estableció entre la consulta y la inserción.
                return Resultado.Rechazo(MotivoRechazo.Duplicado, "La cuenta ya tiene contraseña; solo su titular puede cambiarla.");
            }

            return Resultado.Exito();
        }

        /// <summary>
        /// Indica si falta la configuración inicial: ninguna cuenta de administrador, activa o no, tiene contraseña
        /// (ver <see cref="ConfigurarAdministradorInicialAsync"/>). Contar también las inactivas limita el atajo al
        /// primer arranque: desactivar al único administrador con contraseña no vuelve a abrirlo.
        /// </summary>
        public async Task<bool> RequiereConfiguracionInicialAsync(CancellationToken cancellationToken = default)
        {
            foreach (Usuario usuario in await _usuarios.ListarAsync(cancellationToken))
            {
                if (usuario.TieneRol(CodigoRol.Administrador)
                    && await _credenciales.ObtenerPorUsuarioAsync(usuario.Id, cancellationToken) is not null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Da la primera contraseña a un administrador activo mientras ningún administrador tiene contraseña,
        /// para que no tenga que escribirse en un script SQL. Después se rechaza y las demás cuentas pasan
        /// por <see cref="EstablecerContrasenaInicialAsync"/>. La comprobación y la inserción no son atómicas: dos
        /// configuraciones simultáneas de administradores distintos pueden pasar ambas (riesgo aceptado).
        /// </summary>
        public async Task<Resultado> ConfigurarAdministradorInicialAsync(
            string? correoElectronico,
            string? contrasena,
            CancellationToken cancellationToken = default)
        {
            if (!await RequiereConfiguracionInicialAsync(cancellationToken))
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "La configuración inicial ya se hizo; inicie sesión como administrador.");
            }

            if (ValidarNueva(contrasena) is { } invalida)
            {
                return invalida;
            }

            string correo = (correoElectronico ?? string.Empty).Trim().ToLowerInvariant();
            IReadOnlyList<Usuario> usuarios = await _usuarios.ListarAsync(cancellationToken);
            Usuario? administrador = usuarios.FirstOrDefault(u => u.CorreoElectronico == correo);
            if (administrador is null || !administrador.Activo || !administrador.TieneRol(CodigoRol.Administrador))
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No hay una cuenta de administrador activa con ese correo.");
            }

            try
            {
                await _credenciales.AgregarAsync(Crear(administrador.Id, contrasena!, _reloj.GetUtcNow().UtcDateTime), cancellationToken);
            }
            catch (ConflictoUnicidadException)
            {
                return Resultado.Rechazo(MotivoRechazo.Duplicado, "La cuenta ya tiene contraseña; inicie sesión con ella.");
            }

            return Resultado.Exito();
        }

        /// <summary>
        /// Indica si no existe ninguna cuenta con el rol Administrador, activa o no. Solo en ese caso se ofrece
        /// <see cref="CrearAdministradorInicialAsync"/>.
        /// </summary>
        public async Task<bool> FaltaCuentaAdministradorAsync(CancellationToken cancellationToken = default) =>
            !(await _usuarios.ListarAsync(cancellationToken)).Any(u => u.TieneRol(CodigoRol.Administrador));

        /// <summary>
        /// Crea el primer administrador (nombre, correo y contraseña) cuando la base no tiene ninguna cuenta con ese rol,
        /// por ejemplo en una base nueva en la nube. Es de un solo uso: en cuanto existe un administrador, activo o no,
        /// se rechaza y las demás cuentas las crea ese administrador desde Usuarios. No promueve cuentas existentes: si el
        /// correo ya está registrado se rechaza, para que nadie se eleve a administrador reutilizando su cuenta.
        /// </summary>
        /// <remarks>
        /// La traza queda en Oracle: USUARIO.FECHA_REGISTRO_UTC, USUARIO_ROL.FECHA_ASIGNACION_UTC y
        /// CREDENCIAL_ACCESO.FECHA_CAMBIO_UTC. La contraseña solo se guarda como hash PBKDF2. Si la credencial no llegara a
        /// guardarse, la cuenta queda como administrador sin contraseña y <see cref="ConfigurarAdministradorInicialAsync"/>
        /// completa el proceso.
        /// </remarks>
        public async Task<Resultado> CrearAdministradorInicialAsync(
            string? nombre,
            string? correoElectronico,
            string? contrasena,
            CancellationToken cancellationToken = default)
        {
            if (!await FaltaCuentaAdministradorAsync(cancellationToken))
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido,
                    "Ya existe una cuenta de administrador; las demás cuentas se crean desde Usuarios.");
            }

            if (ValidarNueva(contrasena) is { } invalida)
            {
                return invalida;
            }

            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            Usuario usuario;
            try
            {
                usuario = new Usuario(nombre!, correoElectronico!, CodigoRol.Administrador, ahora);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo(error);
            }

            if (await _usuarios.ExisteCorreoAsync(usuario.CorreoElectronico, cancellationToken))
            {
                return Resultado.Rechazo(MotivoRechazo.Duplicado,
                    "Ese correo ya pertenece a otra cuenta; use un correo distinto para el administrador.");
            }

            int idUsuario;
            try
            {
                idUsuario = await _usuarios.AgregarAsync(usuario, cancellationToken);
            }
            catch (ConflictoUnicidadException)
            {
                return Resultado.Rechazo(MotivoRechazo.Duplicado,
                    "Ese correo ya pertenece a otra cuenta; use un correo distinto para el administrador.");
            }

            await _credenciales.AgregarAsync(Crear(idUsuario, contrasena!, ahora), cancellationToken);
            return Resultado.Exito();
        }

        private async Task<Resultado?> ExigirAccesoGuiAsync(int idUsuario, CancellationToken cancellationToken)
        {
            Usuario? usuario = idUsuario > 0 ? await _usuarios.ObtenerPorIdAsync(idUsuario, cancellationToken) : null;
            if (usuario is null)
            {
                return Resultado.Rechazo(MotivoRechazo.ActorNoEncontrado, "No se encontró la cuenta que intenta realizar la operación.");
            }

            if (!usuario.Activo)
            {
                return Resultado.Rechazo(MotivoRechazo.ActorInactivo, "La cuenta está desactivada.");
            }

            return RolesConAccesoGui.Any(usuario.TieneRol)
                ? null
                : Resultado.Rechazo(MotivoRechazo.SinPermiso, "La operación requiere el rol Operario o Administrador.");
        }

        private static Resultado? ValidarNueva(string? contrasena)
        {
            if (contrasena is null
                || contrasena.Length < CredencialAcceso.LongitudMinimaContrasena
                || contrasena.Length > CredencialAcceso.LongitudMaximaContrasena)
            {
                return Resultado.Rechazo(MotivoRechazo.DatosInvalidos,
                    $"La contraseña debe tener entre {CredencialAcceso.LongitudMinimaContrasena} y {CredencialAcceso.LongitudMaximaContrasena} caracteres.");
            }

            return Compat.IsNullOrWhiteSpace(contrasena)
                ? Resultado.Rechazo(MotivoRechazo.DatosInvalidos, "La contraseña no puede estar formada solo por espacios.")
                : null;
        }

        private CredencialAcceso Crear(int idUsuario, string contrasena, DateTime ahora)
        {
            HashContrasena calculado = _hasher.Calcular(contrasena);
            return new CredencialAcceso(idUsuario, calculado.Hash, calculado.Sal, calculado.Iteraciones, ahora);
        }

        private bool Verificar(string contrasena, CredencialAcceso credencial) =>
            _hasher.Verificar(contrasena, credencial.Sal, credencial.Iteraciones, credencial.Hash);

        private void VerificarRelleno(string contrasena)
        {
            HashContrasena relleno = _relleno.Value;
            _ = _hasher.Verificar(contrasena, relleno.Sal, relleno.Iteraciones, relleno.Hash);
        }

        private static Resultado<T> Fallo<T>() => Resultado<T>.Rechazo(MotivoRechazo.CredencialesInvalidas, MensajeFallo);
    }
}
