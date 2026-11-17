-- =====================================================================
-- EcoAccess BD — 11 · Datos SINTÉTICOS reproducibles
-- Ejecutar como ECOACCESS_BD después de 10 (F5). Tarda unos segundos.
--
-- ¡ATENCIÓN! Todo lo que crea este script es FICTICIO y queda marcado:
--   USUARIO.es_sintetico = 1, INTERACCION.es_sintetico = 1,
--   CONSULTA_VISION.es_sintetico = 1, REGLA_ECOPUNTOS.es_sintetica = 1,
--   estaciones con «(SIMULACIÓN)» y correos @ejemplo.test.
-- No son depósitos reales observados en la Universidad Popular del Cesar,
-- ni pesos medidos de verdad (la «báscula» también es simulada).
--
-- Qué genera (semilla fija 20261110 → mismos datos cada vez en una base vacía):
--   * 3 cuentas de personal: 2 operarios (uno también ciudadano) y 1 administradora.
--   * 50 ciudadanos sintéticos (algunos sin actividad, a propósito).
--   * Puesta en servicio de las 4 canecas (deja historial por el trigger 1).
--   * Tarifas SINTÉTICAS de EcoPuntos (solo para algunos materiales).
--   * 130 recorridos QR (sesiones) en los últimos 45 días, con fotos,
--     órdenes, eventos del dispositivo simulado y depósitos.
--   * 40 consultas de foto SIN QR (solo orientación: no son depósitos).
--   * Verificaciones, rechazos y 2 correcciones auditadas.
--
-- Protección: si ya hay usuarios sintéticos, no hace nada (ORA-20020).
-- Para regenerar desde cero, ver 99_PLAN_REVERSION.md.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM usuario WHERE es_sintetico = 1;
    IF n > 0 THEN
        RAISE_APPLICATION_ERROR(-20020, 'Ya hay ' || n || ' usuarios sintéticos. No se cargó nada.');
    END IF;
END;
/

DECLARE
    TYPE t_textos IS TABLE OF VARCHAR2(40);
    nombres   t_textos := t_textos('Ana','Luis','María','Carlos','Valentina','Andrés','Camila','Jhon','Daniela','Santiago',
                                   'Laura','Sebastián','Paola','Miguel','Yuliana','Esteban','Natalia','Kevin','Diana','Óscar',
                                   'Sara','Julián','Mónica','Felipe','Karen');
    apellidos t_textos := t_textos('Ramírez','Gómez','Pérez','Martínez','Rodríguez','Ortega','Mendoza','Castro','Vargas','Rojas',
                                   'Quintero','Arias','Daza','Maestre','Oñate','Fuentes','Zuleta','Barros','Molina','Cuello');

    v_ahora       TIMESTAMP := SYS_EXTRACT_UTC(SYSTIMESTAMP);
    v_op1         usuario.id_usuario%TYPE;   -- Operario que además es Ciudadano (relación N:M)
    v_op2         usuario.id_usuario%TYPE;
    v_adm         usuario.id_usuario%TYPE;
    v_id          usuario.id_usuario%TYPE;
    v_regla       regla_ecopuntos.id_regla%TYPE;
    TYPE t_ids IS TABLE OF NUMBER INDEX BY PLS_INTEGER;
    ciudadanos    t_ids;

    -- variables de cada recorrido
    v_titular     usuario.id_usuario%TYPE;
    v_punto       punto_instalacion.id_punto_instalacion%TYPE;
    v_material    material.id_material%TYPE;
    v_categoria   material.categoria%TYPE;
    v_caneca      contenedor.id_contenedor%TYPE;
    v_peso_ref    peso_referencial.id_peso_referencial%TYPE;
    v_estado_vis  peso_referencial.estado_visual%TYPE;
    v_clave_obj   peso_referencial.clave_objeto%TYPE;
    v_unidades    NUMBER;
    v_estimado    NUMBER;
    v_inicio      TIMESTAMP;
    v_inter       interaccion.id_interaccion%TYPE;
    v_orden       orden_apertura.id_orden%TYPE;
    v_evento      evento_dispositivo.id_evento%TYPE;
    v_deposito    deposito.id_deposito%TYPE;
    v_sin_sensor  NUMBER;
    v_azar        NUMBER;
    v_canal       NUMBER;
    v_verificador usuario.id_usuario%TYPE;
    v_mat_verif   material.id_material%TYPE;
    v_cant_verif  NUMBER;
    v_motivo      VARCHAR2(300);
    v_dias        NUMBER;
    c_dep         PLS_INTEGER := 0;
    c_ver         PLS_INTEGER := 0;
    c_rec         PLS_INTEGER := 0;

    FUNCTION crear_usuario(p_nombre VARCHAR2, p_correo VARCHAR2, p_dias_atras NUMBER, p_roles VARCHAR2) RETURN NUMBER IS
        v NUMBER;
    BEGIN
        INSERT INTO usuario (nombre, correo_electronico, fecha_registro_utc, version_consentimiento,
                             fecha_consentimiento_utc, es_sintetico)
        VALUES (p_nombre, p_correo, v_ahora - NUMTODSINTERVAL(p_dias_atras, 'DAY'), 'v1-2026',
                v_ahora - NUMTODSINTERVAL(p_dias_atras, 'DAY'), 1)
        RETURNING id_usuario INTO v;
        INSERT INTO usuario_rol (id_usuario, id_rol)
        SELECT v, id_rol FROM rol WHERE INSTR(',' || p_roles || ',', ',' || codigo || ',') > 0;
        RETURN v;
    END;

    -- Hash y sal SINTÉTICOS (bytes aleatorios): no corresponden a ninguna contraseña.
    PROCEDURE crear_credencial(p_id NUMBER) IS
    BEGIN
        INSERT INTO credencial_acceso (id_usuario, hash_contrasena, sal, iteraciones)
        VALUES (p_id, HEXTORAW(RAWTOHEX(SYS_GUID()) || RAWTOHEX(SYS_GUID())), SYS_GUID(), 210000);
    END;

    FUNCTION material_por_codigo(p_codigo VARCHAR2) RETURN NUMBER IS
        v NUMBER;
    BEGIN
        SELECT id_material INTO v FROM material WHERE codigo = p_codigo;
        RETURN v;
    END;

    FUNCTION entero(p_desde NUMBER, p_hasta NUMBER) RETURN NUMBER IS
    BEGIN
        RETURN TRUNC(DBMS_RANDOM.VALUE(p_desde, p_hasta + 1));
    END;
