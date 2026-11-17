-- =====================================================================
-- EcoAccess BD — 08 · Disparadores (triggers) del proyecto
-- Ejecutar como ECOACCESS_BD después de 07 (F5). Se puede repetir.
--
-- Los cuatro disparadores de la matriz (★ = los dos para exponer):
--   1   TRG_CONTENEDOR_AUD_ESTADO     AFTER  UPDATE OF estado ON contenedor
--   2 ★ TRG_DEPOSITO_COHERENCIA       BEFORE INSERT OR UPDATE ON deposito
--   3 ★ TRG_DEPOSITO_AUD_VERIFICACION AFTER  UPDATE OF (verificación) ON deposito
--   4   TRG_DEPOSITO_BLOQUEO_CERRADO  BEFORE UPDATE ON deposito WHEN (verificación cerrada)
--
-- Recordatorios de clase que se cumplen aquí:
--   * FOR EACH ROW = el cuerpo corre UNA VEZ POR FILA afectada.
--   * :OLD = la fila como estaba antes; NO cambia ni en BEFORE ni en AFTER.
--     En INSERT todos sus campos son NULL.
--   * :NEW = la fila como va a quedar; solo se puede ASIGNAR en BEFORE de fila.
--     En DELETE todos sus campos son NULL.
--   * En la cláusula WHEN se escribe OLD/NEW SIN dos puntos.
--   * Ningún trigger hace COMMIT: si la sentencia falla, su auditoría
--     también se deshace (pertenece a la misma transacción).
--   * Ningún trigger de DEPOSITO consulta la tabla DEPOSITO (evita ORA-04091,
--     «tabla mutante»); solo consulta tablas relacionadas.
--   * Ningún trigger acredita EcoPuntos (eso lo hace solo PKG_ECOPUNTOS).
-- =====================================================================
SET SERVEROUTPUT ON

-- =====================================================================
-- 1. TRG_CONTENEDOR_AUD_ESTADO
--    Cuándo: DESPUÉS de cambiar el estado de una caneca, una vez por fila,
--            solo si el estado realmente cambió (WHEN).
--    Por qué AFTER: solo se registra lo que de verdad quedó guardado
--            (si un CHECK rechaza el UPDATE, el AFTER ni siquiera corre).
-- =====================================================================
CREATE OR REPLACE TRIGGER trg_contenedor_aud_estado
AFTER UPDATE OF estado ON contenedor
FOR EACH ROW
WHEN (OLD.estado <> NEW.estado)
BEGIN
    INSERT INTO historial_estado_contenedor
        (id_contenedor, estado_anterior, estado_nuevo, id_actor, usuario_bd, motivo)
    VALUES
        (:OLD.id_contenedor, :OLD.estado, :NEW.estado,
         pkg_contexto_app.actor, USER, pkg_contexto_app.motivo);
END;
/
SHOW ERRORS TRIGGER trg_contenedor_aud_estado

-- =====================================================================
-- 4. TRG_DEPOSITO_BLOQUEO_CERRADO
--    Cuándo: ANTES de modificar un depósito que ya fue verificado o rechazado.
--    Qué hace: impide el UPDATE directo. Solo deja pasar el cambio cuando lo
--    hace PKG_DEPOSITO.corregir_verificacion (que exige motivo y queda auditado).
--    Se crea antes que el 2 para poder ordenarlos con FOLLOWS.
-- =====================================================================
CREATE OR REPLACE TRIGGER trg_deposito_bloqueo_cerrado
BEFORE UPDATE ON deposito
FOR EACH ROW
WHEN (OLD.estado_verificacion <> 0)
BEGIN
    IF NOT pkg_deposito.correccion_en_curso THEN
        RAISE_APPLICATION_ERROR(-20103,
            'El depósito ' || :OLD.id_deposito || ' ya está '
            || CASE :OLD.estado_verificacion WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END
            || '. Solo se cambia con PKG_DEPOSITO.corregir_verificacion y un motivo.');
    END IF;
