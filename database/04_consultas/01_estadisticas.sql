-- =====================================================================
-- EcoAccess P3 — consultas de estadísticas de depósitos
-- Archivo: database/04_consultas/01_estadisticas.sql
--
-- Versión de estudio de las consultas que ejecuta RepositorioEstadisticasOracle.
-- Solo lee: no inserta, no actualiza y no bloquea filas. Requiere 001, V002 y V003.
--
-- Reglas que se ven en el SQL:
--   * Cada tabla se filtra por su propia fecha: inicio de la interacción, emisión
--     de la orden, recepción del evento y detección del depósito.
--   * Período semiabierto [desde, hasta): una fila de las 00:00 del día final no entra.
--   * Interacciones, órdenes, eventos y depósitos se cuentan por separado; una
--     orden no es una apertura confirmada y una apertura no es un depósito.
--   * Kilogramos medidos y estimados van en columnas distintas y nunca se suman
--     (RN-12).
--   * El usuario se muestra solo por id (RN-13); el punto es la ubicación de la
--     caneca, no la residencia del ciudadano.
--   * Los filtros opcionales se escriben (:p IS NULL OR columna = :p).
--
-- Uso en SQL*Plus o SQLcl: ajustar los EXEC del principio y ejecutar el archivo.
-- =====================================================================

SET LINESIZE 200 PAGESIZE 100 FEEDBACK OFF VERIFY OFF

VARIABLE desde        VARCHAR2(30)
VARIABLE hasta        VARCHAR2(30)
VARIABLE contenedor   NUMBER
VARIABLE material     NUMBER
VARIABLE usuario      NUMBER
VARIABLE verificacion NUMBER
VARIABLE criterio     NUMBER

-- Período en UTC (texto ISO). Los demás filtros quedan en NULL = todos.
EXEC :desde := '2026-01-01 00:00:00'; :hasta := '2027-01-01 00:00:00';
-- verificacion: 0 todos, 1 sin verificar, 2 verificados. criterio: 1 declarado, 2 verificado.
EXEC :verificacion := 0; :criterio := 1;

PROMPT
PROMPT === 1. Interacciones del período (solicitudes, no reciclaje) ===
SELECT COUNT(*)                                AS total,
       COUNT(CASE WHEN i.estado = 1 THEN 1 END) AS iniciadas,
       COUNT(CASE WHEN i.estado = 2 THEN 1 END) AS autorizadas,
       COUNT(CASE WHEN i.estado = 3 THEN 1 END) AS con_deposito,
       COUNT(CASE WHEN i.estado = 4 THEN 1 END) AS sin_deposito,
       COUNT(CASE WHEN i.estado = 5 THEN 1 END) AS rechazadas,
       COUNT(CASE WHEN i.canal = 1 THEN 1 END)  AS telegram,
       COUNT(CASE WHEN i.canal = 2 THEN 1 END)  AS manual,
       COUNT(CASE WHEN i.canal = 3 THEN 1 END)  AS simulador
  FROM interaccion i
 WHERE i.fecha_inicio_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
   AND i.fecha_inicio_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
   AND (:contenedor IS NULL OR i.id_contenedor = :contenedor)
   AND (:usuario IS NULL OR i.id_usuario = :usuario);

PROMPT
PROMPT === 2. Órdenes emitidas y aperturas confirmadas por el sensor ===
-- Una orden tiene apertura confirmada si existe un evento de apertura (tipo 2) aplicado (resultado 1).
-- Se usa EXISTS y no un JOIN: si llegaran dos aperturas aplicadas de la misma orden, la orden cuenta una vez.
SELECT COUNT(*)                                            AS total,
       COUNT(CASE WHEN x.estado BETWEEN 1 AND 5 THEN 1 END) AS vigentes,
       COUNT(CASE WHEN x.estado = 6 THEN 1 END)             AS cerradas,
       COUNT(CASE WHEN x.estado = 7 THEN 1 END)             AS vencidas,
       COUNT(CASE WHEN x.estado = 8 THEN 1 END)             AS fallidas,
       COUNT(CASE WHEN x.apertura = 1 THEN 1 END)           AS apertura_confirmada,
       COUNT(x.id_deposito)                                 AS con_deposito
  FROM (SELECT o.estado, dp.id_deposito,
               CASE WHEN EXISTS (SELECT 1 FROM evento_dispositivo ea
                                  WHERE ea.id_orden = o.id_orden AND ea.tipo = 2 AND ea.resultado = 1)
                    THEN 1 ELSE 0 END AS apertura
          FROM orden_apertura o
          JOIN interaccion i ON i.id_interaccion = o.id_interaccion
          LEFT JOIN deposito dp ON dp.id_orden = o.id_orden   -- a lo sumo un depósito por orden (UNIQUE)
         WHERE o.fecha_emision_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
           AND o.fecha_emision_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
           AND (:contenedor IS NULL OR o.id_contenedor = :contenedor)
           AND (:usuario IS NULL OR i.id_usuario = :usuario)) x;

