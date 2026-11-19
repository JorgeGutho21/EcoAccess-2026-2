-- EcoAccess · views/ea_r_consultas_vision.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_CONSULTAS_VISION" ("DIA", "CANAL", "MODO", "PROVEEDOR", "CONTEXTO", "RESULTADO", "ADMISION", "MATERIAL", "CONSULTAS", "CIUDADANOS", "LATENCIA_MEDIA_MS", "LATENCIA_MAXIMA_MS") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT CAST(TRUNC(CAST(FROM_TZ(c.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS DATE) AS dia,
       c.canal, c.modo, c.proveedor, c.contexto, c.resultado, c.admision,
       m.codigo AS material,
       COUNT(*) AS consultas,
       COUNT(DISTINCT c.id_usuario) AS ciudadanos,
       ROUND(AVG(c.latencia_ms)) AS latencia_media_ms,
       MAX(c.latencia_ms) AS latencia_maxima_ms
FROM consulta_vision c
LEFT JOIN material m ON m.id_material = c.id_material
GROUP BY CAST(TRUNC(CAST(FROM_TZ(c.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS DATE),
         c.canal, c.modo, c.proveedor, c.contexto, c.resultado, c.admision, m.codigo;
