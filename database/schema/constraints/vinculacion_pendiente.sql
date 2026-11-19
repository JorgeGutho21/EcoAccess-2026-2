-- EcoAccess · constraints/vinculacion_pendiente.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "VINCULACION_PENDIENTE" ADD CONSTRAINT "FK_VINC_PEND_TOKEN" FOREIGN KEY ("HASH_TOKEN")
	  REFERENCES "TOKEN_VINCULACION" ("HASH_TOKEN") ENABLE;
  ALTER TABLE "VINCULACION_PENDIENTE" ADD CONSTRAINT "FK_VINC_PEND_USUARIO" FOREIGN KEY ("ID_USUARIO")
	  REFERENCES "USUARIO" ("ID_USUARIO") ENABLE;
