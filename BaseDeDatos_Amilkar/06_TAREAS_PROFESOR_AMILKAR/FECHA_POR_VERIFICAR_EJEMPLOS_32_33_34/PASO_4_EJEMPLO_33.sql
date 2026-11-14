-- ===========================================================================
-- DIAPOSITIVA 33: auditoria de salarios con OLD y NEW
-- CONEXION: CNX_HR (o cualquier conexion al MISMO usuario donde se instalo)
-- ABRIR SOLO ESTE ARCHIVO Y PRESIONAR F5. Se puede repetir.
-- Datos y resultados coinciden con PLSQL_y_Disparadores.pdf.
-- ===========================================================================
SET SERVEROUTPUT ON
SET LINESIZE 150
SET PAGESIZE 80
PROMPT
PROMPT =================== DIAPOSITIVA 33: auditoria de salarios con OLD y NEW ===================

-- REQUISITO para esta diapositiva: DIAP_24_PROCEDIMIENTO_AUMENTAR.sql
-- (alli se creo pr_aumentar_salario, llamado al final del ejemplo).
CREATE OR REPLACE TRIGGER trg_aud_salarios
AFTER UPDATE OF salario ON empleados
FOR EACH ROW
WHEN (NEW.salario <> OLD.salario)
BEGIN
    INSERT INTO auditoria_salarios
        (id_empleado, salario_anterior, salario_nuevo, fecha, usuario)
    VALUES
        (:OLD.id_empleado, :OLD.salario,
         :NEW.salario, SYSDATE, USER);
END;
/
SHOW ERRORS

PROMPT Carlos antes
SELECT id_empleado, nombre, salario FROM empleados WHERE id_empleado=6;
BEGIN
    pr_aumentar_salario(6, 10);
END;
/
PROMPT Auditoria generada AUTOMATICAMENTE
SELECT id_empleado, salario_anterior, salario_nuevo,
       TO_CHAR(fecha, 'DD/MM/YYYY HH24:MI:SS') AS fecha,
       usuario
FROM auditoria_salarios
ORDER BY id_empleado;
ROLLBACK;
PROMPT Tras ROLLBACK no quedan cambios de prueba
SELECT COUNT(*) AS registros_auditoria FROM auditoria_salarios;
SELECT id_empleado, nombre, salario FROM empleados WHERE id_empleado=6;
-- RESULTADO: id 6 | 2800000 | 3080000, usuario actual y fecha actual.
