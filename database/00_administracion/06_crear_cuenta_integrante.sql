-- =====================================================================
-- EcoAccess P3 — cuenta personal de un integrante del equipo
-- Archivo: database/00_administracion/06_crear_cuenta_integrante.sql
--
-- Ejecutar como ADMIN, una vez por integrante (tres en total). SQL Developer
-- pedirá en «Enter Binds»:
--   :usuario_integrante  por ejemplo ECO_DEV_JORGE (prefijo ECO_DEV_ obligatorio)
--   :clave_integrante    temporal; la cuenta queda con la contraseña EXPIRADA y
--                        el integrante elige la suya en el primer inicio.
--   :rol_integrante      CONSULTA o EDICION
--
-- Cada persona entra con su propia cuenta (trazabilidad) y trabaja sobre las
-- tablas del dueño con:  ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;
-- Nadie recibe ADMIN ni la contraseña de ECOACCESS_P3_APP por esta vía.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_usuario VARCHAR2(128) := UPPER(TRIM(:usuario_integrante));
    v_clave   VARCHAR2(128) := :clave_integrante;
    v_rol     VARCHAR2(30)  := 'ECOACCESS_ROL_' || UPPER(TRIM(:rol_integrante));
    v_existe  NUMBER;
BEGIN
    IF SYS_CONTEXT('USERENV', 'SESSION_USER') <> 'ADMIN' THEN
        RAISE_APPLICATION_ERROR(-20121, 'Este script se ejecuta como ADMIN.');
    END IF;
    IF NOT REGEXP_LIKE(v_usuario, '^ECO_DEV_[A-Z][A-Z0-9_]{1,20}$') THEN
        RAISE_APPLICATION_ERROR(-20122, 'El usuario debe empezar por ECO_DEV_ y usar solo letras, números y guion bajo.');
    END IF;
    IF v_rol NOT IN ('ECOACCESS_ROL_CONSULTA', 'ECOACCESS_ROL_EDICION') THEN
        RAISE_APPLICATION_ERROR(-20123, 'El rol debe ser CONSULTA o EDICION.');
    END IF;

    SELECT COUNT(*) INTO v_existe FROM dba_users WHERE username = v_usuario;
    IF v_existe = 0 THEN
        IF v_clave IS NULL OR LENGTH(v_clave) NOT BETWEEN 12 AND 30 OR INSTR(v_clave, '"') > 0
           OR NOT REGEXP_LIKE(v_clave, '[A-Z]') OR NOT REGEXP_LIKE(v_clave, '[a-z]') OR NOT REGEXP_LIKE(v_clave, '[0-9]') THEN
            RAISE_APPLICATION_ERROR(-20124, 'Contraseña temporal no admitida: 12 a 30 caracteres con mayúscula, minúscula y número.');
        END IF;
        EXECUTE IMMEDIATE 'CREATE USER ' || v_usuario || ' IDENTIFIED BY "' || v_clave || '" PASSWORD EXPIRE';
        DBMS_OUTPUT.PUT_LINE('Cuenta creada (contraseña expirada): ' || v_usuario);
    ELSE
        DBMS_OUTPUT.PUT_LINE(v_usuario || ' ya existía: no se cambió su contraseña.');
    END IF;
    v_clave := NULL;

    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO ' || v_usuario;
    EXECUTE IMMEDIATE 'GRANT ' || v_rol || ' TO ' || v_usuario;
    DBMS_OUTPUT.PUT_LINE('Concedido ' || v_rol || ' a ' || v_usuario || '.');
END;
/
