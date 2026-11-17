-- Reversión de V023. Solo quita lo que V023 creó: la vista EA_R_CONSULTAS_VISION y la tabla CONSULTA_VISION (con sus
-- índices y restricciones) y su fila en ECO_MIGRACION_APLICADA. No toca usuarios, materiales, canecas, depósitos,
-- ecopuntos ni vinculaciones: ninguna tabla existente depende de CONSULTA_VISION.
--
-- Antes de ejecutarla en Oracle Cloud, exportar las consultas si se quieren conservar (ver
-- docs/base_datos/V023_CONSULTAS_VISION.md, «Respaldo»). Repetible: lo que ya no existe se omite.
-- Este script no está en la carpeta de migraciones a propósito: ningún instalador lo ejecuta solo.
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_views WHERE view_name = 'EA_R_CONSULTAS_VISION';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DROP VIEW ea_r_consultas_vision'; DBMS_OUTPUT.PUT_LINE('R023: vista quitada'); END IF;
    -- Ninguna otra tabla debe referenciar CONSULTA_VISION; si alguien agregó una, se detiene sin borrar.
    SELECT COUNT(*) INTO n FROM user_constraints r JOIN user_constraints p ON p.constraint_name = r.r_constraint_name
     WHERE r.constraint_type = 'R' AND p.table_name = 'CONSULTA_VISION' AND r.table_name <> 'CONSULTA_VISION';
    IF n > 0 THEN RAISE_APPLICATION_ERROR(-20234, 'Otra tabla referencia CONSULTA_VISION; no se borra.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'CONSULTA_VISION';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DROP TABLE consulta_vision PURGE'; DBMS_OUTPUT.PUT_LINE('R023: tabla quitada'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'ECO_MIGRACION_APLICADA';
    IF n = 1 THEN
        EXECUTE IMMEDIATE 'DELETE FROM eco_migracion_aplicada WHERE version = ''V023''';
        DBMS_OUTPUT.PUT_LINE('R023: registro de versión quitado (' || SQL%ROWCOUNT || ')');
        COMMIT;
    END IF;
END;
/
PROMPT R023_CONSULTA_VISION_REVERTIDA
