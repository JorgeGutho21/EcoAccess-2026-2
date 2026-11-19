-- EcoAccess · views/vista_lotes_simulacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "VISTA_LOTES_SIMULACION" ("ID_LOTE", "DESCRIPCION", "SEMILLA", "FECHA_CREACION_UTC", "ESTADO", "CIUDADANOS", "REGISTROS", "USUARIOS_CREADOS", "INTERACCIONES_CREADAS", "DEPOSITOS_CREADOS") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT l.id_lote, l.descripcion, l.semilla, l.fecha_creacion_utc, l.estado, l.ciudadanos, l.registros,
       (SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote = l.id_lote AND r.tabla = 'USUARIO')     AS usuarios_creados,
       (SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote = l.id_lote AND r.tabla = 'INTERACCION') AS interacciones_creadas,
       (SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote = l.id_lote AND r.tabla = 'DEPOSITO')    AS depositos_creados
FROM   lote_simulacion l
WITH READ ONLY;
