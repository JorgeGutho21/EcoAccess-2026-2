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

    /// <summary>Puntos de instalación en la tabla <c>punto_instalacion</c>.</summary>
    public sealed class RepositorioPuntosInstalacionOracle : IRepositorioPuntosInstalacion
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioPuntosInstalacionOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string Columnas = "id_punto_instalacion, nombre, referencia_ubicacion, activo";

        public async Task<PuntoInstalacion?> ObtenerPorIdAsync(int idPunto, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM punto_instalacion WHERE id_punto_instalacion = :id");
            comando.Agregar("id", OracleDbType.Int32, idPunto);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? Leer(lector) : null;
        }

        public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM punto_instalacion WHERE nombre = :nombre");
            comando.Agregar("nombre", OracleDbType.Varchar2, nombre);
            object? cantidad = await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return ConversionOracle.Entero(cantidad!) > 0;
        }

        public async Task<IReadOnlyList<PuntoInstalacion>> ListarAsync(CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM punto_instalacion ORDER BY NLSSORT(nombre, 'NLS_SORT=BINARY'), id_punto_instalacion");
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var puntos = new List<PuntoInstalacion>();
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                puntos.Add(Leer(lector));
            }

            return puntos;
        }

        public async Task<int> AgregarAsync(PuntoInstalacion punto, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(punto, nameof(punto));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO punto_instalacion (nombre, referencia_ubicacion, activo) " +
                "VALUES (:nombre, :referencia, :activo) RETURNING id_punto_instalacion INTO :id");
            comando.Agregar("nombre", OracleDbType.Varchar2, punto.Nombre);
            comando.Agregar("referencia", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(punto.ReferenciaUbicacion));
            comando.Agregar("activo", OracleDbType.Int32, ConversionOracle.DesdeLogico(punto.Activo));
            OracleParameter id = comando.AgregarIdSalida();

            await TraductorErroresOracle.TraducirUnicidadAsync(
                () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            return ComandosOracle.LeerId(id);
        }

        /// <summary>
        /// Solo <c>activo</c> cambia después del alta. Se bloquea la fila del punto: <c>CambiarEstadoAsync</c> de
        /// contenedores toma el mismo bloqueo antes de poner una caneca en servicio, así que el conteo de canecas en
        /// servicio que se hace aquí no puede quedar viejo antes del commit.
        /// </summary>
        public async Task CambiarActivoAsync(int idPunto, bool activo, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await BloquearAsync(conexion, transaccion, idPunto, cancellationToken).ConfigureAwait(false);
            if (!activo && await ContarEnServicioAsync(conexion, transaccion, idPunto, cancellationToken).ConfigureAwait(false) > 0)
            {
                // Sin token: el rollback debe ocurrir aunque la operación se haya cancelado.
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new CambioConcurrenteException(MensajesCatalogo.PuntoConCanecasEnServicio);
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE punto_instalacion SET activo = :activo WHERE id_punto_instalacion = :id", transaccion))
            {
                comando.Agregar("activo", OracleDbType.Int32, ConversionOracle.DesdeLogico(activo));
                comando.Agregar("id", OracleDbType.Int32, idPunto);
                ComandosOracle.ExigirUnaFila(await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false),
                    "punto de instalación", idPunto);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Bloquea la fila del punto y devuelve si está activo; lanza si no existe. Orden de bloqueo del catálogo:
        /// punto, luego material, luego caneca.
        /// </summary>
        internal static async Task<bool> BloquearAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idPunto, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT activo FROM punto_instalacion WHERE id_punto_instalacion = :id FOR UPDATE WAIT 10", transaccion);
            comando.Agregar("id", OracleDbType.Int32, idPunto);
            object? valor = await TraductorErroresOracle.TraducirEsperaAsync(
                () => comando.ExecuteScalarAsync(cancellationToken)).ConfigureAwait(false);
            return ConversionOracle.Logico(valor ?? throw new InvalidOperationException($"No existe punto de instalación con id {idPunto}."));
        }

        private static async Task<int> ContarEnServicioAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idPunto, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM contenedor WHERE id_punto_instalacion = :id AND estado = :estado", transaccion);
            comando.Agregar("id", OracleDbType.Int32, idPunto);
            comando.Agregar("estado", OracleDbType.Int32, (int)EstadoContenedor.EnServicio);
            return ConversionOracle.Entero((await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
        }

        private static PuntoInstalacion Leer(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            lector.GetString(1),
            ConversionOracle.TextoOpcional(lector.GetValue(2)),
            ConversionOracle.Logico(lector.GetValue(3)));
    }
}
