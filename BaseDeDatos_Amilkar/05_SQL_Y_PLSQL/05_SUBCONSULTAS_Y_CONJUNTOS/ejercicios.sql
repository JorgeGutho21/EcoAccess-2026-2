-- 05_SUBCONSULTAS_Y_CONJUNTOS — 32 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E133 · 8. Subconsultas · Subconsulta escalar
-- Objetivo: Depósitos por encima del peso promedio.
-- Explicación: La consulta interna devuelve un solo valor.
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg > (SELECT AVG(peso_estimado_kg) FROM deposito) ORDER BY peso_estimado_kg DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E134 · 8. Subconsultas · IN con subconsulta
-- Objetivo: Usuarios que tienen rol Operario.
-- Explicación: La interna devuelve una lista.
SELECT nombre FROM usuario WHERE id_usuario IN (SELECT ur.id_usuario FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = 2);

-- ===== E135 · 8. Subconsultas · NOT IN
-- Objetivo: Ciudadanos sin ninguna sesión.
-- Explicación: NOT IN excluye la lista (cuidado con NULL: se filtran).
SELECT COUNT(*) FROM usuario WHERE id_usuario NOT IN (SELECT id_usuario FROM interaccion WHERE id_usuario IS NOT NULL);

-- ===== E136 · 8. Subconsultas · EXISTS
-- Objetivo: Canecas que tienen al menos un depósito.
-- Explicación: EXISTS pregunta «¿hay al menos una fila?».
SELECT c.nombre FROM contenedor c WHERE EXISTS (SELECT 1 FROM orden_apertura o JOIN deposito d ON d.id_orden = o.id_orden WHERE o.id_contenedor = c.id_contenedor);

-- ===== E137 · 8. Subconsultas · NOT EXISTS
-- Objetivo: Materiales sin peso de referencia.
-- Explicación: Útil para encontrar huecos en el catálogo.
SELECT m.codigo FROM material m WHERE NOT EXISTS (SELECT 1 FROM peso_referencial p WHERE p.id_material = m.id_material);

-- ===== E138 · 8. Subconsultas · Subconsulta correlacionada
-- Objetivo: Último depósito de cada caneca.
-- Explicación: La interna se repite por cada fila de la externa.
SELECT c.nombre, (SELECT MAX(d.fecha_deteccion_utc) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden WHERE o.id_contenedor = c.id_contenedor) AS ultimo FROM contenedor c;

-- ===== E139 · 8. Subconsultas · Subconsulta en FROM
-- Objetivo: Promedio de depósitos por ciudadano.
-- Explicación: Una consulta puede ser la «tabla» de otra.
SELECT ROUND(AVG(n), 2) AS promedio_por_ciudadano FROM (SELECT i.id_usuario, COUNT(*) n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario);

