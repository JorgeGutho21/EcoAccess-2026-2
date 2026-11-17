-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 14 Ejercicios progresivos — SOLUCIONES
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL. Intenta cada ejercicio antes de mirar su solución.
SET SERVEROUTPUT ON
SET PAGESIZE 100
SET LINESIZE 200

PROMPT ===== E01 (SELECT) Códigos y categorías de todos los materiales, ordenados por código =====
SELECT codigo, categoria FROM material ORDER BY codigo;

PROMPT ===== E02 (WHERE) Canecas en servicio (estado 1) =====
SELECT id_contenedor, nombre FROM contenedor WHERE estado = 1 ORDER BY id_contenedor;

PROMPT ===== E03 (JOIN) Cada caneca con el nombre de su estación =====
SELECT c.nombre AS caneca, p.nombre AS estacion
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
ORDER  BY p.nombre, c.nombre;

PROMPT ===== E04 (GROUP BY) Cuántos depósitos hay en cada estado de verificación =====
SELECT CASE estado_verificacion WHEN 0 THEN 'PENDIENTE' WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END AS estado,
       COUNT(*) AS depositos
FROM   deposito
GROUP  BY estado_verificacion
ORDER  BY estado_verificacion;

PROMPT ===== E05 (HAVING) Materiales declarados en más de 5 depósitos =====
SELECT m.codigo, COUNT(*) AS veces
FROM   deposito d
JOIN   material m ON m.id_material = d.id_material_declarado
GROUP  BY m.codigo
HAVING COUNT(*) > 5
ORDER  BY veces DESC;

PROMPT ===== E06 (cadena de FK) Depósitos por ciudadano: DEPOSITO → ORDEN → INTERACCION → USUARIO =====
SELECT 'C-' || LPAD(i.id_usuario, 4, '0') AS ciudadano, COUNT(*) AS depositos
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY depositos DESC, i.id_usuario
FETCH  FIRST 5 ROWS ONLY;

PROMPT ===== E07 (subconsulta) Canecas que nunca recibieron un depósito =====
SELECT c.id_contenedor, c.nombre
FROM   contenedor c
WHERE  NOT EXISTS (SELECT 1 FROM orden_apertura o
                   JOIN deposito d ON d.id_orden = o.id_orden
                   WHERE o.id_contenedor = c.id_contenedor)
ORDER  BY c.id_contenedor;

PROMPT ===== E08 (bloque anónimo) Mostrar con DBMS_OUTPUT el total de depósitos verificados =====
DECLARE
    v_total NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_total FROM deposito WHERE estado_verificacion = 1;
    DBMS_OUTPUT.PUT_LINE('Depósitos verificados: ' || v_total);
END;
/

PROMPT ===== E09 (IF/ELSIF) Clasificar el volumen de pendientes =====
DECLARE
    v_pend NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_pend FROM deposito WHERE estado_verificacion = 0;
    IF v_pend = 0 THEN
        DBMS_OUTPUT.PUT_LINE('Sin pendientes');
    ELSIF v_pend <= 10 THEN
        DBMS_OUTPUT.PUT_LINE('Pocos pendientes: ' || v_pend);
    ELSE
        DBMS_OUTPUT.PUT_LINE('Muchos pendientes: ' || v_pend);
    END IF;
END;
/

PROMPT ===== E10 (cursor FOR) Recorrer las estaciones activas y contar sus canecas =====
BEGIN
    FOR e IN (SELECT p.nombre, COUNT(c.id_contenedor) AS canecas
              FROM   punto_instalacion p
              LEFT JOIN contenedor c ON c.id_punto_instalacion = p.id_punto_instalacion
              WHERE  p.activo = 1
              GROUP  BY p.nombre ORDER BY p.nombre) LOOP
        DBMS_OUTPUT.PUT_LINE(RPAD(e.nombre, 40) || e.canecas || ' canecas');
    END LOOP;
END;
/

PROMPT ===== E11 (función) Peso estimado de 10 unidades con fn_peso_estimado_kg =====
SELECT clave_objeto, fn_peso_estimado_kg(id_peso_referencial, 10) AS kg_10_unidades
FROM   peso_referencial
ORDER  BY id_peso_referencial
FETCH  FIRST 5 ROWS ONLY;

PROMPT ===== E12 (paquete) Comparar el saldo del paquete con la vista para los 5 primeros ciudadanos =====
SELECT 'C-' || LPAD(v.id_usuario, 4, '0') AS ciudadano,
       v.saldo                             AS saldo_vista,
       pkg_ecopuntos.saldo(v.id_usuario)   AS saldo_paquete,
       CASE WHEN v.saldo = pkg_ecopuntos.saldo(v.id_usuario) THEN 'COINCIDE' ELSE 'REVISAR' END AS control
FROM   vw_saldo_ecopuntos v
ORDER  BY v.saldo DESC, v.id_usuario
FETCH  FIRST 5 ROWS ONLY;
