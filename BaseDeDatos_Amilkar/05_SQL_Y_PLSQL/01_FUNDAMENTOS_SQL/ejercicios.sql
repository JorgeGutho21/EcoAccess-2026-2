-- 01_FUNDAMENTOS_SQL — 57 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E001 · 1. Conexión y diccionario de datos · ¿Con qué usuario estoy?
-- Objetivo: Saber quién y dónde estoy conectado antes de hacer cualquier cosa.
-- Explicación: USER devuelve la cuenta Oracle; SYS_CONTEXT pregunta datos de la sesión. DUAL es una tabla de una fila que sirve para hacer cálculos.
SELECT USER AS usuario, SYS_CONTEXT('USERENV','CON_NAME') AS base_de_datos FROM dual;

-- ===== E002 · 1. Conexión y diccionario de datos · Fecha y hora del servidor
-- Objetivo: Ver la hora del servidor y la de Colombia.
-- Explicación: SYSDATE es la fecha del servidor; AT TIME ZONE convierte a otra zona.
SELECT SYSDATE AS hora_servidor, CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE) AS hora_colombia FROM dual;

-- ===== E003 · 1. Conexión y diccionario de datos · Mis tablas
-- Objetivo: Listar las tablas de mi esquema.
-- Explicación: USER_TABLES es una vista del diccionario: Oracle guarda ahí la lista de MIS tablas.
SELECT table_name FROM user_tables ORDER BY table_name;

-- ===== E004 · 1. Conexión y diccionario de datos · Columnas de una tabla
-- Objetivo: Ver columnas, tipos y si aceptan NULL.
-- Explicación: Es lo mismo que el comando DESCRIBE, pero como consulta.
SELECT column_name, data_type, nullable FROM user_tab_columns WHERE table_name = 'MATERIAL' ORDER BY column_id;

-- ===== E005 · 1. Conexión y diccionario de datos · Restricciones de una tabla
-- Objetivo: Ver PK, FK, UNIQUE y CHECK de DEPOSITO.
-- Explicación: constraint_type: P = clave primaria, R = foránea, U = única, C = CHECK o NOT NULL.
SELECT constraint_name, constraint_type FROM user_constraints WHERE table_name = 'DEPOSITO' ORDER BY constraint_type, constraint_name;

-- ===== E006 · 1. Conexión y diccionario de datos · Comentario de una columna
-- Objetivo: Leer el diccionario guardado dentro de Oracle.
-- Explicación: COMMENT ON guarda la explicación; USER_COL_COMMENTS la muestra.
SELECT column_name, comments FROM user_col_comments WHERE table_name = 'CONTENEDOR' ORDER BY column_name;

-- ===== E007 · 1. Conexión y diccionario de datos · Cuántos objetos hay de cada tipo
-- Objetivo: Inventario del esquema.
-- Explicación: GROUP BY agrupa las filas iguales y COUNT las cuenta.
SELECT object_type, COUNT(*) AS cantidad FROM user_objects GROUP BY object_type ORDER BY object_type;

-- ===== E008 · 1. Conexión y diccionario de datos · ¿Hay objetos inválidos?
-- Objetivo: Comprobar que todo compiló.
-- Explicación: Si la consulta no devuelve filas, todo está válido.
SELECT object_name, object_type FROM user_objects WHERE status <> 'VALID';

-- ===== E009 · 1. Conexión y diccionario de datos · Índices de una tabla
-- Objetivo: Ver qué índices tiene DEPOSITO y si son únicos.
-- Explicación: USER_INDEXES lista los índices; UNIQUENESS dice si impiden repetidos.
SELECT index_name, uniqueness FROM user_indexes WHERE table_name = 'DEPOSITO' ORDER BY index_name;

-- ===== E010 · 1. Conexión y diccionario de datos · Índices funcionales
-- Objetivo: Ver la expresión de los índices únicos de reglas de negocio.
-- Explicación: USER_IND_EXPRESSIONS guarda el CASE que solo indexa filas activas.
SELECT index_name, column_expression FROM user_ind_expressions WHERE index_name LIKE 'UQ_%' ORDER BY index_name;

-- ===== E011 · 1. Conexión y diccionario de datos · Columnas IDENTITY
-- Objetivo: Saber qué tablas generan su id solas.
-- Explicación: GENERATION_TYPE: ALWAYS (nadie escribe el id) o BY DEFAULT.
SELECT table_name, column_name, generation_type FROM user_tab_identity_cols ORDER BY table_name;

