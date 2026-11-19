-- EcoAccess · views/vw_bi_ciudadanos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_BI_CIUDADANOS" ("CIUDADANO", "FECHA_REGISTRO", "ACTIVO", "ES_SINTETICO", "ORIGEN_DATO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT 'C-' || LPAD(u.id_usuario, 4, '0') AS ciudadano,
       TRUNC(CAST(FROM_TZ(u.fecha_registro_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS fecha_registro,
       u.activo,
       CASE WHEN REGEXP_LIKE(u.correo_electronico, '(@prueba|@ejemplo|@test|\.invalid$|\.test$)', 'i') THEN 1 ELSE 0 END AS es_sintetico,
       CASE WHEN REGEXP_LIKE(u.correo_electronico, '(@prueba|@ejemplo|@test|\.invalid$|\.test$)', 'i') THEN 'DATOS DE ENSAYO' ELSE 'REAL' END AS origen_dato
FROM   usuario u
WHERE  EXISTS (SELECT 1 FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE ur.id_usuario = u.id_usuario AND r.codigo = 1)
WITH READ ONLY;
