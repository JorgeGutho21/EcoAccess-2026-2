-- EcoAccess · views/vw_bi_consultas_vision.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_BI_CONSULTAS_VISION" ("ID_CONSULTA", "CIUDADANO", "DIA", "CANAL", "MODO", "RESULTADO", "MATERIAL", "CANECA_INDICADA", "UNIDADES_ESTIMADAS", "PESO_ESTIMADO_KG", "CONTEXTO_QR", "LATENCIA_MS", "ORIGEN_DATO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT cv.id_consulta,
       CASE WHEN cv.id_usuario IS NULL THEN 'SIN CUENTA' ELSE 'C-' || LPAD(cv.id_usuario, 4, '0') END AS ciudadano,
       TRUNC(CAST(FROM_TZ(cv.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       DECODE(cv.canal, 1, 'TELEGRAM', 2, 'ESCRITORIO', 3, 'EVALUACION') AS canal,
       DECODE(cv.modo, 1, 'SOFTWARE', 2, 'CON DISPOSITIVO') AS modo,
       CASE WHEN cv.resultado IN (1, 10) THEN 'IDENTIFICADO'
            WHEN cv.resultado IN (2, 3, 4, 6) THEN 'PIDIÃ“ OTRA FOTO'
            WHEN cv.resultado = 5 THEN 'NO ES RESIDUO'
            ELSE 'ERROR' END AS resultado,
       m.codigo AS material,
       DECODE(m.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 4, 'RUTA ESPECIAL') AS caneca_indicada,
       CAST(NULL AS NUMBER) AS unidades_estimadas,
       CAST(NULL AS NUMBER) AS peso_estimado_kg,
       CASE WHEN cv.contexto IS NULL OR cv.id_contenedor IS NULL THEN 'SIN QR' ELSE 'CON QR' END AS contexto_qr,
       cv.latencia_ms,
       CASE WHEN cv.canal = 3 THEN 'DATOS DE ENSAYO' ELSE 'REAL' END AS origen_dato
FROM   consulta_vision cv
LEFT JOIN material m ON m.id_material = cv.id_material
WITH READ ONLY;
