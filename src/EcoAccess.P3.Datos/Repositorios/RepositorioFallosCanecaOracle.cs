using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>Consulta de solo lectura: ¿la caneca informó un fallo físico reciente que ninguna apertura posterior haya superado?</summary>
    public sealed class RepositorioFallosCanecaOracle
    {
        private const int TipoApertura = 2, TipoFallo = 5;

        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioFallosCanecaOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica ?? throw new ArgumentNullException(nameof(fabrica));
        }

        public async Task<bool> TieneFalloRecienteAsync(int idContenedor, DateTime desdeUtc, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM evento_dispositivo f WHERE f.id_contenedor = :contenedor AND f.tipo = " + TipoFallo +
                " AND f.fecha_recepcion_utc >= :desde AND NOT EXISTS (SELECT 1 FROM evento_dispositivo a WHERE a.id_contenedor = f.id_contenedor" +
                " AND a.tipo = " + TipoApertura + " AND a.fecha_recepcion_utc > f.fecha_recepcion_utc)");
            comando.Agregar("contenedor", OracleDbType.Int32, idContenedor);
            comando.Agregar("desde", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(desdeUtc));
            return ConversionOracle.Entero(await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;
        }
    }
}
