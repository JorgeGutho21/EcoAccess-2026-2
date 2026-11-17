-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 11 Secuencias y claves primarias
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL. Ejecuta con F5.
-- No se llama NEXTVAL aquí: cada NEXTVAL consume un número que no se devuelve.
SET PAGESIZE 100
SET LINESIZE 200
COLUMN table_name      FORMAT A28
COLUMN column_name     FORMAT A22
COLUMN constraint_name FORMAT A32
COLUMN sequence_name   FORMAT A20

PROMPT ===== A. Clave primaria de cada tabla (19 tablas) =====
SELECT c.table_name, c.constraint_name, cc.column_name
FROM   user_constraints c
JOIN   user_cons_columns cc ON cc.constraint_name = c.constraint_name
WHERE  c.constraint_type = 'P'
ORDER  BY c.table_name, cc.position;

PROMPT ===== B. Columnas IDENTITY: ALWAYS (nadie escribe el id) o BY DEFAULT ON NULL (catálogos) =====
SELECT table_name, column_name, generation_type, sequence_name
FROM   user_tab_identity_cols
ORDER  BY generation_type, table_name;

PROMPT ===== C. Las secuencias internas que Oracle creó para cada IDENTITY =====
SELECT sequence_name, min_value, increment_by, cache_size, last_number
FROM   user_sequences
ORDER  BY sequence_name;

PROMPT ===== D. Tablas cuya PK no es IDENTITY (claves compuestas o naturales) =====
SELECT c.table_name, LISTAGG(cc.column_name, ', ') WITHIN GROUP (ORDER BY cc.position) AS columnas_pk
FROM   user_constraints c
JOIN   user_cons_columns cc ON cc.constraint_name = c.constraint_name
WHERE  c.constraint_type = 'P'
AND    c.table_name NOT IN (SELECT table_name FROM user_tab_identity_cols)
GROUP  BY c.table_name
ORDER  BY c.table_name;

PROMPT ===== E. Huecos en los id: normales (ROLLBACK o caché de la secuencia) =====
SELECT MIN(id_deposito) AS menor, MAX(id_deposito) AS mayor, COUNT(*) AS filas,
       MAX(id_deposito) - MIN(id_deposito) + 1 - COUNT(*) AS huecos
FROM   deposito;
