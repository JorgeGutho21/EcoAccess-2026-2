-- ===========================================================================
-- DIAPOSITIVA 32: inventario: descontar stock
-- CONEXION: CNX_HR (o cualquier conexion al MISMO usuario donde se instalo)
-- ABRIR SOLO ESTE ARCHIVO Y PRESIONAR F5. Se puede repetir.
-- Datos y resultados coinciden con PLSQL_y_Disparadores.pdf.
-- ===========================================================================
SET SERVEROUTPUT ON
SET LINESIZE 150
SET PAGESIZE 80
PROMPT
PROMPT =================== DIAPOSITIVA 32: inventario: descontar stock ===================

CREATE OR REPLACE TRIGGER trg_descontar_stock
AFTER INSERT ON ventas
FOR EACH ROW
BEGIN
    UPDATE productos
    SET stock = stock - :NEW.cantidad
    WHERE id_producto = :NEW.id_producto;
END;
/
SHOW ERRORS

PROMPT Stock antes
SELECT id_producto, nombre, stock FROM productos WHERE id_producto = 1;
INSERT INTO ventas VALUES (9001, 1, 5);
PROMPT Stock luego de registrar la venta
SELECT id_producto, nombre, stock FROM productos WHERE id_producto = 1;
ROLLBACK;
PROMPT Stock restaurado: se puede repetir
SELECT id_producto, nombre, stock FROM productos WHERE id_producto = 1;
-- RESULTADO: 50 -> 45 -> 50.
