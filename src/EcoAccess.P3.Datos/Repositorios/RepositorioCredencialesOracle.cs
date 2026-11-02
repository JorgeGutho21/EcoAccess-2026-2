using System;
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
    /// Credenciales en <c>credencial_acceso</c> (migración V002). Cada método es una sola sentencia,
    /// así que no necesita transacción explícita: Oracle confirma cada una por sí misma con el autocommit de
    /// ODP.NET, y dos intentos simultáneos no se pisan porque el incremento se calcula dentro del UPDATE.
    /// </summary>
    public sealed class RepositorioCredencialesOracle : IRepositorioCredenciales
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioCredencialesOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string Columnas =
            "c.id_usuario, c.hash_contrasena, c.sal, c.iteraciones, c.fecha_cambio_utc, c.intentos_fallidos, c.bloqueada_hasta_utc";

        public Task<CredencialAcceso?> ObtenerPorCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default) =>
            ObtenerAsync(
                $"SELECT {Columnas} FROM credencial_acceso c JOIN usuario u ON u.id_usuario = c.id_usuario WHERE u.correo_electronico = :valor",
                OracleDbType.Varchar2, correoElectronico, cancellationToken);

        public Task<CredencialAcceso?> ObtenerPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            ObtenerAsync($"SELECT {Columnas} FROM credencial_acceso c WHERE c.id_usuario = :valor",
                OracleDbType.Int32, idUsuario, cancellationToken);

        public async Task AgregarAsync(CredencialAcceso credencial, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(credencial, nameof(credencial));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO credencial_acceso (id_usuario, hash_contrasena, sal, iteraciones, intentos_fallidos, bloqueada_hasta_utc, fecha_cambio_utc) " +
                "VALUES (:id, :hash, :sal, :iteraciones, :intentos, :bloqueada, :fecha)");
            AgregarSecreto(comando, credencial);
            comando.Agregar("intentos", OracleDbType.Int32, credencial.IntentosFallidos);
            comando.Agregar("bloqueada", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(credencial.BloqueadaHastaUtc));

            await TraductorErroresOracle.TraducirUnicidadAsync(
                () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
        }

        public async Task ReemplazarSecretoAsync(CredencialAcceso credencial, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(credencial, nameof(credencial));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE credencial_acceso SET hash_contrasena = :hash, sal = :sal, iteraciones = :iteraciones, " +
                "fecha_cambio_utc = :fecha, intentos_fallidos = 0, bloqueada_hasta_utc = NULL WHERE id_usuario = :id");
            AgregarSecreto(comando, credencial);
            ComandosOracle.ExigirUnaFila(await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false),
                "credencial de la cuenta", credencial.IdUsuario);
        }

        /// <summary>
        /// Un único UPDATE: en Oracle, las columnas de la derecha del SET valen lo que valían antes de la
        /// sentencia, así que los dos CASE ven el mismo contador. Si dos sesiones fallan a la vez, la segunda
        /// espera el bloqueo de fila de la primera y suma sobre su resultado.
        /// </summary>
        public async Task RegistrarFalloAsync(int idUsuario, int maximoIntentos, DateTime bloquearHastaUtc, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNegativeOrZero(maximoIntentos, nameof(maximoIntentos));
            DateTime hasta = ConversionOracle.ParaColumnaUtc(bloquearHastaUtc);
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE credencial_acceso SET " +
                "bloqueada_hasta_utc = CASE WHEN intentos_fallidos + 1 >= :maximo THEN :hasta ELSE bloqueada_hasta_utc END, " +
                "intentos_fallidos = CASE WHEN intentos_fallidos + 1 >= :maximo THEN 0 ELSE intentos_fallidos + 1 END " +
                "WHERE id_usuario = :id");
            comando.Agregar("maximo", OracleDbType.Int32, maximoIntentos);
            comando.Agregar("hasta", OracleDbType.TimeStamp, hasta);
            comando.Agregar("id", OracleDbType.Int32, idUsuario);
            await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task ReiniciarIntentosAsync(int idUsuario, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE credencial_acceso SET intentos_fallidos = 0, bloqueada_hasta_utc = NULL WHERE id_usuario = :id");
            comando.Agregar("id", OracleDbType.Int32, idUsuario);
            await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<CredencialAcceso?> ObtenerAsync(string sql, OracleDbType tipo, object valor, CancellationToken cancellationToken)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
            comando.Agregar("valor", tipo, valor);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return new CredencialAcceso(
                ConversionOracle.Entero(lector["id_usuario"]),
                (byte[])lector["hash_contrasena"],
                (byte[])lector["sal"],
                ConversionOracle.Entero(lector["iteraciones"]),
                ConversionOracle.FechaUtc(lector["fecha_cambio_utc"]),
                ConversionOracle.Entero(lector["intentos_fallidos"]),
                ConversionOracle.FechaUtcOpcional(lector["bloqueada_hasta_utc"]));
        }

        private static void AgregarSecreto(OracleCommand comando, CredencialAcceso credencial)
        {
            comando.Agregar("id", OracleDbType.Int32, credencial.IdUsuario);
            comando.Agregar("hash", OracleDbType.Raw, credencial.Hash);
            comando.Agregar("sal", OracleDbType.Raw, credencial.Sal);
            comando.Agregar("iteraciones", OracleDbType.Int32, credencial.Iteraciones);
            comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(credencial.FechaCambioUtc));
        }
    }
}
