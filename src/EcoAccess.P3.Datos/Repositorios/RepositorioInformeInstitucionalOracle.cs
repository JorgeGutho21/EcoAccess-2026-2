using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed class RepositorioInformeInstitucionalOracle : IRepositorioInformeInstitucional
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioInformeInstitucionalOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        // Las vistas usan día civil de Colombia. El servicio recibe fronteras UTC de esos días.
        private const string Filtro = " es_ensayo=:ensayo AND dia >= :desde AND dia < :hasta AND (:caneca IS NULL OR id_contenedor=:caneca) AND (:ubicacion IS NULL OR id_punto_instalacion=:ubicacion) ";
        // Solo los depósitos conocen su material; puntos y solicitudes de estas vistas no se reparten por material.
        private const string FiltroMaterial = " AND (:codigo IS NULL OR material=:codigo) AND (:categoria IS NULL OR material IN (SELECT codigo FROM material WHERE categoria=:categoria)) ";
        private static void ParametrosMaterial(OracleCommand cmd, FiltroInforme f)
        {
            cmd.Agregar("codigo", OracleDbType.Varchar2, f.CodigoMaterial);
            cmd.Agregar("categoria", OracleDbType.Int32, f.Categoria is { } categoria ? (int)categoria : (int?)null);
        }
        private static void Parametros(OracleCommand cmd, FiltroInforme f)
        {
            cmd.CommandTimeout = 15;
            cmd.Agregar("ensayo", OracleDbType.Int32, f.SoloDemostracion ? 1 : 0);
            cmd.Agregar("desde", OracleDbType.Date, f.DesdeUtc.AddHours(-5));
            cmd.Agregar("hasta", OracleDbType.Date, f.HastaUtc.AddHours(-5));
            cmd.Agregar("caneca", OracleDbType.Int32, f.IdContenedor);
            cmd.Agregar("ubicacion", OracleDbType.Int32, f.IdUbicacion);
        }
        public async Task<IndicadoresInforme> ConsultarAsync(FiltroInforme f, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            string sql = "SELECT d.depositos,d.verificados,u.ciudadanos,p.acreditados,p.netos,v.opiniones,v.promedio,s.solicitudes,d.unidades,p.reversos,d.sin_conteo FROM " +
                "(SELECT NVL(SUM(depositos),0) depositos,NVL(SUM(verificados),0) verificados,NVL(SUM(unidades_verificadas),0) unidades,NVL(SUM(verificados_sin_conteo),0) sin_conteo FROM ea_r_base_depositos WHERE " + Filtro + FiltroMaterial + ") d CROSS JOIN " +
                "(SELECT NVL(SUM(acreditados),0) acreditados,NVL(SUM(netos),0) netos,NVL(SUM(reversos),0) reversos FROM ea_r_base_puntos WHERE " + Filtro + ") p CROSS JOIN " +
                "(SELECT NVL(SUM(solicitudes),0) solicitudes FROM ea_r_solicitudes WHERE " + Filtro + ") s CROSS JOIN " +
                "(SELECT NVL(SUM(opiniones),0) opiniones,SUM(suma_estrellas)/NULLIF(SUM(opiniones),0) promedio FROM ea_r_opiniones WHERE dia>=:desde AND dia<:hasta) v CROSS JOIN " +
                "(SELECT COUNT(DISTINCT i.id_usuario) ciudadanos FROM deposito dp JOIN orden_apertura o ON o.id_orden=dp.id_orden " +
                "JOIN interaccion i ON i.id_interaccion=o.id_interaccion JOIN material m ON m.id_material=dp.id_material_declarado " +
                "JOIN contenedor c ON c.id_contenedor=o.id_contenedor JOIN punto_instalacion pi ON pi.id_punto_instalacion=c.id_punto_instalacion " +
                "WHERE dp.fecha_deteccion_utc>=:desde_utc AND dp.fecha_deteccion_utc<:hasta_utc " +
                "AND (:caneca IS NULL OR c.id_contenedor=:caneca) AND (:ubicacion IS NULL OR pi.id_punto_instalacion=:ubicacion) " +
                "AND (:codigo IS NULL OR m.codigo=:codigo) AND (:categoria IS NULL OR m.categoria=:categoria) " +
                "AND CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(pi.nombre) LIKE '%SIMULACI%' " +
                "OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END=:ensayo) u";
            using var cmd = ComandosOracle.Crear(c, sql);
            Parametros(cmd, f);
            cmd.Agregar("desde_utc", OracleDbType.TimeStamp, f.DesdeUtc);
            cmd.Agregar("hasta_utc", OracleDbType.TimeStamp, f.HastaUtc);
            ParametrosMaterial(cmd, f);
            using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) throw new InvalidDataException("Sin indicadores de informe.");
            return new(r.GetInt64(0), r.GetInt64(1), r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetInt64(5), r.IsDBNull(6) ? null : r.GetDecimal(6), r.GetInt64(7), r.GetInt64(8), r.GetInt64(9), r.GetInt64(10));
        }
        public async Task<IReadOnlyList<FilaAgrupada>> AgruparAsync(FiltroInforme f, AgrupacionDepositos agrupacion, CancellationToken ct = default)
        {
            string columna = agrupacion switch
            {
                AgrupacionDepositos.Dia => "dia",
                AgrupacionDepositos.Material => "material",
                AgrupacionDepositos.Contenedor => "caneca",
                AgrupacionDepositos.Punto => "ubicacion",
                _ => throw new ArgumentOutOfRangeException(nameof(agrupacion))
            };
            string clave = agrupacion switch
            {
                AgrupacionDepositos.Contenedor => "id_contenedor",
                AgrupacionDepositos.Punto => "id_punto_instalacion",
                _ => "NULL"
            };
            // Fragmentos de una lista cerrada; ningún valor del usuario se concatena.
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = ComandosOracle.Crear(c, $"SELECT {columna},{clave},SUM(depositos),SUM(verificados),SUM(con_peso_medido),NVL(SUM(kg_medidos),0) FROM ea_r_base_depositos WHERE {Filtro}{FiltroMaterial} GROUP BY {columna},{clave} ORDER BY {columna},{clave}");
            Parametros(cmd, f);
            ParametrosMaterial(cmd, f);
            using DbDataReader r = await cmd.ExecuteReaderAsync(ct);
            var filas = new List<FilaAgrupada>();
            while (await r.ReadAsync(ct))
            {
                DateOnly? dia = agrupacion == AgrupacionDepositos.Dia ? DateOnly.FromDateTime(r.GetDateTime(0)) : null;
                filas.Add(new(r.IsDBNull(1) ? null : r.GetInt32(1), dia, dia?.ToString("yyyy-MM-dd") ?? r.GetString(0), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4), r.GetDecimal(5), 0, 0));
            }
            return filas;
        }
    }
}
