-- EcoAccess Challenge: PKG_ECO_JUEGO con bancos de preguntas por competencia (7 o 10). Requiere V031, V032 y V034.
-- Solo reemplaza el paquete (CREATE OR REPLACE). El módulo REST eco.juego.v1 de V032 NO cambia: mismas rutas, mismos
-- parámetros y las mismas firmas de los procedimientos que llama, así que no hace falta redefinirlo.
-- Comportamiento de una competencia con BANCO_ID nulo (todas las anteriores, incluida la oficial abierta): idéntico a V032.
--   - Las preguntas salen del banco 1 (ids 1..7 en orden 1..7), el mismo orden y contenido que V032 leía por ORDEN.
--   - Mismo JSON (mismas claves y valores), 100 puntos por acierto, desempate aciertos↓ tiempo↑ fin↑ id↑ (vista de V031),
--     cronómetro del servidor, idempotencia de registro/inicio/respuesta, sesión guardada como SHA-256, respuestas
--     inmutables, límites por origen y dispositivo.
-- Lo único nuevo:
--   - El total de preguntas (y el puntaje máximo) se lee del banco de la competencia: 7 o 10.
--   - «pregunta» en /intento/respuesta y «orden» en las preguntas y en la revisión son el orden dentro del banco (1..N).
--   - Administración: «crear» acepta {"banco":"EXPO2026-10"} (sin banco = 7 preguntas, como antes) y «competencias»
--     informa «banco» y «preguntas» de cada competencia.
-- Reversión: database/06_migraciones/reversion/R035__juego_paquete_bancos.sql (restaura el paquete exacto de V032).
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON

