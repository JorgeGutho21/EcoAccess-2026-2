-- EcoAccess · triggers/trg_contenedor_aud_estado.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE TRIGGER "TRG_CONTENEDOR_AUD_ESTADO"
AFTER UPDATE OF estado ON contenedor
FOR EACH ROW
 WHEN (OLD.estado <> NEW.estado) BEGIN
    INSERT INTO historial_estado_contenedor (id_contenedor, estado_anterior, estado_nuevo, id_actor, usuario_bd, motivo)
    VALUES (:OLD.id_contenedor, :OLD.estado, :NEW.estado, pkg_contexto_app.actor, USER, pkg_contexto_app.motivo);
END;
/
ALTER TRIGGER "TRG_CONTENEDOR_AUD_ESTADO" ENABLE;
