-- EcoAccess · views/vw_deposito_detalle.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_DEPOSITO_DETALLE" ("ID_DEPOSITO", "ID_INTERACCION", "ID_TITULAR", "TITULAR", "ID_CONTENEDOR", "CANECA", "COLOR", "ESTACION", "MATERIAL_DECLARADO", "MATERIAL_VERIFICADO", "CANTIDAD_DECLARADA", "CANTIDAD_VERIFICADA", "EVIDENCIA_FISICA", "ESTADO_VERIFICACION", "ESTADO_TEXTO", "ID_VERIFICADOR", "VERIFICADOR", "PESO_ESTIMADO_KG", "PESO_ESTIMADO_G", "PESO_MEDIDO_KG", "PESO_MEDIDO_G", "FECHA_DETECCION_UTC", "FECHA_COLOMBIA", "CANAL", "ES_SINTETICO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT d.id_deposito, i.id_interaccion, i.id_usuario AS id_titular, u.nombre AS titular,
       c.id_contenedor, c.nombre AS caneca, DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA') AS color,
       p.nombre AS estacion, md.codigo AS material_declarado, mv.codigo AS material_verificado,
       i.cantidad_declarada, d.cantidad_verificada,
       DECODE(d.sin_sensor, 1, 'DECLARADO SIN SENSOR', 'DETECTADO POR SENSOR') AS evidencia_fisica,
       pkg_deposito.estado(d.id_deposito) AS estado_verificacion,
       DECODE(pkg_deposito.estado(d.id_deposito), 0, 'PENDIENTE', 1, 'VERIFICADO', 2, 'RECHAZADO') AS estado_texto,
       d.id_verificador, uv.nombre AS verificador,
       d.peso_estimado_kg, ROUND(d.peso_estimado_kg * 1000, 1) AS peso_estimado_g,
       d.peso_medido_kg, ROUND(d.peso_medido_kg * 1000, 1) AS peso_medido_g,
       d.fecha_deteccion_utc,
       CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS fecha_colombia,
       i.canal,
       CASE WHEN i.canal IN (3, 4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%'
                 OR REGEXP_LIKE(md.codigo, '^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END AS es_sintetico
FROM   deposito d
JOIN   orden_apertura o    ON o.id_orden = d.id_orden
JOIN   interaccion i       ON i.id_interaccion = o.id_interaccion
JOIN   contenedor c        ON c.id_contenedor = o.id_contenedor
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
JOIN   material md         ON md.id_material = d.id_material_declarado
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor
LEFT JOIN material mv      ON mv.id_material = d.id_material_verificado
LEFT JOIN usuario u        ON u.id_usuario = i.id_usuario
LEFT JOIN usuario uv       ON uv.id_usuario = d.id_verificador
WITH READ ONLY;
