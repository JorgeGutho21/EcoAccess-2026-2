-- EcoAccess · triggers/trg_deposito_bloqueo_cerrado.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE TRIGGER "TRG_DEPOSITO_BLOQUEO_CERRADO"
BEFORE UPDATE ON deposito
FOR EACH ROW
 WHEN (OLD.id_material_verificado IS NOT NULL) BEGIN
    IF NOT pkg_deposito.correccion_en_curso THEN
        RAISE_APPLICATION_ERROR(-20103, 'El depÃ³sito ' || :OLD.id_deposito
            || ' ya estÃ¡ VERIFICADO. Solo se cambia con PKG_DEPOSITO.corregir_verificacion y un motivo.');
    END IF;
END;
/
ALTER TRIGGER "TRG_DEPOSITO_BLOQUEO_CERRADO" ENABLE;
