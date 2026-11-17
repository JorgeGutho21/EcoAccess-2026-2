-- EcoAccess BD | Practicas de lectura para SQL*Plus.
-- Abre una sesion NUEVA como ECOACCESS_BD en FREEPDB1 y usa @ con este archivo.
-- Contiene SELECT y bloques de lectura o de contexto de sesion.
-- Cada PROMPT indica la seccion. Puedes copiar y ejecutar una seccion.
-- La salida puede incluir datos de estudio. Revisa antes de compartir.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET VERIFY OFF
SET AUTOCOMMIT OFF
SET SQLBLANKLINES ON
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LINESIZE 200
SET PAGESIZE 60
BEGIN
    IF SYS_CONTEXT('USERENV','SESSION_USER') <> 'ECOACCESS_BD'
       OR UPPER(SYS_CONTEXT('USERENV','CON_NAME')) <> 'FREEPDB1' THEN
        RAISE_APPLICATION_ERROR(-20090,'Conecta como ECOACCESS_BD en FREEPDB1.');
    END IF;
END;
/
SET TRANSACTION READ ONLY;


PROMPT === entorno - entorno ===
SHOW USER
SELECT SYS_CONTEXT('USERENV','SESSION_USER') AS usuario,
       SYS_CONTEXT('USERENV','CON_NAME') AS pdb,
       SYS_CONTEXT('USERENV','SERVICE_NAME') AS servicio FROM dual;

PROMPT === cat01 - Tablas del propietario ===
SELECT table_name FROM user_tables ORDER BY table_name;

PROMPT === cat02 - Vistas y modo de lectura ===
SELECT view_name, read_only FROM user_views ORDER BY view_name;

PROMPT === cat03 - Paquetes y cuerpos ===
SELECT object_type, object_name, status FROM user_objects
WHERE object_type IN ('PACKAGE','PACKAGE BODY') ORDER BY object_name, object_type;

PROMPT === cat04 - Rutinas independientes ===
SELECT object_name, object_type, status FROM user_objects
WHERE object_type IN ('PROCEDURE','FUNCTION') ORDER BY object_type, object_name;

PROMPT === cat05 - Operaciones publicas ===
SELECT object_name AS paquete, procedure_name AS miembro
FROM user_procedures
WHERE object_type = 'PACKAGE' AND procedure_name IS NOT NULL
ORDER BY 1, 2;

PROMPT === cat06 - Disparadores y eventos ===
SELECT trigger_name, trigger_type, triggering_event, table_name, status
FROM user_triggers ORDER BY table_name, trigger_name;

PROMPT === cat07 - Indices por tabla ===
SELECT index_name, table_name, uniqueness, index_type
FROM user_indexes ORDER BY table_name, index_name;

PROMPT === cat08 - Secuencias y cache ===
SELECT sequence_name, increment_by, cache_size, last_number
FROM user_sequences ORDER BY sequence_name;

PROMPT === cat09 - Compilacion de objetos ===
SELECT object_name, object_type, status FROM user_objects
WHERE status = 'INVALID' ORDER BY object_type, object_name;

PROMPT === cat10 - Codigo de procedimiento ===
SELECT line, text FROM user_source
WHERE name = 'PR_CAMBIAR_ESTADO_CONTENEDOR' AND type = 'PROCEDURE' ORDER BY line;

PROMPT === cat11 - Codigo de funcion ===
SELECT line, text FROM user_source
WHERE name = 'FN_PESO_ESTIMADO_KG' AND type = 'FUNCTION' ORDER BY line;

PROMPT === cat12 - Especificacion y cuerpo ===
SELECT type, line, text FROM user_source
WHERE name = 'PKG_DEPOSITO' AND type IN ('PACKAGE','PACKAGE BODY') ORDER BY type, line;

PROMPT === cat13 - Codigo de un trigger ===
SELECT line, text FROM user_source
WHERE name = 'TRG_DEPOSITO_COHERENCIA' AND type = 'TRIGGER' ORDER BY line;

