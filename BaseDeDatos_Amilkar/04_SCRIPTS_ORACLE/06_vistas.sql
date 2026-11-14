-- =====================================================================
-- EcoAccess BD — 06 · Vistas
-- Ejecutar como ECOACCESS_BD (F5). Se puede repetir (CREATE OR REPLACE).
--
-- Una VISTA es una consulta guardada con nombre: no guarda datos propios.
--   VW_*     vistas de trabajo (operario/administrador, SQL Developer, C#).
--   VW_BI_*  vistas para Power BI: SIN nombres, correos ni credenciales;
--            el ciudadano aparece como seudónimo C-0001. Son las únicas que
--            recibe el rol ROL_ECO_LECTURA_BI (09_permisos.sql).
-- Todas son WITH READ ONLY: no se puede hacer INSERT/UPDATE a través de ellas.
-- Fechas: se guardan en UTC y aquí se muestran en hora de Colombia.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

-- Canecas con su color derivado de la categoría (el color NO se guarda:
-- guardarlo sería repetir un dato que ya dice CATEGORIA).
CREATE OR REPLACE VIEW vw_caneca_estacion AS
SELECT p.id_punto_instalacion,
       p.nombre                                             AS estacion,
       p.codigo_qr_estacion,
       c.id_contenedor,
       c.nombre                                             AS caneca,
       c.categoria,
       DECODE(c.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA')                     AS color,
       DECODE(c.categoria, 1, 'Aprovechables', 2, 'Orgánicos aprovechables',
                           3, 'No aprovechables')                                   AS tipo_residuo,
       c.estado,
       DECODE(c.estado, 1, 'EN SERVICIO', 2, 'FUERA DE SERVICIO', 3, 'DADA DE BAJA') AS estado_texto,
       c.identificador_qr                                   AS qr_legado_caneca,
       c.identificador_dispositivo,
       p.es_sintetico
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
WITH READ ONLY;

-- Detalle de cada depósito con su titular, caneca y verificación.
CREATE OR REPLACE VIEW vw_deposito_detalle AS
SELECT d.id_deposito,
       i.id_interaccion,
       i.id_usuario                                          AS id_titular,
       u.nombre                                              AS titular,
       c.id_contenedor,
       c.nombre                                              AS caneca,
       DECODE(c.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA') AS color,
       p.nombre                                              AS estacion,
       md.codigo                                             AS material_declarado,
       mv.codigo                                             AS material_verificado,
       d.cantidad_declarada,
       d.cantidad_verificada,
       DECODE(d.sin_sensor, 1, 'DECLARADO SIN SENSOR', 'DETECTADO POR SENSOR') AS evidencia_fisica,
       d.estado_verificacion,
       DECODE(d.estado_verificacion, 0, 'PENDIENTE', 1, 'VERIFICADO', 2, 'RECHAZADO') AS estado_texto,
       d.id_verificador,
       uv.nombre                                             AS verificador,
       d.peso_estimado_kg,
       ROUND(d.peso_estimado_kg * 1000, 1)                   AS peso_estimado_g,
       d.peso_medido_kg,
       ROUND(d.peso_medido_kg * 1000, 1)                     AS peso_medido_g,
       d.instrumento_medicion,
       d.fecha_deteccion_utc,
       CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS fecha_colombia,
       i.canal,
       i.es_sintetico
FROM   deposito d
JOIN   orden_apertura o     ON o.id_orden = d.id_orden
JOIN   interaccion i        ON i.id_interaccion = o.id_interaccion
JOIN   contenedor c         ON c.id_contenedor = o.id_contenedor
JOIN   punto_instalacion p  ON p.id_punto_instalacion = c.id_punto_instalacion
JOIN   material md          ON md.id_material = d.id_material_declarado
LEFT JOIN material mv       ON mv.id_material = d.id_material_verificado
LEFT JOIN usuario u         ON u.id_usuario = i.id_usuario
LEFT JOIN usuario uv        ON uv.id_usuario = d.id_verificador
WITH READ ONLY;

-- Saldo de EcoPuntos: es la SUMA del libro (no se guarda en una columna,
-- así nunca puede quedar desactualizado). Incluye ciudadanos con 0.
CREATE OR REPLACE VIEW vw_saldo_ecopuntos AS
SELECT u.id_usuario,
       u.nombre,
       NVL(SUM(m.puntos), 0)                                   AS saldo,
       NVL(SUM(CASE WHEN m.tipo = 1 THEN m.puntos END), 0)     AS acreditados,
       NVL(SUM(CASE WHEN m.tipo = 2 THEN m.puntos END), 0)     AS revertidos,
       COUNT(m.id_movimiento)                                  AS movimientos,
       u.es_sintetico
FROM   usuario u
JOIN   usuario_rol ur ON ur.id_usuario = u.id_usuario
JOIN   rol r          ON r.id_rol = ur.id_rol AND r.codigo = 1
LEFT JOIN movimiento_ecopuntos m ON m.id_usuario = u.id_usuario
GROUP  BY u.id_usuario, u.nombre, u.es_sintetico
WITH READ ONLY;

-- Recorrido completo de una sesión QR: separa cada evidencia (a)–(g).
CREATE OR REPLACE VIEW vw_recorrido AS
SELECT i.id_interaccion,
       i.id_usuario,
       i.id_punto_instalacion,
       i.canal,
       DECODE(i.estado, 1, 'INICIADA', 2, 'AUTORIZADA', 3, 'CON DEPOSITO', 4, 'SIN DEPOSITO', 5, 'RECHAZADA') AS estado_sesion,
       (SELECT COUNT(*) FROM consulta_vision cv WHERE cv.id_interaccion = i.id_interaccion) AS fotos_consultadas,
       i.id_material_sugerido,
       o.id_orden,
       DECODE(o.estado, 1,'EMITIDA',2,'ENVIADA',3,'ACUSADA',4,'ABIERTA',5,'DETECTADA',6,'CERRADA',7,'VENCIDA',8,'FALLIDA') AS estado_orden,
       (SELECT COUNT(*) FROM evento_dispositivo e WHERE e.id_orden = o.id_orden AND e.tipo = 2) AS aperturas_confirmadas,
       d.id_deposito,
       d.sin_sensor,
       d.estado_verificacion,
       CAST(FROM_TZ(i.fecha_inicio_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS inicio_colombia,
       i.es_sintetico
FROM   interaccion i
LEFT JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion
LEFT JOIN deposito d       ON d.id_orden = o.id_orden
WITH READ ONLY;

-- ======================= Vistas para Power BI =======================
CREATE OR REPLACE VIEW vw_bi_ciudadanos AS
SELECT 'C-' || LPAD(u.id_usuario, 4, '0')                         AS ciudadano,
       TRUNC(CAST(FROM_TZ(u.fecha_registro_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS fecha_registro,
       u.activo,
       u.es_sintetico,
       CASE WHEN u.es_sintetico = 1 THEN 'DATOS SINTÉTICOS' ELSE 'REAL' END AS origen_dato
FROM   usuario u
WHERE  EXISTS (SELECT 1 FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol
               WHERE ur.id_usuario = u.id_usuario AND r.codigo = 1)
WITH READ ONLY;

CREATE OR REPLACE VIEW vw_bi_depositos AS
SELECT d.id_deposito,
       CASE WHEN i.id_usuario IS NULL THEN 'SIN CUENTA' ELSE 'C-' || LPAD(i.id_usuario, 4, '0') END AS ciudadano,
       TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)        AS fecha_hora,
       p.nombre                                                   AS estacion,
       DECODE(c.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA')   AS caneca,
       NVL(mv.codigo, md.codigo)                                  AS material,
       NVL(mv.nombre, md.nombre)                                  AS material_nombre,
       DECODE(d.estado_verificacion, 0, 'PENDIENTE', 1, 'VERIFICADO', 2, 'RECHAZADO') AS estado,
       DECODE(d.sin_sensor, 1, 'SIN SENSOR', 'SENSOR')            AS evidencia,
       NVL(d.cantidad_verificada, d.cantidad_declarada)           AS unidades,
       d.peso_estimado_kg,
       d.peso_medido_kg,
       DECODE(i.canal, 1,'TELEGRAM',2,'MANUAL',3,'SIMULADOR',4,'TELEGRAM SIMULADO',5,'ASISTIDO') AS canal,
       CASE WHEN i.es_sintetico = 1 THEN 'DATOS SINTÉTICOS' ELSE 'REAL' END AS origen_dato
FROM   deposito d
JOIN   orden_apertura o    ON o.id_orden = d.id_orden
JOIN   interaccion i       ON i.id_interaccion = o.id_interaccion
JOIN   contenedor c        ON c.id_contenedor = o.id_contenedor
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
JOIN   material md         ON md.id_material = d.id_material_declarado
LEFT JOIN material mv      ON mv.id_material = d.id_material_verificado
WITH READ ONLY;

CREATE OR REPLACE VIEW vw_bi_consultas_vision AS
SELECT cv.id_consulta,
       CASE WHEN cv.id_usuario IS NULL THEN 'SIN CUENTA' ELSE 'C-' || LPAD(cv.id_usuario, 4, '0') END AS ciudadano,
       TRUNC(CAST(FROM_TZ(cv.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       DECODE(cv.canal, 1, 'TELEGRAM', 2, 'ESCRITORIO', 3, 'EVALUACION')  AS canal,
       DECODE(cv.modo, 1, 'SOFTWARE', 2, 'CON DISPOSITIVO')                AS modo,
       DECODE(cv.resultado, 1, 'IDENTIFICADO', 2, 'PIDIÓ OTRA FOTO', 3, 'NO ES RESIDUO', 4, 'ERROR') AS resultado,
       m.codigo                                                            AS material,
       DECODE(m.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 4, 'RUTA ESPECIAL') AS caneca_indicada,
       cv.unidades_estimadas,
       cv.peso_estimado_kg,
       CASE WHEN cv.id_interaccion IS NULL THEN 'SIN QR' ELSE 'CON QR' END AS contexto_qr,
       cv.latencia_ms,
       CASE WHEN cv.es_sintetico = 1 THEN 'DATOS SINTÉTICOS' ELSE 'REAL' END AS origen_dato
FROM   consulta_vision cv
LEFT JOIN material m ON m.id_material = cv.id_material
WITH READ ONLY;

CREATE OR REPLACE VIEW vw_bi_ecopuntos AS
SELECT m.id_movimiento,
       'C-' || LPAD(m.id_usuario, 4, '0')                              AS ciudadano,
       TRUNC(CAST(FROM_TZ(m.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
       DECODE(m.tipo, 1, 'CRÉDITO', 2, 'REVERSO')                       AS tipo,
       m.puntos,
       mt.codigo                                                        AS material,
       CASE WHEN r.es_sintetica = 1 THEN 'TARIFA SINTÉTICA' ELSE 'TARIFA APROBADA' END AS tarifa
FROM   movimiento_ecopuntos m
JOIN   regla_ecopuntos r ON r.id_regla = m.id_regla
JOIN   material mt       ON mt.id_material = r.id_material
WITH READ ONLY;

-- Auditoría autorizada para informe: sin nombres, solo seudónimos y valores.
CREATE OR REPLACE VIEW vw_bi_auditoria AS
SELECT a.id_auditoria,
       a.id_deposito,
       a.operacion,
       DECODE(a.estado_anterior, 0, 'PENDIENTE', 1, 'VERIFICADO', 2, 'RECHAZADO') AS estado_anterior,
       DECODE(a.estado_nuevo,    0, 'PENDIENTE', 1, 'VERIFICADO', 2, 'RECHAZADO') AS estado_nuevo,
       a.cantidad_anterior,
       a.cantidad_nueva,
       a.peso_medido_anterior_kg,
       a.peso_medido_nuevo_kg,
       CASE WHEN a.id_actor IS NULL THEN 'SIN ACTOR DE APLICACIÓN' ELSE 'U-' || LPAD(a.id_actor, 4, '0') END AS actor,
       a.motivo,
       CAST(FROM_TZ(a.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS fecha_colombia
FROM   auditoria_deposito a
WITH READ ONLY;

BEGIN
    DBMS_OUTPUT.PUT_LINE('06_vistas: 4 vistas de trabajo y 5 vistas para Power BI.');
END;
/
WHENEVER SQLERROR CONTINUE
