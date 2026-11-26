-- T004 · Reversión EXCLUSIVA del lote de feria SIM-FERIA-2026 (datos sintéticos de ExpoSoftware).
--
-- Operación MANUAL (no está en 06_migraciones a propósito: ningún instalador la ejecuta sola). Solo con autorización
-- expresa del responsable de la base de datos. Se ejecuta con:
--   powershell -ExecutionPolicy Bypass -File scripts\feria\Revertir-LoteFeria.ps1                                   (solo verifica)
--   powershell -ExecutionPolicy Bypass -File scripts\feria\Revertir-LoteFeria.ps1 -Aplicar -Confirmar SIM-FERIA-2026 (respalda y borra)
--
-- Qué borra (y nada más): todo lo que REGISTRO_SIMULACION atribuye al lote y lo que cuelga de ello:
--   movimientos y operaciones de Ecopuntos de sus depósitos, auditoría de verificación/rechazo, depósitos, eventos
--   sintéticos (id_externo «sim-…») de sus órdenes, órdenes, interacciones, contraseñas, roles y cuentas
--   @feria.ecoaccess.test, y por último las filas de REGISTRO_SIMULACION / LOTE_SIMULACION del lote.
-- Qué NO toca: la Estación Oficial y sus tres canecas, el catálogo, las tarifas, las cuentas reales y su actividad.
--
-- Seguridad:
--   · Se DETIENE sin cambiar nada si encuentra cualquier dependencia ajena al lote (actividad real sobre cuentas del lote,
--     eventos no sintéticos, estaciones o canecas creadas por el lote, un lote todavía EN_CURSO).
--   · Antes de borrar copia cada fila en tablas ECO_RESP_T004_* del mismo esquema (respaldo consultable y restaurable a mano).
--     ECO_RESP_T004_CREDENCIAL guarda hashes con sal (nunca contraseñas en texto).
--   · Crear las tablas de respaldo (DDL) confirma la sesión; todos los borrados van en UNA transacción que se revierte
--     completa si la verificación final detecta que cambió algo ajeno al lote.
--   · Idempotente: si el lote ya no existe, solo lo informa.
DECLARE
    c_lote              CONSTANT VARCHAR2(40) := 'SIM-FERIA-2026';
    c_permitir_en_curso CONSTANT BOOLEAN      := /*PERMITIR_EN_CURSO*/FALSE;
    n        NUMBER;
    borrados NUMBER;
    a_usuarios NUMBER; a_interacciones NUMBER; a_depositos NUMBER; a_eventos NUMBER; a_movimientos NUMBER; a_operaciones NUMBER;
    v_estado lote_simulacion.estado%TYPE;

    PROCEDURE respaldar(p_tabla VARCHAR2, p_select VARCHAR2) IS
        c NUMBER;
    BEGIN
        SELECT COUNT(*) INTO c FROM user_tables WHERE table_name = 'ECO_RESP_T004_' || p_tabla;
        IF c = 0 THEN
            EXECUTE IMMEDIATE 'CREATE TABLE eco_resp_t004_' || p_tabla || ' AS ' || p_select;
        END IF;
        EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM eco_resp_t004_' || p_tabla INTO c;
        DBMS_OUTPUT.PUT_LINE('T004: respaldo ECO_RESP_T004_' || p_tabla || ' = ' || c || ' filas');
    END;

    PROCEDURE exigir_cero(p_texto VARCHAR2, p_cuenta NUMBER) IS
    BEGIN
        IF p_cuenta <> 0 THEN
            RAISE_APPLICATION_ERROR(-20460, 'T004 detenida sin cambios: ' || p_texto || ' (' || p_cuenta || ' filas). Revisar a mano.');
        END IF;
    END;

