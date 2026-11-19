-- EcoAccess · constraints/regla_ecopuntos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  ALTER TABLE "REGLA_ECOPUNTOS" ADD FOREIGN KEY ("ID_MATERIAL")
	  REFERENCES "MATERIAL" ("ID_MATERIAL") ENABLE;
  ALTER TABLE "REGLA_ECOPUNTOS" ADD FOREIGN KEY ("ID_CREADOR")
	  REFERENCES "USUARIO" ("ID_USUARIO") ENABLE;