-- ===== E140 · 8. Subconsultas · WITH (CTE)
-- Objetivo: La misma idea, más legible.
-- Explicación: WITH le pone nombre a una subconsulta.
WITH por_ciudadano AS (SELECT i.id_usuario, COUNT(*) n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT MAX(n) AS maximo, MIN(n) AS minimo FROM por_ciudadano;

-- ===== E141 · 8. Subconsultas · ANY / ALL
-- Objetivo: Pesos mayores que TODOS los de la caneca verde.
-- Explicación: ALL compara contra todos los valores.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico > ALL (SELECT gramos_tipico FROM peso_referencial p JOIN material m USING (id_material) WHERE m.categoria = 2);

-- ===== E142 · 8. Subconsultas · Subconsulta con HAVING
-- Objetivo: Ciudadanos con más depósitos que el promedio.
-- Explicación: Se compara un grupo contra un promedio.
SELECT i.id_usuario, COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario HAVING COUNT(*) > (SELECT COUNT(*) / COUNT(DISTINCT i2.id_usuario) FROM deposito d2 JOIN orden_apertura o2 ON o2.id_orden = d2.id_orden JOIN interaccion i2 ON i2.id_interaccion = o2.id_interaccion) ORDER BY 2 DESC;

-- ===== E143 · 8. Subconsultas · Dos CTE
-- Objetivo: Fotos y depósitos por ciudadano lado a lado.
-- Explicación: Comparar orientación vs reciclaje.
WITH f AS (SELECT id_usuario, COUNT(*) fotos FROM consulta_vision GROUP BY id_usuario), d AS (SELECT i.id_usuario, COUNT(*) deps FROM deposito x JOIN orden_apertura o ON o.id_orden = x.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT f.id_usuario, f.fotos, NVL(d.deps, 0) deps FROM f LEFT JOIN d ON d.id_usuario = f.id_usuario WHERE f.id_usuario IS NOT NULL ORDER BY f.fotos DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E144 · 8. Subconsultas · Subconsulta en UPDATE (con ROLLBACK)
-- Objetivo: Cambiar un valor usando otra tabla.
-- Explicación: Al final ROLLBACK deja todo como estaba.
UPDATE material SET descripcion = descripcion || ' (revisado)' WHERE id_material IN (SELECT id_material FROM peso_referencial WHERE gramos_tipico > 500);
ROLLBACK;

-- ===== E145 · 8. Subconsultas · El depósito más pesado
-- Objetivo: El depósito con mayor peso estimado.
-- Explicación: La subconsulta calcula el máximo; la externa busca quién lo tiene.
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg = (SELECT MAX(peso_estimado_kg) FROM deposito);

-- ===== E146 · 8. Subconsultas · Personas con más de 3 depósitos
-- Objetivo: IN con una subconsulta agrupada.
-- Explicación: La subconsulta devuelve una lista de ids.
SELECT nombre FROM usuario WHERE id_usuario IN (SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario HAVING COUNT(*) > 3) ORDER BY nombre;

-- ===== E147 · 8. Subconsultas · Materiales nunca depositados
-- Objetivo: NOT EXISTS: lo que nunca pasó.
-- Explicación: NOT EXISTS es seguro aunque haya NULL (a diferencia de NOT IN).
SELECT m.codigo FROM material m WHERE NOT EXISTS (SELECT 1 FROM deposito d WHERE d.id_material_declarado = m.id_material) ORDER BY m.codigo;

-- ===== E148 · 8. Subconsultas · Sobre el promedio de su material
-- Objetivo: Correlacionada: comparar con el promedio del mismo material.
-- Explicación: La subconsulta se repite para cada fila externa.
SELECT d.id_deposito, d.id_material_declarado, d.peso_estimado_kg FROM deposito d WHERE d.peso_estimado_kg > (SELECT AVG(d2.peso_estimado_kg) FROM deposito d2 WHERE d2.id_material_declarado = d.id_material_declarado) ORDER BY d.id_deposito FETCH FIRST 8 ROWS ONLY;

-- ===== E149 · 8. Subconsultas · Subconsulta en la lista SELECT
-- Objetivo: Cuántas fotos tiene cada persona.
-- Explicación: Una subconsulta escalar por fila.
SELECT u.nombre, (SELECT COUNT(*) FROM consulta_vision cv WHERE cv.id_usuario = u.id_usuario) AS fotos FROM usuario u ORDER BY fotos DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E150 · 8. Subconsultas · IN con dos columnas
-- Objetivo: Pares caneca-material admitidos.
-- Explicación: Se pueden comparar varias columnas a la vez.
SELECT id_contenedor, id_material FROM contenedor_material WHERE (id_contenedor, id_material) IN (SELECT 11, 101 FROM dual UNION ALL SELECT 12, 201 FROM dual);

-- ===== E151 · 8. Subconsultas · Vista en línea con ranking
-- Objetivo: Top 3 materiales verificados.
-- Explicación: La subconsulta en FROM actúa como una tabla temporal.
SELECT * FROM (SELECT id_material_verificado, COUNT(*) AS n FROM deposito WHERE estado_verificacion = 1 GROUP BY id_material_verificado ORDER BY n DESC) WHERE ROWNUM <= 3;

-- ===== E152 · 8. Subconsultas · CTE con varios pasos
-- Objetivo: Fotos, depósitos y razón por persona.
-- Explicación: WITH ordena el razonamiento en pasos con nombre.
WITH f AS (SELECT id_usuario, COUNT(*) AS fotos FROM consulta_vision GROUP BY id_usuario), d AS (SELECT i.id_usuario, COUNT(*) AS depositos FROM deposito x JOIN orden_apertura o ON o.id_orden = x.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT f.id_usuario, f.fotos, NVL(d.depositos, 0) AS depositos FROM f LEFT JOIN d ON d.id_usuario = f.id_usuario ORDER BY f.fotos DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E153 · 9. Operaciones de conjuntos · UNION
-- Objetivo: Lista única de materiales declarados o verificados.
-- Explicación: UNION quita repetidos.
SELECT id_material_declarado AS id FROM deposito UNION SELECT id_material_verificado FROM deposito WHERE id_material_verificado IS NOT NULL ORDER BY 1;

-- ===== E154 · 9. Operaciones de conjuntos · UNION ALL
-- Objetivo: Embudo de evidencias en una tabla.
-- Explicación: UNION ALL no quita repetidos y es más rápido.
SELECT 'fotos' etapa, COUNT(*) n FROM consulta_vision UNION ALL SELECT 'ordenes', COUNT(*) FROM orden_apertura UNION ALL SELECT 'depositos', COUNT(*) FROM deposito;

-- ===== E155 · 9. Operaciones de conjuntos · INTERSECT
-- Objetivo: Materiales que fueron declarados Y verificados.
-- Explicación: INTERSECT deja lo común.
SELECT id_material_declarado FROM deposito INTERSECT SELECT id_material_verificado FROM deposito ORDER BY 1;

-- ===== E156 · 9. Operaciones de conjuntos · MINUS
-- Objetivo: Ciudadanos con fotos pero sin depósitos.
-- Explicación: MINUS resta conjuntos.
SELECT id_usuario FROM consulta_vision WHERE id_usuario IS NOT NULL MINUS SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1;

-- ===== E157 · 9. Operaciones de conjuntos · UNION de distintas tablas
-- Objetivo: Bitácora única de cambios (auditoría + historial).
-- Explicación: Las columnas deben coincidir en número y tipo.
SELECT 'DEPOSITO' AS objeto, id_deposito AS id, operacion AS cambio, fecha_utc FROM auditoria_deposito WHERE operacion <> 'VERIFICACION' UNION ALL SELECT 'CANECA', id_contenedor, estado_anterior || '->' || estado_nuevo, fecha_utc FROM historial_estado_contenedor ORDER BY fecha_utc;

-- ===== E158 · 9. Operaciones de conjuntos · Verificar con MINUS
-- Objetivo: Comprobar que la vista coincide con la tabla.
-- Explicación: Si MINUS no devuelve filas, son iguales.
SELECT id_deposito FROM deposito MINUS SELECT id_deposito FROM vw_deposito_detalle;

-- ===== E159 · 9. Operaciones de conjuntos · Sugerido o confirmado
-- Objetivo: Materiales que la IA sugirió o el ciudadano confirmó.
-- Explicación: UNION quita los repetidos.
SELECT id_material_sugerido AS id_material FROM interaccion WHERE id_material_sugerido IS NOT NULL UNION SELECT id_material_confirmado FROM interaccion WHERE id_material_confirmado IS NOT NULL ORDER BY 1;

-- ===== E160 · 9. Operaciones de conjuntos · Con foto y con depósito
-- Objetivo: Personas que hicieron las dos cosas.
-- Explicación: INTERSECT deja solo lo común.
SELECT id_usuario FROM consulta_vision INTERSECT SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1 FETCH FIRST 10 ROWS ONLY;

-- ===== E161 · 9. Operaciones de conjuntos · Solo fotos, sin depósito
-- Objetivo: Personas que preguntaron pero no depositaron.
-- Explicación: MINUS = el primer conjunto menos el segundo.
SELECT id_usuario FROM consulta_vision MINUS SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1;

-- ===== E162 · 9. Operaciones de conjuntos · Embudo con UNION ALL
-- Objetivo: Contar cada etapa en una sola tabla.
-- Explicación: UNION ALL pega filas sin quitar repetidos (y es más rápido).
SELECT '1 Fotos' AS etapa, COUNT(*) AS n FROM consulta_vision UNION ALL SELECT '2 Sesiones', COUNT(*) FROM interaccion UNION ALL SELECT '3 Órdenes', COUNT(*) FROM orden_apertura UNION ALL SELECT '4 Depósitos', COUNT(*) FROM deposito UNION ALL SELECT '5 Verificados', COUNT(*) FROM deposito WHERE estado_verificacion = 1 ORDER BY 1;

-- ===== E163 · 9. Operaciones de conjuntos · Colores sin depósitos
-- Objetivo: Categorías de material sin ningún depósito.
-- Explicación: MINUS entre catálogo y uso.
SELECT categoria FROM material MINUS SELECT m.categoria FROM deposito d JOIN material m ON m.id_material = d.id_material_declarado;

-- ===== E164 · 9. Operaciones de conjuntos · Reglas de UNION
-- Objetivo: Las columnas deben coincidir en número y tipo.
-- Explicación: Se usan alias en la primera consulta; ORDER BY va al final.
SELECT codigo AS texto, 'material' AS origen FROM material WHERE categoria = 3 UNION SELECT nombre, 'caneca' FROM contenedor WHERE categoria = 3 ORDER BY origen, texto;
