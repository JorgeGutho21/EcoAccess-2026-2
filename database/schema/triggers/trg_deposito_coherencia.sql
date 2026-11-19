-- EcoAccess · triggers/trg_deposito_coherencia.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE TRIGGER "TRG_DEPOSITO_COHERENCIA"
BEFORE INSERT OR UPDATE ON deposito
FOR EACH ROW
FOLLOWS trg_deposito_bloqueo_cerrado
DECLARE
    v_titular    interaccion.id_usuario%TYPE;
    v_contenedor contenedor.id_contenedor%TYPE;
    v_admitido   NUMBER;
BEGIN
    IF UPDATING THEN
        IF :NEW.id_orden <> :OLD.id_orden OR :NEW.id_evento <> :OLD.id_evento
           OR :NEW.id_material_declarado <> :OLD.id_material_declarado
           OR :NEW.sin_sensor <> :OLD.sin_sensor
           OR :NEW.fecha_deteccion_utc <> :OLD.fecha_deteccion_utc THEN
            RAISE_APPLICATION_ERROR(-20109, 'Orden, evento, material declarado, sensor y fecha de detecciÃ³n no se modifican.');
        END IF;
    END IF;
    BEGIN
        SELECT i.id_usuario, o.id_contenedor INTO v_titular, v_contenedor
        FROM   orden_apertura o JOIN interaccion i ON i.id_interaccion = o.id_interaccion
        WHERE  o.id_orden = :NEW.id_orden;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN;                                    -- orden inexistente: la rechaza la FK
    END;
    IF INSERTING THEN
        SELECT COUNT(*) INTO v_admitido FROM contenedor_material
        WHERE  id_contenedor = v_contenedor AND id_material = :NEW.id_material_declarado;
        IF v_admitido = 0 THEN
            RAISE_APPLICATION_ERROR(-20102, 'La caneca ' || v_contenedor || ' no admite el material declarado '
                || :NEW.id_material_declarado || '.');
        END IF;
    END IF;
    IF :NEW.id_verificador IS NOT NULL AND (INSERTING OR NVL(:OLD.id_verificador, -1) <> :NEW.id_verificador) THEN
        IF pkg_contexto_app.tiene_rol(:NEW.id_verificador, 2) = 0 AND pkg_contexto_app.tiene_rol(:NEW.id_verificador, 3) = 0 THEN
            RAISE_APPLICATION_ERROR(-20104, 'El verificador ' || :NEW.id_verificador || ' no es Operario ni Administrador activo.');
        END IF;
    END IF;
END;
/
ALTER TRIGGER "TRG_DEPOSITO_COHERENCIA" ENABLE;
