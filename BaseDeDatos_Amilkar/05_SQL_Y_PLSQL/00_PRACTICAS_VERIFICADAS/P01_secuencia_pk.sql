-- =====================================================================
-- Práctica P01 · Clave primaria SUBROGADA con SEQUENCE + TRIGGER
-- Conexión: ECOACCESS_PRACTICAS (F5). Se puede repetir.
--
-- Tarea: «trigger que asigne automáticamente una clave única al insertar».
-- Tabla académica propia BD_EVENTO_ACADEMICO (sin IDENTITY, a propósito):
--   1. SEQ_BD_EVENTO_ACADEMICO genera 1, 2, 3, ...
--   2. TRG_BD_EVENTO_ACADEMICO_ID, BEFORE INSERT FOR EACH ROW, pone
--      :NEW.id_evento := secuencia.NEXTVAL cuando llega NULL.
-- ¿Por qué BEFORE? Porque solo antes de guardar la fila se puede escribir :NEW.
-- ¿Qué es una clave SUBROGADA? Un número sin significado de negocio que solo
-- identifica la fila (el «nombre» del evento podría repetirse o cambiar).
-- =====================================================================
SET SERVEROUTPUT ON
SET LINESIZE 160
COLUMN nombre FORMAT A40

DECLARE
    v NUMBER;
BEGIN
    SELECT COUNT(*) INTO v FROM user_tables WHERE table_name = 'BD_EVENTO_ACADEMICO';
    IF v = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLE bd_evento_academico (
            id_evento     NUMBER(10)          CONSTRAINT pk_bd_evento_academico PRIMARY KEY,
            nombre        VARCHAR2(100 CHAR)  NOT NULL,
            fecha_evento  DATE                DEFAULT SYSDATE NOT NULL,
            CONSTRAINT ck_bd_evento_nombre CHECK (TRIM(nombre) IS NOT NULL))';
        EXECUTE IMMEDIATE 'CREATE SEQUENCE seq_bd_evento_academico START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE';
        DBMS_OUTPUT.PUT_LINE('Tabla y secuencia creadas.');
    END IF;
END;
/

CREATE OR REPLACE TRIGGER trg_bd_evento_academico_id
BEFORE INSERT ON bd_evento_academico
FOR EACH ROW
WHEN (NEW.id_evento IS NULL)          -- en WHEN se escribe NEW sin dos puntos
BEGIN
    :NEW.id_evento := seq_bd_evento_academico.NEXTVAL;   -- dentro del bloque, con dos puntos
END;
/
SHOW ERRORS TRIGGER trg_bd_evento_academico_id

PROMPT ==== Prueba 1: tres INSERT sin id -> el trigger pone números consecutivos ====
INSERT INTO bd_evento_academico (nombre) VALUES ('Clase de disparadores');
INSERT INTO bd_evento_academico (nombre) VALUES ('Sustentación EcoAccess');
INSERT INTO bd_evento_academico (nombre) VALUES ('Taller de PL/SQL');
SELECT id_evento, nombre FROM bd_evento_academico ORDER BY id_evento;

PROMPT ==== Prueba 2: unicidad — ningún id repetido ====
SELECT COUNT(*) AS filas, COUNT(DISTINCT id_evento) AS ids_distintos FROM bd_evento_academico;

PROMPT ==== Prueba 3: HUECOS — un ROLLBACK no devuelve el número a la secuencia ====
SAVEPOINT antes_hueco;
INSERT INTO bd_evento_academico (nombre) VALUES ('Evento que se deshará');
ROLLBACK TO antes_hueco;
INSERT INTO bd_evento_academico (nombre) VALUES ('Evento después del rollback');
SELECT id_evento, nombre FROM bd_evento_academico ORDER BY id_evento;
PROMPT  -> El número del evento deshecho se «perdió»: una secuencia garantiza números ÚNICOS,
PROMPT     no CONSECUTIVOS. Dos sesiones simultáneas nunca reciben el mismo NEXTVAL (concurrencia).

PROMPT ==== Prueba 4: si se da un id a mano, el trigger no lo cambia (WHEN NEW.id_evento IS NULL) ====
INSERT INTO bd_evento_academico (id_evento, nombre) VALUES (1000, 'Id puesto a mano');
SELECT id_evento, nombre FROM bd_evento_academico WHERE id_evento = 1000;

PROMPT ==== Prueba 5 (debe fallar con ORA-00001): repetir un id existente a mano ====
INSERT INTO bd_evento_academico (id_evento, nombre) VALUES (1000, 'Repetido');

ROLLBACK;
PROMPT Se hizo ROLLBACK: la tabla queda vacía para repetir la práctica (la secuencia sigue avanzando).
