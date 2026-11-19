-- EcoAccess · comments/vinculo_telegram.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "VINCULO_TELEGRAM"."TELEGRAM_USER_ID" IS 'Id de la cuenta de Telegram (from.id); no prueba por sí solo la identidad de nadie.';
   COMMENT ON COLUMN "VINCULO_TELEGRAM"."FECHA_BAJA_UTC" IS 'Fin del vínculo; NULL mientras está activo.';
   COMMENT ON TABLE "VINCULO_TELEGRAM"  IS 'Cuenta de EcoAccess unida a una cuenta de Telegram; las bajas se conservan (RN-24, ADR-037).';
