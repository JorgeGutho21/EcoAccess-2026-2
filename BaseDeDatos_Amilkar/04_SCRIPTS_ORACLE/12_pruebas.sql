-- =====================================================================
-- EcoAccess BD — 12 · Pruebas automáticas
-- Ejecutar como ECOACCESS_BD después de 11 (F5). Se puede repetir.
--
-- Cada prueba imprime una línea:
--   PASA  P001 descripción
--   FALLA P0xx descripción  -> (qué se esperaba / qué pasó)
-- Las pruebas que intentan romper reglas usan SAVEPOINT y vuelven atrás.
-- Al final se hace ROLLBACK: este script NO deja datos nuevos.
--
-- Grupos:  A estructura · B datos sintéticos · C restricciones ·
--          D disparadores · E procedimientos/funciones/paquetes
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK OFF

DECLARE
    g_total   PLS_INTEGER := 0;
    g_fallas  PLS_INTEGER := 0;
    n         NUMBER;
    n2        NUMBER;
    v_op1     NUMBER;  v_op2 NUMBER;  v_adm NUMBER;  v_cid NUMBER;  v_cid2 NUMBER;
    v_dep     NUMBER;  v_dep2 NUMBER; v_mov NUMBER;  v_mov2 NUMBER;
    v_pet     NUMBER;  v_vidrio NUMBER; v_fruta NUMBER; v_pilas NUMBER; v_servilleta NUMBER; v_lata NUMBER;
    v_icopor  NUMBER; v_plastico NUMBER;
    v_cur     SYS_REFCURSOR;
    v_seq     PLS_INTEGER := 0;

    PROCEDURE registrar(p_nombre VARCHAR2, p_ok BOOLEAN, p_detalle VARCHAR2 DEFAULT NULL) IS
    BEGIN
        g_total := g_total + 1;
        IF p_ok THEN
            DBMS_OUTPUT.PUT_LINE('PASA  P' || LPAD(g_total, 3, '0') || ' ' || p_nombre);
        ELSE
            g_fallas := g_fallas + 1;
            DBMS_OUTPUT.PUT_LINE('FALLA P' || LPAD(g_total, 3, '0') || ' ' || p_nombre || '  -> ' || p_detalle);
        END IF;
    END;

    PROCEDURE ok(p_nombre VARCHAR2, p_cond BOOLEAN) IS
    BEGIN
        registrar(p_nombre, NVL(p_cond, FALSE), 'condición falsa o nula');
    END;

    -- Ejecuta p_sql y exige que falle con el código p_codigo (por ejemplo -2290).
    PROCEDURE espera_error(p_nombre VARCHAR2, p_codigo NUMBER, p_sql VARCHAR2) IS
    BEGIN
        SAVEPOINT antes_prueba;
        EXECUTE IMMEDIATE p_sql;
        ROLLBACK TO antes_prueba;
        registrar(p_nombre, FALSE, 'se esperaba ORA' || p_codigo || ' y la sentencia se aceptó');
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK TO antes_prueba;
            registrar(p_nombre, SQLCODE = p_codigo, 'se esperaba ORA' || p_codigo || ' y llegó: ' || SQLERRM);
    END;

    -- Como espera_error, pero exige que el rechazo venga de UNA restricción CHECK concreta (ORA-02290).
    PROCEDURE espera_check(p_nombre VARCHAR2, p_restriccion VARCHAR2, p_sql VARCHAR2) IS
    BEGIN
        SAVEPOINT antes_prueba;
        EXECUTE IMMEDIATE p_sql;
        ROLLBACK TO antes_prueba;
        registrar(p_nombre, FALSE, 'se esperaba ORA-02290 (' || p_restriccion || ') y la sentencia se aceptó');
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK TO antes_prueba;
            registrar(p_nombre, SQLCODE = -2290 AND INSTR(UPPER(SQLERRM), UPPER(p_restriccion)) > 0,
                      'se esperaba ORA-02290 (' || p_restriccion || ') y llegó: ' || SQLERRM);
    END;

    FUNCTION mat(p_codigo VARCHAR2) RETURN NUMBER IS
        v NUMBER;
    BEGIN
        SELECT id_material INTO v FROM material WHERE codigo = p_codigo;
        RETURN v;
    END;

    -- Crea un recorrido completo hasta un depósito PENDIENTE (todo se revierte al final).
    FUNCTION nuevo_deposito(p_titular NUMBER, p_material NUMBER, p_unidades NUMBER DEFAULT 1,
                            p_sin_sensor NUMBER DEFAULT 0) RETURN NUMBER IS
        v_i NUMBER; v_o NUMBER; v_e NUMBER; v_c NUMBER; v_d NUMBER;
    BEGIN
        v_seq := v_seq + 1;
        v_c := fn_resolver_caneca_disponible(1, p_material);
        INSERT INTO interaccion (id_punto_instalacion, id_contenedor, id_usuario, canal, estado,
                                 id_material_confirmado, cantidad_declarada, es_sintetico)
        VALUES (1, v_c, p_titular, 3, 2, p_material, p_unidades, 1)
        RETURNING id_interaccion INTO v_i;
        INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado,
                                    fecha_emision_utc, fecha_vencimiento_utc, fecha_cierre_utc)
        VALUES (v_i, v_c, 'TEST-ORD-' || v_seq, 6, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '1' MINUTE,
                SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL '1' MINUTE, SYS_EXTRACT_UTC(SYSTIMESTAMP))
        RETURNING id_orden INTO v_o;
        INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado)
        VALUES (v_c, 'TEST-EV-' || v_seq, CASE p_sin_sensor WHEN 1 THEN 6 ELSE 3 END, v_o, 1)
        RETURNING id_evento INTO v_e;
        INSERT INTO deposito (id_orden, id_evento, sin_sensor, id_material_declarado, cantidad_declarada,
                              fecha_deteccion_utc)
        VALUES (v_o, v_e, p_sin_sensor, p_material, p_unidades, SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '30' SECOND)
        RETURNING id_deposito INTO v_d;
        RETURN v_d;
    END;
