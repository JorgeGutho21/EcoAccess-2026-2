-- =====================================================================
-- EcoAccess BD (prof. Amílkar) — 01 · Crear los esquemas académicos
-- Archivo: 04_SCRIPTS_ORACLE/01_esquema_practicas.sql
--
-- QUIÉN LO EJECUTA: un administrador de la base LOCAL (por ejemplo la
-- conexión «system» de SQL Developer apuntando a localhost:1521/FREEPDB1).
-- CÓMO: abrir el archivo y pulsar F5 (Ejecutar script). Pide tres
-- contraseñas; escríbelas tú (no se muestran ni se guardan en el archivo).
--
-- QUÉ HACE (y nada más):
--   1. Comprueba que NO estás en el contenedor raíz CDB$ROOT.
--   2. Crea, SOLO SI NO EXISTEN, tres usuarios:
--        ECOACCESS_BD         dueño del modelo EcoAccess académico
--        ECOACCESS_PRACTICAS  dueño de los ejercicios de clase (precio,
--                             secuencia, notas, empleados)
--        ECOACCESS_BD_LECTOR  cuenta de solo lectura para Power BI / C#
--   3. Crea el rol ROL_ECO_LECTURA_BI (si no existe).
--   4. Concede privilegios mínimos (ver tabla al final).
--
-- QUÉ NO HACE: no borra usuarios (nunca DROP USER ... CASCADE), no toca
-- ECOACCESS_P3, ECOACCESS_P3_PRUEBAS, HR ni Oracle Cloud.
-- Si un usuario ya existe, se informa y se conserva tal cual.
-- =====================================================================
SET SERVEROUTPUT ON
SET VERIFY OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE

ACCEPT clave_bd      CHAR PROMPT 'Contraseña para ECOACCESS_BD: '        HIDE
ACCEPT clave_prac    CHAR PROMPT 'Contraseña para ECOACCESS_PRACTICAS: ' HIDE
ACCEPT clave_lector  CHAR PROMPT 'Contraseña para ECOACCESS_BD_LECTOR: ' HIDE

DECLARE
    v_con VARCHAR2(128) := SYS_CONTEXT('USERENV', 'CON_NAME');
BEGIN
    IF v_con = 'CDB$ROOT' THEN
        RAISE_APPLICATION_ERROR(-20900,
            'Estás en CDB$ROOT. Conéctate a la PDB (FREEPDB1) y repite. No se creó nada.');
    END IF;
    DBMS_OUTPUT.PUT_LINE('Contenedor: ' || v_con || ' · usuario que ejecuta: ' || USER);
END;
/

DECLARE
    PROCEDURE crear_usuario(p_nombre VARCHAR2, p_clave VARCHAR2) IS
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n FROM dba_users WHERE username = p_nombre;
        IF n = 0 THEN
            -- La clave va entre comillas dobles: admite mayúsculas/minúsculas.
            EXECUTE IMMEDIATE 'CREATE USER ' || p_nombre
                || ' IDENTIFIED BY "' || REPLACE(p_clave, '"', '') || '"'
                || ' DEFAULT TABLESPACE users TEMPORARY TABLESPACE temp'
                || ' QUOTA 200M ON users';
            DBMS_OUTPUT.PUT_LINE('Creado: ' || p_nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('Ya existía, se conserva sin cambios: ' || p_nombre);
        END IF;
    END;
    PROCEDURE crear_rol(p_nombre VARCHAR2) IS
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n FROM dba_roles WHERE role = p_nombre;
        IF n = 0 THEN
            EXECUTE IMMEDIATE 'CREATE ROLE ' || p_nombre;
            DBMS_OUTPUT.PUT_LINE('Rol creado: ' || p_nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('Rol ya existía: ' || p_nombre);
        END IF;
    END;
BEGIN
    crear_usuario('ECOACCESS_BD',        '&clave_bd');
    crear_usuario('ECOACCESS_PRACTICAS', '&clave_prac');
    crear_usuario('ECOACCESS_BD_LECTOR', '&clave_lector');
    crear_rol('ROL_ECO_LECTURA_BI');
END;
/

-- Privilegios mínimos del dueño del modelo: crear SUS objetos, nada más.
GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE SEQUENCE,
      CREATE PROCEDURE, CREATE TRIGGER TO ecoaccess_bd;
-- El dueño necesita poder conceder SELECT de sus vistas al rol de lectura.
GRANT ROL_ECO_LECTURA_BI TO ecoaccess_bd WITH ADMIN OPTION;

-- Ejercicios de clase: los mismos privilegios, en otro esquema.
GRANT CREATE SESSION, CREATE TABLE, CREATE VIEW, CREATE SEQUENCE,
      CREATE PROCEDURE, CREATE TRIGGER TO ecoaccess_practicas;
-- Lectura de HR para copiar EMPLOYEES/DEPARTMENTS sin modificarlas.
GRANT SELECT ON hr.employees   TO ecoaccess_practicas;
GRANT SELECT ON hr.departments TO ecoaccess_practicas;

-- Lector de informes: solo conectarse; lo que puede leer lo da el rol.
GRANT CREATE SESSION TO ecoaccess_bd_lector;
GRANT ROL_ECO_LECTURA_BI TO ecoaccess_bd_lector;

PROMPT
PROMPT ==== Resultado (debe mostrar 3 usuarios OPEN y 1 rol) ====
SELECT username, account_status, default_tablespace
FROM   dba_users
WHERE  username IN ('ECOACCESS_BD', 'ECOACCESS_PRACTICAS', 'ECOACCESS_BD_LECTOR')
ORDER  BY username;
SELECT role FROM dba_roles WHERE role = 'ROL_ECO_LECTURA_BI';

UNDEFINE clave_bd
UNDEFINE clave_prac
UNDEFINE clave_lector
WHENEVER SQLERROR CONTINUE
-- Privilegios concedidos (resumen):
--   ECOACCESS_BD         CREATE SESSION/TABLE/VIEW/SEQUENCE/PROCEDURE/TRIGGER, ROL_ECO_LECTURA_BI (ADMIN)
--   ECOACCESS_PRACTICAS  los mismos + SELECT en HR.EMPLOYEES y HR.DEPARTMENTS
--   ECOACCESS_BD_LECTOR  CREATE SESSION + ROL_ECO_LECTURA_BI (el rol recibe SELECT de vistas en 09_permisos.sql)
