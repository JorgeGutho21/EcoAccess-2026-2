-- =====================================================================
-- EcoAccess · Prueba B — estructura (SOLO LECTURA)
-- Verifica que existan las tablas, vistas, paquetes, procedimientos, funciones y disparadores de EcoAccess
-- (lista tomada de database/schema/INVENTARIO.md) y las restricciones clave de las tres canecas y los ecopuntos.
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
    v_ok NUMBER := 0; v_fallo NUMBER := 0; n NUMBER;
    TYPE t_lista IS TABLE OF VARCHAR2(128);
    PROCEDURE ok(p VARCHAR2, c BOOLEAN) IS
    BEGIN
        IF c THEN v_ok := v_ok + 1; DBMS_OUTPUT.PUT_LINE('OK    ' || p);
        ELSE v_fallo := v_fallo + 1; DBMS_OUTPUT.PUT_LINE('FALLO ' || p); END IF;
    END;
    PROCEDURE existen(p_tipo VARCHAR2, p_lista t_lista) IS
        c NUMBER; faltan VARCHAR2(4000);
    BEGIN
        FOR i IN 1 .. p_lista.COUNT LOOP
            SELECT COUNT(*) INTO c FROM user_objects WHERE object_type = p_tipo AND object_name = p_lista(i);
            IF c = 0 THEN faltan := faltan || ' ' || p_lista(i); END IF;
        END LOOP;
        ok('B ' || p_tipo || ': ' || p_lista.COUNT || ' esperados' || CASE WHEN faltan IS NOT NULL THEN ' — faltan:' || faltan END, faltan IS NULL);
    END;
    PROCEDURE restriccion(p_nombre VARCHAR2, p_tabla VARCHAR2, p_tipo VARCHAR2) IS
        c NUMBER;
    BEGIN
        SELECT COUNT(*) INTO c FROM user_constraints WHERE constraint_name = p_nombre AND table_name = p_tabla AND constraint_type = p_tipo AND status = 'ENABLED';
        ok('B restricción ' || p_nombre || ' (' || p_tipo || ') en ' || p_tabla, c = 1);
    END;
BEGIN
    existen('TABLE', t_lista('ANALISIS_IMAGEN', 'AUDITORIA_DEPOSITO', 'COMENTARIO_CIUDADANO', 'CONFIGURACION_CANECA',
        'CONSENTIMIENTO_VISION_EXTERNA', 'CONSULTA_VISION', 'CONTENEDOR', 'CONTENEDOR_MATERIAL', 'CONVERSACION_TELEGRAM',
        'CREDENCIAL_ACCESO', 'DEPOSITO', 'ECO_MIGRACION_APLICADA', 'EVENTO_DISPOSITIVO', 'HISTORIAL_ESTADO_CONTENEDOR',
        'INTERACCION', 'LOTE_SIMULACION', 'MATERIAL', 'MOVIMIENTO_ECOPUNTOS', 'OPERACION_ECOPUNTOS', 'OPINION', 'ORDEN_APERTURA',
        'PESO_REFERENCIAL', 'PUNTO_DISPOSICION', 'PUNTO_INSTALACION', 'REGISTRO_SIMULACION', 'REGLA_ECOPUNTOS', 'ROL',
        'TOKEN_VINCULACION', 'UPDATE_TELEGRAM', 'USUARIO', 'USUARIO_ROL', 'VINCULACION_PENDIENTE', 'VINCULO_TELEGRAM'));
    existen('VIEW', t_lista('VW_BI_AUDITORIA', 'VW_BI_CIUDADANOS', 'VW_BI_CONSULTAS_VISION', 'VW_BI_DEPOSITOS', 'VW_BI_ECOPUNTOS',
        'VW_CANECA_ESTACION', 'VW_DEPOSITO_DETALLE', 'VW_RECORRIDO', 'VW_SALDO_ECOPUNTOS', 'EA_R_BASE_DEPOSITOS', 'EA_R_BASE_PUNTOS',
        'EA_R_CONSULTAS_VISION', 'EA_R_DEPOSITOS', 'EA_R_MATERIALES', 'EA_R_OPINIONES', 'EA_R_PARTICIPACION_MENSUAL', 'EA_R_PUNTOS',
        'EA_R_SOLICITUDES', 'VISTA_CANECAS_ESTACION', 'VISTA_DEPOSITOS_ESTADO', 'VISTA_LOTES_SIMULACION'));
    existen('PACKAGE', t_lista('PKG_CONTEXTO_APP', 'PKG_DEPOSITO', 'PKG_ECOPUNTOS', 'PKG_ESTADISTICAS', 'PKG_ECOACCESS_STATS'));
    existen('PACKAGE BODY', t_lista('PKG_CONTEXTO_APP', 'PKG_DEPOSITO', 'PKG_ECOPUNTOS', 'PKG_ESTADISTICAS', 'PKG_ECOACCESS_STATS'));
    existen('PROCEDURE', t_lista('PR_CAMBIAR_ESTADO_CONTENEDOR', 'PR_CONSULTAR_ESTADISTICAS'));
    existen('FUNCTION', t_lista('FN_DEPOSITOS_CIUDADANO', 'FN_PESO_ESTIMADO_KG', 'FN_RESOLVER_CANECA_DISPONIBLE'));
    existen('TRIGGER', t_lista('TR_ECOP_UNIDADES', 'TRG_CONTENEDOR_AUD_ESTADO', 'TRG_DEPOSITO_AUD_VERIFICACION',
        'TRG_DEPOSITO_BLOQUEO_CERRADO', 'TRG_DEPOSITO_COHERENCIA'));

    SELECT COUNT(*) INTO n FROM user_triggers WHERE status <> 'ENABLED';
    ok('B todos los disparadores están habilitados', n = 0);
    SELECT COUNT(*) INTO n FROM user_tab_identity_cols;
    ok('B 19 columnas IDENTITY (secuencias ISEQ$$ de sistema)', n = 19);

    -- Reglas que sostienen las tres canecas y el libro de ecopuntos.
    restriccion('CK_CONFIGURACION_CATEGORIA', 'CONFIGURACION_CANECA', 'C');
    restriccion('CK_CONTENEDOR_ESTADO', 'CONTENEDOR', 'C');
    restriccion('CK_MATERIAL_CATEGORIA', 'MATERIAL', 'C');
    restriccion('UQ_DEPOSITO_ORDEN', 'DEPOSITO', 'U');
    restriccion('CK_DEPOSITO_VERIFICACION', 'DEPOSITO', 'C');
    restriccion('UQ_ECOP_OPERACION_CLAVE', 'OPERACION_ECOPUNTOS', 'U');
    restriccion('UQ_ECOP_REVERSO', 'MOVIMIENTO_ECOPUNTOS', 'U');
    restriccion('FK_ECOP_MOV_REVERTIDO', 'MOVIMIENTO_ECOPUNTOS', 'R');
    SELECT COUNT(*) INTO n FROM user_indexes WHERE index_name = 'UQ_ECOP_CREDITO_INICIAL' AND uniqueness = 'UNIQUE' AND status = 'VALID';
    ok('B índice único UQ_ECOP_CREDITO_INICIAL (un solo crédito inicial por depósito)', n = 1);
    SELECT COUNT(*) INTO n FROM user_constraints WHERE constraint_type = 'R' AND status <> 'ENABLED';
    ok('B todas las claves foráneas están habilitadas', n = 0);
    DBMS_OUTPUT.PUT_LINE('RESULTADO B: ' || v_ok || ' correctas, ' || v_fallo || ' fallidas.');
END;
/
