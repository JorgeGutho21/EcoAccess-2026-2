-- EcoAccess · packages/pkg_ecoaccess_stats.pks.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE "PKG_ECOACCESS_STATS" AUTHID DEFINER AS
-- ECOACCESS_V022_SOURCE_SHA256=4138349490eb1215d5f06b1859ed8b467041ac1b041a0a4e49f967ef3def80b9
    PROCEDURE consultar_resumen(
        p_desde_utc IN TIMESTAMP,
        p_hasta_utc IN TIMESTAMP,
        p_id_punto IN NUMBER,
        p_categoria IN NUMBER,
        p_resultado OUT SYS_REFCURSOR
    );
END PKG_ECOACCESS_STATS;
/
