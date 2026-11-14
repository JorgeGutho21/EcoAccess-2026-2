-- =====================================================================
-- EcoAccess BD — 18 · Inventario de objetos para comparar DDL vs scripts
-- Conexión: ECOACCESS_BD. Solo lectura. Imprime una línea por objeto:
--   OBJ|tipo|nombre|estado   COL|tabla|columna   CON|tipo|nombre|tabla
-- INSTALAR_Y_PROBAR.ps1 compara estas líneas con los nombres que aparecen
-- en los scripts 02–08 y con el diccionario de datos (173 columnas).
-- =====================================================================
SET PAGESIZE 0 LINESIZE 400 FEEDBACK OFF HEADING OFF TRIMSPOOL ON
SELECT 'OBJ|' || object_type || '|' || object_name || '|' || status
FROM   user_objects
WHERE  object_type IN ('TABLE', 'VIEW', 'INDEX', 'PACKAGE', 'PACKAGE BODY', 'FUNCTION', 'PROCEDURE', 'TRIGGER')
ORDER  BY object_type, object_name;
SELECT 'COL|' || c.table_name || '|' || c.column_name
FROM   user_tab_columns c JOIN user_tables t ON t.table_name = c.table_name
ORDER  BY c.table_name, c.column_id;
SELECT 'CON|' || constraint_type || '|' || constraint_name || '|' || table_name
FROM   user_constraints
WHERE  constraint_name NOT LIKE 'SYS\_%' ESCAPE '\' AND constraint_name NOT LIKE 'BIN$%'
ORDER  BY constraint_type, constraint_name;
SELECT 'CMT|' || COUNT(*) FROM user_col_comments c JOIN user_tables t ON t.table_name = c.table_name WHERE c.comments IS NOT NULL;
SET PAGESIZE 50 FEEDBACK ON HEADING ON
