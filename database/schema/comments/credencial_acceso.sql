-- EcoAccess · comments/credencial_acceso.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "CREDENCIAL_ACCESO"."HASH_CONTRASENA" IS 'PBKDF2-HMAC-SHA256, 32 bytes.';
   COMMENT ON COLUMN "CREDENCIAL_ACCESO"."SAL" IS 'Sal aleatoria propia de esta contraseña, 16 bytes.';
   COMMENT ON COLUMN "CREDENCIAL_ACCESO"."ITERACIONES" IS 'Iteraciones con que se calculó el hash; permite subirlas sin invalidar las anteriores.';
   COMMENT ON COLUMN "CREDENCIAL_ACCESO"."INTENTOS_FALLIDOS" IS 'Fallos seguidos desde el último acierto o bloqueo.';
   COMMENT ON COLUMN "CREDENCIAL_ACCESO"."BLOQUEADA_HASTA_UTC" IS 'Hora UTC hasta la que no se admite inicio de sesión; NULL si no está bloqueada.';
   COMMENT ON COLUMN "CREDENCIAL_ACCESO"."FECHA_CAMBIO_UTC" IS 'Hora UTC del último cambio de contraseña.';
   COMMENT ON TABLE "CREDENCIAL_ACCESO"  IS 'Contraseña de inicio de sesión de la GUI: hash PBKDF2-SHA256 con sal (ADR-034). Nunca la contraseña.';
