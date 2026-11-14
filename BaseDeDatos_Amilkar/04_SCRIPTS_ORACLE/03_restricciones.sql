-- =====================================================================
-- EcoAccess BD — 03 · Restricciones (UNIQUE, CHECK, FOREIGN KEY)
-- Ejecutar como ECOACCESS_BD después de 02_tablas.sql (F5).
--
-- Regla de diseño: lo que se puede comprobar MIRANDO SOLO UNA FILA va
-- en un CHECK; lo que relaciona tablas va en una FK (a veces compuesta).
-- Solo lo que ninguna restricción puede expresar va a un disparador
-- (08_triggers.sql). Así no se duplica la misma regla dos veces.
--
-- Truco de los nombres: TRIM(x) IS NOT NULL AND x = TRIM(x) obliga a que
-- el texto no esté vacío ni tenga espacios sobrantes (un CHECK cuyo
-- resultado es «desconocido» se acepta, por eso se exige IS NOT NULL).
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

-- ======================= ROL =======================
ALTER TABLE rol ADD (
    CONSTRAINT uq_rol_codigo       UNIQUE (codigo),
    CONSTRAINT ck_rol_codigo       CHECK (codigo IN (1, 2, 3)),          -- 1 Ciudadano, 2 Operario, 3 Administrador
    CONSTRAINT ck_rol_descripcion  CHECK (TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion))
);

-- ===================== USUARIO =====================
ALTER TABLE usuario ADD (
    CONSTRAINT uq_usuario_correo         UNIQUE (correo_electronico),
    CONSTRAINT ck_usuario_nombre         CHECK (TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2),
    CONSTRAINT ck_usuario_correo_min     CHECK (correo_electronico = LOWER(correo_electronico)),
    CONSTRAINT ck_usuario_correo_forma   CHECK (INSTR(correo_electronico, '@') > 1
                                                AND INSTR(correo_electronico, '.', INSTR(correo_electronico, '@')) > INSTR(correo_electronico, '@') + 1
                                                AND INSTR(correo_electronico, ' ') = 0),
    CONSTRAINT ck_usuario_activo         CHECK (activo IN (0, 1)),
    CONSTRAINT ck_usuario_consentimiento CHECK (
        (version_consentimiento IS NULL AND fecha_consentimiento_utc IS NULL) OR
        (version_consentimiento IS NOT NULL AND fecha_consentimiento_utc IS NOT NULL)),
    CONSTRAINT ck_usuario_sintetico      CHECK (es_sintetico IN (0, 1))
);

-- =================== USUARIO_ROL ===================
ALTER TABLE usuario_rol ADD (
    CONSTRAINT fk_usuario_rol_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    CONSTRAINT fk_usuario_rol_rol     FOREIGN KEY (id_rol)     REFERENCES rol (id_rol)
);

-- ================ CREDENCIAL_ACCESO ================
ALTER TABLE credencial_acceso ADD (
    CONSTRAINT fk_credencial_usuario  FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    -- RAW no tiene largo fijo: 32 bytes = 64 cifras hexadecimales; 16 bytes = 32.
    CONSTRAINT ck_credencial_hash     CHECK (LENGTH(RAWTOHEX(hash_contrasena)) = 64),
    CONSTRAINT ck_credencial_sal      CHECK (LENGTH(RAWTOHEX(sal)) = 32),
    CONSTRAINT ck_credencial_iter     CHECK (iteraciones >= 100000),
    CONSTRAINT ck_credencial_intentos CHECK (intentos_fallidos BETWEEN 0 AND 100)
);

-- ================= VINCULO_TELEGRAM ================
ALTER TABLE vinculo_telegram ADD (
    CONSTRAINT fk_vinculo_usuario     FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    CONSTRAINT ck_vinculo_telegram_id CHECK (telegram_user_id > 0),
    CONSTRAINT ck_vinculo_baja        CHECK (fecha_baja_utc IS NULL OR fecha_baja_utc >= fecha_vinculo_utc)
);

