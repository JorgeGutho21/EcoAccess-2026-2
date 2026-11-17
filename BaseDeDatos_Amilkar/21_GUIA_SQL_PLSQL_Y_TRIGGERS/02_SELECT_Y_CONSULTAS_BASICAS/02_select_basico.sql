-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 02 SELECT y consultas básicas
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL (ECOACCESS_BD). Ctrl+Enter en cada consulta, o F5 para todo.
SET PAGESIZE 100
SET LINESIZE 180

PROMPT ===== A. Ejemplo básico: todas las columnas de un catálogo pequeño =====
SELECT * FROM rol;

PROMPT ===== B. Columnas elegidas, alias y orden =====
SELECT codigo      AS codigo_material,
       nombre      AS material,
       categoria   AS color_caneca
FROM   material
ORDER  BY categoria, nombre;

PROMPT ===== C. Expresión CASE: traducir el número de la categoría a palabras =====
SELECT codigo,
       nombre,
       CASE categoria
           WHEN 1 THEN 'BLANCA (aprovechables)'
           WHEN 2 THEN 'VERDE (orgánicos)'
           WHEN 3 THEN 'NEGRA (no aprovechables)'
           ELSE        'RUTA ESPECIAL (peligrosos)'
       END AS caneca
FROM   material
ORDER  BY codigo;

PROMPT ===== D. Valores distintos: qué estados de verificación existen =====
SELECT DISTINCT estado_verificacion FROM deposito ORDER BY 1;

PROMPT ===== E. Las primeras 5 canecas =====
SELECT id_contenedor, nombre, categoria, estado
FROM   contenedor
ORDER  BY id_contenedor
FETCH  FIRST 5 ROWS ONLY;
