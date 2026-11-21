-- =====================================================================
-- EcoAccess P3 — permisos de los roles sobre las tablas del esquema
-- Archivo: database/00_administracion/05_conceder_permisos_roles.sql
--
-- Ejecutar como ECOACCESS_P3_APP (dueño de las tablas) DESPUÉS de instalar
-- las migraciones, y otra vez cada vez que una migración nueva cree tablas.
-- El instalador lo ejecuta automáticamente al final de cada instalación.
--
--   * ECOACCESS_ROL_CONSULTA: SELECT en las tablas y vistas, salvo las SENSIBLES.
--   * ECOACCESS_ROL_EDICION:  SELECT; INSERT y UPDATE solo en tablas de trabajo (sin DELETE,
--     para que una consulta manual equivocada no borre historial).
-- Mínimo privilegio (revisado el 21/11/2026 con la auditoría de la instancia):
--   * SENSIBLES, sin acceso directo para ningún rol (se revoca si estaba concedido):
--     CREDENCIAL_ACCESO (hashes de contraseñas), TOKEN_VINCULACION y VINCULACION_PENDIENTE
--     (códigos para vincular Telegram) y UPDATE_TELEGRAM (mensajes crudos de Telegram).
--   * SOLO LECTURA también para EDICION (los escribe únicamente la aplicación con sus reglas):
--     libro de ecopuntos (MOVIMIENTO_ECOPUNTOS, OPERACION_ECOPUNTOS, REGLA_ECOPUNTOS),
--     auditoría (AUDITORIA_DEPOSITO, EVENTO_DISPOSITIVO, HISTORIAL_ESTADO_CONTENEDOR),
--     ORDEN_APERTURA y ECO_MIGRACION_APLICADA.
-- La aplicación no usa estos roles: se conecta como ECOACCESS_P3_APP, dueña de las tablas.
-- Repetible: volver a conceder un permiso existente no produce error.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    -- En PL/SQL las constantes van antes que las funciones internas.
    v_sensibles CONSTANT VARCHAR2(400) := 'CREDENCIAL_ACCESO,TOKEN_VINCULACION,VINCULACION_PENDIENTE,UPDATE_TELEGRAM';
    v_solo_lectura CONSTANT VARCHAR2(400) := 'MOVIMIENTO_ECOPUNTOS,OPERACION_ECOPUNTOS,REGLA_ECOPUNTOS,AUDITORIA_DEPOSITO,'
        || 'EVENTO_DISPOSITIVO,HISTORIAL_ESTADO_CONTENEDOR,ORDEN_APERTURA,ECO_MIGRACION_APLICADA';
    FUNCTION en_lista(p_tabla VARCHAR2, p_lista VARCHAR2) RETURN BOOLEAN IS
    BEGIN
        RETURN INSTR(',' || p_lista || ',', ',' || p_tabla || ',') > 0;
    END;
    PROCEDURE quitar(p_privilegios VARCHAR2, p_tabla VARCHAR2, p_rol VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE 'REVOKE ' || p_privilegios || ' ON ' || p_tabla || ' FROM ' || p_rol;
    EXCEPTION
        WHEN OTHERS THEN
            IF SQLCODE NOT IN (-1927, -1952) THEN RAISE; END IF; -- no estaba concedido
    END;
BEGIN
    FOR t IN (SELECT table_name FROM user_tables ORDER BY table_name) LOOP
        BEGIN
            IF en_lista(t.table_name, v_sensibles) THEN
                quitar('SELECT', t.table_name, 'ECOACCESS_ROL_CONSULTA');
                quitar('SELECT, INSERT, UPDATE', t.table_name, 'ECOACCESS_ROL_EDICION');
                quitar('SELECT', t.table_name, 'ECOACCESS_ROL_EDICION');
            ELSIF en_lista(t.table_name, v_solo_lectura) THEN
                EXECUTE IMMEDIATE 'GRANT SELECT ON ' || t.table_name || ' TO ECOACCESS_ROL_CONSULTA';
                EXECUTE IMMEDIATE 'GRANT SELECT ON ' || t.table_name || ' TO ECOACCESS_ROL_EDICION';
                quitar('INSERT, UPDATE', t.table_name, 'ECOACCESS_ROL_EDICION');
            ELSE
                EXECUTE IMMEDIATE 'GRANT SELECT ON ' || t.table_name || ' TO ECOACCESS_ROL_CONSULTA';
                EXECUTE IMMEDIATE 'GRANT SELECT, INSERT, UPDATE ON ' || t.table_name || ' TO ECOACCESS_ROL_EDICION';
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
