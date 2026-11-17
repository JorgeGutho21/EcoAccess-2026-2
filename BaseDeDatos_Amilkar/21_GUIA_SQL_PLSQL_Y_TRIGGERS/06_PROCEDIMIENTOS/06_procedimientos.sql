-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 06 Procedimientos
-- SOLO LECTURA: no crea objetos ni modifica datos. Conexión: EcoAccess BD LOCAL. Ejecuta con F5.
SET SERVEROUTPUT ON
SET PAGESIZE 100
SET LINESIZE 200

PROMPT ===== A. Ejemplo básico: un procedimiento LOCAL dentro de un bloque (no queda guardado) =====
DECLARE
    PROCEDURE saludar(p_nombre IN VARCHAR2, p_veces IN PLS_INTEGER DEFAULT 1) IS
    BEGIN
        FOR i IN 1 .. p_veces LOOP
            DBMS_OUTPUT.PUT_LINE('Hola, ' || p_nombre || ' (' || i || ')');
        END LOOP;
    END saludar;
BEGIN
    saludar('Amílkar');
    saludar(p_nombre => 'EcoAccess', p_veces => 2);     -- parámetros por nombre
END;
/

PROMPT ===== B. Procedimientos reales de EcoAccess (lo que Oracle tiene guardado) =====
SELECT object_name, object_type, status
FROM   user_objects
WHERE  object_type = 'PROCEDURE'
ORDER  BY object_name;

PROMPT ===== C. Parámetros de pr_consultar_estadisticas (IN y OUT) =====
SELECT argument_name, position, data_type, in_out
FROM   user_arguments
WHERE  object_name = 'PR_CONSULTAR_ESTADISTICAS' AND package_name IS NULL
ORDER  BY position;

PROMPT ===== D. Llamada de SOLO LECTURA: resumen de los últimos 30 días, todas las estaciones y colores =====
VARIABLE rc REFCURSOR
BEGIN
    pr_consultar_estadisticas(
        p_desde_col => TRUNC(SYSDATE) - 30,
        p_hasta_col => TRUNC(SYSDATE) + 1,
        p_id_punto  => NULL,          -- NULL = todas las estaciones
        p_categoria => NULL,          -- NULL = todos los colores de caneca
        p_resultado => :rc);
END;
/
PRINT rc

-- E. pr_cambiar_estado_contenedor SÍ modifica datos (cambia el estado de una caneca y el
--    disparador 1 escribe en HISTORIAL_ESTADO_CONTENEDOR). No se ejecuta aquí.
--    Se demuestra, con ROLLBACK al final, en ../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql.
