-- Reversión de V032 (EcoAccess Challenge: servicios REST y paquete). Retira el módulo ORDS y el paquete PKG_ECO_JUEGO.
-- Las tablas y los datos del concurso se conservan (se retiran con R031). No toca la habilitación de ORDS del esquema.
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
BEGIN
    BEGIN ORDS.DELETE_MODULE(p_module_name => 'eco.juego.v1'); DBMS_OUTPUT.PUT_LINE('R032: módulo REST eco.juego.v1 retirado');
    EXCEPTION WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('R032: el módulo REST no existía'); END;
    SELECT COUNT(*) INTO n FROM user_objects WHERE object_name = 'PKG_ECO_JUEGO' AND object_type = 'PACKAGE';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DROP PACKAGE pkg_eco_juego'; DBMS_OUTPUT.PUT_LINE('R032: paquete borrado'); END IF;
    COMMIT;
END;
/
PROMPT R032_JUEGO_PAQUETE_ORDS_REVERTIDA
