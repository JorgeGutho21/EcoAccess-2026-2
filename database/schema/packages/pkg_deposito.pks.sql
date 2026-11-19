-- EcoAccess · packages/pkg_deposito.pks.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE "PKG_DEPOSITO" AS
    -- ECOACCESS_V027
    PROCEDURE verificar(p_id_deposito IN deposito.id_deposito%TYPE, p_id_verificador IN usuario.id_usuario%TYPE,
                        p_id_material IN material.id_material%TYPE, p_cantidad IN deposito.cantidad_verificada%TYPE,
                        p_peso_medido_kg IN deposito.peso_medido_kg%TYPE DEFAULT NULL, p_motivo IN VARCHAR2 DEFAULT NULL);
    PROCEDURE rechazar(p_id_deposito IN deposito.id_deposito%TYPE, p_id_verificador IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2);
    -- p_nuevo_estado: 0 reabrir (vuelve a pendiente), 1 verificado con otros datos.
    PROCEDURE corregir_verificacion(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE,
                                    p_nuevo_estado IN NUMBER, p_id_material IN material.id_material%TYPE,
                                    p_cantidad IN deposito.cantidad_verificada%TYPE, p_motivo IN VARCHAR2);
    FUNCTION estado(p_id_deposito IN deposito.id_deposito%TYPE) RETURN NUMBER;
    FUNCTION correccion_en_curso RETURN BOOLEAN;
END pkg_deposito;
/
