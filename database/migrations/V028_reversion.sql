-- ============================================================================
-- ECOACCESS P3 — Script de reversión verificable para migración V028
-- Ejecutar únicamente si V028 fue aplicada y se requiere revertir completamente.
-- ============================================================================

DROP INDEX IX_REGMULTI_COMBINACION;
DROP INDEX IX_REGMULTI_FECHA;
DROP TABLE REGISTRO_MULTIRRESIDUO PURGE;
DROP TABLE CATALOGO_PRIORITARIO_35 PURGE;
