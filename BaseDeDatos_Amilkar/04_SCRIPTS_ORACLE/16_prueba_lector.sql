-- =====================================================================
-- EcoAccess BD — 16 · Prueba de la cuenta de SOLO LECTURA (Power BI / C#)
-- Conexión: ECOACCESS_BD_LECTOR. Se puede repetir; no modifica nada.
-- Qué demuestra: el lector VE las vistas preparadas y NO puede leer ni
-- modificar las tablas con datos personales (mínimo privilegio).
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK OFF
DECLARE
    n_ok  PLS_INTEGER := 0;
    n_mal PLS_INTEGER := 0;
    n     NUMBER;
    e     NUMBER;
    PROCEDURE ok(p_nombre VARCHAR2, p_cond BOOLEAN, p_detalle VARCHAR2 DEFAULT NULL) IS
    BEGIN
        IF p_cond THEN n_ok := n_ok + 1; DBMS_OUTPUT.PUT_LINE('PASA  ' || p_nombre || ' ' || p_detalle);
        ELSE n_mal := n_mal + 1; DBMS_OUTPUT.PUT_LINE('FALLA ' || p_nombre || ' ' || p_detalle); END IF;
    END;
    FUNCTION error_de(p_sql VARCHAR2) RETURN NUMBER IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
        RETURN 0;
    EXCEPTION WHEN OTHERS THEN RETURN SQLCODE;
    END;
BEGIN
    ok('L01 La sesión es la del lector', USER = 'ECOACCESS_BD_LECTOR', '(' || USER || ')');
    SELECT COUNT(*) INTO n FROM ecoaccess_bd.vw_bi_depositos;
    ok('L02 Lee VW_BI_DEPOSITOS', n > 0, '(' || n || ' filas)');
    SELECT COUNT(*) INTO n FROM ecoaccess_bd.vw_bi_consultas_vision;
    ok('L03 Lee VW_BI_CONSULTAS_VISION', n > 0, '(' || n || ' filas)');
    SELECT COUNT(*) INTO n FROM all_views WHERE owner = 'ECOACCESS_BD';
    ok('L04 Ve exactamente 6 vistas del esquema', n = 6, '(' || n || ')');
    SELECT COUNT(*) INTO n FROM all_tables WHERE owner = 'ECOACCESS_BD';
    ok('L05 No ve ninguna tabla del esquema', n = 0, '(' || n || ')');
    e := error_de('SELECT COUNT(*) FROM ecoaccess_bd.usuario');
    ok('L06 No puede leer USUARIO (ORA-00942)', e = -942, '(SQLCODE ' || e || ')');
    e := error_de('SELECT COUNT(*) FROM ecoaccess_bd.credencial_acceso');
    ok('L07 No puede leer CREDENCIAL_ACCESO (ORA-00942)', e = -942, '(SQLCODE ' || e || ')');
    e := error_de('UPDATE ecoaccess_bd.vw_bi_depositos SET estado = ''X''');
    ok('L08 No puede modificar una vista', e <> 0, '(SQLCODE ' || e || ')');
    SELECT COUNT(*) INTO n FROM ecoaccess_bd.vw_bi_depositos WHERE ciudadano NOT LIKE 'C-____';
    ok('L09 Solo ve seudónimos C-0000, nunca nombres', n = 0, '(' || n || ' filas sin seudónimo)');
    SELECT COUNT(*) INTO n FROM session_privs WHERE privilege NOT IN ('CREATE SESSION');
    ok('L10 Su único privilegio de sistema es CREATE SESSION', n = 0, '(' || n || ' extra)');
    DBMS_OUTPUT.PUT_LINE('TOTAL LECTOR: ' || (n_ok + n_mal) || ' pruebas · PASAN: ' || n_ok || ' · FALLAN: ' || n_mal);
END;
/
