-- =====================================================================
-- EcoAccess BD — 17 · Extraer el DDL FINAL tal como quedó instalado
-- Conexión: ECOACCESS_BD. Solo lectura (DBMS_METADATA no cambia nada).
-- Ejecutar desde la carpeta raíz del proyecto (lo hace INSTALAR_Y_PROBAR.ps1):
-- escribe 04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql con TODAS las tablas (con sus
-- restricciones), índices, vistas, paquetes, funciones, procedimientos,
-- disparadores y comentarios. Ese archivo es la «foto» de lo que Oracle
-- tiene de verdad, para compararla con los scripts 02–09 y 15.
-- =====================================================================
SET LONG 2000000 LONGCHUNKSIZE 2000000 PAGESIZE 0 LINESIZE 32767
SET TRIMSPOOL ON FEEDBACK OFF HEADING OFF ECHO OFF VERIFY OFF
BEGIN
    DBMS_METADATA.SET_TRANSFORM_PARAM(DBMS_METADATA.SESSION_TRANSFORM, 'SQLTERMINATOR', TRUE);
    DBMS_METADATA.SET_TRANSFORM_PARAM(DBMS_METADATA.SESSION_TRANSFORM, 'PRETTY', TRUE);
    DBMS_METADATA.SET_TRANSFORM_PARAM(DBMS_METADATA.SESSION_TRANSFORM, 'SEGMENT_ATTRIBUTES', FALSE);
    DBMS_METADATA.SET_TRANSFORM_PARAM(DBMS_METADATA.SESSION_TRANSFORM, 'STORAGE', FALSE);
    DBMS_METADATA.SET_TRANSFORM_PARAM(DBMS_METADATA.SESSION_TRANSFORM, 'EMIT_SCHEMA', FALSE);
END;
/
SET TERMOUT OFF
SPOOL DDL_FINAL_EXTRAIDO.sql
SELECT '-- DDL FINAL de ECOACCESS_BD extraído con DBMS_METADATA el '
       || TO_CHAR(CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE), 'YYYY-MM-DD HH24:MI') || ' (hora Colombia)'
       || CHR(10) || '-- Base: ' || SYS_CONTEXT('USERENV', 'CON_NAME') || ' · Es una evidencia: para instalar usa los scripts 01–15.'
FROM dual;
SELECT CHR(10) || '-- ================= TABLAS (con PK, FK, UNIQUE y CHECK) =================' FROM dual;
SELECT DBMS_METADATA.GET_DDL('TABLE', table_name) FROM user_tables ORDER BY table_name;
SELECT CHR(10) || '-- ================= ÍNDICES propios (no los que crean PK/UNIQUE) =================' FROM dual;
SELECT DBMS_METADATA.GET_DDL('INDEX', i.index_name) FROM user_indexes i
WHERE  NOT EXISTS (SELECT 1 FROM user_constraints c WHERE c.index_name = i.index_name)
ORDER  BY i.table_name, i.index_name;
SELECT CHR(10) || '-- ================= VISTAS =================' FROM dual;
SELECT DBMS_METADATA.GET_DDL('VIEW', view_name) FROM user_views ORDER BY view_name;
SELECT CHR(10) || '-- ================= FUNCIONES Y PROCEDIMIENTOS =================' FROM dual;
SELECT DBMS_METADATA.GET_DDL(object_type, object_name) FROM user_objects WHERE object_type IN ('FUNCTION', 'PROCEDURE') ORDER BY object_type, object_name;
SELECT CHR(10) || '-- ================= PAQUETES (especificación y cuerpo) =================' FROM dual;
SELECT DBMS_METADATA.GET_DDL('PACKAGE', object_name) FROM user_objects WHERE object_type = 'PACKAGE' ORDER BY object_name;
SELECT CHR(10) || '-- ================= DISPARADORES =================' FROM dual;
SELECT DBMS_METADATA.GET_DDL('TRIGGER', trigger_name) FROM user_triggers ORDER BY trigger_name;
SELECT CHR(10) || '-- ================= COMENTARIOS (diccionario dentro de Oracle) =================' FROM dual;
SELECT DBMS_METADATA.GET_DEPENDENT_DDL('COMMENT', t.table_name) FROM user_tables t
WHERE  EXISTS (SELECT 1 FROM user_col_comments c WHERE c.table_name = t.table_name AND c.comments IS NOT NULL)
ORDER  BY t.table_name;
SPOOL OFF
SET TERMOUT ON PAGESIZE 50 HEADING ON FEEDBACK ON
PROMPT DDL final escrito en DDL_FINAL_EXTRAIDO.sql (carpeta actual de SQL*Plus)
