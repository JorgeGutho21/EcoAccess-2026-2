-- EcoAccess · constraints/configuracion_caneca.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "CONFIGURACION_CANECA" ADD CONSTRAINT "FK_CONFIGURACION_CONTENEDOR" FOREIGN KEY ("ID_CONTENEDOR")
	  REFERENCES "CONTENEDOR" ("ID_CONTENEDOR") ENABLE;
