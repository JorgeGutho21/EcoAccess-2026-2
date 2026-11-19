-- EcoAccess · views/ea_r_materiales.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_MATERIALES" ("MATERIAL", "NOMBRE", "CATEGORIA", "HABILITADO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT m.codigo material, m.nombre,
       CASE m.categoria WHEN 1 THEN 'Aprovechable' WHEN 2 THEN TO_CHAR(UNISTR('Org\00E1nico aprovechable')) WHEN 3 THEN 'No aprovechable' ELSE 'Peligroso o especial' END categoria,
       m.habilitado
FROM material m
WITH READ ONLY;