PROMPT
PROMPT === 3. Eventos de las canecas (en revisión = no aplicados, RN-20) ===
-- LEFT JOIN: un evento huérfano no tiene orden ni usuario; filtrar por usuario lo deja fuera.
SELECT COUNT(*)                                    AS total,
       COUNT(CASE WHEN e.resultado = 1 THEN 1 END) AS aplicados,
       COUNT(CASE WHEN e.resultado = 2 THEN 1 END) AS en_revision,
       COUNT(CASE WHEN e.tipo = 5 THEN 1 END)      AS fallos
  FROM evento_dispositivo e
  LEFT JOIN orden_apertura o ON o.id_orden = e.id_orden
  LEFT JOIN interaccion i ON i.id_interaccion = o.id_interaccion
 WHERE e.fecha_recepcion_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
   AND e.fecha_recepcion_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
   AND (:contenedor IS NULL OR e.id_contenedor = :contenedor)
   AND (:usuario IS NULL OR i.id_usuario = :usuario);

PROMPT
PROMPT === 4. Depósitos agrupados por material (criterio y verificación elegidos) ===
-- El filtro de dep es el mismo en las consultas 4, 5 y 7. Material y verificación solo filtran depósitos.
WITH dep AS (
    SELECT d.id_deposito, d.fecha_deteccion_utc, o.id_contenedor, i.id_usuario,
           d.id_material_declarado, d.id_material_verificado, d.peso_medido_kg, d.peso_estimado_kg,
           CASE WHEN :criterio = 2 THEN d.id_material_verificado ELSE d.id_material_declarado END AS id_material
      FROM deposito d
      JOIN orden_apertura o ON o.id_orden = d.id_orden
      JOIN interaccion i ON i.id_interaccion = o.id_interaccion
     WHERE d.fecha_deteccion_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
       AND d.fecha_deteccion_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
       AND (:contenedor IS NULL OR o.id_contenedor = :contenedor)
       AND (:usuario IS NULL OR i.id_usuario = :usuario)
       AND (:criterio = 1 OR d.id_material_verificado IS NOT NULL)
       AND (:material IS NULL
            OR (CASE WHEN :criterio = 2 THEN d.id_material_verificado ELSE d.id_material_declarado END) = :material)
       AND (:verificacion = 0
            OR (:verificacion = 1 AND d.id_material_verificado IS NULL)
            OR (:verificacion = 2 AND d.id_material_verificado IS NOT NULL))
)
SELECT m.nombre                         AS material,
       COUNT(*)                         AS depositos,
       COUNT(dep.id_material_verificado) AS verificados,
       COUNT(dep.peso_medido_kg)        AS con_peso_medido,
       NVL(SUM(dep.peso_medido_kg), 0)  AS kg_medidos,
       COUNT(dep.peso_estimado_kg)      AS con_peso_estimado,
       NVL(SUM(dep.peso_estimado_kg), 0) AS kg_estimados
  FROM dep
  JOIN material m ON m.id_material = dep.id_material
 GROUP BY dep.id_material, m.nombre
 ORDER BY depositos DESC, m.nombre, dep.id_material;   -- el id desempata nombres iguales

PROMPT
PROMPT === 5. Depósitos por día civil de Colombia (UTC-5, sin horario de verano) ===
WITH dep AS (
    SELECT d.fecha_deteccion_utc, d.id_material_verificado, d.peso_medido_kg, d.peso_estimado_kg
      FROM deposito d
      JOIN orden_apertura o ON o.id_orden = d.id_orden
      JOIN interaccion i ON i.id_interaccion = o.id_interaccion
     WHERE d.fecha_deteccion_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
       AND d.fecha_deteccion_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
       AND (:contenedor IS NULL OR o.id_contenedor = :contenedor)
       AND (:usuario IS NULL OR i.id_usuario = :usuario)
       AND (:criterio = 1 OR d.id_material_verificado IS NOT NULL)
       AND (:material IS NULL
            OR (CASE WHEN :criterio = 2 THEN d.id_material_verificado ELSE d.id_material_declarado END) = :material)
       AND (:verificacion = 0
            OR (:verificacion = 1 AND d.id_material_verificado IS NULL)
            OR (:verificacion = 2 AND d.id_material_verificado IS NOT NULL))
)
SELECT TO_CHAR(TRUNC(CAST(dep.fecha_deteccion_utc - INTERVAL '5' HOUR AS DATE)), 'YYYY-MM-DD') AS dia,
       COUNT(*)                          AS depositos,
       COUNT(dep.id_material_verificado) AS verificados,
       NVL(SUM(dep.peso_medido_kg), 0)   AS kg_medidos,
       NVL(SUM(dep.peso_estimado_kg), 0) AS kg_estimados
  FROM dep
 GROUP BY TRUNC(CAST(dep.fecha_deteccion_utc - INTERVAL '5' HOUR AS DATE))
 ORDER BY 1;

