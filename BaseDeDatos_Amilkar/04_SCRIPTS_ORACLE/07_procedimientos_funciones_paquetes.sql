-- =====================================================================
-- EcoAccess BD — 07 · Procedimientos, funciones y paquetes (PL/SQL)
-- Ejecutar como ECOACCESS_BD después de 06 (F5). Se puede repetir.
--
-- Contenido:
--   PKG_CONTEXTO_APP        quién actúa (actor de la aplicación) y roles
--   FN_PESO_ESTIMADO_KG     función: unidades × peso típico del catálogo
--   FN_RESOLVER_CANECA_DISPONIBLE  función: qué caneca de la estación recibe el material
--   PR_CAMBIAR_ESTADO_CONTENEDOR   procedimiento: poner/quitar de servicio (audita trigger 1)
--   PKG_ECOPUNTOS           tarifas versionadas, crédito idempotente, reverso y saldo
--   PKG_DEPOSITO            verificar, rechazar y corregir (procedimiento auditado)
--   PKG_ESTADISTICAS        resumen y ranking con SYS_REFCURSOR
--   PR_CONSULTAR_ESTADISTICAS / FN_DEPOSITOS_CIUDADANO  (estilo de clase)
--
-- Regla de transacciones: NINGÚN procedimiento hace COMMIT. Quien llama
-- (la aplicación C# o el script) decide COMMIT o ROLLBACK; así verificar
-- un depósito y acreditar sus puntos ocurre TODO o NADA.
--
-- Códigos de error propios (RAISE_APPLICATION_ERROR, rango -20000..-20999):
--   -20100 actor inexistente o inactivo     -20120 permiso insuficiente
--   -20101 verificador = titular            -20121 estado de caneca no válido
--   -20102 caneca no admite el material     -20201 depósito no verificado
--   -20103 verificación cerrada             -20203 tarifas solapadas
--   -20104 verificador sin rol              -20204 tarifa por unidad sin conteo
--   -20105 material distinto sin motivo     -20205 no hay crédito que revertir
--   -20106 peso estimado fuera de rango     -20206 regla no válida
--   -20107 depósito debe nacer pendiente    -20801..-20803 filtros de estadística
--   -20108 evento no coincide con sensor    -20810..-20819 resolver caneca
--   -20109 dato inmutable
--   -20110 depósito inexistente  -20111 ya cerrado  -20112 falta motivo
-- =====================================================================
SET SERVEROUTPUT ON

-- ---------------------------------------------------------------------
-- PKG_CONTEXTO_APP: Oracle solo conoce la CUENTA de base de datos (USER),
-- que es la misma para todas las personas que usan la aplicación. Para
-- saber QUÉ PERSONA actuó, el servicio C# llama a fijar_actor después de
-- autenticar al usuario. Los disparadores leen ese valor.
-- ---------------------------------------------------------------------
CREATE OR REPLACE PACKAGE pkg_contexto_app AS
    PROCEDURE fijar_actor(p_id_usuario IN usuario.id_usuario%TYPE,
                          p_motivo     IN VARCHAR2 DEFAULT NULL);
    PROCEDURE limpiar;
    FUNCTION  actor  RETURN usuario.id_usuario%TYPE;
    FUNCTION  motivo RETURN VARCHAR2;
    -- 1 si el usuario está activo y tiene el rol (1 Ciudadano, 2 Operario, 3 Administrador).
    FUNCTION  tiene_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigo IN rol.codigo%TYPE) RETURN NUMBER;
    PROCEDURE exigir_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigos IN VARCHAR2);
END pkg_contexto_app;
/
SHOW ERRORS PACKAGE pkg_contexto_app

