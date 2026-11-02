using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed class RepositorioAnalisisImagenOracle : IRepositorioAnalisisImagen
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioAnalisisImagenOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string OrdenIntentos = "CASE WHEN REGEXP_LIKE(clave,'^telegram:[0-9]{1,10}:foto:[0-9]{1,10}$') " +
            "THEN TO_NUMBER(REGEXP_SUBSTR(clave,'[0-9]+$')) ELSE -1 END DESC,fecha_utc DESC,clave DESC";

        public async Task GuardarAsync(AnalisisImagen a, CancellationToken ct = default)
        {
            Compat.ThrowIfNull(a, nameof(a));
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "INSERT INTO analisis_imagen " +
                "(clave,id_interaccion,proveedor,modelo,version_modelo,estado,id_material_sugerido,fecha_utc) " +
                "VALUES (:clave,:interaccion,:proveedor,:modelo,:version,:estado,:material,:fecha)");
            cmd.Agregar("clave", OracleDbType.Varchar2, a.Clave);
            cmd.Agregar("interaccion", OracleDbType.Int32, a.IdInteraccion);
            cmd.Agregar("proveedor", OracleDbType.Varchar2, a.Proveedor);
            cmd.Agregar("modelo", OracleDbType.Varchar2, a.Modelo);
            cmd.Agregar("version", OracleDbType.Varchar2, a.Version);
            cmd.Agregar("estado", OracleDbType.Int32, a.Estado);
            cmd.Agregar("material", OracleDbType.Int32, a.IdMaterialSugerido);
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(a.FechaUtc));
            // Una clave repetida no reescribe ni oculta un análisis distinto.
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<AnalisisImagen?> ObtenerUltimoAsync(int interaccion, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "SELECT clave,proveedor,modelo,version_modelo,estado,id_material_sugerido,fecha_utc," +
                "id_material_confirmado,fecha_confirmacion_utc FROM analisis_imagen WHERE id_interaccion=:id " +
                "ORDER BY " + OrdenIntentos + " FETCH FIRST 1 ROWS ONLY");
            cmd.Agregar("id", OracleDbType.Int32, interaccion);
            using var r = await cmd.ExecuteReaderAsync(ct);
            return !await r.ReadAsync(ct) ? null : new AnalisisImagen(r.GetString(0), interaccion, r.GetString(1), r.GetString(2), r.GetString(3),
                r.GetInt32(4), r.IsDBNull(5) ? null : r.GetInt32(5), ConversionOracle.FechaUtc(r.GetValue(6)),
                r.IsDBNull(7) ? null : r.GetInt32(7), r.IsDBNull(8) ? null : ConversionOracle.FechaUtc(r.GetValue(8)));
        }

        public async Task ConfirmarAsync(int interaccion, int material, DateTime fechaUtc, CancellationToken ct = default)
        {
            if (fechaUtc.Kind != DateTimeKind.Utc || interaccion <= 0 || material <= 0) throw new ArgumentException("Confirmación inválida.");
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "UPDATE analisis_imagen SET id_material_confirmado=:material,fecha_confirmacion_utc=:fecha " +
                "WHERE clave=(SELECT clave FROM analisis_imagen WHERE id_interaccion=:id ORDER BY " + OrdenIntentos + " FETCH FIRST 1 ROWS ONLY) " +
                "AND id_material_confirmado IS NULL AND fecha_utc<=:fecha");
            cmd.Agregar("material", OracleDbType.Int32, material);
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(fechaUtc));
            cmd.Agregar("id", OracleDbType.Int32, interaccion);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
