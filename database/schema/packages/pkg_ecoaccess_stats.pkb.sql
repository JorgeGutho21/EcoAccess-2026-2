-- EcoAccess · packages/pkg_ecoaccess_stats.pkb.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE EDITIONABLE PACKAGE BODY "PKG_ECOACCESS_STATS" AS
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
/
