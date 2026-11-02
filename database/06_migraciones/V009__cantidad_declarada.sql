-- Cantidad declarada, sin convertir declaraciones en detecciones o puntos.
-- Ejecutar como dueño tras V008. Las filas históricas conservan NULL: no se inventa una unidad.
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'ANALISIS_IMAGEN';
    IF n <> 1 THEN RAISE_APPLICATION_ERROR(-20091, 'V009 requiere V008.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name = 'INTERACCION' AND column_name = 'CANTIDAD_DECLARADA';
    IF n <> 0 THEN RAISE_APPLICATION_ERROR(-20092, 'V009 ya aplicada; no se modificó nada.'); END IF;
END;
/
ALTER TABLE interaccion ADD (cantidad_declarada NUMBER(4));
ALTER TABLE interaccion ADD CONSTRAINT ck_interaccion_cantidad CHECK
    (cantidad_declarada IS NULL OR (cantidad_declarada BETWEEN 1 AND 1000 AND cantidad_declarada = TRUNC(cantidad_declarada)));
COMMENT ON COLUMN interaccion.cantidad_declarada IS 'Unidades declaradas, sin verificación física. NULL = no capturadas. No determina ecopuntos.';
PROMPT V009_CANTIDAD_DECLARADA_APLICADA
