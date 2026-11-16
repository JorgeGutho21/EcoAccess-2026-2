-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q13 · ¿Qué usuarios utilizaron una caneca determinada?
-- JOIN: CONTENEDOR → ORDEN_APERTURA → INTERACCION; LEFT JOIN DEPOSITO (una orden puede no terminar en depósito).
-- FK: FK_ORDEN_CONTENEDOR, FK_ORDEN_INTERACCION, FK_DEPOSITO_EVENTO

DEFINE id_caneca = 12
SELECT c.id_contenedor,
       c.nombre                             AS caneca,
       'C-' || LPAD(i.id_usuario, 4, '0')   AS usuario,
       COUNT(DISTINCT o.id_orden)           AS aperturas,
       COUNT(d.id_deposito)                 AS depositos
FROM   contenedor c
JOIN   orden_apertura o ON o.id_contenedor  = c.id_contenedor
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
LEFT   JOIN deposito d  ON d.id_orden       = o.id_orden
WHERE  c.id_contenedor = &id_caneca
AND    i.id_usuario IS NOT NULL
GROUP  BY c.id_contenedor, c.nombre, i.id_usuario
ORDER  BY depositos DESC, usuario;
