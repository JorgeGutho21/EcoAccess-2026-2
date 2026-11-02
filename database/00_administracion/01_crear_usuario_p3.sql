-- =====================================================================
-- EcoAccess P3 — creación del usuario (esquema) de la aplicación
-- Archivo: database/00_administracion/01_crear_usuario_p3.sql
--
-- Qué hace: pide la contraseña sin mostrarla y ejecuta
-- 01_crear_usuario_p3_cuerpo.sql, que crea ECOACCESS_P3 con privilegios mínimos.
--
-- Cómo ejecutarlo:
--   1. Conéctate como SYSTEM al PDB FREEPDB1 (no al contenedor raíz).
--   2. En SQL Developer: abre este archivo desde la carpeta del repositorio y
--      pulsa F5 (Ejecutar script). En SQL*Plus: @01_crear_usuario_p3.sql
--   3. Escribe la contraseña cuando la pida. No uses comillas dobles en ella.
--
-- Por qué hay dos archivos: ACCEPT ... HIDE solo lee desde una terminal o un
-- diálogo, no desde una tubería. Una ejecución automatizada define la variable
-- clave_p3 con DEFINE y llama directamente al cuerpo.
-- =====================================================================

SET ECHO OFF
SET VERIFY OFF

ACCEPT clave_p3 CHAR PROMPT 'Contraseña para ECOACCESS_P3: ' HIDE

@@01_crear_usuario_p3_cuerpo.sql
