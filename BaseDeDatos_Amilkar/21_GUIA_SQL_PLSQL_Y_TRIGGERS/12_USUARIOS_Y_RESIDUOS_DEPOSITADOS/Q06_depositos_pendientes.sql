-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q06 · ¿Cuáles depósitos están pendientes de verificación?
-- JOIN: DEPOSITO → ORDEN_APERTURA → INTERACCION; DEPOSITO → MATERIAL.
-- FK: FK_ORDEN_INTERACCION, FK_DEPOSITO_MAT_DECL

SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0') AS usuario,
       m.codigo                            AS material_declarado,
       d.cantidad_declarada,
       ROUND((CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS DATE) - CAST(d.fecha_deteccion_utc AS DATE)), 1) AS dias_esperando
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material m       ON m.id_material    = d.id_material_declarado
WHERE  d.estado_verificacion = 0
ORDER  BY d.fecha_deteccion_utc;
