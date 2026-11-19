-- EcoAccess · indexes/uq_orden_vigente.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE UNIQUE INDEX "UQ_ORDEN_VIGENTE" ON "ORDEN_APERTURA" (CASE  WHEN ("ESTADO">=1 AND "ESTADO"<=5) THEN "ID_CONTENEDOR" END )
  ;
