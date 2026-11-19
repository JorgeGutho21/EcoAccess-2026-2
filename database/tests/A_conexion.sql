-- =====================================================================
-- EcoAccess · Prueba A — conexión (SOLO LECTURA)
-- Comprueba con quién, en qué esquema y en qué servicio quedó conectada la sesión, y que no es una cuenta
-- administrativa. No muestra contraseñas ni la cadena de conexión.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
COLUMN usuario   FORMAT A24
COLUMN esquema   FORMAT A24
COLUMN servicio  FORMAT A48
COLUMN base      FORMAT A28
SELECT SYS_CONTEXT('USERENV', 'SESSION_USER')   AS usuario,
       SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') AS esquema,
       SYS_CONTEXT('USERENV', 'SERVICE_NAME')   AS servicio,
       SYS_CONTEXT('USERENV', 'DB_NAME')        AS base
FROM   dual;

DECLARE
    v_ok NUMBER := 0; v_fallo NUMBER := 0; n NUMBER; v_version VARCHAR2(40);
    PROCEDURE ok(p VARCHAR2, c BOOLEAN) IS
    BEGIN
        IF c THEN v_ok := v_ok + 1; DBMS_OUTPUT.PUT_LINE('OK    ' || p);
        ELSE v_fallo := v_fallo + 1; DBMS_OUTPUT.PUT_LINE('FALLO ' || p); END IF;
    END;
BEGIN
    SELECT MAX(version) INTO v_version FROM product_component_version;
    DBMS_OUTPUT.PUT_LINE('Oracle ' || v_version);
    ok('A1 la sesión está abierta', SYS_CONTEXT('USERENV', 'SESSION_USER') IS NOT NULL);
    ok('A2 no es una cuenta administrativa (SYS, SYSTEM, ADMIN)', SYS_CONTEXT('USERENV', 'SESSION_USER') NOT IN ('SYS', 'SYSTEM', 'ADMIN'));
    ok('A3 el esquema actual es el del usuario (sin ALTER SESSION SET CURRENT_SCHEMA)',
       SYS_CONTEXT('USERENV', 'SESSION_USER') = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'));
    SELECT COUNT(*) INTO n FROM session_privs WHERE privilege IN ('CREATE SESSION');
    ok('A4 tiene CREATE SESSION', n = 1);
    SELECT COUNT(*) INTO n FROM session_privs WHERE privilege LIKE '%ANY%' OR privilege IN ('DBA', 'CREATE USER', 'DROP USER', 'ALTER SYSTEM');
    ok('A5 no tiene privilegios de sistema «ANY» ni de administración (mínimo privilegio)', n = 0);
    DBMS_OUTPUT.PUT_LINE('RESULTADO A: ' || v_ok || ' correctas, ' || v_fallo || ' fallidas.');
END;
/
