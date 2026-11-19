-- EcoAccess · tables/consentimiento_vision_externa.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE TABLE "CONSENTIMIENTO_VISION_EXTERNA"
   (	"TELEGRAM_USER_ID" NUMBER(20,0) NOT NULL ENABLE,
	"VERSION_AVISO" VARCHAR2(30 CHAR) COLLATE "USING_NLS_COMP" NOT NULL ENABLE,
	"FECHA_OTORGADO_UTC" TIMESTAMP (6) NOT NULL ENABLE,
	"FECHA_REVOCADO_UTC" TIMESTAMP (6),
	 CONSTRAINT "CK_CONSENTIMIENTO_VISION_FECHAS" CHECK (fecha_revocado_utc IS NULL OR fecha_revocado_utc >= fecha_otorgado_utc) ENABLE,
	 CONSTRAINT "PK_CONSENTIMIENTO_VISION" PRIMARY KEY ("TELEGRAM_USER_ID")
  USING INDEX  ENABLE
   )  DEFAULT COLLATION "USING_NLS_COMP" ;