PROMPT === cat14 - Parametros del paquete ===
SELECT object_name, overload, position, argument_name, in_out, data_type, defaulted
FROM user_arguments WHERE package_name = 'PKG_ECOPUNTOS'
ORDER BY object_name, overload, sequence;

PROMPT === cat15 - Errores de compilacion ===
SELECT name, type, line, position, text FROM user_errors ORDER BY name, type, sequence;

PROMPT === cat16 - Dependencias directas ===
SELECT name, type, referenced_owner, referenced_name, referenced_type
FROM user_dependencies WHERE name = 'PKG_DEPOSITO' ORDER BY type, referenced_name;

PROMPT === cat17 - Columnas de vistas ===
SELECT c.table_name AS vista, c.column_id, c.column_name, c.data_type
FROM user_tab_columns c JOIN user_views v ON v.view_name=c.table_name
ORDER BY c.table_name,c.column_id;

PROMPT === cat18 - Columnas de indices ===
SELECT index_name, column_position, column_name, descend FROM user_ind_columns
WHERE table_name = 'INTERACCION' ORDER BY index_name,column_position;

PROMPT === cat19 - Expresiones de indices ===
SELECT index_name, column_position, column_expression FROM user_ind_expressions
ORDER BY index_name, column_position;

PROMPT === cat20 - Restricciones y columnas ===
SELECT k.constraint_name,k.constraint_type,c.position,c.column_name
FROM user_constraints k JOIN user_cons_columns c ON c.constraint_name=k.constraint_name
WHERE k.table_name='DEPOSITO' ORDER BY k.constraint_type,k.constraint_name,c.position;

PROMPT === cat21 - PDB y servicio ===
SELECT SYS_CONTEXT('USERENV','SESSION_USER') AS usuario,
       SYS_CONTEXT('USERENV','CON_NAME') AS pdb,
       SYS_CONTEXT('USERENV','SERVICE_NAME') AS servicio FROM dual;

PROMPT === count_vw_bi_auditoria - count vw bi auditoria ===
SELECT COUNT(*) AS filas FROM VW_BI_AUDITORIA;

PROMPT === view_vw_bi_auditoria - view vw bi auditoria ===
SELECT id_auditoria, operacion, estado_anterior, estado_nuevo
FROM VW_BI_AUDITORIA
ORDER BY id_auditoria
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_bi_ciudadanos - count vw bi ciudadanos ===
SELECT COUNT(*) AS filas FROM VW_BI_CIUDADANOS;

PROMPT === view_vw_bi_ciudadanos - view vw bi ciudadanos ===
SELECT ciudadano, activo, origen_dato
FROM VW_BI_CIUDADANOS
ORDER BY ciudadano
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_bi_consultas_vision - count vw bi consultas vision ===
SELECT COUNT(*) AS filas FROM VW_BI_CONSULTAS_VISION;

PROMPT === view_vw_bi_consultas_vision - view vw bi consultas vision ===
SELECT id_consulta, resultado, contexto_qr, material
FROM VW_BI_CONSULTAS_VISION
ORDER BY id_consulta
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_bi_depositos - count vw bi depositos ===
SELECT COUNT(*) AS filas FROM VW_BI_DEPOSITOS;

PROMPT === view_vw_bi_depositos - view vw bi depositos ===
SELECT id_deposito, estado, material, unidades, evidencia
FROM VW_BI_DEPOSITOS
ORDER BY id_deposito
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_bi_ecopuntos - count vw bi ecopuntos ===
SELECT COUNT(*) AS filas FROM VW_BI_ECOPUNTOS;

PROMPT === view_vw_bi_ecopuntos - view vw bi ecopuntos ===
SELECT id_movimiento, tipo, puntos, material, tarifa
FROM VW_BI_ECOPUNTOS
ORDER BY id_movimiento
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_caneca_estacion - count vw caneca estacion ===
SELECT COUNT(*) AS filas FROM VW_CANECA_ESTACION;

