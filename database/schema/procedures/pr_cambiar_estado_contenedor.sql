-- EcoAccess · procedures/pr_cambiar_estado_contenedor.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PROCEDURE "PR_CAMBIAR_ESTADO_CONTENEDOR" (
    p_id_contenedor IN contenedor.id_contenedor%TYPE,
    p_estado        IN contenedor.estado%TYPE,
    p_id_actor      IN usuario.id_usuario%TYPE,
    p_motivo        IN VARCHAR2
)
IS
    -- ECOACCESS_V027
BEGIN
    pkg_contexto_app.exigir_rol(p_id_actor, '3');
    IF p_estado NOT IN (1, 2, 3) THEN
        RAISE_APPLICATION_ERROR(-20121, 'Estado de caneca no vÃ¡lido: ' || p_estado);
    END IF;
    pkg_contexto_app.fijar_actor(p_id_actor, p_motivo);
    UPDATE contenedor SET estado = p_estado WHERE id_contenedor = p_id_contenedor;
    IF SQL%ROWCOUNT = 0 THEN
        RAISE_APPLICATION_ERROR(-20121, 'No existe la caneca ' || p_id_contenedor);
    END IF;
    pkg_contexto_app.limpiar;
EXCEPTION
    WHEN OTHERS THEN
        pkg_contexto_app.limpiar;
        RAISE;
END pr_cambiar_estado_contenedor;
/
