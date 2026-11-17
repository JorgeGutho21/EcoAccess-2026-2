-- Banco de ejercicios EcoAccess BD — conexión: ECOACCESS_BD
-- GENERADO por generar_banco.ps1 desde ejercicios_fuente.txt. Cada ejercicio se puede ejecutar
-- solo: selecciónalo y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LINESIZE 200
SET PAGESIZE 50
SET FEEDBACK ON

PROMPT <<<E001>>>
-- E001 · 1. Conexión y diccionario de datos · ¿Con qué usuario estoy?
-- Objetivo: Saber quién y dónde estoy conectado antes de hacer cualquier cosa.
-- Explicación: USER devuelve la cuenta Oracle; SYS_CONTEXT pregunta datos de la sesión. DUAL es una tabla de una fila que sirve para hacer cálculos.
SELECT USER AS usuario, SYS_CONTEXT('USERENV','CON_NAME') AS base_de_datos FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E002>>>
-- E002 · 1. Conexión y diccionario de datos · Fecha y hora del servidor
-- Objetivo: Ver la hora del servidor y la de Colombia.
-- Explicación: SYSDATE es la fecha del servidor; AT TIME ZONE convierte a otra zona.
SELECT SYSDATE AS hora_servidor, CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE) AS hora_colombia FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E003>>>
-- E003 · 1. Conexión y diccionario de datos · Mis tablas
-- Objetivo: Listar las tablas de mi esquema.
-- Explicación: USER_TABLES es una vista del diccionario: Oracle guarda ahí la lista de MIS tablas.
SELECT table_name FROM user_tables ORDER BY table_name;
PROMPT <<<FIN>>>

PROMPT <<<E004>>>
-- E004 · 1. Conexión y diccionario de datos · Columnas de una tabla
-- Objetivo: Ver columnas, tipos y si aceptan NULL.
-- Explicación: Es lo mismo que el comando DESCRIBE, pero como consulta.
SELECT column_name, data_type, nullable FROM user_tab_columns WHERE table_name = 'MATERIAL' ORDER BY column_id;
PROMPT <<<FIN>>>

PROMPT <<<E005>>>
-- E005 · 1. Conexión y diccionario de datos · Restricciones de una tabla
-- Objetivo: Ver PK, FK, UNIQUE y CHECK de DEPOSITO.
-- Explicación: constraint_type: P = clave primaria, R = foránea, U = única, C = CHECK o NOT NULL.
SELECT constraint_name, constraint_type FROM user_constraints WHERE table_name = 'DEPOSITO' ORDER BY constraint_type, constraint_name;
PROMPT <<<FIN>>>

PROMPT <<<E006>>>
-- E006 · 1. Conexión y diccionario de datos · Comentario de una columna
-- Objetivo: Leer el diccionario guardado dentro de Oracle.
-- Explicación: COMMENT ON guarda la explicación; USER_COL_COMMENTS la muestra.
SELECT column_name, comments FROM user_col_comments WHERE table_name = 'CONTENEDOR' ORDER BY column_name;
PROMPT <<<FIN>>>

PROMPT <<<E007>>>
-- E007 · 1. Conexión y diccionario de datos · Cuántos objetos hay de cada tipo
-- Objetivo: Inventario del esquema.
-- Explicación: GROUP BY agrupa las filas iguales y COUNT las cuenta.
SELECT object_type, COUNT(*) AS cantidad FROM user_objects GROUP BY object_type ORDER BY object_type;
PROMPT <<<FIN>>>

PROMPT <<<E008>>>
-- E008 · 1. Conexión y diccionario de datos · ¿Hay objetos inválidos?
-- Objetivo: Comprobar que todo compiló.
-- Explicación: Si la consulta no devuelve filas, todo está válido.
SELECT object_name, object_type FROM user_objects WHERE status <> 'VALID';
PROMPT <<<FIN>>>

PROMPT <<<E009>>>
-- E009 · 1. Conexión y diccionario de datos · Índices de una tabla
-- Objetivo: Ver qué índices tiene DEPOSITO y si son únicos.
-- Explicación: USER_INDEXES lista los índices; UNIQUENESS dice si impiden repetidos.
SELECT index_name, uniqueness FROM user_indexes WHERE table_name = 'DEPOSITO' ORDER BY index_name;
PROMPT <<<FIN>>>

PROMPT <<<E010>>>
-- E010 · 1. Conexión y diccionario de datos · Índices funcionales
-- Objetivo: Ver la expresión de los índices únicos de reglas de negocio.
-- Explicación: USER_IND_EXPRESSIONS guarda el CASE que solo indexa filas activas.
SELECT index_name, column_expression FROM user_ind_expressions WHERE index_name LIKE 'UQ_%' ORDER BY index_name;
PROMPT <<<FIN>>>

PROMPT <<<E011>>>
-- E011 · 1. Conexión y diccionario de datos · Columnas IDENTITY
-- Objetivo: Saber qué tablas generan su id solas.
-- Explicación: GENERATION_TYPE: ALWAYS (nadie escribe el id) o BY DEFAULT.
SELECT table_name, column_name, generation_type FROM user_tab_identity_cols ORDER BY table_name;
PROMPT <<<FIN>>>

PROMPT <<<E012>>>
-- E012 · 1. Conexión y diccionario de datos · Columnas de una FK compuesta
-- Objetivo: Ver las dos columnas de una clave foránea.
-- Explicación: USER_CONS_COLUMNS dice qué columnas forman cada restricción y en qué orden.
SELECT c.constraint_name, cc.column_name, cc.position FROM user_constraints c JOIN user_cons_columns cc ON cc.constraint_name = c.constraint_name WHERE c.table_name = 'CONTENEDOR_MATERIAL' AND c.constraint_type = 'R' ORDER BY c.constraint_name, cc.position;
PROMPT <<<FIN>>>

PROMPT <<<E013>>>
-- E013 · 1. Conexión y diccionario de datos · Comentario de las tablas
-- Objetivo: Leer la explicación de cada tabla.
-- Explicación: USER_TAB_COMMENTS guarda el COMMENT ON TABLE.
SELECT table_name, SUBSTR(comments, 1, 70) AS comentario FROM user_tab_comments WHERE table_type = 'TABLE' ORDER BY table_name;
PROMPT <<<FIN>>>

PROMPT <<<E014>>>
-- E014 · 1. Conexión y diccionario de datos · Dependencias de un paquete
-- Objetivo: ¿Qué tablas usa PKG_ECOPUNTOS?
-- Explicación: USER_DEPENDENCIES muestra de qué objetos depende otro.
SELECT DISTINCT referenced_name, referenced_type FROM user_dependencies WHERE name = 'PKG_ECOPUNTOS' AND referenced_owner = USER ORDER BY referenced_type, referenced_name;
PROMPT <<<FIN>>>

PROMPT <<<E015>>>
-- E015 · 2. SELECT básico · Todas las columnas
-- Objetivo: Ver la tabla ROL completa.
-- Explicación: El asterisco (*) significa «todas las columnas».
SELECT * FROM rol;
PROMPT <<<FIN>>>

PROMPT <<<E016>>>
-- E016 · 2. SELECT básico · Columnas elegidas
-- Objetivo: Mostrar solo código y nombre de los materiales.
-- Explicación: Escribir las columnas es más claro y más rápido que usar *.
SELECT codigo, nombre FROM material;
PROMPT <<<FIN>>>

PROMPT <<<E017>>>
-- E017 · 2. SELECT básico · Alias de columna
-- Objetivo: Cambiar el título de una columna en el resultado.
-- Explicación: AS pone un nombre más amigable; no cambia la tabla.
SELECT codigo AS "Código", nombre AS "Material" FROM material WHERE categoria = 2;
PROMPT <<<FIN>>>

PROMPT <<<E018>>>
-- E018 · 2. SELECT básico · Columnas calculadas
-- Objetivo: Pasar kilogramos a gramos.
-- Explicación: Se puede operar con columnas: + - * /.
SELECT clave_objeto, gramos_tipico, gramos_tipico / 1000 AS kilos_tipicos FROM peso_referencial WHERE id_material = 101;
PROMPT <<<FIN>>>

PROMPT <<<E019>>>
-- E019 · 2. SELECT básico · Concatenar texto
-- Objetivo: Unir dos textos en una frase.
-- Explicación: El operador 
SELECT nombre || ' va en la caneca ' || DECODE(categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 'ESPECIAL') AS frase FROM material WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E020>>>
-- E020 · 2. SELECT básico · Contar filas
-- Objetivo: ¿Cuántos depósitos hay?
-- Explicación: COUNT(*) cuenta filas.
SELECT COUNT(*) AS depositos FROM deposito;
PROMPT <<<FIN>>>

PROMPT <<<E021>>>
-- E021 · 2. SELECT básico · Ver una vista
-- Objetivo: Consultar una vista igual que una tabla.
-- Explicación: Una vista es una consulta guardada con nombre.
SELECT estacion, caneca, color, estado_texto FROM vw_caneca_estacion ORDER BY id_contenedor;
PROMPT <<<FIN>>>

PROMPT <<<E022>>>
-- E022 · 2. SELECT básico · Valores distintos
-- Objetivo: ¿Qué canales se usaron?
-- Explicación: DISTINCT elimina repetidos.
SELECT DISTINCT canal FROM interaccion ORDER BY canal;
PROMPT <<<FIN>>>

PROMPT <<<E023>>>
-- E023 · 2. SELECT básico · Primeras filas
-- Objetivo: Ver solo 5 consultas de visión.
-- Explicación: FETCH FIRST n ROWS ONLY limita el resultado.
SELECT id_consulta, clave_objeto, peso_estimado_kg FROM consulta_vision ORDER BY id_consulta FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E024>>>
-- E024 · 2. SELECT básico · Expresión CASE
-- Objetivo: Traducir el estado de verificación.
-- Explicación: CASE es un «si… entonces…» dentro de SQL.
SELECT id_deposito, CASE estado_verificacion WHEN 0 THEN 'Pendiente' WHEN 1 THEN 'Verificado' ELSE 'Rechazado' END AS estado FROM deposito FETCH FIRST 6 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E025>>>
-- E025 · 2. SELECT básico · Estaciones
-- Objetivo: Ver las estaciones y su QR.
-- Explicación: PUNTO_INSTALACION es la estación física; el QR identifica la estación, no la caneca.
SELECT id_punto_instalacion, nombre, codigo_qr_estacion, activo FROM punto_instalacion;
PROMPT <<<FIN>>>

PROMPT <<<E026>>>
-- E026 · 2. SELECT básico · Canecas
-- Objetivo: Ver las canecas con su color en número.
-- Explicación: categoria: 1 BLANCA, 2 VERDE, 3 NEGRA.
SELECT id_contenedor, nombre, categoria, estado FROM contenedor ORDER BY id_contenedor;
PROMPT <<<FIN>>>

PROMPT <<<E027>>>
-- E027 · 2. SELECT básico · Catálogo de pesos
-- Objetivo: Ver los gramos típicos de algunos objetos.
-- Explicación: PESO_REFERENCIAL sirve para ESTIMAR, nunca para medir.
SELECT clave_objeto, estado_visual, gramos_minimo, gramos_tipico, gramos_maximo FROM peso_referencial FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E028>>>
-- E028 · 2. SELECT básico · Tarifas de EcoPuntos
-- Objetivo: Ver las reglas de puntos.
-- Explicación: Las tarifas de esta base son SINTÉTICAS (es_sintetica = 1).
SELECT id_regla, id_material, puntos, por_unidad, es_sintetica FROM regla_ecopuntos ORDER BY id_material, desde_utc;
PROMPT <<<FIN>>>

PROMPT <<<E029>>>
-- E029 · 2. SELECT básico · Literal y expresión
-- Objetivo: Mostrar un texto fijo junto a cada material.
-- Explicación: Se puede seleccionar un valor que no está en la tabla.
SELECT codigo, 'pertenece al catálogo EcoAccess' AS nota FROM material WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E030>>>
-- E030 · 2. SELECT básico · Vista de saldos
-- Objetivo: Ver los saldos de EcoPuntos ya calculados.
-- Explicación: La vista suma el libro de movimientos; el saldo no se guarda en ninguna tabla.
SELECT nombre, saldo, acreditados, revertidos FROM vw_saldo_ecopuntos WHERE saldo > 0 ORDER BY saldo DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E031>>>
-- E031 · 3. Filtros con WHERE · Igualdad
-- Objetivo: Materiales de la caneca verde.
-- Explicación: WHERE deja pasar solo las filas que cumplen la condición.
SELECT codigo, nombre FROM material WHERE categoria = 2;
PROMPT <<<FIN>>>

PROMPT <<<E032>>>
-- E032 · 3. Filtros con WHERE · Distinto
-- Objetivo: Materiales que NO son peligrosos.
-- Explicación: <> significa «distinto de».
SELECT codigo FROM material WHERE categoria <> 4 ORDER BY codigo;
PROMPT <<<FIN>>>

PROMPT <<<E033>>>
-- E033 · 3. Filtros con WHERE · Mayor que
-- Objetivo: Pesos típicos de más de 100 g.
-- Explicación: Comparadores: > < >= <=.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico > 100 ORDER BY gramos_tipico;
PROMPT <<<FIN>>>

PROMPT <<<E034>>>
-- E034 · 3. Filtros con WHERE · AND
-- Objetivo: Depósitos verificados y medidos con báscula.
-- Explicación: AND exige que se cumplan las dos condiciones.
SELECT id_deposito, peso_estimado_kg, peso_medido_kg FROM deposito WHERE estado_verificacion = 1 AND peso_medido_kg IS NOT NULL;
PROMPT <<<FIN>>>

PROMPT <<<E035>>>
-- E035 · 3. Filtros con WHERE · OR
-- Objetivo: Depósitos pendientes o rechazados.
-- Explicación: OR basta con que se cumpla una.
SELECT COUNT(*) AS por_revisar FROM deposito WHERE estado_verificacion = 0 OR estado_verificacion = 2;
PROMPT <<<FIN>>>

PROMPT <<<E036>>>
-- E036 · 3. Filtros con WHERE · IN
-- Objetivo: Lo mismo que el anterior, más corto.
-- Explicación: IN compara contra una lista.
SELECT COUNT(*) AS por_revisar FROM deposito WHERE estado_verificacion IN (0, 2);
PROMPT <<<FIN>>>

PROMPT <<<E037>>>
-- E037 · 3. Filtros con WHERE · BETWEEN
-- Objetivo: Pesos típicos entre 10 y 50 g.
-- Explicación: BETWEEN incluye los dos extremos.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico BETWEEN 10 AND 50 ORDER BY gramos_tipico;
PROMPT <<<FIN>>>

PROMPT <<<E038>>>
-- E038 · 3. Filtros con WHERE · LIKE que empieza
-- Objetivo: Materiales cuyo código empieza por C.
-- Explicación: % significa «cualquier texto».
SELECT codigo FROM material WHERE codigo LIKE 'C%';
PROMPT <<<FIN>>>

PROMPT <<<E039>>>
-- E039 · 3. Filtros con WHERE · LIKE que contiene
-- Objetivo: Nombres que contienen «Gómez».
-- Explicación: '%texto%' busca en cualquier posición.
SELECT nombre FROM usuario WHERE nombre LIKE '%Gómez%' ORDER BY nombre;
PROMPT <<<FIN>>>

PROMPT <<<E040>>>
-- E040 · 3. Filtros con WHERE · IS NULL
-- Objetivo: Depósitos sin peso medido.
-- Explicación: Para preguntar por NULL se usa IS NULL, nunca = NULL.
SELECT COUNT(*) AS sin_medicion FROM deposito WHERE peso_medido_kg IS NULL;
PROMPT <<<FIN>>>

PROMPT <<<E041>>>
-- E041 · 3. Filtros con WHERE · El error de = NULL
-- Objetivo: Ver por qué = NULL no funciona.
-- Explicación: NULL = NULL no es verdadero sino «desconocido»: devuelve 0 filas.
SELECT COUNT(*) AS filas FROM deposito WHERE peso_medido_kg = NULL;
PROMPT <<<FIN>>>

