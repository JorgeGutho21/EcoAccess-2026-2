-- EcoAccess · comments/contenedor.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "CONTENEDOR"."IDENTIFICADOR_QR" IS 'Parámetro start del enlace de Telegram; público, no autoriza nada';
   COMMENT ON COLUMN "CONTENEDOR"."ESTADO" IS '1 EnServicio, 2 FueraDeServicio, 3 DadoDeBaja';
   COMMENT ON TABLE "CONTENEDOR"  IS 'Caneca física de un compartimiento y su QR público';