CREATE OR REPLACE PACKAGE BODY pkg_contexto_app AS
    g_actor  usuario.id_usuario%TYPE;
    g_motivo VARCHAR2(300 CHAR);

    PROCEDURE fijar_actor(p_id_usuario IN usuario.id_usuario%TYPE,
                          p_motivo     IN VARCHAR2 DEFAULT NULL) IS
        v_activo usuario.activo%TYPE;
    BEGIN
        SELECT activo INTO v_activo FROM usuario WHERE id_usuario = p_id_usuario;
        IF v_activo <> 1 THEN
            RAISE_APPLICATION_ERROR(-20100, 'El actor ' || p_id_usuario || ' está inactivo.');
        END IF;
        g_actor  := p_id_usuario;
        g_motivo := p_motivo;
        -- Queda visible también en V$SESSION.CLIENT_IDENTIFIER para el DBA.
        DBMS_SESSION.SET_IDENTIFIER('ECO_U' || p_id_usuario);
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20100, 'El actor ' || p_id_usuario || ' no existe.');
    END fijar_actor;

    PROCEDURE limpiar IS
    BEGIN
        g_actor := NULL;
        g_motivo := NULL;
        DBMS_SESSION.CLEAR_IDENTIFIER;
    END limpiar;

    FUNCTION actor RETURN usuario.id_usuario%TYPE IS
    BEGIN
        RETURN g_actor;
    END actor;

    FUNCTION motivo RETURN VARCHAR2 IS
    BEGIN
        RETURN g_motivo;
    END motivo;

    FUNCTION tiene_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigo IN rol.codigo%TYPE) RETURN NUMBER IS
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n
        FROM   usuario u
        JOIN   usuario_rol ur ON ur.id_usuario = u.id_usuario
        JOIN   rol r          ON r.id_rol = ur.id_rol
        WHERE  u.id_usuario = p_id_usuario AND u.activo = 1 AND r.codigo = p_codigo;
        RETURN CASE WHEN n > 0 THEN 1 ELSE 0 END;
    END tiene_rol;

    -- p_codigos es una lista como '2,3'. Lanza -20120 si no tiene ninguno.
    PROCEDURE exigir_rol(p_id_usuario IN usuario.id_usuario%TYPE, p_codigos IN VARCHAR2) IS
        n NUMBER;
    BEGIN
        SELECT COUNT(*) INTO n
        FROM   usuario u
        JOIN   usuario_rol ur ON ur.id_usuario = u.id_usuario
        JOIN   rol r          ON r.id_rol = ur.id_rol
        WHERE  u.id_usuario = p_id_usuario AND u.activo = 1
        AND    INSTR(',' || p_codigos || ',', ',' || r.codigo || ',') > 0;
        IF n = 0 THEN
            RAISE_APPLICATION_ERROR(-20120, 'El usuario ' || NVL(TO_CHAR(p_id_usuario), '(nulo)')
                || ' no tiene un rol permitido (' || p_codigos || ') o está inactivo.');
        END IF;
    END exigir_rol;
END pkg_contexto_app;
/
SHOW ERRORS PACKAGE BODY pkg_contexto_app

-- ---------------------------------------------------------------------
-- FUNCIÓN: peso ESTIMADO en kg = unidades × gramos típicos / 1000.
-- Nunca devuelve un peso «medido»; solo una estimación del catálogo.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_peso_estimado_kg (
    p_id_peso_referencial IN peso_referencial.id_peso_referencial%TYPE,
    p_unidades            IN NUMBER
) RETURN NUMBER
IS
    v_gramos peso_referencial.gramos_tipico%TYPE;
BEGIN
    IF p_id_peso_referencial IS NULL OR p_unidades IS NULL OR p_unidades < 1 THEN
        RETURN NULL;                       -- sin catálogo o sin conteo no se inventa un peso
    END IF;
    SELECT gramos_tipico INTO v_gramos
    FROM   peso_referencial
    WHERE  id_peso_referencial = p_id_peso_referencial;
    RETURN ROUND(v_gramos * p_unidades / 1000, 4);
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN NULL;
END fn_peso_estimado_kg;
/
SHOW ERRORS FUNCTION fn_peso_estimado_kg

-- ---------------------------------------------------------------------
-- FUNCIÓN: dada una estación y un material, ¿qué caneca lo recibe?
-- (Adaptación de FN_RESOLVER_CANECA_DISPONIBLE de P3/V022, mismos códigos.)
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_resolver_caneca_disponible (
    p_id_punto    IN punto_instalacion.id_punto_instalacion%TYPE,
    p_id_material IN material.id_material%TYPE
) RETURN contenedor.id_contenedor%TYPE
IS
    v_activo     punto_instalacion.activo%TYPE;
    v_categoria  material.categoria%TYPE;
    v_habilitado material.habilitado%TYPE;
    v_cantidad   PLS_INTEGER;
    v_id         contenedor.id_contenedor%TYPE;
    v_estado     contenedor.estado%TYPE;
BEGIN
    IF p_id_punto IS NULL OR p_id_material IS NULL THEN
        RAISE_APPLICATION_ERROR(-20810, 'Identificadores no válidos.');
    END IF;
    BEGIN
        SELECT activo INTO v_activo FROM punto_instalacion WHERE id_punto_instalacion = p_id_punto;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20818, 'Estación inexistente.');
    END;
    IF v_activo <> 1 THEN
        RAISE_APPLICATION_ERROR(-20819, 'Estación inactiva.');
    END IF;
    BEGIN
        SELECT categoria, habilitado INTO v_categoria, v_habilitado
        FROM   material WHERE id_material = p_id_material;
    EXCEPTION WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20811, 'Material inexistente.');
    END;
    IF v_habilitado <> 1 THEN
        RAISE_APPLICATION_ERROR(-20812, 'Material deshabilitado.');
    ELSIF v_categoria NOT IN (1, 2, 3) THEN
        RAISE_APPLICATION_ERROR(-20813, 'Material de ruta especial: no va en ninguna caneca.');
    END IF;
    SELECT COUNT(*), MIN(id_contenedor), MIN(estado)
    INTO   v_cantidad, v_id, v_estado
    FROM   contenedor
    WHERE  id_punto_instalacion = p_id_punto AND estado <> 3 AND categoria = v_categoria;
    IF v_cantidad = 0 THEN
        RAISE_APPLICATION_ERROR(-20814, 'La estación no tiene caneca de ese color.');
    ELSIF v_cantidad > 1 THEN
        RAISE_APPLICATION_ERROR(-20815, 'Configuración ambigua.');   -- el índice único lo impide; defensa extra
    ELSIF v_estado <> 1 THEN
        RAISE_APPLICATION_ERROR(-20816, 'Caneca fuera de servicio.');
    END IF;
    SELECT COUNT(*) INTO v_cantidad FROM contenedor_material
    WHERE  id_contenedor = v_id AND id_material = p_id_material;
    IF v_cantidad <> 1 THEN
        RAISE_APPLICATION_ERROR(-20817, 'La caneca no admite ese material.');
    END IF;
    RETURN v_id;
