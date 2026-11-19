-- EcoAccess · views/vw_bi_ecopuntos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VW_BI_ECOPUNTOS" ("ID_MOVIMIENTO", "CIUDADANO", "DIA", "TIPO", "PUNTOS", "MATERIAL", "TARIFA") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT m.id_movimiento,
       'C-' || LPAD(m.id_usuario, 4, '0') AS ciudadano,
       TRUNC(CAST(FROM_TZ(op.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       DECODE(m.tipo, 1, 'CRÃ‰DITO', 2, 'REVERSO') AS tipo,
       m.puntos, mt.codigo AS material,
       'TARIFA APROBADA' AS tarifa
FROM   movimiento_ecopuntos m
JOIN   operacion_ecopuntos op ON op.id_operacion = m.id_operacion
JOIN   regla_ecopuntos r ON r.id_regla = m.id_regla
JOIN   material mt       ON mt.id_material = r.id_material
WITH READ ONLY;
