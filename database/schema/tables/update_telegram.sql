-- EcoAccess · tables/update_telegram.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE TABLE "UPDATE_TELEGRAM"
   (	"UPDATE_ID" NUMBER(19,0) NOT NULL ENABLE,
	"RECIBIDO_UTC" TIMESTAMP (6) NOT NULL ENABLE,
	 CONSTRAINT "CK_UPDATE_ID" CHECK (update_id >= 0) ENABLE,
	 CONSTRAINT "PK_UPDATE_TELEGRAM" PRIMARY KEY ("UPDATE_ID")
  USING INDEX  ENABLE
   )  DEFAULT COLLATION "USING_NLS_COMP" ;