BEGIN
    DBMS_RANDOM.SEED(20261110);

    -- ---------- 1. Personal ----------
    v_op1 := crear_usuario('Operario Demo Uno (SINTÉTICO)', 'operario.uno@ejemplo.test', 100, '1,2');
    v_op2 := crear_usuario('Operaria Demo Dos (SINTÉTICO)', 'operaria.dos@ejemplo.test', 100, '2');
    v_adm := crear_usuario('Administradora Demo (SINTÉTICO)', 'administradora@ejemplo.test', 120, '3');
    crear_credencial(v_op1);
    crear_credencial(v_op2);
    crear_credencial(v_adm);

    -- ---------- 2. 50 ciudadanos ----------
    FOR k IN 1 .. 50 LOOP
        v_id := crear_usuario(nombres(MOD(k - 1, nombres.COUNT) + 1) || ' '
                              || apellidos(MOD(k * 7, apellidos.COUNT) + 1) || ' '
                              || apellidos(MOD(k * 3 + 5, apellidos.COUNT) + 1),
                              'ciudadano' || LPAD(k, 2, '0') || '@ejemplo.test',
                              entero(46, 90), '1');
        ciudadanos(k) := v_id;
        IF k <= 42 THEN                         -- 42 vinculados a Telegram (ids ficticios)
            INSERT INTO vinculo_telegram (id_usuario, telegram_user_id, fecha_vinculo_utc)
            VALUES (v_id, 900000000 + k, v_ahora - NUMTODSINTERVAL(45, 'DAY'));
        END IF;
    END LOOP;
    -- El operario 1 también es ciudadano y tiene Telegram.
    INSERT INTO vinculo_telegram (id_usuario, telegram_user_id, fecha_vinculo_utc)
    VALUES (v_op1, 900000999, v_ahora - NUMTODSINTERVAL(60, 'DAY'));
    -- Un vínculo dado de baja (historia conservada) y luego uno nuevo de la misma persona.
    UPDATE vinculo_telegram SET fecha_baja_utc = v_ahora - NUMTODSINTERVAL(10, 'DAY') WHERE id_usuario = ciudadanos(3);
    INSERT INTO vinculo_telegram (id_usuario, telegram_user_id, fecha_vinculo_utc)
    VALUES (ciudadanos(3), 900001003, v_ahora - NUMTODSINTERVAL(9, 'DAY'));

    -- ---------- 3. La administradora pone en servicio las canecas ----------
    FOR c IN (SELECT id_contenedor FROM contenedor ORDER BY id_contenedor) LOOP
        pr_cambiar_estado_contenedor(c.id_contenedor, 1, v_adm, 'Puesta en servicio inicial (simulación)');
    END LOOP;
    -- Mantenimiento simulado de la caneca negra: sale y vuelve a servicio.
    pr_cambiar_estado_contenedor(13, 2, v_adm, 'Mantenimiento de la tapa (simulación)');
    pr_cambiar_estado_contenedor(13, 1, v_adm, 'Mantenimiento terminado (simulación)');

    -- ---------- 4. Tarifas SINTÉTICAS (solo prueba; no son tarifas aprobadas) ----------
    -- Materiales negros y peligrosos: sin tarifa (no aprovechables) → no dan puntos.
    FOR t IN (SELECT 'PET' cod, 10 pts, 1 uni, 'Tarifa sintética: 10 puntos por botella PET verificada' d FROM dual UNION ALL
              SELECT 'LATA_ALUMINIO', 12, 1, 'Tarifa sintética: 12 puntos por lata verificada' FROM dual UNION ALL
              SELECT 'VIDRIO', 8, 1, 'Tarifa sintética: 8 puntos por envase de vidrio verificado' FROM dual UNION ALL
              SELECT 'TETRAPAK', 10, 1, 'Tarifa sintética: 10 puntos por caja multicapa verificada' FROM dual UNION ALL
              SELECT 'CARTON', 15, 0, 'Tarifa sintética: 15 puntos por depósito de cartón verificado' FROM dual UNION ALL
              SELECT 'PAPEL', 5, 0, 'Tarifa sintética: 5 puntos por depósito de papel verificado' FROM dual UNION ALL
              SELECT 'RESTO_FRUTA_VERDURA', 4, 0, 'Tarifa sintética: 4 puntos por depósito orgánico verificado' FROM dual) LOOP
        pkg_ecopuntos.registrar_regla(material_por_codigo(t.cod), t.pts, t.uni, t.d,
                                      v_ahora - NUMTODSINTERVAL(120, 'DAY'), v_adm, 1, v_regla);
    END LOOP;
    -- Una segunda versión de la tarifa PET (sube a 12 desde hace 20 días): demuestra el versionado.
    pkg_ecopuntos.registrar_regla(material_por_codigo('PET'), 12, 1,
        'Tarifa sintética v2: 12 puntos por botella PET verificada', v_ahora - NUMTODSINTERVAL(20, 'DAY'), v_adm, 1, v_regla);

    -- ---------- 5. 130 recorridos QR ----------
    FOR r IN 1 .. 130 LOOP
        -- Titular: ciudadanos 1..44 (45..50 quedan sin actividad); a veces el operario 1.
        IF MOD(r, 29) = 0 THEN
            v_titular := v_op1;
        ELSE
            v_titular := ciudadanos(LEAST(44, TRUNC(ABS(DBMS_RANDOM.NORMAL) * 14) + 1));
        END IF;
        -- 40 % de los recorridos en los últimos 7 días, el resto hasta 45 días atrás.
        v_dias   := CASE WHEN DBMS_RANDOM.VALUE < 0.4 THEN DBMS_RANDOM.VALUE(0.05, 7) ELSE DBMS_RANDOM.VALUE(7, 45) END;
        v_inicio := v_ahora - NUMTODSINTERVAL(v_dias, 'DAY');
        v_punto  := CASE WHEN MOD(r, 10) = 0 THEN 2 ELSE 1 END;
        v_canal  := CASE WHEN MOD(r, 4) = 0 THEN 3 ELSE 4 END;   -- 3 Simulador, 4 Telegram simulado

        -- Material: en la estación legado solo PET, lata o cartón; 6 % peligrosos en UPC01.
        IF v_punto = 2 THEN
            SELECT id_material, categoria INTO v_material, v_categoria FROM (
                SELECT m.id_material, m.categoria FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material
                WHERE cm.id_contenedor = 21 ORDER BY DBMS_RANDOM.VALUE) WHERE ROWNUM = 1;
        ELSIF DBMS_RANDOM.VALUE < 0.06 THEN
            SELECT id_material, categoria INTO v_material, v_categoria FROM (
                SELECT id_material, categoria FROM material WHERE categoria = 4 ORDER BY DBMS_RANDOM.VALUE) WHERE ROWNUM = 1;
        ELSE
            v_azar := DBMS_RANDOM.VALUE;           -- 55 % blanca, 25 % verde, 20 % negra
            SELECT id_material, categoria INTO v_material, v_categoria FROM (
                SELECT id_material, categoria FROM material
                WHERE categoria = CASE WHEN v_azar < 0.55 THEN 1 WHEN v_azar < 0.80 THEN 2 ELSE 3 END
                ORDER BY DBMS_RANDOM.VALUE) WHERE ROWNUM = 1;
        END IF;

        -- Objeto y peso de referencia (si el material tiene catálogo).
        BEGIN
            SELECT id_peso_referencial, estado_visual, clave_objeto INTO v_peso_ref, v_estado_vis, v_clave_obj FROM (
                SELECT id_peso_referencial, estado_visual, clave_objeto FROM peso_referencial
                WHERE id_material = v_material
                ORDER BY CASE WHEN estado_visual IN (0, 1) THEN DBMS_RANDOM.VALUE ELSE DBMS_RANDOM.VALUE + 0.6 END)
            WHERE ROWNUM = 1;
        EXCEPTION WHEN NO_DATA_FOUND THEN
            v_peso_ref := NULL; v_estado_vis := NULL;
            SELECT codigo INTO v_clave_obj FROM material WHERE id_material = v_material;
        END;
        v_unidades := CASE WHEN v_categoria = 2 THEN 1 ELSE entero(1, 3) END;
        v_estimado := fn_peso_estimado_kg(v_peso_ref, v_unidades);

        -- (b) Sesión QR. En la estación legado la caneca se conoce desde el QR.
        INSERT INTO interaccion (id_punto_instalacion, id_contenedor, id_usuario, canal, fecha_inicio_utc, es_sintetico)
        VALUES (v_punto, CASE WHEN v_punto = 2 THEN 21 END, v_titular, v_canal, v_inicio, 1)
        RETURNING id_interaccion INTO v_inter;

        -- (a) A veces la primera foto no sirve y Eco pide otra.
        IF DBMS_RANDOM.VALUE < 0.15 THEN
            INSERT INTO consulta_vision (clave, id_usuario, id_interaccion, canal, modo, proveedor, modelo, resultado,
                                         latencia_ms, fecha_utc, es_sintetico)
            VALUES ('SIM-CV-' || r || '-A', v_titular, v_inter, 1, 2, 'SIMULADOR', 'sin-modelo', 2,
                    TRUNC(DBMS_RANDOM.VALUE(900, 4001)), v_inicio + NUMTODSINTERVAL(5, 'SECOND'), 1);
        END IF;
        -- (a)+(c) Foto identificada: objeto, material sugerido, unidades y peso ESTIMADO.
        INSERT INTO consulta_vision (clave, id_usuario, id_interaccion, canal, modo, proveedor, modelo, resultado,
                                     clave_objeto, id_material, unidades_estimadas, estado_visual, id_peso_referencial,
                                     peso_estimado_kg, admision, id_contenedor_sugerido, latencia_ms, fecha_utc, es_sintetico)
        VALUES ('SIM-CV-' || r || '-B', v_titular, v_inter, 1, 2, 'SIMULADOR', 'sin-modelo', 1,
                v_clave_obj, v_material, v_unidades, v_estado_vis, v_peso_ref, v_estimado,
                CASE WHEN v_categoria = 4 THEN 1
                     WHEN v_punto = 2 AND v_categoria <> 1 THEN 3 ELSE 2 END,
                CASE WHEN v_categoria = 4 THEN NULL WHEN v_punto = 2 THEN 21 ELSE 10 + v_categoria END,
                TRUNC(DBMS_RANDOM.VALUE(800, 3501)), v_inicio + NUMTODSINTERVAL(12, 'SECOND'), 1);
        UPDATE interaccion
        SET    id_material_sugerido = v_material, confianza_sugerida = ROUND(DBMS_RANDOM.VALUE(0.62, 0.99), 3)
        WHERE  id_interaccion = v_inter;

        v_azar := DBMS_RANDOM.VALUE;
        IF v_categoria = 4 OR v_azar < 0.06 THEN
            -- Sin depósito: residuo peligroso (ruta especial) o la persona no continuó.
            UPDATE interaccion
            SET    estado = 4, fecha_cierre_utc = v_inicio + NUMTODSINTERVAL(40, 'SECOND'),
                   motivo = CASE WHEN v_categoria = 4 THEN 'Residuo peligroso: se orientó al punto posconsumo'
                                 ELSE 'La persona no continuó después de la orientación' END
            WHERE  id_interaccion = v_inter;
        ELSE
            -- Material confirmado y caneca resuelta por la función de la estación.
            v_caneca := CASE WHEN v_punto = 2 THEN 21 ELSE fn_resolver_caneca_disponible(v_punto, v_material) END;
            UPDATE interaccion
            SET    estado = 2, id_material_confirmado = v_material, id_contenedor = v_caneca,
                   cantidad_declarada = v_unidades
            WHERE  id_interaccion = v_inter;

            -- (d) Orden de apertura (vence a los 2 minutos).
            INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado,
                                        fecha_emision_utc, fecha_vencimiento_utc)
            VALUES (v_inter, v_caneca, 'SIM-ORD-' || LPAD(r, 4, '0'), 1,
                    v_inicio + NUMTODSINTERVAL(30, 'SECOND'), v_inicio + NUMTODSINTERVAL(150, 'SECOND'))
            RETURNING id_orden INTO v_orden;
            INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado, fecha_dispositivo_utc, fecha_recepcion_utc)
            VALUES (v_caneca, 'SIM-' || r || '-ACUSE', 1, v_orden, 1, v_inicio + NUMTODSINTERVAL(31, 'SECOND'), v_inicio + NUMTODSINTERVAL(31, 'SECOND'));

            IF v_azar < 0.13 THEN
                -- Orden vencida: la caneca acusó pero nunca confirmó apertura.
                UPDATE orden_apertura SET estado = 7, fecha_cierre_utc = v_inicio + NUMTODSINTERVAL(150, 'SECOND')
                WHERE id_orden = v_orden;
                UPDATE interaccion SET estado = 4, fecha_cierre_utc = v_inicio + NUMTODSINTERVAL(151, 'SECOND'),
                       motivo = 'Orden vencida sin apertura confirmada'
                WHERE id_interaccion = v_inter;
            ELSE
                -- (e) Apertura confirmada (+10 s de ventana) y (f) detección o cierre sin sensor.
                v_sin_sensor := CASE WHEN DBMS_RANDOM.VALUE < 0.35 THEN 1 ELSE 0 END;
                INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado, fecha_dispositivo_utc, fecha_recepcion_utc)
                VALUES (v_caneca, 'SIM-' || r || '-APERTURA', 2, v_orden, 1, v_inicio + NUMTODSINTERVAL(33, 'SECOND'), v_inicio + NUMTODSINTERVAL(33, 'SECOND'));
                INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado, fecha_dispositivo_utc, fecha_recepcion_utc)
                VALUES (v_caneca, 'SIM-' || r || CASE v_sin_sensor WHEN 1 THEN '-CIERRE_SIN_SENSOR' ELSE '-DETECCION' END,
                        CASE v_sin_sensor WHEN 1 THEN 6 ELSE 3 END, v_orden, 1,
                        v_inicio + NUMTODSINTERVAL(40, 'SECOND'), v_inicio + NUMTODSINTERVAL(40, 'SECOND'))
                RETURNING id_evento INTO v_evento;
                UPDATE orden_apertura SET estado = 6, fecha_cierre_utc = v_inicio + NUMTODSINTERVAL(44, 'SECOND')
                WHERE id_orden = v_orden;
                UPDATE interaccion SET estado = 3, fecha_cierre_utc = v_inicio + NUMTODSINTERVAL(45, 'SECOND')
                WHERE id_interaccion = v_inter;

                INSERT INTO deposito (id_orden, id_evento, sin_sensor, id_material_declarado, cantidad_declarada,
                                      peso_estimado_kg, id_peso_referencial, fecha_deteccion_utc)
                VALUES (v_orden, v_evento, v_sin_sensor, v_material, v_unidades, v_estimado, v_peso_ref,
                        v_inicio + NUMTODSINTERVAL(40, 'SECOND'))
                RETURNING id_deposito INTO v_deposito;
                c_dep := c_dep + 1;

                -- (g) Verificación independiente (los de las últimas 36 h siguen pendientes).
                IF v_dias > 1.5 THEN
                    v_verificador := CASE WHEN v_titular = v_op1 OR DBMS_RANDOM.VALUE < 0.5 THEN v_op2 ELSE v_op1 END;
                    v_azar := DBMS_RANDOM.VALUE;
                    IF v_azar < 0.10 THEN
                        pkg_deposito.rechazar(v_deposito, v_verificador,
                            'Revisión de la caneca: no se encontró el residuo declarado (simulación)');
                        c_rec := c_rec + 1;
                    ELSIF v_azar < 0.85 THEN
                        v_mat_verif  := v_material;
                        v_cant_verif := v_unidades;
                        v_motivo     := NULL;
                        IF DBMS_RANDOM.VALUE < 0.10 AND v_unidades > 1 THEN
                            v_cant_verif := v_unidades - 1;            -- el conteo real fue menor
                            v_motivo     := 'Conteo físico menor al declarado (simulación)';
                        END IF;
                        IF DBMS_RANDOM.VALUE < 0.04 AND v_categoria = 1 THEN   -- residuo mal separado
                            v_mat_verif := material_por_codigo('SERVILLETA_USADA');
                            v_motivo    := 'Se encontró papel sucio en la caneca blanca (simulación)';
                        END IF;
                        IF DBMS_RANDOM.VALUE < 0.30 AND v_estimado IS NOT NULL THEN
                            pkg_deposito.verificar(v_deposito, v_verificador, v_mat_verif, v_cant_verif,
                                ROUND(v_estimado * DBMS_RANDOM.VALUE(0.85, 1.15), 4),
                                'Báscula digital (SIMULADA)', 1, v_motivo);
                        ELSE
                            pkg_deposito.verificar(v_deposito, v_verificador, v_mat_verif, v_cant_verif,
                                p_motivo => v_motivo);
                        END IF;
                        c_ver := c_ver + 1;
                    END IF;   -- el 15 % restante queda pendiente
                END IF;
            END IF;
        END IF;
    END LOOP;

    -- ---------- 6. 40 consultas de foto SIN QR (solo orientación) ----------
    FOR k IN 1 .. 40 LOOP
        v_titular := CASE WHEN MOD(k, 5) = 0 THEN NULL ELSE ciudadanos(entero(1, 48)) END;   -- algunas de visitantes
        SELECT id_material, categoria INTO v_material, v_categoria FROM (
            SELECT id_material, categoria FROM material ORDER BY DBMS_RANDOM.VALUE) WHERE ROWNUM = 1;
        BEGIN
            SELECT id_peso_referencial, estado_visual, clave_objeto INTO v_peso_ref, v_estado_vis, v_clave_obj FROM (
                SELECT id_peso_referencial, estado_visual, clave_objeto FROM peso_referencial
                WHERE id_material = v_material ORDER BY DBMS_RANDOM.VALUE) WHERE ROWNUM = 1;
        EXCEPTION WHEN NO_DATA_FOUND THEN
            v_peso_ref := NULL; v_estado_vis := NULL;
            SELECT codigo INTO v_clave_obj FROM material WHERE id_material = v_material;
        END;
        v_unidades := entero(1, 2);
        v_azar := DBMS_RANDOM.VALUE;
        INSERT INTO consulta_vision (clave, id_usuario, id_interaccion, canal, modo, proveedor, modelo, resultado,
                                     clave_objeto, id_material, unidades_estimadas, estado_visual, id_peso_referencial,
                                     peso_estimado_kg, admision, latencia_ms, fecha_utc, es_sintetico)
        VALUES ('SIM-CV-LIBRE-' || k, v_titular, NULL, 1, 1, 'SIMULADOR', 'sin-modelo',
                CASE WHEN v_azar < 0.08 THEN 3 ELSE 1 END,
                CASE WHEN v_azar < 0.08 THEN NULL ELSE v_clave_obj END,
                CASE WHEN v_azar < 0.08 THEN NULL ELSE v_material END,
                CASE WHEN v_azar < 0.08 THEN NULL ELSE v_unidades END,
                CASE WHEN v_azar < 0.08 THEN NULL ELSE v_estado_vis END,
                CASE WHEN v_azar < 0.08 THEN NULL ELSE v_peso_ref END,
                CASE WHEN v_azar < 0.08 THEN NULL ELSE fn_peso_estimado_kg(v_peso_ref, v_unidades) END,
                CASE WHEN v_azar < 0.08 THEN NULL ELSE 1 END,
                TRUNC(DBMS_RANDOM.VALUE(700, 5001)), v_ahora - NUMTODSINTERVAL(DBMS_RANDOM.VALUE(0.1, 45), 'DAY'), 1);
    END LOOP;

    -- ---------- 7. Dos correcciones auditadas ----------
    -- (1) Un depósito verificado se corrige: el conteo real era 1 unidad.
    SELECT MIN(id_deposito) INTO v_deposito FROM deposito
    WHERE  estado_verificacion = 1 AND cantidad_verificada >= 2 AND peso_medido_kg IS NULL;
    IF v_deposito IS NOT NULL THEN
        SELECT id_material_verificado INTO v_mat_verif FROM deposito WHERE id_deposito = v_deposito;
        pkg_deposito.corregir_verificacion(v_deposito, v_adm, 1, v_mat_verif, 1,
            'Revisión de cámara: solo se depositó 1 unidad (simulación)');
    END IF;
    -- (2) Un rechazo se reabre para revisarlo otra vez (queda pendiente).
    SELECT MIN(id_deposito) INTO v_deposito FROM deposito WHERE estado_verificacion = 2;
    IF v_deposito IS NOT NULL THEN
        pkg_deposito.corregir_verificacion(v_deposito, v_adm, 0, NULL, NULL,
            'El ciudadano presentó reclamo; se reabre la revisión (simulación)');
    END IF;

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Carga sintética terminada: ' || c_dep || ' depósitos, ' || c_ver
        || ' verificados, ' || c_rec || ' rechazados (antes de las correcciones).');
