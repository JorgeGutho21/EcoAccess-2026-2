-- =====================================================================
-- EcoAccess BD — 13 · Consultas para la exposición
-- Ejecutar como ECOACCESS_BD. Cada consulta se puede ejecutar sola:
-- pon el cursor dentro de ella y pulsa Ctrl+Enter (o F5 para todas).
--
-- Reglas comunes (para no confundir al público):
--   * «Depósito verificado» = DEPOSITO.estado_verificacion = 1. Una foto
--     consultada (CONSULTA_VISION) NUNCA se cuenta como depósito.
--   * Las fechas se guardan en UTC; aquí se convierten a hora de Colombia
--     (America/Bogota, UTC-5, sin horario de verano).
--   * «Últimos 7 días» = desde este mismo instante hace 7×24 horas hasta
--     ahora (incluye el inicio, excluye el futuro).
--     «Semana calendario» = lunes 00:00 a domingo 23:59 (TRUNC(...,'IW')).
--   * Unidades: kg con 3 decimales y g con 1 decimal. Estimado ≠ medido.
--   * Todos los datos del esquema académico son SINTÉTICOS.
-- =====================================================================
SET LINESIZE 220
SET PAGESIZE 60
COLUMN nombre FORMAT A34
COLUMN titular FORMAT A30
COLUMN verificador FORMAT A30
COLUMN actor FORMAT A30
COLUMN material FORMAT A22
COLUMN material_declarado FORMAT A20
COLUMN material_verificado FORMAT A20
COLUMN material_encontrado FORMAT A30
COLUMN motivo FORMAT A45
COLUMN motivo_verificacion FORMAT A45
COLUMN caneca FORMAT A8
COLUMN estado_texto FORMAT A10
COLUMN evidencia_fisica FORMAT A20
COLUMN etapa FORMAT A36
COLUMN canal FORMAT A10
COLUMN contexto FORMAT A7
COLUMN resultado FORMAT A16
COLUMN fecha_registro_utc FORMAT A28

-- Q01 · ¿Quiénes depositaron MÁS residuos VERIFICADOS en los últimos 7 días?
PROMPT ===== Q01 · ¿Quiénes depositaron MÁS residuos VERIFICADOS en los últimos 7 días?
--       Unidad: número de depósitos y de unidades verificadas. Empates comparten puesto.
SELECT puesto, nombre, depositos_verificados, unidades_verificadas, kg_estimados, kg_medidos
FROM (
    SELECT DENSE_RANK() OVER (ORDER BY COUNT(*) DESC, SUM(d.cantidad_verificada) DESC) AS puesto,
           u.nombre,
           COUNT(*)                                  AS depositos_verificados,
           SUM(d.cantidad_verificada)                AS unidades_verificadas,
           ROUND(SUM(d.peso_estimado_kg), 3)         AS kg_estimados,
           ROUND(SUM(d.peso_medido_kg), 3)           AS kg_medidos
    FROM   deposito d
    JOIN   orden_apertura o ON o.id_orden = d.id_orden
    JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
    JOIN   usuario u        ON u.id_usuario = i.id_usuario
    WHERE  d.estado_verificacion = 1
    AND    d.fecha_deteccion_utc >= SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '7' DAY
    GROUP  BY u.id_usuario, u.nombre)
WHERE  puesto <= 10             -- empates en el puesto 10 también aparecen
ORDER  BY puesto, nombre;

-- Q02 · Lo mismo con la función del paquete (SYS_REFCURSOR) — útil para C#.
PROMPT ===== Q02 · Lo mismo con la función del paquete (SYS_REFCURSOR) — útil para C#.
--       Recibe fechas en hora de Colombia: desde hace 7×24 h hasta ahora.
VARIABLE rc REFCURSOR
EXEC :rc := pkg_estadisticas.ranking_verificados(CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE) - 7, CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE), 10);
PRINT rc

