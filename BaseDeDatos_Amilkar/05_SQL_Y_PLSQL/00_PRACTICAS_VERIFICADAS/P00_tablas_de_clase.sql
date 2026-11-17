-- =====================================================================
-- Prácticas de clase — P00 · Tablas de los ejemplos 32, 33 y 34 y copia de HR
-- Conexión: ECOACCESS_PRACTICAS (F5). Se puede repetir: solo crea lo que falta.
--
-- ¿Por qué un esquema aparte? Estos ejercicios (empleados, productos, notas)
-- NO son parte del negocio de EcoAccess. Mezclarlos con el modelo ambiental
-- confundiría al lector del diagrama y del diccionario de datos.
-- HR.EMPLOYEES y HR.DEPARTMENTS solo se LEEN: se copian a EMPLEADO_PRACTICA y
-- DEPARTAMENTO_PRACTICA para que el CRUD no modifique el esquema HR original.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    PROCEDURE crear_si_falta(p_nombre VARCHAR2, p_ddl VARCHAR2) IS
        v NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v FROM user_objects WHERE object_name = UPPER(p_nombre);
        IF v = 0 THEN
            EXECUTE IMMEDIATE p_ddl;
            DBMS_OUTPUT.PUT_LINE('Creado: ' || p_nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('Ya existía (se conserva): ' || p_nombre);
        END IF;
    END;
BEGIN
    IF USER <> 'ECOACCESS_PRACTICAS' THEN
        RAISE_APPLICATION_ERROR(-20009, 'Conéctate como ECOACCESS_PRACTICAS. Usuario actual: ' || USER);
    END IF;
    -- Tablas de las diapositivas 32-34 (mismos nombres y datos de la clase).
    crear_si_falta('DEPARTAMENTOS', 'CREATE TABLE departamentos (id_departamento NUMBER PRIMARY KEY, nombre VARCHAR2(50 CHAR))');
    crear_si_falta('EMPLEADOS', 'CREATE TABLE empleados (id_empleado NUMBER PRIMARY KEY, nombre VARCHAR2(50 CHAR), '
        || 'salario NUMBER(10,2), id_departamento NUMBER REFERENCES departamentos(id_departamento))');
    crear_si_falta('PRODUCTOS', 'CREATE TABLE productos (id_producto NUMBER PRIMARY KEY, nombre VARCHAR2(50 CHAR), stock NUMBER)');
    crear_si_falta('VENTAS', 'CREATE TABLE ventas (id_venta NUMBER PRIMARY KEY, '
        || 'id_producto NUMBER REFERENCES productos(id_producto), cantidad NUMBER)');
    crear_si_falta('AUDITORIA_SALARIOS', 'CREATE TABLE auditoria_salarios (id_empleado NUMBER, salario_anterior NUMBER, '
        || 'salario_nuevo NUMBER, fecha DATE, usuario VARCHAR2(128 CHAR))');
    crear_si_falta('NOTAS', 'CREATE TABLE notas (id_nota NUMBER PRIMARY KEY, id_estudiante NUMBER, nota NUMBER(3,1))');
    -- Copias de HR para el CRUD sin DELETE (tarea de C#).
    crear_si_falta('DEPARTAMENTO_PRACTICA', 'CREATE TABLE departamento_practica AS '
        || 'SELECT department_id AS id_departamento, department_name AS nombre FROM hr.departments');
    crear_si_falta('EMPLEADO_PRACTICA', 'CREATE TABLE empleado_practica AS '
        || 'SELECT employee_id AS id_empleado, first_name || '' '' || last_name AS nombre, '
        || 'email, salary AS salario, department_id AS id_departamento FROM hr.employees');
END;
/

-- Llaves de las copias (CREATE TABLE AS SELECT no copia restricciones).
DECLARE
    v NUMBER;
BEGIN
    SELECT COUNT(*) INTO v FROM user_constraints WHERE constraint_name = 'PK_DEPARTAMENTO_PRACTICA';
    IF v = 0 THEN
        EXECUTE IMMEDIATE 'ALTER TABLE departamento_practica ADD CONSTRAINT pk_departamento_practica PRIMARY KEY (id_departamento)';
        EXECUTE IMMEDIATE 'ALTER TABLE empleado_practica ADD CONSTRAINT pk_empleado_practica PRIMARY KEY (id_empleado)';
        EXECUTE IMMEDIATE 'ALTER TABLE empleado_practica MODIFY (nombre NOT NULL, salario NOT NULL)';
        EXECUTE IMMEDIATE 'ALTER TABLE empleado_practica ADD CONSTRAINT ck_empleado_practica_salario CHECK (salario >= 0)';
        EXECUTE IMMEDIATE 'ALTER TABLE empleado_practica ADD CONSTRAINT fk_empleado_practica_depto '
            || 'FOREIGN KEY (id_departamento) REFERENCES departamento_practica (id_departamento)';
        EXECUTE IMMEDIATE 'CREATE INDEX ix_empleado_practica_depto ON empleado_practica (id_departamento)';
        DBMS_OUTPUT.PUT_LINE('Llaves de EMPLEADO_PRACTICA y DEPARTAMENTO_PRACTICA creadas.');
    END IF;
END;
/

-- Datos de la diapositiva 4 (MERGE = inserta solo si falta).
MERGE INTO departamentos d USING (
    SELECT 10 id, 'Ventas' nombre FROM dual UNION ALL SELECT 20, 'Sistemas' FROM dual UNION ALL SELECT 30, 'Contabilidad' FROM dual) x
ON (d.id_departamento = x.id)
WHEN NOT MATCHED THEN INSERT (id_departamento, nombre) VALUES (x.id, x.nombre);
MERGE INTO empleados e USING (
    SELECT 1 id, 'Ana' nombre, 3000000 salario, 10 depto FROM dual UNION ALL
    SELECT 2, 'Luis', 2500000, 10 FROM dual UNION ALL SELECT 3, 'Marta', 4200000, 20 FROM dual UNION ALL
    SELECT 4, 'Pedro', 1800000, 20 FROM dual UNION ALL SELECT 5, 'Sofía', 3600000, 30 FROM dual UNION ALL
    SELECT 6, 'Carlos', 2800000, 30 FROM dual) x
ON (e.id_empleado = x.id)
WHEN NOT MATCHED THEN INSERT (id_empleado, nombre, salario, id_departamento) VALUES (x.id, x.nombre, x.salario, x.depto);
MERGE INTO productos p USING (SELECT 1 id, 'Producto 1' nombre, 50 stock FROM dual) x
ON (p.id_producto = x.id)
WHEN NOT MATCHED THEN INSERT (id_producto, nombre, stock) VALUES (x.id, x.nombre, x.stock);
COMMIT;

PROMPT ==== Comprobación: 3 departamentos, 6 empleados, stock 50 y 107 empleados copiados de HR ====
SELECT (SELECT COUNT(*) FROM departamentos) departamentos, (SELECT COUNT(*) FROM empleados) empleados,
       (SELECT stock FROM productos WHERE id_producto = 1) stock, (SELECT COUNT(*) FROM empleado_practica) copia_hr
FROM dual;
WHENEVER SQLERROR CONTINUE
