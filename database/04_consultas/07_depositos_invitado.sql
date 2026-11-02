-- Depósitos de visitante (sin cuenta) hechos desde la aplicación de escritorio con la caneca DEMO.
-- Solo lectura. Ejecutar en PRUEBAP3 o en la base configurada; no modifica datos.

-- 1. Últimas operaciones de visitante: solicitud, orden, estado y depósito.
SELECT *
  FROM (SELECT i.id_interaccion,
               i.canal,
               i.estado                AS estado_interaccion,
               m.nombre                AS material,
               o.id_orden,
               o.estado                AS estado_orden,
               o.clave_idempotencia,
               d.id_deposito,
               d.fecha_deteccion_utc,
               d.id_verificador
          FROM interaccion i
          LEFT JOIN material m        ON m.id_material = i.id_material_confirmado
          LEFT JOIN orden_apertura o  ON o.id_interaccion = i.id_interaccion
          LEFT JOIN deposito d        ON d.id_orden = o.id_orden
         WHERE i.id_usuario IS NULL
           AND i.canal = 3
         ORDER BY i.id_interaccion DESC)
 WHERE ROWNUM <= 20;

-- 2. Eventos de la caneca para una orden (reemplazar :id_orden).
SELECT id_evento, tipo, resultado, fecha_recepcion_utc
  FROM evento_dispositivo
 WHERE id_orden = :id_orden
 ORDER BY id_evento;

-- 3. Comprobación: ningún depósito de visitante tiene ecopuntos.
SELECT COUNT(*) AS movimientos_de_visitante
  FROM movimiento_ecopuntos me
  JOIN deposito d       ON d.id_deposito = me.id_deposito
  JOIN orden_apertura o ON o.id_orden = d.id_orden
  JOIN interaccion i    ON i.id_interaccion = o.id_interaccion
 WHERE i.id_usuario IS NULL;
