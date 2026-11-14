-- =====================================================================
-- Práctica P05 · CRUD de empleado SIN DELETE (insertar, consultar, actualizar)
-- Conexión: ECOACCESS_PRACTICAS (F5), después de P00. Se puede repetir.
-- Lo usa la aplicación C# de 10_CSHARP_VISUAL_STUDIO\SOLUCION_CSHARP/EmpleadosCrud.
--
-- ¿Por qué sin DELETE? La tarea lo pide así: en sistemas reales un empleado
-- no se borra (se perdería su historia); se desactiva o se cambia de estado.
-- Los nuevos ids salen de SEQ_EMPLEADO_PRACTICA (empieza en 1000 para no
-- chocar con los ids 100..206 copiados de HR) mediante un trigger BEFORE INSERT.
-- =====================================================================
SET SERVEROUTPUT ON
SET LINESIZE 160
COLUMN nombre FORMAT A30
COLUMN email FORMAT A14

DECLARE
    v NUMBER;
BEGIN
    SELECT COUNT(*) INTO v FROM user_sequences WHERE sequence_name = 'SEQ_EMPLEADO_PRACTICA';
    IF v = 0 THEN
        EXECUTE IMMEDIATE 'CREATE SEQUENCE seq_empleado_practica START WITH 1000 INCREMENT BY 1 NOCYCLE';
    END IF;
END;
/

CREATE OR REPLACE TRIGGER trg_empleado_practica_id
BEFORE INSERT ON empleado_practica
FOR EACH ROW
WHEN (NEW.id_empleado IS NULL)
BEGIN
    :NEW.id_empleado := seq_empleado_practica.NEXTVAL;
END;
/
SHOW ERRORS TRIGGER trg_empleado_practica_id

CREATE OR REPLACE PACKAGE pkg_empleado_crud AS
    PROCEDURE insertar(p_nombre IN empleado_practica.nombre%TYPE,
                       p_email  IN empleado_practica.email%TYPE,
                       p_salario IN empleado_practica.salario%TYPE,
                       p_depto  IN empleado_practica.id_departamento%TYPE,
                       p_id     OUT empleado_practica.id_empleado%TYPE);
    FUNCTION  consultar(p_depto IN empleado_practica.id_departamento%TYPE DEFAULT NULL) RETURN SYS_REFCURSOR;
    PROCEDURE actualizar(p_id      IN empleado_practica.id_empleado%TYPE,
                         p_nombre  IN empleado_practica.nombre%TYPE,
                         p_salario IN empleado_practica.salario%TYPE,
                         p_depto   IN empleado_practica.id_departamento%TYPE);
    -- No hay procedimiento «eliminar»: a propósito.
END pkg_empleado_crud;
/
SHOW ERRORS PACKAGE pkg_empleado_crud

CREATE OR REPLACE PACKAGE BODY pkg_empleado_crud AS
    PROCEDURE insertar(p_nombre IN empleado_practica.nombre%TYPE,
                       p_email  IN empleado_practica.email%TYPE,
                       p_salario IN empleado_practica.salario%TYPE,
                       p_depto  IN empleado_practica.id_departamento%TYPE,
                       p_id     OUT empleado_practica.id_empleado%TYPE) IS
    BEGIN
        IF TRIM(p_nombre) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20401, 'El nombre es obligatorio.');
        END IF;
        INSERT INTO empleado_practica (id_empleado, nombre, email, salario, id_departamento)
        VALUES (NULL, TRIM(p_nombre), UPPER(TRIM(p_email)), p_salario, p_depto)
        RETURNING id_empleado INTO p_id;
    EXCEPTION
        WHEN DUP_VAL_ON_INDEX THEN
            RAISE_APPLICATION_ERROR(-20402, 'Ya existe un empleado con ese id.');
    END insertar;

    FUNCTION consultar(p_depto IN empleado_practica.id_departamento%TYPE DEFAULT NULL) RETURN SYS_REFCURSOR IS
        c SYS_REFCURSOR;
    BEGIN
        OPEN c FOR
            SELECT e.id_empleado, e.nombre, e.email, e.salario, e.id_departamento, d.nombre AS departamento
            FROM   empleado_practica e
            LEFT JOIN departamento_practica d ON d.id_departamento = e.id_departamento
            WHERE  p_depto IS NULL OR e.id_departamento = p_depto
            ORDER  BY e.id_empleado;
        RETURN c;
    END consultar;

    PROCEDURE actualizar(p_id      IN empleado_practica.id_empleado%TYPE,
                         p_nombre  IN empleado_practica.nombre%TYPE,
                         p_salario IN empleado_practica.salario%TYPE,
                         p_depto   IN empleado_practica.id_departamento%TYPE) IS
    BEGIN
        UPDATE empleado_practica
        SET    nombre = TRIM(p_nombre), salario = p_salario, id_departamento = p_depto
        WHERE  id_empleado = p_id;
        IF SQL%ROWCOUNT = 0 THEN
            RAISE_APPLICATION_ERROR(-20002, 'No existe empleado con ese ID');
        END IF;
    END actualizar;
END pkg_empleado_crud;
/
SHOW ERRORS PACKAGE BODY pkg_empleado_crud

PROMPT ===== Prueba: INSERT (id desde la secuencia), SELECT, UPDATE; sin DELETE =====
DECLARE
    v_id empleado_practica.id_empleado%TYPE;
    v_salario empleado_practica.salario%TYPE;
BEGIN
    pkg_empleado_crud.insertar('Saray Prueba', 'sprueba', 3500, 60, v_id);
    DBMS_OUTPUT.PUT_LINE('Insertado con id ' || v_id || ' (debe ser >= 1000)');
    pkg_empleado_crud.actualizar(v_id, 'Saray Prueba Actualizada', 3900, 60);
    SELECT salario INTO v_salario FROM empleado_practica WHERE id_empleado = v_id;
    DBMS_OUTPUT.PUT_LINE('Salario tras actualizar: ' || v_salario || ' (esperado 3900)');
    BEGIN
        pkg_empleado_crud.actualizar(99999, 'Nadie', 1, 60);
    EXCEPTION WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Actualizar inexistente -> ' || SQLERRM);
    END;
    BEGIN
        pkg_empleado_crud.insertar('X', 'x', -5, 60, v_id);
    EXCEPTION WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Salario negativo -> ' || SQLERRM);
    END;
END;
/
VARIABLE rc REFCURSOR
EXEC :rc := pkg_empleado_crud.consultar(60)
PRINT rc
ROLLBACK;
PROMPT Se hizo ROLLBACK. Desde C# los cambios sí se confirman con COMMIT.
