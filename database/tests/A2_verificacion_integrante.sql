-- =====================================================================
-- EcoAccess · verificación para un INTEGRANTE (cuenta ECO_DEV_*, rol CONSULTA o EDICION) — SOLO LECTURA
-- Escenario A de docs/database/14-guia-companeros.md: cada integrante entra con su propia cuenta y lee las
-- tablas del dueño ECOACCESS_P3_APP. No crea, cambia ni borra nada.
-- Ejecutar en SQL Developer con F5 (Ejecutar script).
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;
DECLARE
    v_ok NUMBER := 0; v_fallo NUMBER := 0; n NUMBER;
    PROCEDURE ok(p VARCHAR2, c BOOLEAN) IS
    BEGIN
        IF c THEN v_ok := v_ok + 1; DBMS_OUTPUT.PUT_LINE('OK    ' || p);
        ELSE v_fallo := v_fallo + 1; DBMS_OUTPUT.PUT_LINE('FALLO ' || p); END IF;
    END;
BEGIN
    DBMS_OUTPUT.PUT_LINE('Conectado como ' || SYS_CONTEXT('USERENV', 'SESSION_USER') || ', leyendo el esquema ' || SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'));
    ok('I1 la cuenta personal no es la del dueño ni administrativa',
       SYS_CONTEXT('USERENV', 'SESSION_USER') NOT IN ('ECOACCESS_P3_APP', 'ADMIN', 'SYS', 'SYSTEM'));
    SELECT COUNT(*) INTO n FROM session_roles WHERE role IN ('ECOACCESS_ROL_CONSULTA', 'ECOACCESS_ROL_EDICION');
    ok('I2 tiene el rol ECOACCESS_ROL_CONSULTA o ECOACCESS_ROL_EDICION', n >= 1);
    SELECT COUNT(*) INTO n FROM all_objects WHERE owner = 'ECOACCESS_P3_APP' AND object_type = 'TABLE';
    ok('I3 ve las tablas de EcoAccess (' || n || ')', n >= 30);
    SELECT COUNT(*) INTO n FROM all_objects WHERE owner = 'ECOACCESS_P3_APP' AND object_name LIKE 'VW\_%' ESCAPE '\' AND object_type = 'VIEW';
    ok('I4 ve las nueve vistas VW_* (' || n || ')', n = 9);
    SELECT COUNT(*) INTO n FROM all_objects WHERE owner = 'ECOACCESS_P3_APP' AND status <> 'VALID';
    ok('I5 ningún objeto visible está inválido', n = 0);
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM vw_caneca_estacion WHERE es_sintetico = 0' INTO n;
    ok('I6 consulta VW_CANECA_ESTACION: ' || n || ' canecas reales', n >= 3);
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM vw_bi_depositos' INTO n;
    ok('I7 consulta VW_BI_DEPOSITOS: ' || n || ' depósitos', n >= 0);
    DBMS_OUTPUT.PUT_LINE('RESULTADO INTEGRANTE: ' || v_ok || ' correctas, ' || v_fallo || ' fallidas.');
END;
/