-- ================ PUNTO_INSTALACION ================
ALTER TABLE punto_instalacion ADD (
    CONSTRAINT uq_punto_nombre      UNIQUE (nombre),
    CONSTRAINT uq_punto_qr_estacion UNIQUE (codigo_qr_estacion),        -- varias filas NULL están permitidas
    CONSTRAINT ck_punto_nombre      CHECK (TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2),
    -- Formato del parámetro start de Telegram: 16 a 64 caracteres A-Z a-z 0-9 _ -
    CONSTRAINT ck_punto_qr_estacion CHECK (codigo_qr_estacion IS NULL OR (LENGTH(codigo_qr_estacion) >= 16
        AND TRANSLATE(codigo_qr_estacion, '#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', '#') IS NULL)),
    CONSTRAINT ck_punto_activo      CHECK (activo IN (0, 1)),
    CONSTRAINT ck_punto_sintetico   CHECK (es_sintetico IN (0, 1))
);

-- ==================== CONTENEDOR ===================
ALTER TABLE contenedor ADD (
    CONSTRAINT uq_contenedor_qr          UNIQUE (identificador_qr),
    CONSTRAINT uq_contenedor_dispositivo UNIQUE (identificador_dispositivo),
    -- Claves «candidatas» que usan las FK compuestas de abajo:
    CONSTRAINT uq_contenedor_punto       UNIQUE (id_contenedor, id_punto_instalacion),
    CONSTRAINT uq_contenedor_categoria   UNIQUE (id_contenedor, categoria),
    CONSTRAINT fk_contenedor_punto       FOREIGN KEY (id_punto_instalacion) REFERENCES punto_instalacion (id_punto_instalacion),
    CONSTRAINT ck_contenedor_nombre      CHECK (TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2),
    CONSTRAINT ck_contenedor_categoria   CHECK (categoria IN (1, 2, 3)),   -- 1 BLANCA, 2 VERDE, 3 NEGRA
    CONSTRAINT ck_contenedor_qr          CHECK (identificador_qr IS NULL OR (LENGTH(identificador_qr) >= 16
        AND TRANSLATE(identificador_qr, '#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', '#') IS NULL)),
    CONSTRAINT ck_contenedor_dispositivo CHECK (identificador_dispositivo IS NULL OR (LENGTH(identificador_dispositivo) BETWEEN 3 AND 40
        AND TRANSLATE(identificador_dispositivo, '#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', '#') IS NULL)),
    CONSTRAINT ck_contenedor_estado      CHECK (estado IN (1, 2, 3))      -- 1 En servicio, 2 Fuera de servicio, 3 Dado de baja
);

-- ===================== MATERIAL ====================
ALTER TABLE material ADD (
    CONSTRAINT uq_material_codigo     UNIQUE (codigo),
    CONSTRAINT uq_material_categoria  UNIQUE (id_material, categoria),   -- destino de la FK compuesta
    CONSTRAINT ck_material_codigo     CHECK (LENGTH(codigo) >= 2
        AND INSTR('ABCDEFGHIJKLMNOPQRSTUVWXYZ', SUBSTR(codigo, 1, 1)) > 0
        AND TRANSLATE(codigo, '#ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_', '#') IS NULL),
    CONSTRAINT ck_material_nombre     CHECK (TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2),
    CONSTRAINT ck_material_categoria  CHECK (categoria IN (1, 2, 3, 4)), -- 4 = peligroso: ruta especial, sin caneca
    CONSTRAINT ck_material_habilitado CHECK (habilitado IN (0, 1))
);

-- =============== CONTENEDOR_MATERIAL ===============
-- Dos FK compuestas: (material, categoría) debe existir en MATERIAL y
-- (caneca, categoría) debe existir en CONTENEDOR. Como la categoría es la
-- misma columna, Oracle IMPIDE poner vidrio (blanco) en la caneca verde.
ALTER TABLE contenedor_material ADD (
    CONSTRAINT fk_cont_mat_contenedor FOREIGN KEY (id_contenedor, categoria) REFERENCES contenedor (id_contenedor, categoria),
    CONSTRAINT fk_cont_mat_material   FOREIGN KEY (id_material, categoria)   REFERENCES material (id_material, categoria),
    CONSTRAINT ck_cont_mat_categoria  CHECK (categoria IN (1, 2, 3))
);