PROMPT === view_vw_caneca_estacion - view vw caneca estacion ===
SELECT id_contenedor, categoria, color, estado_texto
FROM VW_CANECA_ESTACION
ORDER BY id_contenedor
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_deposito_detalle - count vw deposito detalle ===
SELECT COUNT(*) AS filas FROM VW_DEPOSITO_DETALLE;

PROMPT === view_vw_deposito_detalle - view vw deposito detalle ===
SELECT id_deposito, estado_texto, evidencia_fisica, peso_estimado_g, peso_medido_g
FROM VW_DEPOSITO_DETALLE
ORDER BY id_deposito
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_recorrido - count vw recorrido ===
SELECT COUNT(*) AS filas FROM VW_RECORRIDO;

PROMPT === view_vw_recorrido - view vw recorrido ===
SELECT id_interaccion, id_orden, id_deposito, fotos_consultadas, aperturas_confirmadas
FROM VW_RECORRIDO
ORDER BY id_interaccion
FETCH FIRST 1 ROW ONLY;

PROMPT === count_vw_saldo_ecopuntos - count vw saldo ecopuntos ===
SELECT COUNT(*) AS filas FROM VW_SALDO_ECOPUNTOS;

PROMPT === view_vw_saldo_ecopuntos - view vw saldo ecopuntos ===
SELECT id_usuario, saldo, acreditados, revertidos, movimientos
FROM VW_SALDO_ECOPUNTOS
ORDER BY id_usuario
FETCH FIRST 1 ROW ONLY;

PROMPT === ej101 - Leer el catalogo de materiales ===
SELECT id_material, codigo, nombre FROM material
ORDER BY id_material FETCH FIRST 8 ROWS ONLY;

PROMPT === ej102 - Distinguir categoria y disponibilidad ===
SELECT codigo, categoria FROM material WHERE habilitado=1 ORDER BY categoria,codigo;

PROMPT === ej103 - Contar estaciones ===
SELECT COUNT(*) AS estaciones FROM punto_instalacion;

PROMPT === ej104 - Contar canecas disponibles ===
SELECT COUNT(*) AS en_servicio FROM contenedor WHERE estado=1;

PROMPT === ej105 - Leer el estado en lenguaje humano ===
SELECT id_contenedor, categoria, color, estado_texto
FROM VW_CANECA_ESTACION
ORDER BY id_contenedor
FETCH FIRST 1 ROW ONLY;

PROMPT === ej106 - Identificar depositos en espera ===
SELECT id_deposito, estado_texto FROM vw_deposito_detalle
WHERE estado_verificacion=0 ORDER BY id_deposito FETCH FIRST 6 ROWS ONLY;

PROMPT === ej107 - Detectar depositos sin medicion ===
SELECT COUNT(*) AS sin_peso_medido FROM deposito WHERE peso_medido_kg IS NULL;

PROMPT === ej108 - Identificar el origen de un registro ciudadano ===
SELECT ciudadano, activo, origen_dato
FROM VW_BI_CIUDADANOS
ORDER BY ciudadano
FETCH FIRST 1 ROW ONLY;

PROMPT === ej109 - Leer un movimiento contable ===
SELECT id_movimiento, tipo, puntos, material, tarifa
FROM VW_BI_ECOPUNTOS
ORDER BY id_movimiento
FETCH FIRST 1 ROW ONLY;

PROMPT === ej110 - Reconocer las tablas del esquema ===
SELECT table_name FROM user_tables ORDER BY table_name;

PROMPT === ej201 - Unir una caneca con su estacion ===
SELECT c.id_contenedor, p.nombre AS estacion
FROM contenedor c JOIN punto_instalacion p
  ON p.id_punto_instalacion=c.id_punto_instalacion
ORDER BY c.id_contenedor FETCH FIRST 8 ROWS ONLY;

PROMPT === ej202 - Contar depositos por estado ===
SELECT estado_verificacion, COUNT(*) AS total
FROM deposito GROUP BY estado_verificacion ORDER BY estado_verificacion;

