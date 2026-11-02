-- =====================================================================
-- EcoAccess P3 — pruebas de las consultas de estadísticas
-- Archivo: database/05_pruebas/04_pruebas_estadisticas.sql
--
-- Mismo formato que 03_pruebas_interacciones.sql: cada caso imprime PASA o
-- FALLA y al final se hace ROLLBACK. Requiere 001, V002 y V003 y al menos dos
-- materiales. Crea su propia caneca y usuarios en el año 2001, así ningún dato
-- real cae en el período y las cifras esperadas son exactas.
--
-- Escenario (fechas UTC):
--   i1 Telegram 2001-03-01 08:30 -> orden 1 con apertura aplicada y depósito
--      material A, 0,125 kg medidos, verificado como B; orden cerrada.
--   i2 Telegram 2001-03-02 08:30 -> orden 2 con depósito material B, 0,050 kg
--      estimados, sin verificar, a las 2001-03-04 04:30 UTC (día 03 en Colombia).
--   i3 Telegram 2001-03-03 08:30 -> orden 3 vencida, sin depósito.
--   i4 Simulador 2001-03-03 09:00 rechazada.
--   Un evento de fallo huérfano (tipo 5, en revisión) y un depósito en el
--   límite superior del período, que no debe contarse.
-- =====================================================================

SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_pasan    PLS_INTEGER := 0;
    v_fallan   PLS_INTEGER := 0;
    v_punto    NUMBER;
    v_caneca   NUMBER;
    v_usuario  NUMBER;
    v_operario NUMBER;
    v_mat_a    NUMBER;
    v_mat_b    NUMBER;
    v_i1 NUMBER; v_i2 NUMBER; v_i3 NUMBER; v_i5 NUMBER;
    v_o1 NUMBER; v_o2 NUMBER; v_o5 NUMBER;
    v_ev NUMBER;
    v_n1 NUMBER; v_n2 NUMBER; v_n3 NUMBER; v_n4 NUMBER;
    v_kg_medidos   NUMBER;
    v_kg_estimados NUMBER;
    v_dia VARCHAR2(10);
    c_desde CONSTANT TIMESTAMP := TIMESTAMP '2001-02-28 00:00:00';
    c_hasta CONSTANT TIMESTAMP := TIMESTAMP '2001-03-06 00:00:00';

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

    PROCEDURE esperar(p_caso VARCHAR2, p_obtenido NUMBER, p_esperado NUMBER) IS
    BEGIN
        registrar(p_obtenido = p_esperado, p_caso, 'se obtuvo ' || p_obtenido || ', se esperaba ' || p_esperado);
    END;

    FUNCTION interaccion(p_inicio TIMESTAMP, p_canal NUMBER := 1) RETURN NUMBER IS
        v_id NUMBER;
    BEGIN
        INSERT INTO interaccion (id_contenedor, id_usuario, canal, estado, id_material_confirmado, fecha_inicio_utc)
        VALUES (v_caneca, v_usuario, p_canal, 2, v_mat_a, p_inicio)
        RETURNING id_interaccion INTO v_id;
        RETURN v_id;
    END;

    FUNCTION orden(p_inter NUMBER, p_emision TIMESTAMP, p_clave VARCHAR2) RETURN NUMBER IS
        v_id NUMBER;
    BEGIN
        INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado,
                                    fecha_emision_utc, fecha_vencimiento_utc)
        VALUES (p_inter, v_caneca, p_clave, 1, p_emision, p_emision + INTERVAL '2' MINUTE)
        RETURNING id_orden INTO v_id;
        RETURN v_id;
    END;

    FUNCTION evento(p_externo VARCHAR2, p_tipo NUMBER, p_orden NUMBER, p_resultado NUMBER, p_fecha TIMESTAMP)
        RETURN NUMBER IS
        v_id NUMBER;
    BEGIN
        INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado, fecha_recepcion_utc)
        VALUES (v_caneca, p_externo, p_tipo, p_orden, p_resultado, p_fecha)
        RETURNING id_evento INTO v_id;
        RETURN v_id;
    END;

    -- Cierra una interacción y su orden con los estados indicados.
    PROCEDURE cerrar(p_inter NUMBER, p_estado_inter NUMBER, p_orden NUMBER, p_estado_orden NUMBER, p_fecha TIMESTAMP) IS
    BEGIN
        UPDATE orden_apertura SET estado = p_estado_orden, fecha_cierre_utc = p_fecha WHERE id_orden = p_orden;
        UPDATE interaccion SET estado = p_estado_inter, fecha_cierre_utc = p_fecha WHERE id_interaccion = p_inter;
    END;

    -- Cuenta depósitos de la caneca de prueba con el filtro de verificación (0, 1, 2) y el criterio (1, 2).
    FUNCTION depositos(p_verificacion NUMBER, p_criterio NUMBER, p_material NUMBER := NULL) RETURN NUMBER IS
        v_total NUMBER;
    BEGIN
        SELECT COUNT(*) INTO v_total
          FROM deposito d
          JOIN orden_apertura o ON o.id_orden = d.id_orden
         WHERE d.fecha_deteccion_utc >= c_desde AND d.fecha_deteccion_utc < c_hasta
           AND o.id_contenedor = v_caneca
           AND (p_criterio = 1 OR d.id_material_verificado IS NOT NULL)
           AND (p_material IS NULL
                OR (CASE WHEN p_criterio = 2 THEN d.id_material_verificado ELSE d.id_material_declarado END) = p_material)
           AND (p_verificacion = 0
                OR (p_verificacion = 1 AND d.id_material_verificado IS NULL)
                OR (p_verificacion = 2 AND d.id_material_verificado IS NOT NULL));
        RETURN v_total;
    END;
