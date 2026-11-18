-- =====================================================================
-- EcoAccess P3 — pruebas de la capa académica V027 (PKG_DEPOSITO, PKG_ECOPUNTOS, disparadores, vistas)
-- Crea sus propios datos de prueba con prefijo IT_V027_, verifica cada regla y termina con ROLLBACK:
-- no deja ninguna fila. Se ejecuta en el esquema aislado y, de solo lectura en la práctica, en producción.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE
    v_ok      PLS_INTEGER := 0;
    v_fallo   PLS_INTEGER := 0;
    v_rol1 NUMBER; v_rol2 NUMBER; v_rol3 NUMBER;
    v_ciud NUMBER; v_oper NUMBER; v_admin NUMBER;
    v_punto NUMBER; v_caneca NUMBER; v_mat NUMBER; v_regla NUMBER;
    v_inter NUMBER; v_orden NUMBER; v_evento NUMBER; v_dep NUMBER; v_dep2 NUMBER;
    v_mov NUMBER; n NUMBER; v_cur SYS_REFCURSOR; v_suf VARCHAR2(12) := TO_CHAR(SYSTIMESTAMP, 'HH24MISSFF3');
    v_tipo_evento NUMBER;

    PROCEDURE ok(p_nombre VARCHAR2, p_cond BOOLEAN) IS
    BEGIN
        IF p_cond THEN v_ok := v_ok + 1; DBMS_OUTPUT.PUT_LINE('OK    ' || p_nombre);
        ELSE v_fallo := v_fallo + 1; DBMS_OUTPUT.PUT_LINE('FALLO ' || p_nombre); END IF;
    END;
    PROCEDURE espera_error(p_nombre VARCHAR2, p_codigo NUMBER, p_sql VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE p_sql;
        ok(p_nombre || ' (se esperaba ORA' || p_codigo || ')', FALSE);
    EXCEPTION WHEN OTHERS THEN
        ok(p_nombre || ' -> ORA' || SQLCODE, SQLCODE = p_codigo);
    END;
    FUNCTION usuario_nuevo(p_nombre VARCHAR2, p_rol NUMBER) RETURN NUMBER IS
        v NUMBER;
    BEGIN
        INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, activo)
        VALUES (p_nombre, LOWER(p_nombre) || '@prueba.invalid', SYS_EXTRACT_UTC(SYSTIMESTAMP), 1) RETURNING id_usuario INTO v;
        INSERT INTO usuario_rol (id_usuario, id_rol, fecha_asignacion_utc) VALUES (v, p_rol, SYS_EXTRACT_UTC(SYSTIMESTAMP));
        RETURN v;
    END;
    FUNCTION deposito_nuevo RETURN NUMBER IS
        v NUMBER;
    BEGIN
        INSERT INTO interaccion (id_contenedor, id_usuario, canal, estado, id_material_confirmado, fecha_inicio_utc, fecha_cierre_utc)
        VALUES (v_caneca, v_ciud, 1, 3, v_mat, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '2' MINUTE, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' MINUTE)
        RETURNING id_interaccion INTO v_inter;
        INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado, fecha_emision_utc, fecha_vencimiento_utc, fecha_cierre_utc)
        VALUES (v_inter, v_caneca, 'IT_V027_' || v_suf || '_' || v_inter, 6, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '2' MINUTE,
                SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL '1' MINUTE, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' MINUTE)
        RETURNING id_orden INTO v_orden;
        INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado, fecha_recepcion_utc)
        VALUES (v_caneca, 'IT_V027_' || v_suf || '_' || v_orden, v_tipo_evento, v_orden, 1, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' MINUTE)
        RETURNING id_evento INTO v_evento;
        INSERT INTO deposito (id_orden, id_evento, id_material_declarado, fecha_deteccion_utc, sin_sensor)
        VALUES (v_orden, v_evento, v_mat, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' MINUTE, CASE WHEN v_tipo_evento = 6 THEN 1 ELSE 0 END)
        RETURNING id_deposito INTO v;
        RETURN v;
    END;
BEGIN
    SELECT id_rol INTO v_rol1 FROM rol WHERE codigo = 1;
    SELECT id_rol INTO v_rol2 FROM rol WHERE codigo = 2;
    SELECT id_rol INTO v_rol3 FROM rol WHERE codigo = 3;
    v_ciud  := usuario_nuevo('IT_V027_CIUDADANO_' || v_suf, v_rol1);
    v_oper  := usuario_nuevo('IT_V027_OPERARIO_' || v_suf, v_rol2);
    v_admin := usuario_nuevo('IT_V027_ADMIN_' || v_suf, v_rol3);
    INSERT INTO punto_instalacion (nombre, activo) VALUES ('IT_V027 punto ' || v_suf, 1) RETURNING id_punto_instalacion INTO v_punto;
    INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado)
    VALUES (v_punto, 'IT_V027 caneca ' || v_suf, 'ITV027_QR_' || v_suf || '_PRUEBA', SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' DAY, 1)
    RETURNING id_contenedor INTO v_caneca;
    SELECT id_material INTO v_mat FROM material WHERE codigo = 'PET';
    INSERT INTO contenedor_material (id_contenedor, id_material) VALUES (v_caneca, v_mat);
    -- Tipo de evento de un depósito real existente (3 detección o 6 cierre sin sensor, según el esquema).
    SELECT NVL(MAX(e.tipo), 6) INTO v_tipo_evento FROM deposito d JOIN evento_dispositivo e ON e.id_evento = d.id_evento WHERE ROWNUM = 1;
    v_regla := pkg_ecopuntos.regla_vigente(v_mat, SYS_EXTRACT_UTC(SYSTIMESTAMP));
    IF v_regla IS NULL THEN
        INSERT INTO regla_ecopuntos (id_material, puntos, descripcion, desde_utc, id_creador, por_unidad)
        VALUES (v_mat, 5, 'IT_V027 tarifa', SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' DAY, v_admin, 1) RETURNING id_regla INTO v_regla;
    END IF;

    v_dep := deposito_nuevo;
    ok('Depósito nuevo queda PENDIENTE', pkg_deposito.estado(v_dep) = 0);
    -- 1. Coherencia al insertar: la caneca de prueba solo admite PET (el disparador actúa antes que las restricciones)
    espera_error('TRG_DEPOSITO_COHERENCIA rechaza material no admitido', -20102,
        'INSERT INTO deposito (id_orden, id_evento, id_material_declarado, fecha_deteccion_utc, sin_sensor) SELECT id_orden, id_evento, '
        || '(SELECT id_material FROM material WHERE codigo = ''ORGANICO''), fecha_deteccion_utc, sin_sensor FROM deposito WHERE id_deposito = ' || v_dep);

    -- 2. Verificar: el titular no se verifica a sí mismo; el operario sí; acredita en el mismo libro
    BEGIN pkg_deposito.verificar(v_dep, v_ciud, v_mat, 2); ok('Ciudadano no puede verificar', FALSE);
    EXCEPTION WHEN OTHERS THEN ok('Ciudadano no puede verificar -> ORA' || SQLCODE, SQLCODE = -20120); END;
    pkg_deposito.verificar(v_dep, v_oper, v_mat, 2);
    ok('PKG_DEPOSITO.verificar deja VERIFICADO', pkg_deposito.estado(v_dep) = 1);
    SELECT COUNT(*) INTO n FROM auditoria_deposito WHERE id_deposito = v_dep AND operacion = 'VERIFICACION';
    ok('TRG_DEPOSITO_AUD_VERIFICACION audita la verificación', n = 1);
    v_mov := pkg_ecopuntos.credito_vigente(v_dep);
    ok('PKG_ECOPUNTOS acreditó en MOVIMIENTO_ECOPUNTOS', v_mov IS NOT NULL);
    SELECT COUNT(*) INTO n FROM operacion_ecopuntos WHERE id_deposito = v_dep AND tipo = 1;
    ok('OPERACION_ECOPUNTOS registra la operación de crédito', n = 1);
    ok('Saldo del ciudadano > 0', pkg_ecopuntos.saldo(v_ciud) > 0);
    pkg_ecopuntos.acreditar(v_dep, v_oper, n);
    ok('Acreditar dos veces es idempotente', n = v_mov);

    -- 3. Bloqueo: un depósito verificado no se modifica fuera de PKG_DEPOSITO
    espera_error('TRG_DEPOSITO_BLOQUEO_CERRADO', -20103, 'UPDATE deposito SET cantidad_verificada = 3 WHERE id_deposito = ' || v_dep);

    -- 4. Corregir: solo administrador; reabrir revierte los puntos; volver a verificar acredita con predecesor
    BEGIN pkg_deposito.corregir_verificacion(v_dep, v_oper, 0, NULL, NULL, 'prueba'); ok('Operario no corrige', FALSE);
    EXCEPTION WHEN OTHERS THEN ok('Operario no corrige -> ORA' || SQLCODE, SQLCODE = -20120); END;
    pkg_deposito.corregir_verificacion(v_dep, v_admin, 1, v_mat, 3, 'Conteo corregido en prueba');
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos WHERE id_deposito = v_dep AND tipo = 2;
    ok('Corrección revierte el crédito anterior', n = 1);
    ok('Corrección deja un crédito vigente nuevo', pkg_ecopuntos.credito_vigente(v_dep) IS NOT NULL AND pkg_ecopuntos.credito_vigente(v_dep) <> v_mov);
    pkg_deposito.corregir_verificacion(v_dep, v_admin, 0, NULL, NULL, 'Reapertura de prueba');
    ok('Reabrir deja PENDIENTE', pkg_deposito.estado(v_dep) = 0);
    ok('Reabrir deja saldo del depósito en 0', pkg_ecopuntos.credito_vigente(v_dep) IS NULL);

    -- 5. Rechazar
    v_dep2 := deposito_nuevo;
    BEGIN pkg_deposito.rechazar(v_dep2, v_oper, ' '); ok('Rechazo sin motivo', FALSE);
    EXCEPTION WHEN OTHERS THEN ok('Rechazo exige motivo -> ORA' || SQLCODE, SQLCODE = -20112); END;
    pkg_deposito.rechazar(v_dep2, v_oper, 'No corresponde a la caneca');
    ok('PKG_DEPOSITO.rechazar deja RECHAZADO', pkg_deposito.estado(v_dep2) = 2);
    BEGIN pkg_deposito.verificar(v_dep2, v_oper, v_mat, 1); ok('No se verifica un rechazado', FALSE);
    EXCEPTION WHEN OTHERS THEN ok('No se verifica un rechazado -> ORA' || SQLCODE, SQLCODE = -20111); END;

    -- 6. Coherencia en actualización: datos inmutables
    espera_error('TRG_DEPOSITO_COHERENCIA protege la fecha de detección', -20109,
        'UPDATE deposito SET fecha_deteccion_utc = fecha_deteccion_utc - INTERVAL ''1'' DAY WHERE id_deposito = ' || v_dep2);

    -- 7. Estado de caneca auditado
    BEGIN pr_cambiar_estado_contenedor(v_caneca, 2, v_oper, 'prueba'); ok('Operario no cambia estado', FALSE);
    EXCEPTION WHEN OTHERS THEN ok('Solo administrador cambia estado -> ORA' || SQLCODE, SQLCODE = -20120); END;
    pr_cambiar_estado_contenedor(v_caneca, 2, v_admin, 'Mantenimiento de prueba');
    SELECT COUNT(*) INTO n FROM historial_estado_contenedor WHERE id_contenedor = v_caneca AND estado_nuevo = 2 AND id_actor = v_admin;
    ok('TRG_CONTENEDOR_AUD_ESTADO guarda la historia con el actor', n = 1);

    -- 8. Consultas, función y vistas
    pkg_estadisticas.consultar_resumen(SYSDATE - 1, SYSDATE + 1, v_punto, NULL, v_cur);
    DECLARE a NUMBER; b NUMBER; c NUMBER; d NUMBER; e NUMBER; f NUMBER; g NUMBER; h NUMBER;
    BEGIN FETCH v_cur INTO n, a, b, c, d, e, f, g, h; CLOSE v_cur; END;
    ok('PKG_ESTADISTICAS.consultar_resumen cuenta los 2 depósitos', n = 2);
    v_cur := fn_depositos_ciudadano(v_ciud);
    n := 0;
    DECLARE r vw_deposito_detalle.id_deposito%TYPE; f DATE; c VARCHAR2(10); a VARCHAR2(40); b VARCHAR2(40); e VARCHAR2(20); x VARCHAR2(30); g NUMBER; h NUMBER;
    BEGIN LOOP FETCH v_cur INTO r, f, c, a, b, e, x, g, h; EXIT WHEN v_cur%NOTFOUND; n := n + 1; END LOOP; CLOSE v_cur; END;
    ok('FN_DEPOSITOS_CIUDADANO devuelve sus 2 depósitos', n = 2);
    SELECT COUNT(*) INTO n FROM vw_bi_depositos WHERE ciudadano = 'C-' || LPAD(v_ciud, 4, '0') AND estado IN ('PENDIENTE', 'RECHAZADO');
    ok('VW_BI_DEPOSITOS refleja los estados', n = 2);
    SELECT COUNT(*) INTO n FROM vw_bi_auditoria WHERE id_deposito IN (v_dep, v_dep2);
    ok('VW_BI_AUDITORIA muestra la auditoría', n >= 4);
    SELECT COUNT(*) INTO n FROM vw_bi_ecopuntos WHERE ciudadano = 'C-' || LPAD(v_ciud, 4, '0');
    ok('VW_BI_ECOPUNTOS muestra créditos y reversos', n >= 3);
    SELECT id_peso_referencial INTO n FROM peso_referencial WHERE clave_objeto = 'BOTELLA_PET_600ML' AND estado_visual = 1;
    ok('FN_PESO_ESTIMADO_KG: 2 botellas PET de 600 ml = 0,044 kg', fn_peso_estimado_kg(n, 2) = 0.044);

    DBMS_OUTPUT.PUT_LINE('RESULTADO V027: ' || v_ok || ' correctas, ' || v_fallo || ' fallidas.');
    ROLLBACK;
END;
/
WHENEVER SQLERROR CONTINUE
