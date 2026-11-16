-- EcoAccess · Base de Datos (profesor Amílkar) · Carpeta 21 · 09 Disparadores de EcoAccess
-- SOLO LECTURA: se consultan los 4 disparadores y lo que han dejado registrado.
-- Para VERLOS ACTUAR (modifica datos y termina en ROLLBACK): ../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql
-- Conexión: EcoAccess BD LOCAL. Ejecuta con F5.
SET PAGESIZE 100
SET LINESIZE 200
COLUMN trigger_name     FORMAT A30
COLUMN trigger_type     FORMAT A16
COLUMN triggering_event FORMAT A18
COLUMN table_name       FORMAT A12

PROMPT ===== A. Los 4 disparadores: momento, evento, tabla y estado =====
SELECT t.trigger_name, t.trigger_type, t.triggering_event, t.table_name,
       t.status AS habilitado, o.status AS compilacion
FROM   user_triggers t
JOIN   user_objects  o ON o.object_name = t.trigger_name AND o.object_type = 'TRIGGER'
ORDER  BY t.table_name, t.trigger_name;

PROMPT ===== B. Cláusula WHEN y columnas que los disparan =====
SELECT trigger_name, when_clause FROM user_triggers WHERE when_clause IS NOT NULL ORDER BY trigger_name;
SELECT trigger_name, column_name FROM user_trigger_cols ORDER BY trigger_name, column_name;

PROMPT ===== C. Orden entre disparadores (FOLLOWS) =====
SELECT trigger_name, referenced_trigger_name
FROM   user_trigger_ordering
ORDER  BY trigger_name;

PROMPT ===== D. Lo que dejó el disparador 1 (historial de canecas), últimas 5 filas =====
SELECT h.id_contenedor, h.estado_anterior, h.estado_nuevo, h.id_actor, h.usuario_bd, h.motivo
FROM   historial_estado_contenedor h
ORDER  BY h.id_historial DESC
FETCH  FIRST 5 ROWS ONLY;

PROMPT ===== E. Lo que dejó el disparador 3 (auditoría de verificación), por operación =====
SELECT operacion, COUNT(*) AS filas
FROM   auditoria_deposito
GROUP  BY operacion
ORDER  BY operacion;

PROMPT ===== F. Un ejemplo de auditoría: valores :OLD y :NEW guardados =====
SELECT a.id_deposito, a.operacion,
       a.estado_anterior, a.estado_nuevo,
       a.cantidad_anterior, a.cantidad_nueva,
       a.id_verificador_anterior, a.id_verificador_nuevo
FROM   auditoria_deposito a
ORDER  BY a.id_auditoria DESC
FETCH  FIRST 5 ROWS ONLY;
