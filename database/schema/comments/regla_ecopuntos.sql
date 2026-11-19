-- EcoAccess · comments/regla_ecopuntos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "REGLA_ECOPUNTOS"."POR_UNIDAD" IS '0 tarifa por deposito historica, 1 tarifa por unidad verificada.';
   COMMENT ON TABLE "REGLA_ECOPUNTOS"  IS 'Versiones de tarifa; crear una versión nueva, nunca modificar la que respalda movimientos';
