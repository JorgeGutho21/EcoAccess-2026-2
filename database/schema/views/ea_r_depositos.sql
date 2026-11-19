-- EcoAccess · views/ea_r_depositos.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE OR REPLACE FORCE EDITIONABLE VIEW "EA_R_DEPOSITOS" ("DIA", "ID_CONTENEDOR", "CANECA", "ID_PUNTO_INSTALACION", "UBICACION", "MATERIAL", "DEPOSITOS", "VERIFICADOS", "CIUDADANOS_DEL_DIA", "CON_PESO_MEDIDO", "KG_MEDIDOS", "UNIDADES_VERIFICADAS", "VERIFICADOS_SIN_CONTEO") DEFAULT COLLATION "USING_NLS_COMP"  AS
  SELECT dia,id_contenedor,caneca,id_punto_instalacion,ubicacion,material,depositos,verificados,
       ciudadanos_del_dia,con_peso_medido,kg_medidos,unidades_verificadas,verificados_sin_conteo
FROM ea_r_base_depositos WHERE es_ensayo=0 WITH READ ONLY;