-- ================= PESO_REFERENCIAL ================
ALTER TABLE peso_referencial ADD (
    CONSTRAINT uq_peso_objeto_estado UNIQUE (clave_objeto, estado_visual),  -- un peso por objeto y estado
    CONSTRAINT fk_peso_material     FOREIGN KEY (id_material) REFERENCES material (id_material),
    CONSTRAINT ck_peso_clave         CHECK (LENGTH(clave_objeto) >= 2
        AND TRANSLATE(clave_objeto, '#ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_', '#') IS NULL),
    CONSTRAINT ck_peso_descripcion   CHECK (TRIM(descripcion_objeto) IS NOT NULL AND descripcion_objeto = TRIM(descripcion_objeto)),
    CONSTRAINT ck_peso_estado_visual CHECK (estado_visual IN (0, 1, 2, 3)), -- 0 no aplica, 1 vacío, 2 parcial, 3 lleno
    CONSTRAINT ck_peso_orden         CHECK (gramos_minimo > 0 AND gramos_minimo <= gramos_tipico AND gramos_tipico <= gramos_maximo),
    CONSTRAINT ck_peso_fuente        CHECK (TRIM(fuente) IS NOT NULL AND fuente = TRIM(fuente))
);

-- =================== INTERACCION ===================
ALTER TABLE interaccion ADD (
    CONSTRAINT uq_interaccion_contenedor   UNIQUE (id_interaccion, id_contenedor),  -- destino de la FK de ORDEN
    CONSTRAINT fk_interaccion_punto        FOREIGN KEY (id_punto_instalacion) REFERENCES punto_instalacion (id_punto_instalacion),
    -- La caneca, si ya se conoce, tiene que ser de ESA estación:
    CONSTRAINT fk_interaccion_contenedor   FOREIGN KEY (id_contenedor, id_punto_instalacion)
                                           REFERENCES contenedor (id_contenedor, id_punto_instalacion),
    CONSTRAINT fk_interaccion_usuario      FOREIGN KEY (id_usuario)             REFERENCES usuario (id_usuario),
    CONSTRAINT fk_interaccion_mat_sug      FOREIGN KEY (id_material_sugerido)   REFERENCES material (id_material),
    CONSTRAINT fk_interaccion_mat_conf     FOREIGN KEY (id_material_confirmado) REFERENCES material (id_material),
    CONSTRAINT fk_interaccion_operario     FOREIGN KEY (id_operario)            REFERENCES usuario (id_usuario),
    CONSTRAINT ck_interaccion_canal        CHECK (canal IN (1, 2, 3, 4, 5)),  -- Telegram, Manual, Simulador, TelegramSimulado, Asistido
    CONSTRAINT ck_interaccion_estado       CHECK (estado IN (1, 2, 3, 4, 5)), -- Iniciada, Autorizada, ConDeposito, SinDeposito, Rechazada
    CONSTRAINT ck_interaccion_confianza    CHECK (confianza_sugerida BETWEEN 0 AND 1),
    CONSTRAINT ck_interaccion_sugerencia   CHECK ((id_material_sugerido IS NULL AND confianza_sugerida IS NULL)
                                               OR (id_material_sugerido IS NOT NULL AND confianza_sugerida IS NOT NULL)),
    CONSTRAINT ck_interaccion_cantidad     CHECK (cantidad_declarada IS NULL OR cantidad_declarada BETWEEN 1 AND 1000),
    CONSTRAINT ck_interaccion_motivo       CHECK (motivo IS NULL OR (TRIM(motivo) IS NOT NULL AND motivo = TRIM(motivo))),
    CONSTRAINT ck_interaccion_cierre       CHECK (
        (estado IN (1, 2) AND fecha_cierre_utc IS NULL) OR
        (estado IN (3, 4, 5) AND fecha_cierre_utc IS NOT NULL AND fecha_cierre_utc >= fecha_inicio_utc)),
    -- Autorizada o con depósito: ya se sabe material y caneca.
    CONSTRAINT ck_interaccion_autorizada   CHECK (estado NOT IN (2, 3)
                                               OR (id_material_confirmado IS NOT NULL AND id_contenedor IS NOT NULL)),
    CONSTRAINT ck_interaccion_rechazo      CHECK (estado <> 5 OR motivo IS NOT NULL),
    -- Apertura manual del operario: sin ciudadano ni IA, siempre con motivo.
    CONSTRAINT ck_interaccion_manual       CHECK (
        (canal = 2 AND id_operario IS NOT NULL AND motivo IS NOT NULL AND id_material_sugerido IS NULL) OR
        (canal <> 2 AND id_operario IS NULL)),
    CONSTRAINT ck_interaccion_sintetico    CHECK (es_sintetico IN (0, 1))
);

