-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 01 Conexiones y primeros comandos
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL (ECOACCESS_BD). Ejecuta con F5.
SET PAGESIZE 100
SET LINESIZE 160

PROMPT ===== 1. Con qué usuario estoy conectado =====
SELECT USER AS usuario_conectado FROM dual;

PROMPT ===== 2. En qué base de datos (contenedor) estoy =====
SELECT SYS_CONTEXT('USERENV', 'CON_NAME') AS base_de_datos FROM dual;

PROMPT ===== 3. Fecha y hora del servidor =====
SELECT TO_CHAR(SYSDATE, 'YYYY-MM-DD HH24:MI:SS') AS fecha_servidor FROM dual;

PROMPT ===== 4. Cuántas tablas tiene mi usuario (deben ser 19) =====
SELECT COUNT(*) AS tablas FROM user_tables;

PROMPT ===== 5. Objetos por tipo =====
SELECT object_type, COUNT(*) AS cantidad
FROM   user_objects
GROUP  BY object_type
ORDER  BY object_type;

PROMPT ===== 6. Objetos inválidos (no debe salir ninguno) =====
SELECT object_name, object_type, status
FROM   user_objects
WHERE  status <> 'VALID';