BEGIN
    SELECT id_usuario INTO v_op1 FROM usuario WHERE correo_electronico = 'operario.uno@ejemplo.test';
    SELECT id_usuario INTO v_op2 FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';
    SELECT id_usuario INTO v_adm FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT id_usuario INTO v_cid FROM usuario WHERE correo_electronico = 'ciudadano01@ejemplo.test';
    SELECT id_usuario INTO v_cid2 FROM usuario WHERE correo_electronico = 'ciudadano02@ejemplo.test';
    v_pet := mat('PET'); v_vidrio := mat('VIDRIO'); v_fruta := mat('RESTO_FRUTA_VERDURA');
    v_pilas := mat('PILAS'); v_servilleta := mat('SERVILLETA_USADA'); v_lata := mat('LATA_ALUMINIO');
    v_icopor := mat('ICOPOR_SUCIO'); v_plastico := mat('PLASTICO_RIGIDO');

    DBMS_OUTPUT.PUT_LINE('===== A. ESTRUCTURA =====');
    SELECT COUNT(*) INTO n FROM user_tables;
    ok('Existen exactamente 19 tablas en ECOACCESS_BD', n = 19);
    SELECT COUNT(*) INTO n FROM user_tables t
    WHERE NOT EXISTS (SELECT 1 FROM user_constraints c WHERE c.table_name = t.table_name AND c.constraint_type = 'P');
    ok('Todas las tablas tienen clave primaria', n = 0);
    SELECT COUNT(*) INTO n FROM user_objects WHERE status <> 'VALID';
    ok('0 objetos inválidos', n = 0);
    SELECT COUNT(*) INTO n FROM user_triggers WHERE status = 'ENABLED';
    ok('Los 4 disparadores del proyecto están habilitados', n = 4);
    SELECT COUNT(*) INTO n FROM user_views;
    ok('Existen las 9 vistas', n = 9);
    SELECT COUNT(*) INTO n FROM user_objects WHERE object_type = 'PACKAGE';
    ok('Existen los 4 paquetes', n = 4);
    SELECT COUNT(*) INTO n FROM user_constraints WHERE constraint_type = 'R' AND status = 'ENABLED' AND validated = 'VALIDATED';
    SELECT COUNT(*) INTO n2 FROM user_constraints WHERE constraint_type = 'R';
    ok('Todas las FK (' || n2 || ') están habilitadas y validadas', n = n2 AND n2 > 0);
    SELECT COUNT(*) INTO n FROM user_constraints WHERE constraint_type = 'C' AND status <> 'ENABLED';
    ok('Todos los CHECK están habilitados', n = 0);
    -- FK sin índice que empiece por sus columnas (riesgo de bloqueos).
    SELECT COUNT(*) INTO n FROM user_constraints c
    WHERE c.constraint_type = 'R' AND NOT EXISTS (
        SELECT 1 FROM user_ind_columns ic
        JOIN user_cons_columns cc ON cc.table_name = ic.table_name AND cc.column_name = ic.column_name
             AND cc.position = ic.column_position
        WHERE cc.constraint_name = c.constraint_name AND ic.table_name = c.table_name);
    ok('Cada FK tiene un índice que empieza por su primera columna', n = 0);
    SELECT COUNT(*) INTO n FROM user_tab_identity_cols;
    ok('16 tablas usan IDENTITY (las 3 tablas puente/1:1 no lo necesitan)', n = 16);
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name IN ('TARJETA_RFID','ORGANIZACION','SUPERVISOR','COMUNA','BARRIO','RECOMPENSA');
    ok('No se copiaron tablas obsoletas de V3 (RFID, organización, supervisor...)', n = 0);
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE LOWER(column_name) LIKE '%contrasena%' AND table_name <> 'CREDENCIAL_ACCESO';
    ok('Ninguna tabla fuera de CREDENCIAL_ACCESO tiene columnas de contraseña', n = 0);
    SELECT COUNT(*) INTO n FROM user_tab_columns c JOIN user_tables t ON t.table_name = c.table_name
    WHERE c.data_type = 'VARCHAR2' AND c.char_used <> 'C';
    ok('Todos los VARCHAR2 de las tablas cuentan caracteres (CHAR), no bytes', n = 0);
    SELECT COUNT(*) INTO n FROM user_tab_columns c JOIN user_tables t ON t.table_name = c.table_name
    WHERE c.column_name LIKE 'FECHA%' AND c.data_type NOT LIKE 'TIMESTAMP%';
    ok('Todas las fechas de las tablas son TIMESTAMP (UTC)', n = 0);
    SELECT COUNT(*) INTO n FROM user_constraints WHERE constraint_type = 'R' AND delete_rule = 'CASCADE';
    ok('Ninguna FK borra en cascada', n = 0);
    SELECT COUNT(*) INTO n FROM user_tab_privs_made p JOIN user_tables t ON t.table_name = p.table_name WHERE p.grantee = 'ROL_ECO_LECTURA_BI';
    ok('El rol de Power BI no recibió ninguna tabla', n = 0);
    SELECT COUNT(*) INTO n FROM user_tab_privs_made WHERE grantee = 'ROL_ECO_LECTURA_BI';
    ok('El rol de Power BI tiene 6 vistas', n = 6);
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name LIKE 'VW_BI%' AND column_name IN ('NOMBRE','CORREO_ELECTRONICO','HASH_CONTRASENA','TELEGRAM_USER_ID');
    ok('Las vistas de Power BI no exponen nombre, correo, hash ni Telegram', n = 0);
    SELECT COUNT(*) INTO n FROM user_indexes WHERE index_name IN ('UQ_ORDEN_VIGENTE','UQ_CONTENEDOR_COLOR_ACTIVO','UQ_VINCULO_TELEGRAM_ACTIVO','UQ_VINCULO_USUARIO_ACTIVO') AND uniqueness = 'UNIQUE';
    ok('Existen los 4 índices únicos funcionales de reglas de negocio', n = 4);

    DBMS_OUTPUT.PUT_LINE('===== B. DATOS SINTÉTICOS =====');
    SELECT COUNT(*) INTO n FROM usuario u WHERE es_sintetico = 1 AND correo_electronico LIKE 'ciudadano%';
    ok('50 ciudadanos sintéticos', n = 50);
    SELECT COUNT(*) INTO n FROM usuario WHERE es_sintetico = 0;
    ok('Ningún usuario real en el esquema académico', n = 0);
    SELECT COUNT(*) INTO n FROM interaccion;
    ok('Al menos 100 recorridos QR (' || n || ')', n >= 100);
    SELECT COUNT(*) INTO n FROM interaccion WHERE es_sintetico = 0;
    ok('Todos los recorridos están marcados como sintéticos', n = 0);
    SELECT COUNT(*) INTO n FROM consulta_vision WHERE es_sintetico = 0;
    ok('Todas las consultas de foto están marcadas como sintéticas', n = 0);
    SELECT COUNT(DISTINCT c.categoria) INTO n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN contenedor c ON c.id_contenedor = o.id_contenedor;
    ok('Hay depósitos en las tres canecas (blanca, verde, negra)', n = 3);
    SELECT COUNT(DISTINCT estado_verificacion) INTO n FROM deposito;
    ok('Hay depósitos pendientes, verificados y rechazados', n = 3);
    SELECT COUNT(DISTINCT sin_sensor) INTO n FROM deposito;
    ok('Hay depósitos con sensor y declarados sin sensor', n = 2);
    SELECT COUNT(*) INTO n FROM consulta_vision WHERE id_interaccion IS NULL;
    ok('Hay consultas de foto sin QR (no son depósitos)', n >= 1);
    SELECT COUNT(*) INTO n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion
    WHERE d.id_verificador = i.id_usuario;
    ok('Ningún depósito está verificado por su propio titular', n = 0);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos m JOIN deposito d ON d.id_deposito = m.id_deposito
    WHERE m.tipo = 1 AND d.estado_verificacion <> 1
      AND NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos r WHERE r.id_movimiento_revertido = m.id_movimiento);
    ok('No hay créditos vigentes de depósitos no verificados', n = 0);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos m JOIN regla_ecopuntos r ON r.id_regla = m.id_regla WHERE r.es_sintetica = 0;
    ok('Todos los EcoPuntos vienen de tarifas marcadas SINTÉTICAS', n = 0);
    SELECT COUNT(*) INTO n FROM deposito d JOIN material m ON m.id_material = d.id_material_verificado
    JOIN movimiento_ecopuntos mv ON mv.id_deposito = d.id_deposito WHERE m.categoria = 3;
    ok('Ningún residuo NO aprovechable generó EcoPuntos', n = 0);
    SELECT COUNT(*) INTO n FROM deposito d JOIN peso_referencial p ON p.id_peso_referencial = d.id_peso_referencial
    WHERE d.peso_estimado_kg * 1000 NOT BETWEEN p.gramos_minimo * NVL(d.cantidad_declarada, 1) AND p.gramos_maximo * NVL(d.cantidad_declarada, 1);
    ok('Todos los pesos estimados están dentro del rango del catálogo', n = 0);
    SELECT COUNT(*) INTO n FROM deposito WHERE peso_medido_kg IS NOT NULL AND estado_verificacion <> 1;
    ok('Solo los depósitos verificados tienen peso medido', n = 0);
    SELECT COUNT(*) INTO n FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol AND r.codigo = 1
    WHERE NOT EXISTS (SELECT 1 FROM interaccion i WHERE i.id_usuario = u.id_usuario);
    ok('Hay ciudadanos sin ninguna actividad (para la consulta «sin depósitos»)', n >= 5);
    SELECT COUNT(*) INTO n FROM usuario_rol WHERE id_usuario = v_op1;
    ok('El operario 1 tiene dos roles (N:M Ciudadano + Operario)', n = 2);
    SELECT COUNT(*) INTO n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
    WHERE NOT EXISTS (SELECT 1 FROM contenedor_material cm WHERE cm.id_contenedor = o.id_contenedor AND cm.id_material = d.id_material_declarado);
    ok('Todo material declarado lo admite su caneca', n = 0);
    SELECT COUNT(*) INTO n FROM vw_saldo_ecopuntos s WHERE s.saldo <> pkg_ecopuntos.saldo(s.id_usuario);
    ok('El saldo de la vista coincide con la función saldo()', n = 0);
    SELECT COUNT(*) INTO n FROM auditoria_deposito WHERE operacion = 'CORRECCION';
    ok('La carga dejó al menos una corrección auditada', n >= 1);
    SELECT COUNT(*) INTO n FROM auditoria_deposito WHERE operacion = 'REAPERTURA';
    ok('La carga dejó una reapertura auditada', n >= 1);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos WHERE tipo = 2;
    ok('La corrección generó un reverso de EcoPuntos', n >= 1);
    SELECT COUNT(*) INTO n FROM historial_estado_contenedor;
    ok('El historial de canecas tiene la puesta en servicio y el mantenimiento (6)', n = 6);
    SELECT COUNT(*) INTO n FROM regla_ecopuntos r1 JOIN regla_ecopuntos r2
        ON r1.id_material = r2.id_material AND r1.id_regla < r2.id_regla
       AND r1.desde_utc < NVL(r2.hasta_utc, TIMESTAMP '9999-12-31 00:00:00')
       AND r2.desde_utc < NVL(r1.hasta_utc, TIMESTAMP '9999-12-31 00:00:00');
    ok('No hay tarifas solapadas para un mismo material', n = 0);
    SELECT COUNT(*) INTO n FROM interaccion WHERE estado IN (1, 2);
    ok('No quedaron sesiones abiertas en la carga', n = 0);

    DBMS_OUTPUT.PUT_LINE('===== C. RESTRICCIONES (deben FALLAR a propósito) =====');
    espera_error('CHECK: rol con código 4', -2290, q'[INSERT INTO rol (codigo, descripcion) VALUES (4, 'Supervisor')]');
    espera_error('CHECK: correo con mayúsculas', -2290, q'[INSERT INTO usuario (nombre, correo_electronico) VALUES ('Prueba Uno', 'Prueba@Ejemplo.test')]');
    espera_error('CHECK: correo sin arroba', -2290, q'[INSERT INTO usuario (nombre, correo_electronico) VALUES ('Prueba Uno', 'prueba.ejemplo.test')]');
    espera_error('UNIQUE: correo repetido', -1, q'[INSERT INTO usuario (nombre, correo_electronico) VALUES ('Otra Persona', 'ciudadano01@ejemplo.test')]');
    espera_error('CHECK: nombre con espacios sobrantes', -2290, q'[INSERT INTO usuario (nombre, correo_electronico) VALUES (' Ana ', 'ana.x@ejemplo.test')]');
    espera_error('CHECK: consentimiento sin fecha', -2290, q'[INSERT INTO usuario (nombre, correo_electronico, version_consentimiento) VALUES ('Ana Prueba', 'ana.y@ejemplo.test', 'v1')]');
    espera_error('NOT NULL: usuario sin nombre', -1400, q'[INSERT INTO usuario (nombre, correo_electronico) VALUES (NULL, 'ana.z@ejemplo.test')]');
    espera_error('FK: rol inexistente en USUARIO_ROL', -2291, 'INSERT INTO usuario_rol (id_usuario, id_rol) VALUES (' || v_cid || ', 999)');
    espera_error('PK: rol repetido para la misma persona', -1, 'INSERT INTO usuario_rol (id_usuario, id_rol) SELECT ' || v_cid || ', id_rol FROM rol WHERE codigo = 1');
    espera_error('CHECK: hash de 16 bytes (debe ser 32)', -2290, 'INSERT INTO credencial_acceso (id_usuario, hash_contrasena, sal, iteraciones) VALUES (' || v_cid || ', SYS_GUID(), SYS_GUID(), 210000)');
    espera_error('CHECK: menos de 100 000 iteraciones', -2290, 'INSERT INTO credencial_acceso (id_usuario, hash_contrasena, sal, iteraciones) VALUES (' || v_cid || ', HEXTORAW(RAWTOHEX(SYS_GUID())||RAWTOHEX(SYS_GUID())), SYS_GUID(), 1000)');
    espera_error('Índice único: segunda cuenta de Telegram activa igual', -1, 'INSERT INTO vinculo_telegram (id_usuario, telegram_user_id) VALUES (' || v_cid2 || ', 900000001)');
    espera_error('CHECK: QR de estación con caracteres no válidos', -2290, q'[INSERT INTO punto_instalacion (nombre, codigo_qr_estacion) VALUES ('Estación X', 'qr con espacios!!!!')]');
    espera_error('CHECK: caneca de categoría 4 (no existe caneca de peligrosos)', -2290, q'[INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (1, 'Caneca roja', 4)]');
    espera_error('Índice único: segunda caneca BLANCA activa en la misma estación', -1, q'[INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (1, 'Otra blanca', 1)]');
    espera_error('FK compuesta: vidrio (blanco) en la caneca verde', -2291, 'INSERT INTO contenedor_material (id_contenedor, id_material, categoria) VALUES (12, ' || v_vidrio || ', 2)');
    espera_error('FK compuesta: vidrio declarando categoría falsa', -2291, 'INSERT INTO contenedor_material (id_contenedor, id_material, categoria) VALUES (12, ' || v_vidrio || ', 1)');
    espera_error('CHECK: código de material en minúsculas', -2290, q'[INSERT INTO material (codigo, nombre, categoria) VALUES ('pet2', 'Otro PET', 1)]');
    espera_error('CHECK: peso mínimo mayor que el típico', -2290, 'INSERT INTO peso_referencial (id_material, clave_objeto, descripcion_objeto, gramos_minimo, gramos_tipico, gramos_maximo, fuente) VALUES (' || v_pet || q'[, 'X_PRUEBA', 'Prueba', 50, 20, 60, 'prueba')]');
    espera_error('UNIQUE: mismo objeto y estado visual en el catálogo', -1, 'INSERT INTO peso_referencial (id_material, clave_objeto, descripcion_objeto, estado_visual, gramos_minimo, gramos_tipico, gramos_maximo, fuente) VALUES (' || v_pet || q'[, 'BOTELLA_PET_600ML', 'Dup', 1, 1, 2, 3, 'prueba')]');
    espera_error('CHECK: canal de interacción 9', -2290, 'INSERT INTO interaccion (id_punto_instalacion, canal) VALUES (1, 9)');
    espera_error('FK compuesta: caneca 21 no pertenece a la estación 1', -2291, 'INSERT INTO interaccion (id_punto_instalacion, id_contenedor, canal) VALUES (1, 21, 3)');
    espera_error('CHECK: sesión autorizada sin material confirmado', -2290, 'INSERT INTO interaccion (id_punto_instalacion, id_contenedor, canal, estado) VALUES (1, 11, 3, 2)');
    espera_error('CHECK: apertura manual sin operario', -2290, q'[INSERT INTO interaccion (id_punto_instalacion, canal, estado, motivo, fecha_cierre_utc) VALUES (1, 2, 4, 'manual', SYS_EXTRACT_UTC(SYSTIMESTAMP))]');
    espera_error('CHECK: sesión cerrada sin fecha de cierre', -2290, 'INSERT INTO interaccion (id_punto_instalacion, canal, estado) VALUES (1, 3, 4)');
    espera_error('CHECK: sugerencia de IA sin confianza', -2290, 'INSERT INTO interaccion (id_punto_instalacion, canal, id_material_sugerido) VALUES (1, 3, ' || v_pet || ')');
    espera_error('CHECK: consulta «identificada» sin material', -2290, q'[INSERT INTO consulta_vision (clave, canal, modo, proveedor, resultado, latencia_ms) VALUES ('T-CV-1', 1, 1, 'SIMULADOR', 1, 10)]');
    espera_error('CHECK: peso estimado sin fila de catálogo', -2290, q'[INSERT INTO consulta_vision (clave, canal, modo, proveedor, resultado, latencia_ms, peso_estimado_kg) VALUES ('T-CV-2', 1, 1, 'SIMULADOR', 2, 10, 0.5)]');
    espera_error('UNIQUE: clave de consulta repetida', -1, q'[INSERT INTO consulta_vision (clave, canal, modo, proveedor, resultado, latencia_ms) VALUES ('SIM-CV-LIBRE-1', 1, 1, 'SIMULADOR', 2, 10)]');
    espera_error('CHECK: latencia negativa', -2290, q'[INSERT INTO consulta_vision (clave, canal, modo, proveedor, resultado, latencia_ms) VALUES ('T-CV-3', 1, 1, 'SIMULADOR', 2, -5)]');
    espera_error('CHECK: evento de tipo 7', -2290, q'[INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, resultado) VALUES (11, 'T-EV-7', 7, 2)]');
    espera_error('UNIQUE: evento retransmitido (mismo id externo)', -1, q'[INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, resultado) SELECT id_contenedor, id_externo, tipo, 2 FROM evento_dispositivo WHERE ROWNUM = 1]');
    espera_error('CHECK: evento «aplicado» sin orden', -2290, q'[INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, resultado) VALUES (11, 'T-EV-8', 1, 1)]');
    espera_error('UNIQUE: segundo depósito para la misma orden y evento', -1, 'INSERT INTO deposito (id_orden, id_evento, sin_sensor, id_material_declarado) SELECT d.id_orden, d.id_evento, d.sin_sensor, d.id_material_declarado FROM deposito d WHERE ROWNUM = 1');
    espera_error('CHECK: regla con fin antes del inicio', -2290, 'INSERT INTO regla_ecopuntos (id_material, puntos, descripcion, desde_utc, hasta_utc, id_creador) VALUES (' || v_pet || q'[, 5, 'x', SYSTIMESTAMP, SYSTIMESTAMP - 1, ]' || v_adm || ')');
    espera_error('CHECK: regla de 0 puntos', -2290, 'INSERT INTO regla_ecopuntos (id_material, puntos, descripcion, desde_utc, id_creador) VALUES (' || v_pet || q'[, 0, 'x', SYSTIMESTAMP, ]' || v_adm || ')');
    espera_error('CHECK: reverso con puntos positivos', -2290, q'[INSERT INTO movimiento_ecopuntos (clave_idempotencia, tipo, id_deposito, id_usuario, id_regla, puntos, id_movimiento_revertido, id_actor, motivo) SELECT 'T-REV', 2, id_deposito, id_usuario, id_regla, 10, id_movimiento, id_actor, 'x' FROM movimiento_ecopuntos WHERE tipo = 1 AND ROWNUM = 1]');
    espera_error('UNIQUE: clave de movimiento repetida (idempotencia)', -1, q'[INSERT INTO movimiento_ecopuntos (clave_idempotencia, tipo, id_deposito, id_usuario, id_regla, puntos, id_actor, motivo) SELECT clave_idempotencia, 1, id_deposito, id_usuario, id_regla, puntos, id_actor, 'x' FROM movimiento_ecopuntos WHERE tipo = 1 AND ROWNUM = 1]');
    espera_error('CHECK: historial con el mismo estado antes y después', -2290, q'[INSERT INTO historial_estado_contenedor (id_contenedor, estado_anterior, estado_nuevo, usuario_bd) VALUES (11, 1, 1, USER)]');

    -- Órdenes: se necesita una sesión autorizada en la caneca 11.
    SAVEPOINT ordenes;
    INSERT INTO interaccion (id_punto_instalacion, id_contenedor, id_usuario, canal, estado, id_material_confirmado, es_sintetico)
    VALUES (1, 11, v_cid, 3, 2, v_pet, 1);
    INSERT INTO interaccion (id_punto_instalacion, id_contenedor, id_usuario, canal, estado, id_material_confirmado, es_sintetico)
    VALUES (1, 11, v_cid2, 3, 2, v_pet, 1);
    INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, fecha_vencimiento_utc)
    SELECT MIN(id_interaccion), 11, 'TEST-VIGENTE-1', SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL '2' MINUTE
    FROM interaccion WHERE id_usuario = v_cid AND estado = 2;
    espera_error('Índice único: segunda orden VIGENTE en la misma caneca (concurrencia)', -1,
        'INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, fecha_vencimiento_utc) SELECT MIN(id_interaccion), 11, ''TEST-VIGENTE-2'', SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL ''2'' MINUTE FROM interaccion WHERE id_usuario = ' || v_cid2 || ' AND estado = 2');
    espera_error('UNIQUE: misma clave de idempotencia de orden', -1,
        'INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado, fecha_vencimiento_utc, fecha_cierre_utc) SELECT MIN(id_interaccion), 11, ''TEST-VIGENTE-1'', 6, SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL ''2'' MINUTE, SYS_EXTRACT_UTC(SYSTIMESTAMP) FROM interaccion WHERE id_usuario = ' || v_cid2 || ' AND estado = 2');
    espera_error('FK compuesta: orden para una caneca distinta a la de la sesión', -2291,
        'INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado, fecha_vencimiento_utc, fecha_cierre_utc) SELECT MIN(id_interaccion), 12, ''TEST-OTRA-CANECA'', 6, SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL ''2'' MINUTE, SYS_EXTRACT_UTC(SYSTIMESTAMP) FROM interaccion WHERE id_usuario = ' || v_cid2 || ' AND estado = 2');
    espera_error('CHECK: orden que vence antes de emitirse', -2290,
        'INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado, fecha_emision_utc, fecha_vencimiento_utc, fecha_cierre_utc) SELECT MIN(id_interaccion), 11, ''TEST-VENCE'', 6, SYS_EXTRACT_UTC(SYSTIMESTAMP), SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL ''1'' MINUTE, SYS_EXTRACT_UTC(SYSTIMESTAMP) FROM interaccion WHERE id_usuario = ' || v_cid2 || ' AND estado = 2');
    ROLLBACK TO ordenes;

    DBMS_OUTPUT.PUT_LINE('===== D. DISPARADORES =====');
    -- Trigger 2 (BEFORE INSERT/UPDATE): coherencia del depósito.
    SAVEPOINT trig2b;
    v_dep := nuevo_deposito(v_cid, v_pet, 2);
    SELECT COUNT(*) INTO n FROM deposito WHERE id_deposito = v_dep AND estado_verificacion = 0;
    ok('T2 positivo: un depósito coherente se inserta PENDIENTE', n = 1);
    -- :NEW asignado en BEFORE: fecha_deteccion_utc NULL se completa sola.
    SAVEPOINT fecha_nula;
    DECLARE v_d NUMBER; v_f TIMESTAMP;
    BEGIN
        INSERT INTO interaccion (id_punto_instalacion, id_contenedor, id_usuario, canal, estado, id_material_confirmado, es_sintetico)
        VALUES (1, 11, v_cid, 3, 2, v_pet, 1) RETURNING id_interaccion INTO v_d;
        INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado, fecha_vencimiento_utc, fecha_cierre_utc)
        VALUES (v_d, 11, 'TEST-FECHA-NULA', 6, SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL '1' MINUTE, SYS_EXTRACT_UTC(SYSTIMESTAMP))
        RETURNING id_orden INTO n;
        INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado) VALUES (11, 'TEST-FN', 3, n, 1)
        RETURNING id_evento INTO n2;
        INSERT INTO deposito (id_orden, id_evento, id_material_declarado) VALUES (n, n2, v_pet)
        RETURNING id_deposito, fecha_deteccion_utc INTO v_d, v_f;
        ok('T2 :NEW en BEFORE: fecha_deteccion NULL se completó con la hora UTC', v_f IS NOT NULL);
    END;
    ROLLBACK TO fecha_nula;

    espera_error('T2: insertar un depósito ya VERIFICADO (ORA-20107)', -20107,
        'INSERT INTO deposito (id_orden, id_evento, id_material_declarado, estado_verificacion, id_material_verificado, id_verificador, fecha_verificacion_utc, fecha_deteccion_utc) '
        || 'SELECT o.id_orden, e.id_evento, ' || v_pet || ', 1, ' || v_pet || ', ' || v_op2 || ', SYS_EXTRACT_UTC(SYSTIMESTAMP), SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL ''1'' MINUTE '
        || 'FROM orden_apertura o JOIN evento_dispositivo e ON e.id_orden = o.id_orden AND e.tipo = 1 WHERE o.estado = 7 AND ROWNUM = 1');
    espera_error('T2: sin_sensor=0 con evento de cierre sin sensor (ORA-20108)', -20108,
        'INSERT INTO deposito (id_orden, id_evento, sin_sensor, id_material_declarado) '
        || 'SELECT o.id_orden, e.id_evento, 0, ' || v_pet || ' FROM orden_apertura o JOIN evento_dispositivo e ON e.id_orden = o.id_orden AND e.tipo = 1 WHERE o.estado = 7 AND ROWNUM = 1');
    -- Para estas dos se usa una orden vencida de la caneca blanca (11) y un evento de detección nuevo.
    espera_error('T2: material que la caneca no admite (ORA-20102)', -20102,
        'DECLARE o NUMBER; v NUMBER; BEGIN SELECT MIN(id_orden) INTO o FROM orden_apertura WHERE estado = 7 AND id_contenedor = 11; '
        || 'INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado) VALUES (11, ''TEST-NOADM'', 3, o, 1) RETURNING id_evento INTO v; '
        || 'INSERT INTO deposito (id_orden, id_evento, id_material_declarado) VALUES (o, v, ' || v_fruta || '); END;');
    espera_error('T2: peso estimado fuera del rango del catálogo (ORA-20106)', -20106,
        'DECLARE o NUMBER; v NUMBER; BEGIN SELECT MIN(id_orden) INTO o FROM orden_apertura WHERE estado = 7 AND id_contenedor = 11; '
        || 'INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado) VALUES (11, ''TEST-PESO'', 3, o, 1) RETURNING id_evento INTO v; '
        || 'INSERT INTO deposito (id_orden, id_evento, id_material_declarado, cantidad_declarada, peso_estimado_kg, id_peso_referencial) VALUES (o, v, ' || v_pet || ', 1, 5, 1); END;');
    espera_error('T2: el titular verifica su propio depósito (ORA-20101)', -20101,
        'BEGIN pkg_deposito.verificar(' || v_dep || ', ' || v_cid || ', ' || v_pet || ', 2); END;');
    espera_error('T2: un ciudadano (sin rol operario) intenta verificar (ORA-20104)', -20104,
        'BEGIN pkg_deposito.verificar(' || v_dep || ', ' || v_cid2 || ', ' || v_pet || ', 2); END;');
    espera_error('T2: material verificado de otro color sin motivo (ORA-20105)', -20105,
        'BEGIN pkg_deposito.verificar(' || v_dep || ', ' || v_op2 || ', ' || v_servilleta || ', 1); END;');
    espera_error('T2: cambiar el material declarado (ORA-20109)', -20109,
        'UPDATE deposito SET id_material_declarado = ' || v_lata || ' WHERE id_deposito = ' || v_dep);
    espera_error('T2: cambiar sin_sensor después de registrado (ORA-20109)', -20109,
        'UPDATE deposito SET sin_sensor = 1 WHERE id_deposito = ' || v_dep);

    -- Trigger 3 (AFTER UPDATE): auditoría con :OLD y :NEW.
    SELECT COUNT(*) INTO n FROM auditoria_deposito WHERE id_deposito = v_dep;
    UPDATE deposito SET cantidad_verificada = cantidad_verificada WHERE id_deposito = v_dep;  -- NULL -> NULL: sin cambio real
    SELECT COUNT(*) INTO n2 FROM auditoria_deposito WHERE id_deposito = v_dep;
    ok('T3: un UPDATE sin cambio real (NULL->NULL) no deja auditoría', n2 = n);
    pkg_deposito.verificar(v_dep, v_op2, v_pet, 2);
    SELECT COUNT(*) INTO n FROM auditoria_deposito
    WHERE id_deposito = v_dep AND operacion = 'VERIFICACION' AND estado_anterior = 0 AND estado_nuevo = 1
      AND id_verificador_anterior IS NULL AND id_verificador_nuevo = v_op2 AND cantidad_nueva = 2 AND id_actor = v_op2;
    ok('T3 positivo: la verificación deja :OLD (0, NULL) y :NEW (1, operario) y el actor', n = 1);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos WHERE id_deposito = v_dep AND tipo = 1 AND puntos = 24;
    ok('E: verificar 2 botellas PET con tarifa v2 (12 por unidad) acredita 24 puntos', n = 1);

    -- Trigger 4 (BEFORE UPDATE cerrado).
    espera_error('T4: UPDATE directo de un depósito VERIFICADO (ORA-20103)', -20103,
        'UPDATE deposito SET cantidad_verificada = 1 WHERE id_deposito = ' || v_dep);
    espera_error('T4: ni siquiera el peso medido se cambia directo (ORA-20103)', -20103,
        'UPDATE deposito SET peso_medido_kg = 0.05, instrumento_medicion = ''x'', precision_medicion_g = 1 WHERE id_deposito = ' || v_dep);
    espera_error('T4: verificar dos veces el mismo depósito (ORA-20111)', -20111,
        'BEGIN pkg_deposito.verificar(' || v_dep || ', ' || v_op2 || ', ' || v_pet || ', 2); END;');
    espera_error('Corrección sin motivo (ORA-20112)', -20112,
        'BEGIN pkg_deposito.corregir_verificacion(' || v_dep || ', ' || v_adm || ', 1, ' || v_pet || ', 1, NULL); END;');
    espera_error('Corrección hecha por un ciudadano (ORA-20120)', -20120,
        'BEGIN pkg_deposito.corregir_verificacion(' || v_dep || ', ' || v_cid2 || ', 1, ' || v_pet || ', 1, ''x''); END;');
    -- Una corrección que falla a mitad (material inexistente) NO deja la puerta abierta.
    espera_error('Corrección con material inexistente falla (ORA-02291)', -2291,
        'BEGIN pkg_deposito.corregir_verificacion(' || v_dep || ', ' || v_adm || ', 1, 999999, 1, ''prueba''); END;');
    ok('Tras el error, la puerta de corrección quedó cerrada', NOT pkg_deposito.correccion_en_curso);
    espera_error('T4: después del error sigue bloqueado el UPDATE directo (ORA-20103)', -20103,
        'UPDATE deposito SET cantidad_verificada = 1 WHERE id_deposito = ' || v_dep);
    pkg_deposito.corregir_verificacion(v_dep, v_adm, 1, v_pet, 1, 'Prueba: el conteo real fue 1');
    SELECT COUNT(*) INTO n FROM auditoria_deposito
    WHERE id_deposito = v_dep AND operacion = 'CORRECCION' AND cantidad_anterior = 2 AND cantidad_nueva = 1 AND id_actor = v_adm;
    ok('T3+T4: la corrección autorizada pasa y audita 2 -> 1 con la administradora', n = 1);
    SELECT NVL(SUM(puntos), 0) INTO n FROM movimiento_ecopuntos WHERE id_deposito = v_dep;
    ok('E: tras la corrección el depósito suma 12 puntos (24 - 24 + 12)', n = 12);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos WHERE id_deposito = v_dep AND tipo = 2 AND puntos = -24;
    ok('E: se registró el reverso de -24 (el libro no se borra)', n = 1);

    -- Rechazo y su auditoría.
    v_dep2 := nuevo_deposito(v_cid2, v_lata, 1, 1);
    espera_error('Rechazar sin motivo (ORA-20112)', -20112, 'BEGIN pkg_deposito.rechazar(' || v_dep2 || ', ' || v_op1 || ', NULL); END;');
    pkg_deposito.rechazar(v_dep2, v_op1, 'Prueba: la caneca estaba vacía');
    SELECT COUNT(*) INTO n FROM auditoria_deposito WHERE id_deposito = v_dep2 AND operacion = 'RECHAZO' AND estado_nuevo = 2;
    ok('T3: el rechazo queda auditado como RECHAZO', n = 1);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos WHERE id_deposito = v_dep2;
    ok('E: un depósito rechazado no tiene EcoPuntos', n = 0);
    -- La auditoría pertenece a la transacción: si se deshace, desaparece.
    SAVEPOINT antes_aud;
    v_mov := nuevo_deposito(v_cid2, v_lata, 1);
    pkg_deposito.verificar(v_mov, v_op1, v_lata, 1);
    ROLLBACK TO antes_aud;
    SELECT COUNT(*) INTO n FROM auditoria_deposito WHERE id_deposito = v_mov;
    ok('T3: al hacer ROLLBACK, su auditoría también se deshace (sin COMMIT en el trigger)', n = 0);
    ROLLBACK TO trig2b;

    -- Trigger 1 (AFTER UPDATE OF estado ON contenedor).
    SAVEPOINT trig1;
    SELECT COUNT(*) INTO n FROM historial_estado_contenedor WHERE id_contenedor = 12;
    pr_cambiar_estado_contenedor(12, 2, v_adm, 'Prueba de historial');
    SELECT COUNT(*) INTO n2 FROM historial_estado_contenedor WHERE id_contenedor = 12 AND estado_anterior = 1 AND estado_nuevo = 2 AND id_actor = v_adm;
    ok('T1 positivo: cambiar 1 -> 2 deja historial con la administradora', n2 >= 1);
    UPDATE contenedor SET estado = estado WHERE id_contenedor = 12;
    SELECT COUNT(*) INTO n2 FROM historial_estado_contenedor WHERE id_contenedor = 12;
    ok('T1 WHEN: un UPDATE con el mismo estado no deja historial', n2 = n + 1);
    UPDATE contenedor SET nombre = 'UPC01 - Caneca verde (renombrada)' WHERE id_contenedor = 12;
    SELECT COUNT(*) INTO n2 FROM historial_estado_contenedor WHERE id_contenedor = 12;
    ok('T1 UPDATE OF estado: cambiar el nombre no dispara el historial', n2 = n + 1);
    espera_error('Resolver caneca fuera de servicio (ORA-20816)', -20816,
        'DECLARE v NUMBER; BEGIN v := fn_resolver_caneca_disponible(1, ' || v_fruta || '); END;');
    UPDATE contenedor SET estado = 1 WHERE id_contenedor = 12;
    SELECT COUNT(*) INTO n2 FROM historial_estado_contenedor WHERE id_contenedor = 12 AND id_actor IS NULL;
    ok('T1: un UPDATE sin actor de aplicación queda registrado con actor NULL y usuario BD', n2 >= 1);
    espera_error('Un operario no puede cambiar el estado de una caneca (ORA-20120)', -20120,
        'BEGIN pr_cambiar_estado_contenedor(12, 2, ' || v_op1 || ', ''x''); END;');
    espera_error('Estado de caneca 5 no válido (ORA-20121)', -20121,
        'BEGIN pr_cambiar_estado_contenedor(12, 5, ' || v_adm || ', ''x''); END;');
    ROLLBACK TO trig1;

    DBMS_OUTPUT.PUT_LINE('===== E. PROCEDIMIENTOS, FUNCIONES Y PAQUETES =====');
    ok('fn_peso_estimado_kg: 3 botellas PET 600 ml vacías = 0,066 kg', fn_peso_estimado_kg(1, 3) = 0.066);
    ok('fn_peso_estimado_kg: sin catálogo devuelve NULL (no inventa)', fn_peso_estimado_kg(NULL, 3) IS NULL);
    ok('fn_peso_estimado_kg: 0 unidades devuelve NULL', fn_peso_estimado_kg(1, 0) IS NULL);
    ok('fn_resolver_caneca: PET va a la caneca BLANCA (11)', fn_resolver_caneca_disponible(1, v_pet) = 11);
    ok('fn_resolver_caneca: restos de fruta van a la VERDE (12)', fn_resolver_caneca_disponible(1, v_fruta) = 12);
    ok('fn_resolver_caneca: icopor sucio va a la NEGRA (13)', fn_resolver_caneca_disponible(1, v_icopor) = 13);
    espera_error('fn_resolver_caneca: pilas = ruta especial (ORA-20813)', -20813,
        'DECLARE v NUMBER; BEGIN v := fn_resolver_caneca_disponible(1, ' || v_pilas || '); END;');
    espera_error('fn_resolver_caneca: estación legado sin caneca verde (ORA-20814)', -20814,
        'DECLARE v NUMBER; BEGIN v := fn_resolver_caneca_disponible(2, ' || v_fruta || '); END;');
    espera_error('fn_resolver_caneca: la caneca legado no admite vidrio (ORA-20817)', -20817,
        'DECLARE v NUMBER; BEGIN v := fn_resolver_caneca_disponible(2, ' || v_vidrio || '); END;');
    espera_error('fn_resolver_caneca: estación inexistente (ORA-20818)', -20818,
        'DECLARE v NUMBER; BEGIN v := fn_resolver_caneca_disponible(999, ' || v_pet || '); END;');

    SAVEPOINT eco;
    v_dep := nuevo_deposito(v_cid, v_vidrio, 3);
    espera_error('acreditar un depósito PENDIENTE (ORA-20201)', -20201,
        'DECLARE v NUMBER; BEGIN pkg_ecopuntos.acreditar(' || v_dep || ', ' || v_op1 || ', v); END;');
    pkg_deposito.verificar(v_dep, v_op1, v_vidrio, 3);
    SELECT id_movimiento, puntos INTO v_mov, n FROM movimiento_ecopuntos WHERE id_deposito = v_dep;
    ok('Tarifa por unidad: 3 vidrios × 8 = 24 puntos', n = 24);
    pkg_ecopuntos.acreditar(v_dep, v_op1, v_mov2);
    SELECT COUNT(*) INTO n FROM movimiento_ecopuntos WHERE id_deposito = v_dep;
    ok('Idempotencia: acreditar otra vez devuelve el mismo movimiento y no duplica', v_mov2 = v_mov AND n = 1);
    v_dep2 := nuevo_deposito(v_cid, v_icopor, 1);
    n := pkg_ecopuntos.saldo(v_cid);
    pkg_deposito.verificar(v_dep2, v_op1, v_icopor, 1);
    ok('Sin tarifa aprobada (icopor, no aprovechable) el saldo NO aumenta', pkg_ecopuntos.saldo(v_cid) = n);
    espera_error('revertir un depósito sin crédito (ORA-20205)', -20205,
        'BEGIN pkg_ecopuntos.revertir(' || v_dep2 || ', ' || v_op1 || ', ''x''); END;');
    espera_error('Un operario no puede registrar tarifas (ORA-20120)', -20120,
        'DECLARE v NUMBER; BEGIN pkg_ecopuntos.registrar_regla(' || v_pet || ', 5, 1, ''x'', SYSTIMESTAMP, ' || v_op1 || ', 1, v); END;');
    espera_error('Tarifa que empieza antes de la versión vigente (ORA-20206)', -20206,
        'DECLARE v NUMBER; BEGIN pkg_ecopuntos.registrar_regla(' || v_pet || ', 5, 1, ''x'', SYS_EXTRACT_UTC(SYSTIMESTAMP) - 60, ' || v_adm || ', 1, v); END;');
    pkg_ecopuntos.registrar_regla(v_lata, 20, 1, 'Prueba: nueva versión', SYS_EXTRACT_UTC(SYSTIMESTAMP), v_adm, 1, v_mov);
    SELECT COUNT(*) INTO n FROM regla_ecopuntos WHERE id_material = v_lata AND hasta_utc IS NULL;
    ok('Versionar una tarifa deja UNA sola versión abierta', n = 1);
    ok('regla_vigente devuelve la versión nueva', pkg_ecopuntos.regla_vigente(v_lata, SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL '1' SECOND) = v_mov);
    ok('regla_vigente de un material sin tarifa es NULL', pkg_ecopuntos.regla_vigente(v_icopor, SYS_EXTRACT_UTC(SYSTIMESTAMP)) IS NULL);

    -- Revisión del PR #12 (P2, primera observación): una medición es completa o no existe.
    -- Antes, un CHECK con un NULL daba UNKNOWN y Oracle lo aceptaba.
    v_dep2 := nuevo_deposito(v_cid, v_pet, 1);
    espera_check('Medición sin precisión: peso e instrumento sin precision_medicion_g (CK_DEPOSITO_MEDIDO)', 'CK_DEPOSITO_MEDIDO',
        'BEGIN pkg_deposito.verificar(' || v_dep2 || ', ' || v_op1 || ', ' || v_pet || ', 1, 0.03, ''Báscula digital (SIMULADA)''); END;');
    espera_check('Medición sin peso: instrumento y precisión sin peso_medido_kg (CK_DEPOSITO_MEDIDO)', 'CK_DEPOSITO_MEDIDO',
        'BEGIN pkg_deposito.verificar(' || v_dep2 || ', ' || v_op1 || ', ' || v_pet || ', 1, NULL, ''Báscula digital (SIMULADA)'', 1); END;');
    espera_check('Medición sin instrumento: peso y precisión sin instrumento_medicion (CK_DEPOSITO_MEDIDO)', 'CK_DEPOSITO_MEDIDO',
        'BEGIN pkg_deposito.verificar(' || v_dep2 || ', ' || v_op1 || ', ' || v_pet || ', 1, 0.03, NULL, 1); END;');
    espera_check('Medición con instrumento en blanco (CK_DEPOSITO_MEDIDO)', 'CK_DEPOSITO_MEDIDO',
        'BEGIN pkg_deposito.verificar(' || v_dep2 || ', ' || v_op1 || ', ' || v_pet || ', 1, 0.03, ''   '', 1); END;');
    espera_check('Medición con precisión cero (CK_DEPOSITO_MEDIDO)', 'CK_DEPOSITO_MEDIDO',
        'BEGIN pkg_deposito.verificar(' || v_dep2 || ', ' || v_op1 || ', ' || v_pet || ', 1, 0.03, ''Báscula digital (SIMULADA)'', 0); END;');
    espera_check('Medición con peso cero (CK_DEPOSITO_MEDIDO)', 'CK_DEPOSITO_MEDIDO',
        'BEGIN pkg_deposito.verificar(' || v_dep2 || ', ' || v_op1 || ', ' || v_pet || ', 1, 0, ''Báscula digital (SIMULADA)'', 1); END;');
    pkg_deposito.verificar(v_dep2, v_op1, v_pet, 1, 0.03, 'Báscula digital (SIMULADA)', 1);
    SELECT COUNT(*) INTO n FROM deposito
    WHERE id_deposito = v_dep2 AND estado_verificacion = 1 AND peso_medido_kg = 0.03 AND precision_medicion_g = 1;
    ok('Medición completa (peso, instrumento y precisión) se acepta', n = 1);
    SELECT COUNT(*) INTO n FROM deposito
    WHERE NOT ((peso_medido_kg IS NULL AND instrumento_medicion IS NULL AND precision_medicion_g IS NULL)
            OR (peso_medido_kg > 0 AND TRIM(instrumento_medicion) IS NOT NULL AND precision_medicion_g > 0));
    ok('Ningún depósito guardado tiene una medición incompleta', n = 0);

    -- Revisión del PR #12 (P2, segunda observación): registrar_regla bloquea la fila padre de MATERIAL.
    -- La prueba de dos sesiones simultáneas está en 20_prueba_concurrencia_tarifas.sql.
    espera_error('Tarifa para un material inexistente (ORA-20207)', -20207,
        'DECLARE v NUMBER; BEGIN pkg_ecopuntos.registrar_regla(999999, 5, 1, ''x'', SYS_EXTRACT_UTC(SYSTIMESTAMP), ' || v_adm || ', 1, v); END;');
    pkg_ecopuntos.registrar_regla(v_plastico, 7, 1, 'Prueba: primera tarifa', SYS_EXTRACT_UTC(SYSTIMESTAMP), v_adm, 1, v_mov);
    SELECT COUNT(*) INTO n FROM regla_ecopuntos WHERE id_material = v_plastico AND hasta_utc IS NULL;
    ok('La primera tarifa de un material sin reglas deja una sola versión abierta', n = 1);
    espera_error('Actor inexistente (ORA-20100)', -20100, 'BEGIN pkg_contexto_app.fijar_actor(999999); END;');
    ok('tiene_rol: la administradora tiene rol 3', pkg_contexto_app.tiene_rol(v_adm, 3) = 1);
    ok('tiene_rol: un ciudadano no tiene rol 2', pkg_contexto_app.tiene_rol(v_cid, 2) = 0);
    UPDATE usuario SET activo = 0 WHERE id_usuario = v_op2;
    ok('tiene_rol: un operario DESACTIVADO ya no cuenta como operario', pkg_contexto_app.tiene_rol(v_op2, 2) = 0);
    ROLLBACK TO eco;

    espera_error('Estadísticas: periodo invertido (ORA-20801)', -20801,
        'DECLARE c SYS_REFCURSOR; BEGIN pkg_estadisticas.consultar_resumen(SYSDATE, SYSDATE - 7, NULL, NULL, c); END;');
    espera_error('Estadísticas: categoría 9 (ORA-20803)', -20803,
        'DECLARE c SYS_REFCURSOR; BEGIN pkg_estadisticas.consultar_resumen(SYSDATE - 7, SYSDATE, NULL, 9, c); END;');
    DECLARE
        r_dep NUMBER; r_ver NUMBER; r_pen NUMBER; r_rec NUMBER; r_sin NUMBER; r_ciu NUMBER;
        r_uni NUMBER; r_kge NUMBER; r_cpm NUMBER; r_kgm NUMBER;
        v_total_tabla NUMBER;
    BEGIN
        pr_consultar_estadisticas(TRUNC(SYSDATE) - 398, TRUNC(SYSDATE) + 1, NULL, NULL, v_cur);
        FETCH v_cur INTO r_dep, r_ver, r_pen, r_rec, r_sin, r_ciu, r_uni, r_kge, r_cpm, r_kgm;
        CLOSE v_cur;
        SELECT COUNT(*) INTO v_total_tabla FROM deposito;
        ok('pr_consultar_estadisticas: el total coincide con COUNT(*) de DEPOSITO', r_dep = v_total_tabla);
        ok('pr_consultar_estadisticas: verificados + pendientes + rechazados = total', r_ver + r_pen + r_rec = r_dep);
    END;
    DECLARE
        v_id NUMBER; v_nom VARCHAR2(100); v_d NUMBER; v_u NUMBER; v_kge NUMBER; v_kgm NUMBER; v_p NUMBER; v_filas NUMBER := 0;
        v_ok BOOLEAN := TRUE; v_ant NUMBER := 0;
    BEGIN
        v_cur := pkg_estadisticas.ranking_verificados(TRUNC(SYSDATE) - 30, TRUNC(SYSDATE) + 1, 5);
        LOOP
            FETCH v_cur INTO v_id, v_nom, v_d, v_u, v_kge, v_kgm, v_p;
            EXIT WHEN v_cur%NOTFOUND;
            v_filas := v_filas + 1;
            IF v_p < v_ant OR v_p > 5 THEN v_ok := FALSE; END IF;
            v_ant := v_p;
        END LOOP;
        CLOSE v_cur;
        ok('ranking_verificados: devuelve filas ordenadas con puesto <= 5 (' || v_filas || ' filas)', v_filas > 0 AND v_ok);
    END;
    v_cur := fn_depositos_ciudadano(v_cid);
    ok('fn_depositos_ciudadano devuelve un cursor abierto', v_cur%ISOPEN);
    CLOSE v_cur;

    ROLLBACK;
    DBMS_OUTPUT.PUT_LINE('==========================================================');
    DBMS_OUTPUT.PUT_LINE('TOTAL: ' || g_total || ' pruebas · PASAN: ' || (g_total - g_fallas) || ' · FALLAN: ' || g_fallas);
    DBMS_OUTPUT.PUT_LINE('Se hizo ROLLBACK: la base quedó igual que antes de las pruebas.');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('ERROR INESPERADO tras ' || g_total || ' pruebas: ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE(DBMS_UTILITY.FORMAT_ERROR_BACKTRACE);
        DBMS_OUTPUT.PUT_LINE('Se hizo ROLLBACK. Las pruebas quedaron INCOMPLETAS.');
END;
/
SET FEEDBACK ON
