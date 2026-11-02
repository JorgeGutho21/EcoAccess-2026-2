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

    /// <summary>
    /// Hechos recientes para avisar por Telegram, leídos de las tablas existentes (sin tabla nueva): aperturas y fallos
    /// de <c>evento_dispositivo</c>, cierres sin depósito, depósitos, verificaciones y movimientos de ecopuntos. Solo
    /// incluye cuentas con vínculo vigente; el destinatario es el dueño de la orden, nunca otro ciudadano.
    /// </summary>
    public sealed class RepositorioNotificacionesTelegramOracle : IRepositorioNotificacionesTelegram
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioNotificacionesTelegramOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string Sql =
            "WITH duenos AS (SELECT o.id_orden, v.telegram_user_id, i.id_usuario, i.id_material_confirmado " +
            "  FROM orden_apertura o JOIN interaccion i ON i.id_interaccion = o.id_interaccion " +
            "  JOIN vinculo_telegram v ON v.id_usuario = i.id_usuario AND v.fecha_baja_utc IS NULL) " +
            "SELECT 'ev:' || e.id_evento, CASE e.tipo WHEN 2 THEN 1 ELSE 2 END, d.telegram_user_id, e.id_orden, " +
            "  CAST(NULL AS NUMBER), m.nombre, CAST(NULL AS NUMBER), CAST(NULL AS NUMBER), e.fecha_recepcion_utc " +
            "FROM evento_dispositivo e JOIN duenos d ON d.id_orden = e.id_orden LEFT JOIN material m ON m.id_material = d.id_material_confirmado " +
            "WHERE e.tipo IN (2, 5) AND e.fecha_recepcion_utc >= :desde1 " +
            "UNION ALL " +
            // El cierre se notifica sin depósito solo si la orden no tiene ninguno: el sensor no detectó residuo.
            "SELECT 'ci:' || e.id_evento, 3, d.telegram_user_id, e.id_orden, CAST(NULL AS NUMBER), m.nombre, " +
            "  CAST(NULL AS NUMBER), CAST(NULL AS NUMBER), e.fecha_recepcion_utc " +
            "FROM evento_dispositivo e JOIN duenos d ON d.id_orden = e.id_orden LEFT JOIN material m ON m.id_material = d.id_material_confirmado " +
            "WHERE e.tipo = 4 AND e.fecha_recepcion_utc >= :desde2 AND NOT EXISTS (SELECT 1 FROM deposito x WHERE x.id_orden = e.id_orden) " +
            "UNION ALL " +
            "SELECT 'de:' || p.id_deposito, 4, d.telegram_user_id, p.id_orden, p.id_deposito, m.nombre, " +
            "  CAST(NULL AS NUMBER), CAST(NULL AS NUMBER), p.fecha_deteccion_utc " +
            "FROM deposito p JOIN duenos d ON d.id_orden = p.id_orden JOIN material m ON m.id_material = p.id_material_declarado " +
            "WHERE p.fecha_deteccion_utc >= :desde3 " +
            "UNION ALL " +
            "SELECT 've:' || p.id_deposito, 5, d.telegram_user_id, p.id_orden, p.id_deposito, m.nombre, " +
            "  CAST(NULL AS NUMBER), CAST(NULL AS NUMBER), p.fecha_verificacion_utc " +
            "FROM deposito p JOIN duenos d ON d.id_orden = p.id_orden JOIN material m ON m.id_material = p.id_material_verificado " +
            "WHERE p.fecha_verificacion_utc >= :desde4 " +
            "UNION ALL " +
            "SELECT 'mo:' || me.id_movimiento, CASE me.tipo WHEN 1 THEN 6 ELSE 7 END, d.telegram_user_id, p.id_orden, p.id_deposito, " +
            "  m.nombre, me.puntos, (SELECT SUM(s.puntos) FROM movimiento_ecopuntos s WHERE s.id_usuario = me.id_usuario), op.fecha_utc " +
            "FROM movimiento_ecopuntos me JOIN operacion_ecopuntos op ON op.id_operacion = me.id_operacion " +
            "JOIN deposito p ON p.id_deposito = me.id_deposito JOIN duenos d ON d.id_orden = p.id_orden AND d.id_usuario = me.id_usuario " +
            "LEFT JOIN material m ON m.id_material = NVL(p.id_material_verificado, p.id_material_declarado) " +
            "WHERE op.fecha_utc >= :desde5 " +
            "ORDER BY 9, 1 FETCH FIRST 500 ROWS ONLY";

        public async Task<IReadOnlyList<HechoNotificable>> ListarDesdeAsync(DateTime desdeUtc, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion, Sql);
            DateTime desde = ConversionOracle.ParaColumnaUtc(desdeUtc);
            for (int i = 1; i <= 5; i++) comando.Agregar("desde" + i, OracleDbType.TimeStamp, desde);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var hechos = new List<HechoNotificable>();
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                hechos.Add(new HechoNotificable(
                    (string)lector.GetValue(0),
                    (TipoHechoNotificable)ConversionOracle.Entero(lector.GetValue(1)),
                    RepositorioVinculosTelegramOracle.Largo(lector.GetValue(2)),
                    ConversionOracle.Entero(lector.GetValue(3)),
                    Opcional(lector.GetValue(4)),
                    ConversionOracle.TextoOpcional(lector.GetValue(5)),
                    Opcional(lector.GetValue(6)),
                    Opcional(lector.GetValue(7)),
                    ConversionOracle.FechaUtc(lector.GetValue(8))));
            }

            return hechos;
        }

        private static int? Opcional(object valor) => valor is DBNull ? null : ConversionOracle.Entero(valor);
    }
}
