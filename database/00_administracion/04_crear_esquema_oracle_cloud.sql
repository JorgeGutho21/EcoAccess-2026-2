-- =====================================================================
-- EcoAccess P3 — esquema y roles en Oracle Cloud (Autonomous Database 19c)
-- Archivo: database/00_administracion/04_crear_esquema_oracle_cloud.sql
--
-- Ejecutar UNA vez como ADMIN de la base «EcoAccess» (nunca en SQLSelectLab).
--   * SQL Developer: abrir el archivo, F5 (Run Script). Pedirá el valor de
--     :clave_esquema en la ventana «Enter Binds»; no se guarda en el archivo.
--   * Instalador: dotnet run --project src/EcoAccess.P3.Instalador -- preparar-esquema
--     (pide la contraseña de ADMIN y la del esquema sin mostrarlas).
--
-- Crea, si no existen:
--   * ECOACCESS_P3_APP: dueño de las tablas y cuenta con la que se conectan
--     Windows Forms y el bot. NO es administradora: solo puede crear y usar
--     sus propios objetos (menor privilegio).
--   * ECOACCESS_ROL_CONSULTA: lectura de las tablas (SQL Developer, informes).
--   * ECOACCESS_ROL_EDICION: lectura y escritura sin borrar (pruebas manuales).
-- No cambia contraseñas existentes, no borra usuarios ni datos.
--
-- Diferencias con 01_crear_usuario_p3_cuerpo.sql (Oracle local, histórico):
--   * Autonomous Database no tiene el tablespace USERS: se usa DATA.
--   * Exige contraseñas de 12 a 30 caracteres con mayúscula, minúscula y
--     número, sin comillas dobles y sin contener el nombre del usuario.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    c_esquema CONSTANT VARCHAR2(30) := 'ECOACCESS_P3_APP';
    v_clave   VARCHAR2(128) := :clave_esquema;
    v_existe  NUMBER;

    PROCEDURE crear_rol(p_rol VARCHAR2) IS
        v_n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_n FROM dba_roles WHERE role = p_rol;
        IF v_n = 0 THEN
            EXECUTE IMMEDIATE 'CREATE ROLE ' || p_rol;
            DBMS_OUTPUT.PUT_LINE('Rol creado: ' || p_rol);
        ELSE
            DBMS_OUTPUT.PUT_LINE('Rol existente, se conserva: ' || p_rol);
        END IF;
    END;
BEGIN
    IF SYS_CONTEXT('USERENV', 'SESSION_USER') <> 'ADMIN' THEN
        RAISE_APPLICATION_ERROR(-20101, 'Este script se ejecuta como ADMIN de la base EcoAccess.');
    END IF;

    SELECT COUNT(*) INTO v_existe FROM dba_users WHERE username = c_esquema;
    IF v_existe = 0 THEN
        IF v_clave IS NULL OR LENGTH(v_clave) NOT BETWEEN 12 AND 30 OR INSTR(v_clave, '"') > 0
           OR NOT REGEXP_LIKE(v_clave, '[A-Z]') OR NOT REGEXP_LIKE(v_clave, '[a-z]') OR NOT REGEXP_LIKE(v_clave, '[0-9]')
           OR INSTR(UPPER(v_clave), c_esquema) > 0 THEN
            RAISE_APPLICATION_ERROR(-20102,
                'Contraseña no admitida: 12 a 30 caracteres, con mayúscula, minúscula y número, sin comillas dobles.');
        END IF;
        EXECUTE IMMEDIATE 'CREATE USER ' || c_esquema || ' IDENTIFIED BY "' || v_clave || '"'
            || ' DEFAULT TABLESPACE DATA QUOTA 2G ON DATA';
        DBMS_OUTPUT.PUT_LINE('Usuario creado: ' || c_esquema);
    ELSE
        DBMS_OUTPUT.PUT_LINE(c_esquema || ' ya existía: no se cambió su contraseña.');
    END IF;
    v_clave := NULL;

    -- Privilegios mínimos para crear y mantener sus propios objetos. Repetir un GRANT no produce error.
    FOR p IN (SELECT column_value AS privilegio FROM TABLE(sys.odcivarchar2list(
                'CREATE SESSION', 'CREATE TABLE', 'CREATE SEQUENCE', 'CREATE VIEW',
                'CREATE PROCEDURE', 'CREATE TRIGGER'))) LOOP
        EXECUTE IMMEDIATE 'GRANT ' || p.privilegio || ' TO ' || c_esquema;
    END LOOP;

    crear_rol('ECOACCESS_ROL_CONSULTA');
    crear_rol('ECOACCESS_ROL_EDICION');
    DBMS_OUTPUT.PUT_LINE('Esquema de EcoAccess preparado en ' || SYS_CONTEXT('USERENV', 'CON_NAME') || '.');
END;
/

WHENEVER SQLERROR CONTINUE

-- Evidencia (copiar a docs/oracle-cloud/02_INSTALACION_BASE_DATOS.md):
SELECT username, account_status, default_tablespace, created
FROM   dba_users
WHERE  username = 'ECOACCESS_P3_APP';

SELECT privilege
FROM   dba_sys_privs
WHERE  grantee = 'ECOACCESS_P3_APP'
ORDER  BY privilege;

SELECT role
FROM   dba_roles
WHERE  role LIKE 'ECOACCESS_ROL_%'
ORDER  BY role;