END fn_resolver_caneca_disponible;
/
SHOW ERRORS FUNCTION fn_resolver_caneca_disponible

-- ---------------------------------------------------------------------
-- PROCEDIMIENTO: cambiar el estado de una caneca (solo Administrador).
-- El trigger TRG_CONTENEDOR_AUD_ESTADO deja la historia automáticamente.
-- ---------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE pr_cambiar_estado_contenedor (
    p_id_contenedor IN contenedor.id_contenedor%TYPE,
    p_estado        IN contenedor.estado%TYPE,
    p_id_actor      IN usuario.id_usuario%TYPE,
    p_motivo        IN VARCHAR2
)
IS
BEGIN
    pkg_contexto_app.exigir_rol(p_id_actor, '3');
    IF p_estado NOT IN (1, 2, 3) THEN
        RAISE_APPLICATION_ERROR(-20121, 'Estado de caneca no válido: ' || p_estado);
    END IF;
    pkg_contexto_app.fijar_actor(p_id_actor, p_motivo);
    UPDATE contenedor SET estado = p_estado WHERE id_contenedor = p_id_contenedor;
    IF SQL%ROWCOUNT = 0 THEN
        RAISE_APPLICATION_ERROR(-20121, 'No existe la caneca ' || p_id_contenedor);
    END IF;
    pkg_contexto_app.limpiar;
EXCEPTION
    WHEN OTHERS THEN
        pkg_contexto_app.limpiar;
        RAISE;
END pr_cambiar_estado_contenedor;
/
SHOW ERRORS PROCEDURE pr_cambiar_estado_contenedor

