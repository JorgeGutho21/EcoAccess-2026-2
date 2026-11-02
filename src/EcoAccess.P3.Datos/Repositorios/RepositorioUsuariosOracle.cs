using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>
    /// Cuentas en <c>usuario</c> y sus roles en <c>usuario_rol</c>. Los roles se guardan por su código
    /// (<see cref="CodigoRol"/>), que coincide con <c>rol.codigo</c>; el <c>id_rol</c> se resuelve en el SQL.
    /// </summary>
    public sealed partial class RepositorioUsuariosOracle : IRepositorioUsuarios
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioUsuariosOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string Columnas =
            "u.id_usuario, u.nombre, u.correo_electronico, u.fecha_registro_utc, u.activo, u.version_consentimiento, u.fecha_consentimiento_utc";

        private const string SinFiltro = "1 = 1";
        private const string FiltroId = "u.id_usuario = :valor";

        private const string SqlInsertarRol =
            "INSERT INTO usuario_rol (id_usuario, id_rol) SELECT :id, id_rol FROM rol WHERE codigo = :codigo";

        private const string SqlQuitarRol =
            "DELETE FROM usuario_rol WHERE id_usuario = :id AND id_rol = (SELECT id_rol FROM rol WHERE codigo = :codigo)";

        public async Task<Usuario?> ObtenerPorIdAsync(int idUsuario, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Usuario> encontrados = await ListarConFiltroAsync(FiltroId, idUsuario, cancellationToken).ConfigureAwait(false);
            return encontrados.Count == 0 ? null : encontrados[0];
        }

        public async Task<bool> ExisteCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM usuario WHERE correo_electronico = :correo");
            comando.Agregar("correo", OracleDbType.Varchar2, correoElectronico);
            return ConversionOracle.Entero((await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!) > 0;
        }

        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
            ListarConFiltroAsync(SinFiltro, null, cancellationToken);

        public async Task<int> AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(usuario, nameof(usuario));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, activo, version_consentimiento, fecha_consentimiento_utc) " +
                "VALUES (:nombre, :correo, :fecha_registro, :activo, :version, :fecha_consentimiento) RETURNING id_usuario INTO :id", transaccion);
            comando.Agregar("nombre", OracleDbType.Varchar2, usuario.Nombre);
            comando.Agregar("correo", OracleDbType.Varchar2, usuario.CorreoElectronico);
            comando.Agregar("fecha_registro", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(usuario.FechaRegistroUtc));
            comando.Agregar("activo", OracleDbType.Int32, ConversionOracle.DesdeLogico(usuario.Activo));
            comando.Agregar("version", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(usuario.VersionConsentimiento));
            comando.Agregar("fecha_consentimiento", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(usuario.FechaConsentimientoUtc));
            OracleParameter parametroId = comando.AgregarIdSalida();

            await TraductorErroresOracle.TraducirUnicidadAsync(
                () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            int id = ComandosOracle.LeerId(parametroId);

            foreach (CodigoRol rol in usuario.Roles)
            {
                await CambiarRolAsync(conexion, transaccion, SqlInsertarRol, id, rol, cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
            return id;
        }

        /// <summary>
        /// Cambia solo la columna <c>activo</c>. Para cerrar la carrera de RN-23 (dos administradores que se
        /// desactivan o se quitan el rol el uno al otro a la vez) bloquea primero la fila del rol Administrador:
        /// esas transacciones se ordenan una tras otra y la segunda ve lo que confirmó la primera.
        /// </summary>
        public async Task CambiarActivoAsync(int idUsuario, bool activo, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await BloquearRolAdministradorAsync(conexion, transaccion, cancellationToken).ConfigureAwait(false);
            bool activoAntes = await BloquearCuentaAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false);

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE usuario SET activo = :activo WHERE id_usuario = :id", transaccion))
            {
                comando.Agregar("activo", OracleDbType.Int32, ConversionOracle.DesdeLogico(activo));
                comando.Agregar("id", OracleDbType.Int32, idUsuario);
                ComandosOracle.ExigirUnaFila(await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), "cuenta", idUsuario);
            }

            if (activoAntes && !activo
                && (await LeerRolesAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false)).Contains(CodigoRol.Administrador))
            {
                await ExigirAdministradorActivoAsync(conexion, transaccion, cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Agrega un rol si la cuenta no lo tiene. Bloquea la fila de la cuenta para ordenarse con
        /// <see cref="QuitarRolAsync"/> sobre la misma cuenta; agregar nunca reduce administradores, así que no
        /// necesita el bloqueo del rol Administrador.
        /// </summary>
        public async Task AgregarRolAsync(int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await BloquearCuentaAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false);
            if (!(await LeerRolesAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false)).Contains(rol))
            {
                await CambiarRolAsync(conexion, transaccion, SqlInsertarRol, idUsuario, rol, cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Quita un rol si la cuenta lo tiene. Vuelve a comprobar RN-01 y RN-23 dentro de la transacción, después
        /// de los bloqueos (primero el rol Administrador y luego la cuenta, siempre en ese orden).
        /// </summary>
        public async Task QuitarRolAsync(int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await BloquearRolAdministradorAsync(conexion, transaccion, cancellationToken).ConfigureAwait(false);
            bool activo = await BloquearCuentaAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false);

            HashSet<CodigoRol> roles = await LeerRolesAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false);
            if (!roles.Contains(rol))
            {
                await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            if (roles.Count == 1)
            {
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new UltimoRolException();
            }

            await CambiarRolAsync(conexion, transaccion, SqlQuitarRol, idUsuario, rol, cancellationToken).ConfigureAwait(false);

            if (rol == CodigoRol.Administrador && activo)
            {
                await ExigirAdministradorActivoAsync(conexion, transaccion, cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Bloquea la cuenta con <c>WAIT 10</c> y comprueba el estado confirmado: si otra sesión la desactivó, no se
        /// guarda nada y devuelve false. Solo escribe las dos columnas del consentimiento; no toca el estado ni los roles.
        /// </summary>
        public async Task<bool> RegistrarConsentimientoAsync(int idUsuario, string version, DateTime fechaUtc,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(version, nameof(version));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            bool? activo = await BloquearCuentaSiExisteAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false);
            if (activo != true)
            {
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE usuario SET version_consentimiento = :version, fecha_consentimiento_utc = :fecha WHERE id_usuario = :id",
                transaccion))
            {
                comando.Agregar("version", OracleDbType.Varchar2, version);
                comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(fechaUtc));
                comando.Agregar("id", OracleDbType.Int32, idUsuario);
                ComandosOracle.ExigirUnaFila(await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), "cuenta", idUsuario);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>Retirar el consentimiento se permite también en cuentas inactivas; lanza si la cuenta no existe.</summary>
        public async Task RevocarConsentimientoAsync(int idUsuario, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await BloquearCuentaAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false);
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE usuario SET version_consentimiento = NULL, fecha_consentimiento_utc = NULL WHERE id_usuario = :id",
                transaccion))
            {
                comando.Agregar("id", OracleDbType.Int32, idUsuario);
                ComandosOracle.ExigirUnaFila(await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), "cuenta", idUsuario);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task BloquearRolAdministradorAsync(OracleConnection conexion, OracleTransaction transaccion,
            CancellationToken cancellationToken)
        {
            // WAIT 10: si otra transacción retiene el bloqueo más de 10 s, Oracle responde ORA-00054 (26ai) u ORA-30006 (versiones anteriores) en lugar de esperar sin límite;
            // el traductor lo convierte en CambioConcurrenteException.
            using OracleCommand bloqueo = ComandosOracle.Crear(conexion,
                "SELECT id_rol FROM rol WHERE codigo = :codigo FOR UPDATE WAIT 10", transaccion);
            bloqueo.Agregar("codigo", OracleDbType.Int32, (int)CodigoRol.Administrador);
            if (await TraductorErroresOracle.TraducirEsperaAsync(
                    () => bloqueo.ExecuteScalarAsync(cancellationToken)).ConfigureAwait(false) is null)
            {
                throw new InvalidOperationException("Falta la fila del rol Administrador en la tabla rol; sin ella no se puede proteger RN-23.");
            }
        }

        /// <summary>Bloquea la fila de la cuenta y devuelve su estado confirmado; lanza si la cuenta no existe.</summary>
        private static async Task<bool> BloquearCuentaAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idUsuario, CancellationToken cancellationToken) =>
            await BloquearCuentaSiExisteAsync(conexion, transaccion, idUsuario, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No existe cuenta con id {idUsuario}.");

        /// <summary>Bloquea la fila de la cuenta y devuelve su estado confirmado, o null si la cuenta no existe.</summary>
        private static async Task<bool?> BloquearCuentaSiExisteAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idUsuario, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT activo FROM usuario WHERE id_usuario = :id FOR UPDATE WAIT 10", transaccion);
            comando.Agregar("id", OracleDbType.Int32, idUsuario);
            object? valor = await TraductorErroresOracle.TraducirEsperaAsync(
                () => comando.ExecuteScalarAsync(cancellationToken)).ConfigureAwait(false);
            return valor is null ? null : ConversionOracle.Logico(valor);
        }

        /// <summary>Si ya no queda ningún administrador activo, deshace la transacción y lanza RN-23.</summary>
        private static async Task ExigirAdministradorActivoAsync(OracleConnection conexion, OracleTransaction transaccion,
            CancellationToken cancellationToken)
        {
            if (await ContarAdministradoresActivosAsync(conexion, transaccion, cancellationToken).ConfigureAwait(false) == 0)
            {
                // Sin token: el rollback debe ocurrir aunque la operación se haya cancelado.
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new UltimoAdministradorException();
            }
        }

        private static async Task CambiarRolAsync(OracleConnection conexion, OracleTransaction transaccion, string sql,
            int idUsuario, CodigoRol rol, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql, transaccion);
            comando.Agregar("id", OracleDbType.Int32, idUsuario);
            comando.Agregar("codigo", OracleDbType.Int32, (int)rol);
            int filas = await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (filas != 1)
            {
                // Solo pasa si falta el rol en el catálogo ROL: es un fallo de instalación, no de la persona usuaria.
                throw new InvalidOperationException($"No se pudo guardar el rol {rol} de la cuenta {idUsuario}; revisa el catálogo ROL.");
            }
        }

        private static async Task<HashSet<CodigoRol>> LeerRolesAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idUsuario, CancellationToken cancellationToken)
        {
            var roles = new HashSet<CodigoRol>();
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT r.codigo FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE ur.id_usuario = :id", transaccion);
            comando.Agregar("id", OracleDbType.Int32, idUsuario);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                roles.Add((CodigoRol)ConversionOracle.Entero(lector.GetValue(0)));
            }

            return roles;
        }

        private static async Task<int> ContarAdministradoresActivosAsync(OracleConnection conexion, OracleTransaction transaccion,
            CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM usuario u WHERE u.activo = 1 AND EXISTS (" +
                "SELECT 1 FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE ur.id_usuario = u.id_usuario AND r.codigo = :codigo)",
                transaccion);
            comando.Agregar("codigo", OracleDbType.Int32, (int)CodigoRol.Administrador);
            return ConversionOracle.Entero((await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
        }

        private async Task<IReadOnlyList<Usuario>> ListarConFiltroAsync(string filtro, int? valor, CancellationToken cancellationToken)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);

            var roles = new Dictionary<int, List<CodigoRol>>();
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT ur.id_usuario, r.codigo FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol " +
                $"JOIN usuario u ON u.id_usuario = ur.id_usuario WHERE {filtro}"))
            {
                AgregarValor(comando, valor);
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    int idUsuario = ConversionOracle.Entero(lector.GetValue(0));
                    if (!roles.TryGetValue(idUsuario, out List<CodigoRol>? lista))
                    {
                        roles[idUsuario] = lista = new List<CodigoRol>();
                    }

                    lista.Add((CodigoRol)ConversionOracle.Entero(lector.GetValue(1)));
                }
            }

            var cuentas = new List<Usuario>();
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM usuario u WHERE {filtro} ORDER BY NLSSORT(u.nombre, 'NLS_SORT=BINARY'), u.id_usuario"))
            {
                AgregarValor(comando, valor);
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    int id = ConversionOracle.Entero(lector.GetValue(0));
                    cuentas.Add(new Usuario(
                        id,
                        lector.GetString(1),
                        lector.GetString(2),
                        ConversionOracle.FechaUtc(lector.GetValue(3)),
                        ConversionOracle.Logico(lector.GetValue(4)),
                        roles.TryGetValue(id, out List<CodigoRol>? suyos) ? suyos : new List<CodigoRol>(),
                        ConversionOracle.TextoOpcional(lector.GetValue(5)),
                        ConversionOracle.FechaUtcOpcional(lector.GetValue(6))));
                }
            }

            return cuentas;
        }

        private static void AgregarValor(OracleCommand comando, int? valor)
        {
            if (valor is not null)
            {
                comando.Agregar("valor", OracleDbType.Int32, valor.Value);
            }
        }
    }
}
