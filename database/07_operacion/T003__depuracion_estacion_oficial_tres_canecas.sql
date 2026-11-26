-- T003 · Depuración final: solo la Estación Oficial EcoAccess con sus tres canecas físicas.
--
-- Operación MANUAL y única sobre la base compartida (no está en 06_migraciones a propósito: ningún instalador la ejecuta
-- sola). La ejecuta el dueño del esquema con autorización expresa del equipo:
--   powershell -ExecutionPolicy Bypass -File scripts\instalacion\Depurar-EstacionOficial.ps1            (solo verifica)
--   powershell -ExecutionPolicy Bypass -File scripts\instalacion\Depurar-EstacionOficial.ps1 -Aplicar   (respalda y aplica)
--
-- Qué hace (y nada más):
--   1. Nombres visibles de las tres canecas físicas, localizadas por su QR (los identificadores NO cambian, así el QR
--      impreso https://t.me/EcoAccessP3_JG_bot?start=EcoAccessCanecaPiloto1 sigue funcionando):
--        EcoAccessCanecaPiloto1        → «EcoAccess — Caneca blanca»
--        EcoAccessPilotoInternoVerde1  → «EcoAccess — Caneca verde»
--        EcoAccessPilotoInternoNegra1  → «EcoAccess — Caneca negra»
--      y la estación que las contiene → «Estación Oficial EcoAccess».
--   2. Elimina el lote sintético SIM-2026-11-CIERRE (estación «EcoAccess — Estación de SIMULACIÓN», sus tres canecas
--      «SIMULACIÓN — Caneca …», 51 usuarios ficticios, 100 interacciones, 100 órdenes, 336 eventos, 100 depósitos). Todo
--      lo que borra está trazado en REGISTRO_SIMULACION; nada operativo ni académico legítimo depende de ello (la
--      operación se DETIENE sin tocar nada si encuentra cualquier dependencia ajena al lote).
--   3. Antes de borrar copia cada fila afectada en tablas ECO_RESP_T003_* del mismo esquema (respaldo consultable y
--      restaurable con database/07_operacion/reversion/R_T003__restaurar_simulacion.sql).
-- Idempotente: si el lote ya no existe, solo verifica los nombres y termina.
-- Transacciones: crear las tablas de respaldo (DDL) confirma los cambios de nombre del paso 1; todos los borrados del
-- paso 2 van en una sola transacción que se revierte completa si la verificación final falla.
-- Las canecas «DEMO - …» y el punto «DEMO - Bloque ficticio» ya no existen en Oracle (25/11/2026): si reaparecieran
-- (02_datos_demo.sql), esta operación se detiene para revisarlas a mano en lugar de borrarlas a ciegas.
DECLARE
    c_lote CONSTANT VARCHAR2(40) := 'SIM-2026-11-CIERRE';
    n      NUMBER;

    PROCEDURE respaldar(p_tabla VARCHAR2, p_select VARCHAR2) IS
        c NUMBER;
    BEGIN
        SELECT COUNT(*) INTO c FROM user_tables WHERE table_name = 'ECO_RESP_T003_' || p_tabla;
        IF c = 0 THEN
            EXECUTE IMMEDIATE 'CREATE TABLE eco_resp_t003_' || p_tabla || ' AS ' || p_select;
        END IF;
        EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM eco_resp_t003_' || p_tabla INTO c;
        DBMS_OUTPUT.PUT_LINE('T003: respaldo ECO_RESP_T003_' || p_tabla || ' = ' || c || ' filas');
    END;

    PROCEDURE exigir_cero(p_texto VARCHAR2, p_cuenta NUMBER) IS
    BEGIN
        IF p_cuenta <> 0 THEN
            RAISE_APPLICATION_ERROR(-20360, 'T003 detenida sin cambios: ' || p_texto || ' (' || p_cuenta || ' filas). Revisar a mano.');
        END IF;
    END;
BEGIN
    -- 0. La Estación Oficial debe existir con sus tres QR.
    SELECT COUNT(*) INTO n FROM contenedor
     WHERE identificador_qr IN ('EcoAccessCanecaPiloto1', 'EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1');
    IF n <> 3 THEN RAISE_APPLICATION_ERROR(-20361, 'T003: no están las tres canecas físicas de la Estación Oficial (' || n || ').'); END IF;
    SELECT COUNT(DISTINCT id_punto_instalacion) INTO n FROM contenedor
     WHERE identificador_qr IN ('EcoAccessCanecaPiloto1', 'EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1');
    IF n <> 1 THEN RAISE_APPLICATION_ERROR(-20362, 'T003: las tres canecas físicas no están en una sola estación.'); END IF;

    -- DEMO: no debe quedar ninguna; si aparece, se revisa a mano.
    SELECT COUNT(*) INTO n FROM contenedor WHERE UPPER(nombre) LIKE 'DEMO%';
    exigir_cero('existen canecas DEMO', n);

    -- 1. Nombres visibles (los QR no cambian).
    UPDATE contenedor SET nombre = 'EcoAccess — Caneca blanca' WHERE identificador_qr = 'EcoAccessCanecaPiloto1'       AND nombre <> 'EcoAccess — Caneca blanca';
    UPDATE contenedor SET nombre = 'EcoAccess — Caneca verde'  WHERE identificador_qr = 'EcoAccessPilotoInternoVerde1' AND nombre <> 'EcoAccess — Caneca verde';
    UPDATE contenedor SET nombre = 'EcoAccess — Caneca negra'  WHERE identificador_qr = 'EcoAccessPilotoInternoNegra1' AND nombre <> 'EcoAccess — Caneca negra';
    UPDATE punto_instalacion SET nombre = 'Estación Oficial EcoAccess', activo = 1
     WHERE id_punto_instalacion = (SELECT id_punto_instalacion FROM contenedor WHERE identificador_qr = 'EcoAccessCanecaPiloto1')
       AND (nombre <> 'Estación Oficial EcoAccess' OR activo <> 1);

    -- 2. Lote sintético.
    SELECT COUNT(*) INTO n FROM lote_simulacion WHERE id_lote = c_lote;
    IF n = 0 THEN
        COMMIT;
        DBMS_OUTPUT.PUT_LINE('T003: el lote ' || c_lote || ' ya no existe; solo se verificaron los nombres.');
        RETURN;
    END IF;

    -- 2a. Precondiciones: la estación y sus canecas ya están fuera de producción y nada ajeno al lote depende de ellas.
    SELECT COUNT(*) INTO n FROM punto_instalacion
     WHERE activo <> 0 AND id_punto_instalacion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'PUNTO_INSTALACION');
    exigir_cero('la estación de simulación sigue activa', n);
    SELECT COUNT(*) INTO n FROM contenedor
     WHERE identificador_qr IN ('EcoAccessCanecaPiloto1', 'EcoAccessPilotoInternoVerde1', 'EcoAccessPilotoInternoNegra1')
       AND id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR');
    exigir_cero('el lote incluye una caneca física', n);
    SELECT COUNT(*) INTO n FROM interaccion i
     WHERE (i.id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR')
         OR i.id_usuario    IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
         OR i.id_operario   IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO'))
       AND i.id_interaccion NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    exigir_cero('interacciones reales sobre canecas o usuarios del lote', n);
    SELECT COUNT(*) INTO n FROM evento_dispositivo e
     WHERE e.id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR')
       AND e.id_orden NOT IN (SELECT o.id_orden FROM orden_apertura o
                               WHERE o.id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION'));
    exigir_cero('eventos de dispositivo ajenos al lote', n);
    SELECT COUNT(*) INTO n FROM deposito d
     WHERE d.id_evento IN (SELECT e.id_evento FROM evento_dispositivo e
                            WHERE e.id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR'))
       AND d.id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    exigir_cero('depósitos ajenos al lote', n);
    SELECT COUNT(*) INTO n FROM deposito
     WHERE id_verificador IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
       AND id_deposito NOT IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    exigir_cero('depósitos reales verificados por usuarios ficticios', n);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos
     WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
        OR id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    exigir_cero('movimientos de EcoPuntos del lote', n);
    SELECT COUNT(*) INTO n FROM operacion_ecopuntos
     WHERE id_actor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO')
        OR id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    exigir_cero('operaciones de EcoPuntos del lote', n);
    SELECT COUNT(*) INTO n FROM (
        SELECT id_usuario FROM credencial_acceso UNION ALL SELECT id_usuario FROM vinculo_telegram
        UNION ALL SELECT id_usuario FROM opinion UNION ALL SELECT id_usuario FROM comentario_ciudadano
        UNION ALL SELECT id_usuario FROM vinculacion_pendiente UNION ALL SELECT id_usuario_registrado FROM token_vinculacion
        UNION ALL SELECT id_creador FROM regla_ecopuntos UNION ALL SELECT id_usuario FROM operacion_estacion)
     WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    exigir_cero('cuentas, vínculos u opiniones de usuarios ficticios', n);
    SELECT COUNT(*) INTO n FROM (
        SELECT id_interaccion FROM analisis_imagen UNION ALL SELECT id_interaccion FROM comentario_ciudadano
        UNION ALL SELECT id_interaccion FROM conversacion_telegram)
     WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    exigir_cero('análisis, comentarios o conversaciones de interacciones del lote', n);
    SELECT COUNT(*) INTO n FROM conversacion_telegram
     WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR');
    exigir_cero('conversaciones de Telegram sobre canecas del lote', n);

    -- 2b. Respaldo (antes de cualquier borrado).
    respaldar('LOTE',        'SELECT * FROM lote_simulacion WHERE id_lote = ''' || c_lote || '''');
    respaldar('REGISTRO',    'SELECT * FROM registro_simulacion WHERE id_lote = ''' || c_lote || '''');
    respaldar('PUNTO',       'SELECT * FROM punto_instalacion WHERE id_punto_instalacion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''PUNTO_INSTALACION'')');
    respaldar('CONTENEDOR',  'SELECT * FROM contenedor WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''CONTENEDOR'')');
    respaldar('CONT_MAT',    'SELECT * FROM contenedor_material WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''CONTENEDOR'')');
    respaldar('CONFIG',      'SELECT * FROM configuracion_caneca WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''CONTENEDOR'')');
    respaldar('HISTORIAL',   'SELECT * FROM historial_estado_contenedor WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''CONTENEDOR'')');
    respaldar('INTERACCION', 'SELECT * FROM interaccion WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''INTERACCION'')');
    respaldar('ORDEN',       'SELECT * FROM orden_apertura WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''INTERACCION'')');
    respaldar('EVENTO',      'SELECT * FROM evento_dispositivo WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''CONTENEDOR'')');
    respaldar('DEPOSITO',    'SELECT * FROM deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''DEPOSITO'')');
    respaldar('AUDITORIA',   'SELECT * FROM auditoria_deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''DEPOSITO'')');
    respaldar('USUARIO',     'SELECT * FROM usuario WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''USUARIO'')');
    respaldar('USUARIO_ROL', 'SELECT * FROM usuario_rol WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = ''' || c_lote || ''' AND tabla = ''USUARIO'')');

    -- 2c. Borrado en orden de dependencias (hijos primero).
    DELETE FROM auditoria_deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    DBMS_OUTPUT.PUT_LINE('T003: auditoria_deposito ' || SQL%ROWCOUNT);
    DELETE FROM deposito WHERE id_deposito IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'DEPOSITO');
    DBMS_OUTPUT.PUT_LINE('T003: deposito ' || SQL%ROWCOUNT);
    DELETE FROM evento_dispositivo WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR');
    DBMS_OUTPUT.PUT_LINE('T003: evento_dispositivo ' || SQL%ROWCOUNT);
    DELETE FROM orden_apertura WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    DBMS_OUTPUT.PUT_LINE('T003: orden_apertura ' || SQL%ROWCOUNT);
    DELETE FROM interaccion WHERE id_interaccion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'INTERACCION');
    DBMS_OUTPUT.PUT_LINE('T003: interaccion ' || SQL%ROWCOUNT);
    DELETE FROM historial_estado_contenedor WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR');
    DELETE FROM configuracion_caneca WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR');
    DELETE FROM contenedor_material WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR');
    DELETE FROM contenedor WHERE id_contenedor IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'CONTENEDOR');
    DBMS_OUTPUT.PUT_LINE('T003: contenedor ' || SQL%ROWCOUNT);
    DELETE FROM punto_instalacion WHERE id_punto_instalacion IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'PUNTO_INSTALACION');
    DBMS_OUTPUT.PUT_LINE('T003: punto_instalacion ' || SQL%ROWCOUNT);
    DELETE FROM usuario_rol WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    DELETE FROM usuario WHERE id_usuario IN (SELECT id_registro FROM registro_simulacion WHERE id_lote = c_lote AND tabla = 'USUARIO');
    DBMS_OUTPUT.PUT_LINE('T003: usuario ' || SQL%ROWCOUNT);
    DELETE FROM registro_simulacion WHERE id_lote = c_lote;
    DELETE FROM lote_simulacion WHERE id_lote = c_lote;

    -- 2d. Verificación final dentro de la misma transacción: solo la Estación Oficial con tres canecas.
    SELECT COUNT(*) INTO n FROM contenedor;
    IF n <> 3 THEN RAISE_APPLICATION_ERROR(-20363, 'T003: tras depurar quedan ' || n || ' canecas (se esperaban 3); se revierte todo.'); END IF;
    SELECT COUNT(*) INTO n FROM punto_instalacion;
    IF n <> 1 THEN RAISE_APPLICATION_ERROR(-20364, 'T003: tras depurar quedan ' || n || ' estaciones (se esperaba 1); se revierte todo.'); END IF;
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('T003: depuración aplicada. Solo queda la Estación Oficial EcoAccess con sus tres canecas físicas.');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
