-- V022: objetos PL/SQL de consulta sobre el esquema existente.
-- Sin nuevas tablas, cambios a columnas, DML, GRANT ni COMMIT explicito.
-- Ejecutar en una conexion dedicada: Oracle confirma DDL implicitamente.
-- El instalador registra V022 solo despues de finalizar este bloque sin errores.
-- Reejecucion: acepta un objeto existente unicamente si su fuente coincide
-- exactamente con la esperada, incluidos firma, cuerpo y marcador SHA-256.
-- No reemplaza definiciones diferentes aunque compartan nombre o marcador.
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    TYPE definicion IS RECORD (
        nombre VARCHAR2(128),
        tipo VARCHAR2(30),
        fuente VARCHAR2(32767),
        existe BOOLEAN
    );
    TYPE definiciones IS TABLE OF definicion INDEX BY PLS_INTEGER;
    plan definiciones;
    tablas_antes PLS_INTEGER;
    tablas_despues PLS_INTEGER;
    n PLS_INTEGER;
    nombre_actual VARCHAR2(128);
    tipo_actual VARCHAR2(30);

    FUNCTION normalizar(p_texto VARCHAR2) RETURN VARCHAR2 IS
    BEGIN
        RETURN LTRIM(RTRIM(REPLACE(p_texto, CHR(13), ''),
               CHR(10) || CHR(9) || ' '), CHR(10) || CHR(9) || ' ');
    END;

    PROCEDURE definir(p_indice PLS_INTEGER, p_nombre VARCHAR2,
                      p_tipo VARCHAR2, p_fuente VARCHAR2) IS
    BEGIN
        plan(p_indice).nombre := p_nombre;
        plan(p_indice).tipo := p_tipo;
        plan(p_indice).fuente := normalizar(p_fuente);
        plan(p_indice).existe := FALSE;
    END;

    PROCEDURE exigir_columna(p_tabla VARCHAR2, p_columna VARCHAR2) IS
        cantidad PLS_INTEGER;
    BEGIN
        SELECT COUNT(*) INTO cantidad FROM user_tab_columns
        WHERE table_name = p_tabla AND column_name = p_columna;
        IF cantidad <> 1 THEN
            RAISE_APPLICATION_ERROR(-20882,
                'V022 requiere ' || p_tabla || '.' || p_columna || '.');
        END IF;
    END;

    PROCEDURE exigir_trigger IS
        cantidad PLS_INTEGER;
    BEGIN
        SELECT COUNT(*) INTO cantidad
        FROM user_objects o JOIN user_triggers t ON t.trigger_name=o.object_name
        WHERE o.object_name='TR_ECOP_UNIDADES'
          AND o.object_type='TRIGGER' AND o.status='VALID'
          AND t.status='ENABLED';
        IF cantidad <> 1 THEN
            RAISE_APPLICATION_ERROR(-20883,
                'V022 conserva y requiere TR_ECOP_UNIDADES VALID y ENABLED.');
        END IF;
    END;

    PROCEDURE revisar_colision(p_indice PLS_INTEGER) IS
        cantidad PLS_INTEGER;
        fuente_real VARCHAR2(32767);
        nombre_esperado VARCHAR2(128) := plan(p_indice).nombre;
        tipo_esperado VARCHAR2(30) := plan(p_indice).tipo;
    BEGIN
        SELECT COUNT(*) INTO cantidad FROM user_objects
        WHERE object_name = nombre_esperado
          AND object_type NOT IN (
              CASE WHEN tipo_esperado IN ('PACKAGE','PACKAGE BODY')
                   THEN 'PACKAGE' ELSE tipo_esperado END,
              CASE WHEN tipo_esperado IN ('PACKAGE','PACKAGE BODY')
                   THEN 'PACKAGE BODY' ELSE tipo_esperado END);
        IF cantidad <> 0 THEN
            RAISE_APPLICATION_ERROR(-20884,
                'Colision de tipo de objeto: ' || plan(p_indice).nombre || '.');
        END IF;
        SELECT COUNT(*) INTO cantidad FROM user_objects
        WHERE object_name = nombre_esperado
          AND object_type = tipo_esperado;
        IF cantidad = 0 THEN RETURN; END IF;

        FOR linea IN (SELECT text FROM user_source
                      WHERE name=nombre_esperado AND type=tipo_esperado
                      ORDER BY line) LOOP
            IF NVL(LENGTH(fuente_real),0) + NVL(LENGTH(linea.text),0) > 32767 THEN
                RAISE_APPLICATION_ERROR(-20885,
                    'Fuente existente excesiva; revisar sin reemplazar: '
                    || plan(p_indice).nombre || '.');
            END IF;
            fuente_real := fuente_real || linea.text;
        END LOOP;
        IF fuente_real IS NULL
           OR normalizar(fuente_real) <> plan(p_indice).fuente THEN
            RAISE_APPLICATION_ERROR(-20886,
                'Objeto existente diferente; no se reemplaza: '
                || plan(p_indice).nombre || ' (' || plan(p_indice).tipo || ').');
        END IF;
        plan(p_indice).existe := TRUE;
    END;
