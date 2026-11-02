-- Solo columnas opcionales: no modifica nombre, categoría, descripción ni admisión de registros existentes.
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name='REGLA_ECOPUNTOS' AND column_name='POR_UNIDAD';
    IF n<>1 THEN RAISE_APPLICATION_ERROR(-20121,'V012 requiere V011.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name='MATERIAL' AND column_name IN ('SINONIMOS','PRECAUCIONES','RUTA_SEPARACION');
    IF n<>0 THEN RAISE_APPLICATION_ERROR(-20122,'V012 aplicada o parcial; revisar antes de continuar.'); END IF;
END;
/
ALTER TABLE material ADD (sinonimos VARCHAR2(300 CHAR), precauciones VARCHAR2(500 CHAR), ruta_separacion VARCHAR2(150 CHAR));
COMMENT ON COLUMN material.sinonimos IS 'Nombres de búsqueda, sin convertir plásticos genéricos en PET.';
COMMENT ON COLUMN material.precauciones IS 'Orientación de manejo; no sustituye las instrucciones del gestor o de la EPS/IPS.';
COMMENT ON COLUMN material.ruta_separacion IS 'Destino educativo propuesto, no evidencia de gestión realizada.';
PROMPT V012_ORIENTACION_CATALOGO_APLICADA
