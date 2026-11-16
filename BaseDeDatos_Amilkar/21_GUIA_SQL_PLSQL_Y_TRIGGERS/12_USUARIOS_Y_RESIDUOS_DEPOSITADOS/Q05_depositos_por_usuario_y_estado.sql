-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q05 · ¿Cuántos depósitos tiene cada usuario y en qué estado?
-- JOIN: DEPOSITO → ORDEN_APERTURA → INTERACCION.
-- FK: FK_DEPOSITO_EVENTO, FK_EVENTO_ORDEN, FK_ORDEN_INTERACCION

SELECT 'C-' || LPAD(i.id_usuario, 4, '0')                       AS usuario,
       COUNT(*)                                                 AS depositos,
       SUM(CASE WHEN d.estado_verificacion = 0 THEN 1 ELSE 0 END) AS pendientes,
       SUM(CASE WHEN d.estado_verificacion = 1 THEN 1 ELSE 0 END) AS verificados,
       SUM(CASE WHEN d.estado_verificacion = 2 THEN 1 ELSE 0 END) AS rechazados
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY depositos DESC, usuario;
