-- EcoAccess · functions/fn_depositos_ciudadano.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE FUNCTION "FN_DEPOSITOS_CIUDADANO" (p_id_usuario IN usuario.id_usuario%TYPE) RETURN SYS_REFCURSOR
IS
    -- ECOACCESS_V027
    v_cursor SYS_REFCURSOR;
BEGIN
    OPEN v_cursor FOR
        SELECT id_deposito, fecha_colombia, color, material_declarado, material_verificado,
               estado_texto, evidencia_fisica, peso_estimado_g, peso_medido_g
        FROM   vw_deposito_detalle
        WHERE  id_titular = p_id_usuario
        ORDER  BY fecha_colombia DESC;
    RETURN v_cursor;
END fn_depositos_ciudadano;
/
