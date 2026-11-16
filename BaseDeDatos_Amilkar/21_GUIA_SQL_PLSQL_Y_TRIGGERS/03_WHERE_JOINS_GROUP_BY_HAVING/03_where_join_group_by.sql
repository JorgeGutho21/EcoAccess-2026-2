-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 03 WHERE, JOIN, GROUP BY y HAVING
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL (ECOACCESS_BD).
SET PAGESIZE 100
SET LINESIZE 180

PROMPT ===== A. WHERE: canecas en servicio (estado 1) =====
SELECT id_contenedor, nombre, categoria
FROM   contenedor
WHERE  estado = 1
ORDER  BY id_contenedor;

PROMPT ===== B. WHERE con AND, IN e IS NULL: depósitos verificados sin peso medido =====
SELECT id_deposito, estado_verificacion, peso_estimado_kg, peso_medido_kg
FROM   deposito
WHERE  estado_verificacion IN (1)
AND    peso_medido_kg IS NULL
ORDER  BY id_deposito
FETCH  FIRST 10 ROWS ONLY;

PROMPT ===== C. INNER JOIN: cada caneca con su estación =====
SELECT p.nombre AS estacion, c.nombre AS caneca, c.categoria
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
ORDER  BY p.nombre, c.categoria;

PROMPT ===== D. JOIN de tres tablas: qué materiales acepta cada caneca =====
SELECT c.nombre AS caneca, m.codigo AS material
FROM   contenedor_material cm
JOIN   contenedor c ON c.id_contenedor = cm.id_contenedor
JOIN   material   m ON m.id_material   = cm.id_material
ORDER  BY c.nombre, m.codigo
FETCH  FIRST 15 ROWS ONLY;

PROMPT ===== E. LEFT JOIN: materiales y cuántos depósitos declarados tienen (incluye los que no tienen) =====
SELECT m.codigo, COUNT(d.id_deposito) AS depositos
FROM   material m
LEFT   JOIN deposito d ON d.id_material_declarado = m.id_material
GROUP  BY m.codigo
ORDER  BY depositos DESC, m.codigo;

PROMPT ===== F. GROUP BY: depósitos por estado =====
SELECT CASE estado_verificacion WHEN 0 THEN 'PENDIENTE' WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END AS estado,
       COUNT(*) AS depositos
FROM   deposito
GROUP  BY estado_verificacion
ORDER  BY estado_verificacion;

PROMPT ===== G. HAVING: materiales con 8 o más depósitos verificados =====
SELECT m.codigo, COUNT(*) AS verificados
FROM   deposito d
JOIN   material m ON m.id_material = d.id_material_verificado
WHERE  d.estado_verificacion = 1
GROUP  BY m.codigo
HAVING COUNT(*) >= 8
ORDER  BY verificados DESC;
