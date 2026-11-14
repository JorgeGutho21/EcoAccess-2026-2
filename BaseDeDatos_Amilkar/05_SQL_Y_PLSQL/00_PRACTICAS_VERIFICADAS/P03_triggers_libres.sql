-- =====================================================================
-- Práctica P03 · Disparadores LIBRES y ejemplos 32, 33 y 34 de clase
-- Conexión: ECOACCESS_PRACTICAS (F5), después de P00. Se puede repetir.
--
--   Libre 1  TRG_AUD_SALARIOS       AFTER UPDATE OF salario  (ejemplo 33 mejorado: NULL y cambio real)
--   Libre 2  TRG_VALIDAR_NOTA       BEFORE INSERT OR UPDATE  (ejemplo 34: ORA-20001 nota fuera de 0..5)
--   Ej. 32   TRG_DESCONTAR_STOCK    AFTER INSERT ON ventas   (descuenta stock; impide stock negativo)
--   Extra    TRG_LIMITE_AUMENTO     BEFORE UPDATE OF salario (regla del 20 %: MODIFICA :NEW)
--   Proc.    PR_AUMENTAR_SALARIO    (diapositiva 24)
-- Ninguno hace COMMIT. Cada demostración termina con ROLLBACK.
-- Advertencia de clase: no tener DOS disparadores de auditoría de salario a la
-- vez, porque cada UPDATE dejaría dos filas repetidas.
-- =====================================================================
SET SERVEROUTPUT ON
SET LINESIZE 160
COLUMN nombre FORMAT A12
COLUMN usuario FORMAT A22

CREATE OR REPLACE PROCEDURE pr_aumentar_salario (
    p_id      IN empleados.id_empleado%TYPE,
    p_porcent IN NUMBER
)
IS
BEGIN
    UPDATE empleados
    SET    salario = salario + salario * p_porcent / 100
    WHERE  id_empleado = p_id;
    IF SQL%ROWCOUNT = 0 THEN                         -- cursor IMPLÍCITO: cuántas filas cambió el UPDATE
        RAISE_APPLICATION_ERROR(-20002, 'No existe el empleado ' || p_id);
    END IF;
END pr_aumentar_salario;
/
SHOW ERRORS PROCEDURE pr_aumentar_salario

-- ---------------- Libre 1: auditoría de salario (AFTER) ----------------
-- AFTER: se registra solo lo que realmente se guardó.
-- WHEN: solo si hubo cambio real, contemplando NULL (NULL <> 5 da «desconocido»).
CREATE OR REPLACE TRIGGER trg_aud_salarios
AFTER UPDATE OF salario ON empleados
FOR EACH ROW
WHEN (NEW.salario <> OLD.salario
      OR (NEW.salario IS NULL AND OLD.salario IS NOT NULL)
      OR (NEW.salario IS NOT NULL AND OLD.salario IS NULL))
BEGIN
    INSERT INTO auditoria_salarios (id_empleado, salario_anterior, salario_nuevo, fecha, usuario)
    VALUES (:OLD.id_empleado, :OLD.salario, :NEW.salario, SYSDATE, USER);
END;
/
SHOW ERRORS TRIGGER trg_aud_salarios

-- ---------------- Extra: regla del 20 % (BEFORE modifica :NEW) ----------------
-- Si alguien intenta subir más del 20 %, el aumento se recorta al 20 %.
-- Solo un BEFORE de fila puede asignar :NEW; :OLD nunca cambia.
CREATE OR REPLACE TRIGGER trg_limite_aumento
BEFORE UPDATE OF salario ON empleados
FOR EACH ROW
WHEN (NEW.salario > OLD.salario * 1.2)
BEGIN
    DBMS_OUTPUT.PUT_LINE('Aumento recortado al 20 %: ' || :NEW.salario || ' -> ' || :OLD.salario * 1.2);
    :NEW.salario := :OLD.salario * 1.2;
