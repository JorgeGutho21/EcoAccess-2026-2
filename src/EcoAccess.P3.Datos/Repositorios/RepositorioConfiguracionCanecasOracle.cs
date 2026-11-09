using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{
    /// <summary>
    /// Configuración de las canecas de una estación en <c>configuracion_caneca</c> (V021) y cambio de nombre en
    /// <c>contenedor</c>. Solo SQL parametrizado; las reglas (permisos, colores repetidos) están en
    /// <see cref="Logica.Servicios.ServicioTresCanecas"/>.
    /// </summary>
    public sealed class RepositorioConfiguracionCanecasOracle : IRepositorioConfiguracionCanecas
    {
        private const string Columnas = "k.id_contenedor, k.categoria, k.identificador_dispositivo, k.ubicacion_en_estacion, k.fecha_actualizacion_utc";
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioConfiguracionCanecasOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica ?? throw new ArgumentNullException(nameof(fabrica));
        }

        public async Task<ConfiguracionCaneca?> ObtenerAsync(int idContenedor, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM configuracion_caneca k WHERE k.id_contenedor = :id");
            comando.Agregar("id", OracleDbType.Int32, idContenedor);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? Leer(lector) : null;
        }

        public async Task<IReadOnlyList<ConfiguracionCaneca>> ListarPorPuntoAsync(int idPunto, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM configuracion_caneca k JOIN contenedor c ON c.id_contenedor = k.id_contenedor " +
                "WHERE c.id_punto_instalacion = :punto AND c.estado <> 3 ORDER BY k.categoria, k.id_contenedor");
            comando.Agregar("punto", OracleDbType.Int32, idPunto);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var resultado = new List<ConfiguracionCaneca>();
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false)) resultado.Add(Leer(lector));
            return resultado;
        }

        public async Task GuardarAsync(ConfiguracionCaneca configuracion, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(configuracion, nameof(configuracion));
            await TraductorErroresOracle.TraducirUnicidadAsync(async () =>
            {
                using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
                using OracleCommand comando = ComandosOracle.Crear(conexion,
                    "MERGE INTO configuracion_caneca k USING (SELECT :id AS id_contenedor FROM dual) d " +
                    "ON (k.id_contenedor = d.id_contenedor) " +
                    "WHEN MATCHED THEN UPDATE SET k.categoria = :categoria, k.identificador_dispositivo = :dispositivo, " +
                    "  k.ubicacion_en_estacion = :ubicacion, k.fecha_actualizacion_utc = :fecha " +
                    "WHEN NOT MATCHED THEN INSERT (id_contenedor, categoria, identificador_dispositivo, ubicacion_en_estacion, fecha_actualizacion_utc) " +
                    "  VALUES (d.id_contenedor, :categoria, :dispositivo, :ubicacion, :fecha)");
                comando.Agregar("id", OracleDbType.Int32, configuracion.IdContenedor);
                comando.Agregar("categoria", OracleDbType.Int16, (short)configuracion.Categoria);
                comando.Agregar("dispositivo", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(configuracion.IdentificadorDispositivo));
                comando.Agregar("ubicacion", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(configuracion.UbicacionEnEstacion));
                comando.Agregar("fecha", OracleDbType.TimeStamp, configuracion.FechaActualizacionUtc);
                return await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task RenombrarAsync(int idContenedor, string nombre, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE contenedor SET nombre = :nombre WHERE id_contenedor = :id");
            comando.Agregar("nombre", OracleDbType.Varchar2, nombre);
            comando.Agregar("id", OracleDbType.Int32, idContenedor);
            ComandosOracle.ExigirUnaFila(await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false), "contenedor", idContenedor);
        }

        private static ConfiguracionCaneca Leer(DbDataReader lector) => new ConfiguracionCaneca(
            ConversionOracle.Entero(lector.GetValue(0)),
            (CategoriaMaterial)ConversionOracle.Entero(lector.GetValue(1)),
            lector.IsDBNull(2) ? null : lector.GetString(2),
            lector.IsDBNull(3) ? null : lector.GetString(3),
            DateTime.SpecifyKind(lector.GetDateTime(4), DateTimeKind.Utc));
    }
}
