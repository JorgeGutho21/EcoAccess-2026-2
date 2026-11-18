-- =====================================================================
-- EcoAccess P3 — permisos de los roles sobre las tablas del esquema
-- Archivo: database/00_administracion/05_conceder_permisos_roles.sql
--
-- Ejecutar como ECOACCESS_P3_APP (dueño de las tablas) DESPUÉS de instalar
-- las migraciones, y otra vez cada vez que una migración nueva cree tablas.
-- El instalador lo ejecuta automáticamente al final de cada instalación.
--
--   * ECOACCESS_ROL_CONSULTA: SELECT en todas las tablas.
--   * ECOACCESS_ROL_EDICION:  SELECT, INSERT y UPDATE (sin DELETE, para que una
--     consulta manual equivocada no borre historial).
-- Repetible: volver a conceder un permiso existente no produce error.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

BEGIN
    FOR t IN (SELECT table_name FROM user_tables ORDER BY table_name) LOOP
        BEGIN
            EXECUTE IMMEDIATE 'GRANT SELECT ON ' || t.table_name || ' TO ECOACCESS_ROL_CONSULTA';
            IF t.table_name <> 'ECO_MIGRACION_APLICADA' THEN
                EXECUTE IMMEDIATE 'GRANT SELECT, INSERT, UPDATE ON ' || t.table_name || ' TO ECOACCESS_ROL_EDICION';
            ELSE
                EXECUTE IMMEDIATE 'GRANT SELECT ON ' || t.table_name || ' TO ECOACCESS_ROL_EDICION';
            END IF;
        EXCEPTION
            WHEN OTHERS THEN
                IF SQLCODE = -1917 OR SQLCODE = -1919 THEN
                    RAISE_APPLICATION_ERROR(-20111, 'Faltan los roles ECOACCESS_ROL_*: ejecuta antes 04_crear_esquema_oracle_cloud.sql como ADMIN.');
                END IF;
                RAISE;
        END;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Permisos de ECOACCESS_ROL_CONSULTA y ECOACCESS_ROL_EDICION actualizados.');
END;
/
