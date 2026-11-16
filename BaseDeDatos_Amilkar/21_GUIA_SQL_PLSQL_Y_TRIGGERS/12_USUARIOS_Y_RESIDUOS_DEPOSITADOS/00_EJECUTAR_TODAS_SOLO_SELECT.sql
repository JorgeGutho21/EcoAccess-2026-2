-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
SET PAGESIZE 200
SET LINESIZE 220
SET VERIFY OFF

PROMPT ===== Q01 · ¿Qué usuarios están registrados y qué roles tienen? =====
SELECT u.id_usuario,
       'C-' || LPAD(u.id_usuario, 4, '0')                       AS seudonimo,
       u.activo,
       u.es_sintetico,
       TO_CHAR(u.fecha_registro_utc, 'YYYY-MM-DD')               AS fecha_registro,
       LISTAGG(r.descripcion, ', ') WITHIN GROUP (ORDER BY r.codigo) AS roles
FROM   usuario u
LEFT   JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario
LEFT   JOIN rol r          ON r.id_rol      = ur.id_rol
GROUP  BY u.id_usuario, u.activo, u.es_sintetico, u.fecha_registro_utc
ORDER  BY u.id_usuario;

PROMPT ===== Q02 · ¿Cuántos usuarios han realizado interacciones (sesiones QR)? =====
SELECT COUNT(*)                        AS interacciones,
       COUNT(i.id_usuario)             AS interacciones_con_usuario,
       COUNT(*) - COUNT(i.id_usuario)  AS interacciones_de_visitante,
       COUNT(DISTINCT i.id_usuario)    AS usuarios_distintos
FROM   interaccion i;

PROMPT ===== Q03 · ¿Qué residuos ha depositado cada usuario? =====
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

PROMPT ===== Q04 · ¿Qué material deposita con mayor frecuencia cada usuario? =====
SELECT usuario, material, depositos
FROM  (SELECT 'C-' || LPAD(i.id_usuario, 4, '0') AS usuario,
              m.codigo                           AS material,
              COUNT(*)                           AS depositos,
              ROW_NUMBER() OVER (PARTITION BY i.id_usuario ORDER BY COUNT(*) DESC, m.codigo) AS puesto
       FROM   deposito d
       JOIN   orden_apertura o ON o.id_orden       = d.id_orden
       JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
       JOIN   material m       ON m.id_material    = d.id_material_declarado
       WHERE  i.id_usuario IS NOT NULL
       GROUP  BY i.id_usuario, m.codigo)
WHERE  puesto = 1
ORDER  BY depositos DESC, usuario;

PROMPT ===== Q05 · ¿Cuántos depósitos tiene cada usuario y en qué estado? =====
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

PROMPT ===== Q06 · ¿Cuáles depósitos están pendientes de verificación? =====
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

PROMPT ===== Q07 · ¿Cuáles depósitos fueron verificados y por quién? =====
SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')    AS titular,
       'C-' || LPAD(d.id_verificador, 4, '0') AS verificador,
       mv.codigo                             AS material_verificado,
       d.cantidad_verificada,
       TO_CHAR(d.fecha_verificacion_utc, 'YYYY-MM-DD HH24:MI') AS verificado_utc
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material mv      ON mv.id_material   = d.id_material_verificado
WHERE  d.estado_verificacion = 1
ORDER  BY d.fecha_verificacion_utc;

PROMPT ===== Q08 · ¿Cuáles depósitos fueron rechazados y por qué? =====
SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')    AS titular,
       md.codigo                             AS material_declarado,
       'C-' || LPAD(d.id_verificador, 4, '0') AS verificador,
       d.motivo_verificacion
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material md      ON md.id_material   = d.id_material_declarado
WHERE  d.estado_verificacion = 2
ORDER  BY d.id_deposito;

PROMPT ===== Q09 · ¿Cuántas unidades se declararon y cuántas se verificaron por usuario? =====
SELECT 'C-' || LPAD(i.id_usuario, 4, '0')                             AS usuario,
       SUM(NVL(d.cantidad_declarada, 0))                              AS unidades_declaradas,
       SUM(CASE WHEN d.estado_verificacion = 1 THEN NVL(d.cantidad_verificada, 0) ELSE 0 END) AS unidades_verificadas
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY unidades_verificadas DESC, usuario;

PROMPT ===== Q10 · ¿Qué peso se estimó y qué peso se midió por usuario? =====
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

PROMPT ===== Q11 · ¿Cuántos EcoPuntos se acreditaron a cada usuario? =====
SELECT 'C-' || LPAD(mv.id_usuario, 4, '0')                     AS usuario,
       SUM(CASE WHEN mv.tipo = 1 THEN mv.puntos ELSE 0 END)    AS creditos,
       SUM(CASE WHEN mv.tipo = 2 THEN mv.puntos ELSE 0 END)    AS reversos,
       SUM(mv.puntos)                                          AS saldo
FROM   movimiento_ecopuntos mv
GROUP  BY mv.id_usuario
ORDER  BY saldo DESC, usuario;

PROMPT ===== Q12 · ¿Qué movimientos de EcoPuntos fueron reversados? =====
SELECT rv.id_movimiento                     AS id_reverso,
       rv.id_movimiento_revertido           AS id_credito_anulado,
       'C-' || LPAD(rv.id_usuario, 4, '0')  AS usuario,
       cr.puntos                            AS puntos_del_credito,
       rv.puntos                            AS puntos_del_reverso,
       rv.motivo
FROM   movimiento_ecopuntos rv
JOIN   movimiento_ecopuntos cr ON cr.id_movimiento = rv.id_movimiento_revertido
WHERE  rv.tipo = 2
ORDER  BY rv.id_movimiento;

PROMPT ===== Q13 · ¿Qué usuarios utilizaron una caneca determinada? =====
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

PROMPT ===== Q14 · ¿Qué cambios fueron registrados en la auditoría de depósitos? =====
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

PROMPT ===== Q15 · ¿Cuántas fotos consultó cada usuario frente a cuántos depósitos hizo? =====
SELECT u.id_usuario,
       'C-' || LPAD(u.id_usuario, 4, '0') AS usuario,
       (SELECT COUNT(*) FROM consulta_vision cv WHERE cv.id_usuario = u.id_usuario) AS fotos_consultadas,
       (SELECT COUNT(*)
        FROM   deposito d
        JOIN   orden_apertura o ON o.id_orden       = d.id_orden
        JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
        WHERE  i.id_usuario = u.id_usuario)                                       AS depositos
FROM   usuario u
ORDER  BY fotos_consultadas DESC, u.id_usuario
FETCH  FIRST 15 ROWS ONLY;

