-- Reversión de V031 (EcoAccess Challenge). Borra SOLO lo que creó V031: la vista y las tablas ECO_JUEGO_*.
-- Ejecutar primero R032. Antes de borrar, exporta los resultados si se quieren conservar (panel → Exportar CSV, o
--   SELECT * FROM vw_juego_clasificacion;). No afecta usuarios, invitados, materiales ni operaciones de EcoAccess.
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
    PROCEDURE borrar(p_tabla VARCHAR2) IS
    BEGIN
        SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = p_tabla;
        IF n = 1 THEN EXECUTE IMMEDIATE 'DROP TABLE ' || p_tabla || ' PURGE'; DBMS_OUTPUT.PUT_LINE('R031: tabla ' || p_tabla || ' borrada'); END IF;
    END;
BEGIN
    SELECT COUNT(*) INTO n FROM user_views WHERE view_name = 'VW_JUEGO_CLASIFICACION';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DROP VIEW vw_juego_clasificacion'; END IF;
    borrar('ECO_JUEGO_RESPUESTAS'); borrar('ECO_JUEGO_INTENTOS'); borrar('ECO_JUEGO_PARTICIPANTES');
    borrar('ECO_JUEGO_OPCIONES'); borrar('ECO_JUEGO_PREGUNTAS'); borrar('ECO_JUEGO_COMPETENCIAS');
    borrar('ECO_JUEGO_CONFIGURACION'); borrar('ECO_JUEGO_LIMITES');
END;
/
PROMPT R031_JUEGO_ECOACCESS_CHALLENGE_REVERTIDA
