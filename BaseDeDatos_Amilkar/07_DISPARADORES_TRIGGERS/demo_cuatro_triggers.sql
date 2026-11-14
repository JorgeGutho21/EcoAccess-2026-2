-- =====================================================================
-- EcoAccess BD · Demostración de los CUATRO disparadores: ANTES → SENTENCIA → DESPUÉS
-- Conexión: ECOACCESS_BD (F5 en SQL Developer). No deja cambios: termina en ROLLBACK.
-- Cada bloque imprime el estado antes, la sentencia que dispara el trigger,
-- el estado después y un caso que el trigger debe RECHAZAR.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LINESIZE 220
SET FEEDBACK OFF
DECLARE
    v_admin  NUMBER; v_oper NUMBER; v_dep NUMBER; v_tit NUMBER; v_mat NUMBER; v_cant NUMBER;
    v_estado NUMBER; v_n NUMBER; v_n2 NUMBER;
    PROCEDURE t(p VARCHAR2) IS BEGIN DBMS_OUTPUT.PUT_LINE(p); END;
BEGIN
    SELECT id_usuario INTO v_admin FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT id_usuario INTO v_oper  FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';

    t('======== TRIGGER 1 · TRG_CONTENEDOR_AUD_ESTADO (AFTER UPDATE OF estado, FOR EACH ROW, WHEN) ========');
    SELECT estado INTO v_estado FROM contenedor WHERE id_contenedor = 12;
    SELECT COUNT(*) INTO v_n FROM historial_estado_contenedor WHERE id_contenedor = 12;
    t('ANTES     caneca 12 (verde) estado=' || v_estado || ' · filas de historial=' || v_n);
    t('SENTENCIA pr_cambiar_estado_contenedor(12, 2, administradora, ''Mantenimiento de la tapa'')  → UPDATE contenedor SET estado = 2');
    pr_cambiar_estado_contenedor(12, 2, v_admin, 'Mantenimiento de la tapa');
    SELECT estado INTO v_estado FROM contenedor WHERE id_contenedor = 12;
    SELECT COUNT(*) INTO v_n2 FROM historial_estado_contenedor WHERE id_contenedor = 12;
    t('DESPUÉS   estado=' || v_estado || ' · filas de historial=' || v_n2 || ' (+' || (v_n2 - v_n) || ')');
    FOR h IN (SELECT estado_anterior, estado_nuevo, id_actor, usuario_bd, motivo FROM historial_estado_contenedor
              WHERE id_contenedor = 12 ORDER BY id_historial DESC FETCH FIRST 1 ROWS ONLY) LOOP
        t('          fila nueva: :OLD.estado=' || h.estado_anterior || ' → :NEW.estado=' || h.estado_nuevo || ' · actor=' || h.id_actor || ' · cuenta Oracle=' || h.usuario_bd || ' · motivo=' || h.motivo);
    END LOOP;
    UPDATE contenedor SET estado = estado WHERE id_contenedor = 12;
    SELECT COUNT(*) INTO v_n FROM historial_estado_contenedor WHERE id_contenedor = 12;
    t('SIN CAMBIO UPDATE con el mismo estado → historial sigue en ' || v_n || ' (la cláusula WHEN evitó una fila falsa)');
    BEGIN
        pr_cambiar_estado_contenedor(12, 1, v_oper, 'Intento de un operario');
        t('RECHAZO   (no debería llegar aquí)');
    EXCEPTION WHEN OTHERS THEN t('RECHAZO   un operario intenta cambiar el estado → ' || SUBSTR(SQLERRM, 1, 95));
    END;
    ROLLBACK;

    t(' ');
    t('======== TRIGGER 2 · TRG_DEPOSITO_COHERENCIA (BEFORE INSERT OR UPDATE, FOR EACH ROW) ========');
    SELECT d.id_deposito, i.id_usuario, d.id_material_declarado, NVL(d.cantidad_declarada, 1)
    INTO   v_dep, v_tit, v_mat, v_cant
    FROM   deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion
    WHERE  d.estado_verificacion = 0 AND i.id_usuario NOT IN (v_admin, v_oper) ORDER BY d.id_deposito FETCH FIRST 1 ROWS ONLY;
    t('ANTES     depósito ' || v_dep || ' PENDIENTE · titular=' || v_tit || ' · material declarado=' || v_mat);
    BEGIN
        UPDATE deposito SET estado_verificacion = 1, id_verificador = v_tit, id_material_verificado = v_mat,
               cantidad_verificada = v_cant, fecha_verificacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP)
        WHERE  id_deposito = v_dep;
        t('RECHAZO   (no debería llegar aquí)');
    EXCEPTION WHEN OTHERS THEN t('RECHAZO   el titular intenta verificar su propio depósito → ' || SUBSTR(SQLERRM, 1, 90));
    END;
    t('SENTENCIA verificación por la operaria (persona distinta, con rol 2)');
    pkg_deposito.verificar(v_dep, v_oper, v_mat, v_cant);
    SELECT estado_verificacion INTO v_estado FROM deposito WHERE id_deposito = v_dep;
    t('DESPUÉS   estado_verificacion=' || v_estado || ' (1 = VERIFICADO): el BEFORE dejó pasar la fila coherente');

    t(' ');
    t('======== TRIGGER 3 · TRG_DEPOSITO_AUD_VERIFICACION (AFTER UPDATE OF columnas de verificación) ========');
    FOR a IN (SELECT operacion, estado_anterior, estado_nuevo, id_verificador_anterior, id_verificador_nuevo, id_actor
              FROM auditoria_deposito WHERE id_deposito = v_dep ORDER BY id_auditoria DESC FETCH FIRST 1 ROWS ONLY) LOOP
        t('DESPUÉS   fila de auditoría: ' || a.operacion || ' · :OLD.estado=' || a.estado_anterior || ' → :NEW.estado=' || a.estado_nuevo
          || ' · :OLD.verificador=' || NVL(TO_CHAR(a.id_verificador_anterior), 'NULL') || ' → :NEW.verificador=' || a.id_verificador_nuevo || ' · actor=' || a.id_actor);
    END LOOP;
    SELECT COUNT(*) INTO v_n FROM auditoria_deposito WHERE id_deposito = v_dep;
    t('          filas de auditoría del depósito = ' || v_n);

    t(' ');
    t('======== TRIGGER 4 · TRG_DEPOSITO_BLOQUEO_CERRADO (BEFORE UPDATE, WHEN OLD.estado_verificacion <> 0) ========');
    BEGIN
        UPDATE deposito SET cantidad_verificada = 99 WHERE id_deposito = v_dep;
        t('RECHAZO   (no debería llegar aquí)');
    EXCEPTION WHEN OTHERS THEN t('RECHAZO   UPDATE directo de un depósito ya VERIFICADO → ' || SUBSTR(SQLERRM, 1, 80));
    END;
    t('SENTENCIA corrección autorizada: pkg_deposito.corregir_verificacion(…, administradora, cantidad 1, motivo)');
    pkg_deposito.corregir_verificacion(v_dep, v_admin, 1, v_mat, 1, 'Revisión de cámara: solo había 1 unidad');
    SELECT COUNT(*) INTO v_n2 FROM auditoria_deposito WHERE id_deposito = v_dep;
    t('DESPUÉS   la corrección pasó y el trigger 3 la auditó: filas de auditoría ' || v_n || ' → ' || v_n2);
    FOR a IN (SELECT operacion, cantidad_anterior, cantidad_nueva, motivo FROM auditoria_deposito WHERE id_deposito = v_dep ORDER BY id_auditoria DESC FETCH FIRST 1 ROWS ONLY) LOOP
        t('          ' || a.operacion || ' · :OLD.cantidad=' || a.cantidad_anterior || ' → :NEW.cantidad=' || a.cantidad_nueva || ' · motivo=' || a.motivo);
    END LOOP;

    ROLLBACK;
    SELECT COUNT(*) INTO v_n FROM auditoria_deposito WHERE id_deposito = v_dep;
    t(' ');
    t('ROLLBACK  todo se deshizo, incluida la auditoría (filas del depósito = ' || v_n || '): los triggers no hacen COMMIT propio.');
END;
/