PROMPT <<<E042>>>
-- E042 · 3. Filtros con WHERE · IS NOT NULL
-- Objetivo: Consultas de foto hechas dentro de una sesión QR.
-- Explicación: IS NOT NULL = tiene valor.
SELECT COUNT(*) AS con_qr FROM consulta_vision WHERE id_interaccion IS NOT NULL;
PROMPT <<<FIN>>>

PROMPT <<<E043>>>
-- E043 · 3. Filtros con WHERE · NOT
-- Objetivo: Usuarios que NO son sintéticos.
-- Explicación: NOT invierte la condición (en esta base no hay reales: 0).
SELECT COUNT(*) AS reales FROM usuario WHERE NOT es_sintetico = 1;
PROMPT <<<FIN>>>

PROMPT <<<E044>>>
-- E044 · 3. Filtros con WHERE · Filtro por fecha
-- Objetivo: Depósitos de los últimos 7 días.
-- Explicación: Restar un INTERVAL a la hora actual da el límite.
SELECT COUNT(*) AS ultimos_7_dias FROM deposito WHERE fecha_deteccion_utc >= SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '7' DAY;
PROMPT <<<FIN>>>

PROMPT <<<E045>>>
-- E045 · 3. Filtros con WHERE · Paréntesis
-- Objetivo: Prioridad entre AND y OR.
-- Explicación: AND se evalúa antes que OR: los paréntesis dejan clara la intención.
SELECT COUNT(*) FROM deposito WHERE sin_sensor = 1 AND (estado_verificacion = 0 OR estado_verificacion = 2);
PROMPT <<<FIN>>>

PROMPT <<<E046>>>
-- E046 · 3. Filtros con WHERE · Depósitos sin sensor
-- Objetivo: Ver los depósitos declarados sin sensor.
-- Explicación: sin_sensor = 1: la caneca cerró sin detectar el objeto.
SELECT id_deposito, id_material_declarado, estado_verificacion FROM deposito WHERE sin_sensor = 1 FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E047>>>
-- E047 · 3. Filtros con WHERE · Depósitos con peso medido
-- Objetivo: Ver solo los que pasaron por una báscula.
-- Explicación: IS NOT NULL encuentra los que sí tienen medición.
SELECT id_deposito, peso_estimado_kg, peso_medido_kg, instrumento_medicion FROM deposito WHERE peso_medido_kg IS NOT NULL;
PROMPT <<<FIN>>>

PROMPT <<<E048>>>
-- E048 · 3. Filtros con WHERE · Alta confianza de la IA
-- Objetivo: Sesiones donde la sugerencia superó 90 %.
-- Explicación: confianza_sugerida va de 0 a 1.
SELECT id_interaccion, id_material_sugerido, confianza_sugerida FROM interaccion WHERE confianza_sugerida >= 0.9 FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E049>>>
-- E049 · 3. Filtros con WHERE · LIKE con ESCAPE
-- Objetivo: Códigos que contienen un guion bajo de verdad.
-- Explicación: En LIKE, _ significa «cualquier letra»; con ESCAPE se busca el carácter literal.
SELECT codigo FROM material WHERE codigo LIKE '%\_%' ESCAPE '\' ORDER BY codigo;
PROMPT <<<FIN>>>

PROMPT <<<E050>>>
-- E050 · 3. Filtros con WHERE · NOT IN con lista
-- Objetivo: Materiales que NO son de la caneca blanca.
-- Explicación: NOT IN (lista) excluye varios valores.
SELECT codigo, categoria FROM material WHERE categoria NOT IN (1) ORDER BY categoria, codigo;
PROMPT <<<FIN>>>

PROMPT <<<E051>>>
-- E051 · 3. Filtros con WHERE · Canal simulador
-- Objetivo: Recorridos hechos con el simulador.
-- Explicación: canal 3 = Simulador, igual que el enum de C#.
SELECT COUNT(*) AS recorridos_simulador FROM interaccion WHERE canal = 3;
PROMPT <<<FIN>>>

PROMPT <<<E052>>>
-- E052 · 4. Ordenar y limitar · ORDER BY ascendente
-- Objetivo: Materiales en orden alfabético.
-- Explicación: ASC es el orden por defecto.
SELECT nombre FROM material ORDER BY nombre;
PROMPT <<<FIN>>>

PROMPT <<<E053>>>
-- E053 · 4. Ordenar y limitar · ORDER BY descendente
-- Objetivo: Objetos más pesados primero.
-- Explicación: DESC invierte el orden.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial ORDER BY gramos_tipico DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E054>>>
-- E054 · 4. Ordenar y limitar · Dos criterios
-- Objetivo: Ordenar por color y luego por nombre.
-- Explicación: Si empatan en el primero, decide el segundo.
SELECT categoria, nombre FROM material ORDER BY categoria, nombre;
PROMPT <<<FIN>>>

PROMPT <<<E055>>>
-- E055 · 4. Ordenar y limitar · NULLS LAST
-- Objetivo: Pesos medidos, dejando los vacíos al final.
-- Explicación: Por defecto los NULL van al final en ASC; se puede elegir.
SELECT id_deposito, peso_medido_kg FROM deposito ORDER BY peso_medido_kg DESC NULLS LAST FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E056>>>
-- E056 · 4. Ordenar y limitar · Ordenar por alias
-- Objetivo: Ordenar por una columna calculada.
-- Explicación: Se puede ordenar por el alias.
SELECT clave_objeto, gramos_maximo - gramos_minimo AS rango FROM peso_referencial ORDER BY rango DESC FETCH FIRST 3 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E057>>>
-- E057 · 4. Ordenar y limitar · Paginación
-- Objetivo: Ver la «página 2» de materiales (filas 6 a 10).
-- Explicación: OFFSET salta filas; útil en pantallas con páginas.
SELECT codigo FROM material ORDER BY codigo OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E058>>>
-- E058 · 4. Ordenar y limitar · DISTINCT con varias columnas
-- Objetivo: Combinaciones únicas de canal y modo.
-- Explicación: DISTINCT mira la fila completa.
SELECT DISTINCT canal, modo FROM consulta_vision ORDER BY canal, modo;
PROMPT <<<FIN>>>

PROMPT <<<E059>>>
-- E059 · 4. Ordenar y limitar · Empates con WITH TIES
-- Objetivo: Top 3 de pesos incluyendo empates.
-- Explicación: WITH TIES agrega las filas que empatan con la última.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial ORDER BY gramos_tipico FETCH FIRST 3 ROWS WITH TIES;
PROMPT <<<FIN>>>

PROMPT <<<E060>>>
-- E060 · 4. Ordenar y limitar · Orden personalizado
-- Objetivo: Ordenar canecas: verde, blanca, negra.
-- Explicación: CASE dentro de ORDER BY permite cualquier orden.
SELECT nombre, categoria FROM contenedor WHERE id_punto_instalacion = 1 ORDER BY CASE categoria WHEN 2 THEN 1 WHEN 1 THEN 2 ELSE 3 END;
PROMPT <<<FIN>>>

PROMPT <<<E061>>>
-- E061 · 4. Ordenar y limitar · OFFSET: segunda página
-- Objetivo: Ver los materiales 6 a 10.
-- Explicación: OFFSET salta filas; FETCH NEXT toma las siguientes.
SELECT codigo FROM material ORDER BY codigo OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E062>>>
-- E062 · 4. Ordenar y limitar · Ordenar por posición
-- Objetivo: ORDER BY 2 ordena por la segunda columna.
-- Explicación: Funciona, pero es menos claro que usar el nombre.
SELECT codigo, nombre FROM material ORDER BY 2 FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E063>>>
-- E063 · 4. Ordenar y limitar · NULLS FIRST
-- Objetivo: Poner primero los depósitos sin verificador.
-- Explicación: Por defecto, en ASC los NULL van al final.
SELECT id_deposito, id_verificador FROM deposito ORDER BY id_verificador NULLS FIRST FETCH FIRST 6 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E064>>>
-- E064 · 4. Ordenar y limitar · Top por porcentaje
-- Objetivo: El 10 % de depósitos más pesados (estimado).
-- Explicación: FETCH FIRST n PERCENT toma un porcentaje de las filas.
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg IS NOT NULL ORDER BY peso_estimado_kg DESC FETCH FIRST 10 PERCENT ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E065>>>
-- E065 · 4. Ordenar y limitar · Últimas fotos
-- Objetivo: Las 5 consultas de foto más recientes.
-- Explicación: DESC pone primero lo más nuevo.
SELECT id_consulta, resultado, fecha_utc FROM consulta_vision ORDER BY fecha_utc DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E066>>>
-- E066 · 5. Funciones de una fila · UPPER y LOWER
-- Objetivo: Cambiar mayúsculas.
-- Explicación: UPPER todo en mayúsculas; LOWER en minúsculas.
SELECT UPPER(nombre) AS mayus, LOWER(codigo) AS minusculas FROM material WHERE ROWNUM <= 3;
PROMPT <<<FIN>>>

PROMPT <<<E067>>>
-- E067 · 5. Funciones de una fila · LENGTH
-- Objetivo: Largo de un texto.
-- Explicación: LENGTH cuenta caracteres.
SELECT codigo, LENGTH(codigo) AS letras FROM material ORDER BY letras DESC FETCH FIRST 3 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E068>>>
-- E068 · 5. Funciones de una fila · SUBSTR e INSTR
-- Objetivo: Sacar el usuario de un correo.
-- Explicación: INSTR busca la posición de '@'; SUBSTR corta.
SELECT correo_electronico, SUBSTR(correo_electronico, 1, INSTR(correo_electronico, '@') - 1) AS usuario FROM usuario WHERE ROWNUM <= 3;
PROMPT <<<FIN>>>

PROMPT <<<E069>>>
-- E069 · 5. Funciones de una fila · REPLACE
-- Objetivo: Reemplazar texto.
-- Explicación: REPLACE cambia un texto por otro.
SELECT REPLACE(clave_objeto, '_', ' ') AS legible FROM peso_referencial WHERE ROWNUM <= 3;
PROMPT <<<FIN>>>

PROMPT <<<E070>>>
-- E070 · 5. Funciones de una fila · ROUND y TRUNC
-- Objetivo: Redondear números.
-- Explicación: ROUND redondea; TRUNC corta sin redondear.
SELECT 2.675 AS valor, ROUND(2.675, 2) AS redondeado, TRUNC(2.675, 2) AS truncado FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E071>>>
-- E071 · 5. Funciones de una fila · MOD
-- Objetivo: Residuo de una división.
-- Explicación: MOD(10,3) = 1. Sirve para saber si un número es par.
SELECT id_deposito, MOD(id_deposito, 2) AS es_impar FROM deposito WHERE ROWNUM <= 4;
PROMPT <<<FIN>>>

