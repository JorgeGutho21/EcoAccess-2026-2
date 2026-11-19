-- EcoAccess · packages/pkg_ecopuntos.pkb.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "PKG_ECOPUNTOS" AS
    -- ECOACCESS_V027
    FUNCTION ahora RETURN TIMESTAMP IS
    BEGIN
        RETURN SYS_EXTRACT_UTC(SYSTIMESTAMP);
    END ahora;

    FUNCTION regla_vigente(p_id_material IN material.id_material%TYPE, p_fecha_utc IN TIMESTAMP) RETURN regla_ecopuntos.id_regla%TYPE IS
        v_id regla_ecopuntos.id_regla%TYPE;
    BEGIN
        SELECT id_regla INTO v_id FROM regla_ecopuntos
        WHERE  id_material = p_id_material AND desde_utc <= p_fecha_utc AND (hasta_utc IS NULL OR hasta_utc > p_fecha_utc);
        RETURN v_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN RETURN NULL;
        WHEN TOO_MANY_ROWS THEN RAISE_APPLICATION_ERROR(-20203, 'Hay dos tarifas vigentes para el material ' || p_id_material);
    END regla_vigente;

    FUNCTION credito_vigente(p_id_deposito IN deposito.id_deposito%TYPE) RETURN movimiento_ecopuntos.id_movimiento%TYPE IS
        v_id movimiento_ecopuntos.id_movimiento%TYPE;
    BEGIN
        SELECT MAX(m.id_movimiento) INTO v_id FROM movimiento_ecopuntos m
        WHERE  m.id_deposito = p_id_deposito AND m.tipo = 1
        AND    NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos r WHERE r.id_credito_revertido = m.id_movimiento);
        RETURN v_id;
    END credito_vigente;

    -- Bloqueos en el mismo orden que la aplicación C#: material → actor → depósito.
    PROCEDURE preparar(p_id_deposito IN NUMBER, p_id_actor IN NUMBER, p_roles IN VARCHAR2,
                       p_titular OUT NUMBER, p_material OUT NUMBER, p_cantidad OUT NUMBER) IS
        v_verificador NUMBER;
        v_tmp         NUMBER;
    BEGIN
        BEGIN
            SELECT id_material_verificado INTO p_material FROM deposito WHERE id_deposito = p_id_deposito;
        EXCEPTION WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20110, 'No existe el depósito ' || p_id_deposito);
        END;
        IF p_material IS NULL THEN
            RAISE_APPLICATION_ERROR(-20201, 'Solo se acreditan depósitos VERIFICADOS (depósito ' || p_id_deposito || ').');
        END IF;
        SELECT id_material INTO v_tmp FROM material WHERE id_material = p_material FOR UPDATE WAIT 10;
        SELECT id_usuario INTO v_tmp FROM usuario WHERE id_usuario = p_id_actor FOR UPDATE WAIT 10;
        pkg_contexto_app.exigir_rol(p_id_actor, p_roles);
        SELECT i.id_usuario, d.id_verificador, d.id_material_verificado, d.cantidad_verificada
        INTO   p_titular, v_verificador, p_material, p_cantidad
        FROM   deposito d
        JOIN   orden_apertura o ON o.id_orden = d.id_orden
        JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
        WHERE  d.id_deposito = p_id_deposito
        FOR UPDATE OF d.id_material_verificado WAIT 10;
        IF p_titular IS NULL THEN
            RAISE_APPLICATION_ERROR(-20208, 'El depósito no tiene titular con cuenta: no genera ecopuntos.');
        END IF;
        IF p_titular = p_id_actor OR p_titular = v_verificador THEN
            RAISE_APPLICATION_ERROR(-20101, 'Se requiere verificación y acreditación independientes del titular.');
        END IF;
    END preparar;

    FUNCTION calcular(p_id_regla IN NUMBER, p_cantidad IN NUMBER) RETURN NUMBER IS
        v_puntos NUMBER;
        v_unidad NUMBER;
    BEGIN
        SELECT puntos, por_unidad INTO v_puntos, v_unidad FROM regla_ecopuntos WHERE id_regla = p_id_regla;
        IF v_unidad = 1 THEN
            IF p_cantidad IS NULL THEN
                RAISE_APPLICATION_ERROR(-20204, 'La tarifa es por unidad y el depósito no tiene conteo verificado.');
            END IF;
            v_puntos := v_puntos * p_cantidad;
            IF v_puntos > 1000000 THEN
                RAISE_APPLICATION_ERROR(-20204, 'El total supera el máximo de un crédito.');
            END IF;
        END IF;
        RETURN v_puntos;
    END calcular;

    FUNCTION operacion(p_clave VARCHAR2, p_tipo NUMBER, p_id_deposito NUMBER, p_id_actor NUMBER, p_id_credito NUMBER,
                       p_id_regla NUMBER, p_motivo VARCHAR2) RETURN NUMBER IS
        v_id    NUMBER;
        v_fecha TIMESTAMP := ahora;   -- una función privada no se puede usar dentro de SQL (PLS-00231)
    BEGIN
        INSERT INTO operacion_ecopuntos (clave, tipo, id_deposito, id_actor, id_credito, id_regla, motivo, fecha_utc)
        VALUES (p_clave, p_tipo, p_id_deposito, p_id_actor, p_id_credito, p_id_regla, SUBSTR(TRIM(p_motivo), 1, 300), v_fecha)
        RETURNING id_operacion INTO v_id;
        RETURN v_id;
    END operacion;

    FUNCTION movimiento(p_operacion NUMBER, p_id_deposito NUMBER, p_id_usuario NUMBER, p_id_regla NUMBER, p_tipo NUMBER,
                        p_puntos NUMBER, p_revertido NUMBER, p_predecesor NUMBER) RETURN NUMBER IS
        v_id NUMBER;
    BEGIN
        INSERT INTO movimiento_ecopuntos (id_operacion, id_deposito, id_usuario, id_regla, tipo, puntos, id_credito_revertido, id_predecesor)
        VALUES (p_operacion, p_id_deposito, p_id_usuario, p_id_regla, p_tipo, p_puntos, p_revertido, p_predecesor)
        RETURNING id_movimiento INTO v_id;
        RETURN v_id;
    END movimiento;

    PROCEDURE acreditar(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE,
                        p_id_movimiento OUT movimiento_ecopuntos.id_movimiento%TYPE) IS
        v_titular NUMBER; v_material NUMBER; v_cantidad NUMBER; v_regla NUMBER; v_op NUMBER; v_previos NUMBER;
    BEGIN
        preparar(p_id_deposito, p_id_actor, '2,3', v_titular, v_material, v_cantidad);
        p_id_movimiento := credito_vigente(p_id_deposito);
        IF p_id_movimiento IS NOT NULL THEN
            RETURN;                                    -- idempotente: el crédito ya existe
        END IF;
        SELECT COUNT(*) INTO v_previos FROM movimiento_ecopuntos WHERE id_deposito = p_id_deposito;
        IF v_previos > 0 THEN
            RAISE_APPLICATION_ERROR(-20202, 'El depósito tuvo un crédito revertido: usa PKG_DEPOSITO.corregir_verificacion.');
        END IF;
        v_regla := regla_vigente(v_material, ahora);
        IF v_regla IS NULL THEN
            p_id_movimiento := NULL;                   -- sin tarifa aprobada: el saldo no cambia
            RETURN;
        END IF;
        v_op := operacion('BD-CRED-' || p_id_deposito, 1, p_id_deposito, p_id_actor, NULL, v_regla, 'Crédito por depósito verificado (PKG_ECOPUNTOS)');
        p_id_movimiento := movimiento(v_op, p_id_deposito, v_titular, v_regla, 1, calcular(v_regla, v_cantidad), NULL, NULL);
    END acreditar;

    PROCEDURE revertir(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2) IS
        v_titular NUMBER; v_material NUMBER; v_cantidad NUMBER; v_credito NUMBER; v_op NUMBER; v_mov NUMBER;
        v_regla NUMBER; v_puntos NUMBER;
    BEGIN
        IF TRIM(p_motivo) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20112, 'El reverso exige un motivo.');
        END IF;
        preparar(p_id_deposito, p_id_actor, '3', v_titular, v_material, v_cantidad);
        v_credito := credito_vigente(p_id_deposito);
        IF v_credito IS NULL THEN
            RAISE_APPLICATION_ERROR(-20205, 'El depósito ' || p_id_deposito || ' no tiene crédito vigente.');
        END IF;
        SELECT id_regla, puntos INTO v_regla, v_puntos FROM movimiento_ecopuntos WHERE id_movimiento = v_credito;
        v_op := operacion('BD-REV-' || v_credito, 2, p_id_deposito, p_id_actor, v_credito, NULL, p_motivo);
        v_mov := movimiento(v_op, p_id_deposito, v_titular, v_regla, 2, -v_puntos, v_credito, NULL);
    END revertir;

    PROCEDURE recalcular(p_id_deposito IN deposito.id_deposito%TYPE, p_id_actor IN usuario.id_usuario%TYPE, p_motivo IN VARCHAR2) IS
        v_titular NUMBER; v_material NUMBER; v_cantidad NUMBER; v_credito NUMBER; v_op NUMBER; v_mov NUMBER;
        v_regla_ant NUMBER; v_puntos_ant NUMBER; v_regla NUMBER; v_ultimo NUMBER;
    BEGIN
        IF TRIM(p_motivo) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20112, 'Una corrección exige un motivo.');
        END IF;
        preparar(p_id_deposito, p_id_actor, '3', v_titular, v_material, v_cantidad);
        v_regla := regla_vigente(v_material, ahora);
        v_credito := credito_vigente(p_id_deposito);
        IF v_regla IS NULL THEN
            IF v_credito IS NOT NULL THEN
                revertir(p_id_deposito, p_id_actor, p_motivo);
            END IF;
            RETURN;
        END IF;
        IF v_credito IS NOT NULL THEN
            SELECT id_regla, puntos INTO v_regla_ant, v_puntos_ant FROM movimiento_ecopuntos WHERE id_movimiento = v_credito;
            v_op := operacion('BD-COR-' || v_credito, 3, p_id_deposito, p_id_actor, v_credito, v_regla, p_motivo);
            v_mov := movimiento(v_op, p_id_deposito, v_titular, v_regla_ant, 2, -v_puntos_ant, v_credito, NULL);
            v_mov := movimiento(v_op, p_id_deposito, v_titular, v_regla, 1, calcular(v_regla, v_cantidad), NULL, v_credito);
        ELSE
            SELECT MAX(id_movimiento) INTO v_ultimo FROM movimiento_ecopuntos WHERE id_deposito = p_id_deposito AND tipo = 1;
            IF v_ultimo IS NULL THEN
                acreditar(p_id_deposito, p_id_actor, v_mov);
            ELSE
                v_op := operacion('BD-COR-' || v_ultimo || '-N', 3, p_id_deposito, p_id_actor, v_ultimo, v_regla, p_motivo);
                v_mov := movimiento(v_op, p_id_deposito, v_titular, v_regla, 1, calcular(v_regla, v_cantidad), NULL, v_ultimo);
            END IF;
        END IF;
    END recalcular;

    FUNCTION saldo(p_id_usuario IN usuario.id_usuario%TYPE) RETURN NUMBER IS
        v_saldo NUMBER;
    BEGIN
        SELECT NVL(SUM(puntos), 0) INTO v_saldo FROM movimiento_ecopuntos WHERE id_usuario = p_id_usuario;
        RETURN v_saldo;
    END saldo;
END pkg_ecopuntos;
/