-- ---------------------------------------------------------------------
-- PKG_ECOPUNTOS: la ÚNICA puerta para crear EcoPuntos.
-- (Ningún trigger acredita puntos: así no hay doble acreditación con C#.)
-- ---------------------------------------------------------------------
CREATE OR REPLACE PACKAGE pkg_ecopuntos AS
    FUNCTION  regla_vigente(p_id_material IN material.id_material%TYPE,
                            p_fecha_utc   IN TIMESTAMP) RETURN regla_ecopuntos.id_regla%TYPE;
    PROCEDURE registrar_regla(p_id_material  IN material.id_material%TYPE,
                              p_puntos       IN regla_ecopuntos.puntos%TYPE,
                              p_por_unidad   IN regla_ecopuntos.por_unidad%TYPE,
                              p_descripcion  IN regla_ecopuntos.descripcion%TYPE,
                              p_desde_utc    IN TIMESTAMP,
                              p_id_actor     IN usuario.id_usuario%TYPE,
                              p_es_sintetica IN regla_ecopuntos.es_sintetica%TYPE DEFAULT 0,
                              p_id_regla     OUT regla_ecopuntos.id_regla%TYPE);
    PROCEDURE acreditar(p_id_deposito   IN deposito.id_deposito%TYPE,
                        p_id_actor      IN usuario.id_usuario%TYPE,
                        p_id_movimiento OUT movimiento_ecopuntos.id_movimiento%TYPE);
    PROCEDURE revertir(p_id_deposito IN deposito.id_deposito%TYPE,
                       p_id_actor    IN usuario.id_usuario%TYPE,
                       p_motivo      IN VARCHAR2);
    FUNCTION  saldo(p_id_usuario IN usuario.id_usuario%TYPE) RETURN NUMBER;
END pkg_ecopuntos;
/
SHOW ERRORS PACKAGE pkg_ecopuntos

CREATE OR REPLACE PACKAGE BODY pkg_ecopuntos AS

    FUNCTION regla_vigente(p_id_material IN material.id_material%TYPE,
                           p_fecha_utc   IN TIMESTAMP) RETURN regla_ecopuntos.id_regla%TYPE IS
        v_id regla_ecopuntos.id_regla%TYPE;
    BEGIN
        SELECT id_regla INTO v_id
        FROM   regla_ecopuntos
        WHERE  id_material = p_id_material
        AND    desde_utc <= p_fecha_utc
        AND    (hasta_utc IS NULL OR hasta_utc > p_fecha_utc);
        RETURN v_id;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN NULL;                                   -- sin tarifa aprobada: no hay puntos
        WHEN TOO_MANY_ROWS THEN
            RAISE_APPLICATION_ERROR(-20203, 'Hay dos tarifas vigentes para el material ' || p_id_material);
    END regla_vigente;

    PROCEDURE registrar_regla(p_id_material  IN material.id_material%TYPE,
                              p_puntos       IN regla_ecopuntos.puntos%TYPE,
                              p_por_unidad   IN regla_ecopuntos.por_unidad%TYPE,
                              p_descripcion  IN regla_ecopuntos.descripcion%TYPE,
                              p_desde_utc    IN TIMESTAMP,
                              p_id_actor     IN usuario.id_usuario%TYPE,
                              p_es_sintetica IN regla_ecopuntos.es_sintetica%TYPE DEFAULT 0,
                              p_id_regla     OUT regla_ecopuntos.id_regla%TYPE) IS
        v_posteriores NUMBER;
    BEGIN
        pkg_contexto_app.exigir_rol(p_id_actor, '3');      -- solo Administrador fija tarifas
        IF p_desde_utc IS NULL OR p_puntos IS NULL OR p_puntos < 1 THEN
            RAISE_APPLICATION_ERROR(-20206, 'Regla no válida: puntos >= 1 y fecha de inicio obligatoria.');
        END IF;
        -- Bloquea las reglas del material para que dos administradores no versionen a la vez.
        FOR r IN (SELECT id_regla FROM regla_ecopuntos WHERE id_material = p_id_material FOR UPDATE) LOOP
            NULL;
        END LOOP;
        SELECT COUNT(*) INTO v_posteriores FROM regla_ecopuntos
        WHERE  id_material = p_id_material AND desde_utc >= p_desde_utc;
        IF v_posteriores > 0 THEN
            RAISE_APPLICATION_ERROR(-20206, 'Ya existe una versión que empieza en o después de esa fecha.');
        END IF;
        -- Cierra la versión abierta: nunca se modifica el valor de una tarifa usada.
        UPDATE regla_ecopuntos SET hasta_utc = p_desde_utc
        WHERE  id_material = p_id_material AND hasta_utc IS NULL;
        INSERT INTO regla_ecopuntos (id_material, puntos, por_unidad, descripcion, desde_utc, id_creador, es_sintetica)
        VALUES (p_id_material, p_puntos, p_por_unidad, p_descripcion, p_desde_utc, p_id_actor, p_es_sintetica)
        RETURNING id_regla INTO p_id_regla;
    END registrar_regla;

    PROCEDURE acreditar(p_id_deposito   IN deposito.id_deposito%TYPE,
                        p_id_actor      IN usuario.id_usuario%TYPE,
                        p_id_movimiento OUT movimiento_ecopuntos.id_movimiento%TYPE) IS
        v_estado    deposito.estado_verificacion%TYPE;
        v_material  deposito.id_material_verificado%TYPE;
        v_cantidad  deposito.cantidad_verificada%TYPE;
        v_fecha     deposito.fecha_deteccion_utc%TYPE;
        v_titular   interaccion.id_usuario%TYPE;
        v_regla     regla_ecopuntos.id_regla%TYPE;
        v_puntos    regla_ecopuntos.puntos%TYPE;
        v_unidad    regla_ecopuntos.por_unidad%TYPE;
        v_creditos  NUMBER;
    BEGIN
        pkg_contexto_app.exigir_rol(p_id_actor, '2,3');
        -- FOR UPDATE: si dos procesos acreditan el mismo depósito a la vez,
        -- el segundo espera y luego encuentra el crédito ya hecho.
        SELECT d.estado_verificacion, d.id_material_verificado, d.cantidad_verificada, d.fecha_deteccion_utc, i.id_usuario
        INTO   v_estado, v_material, v_cantidad, v_fecha, v_titular
        FROM   deposito d
        JOIN   orden_apertura o ON o.id_orden = d.id_orden
        JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
        WHERE  d.id_deposito = p_id_deposito
        FOR UPDATE OF d.estado_verificacion;

        IF v_estado <> 1 THEN
            RAISE_APPLICATION_ERROR(-20201, 'Solo se acreditan depósitos VERIFICADOS (depósito ' || p_id_deposito || ').');
        END IF;
        IF v_titular IS NULL THEN
            p_id_movimiento := NULL;                       -- visitante sin cuenta: nadie recibe puntos
            RETURN;
        END IF;

        -- Idempotencia: si ya hay un crédito sin revertir, se devuelve ese mismo.
        BEGIN
            SELECT m.id_movimiento INTO p_id_movimiento
            FROM   movimiento_ecopuntos m
            WHERE  m.id_deposito = p_id_deposito AND m.tipo = 1
            AND    NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos r WHERE r.id_movimiento_revertido = m.id_movimiento);
            RETURN;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN NULL;
        END;

        v_regla := regla_vigente(v_material, v_fecha);
        IF v_regla IS NULL THEN
            p_id_movimiento := NULL;                       -- sin tarifa aprobada: el saldo NO aumenta
            RETURN;
        END IF;
        SELECT puntos, por_unidad INTO v_puntos, v_unidad FROM regla_ecopuntos WHERE id_regla = v_regla;
        IF v_unidad = 1 THEN
            IF v_cantidad IS NULL THEN
                RAISE_APPLICATION_ERROR(-20204, 'La tarifa es por unidad y el depósito no tiene conteo verificado.');
            END IF;
            v_puntos := v_puntos * v_cantidad;
        END IF;

        SELECT COUNT(*) INTO v_creditos FROM movimiento_ecopuntos WHERE id_deposito = p_id_deposito AND tipo = 1;
        INSERT INTO movimiento_ecopuntos (clave_idempotencia, tipo, id_deposito, id_usuario, id_regla,
                                          puntos, id_actor, motivo)
        VALUES ('CRED-' || p_id_deposito || '-' || (v_creditos + 1), 1, p_id_deposito, v_titular, v_regla,
                v_puntos, p_id_actor, 'Crédito por depósito verificado')
        RETURNING id_movimiento INTO p_id_movimiento;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20110, 'No existe el depósito ' || p_id_deposito);
    END acreditar;

    PROCEDURE revertir(p_id_deposito IN deposito.id_deposito%TYPE,
                       p_id_actor    IN usuario.id_usuario%TYPE,
                       p_motivo      IN VARCHAR2) IS
        v_credito movimiento_ecopuntos%ROWTYPE;
    BEGIN
        pkg_contexto_app.exigir_rol(p_id_actor, '2,3');
        IF TRIM(p_motivo) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20112, 'El reverso exige un motivo.');
        END IF;
        SELECT m.* INTO v_credito
        FROM   movimiento_ecopuntos m
        WHERE  m.id_deposito = p_id_deposito AND m.tipo = 1
        AND    NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos r WHERE r.id_movimiento_revertido = m.id_movimiento)
        FOR UPDATE;
        INSERT INTO movimiento_ecopuntos (clave_idempotencia, tipo, id_deposito, id_usuario, id_regla,
                                          puntos, id_movimiento_revertido, id_actor, motivo)
        VALUES ('REV-' || v_credito.id_movimiento, 2, p_id_deposito, v_credito.id_usuario, v_credito.id_regla,
                -v_credito.puntos, v_credito.id_movimiento, p_id_actor, TRIM(p_motivo));
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20205, 'El depósito ' || p_id_deposito || ' no tiene crédito vigente.');
    END revertir;

    FUNCTION saldo(p_id_usuario IN usuario.id_usuario%TYPE) RETURN NUMBER IS
        v_saldo NUMBER;
    BEGIN
        SELECT NVL(SUM(puntos), 0) INTO v_saldo FROM movimiento_ecopuntos WHERE id_usuario = p_id_usuario;
        RETURN v_saldo;
    END saldo;
