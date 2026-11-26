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
    /// Tabla OPERACION_ESTACION (V030) y, si existe, su detalle por residuo e invitados (V033): una fila por operación de
    /// la estación de tres canecas desde el PC, Telegram o la web. MERGE por clave de idempotencia: reintentar o
    /// sincronizar nunca duplica la fila ni el detalle, y el estado solo avanza. Sin V033 escribe solo la cabecera.
    /// </summary>
    public sealed class RepositorioOperacionesEstacionOracle : IRegistroOperacionesEstacion
    {
        private readonly FabricaConexionesOracle _fabrica;
        private bool? _v031;
        private DateTime _v031ComprobadoUtc = DateTime.MinValue;

        public RepositorioOperacionesEstacionOracle(FabricaConexionesOracle fabrica) => _fabrica = fabrica;

        public async Task<bool> TablaDisponibleAsync(CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion, "SELECT COUNT(*) FROM user_tables WHERE table_name='OPERACION_ESTACION'");
            return ConversionOracle.Entero(await cmd.ExecuteScalarAsync(ct) ?? 0) == 1;
        }

        /// <summary>¿Está aplicada V033 (detalle e invitados)? Se vuelve a comprobar cada 5 minutos mientras no lo esté.</summary>
        public async Task<bool> DetalleDisponibleAsync(OracleConnection conexion, CancellationToken ct = default)
        {
            if (_v031 == true || (_v031 == false && DateTime.UtcNow - _v031ComprobadoUtc < TimeSpan.FromMinutes(5))) return _v031.Value;
            using var cmd = ComandosOracle.Crear(conexion,
                "SELECT (SELECT COUNT(*) FROM user_tables WHERE table_name='OPERACION_ESTACION_DETALLE') + " +
                "(SELECT COUNT(*) FROM user_tab_columns WHERE table_name='OPERACION_ESTACION' AND column_name='METODO_RECONOCIMIENTO') FROM dual");
            _v031 = ConversionOracle.Entero(await cmd.ExecuteScalarAsync(ct) ?? 0) == 2;
            _v031ComprobadoUtc = DateTime.UtcNow;
            return _v031.Value;
        }

        public async Task GuardarAsync(OperacionEstacion o, CancellationToken ct = default)
        {
            if (o is null || string.IsNullOrWhiteSpace(o.Clave) || o.Clave.Length > 100) throw new ArgumentException("Operación de estación inválida.");
            using var conexion = await _fabrica.AbrirAsync(ct);
            bool v031 = await DetalleDisponibleAsync(conexion, ct);
            using var tx = conexion.BeginTransaction();
            using (var cmd = ComandosOracle.Crear(conexion,
                "MERGE INTO operacion_estacion d USING (SELECT :clave clave FROM dual) s ON (d.clave = s.clave) " +
                "WHEN MATCHED THEN UPDATE SET d.estado_operacion = :estado, d.estado_hardware = :hardware, d.tipo = :tipo, " +
                "  d.disposicion_declarada = GREATEST(d.disposicion_declarada, :declarada), d.disposicion_verificada = GREATEST(d.disposicion_verificada, :verificada), " +
                "  d.detalle = :detalle, d.sincronizada_offline = GREATEST(d.sincronizada_offline, :sinc), d.fecha_actualizacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP) " +
                (v031 ? ", d.tipo_usuario = NVL(d.tipo_usuario, :tipousr), d.metodo_reconocimiento = NVL(d.metodo_reconocimiento, :metodo), " +
                        "  d.confianza = NVL(d.confianza, :confianza), d.sesion_anonima = NVL(d.sesion_anonima, :sesion) " : "") +
                "  WHERE d.estado_operacion NOT IN ('DISPOSICION_CONFIRMADA') OR :estado = 'DISPOSICION_CONFIRMADA' " +
                "WHEN NOT MATCHED THEN INSERT (clave, fecha_utc, canal, tipo, id_usuario, solicitante, estacion, materiales, categorias, canecas, " +
                "  cantidad_residuos, duracion_ms, modalidad, resultado_reconocimiento, estado_hardware, estado_operacion, disposicion_declarada, " +
                "  disposicion_verificada, modelo_vision, latencia_ms, sincronizada_offline, detalle, fecha_actualizacion_utc" +
                (v031 ? ", tipo_usuario, metodo_reconocimiento, confianza, sesion_anonima" : "") + ") " +
                "VALUES (:clave, :fecha, :canal, :tipo, :usuario, :solicitante, :estacion, :materiales, :categorias, :canecas, :cantidad, :duracion, " +
                "  :modalidad, :resultado, :hardware, :estado, :declarada, :verificada, :modelo, :latencia, :sinc, :detalle, SYS_EXTRACT_UTC(SYSTIMESTAMP)" +
                (v031 ? ", :tipousr, :metodo, :confianza, :sesion" : "") + ")"))
            {
                cmd.Transaction = tx;
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
                if (v031)
                {
                    cmd.Agregar("tipousr", OracleDbType.Varchar2, o.TipoUsuario is "INVITADO" or "REGISTRADO" ? o.TipoUsuario : (o.IdUsuario is null ? "INVITADO" : "REGISTRADO"));
                    cmd.Agregar("metodo", OracleDbType.Varchar2, o.Metodo is "FOTO" or "TEXTO" or "VOZ" or "MANUAL" ? o.Metodo : null);
                    cmd.Agregar("confianza", OracleDbType.Decimal, o.Confianza is double k ? (decimal?)Math.Round((decimal)Math.Min(1, Math.Max(0, k)), 3) : null);
                    cmd.Agregar("sesion", OracleDbType.Varchar2, Recortar(o.SesionAnonima, 40));
                }
                await cmd.ExecuteNonQueryAsync(ct);
            }
            if (v031 && o.Detalles is { Count: > 0 } detalles)
            {
                int orden = 0;
                foreach (var d in detalles)
                {
                    if (string.IsNullOrWhiteSpace(d.Codigo) || d.Caneca is not ("VERDE" or "BLANCA" or "NEGRA")) continue;
                    orden++;
                    using var det = ComandosOracle.Crear(conexion,
                        "MERGE INTO operacion_estacion_detalle t USING (SELECT :clave clave, :codigo codigo FROM dual) s " +
                        "ON (t.clave_operacion = s.clave AND t.codigo_residuo = s.codigo) " +
                        "WHEN MATCHED THEN UPDATE SET t.cantidad = :cantidad " +
                        "WHEN NOT MATCHED THEN INSERT (clave_operacion, orden, codigo_residuo, nombre_residuo, material_oracle, caneca, cantidad, metodo, confianza) " +
                        "VALUES (:clave, :orden, :codigo, :nombre, :material, :caneca, :cantidad, :metodo, :confianza)");
                    det.Transaction = tx;
                    det.BindByName = true;
                    det.Agregar("clave", OracleDbType.Varchar2, o.Clave);
                    det.Agregar("codigo", OracleDbType.Varchar2, Recortar(d.Codigo, 10));
                    det.Agregar("orden", OracleDbType.Int32, Math.Min(99, orden));
                    det.Agregar("nombre", OracleDbType.Varchar2, Recortar(d.Nombre, 80) ?? d.Codigo);
                    det.Agregar("material", OracleDbType.Varchar2, Recortar(d.MaterialOracle, 40));
                    det.Agregar("caneca", OracleDbType.Varchar2, d.Caneca);
                    det.Agregar("cantidad", OracleDbType.Int32, Math.Min(99, Math.Max(1, d.Cantidad)));
                    det.Agregar("metodo", OracleDbType.Varchar2, d.Metodo is "FOTO" or "TEXTO" or "VOZ" or "MANUAL" ? d.Metodo : "MANUAL");
                    det.Agregar("confianza", OracleDbType.Decimal, d.Confianza is double k ? (decimal?)Math.Round((decimal)Math.Min(1, Math.Max(0, k)), 3) : null);
                    await det.ExecuteNonQueryAsync(ct);
                }
            }
            tx.Commit();
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

        /// <summary>
        /// Filas para las estadísticas del PC entre dos fechas locales (America/Bogota). Con V033 lee la vista
        /// EA_R_ESTADISTICAS_ESTACION (una fila por residuo); sin V033, una fila por operación deducida de la cabecera.
        /// </summary>
        public async Task<IReadOnlyList<FilaEstadisticaEstacion>> EstadisticasAsync(DateTime desdeLocal, DateTime hastaLocal, CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            bool v031 = await DetalleDisponibleAsync(conexion, ct);
            string sql = v031
                ? "SELECT clave, fecha_local, canal, tipo_usuario, metodo, tipo, codigo_residuo, nombre_residuo, caneca, cantidad, canecas_operacion, " +
                  "duracion_ms, estado_hardware, estado_operacion, disposicion_declarada, disposicion_verificada FROM ea_r_estadisticas_estacion " +
                  "WHERE fecha_local >= :desde AND fecha_local < :hasta ORDER BY fecha_local DESC FETCH FIRST 5000 ROWS ONLY"
                : "SELECT clave, CAST(FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE), canal, " +
                  "CASE WHEN id_usuario IS NULL THEN 'INVITADO' ELSE 'REGISTRADO' END, CASE tipo WHEN 'APERTURA_MANUAL' THEN 'MANUAL' WHEN 'RECONOCIMIENTO_AUTOMATICO' THEN 'FOTO' END, " +
                  "tipo, NULL, materiales, canecas, 1, canecas, duracion_ms, estado_hardware, estado_operacion, disposicion_declarada, disposicion_verificada " +
                  "FROM operacion_estacion WHERE CAST(FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) >= :desde " +
                  "AND CAST(FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) < :hasta ORDER BY fecha_utc DESC FETCH FIRST 5000 ROWS ONLY";
            using var cmd = ComandosOracle.Crear(conexion, sql);
            cmd.BindByName = true;
            cmd.Agregar("desde", OracleDbType.Date, desdeLocal);
            cmd.Agregar("hasta", OracleDbType.Date, hastaLocal);
            var filas = new List<FilaEstadisticaEstacion>();
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                filas.Add(new FilaEstadisticaEstacion(r.GetString(0), r.GetDateTime(1), r.GetString(2), Texto(r, 3) ?? "INVITADO", Texto(r, 4), r.GetString(5),
                    Texto(r, 6), Texto(r, 7), Texto(r, 8), r.IsDBNull(9) ? 1 : ConversionOracle.Entero(r.GetValue(9)), Texto(r, 10),
                    ConversionOracle.Entero(r.GetValue(11)), r.GetString(12), r.GetString(13), ConversionOracle.Entero(r.GetValue(14)) == 1,
                    ConversionOracle.Entero(r.GetValue(15)) == 1));
            return filas;
        }

        private static string? Texto(OracleDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
        private static string? Recortar(string? s, int n) => s is null ? null : s.Length <= n ? s : s.Substring(0, n);
    }
}
