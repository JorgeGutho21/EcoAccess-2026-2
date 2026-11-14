-- 15_DISPARADORES_EJERCICIOS — 12 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E278 · 18. Disparadores (triggers) · Ver los disparadores
-- Objetivo: Listar triggers y su momento.
-- Explicación: BEFORE/AFTER, EACH ROW y el evento.
SELECT trigger_name, trigger_type, triggering_event, table_name FROM user_triggers ORDER BY table_name;

-- ===== E279 · 18. Disparadores (triggers) · Trigger 1 en acción (y deshacer)
-- Objetivo: Cambiar el estado de una caneca deja historia.
-- Explicación: AFTER UPDATE OF estado escribe en HISTORIAL_ESTADO_CONTENEDOR.
DECLARE
    v_adm NUMBER; v_antes NUMBER; v_despues NUMBER;
BEGIN
    SELECT id_usuario INTO v_adm FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT COUNT(*) INTO v_antes FROM historial_estado_contenedor;
    pr_cambiar_estado_contenedor(12, 2, v_adm, 'Ejercicio de la guía');
    SELECT COUNT(*) INTO v_despues FROM historial_estado_contenedor;
    DBMS_OUTPUT.PUT_LINE('Historial: ' || v_antes || ' -> ' || v_despues);
    ROLLBACK;
END;
/

-- ===== E280 · 18. Disparadores (triggers) · Trigger 4 en acción
-- Objetivo: UPDATE directo de un depósito verificado (ORA-20103).
-- Explicación: BEFORE UPDATE bloquea; solo el procedimiento de corrección puede.
DECLARE
    v_dep NUMBER;
BEGIN
    SELECT MIN(id_deposito) INTO v_dep FROM deposito WHERE estado_verificacion = 1;
    UPDATE deposito SET cantidad_verificada = 99 WHERE id_deposito = v_dep;
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/

-- ===== E281 · 18. Disparadores (triggers) · Trigger 3: leer la auditoría
-- Objetivo: Ver :OLD y :NEW guardados.
-- Explicación: Antes y después de cada corrección.
SELECT id_deposito, operacion, estado_anterior, estado_nuevo, cantidad_anterior, cantidad_nueva, motivo FROM auditoria_deposito WHERE operacion IN ('CORRECCION', 'REAPERTURA');

-- ===== E282 · 18. Disparadores (triggers) · Trigger 2: material que la caneca no admite
-- Objetivo: Restos de fruta en la caneca blanca (ORA-20102).
-- Explicación: BEFORE INSERT revisa CONTENEDOR_MATERIAL.
DECLARE
    v_o NUMBER; v_e NUMBER;
BEGIN
    SELECT MIN(id_orden) INTO v_o FROM orden_apertura WHERE estado = 7 AND id_contenedor = 11;
    INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado) VALUES (11, 'GUIA-EJ', 3, v_o, 1) RETURNING id_evento INTO v_e;
    INSERT INTO deposito (id_orden, id_evento, id_material_declarado) VALUES (v_o, v_e, 201);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/

-- ===== E283 · 18. Disparadores (triggers) · Orden de los triggers
-- Objetivo: ¿Qué trigger sigue a cuál?
-- Explicación: FOLLOWS define el orden entre triggers del mismo momento.
SELECT trigger_name, referenced_trigger_name FROM user_trigger_ordering;

-- ===== E284 · 18. Disparadores (triggers) · Tipo y evento de cada trigger
-- Objetivo: Leer cuándo dispara cada uno.
-- Explicación: TRIGGER_TYPE dice BEFORE/AFTER y EACH ROW.
SELECT trigger_name, trigger_type, triggering_event, status FROM user_triggers ORDER BY trigger_name;

-- ===== E285 · 18. Disparadores (triggers) · La cláusula WHEN guardada
-- Objetivo: Ver el WHEN de los triggers.
-- Explicación: En WHEN se escribe OLD/NEW sin dos puntos.
SELECT trigger_name, when_clause FROM user_triggers WHERE when_clause IS NOT NULL;

-- ===== E286 · 18. Disparadores (triggers) · Trigger 1 con el mismo estado
-- Objetivo: Un UPDATE que no cambia el estado no deja historia.
-- Explicación: WHEN (OLD.estado <> NEW.estado) evita registros falsos.
DECLARE
    v_antes NUMBER; v_despues NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_antes FROM historial_estado_contenedor;
    UPDATE contenedor SET estado = estado WHERE id_contenedor = 11;
    SELECT COUNT(*) INTO v_despues FROM historial_estado_contenedor;
    DBMS_OUTPUT.PUT_LINE('Historial: ' || v_antes || ' -> ' || v_despues || ' (no cambia)');
    ROLLBACK;
END;
/

-- ===== E287 · 18. Disparadores (triggers) · Trigger 2: autoverificación
-- Objetivo: El titular intenta verificar lo suyo (ORA-20101).
-- Explicación: El BEFORE compara el verificador con el titular de la sesión.
DECLARE
    v_dep NUMBER; v_tit NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario INTO v_dep, v_tit FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 FETCH FIRST 1 ROWS ONLY;
    UPDATE deposito SET estado_verificacion = 1, id_verificador = v_tit, fecha_verificacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP) WHERE id_deposito = v_dep;
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/

-- ===== E288 · 18. Disparadores (triggers) · Trigger 3: auditoría deshecha
-- Objetivo: Sin COMMIT en el trigger, el ROLLBACK borra también su auditoría.
-- Explicación: La auditoría pertenece a la misma transacción.
DECLARE
    v_dep NUMBER; v_ver NUMBER; v_mat NUMBER; v_antes NUMBER; v_durante NUMBER; v_despues NUMBER;
BEGIN
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT d.id_deposito, d.id_material_declarado INTO v_dep, v_mat FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 AND i.id_usuario <> v_ver FETCH FIRST 1 ROWS ONLY;
    SELECT COUNT(*) INTO v_antes FROM auditoria_deposito;
    pkg_deposito.verificar(v_dep, v_ver, v_mat, 1);
    SELECT COUNT(*) INTO v_durante FROM auditoria_deposito;
    ROLLBACK;
    SELECT COUNT(*) INTO v_despues FROM auditoria_deposito;
    DBMS_OUTPUT.PUT_LINE('Auditoría: ' || v_antes || ' -> ' || v_durante || ' -> (ROLLBACK) ' || v_despues);
END;
/

-- ===== E289 · 18. Disparadores (triggers) · Historial real de canecas
-- Objetivo: Lo que dejó el trigger 1 en la carga.
-- Explicación: Cada fila es un cambio de disponibilidad con su actor.
SELECT h.id_contenedor, h.estado_anterior, h.estado_nuevo, u.nombre AS actor, h.motivo FROM historial_estado_contenedor h LEFT JOIN usuario u ON u.id_usuario = h.id_actor ORDER BY h.id_historial;
