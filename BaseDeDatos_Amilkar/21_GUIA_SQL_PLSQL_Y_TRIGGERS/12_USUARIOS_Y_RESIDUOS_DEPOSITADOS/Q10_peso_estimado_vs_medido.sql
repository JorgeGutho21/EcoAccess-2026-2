-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q10 · ¿Qué peso se estimó y qué peso se midió por usuario?
-- JOIN: DEPOSITO → ORDEN_APERTURA → INTERACCION.
-- FK: FK_ORDEN_INTERACCION (y FK_DEPOSITO_PESO explica de dónde sale el estimado)

SELECT 'C-' || LPAD(i.id_usuario, 4, '0') AS usuario,
       ROUND(SUM(d.peso_estimado_kg), 3)  AS kg_estimados,
       COUNT(d.peso_medido_kg)            AS depositos_con_bascula,
       ROUND(SUM(d.peso_medido_kg), 3)    AS kg_medidos
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY kg_estimados DESC NULLS LAST, usuario;
