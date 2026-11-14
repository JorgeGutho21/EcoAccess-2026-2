-- 04_GROUP_BY_HAVING_Y_ANALITICAS — 37 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E088 · 6. Agrupar: GROUP BY y HAVING · COUNT por grupo
-- Objetivo: Depósitos por estado.
-- Explicación: GROUP BY forma un grupo por cada valor.
SELECT estado_verificacion, COUNT(*) AS cantidad FROM deposito GROUP BY estado_verificacion ORDER BY 1;

-- ===== E089 · 6. Agrupar: GROUP BY y HAVING · SUM
-- Objetivo: Kilogramos estimados verificados.
-- Explicación: SUM suma; los NULL se ignoran.
SELECT ROUND(SUM(peso_estimado_kg), 3) AS kg_estimados FROM deposito WHERE estado_verificacion = 1;

-- ===== E090 · 6. Agrupar: GROUP BY y HAVING · AVG, MIN, MAX
-- Objetivo: Estadísticas de latencia de la IA.
-- Explicación: Promedio, mínimo y máximo.
SELECT ROUND(AVG(latencia_ms)) AS promedio, MIN(latencia_ms) AS minimo, MAX(latencia_ms) AS maximo FROM consulta_vision;

-- ===== E091 · 6. Agrupar: GROUP BY y HAVING · COUNT(columna) vs COUNT(*)
-- Objetivo: Ver la diferencia con NULL.
-- Explicación: COUNT(columna) no cuenta los NULL.
SELECT COUNT(*) AS todos, COUNT(peso_medido_kg) AS con_medicion FROM deposito;

-- ===== E092 · 6. Agrupar: GROUP BY y HAVING · COUNT DISTINCT
-- Objetivo: Ciudadanos distintos con sesión.
-- Explicación: Cuenta valores sin repetir.
SELECT COUNT(DISTINCT id_usuario) AS ciudadanos FROM interaccion;

-- ===== E093 · 6. Agrupar: GROUP BY y HAVING · Agrupar por dos columnas
-- Objetivo: Depósitos por sensor y estado.
-- Explicación: Un grupo por cada combinación.
SELECT sin_sensor, estado_verificacion, COUNT(*) FROM deposito GROUP BY sin_sensor, estado_verificacion ORDER BY 1, 2;

-- ===== E094 · 6. Agrupar: GROUP BY y HAVING · HAVING
-- Objetivo: Ciudadanos con 3 o más sesiones.
-- Explicación: HAVING filtra GRUPOS (WHERE filtra filas).
SELECT id_usuario, COUNT(*) AS sesiones FROM interaccion WHERE id_usuario IS NOT NULL GROUP BY id_usuario HAVING COUNT(*) >= 3 ORDER BY sesiones DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E095 · 6. Agrupar: GROUP BY y HAVING · WHERE + GROUP BY + HAVING
-- Objetivo: Materiales verificados más de 3 veces.
-- Explicación: Orden: WHERE → GROUP BY → HAVING → ORDER BY.
SELECT id_material_verificado, COUNT(*) AS veces FROM deposito WHERE estado_verificacion = 1 GROUP BY id_material_verificado HAVING COUNT(*) > 3 ORDER BY veces DESC;

