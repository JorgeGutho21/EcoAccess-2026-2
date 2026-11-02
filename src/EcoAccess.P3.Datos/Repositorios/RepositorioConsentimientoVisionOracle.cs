using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed class RepositorioConsentimientoVisionOracle : IRepositorioConsentimientoVision
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioConsentimientoVisionOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<bool> EstaVigenteAsync(long telegramUserId, string versionAviso, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "SELECT COUNT(*) FROM consentimiento_vision_externa " +
                "WHERE telegram_user_id=:id AND version_aviso=:version AND fecha_revocado_utc IS NULL");
            cmd.Agregar("id", OracleDbType.Int64, telegramUserId);
            cmd.Agregar("version", OracleDbType.Varchar2, versionAviso);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
        }

        public async Task RegistrarAsync(long telegramUserId, string versionAviso, DateTime fechaUtc, CancellationToken ct = default)
        {
            if (fechaUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("La fecha debe estar en UTC.", nameof(fechaUtc));
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "MERGE INTO consentimiento_vision_externa d " +
                "USING (SELECT :id AS telegram_user_id, :version AS version_aviso, :fecha AS fecha FROM dual) o " +
                "ON (d.telegram_user_id=o.telegram_user_id) " +
                "WHEN MATCHED THEN UPDATE SET d.version_aviso=o.version_aviso,d.fecha_otorgado_utc=o.fecha,d.fecha_revocado_utc=NULL " +
                "WHEN NOT MATCHED THEN INSERT (telegram_user_id,version_aviso,fecha_otorgado_utc) VALUES (o.telegram_user_id,o.version_aviso,o.fecha)");
            cmd.Agregar("id", OracleDbType.Int64, telegramUserId);
            cmd.Agregar("version", OracleDbType.Varchar2, versionAviso);
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(fechaUtc));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<bool> RevocarAsync(long telegramUserId, DateTime fechaUtc, CancellationToken ct = default)
        {
            if (fechaUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("La fecha debe estar en UTC.", nameof(fechaUtc));
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, "UPDATE consentimiento_vision_externa SET fecha_revocado_utc=:fecha " +
                "WHERE telegram_user_id=:id AND fecha_revocado_utc IS NULL");
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(fechaUtc));
            cmd.Agregar("id", OracleDbType.Int64, telegramUserId);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }
    }
}
