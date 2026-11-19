-- =====================================================================
-- EcoAccess · Prueba D — consultas sobre las nueve vistas VW_* (SOLO LECTURA)
-- 1) Cada vista se puede consultar. 2) Sus columnas, en orden, son las documentadas en docs/database/06-vistas.md.
-- 3) Algunas comprobaciones de coherencia entre la vista y las tablas de las que sale.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
    v_ok NUMBER := 0; v_fallo NUMBER := 0; n NUMBER; m NUMBER;
    PROCEDURE ok(p VARCHAR2, c BOOLEAN) IS
    BEGIN
        IF c THEN v_ok := v_ok + 1; DBMS_OUTPUT.PUT_LINE('OK    ' || p);
        ELSE v_fallo := v_fallo + 1; DBMS_OUTPUT.PUT_LINE('FALLO ' || p); END IF;
    END;
    PROCEDURE vista(p_vista VARCHAR2, p_columnas VARCHAR2) IS
        v_real VARCHAR2(4000); v_filas NUMBER;
    BEGIN
        SELECT LISTAGG(column_name, ',') WITHIN GROUP (ORDER BY column_id) INTO v_real FROM user_tab_columns WHERE table_name = p_vista;
        EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ' || DBMS_ASSERT.SIMPLE_SQL_NAME(p_vista) INTO v_filas;
        ok('D ' || p_vista || ' responde (' || v_filas || ' filas) y tiene las columnas documentadas', v_real = p_columnas);
        IF v_real <> p_columnas THEN DBMS_OUTPUT.PUT_LINE('      real: ' || v_real); END IF;
    EXCEPTION WHEN OTHERS THEN
        ok('D ' || p_vista || ' -> ' || SQLERRM, FALSE);
    END;
BEGIN
    vista('VW_BI_AUDITORIA', 'ID_AUDITORIA,ID_DEPOSITO,OPERACION,ESTADO_ANTERIOR,ESTADO_NUEVO,CANTIDAD_ANTERIOR,CANTIDAD_NUEVA,PESO_MEDIDO_ANTERIOR_KG,PESO_MEDIDO_NUEVO_KG,ACTOR,MOTIVO,FECHA_COLOMBIA');
    vista('VW_BI_CIUDADANOS', 'CIUDADANO,FECHA_REGISTRO,ACTIVO,ES_SINTETICO,ORIGEN_DATO');
    vista('VW_BI_CONSULTAS_VISION', 'ID_CONSULTA,CIUDADANO,DIA,CANAL,MODO,RESULTADO,MATERIAL,CANECA_INDICADA,UNIDADES_ESTIMADAS,PESO_ESTIMADO_KG,CONTEXTO_QR,LATENCIA_MS,ORIGEN_DATO');
    vista('VW_BI_DEPOSITOS', 'ID_DEPOSITO,CIUDADANO,DIA,FECHA_HORA,ESTACION,CANECA,MATERIAL,MATERIAL_NOMBRE,ESTADO,EVIDENCIA,UNIDADES,PESO_ESTIMADO_KG,PESO_MEDIDO_KG,CANAL,ORIGEN_DATO');
    vista('VW_BI_ECOPUNTOS', 'ID_MOVIMIENTO,CIUDADANO,DIA,TIPO,PUNTOS,MATERIAL,TARIFA');
    vista('VW_CANECA_ESTACION', 'ID_PUNTO_INSTALACION,ESTACION,CODIGO_QR_ESTACION,ID_CONTENEDOR,CANECA,CATEGORIA,COLOR,TIPO_RESIDUO,ESTADO,ESTADO_TEXTO,QR_LEGADO_CANECA,IDENTIFICADOR_DISPOSITIVO,ES_SINTETICO');
    vista('VW_DEPOSITO_DETALLE', 'ID_DEPOSITO,ID_INTERACCION,ID_TITULAR,TITULAR,ID_CONTENEDOR,CANECA,COLOR,ESTACION,MATERIAL_DECLARADO,MATERIAL_VERIFICADO,CANTIDAD_DECLARADA,CANTIDAD_VERIFICADA,EVIDENCIA_FISICA,ESTADO_VERIFICACION,ESTADO_TEXTO,ID_VERIFICADOR,VERIFICADOR,PESO_ESTIMADO_KG,PESO_ESTIMADO_G,PESO_MEDIDO_KG,PESO_MEDIDO_G,FECHA_DETECCION_UTC,FECHA_COLOMBIA,CANAL,ES_SINTETICO');
    vista('VW_RECORRIDO', 'ID_INTERACCION,ID_USUARIO,ID_PUNTO_INSTALACION,CANAL,ESTADO_SESION,ID_MATERIAL_SUGERIDO,ID_ORDEN,ESTADO_ORDEN,APERTURAS_CONFIRMADAS,ID_DEPOSITO,SIN_SENSOR,ESTADO_VERIFICACION,INICIO_COLOMBIA,ES_SINTETICO');
    vista('VW_SALDO_ECOPUNTOS', 'ID_USUARIO,NOMBRE,SALDO,ACREDITADOS,REVERTIDOS,MOVIMIENTOS,ES_SINTETICO');

    -- Coherencia con las tablas de origen.
    SELECT COUNT(*) INTO n FROM vw_deposito_detalle;  SELECT COUNT(*) INTO m FROM deposito;
    ok('D VW_DEPOSITO_DETALLE tiene una fila por depósito (' || n || ' = ' || m || ')', n = m);
    SELECT COUNT(*) INTO n FROM vw_bi_depositos;
    ok('D VW_BI_DEPOSITOS tiene una fila por depósito', n = m);
    SELECT COUNT(*) INTO n FROM vw_deposito_detalle WHERE CASE WHEN estado_verificacion = 1 THEN 1 ELSE 0 END <> CASE WHEN material_verificado IS NOT NULL THEN 1 ELSE 0 END;
    ok('D VERIFICADO en la vista coincide con ID_MATERIAL_VERIFICADO no nulo', n = 0);
    SELECT COUNT(*) INTO n FROM vw_caneca_estacion;   SELECT COUNT(*) INTO m FROM contenedor;
    ok('D VW_CANECA_ESTACION tiene una fila por caneca (' || n || ')', n = m);
    SELECT COUNT(*) INTO n FROM vw_caneca_estacion WHERE categoria IS NOT NULL AND color NOT IN ('BLANCA', 'VERDE', 'NEGRA');
    ok('D toda caneca configurada es BLANCA, VERDE o NEGRA', n = 0);
    SELECT NVL(SUM(saldo), 0) INTO n FROM vw_saldo_ecopuntos;
    SELECT NVL(SUM(m.puntos), 0) INTO m FROM movimiento_ecopuntos m
    WHERE EXISTS (SELECT 1 FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE ur.id_usuario = m.id_usuario AND r.codigo = 1);
    ok('D la suma de VW_SALDO_ECOPUNTOS es la suma del libro de movimientos de ciudadanos (' || n || ')', n = m);
    SELECT COUNT(*) INTO n FROM vw_recorrido;  SELECT COUNT(*) INTO m FROM interaccion;
    ok('D VW_RECORRIDO cubre todas las interacciones (' || n || ' filas para ' || m || ')', n >= m);
    SELECT COUNT(*) INTO n FROM vw_bi_ciudadanos WHERE ciudadano NOT LIKE 'C-%';
    ok('D VW_BI_CIUDADANOS seudonimiza (C-0000), sin nombre ni correo', n = 0);
    DBMS_OUTPUT.PUT_LINE('RESULTADO D: ' || v_ok || ' correctas, ' || v_fallo || ' fallidas.');
END;
/