PROMPT === ej203 - Buscar categorias con mas de un material ===
SELECT categoria, COUNT(*) AS total FROM material
GROUP BY categoria HAVING COUNT(*)>1 ORDER BY categoria;

PROMPT === ej204 - Seguir una sesion sin perder ordenes ausentes ===
SELECT i.id_interaccion,o.id_orden FROM interaccion i
LEFT JOIN orden_apertura o ON o.id_interaccion=i.id_interaccion
WHERE i.es_sintetico=1 ORDER BY i.id_interaccion,o.id_orden FETCH FIRST 10 ROWS ONLY;

PROMPT === ej205 - Medir dos tipos de cobertura del peso ===
SELECT COUNT(*) AS filas, COUNT(peso_estimado_kg) AS estimadas,
       COUNT(peso_medido_kg) AS medidas FROM deposito;

PROMPT === ej206 - Buscar fotografias por resultado ===
SELECT resultado,COUNT(*) AS fotos FROM vw_bi_consultas_vision
GROUP BY resultado ORDER BY fotos DESC,resultado;

PROMPT === ej207 - Rastrear el deposito por claves ===
SELECT d.id_deposito,o.id_orden,i.id_interaccion
FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
ORDER BY d.id_deposito FETCH FIRST 10 ROWS ONLY;

PROMPT === ej208 - Inspeccionar los prefijos de Interaccion ===
SELECT index_name, column_position, column_name, descend FROM user_ind_columns
WHERE table_name = 'INTERACCION' ORDER BY index_name,column_position;

PROMPT === ej209 - Comparar consulta por usuario con orden por fecha ===
SELECT id_interaccion,fecha_inicio_utc FROM interaccion
WHERE id_usuario=(SELECT MIN(id_usuario) FROM interaccion WHERE es_sintetico=1)
ORDER BY fecha_inicio_utc FETCH FIRST 5 ROWS ONLY;

PROMPT === ej210 - Estudiar la FK compuesta de material por caneca ===
SELECT constraint_name,position,column_name FROM user_cons_columns
WHERE table_name='CONTENEDOR_MATERIAL' ORDER BY constraint_name,position;

PROMPT === ej301 - Imprimir un recuento desde PL SQL ===
DECLARE
    v_total NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_total FROM material;
    DBMS_OUTPUT.PUT_LINE('Materiales = ' || v_total);
END;
/

PROMPT === ej302 - Presentar estados mediante un cursor FOR ===
BEGIN
    FOR r IN (SELECT estado_verificacion,COUNT(*) total FROM deposito
              GROUP BY estado_verificacion ORDER BY estado_verificacion) LOOP
        DBMS_OUTPUT.PUT_LINE(r.estado_verificacion || ' = ' || r.total);
    END LOOP;
END;
/

PROMPT === ej303 - Estimar dos objetos por catalogo ===
SELECT gramos_tipico,
       fn_peso_estimado_kg(id_peso_referencial, 2) AS kg_estimados
FROM peso_referencial
ORDER BY id_peso_referencial FETCH FIRST 1 ROW ONLY;

PROMPT === ej304 - Entender un retorno nulo ===
SELECT fn_peso_estimado_kg(NULL, 2) AS estimado FROM dual;

PROMPT === ej305 - Resolver una caneca disponible ===
SELECT fn_resolver_caneca_disponible(e.id_punto, e.id_material) AS caneca
FROM (
  SELECT p.id_punto_instalacion AS id_punto, m.id_material
  FROM punto_instalacion p
  JOIN contenedor c ON c.id_punto_instalacion = p.id_punto_instalacion
  JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor
  JOIN material m ON m.id_material = cm.id_material
  WHERE p.activo = 1 AND c.estado = 1 AND m.habilitado = 1
  ORDER BY p.id_punto_instalacion, m.id_material
  FETCH FIRST 1 ROW ONLY
) e;

PROMPT === ej306 - Obtener depositos mediante un cursor de enlace ===
VARIABLE depositos_cur REFCURSOR
DECLARE
    v_usuario usuario.id_usuario%TYPE;
