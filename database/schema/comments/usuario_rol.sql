-- EcoAccess · comments/usuario_rol.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "USUARIO_ROL"."FECHA_ASIGNACION_UTC" IS 'Metadato técnico; la evidencia de quién asignó el rol va en auditoría (ADR-027)';
   COMMENT ON TABLE "USUARIO_ROL"  IS 'Roles de cada usuario; una cuenta puede tener varios';
