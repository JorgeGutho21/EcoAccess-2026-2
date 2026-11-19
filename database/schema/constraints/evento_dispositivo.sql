-- EcoAccess · constraints/evento_dispositivo.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "EVENTO_DISPOSITIVO" ADD CONSTRAINT "FK_EVENTO_CONTENEDOR" FOREIGN KEY ("ID_CONTENEDOR")
	  REFERENCES "CONTENEDOR" ("ID_CONTENEDOR") ENABLE;
  ALTER TABLE "EVENTO_DISPOSITIVO" ADD CONSTRAINT "FK_EVENTO_ORDEN" FOREIGN KEY ("ID_ORDEN", "ID_CONTENEDOR")
	  REFERENCES "ORDEN_APERTURA" ("ID_ORDEN", "ID_CONTENEDOR") ENABLE;
