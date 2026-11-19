-- EcoAccess · views/ea_r_opiniones.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_OPINIONES" ("DIA", "OPINIONES", "SUMA_ESTRELLAS", "INVITADAS") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT TRUNC(CAST(FROM_TZ(fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       COUNT(*) opiniones,SUM(estrellas) suma_estrellas,
       COUNT(CASE WHEN id_usuario IS NULL THEN 1 END) invitadas
FROM opinion
GROUP BY TRUNC(CAST(FROM_TZ(fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE))
WITH READ ONLY;
