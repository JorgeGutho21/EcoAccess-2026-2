-- EcoAccess · tables/contenedor_material.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE TABLE "CONTENEDOR_MATERIAL"
   (	"ID_CONTENEDOR" NUMBER(10,0) NOT NULL ENABLE,
	"ID_MATERIAL" NUMBER(10,0) NOT NULL ENABLE,
	 CONSTRAINT "PK_CONTENEDOR_MATERIAL" PRIMARY KEY ("ID_CONTENEDOR", "ID_MATERIAL")
  USING INDEX  ENABLE
   )  DEFAULT COLLATION "USING_NLS_COMP" ;
