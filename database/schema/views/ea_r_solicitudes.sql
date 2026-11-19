-- EcoAccess · views/ea_r_solicitudes.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_SOLICITUDES" ("DIA", "ID_CONTENEDOR", "CANECA", "ID_PUNTO_INSTALACION", "UBICACION", "ES_ENSAYO", "SOLICITUDES") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT TRUNC(CAST(FROM_TZ(i.fecha_inicio_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor,c.nombre caneca,p.id_punto_instalacion,p.nombre ubicacion,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo,COUNT(*) solicitudes
FROM interaccion i
JOIN contenedor c ON c.id_contenedor=i.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
LEFT JOIN material m ON m.id_material=i.id_material_confirmado
GROUP BY TRUNC(CAST(FROM_TZ(i.fecha_inicio_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;
