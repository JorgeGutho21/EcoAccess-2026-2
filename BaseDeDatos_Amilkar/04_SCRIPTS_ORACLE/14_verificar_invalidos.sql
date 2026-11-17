-- =====================================================================
-- EcoAccess BD — 14 · Verificación final (SOLO LECTURA)
-- Ejecutar como ECOACCESS_BD al terminar la instalación (F5).
-- Resultado esperado: 0 objetos inválidos y conteos de la tabla final.
-- =====================================================================
SET LINESIZE 200
SET PAGESIZE 100
COLUMN object_name FORMAT A34

PROMPT ==== 1. Objetos INVÁLIDOS (debe salir «no se ha seleccionado ninguna fila») ====
SELECT object_type, object_name, status FROM user_objects WHERE status <> 'VALID' ORDER BY 1, 2;

PROMPT ==== 2. Errores de compilación pendientes (debe salir vacío) ====
SELECT name, type, line, position, text FROM user_errors ORDER BY name, sequence;

PROMPT ==== 3. Inventario de objetos ====
SELECT object_type, COUNT(*) AS cantidad FROM user_objects GROUP BY object_type ORDER BY object_type;

PROMPT ==== 4. Restricciones por tipo (P=PK, U=UNIQUE, R=FK, C=CHECK y NOT NULL) ====
SELECT constraint_type, COUNT(*) AS cantidad,
       COUNT(CASE WHEN status = 'ENABLED' THEN 1 END)      AS habilitadas,
       COUNT(CASE WHEN validated = 'VALIDATED' THEN 1 END) AS validadas
FROM   user_constraints
WHERE  constraint_name NOT LIKE 'BIN$%'
GROUP  BY constraint_type ORDER BY constraint_type;

PROMPT ==== 5. Filas por tabla ====
SELECT table_name, TO_NUMBER(EXTRACTVALUE(XMLTYPE(DBMS_XMLGEN.GETXML('SELECT COUNT(*) c FROM ' || table_name)), '/ROWSET/ROW/C')) AS filas
FROM   user_tables ORDER BY table_name;

PROMPT ==== 6. Disparadores ====
SELECT trigger_name, trigger_type, triggering_event, table_name, status FROM user_triggers ORDER BY table_name, trigger_name;
