-- EcoAccess · packages/pkg_contexto_app.pks.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE "PKG_CONTEXTO_APP" AS
    -- ECOACCESS_V027
    PROCEDURE fijar_actor(p_id_usuario IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2 DEFAULT NULL);
    PROCEDURE limpiar;
    FUNCTION  actor  RETURN usuario.id_usuario%TYPE;
    FUNCTION  motivo RETURN VARCHAR2;
    -- 1 si el usuario está activo y tiene el rol (1 Ciudadano, 2 Operario, 3 Administrador).
    FUNCTION  tiene_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigo IN rol.codigo%TYPE) RETURN NUMBER;
    PROCEDURE exigir_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigos IN VARCHAR2);
END pkg_contexto_app;
/
