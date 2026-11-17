-- =====================================================================
-- EcoAccess BD — 99 · REVERSIÓN del esquema académico (DESTRUCTIVO)
--
-- NO es parte de la instalación normal. Úsalo solo para reinstalar el
-- esquema académico desde cero en tu PC (por ejemplo, para regenerar los
-- datos sintéticos). Borra TODOS los objetos de ECOACCESS_BD y sus datos.
--
-- Protecciones:
--   1. Solo funciona conectado como ECOACCESS_BD (nunca como P3, HR o SYSTEM).
--   2. Pide escribir exactamente BORRAR_ECOACCESS_BD.
--   3. No borra el usuario (no usa DROP USER ... CASCADE) ni toca otros esquemas.
-- Después de ejecutarlo, repite 02 → 14 en orden.
-- =====================================================================
SET SERVEROUTPUT ON
SET VERIFY OFF
ACCEPT confirmar CHAR PROMPT 'Escribe BORRAR_ECOACCESS_BD para confirmar: '
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    v_confirmar VARCHAR2(40) := '&confirmar';
BEGIN
    IF USER <> 'ECOACCESS_BD' THEN
        RAISE_APPLICATION_ERROR(-20990, 'Solo se ejecuta conectado como ECOACCESS_BD. Usuario actual: ' || USER);
    END IF;
    IF v_confirmar <> 'BORRAR_ECOACCESS_BD' THEN
        RAISE_APPLICATION_ERROR(-20991, 'Confirmación incorrecta. No se borró nada.');
    END IF;
    -- Primero el código (paquetes, funciones, procedimientos, vistas, triggers)...
    FOR o IN (SELECT object_name, object_type FROM user_objects
              WHERE object_type IN ('PACKAGE', 'FUNCTION', 'PROCEDURE', 'VIEW')) LOOP
        EXECUTE IMMEDIATE 'DROP ' || o.object_type || ' ' || o.object_name;
        DBMS_OUTPUT.PUT_LINE('Borrado ' || o.object_type || ' ' || o.object_name);
    END LOOP;
    -- ...después las tablas (CASCADE CONSTRAINTS quita las FK entre ellas;
    -- PURGE evita que queden en la papelera BIN$).
    FOR t IN (SELECT table_name FROM user_tables) LOOP
        EXECUTE IMMEDIATE 'DROP TABLE ' || t.table_name || ' CASCADE CONSTRAINTS PURGE';
        DBMS_OUTPUT.PUT_LINE('Borrada tabla ' || t.table_name);
    END LOOP;
END;
/
SELECT COUNT(*) AS objetos_restantes FROM user_objects;
UNDEFINE confirmar
WHENEVER SQLERROR CONTINUE
