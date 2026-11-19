-- EcoAccess · triggers/trg_deposito_aud_verificacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE TRIGGER "TRG_DEPOSITO_AUD_VERIFICACION"
AFTER UPDATE OF id_material_verificado, cantidad_verificada, peso_medido_kg, id_verificador ON deposito
FOR EACH ROW
DECLARE
    v_cambios NUMBER;
    v_ant     NUMBER := CASE WHEN :OLD.id_material_verificado IS NOT NULL THEN 1 ELSE 0 END;
    v_nue     NUMBER := CASE WHEN :NEW.id_material_verificado IS NOT NULL THEN 1 ELSE 0 END;
    FUNCTION cambio(a NUMBER, b NUMBER) RETURN NUMBER IS
    BEGIN
        RETURN CASE WHEN (a = b) OR (a IS NULL AND b IS NULL) THEN 0 ELSE 1 END;
    END;
BEGIN
    v_cambios := cambio(:OLD.id_material_verificado, :NEW.id_material_verificado) + cambio(:OLD.cantidad_verificada, :NEW.cantidad_verificada)
               + cambio(:OLD.peso_medido_kg, :NEW.peso_medido_kg) + cambio(:OLD.id_verificador, :NEW.id_verificador);
    IF v_cambios = 0 THEN
        RETURN;
    END IF;
    INSERT INTO auditoria_deposito (id_deposito, operacion, estado_anterior, estado_nuevo,
        id_material_verificado_anterior, id_material_verificado_nuevo, cantidad_anterior, cantidad_nueva,
        peso_medido_anterior_kg, peso_medido_nuevo_kg, id_verificador_anterior, id_verificador_nuevo, id_actor, usuario_bd, motivo)
    VALUES (:NEW.id_deposito,
        CASE WHEN v_ant = 0 AND v_nue = 1 THEN 'VERIFICACION' WHEN v_ant = 1 AND v_nue = 0 THEN 'REAPERTURA' ELSE 'CORRECCION' END,
        v_ant, v_nue, :OLD.id_material_verificado, :NEW.id_material_verificado, :OLD.cantidad_verificada, :NEW.cantidad_verificada,
        :OLD.peso_medido_kg, :NEW.peso_medido_kg, :OLD.id_verificador, :NEW.id_verificador,
        NVL(pkg_contexto_app.actor, :NEW.id_verificador), USER, pkg_contexto_app.motivo);
END;
/
ALTER TRIGGER "TRG_DEPOSITO_AUD_VERIFICACION" ENABLE;
