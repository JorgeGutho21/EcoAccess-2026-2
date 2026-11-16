-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q09 · ¿Cuántas unidades se declararon y cuántas se verificaron por usuario?
-- JOIN: DEPOSITO → ORDEN_APERTURA → INTERACCION.
-- FK: FK_ORDEN_INTERACCION

SELECT 'C-' || LPAD(i.id_usuario, 4, '0')                             AS usuario,
       SUM(NVL(d.cantidad_declarada, 0))                              AS unidades_declaradas,
       SUM(CASE WHEN d.estado_verificacion = 1 THEN NVL(d.cantidad_verificada, 0) ELSE 0 END) AS unidades_verificadas
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY unidades_verificadas DESC, usuario;
