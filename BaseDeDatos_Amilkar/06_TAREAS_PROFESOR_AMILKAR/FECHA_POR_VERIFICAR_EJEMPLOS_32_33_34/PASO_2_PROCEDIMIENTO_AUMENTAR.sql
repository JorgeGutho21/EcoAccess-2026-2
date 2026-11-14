-- ===========================================================================
-- DIAPOSITIVA 24: crear y llamar pr_aumentar_salario
-- CONEXION: CNX_HR (o cualquier conexion al MISMO usuario donde se instalo)
-- ABRIR SOLO ESTE ARCHIVO Y PRESIONAR F5. Se puede repetir.
-- Datos y resultados coinciden con PLSQL_y_Disparadores.pdf.
-- ===========================================================================
SET SERVEROUTPUT ON
SET LINESIZE 150
SET PAGESIZE 80
PROMPT
PROMPT =================== DIAPOSITIVA 24: crear y llamar pr_aumentar_salario ===================

CREATE OR REPLACE PROCEDURE pr_aumentar_salario (
    p_id      IN NUMBER,
    p_porcent IN NUMBER
)
IS
BEGIN
    UPDATE empleados
    SET salario = salario + salario * p_porcent / 100
    WHERE id_empleado = p_id;
END pr_aumentar_salario;
/
SHOW ERRORS

PROMPT Carlos antes
SELECT nombre, salario FROM empleados WHERE id_empleado = 6;
BEGIN
    pr_aumentar_salario(6, 10);
END;
/
PROMPT Carlos despues (2800000 -> 3080000)
SELECT nombre, salario FROM empleados WHERE id_empleado = 6;

-- Deshacer: permite volver a mostrar el ejemplo en clase.
ROLLBACK;
PROMPT Carlos restaurado
SELECT nombre, salario FROM empleados WHERE id_empleado = 6;