-- ===== E096 · 6. Agrupar: GROUP BY y HAVING · Agrupar por día
-- Objetivo: Depósitos por día (hora Colombia).
-- Explicación: Se convierte a Colombia y TRUNC quita la hora.
SELECT TRUNC(CAST(FROM_TZ(fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia, COUNT(*) FROM deposito GROUP BY TRUNC(CAST(FROM_TZ(fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) ORDER BY dia DESC FETCH FIRST 7 ROWS ONLY;

-- ===== E097 · 6. Agrupar: GROUP BY y HAVING · Agrupar por semana
-- Objetivo: Depósitos por semana ISO.
-- Explicación: 'IW' lleva cada fecha al lunes de su semana.
SELECT TRUNC(fecha_colombia, 'IW') AS semana, COUNT(*) FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia, 'IW') ORDER BY semana;

-- ===== E098 · 6. Agrupar: GROUP BY y HAVING · Conteo condicional
-- Objetivo: Verificados y pendientes en una sola fila.
-- Explicación: COUNT(CASE ...) cuenta solo cuando se cumple.
SELECT COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END) AS verificados, COUNT(CASE WHEN estado_verificacion = 0 THEN 1 END) AS pendientes FROM deposito;

-- ===== E099 · 6. Agrupar: GROUP BY y HAVING · ROLLUP
-- Objetivo: Subtotales por color y total general.
-- Explicación: ROLLUP agrega filas de subtotal.
SELECT color, estado_texto, COUNT(*) FROM vw_deposito_detalle GROUP BY ROLLUP(color, estado_texto) ORDER BY color, estado_texto;

-- ===== E100 · 6. Agrupar: GROUP BY y HAVING · LISTAGG
-- Objetivo: Materiales de cada caneca en una sola línea.
-- Explicación: LISTAGG une textos de un grupo.
SELECT cm.id_contenedor, LISTAGG(m.codigo, ', ') WITHIN GROUP (ORDER BY m.codigo) AS materiales FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material GROUP BY cm.id_contenedor;

-- ===== E101 · 6. Agrupar: GROUP BY y HAVING · Promedio de un grupo
-- Objetivo: Peso estimado promedio por estado.
-- Explicación: AVG con GROUP BY.
SELECT estado_verificacion, ROUND(AVG(peso_estimado_kg) * 1000, 1) AS gramos_promedio FROM deposito GROUP BY estado_verificacion ORDER BY 1;

-- ===== E102 · 6. Agrupar: GROUP BY y HAVING · Porcentaje
-- Objetivo: Qué porcentaje de depósitos fue sin sensor.
-- Explicación: Se divide y se multiplica por 100.
SELECT ROUND(100 * COUNT(CASE WHEN sin_sensor = 1 THEN 1 END) / COUNT(*), 1) AS porcentaje_sin_sensor FROM deposito;

-- ===== E103 · 6. Agrupar: GROUP BY y HAVING · Depósitos por estado
-- Objetivo: Cuántos pendientes, verificados y rechazados.
-- Explicación: Agrupar por el código y traducirlo con CASE.
SELECT CASE estado_verificacion WHEN 0 THEN 'PENDIENTE' WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END AS estado, COUNT(*) AS cantidad FROM deposito GROUP BY estado_verificacion ORDER BY estado_verificacion;

-- ===== E104 · 6. Agrupar: GROUP BY y HAVING · Fotos por resultado
-- Objetivo: Cuántas fotos se identificaron y cuántas no.
-- Explicación: resultado es un código; se agrupa igual que cualquier columna.
SELECT resultado, COUNT(*) AS fotos FROM consulta_vision GROUP BY resultado ORDER BY resultado;

-- ===== E105 · 6. Agrupar: GROUP BY y HAVING · Latencia promedio por canal
-- Objetivo: ¿Cuánto tarda Eco en responder?
-- Explicación: AVG ignora los NULL; ROUND deja el número legible.
SELECT canal, ROUND(AVG(latencia_ms)) AS ms_promedio, MAX(latencia_ms) AS ms_maximo FROM consulta_vision GROUP BY canal;

-- ===== E106 · 6. Agrupar: GROUP BY y HAVING · CUBE
-- Objetivo: Subtotales en todas las combinaciones.
-- Explicación: CUBE agrega filas de subtotal por cada columna y el total general.
SELECT sin_sensor, estado_verificacion, COUNT(*) AS n FROM deposito GROUP BY CUBE(sin_sensor, estado_verificacion) ORDER BY sin_sensor, estado_verificacion;

-- ===== E107 · 6. Agrupar: GROUP BY y HAVING · GROUPING
-- Objetivo: Distinguir una fila de subtotal.
-- Explicación: GROUPING(col) = 1 en las filas que ROLLUP agregó.
SELECT DECODE(GROUPING(categoria), 1, 'TOTAL', TO_CHAR(categoria)) AS categoria, COUNT(*) AS materiales FROM material GROUP BY ROLLUP(categoria);

-- ===== E108 · 6. Agrupar: GROUP BY y HAVING · GROUPING SETS
-- Objetivo: Dos agrupaciones en una sola consulta.
-- Explicación: Equivale a dos GROUP BY unidos con UNION ALL.
SELECT canal, estado, COUNT(*) AS n FROM interaccion GROUP BY GROUPING SETS ((canal), (estado)) ORDER BY canal, estado;

-- ===== E109 · 6. Agrupar: GROUP BY y HAVING · HAVING con COUNT DISTINCT
-- Objetivo: Materiales depositados por al menos 3 personas distintas.
-- Explicación: HAVING filtra grupos ya formados.
SELECT d.id_material_declarado, COUNT(DISTINCT i.id_usuario) AS personas FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY d.id_material_declarado HAVING COUNT(DISTINCT i.id_usuario) >= 3 ORDER BY personas DESC;

-- ===== E165 · 10. Funciones analíticas · ROW_NUMBER
-- Objetivo: Numerar los depósitos de cada ciudadano.
-- Explicación: OVER(PARTITION BY ...) reinicia la cuenta por grupo.
SELECT id_titular, id_deposito, ROW_NUMBER() OVER (PARTITION BY id_titular ORDER BY fecha_colombia) AS n FROM vw_deposito_detalle WHERE id_titular IN (SELECT MIN(id_titular) FROM vw_deposito_detalle);

-- ===== E166 · 10. Funciones analíticas · RANK y DENSE_RANK
-- Objetivo: Ranking con empates.
-- Explicación: RANK salta puestos tras un empate; DENSE_RANK no.
SELECT titular, COUNT(*) n, RANK() OVER (ORDER BY COUNT(*) DESC) AS rank, DENSE_RANK() OVER (ORDER BY COUNT(*) DESC) AS dense FROM vw_deposito_detalle WHERE estado_verificacion = 1 GROUP BY titular FETCH FIRST 8 ROWS ONLY;

-- ===== E167 · 10. Funciones analíticas · SUM acumulado
-- Objetivo: Depósitos acumulados por día.
-- Explicación: SUM() OVER (ORDER BY ...) va sumando.
SELECT dia, n, SUM(n) OVER (ORDER BY dia) AS acumulado FROM (SELECT TRUNC(fecha_colombia) dia, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia)) ORDER BY dia DESC FETCH FIRST 7 ROWS ONLY;

-- ===== E168 · 10. Funciones analíticas · LAG
-- Objetivo: Comparar cada semana con la anterior.
-- Explicación: LAG trae el valor de la fila anterior.
SELECT semana, n, LAG(n) OVER (ORDER BY semana) AS semana_anterior, n - LAG(n) OVER (ORDER BY semana) AS diferencia FROM (SELECT TRUNC(fecha_colombia,'IW') semana, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia,'IW')) ORDER BY semana;

-- ===== E169 · 10. Funciones analíticas · Porcentaje del total
-- Objetivo: Participación de cada color.
-- Explicación: RATIO_TO_REPORT calcula la fracción del total.
SELECT color, COUNT(*) n, ROUND(100 * RATIO_TO_REPORT(COUNT(*)) OVER (), 1) AS porcentaje FROM vw_deposito_detalle GROUP BY color;

-- ===== E170 · 10. Funciones analíticas · Primero de cada grupo
-- Objetivo: Primer depósito de cada caneca.
-- Explicación: FIRST_VALUE toma el primero según el orden.
SELECT DISTINCT caneca, FIRST_VALUE(id_deposito) OVER (PARTITION BY caneca ORDER BY fecha_colombia) AS primer_deposito FROM vw_deposito_detalle;

-- ===== E171 · 10. Funciones analíticas · Promedio móvil
-- Objetivo: Promedio de 3 días.
-- Explicación: ROWS BETWEEN 2 PRECEDING AND CURRENT ROW.
SELECT dia, n, ROUND(AVG(n) OVER (ORDER BY dia ROWS BETWEEN 2 PRECEDING AND CURRENT ROW), 2) AS movil_3 FROM (SELECT TRUNC(fecha_colombia) dia, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia)) ORDER BY dia DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E172 · 10. Funciones analíticas · NTILE
-- Objetivo: Dividir ciudadanos en 4 grupos por actividad.
-- Explicación: NTILE(4) reparte en cuartiles.
SELECT titular, n, NTILE(4) OVER (ORDER BY n DESC) AS cuartil FROM (SELECT titular, COUNT(*) n FROM vw_deposito_detalle WHERE titular IS NOT NULL GROUP BY titular) FETCH FIRST 8 ROWS ONLY;

