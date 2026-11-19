-- EcoAccess · views/vista_depositos_estado.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VISTA_DEPOSITOS_ESTADO" ("ID_DEPOSITO", "FECHA_DETECCION_UTC", "MATERIAL", "CATEGORIA_MATERIAL", "ID_CONTENEDOR", "CANECA", "COLOR_CANECA", "ID_PUNTO_INSTALACION", "ESTACION", "CANAL", "ESTADO_REGISTRO", "PESO_MEDIDO_KG", "PESO_ESTIMADO_KG", "CANTIDAD_DECLARADA", "CANTIDAD_VERIFICADA", "ES_SIMULACION", "ID_LOTE", "ES_ENSAYO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT d.id_deposito,
       d.fecha_deteccion_utc,
       m.codigo                                   AS material,
       m.categoria                                AS categoria_material,
       c.id_contenedor,
       c.nombre                                   AS caneca,
       DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 'SIN CONFIGURAR') AS color_caneca,
       p.id_punto_instalacion,
       p.nombre                                   AS estacion,
       i.canal,
       CASE WHEN d.id_material_verificado IS NOT NULL THEN 'VERIFICADA'
            WHEN d.sin_sensor = 1 THEN 'DECLARADA_SIN_SENSOR'
            ELSE 'DETECTADA_POR_SENSOR' END       AS estado_registro,
       d.peso_medido_kg,
       d.peso_estimado_kg,
       i.cantidad_declarada,
       d.cantidad_verificada,
       CASE WHEN rs.id_lote IS NOT NULL THEN 1 ELSE 0 END AS es_simulacion,
       rs.id_lote,
       CASE WHEN rs.id_lote IS NOT NULL OR i.canal IN (3, 4) OR UPPER(c.nombre) LIKE '%FICTIC%'
                 OR UPPER(p.nombre) LIKE '%SIMULACI%' THEN 1 ELSE 0 END AS es_ensayo
FROM   deposito d
JOIN   orden_apertura o     ON o.id_orden = d.id_orden
JOIN   interaccion i        ON i.id_interaccion = o.id_interaccion
JOIN   material m           ON m.id_material = d.id_material_declarado
JOIN   contenedor c         ON c.id_contenedor = o.id_contenedor
JOIN   punto_instalacion p  ON p.id_punto_instalacion = c.id_punto_instalacion
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor
LEFT JOIN registro_simulacion rs ON rs.tabla = 'DEPOSITO' AND rs.id_registro = d.id_deposito
WITH READ ONLY;
