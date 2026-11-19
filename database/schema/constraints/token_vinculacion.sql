-- EcoAccess · constraints/token_vinculacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "TOKEN_VINCULACION" ADD CONSTRAINT "FK_TOKEN_USUARIO" FOREIGN KEY ("ID_USUARIO_REGISTRADO")
	  REFERENCES "USUARIO" ("ID_USUARIO") ENABLE;
