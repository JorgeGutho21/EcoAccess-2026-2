-- EcoAccess · comments/orden_apertura.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "ORDEN_APERTURA"."ID_CONTENEDOR" IS 'Caneca de la interacción; se repite para poder indexar la orden vigente.';
   COMMENT ON COLUMN "ORDEN_APERTURA"."CLAVE_IDEMPOTENCIA" IS 'Clave del canal: repetir la solicitud con la misma clave devuelve la misma orden.';
   COMMENT ON COLUMN "ORDEN_APERTURA"."ESTADO" IS 'EstadoOrdenApertura: 1 Emitida, 2 Enviada, 3 Acusada, 4 Abierta, 5 Detectada, 6 Cerrada, 7 Vencida, 8 Fallida.';
   COMMENT ON COLUMN "ORDEN_APERTURA"."FECHA_VENCIMIENTO_UTC" IS 'Después de esta hora la orden no se reenvía ni admite un depósito.';
   COMMENT ON TABLE "ORDEN_APERTURA"  IS 'Autorización para abrir una caneca; abrirla no es reciclar (RN-07, RN-14, RN-16).';
