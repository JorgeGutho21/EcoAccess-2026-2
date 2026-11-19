-- EcoAccess · views/vw_bi_auditoria.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_BI_AUDITORIA" ("ID_AUDITORIA", "ID_DEPOSITO", "OPERACION", "ESTADO_ANTERIOR", "ESTADO_NUEVO", "CANTIDAD_ANTERIOR", "CANTIDAD_NUEVA", "PESO_MEDIDO_ANTERIOR_KG", "PESO_MEDIDO_NUEVO_KG", "ACTOR", "MOTIVO", "FECHA_COLOMBIA") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT a.id_auditoria, a.id_deposito, a.operacion,
       DECODE(a.estado_anterior, 0, 'PENDIENTE', 1, 'VERIFICADO', 2, 'RECHAZADO') AS estado_anterior,
       DECODE(a.estado_nuevo,    0, 'PENDIENTE', 1, 'VERIFICADO', 2, 'RECHAZADO') AS estado_nuevo,
       a.cantidad_anterior, a.cantidad_nueva, a.peso_medido_anterior_kg, a.peso_medido_nuevo_kg,
       CASE WHEN a.id_actor IS NULL THEN 'SIN ACTOR DE APLICACIÃ“N' ELSE 'U-' || LPAD(a.id_actor, 4, '0') END AS actor,
       a.motivo,
       CAST(FROM_TZ(a.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS fecha_colombia
FROM   auditoria_deposito a
WITH READ ONLY;
