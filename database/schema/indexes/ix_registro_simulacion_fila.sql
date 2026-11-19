-- EcoAccess · indexes/ix_registro_simulacion_fila.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE INDEX "IX_REGISTRO_SIMULACION_FILA" ON "REGISTRO_SIMULACION" ("TABLA", "ID_REGISTRO")
  ;
