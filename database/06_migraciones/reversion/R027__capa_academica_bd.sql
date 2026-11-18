-- Reversión de V027 (capa académica). Elimina SOLO los objetos que creó V027: disparadores, vistas, paquetes,
-- funciones, procedimiento y las tablas PESO_REFERENCIAL, HISTORIAL_ESTADO_CONTENEDOR y AUDITORIA_DEPOSITO
-- (con su historial). No toca depósitos, ecopuntos ni ningún objeto anterior. Los movimientos de ecopuntos creados
-- con PKG_ECOPUNTOS quedan en el libro (son hechos auditables, igual que los de la aplicación).
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE
    PROCEDURE quitar(p_tipo VARCHAR2, p_nombre VARCHAR2) IS
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n FROM user_objects WHERE object_type = p_tipo AND object_name = p_nombre;
        IF n = 1 THEN
            EXECUTE IMMEDIATE 'DROP ' || p_tipo || ' ' || p_nombre || CASE WHEN p_tipo = 'TABLE' THEN ' PURGE' END;
        END IF;
    END;
BEGIN
    quitar('TRIGGER', 'TRG_DEPOSITO_AUD_VERIFICACION');
    quitar('TRIGGER', 'TRG_DEPOSITO_COHERENCIA');
    quitar('TRIGGER', 'TRG_DEPOSITO_BLOQUEO_CERRADO');
    quitar('TRIGGER', 'TRG_CONTENEDOR_AUD_ESTADO');
    FOR v IN (SELECT 'VW_BI_AUDITORIA' n FROM dual UNION ALL SELECT 'VW_BI_ECOPUNTOS' FROM dual UNION ALL
              SELECT 'VW_BI_CONSULTAS_VISION' FROM dual UNION ALL SELECT 'VW_BI_DEPOSITOS' FROM dual UNION ALL
              SELECT 'VW_BI_CIUDADANOS' FROM dual UNION ALL SELECT 'VW_RECORRIDO' FROM dual UNION ALL
              SELECT 'VW_SALDO_ECOPUNTOS' FROM dual UNION ALL SELECT 'VW_DEPOSITO_DETALLE' FROM dual UNION ALL
              SELECT 'VW_CANECA_ESTACION' FROM dual) LOOP
        quitar('VIEW', v.n);
    END LOOP;
    quitar('FUNCTION', 'FN_DEPOSITOS_CIUDADANO');
    quitar('PACKAGE', 'PKG_ESTADISTICAS');
    quitar('PACKAGE', 'PKG_DEPOSITO');
    quitar('PACKAGE', 'PKG_ECOPUNTOS');
    quitar('PROCEDURE', 'PR_CAMBIAR_ESTADO_CONTENEDOR');
    quitar('FUNCTION', 'FN_PESO_ESTIMADO_KG');
    quitar('PACKAGE', 'PKG_CONTEXTO_APP');
    quitar('TABLE', 'AUDITORIA_DEPOSITO');
    quitar('TABLE', 'HISTORIAL_ESTADO_CONTENEDOR');
    quitar('TABLE', 'PESO_REFERENCIAL');
END;
/
DECLARE n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'ECO_MIGRACION_APLICADA';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DELETE FROM eco_migracion_aplicada WHERE version = ''V027'''; COMMIT; END IF;
END;
/