BEGIN
    SELECT MIN(id_titular) INTO v_usuario FROM vw_deposito_detalle
    WHERE es_sintetico = 1 AND id_titular IS NOT NULL;
    :depositos_cur := fn_depositos_ciudadano(v_usuario);
END;
/
PRINT depositos_cur

PROMPT === ej307 - Obtener un parametro OUT de estadisticas ===
VARIABLE resumen REFCURSOR
BEGIN
    pr_consultar_estadisticas(
        p_desde_col => DATE '2026-01-01',
        p_hasta_col => DATE '2027-01-01',
        p_id_punto  => NULL,
        p_categoria => NULL,
        p_resultado => :resumen
    );
END;
/
PRINT resumen

PROMPT === ej308 - Consultar la firma de un procedimiento ===
SELECT position,argument_name,in_out,data_type FROM user_arguments
WHERE object_name='PR_CONSULTAR_ESTADISTICAS' AND package_name IS NULL
ORDER BY sequence;

PROMPT === ej309 - Comprobar un rol con retorno numerico ===
SELECT pkg_contexto_app.tiene_rol(MIN(id_usuario), 1) AS ciudadano
FROM usuario WHERE es_sintetico = 1;

PROMPT === ej310 - Buscar donde se maneja falta de datos ===
SELECT line,text FROM user_source WHERE name='FN_PESO_ESTIMADO_KG'
AND type='FUNCTION' AND UPPER(text) LIKE '%NO_DATA_FOUND%';

PROMPT === ej401 - Comparar saldo vista y funcion ===
SELECT id_usuario, saldo AS saldo_vista,
       pkg_ecopuntos.saldo(id_usuario) AS saldo_funcion
FROM vw_saldo_ecopuntos
WHERE es_sintetico = 1
ORDER BY id_usuario FETCH FIRST 5 ROWS ONLY;

PROMPT === ej402 - Ubicar la tarifa justo al inicio de su vigencia ===
SELECT id_regla,
       pkg_ecopuntos.regla_vigente(id_material, desde_utc) AS regla_al_inicio
FROM regla_ecopuntos
ORDER BY id_regla FETCH FIRST 5 ROWS ONLY;

PROMPT === ej403 - Validar un ciudadano activo sin modificar registros ===
DECLARE
    v_usuario usuario.id_usuario%TYPE;
BEGIN
    SELECT MIN(ur.id_usuario) INTO v_usuario
    FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol
    JOIN usuario u ON u.id_usuario = ur.id_usuario
    WHERE r.codigo = 1 AND u.activo = 1 AND u.es_sintetico = 1;
    IF v_usuario IS NOT NULL THEN
        pkg_contexto_app.exigir_rol(v_usuario, '1');
        DBMS_OUTPUT.PUT_LINE('Rol permitido');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Sin ciudadano sintetico activo');
    END IF;
END;
/

PROMPT === ej404 - Comprobar el actor en el estado de sesion ===
DECLARE
    v_usuario usuario.id_usuario%TYPE;
BEGIN
    SELECT MIN(id_usuario) INTO v_usuario FROM usuario WHERE activo = 1 AND es_sintetico = 1;
    IF v_usuario IS NOT NULL THEN
        pkg_contexto_app.fijar_actor(v_usuario, 'Practica de sesion');
        DBMS_OUTPUT.PUT_LINE('Actor=' || pkg_contexto_app.actor);
        DBMS_OUTPUT.PUT_LINE(pkg_contexto_app.motivo);
        pkg_contexto_app.limpiar;
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        pkg_contexto_app.limpiar;
        RAISE;
END;
/

PROMPT === ej405 - Leer una funcion booleana mediante IF ===
BEGIN
    IF pkg_deposito.correccion_en_curso THEN
        DBMS_OUTPUT.PUT_LINE('Correccion en curso');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Sin correccion en esta sesion');
    END IF;
END;
/

PROMPT === ej406 - Encontrar el registro de auditoria por evento ===
SELECT operacion, COUNT(*) AS total FROM auditoria_deposito
GROUP BY operacion ORDER BY operacion;

