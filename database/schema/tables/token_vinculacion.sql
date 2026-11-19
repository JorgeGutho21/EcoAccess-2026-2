-- EcoAccess · tables/token_vinculacion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE TABLE "TOKEN_VINCULACION"
   (	"HASH_TOKEN" VARCHAR2(64 CHAR) COLLATE "USING_NLS_COMP" NOT NULL ENABLE,
	"TELEGRAM_USER_ID" NUMBER(19,0) NOT NULL ENABLE,
	"CHAT_ID" NUMBER(19,0) NOT NULL ENABLE,
	"FECHA_CREACION_UTC" TIMESTAMP (6) NOT NULL ENABLE,
	"FECHA_VENCIMIENTO_UTC" TIMESTAMP (6) NOT NULL ENABLE,
	"FECHA_USO_UTC" TIMESTAMP (6),
	"ID_USUARIO_REGISTRADO" NUMBER(10,0),
	 CONSTRAINT "CK_TOKEN_HASH" CHECK (REGEXP_LIKE(hash_token, '^[0-9a-f]{64}$')) ENABLE,
	 CONSTRAINT "CK_TOKEN_TELEGRAM_ID" CHECK (telegram_user_id > 0) ENABLE,
	 CONSTRAINT "CK_TOKEN_CHAT" CHECK (chat_id <> 0) ENABLE,
	 CONSTRAINT "CK_TOKEN_VENCIMIENTO" CHECK (fecha_vencimiento_utc > fecha_creacion_utc) ENABLE,
	 CONSTRAINT "CK_TOKEN_USO" CHECK (
        (fecha_uso_utc IS NULL AND id_usuario_registrado IS NULL) OR
        (fecha_uso_utc IS NOT NULL AND id_usuario_registrado IS NOT NULL
         AND fecha_uso_utc >= fecha_creacion_utc AND fecha_uso_utc < fecha_vencimiento_utc)) ENABLE,
	 CONSTRAINT "PK_TOKEN_VINCULACION" PRIMARY KEY ("HASH_TOKEN")
  USING INDEX  ENABLE
   )  DEFAULT COLLATION "USING_NLS_COMP" ;
