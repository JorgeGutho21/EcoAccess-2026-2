-- Reversión de V033. Borra SOLO lo que creó V033: la vista de estadísticas, la tabla de detalle y las 4 columnas
-- añadidas a OPERACION_ESTACION (con sus 3 restricciones). Las filas de OPERACION_ESTACION (V030) se conservan.
-- Antes, exporta el detalle si se quiere conservar:
--   SELECT * FROM operacion_estacion_detalle ORDER BY clave_operacion, orden;   (SQL Developer → Exportar → CSV)
-- La aplicación detecta la ausencia de V033 y sigue registrando solo la cabecera (como con V030).
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
    PROCEDURE si_existe(p_sql VARCHAR2, p_ddl VARCHAR2) IS c NUMBER; BEGIN EXECUTE IMMEDIATE p_sql INTO c; IF c > 0 THEN EXECUTE IMMEDIATE p_ddl; DBMS_OUTPUT.PUT_LINE('R033: ' || p_ddl); END IF; END;
BEGIN
    si_existe(q'[SELECT COUNT(*) FROM user_views WHERE view_name = 'EA_R_ESTADISTICAS_ESTACION']', 'DROP VIEW ea_r_estadisticas_estacion');
    si_existe(q'[SELECT COUNT(*) FROM user_tables WHERE table_name = 'OPERACION_ESTACION_DETALLE']', 'DROP TABLE operacion_estacion_detalle PURGE');
    si_existe(q'[SELECT COUNT(*) FROM user_constraints WHERE constraint_name = 'CK_OPEST_TIPO_USUARIO']', 'ALTER TABLE operacion_estacion DROP CONSTRAINT ck_opest_tipo_usuario');
    si_existe(q'[SELECT COUNT(*) FROM user_constraints WHERE constraint_name = 'CK_OPEST_METODO']', 'ALTER TABLE operacion_estacion DROP CONSTRAINT ck_opest_metodo');
    si_existe(q'[SELECT COUNT(*) FROM user_constraints WHERE constraint_name = 'CK_OPEST_CONFIANZA']', 'ALTER TABLE operacion_estacion DROP CONSTRAINT ck_opest_confianza');
    si_existe(q'[SELECT COUNT(*) FROM user_tab_columns WHERE table_name = 'OPERACION_ESTACION' AND column_name = 'TIPO_USUARIO']', 'ALTER TABLE operacion_estacion DROP COLUMN tipo_usuario');
    si_existe(q'[SELECT COUNT(*) FROM user_tab_columns WHERE table_name = 'OPERACION_ESTACION' AND column_name = 'METODO_RECONOCIMIENTO']', 'ALTER TABLE operacion_estacion DROP COLUMN metodo_reconocimiento');
    si_existe(q'[SELECT COUNT(*) FROM user_tab_columns WHERE table_name = 'OPERACION_ESTACION' AND column_name = 'CONFIANZA']', 'ALTER TABLE operacion_estacion DROP COLUMN confianza');
    si_existe(q'[SELECT COUNT(*) FROM user_tab_columns WHERE table_name = 'OPERACION_ESTACION' AND column_name = 'SESION_ANONIMA']', 'ALTER TABLE operacion_estacion DROP COLUMN sesion_anonima');
END;
/
PROMPT R033_OPERACION_ESTACION_DETALLE_REVERTIDA
