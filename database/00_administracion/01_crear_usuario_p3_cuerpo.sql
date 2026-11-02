-- =====================================================================
-- EcoAccess P3 — cuerpo de la creación del usuario ECOACCESS_P3
-- Archivo: database/00_administracion/01_crear_usuario_p3_cuerpo.sql
--
-- Requiere que la variable de sustitución clave_p3 ya esté definida, ya sea
-- por 01_crear_usuario_p3.sql (ACCEPT) o por un DEFINE de una ejecución
-- automatizada. No contiene contraseñas.
--
-- Crea el usuario dueño de las tablas de P3 con solo los privilegios
-- necesarios para crear sus propios objetos (menor privilegio: no recibe DBA
-- ni privilegios ANY). Se puede ejecutar dos veces: si el usuario existe, no
-- lo vuelve a crear ni cambia su contraseña.
-- =====================================================================

SET ECHO OFF
SET VERIFY OFF
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

-- 1. Protección: en el contenedor raíz los usuarios locales no se pueden crear
--    (ORA-65096) y además no es el lugar correcto para los datos de P3.
DECLARE
    v_contenedor VARCHAR2(128) := SYS_CONTEXT('USERENV', 'CON_NAME');
BEGIN
    IF v_contenedor = 'CDB$ROOT' THEN
        RAISE_APPLICATION_ERROR(-20001,
            'Estás conectado a CDB$ROOT. Conéctate al PDB FREEPDB1 y vuelve a ejecutar.');
    END IF;
    DBMS_OUTPUT.PUT_LINE('Contenedor actual: ' || v_contenedor);
END;
/

-- 2. Creación idempotente del usuario. Una contraseña vacía se rechaza con un
--    mensaje claro en lugar del confuso ORA-01741.
DECLARE
    v_existe NUMBER;
    v_clave  VARCHAR2(128) := '&clave_p3';
BEGIN
    IF v_clave IS NULL THEN
        RAISE_APPLICATION_ERROR(-20002,
            'La contraseña llegó vacía. Ejecuta 01_crear_usuario_p3.sql desde SQL Developer o define clave_p3.');
    END IF;

    SELECT COUNT(*) INTO v_existe FROM dba_users WHERE username = 'ECOACCESS_P3';

    IF v_existe = 0 THEN
        EXECUTE IMMEDIATE 'CREATE USER ecoaccess_p3 IDENTIFIED BY "' || v_clave || '"'
            || ' DEFAULT TABLESPACE users'
            || ' TEMPORARY TABLESPACE temp'
            || ' QUOTA 200M ON users';
        DBMS_OUTPUT.PUT_LINE('Usuario ECOACCESS_P3 creado.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('ECOACCESS_P3 ya existía: no se cambió su contraseña.');
    END IF;
END;
/

UNDEFINE clave_p3

-- 3. Privilegios mínimos para que el esquema cree y mantenga sus propios objetos.
--    Repetir un GRANT no produce error, así que esta parte también es repetible.
GRANT CREATE SESSION   TO ecoaccess_p3;
GRANT CREATE TABLE     TO ecoaccess_p3;
GRANT CREATE SEQUENCE  TO ecoaccess_p3;
GRANT CREATE VIEW      TO ecoaccess_p3;
GRANT CREATE PROCEDURE TO ecoaccess_p3;
GRANT CREATE TRIGGER   TO ecoaccess_p3;

-- 4. Comprobación del resultado.
SELECT username, account_status, default_tablespace, temporary_tablespace
FROM   dba_users
WHERE  username = 'ECOACCESS_P3';

SELECT privilege
FROM   dba_sys_privs
WHERE  grantee = 'ECOACCESS_P3'
ORDER  BY privilege;

WHENEVER SQLERROR CONTINUE
