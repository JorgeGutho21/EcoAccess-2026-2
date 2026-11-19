-- EcoAccess · comments/interaccion.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "INTERACCION"."ID_USUARIO" IS 'Ciudadano identificado; NULL para un visitante o una apertura manual.';
   COMMENT ON COLUMN "INTERACCION"."CANAL" IS 'CanalInteraccion: 1 Telegram, 2 Manual, 3 Simulador escritorio, 4 Telegram simulado con sesion privada.';
   COMMENT ON COLUMN "INTERACCION"."ESTADO" IS 'EstadoInteraccion: 1 Iniciada, 2 Autorizada, 3 ConDeposito, 4 SinDeposito, 5 Rechazada.';
   COMMENT ON COLUMN "INTERACCION"."ID_MATERIAL_SUGERIDO" IS 'Material que sugirió la clasificación por imagen; solo orienta.';
   COMMENT ON COLUMN "INTERACCION"."CONFIANZA_SUGERIDA" IS 'Confianza de la sugerencia, de 0 a 1.';
   COMMENT ON COLUMN "INTERACCION"."ID_MATERIAL_CONFIRMADO" IS 'Material que la persona confirmó y que se autorizó en la caneca (RN-06).';
   COMMENT ON COLUMN "INTERACCION"."ID_OPERARIO" IS 'Operario que hizo la apertura manual (CU-17).';
   COMMENT ON COLUMN "INTERACCION"."MOTIVO" IS 'Motivo de la apertura manual o del rechazo.';
   COMMENT ON COLUMN "INTERACCION"."CANTIDAD_DECLARADA" IS 'Unidades declaradas, sin verificación física. NULL = no capturadas. No determina ecopuntos.';
   COMMENT ON TABLE "INTERACCION"  IS 'Visita a una caneca por un canal; la sugerencia de la IA no es material verificado (ADR-035).';
