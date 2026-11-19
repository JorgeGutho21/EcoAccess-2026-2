-- EcoAccess · views/ea_r_base_depositos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_BASE_DEPOSITOS" ("DIA", "ID_CONTENEDOR", "CANECA", "ID_PUNTO_INSTALACION", "UBICACION", "ES_ENSAYO", "MATERIAL", "DEPOSITOS", "VERIFICADOS", "CIUDADANOS_DEL_DIA", "CON_PESO_MEDIDO", "KG_MEDIDOS", "UNIDADES_VERIFICADAS", "VERIFICADOS_SIN_CONTEO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor, c.nombre caneca, p.id_punto_instalacion, p.nombre ubicacion,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo, m.codigo material, COUNT(*) depositos,
       COUNT(d.id_material_verificado) verificados,
       COUNT(DISTINCT i.id_usuario) ciudadanos_del_dia,
       COUNT(d.peso_medido_kg) con_peso_medido, SUM(d.peso_medido_kg) kg_medidos,
       SUM(CASE WHEN d.id_material_verificado IS NOT NULL THEN NVL(d.cantidad_verificada,0) ELSE 0 END) unidades_verificadas,
       COUNT(CASE WHEN d.id_material_verificado IS NOT NULL AND d.cantidad_verificada IS NULL THEN 1 END) verificados_sin_conteo
FROM deposito d
JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
JOIN material m ON m.id_material=d.id_material_declarado
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,m.codigo,
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;
