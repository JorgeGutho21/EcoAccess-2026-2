-- EcoAccess · comments/conversacion_telegram.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "CONVERSACION_TELEGRAM"."ESTADO" IS 'EstadoConversacion: 1 SinCaneca, 2 CanecaIdentificada, 3 EsperandoConfirmacion, 4 OrdenEmitida.';
   COMMENT ON COLUMN "CONVERSACION_TELEGRAM"."VERSION" IS 'Sube en cada escritura; una escritura con versión vieja se rechaza.';
   COMMENT ON TABLE "CONVERSACION_TELEGRAM"  IS 'Estado del diálogo del bot con cada cuenta de Telegram, con versión optimista.';
