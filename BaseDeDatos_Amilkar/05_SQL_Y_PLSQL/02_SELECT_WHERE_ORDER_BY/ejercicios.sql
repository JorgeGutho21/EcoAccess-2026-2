-- 02_SELECT_WHERE_ORDER_BY — 51 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E015 · 2. SELECT básico · Todas las columnas
-- Objetivo: Ver la tabla ROL completa.
-- Explicación: El asterisco (*) significa «todas las columnas».
SELECT * FROM rol;

-- ===== E016 · 2. SELECT básico · Columnas elegidas
-- Objetivo: Mostrar solo código y nombre de los materiales.
-- Explicación: Escribir las columnas es más claro y más rápido que usar *.
SELECT codigo, nombre FROM material;

-- ===== E017 · 2. SELECT básico · Alias de columna
-- Objetivo: Cambiar el título de una columna en el resultado.
-- Explicación: AS pone un nombre más amigable; no cambia la tabla.
SELECT codigo AS "Código", nombre AS "Material" FROM material WHERE categoria = 2;

-- ===== E018 · 2. SELECT básico · Columnas calculadas
-- Objetivo: Pasar kilogramos a gramos.
-- Explicación: Se puede operar con columnas: + - * /.
SELECT clave_objeto, gramos_tipico, gramos_tipico / 1000 AS kilos_tipicos FROM peso_referencial WHERE id_material = 101;

-- ===== E019 · 2. SELECT básico · Concatenar texto
-- Objetivo: Unir dos textos en una frase.
-- Explicación: El operador 
SELECT nombre || ' va en la caneca ' || DECODE(categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 'ESPECIAL') AS frase FROM material WHERE ROWNUM <= 5;

-- ===== E020 · 2. SELECT básico · Contar filas
-- Objetivo: ¿Cuántos depósitos hay?
-- Explicación: COUNT(*) cuenta filas.
SELECT COUNT(*) AS depositos FROM deposito;

-- ===== E021 · 2. SELECT básico · Ver una vista
-- Objetivo: Consultar una vista igual que una tabla.
-- Explicación: Una vista es una consulta guardada con nombre.
SELECT estacion, caneca, color, estado_texto FROM vw_caneca_estacion ORDER BY id_contenedor;

-- ===== E022 · 2. SELECT básico · Valores distintos
-- Objetivo: ¿Qué canales se usaron?
-- Explicación: DISTINCT elimina repetidos.
SELECT DISTINCT canal FROM interaccion ORDER BY canal;

-- ===== E023 · 2. SELECT básico · Primeras filas
-- Objetivo: Ver solo 5 consultas de visión.
-- Explicación: FETCH FIRST n ROWS ONLY limita el resultado.
SELECT id_consulta, clave_objeto, peso_estimado_kg FROM consulta_vision ORDER BY id_consulta FETCH FIRST 5 ROWS ONLY;

-- ===== E024 · 2. SELECT básico · Expresión CASE
-- Objetivo: Traducir el estado de verificación.
-- Explicación: CASE es un «si… entonces…» dentro de SQL.
SELECT id_deposito, CASE estado_verificacion WHEN 0 THEN 'Pendiente' WHEN 1 THEN 'Verificado' ELSE 'Rechazado' END AS estado FROM deposito FETCH FIRST 6 ROWS ONLY;

-- ===== E025 · 2. SELECT básico · Estaciones
-- Objetivo: Ver las estaciones y su QR.
-- Explicación: PUNTO_INSTALACION es la estación física; el QR identifica la estación, no la caneca.
SELECT id_punto_instalacion, nombre, codigo_qr_estacion, activo FROM punto_instalacion;

-- ===== E026 · 2. SELECT básico · Canecas
-- Objetivo: Ver las canecas con su color en número.
-- Explicación: categoria: 1 BLANCA, 2 VERDE, 3 NEGRA.
SELECT id_contenedor, nombre, categoria, estado FROM contenedor ORDER BY id_contenedor;

-- ===== E027 · 2. SELECT básico · Catálogo de pesos
-- Objetivo: Ver los gramos típicos de algunos objetos.
-- Explicación: PESO_REFERENCIAL sirve para ESTIMAR, nunca para medir.
SELECT clave_objeto, estado_visual, gramos_minimo, gramos_tipico, gramos_maximo FROM peso_referencial FETCH FIRST 8 ROWS ONLY;