END;
/
SHOW ERRORS TRIGGER trg_deposito_bloqueo_cerrado

-- =====================================================================
-- 2. ★ TRG_DEPOSITO_COHERENCIA
--    Cuándo: ANTES de insertar o modificar cada fila de DEPOSITO.
--    Por qué BEFORE: debe RECHAZAR datos incoherentes antes de guardarlos y,
--    además, COMPLETA :NEW.fecha_deteccion_utc (solo BEFORE puede asignar :NEW).
--    Por qué no basta un CHECK: estas reglas miran OTRAS tablas (titular en
--    INTERACCION, roles en USUARIO_ROL, caneca en CONTENEDOR_MATERIAL, peso en
--    PESO_REFERENCIAL, tipo de evento en EVENTO_DISPOSITIVO). Lo que solo mira
--    la propia fila ya está en ck_deposito_* y NO se repite aquí.
-- =====================================================================
CREATE OR REPLACE TRIGGER trg_deposito_coherencia
BEFORE INSERT OR UPDATE ON deposito
FOR EACH ROW
FOLLOWS trg_deposito_bloqueo_cerrado
DECLARE
    v_titular        interaccion.id_usuario%TYPE;
    v_contenedor     contenedor.id_contenedor%TYPE;
    v_cat_caneca     contenedor.categoria%TYPE;
    v_cat_verif      material.categoria%TYPE;
    v_admitido       NUMBER;
    v_tipo_evento    evento_dispositivo.tipo%TYPE;
    v_min            peso_referencial.gramos_minimo%TYPE;
    v_max            peso_referencial.gramos_maximo%TYPE;
    v_unidades       NUMBER;
