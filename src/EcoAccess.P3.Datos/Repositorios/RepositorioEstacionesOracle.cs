using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{
    /// <summary>Lee CONFIGURACION_CANECA (V021) de la estación de una caneca. Sin la tabla devuelve vacío.</summary>
    public sealed class RepositorioEstacionesOracle : IRepositorioEstaciones
    {
        private const int TablaInexistente = 942;
        private const int EstadoEnServicio = 1;
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioEstacionesOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<IReadOnlyList<CanecaEstacion>> ListarEstacionDeAsync(int idContenedor, CancellationToken ct = default)
        {
            var canecas = new List<CanecaEstacion>();
            if (idContenedor <= 0) return canecas;
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion,
                "SELECT c.id_contenedor, c.nombre, k.categoria, c.estado, k.identificador_dispositivo, k.ubicacion_en_estacion " +
                "FROM contenedor c JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor " +
                "WHERE c.id_punto_instalacion = (SELECT e.id_punto_instalacion FROM contenedor e WHERE e.id_contenedor = :id) " +
                "AND c.estado <> 3 ORDER BY k.categoria, c.id_contenedor");
            cmd.Agregar("id", OracleDbType.Int32, idContenedor);
            try
            {
                using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                {
                    canecas.Add(new CanecaEstacion(ConversionOracle.Entero(r.GetValue(0)), r.GetString(1),
                        (CategoriaMaterial)ConversionOracle.Entero(r.GetValue(2)), ConversionOracle.Entero(r.GetValue(3)) == EstadoEnServicio,
                        r.IsDBNull(4) ? null : r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5)));
                }
            }
            // Base sin V021: la caneca sigue funcionando como caneca única.
            catch (OracleException e) when (e.Number == TablaInexistente) { }
            return canecas;
        }
    }
}
