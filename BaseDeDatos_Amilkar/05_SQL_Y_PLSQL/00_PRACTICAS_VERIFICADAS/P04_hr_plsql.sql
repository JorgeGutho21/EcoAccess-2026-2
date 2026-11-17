-- =====================================================================
-- Práctica P04 · HR: bloque anónimo → procedimiento → función con cursor
-- Conexión: ECOACCESS_PRACTICAS (F5). Lee HR.EMPLOYEES (permiso SELECT).
-- Es la misma tarea de clase, pero sin crear nada dentro del esquema HR.
-- =====================================================================
SET SERVEROUTPUT ON
SET LINESIZE 160

PROMPT ===== 1. BLOQUE ANÓNIMO con cursor FOR implícito y %TYPE =====
DECLARE
    v_depto hr.employees.department_id%TYPE := 80;   -- %TYPE copia el tipo de la columna
    v_total NUMBER := 0;
BEGIN
    FOR r IN (SELECT employee_id, last_name, salary
              FROM   hr.employees
              WHERE  department_id = v_depto
              ORDER  BY employee_id
              FETCH FIRST 5 ROWS ONLY) LOOP
        DBMS_OUTPUT.PUT_LINE(r.employee_id || ' - ' || r.last_name || ' - ' || r.salary);
        v_total := v_total + 1;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Mostrados: ' || v_total || ' (primeros 5 del departamento ' || v_depto || ')');
END;
/

PROMPT ===== 2. Cursor EXPLÍCITO: DECLARE CURSOR, OPEN, FETCH, %NOTFOUND, CLOSE =====
DECLARE
    CURSOR c_dep IS
        SELECT department_id, COUNT(*) AS empleados, ROUND(AVG(salary)) AS promedio
        FROM   hr.employees
        WHERE  department_id IS NOT NULL
        GROUP  BY department_id
        ORDER  BY department_id;
    r c_dep%ROWTYPE;                                  -- %ROWTYPE = una fila del cursor
BEGIN
    OPEN c_dep;
    LOOP
        FETCH c_dep INTO r;
        EXIT WHEN c_dep%NOTFOUND;
        IF r.promedio >= 10000 THEN
            DBMS_OUTPUT.PUT_LINE('Depto ' || r.department_id || ': ' || r.empleados || ' empleados, promedio ALTO ' || r.promedio);
        ELSE
            DBMS_OUTPUT.PUT_LINE('Depto ' || r.department_id || ': ' || r.empleados || ' empleados, promedio ' || r.promedio);
        END IF;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Filas leídas: ' || c_dep%ROWCOUNT);
    CLOSE c_dep;
END;
/

PROMPT ===== 3. EXCEPCIONES: NO_DATA_FOUND y TOO_MANY_ROWS =====
DECLARE
    v_nombre hr.employees.last_name%TYPE;
BEGIN
    SELECT last_name INTO v_nombre FROM hr.employees WHERE employee_id = 99999;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        DBMS_OUTPUT.PUT_LINE('NO_DATA_FOUND: no existe el empleado 99999.');
END;
/
DECLARE
    v_nombre hr.employees.last_name%TYPE;
BEGIN
    SELECT last_name INTO v_nombre FROM hr.employees WHERE department_id = 80;
EXCEPTION
    WHEN TOO_MANY_ROWS THEN
        DBMS_OUTPUT.PUT_LINE('TOO_MANY_ROWS: SELECT INTO espera UNA fila y el departamento 80 tiene varias.');
END;
/

PROMPT ===== 4. PROCEDIMIENTO pr_empleados (el bloque convertido en objeto guardado) =====
CREATE OR REPLACE PROCEDURE pr_empleados (
    p_depto IN hr.employees.department_id%TYPE
)
IS
    CURSOR cur_dpto IS
        SELECT employee_id, last_name, salary
        FROM   hr.employees
        WHERE  department_id = p_depto
        ORDER  BY employee_id;
    v_filas PLS_INTEGER := 0;
BEGIN
    FOR r IN cur_dpto LOOP
        DBMS_OUTPUT.PUT_LINE(r.employee_id || ' - ' || r.last_name || ' - ' || r.salary);
        v_filas := v_filas + 1;
    END LOOP;
    IF v_filas = 0 THEN
        DBMS_OUTPUT.PUT_LINE('El departamento ' || p_depto || ' no tiene empleados.');
    END IF;
END pr_empleados;
/
SHOW ERRORS PROCEDURE pr_empleados
EXEC pr_empleados(60)
EXEC pr_empleados(999)

PROMPT ===== 5. FUNCIÓN fn_empleados que devuelve SYS_REFCURSOR (la usa C#) =====
CREATE OR REPLACE FUNCTION fn_empleados (
    p_depto IN hr.employees.department_id%TYPE
) RETURN SYS_REFCURSOR
IS
    v_cursor SYS_REFCURSOR;
BEGIN
    OPEN v_cursor FOR
        SELECT employee_id, last_name, salary
        FROM   hr.employees
        WHERE  department_id = p_depto
        ORDER  BY employee_id;
    RETURN v_cursor;
END fn_empleados;
/
SHOW ERRORS FUNCTION fn_empleados
VARIABLE rc REFCURSOR
EXEC :rc := fn_empleados(60)
PRINT rc
