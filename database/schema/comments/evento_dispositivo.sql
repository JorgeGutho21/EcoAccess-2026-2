-- EcoAccess · comments/evento_dispositivo.sql
-- DDL real extraído con DBMS_METADATA de ECOACCESS_P3_APP (Oracle 19.0.0.0.0) el 2026-11-18.
-- Generado por database/scripts/Exportar-EsquemaOracle.ps1: no editar a mano; los cambios van en database/06_migraciones.

   COMMENT ON COLUMN "EVENTO_DISPOSITIVO"."ID_EXTERNO" IS 'Id que asigna el dispositivo; el mismo id en la misma caneca es una retransmisión.';
   COMMENT ON COLUMN "EVENTO_DISPOSITIVO"."TIPO" IS 'TipoEventoDispositivo: 1 Acuse, 2 Apertura, 3 Deteccion, 4 Cierre, 5 Fallo.';
   COMMENT ON COLUMN "EVENTO_DISPOSITIVO"."RESULTADO" IS 'ResultadoEvento: 1 Aplicado a la orden, 2 Revision.';
   COMMENT ON COLUMN "EVENTO_DISPOSITIVO"."FECHA_DISPOSITIVO_UTC" IS 'Hora según el reloj de la caneca; puede faltar o no ser fiable.';
   COMMENT ON COLUMN "EVENTO_DISPOSITIVO"."FECHA_RECEPCION_UTC" IS 'Hora UTC del servidor; es la que cuenta para la ventana de la orden.';
   COMMENT ON TABLE "EVENTO_DISPOSITIVO"  IS 'Evento recibido de una caneca ya autenticada por el adaptador (RN-20, ADR-010).';
