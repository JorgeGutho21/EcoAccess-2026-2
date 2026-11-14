-- 03_JOIN — 23 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E110 · 7. Uniones (JOIN) · INNER JOIN
-- Objetivo: Canecas con el nombre de su estación.
-- Explicación: JOIN une filas que coinciden en la clave.
SELECT c.nombre AS caneca, p.nombre AS estacion FROM contenedor c JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion;

-- ===== E111 · 7. Uniones (JOIN) · JOIN con tabla puente
-- Objetivo: Roles de cada persona del personal.
-- Explicación: USUARIO_ROL une USUARIO y ROL (N:M).
SELECT u.nombre, r.descripcion FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo IN (2, 3) ORDER BY u.nombre;

-- ===== E112 · 7. Uniones (JOIN) · Varios JOIN
-- Objetivo: Depósito con titular, caneca y material.
-- Explicación: Se sigue la cadena depósito → orden → sesión → usuario.
SELECT d.id_deposito, u.nombre, c.nombre AS caneca, m.codigo FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario u ON u.id_usuario = i.id_usuario JOIN contenedor c ON c.id_contenedor = o.id_contenedor JOIN material m ON m.id_material = d.id_material_declarado FETCH FIRST 5 ROWS ONLY;

-- ===== E113 · 7. Uniones (JOIN) · LEFT JOIN
-- Objetivo: Todos los materiales y cuántas veces se verificaron (incluye 0).
-- Explicación: LEFT JOIN conserva las filas de la izquierda aunque no tengan pareja.
SELECT m.codigo, COUNT(d.id_deposito) AS verificaciones FROM material m LEFT JOIN deposito d ON d.id_material_verificado = m.id_material GROUP BY m.codigo ORDER BY verificaciones DESC, m.codigo;

-- ===== E114 · 7. Uniones (JOIN) · LEFT JOIN + IS NULL
-- Objetivo: Materiales que nunca se han verificado.
-- Explicación: Las filas sin pareja tienen NULL en la tabla derecha.
SELECT m.codigo FROM material m LEFT JOIN deposito d ON d.id_material_verificado = m.id_material WHERE d.id_deposito IS NULL ORDER BY m.codigo;

-- ===== E115 · 7. Uniones (JOIN) · RIGHT JOIN
-- Objetivo: Lo mismo, escrito al revés.
-- Explicación: RIGHT JOIN conserva las de la derecha.
SELECT m.codigo, COUNT(d.id_deposito) AS veces FROM deposito d RIGHT JOIN material m ON m.id_material = d.id_material_declarado GROUP BY m.codigo ORDER BY veces DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E116 · 7. Uniones (JOIN) · FULL OUTER JOIN
-- Objetivo: Canecas y materiales, tengan o no pareja.
-- Explicación: FULL conserva todas las filas de ambos lados.
SELECT c.id_contenedor, m.codigo FROM contenedor_material cm FULL OUTER JOIN material m ON m.id_material = cm.id_material LEFT JOIN contenedor c ON c.id_contenedor = cm.id_contenedor WHERE c.id_contenedor IS NULL;

-- ===== E117 · 7. Uniones (JOIN) · SELF JOIN
-- Objetivo: Crédito y su reverso (la tabla consigo misma).
-- Explicación: Se usa la misma tabla con dos alias.
SELECT r.id_movimiento AS reverso, r.puntos, c.id_movimiento AS credito, c.puntos AS puntos_credito FROM movimiento_ecopuntos r JOIN movimiento_ecopuntos c ON c.id_movimiento = r.id_movimiento_revertido;

-- ===== E118 · 7. Uniones (JOIN) · CROSS JOIN
-- Objetivo: Todas las combinaciones de rol y estación.
-- Explicación: Producto cartesiano: 3 roles × 2 estaciones = 6 filas.
SELECT r.descripcion, p.nombre FROM rol r CROSS JOIN punto_instalacion p;

-- ===== E119 · 7. Uniones (JOIN) · JOIN con USING
-- Objetivo: Sintaxis corta cuando la columna se llama igual.
-- Explicación: USING(columna) evita repetir el ON.
SELECT codigo, clave_objeto FROM material JOIN peso_referencial USING (id_material) WHERE codigo = 'PET';

-- ===== E120 · 7. Uniones (JOIN) · JOIN para verificador
-- Objetivo: Quién verificó cada depósito.
-- Explicación: La misma tabla USUARIO aparece dos veces con papeles distintos.
SELECT d.id_deposito, t.nombre AS titular, v.nombre AS verificador FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario t ON t.id_usuario = i.id_usuario JOIN usuario v ON v.id_usuario = d.id_verificador FETCH FIRST 5 ROWS ONLY;

-- ===== E121 · 7. Uniones (JOIN) · Recorrido completo
-- Objetivo: Sesión → orden → evento → depósito.
-- Explicación: Ver cada evidencia separada.
SELECT id_interaccion, estado_sesion, fotos_consultadas, estado_orden, aperturas_confirmadas, id_deposito FROM vw_recorrido FETCH FIRST 6 ROWS ONLY;