END pkg_ecopuntos;
/
SHOW ERRORS PACKAGE BODY pkg_ecopuntos

-- ---------------------------------------------------------------------
-- PKG_DEPOSITO: verificar, rechazar y corregir un depósito.
-- correccion_en_curso es leída por el trigger TRG_DEPOSITO_BLOQUEO_CERRADO:
-- la variable que la activa es PRIVADA del cuerpo, así que solo este
-- paquete puede abrir la «puerta» de corrección.
-- ---------------------------------------------------------------------
CREATE OR REPLACE PACKAGE pkg_deposito AS
    PROCEDURE verificar(p_id_deposito    IN deposito.id_deposito%TYPE,
                        p_id_verificador IN usuario.id_usuario%TYPE,
                        p_id_material    IN material.id_material%TYPE,
                        p_cantidad       IN deposito.cantidad_verificada%TYPE,
                        p_peso_medido_kg IN deposito.peso_medido_kg%TYPE DEFAULT NULL,
                        p_instrumento    IN deposito.instrumento_medicion%TYPE DEFAULT NULL,
                        p_precision_g    IN deposito.precision_medicion_g%TYPE DEFAULT NULL,
                        p_motivo         IN deposito.motivo_verificacion%TYPE DEFAULT NULL);
    PROCEDURE rechazar(p_id_deposito    IN deposito.id_deposito%TYPE,
                       p_id_verificador IN usuario.id_usuario%TYPE,
                       p_motivo         IN deposito.motivo_verificacion%TYPE);
    -- p_nuevo_estado: 0 reabrir (vuelve a pendiente), 1 verificado con otros datos, 2 rechazado.
    PROCEDURE corregir_verificacion(p_id_deposito    IN deposito.id_deposito%TYPE,
                                    p_id_actor       IN usuario.id_usuario%TYPE,
                                    p_nuevo_estado   IN deposito.estado_verificacion%TYPE,
                                    p_id_material    IN material.id_material%TYPE,
                                    p_cantidad       IN deposito.cantidad_verificada%TYPE,
                                    p_motivo         IN VARCHAR2);
    FUNCTION correccion_en_curso RETURN BOOLEAN;
END pkg_deposito;
/
SHOW ERRORS PACKAGE pkg_deposito

