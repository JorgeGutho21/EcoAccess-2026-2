-- EcoAccess · constraints/comentario_ciudadano.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "COMENTARIO_CIUDADANO" ADD CONSTRAINT "FK_COMENTARIO_USUARIO" FOREIGN KEY ("ID_USUARIO")
	  REFERENCES "USUARIO" ("ID_USUARIO") ENABLE;
  ALTER TABLE "COMENTARIO_CIUDADANO" ADD CONSTRAINT "FK_COMENTARIO_INTERACCION" FOREIGN KEY ("ID_INTERACCION")
	  REFERENCES "INTERACCION" ("ID_INTERACCION") ENABLE;
