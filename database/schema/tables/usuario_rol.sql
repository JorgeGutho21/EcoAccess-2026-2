-- EcoAccess · tables/usuario_rol.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE TABLE "USUARIO_ROL"
   (	"ID_USUARIO" NUMBER(10,0) NOT NULL ENABLE,
	"ID_ROL" NUMBER(10,0) NOT NULL ENABLE,
	"FECHA_ASIGNACION_UTC" TIMESTAMP (6) DEFAULT SYS_EXTRACT_UTC(SYSTIMESTAMP) NOT NULL ENABLE,
	 CONSTRAINT "PK_USUARIO_ROL" PRIMARY KEY ("ID_USUARIO", "ID_ROL")
  USING INDEX  ENABLE
   )  DEFAULT COLLATION "USING_NLS_COMP" ;
