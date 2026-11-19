-- EcoAccess · packages/pkg_contexto_app.pkb.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "PKG_CONTEXTO_APP" AS
    -- ECOACCESS_V027
    g_actor  usuario.id_usuario%TYPE;
    g_motivo VARCHAR2(300 CHAR);

    PROCEDURE fijar_actor(p_id_usuario IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2 DEFAULT NULL) IS
        v_activo usuario.activo%TYPE;
    BEGIN
        SELECT activo INTO v_activo FROM usuario WHERE id_usuario = p_id_usuario;
        IF v_activo <> 1 THEN
            RAISE_APPLICATION_ERROR(-20100, 'El actor ' || p_id_usuario || ' está inactivo.');
        END IF;
        g_actor  := p_id_usuario;
        g_motivo := SUBSTR(p_motivo, 1, 300);
        DBMS_SESSION.SET_IDENTIFIER('ECO_U' || p_id_usuario);
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20100, 'El actor ' || p_id_usuario || ' no existe.');
    END fijar_actor;

    PROCEDURE limpiar IS
    BEGIN
        g_actor := NULL;
        g_motivo := NULL;
        DBMS_SESSION.CLEAR_IDENTIFIER;
    END limpiar;

    FUNCTION actor RETURN usuario.id_usuario%TYPE IS
    BEGIN
        RETURN g_actor;
    END actor;

    FUNCTION motivo RETURN VARCHAR2 IS
    BEGIN
        RETURN g_motivo;
    END motivo;

    FUNCTION tiene_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigo IN rol.codigo%TYPE) RETURN NUMBER IS
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n
        FROM   usuario u
        JOIN   usuario_rol ur ON ur.id_usuario = u.id_usuario
        JOIN   rol r          ON r.id_rol = ur.id_rol
        WHERE  u.id_usuario = p_id_usuario AND u.activo = 1 AND r.codigo = p_codigo;
        RETURN CASE WHEN n > 0 THEN 1 ELSE 0 END;
    END tiene_rol;

    PROCEDURE exigir_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigos IN VARCHAR2) IS
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n
        FROM   usuario u
        JOIN   usuario_rol ur ON ur.id_usuario = u.id_usuario
        JOIN   rol r          ON r.id_rol = ur.id_rol
        WHERE  u.id_usuario = p_id_usuario AND u.activo = 1
        AND    INSTR(',' || p_codigos || ',', ',' || r.codigo || ',') > 0;
        IF n = 0 THEN
            RAISE_APPLICATION_ERROR(-20120, 'El usuario ' || NVL(TO_CHAR(p_id_usuario), '(nulo)')
                || ' no tiene un rol permitido (' || p_codigos || ') o está inactivo.');
        END IF;
    END exigir_rol;
END pkg_contexto_app;
/
