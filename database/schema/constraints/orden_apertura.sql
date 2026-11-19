-- EcoAccess · constraints/orden_apertura.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "ORDEN_APERTURA" ADD CONSTRAINT "FK_ORDEN_INTERACCION" FOREIGN KEY ("ID_INTERACCION", "ID_CONTENEDOR")
	  REFERENCES "INTERACCION" ("ID_INTERACCION", "ID_CONTENEDOR") ENABLE;