-- ===== E012 · 1. Conexión y diccionario de datos · Columnas de una FK compuesta
-- Objetivo: Ver las dos columnas de una clave foránea.
-- Explicación: USER_CONS_COLUMNS dice qué columnas forman cada restricción y en qué orden.
SELECT c.constraint_name, cc.column_name, cc.position FROM user_constraints c JOIN user_cons_columns cc ON cc.constraint_name = c.constraint_name WHERE c.table_name = 'CONTENEDOR_MATERIAL' AND c.constraint_type = 'R' ORDER BY c.constraint_name, cc.position;

-- ===== E013 · 1. Conexión y diccionario de datos · Comentario de las tablas
-- Objetivo: Leer la explicación de cada tabla.
-- Explicación: USER_TAB_COMMENTS guarda el COMMENT ON TABLE.
SELECT table_name, SUBSTR(comments, 1, 70) AS comentario FROM user_tab_comments WHERE table_type = 'TABLE' ORDER BY table_name;

-- ===== E014 · 1. Conexión y diccionario de datos · Dependencias de un paquete
-- Objetivo: ¿Qué tablas usa PKG_ECOPUNTOS?
-- Explicación: USER_DEPENDENCIES muestra de qué objetos depende otro.
SELECT DISTINCT referenced_name, referenced_type FROM user_dependencies WHERE name = 'PKG_ECOPUNTOS' AND referenced_owner = USER ORDER BY referenced_type, referenced_name;

-- ===== E066 · 5. Funciones de una fila · UPPER y LOWER
-- Objetivo: Cambiar mayúsculas.
-- Explicación: UPPER todo en mayúsculas; LOWER en minúsculas.
SELECT UPPER(nombre) AS mayus, LOWER(codigo) AS minusculas FROM material WHERE ROWNUM <= 3;

-- ===== E067 · 5. Funciones de una fila · LENGTH
-- Objetivo: Largo de un texto.
-- Explicación: LENGTH cuenta caracteres.
SELECT codigo, LENGTH(codigo) AS letras FROM material ORDER BY letras DESC FETCH FIRST 3 ROWS ONLY;

-- ===== E068 · 5. Funciones de una fila · SUBSTR e INSTR
-- Objetivo: Sacar el usuario de un correo.
-- Explicación: INSTR busca la posición de '@'; SUBSTR corta.
SELECT correo_electronico, SUBSTR(correo_electronico, 1, INSTR(correo_electronico, '@') - 1) AS usuario FROM usuario WHERE ROWNUM <= 3;

-- ===== E069 · 5. Funciones de una fila · REPLACE
-- Objetivo: Reemplazar texto.
-- Explicación: REPLACE cambia un texto por otro.
SELECT REPLACE(clave_objeto, '_', ' ') AS legible FROM peso_referencial WHERE ROWNUM <= 3;

-- ===== E070 · 5. Funciones de una fila · ROUND y TRUNC
-- Objetivo: Redondear números.
-- Explicación: ROUND redondea; TRUNC corta sin redondear.
SELECT 2.675 AS valor, ROUND(2.675, 2) AS redondeado, TRUNC(2.675, 2) AS truncado FROM dual;

-- ===== E071 · 5. Funciones de una fila · MOD
-- Objetivo: Residuo de una división.
-- Explicación: MOD(10,3) = 1. Sirve para saber si un número es par.
SELECT id_deposito, MOD(id_deposito, 2) AS es_impar FROM deposito WHERE ROWNUM <= 4;

-- ===== E072 · 5. Funciones de una fila · NVL
-- Objetivo: Reemplazar NULL por un valor.
-- Explicación: NVL(x, 0) devuelve 0 cuando x es NULL.
SELECT id_deposito, NVL(peso_medido_kg, 0) AS medido_o_cero FROM deposito WHERE ROWNUM <= 5;

-- ===== E073 · 5. Funciones de una fila · NVL2
-- Objetivo: Un valor si hay dato, otro si no.
-- Explicación: NVL2(x, si_hay, si_no_hay).
SELECT id_deposito, NVL2(peso_medido_kg, 'Medido', 'Solo estimado') AS tipo_peso FROM deposito WHERE ROWNUM <= 5;

-- ===== E074 · 5. Funciones de una fila · COALESCE
-- Objetivo: Primer valor no nulo.
-- Explicación: COALESCE(a, b, c) devuelve el primero que tenga dato.
SELECT id_deposito, COALESCE(cantidad_verificada, cantidad_declarada, 1) AS unidades FROM deposito WHERE ROWNUM <= 5;