BEGIN
    -- (0) Hechos que nunca cambian después del registro (solo en UPDATE).
    IF UPDATING THEN
        IF :NEW.id_orden <> :OLD.id_orden OR :NEW.id_evento <> :OLD.id_evento
           OR :NEW.id_material_declarado <> :OLD.id_material_declarado
           OR :NEW.sin_sensor <> :OLD.sin_sensor
           OR :NEW.fecha_deteccion_utc <> :OLD.fecha_deteccion_utc THEN
            RAISE_APPLICATION_ERROR(-20109,
                'Orden, evento, material declarado, sensor y fecha de detección no se modifican.');
        END IF;
    END IF;

    IF INSERTING THEN
        -- (1) Todo depósito nace PENDIENTE: la verificación es un paso aparte.
        IF :NEW.estado_verificacion <> 0 THEN
            RAISE_APPLICATION_ERROR(-20107, 'Un depósito nuevo debe quedar PENDIENTE (estado 0).');
        END IF;
        -- (2) BEFORE puede escribir :NEW: si no llega la hora, se pone la del servidor.
        IF :NEW.fecha_deteccion_utc IS NULL THEN
            :NEW.fecha_deteccion_utc := SYS_EXTRACT_UTC(SYSTIMESTAMP);
        END IF;
        -- (3) El evento debe ser una detección (tipo 3) o un cierre sin sensor (tipo 6),
        --     coherente con la columna sin_sensor.
        BEGIN
            SELECT tipo INTO v_tipo_evento FROM evento_dispositivo WHERE id_evento = :NEW.id_evento;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                RETURN;   -- evento u orden inexistentes: los rechaza la FK (ORA-02291)
        END;
        IF NOT ((:NEW.sin_sensor = 0 AND v_tipo_evento = 3) OR (:NEW.sin_sensor = 1 AND v_tipo_evento = 6)) THEN
            RAISE_APPLICATION_ERROR(-20108, 'sin_sensor=' || :NEW.sin_sensor
                || ' no coincide con el evento tipo ' || v_tipo_evento || ' (3 detección / 6 cierre sin sensor).');
        END IF;
    END IF;

    -- Datos de la orden: titular (ciudadano) y caneca.
    BEGIN
        SELECT i.id_usuario, o.id_contenedor, c.categoria
        INTO   v_titular, v_contenedor, v_cat_caneca
        FROM   orden_apertura o
        JOIN   interaccion i ON i.id_interaccion = o.id_interaccion
        JOIN   contenedor c  ON c.id_contenedor  = o.id_contenedor
        WHERE  o.id_orden = :NEW.id_orden;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN;       -- orden inexistente: la rechaza la FK (ORA-02291)
    END;

    -- (4) La caneca de la orden admite el material declarado.
    IF INSERTING THEN
        SELECT COUNT(*) INTO v_admitido FROM contenedor_material
        WHERE  id_contenedor = v_contenedor AND id_material = :NEW.id_material_declarado;
        IF v_admitido = 0 THEN
            RAISE_APPLICATION_ERROR(-20102, 'La caneca ' || v_contenedor
                || ' no admite el material declarado ' || :NEW.id_material_declarado || '.');
        END IF;
    END IF;

    -- (5) El peso ESTIMADO cabe en el rango del catálogo × unidades.
    IF :NEW.peso_estimado_kg IS NOT NULL
       AND (INSERTING OR :NEW.peso_estimado_kg <> NVL(:OLD.peso_estimado_kg, -1)
            OR NVL(:NEW.id_peso_referencial, -1) <> NVL(:OLD.id_peso_referencial, -1)) THEN
        BEGIN
            SELECT gramos_minimo, gramos_maximo INTO v_min, v_max
            FROM   peso_referencial WHERE id_peso_referencial = :NEW.id_peso_referencial;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                v_min := NULL;   -- fila de catálogo inexistente: la rechaza la FK o el CHECK
        END;
        v_unidades := NVL(:NEW.cantidad_declarada, 1);
        IF v_min IS NOT NULL AND (:NEW.peso_estimado_kg * 1000 < v_min * v_unidades
           OR :NEW.peso_estimado_kg * 1000 > v_max * v_unidades) THEN
            RAISE_APPLICATION_ERROR(-20106, 'Peso estimado ' || :NEW.peso_estimado_kg
                || ' kg fuera del rango del catálogo para ' || v_unidades || ' unidad(es).');
        END IF;
    END IF;

    -- (6) Verificación independiente: quien verifica o rechaza es Operario o
    --     Administrador activo y NO es el titular del depósito.
    IF :NEW.estado_verificacion IN (1, 2)
       AND (INSERTING OR :OLD.estado_verificacion <> :NEW.estado_verificacion
            OR NVL(:OLD.id_verificador, -1) <> NVL(:NEW.id_verificador, -1)) THEN
        IF :NEW.id_verificador = v_titular THEN
            RAISE_APPLICATION_ERROR(-20101, 'Nadie puede verificar su propio depósito (usuario '
                || v_titular || ').');
        END IF;
        IF pkg_contexto_app.tiene_rol(:NEW.id_verificador, 2) = 0
           AND pkg_contexto_app.tiene_rol(:NEW.id_verificador, 3) = 0 THEN
            RAISE_APPLICATION_ERROR(-20104, 'El verificador ' || :NEW.id_verificador
                || ' no es Operario ni Administrador activo.');
        END IF;
    END IF;

    -- (7) Si el material comprobado es de OTRO color que la caneca (residuo
    --     mal separado), se exige explicarlo en motivo_verificacion.
    IF :NEW.estado_verificacion = 1 AND :NEW.id_material_verificado IS NOT NULL THEN
        BEGIN
            SELECT categoria INTO v_cat_verif FROM material WHERE id_material = :NEW.id_material_verificado;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                v_cat_verif := v_cat_caneca;   -- material inexistente: lo rechaza la FK (ORA-02291), no este trigger
        END;
        IF v_cat_verif <> v_cat_caneca AND :NEW.motivo_verificacion IS NULL THEN
            RAISE_APPLICATION_ERROR(-20105,
                'El material verificado no corresponde al color de la caneca: escribe el motivo.');
        END IF;
    END IF;
END;
/
SHOW ERRORS TRIGGER trg_deposito_coherencia