-- ===== E028 · 2. SELECT básico · Tarifas de EcoPuntos
-- Objetivo: Ver las reglas de puntos.
-- Explicación: Las tarifas de esta base son SINTÉTICAS (es_sintetica = 1).
SELECT id_regla, id_material, puntos, por_unidad, es_sintetica FROM regla_ecopuntos ORDER BY id_material, desde_utc;

-- ===== E029 · 2. SELECT básico · Literal y expresión
-- Objetivo: Mostrar un texto fijo junto a cada material.
-- Explicación: Se puede seleccionar un valor que no está en la tabla.
SELECT codigo, 'pertenece al catálogo EcoAccess' AS nota FROM material WHERE ROWNUM <= 5;

-- ===== E030 · 2. SELECT básico · Vista de saldos
-- Objetivo: Ver los saldos de EcoPuntos ya calculados.
-- Explicación: La vista suma el libro de movimientos; el saldo no se guarda en ninguna tabla.
SELECT nombre, saldo, acreditados, revertidos FROM vw_saldo_ecopuntos WHERE saldo > 0 ORDER BY saldo DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E031 · 3. Filtros con WHERE · Igualdad
-- Objetivo: Materiales de la caneca verde.
-- Explicación: WHERE deja pasar solo las filas que cumplen la condición.
SELECT codigo, nombre FROM material WHERE categoria = 2;

-- ===== E032 · 3. Filtros con WHERE · Distinto
-- Objetivo: Materiales que NO son peligrosos.
-- Explicación: <> significa «distinto de».
SELECT codigo FROM material WHERE categoria <> 4 ORDER BY codigo;

-- ===== E033 · 3. Filtros con WHERE · Mayor que
-- Objetivo: Pesos típicos de más de 100 g.
-- Explicación: Comparadores: > < >= <=.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico > 100 ORDER BY gramos_tipico;

-- ===== E034 · 3. Filtros con WHERE · AND
-- Objetivo: Depósitos verificados y medidos con báscula.
-- Explicación: AND exige que se cumplan las dos condiciones.
SELECT id_deposito, peso_estimado_kg, peso_medido_kg FROM deposito WHERE estado_verificacion = 1 AND peso_medido_kg IS NOT NULL;

-- ===== E035 · 3. Filtros con WHERE · OR
-- Objetivo: Depósitos pendientes o rechazados.
-- Explicación: OR basta con que se cumpla una.
SELECT COUNT(*) AS por_revisar FROM deposito WHERE estado_verificacion = 0 OR estado_verificacion = 2;

-- ===== E036 · 3. Filtros con WHERE · IN
-- Objetivo: Lo mismo que el anterior, más corto.
-- Explicación: IN compara contra una lista.
SELECT COUNT(*) AS por_revisar FROM deposito WHERE estado_verificacion IN (0, 2);

-- ===== E037 · 3. Filtros con WHERE · BETWEEN
-- Objetivo: Pesos típicos entre 10 y 50 g.
-- Explicación: BETWEEN incluye los dos extremos.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico BETWEEN 10 AND 50 ORDER BY gramos_tipico;

-- ===== E038 · 3. Filtros con WHERE · LIKE que empieza
-- Objetivo: Materiales cuyo código empieza por C.
-- Explicación: % significa «cualquier texto».
SELECT codigo FROM material WHERE codigo LIKE 'C%';

-- ===== E039 · 3. Filtros con WHERE · LIKE que contiene
-- Objetivo: Nombres que contienen «Gómez».
-- Explicación: '%texto%' busca en cualquier posición.
SELECT nombre FROM usuario WHERE nombre LIKE '%Gómez%' ORDER BY nombre;

-- ===== E040 · 3. Filtros con WHERE · IS NULL
-- Objetivo: Depósitos sin peso medido.
-- Explicación: Para preguntar por NULL se usa IS NULL, nunca = NULL.
SELECT COUNT(*) AS sin_medicion FROM deposito WHERE peso_medido_kg IS NULL;