-- Q03 · Depósitos verificados por CANECA (color) y MATERIAL en los últimos 7 días.
PROMPT ===== Q03 · Depósitos verificados por CANECA (color) y MATERIAL en los últimos 7 días.
SELECT DECODE(c.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA') AS caneca,
       m.codigo                                  AS material,
       COUNT(*)                                  AS depositos,
       SUM(d.cantidad_verificada)                AS unidades,
       ROUND(SUM(d.peso_estimado_kg) * 1000, 1)  AS gramos_estimados
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden = d.id_orden
JOIN   contenedor c     ON c.id_contenedor = o.id_contenedor
JOIN   material m       ON m.id_material = d.id_material_verificado
WHERE  d.estado_verificacion = 1
AND    d.fecha_deteccion_utc >= SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '7' DAY
GROUP  BY ROLLUP (c.categoria, m.codigo)
ORDER  BY c.categoria, m.codigo;

-- Q04 · Ranking DIARIO: el ciudadano con más verificados cada día (hora Colombia).
PROMPT ===== Q04 · Ranking DIARIO: el ciudadano con más verificados cada día (hora Colombia).
SELECT dia, nombre, verificados
FROM (
    SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia,
           u.nombre, COUNT(*) AS verificados,
           RANK() OVER (PARTITION BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE))
                        ORDER BY COUNT(*) DESC) AS puesto
    FROM   deposito d
    JOIN   orden_apertura o ON o.id_orden = d.id_orden
    JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
    JOIN   usuario u        ON u.id_usuario = i.id_usuario
    WHERE  d.estado_verificacion = 1
    GROUP  BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)), u.id_usuario, u.nombre)
WHERE  puesto = 1
ORDER  BY dia DESC;

-- Q05 · Totales por SEMANA CALENDARIO (lunes a domingo).
PROMPT ===== Q05 · Totales por SEMANA CALENDARIO (lunes a domingo).
SELECT TRUNC(fecha_colombia, 'IW')                                   AS lunes_semana,
       COUNT(*)                                                      AS depositos,
       COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END)           AS verificados,
       ROUND(SUM(CASE WHEN estado_verificacion = 1 THEN peso_estimado_kg END), 3) AS kg_estimados_verificados
FROM   vw_deposito_detalle
GROUP  BY TRUNC(fecha_colombia, 'IW')
ORDER  BY lunes_semana;

-- Q06 · Totales por MES.
PROMPT ===== Q06 · Totales por MES.
SELECT TO_CHAR(TRUNC(fecha_colombia, 'MM'), 'YYYY-MM') AS mes,
       COUNT(*) AS depositos,
       COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END) AS verificados
FROM   vw_deposito_detalle
GROUP  BY TRUNC(fecha_colombia, 'MM')
ORDER  BY mes;

-- Q07 · Intervalo libre: cambia las dos fechas (hora Colombia).
PROMPT ===== Q07 · Intervalo libre: cambia las dos fechas (hora Colombia).
DEFINE desde = '2026-10-01'
DEFINE hasta = '2026-11-30'
SELECT color AS caneca, estado_texto, COUNT(*) AS depositos
FROM   vw_deposito_detalle
WHERE  fecha_colombia >= TO_DATE('&desde', 'YYYY-MM-DD')
AND    fecha_colombia <  TO_DATE('&hasta', 'YYYY-MM-DD') + 1
GROUP  BY color, estado_texto
ORDER  BY color, estado_texto;

-- Q08 · Ciudadanos ACTIVOS (con al menos un depósito verificado en 30 días).
PROMPT ===== Q08 · Ciudadanos ACTIVOS (con al menos un depósito verificado en 30 días).
SELECT COUNT(DISTINCT i.id_usuario) AS ciudadanos_activos_30_dias
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  d.estado_verificacion = 1
AND    d.fecha_deteccion_utc >= SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '30' DAY;

-- Q09 · Ciudadanos SIN ningún depósito (LEFT JOIN + IS NULL = «los que faltan»).
PROMPT ===== Q09 · Ciudadanos SIN ningún depósito (LEFT JOIN + IS NULL = «los que faltan»).
SELECT u.id_usuario, u.nombre, u.fecha_registro_utc
FROM   usuario u
JOIN   usuario_rol ur ON ur.id_usuario = u.id_usuario
JOIN   rol r          ON r.id_rol = ur.id_rol AND r.codigo = 1
LEFT JOIN interaccion i ON i.id_usuario = u.id_usuario
LEFT JOIN orden_apertura o ON o.id_interaccion = i.id_interaccion
LEFT JOIN deposito d       ON d.id_orden = o.id_orden
GROUP  BY u.id_usuario, u.nombre, u.fecha_registro_utc
HAVING COUNT(d.id_deposito) = 0
ORDER  BY u.id_usuario;