CREATE OR REPLACE PACKAGE pkg_eco_juego AUTHID DEFINER AS
    -- Total del banco clásico (banco 1). Se conserva por compatibilidad; el total real sale del banco de cada competencia.
    c_total_preguntas CONSTANT PLS_INTEGER := 7;

    -- Utilidades puras (públicas para poder usarse dentro de SQL).
    FUNCTION sha256(p_texto VARCHAR2) RETURN VARCHAR2;
    FUNCTION normalizar(p_texto VARCHAR2) RETURN VARCHAR2;
    FUNCTION ms(p_intervalo INTERVAL DAY TO SECOND) RETURN NUMBER;

    PROCEDURE estado_publico(p_ronda IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE registrar(p_cuerpo IN CLOB, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE iniciar(p_sesion IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE consultar(p_sesion IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE responder(p_sesion IN VARCHAR2, p_cuerpo IN CLOB, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE clasificacion(p_ronda IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE revision(p_sesion IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE administrar(p_clave IN VARCHAR2, p_accion IN VARCHAR2, p_cuerpo IN CLOB, p_json OUT CLOB, p_http OUT NUMBER);
    PROCEDURE exportar(p_clave IN VARCHAR2, p_competencia IN NUMBER, p_csv OUT CLOB, p_http OUT NUMBER);
    -- Escribe la respuesta HTTP (ORDS / OWA).
    PROCEDURE emitir(p_contenido IN CLOB, p_tipo IN VARCHAR2 DEFAULT 'application/json', p_archivo IN VARCHAR2 DEFAULT NULL);
END pkg_eco_juego;
/

CREATE OR REPLACE PACKAGE BODY pkg_eco_juego AS

    e_negocio EXCEPTION;
    g_http    NUMBER;
    g_codigo  VARCHAR2(60);
    g_mensaje VARCHAR2(400);

    PROCEDURE fallar(p_http NUMBER, p_codigo VARCHAR2, p_mensaje VARCHAR2) IS
    BEGIN
        g_http := p_http; g_codigo := p_codigo; g_mensaje := p_mensaje;
        RAISE e_negocio;
    END;

    FUNCTION error_json RETURN CLOB IS
        o JSON_OBJECT_T := JSON_OBJECT_T();
    BEGIN
        o.put('ok', FALSE); o.put('codigo', g_codigo); o.put('error', g_mensaje);
        RETURN o.to_clob;
    END;

    FUNCTION sha256(p_texto VARCHAR2) RETURN VARCHAR2 IS
        r VARCHAR2(64);
    BEGIN
        SELECT LOWER(RAWTOHEX(STANDARD_HASH(p_texto, 'SHA256'))) INTO r FROM dual;
        RETURN r;
    END;

    FUNCTION ms(p_intervalo INTERVAL DAY TO SECOND) RETURN NUMBER IS
    BEGIN
        RETURN EXTRACT(DAY FROM p_intervalo) * 86400000 + EXTRACT(HOUR FROM p_intervalo) * 3600000
             + EXTRACT(MINUTE FROM p_intervalo) * 60000 + ROUND(EXTRACT(SECOND FROM p_intervalo) * 1000);
    END;

    FUNCTION normalizar(p_texto VARCHAR2) RETURN VARCHAR2 IS
    BEGIN
        RETURN UPPER(TRIM(REGEXP_REPLACE(TRANSLATE(p_texto, 'áéíóúüñÁÉÍÓÚÜÑàèìòùÀÈÌÒÙ', 'aeiouunAEIOUUNaeiouAEIOU'), '\s+', ' ')));
    END;

    -- Origen de la solicitud (IP del cliente detrás del balanceador), solo como huella para limitar abusos.
    FUNCTION origen RETURN VARCHAR2 IS
        v VARCHAR2(400);
    BEGIN
        BEGIN v := OWA_UTIL.GET_CGI_ENV('X-FORWARDED-FOR'); EXCEPTION WHEN OTHERS THEN v := NULL; END;
        IF v IS NULL THEN BEGIN v := OWA_UTIL.GET_CGI_ENV('REMOTE_ADDR'); EXCEPTION WHEN OTHERS THEN v := NULL; END; END IF;
        RETURN sha256('eco-juego|' || NVL(REGEXP_SUBSTR(v, '[^,]+'), 'desconocido'));
    END;

    -- Límite de solicitudes por clave y ventana fija. Transacción autónoma: el conteo queda aunque la operación falle.
    FUNCTION permitir(p_clave VARCHAR2, p_maximo NUMBER, p_segundos NUMBER) RETURN BOOLEAN IS
        PRAGMA AUTONOMOUS_TRANSACTION;
        v_inicio TIMESTAMP WITH TIME ZONE; v_cuenta NUMBER;
    BEGIN
        BEGIN
            SELECT ventana_inicio, contador INTO v_inicio, v_cuenta FROM eco_juego_limites WHERE clave = p_clave FOR UPDATE;
            IF SYSTIMESTAMP - v_inicio > NUMTODSINTERVAL(p_segundos, 'SECOND') THEN
                UPDATE eco_juego_limites SET ventana_inicio = SYSTIMESTAMP, contador = 1 WHERE clave = p_clave;
                v_cuenta := 1;
            ELSE
                UPDATE eco_juego_limites SET contador = contador + 1 WHERE clave = p_clave;
                v_cuenta := v_cuenta + 1;
            END IF;
        EXCEPTION WHEN NO_DATA_FOUND THEN
            BEGIN
                INSERT INTO eco_juego_limites (clave, ventana_inicio, contador) VALUES (p_clave, SYSTIMESTAMP, 1);
                v_cuenta := 1;
            EXCEPTION WHEN DUP_VAL_ON_INDEX THEN
                UPDATE eco_juego_limites SET contador = contador + 1 WHERE clave = p_clave RETURNING contador INTO v_cuenta;
            END;
        END;
        COMMIT;
        RETURN v_cuenta <= p_maximo;
    END;

    -- Competencia pública: la ronda oficial más reciente que ya se abrió. Las rondas de prueba solo se usan por código.
    FUNCTION competencia(p_ronda VARCHAR2) RETURN eco_juego_competencias%ROWTYPE IS
        c eco_juego_competencias%ROWTYPE;
    BEGIN
        IF p_ronda IS NOT NULL THEN
            SELECT * INTO c FROM eco_juego_competencias WHERE codigo = SUBSTR(p_ronda, 1, 40);
        ELSE
            SELECT * INTO c FROM (
                SELECT * FROM eco_juego_competencias WHERE es_prueba = 'N'
                 ORDER BY CASE WHEN estado = 'PREPARADA' THEN 1 ELSE 0 END, NVL(abierta_en, creada_en) DESC, id DESC)
             WHERE ROWNUM = 1;
        END IF;
        RETURN c;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        fallar(404, 'sin_competencia', 'No hay una competencia disponible en este momento.');
    END;

    -- Banco de preguntas de una competencia: nulo = banco 1 (las 7 preguntas de V031, en su orden original).
    FUNCTION banco_de(p_competencia NUMBER) RETURN NUMBER IS
        v NUMBER;
    BEGIN
        SELECT NVL(banco_id, 1) INTO v FROM eco_juego_competencias WHERE id = p_competencia;
        RETURN v;
    EXCEPTION WHEN NO_DATA_FOUND THEN RETURN 1;
    END;

    -- Número de preguntas del banco (7 o 10): define el final del intento y el puntaje máximo.
    FUNCTION total(p_banco NUMBER) RETURN PLS_INTEGER IS
        n PLS_INTEGER;
    BEGIN
        SELECT COUNT(*) INTO n FROM eco_juego_banco_preguntas WHERE banco_id = p_banco;
        RETURN n;
    END;

    FUNCTION json_competencia(c eco_juego_competencias%ROWTYPE) RETURN JSON_OBJECT_T IS
        o JSON_OBJECT_T := JSON_OBJECT_T();
    BEGIN
        o.put('nombre', c.nombre);
        o.put('estado', c.estado);
        o.put('registroAbierto', c.estado = 'ABIERTA');
        o.put('definitiva', c.estado = 'FINALIZADA');
        o.put('prueba', c.es_prueba = 'S');
        o.put('avisoVersion', c.aviso_version);
        o.put('limiteMs', c.duracion_max_seg * 1000);
        RETURN o;
    END;

    FUNCTION intento_por_sesion(p_sesion VARCHAR2, p_bloquear BOOLEAN) RETURN eco_juego_intentos%ROWTYPE IS
        i eco_juego_intentos%ROWTYPE;
        h VARCHAR2(64);
    BEGIN
        IF p_sesion IS NULL OR NOT REGEXP_LIKE(p_sesion, '^[A-Za-z0-9_-]{32,128}$') THEN
            fallar(401, 'sesion', 'La sesión de juego no es válida. Vuelve a la pantalla de inicio.');
        END IF;
        h := sha256(p_sesion);
        IF p_bloquear THEN
            SELECT * INTO i FROM eco_juego_intentos WHERE sesion_sha256 = h FOR UPDATE;
        ELSE
            SELECT * INTO i FROM eco_juego_intentos WHERE sesion_sha256 = h;
        END IF;
        RETURN i;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        fallar(401, 'sesion', 'No encontramos tu participación. Vuelve a la pantalla de inicio.');
    END;

    -- Cierra un intento en curso con lo respondido: lo no respondido cuenta como error.
    PROCEDURE cerrar_intento(p_id NUMBER, p_maximo_ms NUMBER DEFAULT NULL) IS
        v_inicio TIMESTAMP WITH TIME ZONE; v_aciertos NUMBER; v_ms NUMBER; v_comp NUMBER; v_total PLS_INTEGER;
    BEGIN
        SELECT inicio, competencia_id INTO v_inicio, v_comp FROM eco_juego_intentos WHERE id = p_id;
        v_total := total(banco_de(v_comp));
        SELECT COUNT(CASE WHEN es_correcta = 'S' THEN 1 END) INTO v_aciertos FROM eco_juego_respuestas WHERE intento_id = p_id;
        v_ms := ms(SYSTIMESTAMP - v_inicio);
        IF p_maximo_ms IS NOT NULL AND v_ms > p_maximo_ms THEN v_ms := p_maximo_ms; END IF;
        UPDATE eco_juego_intentos
           SET estado = 'FINALIZADO', fin = SYSTIMESTAMP, aciertos = v_aciertos, errores = v_total - v_aciertos,
               puntaje = v_aciertos * 100, tiempo_ms = v_ms, pregunta_actual = v_total + 1
         WHERE id = p_id AND estado = 'EN_CURSO';
    END;

    -- Si el tiempo máximo se agotó, cierra el intento. Devuelve el intento actualizado.
    FUNCTION vigente(i eco_juego_intentos%ROWTYPE) RETURN eco_juego_intentos%ROWTYPE IS
        c eco_juego_competencias%ROWTYPE; r eco_juego_intentos%ROWTYPE;
    BEGIN
        IF i.estado <> 'EN_CURSO' THEN RETURN i; END IF;
        SELECT * INTO c FROM eco_juego_competencias WHERE id = i.competencia_id;
        IF ms(SYSTIMESTAMP - i.inicio) > c.duracion_max_seg * 1000 OR c.estado = 'FINALIZADA' THEN
            cerrar_intento(i.id, c.duracion_max_seg * 1000);
            COMMIT;
        END IF;
        SELECT * INTO r FROM eco_juego_intentos WHERE id = i.id;
        RETURN r;
    END;

    FUNCTION json_pregunta(p_intento NUMBER, p_banco NUMBER, p_orden NUMBER, p_total PLS_INTEGER) RETURN JSON_OBJECT_T IS
        o JSON_OBJECT_T := JSON_OBJECT_T(); a JSON_ARRAY_T := JSON_ARRAY_T(); x JSON_OBJECT_T;
    BEGIN
        FOR q IN (SELECT p.id, bp.orden, p.nivel, p.tema, p.enunciado, p.imagen, p.opciones_con_imagen
                    FROM eco_juego_banco_preguntas bp JOIN eco_juego_preguntas p ON p.id = bp.pregunta_id
                   WHERE bp.banco_id = p_banco AND bp.orden = p_orden) LOOP
            o.put('orden', q.orden); o.put('total', p_total); o.put('nivel', q.nivel); o.put('tema', q.tema);
            o.put('enunciado', q.enunciado); o.put('imagen', q.imagen); o.put('opcionesConImagen', q.opciones_con_imagen = 'S');
            -- Orden de opciones propio de cada intento (estable al recargar) y sin ninguna marca de corrección.
            FOR op IN (SELECT id, texto, imagen FROM eco_juego_opciones WHERE pregunta_id = q.id
                        ORDER BY ORA_HASH(p_intento * 10000 + id, 999999, MOD(p_intento, 4096)), id) LOOP
                x := JSON_OBJECT_T(); x.put('id', op.id); x.put('texto', op.texto); x.put('imagen', op.imagen);
                a.append(x);
            END LOOP;
            o.put('opciones', a);
        END LOOP;
        RETURN o;
    END;

    FUNCTION json_estado(i eco_juego_intentos%ROWTYPE) RETURN CLOB IS
        o JSON_OBJECT_T := JSON_OBJECT_T(); r JSON_OBJECT_T; c eco_juego_competencias%ROWTYPE;
        v_alias VARCHAR2(24); v_pos NUMBER; v_total NUMBER; v_banco NUMBER; v_preguntas PLS_INTEGER;
    BEGIN
        SELECT * INTO c FROM eco_juego_competencias WHERE id = i.competencia_id;
        v_banco := NVL(c.banco_id, 1); v_preguntas := total(v_banco);
        SELECT alias INTO v_alias FROM eco_juego_participantes WHERE id = i.participante_id;
        o.put('ok', TRUE); o.put('estado', i.estado); o.put('alias', v_alias);
        o.put('competencia', json_competencia(c));
        o.put('respondidas', LEAST(i.pregunta_actual - 1, v_preguntas));
        IF i.estado = 'EN_CURSO' THEN
            o.put('transcurridoMs', ms(SYSTIMESTAMP - i.inicio));
            o.put('pregunta', json_pregunta(i.id, v_banco, i.pregunta_actual, v_preguntas));
        ELSIF i.estado = 'FINALIZADO' THEN
            SELECT posicion INTO v_pos FROM vw_juego_clasificacion WHERE intento_id = i.id;
            SELECT COUNT(*) INTO v_total FROM eco_juego_intentos WHERE competencia_id = i.competencia_id AND estado = 'FINALIZADO';
            r := JSON_OBJECT_T();
            r.put('puntaje', i.puntaje); r.put('aciertos', i.aciertos); r.put('errores', i.errores);
            r.put('tiempoMs', i.tiempo_ms); r.put('posicion', NVL(i.posicion_final, v_pos));
            r.put('participantes', v_total); r.put('definitiva', c.estado = 'FINALIZADA');
            r.put('revisionDisponible', c.estado = 'FINALIZADA');
            o.put('resultado', r);
        END IF;
        RETURN o.to_clob;
    END;

    -- ─────────────────────────── Público ───────────────────────────
    PROCEDURE estado_publico(p_ronda IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER) IS
        o JSON_OBJECT_T := JSON_OBJECT_T(); c eco_juego_competencias%ROWTYPE; n NUMBER; v_preguntas PLS_INTEGER;
    BEGIN
        c := competencia(p_ronda);
        v_preguntas := total(NVL(c.banco_id, 1));
        SELECT COUNT(*) INTO n FROM eco_juego_participantes WHERE competencia_id = c.id;
        o.put('ok', TRUE); o.put('competencia', json_competencia(c)); o.put('participantes', n);
        o.put('preguntas', v_preguntas); o.put('puntajeMaximo', v_preguntas * 100);
        p_json := o.to_clob; p_http := 200;
    EXCEPTION WHEN e_negocio THEN p_json := error_json; p_http := g_http;
    END;

    PROCEDURE registrar(p_cuerpo IN CLOB, p_json OUT CLOB, p_http OUT NUMBER) IS
        b JSON_OBJECT_T; c eco_juego_competencias%ROWTYPE; i eco_juego_intentos%ROWTYPE;
        v_nombre VARCHAR2(400); v_alias VARCHAR2(200); v_sesion VARCHAR2(200); v_disp VARCHAR2(200);
        v_hash VARCHAR2(64); v_origen VARCHAR2(64); v_part NUMBER; n NUMBER;
    BEGIN
        BEGIN b := JSON_OBJECT_T.parse(p_cuerpo); EXCEPTION WHEN OTHERS THEN fallar(400, 'solicitud', 'Solicitud inválida.'); END;
        IF DBMS_LOB.GETLENGTH(p_cuerpo) > 4000 THEN fallar(413, 'solicitud', 'Solicitud demasiado grande.'); END IF;
        v_origen := origen;
        IF NOT permitir('reg|' || v_origen, 400, 600) THEN fallar(429, 'limite', 'Demasiados registros desde esta red. Espera unos minutos.'); END IF;
        v_nombre := TRIM(REGEXP_REPLACE(b.get_string('nombre'), '\s+', ' '));
        v_alias  := TRIM(REGEXP_REPLACE(b.get_string('alias'), '\s+', ' '));
        v_sesion := b.get_string('sesion');
        v_disp   := b.get_string('dispositivo');
        c := competencia(b.get_string('ronda'));

        IF v_sesion IS NULL OR NOT REGEXP_LIKE(v_sesion, '^[A-Za-z0-9_-]{32,128}$') THEN fallar(400, 'sesion', 'Sesión inválida. Recarga la página.'); END IF;
        v_hash := sha256(v_sesion);
        -- Reintento del mismo registro (p. ej., se perdió la respuesta por la red): devuelve la misma participación.
        BEGIN
            SELECT * INTO i FROM eco_juego_intentos WHERE sesion_sha256 = v_hash;
            p_json := json_estado(vigente(i)); p_http := 200; RETURN;
        EXCEPTION WHEN NO_DATA_FOUND THEN NULL;
        END;

        IF c.estado <> 'ABIERTA' THEN
            fallar(409, 'registro_cerrado', CASE c.estado WHEN 'PREPARADA' THEN 'El concurso aún no ha comenzado.'
                                                         WHEN 'FINALIZADA' THEN 'El concurso ya terminó. Consulta la clasificación.'
                                                         ELSE 'El registro está cerrado.' END);
        END IF;
        IF NOT b.has('acepta') OR NOT b.get_boolean('acepta') THEN fallar(400, 'aviso', 'Debes aceptar el aviso de datos y las reglas del concurso.'); END IF;
        IF v_nombre IS NULL OR LENGTH(v_nombre) NOT BETWEEN 5 AND 80 OR NOT REGEXP_LIKE(v_nombre, '^[[:alpha:] .''-]+$')
           OR NOT REGEXP_LIKE(v_nombre, '[[:alpha:]]{2,}[ ]+[[:alpha:]]{2,}') THEN
            fallar(400, 'nombre', 'Escribe tu nombre y apellido (solo letras).');
        END IF;
        IF v_alias IS NULL OR LENGTH(v_alias) NOT BETWEEN 3 AND 20 OR NOT REGEXP_LIKE(v_alias, '^[[:alnum:] _.-]+$') THEN
            fallar(400, 'alias', 'El alias debe tener entre 3 y 20 letras, números, espacios, puntos o guiones.');
        END IF;
        IF REGEXP_LIKE(normalizar(v_alias), '(PUT[AO]|MIERD|HIJUEP|MALPARID|GONORRE|CULO|PENDEJ|IDIOTA|ESTUPID|MARIC)') THEN
            fallar(400, 'alias', 'Elige otro alias para la clasificación pública.');
        END IF;
        IF v_disp IS NOT NULL AND REGEXP_LIKE(v_disp, '^[A-Za-z0-9_-]{16,128}$') THEN
            SELECT COUNT(*) INTO n FROM eco_juego_participantes WHERE competencia_id = c.id AND dispositivo_sha256 = sha256(v_disp);
            IF n >= 6 THEN fallar(429, 'dispositivo', 'Este dispositivo ya registró varios participantes. Pide ayuda en el stand.'); END IF;
        END IF;

        BEGIN
            INSERT INTO eco_juego_participantes (competencia_id, nombre_completo, nombre_normalizado, alias, alias_normalizado,
                                                 acepta_aviso, aviso_version, dispositivo_sha256, origen_sha256)
            VALUES (c.id, v_nombre, normalizar(v_nombre), v_alias, normalizar(v_alias), 'S', c.aviso_version,
                    CASE WHEN v_disp IS NOT NULL THEN sha256(v_disp) END, v_origen)
            RETURNING id INTO v_part;
        EXCEPTION WHEN DUP_VAL_ON_INDEX THEN
            IF INSTR(SQLERRM, 'UQ_ECO_JUEGO_PART_ALIAS') > 0 THEN fallar(409, 'alias_ocupado', 'Ese alias ya está en uso. Elige otro.'); END IF;
            fallar(409, 'ya_registrado', 'Ya existe un participante registrado con ese nombre en este concurso.');
        END;
        INSERT INTO eco_juego_intentos (participante_id, competencia_id, sesion_sha256) VALUES (v_part, c.id, v_hash)
        RETURNING id INTO i.id;
        COMMIT;
        SELECT * INTO i FROM eco_juego_intentos WHERE id = i.id;
        p_json := json_estado(i); p_http := 201;
    EXCEPTION WHEN e_negocio THEN ROLLBACK; p_json := error_json; p_http := g_http;
    END;

    PROCEDURE iniciar(p_sesion IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER) IS
        i eco_juego_intentos%ROWTYPE; c eco_juego_competencias%ROWTYPE;
    BEGIN
        i := intento_por_sesion(p_sesion, TRUE);
        IF i.estado = 'REGISTRADO' THEN
            SELECT * INTO c FROM eco_juego_competencias WHERE id = i.competencia_id;
            IF c.estado NOT IN ('ABIERTA', 'REGISTRO_CERRADO') THEN fallar(409, 'competencia_cerrada', 'El concurso ya terminó.'); END IF;
            -- El cronómetro oficial empieza aquí, con la hora del servidor, una sola vez.
            UPDATE eco_juego_intentos SET estado = 'EN_CURSO', inicio = SYSTIMESTAMP WHERE id = i.id AND estado = 'REGISTRADO';
            COMMIT;
            SELECT * INTO i FROM eco_juego_intentos WHERE id = i.id;
        ELSE
            ROLLBACK;
        END IF;
        p_json := json_estado(vigente(i)); p_http := 200;
    EXCEPTION WHEN e_negocio THEN ROLLBACK; p_json := error_json; p_http := g_http;
    END;

    PROCEDURE consultar(p_sesion IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER) IS
    BEGIN
        p_json := json_estado(vigente(intento_por_sesion(p_sesion, FALSE))); p_http := 200;
    EXCEPTION WHEN e_negocio THEN p_json := error_json; p_http := g_http;
    END;

    PROCEDURE responder(p_sesion IN VARCHAR2, p_cuerpo IN CLOB, p_json OUT CLOB, p_http OUT NUMBER) IS
        b JSON_OBJECT_T; i eco_juego_intentos%ROWTYPE; v_orden NUMBER; v_opcion NUMBER;
        v_pregunta NUMBER; v_correcta CHAR(1); v_previa NUMBER; v_fin TIMESTAMP WITH TIME ZONE; v_banco NUMBER;
    BEGIN
        BEGIN b := JSON_OBJECT_T.parse(p_cuerpo); EXCEPTION WHEN OTHERS THEN fallar(400, 'solicitud', 'Solicitud inválida.'); END;
        IF NOT permitir('resp|' || origen, 3000, 600) THEN fallar(429, 'limite', 'Demasiadas solicitudes. Espera un momento.'); END IF;
        v_orden := b.get_number('pregunta'); v_opcion := b.get_number('opcion');
        i := intento_por_sesion(p_sesion, TRUE);
        i := vigente(i);
        IF i.estado = 'REGISTRADO' THEN fallar(409, 'no_iniciado', 'Primero inicia el desafío.'); END IF;
        IF i.estado <> 'EN_CURSO' THEN ROLLBACK; p_json := json_estado(i); p_http := 200; RETURN; END IF;
        -- «pregunta» es el orden dentro del banco de la competencia (1..7 o 1..10).
        v_banco := banco_de(i.competencia_id);
        SELECT pregunta_id INTO v_pregunta FROM eco_juego_banco_preguntas WHERE banco_id = v_banco AND orden = v_orden;
        IF v_orden < i.pregunta_actual THEN
            -- Reintento de una respuesta ya confirmada: no cambia nada y devuelve el estado actual.
            SELECT COUNT(*) INTO v_previa FROM eco_juego_respuestas WHERE intento_id = i.id AND pregunta_id = v_pregunta;
            ROLLBACK; p_json := json_estado(i); p_http := 200; RETURN;
        ELSIF v_orden <> i.pregunta_actual THEN
            fallar(409, 'pregunta', 'Esa pregunta no está disponible ahora.');
        END IF;
        BEGIN
            SELECT es_correcta INTO v_correcta FROM eco_juego_opciones WHERE id = v_opcion AND pregunta_id = v_pregunta;
        EXCEPTION WHEN NO_DATA_FOUND THEN fallar(400, 'opcion', 'Elige una de las opciones de la pregunta.');
        END;
        INSERT INTO eco_juego_respuestas (intento_id, pregunta_id, opcion_id, es_correcta, ms_desde_inicio)
        VALUES (i.id, v_pregunta, v_opcion, v_correcta, ms(SYSTIMESTAMP - i.inicio));
        UPDATE eco_juego_intentos
           SET pregunta_actual = pregunta_actual + 1,
               aciertos = aciertos + CASE WHEN v_correcta = 'S' THEN 1 ELSE 0 END,
               errores  = errores  + CASE WHEN v_correcta = 'S' THEN 0 ELSE 1 END
         WHERE id = i.id;
        IF i.pregunta_actual = total(v_banco) THEN
            -- Última respuesta: fin y tiempo salen del mismo instante del servidor.
            v_fin := SYSTIMESTAMP;
            UPDATE eco_juego_intentos
               SET estado = 'FINALIZADO', fin = v_fin, puntaje = aciertos * 100, tiempo_ms = ms(v_fin - inicio)
             WHERE id = i.id;
        END IF;
        COMMIT;
        SELECT * INTO i FROM eco_juego_intentos WHERE id = i.id;
        p_json := json_estado(i); p_http := 200;
    EXCEPTION
        WHEN e_negocio THEN ROLLBACK; p_json := error_json; p_http := g_http;
        WHEN DUP_VAL_ON_INDEX THEN ROLLBACK; p_json := json_estado(intento_por_sesion(p_sesion, FALSE)); p_http := 200;
        WHEN NO_DATA_FOUND THEN ROLLBACK; g_codigo := 'pregunta'; g_mensaje := 'Pregunta inexistente.'; p_json := error_json; p_http := 400;
    END;

    PROCEDURE clasificacion(p_ronda IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER) IS
        o JSON_OBJECT_T := JSON_OBJECT_T(); a JSON_ARRAY_T := JSON_ARRAY_T(); x JSON_OBJECT_T;
        c eco_juego_competencias%ROWTYPE; v_part NUMBER; v_fin NUMBER; v_curso NUMBER;
    BEGIN
        c := competencia(p_ronda);
        FOR r IN (SELECT * FROM vw_juego_clasificacion WHERE competencia_id = c.id AND posicion <= 5 ORDER BY posicion) LOOP
            x := JSON_OBJECT_T();
            x.put('posicion', NVL(r.posicion_final, r.posicion)); x.put('alias', r.alias); x.put('puntaje', r.puntaje);
            x.put('aciertos', r.aciertos); x.put('tiempoMs', r.tiempo_ms);
            a.append(x);
        END LOOP;
        SELECT COUNT(*) INTO v_part FROM eco_juego_participantes WHERE competencia_id = c.id;
        SELECT COUNT(CASE WHEN estado = 'FINALIZADO' THEN 1 END), COUNT(CASE WHEN estado = 'EN_CURSO' THEN 1 END)
          INTO v_fin, v_curso FROM eco_juego_intentos WHERE competencia_id = c.id;
        o.put('ok', TRUE); o.put('competencia', json_competencia(c)); o.put('podio', a);
        o.put('participantes', v_part); o.put('finalizados', v_fin); o.put('enCurso', v_curso);
        o.put('actualizado', TO_CHAR(SYSTIMESTAMP AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.FF3"Z"'));
        p_json := o.to_clob; p_http := 200;
    EXCEPTION WHEN e_negocio THEN p_json := error_json; p_http := g_http;
    END;

    PROCEDURE revision(p_sesion IN VARCHAR2, p_json OUT CLOB, p_http OUT NUMBER) IS
        i eco_juego_intentos%ROWTYPE; c eco_juego_competencias%ROWTYPE;
        o JSON_OBJECT_T := JSON_OBJECT_T(); a JSON_ARRAY_T := JSON_ARRAY_T(); x JSON_OBJECT_T;
    BEGIN
        i := intento_por_sesion(p_sesion, FALSE);
        SELECT * INTO c FROM eco_juego_competencias WHERE id = i.competencia_id;
        IF c.estado <> 'FINALIZADA' THEN fallar(403, 'revision', 'La revisión estará disponible cuando termine el concurso.'); END IF;
        FOR q IN (SELECT bp.orden, p.enunciado, p.explicacion,
                         (SELECT texto FROM eco_juego_opciones WHERE pregunta_id = p.id AND es_correcta = 'S' AND ROWNUM = 1) correcta,
                         (SELECT o2.texto FROM eco_juego_respuestas r JOIN eco_juego_opciones o2 ON o2.id = r.opcion_id
                           WHERE r.intento_id = i.id AND r.pregunta_id = p.id) elegida,
                         (SELECT r.es_correcta FROM eco_juego_respuestas r WHERE r.intento_id = i.id AND r.pregunta_id = p.id) acierto
                    FROM eco_juego_banco_preguntas bp JOIN eco_juego_preguntas p ON p.id = bp.pregunta_id
                   WHERE bp.banco_id = NVL(c.banco_id, 1) ORDER BY bp.orden) LOOP
            x := JSON_OBJECT_T();
            x.put('orden', q.orden); x.put('enunciado', q.enunciado); x.put('correcta', q.correcta);
            x.put('elegida', q.elegida); x.put('acierto', NVL(q.acierto, 'N') = 'S'); x.put('explicacion', q.explicacion);
            a.append(x);
        END LOOP;
        o.put('ok', TRUE); o.put('revision', a);
        p_json := o.to_clob; p_http := 200;
    EXCEPTION WHEN e_negocio THEN p_json := error_json; p_http := g_http;
    END;

    -- ─────────────────────────── Administración ───────────────────────────
    PROCEDURE validar_admin(p_clave VARCHAR2) IS
        v_hash VARCHAR2(400);
    BEGIN
        IF NOT permitir('adm|' || origen, 600, 600) THEN fallar(429, 'limite', 'Demasiadas solicitudes administrativas.'); END IF;
        BEGIN SELECT valor INTO v_hash FROM eco_juego_configuracion WHERE clave = 'ADMIN_CLAVE_SHA256';
        EXCEPTION WHEN NO_DATA_FOUND THEN fallar(503, 'admin', 'El panel administrativo no está configurado.'); END;
        IF p_clave IS NULL OR LENGTH(p_clave) NOT BETWEEN 20 AND 200 OR sha256(p_clave) <> v_hash THEN
            IF NOT permitir('admfallo|' || origen, 10, 900) THEN fallar(429, 'limite', 'Demasiados intentos fallidos. Espera 15 minutos.'); END IF;
            fallar(401, 'admin', 'Clave de administración incorrecta.');
        END IF;
    END;

    FUNCTION json_participantes(p_comp NUMBER) RETURN JSON_ARRAY_T IS
        a JSON_ARRAY_T := JSON_ARRAY_T(); x JSON_OBJECT_T; v_resp VARCHAR2(400); v_banco NUMBER := banco_de(p_comp);
    BEGIN
        FOR r IN (SELECT p.id, p.nombre_completo, p.alias, p.registrado_en, p.aviso_version, i.id intento_id, i.estado, i.inicio, i.fin,
                         i.aciertos, i.errores, i.puntaje, i.tiempo_ms, i.posicion_final, v.posicion
                    FROM eco_juego_participantes p
                    JOIN eco_juego_intentos i ON i.participante_id = p.id
                    LEFT JOIN vw_juego_clasificacion v ON v.intento_id = i.id
                   WHERE p.competencia_id = p_comp
                   ORDER BY v.posicion NULLS LAST, p.registrado_en) LOOP
            SELECT LISTAGG(q.orden || ':' || o.letra || CASE r2.es_correcta WHEN 'S' THEN '✓' ELSE '✗' END, ' ') WITHIN GROUP (ORDER BY q.orden)
              INTO v_resp
              FROM eco_juego_respuestas r2
              JOIN eco_juego_banco_preguntas q ON q.pregunta_id = r2.pregunta_id AND q.banco_id = v_banco
              JOIN eco_juego_opciones o ON o.id = r2.opcion_id
             WHERE r2.intento_id = r.intento_id;
            x := JSON_OBJECT_T();
            x.put('id', r.id); x.put('nombre', r.nombre_completo); x.put('alias', r.alias);
            x.put('registrado', TO_CHAR(r.registrado_en, 'YYYY-MM-DD HH24:MI:SS'));
            x.put('estado', r.estado); x.put('inicio', TO_CHAR(r.inicio, 'HH24:MI:SS.FF3')); x.put('fin', TO_CHAR(r.fin, 'HH24:MI:SS.FF3'));
            x.put('aciertos', r.aciertos); x.put('errores', r.errores); x.put('puntaje', r.puntaje); x.put('tiempoMs', r.tiempo_ms);
            x.put('posicion', NVL(r.posicion_final, r.posicion)); x.put('respuestas', v_resp); x.put('aviso', r.aviso_version);
            a.append(x);
        END LOOP;
        RETURN a;
    END;

    PROCEDURE administrar(p_clave IN VARCHAR2, p_accion IN VARCHAR2, p_cuerpo IN CLOB, p_json OUT CLOB, p_http OUT NUMBER) IS
        b JSON_OBJECT_T; o JSON_OBJECT_T := JSON_OBJECT_T(); a JSON_ARRAY_T := JSON_ARRAY_T(); x JSON_OBJECT_T;
        c eco_juego_competencias%ROWTYPE; v_id NUMBER; n NUMBER; v_codigo VARCHAR2(40); v_prueba CHAR(1); v_nombre VARCHAR2(120);
        v_banco NUMBER; v_banco_txt VARCHAR2(400);
    BEGIN
        validar_admin(p_clave);
        BEGIN b := CASE WHEN p_cuerpo IS NULL OR DBMS_LOB.GETLENGTH(p_cuerpo) = 0 THEN JSON_OBJECT_T() ELSE JSON_OBJECT_T.parse(p_cuerpo) END;
        EXCEPTION WHEN OTHERS THEN fallar(400, 'solicitud', 'Solicitud inválida.'); END;
        IF b.has('competencia') THEN
            v_id := b.get_number('competencia');
            BEGIN SELECT * INTO c FROM eco_juego_competencias WHERE id = v_id FOR UPDATE;
            EXCEPTION WHEN NO_DATA_FOUND THEN fallar(404, 'competencia', 'Competencia inexistente.'); END;
        END IF;

        IF p_accion = 'competencias' THEN
            FOR r IN (SELECT k.*, (SELECT COUNT(*) FROM eco_juego_participantes p WHERE p.competencia_id = k.id) part,
                             (SELECT COUNT(*) FROM eco_juego_intentos t WHERE t.competencia_id = k.id AND t.estado = 'FINALIZADO') fin,
                             (SELECT b.codigo FROM eco_juego_bancos b WHERE b.id = NVL(k.banco_id, 1)) banco_codigo,
                             (SELECT COUNT(*) FROM eco_juego_banco_preguntas bp WHERE bp.banco_id = NVL(k.banco_id, 1)) npreguntas
                        FROM eco_juego_competencias k ORDER BY k.id DESC) LOOP
                x := JSON_OBJECT_T();
                x.put('id', r.id); x.put('codigo', r.codigo); x.put('nombre', r.nombre); x.put('estado', r.estado);
                x.put('prueba', r.es_prueba = 'S'); x.put('participantes', r.part); x.put('finalizados', r.fin);
                x.put('creada', TO_CHAR(r.creada_en, 'YYYY-MM-DD HH24:MI'));
                x.put('banco', r.banco_codigo); x.put('preguntas', r.npreguntas);
                a.append(x);
            END LOOP;
            o.put('competencias', a);
        ELSIF p_accion = 'crear' THEN
            v_prueba := CASE WHEN b.has('prueba') THEN CASE WHEN b.get_boolean('prueba') THEN 'S' ELSE 'N' END ELSE 'N' END;
            v_nombre := SUBSTR(NVL(TRIM(b.get_string('nombre')), 'EcoAccess Challenge · ExpoSoftware 2026'), 1, 120);
            v_codigo := CASE v_prueba WHEN 'S' THEN 'PRUEBA-' ELSE 'RONDA-' END
                        || TO_CHAR(SYSTIMESTAMP, 'YYMMDDHH24MISS') || '-' || DBMS_RANDOM.STRING('X', 6);
            -- Banco opcional por código; sin banco, la competencia usa las 7 preguntas de siempre (BANCO_ID nulo).
            v_banco_txt := CASE WHEN b.has('banco') THEN UPPER(TRIM(b.get_string('banco'))) END;
            IF v_banco_txt IS NOT NULL THEN
                BEGIN SELECT id INTO v_banco FROM eco_juego_bancos WHERE codigo = SUBSTR(v_banco_txt, 1, 40);
                EXCEPTION WHEN NO_DATA_FOUND THEN fallar(400, 'banco', 'Ese banco de preguntas no existe.'); END;
            END IF;
            INSERT INTO eco_juego_competencias (codigo, nombre, es_prueba, banco_id) VALUES (v_codigo, v_nombre, v_prueba, v_banco)
            RETURNING id INTO v_id;
            o.put('id', v_id); o.put('codigo', v_codigo); o.put('preguntas', total(NVL(v_banco, 1)));
        ELSIF p_accion IN ('abrir', 'cerrar_registro', 'finalizar', 'eliminar_prueba') THEN
            IF c.id IS NULL THEN fallar(400, 'competencia', 'Indica la competencia.'); END IF;
            IF p_accion = 'abrir' THEN
                IF c.estado = 'FINALIZADA' THEN fallar(409, 'estado', 'Una competencia finalizada no se reabre: crea una nueva ronda.'); END IF;
                IF c.es_prueba = 'N' THEN
                    SELECT COUNT(*) INTO n FROM eco_juego_competencias
                     WHERE es_prueba = 'N' AND id <> c.id AND estado IN ('ABIERTA', 'REGISTRO_CERRADO');
                    IF n > 0 THEN fallar(409, 'estado', 'Ya hay otra competencia oficial en curso: finalízala antes de abrir esta.'); END IF;
                END IF;
                UPDATE eco_juego_competencias SET estado = 'ABIERTA', abierta_en = NVL(abierta_en, SYSTIMESTAMP) WHERE id = c.id;
            ELSIF p_accion = 'cerrar_registro' THEN
                IF c.estado <> 'ABIERTA' THEN fallar(409, 'estado', 'Solo se cierra el registro de una competencia abierta.'); END IF;
                UPDATE eco_juego_competencias SET estado = 'REGISTRO_CERRADO', registro_cerrado_en = SYSTIMESTAMP WHERE id = c.id;
            ELSIF p_accion = 'finalizar' THEN
                IF c.estado = 'FINALIZADA' THEN fallar(409, 'estado', 'La competencia ya está finalizada.'); END IF;
                -- Cierra los intentos en curso con lo respondido y consolida las posiciones definitivas.
                FOR t IN (SELECT id FROM eco_juego_intentos WHERE competencia_id = c.id AND estado = 'EN_CURSO') LOOP
                    cerrar_intento(t.id, c.duracion_max_seg * 1000);
                END LOOP;
                UPDATE eco_juego_intentos t SET posicion_final = (SELECT v.posicion FROM vw_juego_clasificacion v WHERE v.intento_id = t.id)
                 WHERE t.competencia_id = c.id AND t.estado = 'FINALIZADO';
                UPDATE eco_juego_competencias SET estado = 'FINALIZADA', finalizada_en = SYSTIMESTAMP,
                       registro_cerrado_en = NVL(registro_cerrado_en, SYSTIMESTAMP) WHERE id = c.id;
            ELSIF p_accion = 'eliminar_prueba' THEN
                -- Solo rondas marcadas como prueba: las competencias oficiales conservan siempre su historial.
                IF c.es_prueba <> 'S' THEN fallar(403, 'prueba', 'Solo se pueden eliminar rondas de prueba.'); END IF;
                DELETE FROM eco_juego_respuestas WHERE intento_id IN (SELECT id FROM eco_juego_intentos WHERE competencia_id = c.id);
                DELETE FROM eco_juego_intentos WHERE competencia_id = c.id;
                DELETE FROM eco_juego_participantes WHERE competencia_id = c.id;
                DELETE FROM eco_juego_competencias WHERE id = c.id;
            END IF;
            o.put('accion', p_accion);
        ELSIF p_accion = 'participantes' THEN
            IF c.id IS NULL THEN fallar(400, 'competencia', 'Indica la competencia.'); END IF;
            o.put('competencia', json_competencia(c)); o.put('participantes', json_participantes(c.id));
        ELSE
            fallar(404, 'accion', 'Acción desconocida.');
        END IF;
        COMMIT;
        o.put('ok', TRUE);
        p_json := o.to_clob; p_http := 200;
    EXCEPTION WHEN e_negocio THEN ROLLBACK; p_json := error_json; p_http := g_http;
    END;

    PROCEDURE exportar(p_clave IN VARCHAR2, p_competencia IN NUMBER, p_csv OUT CLOB, p_http OUT NUMBER) IS
        v_resp VARCHAR2(400); v_banco NUMBER := banco_de(p_competencia);
        FUNCTION campo(s VARCHAR2) RETURN VARCHAR2 IS BEGIN RETURN '"' || REPLACE(s, '"', '""') || '"'; END;
    BEGIN
        validar_admin(p_clave);
        DBMS_LOB.CREATETEMPORARY(p_csv, TRUE);
        DBMS_LOB.APPEND(p_csv, 'posicion;alias;nombre_completo;estado;puntaje;aciertos;errores;tiempo_ms;inicio;fin;registrado;respuestas' || CHR(13) || CHR(10));
        FOR r IN (SELECT p.alias, p.nombre_completo, p.registrado_en, i.id intento_id, i.estado, i.inicio, i.fin, i.aciertos, i.errores,
                         i.puntaje, i.tiempo_ms, NVL(i.posicion_final, v.posicion) posicion
                    FROM eco_juego_participantes p JOIN eco_juego_intentos i ON i.participante_id = p.id
                    LEFT JOIN vw_juego_clasificacion v ON v.intento_id = i.id
                   WHERE p.competencia_id = p_competencia ORDER BY v.posicion NULLS LAST, p.registrado_en) LOOP
            SELECT LISTAGG(q.orden || ':' || o.letra || CASE r2.es_correcta WHEN 'S' THEN '+' ELSE '-' END, ' ') WITHIN GROUP (ORDER BY q.orden)
              INTO v_resp
              FROM eco_juego_respuestas r2
              JOIN eco_juego_banco_preguntas q ON q.pregunta_id = r2.pregunta_id AND q.banco_id = v_banco
              JOIN eco_juego_opciones o ON o.id = r2.opcion_id
             WHERE r2.intento_id = r.intento_id;
            DBMS_LOB.APPEND(p_csv, r.posicion || ';' || campo(r.alias) || ';' || campo(r.nombre_completo) || ';' || r.estado || ';'
                || r.puntaje || ';' || r.aciertos || ';' || r.errores || ';' || r.tiempo_ms || ';'
                || TO_CHAR(r.inicio, 'YYYY-MM-DD HH24:MI:SS.FF3') || ';' || TO_CHAR(r.fin, 'YYYY-MM-DD HH24:MI:SS.FF3') || ';'
                || TO_CHAR(r.registrado_en, 'YYYY-MM-DD HH24:MI:SS') || ';' || v_resp || CHR(13) || CHR(10));
        END LOOP;
        p_http := 200;
    EXCEPTION WHEN e_negocio THEN p_csv := error_json; p_http := g_http;
    END;

    PROCEDURE emitir(p_contenido IN CLOB, p_tipo IN VARCHAR2 DEFAULT 'application/json', p_archivo IN VARCHAR2 DEFAULT NULL) IS
        v_largo NUMBER := NVL(DBMS_LOB.GETLENGTH(p_contenido), 0); v_pos NUMBER := 1;
    BEGIN
        OWA_UTIL.MIME_HEADER(p_tipo || '; charset=utf-8', FALSE);
        HTP.P('Cache-Control: no-store');
        HTP.P('X-Content-Type-Options: nosniff');
        IF p_archivo IS NOT NULL THEN HTP.P('Content-Disposition: attachment; filename="' || p_archivo || '"'); END IF;
        OWA_UTIL.HTTP_HEADER_CLOSE;
        WHILE v_pos <= v_largo LOOP
            HTP.PRN(DBMS_LOB.SUBSTR(p_contenido, 4000, v_pos));
            v_pos := v_pos + 4000;
        END LOOP;
    END;
END pkg_eco_juego;
/
PROMPT V035_JUEGO_PAQUETE_BANCOS_APLICADA
