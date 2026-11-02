using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>Alta atómica en las tablas existentes; el SQL asigna exclusivamente codigo=1 (Ciudadano).</summary>
    public sealed class RepositorioRegistroCiudadanosOracle : IRepositorioRegistroCiudadanos
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioRegistroCiudadanosOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<int> RegistrarAsync(AltaCiudadano alta, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(alta, nameof(alta));
            var ciudadano = new Usuario(alta.Nombre, alta.CorreoElectronico, CodigoRol.Ciudadano, alta.FechaUtc);
            ciudadano.RegistrarConsentimiento(alta.VersionConsentimiento, alta.FechaUtc);
            byte[] hash = (byte[])alta.Credencial.Hash.Clone();
            byte[] sal = (byte[])alta.Credencial.Sal.Clone();
            int iteraciones = alta.Credencial.Iteraciones;
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                int id;
                using (OracleCommand comando = Crear(conexion, transaccion, @"INSERT INTO usuario(nombre,correo_electronico,fecha_registro_utc,activo,version_consentimiento,fecha_consentimiento_utc)
VALUES (:nombre,:correo,:fecha,1,:version,:fecha) RETURNING id_usuario INTO :id"))
                {
                    comando.Agregar("nombre", OracleDbType.Varchar2, ciudadano.Nombre);
                    comando.Agregar("correo", OracleDbType.Varchar2, ciudadano.CorreoElectronico);
                    comando.Agregar("fecha", OracleDbType.TimeStamp, ciudadano.FechaRegistroUtc);
                    comando.Agregar("version", OracleDbType.Varchar2, ciudadano.VersionConsentimiento);
                    OracleParameter salida = comando.AgregarIdSalida();
                    await TraductorErroresOracle.TraducirUnicidadAsync(() => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
                    id = ComandosOracle.LeerId(salida);
                }
                using (OracleCommand comando = Crear(conexion, transaccion,
                    "INSERT INTO usuario_rol(id_usuario,id_rol) SELECT :id,id_rol FROM rol WHERE codigo=1"))
                {
                    comando.Agregar("id", OracleDbType.Int32, id);
                    ComandosOracle.ExigirUnaFila(await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), "rol ciudadano", id);
                }
                var credencial = new CredencialAcceso(id, hash, sal, iteraciones, alta.FechaUtc);
                using (OracleCommand comando = Crear(conexion, transaccion, @"INSERT INTO credencial_acceso(id_usuario,hash_contrasena,sal,iteraciones,fecha_cambio_utc)
VALUES (:id,:hash,:sal,:iteraciones,:fecha)"))
                {
                    comando.Agregar("id", OracleDbType.Int32, id);
                    comando.Agregar("hash", OracleDbType.Raw, credencial.Hash);
                    comando.Agregar("sal", OracleDbType.Raw, credencial.Sal);
                    comando.Agregar("iteraciones", OracleDbType.Int32, credencial.Iteraciones);
                    comando.Agregar("fecha", OracleDbType.TimeStamp, credencial.FechaCambioUtc);
                    await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
                return id;
            }
            catch
            {
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        private static OracleCommand Crear(OracleConnection conexion, OracleTransaction transaccion, string sql)
        {
            OracleCommand comando = ComandosOracle.Crear(conexion, sql, transaccion);
            comando.CommandTimeout = 10;
            return comando;
        }
    }
}
