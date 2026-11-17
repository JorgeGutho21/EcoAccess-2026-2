-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 10 Tarea de los 4 disparadores
-- SOLO LECTURA. Conexión: ECOACCESS_PRACTICAS (NO EcoAccess BD). Ejecuta con F5.
-- Comprueba que las prácticas P01, P02 y P03 ya están creadas en el esquema de prácticas.
-- Para crearlas o probarlas, usa los archivos originales de 05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS.
SET PAGESIZE 100
SET LINESIZE 200
COLUMN trigger_name     FORMAT A28
COLUMN triggering_event FORMAT A18
COLUMN table_name       FORMAT A22
COLUMN esquema_actual   FORMAT A22
COLUMN pdb              FORMAT A12
COLUMN sequence_name    FORMAT A26

PROMPT ===== A. ¿Estoy en el esquema correcto? (debe decir ECOACCESS_PRACTICAS) =====
SELECT USER AS esquema_actual, SYS_CONTEXT('USERENV', 'CON_NAME') AS pdb FROM dual;

PROMPT ===== B. Disparadores de las prácticas =====
SELECT trigger_name, trigger_type, triggering_event, table_name, status
FROM   user_triggers
ORDER  BY table_name, trigger_name;

PROMPT ===== C. Secuencia del punto 2 (clave subrogada) =====
SELECT sequence_name, min_value, increment_by, last_number
FROM   user_sequences
ORDER  BY sequence_name;

PROMPT ===== D. Punto 4: el precio final ya calculado por el disparador =====
SELECT * FROM producto_practica ORDER BY 1 FETCH FIRST 5 ROWS ONLY;
