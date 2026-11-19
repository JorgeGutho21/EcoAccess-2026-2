-- EcoAccess · packages/pkg_ecopuntos.pks.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE "PKG_ECOPUNTOS" AS
    -- ECOACCESS_V027
    FUNCTION  regla_vigente(p_id_material IN material.id_material%TYPE, p_fecha_utc IN TIMESTAMP) RETURN regla_ecopuntos.id_regla%TYPE;
    FUNCTION  credito_vigente(p_id_deposito IN deposito.id_deposito%TYPE) RETURN movimiento_ecopuntos.id_movimiento%TYPE;
    PROCEDURE acreditar(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE,
                        p_id_movimiento OUT movimiento_ecopuntos.id_movimiento%TYPE);
    PROCEDURE revertir(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2);
    -- Corrección: revierte el crédito vigente (si hay) y acredita con la tarifa vigente del material verificado actual.
    PROCEDURE recalcular(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2);
    FUNCTION  saldo(p_id_usuario IN usuario.id_usuario%TYPE) RETURN NUMBER;
END pkg_ecopuntos;
/