PROMPT <<<E072>>>
-- E072 · 5. Funciones de una fila · NVL
-- Objetivo: Reemplazar NULL por un valor.
-- Explicación: NVL(x, 0) devuelve 0 cuando x es NULL.
SELECT id_deposito, NVL(peso_medido_kg, 0) AS medido_o_cero FROM deposito WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E073>>>
-- E073 · 5. Funciones de una fila · NVL2
-- Objetivo: Un valor si hay dato, otro si no.
-- Explicación: NVL2(x, si_hay, si_no_hay).
SELECT id_deposito, NVL2(peso_medido_kg, 'Medido', 'Solo estimado') AS tipo_peso FROM deposito WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E074>>>
-- E074 · 5. Funciones de una fila · COALESCE
-- Objetivo: Primer valor no nulo.
-- Explicación: COALESCE(a, b, c) devuelve el primero que tenga dato.
SELECT id_deposito, COALESCE(cantidad_verificada, cantidad_declarada, 1) AS unidades FROM deposito WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E075>>>
-- E075 · 5. Funciones de una fila · TO_CHAR con fechas
-- Objetivo: Mostrar la fecha con formato.
-- Explicación: TO_CHAR convierte fecha en texto con el formato que quieras.
SELECT TO_CHAR(SYSDATE, 'DD/MM/YYYY HH24:MI') AS ahora, TO_CHAR(SYSDATE, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E076>>>
-- E076 · 5. Funciones de una fila · TO_DATE
-- Objetivo: Convertir texto en fecha.
-- Explicación: Hay que decir el formato del texto.
SELECT TO_DATE('20/09/2026', 'DD/MM/YYYY') AS fecha_avance FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E077>>>
-- E077 · 5. Funciones de una fila · Diferencia de fechas
-- Objetivo: Días entre dos fechas.
-- Explicación: Restar dos DATE da días.
SELECT TO_DATE('22/09/2026','DD/MM/YYYY') - TO_DATE('18/09/2026','DD/MM/YYYY') AS dias_del_avance FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E078>>>
-- E078 · 5. Funciones de una fila · ADD_MONTHS y LAST_DAY
-- Objetivo: Fechas de calendario.
-- Explicación: ADD_MONTHS suma meses; LAST_DAY da el último día del mes.
SELECT ADD_MONTHS(TRUNC(SYSDATE), 1) AS en_un_mes, LAST_DAY(SYSDATE) AS fin_de_mes FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E079>>>
-- E079 · 5. Funciones de una fila · DECODE
-- Objetivo: Traducir códigos (estilo Oracle).
-- Explicación: DECODE(valor, si1, entonces1, ..., otro).
SELECT codigo, DECODE(categoria, 1, 'Blanca', 2, 'Verde', 3, 'Negra', 'Ruta especial') AS caneca FROM material WHERE ROWNUM <= 6;
PROMPT <<<FIN>>>

PROMPT <<<E080>>>
-- E080 · 5. Funciones de una fila · Gramos y kilogramos
-- Objetivo: Mostrar el mismo peso en g y kg.
-- Explicación: Unidad clara: 1 kg = 1000 g.
SELECT id_deposito, peso_estimado_kg AS kg, peso_estimado_kg * 1000 AS g FROM deposito WHERE peso_estimado_kg IS NOT NULL AND ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E081>>>
-- E081 · 5. Funciones de una fila · INITCAP
-- Objetivo: Poner en mayúscula la primera letra.
-- Explicación: INITCAP convierte «cartón» en «Cartón».
SELECT codigo, INITCAP(LOWER(codigo)) AS bonito FROM material WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E082>>>
-- E082 · 5. Funciones de una fila · LPAD y RPAD
-- Objetivo: Rellenar para alinear.
-- Explicación: LPAD agrega caracteres a la izquierda: sirve para seudónimos como C-0007.
SELECT 'C-' || LPAD(7, 4, '0') AS seudonimo, RPAD('PET', 8, '.') || '|' AS relleno FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E083>>>
-- E083 · 5. Funciones de una fila · TRIM
-- Objetivo: Quitar espacios sobrantes.
-- Explicación: TRIM limpia al inicio y al final; por eso el CHECK de nombre lo exige.
SELECT '[' || TRIM('   María   ') || ']' AS limpio, LENGTH('   María   ') AS antes, LENGTH(TRIM('   María   ')) AS despues FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E084>>>
-- E084 · 5. Funciones de una fila · GREATEST y LEAST
-- Objetivo: El mayor y el menor de varios valores.
-- Explicación: Comparan columnas de una misma fila.
SELECT clave_objeto, GREATEST(gramos_minimo, gramos_tipico, gramos_maximo) AS mayor, LEAST(gramos_minimo, gramos_tipico, gramos_maximo) AS menor FROM peso_referencial WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E085>>>
-- E085 · 5. Funciones de una fila · CEIL y FLOOR
-- Objetivo: Redondear hacia arriba y hacia abajo.
-- Explicación: CEIL(2.1)=3 y FLOOR(2.9)=2.
SELECT CEIL(2.1) AS techo, FLOOR(2.9) AS piso, CEIL(-2.1) AS techo_negativo FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E086>>>
-- E086 · 5. Funciones de una fila · NULLIF
-- Objetivo: Evitar dividir por cero.
-- Explicación: NULLIF(a, b) devuelve NULL si a = b; dividir por NULL da NULL, no error.
SELECT 10 / NULLIF(0, 0) AS sin_error, NVL(10 / NULLIF(0, 0), 0) AS con_cero FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E087>>>
-- E087 · 5. Funciones de una fila · REGEXP_LIKE
-- Objetivo: Validar un correo con expresión regular.
-- Explicación: Es la misma idea que usa el CHECK del correo.
SELECT correo_electronico FROM usuario WHERE REGEXP_LIKE(correo_electronico, '^[a-z0-9._-]+@ejemplo\.test$') FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E088>>>
-- E088 · 6. Agrupar: GROUP BY y HAVING · COUNT por grupo
-- Objetivo: Depósitos por estado.
-- Explicación: GROUP BY forma un grupo por cada valor.
SELECT estado_verificacion, COUNT(*) AS cantidad FROM deposito GROUP BY estado_verificacion ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E089>>>
-- E089 · 6. Agrupar: GROUP BY y HAVING · SUM
-- Objetivo: Kilogramos estimados verificados.
-- Explicación: SUM suma; los NULL se ignoran.
SELECT ROUND(SUM(peso_estimado_kg), 3) AS kg_estimados FROM deposito WHERE estado_verificacion = 1;
PROMPT <<<FIN>>>

PROMPT <<<E090>>>
-- E090 · 6. Agrupar: GROUP BY y HAVING · AVG, MIN, MAX
-- Objetivo: Estadísticas de latencia de la IA.
-- Explicación: Promedio, mínimo y máximo.
SELECT ROUND(AVG(latencia_ms)) AS promedio, MIN(latencia_ms) AS minimo, MAX(latencia_ms) AS maximo FROM consulta_vision;
PROMPT <<<FIN>>>

PROMPT <<<E091>>>
-- E091 · 6. Agrupar: GROUP BY y HAVING · COUNT(columna) vs COUNT(*)
-- Objetivo: Ver la diferencia con NULL.
-- Explicación: COUNT(columna) no cuenta los NULL.
SELECT COUNT(*) AS todos, COUNT(peso_medido_kg) AS con_medicion FROM deposito;
PROMPT <<<FIN>>>

PROMPT <<<E092>>>
-- E092 · 6. Agrupar: GROUP BY y HAVING · COUNT DISTINCT
-- Objetivo: Ciudadanos distintos con sesión.
-- Explicación: Cuenta valores sin repetir.
SELECT COUNT(DISTINCT id_usuario) AS ciudadanos FROM interaccion;
PROMPT <<<FIN>>>

PROMPT <<<E093>>>
-- E093 · 6. Agrupar: GROUP BY y HAVING · Agrupar por dos columnas
-- Objetivo: Depósitos por sensor y estado.
-- Explicación: Un grupo por cada combinación.
SELECT sin_sensor, estado_verificacion, COUNT(*) FROM deposito GROUP BY sin_sensor, estado_verificacion ORDER BY 1, 2;
PROMPT <<<FIN>>>

PROMPT <<<E094>>>
-- E094 · 6. Agrupar: GROUP BY y HAVING · HAVING
-- Objetivo: Ciudadanos con 3 o más sesiones.
-- Explicación: HAVING filtra GRUPOS (WHERE filtra filas).
SELECT id_usuario, COUNT(*) AS sesiones FROM interaccion WHERE id_usuario IS NOT NULL GROUP BY id_usuario HAVING COUNT(*) >= 3 ORDER BY sesiones DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E095>>>
-- E095 · 6. Agrupar: GROUP BY y HAVING · WHERE + GROUP BY + HAVING
-- Objetivo: Materiales verificados más de 3 veces.
-- Explicación: Orden: WHERE → GROUP BY → HAVING → ORDER BY.
SELECT id_material_verificado, COUNT(*) AS veces FROM deposito WHERE estado_verificacion = 1 GROUP BY id_material_verificado HAVING COUNT(*) > 3 ORDER BY veces DESC;
PROMPT <<<FIN>>>

PROMPT <<<E096>>>
-- E096 · 6. Agrupar: GROUP BY y HAVING · Agrupar por día
-- Objetivo: Depósitos por día (hora Colombia).
-- Explicación: Se convierte a Colombia y TRUNC quita la hora.
SELECT TRUNC(CAST(FROM_TZ(fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia, COUNT(*) FROM deposito GROUP BY TRUNC(CAST(FROM_TZ(fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) ORDER BY dia DESC FETCH FIRST 7 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E097>>>
-- E097 · 6. Agrupar: GROUP BY y HAVING · Agrupar por semana
-- Objetivo: Depósitos por semana ISO.
-- Explicación: 'IW' lleva cada fecha al lunes de su semana.
SELECT TRUNC(fecha_colombia, 'IW') AS semana, COUNT(*) FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia, 'IW') ORDER BY semana;
PROMPT <<<FIN>>>

PROMPT <<<E098>>>
-- E098 · 6. Agrupar: GROUP BY y HAVING · Conteo condicional
-- Objetivo: Verificados y pendientes en una sola fila.
-- Explicación: COUNT(CASE ...) cuenta solo cuando se cumple.
SELECT COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END) AS verificados, COUNT(CASE WHEN estado_verificacion = 0 THEN 1 END) AS pendientes FROM deposito;
PROMPT <<<FIN>>>

PROMPT <<<E099>>>
-- E099 · 6. Agrupar: GROUP BY y HAVING · ROLLUP
-- Objetivo: Subtotales por color y total general.
-- Explicación: ROLLUP agrega filas de subtotal.
SELECT color, estado_texto, COUNT(*) FROM vw_deposito_detalle GROUP BY ROLLUP(color, estado_texto) ORDER BY color, estado_texto;
PROMPT <<<FIN>>>

PROMPT <<<E100>>>
-- E100 · 6. Agrupar: GROUP BY y HAVING · LISTAGG
-- Objetivo: Materiales de cada caneca en una sola línea.
-- Explicación: LISTAGG une textos de un grupo.
SELECT cm.id_contenedor, LISTAGG(m.codigo, ', ') WITHIN GROUP (ORDER BY m.codigo) AS materiales FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material GROUP BY cm.id_contenedor;
PROMPT <<<FIN>>>

PROMPT <<<E101>>>
-- E101 · 6. Agrupar: GROUP BY y HAVING · Promedio de un grupo
-- Objetivo: Peso estimado promedio por estado.
-- Explicación: AVG con GROUP BY.
SELECT estado_verificacion, ROUND(AVG(peso_estimado_kg) * 1000, 1) AS gramos_promedio FROM deposito GROUP BY estado_verificacion ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E102>>>
-- E102 · 6. Agrupar: GROUP BY y HAVING · Porcentaje
-- Objetivo: Qué porcentaje de depósitos fue sin sensor.
-- Explicación: Se divide y se multiplica por 100.
SELECT ROUND(100 * COUNT(CASE WHEN sin_sensor = 1 THEN 1 END) / COUNT(*), 1) AS porcentaje_sin_sensor FROM deposito;
PROMPT <<<FIN>>>

PROMPT <<<E103>>>
-- E103 · 6. Agrupar: GROUP BY y HAVING · Depósitos por estado
-- Objetivo: Cuántos pendientes, verificados y rechazados.
-- Explicación: Agrupar por el código y traducirlo con CASE.
SELECT CASE estado_verificacion WHEN 0 THEN 'PENDIENTE' WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END AS estado, COUNT(*) AS cantidad FROM deposito GROUP BY estado_verificacion ORDER BY estado_verificacion;
PROMPT <<<FIN>>>

PROMPT <<<E104>>>
-- E104 · 6. Agrupar: GROUP BY y HAVING · Fotos por resultado
-- Objetivo: Cuántas fotos se identificaron y cuántas no.
-- Explicación: resultado es un código; se agrupa igual que cualquier columna.
SELECT resultado, COUNT(*) AS fotos FROM consulta_vision GROUP BY resultado ORDER BY resultado;
PROMPT <<<FIN>>>

PROMPT <<<E105>>>
-- E105 · 6. Agrupar: GROUP BY y HAVING · Latencia promedio por canal
-- Objetivo: ¿Cuánto tarda Eco en responder?
-- Explicación: AVG ignora los NULL; ROUND deja el número legible.
SELECT canal, ROUND(AVG(latencia_ms)) AS ms_promedio, MAX(latencia_ms) AS ms_maximo FROM consulta_vision GROUP BY canal;
PROMPT <<<FIN>>>

PROMPT <<<E106>>>
-- E106 · 6. Agrupar: GROUP BY y HAVING · CUBE
-- Objetivo: Subtotales en todas las combinaciones.
-- Explicación: CUBE agrega filas de subtotal por cada columna y el total general.
SELECT sin_sensor, estado_verificacion, COUNT(*) AS n FROM deposito GROUP BY CUBE(sin_sensor, estado_verificacion) ORDER BY sin_sensor, estado_verificacion;
PROMPT <<<FIN>>>

PROMPT <<<E107>>>
-- E107 · 6. Agrupar: GROUP BY y HAVING · GROUPING
-- Objetivo: Distinguir una fila de subtotal.
-- Explicación: GROUPING(col) = 1 en las filas que ROLLUP agregó.
SELECT DECODE(GROUPING(categoria), 1, 'TOTAL', TO_CHAR(categoria)) AS categoria, COUNT(*) AS materiales FROM material GROUP BY ROLLUP(categoria);
PROMPT <<<FIN>>>

PROMPT <<<E108>>>
-- E108 · 6. Agrupar: GROUP BY y HAVING · GROUPING SETS
-- Objetivo: Dos agrupaciones en una sola consulta.
-- Explicación: Equivale a dos GROUP BY unidos con UNION ALL.
SELECT canal, estado, COUNT(*) AS n FROM interaccion GROUP BY GROUPING SETS ((canal), (estado)) ORDER BY canal, estado;
PROMPT <<<FIN>>>

PROMPT <<<E109>>>
-- E109 · 6. Agrupar: GROUP BY y HAVING · HAVING con COUNT DISTINCT
-- Objetivo: Materiales depositados por al menos 3 personas distintas.
-- Explicación: HAVING filtra grupos ya formados.
SELECT d.id_material_declarado, COUNT(DISTINCT i.id_usuario) AS personas FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY d.id_material_declarado HAVING COUNT(DISTINCT i.id_usuario) >= 3 ORDER BY personas DESC;
PROMPT <<<FIN>>>

PROMPT <<<E110>>>
-- E110 · 7. Uniones (JOIN) · INNER JOIN
-- Objetivo: Canecas con el nombre de su estación.
-- Explicación: JOIN une filas que coinciden en la clave.
SELECT c.nombre AS caneca, p.nombre AS estacion FROM contenedor c JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion;
PROMPT <<<FIN>>>

PROMPT <<<E111>>>
-- E111 · 7. Uniones (JOIN) · JOIN con tabla puente
-- Objetivo: Roles de cada persona del personal.
-- Explicación: USUARIO_ROL une USUARIO y ROL (N:M).
SELECT u.nombre, r.descripcion FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo IN (2, 3) ORDER BY u.nombre;
PROMPT <<<FIN>>>

PROMPT <<<E112>>>
-- E112 · 7. Uniones (JOIN) · Varios JOIN
-- Objetivo: Depósito con titular, caneca y material.
-- Explicación: Se sigue la cadena depósito → orden → sesión → usuario.
SELECT d.id_deposito, u.nombre, c.nombre AS caneca, m.codigo FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario u ON u.id_usuario = i.id_usuario JOIN contenedor c ON c.id_contenedor = o.id_contenedor JOIN material m ON m.id_material = d.id_material_declarado FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E113>>>
-- E113 · 7. Uniones (JOIN) · LEFT JOIN
-- Objetivo: Todos los materiales y cuántas veces se verificaron (incluye 0).
-- Explicación: LEFT JOIN conserva las filas de la izquierda aunque no tengan pareja.
SELECT m.codigo, COUNT(d.id_deposito) AS verificaciones FROM material m LEFT JOIN deposito d ON d.id_material_verificado = m.id_material GROUP BY m.codigo ORDER BY verificaciones DESC, m.codigo;
PROMPT <<<FIN>>>

PROMPT <<<E114>>>
-- E114 · 7. Uniones (JOIN) · LEFT JOIN + IS NULL
-- Objetivo: Materiales que nunca se han verificado.
-- Explicación: Las filas sin pareja tienen NULL en la tabla derecha.
SELECT m.codigo FROM material m LEFT JOIN deposito d ON d.id_material_verificado = m.id_material WHERE d.id_deposito IS NULL ORDER BY m.codigo;
PROMPT <<<FIN>>>

PROMPT <<<E115>>>
-- E115 · 7. Uniones (JOIN) · RIGHT JOIN
-- Objetivo: Lo mismo, escrito al revés.
-- Explicación: RIGHT JOIN conserva las de la derecha.
SELECT m.codigo, COUNT(d.id_deposito) AS veces FROM deposito d RIGHT JOIN material m ON m.id_material = d.id_material_declarado GROUP BY m.codigo ORDER BY veces DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E116>>>
-- E116 · 7. Uniones (JOIN) · FULL OUTER JOIN
-- Objetivo: Canecas y materiales, tengan o no pareja.
-- Explicación: FULL conserva todas las filas de ambos lados.
SELECT c.id_contenedor, m.codigo FROM contenedor_material cm FULL OUTER JOIN material m ON m.id_material = cm.id_material LEFT JOIN contenedor c ON c.id_contenedor = cm.id_contenedor WHERE c.id_contenedor IS NULL;
PROMPT <<<FIN>>>

PROMPT <<<E117>>>
-- E117 · 7. Uniones (JOIN) · SELF JOIN
-- Objetivo: Crédito y su reverso (la tabla consigo misma).
-- Explicación: Se usa la misma tabla con dos alias.
SELECT r.id_movimiento AS reverso, r.puntos, c.id_movimiento AS credito, c.puntos AS puntos_credito FROM movimiento_ecopuntos r JOIN movimiento_ecopuntos c ON c.id_movimiento = r.id_movimiento_revertido;
PROMPT <<<FIN>>>

PROMPT <<<E118>>>
-- E118 · 7. Uniones (JOIN) · CROSS JOIN
-- Objetivo: Todas las combinaciones de rol y estación.
-- Explicación: Producto cartesiano: 3 roles × 2 estaciones = 6 filas.
SELECT r.descripcion, p.nombre FROM rol r CROSS JOIN punto_instalacion p;
PROMPT <<<FIN>>>

PROMPT <<<E119>>>
-- E119 · 7. Uniones (JOIN) · JOIN con USING
-- Objetivo: Sintaxis corta cuando la columna se llama igual.
-- Explicación: USING(columna) evita repetir el ON.
SELECT codigo, clave_objeto FROM material JOIN peso_referencial USING (id_material) WHERE codigo = 'PET';
PROMPT <<<FIN>>>

PROMPT <<<E120>>>
-- E120 · 7. Uniones (JOIN) · JOIN para verificador
-- Objetivo: Quién verificó cada depósito.
-- Explicación: La misma tabla USUARIO aparece dos veces con papeles distintos.
SELECT d.id_deposito, t.nombre AS titular, v.nombre AS verificador FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario t ON t.id_usuario = i.id_usuario JOIN usuario v ON v.id_usuario = d.id_verificador FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E121>>>
-- E121 · 7. Uniones (JOIN) · Recorrido completo
-- Objetivo: Sesión → orden → evento → depósito.
-- Explicación: Ver cada evidencia separada.
SELECT id_interaccion, estado_sesion, fotos_consultadas, estado_orden, aperturas_confirmadas, id_deposito FROM vw_recorrido FETCH FIRST 6 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E122>>>
-- E122 · 7. Uniones (JOIN) · JOIN y GROUP BY
-- Objetivo: Depósitos por estación.
-- Explicación: Primero se une, luego se agrupa.
SELECT p.nombre, COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN contenedor c ON c.id_contenedor = o.id_contenedor JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion GROUP BY p.nombre;
PROMPT <<<FIN>>>

PROMPT <<<E123>>>
-- E123 · 7. Uniones (JOIN) · Eventos por tipo
-- Objetivo: Cuántos eventos de cada tipo.
-- Explicación: Ver aperturas vs detecciones.
SELECT DECODE(tipo,1,'Acuse',2,'Apertura',3,'Detección',4,'Cierre',5,'Fallo',6,'Cierre sin sensor') AS tipo, COUNT(*) FROM evento_dispositivo GROUP BY tipo ORDER BY tipo;
PROMPT <<<FIN>>>

PROMPT <<<E124>>>
-- E124 · 7. Uniones (JOIN) · Tarifa aplicada
-- Objetivo: Movimiento con su tarifa y material.
-- Explicación: JOIN de 3 tablas del módulo de incentivos.
SELECT m.id_movimiento, mt.codigo, r.puntos AS tarifa, r.por_unidad, m.puntos FROM movimiento_ecopuntos m JOIN regla_ecopuntos r ON r.id_regla = m.id_regla JOIN material mt ON mt.id_material = r.id_material FETCH FIRST 6 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E125>>>
-- E125 · 7. Uniones (JOIN) · Qué recibe cada caneca
-- Objetivo: Nombre de caneca y nombre de material.
-- Explicación: Dos JOIN a través de la tabla puente CONTENEDOR_MATERIAL.
SELECT c.nombre AS caneca, m.nombre AS material FROM contenedor c JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor JOIN material m ON m.id_material = cm.id_material WHERE c.id_punto_instalacion = 1 ORDER BY c.nombre, m.nombre;
PROMPT <<<FIN>>>

PROMPT <<<E126>>>
-- E126 · 7. Uniones (JOIN) · Pesos con su material
-- Objetivo: Catálogo de pesos con el nombre del material.
-- Explicación: JOIN por id_material.
SELECT m.nombre, p.clave_objeto, p.gramos_tipico FROM peso_referencial p JOIN material m ON m.id_material = p.id_material ORDER BY m.nombre FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E127>>>
-- E127 · 7. Uniones (JOIN) · Movimientos con el titular
-- Objetivo: Quién recibió cada movimiento de EcoPuntos.
-- Explicación: JOIN con USUARIO para mostrar el nombre.
SELECT u.nombre, mv.tipo, mv.puntos FROM movimiento_ecopuntos mv JOIN usuario u ON u.id_usuario = mv.id_usuario ORDER BY mv.id_movimiento FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E128>>>
-- E128 · 7. Uniones (JOIN) · Auditoría con el actor
-- Objetivo: Quién hizo cada verificación auditada.
-- Explicación: LEFT JOIN por si el actor quedó NULL.
SELECT a.id_deposito, a.operacion, u.nombre AS actor FROM auditoria_deposito a LEFT JOIN usuario u ON u.id_usuario = a.id_actor ORDER BY a.id_auditoria FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E129>>>
-- E129 · 7. Uniones (JOIN) · Eventos de una orden
-- Objetivo: Ver acuse, apertura y detección de una orden.
-- Explicación: JOIN de ORDEN_APERTURA con EVENTO_DISPOSITIVO (1:N).
SELECT o.id_orden, e.tipo, e.resultado FROM orden_apertura o JOIN evento_dispositivo e ON e.id_orden = o.id_orden WHERE o.id_orden = (SELECT MIN(id_orden) FROM orden_apertura) ORDER BY e.tipo;
PROMPT <<<FIN>>>

PROMPT <<<E130>>>
-- E130 · 7. Uniones (JOIN) · Fotos con material (LEFT)
-- Objetivo: Fotos con el material reconocido, si hubo.
-- Explicación: LEFT JOIN conserva las fotos «no es residuo» sin material.
SELECT cv.id_consulta, cv.resultado, m.nombre FROM consulta_vision cv LEFT JOIN material m ON m.id_material = cv.id_material ORDER BY cv.id_consulta FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E131>>>
-- E131 · 7. Uniones (JOIN) · JOIN no igualitario
-- Objetivo: ¿En qué rango del catálogo cae cada depósito?
-- Explicación: La condición del JOIN puede ser BETWEEN, no solo =.
SELECT d.id_deposito, d.peso_estimado_kg * 1000 AS gramos, p.clave_objeto FROM deposito d JOIN peso_referencial p ON p.id_peso_referencial = d.id_peso_referencial AND d.peso_estimado_kg * 1000 BETWEEN p.gramos_minimo * NVL(d.cantidad_declarada, 1) AND p.gramos_maximo * NVL(d.cantidad_declarada, 1) FETCH FIRST 6 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E132>>>
-- E132 · 7. Uniones (JOIN) · Roles de cada persona
-- Objetivo: Personas con más de un rol.
-- Explicación: JOIN + GROUP BY + HAVING sobre la tabla N:M.
SELECT u.nombre, LISTAGG(r.descripcion, ', ') WITHIN GROUP (ORDER BY r.codigo) AS roles FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol GROUP BY u.nombre HAVING COUNT(*) > 1;
PROMPT <<<FIN>>>

PROMPT <<<E133>>>
-- E133 · 8. Subconsultas · Subconsulta escalar
-- Objetivo: Depósitos por encima del peso promedio.
-- Explicación: La consulta interna devuelve un solo valor.
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg > (SELECT AVG(peso_estimado_kg) FROM deposito) ORDER BY peso_estimado_kg DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E134>>>
-- E134 · 8. Subconsultas · IN con subconsulta
-- Objetivo: Usuarios que tienen rol Operario.
-- Explicación: La interna devuelve una lista.
SELECT nombre FROM usuario WHERE id_usuario IN (SELECT ur.id_usuario FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = 2);
PROMPT <<<FIN>>>

PROMPT <<<E135>>>
-- E135 · 8. Subconsultas · NOT IN
-- Objetivo: Ciudadanos sin ninguna sesión.
-- Explicación: NOT IN excluye la lista (cuidado con NULL: se filtran).
SELECT COUNT(*) FROM usuario WHERE id_usuario NOT IN (SELECT id_usuario FROM interaccion WHERE id_usuario IS NOT NULL);
PROMPT <<<FIN>>>

PROMPT <<<E136>>>
-- E136 · 8. Subconsultas · EXISTS
-- Objetivo: Canecas que tienen al menos un depósito.
-- Explicación: EXISTS pregunta «¿hay al menos una fila?».
SELECT c.nombre FROM contenedor c WHERE EXISTS (SELECT 1 FROM orden_apertura o JOIN deposito d ON d.id_orden = o.id_orden WHERE o.id_contenedor = c.id_contenedor);
PROMPT <<<FIN>>>

PROMPT <<<E137>>>
-- E137 · 8. Subconsultas · NOT EXISTS
-- Objetivo: Materiales sin peso de referencia.
-- Explicación: Útil para encontrar huecos en el catálogo.
SELECT m.codigo FROM material m WHERE NOT EXISTS (SELECT 1 FROM peso_referencial p WHERE p.id_material = m.id_material);
PROMPT <<<FIN>>>

PROMPT <<<E138>>>
-- E138 · 8. Subconsultas · Subconsulta correlacionada
-- Objetivo: Último depósito de cada caneca.
-- Explicación: La interna se repite por cada fila de la externa.
SELECT c.nombre, (SELECT MAX(d.fecha_deteccion_utc) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden WHERE o.id_contenedor = c.id_contenedor) AS ultimo FROM contenedor c;
PROMPT <<<FIN>>>

PROMPT <<<E139>>>
-- E139 · 8. Subconsultas · Subconsulta en FROM
-- Objetivo: Promedio de depósitos por ciudadano.
-- Explicación: Una consulta puede ser la «tabla» de otra.
SELECT ROUND(AVG(n), 2) AS promedio_por_ciudadano FROM (SELECT i.id_usuario, COUNT(*) n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario);
PROMPT <<<FIN>>>

PROMPT <<<E140>>>
-- E140 · 8. Subconsultas · WITH (CTE)
-- Objetivo: La misma idea, más legible.
-- Explicación: WITH le pone nombre a una subconsulta.
WITH por_ciudadano AS (SELECT i.id_usuario, COUNT(*) n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT MAX(n) AS maximo, MIN(n) AS minimo FROM por_ciudadano;
PROMPT <<<FIN>>>

PROMPT <<<E141>>>
-- E141 · 8. Subconsultas · ANY / ALL
-- Objetivo: Pesos mayores que TODOS los de la caneca verde.
-- Explicación: ALL compara contra todos los valores.
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico > ALL (SELECT gramos_tipico FROM peso_referencial p JOIN material m USING (id_material) WHERE m.categoria = 2);
PROMPT <<<FIN>>>

PROMPT <<<E142>>>
-- E142 · 8. Subconsultas · Subconsulta con HAVING
-- Objetivo: Ciudadanos con más depósitos que el promedio.
-- Explicación: Se compara un grupo contra un promedio.
SELECT i.id_usuario, COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario HAVING COUNT(*) > (SELECT COUNT(*) / COUNT(DISTINCT i2.id_usuario) FROM deposito d2 JOIN orden_apertura o2 ON o2.id_orden = d2.id_orden JOIN interaccion i2 ON i2.id_interaccion = o2.id_interaccion) ORDER BY 2 DESC;
PROMPT <<<FIN>>>

PROMPT <<<E143>>>
-- E143 · 8. Subconsultas · Dos CTE
-- Objetivo: Fotos y depósitos por ciudadano lado a lado.
-- Explicación: Comparar orientación vs reciclaje.
WITH f AS (SELECT id_usuario, COUNT(*) fotos FROM consulta_vision GROUP BY id_usuario), d AS (SELECT i.id_usuario, COUNT(*) deps FROM deposito x JOIN orden_apertura o ON o.id_orden = x.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT f.id_usuario, f.fotos, NVL(d.deps, 0) deps FROM f LEFT JOIN d ON d.id_usuario = f.id_usuario WHERE f.id_usuario IS NOT NULL ORDER BY f.fotos DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E144>>>
-- E144 · 8. Subconsultas · Subconsulta en UPDATE (con ROLLBACK)
-- Objetivo: Cambiar un valor usando otra tabla.
-- Explicación: Al final ROLLBACK deja todo como estaba.
UPDATE material SET descripcion = descripcion || ' (revisado)' WHERE id_material IN (SELECT id_material FROM peso_referencial WHERE gramos_tipico > 500);
ROLLBACK;
PROMPT <<<FIN>>>

PROMPT <<<E145>>>
-- E145 · 8. Subconsultas · El depósito más pesado
-- Objetivo: El depósito con mayor peso estimado.
-- Explicación: La subconsulta calcula el máximo; la externa busca quién lo tiene.
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg = (SELECT MAX(peso_estimado_kg) FROM deposito);
PROMPT <<<FIN>>>

PROMPT <<<E146>>>
-- E146 · 8. Subconsultas · Personas con más de 3 depósitos
-- Objetivo: IN con una subconsulta agrupada.
-- Explicación: La subconsulta devuelve una lista de ids.
SELECT nombre FROM usuario WHERE id_usuario IN (SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario HAVING COUNT(*) > 3) ORDER BY nombre;
PROMPT <<<FIN>>>

PROMPT <<<E147>>>
-- E147 · 8. Subconsultas · Materiales nunca depositados
-- Objetivo: NOT EXISTS: lo que nunca pasó.
-- Explicación: NOT EXISTS es seguro aunque haya NULL (a diferencia de NOT IN).
SELECT m.codigo FROM material m WHERE NOT EXISTS (SELECT 1 FROM deposito d WHERE d.id_material_declarado = m.id_material) ORDER BY m.codigo;
PROMPT <<<FIN>>>

PROMPT <<<E148>>>
-- E148 · 8. Subconsultas · Sobre el promedio de su material
-- Objetivo: Correlacionada: comparar con el promedio del mismo material.
-- Explicación: La subconsulta se repite para cada fila externa.
SELECT d.id_deposito, d.id_material_declarado, d.peso_estimado_kg FROM deposito d WHERE d.peso_estimado_kg > (SELECT AVG(d2.peso_estimado_kg) FROM deposito d2 WHERE d2.id_material_declarado = d.id_material_declarado) ORDER BY d.id_deposito FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E149>>>
-- E149 · 8. Subconsultas · Subconsulta en la lista SELECT
-- Objetivo: Cuántas fotos tiene cada persona.
-- Explicación: Una subconsulta escalar por fila.
SELECT u.nombre, (SELECT COUNT(*) FROM consulta_vision cv WHERE cv.id_usuario = u.id_usuario) AS fotos FROM usuario u ORDER BY fotos DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E150>>>
-- E150 · 8. Subconsultas · IN con dos columnas
-- Objetivo: Pares caneca-material admitidos.
-- Explicación: Se pueden comparar varias columnas a la vez.
SELECT id_contenedor, id_material FROM contenedor_material WHERE (id_contenedor, id_material) IN (SELECT 11, 101 FROM dual UNION ALL SELECT 12, 201 FROM dual);
PROMPT <<<FIN>>>

PROMPT <<<E151>>>
-- E151 · 8. Subconsultas · Vista en línea con ranking
-- Objetivo: Top 3 materiales verificados.
-- Explicación: La subconsulta en FROM actúa como una tabla temporal.
SELECT * FROM (SELECT id_material_verificado, COUNT(*) AS n FROM deposito WHERE estado_verificacion = 1 GROUP BY id_material_verificado ORDER BY n DESC) WHERE ROWNUM <= 3;
PROMPT <<<FIN>>>

PROMPT <<<E152>>>
-- E152 · 8. Subconsultas · CTE con varios pasos
-- Objetivo: Fotos, depósitos y razón por persona.
-- Explicación: WITH ordena el razonamiento en pasos con nombre.
WITH f AS (SELECT id_usuario, COUNT(*) AS fotos FROM consulta_vision GROUP BY id_usuario), d AS (SELECT i.id_usuario, COUNT(*) AS depositos FROM deposito x JOIN orden_apertura o ON o.id_orden = x.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT f.id_usuario, f.fotos, NVL(d.depositos, 0) AS depositos FROM f LEFT JOIN d ON d.id_usuario = f.id_usuario ORDER BY f.fotos DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E153>>>
-- E153 · 9. Operaciones de conjuntos · UNION
-- Objetivo: Lista única de materiales declarados o verificados.
-- Explicación: UNION quita repetidos.
SELECT id_material_declarado AS id FROM deposito UNION SELECT id_material_verificado FROM deposito WHERE id_material_verificado IS NOT NULL ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E154>>>
-- E154 · 9. Operaciones de conjuntos · UNION ALL
-- Objetivo: Embudo de evidencias en una tabla.
-- Explicación: UNION ALL no quita repetidos y es más rápido.
SELECT 'fotos' etapa, COUNT(*) n FROM consulta_vision UNION ALL SELECT 'ordenes', COUNT(*) FROM orden_apertura UNION ALL SELECT 'depositos', COUNT(*) FROM deposito;
PROMPT <<<FIN>>>

PROMPT <<<E155>>>
-- E155 · 9. Operaciones de conjuntos · INTERSECT
-- Objetivo: Materiales que fueron declarados Y verificados.
-- Explicación: INTERSECT deja lo común.
SELECT id_material_declarado FROM deposito INTERSECT SELECT id_material_verificado FROM deposito ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E156>>>
-- E156 · 9. Operaciones de conjuntos · MINUS
-- Objetivo: Ciudadanos con fotos pero sin depósitos.
-- Explicación: MINUS resta conjuntos.
SELECT id_usuario FROM consulta_vision WHERE id_usuario IS NOT NULL MINUS SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E157>>>
-- E157 · 9. Operaciones de conjuntos · UNION de distintas tablas
-- Objetivo: Bitácora única de cambios (auditoría + historial).
-- Explicación: Las columnas deben coincidir en número y tipo.
SELECT 'DEPOSITO' AS objeto, id_deposito AS id, operacion AS cambio, fecha_utc FROM auditoria_deposito WHERE operacion <> 'VERIFICACION' UNION ALL SELECT 'CANECA', id_contenedor, estado_anterior || '->' || estado_nuevo, fecha_utc FROM historial_estado_contenedor ORDER BY fecha_utc;
PROMPT <<<FIN>>>

PROMPT <<<E158>>>
-- E158 · 9. Operaciones de conjuntos · Verificar con MINUS
-- Objetivo: Comprobar que la vista coincide con la tabla.
-- Explicación: Si MINUS no devuelve filas, son iguales.
SELECT id_deposito FROM deposito MINUS SELECT id_deposito FROM vw_deposito_detalle;
PROMPT <<<FIN>>>

PROMPT <<<E159>>>
-- E159 · 9. Operaciones de conjuntos · Sugerido o confirmado
-- Objetivo: Materiales que la IA sugirió o el ciudadano confirmó.
-- Explicación: UNION quita los repetidos.
SELECT id_material_sugerido AS id_material FROM interaccion WHERE id_material_sugerido IS NOT NULL UNION SELECT id_material_confirmado FROM interaccion WHERE id_material_confirmado IS NOT NULL ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E160>>>
-- E160 · 9. Operaciones de conjuntos · Con foto y con depósito
-- Objetivo: Personas que hicieron las dos cosas.
-- Explicación: INTERSECT deja solo lo común.
SELECT id_usuario FROM consulta_vision INTERSECT SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1 FETCH FIRST 10 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E161>>>
-- E161 · 9. Operaciones de conjuntos · Solo fotos, sin depósito
-- Objetivo: Personas que preguntaron pero no depositaron.
-- Explicación: MINUS = el primer conjunto menos el segundo.
SELECT id_usuario FROM consulta_vision MINUS SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E162>>>
-- E162 · 9. Operaciones de conjuntos · Embudo con UNION ALL
-- Objetivo: Contar cada etapa en una sola tabla.
-- Explicación: UNION ALL pega filas sin quitar repetidos (y es más rápido).
SELECT '1 Fotos' AS etapa, COUNT(*) AS n FROM consulta_vision UNION ALL SELECT '2 Sesiones', COUNT(*) FROM interaccion UNION ALL SELECT '3 Órdenes', COUNT(*) FROM orden_apertura UNION ALL SELECT '4 Depósitos', COUNT(*) FROM deposito UNION ALL SELECT '5 Verificados', COUNT(*) FROM deposito WHERE estado_verificacion = 1 ORDER BY 1;
PROMPT <<<FIN>>>

PROMPT <<<E163>>>
-- E163 · 9. Operaciones de conjuntos · Colores sin depósitos
-- Objetivo: Categorías de material sin ningún depósito.
-- Explicación: MINUS entre catálogo y uso.
SELECT categoria FROM material MINUS SELECT m.categoria FROM deposito d JOIN material m ON m.id_material = d.id_material_declarado;
PROMPT <<<FIN>>>

PROMPT <<<E164>>>
-- E164 · 9. Operaciones de conjuntos · Reglas de UNION
-- Objetivo: Las columnas deben coincidir en número y tipo.
-- Explicación: Se usan alias en la primera consulta; ORDER BY va al final.
SELECT codigo AS texto, 'material' AS origen FROM material WHERE categoria = 3 UNION SELECT nombre, 'caneca' FROM contenedor WHERE categoria = 3 ORDER BY origen, texto;
PROMPT <<<FIN>>>

PROMPT <<<E165>>>
-- E165 · 10. Funciones analíticas · ROW_NUMBER
-- Objetivo: Numerar los depósitos de cada ciudadano.
-- Explicación: OVER(PARTITION BY ...) reinicia la cuenta por grupo.
SELECT id_titular, id_deposito, ROW_NUMBER() OVER (PARTITION BY id_titular ORDER BY fecha_colombia) AS n FROM vw_deposito_detalle WHERE id_titular IN (SELECT MIN(id_titular) FROM vw_deposito_detalle);
PROMPT <<<FIN>>>

PROMPT <<<E166>>>
-- E166 · 10. Funciones analíticas · RANK y DENSE_RANK
-- Objetivo: Ranking con empates.
-- Explicación: RANK salta puestos tras un empate; DENSE_RANK no.
SELECT titular, COUNT(*) n, RANK() OVER (ORDER BY COUNT(*) DESC) AS rank, DENSE_RANK() OVER (ORDER BY COUNT(*) DESC) AS dense FROM vw_deposito_detalle WHERE estado_verificacion = 1 GROUP BY titular FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E167>>>
-- E167 · 10. Funciones analíticas · SUM acumulado
-- Objetivo: Depósitos acumulados por día.
-- Explicación: SUM() OVER (ORDER BY ...) va sumando.
SELECT dia, n, SUM(n) OVER (ORDER BY dia) AS acumulado FROM (SELECT TRUNC(fecha_colombia) dia, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia)) ORDER BY dia DESC FETCH FIRST 7 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E168>>>
-- E168 · 10. Funciones analíticas · LAG
-- Objetivo: Comparar cada semana con la anterior.
-- Explicación: LAG trae el valor de la fila anterior.
SELECT semana, n, LAG(n) OVER (ORDER BY semana) AS semana_anterior, n - LAG(n) OVER (ORDER BY semana) AS diferencia FROM (SELECT TRUNC(fecha_colombia,'IW') semana, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia,'IW')) ORDER BY semana;
PROMPT <<<FIN>>>

PROMPT <<<E169>>>
-- E169 · 10. Funciones analíticas · Porcentaje del total
-- Objetivo: Participación de cada color.
-- Explicación: RATIO_TO_REPORT calcula la fracción del total.
SELECT color, COUNT(*) n, ROUND(100 * RATIO_TO_REPORT(COUNT(*)) OVER (), 1) AS porcentaje FROM vw_deposito_detalle GROUP BY color;
PROMPT <<<FIN>>>

PROMPT <<<E170>>>
-- E170 · 10. Funciones analíticas · Primero de cada grupo
-- Objetivo: Primer depósito de cada caneca.
-- Explicación: FIRST_VALUE toma el primero según el orden.
SELECT DISTINCT caneca, FIRST_VALUE(id_deposito) OVER (PARTITION BY caneca ORDER BY fecha_colombia) AS primer_deposito FROM vw_deposito_detalle;
PROMPT <<<FIN>>>

PROMPT <<<E171>>>
-- E171 · 10. Funciones analíticas · Promedio móvil
-- Objetivo: Promedio de 3 días.
-- Explicación: ROWS BETWEEN 2 PRECEDING AND CURRENT ROW.
SELECT dia, n, ROUND(AVG(n) OVER (ORDER BY dia ROWS BETWEEN 2 PRECEDING AND CURRENT ROW), 2) AS movil_3 FROM (SELECT TRUNC(fecha_colombia) dia, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia)) ORDER BY dia DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E172>>>
-- E172 · 10. Funciones analíticas · NTILE
-- Objetivo: Dividir ciudadanos en 4 grupos por actividad.
-- Explicación: NTILE(4) reparte en cuartiles.
SELECT titular, n, NTILE(4) OVER (ORDER BY n DESC) AS cuartil FROM (SELECT titular, COUNT(*) n FROM vw_deposito_detalle WHERE titular IS NOT NULL GROUP BY titular) FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E173>>>
-- E173 · 10. Funciones analíticas · Total por partición
-- Objetivo: Cada depósito junto al total de su material.
-- Explicación: SUM() OVER (PARTITION BY) no colapsa filas como GROUP BY.
SELECT id_deposito, id_material_declarado, COUNT(*) OVER (PARTITION BY id_material_declarado) AS del_mismo_material FROM deposito ORDER BY id_material_declarado, id_deposito FETCH FIRST 10 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E174>>>
-- E174 · 10. Funciones analíticas · LEAD
-- Objetivo: Ver la siguiente foto de la misma persona.
-- Explicación: LEAD mira la fila de adelante dentro de la partición.
SELECT id_usuario, id_consulta, LEAD(id_consulta) OVER (PARTITION BY id_usuario ORDER BY fecha_utc) AS siguiente FROM consulta_vision ORDER BY id_usuario, fecha_utc FETCH FIRST 10 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E175>>>
-- E175 · 10. Funciones analíticas · FIRST_VALUE
-- Objetivo: Primer material que depositó cada persona.
-- Explicación: FIRST_VALUE toma el primero según el orden.
SELECT DISTINCT i.id_usuario, FIRST_VALUE(d.id_material_declarado) OVER (PARTITION BY i.id_usuario ORDER BY d.fecha_deteccion_utc) AS primer_material FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY i.id_usuario FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E176>>>
-- E176 · 10. Funciones analíticas · RATIO_TO_REPORT
-- Objetivo: Porcentaje de cada estado sobre el total.
-- Explicación: Divide cada valor por la suma de su ventana.
SELECT estado_verificacion, COUNT(*) AS n, ROUND(100 * RATIO_TO_REPORT(COUNT(*)) OVER (), 1) AS porcentaje FROM deposito GROUP BY estado_verificacion ORDER BY estado_verificacion;
PROMPT <<<FIN>>>

PROMPT <<<E177>>>
-- E177 · 10. Funciones analíticas · PERCENT_RANK
-- Objetivo: Posición relativa de cada peso.
-- Explicación: 0 = el menor, 1 = el mayor.
SELECT id_deposito, peso_estimado_kg, ROUND(PERCENT_RANK() OVER (ORDER BY peso_estimado_kg), 2) AS posicion FROM deposito WHERE peso_estimado_kg IS NOT NULL ORDER BY peso_estimado_kg FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E178>>>
-- E178 · 10. Funciones analíticas · Contador por persona
-- Objetivo: Numerar las fotos de cada persona.
-- Explicación: COUNT(*) OVER con ORDER BY cuenta acumulado.
SELECT id_usuario, id_consulta, COUNT(*) OVER (PARTITION BY id_usuario ORDER BY id_consulta) AS foto_numero FROM consulta_vision ORDER BY id_usuario, id_consulta FETCH FIRST 10 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E179>>>
-- E179 · 10. Funciones analíticas · Mejor del mes con RANK
-- Objetivo: El puesto de cada persona en el mes por verificados.
-- Explicación: Agrupar primero y numerar después.
SELECT TO_CHAR(fecha_colombia, 'YYYY-MM') AS mes, titular, COUNT(*) AS verificados, RANK() OVER (PARTITION BY TO_CHAR(fecha_colombia, 'YYYY-MM') ORDER BY COUNT(*) DESC) AS puesto FROM vw_deposito_detalle WHERE estado_verificacion = 1 GROUP BY TO_CHAR(fecha_colombia, 'YYYY-MM'), titular ORDER BY mes, puesto FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E180>>>
-- E180 · 11. Fechas y hora de Colombia · UTC a Colombia
-- Objetivo: Convertir una fecha guardada en UTC.
-- Explicación: FROM_TZ dice «esta hora es UTC»; AT TIME ZONE la pasa a Bogotá (UTC−5).
SELECT fecha_deteccion_utc, CAST(FROM_TZ(fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS colombia FROM deposito WHERE ROWNUM <= 3;
PROMPT <<<FIN>>>

PROMPT <<<E181>>>
-- E181 · 11. Fechas y hora de Colombia · Hoy en Colombia
-- Objetivo: Depósitos de hoy (hora Colombia).
-- Explicación: Se compara con el inicio del día local.
SELECT COUNT(*) AS hoy FROM vw_deposito_detalle WHERE fecha_colombia >= TRUNC(CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE));
PROMPT <<<FIN>>>

PROMPT <<<E182>>>
-- E182 · 11. Fechas y hora de Colombia · Semana calendario actual
-- Objetivo: Lunes a domingo de esta semana.
-- Explicación: TRUNC(fecha,'IW') = lunes.
SELECT TRUNC(SYSDATE, 'IW') AS lunes, TRUNC(SYSDATE, 'IW') + 6 AS domingo FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E183>>>
-- E183 · 11. Fechas y hora de Colombia · Mes calendario
-- Objetivo: Depósitos de este mes.
-- Explicación: TRUNC(fecha,'MM') = día 1 del mes.
SELECT COUNT(*) FROM vw_deposito_detalle WHERE fecha_colombia >= TRUNC(SYSDATE, 'MM');
PROMPT <<<FIN>>>

PROMPT <<<E184>>>
-- E184 · 11. Fechas y hora de Colombia · Diferencia de TIMESTAMP
-- Objetivo: Segundos entre emitir y cerrar una orden.
-- Explicación: EXTRACT saca partes de un intervalo.
SELECT id_orden, EXTRACT(SECOND FROM (fecha_cierre_utc - fecha_emision_utc)) + 60 * EXTRACT(MINUTE FROM (fecha_cierre_utc - fecha_emision_utc)) AS segundos FROM orden_apertura WHERE estado = 6 FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E185>>>
-- E185 · 11. Fechas y hora de Colombia · Día de la semana con más uso
-- Objetivo: ¿Qué día se recicla más?
-- Explicación: TO_CHAR(fecha,'D') o el nombre del día.
SELECT TO_CHAR(fecha_colombia, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia, COUNT(*) FROM vw_deposito_detalle GROUP BY TO_CHAR(fecha_colombia, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') ORDER BY 2 DESC;
PROMPT <<<FIN>>>

PROMPT <<<E186>>>
-- E186 · 11. Fechas y hora de Colombia · FROM_TZ
-- Objetivo: Declarar que una hora está en UTC y pasarla a Colombia.
-- Explicación: FROM_TZ le pone zona a un TIMESTAMP sin zona; AT TIME ZONE la convierte.
SELECT id_consulta, fecha_utc, FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS hora_colombia FROM consulta_vision WHERE ROWNUM <= 3;
PROMPT <<<FIN>>>

PROMPT <<<E187>>>
-- E187 · 11. Fechas y hora de Colombia · Hora del día
-- Objetivo: ¿A qué hora de Colombia se toman más fotos?
-- Explicación: EXTRACT(HOUR ...) saca la hora; se resta la zona antes.
SELECT EXTRACT(HOUR FROM (FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota')) AS hora, COUNT(*) AS fotos FROM consulta_vision GROUP BY EXTRACT(HOUR FROM (FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota')) ORDER BY fotos DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E188>>>
-- E188 · 11. Fechas y hora de Colombia · Lunes de la semana
-- Objetivo: TRUNC(fecha, 'IW') da el lunes ISO.
-- Explicación: Sirve para agrupar por semana calendario.
SELECT TRUNC(DATE '2026-11-11', 'IW') AS lunes, TRUNC(DATE '2026-11-11', 'IW') + 6 AS domingo FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E189>>>
-- E189 · 11. Fechas y hora de Colombia · INTERVAL
-- Objetivo: Sumar 10 segundos (la ventana de depósito).
-- Explicación: INTERVAL representa una duración.
SELECT SYSTIMESTAMP AS ahora, SYSTIMESTAMP + INTERVAL '10' SECOND AS cierre_ventana FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E190>>>
-- E190 · 11. Fechas y hora de Colombia · Minutos entre emisión y vencimiento
-- Objetivo: Cuánto vive una orden de apertura.
-- Explicación: Restar TIMESTAMP da un INTERVAL; EXTRACT saca sus partes.
SELECT id_orden, EXTRACT(MINUTE FROM (fecha_vencimiento_utc - fecha_emision_utc)) AS minutos, EXTRACT(SECOND FROM (fecha_vencimiento_utc - fecha_emision_utc)) AS segundos FROM orden_apertura WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E191>>>
-- E191 · 11. Fechas y hora de Colombia · Nombre del día
-- Objetivo: Día de la semana en español.
-- Explicación: TO_CHAR con 'Day' y el idioma indicado.
SELECT TO_CHAR(DATE '2026-11-11', 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E192>>>
-- E192 · 12. Modificar datos (DML) y transacciones · INSERT y ROLLBACK
-- Objetivo: Insertar un material y deshacerlo.
-- Explicación: INSERT agrega una fila; ROLLBACK deshace lo no confirmado.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_X', 'Material de prueba', 1);
SELECT codigo FROM material WHERE codigo = 'PRUEBA_X';
ROLLBACK;
SELECT COUNT(*) AS despues_del_rollback FROM material WHERE codigo = 'PRUEBA_X';
PROMPT <<<FIN>>>

PROMPT <<<E193>>>
-- E193 · 12. Modificar datos (DML) y transacciones · UPDATE
-- Objetivo: Cambiar una descripción (y deshacer).
-- Explicación: UPDATE ... SET ... WHERE. ¡Sin WHERE cambia TODAS las filas!
UPDATE material SET descripcion = 'Texto temporal' WHERE codigo = 'PET';
SELECT descripcion FROM material WHERE codigo = 'PET';
ROLLBACK;
PROMPT <<<FIN>>>

PROMPT <<<E194>>>
-- E194 · 12. Modificar datos (DML) y transacciones · DELETE
-- Objetivo: Borrar una fila de prueba (y deshacer).
-- Explicación: DELETE ... WHERE elimina filas.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_Y', 'Para borrar', 3);
DELETE FROM material WHERE codigo = 'PRUEBA_Y';
SELECT COUNT(*) FROM material WHERE codigo = 'PRUEBA_Y';
ROLLBACK;
PROMPT <<<FIN>>>

PROMPT <<<E195>>>
-- E195 · 12. Modificar datos (DML) y transacciones · SAVEPOINT
-- Objetivo: Deshacer solo una parte.
-- Explicación: ROLLBACK TO vuelve al punto guardado.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_A', 'Uno', 1);
SAVEPOINT despues_de_a;
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_B', 'Dos', 1);
ROLLBACK TO despues_de_a;
SELECT codigo FROM material WHERE codigo LIKE 'PRUEBA_%';
ROLLBACK;
PROMPT <<<FIN>>>

PROMPT <<<E196>>>
-- E196 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: CHECK
-- Objetivo: Ver una restricción en acción (ORA-02290).
-- Explicación: Oracle rechaza datos que rompen una regla. ERROR ESPERADO.
INSERT INTO material (codigo, nombre, categoria) VALUES ('MAL', 'Categoría mala', 7);
PROMPT <<<FIN>>>

PROMPT <<<E197>>>
-- E197 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: UNIQUE
-- Objetivo: Código repetido (ORA-00001).
-- Explicación: UNIQUE impide duplicados. ERROR ESPERADO.
INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Otro PET', 1);
PROMPT <<<FIN>>>

PROMPT <<<E198>>>
-- E198 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: FK
-- Objetivo: Caneca en una estación que no existe (ORA-02291).
-- Explicación: La FK exige que el padre exista. ERROR ESPERADO.
INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (999, 'Caneca fantasma', 1);
PROMPT <<<FIN>>>

PROMPT <<<E199>>>
-- E199 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: NOT NULL
-- Objetivo: Material sin nombre (ORA-01400).
-- Explicación: NOT NULL exige un valor. ERROR ESPERADO.
INSERT INTO material (codigo, nombre, categoria) VALUES ('SIN_NOMBRE', NULL, 1);
PROMPT <<<FIN>>>

PROMPT <<<E200>>>
-- E200 · 12. Modificar datos (DML) y transacciones · INSERT de varias filas
-- Objetivo: Agregar dos materiales a la vez (y deshacer).
-- Explicación: INSERT ... SELECT inserta todas las filas que devuelve la consulta; cada una recibe su propio id IDENTITY (con INSERT ALL se repetiría el id y daría ORA-00001).
INSERT INTO material (codigo, nombre, categoria) SELECT 'PRUEBA_M1', 'Prueba uno', 1 FROM dual UNION ALL SELECT 'PRUEBA_M2', 'Prueba dos', 3 FROM dual;
SELECT codigo FROM material WHERE codigo LIKE 'PRUEBA_M%';
ROLLBACK;
PROMPT <<<FIN>>>

PROMPT <<<E201>>>
-- E201 · 12. Modificar datos (DML) y transacciones · MERGE
-- Objetivo: Insertar o actualizar según exista (y deshacer).
-- Explicación: MERGE decide fila por fila: WHEN MATCHED actualiza, WHEN NOT MATCHED inserta.
MERGE INTO material m USING (SELECT 'PET' AS codigo FROM dual UNION ALL SELECT 'PRUEBA_MERGE' FROM dual) s ON (m.codigo = s.codigo) WHEN MATCHED THEN UPDATE SET m.descripcion = 'Actualizado por MERGE' WHEN NOT MATCHED THEN INSERT (codigo, nombre, categoria) VALUES (s.codigo, 'Nuevo por MERGE', 1);
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'PRUEBA_MERGE');
ROLLBACK;
PROMPT <<<FIN>>>

PROMPT <<<E202>>>
-- E202 · 12. Modificar datos (DML) y transacciones · UPDATE de varias filas
-- Objetivo: Deshabilitar todos los peligrosos (y deshacer).
-- Explicación: SQL%ROWCOUNT / el mensaje dice cuántas filas cambiaron.
UPDATE material SET habilitado = 0 WHERE categoria = 4;
SELECT codigo, habilitado FROM material WHERE categoria = 4;
ROLLBACK;
PROMPT <<<FIN>>>

PROMPT <<<E203>>>
-- E203 · 12. Modificar datos (DML) y transacciones · RETURNING INTO
-- Objetivo: Saber el id que generó un INSERT.
-- Explicación: RETURNING devuelve columnas de la fila insertada.
DECLARE
    v_id material.id_material%TYPE;
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_RET', 'Con returning', 1) RETURNING id_material INTO v_id;
    DBMS_OUTPUT.PUT_LINE('Id generado: ' || v_id);
    ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E204>>>
-- E204 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: IDENTITY ALWAYS
-- Objetivo: Escribir el id de un usuario a mano (ORA-32795).
-- Explicación: Con GENERATED ALWAYS solo Oracle pone el id. ERROR ESPERADO.
INSERT INTO usuario (id_usuario, nombre, correo_electronico) VALUES (99999, 'Prueba', 'prueba@ejemplo.test');
PROMPT <<<FIN>>>

PROMPT <<<E205>>>
-- E205 · 12. Modificar datos (DML) y transacciones · Error ESPERADO: borrar un padre
-- Objetivo: Borrar un material usado por canecas (ORA-02292).
-- Explicación: Sin ON DELETE CASCADE, Oracle protege a los hijos. ERROR ESPERADO.
DELETE FROM material WHERE codigo = 'PET';
PROMPT <<<FIN>>>

PROMPT <<<E206>>>
-- E206 · 12. Modificar datos (DML) y transacciones · Transacción de dos pasos
-- Objetivo: Dos cambios que se confirman o se deshacen juntos.
-- Explicación: Hasta el COMMIT, nadie más ve los cambios; aquí se deshacen con ROLLBACK.
UPDATE material SET descripcion = 'Paso 1' WHERE codigo = 'PET';
UPDATE material SET descripcion = 'Paso 2' WHERE codigo = 'VIDRIO';
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'VIDRIO');
ROLLBACK;
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'VIDRIO');
PROMPT <<<FIN>>>

PROMPT <<<E207>>>
-- E207 · 13. Vistas y seguridad · Vista para Power BI
-- Objetivo: Ver qué muestra la vista anónima.
-- Explicación: El ciudadano aparece como C-0001: sin nombre ni correo.
SELECT ciudadano, dia, caneca, material, estado, origen_dato FROM vw_bi_depositos FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E208>>>
-- E208 · 13. Vistas y seguridad · Privilegios concedidos
-- Objetivo: Qué puede leer el rol de informes.
-- Explicación: USER_TAB_PRIVS_MADE muestra lo que yo concedí.
SELECT grantee, table_name, privilege FROM user_tab_privs_made ORDER BY table_name;
PROMPT <<<FIN>>>

PROMPT <<<E209>>>
-- E209 · 13. Vistas y seguridad · Texto de una vista
-- Objetivo: Ver la consulta guardada.
-- Explicación: USER_VIEWS.TEXT contiene el SELECT de la vista.
SELECT view_name, text_length FROM user_views ORDER BY view_name;
PROMPT <<<FIN>>>

PROMPT <<<E210>>>
-- E210 · 13. Vistas y seguridad · Vista de solo lectura
-- Objetivo: Intentar modificar una vista READ ONLY (ERROR ESPERADO).
-- Explicación: WITH READ ONLY impide INSERT/UPDATE/DELETE por la vista.
UPDATE vw_caneca_estacion SET caneca = 'X' WHERE id_contenedor = 11;
PROMPT <<<FIN>>>

PROMPT <<<E211>>>
-- E211 · 13. Vistas y seguridad · Mis privilegios de sistema
-- Objetivo: ¿Qué puede hacer mi cuenta?
-- Explicación: SESSION_PRIVS lista los privilegios activos de la sesión.
SELECT privilege FROM session_privs ORDER BY privilege;
PROMPT <<<FIN>>>

PROMPT <<<E212>>>
-- E212 · 13. Vistas y seguridad · Columnas actualizables de una vista
-- Objetivo: ¿Se puede escribir en VW_CANECA_ESTACION?
-- Explicación: USER_UPDATABLE_COLUMNS dice qué columnas aceptan UPDATE.
SELECT column_name, updatable FROM user_updatable_columns WHERE table_name = 'VW_CANECA_ESTACION' AND ROWNUM <= 6;
PROMPT <<<FIN>>>

PROMPT <<<E213>>>
-- E213 · 13. Vistas y seguridad · Error ESPERADO: vista de solo lectura
-- Objetivo: Intentar modificar una vista WITH READ ONLY (ORA-42399).
-- Explicación: Las vistas de Power BI no permiten escribir. ERROR ESPERADO.
UPDATE vw_caneca_estacion SET estacion = 'X' WHERE id_contenedor = 11;
PROMPT <<<FIN>>>

PROMPT <<<E214>>>
-- E214 · 13. Vistas y seguridad · Lo que ve Power BI
-- Objetivo: Ranking desde la vista con seudónimos.
-- Explicación: La vista nunca muestra el nombre real: solo C-0001.
SELECT ciudadano, COUNT(*) AS verificados FROM vw_bi_depositos WHERE estado = 'VERIFICADO' GROUP BY ciudadano ORDER BY verificados DESC FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E215>>>
-- E215 · 13. Vistas y seguridad · Vista de detalle
-- Objetivo: Un depósito con todo su contexto en una fila.
-- Explicación: VW_DEPOSITO_DETALLE junta 6 tablas para que la consulta sea simple.
SELECT id_deposito, titular, color, material_declarado, estado_texto, peso_estimado_g FROM vw_deposito_detalle WHERE ROWNUM <= 5;
PROMPT <<<FIN>>>

PROMPT <<<E216>>>
-- E216 · 14. PL/SQL básico · Hola mundo
-- Objetivo: El bloque anónimo más simple.
-- Explicación: BEGIN ... END; y DBMS_OUTPUT.PUT_LINE para escribir. En SQL Developer: Ver > Salida DBMS.
BEGIN
    DBMS_OUTPUT.PUT_LINE('Hola, EcoAccess');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E217>>>
-- E217 · 14. PL/SQL básico · Variables
-- Objetivo: Declarar, asignar y mostrar.
-- Explicación: DECLARE crea variables; := asigna.
DECLARE
    v_nombre VARCHAR2(50) := 'Caneca verde';
    v_kg     NUMBER := 0.25;
BEGIN
    DBMS_OUTPUT.PUT_LINE(v_nombre || ' recibió ' || v_kg * 1000 || ' g');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E218>>>
-- E218 · 14. PL/SQL básico · SELECT INTO
-- Objetivo: Guardar un resultado en una variable.
-- Explicación: SELECT ... INTO exige que salga exactamente UNA fila.
DECLARE
    v_total NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_total FROM deposito;
    DBMS_OUTPUT.PUT_LINE('Depósitos: ' || v_total);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E219>>>
-- E219 · 14. PL/SQL básico · %TYPE
-- Objetivo: La variable copia el tipo de una columna.
-- Explicación: Si la columna cambia de tamaño, la variable también.
DECLARE
    v_codigo material.codigo%TYPE;
BEGIN
    SELECT codigo INTO v_codigo FROM material WHERE id_material = 101;
    DBMS_OUTPUT.PUT_LINE('Material 101: ' || v_codigo);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E220>>>
-- E220 · 14. PL/SQL básico · %ROWTYPE
-- Objetivo: Una variable con TODA la fila.
-- Explicación: Cada campo se lee como v.columna.
DECLARE
    v material%ROWTYPE;
BEGIN
    SELECT * INTO v FROM material WHERE codigo = 'VIDRIO';
    DBMS_OUTPUT.PUT_LINE(v.nombre || ' - categoría ' || v.categoria);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E221>>>
-- E221 · 14. PL/SQL básico · IF / ELSIF / ELSE
-- Objetivo: Decidir el color de la caneca.
-- Explicación: IF evalúa condiciones en orden.
DECLARE
    v_cat material.categoria%TYPE := 2;
BEGIN
    IF v_cat = 1 THEN DBMS_OUTPUT.PUT_LINE('BLANCA');
    ELSIF v_cat = 2 THEN DBMS_OUTPUT.PUT_LINE('VERDE');
    ELSIF v_cat = 3 THEN DBMS_OUTPUT.PUT_LINE('NEGRA');
    ELSE DBMS_OUTPUT.PUT_LINE('Ruta especial');
    END IF;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E222>>>
-- E222 · 14. PL/SQL básico · CASE en PL/SQL
-- Objetivo: Lo mismo con CASE.
-- Explicación: CASE es más ordenado cuando hay muchos valores.
DECLARE
    v_estado NUMBER := 1;
BEGIN
    DBMS_OUTPUT.PUT_LINE(CASE v_estado WHEN 0 THEN 'Pendiente' WHEN 1 THEN 'Verificado' ELSE 'Rechazado' END);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E223>>>
-- E223 · 14. PL/SQL básico · LOOP con EXIT
-- Objetivo: Contar del 1 al 3.
-- Explicación: LOOP repite hasta que EXIT WHEN se cumple.
DECLARE
    i PLS_INTEGER := 1;
BEGIN
    LOOP
        DBMS_OUTPUT.PUT_LINE('Vuelta ' || i);
        i := i + 1;
        EXIT WHEN i > 3;
    END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E224>>>
-- E224 · 14. PL/SQL básico · WHILE
-- Objetivo: Repetir mientras se cumpla.
-- Explicación: WHILE revisa la condición antes de cada vuelta.
DECLARE
    v_gramos NUMBER := 0;
BEGIN
    WHILE v_gramos < 100 LOOP
        v_gramos := v_gramos + 22;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Botellas hasta pasar 100 g: ' || v_gramos / 22 || ' (' || v_gramos || ' g)');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E225>>>
-- E225 · 14. PL/SQL básico · FOR numérico
-- Objetivo: Tabla de multiplicar de puntos.
-- Explicación: FOR i IN 1..n crea el contador solo.
BEGIN
    FOR i IN 1 .. 5 LOOP
        DBMS_OUTPUT.PUT_LINE(i || ' botellas PET x 12 puntos = ' || i * 12);
    END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E226>>>
-- E226 · 14. PL/SQL básico · FOR REVERSE
-- Objetivo: Contar hacia atrás.
-- Explicación: REVERSE invierte el recorrido.
BEGIN
    FOR i IN REVERSE 1 .. 3 LOOP
        DBMS_OUTPUT.PUT_LINE('Cerrando tapa en ' || i);
    END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E227>>>
-- E227 · 14. PL/SQL básico · Bloque anidado
-- Objetivo: Un bloque dentro de otro.
-- Explicación: Cada bloque tiene sus variables y su EXCEPTION.
DECLARE
    v_externa NUMBER := 1;
BEGIN
    DECLARE
        v_interna NUMBER := 2;
    BEGIN
        DBMS_OUTPUT.PUT_LINE('Suma: ' || (v_externa + v_interna));
    END;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E228>>>
-- E228 · 14. PL/SQL básico · Constante
-- Objetivo: Una variable que no puede cambiar.
-- Explicación: CONSTANT obliga a dar valor y protege de cambios.
DECLARE
    c_ventana CONSTANT NUMBER := 10;
BEGIN
    DBMS_OUTPUT.PUT_LINE('La caneca queda abierta ' || c_ventana || ' segundos.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E229>>>
-- E229 · 14. PL/SQL básico · BOOLEAN
-- Objetivo: Variables verdadero/falso en PL/SQL.
-- Explicación: En PL/SQL existe BOOLEAN; en tablas se usa NUMBER(1).
DECLARE
    v_hay_pendientes BOOLEAN;
    v_n NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_n FROM deposito WHERE estado_verificacion = 0;
    v_hay_pendientes := v_n > 0;
    IF v_hay_pendientes THEN DBMS_OUTPUT.PUT_LINE('Hay ' || v_n || ' depósitos por verificar.'); END IF;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E230>>>
-- E230 · 14. PL/SQL básico · Registro propio (RECORD)
-- Objetivo: Agrupar varios datos en una variable.
-- Explicación: TYPE ... IS RECORD define una estructura.
DECLARE
    TYPE t_resumen IS RECORD (verificados NUMBER, pendientes NUMBER);
    r t_resumen;
BEGIN
    SELECT COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END), COUNT(CASE WHEN estado_verificacion = 0 THEN 1 END) INTO r.verificados, r.pendientes FROM deposito;
    DBMS_OUTPUT.PUT_LINE('Verificados=' || r.verificados || ' Pendientes=' || r.pendientes);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E231>>>
-- E231 · 14. PL/SQL básico · Colección y BULK COLLECT
-- Objetivo: Traer muchas filas de una sola vez.
-- Explicación: BULK COLLECT llena una colección; es más rápido que fila por fila.
DECLARE
    TYPE t_codigos IS TABLE OF material.codigo%TYPE;
    v t_codigos;
BEGIN
    SELECT codigo BULK COLLECT INTO v FROM material WHERE categoria = 1 ORDER BY codigo;
    DBMS_OUTPUT.PUT_LINE('Materiales blancos: ' || v.COUNT || '. Primero: ' || v(1) || ', último: ' || v(v.LAST));
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E232>>>
-- E232 · 14. PL/SQL básico · Arreglo asociativo
-- Objetivo: Una «tabla» indexada por texto.
-- Explicación: INDEX BY VARCHAR2 funciona como un diccionario.
DECLARE
    TYPE t_color IS TABLE OF VARCHAR2(10) INDEX BY PLS_INTEGER;
    color t_color;
BEGIN
    color(1) := 'BLANCA'; color(2) := 'VERDE'; color(3) := 'NEGRA';
    FOR i IN 1 .. 3 LOOP DBMS_OUTPUT.PUT_LINE(i || ' = ' || color(i)); END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E233>>>
-- E233 · 14. PL/SQL básico · CONTINUE en un ciclo
-- Objetivo: Saltar los números pares.
-- Explicación: CONTINUE pasa a la siguiente vuelta.
BEGIN
    FOR i IN 1 .. 7 LOOP
        CONTINUE WHEN MOD(i, 2) = 0;
        DBMS_OUTPUT.PUT_LINE('Impar: ' || i);
    END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E234>>>
-- E234 · 15. Cursores · Cursor implícito SQL%ROWCOUNT
-- Objetivo: Cuántas filas cambió un UPDATE.
-- Explicación: SQL% describe la última sentencia.
BEGIN
    UPDATE material SET habilitado = habilitado WHERE categoria = 1;
    DBMS_OUTPUT.PUT_LINE('Filas tocadas: ' || SQL%ROWCOUNT);
    ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E235>>>
-- E235 · 15. Cursores · Cursor FOR (implícito)
-- Objetivo: Recorrer filas fácilmente.
-- Explicación: Oracle abre, lee y cierra solo.
BEGIN
    FOR r IN (SELECT codigo, nombre FROM material WHERE categoria = 2 ORDER BY codigo) LOOP
        DBMS_OUTPUT.PUT_LINE(r.codigo || ': ' || r.nombre);
    END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E236>>>
-- E236 · 15. Cursores · Cursor explícito
-- Objetivo: OPEN, FETCH, CLOSE paso a paso.
-- Explicación: %NOTFOUND indica que no hay más filas.
DECLARE
    CURSOR c IS SELECT nombre FROM contenedor ORDER BY id_contenedor;
    v_nombre contenedor.nombre%TYPE;
BEGIN
    OPEN c;
    LOOP
        FETCH c INTO v_nombre;
        EXIT WHEN c%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(c%ROWCOUNT || '. ' || v_nombre);
    END LOOP;
    CLOSE c;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E237>>>
-- E237 · 15. Cursores · Cursor con parámetro
-- Objetivo: El mismo cursor para distintos colores.
-- Explicación: Los parámetros hacen reutilizable el cursor.
DECLARE
    CURSOR c (p_cat NUMBER) IS SELECT codigo FROM material WHERE categoria = p_cat ORDER BY codigo;
BEGIN
    FOR r IN c(3) LOOP
        DBMS_OUTPUT.PUT_LINE('Negra: ' || r.codigo);
    END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E238>>>
-- E238 · 15. Cursores · Cursor con %ROWTYPE
-- Objetivo: Leer filas completas del cursor.
-- Explicación: cursor%ROWTYPE toma la forma del SELECT.
DECLARE
    CURSOR c IS SELECT id_deposito, estado_texto, color FROM vw_deposito_detalle WHERE estado_verificacion = 2;
    r c%ROWTYPE;
BEGIN
    OPEN c;
    FETCH c INTO r;
    DBMS_OUTPUT.PUT_LINE('Primer rechazado: ' || r.id_deposito || ' en caneca ' || r.color);
    CLOSE c;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E239>>>
-- E239 · 15. Cursores · SYS_REFCURSOR
-- Objetivo: Cursor que se devuelve a otro programa (C#).
-- Explicación: Lo usa fn_depositos_ciudadano.
VARIABLE rc REFCURSOR
EXEC :rc := fn_depositos_ciudadano(6)
PRINT rc
PROMPT <<<FIN>>>

PROMPT <<<E240>>>
-- E240 · 15. Cursores · Acumular con cursor
-- Objetivo: Sumar kilos verificados con un cursor.
-- Explicación: Ejemplo de cálculo fila por fila (en SQL sería SUM).
DECLARE
    v_total NUMBER := 0;
BEGIN
    FOR r IN (SELECT peso_estimado_kg FROM deposito WHERE estado_verificacion = 1 AND peso_estimado_kg IS NOT NULL) LOOP
        v_total := v_total + r.peso_estimado_kg;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Kg estimados verificados: ' || ROUND(v_total, 3));
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E241>>>
-- E241 · 15. Cursores · Cursor FOR UPDATE
-- Objetivo: Bloquear filas para modificarlas (con ROLLBACK).
-- Explicación: WHERE CURRENT OF actualiza la fila del cursor.
DECLARE
    CURSOR c IS SELECT id_material, descripcion FROM material WHERE categoria = 4 FOR UPDATE OF descripcion;
BEGIN
    FOR r IN c LOOP
        UPDATE material SET descripcion = descripcion || ' (peligroso)' WHERE CURRENT OF c;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Actualizados y deshechos.');
    ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E242>>>
-- E242 · 15. Cursores · %FOUND y %NOTFOUND
-- Objetivo: Recorrer un cursor manualmente.
-- Explicación: %NOTFOUND se vuelve TRUE cuando ya no hay filas.
DECLARE
    CURSOR c IS SELECT nombre FROM contenedor ORDER BY id_contenedor;
    v contenedor.nombre%TYPE;
BEGIN
    OPEN c;
    LOOP
        FETCH c INTO v;
        EXIT WHEN c%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(c%ROWCOUNT || '. ' || v);
    END LOOP;
    CLOSE c;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E243>>>
-- E243 · 15. Cursores · %ISOPEN
-- Objetivo: Preguntar si un cursor está abierto.
-- Explicación: Evita el error de cerrar un cursor ya cerrado.
DECLARE
    CURSOR c IS SELECT 1 FROM dual;
BEGIN
    DBMS_OUTPUT.PUT_LINE('Antes: ' || CASE WHEN c%ISOPEN THEN 'abierto' ELSE 'cerrado' END);
    OPEN c;
    DBMS_OUTPUT.PUT_LINE('Después: ' || CASE WHEN c%ISOPEN THEN 'abierto' ELSE 'cerrado' END);
    CLOSE c;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E244>>>
-- E244 · 15. Cursores · Cursores anidados
-- Objetivo: Cada caneca con sus materiales.
-- Explicación: Un cursor FOR dentro de otro, usando un parámetro.
DECLARE
    CURSOR c_mat(p_cont NUMBER) IS SELECT m.codigo FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material WHERE cm.id_contenedor = p_cont ORDER BY m.codigo;
BEGIN
    FOR c IN (SELECT id_contenedor, nombre FROM contenedor WHERE id_punto_instalacion = 1 ORDER BY id_contenedor) LOOP
        DBMS_OUTPUT.PUT(c.nombre || ': ');
        FOR m IN c_mat(c.id_contenedor) LOOP DBMS_OUTPUT.PUT(m.codigo || ' '); END LOOP;
        DBMS_OUTPUT.NEW_LINE;
    END LOOP;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E245>>>
-- E245 · 15. Cursores · BULK COLLECT con LIMIT
-- Objetivo: Traer filas por lotes.
-- Explicación: LIMIT evita cargar millones de filas en memoria.
DECLARE
    CURSOR c IS SELECT id_consulta FROM consulta_vision;
    TYPE t_ids IS TABLE OF consulta_vision.id_consulta%TYPE;
    v t_ids;
    lotes NUMBER := 0;
BEGIN
    OPEN c;
    LOOP
        FETCH c BULK COLLECT INTO v LIMIT 50;
        EXIT WHEN v.COUNT = 0;
        lotes := lotes + 1;
        DBMS_OUTPUT.PUT_LINE('Lote ' || lotes || ': ' || v.COUNT || ' fotos');
    END LOOP;
    CLOSE c;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E246>>>
-- E246 · 15. Cursores · Cursor dinámico
-- Objetivo: OPEN FOR con un texto SQL.
-- Explicación: Útil cuando la consulta se arma en tiempo de ejecución; el parámetro va con USING.
DECLARE
    rc SYS_REFCURSOR;
    v_n NUMBER;
BEGIN
    OPEN rc FOR 'SELECT COUNT(*) FROM material WHERE categoria = :c' USING 2;
    FETCH rc INTO v_n;
    CLOSE rc;
    DBMS_OUTPUT.PUT_LINE('Materiales de la caneca verde: ' || v_n);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E247>>>
-- E247 · 15. Cursores · Recorrer el cursor de un paquete
-- Objetivo: Leer el ranking que devuelve PKG_ESTADISTICAS.
-- Explicación: Un SYS_REFCURSOR se recorre con FETCH igual que un cursor normal.
DECLARE
    rc SYS_REFCURSOR;
    v_id NUMBER; v_nom VARCHAR2(100); v_dep NUMBER; v_uni NUMBER; v_est NUMBER; v_med NUMBER; v_pue NUMBER;
BEGIN
    rc := pkg_estadisticas.ranking_verificados(SYSDATE - 30, SYSDATE, 3);
    LOOP
        FETCH rc INTO v_id, v_nom, v_dep, v_uni, v_est, v_med, v_pue;
        EXIT WHEN rc%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(v_pue || '. ' || v_nom || ' - ' || v_dep || ' verificados');
    END LOOP;
    CLOSE rc;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E248>>>
-- E248 · 16. Excepciones · NO_DATA_FOUND
-- Objetivo: SELECT INTO sin filas.
-- Explicación: Se captura y se da un mensaje amable.
DECLARE
    v material.nombre%TYPE;
BEGIN
    SELECT nombre INTO v FROM material WHERE codigo = 'NO_EXISTE';
EXCEPTION
    WHEN NO_DATA_FOUND THEN DBMS_OUTPUT.PUT_LINE('No existe ese material.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E249>>>
-- E249 · 16. Excepciones · TOO_MANY_ROWS
-- Objetivo: SELECT INTO con varias filas.
-- Explicación: SELECT INTO solo admite una fila.
DECLARE
    v material.nombre%TYPE;
BEGIN
    SELECT nombre INTO v FROM material WHERE categoria = 1;
EXCEPTION
    WHEN TOO_MANY_ROWS THEN DBMS_OUTPUT.PUT_LINE('Hay varios materiales blancos: usa un cursor.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E250>>>
-- E250 · 16. Excepciones · ZERO_DIVIDE
-- Objetivo: División por cero.
-- Explicación: Excepción predefinida de Oracle.
DECLARE
    v NUMBER;
BEGIN
    v := 10 / 0;
EXCEPTION
    WHEN ZERO_DIVIDE THEN DBMS_OUTPUT.PUT_LINE('No se puede dividir entre cero.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E251>>>
-- E251 · 16. Excepciones · Excepción propia
-- Objetivo: Declarar y lanzar una excepción.
-- Explicación: RAISE dispara la excepción declarada.
DECLARE
    e_peso_invalido EXCEPTION;
    v_kg NUMBER := -1;
BEGIN
    IF v_kg <= 0 THEN RAISE e_peso_invalido; END IF;
EXCEPTION
    WHEN e_peso_invalido THEN DBMS_OUTPUT.PUT_LINE('El peso debe ser positivo.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E252>>>
-- E252 · 16. Excepciones · RAISE_APPLICATION_ERROR
-- Objetivo: Error con número y mensaje propio.
-- Explicación: Rango permitido: -20000 a -20999. Así funcionan los triggers del proyecto.
BEGIN
    RAISE_APPLICATION_ERROR(-20555, 'Ejemplo de error de negocio');
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('Capturado: ' || SQLCODE || ' ' || SQLERRM);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E253>>>
-- E253 · 16. Excepciones · DUP_VAL_ON_INDEX
-- Objetivo: Capturar un duplicado.
-- Explicación: Excepción de Oracle para ORA-00001.
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Duplicado', 1);
EXCEPTION
    WHEN DUP_VAL_ON_INDEX THEN DBMS_OUTPUT.PUT_LINE('Ese código ya existe.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E254>>>
-- E254 · 16. Excepciones · PRAGMA EXCEPTION_INIT
-- Objetivo: Ponerle nombre a un error de Oracle.
-- Explicación: Asocia ORA-02291 a un nombre legible.
DECLARE
    e_padre_no_existe EXCEPTION;
    PRAGMA EXCEPTION_INIT(e_padre_no_existe, -2291);
BEGIN
    INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (999, 'Fantasma', 1);
EXCEPTION
    WHEN e_padre_no_existe THEN DBMS_OUTPUT.PUT_LINE('La estación 999 no existe.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E255>>>
-- E255 · 16. Excepciones · WHEN OTHERS con SQLERRM
-- Objetivo: Atrapar cualquier error y explicarlo.
-- Explicación: Úsalo con cuidado: no escondas errores.
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('x', 'minúscula', 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('Error ' || SQLCODE || ': ' || SUBSTR(SQLERRM, 1, 80));
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E256>>>
-- E256 · 16. Excepciones · VALUE_ERROR
-- Objetivo: Convertir texto que no es número.
-- Explicación: En PL/SQL, TO_NUMBER('abc') produce VALUE_ERROR.
DECLARE
    v NUMBER;
BEGIN
    v := TO_NUMBER('abc');
EXCEPTION
    WHEN VALUE_ERROR THEN DBMS_OUTPUT.PUT_LINE('No es un número: se atrapó VALUE_ERROR.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E257>>>
-- E257 · 16. Excepciones · SQLCODE
-- Objetivo: Leer el número del error.
-- Explicación: SQLCODE es negativo para errores de Oracle.
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Repetido', 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('SQLCODE = ' || SQLCODE); ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E258>>>
-- E258 · 16. Excepciones · Propagación
-- Objetivo: Un error interno sube al bloque de afuera.
-- Explicación: Si el bloque interno no lo atrapa, lo maneja el externo.
BEGIN
    BEGIN
        RAISE NO_DATA_FOUND;
    END;
    DBMS_OUTPUT.PUT_LINE('Esta línea no se ejecuta.');
EXCEPTION
    WHEN NO_DATA_FOUND THEN DBMS_OUTPUT.PUT_LINE('El bloque externo atrapó el error del interno.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E259>>>
-- E259 · 16. Excepciones · Atrapar un error de negocio
-- Objetivo: Capturar ORA-20813 de la función de canecas.
-- Explicación: PRAGMA EXCEPTION_INIT da nombre a un código propio.
DECLARE
    e_peligroso EXCEPTION;
    PRAGMA EXCEPTION_INIT(e_peligroso, -20813);
    v NUMBER;
BEGIN
    SELECT MIN(id_material) INTO v FROM material WHERE categoria = 4;
    v := fn_resolver_caneca_disponible(1, v);
EXCEPTION
    WHEN e_peligroso THEN DBMS_OUTPUT.PUT_LINE('Residuo peligroso: va por ruta especial, no a una caneca.');
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E260>>>
-- E260 · 16. Excepciones · Volver a lanzar (RAISE)
-- Objetivo: Registrar el error y dejar que siga.
-- Explicación: RAISE sin nombre relanza la misma excepción. ERROR ESPERADO.
BEGIN
    UPDATE material SET categoria = 9 WHERE codigo = 'PET';
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Registro: ' || SQLERRM);
        RAISE;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E261>>>
-- E261 · 16. Excepciones · Dónde ocurrió el error
-- Objetivo: FORMAT_ERROR_BACKTRACE indica la línea.
-- Explicación: Muy útil para depurar paquetes grandes.
DECLARE
    v NUMBER;
BEGIN
    v := 1 / 0;
EXCEPTION
    WHEN ZERO_DIVIDE THEN DBMS_OUTPUT.PUT_LINE('Error en: ' || TRIM(DBMS_UTILITY.FORMAT_ERROR_BACKTRACE));
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E262>>>
-- E262 · 17. Procedimientos, funciones y paquetes · Llamar una función en SELECT
-- Objetivo: Peso estimado de 3 botellas.
-- Explicación: Una función devuelve un valor y se puede usar en SQL.
SELECT fn_peso_estimado_kg(1, 3) AS kg, fn_peso_estimado_kg(1, 3) * 1000 AS g FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E263>>>
-- E263 · 17. Procedimientos, funciones y paquetes · Función que resuelve caneca
-- Objetivo: ¿A qué caneca va el PET en la estación 1?
-- Explicación: Devuelve el id de la caneca.
SELECT fn_resolver_caneca_disponible(1, 101) AS caneca_pet, fn_resolver_caneca_disponible(1, 201) AS caneca_fruta FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E264>>>
-- E264 · 17. Procedimientos, funciones y paquetes · Función de paquete
-- Objetivo: Saldo de EcoPuntos de un ciudadano.
-- Explicación: paquete.funcion(parámetros).
SELECT nombre, pkg_ecopuntos.saldo(id_usuario) AS saldo FROM usuario WHERE id_usuario IN (4, 6, 11);
PROMPT <<<FIN>>>

PROMPT <<<E265>>>
-- E265 · 17. Procedimientos, funciones y paquetes · Procedimiento con cursor de salida
-- Objetivo: Resumen de los últimos 30 días.
-- Explicación: EXEC llama al procedimiento; PRINT muestra el cursor.
VARIABLE rc REFCURSOR
EXEC pr_consultar_estadisticas(SYSDATE - 30, SYSDATE, NULL, NULL, :rc)
PRINT rc
PROMPT <<<FIN>>>

PROMPT <<<E266>>>
-- E266 · 17. Procedimientos, funciones y paquetes · Ranking por función
-- Objetivo: Top 5 de 30 días.
-- Explicación: La función devuelve un SYS_REFCURSOR.
VARIABLE rc REFCURSOR
EXEC :rc := pkg_estadisticas.ranking_verificados(SYSDATE - 30, SYSDATE, 5)
PRINT rc
PROMPT <<<FIN>>>

PROMPT <<<E267>>>
-- E267 · 17. Procedimientos, funciones y paquetes · Tarifa vigente
-- Objetivo: ¿Qué tarifa aplica hoy al PET?
-- Explicación: regla_vigente devuelve NULL si no hay tarifa.
SELECT r.id_regla, r.puntos, r.descripcion FROM regla_ecopuntos r WHERE r.id_regla = pkg_ecopuntos.regla_vigente(101, SYS_EXTRACT_UTC(SYSTIMESTAMP));
PROMPT <<<FIN>>>

PROMPT <<<E268>>>
-- E268 · 17. Procedimientos, funciones y paquetes · Verificar un depósito (y deshacer)
-- Objetivo: El flujo completo del operario.
-- Explicación: Verificar acredita puntos en la misma transacción.
DECLARE
    v_dep NUMBER; v_titular NUMBER; v_ver NUMBER; v_mat NUMBER; v_cant NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario, d.id_material_declarado, NVL(d.cantidad_declarada, 1) INTO v_dep, v_titular, v_mat, v_cant
    FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion
    WHERE d.estado_verificacion = 0 AND i.id_usuario IS NOT NULL FETCH FIRST 1 ROWS ONLY;
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';
    DBMS_OUTPUT.PUT_LINE('Saldo antes: ' || pkg_ecopuntos.saldo(v_titular));
    pkg_deposito.verificar(v_dep, v_ver, v_mat, v_cant);
    DBMS_OUTPUT.PUT_LINE('Saldo después: ' || pkg_ecopuntos.saldo(v_titular) || ' (si el material tiene tarifa)');
    ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E269>>>
-- E269 · 17. Procedimientos, funciones y paquetes · Error de negocio: verificar lo propio
-- Objetivo: Un titular no puede verificarse (ORA-20101).
-- Explicación: El trigger 2 protege la regla aunque se salte la aplicación.
DECLARE
    v_dep NUMBER; v_titular NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario INTO v_dep, v_titular FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden
    JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 AND i.id_usuario IS NOT NULL FETCH FIRST 1 ROWS ONLY;
    pkg_deposito.verificar(v_dep, v_titular, 101, 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 120)); ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E270>>>
-- E270 · 17. Procedimientos, funciones y paquetes · Fuente de un procedimiento
-- Objetivo: Leer el código guardado en Oracle.
-- Explicación: USER_SOURCE guarda el código fuente línea por línea.
SELECT line, text FROM user_source WHERE name = 'FN_PESO_ESTIMADO_KG' ORDER BY line FETCH FIRST 8 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E271>>>
-- E271 · 17. Procedimientos, funciones y paquetes · Parámetros de un paquete
-- Objetivo: Ver la firma de los procedimientos.
-- Explicación: USER_ARGUMENTS describe los parámetros.
SELECT object_name, argument_name, in_out, data_type FROM user_arguments WHERE package_name = 'PKG_DEPOSITO' AND object_name = 'VERIFICAR' ORDER BY position;
PROMPT <<<FIN>>>

PROMPT <<<E272>>>
-- E272 · 17. Procedimientos, funciones y paquetes · Peso estimado en un SELECT
-- Objetivo: Estimar 3 unidades de cada objeto del catálogo.
-- Explicación: Una función se usa como una columna más.
SELECT clave_objeto, gramos_tipico, fn_peso_estimado_kg(id_peso_referencial, 3) AS kg_3_unidades FROM peso_referencial WHERE ROWNUM <= 6;
PROMPT <<<FIN>>>

PROMPT <<<E273>>>
-- E273 · 17. Procedimientos, funciones y paquetes · Saldo con la función del paquete
-- Objetivo: Comparar la función con la vista.
-- Explicación: Ambas leen el mismo libro: deben coincidir.
SELECT s.nombre, s.saldo AS saldo_vista, pkg_ecopuntos.saldo(s.id_usuario) AS saldo_funcion FROM vw_saldo_ecopuntos s WHERE s.saldo > 0 FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E274>>>
-- E274 · 17. Procedimientos, funciones y paquetes · ¿Tiene el rol?
-- Objetivo: Usar tiene_rol en una consulta.
-- Explicación: Devuelve 1 o 0; no confundir con el usuario de Oracle.
SELECT nombre, pkg_contexto_app.tiene_rol(id_usuario, 2) AS es_operario, pkg_contexto_app.tiene_rol(id_usuario, 3) AS es_admin FROM usuario WHERE correo_electronico NOT LIKE 'ciudadano%' FETCH FIRST 5 ROWS ONLY;
PROMPT <<<FIN>>>

PROMPT <<<E275>>>
-- E275 · 17. Procedimientos, funciones y paquetes · Hora de Colombia a UTC
-- Objetivo: La función a_utc del paquete de estadísticas.
-- Explicación: Colombia está 5 horas detrás de UTC.
SELECT pkg_estadisticas.a_utc(DATE '2026-11-11') AS medianoche_colombia_en_utc FROM dual;
PROMPT <<<FIN>>>

PROMPT <<<E276>>>
-- E276 · 17. Procedimientos, funciones y paquetes · Procedimiento local
-- Objetivo: Un procedimiento que solo existe dentro del bloque.
-- Explicación: Sirve para no repetir código en un script.
DECLARE
    v NUMBER;   -- las variables van ANTES de los subprogramas locales (si no: PLS-00103)
    PROCEDURE mostrar(p_titulo VARCHAR2, p_valor NUMBER) IS
    BEGIN
        DBMS_OUTPUT.PUT_LINE(RPAD(p_titulo, 22, '.') || p_valor);
    END;
BEGIN
    SELECT COUNT(*) INTO v FROM deposito; mostrar('Depósitos', v);
    SELECT COUNT(*) INTO v FROM consulta_vision; mostrar('Fotos', v);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E277>>>
-- E277 · 17. Procedimientos, funciones y paquetes · Rechazar un depósito (y deshacer)
-- Objetivo: El operario rechaza con motivo.
-- Explicación: pkg_deposito.rechazar exige motivo; el trigger 3 audita como RECHAZO.
DECLARE
    v_dep NUMBER; v_ver NUMBER; v_op VARCHAR2(12);
BEGIN
    SELECT MIN(d.id_deposito) INTO v_dep FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario u ON u.id_usuario = i.id_usuario WHERE d.estado_verificacion = 0 AND u.correo_electronico <> 'operaria.dos@ejemplo.test';
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'operaria.dos@ejemplo.test';
    pkg_deposito.rechazar(v_dep, v_ver, 'Ejercicio de la guía: residuo no encontrado');
    SELECT operacion INTO v_op FROM auditoria_deposito WHERE id_deposito = v_dep AND id_auditoria = (SELECT MAX(id_auditoria) FROM auditoria_deposito WHERE id_deposito = v_dep);
    DBMS_OUTPUT.PUT_LINE('Depósito ' || v_dep || ' auditado como ' || v_op);
    ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E278>>>
-- E278 · 18. Disparadores (triggers) · Ver los disparadores
-- Objetivo: Listar triggers y su momento.
-- Explicación: BEFORE/AFTER, EACH ROW y el evento.
SELECT trigger_name, trigger_type, triggering_event, table_name FROM user_triggers ORDER BY table_name;
PROMPT <<<FIN>>>

PROMPT <<<E279>>>
-- E279 · 18. Disparadores (triggers) · Trigger 1 en acción (y deshacer)
-- Objetivo: Cambiar el estado de una caneca deja historia.
-- Explicación: AFTER UPDATE OF estado escribe en HISTORIAL_ESTADO_CONTENEDOR.
DECLARE
    v_adm NUMBER; v_antes NUMBER; v_despues NUMBER;
BEGIN
    SELECT id_usuario INTO v_adm FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT COUNT(*) INTO v_antes FROM historial_estado_contenedor;
    pr_cambiar_estado_contenedor(12, 2, v_adm, 'Ejercicio de la guía');
    SELECT COUNT(*) INTO v_despues FROM historial_estado_contenedor;
    DBMS_OUTPUT.PUT_LINE('Historial: ' || v_antes || ' -> ' || v_despues);
    ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E280>>>
-- E280 · 18. Disparadores (triggers) · Trigger 4 en acción
-- Objetivo: UPDATE directo de un depósito verificado (ORA-20103).
-- Explicación: BEFORE UPDATE bloquea; solo el procedimiento de corrección puede.
DECLARE
    v_dep NUMBER;
BEGIN
    SELECT MIN(id_deposito) INTO v_dep FROM deposito WHERE estado_verificacion = 1;
    UPDATE deposito SET cantidad_verificada = 99 WHERE id_deposito = v_dep;
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E281>>>
-- E281 · 18. Disparadores (triggers) · Trigger 3: leer la auditoría
-- Objetivo: Ver :OLD y :NEW guardados.
-- Explicación: Antes y después de cada corrección.
SELECT id_deposito, operacion, estado_anterior, estado_nuevo, cantidad_anterior, cantidad_nueva, motivo FROM auditoria_deposito WHERE operacion IN ('CORRECCION', 'REAPERTURA');
PROMPT <<<FIN>>>

PROMPT <<<E282>>>
-- E282 · 18. Disparadores (triggers) · Trigger 2: material que la caneca no admite
-- Objetivo: Restos de fruta en la caneca blanca (ORA-20102).
-- Explicación: BEFORE INSERT revisa CONTENEDOR_MATERIAL.
DECLARE
    v_o NUMBER; v_e NUMBER;
BEGIN
    SELECT MIN(id_orden) INTO v_o FROM orden_apertura WHERE estado = 7 AND id_contenedor = 11;
    INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado) VALUES (11, 'GUIA-EJ', 3, v_o, 1) RETURNING id_evento INTO v_e;
    INSERT INTO deposito (id_orden, id_evento, id_material_declarado) VALUES (v_o, v_e, 201);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E283>>>
-- E283 · 18. Disparadores (triggers) · Orden de los triggers
-- Objetivo: ¿Qué trigger sigue a cuál?
-- Explicación: FOLLOWS define el orden entre triggers del mismo momento.
SELECT trigger_name, referenced_trigger_name FROM user_trigger_ordering;
PROMPT <<<FIN>>>

PROMPT <<<E284>>>
-- E284 · 18. Disparadores (triggers) · Tipo y evento de cada trigger
-- Objetivo: Leer cuándo dispara cada uno.
-- Explicación: TRIGGER_TYPE dice BEFORE/AFTER y EACH ROW.
SELECT trigger_name, trigger_type, triggering_event, status FROM user_triggers ORDER BY trigger_name;
PROMPT <<<FIN>>>

PROMPT <<<E285>>>
-- E285 · 18. Disparadores (triggers) · La cláusula WHEN guardada
-- Objetivo: Ver el WHEN de los triggers.
-- Explicación: En WHEN se escribe OLD/NEW sin dos puntos.
SELECT trigger_name, when_clause FROM user_triggers WHERE when_clause IS NOT NULL;
PROMPT <<<FIN>>>

PROMPT <<<E286>>>
-- E286 · 18. Disparadores (triggers) · Trigger 1 con el mismo estado
-- Objetivo: Un UPDATE que no cambia el estado no deja historia.
-- Explicación: WHEN (OLD.estado <> NEW.estado) evita registros falsos.
DECLARE
    v_antes NUMBER; v_despues NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_antes FROM historial_estado_contenedor;
    UPDATE contenedor SET estado = estado WHERE id_contenedor = 11;
    SELECT COUNT(*) INTO v_despues FROM historial_estado_contenedor;
    DBMS_OUTPUT.PUT_LINE('Historial: ' || v_antes || ' -> ' || v_despues || ' (no cambia)');
    ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E287>>>
-- E287 · 18. Disparadores (triggers) · Trigger 2: autoverificación
-- Objetivo: El titular intenta verificar lo suyo (ORA-20101).
-- Explicación: El BEFORE compara el verificador con el titular de la sesión.
DECLARE
    v_dep NUMBER; v_tit NUMBER;
BEGIN
    SELECT d.id_deposito, i.id_usuario INTO v_dep, v_tit FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 FETCH FIRST 1 ROWS ONLY;
    UPDATE deposito SET estado_verificacion = 1, id_verificador = v_tit, fecha_verificacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP) WHERE id_deposito = v_dep;
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE(SUBSTR(SQLERRM, 1, 110)); ROLLBACK;
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E288>>>
-- E288 · 18. Disparadores (triggers) · Trigger 3: auditoría deshecha
-- Objetivo: Sin COMMIT en el trigger, el ROLLBACK borra también su auditoría.
-- Explicación: La auditoría pertenece a la misma transacción.
DECLARE
    v_dep NUMBER; v_ver NUMBER; v_mat NUMBER; v_antes NUMBER; v_durante NUMBER; v_despues NUMBER;
BEGIN
    SELECT id_usuario INTO v_ver FROM usuario WHERE correo_electronico = 'administradora@ejemplo.test';
    SELECT d.id_deposito, d.id_material_declarado INTO v_dep, v_mat FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion WHERE d.estado_verificacion = 0 AND i.id_usuario <> v_ver FETCH FIRST 1 ROWS ONLY;
    SELECT COUNT(*) INTO v_antes FROM auditoria_deposito;
    pkg_deposito.verificar(v_dep, v_ver, v_mat, 1);
    SELECT COUNT(*) INTO v_durante FROM auditoria_deposito;
    ROLLBACK;
    SELECT COUNT(*) INTO v_despues FROM auditoria_deposito;
    DBMS_OUTPUT.PUT_LINE('Auditoría: ' || v_antes || ' -> ' || v_durante || ' -> (ROLLBACK) ' || v_despues);
END;
/
PROMPT <<<FIN>>>

PROMPT <<<E289>>>
-- E289 · 18. Disparadores (triggers) · Historial real de canecas
-- Objetivo: Lo que dejó el trigger 1 en la carga.
-- Explicación: Cada fila es un cambio de disponibilidad con su actor.
SELECT h.id_contenedor, h.estado_anterior, h.estado_nuevo, u.nombre AS actor, h.motivo FROM historial_estado_contenedor h LEFT JOIN usuario u ON u.id_usuario = h.id_actor ORDER BY h.id_historial;
PROMPT <<<FIN>>>
ROLLBACK;
