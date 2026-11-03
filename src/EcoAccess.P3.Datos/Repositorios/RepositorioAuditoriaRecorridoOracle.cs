using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed record ConteoAuditoria(string Clave, int Cantidad);

    /// <summary>Lo ocurrido desde una fecha. Solo conteos: ningún dato de personas, mensajes ni fotos.</summary>
    public sealed record AuditoriaRecorrido(DateTime DesdeUtc, IReadOnlyList<ConteoAuditoria> InteraccionesPorCanalYEstado,
        IReadOnlyList<ConteoAuditoria> AnalisisPorModelo, IReadOnlyList<ConteoAuditoria> OrdenesPorEstado, IReadOnlyList<ConteoAuditoria> EventosPorTipo,
        int Depositos, int CreditosEcopuntos, long PuntosAcreditados, int InteraccionesConVariasOrdenes, int OrdenesConVariosDepositos,
        int DepositosConVariosCreditos, int InteraccionesRepetidasEn5Segundos)
    {
        public bool SinDuplicados => InteraccionesConVariasOrdenes == 0 && OrdenesConVariosDepositos == 0
            && DepositosConVariosCreditos == 0 && InteraccionesRepetidasEn5Segundos == 0;
    }

    /// <summary>Solo SELECT. Todo se acota a las interacciones iniciadas desde la fecha pedida.</summary>
    public sealed class RepositorioAuditoriaRecorridoOracle
    {
        private const string Ordenes = "orden_apertura o JOIN interaccion i ON i.id_interaccion=o.id_interaccion WHERE i.fecha_inicio_utc>=:desde";
        private const string Depositos = "deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden JOIN interaccion i ON i.id_interaccion=o.id_interaccion WHERE i.fecha_inicio_utc>=:desde";
        private const string Creditos = "movimiento_ecopuntos m JOIN deposito d ON d.id_deposito=m.id_deposito JOIN orden_apertura o ON o.id_orden=d.id_orden JOIN interaccion i ON i.id_interaccion=o.id_interaccion WHERE i.fecha_inicio_utc>=:desde AND m.tipo=1";

        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioAuditoriaRecorridoOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<AuditoriaRecorrido> ConsultarAsync(DateTime desdeUtc, CancellationToken ct = default)
        {
            if (desdeUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("La fecha debe estar en UTC.", nameof(desdeUtc));
            using OracleConnection c = await _fabrica.AbrirAsync(ct);

            async Task<List<ConteoAuditoria>> AgruparAsync(string sql, Func<object, object, string> clave)
            {
                using var comando = ComandosOracle.Crear(c, sql);
                comando.Agregar("desde", OracleDbType.TimeStamp, desdeUtc);
                using var lector = await comando.ExecuteReaderAsync(ct);
                var filas = new List<ConteoAuditoria>();
                while (await lector.ReadAsync(ct))
                    filas.Add(new ConteoAuditoria(clave(lector.GetValue(0), lector.GetValue(1)), Convert.ToInt32(lector.GetValue(2))));
                return filas;
            }
            async Task<long> ContarAsync(string sql)
            {
                using var comando = ComandosOracle.Crear(c, sql);
                comando.Agregar("desde", OracleDbType.TimeStamp, desdeUtc);
                return Convert.ToInt64(await comando.ExecuteScalarAsync(ct));
            }

            var interacciones = await AgruparAsync("SELECT canal,estado,COUNT(*) FROM interaccion WHERE fecha_inicio_utc>=:desde GROUP BY canal,estado ORDER BY canal,estado",
                (canal, estado) => $"{(CanalInteraccion)Convert.ToInt32(canal)}/{(EstadoInteraccion)Convert.ToInt32(estado)}");
            var analisis = await AgruparAsync("SELECT a.modelo,0,COUNT(*) FROM analisis_imagen a JOIN interaccion i ON i.id_interaccion=a.id_interaccion WHERE i.fecha_inicio_utc>=:desde GROUP BY a.modelo ORDER BY a.modelo",
                (modelo, _) => Convert.ToString(modelo)!);
            var ordenes = await AgruparAsync($"SELECT o.estado,0,COUNT(*) FROM {Ordenes} GROUP BY o.estado ORDER BY o.estado",
                (estado, _) => ((EstadoOrdenApertura)Convert.ToInt32(estado)).ToString());
            var eventos = await AgruparAsync("SELECT e.tipo,0,COUNT(*) FROM evento_dispositivo e JOIN orden_apertura o ON o.id_orden=e.id_orden JOIN interaccion i ON i.id_interaccion=o.id_interaccion WHERE i.fecha_inicio_utc>=:desde GROUP BY e.tipo ORDER BY e.tipo",
                (tipo, _) => ((TipoEventoDispositivo)Convert.ToInt32(tipo)).ToString());

            return new AuditoriaRecorrido(desdeUtc, interacciones, analisis, ordenes, eventos,
                (int)await ContarAsync("SELECT COUNT(*) FROM " + Depositos),
                (int)await ContarAsync("SELECT COUNT(*) FROM " + Creditos),
                await ContarAsync("SELECT NVL(SUM(m.puntos),0) FROM " + Creditos),
                (int)await ContarAsync($"SELECT COUNT(*) FROM (SELECT o.id_interaccion FROM {Ordenes} GROUP BY o.id_interaccion HAVING COUNT(*)>1)"),
                (int)await ContarAsync($"SELECT COUNT(*) FROM (SELECT d.id_orden FROM {Depositos} GROUP BY d.id_orden HAVING COUNT(*)>1)"),
                (int)await ContarAsync($"SELECT COUNT(*) FROM (SELECT m.id_deposito FROM {Creditos} GROUP BY m.id_deposito HAVING COUNT(*)>1)"),
                (int)await ContarAsync("SELECT COUNT(*) FROM (SELECT fecha_inicio_utc - LAG(fecha_inicio_utc) OVER (PARTITION BY id_usuario,id_contenedor ORDER BY fecha_inicio_utc) d " +
                    "FROM interaccion WHERE fecha_inicio_utc>=:desde AND id_usuario IS NOT NULL) WHERE d < INTERVAL '5' SECOND"));
        }
    }
}
