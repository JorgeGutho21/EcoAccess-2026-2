-- EcoAccess · tables/conversacion_telegram.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE TABLE "CONVERSACION_TELEGRAM"
   (	"TELEGRAM_USER_ID" NUMBER(19,0) NOT NULL ENABLE,
	"CHAT_ID" NUMBER(19,0) NOT NULL ENABLE,
	"ESTADO" NUMBER(1,0) NOT NULL ENABLE,
	"ID_CONTENEDOR" NUMBER(10,0),
	"ID_INTERACCION" NUMBER(10,0),
	"ID_MATERIAL_PROPUESTO" NUMBER(10,0),
	"FECHA_ACTUALIZACION_UTC" TIMESTAMP (6) NOT NULL ENABLE,
	"VERSION" NUMBER(10,0) NOT NULL ENABLE,
	 CONSTRAINT "CK_CONVERSACION_TELEGRAM" CHECK (telegram_user_id > 0) ENABLE,
	 CONSTRAINT "CK_CONVERSACION_CHAT" CHECK (chat_id <> 0) ENABLE,
	 CONSTRAINT "CK_CONVERSACION_ESTADO" CHECK (estado IN (1, 2, 3, 4)) ENABLE,
	 CONSTRAINT "CK_CONVERSACION_VERSION" CHECK (version >= 1) ENABLE,
	 CONSTRAINT "CK_CONVERSACION_IDS" CHECK (
        (estado = 1 AND id_contenedor IS NULL AND id_interaccion IS NULL AND id_material_propuesto IS NULL) OR
        (estado = 2 AND id_contenedor IS NOT NULL AND id_material_propuesto IS NULL) OR
        (estado IN (3, 4) AND id_contenedor IS NOT NULL AND id_interaccion IS NOT NULL
                          AND id_material_propuesto IS NOT NULL)) ENABLE,
	 CONSTRAINT "PK_CONVERSACION_TELEGRAM" PRIMARY KEY ("TELEGRAM_USER_ID")
  USING INDEX  ENABLE
   )  DEFAULT COLLATION "USING_NLS_COMP" ;