CREATE OR REPLACE PACKAGE BODY pkg_deposito AS
    g_correccion BOOLEAN := FALSE;

    FUNCTION correccion_en_curso RETURN BOOLEAN IS
    BEGIN
        RETURN g_correccion;
    END correccion_en_curso;

    -- Explica por qué un UPDATE con WHERE estado = 0 no tocó ninguna fila.
    PROCEDURE explicar_sin_fila(p_id_deposito IN deposito.id_deposito%TYPE) IS
        v_estado deposito.estado_verificacion%TYPE;
    BEGIN
        SELECT estado_verificacion INTO v_estado FROM deposito WHERE id_deposito = p_id_deposito;
        RAISE_APPLICATION_ERROR(-20111, 'El depósito ' || p_id_deposito || ' ya está cerrado (estado '
            || v_estado || '). Usa corregir_verificacion con un motivo.');
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RAISE_APPLICATION_ERROR(-20110, 'No existe el depósito ' || p_id_deposito);
    END explicar_sin_fila;

    PROCEDURE verificar(p_id_deposito    IN deposito.id_deposito%TYPE,
                        p_id_verificador IN usuario.id_usuario%TYPE,
                        p_id_material    IN material.id_material%TYPE,
                        p_cantidad       IN deposito.cantidad_verificada%TYPE,
                        p_peso_medido_kg IN deposito.peso_medido_kg%TYPE DEFAULT NULL,
                        p_instrumento    IN deposito.instrumento_medicion%TYPE DEFAULT NULL,
                        p_precision_g    IN deposito.precision_medicion_g%TYPE DEFAULT NULL,
                        p_motivo         IN deposito.motivo_verificacion%TYPE DEFAULT NULL) IS
        v_mov movimiento_ecopuntos.id_movimiento%TYPE;
    BEGIN
        pkg_contexto_app.fijar_actor(p_id_verificador, p_motivo);
        UPDATE deposito
        SET    estado_verificacion    = 1,
               id_material_verificado = p_id_material,
               cantidad_verificada    = p_cantidad,
               id_verificador         = p_id_verificador,
               fecha_verificacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
               peso_medido_kg         = p_peso_medido_kg,
               instrumento_medicion   = p_instrumento,
               precision_medicion_g   = p_precision_g,
               motivo_verificacion    = p_motivo
        WHERE  id_deposito = p_id_deposito AND estado_verificacion = 0;
        IF SQL%ROWCOUNT = 0 THEN
            explicar_sin_fila(p_id_deposito);
        END IF;
        -- Misma transacción: si no hay tarifa, v_mov queda NULL y el saldo no cambia.
        pkg_ecopuntos.acreditar(p_id_deposito, p_id_verificador, v_mov);
        pkg_contexto_app.limpiar;
    EXCEPTION
        WHEN OTHERS THEN
            pkg_contexto_app.limpiar;
            RAISE;
    END verificar;

    PROCEDURE rechazar(p_id_deposito    IN deposito.id_deposito%TYPE,
                       p_id_verificador IN usuario.id_usuario%TYPE,
                       p_motivo         IN deposito.motivo_verificacion%TYPE) IS
    BEGIN
        IF TRIM(p_motivo) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20112, 'Rechazar exige un motivo.');
        END IF;
        pkg_contexto_app.fijar_actor(p_id_verificador, p_motivo);
        UPDATE deposito
        SET    estado_verificacion    = 2,
               id_verificador         = p_id_verificador,
               fecha_verificacion_utc = SYS_EXTRACT_UTC(SYSTIMESTAMP),
               motivo_verificacion    = TRIM(p_motivo)
        WHERE  id_deposito = p_id_deposito AND estado_verificacion = 0;
        IF SQL%ROWCOUNT = 0 THEN
            explicar_sin_fila(p_id_deposito);
        END IF;
        pkg_contexto_app.limpiar;
    EXCEPTION
        WHEN OTHERS THEN
            pkg_contexto_app.limpiar;
            RAISE;
    END rechazar;

    PROCEDURE corregir_verificacion(p_id_deposito    IN deposito.id_deposito%TYPE,
                                    p_id_actor       IN usuario.id_usuario%TYPE,
                                    p_nuevo_estado   IN deposito.estado_verificacion%TYPE,
                                    p_id_material    IN material.id_material%TYPE,
                                    p_cantidad       IN deposito.cantidad_verificada%TYPE,
                                    p_motivo         IN VARCHAR2) IS
        v_estado  deposito.estado_verificacion%TYPE;
        v_creditos NUMBER;
        v_mov     movimiento_ecopuntos.id_movimiento%TYPE;
    BEGIN
        IF TRIM(p_motivo) IS NULL THEN
            RAISE_APPLICATION_ERROR(-20112, 'Una corrección exige un motivo.');
        END IF;
        pkg_contexto_app.exigir_rol(p_id_actor, '2,3');
        SELECT estado_verificacion INTO v_estado FROM deposito WHERE id_deposito = p_id_deposito FOR UPDATE;
        IF v_estado = 0 THEN
            RAISE_APPLICATION_ERROR(-20111, 'El depósito está pendiente: usa verificar o rechazar.');
        END IF;

        -- 1) Si tenía puntos vigentes, se revierten (el libro nunca se borra).
        SELECT COUNT(*) INTO v_creditos
        FROM   movimiento_ecopuntos m
        WHERE  m.id_deposito = p_id_deposito AND m.tipo = 1
        AND    NOT EXISTS (SELECT 1 FROM movimiento_ecopuntos r WHERE r.id_movimiento_revertido = m.id_movimiento);
        IF v_creditos > 0 THEN
            pkg_ecopuntos.revertir(p_id_deposito, p_id_actor, 'Corrección: ' || TRIM(p_motivo));
        END IF;

        -- 2) Se abre la puerta SOLO durante este UPDATE.
        pkg_contexto_app.fijar_actor(p_id_actor, TRIM(p_motivo));
        g_correccion := TRUE;
        UPDATE deposito
        SET    estado_verificacion    = p_nuevo_estado,
               id_material_verificado = CASE WHEN p_nuevo_estado = 1 THEN p_id_material END,
               cantidad_verificada    = CASE WHEN p_nuevo_estado = 1 THEN p_cantidad END,
               id_verificador         = CASE WHEN p_nuevo_estado IN (1, 2) THEN p_id_actor END,
               fecha_verificacion_utc = CASE WHEN p_nuevo_estado IN (1, 2) THEN SYS_EXTRACT_UTC(SYSTIMESTAMP) END,
               motivo_verificacion    = CASE WHEN p_nuevo_estado IN (1, 2) THEN TRIM(p_motivo) END,
               peso_medido_kg         = CASE WHEN p_nuevo_estado = 1 THEN peso_medido_kg END,
               instrumento_medicion   = CASE WHEN p_nuevo_estado = 1 THEN instrumento_medicion END,
               precision_medicion_g   = CASE WHEN p_nuevo_estado = 1 THEN precision_medicion_g END
        WHERE  id_deposito = p_id_deposito;
        g_correccion := FALSE;

        -- 3) Si queda verificado, se acredita con la tarifa vigente (nueva clave).
        IF p_nuevo_estado = 1 THEN
            pkg_ecopuntos.acreditar(p_id_deposito, p_id_actor, v_mov);
        END IF;
        pkg_contexto_app.limpiar;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            g_correccion := FALSE;
            pkg_contexto_app.limpiar;
            RAISE_APPLICATION_ERROR(-20110, 'No existe el depósito ' || p_id_deposito);
        WHEN OTHERS THEN
            g_correccion := FALSE;                  -- la puerta NUNCA queda abierta tras un error
            pkg_contexto_app.limpiar;
            RAISE;
    END corregir_verificacion;
