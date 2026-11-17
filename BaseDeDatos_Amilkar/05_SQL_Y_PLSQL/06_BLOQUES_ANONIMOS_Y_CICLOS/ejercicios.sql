-- 06_BLOQUES_ANONIMOS_Y_CICLOS — 11 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E216 · 14. PL/SQL básico · Hola mundo
-- Objetivo: El bloque anónimo más simple.
-- Explicación: BEGIN ... END; y DBMS_OUTPUT.PUT_LINE para escribir. En SQL Developer: Ver > Salida DBMS.
BEGIN
    DBMS_OUTPUT.PUT_LINE('Hola, EcoAccess');
END;
/

-- ===== E218 · 14. PL/SQL básico · SELECT INTO
-- Objetivo: Guardar un resultado en una variable.
-- Explicación: SELECT ... INTO exige que salga exactamente UNA fila.
DECLARE
    v_total NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_total FROM deposito;
    DBMS_OUTPUT.PUT_LINE('Depósitos: ' || v_total);
END;
/

-- ===== E223 · 14. PL/SQL básico · LOOP con EXIT
-- Objetivo: Contar del 1 al 3.
-- Explicación: LOOP repite hasta que EXIT WHEN se cumple.
DECLARE
    i PLS_INTEGER := 1;
BEGIN
    LOOP
        DBMS_OUTPUT.PUT_LINE('Vuelta ' || i);
        i := i + 1;
        EXIT WHEN i > 3;
    END LOOP;
END;
/

-- ===== E224 · 14. PL/SQL básico · WHILE
-- Objetivo: Repetir mientras se cumpla.
-- Explicación: WHILE revisa la condición antes de cada vuelta.
DECLARE
    v_gramos NUMBER := 0;
BEGIN
    WHILE v_gramos < 100 LOOP
        v_gramos := v_gramos + 22;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Botellas hasta pasar 100 g: ' || v_gramos / 22 || ' (' || v_gramos || ' g)');
END;
/

-- ===== E225 · 14. PL/SQL básico · FOR numérico
-- Objetivo: Tabla de multiplicar de puntos.
-- Explicación: FOR i IN 1..n crea el contador solo.
BEGIN
    FOR i IN 1 .. 5 LOOP
        DBMS_OUTPUT.PUT_LINE(i || ' botellas PET x 12 puntos = ' || i * 12);
    END LOOP;
END;
/

-- ===== E226 · 14. PL/SQL básico · FOR REVERSE
-- Objetivo: Contar hacia atrás.
-- Explicación: REVERSE invierte el recorrido.
BEGIN
    FOR i IN REVERSE 1 .. 3 LOOP
        DBMS_OUTPUT.PUT_LINE('Cerrando tapa en ' || i);
    END LOOP;
END;
/

-- ===== E229 · 14. PL/SQL básico · BOOLEAN
-- Objetivo: Variables verdadero/falso en PL/SQL.
-- Explicación: En PL/SQL existe BOOLEAN; en tablas se usa NUMBER(1).
DECLARE
    v_hay_pendientes BOOLEAN;
    v_n NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_n FROM deposito WHERE estado_verificacion = 0;
    v_hay_pendientes := v_n > 0;
    IF v_hay_pendientes THEN DBMS_OUTPUT.PUT_LINE('Hay ' || v_n || ' depósitos por verificar.'); END IF;
END;
/

-- ===== E230 · 14. PL/SQL básico · Registro propio (RECORD)
-- Objetivo: Agrupar varios datos en una variable.
-- Explicación: TYPE ... IS RECORD define una estructura.
DECLARE
    TYPE t_resumen IS RECORD (verificados NUMBER, pendientes NUMBER);
    r t_resumen;
BEGIN
    SELECT COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END), COUNT(CASE WHEN estado_verificacion = 0 THEN 1 END) INTO r.verificados, r.pendientes FROM deposito;
    DBMS_OUTPUT.PUT_LINE('Verificados=' || r.verificados || ' Pendientes=' || r.pendientes);
END;
/

-- ===== E231 · 14. PL/SQL básico · Colección y BULK COLLECT
-- Objetivo: Traer muchas filas de una sola vez.
-- Explicación: BULK COLLECT llena una colección; es más rápido que fila por fila.
DECLARE
    TYPE t_codigos IS TABLE OF material.codigo%TYPE;
    v t_codigos;
BEGIN
    SELECT codigo BULK COLLECT INTO v FROM material WHERE categoria = 1 ORDER BY codigo;
    DBMS_OUTPUT.PUT_LINE('Materiales blancos: ' || v.COUNT || '. Primero: ' || v(1) || ', último: ' || v(v.LAST));
END;
/

-- ===== E232 · 14. PL/SQL básico · Arreglo asociativo
-- Objetivo: Una «tabla» indexada por texto.
-- Explicación: INDEX BY VARCHAR2 funciona como un diccionario.
DECLARE
    TYPE t_color IS TABLE OF VARCHAR2(10) INDEX BY PLS_INTEGER;
    color t_color;
BEGIN
    color(1) := 'BLANCA'; color(2) := 'VERDE'; color(3) := 'NEGRA';
    FOR i IN 1 .. 3 LOOP DBMS_OUTPUT.PUT_LINE(i || ' = ' || color(i)); END LOOP;
END;
/

-- ===== E233 · 14. PL/SQL básico · CONTINUE en un ciclo
-- Objetivo: Saltar los números pares.
-- Explicación: CONTINUE pasa a la siguiente vuelta.
BEGIN
    FOR i IN 1 .. 7 LOOP
        CONTINUE WHEN MOD(i, 2) = 0;
        DBMS_OUTPUT.PUT_LINE('Impar: ' || i);
    END LOOP;
END;
/
