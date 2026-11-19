-- EcoAccess · comments/deposito.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "DEPOSITO"."ID_MATERIAL_DECLARADO" IS 'Material confirmado por la persona al pedir la apertura; no está verificado.';
   COMMENT ON COLUMN "DEPOSITO"."ID_MATERIAL_VERIFICADO" IS 'Material comprobado por un operario; NULL mientras nadie lo verifique.';
   COMMENT ON COLUMN "DEPOSITO"."PESO_MEDIDO_KG" IS 'Peso medido por una báscula; NULL si no se midió (RN-12, ADR-012).';
   COMMENT ON COLUMN "DEPOSITO"."PESO_ESTIMADO_KG" IS 'Estimación (por imagen u otra fuente); nunca se suma como kilogramos reales.';
   COMMENT ON COLUMN "DEPOSITO"."CANTIDAD_VERIFICADA" IS 'Conteo independiente con el responsable y fecha de verificacion; no se copia de la declaracion.';
   COMMENT ON TABLE "DEPOSITO"  IS 'Depósito confirmado por el sensor dentro de la ventana de una orden vigente (RN-08, RN-17).';
