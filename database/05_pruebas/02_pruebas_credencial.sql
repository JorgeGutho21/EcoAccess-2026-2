-- =====================================================================
-- EcoAccess P3 — pruebas de restricciones de CREDENCIAL_ACCESO (migración V002)
-- Archivo: database/05_pruebas/02_pruebas_credencial.sql
--
-- Mismo formato que 01_pruebas_restricciones.sql: cada caso imprime PASA o
-- FALLA y al final se hace ROLLBACK. Requiere la versión 001 y la V002.
-- Errores esperados: ORA-00001, ORA-01400, ORA-02290, ORA-02291 y ORA-02292.
-- =====================================================================

SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_pasan       PLS_INTEGER := 0;
    v_fallan      PLS_INTEGER := 0;
    v_id_usuario  NUMBER;
    v_hash  CONSTANT VARCHAR2(64) := RPAD('AB', 64, 'CD');
    v_sal   CONSTANT VARCHAR2(32) := RPAD('01', 32, '23');

    PROCEDURE registrar(p_pasa BOOLEAN, p_caso VARCHAR2, p_detalle VARCHAR2 := NULL) IS
    BEGIN
        IF p_pasa THEN
            v_pasan := v_pasan + 1;
            DBMS_OUTPUT.PUT_LINE('PASA   ' || p_caso);
        ELSE
            v_fallan := v_fallan + 1;
            DBMS_OUTPUT.PUT_LINE('FALLA  ' || p_caso || ' -> ' || p_detalle);
        END IF;
    END;

    PROCEDURE esperar_error(p_caso VARCHAR2, p_sql VARCHAR2, p_codigo NUMBER) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
        registrar(FALSE, p_caso, 'Oracle aceptó un dato inválido');
    EXCEPTION
        WHEN OTHERS THEN
            registrar(SQLCODE = p_codigo, p_caso, 'error inesperado: ' || SQLERRM);
    END;

    -- Arma un INSERT con los valores indicados; los demás quedan válidos.
    FUNCTION insertar(p_id VARCHAR2, p_hash VARCHAR2 := NULL, p_sal VARCHAR2 := NULL,
                      p_iter VARCHAR2 := '600000', p_intentos VARCHAR2 := '0') RETURN VARCHAR2 IS
    BEGIN
        RETURN 'INSERT INTO credencial_acceso (id_usuario, hash_contrasena, sal, iteraciones, intentos_fallidos, fecha_cambio_utc) VALUES ('
            || p_id || ', ' || NVL(p_hash, 'HEXTORAW(''' || v_hash || ''')') || ', '
            || NVL(p_sal, 'HEXTORAW(''' || v_sal || ''')') || ', ' || p_iter || ', ' || p_intentos
            || ', SYS_EXTRACT_UTC(SYSTIMESTAMP))';
    END;
BEGIN
    INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
    VALUES ('Prueba Credencial', 'prueba.credencial@example.com', SYS_EXTRACT_UTC(SYSTIMESTAMP))
    RETURNING id_usuario INTO v_id_usuario;

    esperar_error('hash de 31 bytes',      insertar(v_id_usuario, p_hash => 'HEXTORAW(''' || SUBSTR(v_hash, 3) || ''')'), -2290);
    esperar_error('sal de 15 bytes',       insertar(v_id_usuario, p_sal  => 'HEXTORAW(''' || SUBSTR(v_sal, 3) || ''')'), -2290);
    esperar_error('sin hash',              insertar(v_id_usuario, p_hash => 'NULL'), -1400);
    esperar_error('99 999 iteraciones',    insertar(v_id_usuario, p_iter => '99999'), -2290);
    esperar_error('intentos negativos',    insertar(v_id_usuario, p_intentos => '-1'), -2290);
    esperar_error('cuenta inexistente',    insertar('-1'), -2291);

    EXECUTE IMMEDIATE insertar(v_id_usuario);
    registrar(TRUE, 'credencial válida de 600 000 iteraciones se guarda');
    esperar_error('segunda credencial de la misma cuenta', insertar(v_id_usuario), -1);
    esperar_error('borrar una cuenta que tiene contraseña',
        'DELETE FROM usuario WHERE id_usuario = ' || v_id_usuario, -2292);

    ROLLBACK;

    DBMS_OUTPUT.PUT_LINE('-----');
    DBMS_OUTPUT.PUT_LINE('Resultado: ' || v_pasan || ' pasan, ' || v_fallan || ' fallan. Se hizo ROLLBACK de todos los datos de prueba.');

    IF v_fallan > 0 THEN
        RAISE_APPLICATION_ERROR(-20021, v_fallan || ' pruebas de credencial fallaron.');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('Prueba interrumpida; se hizo ROLLBACK. ' || SQLERRM);
        RAISE;
END;
/

WHENEVER SQLERROR CONTINUE
