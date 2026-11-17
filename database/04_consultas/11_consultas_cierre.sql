-- =====================================================================
-- EcoAccess P3 — consultas de demostración del cierre (fases 2, 3, 5, 7 y 8)
-- Solo SELECT. Ejecutables en SQL Developer contra ECOACCESS_P3_APP.
-- Por defecto se excluye la SIMULACIÓN (es_simulacion = 0); el «modo experimental»
-- es la misma consulta con es_simulacion = 1 o filtrando por id_lote.
-- =====================================================================

-- 1. Estaciones y sus tres canecas (V021/V024). El QR oficial es el de la caneca BLANCA del piloto.
SELECT estacion, caneca, color, estado, identificador_dispositivo, ubicacion_en_estacion, identificador_qr
FROM   vista_canecas_estacion
ORDER  BY estacion, categoria;

-- 2. Depósitos REALES por estado de registro (DECLARADA_SIN_SENSOR / DETECTADA_POR_SENSOR / VERIFICADA).
SELECT estado_registro, color_caneca, COUNT(*) AS depositos,
       SUM(peso_medido_kg) AS kg_medidos, SUM(peso_estimado_kg) AS kg_estimados
FROM   vista_depositos_estado
WHERE  es_simulacion = 0 AND es_ensayo = 0
GROUP  BY estado_registro, color_caneca
ORDER  BY estado_registro, color_caneca;

-- 3. Modo experimental: el mismo resumen solo para un lote de SIMULACIÓN.
SELECT estado_registro, color_caneca, COUNT(*) AS depositos, SUM(peso_estimado_kg) AS kg_estimados
FROM   vista_depositos_estado
WHERE  id_lote = 'SIM-2026-11-CIERRE'
GROUP  BY estado_registro, color_caneca
ORDER  BY estado_registro, color_caneca;

-- 4. Lotes de simulación registrados y lo que creó cada uno.
SELECT * FROM vista_lotes_simulacion ORDER BY fecha_creacion_utc;

-- 5. Depósitos por día (hora de Colombia) y material, modo experimental, últimos 30 días.
SELECT TRUNC(CAST(FROM_TZ(fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       material, COUNT(*) AS depositos
FROM   vista_depositos_estado
WHERE  id_lote = 'SIM-2026-11-CIERRE'
GROUP  BY TRUNC(CAST(FROM_TZ(fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)), material
ORDER  BY dia, material;

-- 6. Ningún depósito sintético tiene peso medido (debe dar 0).
SELECT COUNT(*) AS sinteticos_con_peso_medido
FROM   vista_depositos_estado
WHERE  es_simulacion = 1 AND peso_medido_kg IS NOT NULL;

-- 7. Migraciones aplicadas con su huella (auditoría).
SELECT version, archivo, SUBSTR(huella_sha256, 1, 16) AS huella, aplicada_utc
FROM   eco_migracion_aplicada
ORDER  BY aplicada_utc;

-- 8. Objetos inválidos del esquema (debe dar 0).
SELECT object_type, object_name FROM user_objects WHERE status <> 'VALID';
