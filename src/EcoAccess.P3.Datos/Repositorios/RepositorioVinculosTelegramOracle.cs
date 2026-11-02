using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using System.Globalization;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>
    /// Vínculos con Telegram (<c>vinculo_telegram</c>) y tokens del enlace de registro (<c>token_vinculacion</c>),
    /// Los índices únicos funcionales de V004 garantizan un solo vínculo activo por cuenta de
    /// Telegram y por usuario; el registro bloquea la fila del token para que dos envíos del mismo formulario no
    /// creen dos cuentas.
    /// </summary>
    public sealed class RepositorioVinculosTelegramOracle : IRepositorioVinculosTelegram
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioVinculosTelegramOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string ColumnasVinculo = "id_vinculo, id_usuario, telegram_user_id, fecha_vinculo_utc, fecha_baja_utc";

        private const string ColumnasToken =
            "hash_token, telegram_user_id, chat_id, fecha_creacion_utc, fecha_vencimiento_utc, fecha_uso_utc";

        private const string SqlInsertarRol =
            "INSERT INTO usuario_rol (id_usuario, id_rol) SELECT :id, id_rol FROM rol WHERE codigo = :codigo";

        public Task<VinculoTelegram?> ObtenerVinculoActivoPorTelegramAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            LeerVinculoActivoAsync("telegram_user_id = :valor", OracleDbType.Int64, telegramUserId, cancellationToken);

        public Task<VinculoTelegram?> ObtenerVinculoActivoPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            LeerVinculoActivoAsync("id_usuario = :valor", OracleDbType.Int32, idUsuario, cancellationToken);

        public async Task AgregarTokenAsync(TokenVinculacion token, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(token, nameof(token));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO token_vinculacion (hash_token, telegram_user_id, chat_id, fecha_creacion_utc, fecha_vencimiento_utc) " +
                "VALUES (:hash, :telegram, :chat, :creacion, :vence)");
            comando.Agregar("hash", OracleDbType.Varchar2, token.HashToken);
            comando.Agregar("telegram", OracleDbType.Int64, token.TelegramUserId);
            comando.Agregar("chat", OracleDbType.Int64, token.ChatId);
            comando.Agregar("creacion", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(token.CreacionUtc));
            comando.Agregar("vence", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(token.VenceUtc));
            await TraductorErroresOracle.TraducirUnicidadAsync(
                () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
        }

        public async Task<TokenVinculacion?> ObtenerTokenAsync(string hashToken, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(hashToken, nameof(hashToken));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {ColumnasToken} FROM token_vinculacion WHERE hash_token = :hash");
            comando.Agregar("hash", OracleDbType.Varchar2, hashToken);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? LeerToken(lector) : null;
        }

        /// <summary>
        /// En una sola transacción: bloquea el token, comprueba que siga vigente y sea de esa cuenta de Telegram, crea
        /// la cuenta de ciudadano con sus roles, la vincula y marca el token como usado. Un segundo envío espera al
        /// primero y, al ver el token usado, falla con <see cref="CambioConcurrenteException"/>.
        /// </summary>
        public async Task<int> RegistrarCiudadanoYVincularAsync(
            Usuario usuario,
            string hashToken,
            long telegramUserId,
            DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(usuario, nameof(usuario));
            Compat.ThrowIfNull(hashToken, nameof(hashToken));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            TokenVinculacion? token = await BloquearTokenAsync(conexion, transaccion, hashToken, cancellationToken).ConfigureAwait(false);
            // Un reloj atrasado respecto de la emisión rompería ck_token_uso; se trata como token no vigente.
            if (token is null || !token.EstaVigente(ahoraUtc) || ahoraUtc < token.CreacionUtc || token.TelegramUserId != telegramUserId)
            {
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new CambioConcurrenteException("El token ya no está vigente.");
            }

            int idUsuario = await InsertarUsuarioAsync(conexion, transaccion, usuario, cancellationToken).ConfigureAwait(false);

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO vinculo_telegram (id_usuario, telegram_user_id, fecha_vinculo_utc) VALUES (:usuario, :telegram, :fecha)",
                transaccion))
            {
                comando.Agregar("usuario", OracleDbType.Int32, idUsuario);
                comando.Agregar("telegram", OracleDbType.Int64, telegramUserId);
                comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahoraUtc));
                await TraductorErroresOracle.TraducirUnicidadAsync(
                    () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE token_vinculacion SET fecha_uso_utc = :uso, id_usuario_registrado = :usuario " +
                "WHERE hash_token = :hash AND fecha_uso_utc IS NULL", transaccion))
            {
                comando.Agregar("uso", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahoraUtc));
                comando.Agregar("usuario", OracleDbType.Int32, idUsuario);
                comando.Agregar("hash", OracleDbType.Varchar2, hashToken);
                if (await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                {
                    // No debería ocurrir con la fila bloqueada; se deshace todo antes que dejar un token reutilizable.
                    await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    throw new CambioConcurrenteException("El token ya no está vigente.");
                }
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
            return idUsuario;
        }

        public async Task DarDeBajaAsync(int idUsuario, DateTime fechaUtc, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE vinculo_telegram SET fecha_baja_utc = :fecha WHERE id_usuario = :id AND fecha_baja_utc IS NULL");
            comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(fechaUtc));
            comando.Agregar("id", OracleDbType.Int32, idUsuario);
            await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private const string ColumnasPendiente =
            "p.hash_token, p.telegram_user_id, p.id_usuario, p.hash_codigo, p.fecha_creacion_utc, p.fecha_vencimiento_utc, p.intentos";

        /// <summary>Una fila por token (MERGE): preparar de nuevo reemplaza código, cuenta y vencimiento y reinicia intentos.</summary>
        public async Task GuardarVinculacionPendienteAsync(VinculacionPendiente pendiente, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(pendiente, nameof(pendiente));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "MERGE INTO vinculacion_pendiente p USING (SELECT :hash AS hash_token FROM dual) d ON (p.hash_token = d.hash_token) " +
                "WHEN MATCHED THEN UPDATE SET p.telegram_user_id = :telegram, p.id_usuario = :usuario, p.hash_codigo = :codigo, " +
                "p.fecha_creacion_utc = :creacion, p.fecha_vencimiento_utc = :vence, p.intentos = 0 " +
                "WHEN NOT MATCHED THEN INSERT (hash_token, telegram_user_id, id_usuario, hash_codigo, fecha_creacion_utc, fecha_vencimiento_utc, intentos) " +
                "VALUES (d.hash_token, :telegram, :usuario, :codigo, :creacion, :vence, 0)");
            comando.Agregar("hash", OracleDbType.Varchar2, pendiente.HashToken);
            comando.Agregar("telegram", OracleDbType.Int64, pendiente.TelegramUserId);
            comando.Agregar("usuario", OracleDbType.Int32, pendiente.IdUsuario);
            comando.Agregar("codigo", OracleDbType.Varchar2, pendiente.HashCodigo);
            comando.Agregar("creacion", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(pendiente.CreacionUtc));
            comando.Agregar("vence", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(pendiente.VenceUtc));
            await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<VinculacionPendiente?> ObtenerVinculacionPendienteAsync(long telegramUserId, DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {ColumnasPendiente} FROM vinculacion_pendiente p JOIN token_vinculacion t ON t.hash_token = p.hash_token " +
                "WHERE p.telegram_user_id = :telegram AND p.fecha_vencimiento_utc > :ahora AND t.fecha_uso_utc IS NULL " +
                "ORDER BY p.fecha_creacion_utc DESC FETCH FIRST 1 ROW ONLY");
            comando.Agregar("telegram", OracleDbType.Int64, telegramUserId);
            comando.Agregar("ahora", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahoraUtc));
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? LeerPendiente(lector) : null;
        }

        /// <summary>Incremento atómico en el propio UPDATE; el CHECK de V007 limita el contador a 5.</summary>
        public async Task<int> RegistrarIntentoFallidoAsync(string hashToken, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(hashToken, nameof(hashToken));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE vinculacion_pendiente SET intentos = LEAST(intentos + 1, 5) WHERE hash_token = :hash RETURNING intentos INTO :id");
            comando.Agregar("hash", OracleDbType.Varchar2, hashToken);
            OracleParameter intentos = comando.AgregarIdSalida();
            return await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1
                ? ComandosOracle.LeerId(intentos)
                : int.MaxValue;
        }

        /// <summary>
        /// Una transacción: bloquea token, pendiente y cuenta; revalida vigencia, intentos, Telegram del token, cuenta
        /// activa con rol Ciudadano y consentimiento; crea el vínculo (los índices únicos de V004 impiden un segundo
        /// vínculo activo), marca el token usado y borra la pendiente. No toca USUARIO_ROL.
        /// </summary>
        public async Task<int> ConfirmarVinculacionPendienteAsync(VinculacionPendiente verificada, DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(verificada, nameof(verificada));
            string hashToken = verificada.HashToken;
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            TokenVinculacion? token = await BloquearTokenAsync(conexion, transaccion, hashToken, cancellationToken).ConfigureAwait(false);
            VinculacionPendiente? pendiente;
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {ColumnasPendiente} FROM vinculacion_pendiente p WHERE p.hash_token = :hash " +
                "AND p.hash_codigo = :codigo AND p.id_usuario = :usuario FOR UPDATE WAIT 10", transaccion))
            {
                comando.Agregar("hash", OracleDbType.Varchar2, hashToken);
                comando.Agregar("codigo", OracleDbType.Varchar2, verificada.HashCodigo);
                comando.Agregar("usuario", OracleDbType.Int32, verificada.IdUsuario);
                pendiente = await TraductorErroresOracle.TraducirEsperaAsync(async () =>
                {
                    using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? LeerPendiente(lector) : null;
                }).ConfigureAwait(false);
            }

            bool valida = token is not null && pendiente is not null && token.EstaVigente(ahoraUtc) && ahoraUtc >= token.CreacionUtc
                && token.TelegramUserId == pendiente.TelegramUserId && pendiente.VenceUtc > ahoraUtc && pendiente.Intentos < 5
                && await CuentaVinculableAsync(conexion, transaccion, pendiente.IdUsuario, cancellationToken).ConfigureAwait(false);
            if (!valida)
            {
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new CambioConcurrenteException("La vinculación pendiente ya no es válida.");
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO vinculo_telegram (id_usuario, telegram_user_id, fecha_vinculo_utc) VALUES (:usuario, :telegram, :fecha)",
                transaccion))
            {
                comando.Agregar("usuario", OracleDbType.Int32, pendiente!.IdUsuario);
                comando.Agregar("telegram", OracleDbType.Int64, pendiente.TelegramUserId);
                comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahoraUtc));
                await TraductorErroresOracle.TraducirUnicidadAsync(
                    () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE token_vinculacion SET fecha_uso_utc = :uso, id_usuario_registrado = :usuario " +
                "WHERE hash_token = :hash AND fecha_uso_utc IS NULL", transaccion))
            {
                comando.Agregar("uso", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahoraUtc));
                comando.Agregar("usuario", OracleDbType.Int32, pendiente.IdUsuario);
                comando.Agregar("hash", OracleDbType.Varchar2, hashToken);
                if (await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                {
                    await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    throw new CambioConcurrenteException("La vinculación pendiente ya no es válida.");
                }
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "DELETE FROM vinculacion_pendiente WHERE hash_token = :hash", transaccion))
            {
                comando.Agregar("hash", OracleDbType.Varchar2, hashToken);
                await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
            return pendiente.IdUsuario;
        }

        private static async Task<bool> CuentaVinculableAsync(OracleConnection conexion, OracleTransaction transaccion, int idUsuario,
            CancellationToken cancellationToken)
        {
            // Se bloquea la cuenta para que una desactivación o revocación simultánea espere a esta transacción.
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT u.activo, u.version_consentimiento, " +
                "(SELECT COUNT(*) FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE ur.id_usuario = u.id_usuario AND r.codigo = :ciudadano) " +
                "FROM usuario u WHERE u.id_usuario = :id FOR UPDATE OF u.activo WAIT 10", transaccion);
            comando.Agregar("ciudadano", OracleDbType.Int32, (int)CodigoRol.Ciudadano);
            comando.Agregar("id", OracleDbType.Int32, idUsuario);
            return await TraductorErroresOracle.TraducirEsperaAsync(async () =>
            {
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                return await lector.ReadAsync(cancellationToken).ConfigureAwait(false)
                    && ConversionOracle.Entero(lector.GetValue(0)) == 1
                    && !lector.IsDBNull(1)
                    && ConversionOracle.Entero(lector.GetValue(2)) > 0;
            }).ConfigureAwait(false);
        }

        private static VinculacionPendiente LeerPendiente(DbDataReader lector) => new(
            lector.GetString(0),
            Largo(lector.GetValue(1)),
            ConversionOracle.Entero(lector.GetValue(2)),
            lector.GetString(3),
            ConversionOracle.FechaUtc(lector.GetValue(4)),
            ConversionOracle.FechaUtc(lector.GetValue(5)),
            ConversionOracle.Entero(lector.GetValue(6)));

        private async Task<VinculoTelegram?> LeerVinculoActivoAsync(string filtro, OracleDbType tipo, object valor,
            CancellationToken cancellationToken)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {ColumnasVinculo} FROM vinculo_telegram WHERE fecha_baja_utc IS NULL AND {filtro}");
            comando.Agregar("valor", tipo, valor);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return new VinculoTelegram(
                ConversionOracle.Entero(lector.GetValue(0)),
                ConversionOracle.Entero(lector.GetValue(1)),
                Largo(lector.GetValue(2)),
                ConversionOracle.FechaUtc(lector.GetValue(3)),
                ConversionOracle.FechaUtcOpcional(lector.GetValue(4)));
        }

        private static async Task<TokenVinculacion?> BloquearTokenAsync(OracleConnection conexion, OracleTransaction transaccion,
            string hashToken, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {ColumnasToken} FROM token_vinculacion WHERE hash_token = :hash FOR UPDATE WAIT 10", transaccion);
            comando.Agregar("hash", OracleDbType.Varchar2, hashToken);
            return await TraductorErroresOracle.TraducirEsperaAsync(async () =>
            {
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? LeerToken(lector) : null;
            }).ConfigureAwait(false);
        }

        private static async Task<int> InsertarUsuarioAsync(OracleConnection conexion, OracleTransaction transaccion,
            Usuario usuario, CancellationToken cancellationToken)
        {
            int id;
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, activo, version_consentimiento, fecha_consentimiento_utc) " +
                "VALUES (:nombre, :correo, :fecha_registro, :activo, :version, :fecha_consentimiento) RETURNING id_usuario INTO :id", transaccion))
            {
                comando.Agregar("nombre", OracleDbType.Varchar2, usuario.Nombre);
                comando.Agregar("correo", OracleDbType.Varchar2, usuario.CorreoElectronico);
                comando.Agregar("fecha_registro", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(usuario.FechaRegistroUtc));
                comando.Agregar("activo", OracleDbType.Int32, ConversionOracle.DesdeLogico(usuario.Activo));
                comando.Agregar("version", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(usuario.VersionConsentimiento));
                comando.Agregar("fecha_consentimiento", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(usuario.FechaConsentimientoUtc));
                OracleParameter parametroId = comando.AgregarIdSalida();
                await TraductorErroresOracle.TraducirUnicidadAsync(
                    () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
                id = ComandosOracle.LeerId(parametroId);
            }

            foreach (CodigoRol rol in usuario.Roles)
            {
                using OracleCommand comando = ComandosOracle.Crear(conexion, SqlInsertarRol, transaccion);
                comando.Agregar("id", OracleDbType.Int32, id);
                comando.Agregar("codigo", OracleDbType.Int32, (int)rol);
                if (await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                {
                    throw new InvalidOperationException($"No se pudo guardar el rol {rol} de la cuenta {id}; revisa el catálogo ROL.");
                }
            }

            return id;
        }

        private static TokenVinculacion LeerToken(DbDataReader lector) => new(
            lector.GetString(0),
            Largo(lector.GetValue(1)),
            Largo(lector.GetValue(2)),
            ConversionOracle.FechaUtc(lector.GetValue(3)),
            ConversionOracle.FechaUtc(lector.GetValue(4)),
            ConversionOracle.FechaUtcOpcional(lector.GetValue(5)));

        internal static long Largo(object valor) => Convert.ToInt64(valor, CultureInfo.InvariantCulture);
    }
}
