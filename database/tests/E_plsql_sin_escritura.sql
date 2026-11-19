-- =====================================================================
-- EcoAccess · Prueba E — lógica PL/SQL sin escrituras (apta para el esquema compartido)
-- Ejecuta funciones y consultas, y comprueba que las validaciones rechacen datos inválidos ANTES de escribir:
-- cada caso de error termina en RAISE_APPLICATION_ERROR antes de cualquier INSERT o UPDATE (ver el código en
-- database/schema). Los identificadores se buscan en la base; no se fijan a mano. Termina con ROLLBACK.
-- Las pruebas que sí escriben (verificar, acreditar, revertir y disparadores) están en
-- database/05_pruebas/V027_pruebas_capa_academica.sql y solo se ejecutan en el esquema aislado (Prueba F).
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
    v_ok NUMBER := 0; v_fallo NUMBER := 0; v_omit NUMBER := 0; n NUMBER; m NUMBER; v_cur SYS_REFCURSOR;
    v_punto NUMBER; v_mat NUMBER; v_caneca NUMBER; v_peligroso NUMBER; v_ciud NUMBER; v_admin NUMBER; v_oper NUMBER;
    v_dep_pend NUMBER; v_dep_verif NUMBER; v_peso NUMBER; v_gramos NUMBER; v_sin_regla NUMBER; v_con_regla NUMBER;
    v_titular NUMBER; v_inexistente NUMBER;
    a NUMBER; b NUMBER; c NUMBER; d NUMBER; e NUMBER; f NUMBER; g NUMBER; h NUMBER; x VARCHAR2(200); dt DATE;

    PROCEDURE ok(p VARCHAR2, cond BOOLEAN) IS
    BEGIN
        IF cond THEN v_ok := v_ok + 1; DBMS_OUTPUT.PUT_LINE('OK    ' || p);
        ELSE v_fallo := v_fallo + 1; DBMS_OUTPUT.PUT_LINE('FALLO ' || p); END IF;
    END;
    PROCEDURE omitir(p VARCHAR2) IS
    BEGIN
        v_omit := v_omit + 1; DBMS_OUTPUT.PUT_LINE('OMITE ' || p || ' (no hay datos en este esquema para probarlo)');
    END;
    -- Ejecuta un bloque PL/SQL y exige un error concreto. Los valores van como literales numéricos.
    PROCEDURE error_esperado(p VARCHAR2, p_codigo NUMBER, p_bloque VARCHAR2) IS
    BEGIN
        EXECUTE IMMEDIATE 'DECLARE n NUMBER; cur SYS_REFCURSOR; BEGIN ' || p_bloque || ' END;';
        ok(p || ' (se esperaba ORA' || p_codigo || ')', FALSE);
    EXCEPTION WHEN OTHERS THEN
        ok(p || ' -> ORA' || SQLCODE, SQLCODE = p_codigo);
    END;
    FUNCTION uno(p_sql VARCHAR2) RETURN NUMBER IS
        v NUMBER;
    BEGIN
        EXECUTE IMMEDIATE p_sql INTO v;
        RETURN v;
    EXCEPTION WHEN NO_DATA_FOUND THEN RETURN NULL;
    END;
    FUNCTION usuario_con_rol(p_codigo NUMBER, p_solo BOOLEAN) RETURN NUMBER IS
    BEGIN
        RETURN uno('SELECT MIN(u.id_usuario) FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario '
                || 'JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = ' || p_codigo || ' AND u.activo = 1'
                || CASE WHEN p_solo THEN ' AND NOT EXISTS (SELECT 1 FROM usuario_rol x JOIN rol y ON y.id_rol = x.id_rol '
                                      || 'WHERE x.id_usuario = u.id_usuario AND y.codigo <> ' || p_codigo || ')' END);
    END;