-- ===== E041 · 3. Filtros con WHERE · El error de = NULL
-- Objetivo: Ver por qué = NULL no funciona.
-- Explicación: NULL = NULL no es verdadero sino «desconocido»: devuelve 0 filas.
SELECT COUNT(*) AS filas FROM deposito WHERE peso_medido_kg = NULL;

-- ===== E042 · 3. Filtros con WHERE · IS NOT NULL
-- Objetivo: Consultas de foto hechas dentro de una sesión QR.
-- Explicación: IS NOT NULL = tiene valor.
SELECT COUNT(*) AS con_qr FROM consulta_vision WHERE id_interaccion IS NOT NULL;

-- ===== E043 · 3. Filtros con WHERE · NOT
-- Objetivo: Usuarios que NO son sintéticos.
-- Explicación: NOT invierte la condición (en esta base no hay reales: 0).
SELECT COUNT(*) AS reales FROM usuario WHERE NOT es_sintetico = 1;

-- ===== E044 · 3. Filtros con WHERE · Filtro por fecha
-- Objetivo: Depósitos de los últimos 7 días.
-- Explicación: Restar un INTERVAL a la hora actual da el límite.
SELECT COUNT(*) AS ultimos_7_dias FROM deposito WHERE fecha_deteccion_utc >= SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '7' DAY;

-- ===== E045 · 3. Filtros con WHERE · Paréntesis
-- Objetivo: Prioridad entre AND y OR.
-- Explicación: AND se evalúa antes que OR: los paréntesis dejan clara la intención.
SELECT COUNT(*) FROM deposito WHERE sin_sensor = 1 AND (estado_verificacion = 0 OR estado_verificacion = 2);

-- ===== E046 · 3. Filtros con WHERE · Depósitos sin sensor
-- Objetivo: Ver los depósitos declarados sin sensor.
-- Explicación: sin_sensor = 1: la caneca cerró sin detectar el objeto.
SELECT id_deposito, id_material_declarado, estado_verificacion FROM deposito WHERE sin_sensor = 1 FETCH FIRST 8 ROWS ONLY;

-- ===== E047 · 3. Filtros con WHERE · Depósitos con peso medido
-- Objetivo: Ver solo los que pasaron por una báscula.
-- Explicación: IS NOT NULL encuentra los que sí tienen medición.
SELECT id_deposito, peso_estimado_kg, peso_medido_kg, instrumento_medicion FROM deposito WHERE peso_medido_kg IS NOT NULL;

-- ===== E048 · 3. Filtros con WHERE · Alta confianza de la IA
-- Objetivo: Sesiones donde la sugerencia superó 90 %.
-- Explicación: confianza_sugerida va de 0 a 1.
SELECT id_interaccion, id_material_sugerido, confianza_sugerida FROM interaccion WHERE confianza_sugerida >= 0.9 FETCH FIRST 8 ROWS ONLY;

-- ===== E049 · 3. Filtros con WHERE · LIKE con ESCAPE
-- Objetivo: Códigos que contienen un guion bajo de verdad.
-- Explicación: En LIKE, _ significa «cualquier letra»; con ESCAPE se busca el carácter literal.
SELECT codigo FROM material WHERE codigo LIKE '%\_%' ESCAPE '\' ORDER BY codigo;

-- ===== E050 · 3. Filtros con WHERE · NOT IN con lista
-- Objetivo: Materiales que NO son de la caneca blanca.
-- Explicación: NOT IN (lista) excluye varios valores.
SELECT codigo, categoria FROM material WHERE categoria NOT IN (1) ORDER BY categoria, codigo;

-- ===== E051 · 3. Filtros con WHERE · Canal simulador
-- Objetivo: Recorridos hechos con el simulador.
-- Explicación: canal 3 = Simulador, igual que el enum de C#.
SELECT COUNT(*) AS recorridos_simulador FROM interaccion WHERE canal = 3;

-- ===== E052 · 4. Ordenar y limitar · ORDER BY ascendente
-- Objetivo: Materiales en orden alfabético.
-- Explicación: ASC es el orden por defecto.
SELECT nombre FROM material ORDER BY nombre;