-- Q10 · Depósitos PENDIENTES de verificación, del más antiguo al más nuevo.
PROMPT ===== Q10 · Depósitos PENDIENTES de verificación, del más antiguo al más nuevo.
SELECT id_deposito, titular, color, material_declarado, evidencia_fisica,
       ROUND((SYSDATE - fecha_colombia) * 24, 1) AS horas_esperando
FROM   vw_deposito_detalle
WHERE  estado_verificacion = 0
ORDER  BY fecha_colombia;

-- Q11 · TOP por GRAMOS y KILOGRAMOS estimados (solo verificados).
PROMPT ===== Q11 · TOP por GRAMOS y KILOGRAMOS estimados (solo verificados).
SELECT u.nombre,
       ROUND(SUM(d.peso_estimado_kg) * 1000, 1) AS gramos_estimados,
       ROUND(SUM(d.peso_estimado_kg), 3)        AS kg_estimados
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   usuario u        ON u.id_usuario = i.id_usuario
WHERE  d.estado_verificacion = 1 AND d.peso_estimado_kg IS NOT NULL
GROUP  BY u.id_usuario, u.nombre
ORDER  BY kg_estimados DESC
FETCH FIRST 10 ROWS ONLY;

-- Q12 · Diferencia ESTIMADO vs MEDIDO (solo donde hubo báscula).
PROMPT ===== Q12 · Diferencia ESTIMADO vs MEDIDO (solo donde hubo báscula).
SELECT id_deposito, material_verificado,
       peso_estimado_g, peso_medido_g,
       ROUND(peso_medido_g - peso_estimado_g, 1)                          AS diferencia_g,
       ROUND((peso_medido_g - peso_estimado_g) / peso_estimado_g * 100, 1) AS error_porcentual
FROM   vw_deposito_detalle
WHERE  peso_medido_kg IS NOT NULL AND peso_estimado_kg IS NOT NULL
ORDER  BY ABS(peso_medido_g - peso_estimado_g) DESC;

-- Q13 · Saldo de EcoPuntos (histórico) — TOP 10.
PROMPT ===== Q13 · Saldo de EcoPuntos (histórico) — TOP 10.
SELECT nombre, saldo, acreditados, revertidos, movimientos
FROM   vw_saldo_ecopuntos
ORDER  BY saldo DESC, nombre
FETCH FIRST 10 ROWS ONLY;

-- Q14 · Consultas de FOTO por canal y por contexto (con o sin QR) — NO son depósitos.
PROMPT ===== Q14 · Consultas de FOTO por canal y por contexto (con o sin QR) — NO son depósitos.
SELECT DECODE(canal, 1, 'TELEGRAM', 2, 'ESCRITORIO', 3, 'EVALUACION')    AS canal,
       CASE WHEN id_interaccion IS NULL THEN 'SIN QR' ELSE 'CON QR' END AS contexto,
       DECODE(resultado, 1, 'IDENTIFICADO', 2, 'PIDIO OTRA FOTO', 3, 'NO ES RESIDUO', 4, 'ERROR') AS resultado,
       COUNT(*) AS consultas,
       ROUND(AVG(latencia_ms)) AS latencia_media_ms
FROM   consulta_vision
GROUP  BY canal, CASE WHEN id_interaccion IS NULL THEN 'SIN QR' ELSE 'CON QR' END, resultado
ORDER  BY 1, 2, 3;

-- Q15 · Embudo de evidencias: fotos → sesiones → órdenes → aperturas → depósitos → verificados.
PROMPT ===== Q15 · Embudo de evidencias: fotos → sesiones → órdenes → aperturas → depósitos → verificados.
SELECT 'a) Fotos consultadas'          AS etapa, COUNT(*) AS cantidad FROM consulta_vision
UNION ALL SELECT 'b) Sesiones QR',              COUNT(*) FROM interaccion
UNION ALL SELECT 'd) Órdenes de apertura',      COUNT(*) FROM orden_apertura
UNION ALL SELECT 'e) Aperturas confirmadas',    COUNT(*) FROM evento_dispositivo WHERE tipo = 2
UNION ALL SELECT 'f) Depósitos (sensor o declarados)', COUNT(*) FROM deposito
UNION ALL SELECT 'f2) ... de ellos sin sensor', COUNT(*) FROM deposito WHERE sin_sensor = 1
UNION ALL SELECT 'g) Depósitos verificados',    COUNT(*) FROM deposito WHERE estado_verificacion = 1;