PROMPT === ej407 - Emparejar cada reverso con su credito ===
SELECT r.id_movimiento AS reverso,c.id_movimiento AS credito,r.puntos+c.puntos AS neto
FROM movimiento_ecopuntos r JOIN movimiento_ecopuntos c
  ON c.id_movimiento=r.id_movimiento_revertido
WHERE r.tipo=2 ORDER BY r.id_movimiento;

PROMPT === ej408 - Separar habilitacion y validez de los triggers ===
SELECT t.trigger_name,t.status AS habilitacion,o.status AS validez
FROM user_triggers t JOIN user_objects o
ON o.object_name=t.trigger_name AND o.object_type='TRIGGER' ORDER BY 1;

PROMPT === ej409 - Relacionar cada secuencia automatica con su ID ===
SELECT table_name,column_name,generation_type,sequence_name
FROM user_tab_identity_cols ORDER BY table_name;

PROMPT === ej410 - Obtener el ranking con cursor ===
VARIABLE ranking REFCURSOR
BEGIN
    :ranking := pkg_estadisticas.ranking_verificados(
        DATE '2026-01-01', DATE '2027-01-01', 3);
END;
/
PRINT ranking

PROMPT === ej501 - Defender por que un resultado de IA no equivale a reciclaje ===
SELECT (SELECT COUNT(*) FROM consulta_vision) AS consultas,
       (SELECT COUNT(*) FROM deposito WHERE estado_verificacion=1) AS verificados FROM dual;

PROMPT === ej502 - Justificar que el saldo es derivado ===
SELECT id_usuario, saldo AS saldo_vista,
       pkg_ecopuntos.saldo(id_usuario) AS saldo_funcion
FROM vw_saldo_ecopuntos
WHERE es_sintetico = 1
ORDER BY id_usuario FETCH FIRST 5 ROWS ONLY;

PROMPT === ej503 - Demostrar que la verificacion precede a los puntos ===
SELECT COUNT(*) AS creditos_sin_dato FROM movimiento_ecopuntos m
WHERE m.tipo=1 AND NOT EXISTS (SELECT 1 FROM deposito d WHERE d.id_deposito=m.id_deposito);

PROMPT === ej504 - Explicar por que la evidencia fisica tiene dos rutas ===
SELECT d.sin_sensor,e.tipo,COUNT(*) AS total FROM deposito d
JOIN evento_dispositivo e ON e.id_evento=d.id_evento
GROUP BY d.sin_sensor,e.tipo ORDER BY 1,2;

PROMPT === ej505 - Defender el proposito de las vistas de BI ===
SELECT view_name,read_only FROM user_views WHERE view_name LIKE 'VW\_BI\_%' ESCAPE '\'
ORDER BY view_name;

PROMPT === ej506 - Explicar identidad compuesta frente a IDENTITY ===
SELECT column_name,position FROM user_cons_columns
WHERE constraint_name='PK_USUARIO_ROL' ORDER BY position;

PROMPT === ej507 - Justificar la unicidad condicional de una caneca ===
SELECT column_position,column_expression FROM user_ind_expressions
WHERE index_name='UQ_CONTENEDOR_COLOR_ACTIVO' ORDER BY column_position;

PROMPT === ej508 - Distinguir permiso Oracle y rol de negocio ===
SELECT codigo,descripcion FROM rol ORDER BY codigo;

PROMPT === ej509 - Explicar por que se conserva el antes y despues ===
SELECT operacion,estado_anterior,estado_nuevo,COUNT(*) AS total
FROM auditoria_deposito GROUP BY operacion,estado_anterior,estado_nuevo ORDER BY 1,2,3;

PROMPT === ej510 - Localizar la puerta de correccion y sus dependencias ===
SELECT name, type, referenced_owner, referenced_name, referenced_type
FROM user_dependencies WHERE name = 'PKG_DEPOSITO' ORDER BY type, referenced_name;

ROLLBACK;
PROMPT Fin de la practica de lectura.