-- ===== E173 · 10. Funciones analíticas · Total por partición
-- Objetivo: Cada depósito junto al total de su material.
-- Explicación: SUM() OVER (PARTITION BY) no colapsa filas como GROUP BY.
SELECT id_deposito, id_material_declarado, COUNT(*) OVER (PARTITION BY id_material_declarado) AS del_mismo_material FROM deposito ORDER BY id_material_declarado, id_deposito FETCH FIRST 10 ROWS ONLY;

-- ===== E174 · 10. Funciones analíticas · LEAD
-- Objetivo: Ver la siguiente foto de la misma persona.
-- Explicación: LEAD mira la fila de adelante dentro de la partición.
SELECT id_usuario, id_consulta, LEAD(id_consulta) OVER (PARTITION BY id_usuario ORDER BY fecha_utc) AS siguiente FROM consulta_vision ORDER BY id_usuario, fecha_utc FETCH FIRST 10 ROWS ONLY;

-- ===== E175 · 10. Funciones analíticas · FIRST_VALUE
-- Objetivo: Primer material que depositó cada persona.
-- Explicación: FIRST_VALUE toma el primero según el orden.
SELECT DISTINCT i.id_usuario, FIRST_VALUE(d.id_material_declarado) OVER (PARTITION BY i.id_usuario ORDER BY d.fecha_deteccion_utc) AS primer_material FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY i.id_usuario FETCH FIRST 8 ROWS ONLY;

