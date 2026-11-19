-- EcoAccess · views/ea_r_puntos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_PUNTOS" ("DIA", "ID_CONTENEDOR", "CANECA", "ID_PUNTO_INSTALACION", "UBICACION", "ACREDITADOS", "REVERSOS", "NETOS") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT dia,id_contenedor,caneca,id_punto_instalacion,ubicacion,acreditados,reversos,netos
FROM ea_r_base_puntos WHERE es_ensayo=0 WITH READ ONLY;
