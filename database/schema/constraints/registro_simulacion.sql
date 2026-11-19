-- EcoAccess · constraints/registro_simulacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "REGISTRO_SIMULACION" ADD CONSTRAINT "FK_REGISTRO_SIMULACION" FOREIGN KEY ("ID_LOTE")
	  REFERENCES "LOTE_SIMULACION" ("ID_LOTE") ENABLE;
