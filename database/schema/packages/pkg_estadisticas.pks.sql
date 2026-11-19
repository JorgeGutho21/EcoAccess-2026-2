-- EcoAccess · packages/pkg_estadisticas.pks.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE "PKG_ESTADISTICAS" AS
    -- ECOACCESS_V027
    PROCEDURE consultar_resumen(p_desde_col IN DATE, p_hasta_col IN DATE, p_id_punto IN NUMBER, p_categoria IN NUMBER,
                                p_resultado OUT SYS_REFCURSOR);
    FUNCTION ranking_verificados(p_desde_col IN DATE, p_hasta_col IN DATE, p_top IN PLS_INTEGER DEFAULT 10) RETURN SYS_REFCURSOR;
    FUNCTION a_utc(p_fecha_col IN DATE) RETURN TIMESTAMP;
END pkg_estadisticas;
/