END;
/

PROMPT ==== Resumen de la carga (todo marcado como SINTÉTICO) ====
SELECT 'Usuarios sintéticos' indicador, COUNT(*) valor FROM usuario WHERE es_sintetico = 1
UNION ALL SELECT 'Ciudadanos (rol 1)', COUNT(*) FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = 1
UNION ALL SELECT 'Recorridos QR (interacciones)', COUNT(*) FROM interaccion
UNION ALL SELECT 'Consultas de foto', COUNT(*) FROM consulta_vision
UNION ALL SELECT 'Consultas de foto sin QR', COUNT(*) FROM consulta_vision WHERE id_interaccion IS NULL
UNION ALL SELECT 'Órdenes de apertura', COUNT(*) FROM orden_apertura
UNION ALL SELECT 'Eventos del dispositivo simulado', COUNT(*) FROM evento_dispositivo
UNION ALL SELECT 'Depósitos', COUNT(*) FROM deposito
UNION ALL SELECT 'Depósitos verificados', COUNT(*) FROM deposito WHERE estado_verificacion = 1
UNION ALL SELECT 'Depósitos pendientes', COUNT(*) FROM deposito WHERE estado_verificacion = 0
UNION ALL SELECT 'Depósitos rechazados', COUNT(*) FROM deposito WHERE estado_verificacion = 2
UNION ALL SELECT 'Movimientos de EcoPuntos', COUNT(*) FROM movimiento_ecopuntos
UNION ALL SELECT 'Filas de auditoría de depósitos', COUNT(*) FROM auditoria_deposito
UNION ALL SELECT 'Historial de estado de canecas', COUNT(*) FROM historial_estado_contenedor;
WHENEVER SQLERROR CONTINUE
