using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{
    /// <summary>Tabla CONSULTA_VISION (migración V023). Solo metadatos: ni la foto ni el identificador de Telegram.</summary>
    public sealed class RepositorioConsultasVisionOracle : IRepositorioConsultasVision
    {
        private const int ClaveDuplicada = 1;
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioConsultasVisionOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        /// <summary>Si la base conectada ya tiene la migración V023.</summary>
        public async Task<bool> TablaDisponibleAsync(CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion, "SELECT COUNT(*) FROM user_tables WHERE table_name='CONSULTA_VISION'");
            return ConversionOracle.Entero(await cmd.ExecuteScalarAsync(ct) ?? 0) == 1;
        }

        public async Task GuardarAsync(ConsultaVisionRegistrada c, CancellationToken ct = default)
        {
            Compat.ThrowIfNull(c, nameof(c));
            if (c.FechaUtc.Kind != DateTimeKind.Utc || c.LatenciaMs < 0 || Compat.IsNullOrWhiteSpace(c.Clave) || c.Clave.Length > 80)
                throw new ArgumentException("Consulta de visión inválida.");
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion, "INSERT INTO consulta_vision " +
                "(clave,id_usuario,canal,modo,proveedor,clave_objeto,contexto,resultado,id_material,admision,id_contenedor,latencia_ms,fecha_utc) " +
                "VALUES (:clave,:usuario,:canal,:modo,:proveedor,:objeto,:contexto,:resultado,:material,:admision,:contenedor,:latencia,:fecha)");
            cmd.Agregar("clave", OracleDbType.Varchar2, c.Clave);
            cmd.Agregar("usuario", OracleDbType.Int32, c.IdUsuario);
            cmd.Agregar("canal", OracleDbType.Int32, (int)c.Canal);
            cmd.Agregar("modo", OracleDbType.Int32, (int)c.Modo);
            cmd.Agregar("proveedor", OracleDbType.Varchar2, c.Proveedor.Length > 80 ? c.Proveedor.Substring(0, 80) : c.Proveedor);
            cmd.Agregar("objeto", OracleDbType.Varchar2, c.ClaveObjeto);
            cmd.Agregar("contexto", OracleDbType.Int32, c.Contexto is { } contexto ? (int)contexto : null);
            cmd.Agregar("resultado", OracleDbType.Int32, (int)c.Resultado);
            cmd.Agregar("material", OracleDbType.Int32, c.IdMaterial);
            cmd.Agregar("admision", OracleDbType.Int32, c.Admision is { } admision ? (int)admision : null);
            cmd.Agregar("contenedor", OracleDbType.Int32, c.IdContenedor);
            cmd.Agregar("latencia", OracleDbType.Int32, Math.Min(c.LatenciaMs, 9_999_999));
            cmd.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(c.FechaUtc));
            try { await cmd.ExecuteNonQueryAsync(ct); }
            // La misma actualización de Telegram reintentada no cuenta dos consultas.
            catch (OracleException e) when (e.Number == ClaveDuplicada) { }
        }

        public async Task<ResumenConsultasVision> ResumirDeUsuarioAsync(int idUsuario, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
        {
            if (idUsuario <= 0 || desdeUtc.Kind != DateTimeKind.Utc || hastaUtc.Kind != DateTimeKind.Utc || hastaUtc < desdeUtc)
                throw new ArgumentException("Período inválido.");
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(conexion, "SELECT COUNT(*)," +
                $"NVL(SUM(CASE WHEN resultado IN ({(int)ResultadoClasificacion.Identificado},{(int)ResultadoClasificacion.ObjetoReconocido}) THEN 1 ELSE 0 END),0)," +
                $"NVL(SUM(CASE WHEN resultado IN ({(int)ResultadoClasificacion.Incierta},{(int)ResultadoClasificacion.ImagenBorrosa},{(int)ResultadoClasificacion.VariosObjetos}) THEN 1 ELSE 0 END),0) " +
                "FROM consulta_vision WHERE id_usuario=:usuario AND fecha_utc>=:desde AND fecha_utc<:hasta");
            cmd.Agregar("usuario", OracleDbType.Int32, idUsuario);
            cmd.Agregar("desde", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(desdeUtc));
            cmd.Agregar("hasta", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(hastaUtc));
            using var r = await cmd.ExecuteReaderAsync(ct);
            await r.ReadAsync(ct);
            return new ResumenConsultasVision(ConversionOracle.Entero(r.GetValue(0)), ConversionOracle.Entero(r.GetValue(1)), ConversionOracle.Entero(r.GetValue(2)));
        }
    }
}