-- ===== E122 · 7. Uniones (JOIN) · JOIN y GROUP BY
-- Objetivo: Depósitos por estación.
-- Explicación: Primero se une, luego se agrupa.
SELECT p.nombre, COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN contenedor c ON c.id_contenedor = o.id_contenedor JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion GROUP BY p.nombre;

-- ===== E123 · 7. Uniones (JOIN) · Eventos por tipo
-- Objetivo: Cuántos eventos de cada tipo.
-- Explicación: Ver aperturas vs detecciones.
SELECT DECODE(tipo,1,'Acuse',2,'Apertura',3,'Detección',4,'Cierre',5,'Fallo',6,'Cierre sin sensor') AS tipo, COUNT(*) FROM evento_dispositivo GROUP BY tipo ORDER BY tipo;

-- ===== E124 · 7. Uniones (JOIN) · Tarifa aplicada
-- Objetivo: Movimiento con su tarifa y material.
-- Explicación: JOIN de 3 tablas del módulo de incentivos.
SELECT m.id_movimiento, mt.codigo, r.puntos AS tarifa, r.por_unidad, m.puntos FROM movimiento_ecopuntos m JOIN regla_ecopuntos r ON r.id_regla = m.id_regla JOIN material mt ON mt.id_material = r.id_material FETCH FIRST 6 ROWS ONLY;

-- ===== E125 · 7. Uniones (JOIN) · Qué recibe cada caneca
-- Objetivo: Nombre de caneca y nombre de material.
-- Explicación: Dos JOIN a través de la tabla puente CONTENEDOR_MATERIAL.
SELECT c.nombre AS caneca, m.nombre AS material FROM contenedor c JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor JOIN material m ON m.id_material = cm.id_material WHERE c.id_punto_instalacion = 1 ORDER BY c.nombre, m.nombre;

-- ===== E126 · 7. Uniones (JOIN) · Pesos con su material
-- Objetivo: Catálogo de pesos con el nombre del material.
-- Explicación: JOIN por id_material.
SELECT m.nombre, p.clave_objeto, p.gramos_tipico FROM peso_referencial p JOIN material m ON m.id_material = p.id_material ORDER BY m.nombre FETCH FIRST 8 ROWS ONLY;

-- ===== E127 · 7. Uniones (JOIN) · Movimientos con el titular
-- Objetivo: Quién recibió cada movimiento de EcoPuntos.
-- Explicación: JOIN con USUARIO para mostrar el nombre.
SELECT u.nombre, mv.tipo, mv.puntos FROM movimiento_ecopuntos mv JOIN usuario u ON u.id_usuario = mv.id_usuario ORDER BY mv.id_movimiento FETCH FIRST 8 ROWS ONLY;

-- ===== E128 · 7. Uniones (JOIN) · Auditoría con el actor
-- Objetivo: Quién hizo cada verificación auditada.
-- Explicación: LEFT JOIN por si el actor quedó NULL.
SELECT a.id_deposito, a.operacion, u.nombre AS actor FROM auditoria_deposito a LEFT JOIN usuario u ON u.id_usuario = a.id_actor ORDER BY a.id_auditoria FETCH FIRST 8 ROWS ONLY;

-- ===== E129 · 7. Uniones (JOIN) · Eventos de una orden
-- Objetivo: Ver acuse, apertura y detección de una orden.
-- Explicación: JOIN de ORDEN_APERTURA con EVENTO_DISPOSITIVO (1:N).
SELECT o.id_orden, e.tipo, e.resultado FROM orden_apertura o JOIN evento_dispositivo e ON e.id_orden = o.id_orden WHERE o.id_orden = (SELECT MIN(id_orden) FROM orden_apertura) ORDER BY e.tipo;

-- ===== E130 · 7. Uniones (JOIN) · Fotos con material (LEFT)
-- Objetivo: Fotos con el material reconocido, si hubo.
-- Explicación: LEFT JOIN conserva las fotos «no es residuo» sin material.
SELECT cv.id_consulta, cv.resultado, m.nombre FROM consulta_vision cv LEFT JOIN material m ON m.id_material = cv.id_material ORDER BY cv.id_consulta FETCH FIRST 8 ROWS ONLY;

-- ===== E131 · 7. Uniones (JOIN) · JOIN no igualitario
-- Objetivo: ¿En qué rango del catálogo cae cada depósito?
-- Explicación: La condición del JOIN puede ser BETWEEN, no solo =.
SELECT d.id_deposito, d.peso_estimado_kg * 1000 AS gramos, p.clave_objeto FROM deposito d JOIN peso_referencial p ON p.id_peso_referencial = d.id_peso_referencial AND d.peso_estimado_kg * 1000 BETWEEN p.gramos_minimo * NVL(d.cantidad_declarada, 1) AND p.gramos_maximo * NVL(d.cantidad_declarada, 1) FETCH FIRST 6 ROWS ONLY;

-- ===== E132 · 7. Uniones (JOIN) · Roles de cada persona
-- Objetivo: Personas con más de un rol.
-- Explicación: JOIN + GROUP BY + HAVING sobre la tabla N:M.
SELECT u.nombre, LISTAGG(r.descripcion, ', ') WITHIN GROUP (ORDER BY r.codigo) AS roles FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol GROUP BY u.nombre HAVING COUNT(*) > 1;