-- ================= CONSULTA_VISION =================
ALTER TABLE consulta_vision ADD (
    CONSTRAINT uq_consulta_clave        UNIQUE (clave),
    CONSTRAINT fk_consulta_usuario      FOREIGN KEY (id_usuario)             REFERENCES usuario (id_usuario),
    CONSTRAINT fk_consulta_interaccion  FOREIGN KEY (id_interaccion)         REFERENCES interaccion (id_interaccion),
    CONSTRAINT fk_consulta_material     FOREIGN KEY (id_material)            REFERENCES material (id_material),
    CONSTRAINT fk_consulta_peso         FOREIGN KEY (id_peso_referencial)    REFERENCES peso_referencial (id_peso_referencial),
    CONSTRAINT fk_consulta_contenedor   FOREIGN KEY (id_contenedor_sugerido) REFERENCES contenedor (id_contenedor),
    CONSTRAINT ck_consulta_canal        CHECK (canal IN (1, 2, 3)),          -- Telegram, Escritorio, Evaluación
    CONSTRAINT ck_consulta_modo         CHECK (modo IN (1, 2)),              -- 1 Software (sin ESP32), 2 Con dispositivo
    CONSTRAINT ck_consulta_resultado    CHECK (resultado IN (1, 2, 3, 4)),   -- Identificado, PideOtraFoto, NoEsResiduo, ErrorProveedor
    CONSTRAINT ck_consulta_identificado CHECK (resultado <> 1 OR (clave_objeto IS NOT NULL AND id_material IS NOT NULL)),
    CONSTRAINT ck_consulta_unidades     CHECK (unidades_estimadas IS NULL OR unidades_estimadas BETWEEN 1 AND 1000),
    CONSTRAINT ck_consulta_estado_vis   CHECK (estado_visual IS NULL OR estado_visual IN (0, 1, 2, 3)),
    -- Un peso estimado SIEMPRE dice de qué fila del catálogo salió y cuántas unidades se vieron.
    CONSTRAINT ck_consulta_peso         CHECK (peso_estimado_kg IS NULL
                                            OR (peso_estimado_kg > 0 AND id_peso_referencial IS NOT NULL AND unidades_estimadas IS NOT NULL)),
    CONSTRAINT ck_consulta_admision     CHECK (admision IS NULL OR admision IN (1, 2, 3, 4)), -- SinCaneca, Admitido, NoAdmitido, CanecaNoDisponible
    CONSTRAINT ck_consulta_latencia     CHECK (latencia_ms >= 0),
    CONSTRAINT ck_consulta_proveedor    CHECK (TRIM(proveedor) IS NOT NULL AND proveedor = TRIM(proveedor)),
    CONSTRAINT ck_consulta_sintetico    CHECK (es_sintetico IN (0, 1))
);

-- ================== ORDEN_APERTURA =================
ALTER TABLE orden_apertura ADD (
    CONSTRAINT uq_orden_clave        UNIQUE (clave_idempotencia),
    CONSTRAINT uq_orden_interaccion  UNIQUE (id_interaccion),             -- una orden por interacción
    CONSTRAINT uq_orden_contenedor   UNIQUE (id_orden, id_contenedor),    -- destino de la FK de EVENTO
    -- La orden abre la MISMA caneca que tiene la interacción:
    CONSTRAINT fk_orden_interaccion  FOREIGN KEY (id_interaccion, id_contenedor)
                                     REFERENCES interaccion (id_interaccion, id_contenedor),
    CONSTRAINT fk_orden_contenedor   FOREIGN KEY (id_contenedor) REFERENCES contenedor (id_contenedor),
    CONSTRAINT ck_orden_clave        CHECK (LENGTH(clave_idempotencia) >= 8 AND clave_idempotencia = TRIM(clave_idempotencia)),
    CONSTRAINT ck_orden_estado       CHECK (estado BETWEEN 1 AND 8),      -- 1..5 vigentes; 6 Cerrada, 7 Vencida, 8 Fallida
    CONSTRAINT ck_orden_vencimiento  CHECK (fecha_vencimiento_utc > fecha_emision_utc),
    CONSTRAINT ck_orden_cierre       CHECK (
        (estado BETWEEN 1 AND 5 AND fecha_cierre_utc IS NULL) OR
        (estado BETWEEN 6 AND 8 AND fecha_cierre_utc IS NOT NULL AND fecha_cierre_utc >= fecha_emision_utc))
);