-- Q16 · Material MÁS frecuente entre los verificados.
PROMPT ===== Q16 · Material MÁS frecuente entre los verificados.
SELECT m.nombre, COUNT(*) AS veces
FROM   deposito d JOIN material m ON m.id_material = d.id_material_verificado
WHERE  d.estado_verificacion = 1
GROUP  BY m.nombre
ORDER  BY veces DESC
FETCH FIRST 1 ROWS WITH TIES;

-- Q17 · Residuos mal separados (material verificado de otro color que la caneca).
PROMPT ===== Q17 · Residuos mal separados (material verificado de otro color que la caneca).
SELECT d.id_deposito, DECODE(c.categoria,1,'BLANCA',2,'VERDE',3,'NEGRA') AS caneca,
       m.nombre AS material_encontrado, d.motivo_verificacion
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden = d.id_orden
JOIN   contenedor c     ON c.id_contenedor = o.id_contenedor
JOIN   material m       ON m.id_material = d.id_material_verificado
WHERE  m.categoria <> c.categoria;

-- Q18 · Auditoría de verificaciones y correcciones (operario/administrador).
PROMPT ===== Q18 · Auditoría de verificaciones y correcciones (operario/administrador).
SELECT a.id_auditoria, a.id_deposito, a.operacion,
       a.estado_anterior || ' -> ' || a.estado_nuevo         AS estado,
       NVL(TO_CHAR(a.cantidad_anterior), '∅') || ' -> ' || NVL(TO_CHAR(a.cantidad_nueva), '∅') AS cantidad,
       ua.nombre AS actor, a.motivo,
       CAST(FROM_TZ(a.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS fecha_colombia
FROM   auditoria_deposito a
LEFT JOIN usuario ua ON ua.id_usuario = a.id_actor
WHERE  a.operacion IN ('CORRECCION', 'REAPERTURA')
ORDER  BY a.id_auditoria;

-- Q19 · Historial de disponibilidad de las canecas.
PROMPT ===== Q19 · Historial de disponibilidad de las canecas.
SELECT h.id_contenedor, c.nombre, h.estado_anterior || ' -> ' || h.estado_nuevo AS cambio,
       u.nombre AS actor, h.motivo
FROM   historial_estado_contenedor h
JOIN   contenedor c ON c.id_contenedor = h.id_contenedor
LEFT JOIN usuario u ON u.id_usuario = h.id_actor
ORDER  BY h.id_historial;

-- Q20 · Resumen con el procedimiento (OUT SYS_REFCURSOR), últimos 30 días, caneca BLANCA.
PROMPT ===== Q20 · Resumen con el procedimiento (OUT SYS_REFCURSOR), últimos 30 días, caneca BLANCA.
EXEC pr_consultar_estadisticas(TRUNC(SYSDATE) - 30, SYSDATE, NULL, 1, :rc);
PRINT rc

-- Q21 · Subconsulta: ciudadanos por encima del promedio de verificados.
PROMPT ===== Q21 · Subconsulta: ciudadanos por encima del promedio de verificados.
SELECT nombre, verificados
FROM (SELECT u.nombre, COUNT(*) AS verificados
      FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
      JOIN interaccion i ON i.id_interaccion = o.id_interaccion
      JOIN usuario u ON u.id_usuario = i.id_usuario
      WHERE d.estado_verificacion = 1
      GROUP BY u.id_usuario, u.nombre)
WHERE verificados > (SELECT AVG(COUNT(*)) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
                     JOIN interaccion i ON i.id_interaccion = o.id_interaccion
                     WHERE d.estado_verificacion = 1 AND i.id_usuario IS NOT NULL
                     GROUP BY i.id_usuario)
ORDER BY verificados DESC;

-- Q22 · Tiempo medio entre depósito y verificación (horas), por verificador.
PROMPT ===== Q22 · Tiempo medio entre depósito y verificación (horas), por verificador.
SELECT uv.nombre AS verificador, COUNT(*) AS verificaciones,
       ROUND(AVG(EXTRACT(DAY FROM (d.fecha_verificacion_utc - d.fecha_deteccion_utc)) * 24
               + EXTRACT(HOUR FROM (d.fecha_verificacion_utc - d.fecha_deteccion_utc))), 1) AS horas_promedio
FROM   deposito d JOIN usuario uv ON uv.id_usuario = d.id_verificador
WHERE  d.estado_verificacion IN (1, 2)
GROUP  BY uv.nombre;
