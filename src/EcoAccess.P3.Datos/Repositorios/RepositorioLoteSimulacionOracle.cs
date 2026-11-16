using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{
    /// <summary>Tablas LOTE_SIMULACION y REGISTRO_SIMULACION (V025). Solo el generador de lotes las escribe.</summary>
    public sealed class RepositorioLoteSimulacionOracle
    {
        private static readonly Regex FormatoLote = new(@"^SIM-[A-Z0-9-]{4,36}$", RegexOptions.CultureInvariant);
        private static readonly string[] Tablas = { "USUARIO", "PUNTO_INSTALACION", "CONTENEDOR", "INTERACCION", "DEPOSITO" };
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioLoteSimulacionOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica ?? throw new ArgumentNullException(nameof(fabrica));
        }

        public static bool LoteValido(string? idLote) => idLote is not null && FormatoLote.IsMatch(idLote);

        public async Task<bool> TablaDisponibleAsync(CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "SELECT COUNT(*) FROM user_tables WHERE table_name='REGISTRO_SIMULACION'");
            return ConversionOracle.Entero(await cmd.ExecuteScalarAsync(ct) ?? 0) == 1;
        }

        /// <summary>Crea el lote si no existe. Repetirlo con el mismo id no cambia nada.</summary>
        public async Task AsegurarLoteAsync(string idLote, string descripcion, int semilla, DateTime ahoraUtc, CancellationToken ct = default)
        {
            if (!LoteValido(idLote)) throw new ArgumentException("Id de lote inválido (SIM-…).", nameof(idLote));
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c,
                "MERGE INTO lote_simulacion l USING (SELECT :id id_lote FROM dual) d ON (l.id_lote=d.id_lote) " +
                "WHEN NOT MATCHED THEN INSERT (id_lote, descripcion, semilla, fecha_creacion_utc) VALUES (:id, :descripcion, :semilla, :fecha)");
            cmd.Agregar("id", OracleDbType.Varchar2, idLote);
            cmd.Agregar("descripcion", OracleDbType.Varchar2, descripcion);
            cmd.Agregar("semilla", OracleDbType.Int32, semilla);
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahoraUtc));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>Id de la fila que el lote creó con esa clave, o null si todavía no la creó.</summary>
        public async Task<int?> BuscarAsync(string idLote, string clave, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "SELECT MIN(id_registro) FROM registro_simulacion WHERE id_lote=:lote AND clave=:clave");
            cmd.Agregar("lote", OracleDbType.Varchar2, idLote);
            cmd.Agregar("clave", OracleDbType.Varchar2, clave);
            object? valor = await cmd.ExecuteScalarAsync(ct);
            return valor is null or DBNull ? null : ConversionOracle.Entero(valor);
        }

        public async Task RegistrarAsync(string idLote, string tabla, int idRegistro, string clave, CancellationToken ct = default)
        {
            if (Array.IndexOf(Tablas, tabla) < 0) throw new ArgumentOutOfRangeException(nameof(tabla));
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "INSERT INTO registro_simulacion (id_lote, tabla, id_registro, clave) VALUES (:lote, :tabla, :id, :clave)");
            cmd.Agregar("lote", OracleDbType.Varchar2, idLote);
            cmd.Agregar("tabla", OracleDbType.Varchar2, tabla);
            cmd.Agregar("id", OracleDbType.Int32, idRegistro);
            cmd.Agregar("clave", OracleDbType.Varchar2, clave);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>Configura una caneca del lote como blanca, verde o negra (V021) con un ESP32 ficticio propio.</summary>
        public async Task ConfigurarCanecaAsync(int idContenedor, int categoria, string dispositivo, string ubicacion, DateTime ahoraUtc, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c,
                "MERGE INTO configuracion_caneca k USING (SELECT :id id_contenedor FROM dual) d ON (k.id_contenedor=d.id_contenedor) " +
                "WHEN NOT MATCHED THEN INSERT (id_contenedor, categoria, identificador_dispositivo, ubicacion_en_estacion, fecha_actualizacion_utc) " +
                "VALUES (:id, :categoria, :dispositivo, :ubicacion, :fecha)");
            cmd.Agregar("id", OracleDbType.Int32, idContenedor);
            cmd.Agregar("categoria", OracleDbType.Int32, categoria);
            cmd.Agregar("dispositivo", OracleDbType.Varchar2, dispositivo);
            cmd.Agregar("ubicacion", OracleDbType.Varchar2, ubicacion);
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(ahoraUtc));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>Recalcula los conteos del lote desde REGISTRO_SIMULACION y lo marca COMPLETO.</summary>
        public async Task CompletarAsync(string idLote, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c,
                "UPDATE lote_simulacion SET estado='COMPLETO', " +
                "ciudadanos=(SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote AND tabla='USUARIO' AND clave LIKE 'ciudadano-%'), " +
                "registros=(SELECT COUNT(*) FROM registro_simulacion WHERE id_lote=:lote AND tabla='DEPOSITO') WHERE id_lote=:lote");
            cmd.BindByName = true;
            cmd.Agregar("lote", OracleDbType.Varchar2, idLote);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