BEGIN
    -- 0. El lote debe existir y estar terminado (no se compite con un generador que sigue escribiendo).
    BEGIN
        SELECT estado INTO v_estado FROM lote_simulacion WHERE id_lote = c_lote;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        DBMS_OUTPUT.PUT_LINE('T004: el lote ' || c_lote || ' no existe; no hay nada que revertir.');
        RETURN;
    END;
    IF v_estado <> 'COMPLETO' AND NOT c_permitir_en_curso THEN
        RAISE_APPLICATION_ERROR(-20461, 'T004 detenida sin cambios: el lote está ' || v_estado
            || '. Confirma que ningún equipo lo está generando y usa -PermitirEnCurso.');
    END IF;

    -- 1. Precondiciones: nada ajeno al lote depende de lo que se va a borrar.
    SELECT COUNT(*) INTO n FROM registro_simulacion WHERE id_lote = c_lote AND tabla IN ('PUNTO_INSTALACION', 'CONTENEDOR');
    exigir_cero('el lote registra estaciones o canecas (la Estación Oficial no se borra)', n);
    SELECT COUNT(*) INTO n FROM usuario
     WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
       AND correo_electronico NOT LIKE '%@feria.ecoaccess.test';
    exigir_cero('el lote incluye cuentas fuera del dominio @feria.ecoaccess.test', n);
    SELECT COUNT(*) INTO n FROM interaccion
     WHERE id_interaccion NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION')
       AND (id_usuario  IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
         OR id_operario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO'));
    exigir_cero('interacciones reales de cuentas del lote', n);
    SELECT COUNT(*) INTO n FROM deposito
     WHERE id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO')
       AND id_verificador IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    exigir_cero('depósitos reales verificados por cuentas del lote', n);
    SELECT COUNT(*) INTO n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
     WHERE d.id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO')
       AND o.id_interaccion NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    exigir_cero('depósitos del lote sobre interacciones ajenas', n);
    SELECT COUNT(*) INTO n FROM evento_dispositivo
     WHERE id_orden IN (SELECT o.id_orden FROM orden_apertura o
                         WHERE o.id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION'))
       AND NVL(id_externo, '-') NOT LIKE 'sim-%';
    exigir_cero('eventos NO sintéticos en órdenes del lote (posible hardware real)', n);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos
     WHERE (id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
            AND id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO'))
        OR (id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO')
            AND id_usuario NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO'));
    exigir_cero('Ecopuntos que cruzan cuentas reales y el lote', n);
    SELECT COUNT(*) INTO n FROM operacion_ecopuntos
     WHERE id_actor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
       AND id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    exigir_cero('operaciones de Ecopuntos de cuentas del lote sobre depósitos reales', n);
    SELECT COUNT(*) INTO n FROM auditoria_deposito
     WHERE id_actor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
       AND id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    exigir_cero('auditoría de depósitos reales hecha por cuentas del lote', n);
    SELECT COUNT(*) INTO n FROM (
        SELECT id_usuario FROM vinculo_telegram UNION ALL SELECT id_usuario FROM opinion
        UNION ALL SELECT id_usuario FROM comentario_ciudadano UNION ALL SELECT id_usuario FROM vinculacion_pendiente
        UNION ALL SELECT id_usuario_registrado FROM token_vinculacion UNION ALL SELECT id_creador FROM regla_ecopuntos
        UNION ALL SELECT id_usuario FROM operacion_estacion UNION ALL SELECT id_usuario FROM consulta_vision
        UNION ALL SELECT id_actor FROM historial_estado_contenedor)
     WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    exigir_cero('vínculos de Telegram, opiniones, tarifas u operaciones de estación de cuentas del lote', n);
    SELECT COUNT(*) INTO n FROM (
        SELECT id_interaccion FROM analisis_imagen UNION ALL SELECT id_interaccion FROM comentario_ciudadano
        UNION ALL SELECT id_interaccion FROM conversacion_telegram)
     WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    exigir_cero('análisis, comentarios o conversaciones de interacciones del lote', n);

    -- Conteos de lo que NO es del lote: deben quedar iguales al final.
    SELECT COUNT(*) INTO a_usuarios FROM usuario WHERE id_usuario NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    SELECT COUNT(*) INTO a_interacciones FROM interaccion WHERE id_interaccion NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    SELECT COUNT(*) INTO a_depositos FROM deposito WHERE id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    SELECT COUNT(*) INTO a_movimientos FROM movimiento_ecopuntos WHERE id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    SELECT COUNT(*) INTO a_operaciones FROM operacion_ecopuntos WHERE id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    SELECT COUNT(*) INTO a_eventos FROM evento_dispositivo
     WHERE id_orden IS NULL OR id_orden NOT IN (SELECT o.id_orden FROM orden_apertura o
                         WHERE o.id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION'));

    -- 2. Respaldo (antes de cualquier borrado).
    respaldar('LOTE',        'SELECT * FROM lote_simulacion WHERE id_lote = ''' || c_lote || '''');
    respaldar('REGISTRO',    'SELECT * FROM registro_simulacion WHERE id_lote = ''' || c_lote || '''');
    respaldar('USUARIO',     'SELECT * FROM usuario WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''USUARIO'')');
    respaldar('USUARIO_ROL', 'SELECT * FROM usuario_rol WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''USUARIO'')');
    respaldar('CREDENCIAL',  'SELECT * FROM credencial_acceso WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''USUARIO'')');
    respaldar('INTERACCION', 'SELECT * FROM interaccion WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''INTERACCION'')');
    respaldar('ORDEN',       'SELECT * FROM orden_apertura WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''INTERACCION'')');
    respaldar('EVENTO',      'SELECT * FROM evento_dispositivo WHERE id_orden IN (SELECT o.id_orden FROM orden_apertura o WHERE o.id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''INTERACCION''))');
    respaldar('DEPOSITO',    'SELECT * FROM deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''DEPOSITO'')');
    respaldar('AUDITORIA',   'SELECT * FROM auditoria_deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''DEPOSITO'')');
    respaldar('OPERACION',   'SELECT * FROM operacion_ecopuntos WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''DEPOSITO'')');
    respaldar('MOVIMIENTO',  'SELECT * FROM movimiento_ecopuntos WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''DEPOSITO'')');

    -- 3. Ecopuntos: movimientos y operaciones se referencian entre sí (reverso → operación → crédito). Se borra en
    --    rondas lo que ya nadie referencia, hasta vaciar; nunca se desactivan restricciones.
    FOR ronda IN 1 .. 20 LOOP
        DELETE FROM movimiento_ecopuntos m
         WHERE m.id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO')
           AND NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos x WHERE x.id_credito_revertido = m.id_movimiento OR x.id_predecesor = m.id_movimiento)
           AND NOT EXISTS (SELECT 1 FROM operacion_ecopuntos o WHERE o.id_credito = m.id_movimiento);
        borrados := SQL%ROWCOUNT;
        DELETE FROM operacion_ecopuntos o
         WHERE o.id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO')
           AND NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos m WHERE m.id_operacion = o.id_operacion);
        borrados := borrados + SQL%ROWCOUNT;
        DBMS_OUTPUT.PUT_LINE('T004: Ecopuntos ronda ' || ronda || ': ' || borrados || ' filas');
        EXIT WHEN borrados = 0;
    END LOOP;
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    IF n <> 0 THEN RAISE_APPLICATION_ERROR(-20462, 'T004: quedan ' || n || ' movimientos del lote; se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM operacion_ecopuntos WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    IF n <> 0 THEN RAISE_APPLICATION_ERROR(-20463, 'T004: quedan ' || n || ' operaciones del lote; se revierte todo.'); END IF;

    -- 4. Depósitos, eventos, órdenes e interacciones del lote (hijos primero).
    DELETE FROM auditoria_deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    DBMS_OUTPUT.PUT_LINE('T004: auditoria_deposito ' || SQL%ROWCOUNT);
    DELETE FROM deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    DBMS_OUTPUT.PUT_LINE('T004: deposito ' || SQL%ROWCOUNT);
    DELETE FROM evento_dispositivo
     WHERE id_orden IN (SELECT o.id_orden FROM orden_apertura o
                         WHERE o.id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION'))
       AND id_externo LIKE 'sim-%';
    DBMS_OUTPUT.PUT_LINE('T004: evento_dispositivo ' || SQL%ROWCOUNT);
    DELETE FROM orden_apertura WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    DBMS_OUTPUT.PUT_LINE('T004: orden_apertura ' || SQL%ROWCOUNT);
    DELETE FROM interaccion WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    DBMS_OUTPUT.PUT_LINE('T004: interaccion ' || SQL%ROWCOUNT);

    -- 5. Cuentas de demostración.
    DELETE FROM credencial_acceso WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    DBMS_OUTPUT.PUT_LINE('T004: credencial_acceso ' || SQL%ROWCOUNT);
    DELETE FROM usuario_rol WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    DBMS_OUTPUT.PUT_LINE('T004: usuario_rol ' || SQL%ROWCOUNT);
    DELETE FROM usuario WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    DBMS_OUTPUT.PUT_LINE('T004: usuario ' || SQL%ROWCOUNT);
    DELETE FROM registro_simulacion WHERE id_lote = c_lote;
    DBMS_OUTPUT.PUT_LINE('T004: registro_simulacion ' || SQL%ROWCOUNT);
    DELETE FROM lote_simulacion WHERE id_lote = c_lote;

    -- 6. Verificación final en la misma transacción: lo ajeno al lote no cambió y la estación sigue intacta.
    SELECT COUNT(*) INTO n FROM usuario;
    IF n <> a_usuarios THEN RAISE_APPLICATION_ERROR(-20464, 'T004: usuarios reales ' || a_usuarios || ' → ' || n || '; se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM interaccion;
    IF n <> a_interacciones THEN RAISE_APPLICATION_ERROR(-20465, 'T004: interacciones reales ' || a_interacciones || ' → ' || n || '; se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM deposito;
    IF n <> a_depositos THEN RAISE_APPLICATION_ERROR(-20466, 'T004: depósitos reales ' || a_depositos || ' → ' || n || '; se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos;
    IF n <> a_movimientos THEN RAISE_APPLICATION_ERROR(-20467, 'T004: movimientos reales ' || a_movimientos || ' → ' || n || '; se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM operacion_ecopuntos;
    IF n <> a_operaciones THEN RAISE_APPLICATION_ERROR(-20468, 'T004: operaciones reales ' || a_operaciones || ' → ' || n || '; se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM evento_dispositivo;
    IF n <> a_eventos THEN RAISE_APPLICATION_ERROR(-20469, 'T004: eventos reales ' || a_eventos || ' → ' || n || '; se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM contenedor;
    IF n <> 3 THEN RAISE_APPLICATION_ERROR(-20470, 'T004: quedan ' || n || ' canecas (se esperaban 3); se revierte todo.'); END IF;
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('T004: lote ' || c_lote || ' revertido. Respaldo en ECO_RESP_T004_*.');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