-- =====================================================================
-- 3. ★ TRG_DEPOSITO_AUD_VERIFICACION
--    Cuándo: DESPUÉS de modificar columnas de la verificación, una vez por fila.
--    Por qué AFTER: se audita solo lo que pasó todas las validaciones (BEFORE,
--    CHECK, FK). :OLD y :NEW aquí solo se LEEN.
--    Cambio real: la función local cambio(a, b) trata NULL = NULL como «igual», así
--    un UPDATE que deja los mismos valores no deja fila de auditoría.
-- =====================================================================
CREATE OR REPLACE TRIGGER trg_deposito_aud_verificacion
AFTER UPDATE OF estado_verificacion, id_material_verificado, cantidad_verificada,
                peso_medido_kg, id_verificador ON deposito
FOR EACH ROW
DECLARE
    v_hubo_cambio NUMBER;
    v_operacion   auditoria_deposito.operacion%TYPE;
    -- 0 si los dos valores son iguales (también si ambos son NULL), 1 si cambió.
    -- (DECODE haría lo mismo, pero solo existe dentro de sentencias SQL.)
    FUNCTION cambio(a NUMBER, b NUMBER) RETURN NUMBER IS
    BEGIN
        IF (a = b) OR (a IS NULL AND b IS NULL) THEN
            RETURN 0;
        END IF;
        RETURN 1;
    END;
BEGIN
    v_hubo_cambio := cambio(:OLD.estado_verificacion,    :NEW.estado_verificacion)
                   + cambio(:OLD.id_material_verificado, :NEW.id_material_verificado)
                   + cambio(:OLD.cantidad_verificada,    :NEW.cantidad_verificada)
                   + cambio(:OLD.peso_medido_kg,         :NEW.peso_medido_kg)
                   + cambio(:OLD.id_verificador,         :NEW.id_verificador);
    IF v_hubo_cambio = 0 THEN
        RETURN;
    END IF;

    v_operacion := CASE
        WHEN :OLD.estado_verificacion = 0 AND :NEW.estado_verificacion = 1 THEN 'VERIFICACION'
        WHEN :OLD.estado_verificacion = 0 AND :NEW.estado_verificacion = 2 THEN 'RECHAZO'
        WHEN :OLD.estado_verificacion <> 0 AND :NEW.estado_verificacion = 0 THEN 'REAPERTURA'
        ELSE 'CORRECCION' END;

    INSERT INTO auditoria_deposito (
        id_deposito, operacion, estado_anterior, estado_nuevo,
        id_material_verificado_anterior, id_material_verificado_nuevo,
        cantidad_anterior, cantidad_nueva,
        peso_medido_anterior_kg, peso_medido_nuevo_kg,
        id_verificador_anterior, id_verificador_nuevo,
        id_actor, usuario_bd, motivo)
    VALUES (
        :NEW.id_deposito, v_operacion, :OLD.estado_verificacion, :NEW.estado_verificacion,
        :OLD.id_material_verificado, :NEW.id_material_verificado,
        :OLD.cantidad_verificada, :NEW.cantidad_verificada,
        :OLD.peso_medido_kg, :NEW.peso_medido_kg,
        :OLD.id_verificador, :NEW.id_verificador,
        pkg_contexto_app.actor, USER, NVL(pkg_contexto_app.motivo, :NEW.motivo_verificacion));
END;
/
SHOW ERRORS TRIGGER trg_deposito_aud_verificacion

SET LINESIZE 200
COLUMN trigger_name     FORMAT A30
COLUMN trigger_type     FORMAT A16
COLUMN triggering_event FORMAT A18
COLUMN table_name       FORMAT A12
PROMPT ==== Estado de los disparadores (los 4 deben estar ENABLED y VALID) ====
SELECT t.trigger_name, t.trigger_type, t.triggering_event, t.table_name, t.status, o.status AS compilacion
FROM   user_triggers t
JOIN   user_objects o ON o.object_name = t.trigger_name AND o.object_type = 'TRIGGER'
ORDER  BY t.table_name, t.trigger_name;
