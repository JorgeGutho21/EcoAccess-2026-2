-- EcoAccess · comments/update_telegram.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "UPDATE_TELEGRAM"."RECIBIDO_UTC" IS 'Hora UTC del servidor; permite purgar los antiguos.';
   COMMENT ON TABLE "UPDATE_TELEGRAM"  IS 'Updates de Telegram ya procesados; evita atender dos veces un reintento.';
