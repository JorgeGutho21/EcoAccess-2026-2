-- EcoAccess · triggers/tr_ecop_unidades.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE TRIGGER "TR_ECOP_UNIDADES"
BEFORE INSERT ON movimiento_ecopuntos
FOR EACH ROW
 WHEN (NEW.tipo=1) DECLARE
    unidad NUMBER;
    tarifa NUMBER;
    cantidad NUMBER;
BEGIN
    SELECT por_unidad,puntos INTO unidad,tarifa FROM regla_ecopuntos WHERE id_regla=:NEW.id_regla;
    IF unidad=1 THEN
        SELECT cantidad_verificada INTO cantidad FROM deposito WHERE id_deposito=:NEW.id_deposito;
        IF cantidad IS NULL OR :NEW.puntos<>tarifa*cantidad THEN
            RAISE_APPLICATION_ERROR(-20113,'La tarifa unitaria exige conteo verificado y puntos coincidentes.');
        END IF;
    END IF;
END;
/
ALTER TRIGGER "TR_ECOP_UNIDADES" ENABLE;