-- ================ EVENTO_DISPOSITIVO ===============
ALTER TABLE evento_dispositivo ADD (
    CONSTRAINT uq_evento_externo     UNIQUE (id_contenedor, id_externo),  -- retransmisión = mismo id
    CONSTRAINT uq_evento_orden       UNIQUE (id_evento, id_orden),        -- destino de la FK de DEPOSITO
    CONSTRAINT fk_evento_contenedor  FOREIGN KEY (id_contenedor) REFERENCES contenedor (id_contenedor),
    CONSTRAINT fk_evento_orden       FOREIGN KEY (id_orden, id_contenedor) REFERENCES orden_apertura (id_orden, id_contenedor),
    CONSTRAINT ck_evento_externo     CHECK (TRIM(id_externo) IS NOT NULL AND id_externo = TRIM(id_externo)),
    CONSTRAINT ck_evento_tipo        CHECK (tipo BETWEEN 1 AND 6),        -- Acuse, Apertura, Detección, Cierre, Fallo, CierreSinSensor
    CONSTRAINT ck_evento_resultado   CHECK (resultado IN (1, 2)),         -- 1 Aplicado, 2 Revisión
    CONSTRAINT ck_evento_aplicado    CHECK (resultado = 2 OR id_orden IS NOT NULL)
);

-- ===================== DEPOSITO ====================
ALTER TABLE deposito ADD (
    CONSTRAINT uq_deposito_orden         UNIQUE (id_orden),              -- un depósito por orden
    CONSTRAINT uq_deposito_evento        UNIQUE (id_evento),
    CONSTRAINT fk_deposito_evento        FOREIGN KEY (id_evento, id_orden) REFERENCES evento_dispositivo (id_evento, id_orden),
    CONSTRAINT fk_deposito_mat_decl      FOREIGN KEY (id_material_declarado)  REFERENCES material (id_material),
    CONSTRAINT fk_deposito_mat_verif     FOREIGN KEY (id_material_verificado) REFERENCES material (id_material),
    CONSTRAINT fk_deposito_verificador   FOREIGN KEY (id_verificador)         REFERENCES usuario (id_usuario),
    CONSTRAINT fk_deposito_peso          FOREIGN KEY (id_peso_referencial)    REFERENCES peso_referencial (id_peso_referencial),
    CONSTRAINT ck_deposito_sin_sensor    CHECK (sin_sensor IN (0, 1)),
    CONSTRAINT ck_deposito_cant_decl     CHECK (cantidad_declarada IS NULL OR cantidad_declarada BETWEEN 1 AND 1000),
    CONSTRAINT ck_deposito_estimado      CHECK (peso_estimado_kg IS NULL OR (peso_estimado_kg > 0 AND id_peso_referencial IS NOT NULL)),
    CONSTRAINT ck_deposito_estado        CHECK (estado_verificacion IN (0, 1, 2)), -- 0 Pendiente, 1 Verificado, 2 Rechazado
    -- Coherencia de la verificación según el estado:
    CONSTRAINT ck_deposito_verificacion  CHECK (
        (estado_verificacion = 0 AND id_material_verificado IS NULL AND cantidad_verificada IS NULL
             AND id_verificador IS NULL AND fecha_verificacion_utc IS NULL AND peso_medido_kg IS NULL)
     OR (estado_verificacion = 1 AND id_material_verificado IS NOT NULL AND id_verificador IS NOT NULL
             AND fecha_verificacion_utc IS NOT NULL AND fecha_verificacion_utc >= fecha_deteccion_utc)
     OR (estado_verificacion = 2 AND id_material_verificado IS NULL AND cantidad_verificada IS NULL
             AND peso_medido_kg IS NULL AND id_verificador IS NOT NULL AND motivo_verificacion IS NOT NULL
             AND fecha_verificacion_utc IS NOT NULL AND fecha_verificacion_utc >= fecha_deteccion_utc)),
    CONSTRAINT ck_deposito_cant_verif    CHECK (cantidad_verificada IS NULL OR cantidad_verificada BETWEEN 1 AND 1000),
    -- Peso MEDIDO: siempre con instrumento y precisión (si no, no es medición).
    -- Los tres IS NOT NULL son obligatorios: Oracle ACEPTA una fila cuando el CHECK da UNKNOWN,
    -- y «precision_medicion_g > 0» con precisión NULL da UNKNOWN, no FALSE. Con ellos cada rama
    -- es TRUE o FALSE: o los tres datos están completos y son válidos, o los tres son NULL.
    CONSTRAINT ck_deposito_medido        CHECK (
        (peso_medido_kg IS NULL AND instrumento_medicion IS NULL AND precision_medicion_g IS NULL) OR
        (peso_medido_kg IS NOT NULL AND instrumento_medicion IS NOT NULL AND precision_medicion_g IS NOT NULL
             AND peso_medido_kg > 0 AND TRIM(instrumento_medicion) IS NOT NULL AND precision_medicion_g > 0)),
    CONSTRAINT ck_deposito_motivo        CHECK (motivo_verificacion IS NULL
                                             OR (TRIM(motivo_verificacion) IS NOT NULL AND motivo_verificacion = TRIM(motivo_verificacion)))
);