-- ===== E075 · 5. Funciones de una fila · TO_CHAR con fechas
-- Objetivo: Mostrar la fecha con formato.
-- Explicación: TO_CHAR convierte fecha en texto con el formato que quieras.
SELECT TO_CHAR(SYSDATE, 'DD/MM/YYYY HH24:MI') AS ahora, TO_CHAR(SYSDATE, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia FROM dual;

-- ===== E076 · 5. Funciones de una fila · TO_DATE
-- Objetivo: Convertir texto en fecha.
-- Explicación: Hay que decir el formato del texto.
SELECT TO_DATE('20/09/2026', 'DD/MM/YYYY') AS fecha_avance FROM dual;

-- ===== E077 · 5. Funciones de una fila · Diferencia de fechas
-- Objetivo: Días entre dos fechas.
-- Explicación: Restar dos DATE da días.
SELECT TO_DATE('22/09/2026','DD/MM/YYYY') - TO_DATE('18/09/2026','DD/MM/YYYY') AS dias_del_avance FROM dual;

-- ===== E078 · 5. Funciones de una fila · ADD_MONTHS y LAST_DAY
-- Objetivo: Fechas de calendario.
-- Explicación: ADD_MONTHS suma meses; LAST_DAY da el último día del mes.
SELECT ADD_MONTHS(TRUNC(SYSDATE), 1) AS en_un_mes, LAST_DAY(SYSDATE) AS fin_de_mes FROM dual;

-- ===== E079 · 5. Funciones de una fila · DECODE
-- Objetivo: Traducir códigos (estilo Oracle).
-- Explicación: DECODE(valor, si1, entonces1, ..., otro).
SELECT codigo, DECODE(categoria, 1, 'Blanca', 2, 'Verde', 3, 'Negra', 'Ruta especial') AS caneca FROM material WHERE ROWNUM <= 6;

-- ===== E080 · 5. Funciones de una fila · Gramos y kilogramos
-- Objetivo: Mostrar el mismo peso en g y kg.
-- Explicación: Unidad clara: 1 kg = 1000 g.
SELECT id_deposito, peso_estimado_kg AS kg, peso_estimado_kg * 1000 AS g FROM deposito WHERE peso_estimado_kg IS NOT NULL AND ROWNUM <= 5;

-- ===== E081 · 5. Funciones de una fila · INITCAP
-- Objetivo: Poner en mayúscula la primera letra.
-- Explicación: INITCAP convierte «cartón» en «Cartón».
SELECT codigo, INITCAP(LOWER(codigo)) AS bonito FROM material WHERE ROWNUM <= 5;

-- ===== E082 · 5. Funciones de una fila · LPAD y RPAD
-- Objetivo: Rellenar para alinear.
-- Explicación: LPAD agrega caracteres a la izquierda: sirve para seudónimos como C-0007.
SELECT 'C-' || LPAD(7, 4, '0') AS seudonimo, RPAD('PET', 8, '.') || '|' AS relleno FROM dual;

-- ===== E083 · 5. Funciones de una fila · TRIM
-- Objetivo: Quitar espacios sobrantes.
-- Explicación: TRIM limpia al inicio y al final; por eso el CHECK de nombre lo exige.
SELECT '[' || TRIM('   María   ') || ']' AS limpio, LENGTH('   María   ') AS antes, LENGTH(TRIM('   María   ')) AS despues FROM dual;

-- ===== E084 · 5. Funciones de una fila · GREATEST y LEAST
-- Objetivo: El mayor y el menor de varios valores.
-- Explicación: Comparan columnas de una misma fila.
SELECT clave_objeto, GREATEST(gramos_minimo, gramos_tipico, gramos_maximo) AS mayor, LEAST(gramos_minimo, gramos_tipico, gramos_maximo) AS menor FROM peso_referencial WHERE ROWNUM <= 5;

-- ===== E085 · 5. Funciones de una fila · CEIL y FLOOR
-- Objetivo: Redondear hacia arriba y hacia abajo.
-- Explicación: CEIL(2.1)=3 y FLOOR(2.9)=2.
SELECT CEIL(2.1) AS techo, FLOOR(2.9) AS piso, CEIL(-2.1) AS techo_negativo FROM dual;

-- ===== E086 · 5. Funciones de una fila · NULLIF
-- Objetivo: Evitar dividir por cero.
-- Explicación: NULLIF(a, b) devuelve NULL si a = b; dividir por NULL da NULL, no error.
SELECT 10 / NULLIF(0, 0) AS sin_error, NVL(10 / NULLIF(0, 0), 0) AS con_cero FROM dual;

-- ===== E087 · 5. Funciones de una fila · REGEXP_LIKE
-- Objetivo: Validar un correo con expresión regular.
-- Explicación: Es la misma idea que usa el CHECK del correo.
SELECT correo_electronico FROM usuario WHERE REGEXP_LIKE(correo_electronico, '^[a-z0-9._-]+@ejemplo\.test$') FETCH FIRST 5 ROWS ONLY;

-- ===== E180 · 11. Fechas y hora de Colombia · UTC a Colombia
-- Objetivo: Convertir una fecha guardada en UTC.
-- Explicación: FROM_TZ dice «esta hora es UTC»; AT TIME ZONE la pasa a Bogotá (UTC−5).
SELECT fecha_deteccion_utc, CAST(FROM_TZ(fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS colombia FROM deposito WHERE ROWNUM <= 3;

-- ===== E181 · 11. Fechas y hora de Colombia · Hoy en Colombia
-- Objetivo: Depósitos de hoy (hora Colombia).
-- Explicación: Se compara con el inicio del día local.
SELECT COUNT(*) AS hoy FROM vw_deposito_detalle WHERE fecha_colombia >= TRUNC(CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE));

-- ===== E182 · 11. Fechas y hora de Colombia · Semana calendario actual
-- Objetivo: Lunes a domingo de esta semana.
-- Explicación: TRUNC(fecha,'IW') = lunes.
SELECT TRUNC(SYSDATE, 'IW') AS lunes, TRUNC(SYSDATE, 'IW') + 6 AS domingo FROM dual;

-- ===== E183 · 11. Fechas y hora de Colombia · Mes calendario
-- Objetivo: Depósitos de este mes.
-- Explicación: TRUNC(fecha,'MM') = día 1 del mes.
SELECT COUNT(*) FROM vw_deposito_detalle WHERE fecha_colombia >= TRUNC(SYSDATE, 'MM');

-- ===== E184 · 11. Fechas y hora de Colombia · Diferencia de TIMESTAMP
-- Objetivo: Segundos entre emitir y cerrar una orden.
-- Explicación: EXTRACT saca partes de un intervalo.
SELECT id_orden, EXTRACT(SECOND FROM (fecha_cierre_utc - fecha_emision_utc)) + 60 * EXTRACT(MINUTE FROM (fecha_cierre_utc - fecha_emision_utc)) AS segundos FROM orden_apertura WHERE estado = 6 FETCH FIRST 5 ROWS ONLY;

-- ===== E185 · 11. Fechas y hora de Colombia · Día de la semana con más uso
-- Objetivo: ¿Qué día se recicla más?
-- Explicación: TO_CHAR(fecha,'D') o el nombre del día.
SELECT TO_CHAR(fecha_colombia, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia, COUNT(*) FROM vw_deposito_detalle GROUP BY TO_CHAR(fecha_colombia, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') ORDER BY 2 DESC;

-- ===== E186 · 11. Fechas y hora de Colombia · FROM_TZ
-- Objetivo: Declarar que una hora está en UTC y pasarla a Colombia.
-- Explicación: FROM_TZ le pone zona a un TIMESTAMP sin zona; AT TIME ZONE la convierte.
SELECT id_consulta, fecha_utc, FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS hora_colombia FROM consulta_vision WHERE ROWNUM <= 3;

-- ===== E187 · 11. Fechas y hora de Colombia · Hora del día
-- Objetivo: ¿A qué hora de Colombia se toman más fotos?
-- Explicación: EXTRACT(HOUR ...) saca la hora; se resta la zona antes.
SELECT EXTRACT(HOUR FROM (FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota')) AS hora, COUNT(*) AS fotos FROM consulta_vision GROUP BY EXTRACT(HOUR FROM (FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota')) ORDER BY fotos DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E188 · 11. Fechas y hora de Colombia · Lunes de la semana
-- Objetivo: TRUNC(fecha, 'IW') da el lunes ISO.
-- Explicación: Sirve para agrupar por semana calendario.
SELECT TRUNC(DATE '2026-11-11', 'IW') AS lunes, TRUNC(DATE '2026-11-11', 'IW') + 6 AS domingo FROM dual;

-- ===== E189 · 11. Fechas y hora de Colombia · INTERVAL
-- Objetivo: Sumar 10 segundos (la ventana de depósito).
-- Explicación: INTERVAL representa una duración.
SELECT SYSTIMESTAMP AS ahora, SYSTIMESTAMP + INTERVAL '10' SECOND AS cierre_ventana FROM dual;

-- ===== E190 · 11. Fechas y hora de Colombia · Minutos entre emisión y vencimiento
-- Objetivo: Cuánto vive una orden de apertura.
-- Explicación: Restar TIMESTAMP da un INTERVAL; EXTRACT saca sus partes.
SELECT id_orden, EXTRACT(MINUTE FROM (fecha_vencimiento_utc - fecha_emision_utc)) AS minutos, EXTRACT(SECOND FROM (fecha_vencimiento_utc - fecha_emision_utc)) AS segundos FROM orden_apertura WHERE ROWNUM <= 5;

-- ===== E191 · 11. Fechas y hora de Colombia · Nombre del día
-- Objetivo: Día de la semana en español.
-- Explicación: TO_CHAR con 'Day' y el idioma indicado.
SELECT TO_CHAR(DATE '2026-11-11', 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia FROM dual;

-- ===== E207 · 13. Vistas y seguridad · Vista para Power BI
-- Objetivo: Ver qué muestra la vista anónima.
-- Explicación: El ciudadano aparece como C-0001: sin nombre ni correo.
SELECT ciudadano, dia, caneca, material, estado, origen_dato FROM vw_bi_depositos FETCH FIRST 5 ROWS ONLY;

-- ===== E208 · 13. Vistas y seguridad · Privilegios concedidos
-- Objetivo: Qué puede leer el rol de informes.
-- Explicación: USER_TAB_PRIVS_MADE muestra lo que yo concedí.
SELECT grantee, table_name, privilege FROM user_tab_privs_made ORDER BY table_name;

-- ===== E209 · 13. Vistas y seguridad · Texto de una vista
-- Objetivo: Ver la consulta guardada.
-- Explicación: USER_VIEWS.TEXT contiene el SELECT de la vista.
SELECT view_name, text_length FROM user_views ORDER BY view_name;

-- ===== E210 · 13. Vistas y seguridad · Vista de solo lectura
-- Objetivo: Intentar modificar una vista READ ONLY (ERROR ESPERADO).
-- Explicación: WITH READ ONLY impide INSERT/UPDATE/DELETE por la vista.
UPDATE vw_caneca_estacion SET caneca = 'X' WHERE id_contenedor = 11;

-- ===== E211 · 13. Vistas y seguridad · Mis privilegios de sistema
-- Objetivo: ¿Qué puede hacer mi cuenta?
-- Explicación: SESSION_PRIVS lista los privilegios activos de la sesión.
SELECT privilege FROM session_privs ORDER BY privilege;

-- ===== E212 · 13. Vistas y seguridad · Columnas actualizables de una vista
-- Objetivo: ¿Se puede escribir en VW_CANECA_ESTACION?
-- Explicación: USER_UPDATABLE_COLUMNS dice qué columnas aceptan UPDATE.
SELECT column_name, updatable FROM user_updatable_columns WHERE table_name = 'VW_CANECA_ESTACION' AND ROWNUM <= 6;

-- ===== E213 · 13. Vistas y seguridad · Error ESPERADO: vista de solo lectura
-- Objetivo: Intentar modificar una vista WITH READ ONLY (ORA-42399).
-- Explicación: Las vistas de Power BI no permiten escribir. ERROR ESPERADO.
UPDATE vw_caneca_estacion SET estacion = 'X' WHERE id_contenedor = 11;

-- ===== E214 · 13. Vistas y seguridad · Lo que ve Power BI
-- Objetivo: Ranking desde la vista con seudónimos.
-- Explicación: La vista nunca muestra el nombre real: solo C-0001.
SELECT ciudadano, COUNT(*) AS verificados FROM vw_bi_depositos WHERE estado = 'VERIFICADO' GROUP BY ciudadano ORDER BY verificados DESC FETCH FIRST 5 ROWS ONLY;

-- ===== E215 · 13. Vistas y seguridad · Vista de detalle
-- Objetivo: Un depósito con todo su contexto en una fila.
-- Explicación: VW_DEPOSITO_DETALLE junta 6 tablas para que la consulta sea simple.
SELECT id_deposito, titular, color, material_declarado, estado_texto, peso_estimado_g FROM vw_deposito_detalle WHERE ROWNUM <= 5;
