-- =====================================================================
-- EcoAccess · Prueba C — compilación (SOLO LECTURA)
-- Lista los objetos inválidos y sus errores ORA-/PLS- exactos. No recompila nada.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
COLUMN object_name FORMAT A36
COLUMN object_type FORMAT A16
COLUMN texto       FORMAT A100
SELECT object_type, object_name, status FROM user_objects WHERE status <> 'VALID' ORDER BY 1, 2;
SELECT type AS object_type, name AS object_name, line, position, attribute, SUBSTR(text, 1, 100) AS texto
FROM   user_errors ORDER BY name, type, sequence;
DECLARE
    v_inv NUMBER; v_err NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_inv FROM user_objects WHERE status <> 'VALID';
    SELECT COUNT(*) INTO v_err FROM user_errors;
    DBMS_OUTPUT.PUT_LINE(CASE WHEN v_inv = 0 THEN 'OK    ' ELSE 'FALLO ' END || 'C1 objetos inválidos: ' || v_inv);
    DBMS_OUTPUT.PUT_LINE(CASE WHEN v_err = 0 THEN 'OK    ' ELSE 'FALLO ' END || 'C2 errores de compilación (USER_ERRORS): ' || v_err);
    DBMS_OUTPUT.PUT_LINE('RESULTADO C: ' || (CASE WHEN v_inv = 0 THEN 1 ELSE 0 END + CASE WHEN v_err = 0 THEN 1 ELSE 0 END)
        || ' correctas, ' || (CASE WHEN v_inv = 0 THEN 0 ELSE 1 END + CASE WHEN v_err = 0 THEN 0 ELSE 1 END) || ' fallidas.');
END;
/