END pkg_deposito;
/
SHOW ERRORS PACKAGE BODY pkg_deposito

-- ---------------------------------------------------------------------
-- PKG_ESTADISTICAS: consultas parametrizadas que devuelven un cursor
-- (SYS_REFCURSOR) para C#, SQL Developer o Power BI.
-- Las fechas se reciben en hora de Colombia y se convierten a UTC aquí.
-- ---------------------------------------------------------------------
CREATE OR REPLACE PACKAGE pkg_estadisticas AS
    PROCEDURE consultar_resumen(p_desde_col  IN DATE,
                                p_hasta_col  IN DATE,
                                p_id_punto   IN NUMBER,
                                p_categoria  IN NUMBER,
                                p_resultado  OUT SYS_REFCURSOR);
    FUNCTION ranking_verificados(p_desde_col IN DATE,
                                 p_hasta_col IN DATE,
                                 p_top       IN PLS_INTEGER DEFAULT 10) RETURN SYS_REFCURSOR;
    FUNCTION a_utc(p_fecha_col IN DATE) RETURN TIMESTAMP;
END pkg_estadisticas;
/
SHOW ERRORS PACKAGE pkg_estadisticas

CREATE OR REPLACE PACKAGE BODY pkg_estadisticas AS
    FUNCTION a_utc(p_fecha_col IN DATE) RETURN TIMESTAMP IS
    BEGIN
        RETURN SYS_EXTRACT_UTC(FROM_TZ(CAST(p_fecha_col AS TIMESTAMP), 'America/Bogota'));
    END a_utc;

    PROCEDURE validar(p_desde_col IN DATE, p_hasta_col IN DATE) IS
    BEGIN
        IF p_desde_col IS NULL OR p_hasta_col IS NULL OR p_hasta_col <= p_desde_col
           OR p_hasta_col - p_desde_col > 400 THEN
            RAISE_APPLICATION_ERROR(-20801, 'El periodo debe ser positivo y no superar 400 días.');
        END IF;
    END validar;

    PROCEDURE consultar_resumen(p_desde_col  IN DATE,
                                p_hasta_col  IN DATE,
                                p_id_punto   IN NUMBER,
                                p_categoria  IN NUMBER,
                                p_resultado  OUT SYS_REFCURSOR) IS
        v_desde TIMESTAMP;
        v_hasta TIMESTAMP;
    BEGIN
        validar(p_desde_col, p_hasta_col);
        IF p_id_punto IS NOT NULL AND p_id_punto <= 0 THEN
            RAISE_APPLICATION_ERROR(-20802, 'Estación no válida.');
        END IF;
        IF p_categoria IS NOT NULL AND p_categoria NOT IN (1, 2, 3, 4) THEN
            RAISE_APPLICATION_ERROR(-20803, 'Categoría no válida.');
        END IF;
        v_desde := a_utc(p_desde_col);
        v_hasta := a_utc(p_hasta_col);
        OPEN p_resultado FOR
            SELECT COUNT(*)                                                         AS depositos,
                   COUNT(CASE WHEN d.estado_verificacion = 1 THEN 1 END)            AS verificados,
                   COUNT(CASE WHEN d.estado_verificacion = 0 THEN 1 END)            AS pendientes,
                   COUNT(CASE WHEN d.estado_verificacion = 2 THEN 1 END)            AS rechazados,
                   COUNT(CASE WHEN d.sin_sensor = 1 THEN 1 END)                     AS sin_sensor,
                   COUNT(DISTINCT i.id_usuario)                                     AS ciudadanos,
                   NVL(SUM(CASE WHEN d.estado_verificacion = 1 THEN d.cantidad_verificada END), 0) AS unidades_verificadas,
                   NVL(SUM(CASE WHEN d.estado_verificacion = 1 THEN d.peso_estimado_kg END), 0)    AS kg_estimados_verificados,
                   COUNT(d.peso_medido_kg)                                          AS con_peso_medido,
                   NVL(SUM(d.peso_medido_kg), 0)                                    AS kg_medidos
            FROM   deposito d
            JOIN   orden_apertura o ON o.id_orden = d.id_orden
            JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
            JOIN   contenedor c     ON c.id_contenedor = o.id_contenedor
            WHERE  d.fecha_deteccion_utc >= v_desde
            AND    d.fecha_deteccion_utc <  v_hasta
            AND    (p_id_punto  IS NULL OR c.id_punto_instalacion = p_id_punto)
            AND    (p_categoria IS NULL OR c.categoria = p_categoria);
    END consultar_resumen;

    FUNCTION ranking_verificados(p_desde_col IN DATE,
                                 p_hasta_col IN DATE,
                                 p_top       IN PLS_INTEGER DEFAULT 10) RETURN SYS_REFCURSOR IS
        v_cursor SYS_REFCURSOR;
    BEGIN
        validar(p_desde_col, p_hasta_col);
        OPEN v_cursor FOR
            SELECT *
            FROM (
                SELECT i.id_usuario,
                       u.nombre,
                       COUNT(*)                         AS depositos_verificados,
                       SUM(d.cantidad_verificada)       AS unidades,
                       ROUND(SUM(d.peso_estimado_kg), 3) AS kg_estimados,
                       ROUND(SUM(d.peso_medido_kg), 3)  AS kg_medidos,
                       DENSE_RANK() OVER (ORDER BY COUNT(*) DESC, SUM(d.cantidad_verificada) DESC) AS puesto
                FROM   deposito d
                JOIN   orden_apertura o ON o.id_orden = d.id_orden
                JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
                JOIN   usuario u        ON u.id_usuario = i.id_usuario
                WHERE  d.estado_verificacion = 1
                AND    d.fecha_deteccion_utc >= a_utc(p_desde_col)
                AND    d.fecha_deteccion_utc <  a_utc(p_hasta_col)
                GROUP  BY i.id_usuario, u.nombre)
            WHERE  puesto <= p_top
            ORDER  BY puesto, id_usuario;
        RETURN v_cursor;
    END ranking_verificados;
