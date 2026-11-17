-- Reversión de V025. Solo quita lo que V025 creó: las vistas VISTA_DEPOSITOS_ESTADO y VISTA_LOTES_SIMULACION, las
-- tablas REGISTRO_SIMULACION y LOTE_SIMULACION, y la fila V025 en ECO_MIGRACION_APLICADA.
-- Se detiene sin borrar si existe algún lote: primero hay que decidir qué hacer con sus datos (no se borran aquí).
-- Este script no está en la carpeta de migraciones a propósito: ningún instalador lo ejecuta solo.
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'LOTE_SIMULACION';
    IF n = 1 THEN
        EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM lote_simulacion' INTO n;
        IF n > 0 THEN RAISE_APPLICATION_ERROR(-20254, 'Hay lotes de simulación registrados; no se revierte V025.'); END IF;
    END IF;
    FOR v IN (SELECT view_name FROM user_views WHERE view_name IN ('VISTA_DEPOSITOS_ESTADO', 'VISTA_LOTES_SIMULACION')) LOOP
        EXECUTE IMMEDIATE 'DROP VIEW ' || v.view_name;
    END LOOP;
    FOR t IN (SELECT table_name FROM user_tables WHERE table_name IN ('REGISTRO_SIMULACION', 'LOTE_SIMULACION') ORDER BY table_name DESC) LOOP
        EXECUTE IMMEDIATE 'DROP TABLE ' || t.table_name || ' PURGE';
    END LOOP;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'ECO_MIGRACION_APLICADA';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DELETE FROM eco_migracion_aplicada WHERE version = ''V025'''; END IF;
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('R025: registro de simulación quitado');
END;
/
