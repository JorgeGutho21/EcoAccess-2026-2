using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>Conteos de todo el historial, con una instantánea SQL común para los once indicadores.</summary>
    public sealed class RepositorioPanelAdministrativoOracle : IRepositorioPanelAdministrativo
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioPanelAdministrativoOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string SqlIndicadores = @"SELECT (SELECT COUNT(*) FROM contenedor),
       (SELECT COUNT(*) FROM contenedor WHERE estado=1),
       (SELECT COUNT(*) FROM material WHERE habilitado=1),
       (SELECT COUNT(*) FROM deposito),
       (SELECT COUNT(*) FROM deposito WHERE id_material_verificado IS NOT NULL),
       (SELECT COUNT(*) FROM interaccion),
       (SELECT COUNT(*) FROM evento_dispositivo WHERE resultado=2 OR tipo=5),
       (SELECT NVL(SUM(puntos),0) FROM movimiento_ecopuntos),
       (SELECT NVL(SUM(puntos),0) FROM movimiento_ecopuntos WHERE tipo=1),
       (SELECT COUNT(DISTINCT ur.id_usuario) FROM usuario_rol ur JOIN rol r ON r.id_rol=ur.id_rol
          JOIN usuario u ON u.id_usuario=ur.id_usuario WHERE r.codigo=1 AND u.activo=1),
       (SELECT COUNT(*) FROM interaccion WHERE id_material_sugerido IS NOT NULL)
FROM dual";

        public async Task<ResumenPanelAdministrativo> ConsultarAsync(CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            IndicadoresPanel indicadores;
            using (OracleCommand comando = ComandosOracle.Crear(conexion, SqlIndicadores))
            {
                comando.CommandTimeout = 10;
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("Sin indicadores.");
                indicadores = new(lector.GetInt64(0), lector.GetInt64(1), lector.GetInt64(2), lector.GetInt64(3),
                    lector.GetInt64(4), lector.GetInt64(5), lector.GetInt64(6), lector.GetInt64(7), lector.GetInt64(8),
                    lector.GetInt64(9), lector.GetInt64(10));
            }
            var actividad = new List<ActividadPanel>();
            using (OracleCommand comando = ComandosOracle.Crear(conexion, @"SELECT d.id_deposito, d.fecha_deteccion_utc, COALESCE(mv.nombre,md.nombre),
       CASE WHEN d.id_material_verificado IS NULL THEN 0 ELSE 1 END, c.nombre
FROM deposito d JOIN material md ON md.id_material=d.id_material_declarado
JOIN orden_apertura o ON o.id_orden=d.id_orden JOIN contenedor c ON c.id_contenedor=o.id_contenedor
LEFT JOIN material mv ON mv.id_material=d.id_material_verificado
ORDER BY d.fecha_deteccion_utc DESC,d.id_deposito DESC FETCH FIRST 8 ROWS ONLY"))
            {
                comando.CommandTimeout = 10;
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                    actividad.Add(new(lector.GetInt32(0), ConversionOracle.FechaUtc(lector.GetDateTime(1)), lector.GetString(2), lector.GetInt32(3) == 1, lector.GetString(4)));
            }
            return new(indicadores, actividad.AsReadOnly());
        }
    }
}
