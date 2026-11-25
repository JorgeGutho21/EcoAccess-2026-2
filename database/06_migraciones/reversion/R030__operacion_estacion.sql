-- Reversión de V030 (OPERACION_ESTACION). Borra SOLO lo que creó V030: la vista y la tabla con sus índices.
-- Antes de borrar, exporta las filas si se quieren conservar:
--   SELECT * FROM operacion_estacion ORDER BY fecha_utc;   (SQL Developer → Exportar → CSV)
-- La aplicación sigue funcionando sin la tabla: las operaciones quedan en la cola local pendiente de sincronización.
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_views WHERE view_name = 'EA_R_OPERACIONES_ESTACION';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DROP VIEW ea_r_operaciones_estacion'; DBMS_OUTPUT.PUT_LINE('R030: vista borrada'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'OPERACION_ESTACION';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DROP TABLE operacion_estacion PURGE'; DBMS_OUTPUT.PUT_LINE('R030: tabla borrada'); END IF;
END;
/
PROMPT R030_OPERACION_ESTACION_REVERTIDA