-- ================= REGLA_ECOPUNTOS =================
ALTER TABLE regla_ecopuntos ADD (
    CONSTRAINT fk_regla_material    FOREIGN KEY (id_material) REFERENCES material (id_material),
    CONSTRAINT fk_regla_creador     FOREIGN KEY (id_creador)  REFERENCES usuario (id_usuario),
    CONSTRAINT ck_regla_puntos      CHECK (puntos BETWEEN 1 AND 100000),
    CONSTRAINT ck_regla_por_unidad  CHECK (por_unidad IN (0, 1)),
    CONSTRAINT ck_regla_descripcion CHECK (TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion)),
    CONSTRAINT ck_regla_intervalo   CHECK (hasta_utc IS NULL OR hasta_utc > desde_utc),
    CONSTRAINT ck_regla_sintetica   CHECK (es_sintetica IN (0, 1))
);

-- =============== MOVIMIENTO_ECOPUNTOS ==============
ALTER TABLE movimiento_ecopuntos ADD (
    CONSTRAINT uq_mov_clave        UNIQUE (clave_idempotencia),           -- repetir la operación no duplica puntos
    CONSTRAINT uq_mov_revertido    UNIQUE (id_movimiento_revertido),      -- un crédito se revierte una sola vez
    CONSTRAINT fk_mov_deposito     FOREIGN KEY (id_deposito)  REFERENCES deposito (id_deposito),
    CONSTRAINT fk_mov_usuario      FOREIGN KEY (id_usuario)   REFERENCES usuario (id_usuario),
    CONSTRAINT fk_mov_regla        FOREIGN KEY (id_regla)     REFERENCES regla_ecopuntos (id_regla),
    CONSTRAINT fk_mov_actor        FOREIGN KEY (id_actor)     REFERENCES usuario (id_usuario),
    CONSTRAINT fk_mov_revertido    FOREIGN KEY (id_movimiento_revertido) REFERENCES movimiento_ecopuntos (id_movimiento),
    CONSTRAINT ck_mov_tipo         CHECK (
        (tipo = 1 AND puntos > 0 AND id_movimiento_revertido IS NULL) OR   -- 1 Crédito
        (tipo = 2 AND puntos < 0 AND id_movimiento_revertido IS NOT NULL)), -- 2 Reverso
    CONSTRAINT ck_mov_motivo       CHECK (TRIM(motivo) IS NOT NULL AND motivo = TRIM(motivo))
);

-- ================ AUDITORIA_DEPOSITO ===============
ALTER TABLE auditoria_deposito ADD (
    CONSTRAINT fk_aud_dep_deposito  FOREIGN KEY (id_deposito) REFERENCES deposito (id_deposito),
    CONSTRAINT ck_aud_dep_operacion CHECK (operacion IN ('VERIFICACION', 'RECHAZO', 'CORRECCION', 'REAPERTURA')),
    CONSTRAINT ck_aud_dep_estados   CHECK (estado_anterior IN (0, 1, 2) AND estado_nuevo IN (0, 1, 2))
);

-- =========== HISTORIAL_ESTADO_CONTENEDOR ===========
ALTER TABLE historial_estado_contenedor ADD (
    CONSTRAINT fk_hist_contenedor FOREIGN KEY (id_contenedor) REFERENCES contenedor (id_contenedor),
    CONSTRAINT fk_hist_actor      FOREIGN KEY (id_actor)      REFERENCES usuario (id_usuario),
    CONSTRAINT ck_hist_estados    CHECK (estado_anterior IN (1, 2, 3) AND estado_nuevo IN (1, 2, 3)
                                         AND estado_anterior <> estado_nuevo)
);

BEGIN
    DBMS_OUTPUT.PUT_LINE('03_restricciones: aplicadas. Sigue con 04_indices.sql.');
END;
/
WHENEVER SQLERROR CONTINUE
