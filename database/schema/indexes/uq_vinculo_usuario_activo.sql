-- EcoAccess · indexes/uq_vinculo_usuario_activo.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE UNIQUE INDEX "UQ_VINCULO_USUARIO_ACTIVO" ON "VINCULO_TELEGRAM" (CASE  WHEN "FECHA_BAJA_UTC" IS NULL THEN "ID_USUARIO" END )
  ;