BEGIN
    -- Datos de referencia tomados de la base. Caneca en servicio, sin otra de su color en la estación,
    -- y un material habilitado de su categoría que esa caneca admite.
    SELECT MIN(c.id_punto_instalacion) KEEP (DENSE_RANK FIRST ORDER BY c.id_contenedor, cm.id_material),
           MIN(cm.id_material)         KEEP (DENSE_RANK FIRST ORDER BY c.id_contenedor, cm.id_material),
           MIN(c.id_contenedor)
    INTO   v_punto, v_mat, v_caneca
    FROM   contenedor c
    JOIN   punto_instalacion p    ON p.id_punto_instalacion = c.id_punto_instalacion AND p.activo = 1
    JOIN   configuracion_caneca k ON k.id_contenedor = c.id_contenedor
    JOIN   contenedor_material cm ON cm.id_contenedor = c.id_contenedor
    JOIN   material mt            ON mt.id_material = cm.id_material AND mt.categoria = k.categoria AND mt.habilitado = 1
    WHERE  c.estado = 1
    AND    NOT EXISTS (SELECT 1 FROM contenedor c2 JOIN configuracion_caneca k2 ON k2.id_contenedor = c2.id_contenedor
                       WHERE c2.id_punto_instalacion = c.id_punto_instalacion AND k2.categoria = k.categoria
                       AND c2.estado <> 3 AND c2.id_contenedor <> c.id_contenedor);
    v_peligroso   := uno('SELECT MIN(id_material) FROM material WHERE categoria = 4 AND habilitado = 1');
    v_ciud        := usuario_con_rol(1, TRUE);
    v_admin       := usuario_con_rol(3, FALSE);
    v_oper        := usuario_con_rol(2, FALSE);
    v_dep_pend    := uno('SELECT MIN(id_deposito) FROM deposito WHERE id_material_verificado IS NULL');
    v_dep_verif   := uno('SELECT MIN(id_deposito) FROM deposito WHERE id_material_verificado IS NOT NULL');
    v_peso        := uno('SELECT MIN(id_peso_referencial) FROM peso_referencial');
    v_con_regla   := uno('SELECT MIN(id_material) FROM regla_ecopuntos WHERE desde_utc <= SYS_EXTRACT_UTC(SYSTIMESTAMP) AND (hasta_utc IS NULL OR hasta_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP))');
    v_sin_regla   := uno('SELECT MIN(id_material) FROM material m WHERE NOT EXISTS (SELECT 1 FROM regla_ecopuntos r WHERE r.id_material = m.id_material)');
    v_titular     := uno('SELECT MAX(id_titular) KEEP (DENSE_RANK LAST ORDER BY cnt) FROM (SELECT id_titular, COUNT(*) cnt FROM vw_deposito_detalle WHERE id_titular IS NOT NULL GROUP BY id_titular)');
    v_inexistente := uno('SELECT NVL(MAX(id_deposito), 0) + 1000000 FROM deposito');

    -- E1. FN_RESOLVER_CANECA_DISPONIBLE: la caneca correcta o un error explicado.
    IF v_punto IS NOT NULL THEN
        n := fn_resolver_caneca_disponible(v_punto, v_mat);
        ok('E1.1 FN_RESOLVER_CANECA_DISPONIBLE(estación ' || v_punto || ', material ' || v_mat || ') = caneca ' || v_caneca || ' (devolvió ' || n || ')', n = v_caneca);
    ELSE omitir('E1.1 caneca resuelta'); END IF;
    error_esperado('E1.2 identificadores nulos', -20810, 'n := fn_resolver_caneca_disponible(NULL, 1);');
    error_esperado('E1.3 identificador negativo', -20810, 'n := fn_resolver_caneca_disponible(-1, 1);');
    error_esperado('E1.4 identificador con decimales', -20810, 'n := fn_resolver_caneca_disponible(1.5, 1);');
    error_esperado('E1.5 estación inexistente', -20818, 'n := fn_resolver_caneca_disponible(' || (uno('SELECT NVL(MAX(id_punto_instalacion),0) + 100000 FROM punto_instalacion')) || ', 1);');
    IF v_punto IS NOT NULL THEN
        error_esperado('E1.6 material inexistente', -20811, 'n := fn_resolver_caneca_disponible(' || v_punto || ', ' || uno('SELECT NVL(MAX(id_material),0) + 100000 FROM material') || ');');
        IF v_peligroso IS NOT NULL THEN
            error_esperado('E1.7 material peligroso va a ruta especial', -20813, 'n := fn_resolver_caneca_disponible(' || v_punto || ', ' || v_peligroso || ');');
        ELSE omitir('E1.7 material peligroso'); END IF;
    ELSE omitir('E1.6 y E1.7'); END IF;

    -- E2. FN_PESO_ESTIMADO_KG.
    ok('E2.1 FN_PESO_ESTIMADO_KG con referencia nula devuelve NULL', fn_peso_estimado_kg(NULL, 2) IS NULL);
    ok('E2.2 FN_PESO_ESTIMADO_KG con 0 unidades devuelve NULL', fn_peso_estimado_kg(NVL(v_peso, 1), 0) IS NULL);
    ok('E2.3 FN_PESO_ESTIMADO_KG con referencia inexistente devuelve NULL', fn_peso_estimado_kg(-1, 2) IS NULL);
    IF v_peso IS NOT NULL THEN
        SELECT gramos_tipico INTO v_gramos FROM peso_referencial WHERE id_peso_referencial = v_peso;
        ok('E2.4 FN_PESO_ESTIMADO_KG(ref ' || v_peso || ', 3) = 3 × ' || v_gramos || ' g / 1000', fn_peso_estimado_kg(v_peso, 3) = ROUND(v_gramos * 3 / 1000, 4));
    ELSE omitir('E2.4 peso estimado'); END IF;

    -- E3. FN_DEPOSITOS_CIUDADANO devuelve los mismos depósitos que la vista.
    IF v_titular IS NOT NULL THEN
        v_cur := fn_depositos_ciudadano(v_titular);
        n := 0;
        LOOP FETCH v_cur INTO a, dt, x, x, x, x, x, b, c; EXIT WHEN v_cur%NOTFOUND; n := n + 1; END LOOP;
        CLOSE v_cur;
        SELECT COUNT(*) INTO m FROM vw_deposito_detalle WHERE id_titular = v_titular;
        ok('E3.1 FN_DEPOSITOS_CIUDADANO(' || v_titular || ') devuelve ' || n || ' filas = VW_DEPOSITO_DETALLE', n = m AND n > 0);
    ELSE omitir('E3.1 depósitos de un ciudadano'); END IF;
    v_cur := fn_depositos_ciudadano(-1);
    FETCH v_cur INTO a, dt, x, x, x, x, x, b, c;
    ok('E3.2 FN_DEPOSITOS_CIUDADANO de un usuario inexistente devuelve un cursor vacío', v_cur%NOTFOUND);
    CLOSE v_cur;

    -- E4. PKG_ECOPUNTOS: consultas.
    IF v_ciud IS NOT NULL THEN
        SELECT saldo INTO m FROM vw_saldo_ecopuntos WHERE id_usuario = v_ciud;
        ok('E4.1 PKG_ECOPUNTOS.saldo(' || v_ciud || ') = VW_SALDO_ECOPUNTOS (' || m || ')', pkg_ecopuntos.saldo(v_ciud) = m);
    ELSE omitir('E4.1 saldo'); END IF;
    ok('E4.2 PKG_ECOPUNTOS.saldo de un usuario inexistente es 0', pkg_ecopuntos.saldo(-1) = 0);
    IF v_con_regla IS NOT NULL THEN
        ok('E4.3 PKG_ECOPUNTOS.regla_vigente encuentra la tarifa vigente del material ' || v_con_regla, pkg_ecopuntos.regla_vigente(v_con_regla, SYS_EXTRACT_UTC(SYSTIMESTAMP)) IS NOT NULL);
    ELSE omitir('E4.3 tarifa vigente'); END IF;
    IF v_sin_regla IS NOT NULL THEN
        ok('E4.4 PKG_ECOPUNTOS.regla_vigente sin tarifa devuelve NULL (no hay puntos inventados)', pkg_ecopuntos.regla_vigente(v_sin_regla, SYS_EXTRACT_UTC(SYSTIMESTAMP)) IS NULL);
    ELSE omitir('E4.4 material sin tarifa'); END IF;
    ok('E4.5 PKG_ECOPUNTOS.credito_vigente de un depósito inexistente es NULL', pkg_ecopuntos.credito_vigente(v_inexistente) IS NULL);
    IF v_dep_pend IS NOT NULL AND v_oper IS NOT NULL THEN
        error_esperado('E4.6 acreditar un depósito PENDIENTE se rechaza', -20201, 'pkg_ecopuntos.acreditar(' || v_dep_pend || ', ' || v_oper || ', n);');
    ELSE omitir('E4.6 acreditar pendiente'); END IF;
    error_esperado('E4.7 acreditar un depósito inexistente', -20110, 'pkg_ecopuntos.acreditar(' || v_inexistente || ', 1, n);');
    error_esperado('E4.8 revertir sin motivo', -20112, 'pkg_ecopuntos.revertir(' || v_inexistente || ', 1, ''  '');');

    -- E5. PKG_DEPOSITO: estado y validaciones previas a escribir.
    error_esperado('E5.1 estado de un depósito inexistente', -20110, 'n := pkg_deposito.estado(' || v_inexistente || ');');
    IF v_dep_verif IS NOT NULL THEN
        ok('E5.2 PKG_DEPOSITO.estado de un depósito con material verificado = 1', pkg_deposito.estado(v_dep_verif) = 1);
    ELSE omitir('E5.2 depósito verificado'); END IF;
    IF v_dep_pend IS NOT NULL THEN
        ok('E5.3 PKG_DEPOSITO.estado de un depósito sin verificar es 0 o 2', pkg_deposito.estado(v_dep_pend) IN (0, 2));
        error_esperado('E5.4 rechazar sin motivo', -20112, 'pkg_deposito.rechazar(' || v_dep_pend || ', 1, ''  '');');
        IF v_ciud IS NOT NULL THEN
            error_esperado('E5.5 un ciudadano no puede verificar', -20120, 'pkg_deposito.verificar(' || v_dep_pend || ', ' || v_ciud || ', 1, 1);');
        ELSE omitir('E5.5 ciudadano verificador'); END IF;
    ELSE omitir('E5.3 a E5.5 depósito pendiente'); END IF;
    error_esperado('E5.6 corrección con estado inválido', -20113, 'pkg_deposito.corregir_verificacion(1, 1, 7, NULL, NULL, ''motivo'');');
    ok('E5.7 no hay corrección en curso fuera de PKG_DEPOSITO', NOT pkg_deposito.correccion_en_curso);

    -- E6. PKG_CONTEXTO_APP: roles.
    IF v_admin IS NOT NULL THEN ok('E6.1 tiene_rol(administrador ' || v_admin || ', 3) = 1', pkg_contexto_app.tiene_rol(v_admin, 3) = 1);
    ELSE omitir('E6.1 administrador'); END IF;
    IF v_ciud IS NOT NULL THEN
        ok('E6.2 tiene_rol(ciudadano ' || v_ciud || ', 3) = 0', pkg_contexto_app.tiene_rol(v_ciud, 3) = 0);
        error_esperado('E6.3 exigir_rol de administrador a un ciudadano', -20120, 'pkg_contexto_app.exigir_rol(' || v_ciud || ', ''3'');');
    ELSE omitir('E6.2 y E6.3 ciudadano'); END IF;
    error_esperado('E6.4 fijar_actor de un usuario inexistente', -20100, 'pkg_contexto_app.fijar_actor(-1);');
    ok('E6.5 sin actor fijado, actor es NULL', pkg_contexto_app.actor IS NULL);

    -- E7. PR_CAMBIAR_ESTADO_CONTENEDOR: rechaza antes del UPDATE.
    IF v_caneca IS NOT NULL AND v_ciud IS NOT NULL THEN
        error_esperado('E7.1 un ciudadano no cambia el estado de una caneca', -20120, 'pr_cambiar_estado_contenedor(' || v_caneca || ', 2, ' || v_ciud || ', ''prueba'');');
    ELSE omitir('E7.1'); END IF;
    IF v_caneca IS NOT NULL AND v_admin IS NOT NULL THEN
        error_esperado('E7.2 estado de caneca inválido (9)', -20121, 'pr_cambiar_estado_contenedor(' || v_caneca || ', 9, ' || v_admin || ', ''prueba'');');
    ELSE omitir('E7.2'); END IF;

    -- E8. Estadísticas: PKG_ESTADISTICAS y PR_CONSULTAR_ESTADISTICAS.
    ok('E8.1 PKG_ESTADISTICAS.a_utc: 1/1/2026 00:00 en Bogotá = 05:00 UTC',
       pkg_estadisticas.a_utc(DATE '2026-01-01') = TIMESTAMP '2026-01-01 05:00:00');
    pkg_estadisticas.consultar_resumen(SYSDATE - 365, SYSDATE + 1, NULL, NULL, v_cur);
    FETCH v_cur INTO a, b, c, d, e, f, g, h, n; CLOSE v_cur;
    SELECT COUNT(*) INTO m FROM deposito WHERE fecha_deteccion_utc >= pkg_estadisticas.a_utc(SYSDATE - 365) AND fecha_deteccion_utc < pkg_estadisticas.a_utc(SYSDATE + 1);
    ok('E8.2 consultar_resumen del último año cuenta ' || a || ' depósitos = conteo directo', a = m);
    ok('E8.3 verificados + pendientes_o_rechazados = depósitos', b + c = a);
    error_esperado('E8.4 periodo invertido', -20801, 'pkg_estadisticas.consultar_resumen(SYSDATE, SYSDATE - 1, NULL, NULL, cur);');
    error_esperado('E8.5 periodo de más de 400 días', -20801, 'pkg_estadisticas.consultar_resumen(SYSDATE - 500, SYSDATE, NULL, NULL, cur);');
    error_esperado('E8.6 estación 0', -20802, 'pkg_estadisticas.consultar_resumen(SYSDATE - 1, SYSDATE, 0, NULL, cur);');
    error_esperado('E8.7 categoría 9', -20803, 'pkg_estadisticas.consultar_resumen(SYSDATE - 1, SYSDATE, NULL, 9, cur);');
    v_cur := pkg_estadisticas.ranking_verificados(SYSDATE - 365, SYSDATE + 1, 5);
    n := 0; m := 0; g := 0;
    LOOP
        FETCH v_cur INTO a, x, b, c, d, e, f; EXIT WHEN v_cur%NOTFOUND;
        n := n + 1;
        IF f < g THEN m := m + 1; END IF;          -- puestos fuera de orden
        g := f;
    END LOOP;
    CLOSE v_cur;
    -- DENSE_RANK: «top 5» son 5 PUESTOS distintos; con empates puede haber más de 5 filas.
    ok('E8.8 ranking_verificados(top 5): ' || n || ' filas, último puesto ' || NVL(g, 0) || ' (<= 5) y en orden', NVL(g, 0) <= 5 AND m = 0);
    pr_consultar_estadisticas(SYS_EXTRACT_UTC(SYSTIMESTAMP) - NUMTODSINTERVAL(365, 'DAY'), SYS_EXTRACT_UTC(SYSTIMESTAMP) + INTERVAL '1' DAY, NULL, NULL, v_cur);
    FETCH v_cur INTO a, b, c, d, e, f, g, h; CLOSE v_cur;
    SELECT COUNT(*) INTO m FROM deposito WHERE fecha_deteccion_utc >= SYS_EXTRACT_UTC(SYSTIMESTAMP) - NUMTODSINTERVAL(365, 'DAY');
    ok('E8.9 PR_CONSULTAR_ESTADISTICAS (V022) cuenta ' || a || ' depósitos = conteo directo', a = m);
    error_esperado('E8.10 PR_CONSULTAR_ESTADISTICAS con categoría 9', -20803,
        'pr_consultar_estadisticas(SYSTIMESTAMP - INTERVAL ''1'' DAY, SYSTIMESTAMP, NULL, 9, cur);');

    ROLLBACK;
    DBMS_OUTPUT.PUT_LINE('RESULTADO E: ' || v_ok || ' correctas, ' || v_fallo || ' fallidas, ' || v_omit || ' omitidas.');
END;
/
