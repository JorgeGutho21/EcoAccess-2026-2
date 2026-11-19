-- =====================================================================
-- EcoAccess · Ejercicios académicos de SQL y PL/SQL sobre el esquema real
-- Parte 1 (E1–E10): SOLO LECTURA, se puede ejecutar en Oracle Cloud.
-- Parte 2 (E11–E14): escribe y termina con ROLLBACK; ejecutar SOLO en el esquema aislado o en uno de desarrollo.
-- Explicación de cada ejercicio: docs/database/15-explicacion-academica.md
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LINESIZE 200 PAGESIZE 100
COLUMN nombre FORMAT A40
COLUMN estacion FORMAT A36
COLUMN caneca FORMAT A30
COLUMN color FORMAT A8

PROMPT E1 · SELECT con WHERE y ORDER BY: materiales que van a la caneca blanca
SELECT codigo, nombre FROM material WHERE categoria = 1 AND habilitado = 1 ORDER BY nombre;

PROMPT E2 · Funciones de grupo: cuántos materiales hay de cada categoría
SELECT categoria, COUNT(*) AS materiales FROM material GROUP BY categoria ORDER BY categoria;

PROMPT E3 · JOIN: cada caneca con su estación y su color
SELECT p.nombre AS estacion, c.nombre AS caneca,
       DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA') AS color
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor
ORDER  BY p.nombre, k.categoria;

PROMPT E4 · JOIN N:M: cuántos materiales admite cada caneca en servicio
SELECT c.nombre AS caneca, COUNT(cm.id_material) AS materiales_admitidos
FROM   contenedor c LEFT JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor
WHERE  c.estado = 1
GROUP  BY c.nombre ORDER BY 2 DESC;

PROMPT E5 · GROUP BY + HAVING: estados de depósito con más de 10 casos
SELECT estado_texto, COUNT(*) AS depositos FROM vw_deposito_detalle GROUP BY estado_texto HAVING COUNT(*) > 10;

PROMPT E6 · Subconsulta: materiales que tienen una tarifa vigente
SELECT m.codigo, m.nombre FROM material m
WHERE  EXISTS (SELECT 1 FROM regla_ecopuntos r WHERE r.id_material = m.id_material AND r.hasta_utc IS NULL)
ORDER  BY m.codigo;

PROMPT E7 · Función en un SELECT: peso estimado de 3 unidades de cada objeto de referencia
SELECT descripcion_objeto, gramos_tipico, fn_peso_estimado_kg(id_peso_referencial, 3) AS kg_3_unidades
FROM   peso_referencial ORDER BY id_peso_referencial FETCH FIRST 5 ROWS ONLY;

PROMPT E8 · Función de paquete en un SELECT: saldo de 5 ciudadanos (debe coincidir con la vista)
SELECT v.id_usuario, v.saldo AS saldo_vista, pkg_ecopuntos.saldo(v.id_usuario) AS saldo_paquete
FROM   vw_saldo_ecopuntos v ORDER BY v.id_usuario FETCH FIRST 5 ROWS ONLY;

PROMPT E9 · Procedimiento con cursor de salida: resumen de los últimos 30 días
VARIABLE rc REFCURSOR
EXEC pkg_estadisticas.consultar_resumen(SYSDATE - 30, SYSDATE + 1, NULL, NULL, :rc)
PRINT rc

PROMPT E10 · Bloque anónimo con manejo de excepciones: la función explica por qué no hay caneca
DECLARE
    v_caneca NUMBER;
BEGIN
    v_caneca := fn_resolver_caneca_disponible(-1, 1);
    DBMS_OUTPUT.PUT_LINE('Caneca: ' || v_caneca);
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Oracle respondió: ' || SQLERRM);
END;
/
