-- =====================================================================
-- EcoAccess BD — 00 · Comprobación previa (SOLO LECTURA)
-- Ejecutar conectado como ECOACCESS_BD (F5). No crea, cambia ni borra nada.
-- Sirve para saber DÓNDE estás antes de instalar.
-- =====================================================================
SET SERVEROUTPUT ON
SET LINESIZE 200
SET PAGESIZE 100
COLUMN dato  FORMAT A28
COLUMN valor FORMAT A60

PROMPT ==== 1. ¿Dónde estoy conectado? ====
SELECT 'Usuario de base de datos' dato, USER valor FROM dual
UNION ALL SELECT 'Contenedor (PDB)',  SYS_CONTEXT('USERENV','CON_NAME')    FROM dual
UNION ALL SELECT 'Servicio',          SYS_CONTEXT('USERENV','SERVICE_NAME') FROM dual
UNION ALL SELECT 'Equipo servidor',   SYS_CONTEXT('USERENV','SERVER_HOST')  FROM dual
UNION ALL SELECT 'Hora UTC',          TO_CHAR(SYS_EXTRACT_UTC(SYSTIMESTAMP),'YYYY-MM-DD HH24:MI:SS') FROM dual
UNION ALL SELECT 'Hora Colombia',     TO_CHAR(SYSTIMESTAMP AT TIME ZONE 'America/Bogota','YYYY-MM-DD HH24:MI:SS') FROM dual;

PROMPT ==== 2. Versión de Oracle ====
SELECT banner_full FROM v$version;

PROMPT ==== 3. Privilegios que tengo ====
SELECT privilege FROM user_sys_privs ORDER BY privilege;
SELECT granted_role FROM user_role_privs ORDER BY granted_role;

PROMPT ==== 4. ¿Qué objetos hay ya en este esquema? (0 = instalación nueva) ====
SELECT object_type, COUNT(*) cantidad FROM user_objects GROUP BY object_type ORDER BY object_type;

PROMPT ==== 5. Seguridad: este script NO debe correr en P3 ====
DECLARE
BEGIN
    IF USER IN ('ECOACCESS_P3', 'ECOACCESS_P3_APP', 'ECOACCESS_P3_PRUEBAS', 'HR', 'SYSTEM', 'SYS') THEN
        DBMS_OUTPUT.PUT_LINE('ALTO: estás conectado como ' || USER || '. El modelo académico se instala en ECOACCESS_BD.');
    ELSIF USER = 'ECOACCESS_BD' THEN
        DBMS_OUTPUT.PUT_LINE('Correcto: conexión ECOACCESS_BD. Puedes seguir con 02_tablas.sql.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Aviso: usuario ' || USER || '. Revisa que sea el esquema académico.');
    END IF;
END;
/
