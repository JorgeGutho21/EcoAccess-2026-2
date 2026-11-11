-- EcoAccess P3: estadísticas de cierre (11/11/2026). SOLO LECTURA: ninguna sentencia escribe.
-- Ejecutar como ECOACCESS_P3_APP (SQL Developer, F5). Cada consulta separa lo REAL de lo EXPERIMENTAL:
-- un registro es experimental si aparece en REGISTRO_SIMULACION (lote SIM-2026-11-CIERRE, V025).
-- Fechas en hora de Colombia (America/Bogota). Pesos: ESTIMADO (catálogo) y MEDIDO (báscula) nunca se mezclan.

-- Q1. Depósitos por caneca, real y experimental.
SELECT c.id_contenedor, c.nombre AS caneca,
       SUM(CASE WHEN s.id_registro IS NULL THEN 1 ELSE 0 END) AS reales,
       SUM(CASE WHEN s.id_registro IS NOT NULL THEN 1 ELSE 0 END) AS experimentales
FROM deposito d
JOIN orden_apertura o ON o.id_orden = d.id_orden
JOIN contenedor c ON c.id_contenedor = o.id_contenedor
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
GROUP BY c.id_contenedor, c.nombre
ORDER BY c.id_contenedor;

-- Q2. Material más registrado (declarado por la persona y, aparte, verificado por un operario).
SELECT m.codigo AS material,
       COUNT(*) AS declarados,
       SUM(CASE WHEN d.id_material_verificado = d.id_material_declarado THEN 1 ELSE 0 END) AS verificados_igual,
       SUM(CASE WHEN s.id_registro IS NULL THEN 1 ELSE 0 END) AS reales
FROM deposito d
JOIN material m ON m.id_material = d.id_material_declarado
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
GROUP BY m.codigo
ORDER BY declarados DESC, material;

-- Q3. Actividad semanal (semana ISO, lunes) en hora de Colombia.
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE), 'IW') AS semana,
       SUM(CASE WHEN s.id_registro IS NULL THEN 1 ELSE 0 END) AS reales,
       SUM(CASE WHEN s.id_registro IS NOT NULL THEN 1 ELSE 0 END) AS experimentales
FROM deposito d
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE), 'IW')
ORDER BY semana;

-- Q4. Actividad mensual.
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE), 'MM') AS mes,
       SUM(CASE WHEN s.id_registro IS NULL THEN 1 ELSE 0 END) AS reales,
       SUM(CASE WHEN s.id_registro IS NOT NULL THEN 1 ELSE 0 END) AS experimentales
FROM deposito d
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE), 'MM')
ORDER BY mes;

-- Q5. Personas participantes (con al menos un depósito), reales y experimentales.
SELECT CASE WHEN s.id_registro IS NULL THEN 'REAL' ELSE 'EXPERIMENTAL' END AS origen,
       COUNT(DISTINCT i.id_usuario) AS participantes
FROM deposito d
JOIN orden_apertura o ON o.id_orden = d.id_orden
JOIN interaccion i ON i.id_interaccion = o.id_interaccion
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
GROUP BY CASE WHEN s.id_registro IS NULL THEN 'REAL' ELSE 'EXPERIMENTAL' END;

-- Q6. Pesos: estimado (gramos y kilogramos) y medido, por separado. Un estimado nunca cuenta como medido.
SELECT CASE WHEN s.id_registro IS NULL THEN 'REAL' ELSE 'EXPERIMENTAL' END AS origen,
       COUNT(d.peso_estimado_kg) AS con_peso_estimado,
       ROUND(NVL(SUM(d.peso_estimado_kg), 0) * 1000, 1) AS gramos_estimados,
       ROUND(NVL(SUM(d.peso_estimado_kg), 0), 3) AS kg_estimados,
       COUNT(d.peso_medido_kg) AS con_peso_medido,
       ROUND(NVL(SUM(d.peso_medido_kg), 0), 3) AS kg_medidos
FROM deposito d
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
GROUP BY CASE WHEN s.id_registro IS NULL THEN 'REAL' ELSE 'EXPERIMENTAL' END;

-- Q7. Ecopuntos generados (tipo 1, crédito) y anulados (tipo 2, reverso) del libro de movimientos.
SELECT NVL(SUM(CASE WHEN tipo = 1 THEN puntos END), 0) AS puntos_generados,
       NVL(SUM(CASE WHEN tipo = 2 THEN puntos END), 0) AS puntos_anulados,
       COUNT(*) AS movimientos
FROM movimiento_ecopuntos;

-- Q8. Estado de verificación de los depósitos (declarado sin sensor, verificado).
SELECT CASE WHEN s.id_registro IS NULL THEN 'REAL' ELSE 'EXPERIMENTAL' END AS origen,
       SUM(CASE WHEN d.id_verificador IS NULL THEN 1 ELSE 0 END) AS sin_verificar,
       SUM(CASE WHEN d.id_verificador IS NOT NULL THEN 1 ELSE 0 END) AS verificados,
       SUM(CASE WHEN d.sin_sensor = 1 THEN 1 ELSE 0 END) AS declarados_sin_sensor
FROM deposito d
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
GROUP BY CASE WHEN s.id_registro IS NULL THEN 'REAL' ELSE 'EXPERIMENTAL' END;

-- Q9. Fotos consultadas a Echo (V023): no son depósitos.
SELECT COUNT(*) AS fotos_consultadas, COUNT(DISTINCT id_usuario) AS personas_vinculadas FROM consulta_vision;

-- Q10. Reglas de ecopuntos vigentes (T001).
SELECT m.codigo AS material, r.puntos, r.por_unidad
FROM regla_ecopuntos r JOIN material m ON m.id_material = r.id_material
WHERE r.desde_utc <= SYS_EXTRACT_UTC(SYSTIMESTAMP) AND (r.hasta_utc IS NULL OR r.hasta_utc > SYS_EXTRACT_UTC(SYSTIMESTAMP))
ORDER BY m.codigo;