BEGIN
    IF SYS_CONTEXT('USERENV','SESSION_USER') <> SYS_CONTEXT('USERENV','CURRENT_SCHEMA')
       OR SYS_CONTEXT('USERENV','SESSION_USER') IN ('SYS','SYSTEM','ADMIN')
       OR SYS_CONTEXT('USERENV','CON_NAME') = 'CDB$ROOT' THEN
        RAISE_APPLICATION_ERROR(-20880,
            'V022 requiere una conexion dedicada como dueno del esquema.');
    END IF;
    SELECT COUNT(*) INTO n FROM user_sys_privs WHERE privilege='CREATE PROCEDURE';
    IF n = 0 THEN
        RAISE_APPLICATION_ERROR(-20881,
            'El dueno requiere CREATE PROCEDURE directo; no se conceden permisos.');
    END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name='ECO_MIGRACION_APLICADA';
    IF n <> 1 THEN
        RAISE_APPLICATION_ERROR(-20882,'Falta el historial de migraciones.');
    END IF;
    EXECUTE IMMEDIATE
        'SELECT COUNT(*) FROM eco_migracion_aplicada WHERE version=:version'
        INTO n USING 'V021';
    IF n <> 1 THEN
        RAISE_APPLICATION_ERROR(-20882,'V022 requiere V021 registrada.');
    END IF;
    SELECT COUNT(*) INTO tablas_antes FROM user_tables;
    exigir_trigger;

    exigir_columna('DEPOSITO','ID_ORDEN');
    exigir_columna('DEPOSITO','ID_MATERIAL_VERIFICADO');
    exigir_columna('DEPOSITO','ID_MATERIAL_DECLARADO');
    exigir_columna('DEPOSITO','FECHA_DETECCION_UTC');
    exigir_columna('DEPOSITO','SIN_SENSOR');
    exigir_columna('DEPOSITO','CANTIDAD_VERIFICADA');
    exigir_columna('DEPOSITO','PESO_MEDIDO_KG');
    exigir_columna('ORDEN_APERTURA','ID_ORDEN');
    exigir_columna('ORDEN_APERTURA','ID_INTERACCION');
    exigir_columna('ORDEN_APERTURA','ID_CONTENEDOR');
    exigir_columna('INTERACCION','ID_INTERACCION');
    exigir_columna('INTERACCION','ID_USUARIO');
    exigir_columna('CONTENEDOR','ID_CONTENEDOR');
    exigir_columna('CONTENEDOR','ID_PUNTO_INSTALACION');
    exigir_columna('CONTENEDOR','ESTADO');
    exigir_columna('MATERIAL','ID_MATERIAL');
    exigir_columna('MATERIAL','CATEGORIA');
    exigir_columna('MATERIAL','HABILITADO');
    exigir_columna('CONFIGURACION_CANECA','ID_CONTENEDOR');
    exigir_columna('CONFIGURACION_CANECA','CATEGORIA');
    exigir_columna('CONTENEDOR_MATERIAL','ID_CONTENEDOR');
    exigir_columna('CONTENEDOR_MATERIAL','ID_MATERIAL');
    exigir_columna('PUNTO_INSTALACION','ID_PUNTO_INSTALACION');
    exigir_columna('PUNTO_INSTALACION','ACTIVO');

    definir(1, 'PKG_ECOACCESS_STATS', 'PACKAGE', q'~PACKAGE PKG_ECOACCESS_STATS AUTHID DEFINER AS
-- ECOACCESS_V022_SOURCE_SHA256=4138349490eb1215d5f06b1859ed8b467041ac1b041a0a4e49f967ef3def80b9
    PROCEDURE consultar_resumen(
        p_desde_utc IN TIMESTAMP,
        p_hasta_utc IN TIMESTAMP,
        p_id_punto IN NUMBER,
        p_categoria IN NUMBER,
        p_resultado OUT SYS_REFCURSOR
    );
END PKG_ECOACCESS_STATS;
~');

    definir(2, 'PKG_ECOACCESS_STATS', 'PACKAGE BODY', q'~PACKAGE BODY PKG_ECOACCESS_STATS AS
-- ECOACCESS_V022_SOURCE_SHA256=4212374128b5608b8d12aa5d35c5b10b5a02fb3cc1b839ff17713332867b5457
    PROCEDURE consultar_resumen(
        p_desde_utc IN TIMESTAMP,
        p_hasta_utc IN TIMESTAMP,
        p_id_punto IN NUMBER,
        p_categoria IN NUMBER,
        p_resultado OUT SYS_REFCURSOR
    ) IS
    BEGIN
        IF p_desde_utc IS NULL OR p_hasta_utc IS NULL
           OR p_hasta_utc <= p_desde_utc
           OR p_hasta_utc - p_desde_utc > NUMTODSINTERVAL(400, 'DAY') THEN
            RAISE_APPLICATION_ERROR(-20801,
                'El periodo debe ser positivo y no superar 400 dias.');
        END IF;
        IF p_id_punto IS NOT NULL
           AND (p_id_punto <= 0 OR p_id_punto <> TRUNC(p_id_punto)) THEN
            RAISE_APPLICATION_ERROR(-20802,'Estacion no valida.');
        END IF;
        IF p_categoria IS NOT NULL AND p_categoria NOT IN (1,2,3,4) THEN
            RAISE_APPLICATION_ERROR(-20803,'Categoria no valida.');
        END IF;
        OPEN p_resultado FOR
            SELECT COUNT(*) AS depositos_periodo,
                COUNT(d.id_material_verificado) AS depositos_verificados,
                COUNT(CASE WHEN d.sin_sensor=1 THEN 1 END) AS depositos_sin_sensor,
                COUNT(DISTINCT i.id_usuario) AS ciudadanos_distintos,
                NVL(SUM(CASE WHEN d.id_material_verificado IS NOT NULL
                    THEN d.cantidad_verificada ELSE 0 END),0) AS unidades_verificadas,
                COUNT(CASE WHEN d.id_material_verificado IS NOT NULL
                    AND d.cantidad_verificada IS NULL THEN 1 END) AS verificados_sin_conteo,
                COUNT(d.peso_medido_kg) AS con_peso_medido,
                NVL(SUM(d.peso_medido_kg),0) AS kg_medidos
            FROM deposito d
            JOIN orden_apertura o ON o.id_orden=d.id_orden
            JOIN interaccion i ON i.id_interaccion=o.id_interaccion
            JOIN contenedor c ON c.id_contenedor=o.id_contenedor
            JOIN material m ON m.id_material=
                NVL(d.id_material_verificado,d.id_material_declarado)
            WHERE d.fecha_deteccion_utc >= p_desde_utc
              AND d.fecha_deteccion_utc < p_hasta_utc
              AND (p_id_punto IS NULL OR c.id_punto_instalacion=p_id_punto)
              AND (p_categoria IS NULL OR m.categoria=p_categoria);
    END consultar_resumen;
END PKG_ECOACCESS_STATS;
~');

    definir(3, 'FN_RESOLVER_CANECA_DISPONIBLE', 'FUNCTION', q'~FUNCTION FN_RESOLVER_CANECA_DISPONIBLE(
    p_id_punto IN NUMBER,
    p_id_material IN NUMBER
) RETURN NUMBER AUTHID DEFINER IS
-- ECOACCESS_V022_SOURCE_SHA256=9716e7c4540c765c31a10c33ff0ac3562b8f30f918147fb12351a1996a0598a5
    v_activo punto_instalacion.activo%TYPE;
    v_categoria material.categoria%TYPE;
    v_habilitado material.habilitado%TYPE;
    v_cantidad PLS_INTEGER;
    v_id contenedor.id_contenedor%TYPE;
    v_estado contenedor.estado%TYPE;
BEGIN
    IF p_id_punto IS NULL OR p_id_material IS NULL
       OR p_id_punto <= 0 OR p_id_material <= 0
       OR p_id_punto <> TRUNC(p_id_punto)
       OR p_id_material <> TRUNC(p_id_material) THEN
        RAISE_APPLICATION_ERROR(-20810,'Identificadores no validos.');
    END IF;
    BEGIN
        SELECT activo INTO v_activo FROM punto_instalacion
        WHERE id_punto_instalacion=p_id_punto;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20818,'Estacion inexistente.');
    END;
    IF v_activo <> 1 THEN
        RAISE_APPLICATION_ERROR(-20819,'Estacion inactiva.');
    END IF;
    BEGIN
        SELECT categoria,habilitado INTO v_categoria,v_habilitado
        FROM material WHERE id_material=p_id_material;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20811,'Material inexistente.');
    END;
    IF v_habilitado <> 1 THEN
        RAISE_APPLICATION_ERROR(-20812,'Material deshabilitado.');
    END IF;
    IF v_categoria NOT IN (1,2,3) THEN
        RAISE_APPLICATION_ERROR(-20813,'Material de ruta especial.');
    END IF;
    SELECT COUNT(*),MIN(c.id_contenedor),MIN(c.estado)
    INTO v_cantidad,v_id,v_estado
    FROM configuracion_caneca k
    JOIN contenedor c ON c.id_contenedor=k.id_contenedor
    WHERE c.id_punto_instalacion=p_id_punto
      AND c.estado<>3 AND k.categoria=v_categoria;
    IF v_cantidad=0 THEN
        RAISE_APPLICATION_ERROR(-20814,'Sin caneca configurada.');
    ELSIF v_cantidad>1 THEN
        RAISE_APPLICATION_ERROR(-20815,'Configuracion ambigua.');
    END IF;
    IF v_estado<>1 THEN
        RAISE_APPLICATION_ERROR(-20816,'Caneca fuera de servicio.');
    END IF;
    SELECT COUNT(*) INTO v_cantidad FROM contenedor_material
    WHERE id_contenedor=v_id AND id_material=p_id_material;
    IF v_cantidad<>1 THEN
        RAISE_APPLICATION_ERROR(-20817,'La caneca no admite el material.');
    END IF;
    RETURN v_id;