END pkg_estadisticas;
/
SHOW ERRORS PACKAGE BODY pkg_estadisticas

-- Procedimiento con parámetro OUT SYS_REFCURSOR (mismo patrón que P3 y que la clase).
CREATE OR REPLACE PROCEDURE pr_consultar_estadisticas (
    p_desde_col IN DATE,
    p_hasta_col IN DATE,
    p_id_punto  IN NUMBER,
    p_categoria IN NUMBER,
    p_resultado OUT SYS_REFCURSOR
)
IS
BEGIN
    pkg_estadisticas.consultar_resumen(p_desde_col, p_hasta_col, p_id_punto, p_categoria, p_resultado);
END pr_consultar_estadisticas;
/
SHOW ERRORS PROCEDURE pr_consultar_estadisticas

-- Función que devuelve un cursor (como fn_empleados de la tarea HR).
CREATE OR REPLACE FUNCTION fn_depositos_ciudadano (
    p_id_usuario IN usuario.id_usuario%TYPE
) RETURN SYS_REFCURSOR
IS
    v_cursor SYS_REFCURSOR;
BEGIN
    OPEN v_cursor FOR
        SELECT id_deposito, fecha_colombia, color, material_declarado, material_verificado,
               estado_texto, evidencia_fisica, peso_estimado_g, peso_medido_g
        FROM   vw_deposito_detalle
        WHERE  id_titular = p_id_usuario
        ORDER  BY fecha_colombia DESC;
    RETURN v_cursor;
END fn_depositos_ciudadano;
/
SHOW ERRORS FUNCTION fn_depositos_ciudadano

PROMPT 07: objetos PL/SQL compilados. Revisa que arriba diga «No errors» en cada uno.
