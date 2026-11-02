using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed class RepositorioOpinionesOracle : IRepositorioOpiniones
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioOpinionesOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<Opinion?> ObtenerAsync(string participante, DateTime diaUtc, CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var comando = ComandosOracle.Crear(conexion,
                "SELECT id_usuario, participante, estrellas, comentario, fecha_utc FROM opinion WHERE participante=:participante AND dia_utc=:dia");
            comando.Agregar("participante", OracleDbType.Varchar2, participante);
            comando.Agregar("dia", OracleDbType.Date, diaUtc.Date);
            using DbDataReader lector = await comando.ExecuteReaderAsync(ct);
            return await lector.ReadAsync(ct) ? new Opinion(lector.IsDBNull(0) ? null : ConversionOracle.Entero(lector.GetValue(0)),
                lector.GetString(1), ConversionOracle.Entero(lector.GetValue(2)), ConversionOracle.TextoOpcional(lector.GetValue(3)),
                ConversionOracle.FechaUtc(lector.GetValue(4))) : null;
        }

        public async Task AgregarAsync(Opinion opinion, CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var comando = ComandosOracle.Crear(conexion,
                "INSERT INTO opinion(id_usuario,participante,estrellas,comentario,fecha_utc) VALUES(:usuario,:participante,:estrellas,:comentario,:fecha)");
            comando.Agregar("usuario", OracleDbType.Int32, opinion.IdUsuario);
            comando.Agregar("participante", OracleDbType.Varchar2, opinion.Participante);
            comando.Agregar("estrellas", OracleDbType.Int32, opinion.Estrellas);
            comando.Agregar("comentario", OracleDbType.Varchar2, opinion.Comentario);
            comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(opinion.FechaUtc));
            try { await comando.ExecuteNonQueryAsync(ct); }
            catch (OracleException error) when (error.Number == 1) { throw new ConflictoUnicidadException("Ya existe la valoración diaria.", error); }
        }

        public async Task<IReadOnlyList<TendenciaOpiniones>> ResumirAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var comando = ComandosOracle.Crear(conexion,
                "SELECT dia_utc,COUNT(*),ROUND(AVG(estrellas),2),SUM(CASE WHEN id_usuario IS NULL THEN 1 ELSE 0 END) FROM opinion " +
                "WHERE fecha_utc>=:desde AND fecha_utc<:hasta GROUP BY dia_utc ORDER BY dia_utc");
            comando.Agregar("desde", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(desdeUtc));
            comando.Agregar("hasta", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(hastaUtc));
            var filas = new List<TendenciaOpiniones>();
            using DbDataReader lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct)) filas.Add(new(ConversionOracle.FechaUtc(lector.GetValue(0)),
                ConversionOracle.Entero(lector.GetValue(1)), Convert.ToDecimal(lector.GetValue(2), System.Globalization.CultureInfo.InvariantCulture),
                ConversionOracle.Entero(lector.GetValue(3))));
            return filas;
        }
    }
}
