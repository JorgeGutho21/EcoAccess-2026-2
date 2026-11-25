using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Vision35.Expo;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{
    /// <summary>
    /// Tabla OPERACION_ESTACION (migración V030): una fila por operación de la estación de tres canecas, desde el PC,
    /// Telegram o la web de control. MERGE por clave de idempotencia: reintentar o sincronizar nunca duplica la fila y
    /// el estado solo avanza (EN_EJECUCION → CICLO_COMPLETADO → DISPOSICION_CONFIRMADA).
    /// </summary>
    public sealed class RepositorioOperacionesEstacionOracle : IRegistroOperacionesEstacion
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioOperacionesEstacionOracle(FabricaConexionesOracle fabrica) => _fabrica = fabrica;

        public async Task<bool> TablaDisponibleAsync(CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion, "SELECT COUNT(*) FROM user_tables WHERE table_name='OPERACION_ESTACION'");
            return ConversionOracle.Entero(await cmd.ExecuteScalarAsync(ct) ?? 0) == 1;
        }

        public async Task GuardarAsync(OperacionEstacion o, CancellationToken ct = default)
        {
            if (o is null || string.IsNullOrWhiteSpace(o.Clave) || o.Clave.Length > 100) throw new ArgumentException("Operación de estación inválida.");
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion,
                "MERGE INTO operacion_estacion d USING (SELECT :clave clave FROM dual) s ON (d.clave = s.clave) " +
                "WHEN MATCHED THEN UPDATE SET d.estado_operacion = :estado, d.estado_hardware = :hardware, d.tipo = :tipo, " +
                "  d.disposicion_declarada = GREATEST(d.disposicion_declarada, :declarada), d.disposicion_verificada = GREATEST(d.disposicion_verificada, :verificada), " +
                "  d.detalle = :detalle, d.sincronizada_offline = GREATEST(d.sincronizada_offline, :sinc), d.fecha_actualizacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP) " +
                "  WHERE d.estado_operacion NOT IN ('DISPOSICION_CONFIRMADA') OR :estado = 'DISPOSICION_CONFIRMADA' " +
                "WHEN NOT MATCHED THEN INSERT (clave, fecha_utc, canal, tipo, id_usuario, solicitante, estacion, materiales, categorias, canecas, " +
                "  cantidad_residuos, duracion_ms, modalidad, resultado_reconocimiento, estado_hardware, estado_operacion, disposicion_declarada, " +
                "  disposicion_verificada, modelo_vision, latencia_ms, sincronizada_offline, detalle, fecha_actualizacion_utc) " +
                "VALUES (:clave, :fecha, :canal, :tipo, :usuario, :solicitante, :estacion, :materiales, :categorias, :canecas, :cantidad, :duracion, " +
                "  :modalidad, :resultado, :hardware, :estado, :declarada, :verificada, :modelo, :latencia, :sinc, :detalle, SYS_EXTRACT_UTC(SYSTIMESTAMP))");
            cmd.BindByName = true;
            cmd.Agregar("clave", OracleDbType.Varchar2, o.Clave);
            cmd.Agregar("estado", OracleDbType.Varchar2, o.Estado.ToString());
            cmd.Agregar("hardware", OracleDbType.Varchar2, o.EstadoHardware.ToString());
            cmd.Agregar("tipo", OracleDbType.Varchar2, o.Tipo.ToString());
            cmd.Agregar("declarada", OracleDbType.Int32, o.DisposicionDeclarada ? 1 : 0);
            cmd.Agregar("verificada", OracleDbType.Int32, o.DisposicionVerificada ? 1 : 0);
            cmd.Agregar("detalle", OracleDbType.Varchar2, Recortar(o.Detalle, 300));
            cmd.Agregar("sinc", OracleDbType.Int32, o.SincronizadaOffline ? 1 : 0);
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(DateTime.SpecifyKind(o.FechaUtc, DateTimeKind.Utc)));
            cmd.Agregar("canal", OracleDbType.Varchar2, o.Canal.ToString());
            cmd.Agregar("usuario", OracleDbType.Int32, o.IdUsuario);
            cmd.Agregar("solicitante", OracleDbType.Varchar2, Recortar(o.Solicitante, 60));
            cmd.Agregar("estacion", OracleDbType.Varchar2, Recortar(o.Estacion, 60));
            cmd.Agregar("materiales", OracleDbType.Varchar2, Recortar(o.Materiales, 400));
            cmd.Agregar("categorias", OracleDbType.Varchar2, Recortar(o.Categorias, 80));
            cmd.Agregar("canecas", OracleDbType.Varchar2, Recortar(o.Canecas, 30));
            cmd.Agregar("cantidad", OracleDbType.Int32, o.CantidadResiduos is int c ? Math.Min(99, Math.Max(0, c)) : null);
            cmd.Agregar("duracion", OracleDbType.Int32, Math.Min(60000, Math.Max(0, o.DuracionMs)));
            cmd.Agregar("modalidad", OracleDbType.Varchar2, o.Modalidad);
            cmd.Agregar("resultado", OracleDbType.Varchar2, Recortar(o.ResultadoReconocimiento, 30));
            cmd.Agregar("modelo", OracleDbType.Varchar2, Recortar(o.ModeloVision, 60));
            cmd.Agregar("latencia", OracleDbType.Int32, o.LatenciaMs is long l ? (int)Math.Min(9_999_999, Math.Max(0, l)) : null);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion,
                "SELECT clave, fecha_utc, canal, tipo, id_usuario, solicitante, estacion, materiales, categorias, canecas, cantidad_residuos, duracion_ms, " +
                "modalidad, resultado_reconocimiento, estado_hardware, estado_operacion, disposicion_declarada, disposicion_verificada, modelo_vision, latencia_ms, " +
                "sincronizada_offline, detalle FROM operacion_estacion " + (idUsuario is null ? "" : "WHERE id_usuario = :usuario ") +
                "ORDER BY fecha_utc DESC FETCH FIRST :maximo ROWS ONLY");
            cmd.BindByName = true;
            if (idUsuario is int u) cmd.Agregar("usuario", OracleDbType.Int32, u);
            cmd.Agregar("maximo", OracleDbType.Int32, Math.Min(200, Math.Max(1, maximo)));
            var lista = new List<OperacionEstacion>();
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                lista.Add(new OperacionEstacion(r.GetString(0), DateTime.SpecifyKind(r.GetDateTime(1), DateTimeKind.Utc),
                    Enum.TryParse(r.GetString(2), out CanalEstacion canal) ? canal : CanalEstacion.PC,
                    Enum.TryParse(r.GetString(3), out TipoOperacionEstacion tipo) ? tipo : TipoOperacionEstacion.APERTURA_MANUAL,
                    r.IsDBNull(4) ? null : ConversionOracle.Entero(r.GetValue(4)), Texto(r, 5), r.GetString(6), Texto(r, 7), Texto(r, 8), Texto(r, 9),
                    r.IsDBNull(10) ? null : ConversionOracle.Entero(r.GetValue(10)), ConversionOracle.Entero(r.GetValue(11)), r.GetString(12), Texto(r, 13),
                    Enum.TryParse(r.GetString(14), out EstadoHardwareEstacion hw) ? hw : EstadoHardwareEstacion.NO_SOLICITADO,
                    Enum.TryParse(r.GetString(15), out EstadoOperacionEstacion est) ? est : EstadoOperacionEstacion.REGISTRADA,
                    ConversionOracle.Entero(r.GetValue(16)) == 1, ConversionOracle.Entero(r.GetValue(17)) == 1, Texto(r, 18),
                    r.IsDBNull(19) ? null : ConversionOracle.Entero(r.GetValue(19)), ConversionOracle.Entero(r.GetValue(20)) == 1, Texto(r, 21)));
            }
            return lista;
        }

        private static string? Texto(OracleDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
        private static string? Recortar(string? s, int n) => s is null ? null : s.Length <= n ? s : s.Substring(0, n);
    }
}
