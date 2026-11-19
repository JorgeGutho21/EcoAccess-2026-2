-- EcoAccess · comments/configuracion_caneca.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "CONFIGURACION_CANECA"."CATEGORIA" IS '1 blanca aprovechable, 2 verde orgánico, 3 negra no aprovechable; mismos valores que CategoriaMaterial.';
   COMMENT ON COLUMN "CONFIGURACION_CANECA"."IDENTIFICADOR_DISPOSITIVO" IS 'ESP32 vinculado a esta caneca; coincide con DISPOSITIVO del firmware y del bridge.';
   COMMENT ON TABLE "CONFIGURACION_CANECA"  IS 'Color (por categoría), dispositivo y ubicación de cada caneca de una estación de tres canecas.';
