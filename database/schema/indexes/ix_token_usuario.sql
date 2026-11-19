-- EcoAccess · indexes/ix_token_usuario.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

  CREATE INDEX "IX_TOKEN_USUARIO" ON "TOKEN_VINCULACION" ("ID_USUARIO_REGISTRADO")
  ;