END FN_RESOLVER_CANECA_DISPONIBLE;
~');

    definir(4, 'PR_CONSULTAR_ESTADISTICAS', 'PROCEDURE', q'~PROCEDURE PR_CONSULTAR_ESTADISTICAS(
    p_desde_utc IN TIMESTAMP,
    p_hasta_utc IN TIMESTAMP,
    p_id_punto IN NUMBER,
    p_categoria IN NUMBER,
    p_resultado OUT SYS_REFCURSOR
) AUTHID DEFINER AS
-- ECOACCESS_V022_SOURCE_SHA256=313498c770c1ad51cc81386b24fbd416fb20ea3f01935d63ea7878b548d9df16
BEGIN
    PKG_ECOACCESS_STATS.consultar_resumen(
        p_desde_utc,p_hasta_utc,p_id_punto,p_categoria,p_resultado);
END PR_CONSULTAR_ESTADISTICAS;
~');

    -- Primero revisar TODAS las colisiones, antes del primer DDL.
    FOR i IN 1..plan.COUNT LOOP
        revisar_colision(i);
    END LOOP;
    FOR i IN 1..plan.COUNT LOOP
        IF plan(i).existe THEN
            DBMS_OUTPUT.PUT_LINE('V022 conserva fuente identica: '
                || plan(i).tipo || ' ' || plan(i).nombre);
        ELSE
            -- CREATE sin OR REPLACE evita sobreescribir incluso si otra sesion
            -- crea un objeto despues de la prevalidacion de colisiones.
            EXECUTE IMMEDIATE 'CREATE ' || plan(i).fuente;
            DBMS_OUTPUT.PUT_LINE('V022 crea: ' || plan(i).tipo || ' ' || plan(i).nombre);
        END IF;
    END LOOP;

    FOR i IN 1..plan.COUNT LOOP
        nombre_actual := plan(i).nombre;
        tipo_actual := plan(i).tipo;
        SELECT COUNT(*) INTO n FROM user_objects
        WHERE object_name=nombre_actual AND object_type=tipo_actual
          AND status='VALID';
        IF n <> 1 THEN
            RAISE_APPLICATION_ERROR(-20887,
                'Objeto V022 no valido: ' || plan(i).tipo || ' ' || plan(i).nombre
                || '. Consultar USER_ERRORS; no se registra V022.');
        END IF;
        SELECT COUNT(*) INTO n FROM user_errors
        WHERE name=nombre_actual AND type=tipo_actual AND attribute='ERROR';
        IF n <> 0 THEN
            RAISE_APPLICATION_ERROR(-20887,
                'Errores de compilacion V022 en ' || plan(i).nombre || '.');
        END IF;
    END LOOP;
    SELECT COUNT(*) INTO tablas_despues FROM user_tables;
    IF tablas_despues <> tablas_antes THEN
        RAISE_APPLICATION_ERROR(-20888,
            'Cambio concurrente en numero de tablas; revisar antes de registrar V022.');
    END IF;
    exigir_trigger;
    DBMS_OUTPUT.PUT_LINE('V022 completa. Tablas conservadas: ' || tablas_despues
        || '. Sin cambios de filas de negocio; TR_ECOP_UNIDADES preservado.');
END;
/
WHENEVER SQLERROR CONTINUE
