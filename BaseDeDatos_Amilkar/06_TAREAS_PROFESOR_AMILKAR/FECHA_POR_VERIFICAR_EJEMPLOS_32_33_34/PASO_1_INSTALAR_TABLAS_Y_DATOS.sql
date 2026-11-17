-- ============================================================================
-- INSTALACION REUTILIZABLE DE TABLAS DE LA PRESENTACION
-- Ejecutar con F5 ANTES de las diapositivas. No borra tablas ni filas existentes.
-- Se puede reejecutar: las tablas y las filas semilla solo se insertan si faltan.
-- ATENCION: usa estas tablas solo para la exposicion. Si ya existian con otro
-- significado en tu esquema, NO ejecutes este archivo sobre ellas.
-- NO MODIFICA EMPLOYEES NI DEPARTMENTS del HR original.
-- ============================================================================
SET SERVEROUTPUT ON
SET LINESIZE 150
SET PAGESIZE 80
PROMPT ===== CREAR TABLAS FALTANTES =====
DECLARE
    v_existe NUMBER;
    PROCEDURE crear_si_falta(p_nombre VARCHAR2, p_ddl VARCHAR2) IS
    BEGIN
        SELECT COUNT(*) INTO v_existe
        FROM user_tables WHERE table_name = UPPER(p_nombre);
        IF v_existe = 0 THEN
            EXECUTE IMMEDIATE p_ddl;
            DBMS_OUTPUT.PUT_LINE('Creada: ' || p_nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('Ya existe; no se borra: ' || p_nombre);
        END IF;
    END;
BEGIN
    crear_si_falta('DEPARTAMENTOS',
       'CREATE TABLE departamentos (' ||
       'id_departamento NUMBER PRIMARY KEY, nombre VARCHAR2(50))');
    crear_si_falta('EMPLEADOS',
       'CREATE TABLE empleados (' ||
       'id_empleado NUMBER PRIMARY KEY, nombre VARCHAR2(50), ' ||
       'salario NUMBER(10,2), id_departamento NUMBER ' ||
       'REFERENCES departamentos(id_departamento))');
    crear_si_falta('PRODUCTOS',
       'CREATE TABLE productos (' ||
       'id_producto NUMBER PRIMARY KEY, nombre VARCHAR2(50), stock NUMBER)');
    crear_si_falta('VENTAS',
       'CREATE TABLE ventas (' ||
       'id_venta NUMBER PRIMARY KEY, id_producto NUMBER ' ||
       'REFERENCES productos(id_producto), cantidad NUMBER)');
    crear_si_falta('AUDITORIA_SALARIOS',
       'CREATE TABLE auditoria_salarios (' ||
       'id_empleado NUMBER, salario_anterior NUMBER, salario_nuevo NUMBER, ' ||
       'fecha DATE, usuario VARCHAR2(30))');
    crear_si_falta('NOTAS',
       'CREATE TABLE notas (' ||
       'id_nota NUMBER PRIMARY KEY, id_estudiante NUMBER, nota NUMBER(3,1))');
END;
/
PROMPT ===== CARGAR FILAS DE LA DIAPOSITIVA 4 SIN DUPLICAR =====
MERGE INTO departamentos d USING
 (SELECT 10 id_departamento, 'Ventas' nombre FROM dual UNION ALL
  SELECT 20, 'Sistemas' FROM dual UNION ALL
  SELECT 30, 'Contabilidad' FROM dual) x
ON (d.id_departamento = x.id_departamento)
WHEN NOT MATCHED THEN
 INSERT (id_departamento, nombre) VALUES (x.id_departamento, x.nombre);

MERGE INTO empleados e USING
 (SELECT 1 id_empleado, 'Ana' nombre, 3000000 salario, 10 id_departamento FROM dual UNION ALL
  SELECT 2, 'Luis', 2500000, 10 FROM dual UNION ALL
  SELECT 3, 'Marta', 4200000, 20 FROM dual UNION ALL
  SELECT 4, 'Pedro', 1800000, 20 FROM dual UNION ALL
  SELECT 5, 'Sofía', 3600000, 30 FROM dual UNION ALL
  SELECT 6, 'Carlos', 2800000, 30 FROM dual) x
ON (e.id_empleado = x.id_empleado)
WHEN NOT MATCHED THEN
 INSERT (id_empleado, nombre, salario, id_departamento)
 VALUES (x.id_empleado, x.nombre, x.salario, x.id_departamento);

MERGE INTO productos p USING
 (SELECT 1 id_producto, 'Producto 1' nombre, 50 stock FROM dual) x
ON (p.id_producto = x.id_producto)
WHEN NOT MATCHED THEN
 INSERT (id_producto, nombre, stock)
 VALUES (x.id_producto, x.nombre, x.stock);

COMMIT;
PROMPT ===== TABLAS CREADAS. DATOS GUARDADOS. =====
PROMPT ===== COMPRUEBA: 6 EMPLEADOS, 3 DEPARTAMENTOS, STOCK 50 =====
SELECT id_empleado, nombre, salario, id_departamento
FROM empleados ORDER BY id_empleado;
SELECT id_departamento, nombre
FROM departamentos ORDER BY id_departamento;
SELECT id_producto, nombre, stock FROM productos;
