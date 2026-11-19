-- EcoAccess · constraints/peso_referencial.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "PESO_REFERENCIAL" ADD CONSTRAINT "FK_PESO_MATERIAL" FOREIGN KEY ("ID_MATERIAL")
	  REFERENCES "MATERIAL" ("ID_MATERIAL") ENABLE;
