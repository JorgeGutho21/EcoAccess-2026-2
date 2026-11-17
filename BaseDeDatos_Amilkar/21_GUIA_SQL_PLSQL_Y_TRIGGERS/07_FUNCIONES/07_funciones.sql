-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 07 Funciones
-- SOLO LECTURA. Conexión: EcoAccess BD LOCAL. Ejecuta con F5.
SET SERVEROUTPUT ON
SET PAGESIZE 100
SET LINESIZE 200

PROMPT ===== A. Ejemplo básico: una función LOCAL que devuelve un valor =====
DECLARE
    FUNCTION precio_final(p_precio NUMBER, p_utilidad NUMBER) RETURN NUMBER IS
    BEGIN
        RETURN ROUND(p_precio * (1 + p_utilidad / 100), 2);
    END;
BEGIN
    DBMS_OUTPUT.PUT_LINE('1000 con 20 % de utilidad = ' || precio_final(1000, 20));
END;
/

PROMPT ===== B. Funciones reales de EcoAccess =====
SELECT object_name, status FROM user_objects WHERE object_type = 'FUNCTION' ORDER BY object_name;

PROMPT ===== C. fn_peso_estimado_kg dentro de un SELECT: peso típico de 3 unidades =====
SELECT p.id_peso_referencial,
       p.clave_objeto,
       p.gramos_tipico,
       fn_peso_estimado_kg(p.id_peso_referencial, 3) AS kg_estimados_3_unidades
FROM   peso_referencial p
ORDER  BY p.id_peso_referencial
FETCH  FIRST 5 ROWS ONLY;

PROMPT ===== D. fn_peso_estimado_kg con datos inválidos devuelve NULL (no inventa un peso) =====
SELECT fn_peso_estimado_kg(NULL, 3) AS sin_catalogo,
       fn_peso_estimado_kg(1, 0)    AS cero_unidades
FROM   dual;

PROMPT ===== E. fn_resolver_caneca_disponible: qué caneca le corresponde a un material en una estación =====
SELECT c.id_punto_instalacion,
       m.codigo                                                         AS material,
       fn_resolver_caneca_disponible(c.id_punto_instalacion, m.id_material) AS caneca_resuelta,
       c.nombre                                                         AS nombre_caneca
FROM   contenedor_material cm
JOIN   contenedor c        ON c.id_contenedor        = cm.id_contenedor
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
JOIN   material m          ON m.id_material          = cm.id_material
WHERE  c.estado = 1 AND p.activo = 1 AND m.habilitado = 1
ORDER  BY c.id_punto_instalacion, m.codigo
FETCH  FIRST 6 ROWS ONLY;

PROMPT ===== F. fn_depositos_ciudadano: función que devuelve un SYS_REFCURSOR =====
VARIABLE rc REFCURSOR
DECLARE
    v_usuario usuario.id_usuario%TYPE;
BEGIN
    -- el ciudadano con más depósitos (dato sintético)
    SELECT id_usuario INTO v_usuario
    FROM  (SELECT i.id_usuario, COUNT(*) n
           FROM   deposito d
           JOIN   orden_apertura o ON o.id_orden = d.id_orden
           JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
           WHERE  i.id_usuario IS NOT NULL
           GROUP  BY i.id_usuario ORDER BY n DESC, i.id_usuario)
    FETCH FIRST 1 ROWS ONLY;
    DBMS_OUTPUT.PUT_LINE('Depósitos del usuario C-' || LPAD(v_usuario, 4, '0'));
    :rc := fn_depositos_ciudadano(v_usuario);
END;
/
PRINT rc