-- ===== E053 · 4. Ordenar y limitar · ORDER BY descendente
-- Objetivo: Objetos más pesados primero.
-- Explicación: DESC invierte el orden.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial ORDER BY gramos_tipico DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E054 · 4. Ordenar y limitar · Dos criterios
-- Objetivo: Ordenar por color y luego por nombre.
-- Explicación: Si empatan en el primero, decide el segundo.
SELECT categoria, nombre FROM material ORDER BY categoria, nombre;

-- ===== E055 · 4. Ordenar y limitar · NULLS LAST
-- Objetivo: Pesos medidos, dejando los vacíos al final.
-- Explicación: Por defecto los NULL van al final en ASC; se puede elegir.
SELECT id_deposito, peso_medido_kg FROM deposito ORDER BY peso_medido_kg DESC NULLS LAST FETCH FIRST 5 ROWS ONLY;

-- ===== E056 · 4. Ordenar y limitar · Ordenar por alias
-- Objetivo: Ordenar por una columna calculada.
-- Explicación: Se puede ordenar por el alias.
SELECT clave_objeto, gramos_maximo - gramos_minimo AS rango FROM peso_referencial ORDER BY rango DESC FETCH FIRST 3 ROWS ONLY;

-- ===== E057 · 4. Ordenar y limitar · Paginación
-- Objetivo: Ver la «página 2» de materiales (filas 6 a 10).
-- Explicación: OFFSET salta filas; útil en pantallas con páginas.
SELECT codigo FROM material ORDER BY codigo OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY;

-- ===== E058 · 4. Ordenar y limitar · DISTINCT con varias columnas
-- Objetivo: Combinaciones únicas de canal y modo.
-- Explicación: DISTINCT mira la fila completa.
SELECT DISTINCT canal, modo FROM consulta_vision ORDER BY canal, modo;

-- ===== E059 · 4. Ordenar y limitar · Empates con WITH TIES
-- Objetivo: Top 3 de pesos incluyendo empates.
-- Explicación: WITH TIES agrega las filas que empatan con la última.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial ORDER BY gramos_tipico FETCH FIRST 3 ROWS WITH TIES;

-- ===== E060 · 4. Ordenar y limitar · Orden personalizado
-- Objetivo: Ordenar canecas: verde, blanca, negra.
-- Explicación: CASE dentro de ORDER BY permite cualquier orden.
SELECT nombre, categoria FROM contenedor WHERE id_punto_instalacion = 1 ORDER BY CASE categoria WHEN 2 THEN 1 WHEN 1 THEN 2 ELSE 3 END;

-- ===== E061 · 4. Ordenar y limitar · OFFSET: segunda página
-- Objetivo: Ver los materiales 6 a 10.
-- Explicación: OFFSET salta filas; FETCH NEXT toma las siguientes.
SELECT codigo FROM material ORDER BY codigo OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY;

-- ===== E062 · 4. Ordenar y limitar · Ordenar por posición
-- Objetivo: ORDER BY 2 ordena por la segunda columna.
-- Explicación: Funciona, pero es menos claro que usar el nombre.
SELECT codigo, nombre FROM material ORDER BY 2 FETCH FIRST 5 ROWS ONLY;

-- ===== E063 · 4. Ordenar y limitar · NULLS FIRST
-- Objetivo: Poner primero los depósitos sin verificador.
-- Explicación: Por defecto, en ASC los NULL van al final.
SELECT id_deposito, id_verificador FROM deposito ORDER BY id_verificador NULLS FIRST FETCH FIRST 6 ROWS ONLY;

-- ===== E064 · 4. Ordenar y limitar · Top por porcentaje
-- Objetivo: El 10 % de depósitos más pesados (estimado).
-- Explicación: FETCH FIRST n PERCENT toma un porcentaje de las filas.
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg IS NOT NULL ORDER BY peso_estimado_kg DESC FETCH FIRST 10 PERCENT ROWS ONLY;

-- ===== E065 · 4. Ordenar y limitar · Últimas fotos
-- Objetivo: Las 5 consultas de foto más recientes.
-- Explicación: DESC pone primero lo más nuevo.
SELECT id_consulta, resultado, fecha_utc FROM consulta_vision ORDER BY fecha_utc DESC FETCH FIRST 5 ROWS ONLY;
