-- EcoAccess · views/vw_caneca_estacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_CANECA_ESTACION" ("ID_PUNTO_INSTALACION", "ESTACION", "CODIGO_QR_ESTACION", "ID_CONTENEDOR", "CANECA", "CATEGORIA", "COLOR", "TIPO_RESIDUO", "ESTADO", "ESTADO_TEXTO", "QR_LEGADO_CANECA", "IDENTIFICADOR_DISPOSITIVO", "ES_SINTETICO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT p.id_punto_instalacion, p.nombre AS estacion, c.identificador_qr AS codigo_qr_estacion,
       c.id_contenedor, c.nombre AS caneca, k.categoria,
       DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA') AS color,
       DECODE(k.categoria, 1, 'Aprovechables', 2, 'OrgÃ¡nicos aprovechables', 3, 'No aprovechables') AS tipo_residuo,
       c.estado, DECODE(c.estado, 1, 'EN SERVICIO', 2, 'FUERA DE SERVICIO', 3, 'DADA DE BAJA') AS estado_texto,
       c.identificador_qr AS qr_legado_caneca, k.identificador_dispositivo,
       CASE WHEN UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' THEN 1 ELSE 0 END AS es_sintetico
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor
WITH READ ONLY;
