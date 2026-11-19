-- EcoAccess · comments/vinculacion_pendiente.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "VINCULACION_PENDIENTE"."HASH_CODIGO" IS 'SHA-256 de hash_token:código; el código en claro no se guarda';
   COMMENT ON TABLE "VINCULACION_PENDIENTE"  IS 'Vinculación de cuenta existente en espera del código escrito en Telegram (P-15)';
