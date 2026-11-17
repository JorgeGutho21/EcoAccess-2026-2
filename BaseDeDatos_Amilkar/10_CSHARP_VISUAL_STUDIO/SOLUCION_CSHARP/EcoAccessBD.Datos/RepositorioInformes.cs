using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccessBD.Datos;

/// <summary>
/// Lectura de las vistas VW_BI_* con la cuenta de SOLO LECTURA (ECOACCESS_BD_LECTOR).
/// Todas las consultas usan parámetros (:dias) — nunca concatenar texto del usuario en SQL.
/// </summary>
public sealed class RepositorioInformes(string cadena)
{
    /// <summary>Ranking de ciudadanos (seudónimo) por depósitos VERIFICADOS en los últimos N días.</summary>
    public DataTable RankingVerificados(int dias)
    {
        const string sql = """
            SELECT ciudadano, COUNT(*) AS depositos, SUM(unidades) AS unidades,
                   ROUND(SUM(peso_estimado_kg), 3) AS kg_estimados, ROUND(SUM(peso_medido_kg), 3) AS kg_medidos
            FROM   ecoaccess_bd.vw_bi_depositos
            WHERE  estado = 'VERIFICADO'
            AND    fecha_hora >= CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE) - :dias
            GROUP  BY ciudadano
            ORDER  BY depositos DESC, unidades DESC
            FETCH FIRST 10 ROWS ONLY
            """;
        return Consultar(sql, new OracleParameter("dias", OracleDbType.Int32) { Value = dias });
    }

    /// <summary>Embudo: fotos consultadas vs depósitos por estado (nunca se mezclan).</summary>
    public DataTable ResumenPorEstado()
    {
        const string sql = """
            SELECT 'FOTOS CONSULTADAS' AS indicador, COUNT(*) AS cantidad FROM ecoaccess_bd.vw_bi_consultas_vision
            UNION ALL
            SELECT 'DEPÓSITOS ' || estado, COUNT(*) FROM ecoaccess_bd.vw_bi_depositos GROUP BY estado
            """;
        return Consultar(sql);
    }

    private DataTable Consultar(string sql, params OracleParameter[] parametros)
    {
        using var con = new OracleConnection(cadena);
        using var cmd = new OracleCommand(sql, con) { BindByName = true };
        cmd.Parameters.AddRange(parametros);
        var tabla = new DataTable();
        using var adaptador = new OracleDataAdapter(cmd);
        con.Open();
        adaptador.Fill(tabla);
        return tabla;
    }
}
