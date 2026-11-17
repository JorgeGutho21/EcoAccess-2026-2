-- =====================================================================
-- Práctica P02 · Trigger de PRECIO FINAL (tarea punto 4)
-- Conexión: ECOACCESS_PRACTICAS (F5). Se puede repetir.
--
-- PRODUCTO_PRACTICA es una tabla de PRÁCTICA: EcoAccess no vende productos,
-- por eso vive en este esquema y no en ECOACCESS_BD.
-- Regla: precio_final = precio_unitario × (1 + utilidad / 100), redondeado a 2 decimales.
-- Disparador: BEFORE INSERT OR UPDATE OF precio_unitario, utilidad, precio_final, FOR EACH ROW.
--   * BEFORE: hay que ESCRIBIR :NEW.precio_final antes de guardar la fila.
--   * OF precio_unitario, utilidad: lo que pide la tarea. Se agregó precio_final porque, sin él,
--     un UPDATE que solo escribe precio_final NO dispara el trigger y deja un precio falso
--     (lo detectó la prueba PR05 de P99). Si se cambia otra columna (nombre), no recalcula.
--   * El usuario no puede «inventar» el precio final: se recalcula siempre.
-- =====================================================================
SET SERVEROUTPUT ON
SET LINESIZE 160
COLUMN nombre FORMAT A24

DECLARE
    v NUMBER;
BEGIN
    SELECT COUNT(*) INTO v FROM user_tables WHERE table_name = 'PRODUCTO_PRACTICA';
    IF v = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLE producto_practica (
            id_producto      NUMBER(10)    GENERATED ALWAYS AS IDENTITY CONSTRAINT pk_producto_practica PRIMARY KEY,
            nombre           VARCHAR2(80 CHAR) NOT NULL,
            precio_unitario  NUMBER(12,2)  NOT NULL,
            utilidad         NUMBER(5,2)   NOT NULL,
            precio_final     NUMBER(12,2))';
        DBMS_OUTPUT.PUT_LINE('Tabla PRODUCTO_PRACTICA creada.');
    END IF;
END;
/

CREATE OR REPLACE TRIGGER trg_producto_precio_final
BEFORE INSERT OR UPDATE OF precio_unitario, utilidad, precio_final ON producto_practica
FOR EACH ROW
BEGIN
    IF :NEW.precio_unitario IS NULL OR :NEW.precio_unitario < 0 THEN
        RAISE_APPLICATION_ERROR(-20301, 'El precio unitario debe ser cero o positivo.');
    END IF;
    IF :NEW.utilidad IS NULL OR :NEW.utilidad < 0 OR :NEW.utilidad > 500 THEN
        RAISE_APPLICATION_ERROR(-20302, 'La utilidad debe estar entre 0 % y 500 %.');
    END IF;
    :NEW.precio_final := ROUND(:NEW.precio_unitario * (1 + :NEW.utilidad / 100), 2);
    IF UPDATING THEN
        DBMS_OUTPUT.PUT_LINE('Producto ' || :OLD.id_producto || ': precio final ' || :OLD.precio_final
                             || ' -> ' || :NEW.precio_final);
    END IF;
END;
/
SHOW ERRORS TRIGGER trg_producto_precio_final

PROMPT ==== Caso 1 y 2: INSERT 1000 + 20 % = 1200 ; 1500 + 20 % = 1800 ====
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Cuaderno', 1000, 20);
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Lapicero', 1500, 20);
SELECT id_producto, nombre, precio_unitario, utilidad, precio_final FROM producto_practica ORDER BY id_producto;

PROMPT ==== Caso 3: el usuario intenta escribir un precio final falso -> el trigger lo recalcula ====
INSERT INTO producto_practica (nombre, precio_unitario, utilidad, precio_final) VALUES ('Borrador', 800, 25, 1);
SELECT nombre, precio_final FROM producto_practica WHERE nombre = 'Borrador';

PROMPT ==== Caso 3b: escribir un precio final falso con UPDATE -> también se recalcula (sigue en 1000) ====
UPDATE producto_practica SET precio_final = 1 WHERE nombre = 'Borrador';
SELECT nombre, precio_final FROM producto_practica WHERE nombre = 'Borrador';

PROMPT ==== Caso 4: cambiar SOLO el precio unitario (1000 -> 2000, 20 %) = 2400 ====
UPDATE producto_practica SET precio_unitario = 2000 WHERE nombre = 'Cuaderno';
PROMPT ==== Caso 5: cambiar SOLO la utilidad (1500, 20 % -> 30 %) = 1950 ====
UPDATE producto_practica SET utilidad = 30 WHERE nombre = 'Lapicero';
PROMPT ==== Caso 6: cambiar LOS DOS (Borrador: 900 y 10 %) = 990 ====
UPDATE producto_practica SET precio_unitario = 900, utilidad = 10 WHERE nombre = 'Borrador';
PROMPT ==== Caso 7: cambiar solo el nombre NO dispara el trigger (no imprime línea) ====
UPDATE producto_practica SET nombre = 'Borrador blanco' WHERE nombre = 'Borrador';
SELECT nombre, precio_unitario, utilidad, precio_final FROM producto_practica ORDER BY id_producto;

PROMPT ==== Caso 8: redondeo: 999,99 + 12,5 % = 1124,99 (1124,98875 redondeado) ====
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Regla', 999.99, 12.5);
SELECT nombre, precio_final FROM producto_practica WHERE nombre = 'Regla';

PROMPT ==== Caso 9 (debe fallar ORA-20301): precio negativo ====
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Error', -5, 20);
PROMPT ==== Caso 10 (debe fallar ORA-20301): precio nulo ====
INSERT INTO producto_practica (nombre, precio_unitario, utilidad) VALUES ('Error', NULL, 20);
PROMPT ==== Caso 11 (debe fallar ORA-20302): utilidad negativa ====
UPDATE producto_practica SET utilidad = -1 WHERE nombre = 'Lapicero';

ROLLBACK;
PROMPT Se hizo ROLLBACK: la práctica se puede repetir.
