-- EcoAccess · packages/pkg_deposito.pkb.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "PKG_DEPOSITO" AS
    -- ECOACCESS_V027
    g_correccion BOOLEAN := FALSE;

    FUNCTION correccion_en_curso RETURN BOOLEAN IS
    BEGIN
        RETURN g_correccion;
    END correccion_en_curso;

    FUNCTION estado(p_id_deposito IN deposito.id_deposito%TYPE) RETURN NUMBER IS
        v_material NUMBER;
        v_ultima   VARCHAR2(12);
    BEGIN
        SELECT id_material_verificado INTO v_material FROM deposito WHERE id_deposito = p_id_deposito;
        IF v_material IS NOT NULL THEN
            RETURN 1;
        END IF;
        SELECT MAX(operacion) KEEP (DENSE_RANK LAST ORDER BY id_auditoria) INTO v_ultima
        FROM   auditoria_deposito WHERE id_deposito = p_id_deposito;
        RETURN CASE WHEN v_ultima = 'RECHAZO' THEN 2 ELSE 0 END;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20110, 'No existe el depósito ' || p_id_deposito);
    END estado;

    PROCEDURE exigir_pendiente(p_id_deposito IN NUMBER) IS
        v_material NUMBER;
    BEGIN
        SELECT id_material_verificado INTO v_material FROM deposito WHERE id_deposito = p_id_deposito FOR UPDATE WAIT 10;
        IF v_material IS NOT NULL OR estado(p_id_deposito) = 2 THEN
            RAISE_APPLICATION_ERROR(-20111, 'El depósito ' || p_id_deposito || ' ya está cerrado. Usa corregir_verificacion con un motivo.');
        END IF;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20110, 'No existe el depósito ' || p_id_deposito);
    END exigir_pendiente;

    PROCEDURE verificar(p_id_deposito IN deposito.id_deposito%TYPE, p_id_verificador IN usuario.id_usuario%TYPE,
                        p_id_material IN material.id_material%TYPE, p_cantidad IN deposito.cantidad_verificada%TYPE,
                        p_peso_medido_kg IN deposito.peso_medido_kg%TYPE DEFAULT NULL, p_motivo IN VARCHAR2 DEFAULT NULL) IS
        v_mov     NUMBER;
        v_titular NUMBER;
    BEGIN
        pkg_contexto_app.exigir_rol(p_id_verificador, '2,3');
        exigir_pendiente(p_id_deposito);
        SELECT i.id_usuario INTO v_titular
        FROM   deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion
        WHERE  d.id_deposito = p_id_deposito;
        IF v_titular = p_id_verificador THEN
            RAISE_APPLICATION_ERROR(-20101, 'Nadie puede verificar su propio depósito (usuario ' || v_titular || ').');
        END IF;
        pkg_contexto_app.fijar_actor(p_id_verificador, p_motivo);
        UPDATE deposito
        SET    id_material_verificado = p_id_material,
               cantidad_verificada    = p_cantidad,
               id_verificador         = p_id_verificador,
               fecha_verificacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
               peso_medido_kg         = NVL(p_peso_medido_kg, peso_medido_kg)
        WHERE  id_deposito = p_id_deposito AND id_material_verificado IS NULL;
        -- Misma transacción (sin COMMIT): verificar y acreditar ocurren TODO o NADA. Sin cuenta o sin tarifa: sin puntos.
        IF v_titular IS NOT NULL THEN
            pkg_ecopuntos.acreditar(p_id_deposito, p_id_verificador, v_mov);
        END IF;
        pkg_contexto_app.limpiar;
    EXCEPTION
        WHEN OTHERS THEN
            pkg_contexto_app.limpiar;
            RAISE;
    END verificar;

    PROCEDURE rechazar(p_id_deposito IN deposito.id_deposito%TYPE, p_id_verificador IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2) IS
    BEGIN
        IF TRIM(p_motivo) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20112, 'Rechazar exige un motivo.');
        END IF;
        pkg_contexto_app.exigir_rol(p_id_verificador, '2,3');
        exigir_pendiente(p_id_deposito);
        INSERT INTO auditoria_deposito (id_deposito, operacion, estado_anterior, estado_nuevo, id_verificador_nuevo,
                                        id_actor, usuario_bd, motivo)
        VALUES (p_id_deposito, 'RECHAZO', 0, 2, p_id_verificador, p_id_verificador, USER, SUBSTR(TRIM(p_motivo), 1, 300));
    END rechazar;

    PROCEDURE corregir_verificacion(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE,
                                    p_nuevo_estado IN NUMBER, p_id_material IN material.id_material%TYPE,
                                    p_cantidad IN deposito.cantidad_verificada%TYPE, p_motivo IN VARCHAR2) IS
        v_estado NUMBER;
    BEGIN
        IF TRIM(p_motivo) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20112, 'Una corrección exige un motivo.');
        END IF;
        IF p_nuevo_estado NOT IN (0, 1) THEN
            RAISE_APPLICATION_ERROR(-20113, 'Estado de corrección no válido (0 reabrir, 1 verificado).');
        END IF;
        pkg_contexto_app.exigir_rol(p_id_actor, '3');
        v_estado := estado(p_id_deposito);
        IF v_estado = 0 THEN
            RAISE_APPLICATION_ERROR(-20111, 'El depósito está pendiente: usa verificar o rechazar.');
        END IF;
        IF p_nuevo_estado = 0 AND pkg_ecopuntos.credito_vigente(p_id_deposito) IS NOT NULL THEN
            pkg_ecopuntos.revertir(p_id_deposito, p_id_actor, 'Corrección: ' || TRIM(p_motivo));
        END IF;
        pkg_contexto_app.fijar_actor(p_id_actor, TRIM(p_motivo));
        g_correccion := TRUE;
        UPDATE deposito
        SET    id_material_verificado = CASE WHEN p_nuevo_estado = 1 THEN p_id_material END,
               cantidad_verificada    = CASE WHEN p_nuevo_estado = 1 THEN p_cantidad END,
               id_verificador         = CASE WHEN p_nuevo_estado = 1 THEN p_id_actor END,
               fecha_verificacion_utc = CASE WHEN p_nuevo_estado = 1 THEN SYS_EXTRACT_UTC(SYSTIMESTAMP) END
        WHERE  id_deposito = p_id_deposito;
        g_correccion := FALSE;
        IF p_nuevo_estado = 0 AND v_estado = 2 THEN
            INSERT INTO auditoria_deposito (id_deposito, operacion, estado_anterior, estado_nuevo, id_actor, usuario_bd, motivo)
            VALUES (p_id_deposito, 'REAPERTURA', 2, 0, p_id_actor, USER, SUBSTR(TRIM(p_motivo), 1, 300));
        END IF;
        IF p_nuevo_estado = 1 THEN
            pkg_ecopuntos.recalcular(p_id_deposito, p_id_actor, TRIM(p_motivo));
        END IF;
        pkg_contexto_app.limpiar;
    EXCEPTION
        WHEN OTHERS THEN
            g_correccion := FALSE;
            pkg_contexto_app.limpiar;
            RAISE;
    END corregir_verificacion;
END pkg_deposito;
/
