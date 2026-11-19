-- EcoAccess · comments/token_vinculacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "TOKEN_VINCULACION"."HASH_TOKEN" IS 'SHA-256 del token en hexadecimal minúsculo.';
   COMMENT ON COLUMN "TOKEN_VINCULACION"."FECHA_USO_UTC" IS 'Momento en que se consumió; NULL si sigue sin usar.';
   COMMENT ON COLUMN "TOKEN_VINCULACION"."ID_USUARIO_REGISTRADO" IS 'Cuenta creada con este token (trazabilidad).';
   COMMENT ON TABLE "TOKEN_VINCULACION"  IS 'Token de un solo uso del enlace de registro; solo se guarda su hash (ADR-037).';
