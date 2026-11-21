-- =====================================================================
-- EcoAccess P3 — tabla aislada para la prueba colaborativa entre integrantes
-- Archivo: database/00_administracion/07_prueba_colaborativa.sql
--
-- Ejecutar como ECOACCESS_P3_APP (dueño del esquema):
--   dotnet run --project src/EcoAccess.P3.Instalador -- preparar-prueba-colaborativa
-- Crea PRUEBA_COLABORATIVA si no existe. Cada integrante escribe aquí con su propia cuenta
-- (prueba-colaborativa --usuario ECO_<NOMBRE>) y lee lo que escribieron los demás: así se
-- comprueba que todos usan la MISMA base de Oracle Cloud sin tocar usuarios, depósitos ni
-- ecopuntos reales. El integrante, la fecha y la base los pone Oracle (no se pueden falsear).
-- Se puede borrar cuando ya no se necesite: DROP TABLE prueba_colaborativa PURGE;
-- Repetible: si la tabla existe, no hace nada.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_existe NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_existe FROM user_tables WHERE table_name = 'PRUEBA_COLABORATIVA';
    IF v_existe = 0 THEN
        EXECUTE IMMEDIATE q'[
            CREATE TABLE prueba_colaborativa (
                id_prueba   NUMBER GENERATED ALWAYS AS IDENTITY CONSTRAINT pk_prueba_colaborativa PRIMARY KEY,
                integrante  VARCHAR2(128 CHAR) DEFAULT SYS_CONTEXT('USERENV','SESSION_USER') NOT NULL,
                base        VARCHAR2(128 CHAR) DEFAULT SYS_CONTEXT('USERENV','DB_NAME') NOT NULL,
                creado_utc  TIMESTAMP DEFAULT SYS_EXTRACT_UTC(SYSTIMESTAMP) NOT NULL,
                nota        VARCHAR2(100 CHAR) NOT NULL
            )]';
        EXECUTE IMMEDIATE q'[COMMENT ON TABLE prueba_colaborativa IS 'Solo pruebas de conexión entre integrantes; no son datos de la aplicación']';
        DBMS_OUTPUT.PUT_LINE('Tabla PRUEBA_COLABORATIVA creada.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('PRUEBA_COLABORATIVA ya existía.');
    END IF;
END;
/
