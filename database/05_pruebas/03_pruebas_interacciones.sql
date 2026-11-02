-- =====================================================================
-- EcoAccess P3 — pruebas de restricciones de interacciones, órdenes,
-- eventos y depósitos (migración V003)
-- Archivo: database/05_pruebas/03_pruebas_interacciones.sql
--
-- Mismo formato que 02_pruebas_credencial.sql: cada caso imprime PASA o
-- FALLA y al final se hace ROLLBACK. Requiere la versión 001, V002 y V003.
-- Crea su propio punto, caneca y usuario de prueba; no depende de los datos demo.
-- Errores esperados: ORA-00001, ORA-02290 y ORA-02291.
-- =====================================================================

SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_pasan        PLS_INTEGER := 0;
    v_fallan       PLS_INTEGER := 0;
    v_punto        NUMBER;
    v_caneca       NUMBER;
    v_caneca_otra  NUMBER;
    v_usuario      NUMBER;
    v_material     NUMBER;
    v_inter        NUMBER;
    v_inter_2      NUMBER;
    v_orden        NUMBER;
    v_evento       NUMBER;
    v_evento_otro  NUMBER;
    c_ahora CONSTANT VARCHAR2(40) := 'SYS_EXTRACT_UTC(SYSTIMESTAMP)';

    PROCEDURE registrar(p_pasa BOOLEAN, p_caso VARCHAR2, p_detalle VARCHAR2 := NULL) IS
    BEGIN
        IF p_pasa THEN
            v_pasan := v_pasan + 1;
            DBMS_OUTPUT.PUT_LINE('PASA   ' || p_caso);
        ELSE
            v_fallan := v_fallan + 1;
            DBMS_OUTPUT.PUT_LINE('FALLA  ' || p_caso || ' -> ' || p_detalle);
        END IF;
    END;

    PROCEDURE esperar_error(p_caso VARCHAR2, p_sql VARCHAR2, p_codigo NUMBER) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
        registrar(FALSE, p_caso, 'Oracle aceptó un dato inválido');
    EXCEPTION
        WHEN OTHERS THEN
            registrar(SQLCODE = p_codigo, p_caso, 'error inesperado: ' || SQLERRM);
    END;

    PROCEDURE esperar_exito(p_caso VARCHAR2, p_sql VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
        registrar(TRUE, p_caso);
    EXCEPTION
        WHEN OTHERS THEN
            registrar(FALSE, p_caso, 'Oracle rechazó un dato válido: ' || SQLERRM);
    END;

    -- INSERT de interacción con valores por defecto válidos (Telegram, Iniciada).
    FUNCTION interaccion(p_canal VARCHAR2 := '1', p_estado VARCHAR2 := '1', p_sugerido VARCHAR2 := 'NULL',
                         p_confianza VARCHAR2 := 'NULL', p_confirmado VARCHAR2 := 'NULL',
                         p_operario VARCHAR2 := 'NULL', p_motivo VARCHAR2 := 'NULL',
                         p_cierre VARCHAR2 := 'NULL', p_caneca NUMBER := NULL) RETURN VARCHAR2 IS
    BEGIN
        RETURN 'INSERT INTO interaccion (id_contenedor, id_usuario, canal, estado, id_material_sugerido, confianza_sugerida, '
            || 'id_material_confirmado, id_operario, motivo, fecha_inicio_utc, fecha_cierre_utc) VALUES ('
            || NVL(p_caneca, v_caneca) || ', ' || v_usuario || ', ' || p_canal || ', ' || p_estado || ', ' || p_sugerido || ', '
            || p_confianza || ', ' || p_confirmado || ', ' || p_operario || ', ' || p_motivo || ', '
            || c_ahora || ', ' || p_cierre || ')';
    END;

    FUNCTION orden(p_inter NUMBER, p_clave VARCHAR2, p_estado VARCHAR2 := '1',
                   p_vence VARCHAR2 := 'SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL ''2'' MINUTE',
                   p_cierre VARCHAR2 := 'NULL', p_caneca NUMBER := NULL) RETURN VARCHAR2 IS
    BEGIN
        RETURN 'INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado, '
            || 'fecha_emision_utc, fecha_vencimiento_utc, fecha_cierre_utc) VALUES ('
            || p_inter || ', ' || NVL(p_caneca, v_caneca) || ', ''' || p_clave || ''', ' || p_estado || ', '
            || c_ahora || ', ' || p_vence || ', ' || p_cierre || ')';
    END;

    FUNCTION evento(p_externo VARCHAR2, p_tipo VARCHAR2 := '3', p_orden VARCHAR2 := NULL,
                    p_resultado VARCHAR2 := '1') RETURN VARCHAR2 IS
    BEGIN
        RETURN 'INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado, fecha_recepcion_utc) VALUES ('
            || v_caneca || ', ''' || p_externo || ''', ' || p_tipo || ', ' || NVL(p_orden, TO_CHAR(v_orden)) || ', '
            || p_resultado || ', ' || c_ahora || ')';
    END;

    FUNCTION deposito(p_orden VARCHAR2, p_evento VARCHAR2, p_medido VARCHAR2 := 'NULL',
                      p_verificado VARCHAR2 := 'NULL', p_verificador VARCHAR2 := 'NULL',
                      p_fecha_verif VARCHAR2 := 'NULL') RETURN VARCHAR2 IS
    BEGIN
        RETURN 'INSERT INTO deposito (id_orden, id_evento, id_material_declarado, id_material_verificado, id_verificador, '
            || 'fecha_verificacion_utc, peso_medido_kg, fecha_deteccion_utc) VALUES ('
            || p_orden || ', ' || p_evento || ', ' || v_material || ', ' || p_verificado || ', ' || p_verificador || ', '
            || p_fecha_verif || ', ' || p_medido || ', ' || c_ahora || ')';
    END;
BEGIN
    SELECT MIN(id_material) INTO v_material FROM material;
    IF v_material IS NULL THEN
        RAISE_APPLICATION_ERROR(-20022, 'No hay materiales: ejecuta antes database/03_datos_prueba/01_catalogos.sql.');
    END IF;

    INSERT INTO punto_instalacion (nombre, referencia_ubicacion, activo)
    VALUES ('Prueba V003', 'Punto de prueba', 1) RETURNING id_punto_instalacion INTO v_punto;
    INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado)
    VALUES (v_punto, 'Caneca prueba V003', 'PRUEBA_qr_v003_0001', SYS_EXTRACT_UTC(SYSTIMESTAMP), 1)
    RETURNING id_contenedor INTO v_caneca;
    INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado)
    VALUES (v_punto, 'Caneca prueba V003 bis', 'PRUEBA_qr_v003_0002', SYS_EXTRACT_UTC(SYSTIMESTAMP), 1)
    RETURNING id_contenedor INTO v_caneca_otra;
    INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
    VALUES ('Prueba Interaccion', 'prueba.interaccion@example.com', SYS_EXTRACT_UTC(SYSTIMESTAMP))
    RETURNING id_usuario INTO v_usuario;

    -- Interacción
    esperar_error('canal 4',                          interaccion(p_canal => '4'), -2290);
    esperar_error('estado 6',                         interaccion(p_estado => '6'), -2290);
    esperar_error('confianza 1,5',                    interaccion(p_sugerido => v_material, p_confianza => '1.5'), -2290);
    esperar_error('sugerencia sin confianza',         interaccion(p_sugerido => v_material), -2290);
    esperar_error('Autorizada sin material',          interaccion(p_estado => '2'), -2290);
    esperar_error('cerrada sin fecha de cierre',      interaccion(p_estado => '4', p_confirmado => v_material), -2290);
    esperar_error('abierta con fecha de cierre',      interaccion(p_cierre => c_ahora), -2290);
    esperar_error('Rechazada sin motivo',             interaccion(p_estado => '5', p_cierre => c_ahora), -2290);
    esperar_error('motivo con espacios',              interaccion(p_estado => '5', p_cierre => c_ahora, p_motivo => ''' x '''), -2290);
    esperar_error('operario en canal Telegram',       interaccion(p_operario => v_usuario), -2290);
    esperar_error('manual sin operario',              interaccion(p_canal => '2', p_estado => '4', p_motivo => '''Atasco''', p_cierre => c_ahora), -2290);
    esperar_error('manual con material',              interaccion(p_canal => '2', p_estado => '4', p_motivo => '''Atasco''',
                                                          p_cierre => c_ahora, p_operario => v_usuario, p_confirmado => v_material), -2290);
    esperar_error('manual todavía Iniciada',          interaccion(p_canal => '2', p_motivo => '''Atasco''', p_operario => v_usuario), -2290);
    esperar_error('caneca inexistente',               interaccion(p_caneca => -1), -2291);
    esperar_error('material sugerido inexistente',    interaccion(p_sugerido => '-1', p_confianza => '0.5'), -2291);
    esperar_exito('apertura manual válida',           interaccion(p_canal => '2', p_estado => '4', p_motivo => '''Atasco''',
                                                          p_cierre => c_ahora, p_operario => v_usuario));
    esperar_exito('Rechazada con motivo',             interaccion(p_estado => '5', p_cierre => c_ahora, p_motivo => '''Material no admitido'''));

    INSERT INTO interaccion (id_contenedor, id_usuario, canal, estado, id_material_sugerido, confianza_sugerida,
                             id_material_confirmado, fecha_inicio_utc)
    VALUES (v_caneca, v_usuario, 1, 2, v_material, 0.875, v_material, SYS_EXTRACT_UTC(SYSTIMESTAMP))
    RETURNING id_interaccion INTO v_inter;
    registrar(TRUE, 'Autorizada con sugerencia y material confirmado');
    INSERT INTO interaccion (id_contenedor, canal, estado, id_material_confirmado, fecha_inicio_utc)
    VALUES (v_caneca, 3, 2, v_material, SYS_EXTRACT_UTC(SYSTIMESTAMP))
    RETURNING id_interaccion INTO v_inter_2;
    registrar(TRUE, 'visitante sin usuario por el simulador');

    -- Orden de apertura
    esperar_error('clave de 7 caracteres',            orden(v_inter, 'abcdefg'), -2290);
    esperar_error('clave con espacios',               orden(v_inter, ' abcdefgh'), -2290);
    esperar_error('estado 9',                         orden(v_inter, 'clave-0001', p_estado => '9'), -2290);
    esperar_error('vence antes de emitirse',          orden(v_inter, 'clave-0001', p_vence => c_ahora || ' - INTERVAL ''1'' SECOND'), -2290);
    esperar_error('Vencida sin fecha de cierre',      orden(v_inter, 'clave-0001', p_estado => '7'), -2290);
    esperar_error('Emitida con fecha de cierre',      orden(v_inter, 'clave-0001', p_cierre => c_ahora || ' + INTERVAL ''1'' SECOND'), -2290);
    esperar_error('caneca distinta de la interacción', orden(v_inter, 'clave-0001', p_caneca => v_caneca_otra), -2291);

    EXECUTE IMMEDIATE orden(v_inter, 'clave-0001');
    SELECT id_orden INTO v_orden FROM orden_apertura WHERE clave_idempotencia = 'clave-0001';
    registrar(TRUE, 'orden válida se guarda');
    -- Terminal para que solo choque uq_orden_clave y no la regla de una vigente por caneca.
    esperar_error('misma clave de idempotencia',      orden(v_inter_2, 'clave-0001', p_estado => '7', p_cierre => c_ahora || ' + INTERVAL ''1'' SECOND'), -1);
    esperar_error('segunda orden de la interacción',  orden(v_inter, 'clave-0002', p_estado => '7', p_cierre => c_ahora || ' + INTERVAL ''1'' SECOND'), -1);
    esperar_error('segunda orden vigente en la caneca (RN-16)', orden(v_inter_2, 'clave-0002'), -1);
    esperar_exito('orden terminal no cuenta como vigente',
        orden(v_inter_2, 'clave-0003', p_estado => '7', p_cierre => c_ahora || ' + INTERVAL ''1'' SECOND'));

    -- Evento del dispositivo
    esperar_error('tipo 6',                           evento('ev-0001', p_tipo => '6'), -2290);
    esperar_error('resultado 3',                      evento('ev-0001', p_resultado => '3'), -2290);
    esperar_error('aplicado sin orden',               evento('ev-0001', p_orden => 'NULL'), -2290);
    esperar_error('id externo vacío',                 evento(' '), -2290);
    esperar_exito('en revisión sin orden',            evento('ev-0000', p_orden => 'NULL', p_resultado => '2'));
    EXECUTE IMMEDIATE evento('ev-0001');
    SELECT id_evento INTO v_evento FROM evento_dispositivo WHERE id_externo = 'ev-0001';
    registrar(TRUE, 'detección aplicada a la orden');
    esperar_error('retransmisión del mismo id externo', evento('ev-0001', p_resultado => '2', p_orden => 'NULL'), -1);
    SELECT id_orden INTO v_evento_otro FROM orden_apertura WHERE clave_idempotencia = 'clave-0003';
    EXECUTE IMMEDIATE evento('ev-0002', p_orden => TO_CHAR(v_evento_otro), p_resultado => '2');
    SELECT id_evento INTO v_evento_otro FROM evento_dispositivo WHERE id_externo = 'ev-0002';

    -- Depósito
    esperar_error('evento de otra orden',             deposito(v_orden, v_evento_otro), -2291);
    esperar_error('peso medido cero',                 deposito(v_orden, v_evento, p_medido => '0'), -2290);
    esperar_error('verificación incompleta',          deposito(v_orden, v_evento, p_verificado => v_material), -2290);
    esperar_exito('depósito válido con peso nulo', deposito(v_orden, v_evento));
    esperar_error('segundo depósito de la orden (RN-17)', deposito(v_orden, v_evento), -1);

    ROLLBACK;

    DBMS_OUTPUT.PUT_LINE('-----');
    DBMS_OUTPUT.PUT_LINE('Resultado: ' || v_pasan || ' pasan, ' || v_fallan || ' fallan. Se hizo ROLLBACK de todos los datos de prueba.');

    IF v_fallan > 0 THEN
        RAISE_APPLICATION_ERROR(-20023, v_fallan || ' pruebas de interacciones fallaron.');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('Prueba interrumpida; se hizo ROLLBACK. ' || SQLERRM);
        RAISE;
END;
/

WHENEVER SQLERROR CONTINUE
