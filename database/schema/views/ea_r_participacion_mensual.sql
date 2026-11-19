-- EcoAccess · views/ea_r_participacion_mensual.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_PARTICIPACION_MENSUAL" ("MES", "ES_ENSAYO", "CIUDADANOS_UNICOS", "DEPOSITOS") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE),'MM') mes,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo,
       COUNT(DISTINCT i.id_usuario) ciudadanos_unicos,
       COUNT(*) depositos
FROM deposito d
JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
JOIN material m ON m.id_material=d.id_material_declarado
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE),'MM'),
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;
