-- 07_VARIABLES_Y_CONDICIONALES — 7 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E217 · 14. PL/SQL básico · Variables
-- Objetivo: Declarar, asignar y mostrar.
-- Explicación: DECLARE crea variables; := asigna.
DECLARE
    v_nombre VARCHAR2(50) := 'Caneca verde';
    v_kg     NUMBER := 0.25;
BEGIN
    DBMS_OUTPUT.PUT_LINE(v_nombre || ' recibió ' || v_kg * 1000 || ' g');
END;
/

-- ===== E219 · 14. PL/SQL básico · %TYPE
-- Objetivo: La variable copia el tipo de una columna.
-- Explicación: Si la columna cambia de tamaño, la variable también.
DECLARE
    v_codigo material.codigo%TYPE;
BEGIN
    SELECT codigo INTO v_codigo FROM material WHERE id_material = 101;
    DBMS_OUTPUT.PUT_LINE('Material 101: ' || v_codigo);
END;
/

-- ===== E220 · 14. PL/SQL básico · %ROWTYPE
-- Objetivo: Una variable con TODA la fila.
-- Explicación: Cada campo se lee como v.columna.
DECLARE
    v material%ROWTYPE;
BEGIN
    SELECT * INTO v FROM material WHERE codigo = 'VIDRIO';
    DBMS_OUTPUT.PUT_LINE(v.nombre || ' - categoría ' || v.categoria);
END;
/

-- ===== E221 · 14. PL/SQL básico · IF / ELSIF / ELSE
-- Objetivo: Decidir el color de la caneca.
-- Explicación: IF evalúa condiciones en orden.
DECLARE
    v_cat material.categoria%TYPE := 2;
BEGIN
    IF v_cat = 1 THEN DBMS_OUTPUT.PUT_LINE('BLANCA');
    ELSIF v_cat = 2 THEN DBMS_OUTPUT.PUT_LINE('VERDE');
    ELSIF v_cat = 3 THEN DBMS_OUTPUT.PUT_LINE('NEGRA');
    ELSE DBMS_OUTPUT.PUT_LINE('Ruta especial');
    END IF;
END;
/

-- ===== E222 · 14. PL/SQL básico · CASE en PL/SQL
-- Objetivo: Lo mismo con CASE.
-- Explicación: CASE es más ordenado cuando hay muchos valores.
DECLARE
    v_estado NUMBER := 1;
BEGIN
    DBMS_OUTPUT.PUT_LINE(CASE v_estado WHEN 0 THEN 'Pendiente' WHEN 1 THEN 'Verificado' ELSE 'Rechazado' END);
END;
/

-- ===== E227 · 14. PL/SQL básico · Bloque anidado
-- Objetivo: Un bloque dentro de otro.
-- Explicación: Cada bloque tiene sus variables y su EXCEPTION.
DECLARE
    v_externa NUMBER := 1;
BEGIN
    DECLARE
        v_interna NUMBER := 2;
    BEGIN
        DBMS_OUTPUT.PUT_LINE('Suma: ' || (v_externa + v_interna));
    END;
END;
/

-- ===== E228 · 14. PL/SQL básico · Constante
-- Objetivo: Una variable que no puede cambiar.
-- Explicación: CONSTANT obliga a dar valor y protege de cambios.
DECLARE
    c_ventana CONSTANT NUMBER := 10;
BEGIN
    DBMS_OUTPUT.PUT_LINE('La caneca queda abierta ' || c_ventana || ' segundos.');
END;
/
