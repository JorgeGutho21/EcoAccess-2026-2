-- EcoAccess · views/ea_r_base_puntos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_BASE_PUNTOS" ("DIA", "ID_CONTENEDOR", "CANECA", "ID_PUNTO_INSTALACION", "UBICACION", "ES_ENSAYO", "ACREDITADOS", "REVERSOS", "NETOS") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor,c.nombre caneca,p.id_punto_instalacion,p.nombre ubicacion,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo,
       SUM(CASE WHEN me.puntos>0 THEN me.puntos ELSE 0 END) acreditados,
       SUM(CASE WHEN me.puntos<0 THEN me.puntos ELSE 0 END) reversos,
       SUM(me.puntos) netos
FROM movimiento_ecopuntos me
JOIN operacion_ecopuntos op ON op.id_operacion=me.id_operacion
JOIN deposito d ON d.id_deposito=me.id_deposito
JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN material m ON m.id_material=d.id_material_declarado
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
GROUP BY TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;