BEGIN
    SELECT MIN(id_material), MAX(id_material) INTO v_mat_a, v_mat_b FROM material;
    IF v_mat_a IS NULL OR v_mat_a = v_mat_b THEN
        RAISE_APPLICATION_ERROR(-20024, 'Se necesitan dos materiales: ejecuta antes database/03_datos_prueba/01_catalogos.sql.');
    END IF;

    INSERT INTO punto_instalacion (nombre, referencia_ubicacion, activo)
    VALUES ('Prueba estadisticas', 'Punto de prueba', 1) RETURNING id_punto_instalacion INTO v_punto;
    INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado)
    VALUES (v_punto, 'Caneca prueba estadisticas', 'PRUEBA_qr_est_0001', TIMESTAMP '2001-01-01 00:00:00', 1)
    RETURNING id_contenedor INTO v_caneca;
    INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
    VALUES ('Prueba Estadisticas', 'prueba.estadisticas@example.com', TIMESTAMP '2001-01-01 00:00:00')
    RETURNING id_usuario INTO v_usuario;
    INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc)
    VALUES ('Operario Estadisticas', 'operario.estadisticas@example.com', TIMESTAMP '2001-01-01 00:00:00')
    RETURNING id_usuario INTO v_operario;

    -- i1: apertura aplicada, depósito medido de A verificado como B.
    v_i1 := interaccion(TIMESTAMP '2001-03-01 08:30:00');
    v_o1 := orden(v_i1, TIMESTAMP '2001-03-01 08:30:00', 'est-clave-0001');
    v_ev := evento('est-ev-0001', 2, v_o1, 1, TIMESTAMP '2001-03-01 08:30:10');
    v_ev := evento('est-ev-0002', 3, v_o1, 1, TIMESTAMP '2001-03-01 08:30:20');
    INSERT INTO deposito (id_orden, id_evento, id_material_declarado, id_material_verificado, id_verificador,
                          fecha_verificacion_utc, peso_medido_kg, fecha_deteccion_utc)
    VALUES (v_o1, v_ev, v_mat_a, v_mat_b, v_operario, TIMESTAMP '2001-03-01 12:00:00', 0.125, TIMESTAMP '2001-03-01 08:30:20');
    cerrar(v_i1, 3, v_o1, 6, TIMESTAMP '2001-03-01 08:31:00');

    -- i2: sin evento de apertura; depósito estimado de B, detectado de madrugada en UTC.
    v_i2 := interaccion(TIMESTAMP '2001-03-02 08:30:00');
    v_o2 := orden(v_i2, TIMESTAMP '2001-03-02 08:30:00', 'est-clave-0002');
    v_ev := evento('est-ev-0003', 3, v_o2, 1, TIMESTAMP '2001-03-04 04:30:00');
    INSERT INTO deposito (id_orden, id_evento, id_material_declarado, peso_estimado_kg, fecha_deteccion_utc)
    VALUES (v_o2, v_ev, v_mat_b, 0.050, TIMESTAMP '2001-03-04 04:30:00');
    cerrar(v_i2, 3, v_o2, 6, TIMESTAMP '2001-03-04 04:31:00');

    -- i3: orden vencida, sin depósito.
    v_i3 := interaccion(TIMESTAMP '2001-03-03 08:30:00');
    cerrar(v_i3, 4, orden(v_i3, TIMESTAMP '2001-03-03 08:30:00', 'est-clave-0003'), 7, TIMESTAMP '2001-03-03 08:33:00');

    -- i4: rechazada por el simulador.
    INSERT INTO interaccion (id_contenedor, id_usuario, canal, estado, motivo, fecha_inicio_utc, fecha_cierre_utc)
    VALUES (v_caneca, v_usuario, 3, 5, 'Material no admitido', TIMESTAMP '2001-03-03 09:00:00', TIMESTAMP '2001-03-03 09:00:05');

    -- Fallo huérfano y un depósito justo en el límite superior (período semiabierto: no entra).
    v_ev := evento('est-ev-0004', 5, NULL, 2, TIMESTAMP '2001-03-03 10:00:00');
    v_i5 := interaccion(c_hasta);
    v_o5 := orden(v_i5, c_hasta, 'est-clave-0005');
    v_ev := evento('est-ev-0005', 3, v_o5, 1, c_hasta);
    INSERT INTO deposito (id_orden, id_evento, id_material_declarado, peso_medido_kg, fecha_deteccion_utc)
    VALUES (v_o5, v_ev, v_mat_a, 9.999, c_hasta);

    -- Interacciones
    SELECT COUNT(*), COUNT(CASE WHEN estado = 3 THEN 1 END), COUNT(CASE WHEN estado = 5 THEN 1 END),
           COUNT(CASE WHEN canal = 3 THEN 1 END)
      INTO v_n1, v_n2, v_n3, v_n4
      FROM interaccion
     WHERE fecha_inicio_utc >= c_desde AND fecha_inicio_utc < c_hasta AND id_contenedor = v_caneca;
    esperar('interacciones del período (excluye el límite superior)', v_n1, 4);
    esperar('interacciones con depósito', v_n2, 2);
    esperar('interacciones rechazadas', v_n3, 1);
    esperar('interacciones por simulador', v_n4, 1);

    -- Órdenes: emitidas no es lo mismo que abiertas por el sensor
    SELECT COUNT(*), COUNT(ap.id_orden), COUNT(dp.id_orden), COUNT(CASE WHEN o.estado = 7 THEN 1 END)
      INTO v_n1, v_n2, v_n3, v_n4
      FROM orden_apertura o
      LEFT JOIN (SELECT DISTINCT id_orden FROM evento_dispositivo
                  WHERE tipo = 2 AND resultado = 1 AND id_orden IS NOT NULL) ap ON ap.id_orden = o.id_orden
      LEFT JOIN deposito dp ON dp.id_orden = o.id_orden
     WHERE o.fecha_emision_utc >= c_desde AND o.fecha_emision_utc < c_hasta AND o.id_contenedor = v_caneca;
    esperar('órdenes emitidas', v_n1, 3);
    esperar('órdenes con apertura confirmada por el sensor', v_n2, 1);
    esperar('órdenes con depósito', v_n3, 2);
    esperar('órdenes vencidas', v_n4, 1);

    -- Eventos
    SELECT COUNT(*), COUNT(CASE WHEN resultado = 2 THEN 1 END), COUNT(CASE WHEN tipo = 5 THEN 1 END)
      INTO v_n1, v_n2, v_n3
      FROM evento_dispositivo
     WHERE fecha_recepcion_utc >= c_desde AND fecha_recepcion_utc < c_hasta AND id_contenedor = v_caneca;
    esperar('eventos del período', v_n1, 4);
    esperar('eventos en revisión', v_n2, 1);
    esperar('eventos de fallo', v_n3, 1);

    -- Depósitos: kilogramos medidos y estimados por separado
    SELECT COUNT(*), NVL(SUM(d.peso_medido_kg), 0), NVL(SUM(d.peso_estimado_kg), 0)
      INTO v_n1, v_kg_medidos, v_kg_estimados
      FROM deposito d
      JOIN orden_apertura o ON o.id_orden = d.id_orden
     WHERE d.fecha_deteccion_utc >= c_desde AND d.fecha_deteccion_utc < c_hasta AND o.id_contenedor = v_caneca;
    esperar('depósitos del período (excluye el límite superior)', v_n1, 2);
    esperar('kg medidos sin mezclar estimados', v_kg_medidos, 0.125);
    esperar('kg estimados aparte', v_kg_estimados, 0.050);

    -- Filtros de verificación y criterio de material
    esperar('verificación: todos', depositos(0, 1), 2);
    esperar('verificación: sin verificar', depositos(1, 1), 1);
    esperar('verificación: verificados', depositos(2, 1), 1);
    esperar('material A declarado', depositos(0, 1, v_mat_a), 1);
    esperar('material A verificado (el operario lo corrigió a B)', depositos(0, 2, v_mat_a), 0);
    esperar('material B verificado', depositos(0, 2, v_mat_b), 1);

    -- Día civil de Colombia: 2001-03-04 04:30 UTC es 2001-03-03 23:30 en Bogotá
    SELECT TO_CHAR(TRUNC(CAST(d.fecha_deteccion_utc - INTERVAL '5' HOUR AS DATE)), 'YYYY-MM-DD')
      INTO v_dia
      FROM deposito d WHERE d.id_orden = v_o2;
    registrar(v_dia = '2001-03-03', 'día civil de Colombia en la madrugada UTC', 'se obtuvo ' || v_dia);

    -- Incidencias de la caneca: 1 fallo, 0 en revisión sin contar el fallo, 1 vencida
    SELECT COUNT(CASE WHEN tipo = 5 THEN 1 END), COUNT(CASE WHEN resultado = 2 AND tipo <> 5 THEN 1 END)
      INTO v_n1, v_n2
      FROM evento_dispositivo
     WHERE fecha_recepcion_utc >= c_desde AND fecha_recepcion_utc < c_hasta AND id_contenedor = v_caneca;
    esperar('fallos de la caneca', v_n1, 1);
    esperar('revisión sin repetir los fallos', v_n2, 0);

    ROLLBACK;

    DBMS_OUTPUT.PUT_LINE('-----');
    DBMS_OUTPUT.PUT_LINE('Resultado: ' || v_pasan || ' pasan, ' || v_fallan || ' fallan. Se hizo ROLLBACK de todos los datos de prueba.');

    IF v_fallan > 0 THEN
        RAISE_APPLICATION_ERROR(-20025, v_fallan || ' pruebas de estadísticas fallaron.');
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('Prueba interrumpida; se hizo ROLLBACK. ' || SQLERRM);
        RAISE;
END;
/

WHENEVER SQLERROR CONTINUE