-- ===== E176 · 10. Funciones analíticas · RATIO_TO_REPORT
-- Objetivo: Porcentaje de cada estado sobre el total.
-- Explicación: Divide cada valor por la suma de su ventana.
SELECT estado_verificacion, COUNT(*) AS n, ROUND(100 * RATIO_TO_REPORT(COUNT(*)) OVER (), 1) AS porcentaje FROM deposito GROUP BY estado_verificacion ORDER BY estado_verificacion;

-- ===== E177 · 10. Funciones analíticas · PERCENT_RANK
-- Objetivo: Posición relativa de cada peso.
-- Explicación: 0 = el menor, 1 = el mayor.
SELECT id_deposito, peso_estimado_kg, ROUND(PERCENT_RANK() OVER (ORDER BY peso_estimado_kg), 2) AS posicion FROM deposito WHERE peso_estimado_kg IS NOT NULL ORDER BY peso_estimado_kg FETCH FIRST 8 ROWS ONLY;

-- ===== E178 · 10. Funciones analíticas · Contador por persona
-- Objetivo: Numerar las fotos de cada persona.
-- Explicación: COUNT(*) OVER con ORDER BY cuenta acumulado.
SELECT id_usuario, id_consulta, COUNT(*) OVER (PARTITION BY id_usuario ORDER BY id_consulta) AS foto_numero FROM consulta_vision ORDER BY id_usuario, id_consulta FETCH FIRST 10 ROWS ONLY;

-- ===== E179 · 10. Funciones analíticas · Mejor del mes con RANK
-- Objetivo: El puesto de cada persona en el mes por verificados.
-- Explicación: Agrupar primero y numerar después.
SELECT TO_CHAR(fecha_colombia, 'YYYY-MM') AS mes, titular, COUNT(*) AS verificados, RANK() OVER (PARTITION BY TO_CHAR(fecha_colombia, 'YYYY-MM') ORDER BY COUNT(*) DESC) AS puesto FROM vw_deposito_detalle WHERE estado_verificacion = 1 GROUP BY TO_CHAR(fecha_colombia, 'YYYY-MM'), titular ORDER BY mes, puesto FETCH FIRST 8 ROWS ONLY;
