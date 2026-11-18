-- =====================================================================
-- EcoAccess P3 — migración V028: códigos reales de resultado en VW_BI_CONSULTAS_VISION
-- Archivo: database/06_migraciones/V028__vista_bi_consultas_vision.sql
--
-- V027 tomó los códigos de resultado del esquema académico (1–4). La base oficial guarda ResultadoClasificacion
-- de C# (1–10): 1 Identificado y 10 ObjetoReconocido; 2 Incierta, 3 ImagenBorrosa, 4 VariosObjetos y
-- 6 ObjetoDesconocido piden otra foto; 5 SinResiduo; 7–9 proveedor no disponible, cuota o autorización.
-- Con V027 sola, 18 consultas con código 10 quedaban sin rótulo. Solo reemplaza la vista; no toca filas.
-- Reversión: volver a crear la vista con la definición de V027.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
CREATE OR REPLACE VIEW vw_bi_consultas_vision AS
SELECT cv.id_consulta,
       CASE WHEN cv.id_usuario IS NULL THEN 'SIN CUENTA' ELSE 'C-' || LPAD(cv.id_usuario, 4, '0') END AS ciudadano,
       TRUNC(CAST(FROM_TZ(cv.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       DECODE(cv.canal, 1, 'TELEGRAM', 2, 'ESCRITORIO', 3, 'EVALUACION') AS canal,
       DECODE(cv.modo, 1, 'SOFTWARE', 2, 'CON DISPOSITIVO') AS modo,
       CASE WHEN cv.resultado IN (1, 10) THEN 'IDENTIFICADO'
            WHEN cv.resultado IN (2, 3, 4, 6) THEN 'PIDIÓ OTRA FOTO'
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
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_objects WHERE object_name = 'VW_BI_CONSULTAS_VISION' AND status = 'VALID';
    IF n <> 1 THEN
        RAISE_APPLICATION_ERROR(-20894, 'V028: la vista VW_BI_CONSULTAS_VISION no quedó válida.');
    END IF;
    DBMS_OUTPUT.PUT_LINE('V028 completa.');
END;
/
WHENEVER SQLERROR CONTINUE
