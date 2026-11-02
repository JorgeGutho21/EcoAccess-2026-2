-- EcoAccess P3: definiciones SELECT para revisión de informes privados administrativos.
-- Solo lectura, sin datos identificativos ni acceso a contraseñas/vínculos/fotos.
-- Ejecutado en PRUEBAP3 tras recuperar Oracle; fuentes históricas que incluyen ensayos.
-- Para el informe filtrado actual usar las tres vistas V013 y reportes/powerbi.
-- NO concede privilegios ni crea vistas. El administrador debe publicar estos SELECT como vistas
-- de solo lectura después de verificar esquema/migraciones y autorizar su provisión.
-- No usar la cuenta propietaria en Power BI ni compartir tablas base con compañeros.

-- ActividadDiaria: solicitudes, no reciclaje ni depósito confirmado.
SELECT TRUNC(CAST(FROM_TZ(i.fecha_inicio_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       i.id_contenedor, i.canal,
       COUNT(*) AS solicitudes,
       SUM(CASE WHEN i.id_usuario IS NULL THEN 1 ELSE 0 END) AS solicitudes_invitadas,
       SUM(CASE WHEN i.id_usuario IS NOT NULL THEN 1 ELSE 0 END) AS solicitudes_ciudadanas
FROM interaccion i
GROUP BY TRUNC(CAST(FROM_TZ(i.fecha_inicio_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)), i.id_contenedor, i.canal;

-- DepositosDiarios: un registro por día/caneca/material declarado, sin identificador de ciudadano.
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       o.id_contenedor, m.codigo AS material, COUNT(*) AS depositos,
       SUM(CASE WHEN d.id_material_verificado IS NOT NULL THEN 1 ELSE 0 END) AS verificados,
       COUNT(d.peso_medido_kg) AS con_peso_medido,
       SUM(d.peso_medido_kg) AS kg_medidos
FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
JOIN material m ON m.id_material = d.id_material_declarado
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)), o.id_contenedor, m.codigo;

-- OrdenesDiarias: autorización y apertura confirmada cuentan separadamente.
SELECT TRUNC(CAST(FROM_TZ(o.fecha_emision_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       o.id_contenedor, COUNT(*) AS ordenes,
       SUM(CASE WHEN EXISTS (SELECT 1 FROM evento_dispositivo e
           WHERE e.id_orden = o.id_orden AND e.tipo = 2 AND e.resultado = 1) THEN 1 ELSE 0 END) AS aperturas_confirmadas
FROM orden_apertura o
GROUP BY TRUNC(CAST(FROM_TZ(o.fecha_emision_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)), o.id_contenedor;

-- PuntosDiarios: puntos positivos y reversos firmados; no es saldo por ciudadano.
SELECT TRUNC(CAST(FROM_TZ(op.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       SUM(CASE WHEN me.puntos > 0 THEN me.puntos ELSE 0 END) AS acreditados,
       SUM(CASE WHEN me.puntos < 0 THEN me.puntos ELSE 0 END) AS reversos,
       SUM(me.puntos) AS netos
FROM movimiento_ecopuntos me JOIN operacion_ecopuntos op ON op.id_operacion = me.id_operacion
GROUP BY TRUNC(CAST(FROM_TZ(op.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE));
