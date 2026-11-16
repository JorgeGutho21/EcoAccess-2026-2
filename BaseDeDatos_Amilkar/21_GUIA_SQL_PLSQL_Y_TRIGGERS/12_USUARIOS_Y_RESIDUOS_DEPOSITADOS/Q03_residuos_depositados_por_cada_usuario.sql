-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q03 · ¿Qué residuos ha depositado cada usuario?
-- JOIN: DEPOSITO → ORDEN_APERTURA → INTERACCION (titular) y → CONTENEDOR; MATERIAL dos veces (declarado y verificado, con alias md y mv).
-- FK: FK_DEPOSITO_EVENTO (id_evento, id_orden), FK_EVENTO_ORDEN, FK_ORDEN_INTERACCION, FK_ORDEN_CONTENEDOR, FK_DEPOSITO_MAT_DECL, FK_DEPOSITO_MAT_VERIF

SELECT 'C-' || LPAD(i.id_usuario, 4, '0')   AS usuario,
       d.id_deposito,
       md.codigo                            AS material_declarado,
       mv.codigo                            AS material_verificado,
       CASE d.estado_verificacion WHEN 0 THEN 'PENDIENTE' WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END AS estado,
       c.nombre                             AS caneca,
       TO_CHAR(d.fecha_deteccion_utc, 'YYYY-MM-DD HH24:MI') AS fecha_utc
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   contenedor c     ON c.id_contenedor  = o.id_contenedor
JOIN   material md      ON md.id_material   = d.id_material_declarado
LEFT   JOIN material mv ON mv.id_material   = d.id_material_verificado
WHERE  i.id_usuario IS NOT NULL
ORDER  BY i.id_usuario, d.fecha_deteccion_utc;
