-- EcoAccess · procedures/pr_consultar_estadisticas.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PROCEDURE "PR_CONSULTAR_ESTADISTICAS" (
    p_desde_utc IN TIMESTAMP,
    p_hasta_utc IN TIMESTAMP,
    p_id_punto IN NUMBER,
    p_categoria IN NUMBER,
    p_resultado OUT SYS_REFCURSOR
) AUTHID DEFINER AS
-- ECOACCESS_V022_SOURCE_SHA256=313498c770c1ad51cc81386b24fbd416fb20ea3f01935d63ea7878b548d9df16
BEGIN
    PKG_ECOACCESS_STATS.consultar_resumen(
        p_desde_utc,p_hasta_utc,p_id_punto,p_categoria,p_resultado);
END PR_CONSULTAR_ESTADISTICAS;
/
