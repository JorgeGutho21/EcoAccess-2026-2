-- EcoAccess · indexes/uq_ecop_credito_inicial.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE UNIQUE INDEX "UQ_ECOP_CREDITO_INICIAL" ON "MOVIMIENTO_ECOPUNTOS" (CASE  WHEN ("TIPO"=1 AND "ID_PREDECESOR" IS NULL) THEN "ID_DEPOSITO" END )
  ;
