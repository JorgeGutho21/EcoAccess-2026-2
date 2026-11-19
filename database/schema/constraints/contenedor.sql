-- EcoAccess · constraints/contenedor.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "CONTENEDOR" ADD CONSTRAINT "FK_CONTENEDOR_PUNTO" FOREIGN KEY ("ID_PUNTO_INSTALACION")
	  REFERENCES "PUNTO_INSTALACION" ("ID_PUNTO_INSTALACION") ENABLE;
