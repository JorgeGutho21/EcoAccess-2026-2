-- EcoAccess · comments/material.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "MATERIAL"."CATEGORIA" IS '1 Aprovechable, 2 OrganicoAprovechable, 3 NoAprovechable, 4 Peligroso';
   COMMENT ON COLUMN "MATERIAL"."SINONIMOS" IS 'Nombres de búsqueda, sin convertir plásticos genéricos en PET.';
   COMMENT ON COLUMN "MATERIAL"."PRECAUCIONES" IS 'Orientación de manejo; no sustituye las instrucciones del gestor o de la EPS/IPS.';
   COMMENT ON COLUMN "MATERIAL"."RUTA_SEPARACION" IS 'Destino educativo propuesto, no evidencia de gestión realizada.';
   COMMENT ON TABLE "MATERIAL"  IS 'Catálogo validado de materiales; incluye no admitidos para orientar';
