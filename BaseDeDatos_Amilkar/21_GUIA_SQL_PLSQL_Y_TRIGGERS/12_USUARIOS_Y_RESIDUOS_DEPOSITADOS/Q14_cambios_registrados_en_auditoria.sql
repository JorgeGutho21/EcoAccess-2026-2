-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q14 · ¿Qué cambios fueron registrados en la auditoría de depósitos?
-- JOIN: AUDITORIA_DEPOSITO → DEPOSITO → ORDEN_APERTURA → INTERACCION.
-- FK: FK_AUD_DEP_DEPOSITO, FK_ORDEN_INTERACCION

SELECT a.id_auditoria,
       a.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')   AS titular,
       a.operacion,
       a.estado_anterior || ' -> ' || a.estado_nuevo AS estado,
       'C-' || LPAD(a.id_actor, 4, '0')     AS actor,
       a.usuario_bd,
       TO_CHAR(a.fecha_utc, 'YYYY-MM-DD HH24:MI') AS fecha_utc
FROM   auditoria_deposito a
JOIN   deposito d       ON d.id_deposito    = a.id_deposito
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
ORDER  BY a.fecha_utc, a.id_auditoria;
