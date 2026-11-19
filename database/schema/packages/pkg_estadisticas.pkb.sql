-- EcoAccess · packages/pkg_estadisticas.pkb.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "PKG_ESTADISTICAS" AS
    -- ECOACCESS_V027
    FUNCTION a_utc(p_fecha_col IN DATE) RETURN TIMESTAMP IS
    BEGIN
        RETURN SYS_EXTRACT_UTC(FROM_TZ(CAST(p_fecha_col AS TIMESTAMP), 'America/Bogota'));
    END a_utc;

    PROCEDURE validar(p_desde_col IN DATE, p_hasta_col IN DATE) IS
    BEGIN
        IF p_desde_col IS NULL OR p_hasta_col IS NULL OR p_hasta_col <= p_desde_col OR p_hasta_col - p_desde_col > 400 THEN
            RAISE_APPLICATION_ERROR(-20801, 'El periodo debe ser positivo y no superar 400 días.');
        END IF;
    END validar;

    PROCEDURE consultar_resumen(p_desde_col IN DATE, p_hasta_col IN DATE, p_id_punto IN NUMBER, p_categoria IN NUMBER,
                                p_resultado OUT SYS_REFCURSOR) IS
        v_desde TIMESTAMP;
        v_hasta TIMESTAMP;
    BEGIN
        validar(p_desde_col, p_hasta_col);
        IF p_id_punto IS NOT NULL AND p_id_punto <= 0 THEN
            RAISE_APPLICATION_ERROR(-20802, 'Estación no válida.');
        END IF;
        IF p_categoria IS NOT NULL AND p_categoria NOT IN (1, 2, 3, 4) THEN
            RAISE_APPLICATION_ERROR(-20803, 'Categoría no válida.');
        END IF;
        v_desde := a_utc(p_desde_col);
        v_hasta := a_utc(p_hasta_col);
        OPEN p_resultado FOR
            SELECT COUNT(*) AS depositos,
                   COUNT(d.id_material_verificado) AS verificados,
                   COUNT(CASE WHEN d.id_material_verificado IS NULL THEN 1 END) AS pendientes_o_rechazados,
                   COUNT(CASE WHEN d.sin_sensor = 1 THEN 1 END) AS sin_sensor,
                   COUNT(DISTINCT i.id_usuario) AS ciudadanos,
                   NVL(SUM(CASE WHEN d.id_material_verificado IS NOT NULL THEN d.cantidad_verificada END), 0) AS unidades_verificadas,
                   NVL(SUM(CASE WHEN d.id_material_verificado IS NOT NULL THEN d.peso_estimado_kg END), 0) AS kg_estimados_verificados,
                   COUNT(d.peso_medido_kg) AS con_peso_medido,
                   NVL(SUM(d.peso_medido_kg), 0) AS kg_medidos
            FROM   deposito d
            JOIN   orden_apertura o ON o.id_orden = d.id_orden
            JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
            JOIN   contenedor c     ON c.id_contenedor = o.id_contenedor
            LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor
            WHERE  d.fecha_deteccion_utc >= v_desde AND d.fecha_deteccion_utc < v_hasta
            AND    (p_id_punto IS NULL OR c.id_punto_instalacion = p_id_punto)
            AND    (p_categoria IS NULL OR k.categoria = p_categoria);
    END consultar_resumen;

    FUNCTION ranking_verificados(p_desde_col IN DATE, p_hasta_col IN DATE, p_top IN PLS_INTEGER DEFAULT 10) RETURN SYS_REFCURSOR IS
        v_cursor SYS_REFCURSOR;
    BEGIN
        validar(p_desde_col, p_hasta_col);
        OPEN v_cursor FOR
            SELECT * FROM (
                SELECT i.id_usuario, u.nombre,
                       COUNT(*) AS depositos_verificados,
                       SUM(d.cantidad_verificada) AS unidades,
                       ROUND(SUM(d.peso_estimado_kg), 3) AS kg_estimados,
                       ROUND(SUM(d.peso_medido_kg), 3) AS kg_medidos,
                       DENSE_RANK() OVER (ORDER BY COUNT(*) DESC, SUM(d.cantidad_verificada) DESC NULLS LAST) AS puesto
                FROM   deposito d
                JOIN   orden_apertura o ON o.id_orden = d.id_orden
                JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
                JOIN   usuario u        ON u.id_usuario = i.id_usuario
                WHERE  d.id_material_verificado IS NOT NULL
                AND    d.fecha_deteccion_utc >= a_utc(p_desde_col) AND d.fecha_deteccion_utc < a_utc(p_hasta_col)
                GROUP  BY i.id_usuario, u.nombre)
            WHERE  puesto <= p_top
            ORDER  BY puesto, id_usuario;
        RETURN v_cursor;
    END ranking_verificados;
END pkg_estadisticas;
/
