-- EcoAccess · views/vw_recorrido.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_RECORRIDO" ("ID_INTERACCION", "ID_USUARIO", "ID_PUNTO_INSTALACION", "CANAL", "ESTADO_SESION", "ID_MATERIAL_SUGERIDO", "ID_ORDEN", "ESTADO_ORDEN", "APERTURAS_CONFIRMADAS", "ID_DEPOSITO", "SIN_SENSOR", "ESTADO_VERIFICACION", "INICIO_COLOMBIA", "ES_SINTETICO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT i.id_interaccion, i.id_usuario, c.id_punto_instalacion, i.canal,
       DECODE(i.estado, 1, 'INICIADA', 2, 'AUTORIZADA', 3, 'CON DEPOSITO', 4, 'SIN DEPOSITO', 5, 'RECHAZADA') AS estado_sesion,
       i.id_material_sugerido, o.id_orden,
       DECODE(o.estado, 1,'EMITIDA',2,'ENVIADA',3,'ACUSADA',4,'ABIERTA',5,'DETECTADA',6,'CERRADA',7,'VENCIDA',8,'FALLIDA') AS estado_orden,
       (SELECT COUNT(*) FROM evento_dispositivo e WHERE e.id_orden = o.id_orden AND e.tipo = 2) AS aperturas_confirmadas,
       d.id_deposito, d.sin_sensor,
       CASE WHEN d.id_deposito IS NULL THEN NULL ELSE pkg_deposito.estado(d.id_deposito) END AS estado_verificacion,
       CAST(FROM_TZ(i.fecha_inicio_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS inicio_colombia,
       CASE WHEN i.canal IN (3, 4) THEN 1 ELSE 0 END AS es_sintetico
FROM   interaccion i
JOIN   contenedor c ON c.id_contenedor = i.id_contenedor
LEFT JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion
LEFT JOIN deposito d       ON d.id_orden = o.id_orden
WITH READ ONLY;
