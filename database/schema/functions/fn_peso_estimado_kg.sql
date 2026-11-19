-- EcoAccess · functions/fn_peso_estimado_kg.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE FUNCTION "FN_PESO_ESTIMADO_KG" (
    p_id_peso_referencial IN peso_referencial.id_peso_referencial%TYPE,
    p_unidades            IN NUMBER
) RETURN NUMBER
IS
    -- ECOACCESS_V027
    v_gramos peso_referencial.gramos_tipico%TYPE;
BEGIN
    IF p_id_peso_referencial IS NULL OR p_unidades IS NULL OR p_unidades < 1 THEN
        RETURN NULL;
    END IF;
    SELECT gramos_tipico INTO v_gramos FROM peso_referencial WHERE id_peso_referencial = p_id_peso_referencial;
    RETURN ROUND(v_gramos * p_unidades / 1000, 4);
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN NULL;
END fn_peso_estimado_kg;
/