PROMPT
PROMPT === 6. Canecas con incidencias (fallos, revisión, órdenes fallidas o vencidas) ===
-- En revisión excluye los fallos (tipo 5), que ya tienen su columna: cada evento cuenta una vez.
SELECT c.id_contenedor, c.nombre,
       NVL(ev.fallos, 0) AS eventos_fallo, NVL(ev.revision, 0) AS en_revision,
       NVL(od.fallidas, 0) AS ordenes_fallidas, NVL(od.vencidas, 0) AS ordenes_vencidas
  FROM contenedor c
  LEFT JOIN (SELECT e.id_contenedor,
                    COUNT(CASE WHEN e.tipo = 5 THEN 1 END)                     AS fallos,
                    COUNT(CASE WHEN e.resultado = 2 AND e.tipo <> 5 THEN 1 END) AS revision
               FROM evento_dispositivo e
               LEFT JOIN orden_apertura o ON o.id_orden = e.id_orden
               LEFT JOIN interaccion i ON i.id_interaccion = o.id_interaccion
              WHERE e.fecha_recepcion_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
                AND e.fecha_recepcion_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
                AND (:usuario IS NULL OR i.id_usuario = :usuario)
              GROUP BY e.id_contenedor) ev ON ev.id_contenedor = c.id_contenedor
  LEFT JOIN (SELECT o.id_contenedor,
                    COUNT(CASE WHEN o.estado = 8 THEN 1 END) AS fallidas,
                    COUNT(CASE WHEN o.estado = 7 THEN 1 END) AS vencidas
               FROM orden_apertura o
               JOIN interaccion i ON i.id_interaccion = o.id_interaccion
              WHERE o.fecha_emision_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
                AND o.fecha_emision_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
                AND (:usuario IS NULL OR i.id_usuario = :usuario)
              GROUP BY o.id_contenedor) od ON od.id_contenedor = c.id_contenedor
 WHERE (:contenedor IS NULL OR c.id_contenedor = :contenedor)
   AND NVL(ev.fallos, 0) + NVL(ev.revision, 0) + NVL(od.fallidas, 0) + NVL(od.vencidas, 0) > 0
 ORDER BY NVL(ev.fallos, 0) + NVL(ev.revision, 0) + NVL(od.fallidas, 0) + NVL(od.vencidas, 0) DESC, c.nombre, c.id_contenedor;

PROMPT
PROMPT === 7. Últimos 20 depósitos (detalle paginado, página 1) ===
SELECT d.id_deposito, TO_CHAR(d.fecha_deteccion_utc, 'YYYY-MM-DD HH24:MI') AS detectado_utc,
       c.nombre AS caneca, i.id_usuario, md.nombre AS declarado, mv.nombre AS verificado,
       d.peso_medido_kg, d.peso_estimado_kg
  FROM deposito d
  JOIN orden_apertura o ON o.id_orden = d.id_orden
  JOIN interaccion i ON i.id_interaccion = o.id_interaccion
  JOIN contenedor c ON c.id_contenedor = o.id_contenedor
  JOIN material md ON md.id_material = d.id_material_declarado
  LEFT JOIN material mv ON mv.id_material = d.id_material_verificado
 WHERE d.fecha_deteccion_utc >= TO_TIMESTAMP(:desde, 'YYYY-MM-DD HH24:MI:SS')
   AND d.fecha_deteccion_utc <  TO_TIMESTAMP(:hasta, 'YYYY-MM-DD HH24:MI:SS')
   AND (:contenedor IS NULL OR o.id_contenedor = :contenedor)
   AND (:usuario IS NULL OR i.id_usuario = :usuario)
   AND (:criterio = 1 OR d.id_material_verificado IS NOT NULL)
   AND (:material IS NULL
        OR (CASE WHEN :criterio = 2 THEN d.id_material_verificado ELSE d.id_material_declarado END) = :material)
   AND (:verificacion = 0
        OR (:verificacion = 1 AND d.id_material_verificado IS NULL)
        OR (:verificacion = 2 AND d.id_material_verificado IS NOT NULL))
 ORDER BY d.fecha_deteccion_utc DESC, d.id_deposito DESC
 OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;

SET FEEDBACK ON