END;
/
SHOW ERRORS TRIGGER trg_limite_aumento

-- ---------------- Libre 2: validación de nota (ejemplo 34) ----------------
CREATE OR REPLACE TRIGGER trg_validar_nota
BEFORE INSERT OR UPDATE ON notas
FOR EACH ROW
BEGIN
    IF :NEW.nota IS NULL OR :NEW.nota < 0 OR :NEW.nota > 5 THEN
        RAISE_APPLICATION_ERROR(-20001, 'La nota debe estar entre 0 y 5');
    END IF;
END;
/
SHOW ERRORS TRIGGER trg_validar_nota

-- ---------------- Ejemplo 32: descontar stock al vender ----------------
CREATE OR REPLACE TRIGGER trg_descontar_stock
AFTER INSERT ON ventas
FOR EACH ROW
DECLARE
    v_stock productos.stock%TYPE;
BEGIN
    UPDATE productos SET stock = stock - :NEW.cantidad
    WHERE  id_producto = :NEW.id_producto
    RETURNING stock INTO v_stock;
    IF v_stock < 0 THEN
        RAISE_APPLICATION_ERROR(-20003, 'Stock insuficiente para el producto ' || :NEW.id_producto);
    END IF;
END;
/
SHOW ERRORS TRIGGER trg_descontar_stock

PROMPT
PROMPT ===== DEMO Ejemplo 32: stock 50, se venden 5 -> 45 =====
INSERT INTO ventas VALUES (9001, 1, 5);
SELECT id_producto, stock FROM productos WHERE id_producto = 1;
PROMPT ===== DEMO Ejemplo 32 (debe fallar ORA-20003): vender 100 unidades =====
INSERT INTO ventas VALUES (9002, 1, 100);
ROLLBACK;

PROMPT ===== DEMO Ejemplo 33 / Libre 1: Carlos 2 800 000 + 10 % = 3 080 000 y queda auditado =====
EXEC pr_aumentar_salario(6, 10)
SELECT id_empleado, nombre, salario FROM empleados WHERE id_empleado = 6;
SELECT id_empleado, salario_anterior, salario_nuevo, usuario FROM auditoria_salarios;
PROMPT ===== DEMO Libre 1: un UPDATE con el mismo salario NO audita (WHEN) =====
UPDATE empleados SET salario = salario WHERE id_empleado = 1;
SELECT COUNT(*) AS filas_auditoria FROM auditoria_salarios;
PROMPT ===== DEMO Extra 20 %: Pedro 1 800 000 -> pide 5 000 000, queda 2 160 000 =====
UPDATE empleados SET salario = 5000000 WHERE id_empleado = 4;
SELECT id_empleado, nombre, salario FROM empleados WHERE id_empleado = 4;
SELECT id_empleado, salario_anterior, salario_nuevo FROM auditoria_salarios WHERE id_empleado = 4;
PROMPT  -> La auditoría (AFTER) ve el valor YA recortado por el BEFORE: :NEW final = 2 160 000.
PROMPT ===== DEMO procedimiento con empleado inexistente (ORA-20002) =====
EXEC pr_aumentar_salario(999, 10)
ROLLBACK;
SELECT COUNT(*) AS auditoria_tras_rollback FROM auditoria_salarios;

PROMPT ===== DEMO Ejemplo 34-B (debe fallar ORA-20001): nota 6,5 =====
INSERT INTO notas VALUES (9001, 101, 6.5);
PROMPT ===== DEMO Ejemplo 34-C: nota válida 4,2 =====
INSERT INTO notas VALUES (9002, 101, 4.2);
SELECT id_nota, id_estudiante, nota FROM notas;
PROMPT ===== DEMO Libre 2 en UPDATE (debe fallar ORA-20001): cambiar 4,2 por -1 =====
UPDATE notas SET nota = -1 WHERE id_nota = 9002;
ROLLBACK;
PROMPT Se hizo ROLLBACK: las tablas de clase vuelven a su estado inicial.
