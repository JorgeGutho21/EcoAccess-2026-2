-- EcoAccess · views/vw_bi_depositos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_BI_DEPOSITOS" ("ID_DEPOSITO", "CIUDADANO", "DIA", "FECHA_HORA", "ESTACION", "CANECA", "MATERIAL", "MATERIAL_NOMBRE", "ESTADO", "EVIDENCIA", "UNIDADES", "PESO_ESTIMADO_KG", "PESO_MEDIDO_KG", "CANAL", "ORIGEN_DATO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT id_deposito,
       CASE WHEN id_titular IS NULL THEN 'SIN CUENTA' ELSE 'C-' || LPAD(id_titular, 4, '0') END AS ciudadano,
       TRUNC(fecha_colombia) AS dia, fecha_colombia AS fecha_hora, estacion, color AS caneca,
       NVL(material_verificado, material_declarado) AS material,
       (SELECT m.nombre FROM material m WHERE m.codigo = NVL(dd.material_verificado, dd.material_declarado)) AS material_nombre,
       estado_texto AS estado,
       DECODE(evidencia_fisica, 'DECLARADO SIN SENSOR', 'SIN SENSOR', 'SENSOR') AS evidencia,
       NVL(cantidad_verificada, cantidad_declarada) AS unidades,
       peso_estimado_kg, peso_medido_kg,
       DECODE(canal, 1,'TELEGRAM',2,'MANUAL',3,'SIMULADOR',4,'TELEGRAM SIMULADO',5,'ASISTIDO') AS canal,
       CASE WHEN es_sintetico = 1 THEN 'DATOS DE ENSAYO' ELSE 'REAL' END AS origen_dato
FROM   vw_deposito_detalle dd
WITH READ ONLY;
