-- =====================================================================
-- EcoAccess P3 — migración V004: vinculación y conversaciones de Telegram
-- Archivo: database/06_migraciones/V004__telegram.sql
--
-- Crea cuatro tablas:
--   VINCULO_TELEGRAM       une una cuenta de EcoAccess con una cuenta de
--                          Telegram. Como máximo un vínculo activo por cuenta
--                          de Telegram y uno por usuario; las bajas se
--                          conservan para la trazabilidad (RN-24).
--   TOKEN_VINCULACION      token de un solo uso del enlace de registro. Solo se
--                          guarda su SHA-256; vence a los 10 minutos y, una vez
--                          usado, apunta a la cuenta que creó.
--   CONVERSACION_TELEGRAM  estado del diálogo con cada cuenta de Telegram, con
--                          versión optimista: dos mensajes procesados a la vez
--                          no pisan el estado del otro.
--   UPDATE_TELEGRAM        updates ya procesados: un update repetido (reintento
--                          de Telegram o del polling) no se atiende dos veces.
-- No cambia ninguna tabla de las versiones 001 a 003.
--
-- Ejecutar conectado como dueño del esquema, después de V003.
--   * Sin USUARIO, CONTENEDOR, MATERIAL o INTERACCION -> se detiene (ORA-20061).
--   * Ya aplicada (existe VINCULO_TELEGRAM)          -> se detiene sin tocar nada (ORA-20060).
--   * Aplicada a medias (alguna tabla)               -> se detiene (ORA-20062); ver reversión.
--
-- Reversión (solo en desarrollo; borra los vínculos y el estado del bot):
--   DROP TABLE update_telegram;
--   DROP TABLE conversacion_telegram;
--   DROP TABLE token_vinculacion;
--   DROP TABLE vinculo_telegram;
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_requeridas NUMBER;
    v_nuevas     NUMBER;
    v_vinculo    NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_requeridas FROM user_tables
     WHERE table_name IN ('USUARIO', 'CONTENEDOR', 'MATERIAL', 'INTERACCION');
    SELECT COUNT(*) INTO v_vinculo FROM user_tables WHERE table_name = 'VINCULO_TELEGRAM';
    SELECT COUNT(*) INTO v_nuevas FROM user_tables
     WHERE table_name IN ('VINCULO_TELEGRAM', 'TOKEN_VINCULACION', 'CONVERSACION_TELEGRAM', 'UPDATE_TELEGRAM');

    IF v_requeridas < 4 THEN
        RAISE_APPLICATION_ERROR(-20061,
            'Faltan USUARIO, CONTENEDOR, MATERIAL o INTERACCION: ejecuta antes la migración V003.');
    ELSIF v_vinculo = 1 THEN
        RAISE_APPLICATION_ERROR(-20060,
            'La migración V004 ya estaba aplicada (existe VINCULO_TELEGRAM). No se modificó nada.');
    ELSIF v_nuevas > 0 THEN
        RAISE_APPLICATION_ERROR(-20062,
            'V004 quedó aplicada a medias: borra las tablas que creó (ver reversión en la cabecera) y repite.');
    END IF;
END;
/

-- Un vínculo está activo mientras fecha_baja_utc sea NULL. Los índices únicos
-- funcionales solo indexan las filas activas, así que las bajas se acumulan.
CREATE TABLE vinculo_telegram (
    id_vinculo         NUMBER(10)  GENERATED ALWAYS AS IDENTITY,
    id_usuario         NUMBER(10)  NOT NULL,
    telegram_user_id   NUMBER(19)  NOT NULL,
    fecha_vinculo_utc  TIMESTAMP   NOT NULL,
    fecha_baja_utc     TIMESTAMP,
    CONSTRAINT pk_vinculo_telegram     PRIMARY KEY (id_vinculo),
    CONSTRAINT fk_vinculo_usuario      FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    CONSTRAINT ck_vinculo_telegram_id  CHECK (telegram_user_id > 0),
    CONSTRAINT ck_vinculo_baja         CHECK (fecha_baja_utc IS NULL OR fecha_baja_utc >= fecha_vinculo_utc)
);

CREATE UNIQUE INDEX uq_vinculo_telegram_activo ON vinculo_telegram (
    CASE WHEN fecha_baja_utc IS NULL THEN telegram_user_id END);
CREATE UNIQUE INDEX uq_vinculo_usuario_activo ON vinculo_telegram (
    CASE WHEN fecha_baja_utc IS NULL THEN id_usuario END);
CREATE INDEX ix_vinculo_usuario  ON vinculo_telegram (id_usuario);
CREATE INDEX ix_vinculo_telegram ON vinculo_telegram (telegram_user_id);

COMMENT ON TABLE  vinculo_telegram IS 'Cuenta de EcoAccess unida a una cuenta de Telegram; las bajas se conservan (RN-24).';
COMMENT ON COLUMN vinculo_telegram.telegram_user_id IS 'Id de la cuenta de Telegram (from.id); no prueba por sí solo la identidad de nadie.';
COMMENT ON COLUMN vinculo_telegram.fecha_baja_utc   IS 'Fin del vínculo; NULL mientras está activo.';

-- El token en claro solo viaja en el enlace; aquí queda su SHA-256 en
-- hexadecimal minúsculo. Usado = fecha_uso_utc e id_usuario_registrado juntos.
CREATE TABLE token_vinculacion (
    hash_token             VARCHAR2(64 CHAR)  NOT NULL,
    telegram_user_id       NUMBER(19)         NOT NULL,
    chat_id                NUMBER(19)         NOT NULL,
    fecha_creacion_utc     TIMESTAMP          NOT NULL,
    fecha_vencimiento_utc  TIMESTAMP          NOT NULL,
    fecha_uso_utc          TIMESTAMP,
    id_usuario_registrado  NUMBER(10),
    CONSTRAINT pk_token_vinculacion    PRIMARY KEY (hash_token),
    CONSTRAINT fk_token_usuario        FOREIGN KEY (id_usuario_registrado) REFERENCES usuario (id_usuario),
    CONSTRAINT ck_token_hash           CHECK (REGEXP_LIKE(hash_token, '^[0-9a-f]{64}$')),
    CONSTRAINT ck_token_telegram_id    CHECK (telegram_user_id > 0),
    CONSTRAINT ck_token_chat           CHECK (chat_id <> 0),
    CONSTRAINT ck_token_vencimiento    CHECK (fecha_vencimiento_utc > fecha_creacion_utc),
    CONSTRAINT ck_token_uso            CHECK (
        (fecha_uso_utc IS NULL AND id_usuario_registrado IS NULL) OR
        (fecha_uso_utc IS NOT NULL AND id_usuario_registrado IS NOT NULL
         AND fecha_uso_utc >= fecha_creacion_utc AND fecha_uso_utc < fecha_vencimiento_utc))
);

CREATE INDEX ix_token_telegram ON token_vinculacion (telegram_user_id, fecha_creacion_utc);
CREATE INDEX ix_token_usuario  ON token_vinculacion (id_usuario_registrado);

COMMENT ON TABLE  token_vinculacion IS 'Token de un solo uso del enlace de registro; solo se guarda su hash.';
COMMENT ON COLUMN token_vinculacion.hash_token            IS 'SHA-256 del token en hexadecimal minúsculo.';
COMMENT ON COLUMN token_vinculacion.fecha_uso_utc         IS 'Momento en que se consumió; NULL si sigue sin usar.';
COMMENT ON COLUMN token_vinculacion.id_usuario_registrado IS 'Cuenta creada con este token (trazabilidad).';

-- Estado (EstadoConversacion): 1 SinCaneca, 2 CanecaIdentificada,
-- 3 EsperandoConfirmacion, 4 OrdenEmitida. La coherencia de los ids repite la
-- de la entidad ConversacionTelegram.
CREATE TABLE conversacion_telegram (
    telegram_user_id         NUMBER(19)  NOT NULL,
    chat_id                  NUMBER(19)  NOT NULL,
    estado                   NUMBER(1)   NOT NULL,
    id_contenedor            NUMBER(10),
    id_interaccion           NUMBER(10),
    id_material_propuesto    NUMBER(10),
    fecha_actualizacion_utc  TIMESTAMP   NOT NULL,
    version                  NUMBER(10)  NOT NULL,
    CONSTRAINT pk_conversacion_telegram  PRIMARY KEY (telegram_user_id),
    CONSTRAINT fk_conversacion_contenedor  FOREIGN KEY (id_contenedor)         REFERENCES contenedor (id_contenedor),
    CONSTRAINT fk_conversacion_interaccion FOREIGN KEY (id_interaccion)        REFERENCES interaccion (id_interaccion),
    CONSTRAINT fk_conversacion_material    FOREIGN KEY (id_material_propuesto) REFERENCES material (id_material),
    CONSTRAINT ck_conversacion_telegram  CHECK (telegram_user_id > 0),
    CONSTRAINT ck_conversacion_chat      CHECK (chat_id <> 0),
    CONSTRAINT ck_conversacion_estado    CHECK (estado IN (1, 2, 3, 4)),
    CONSTRAINT ck_conversacion_version   CHECK (version >= 1),
    CONSTRAINT ck_conversacion_ids       CHECK (
        (estado = 1 AND id_contenedor IS NULL AND id_interaccion IS NULL AND id_material_propuesto IS NULL) OR
        (estado = 2 AND id_contenedor IS NOT NULL AND id_material_propuesto IS NULL) OR
        (estado IN (3, 4) AND id_contenedor IS NOT NULL AND id_interaccion IS NOT NULL
                          AND id_material_propuesto IS NOT NULL))
);

CREATE INDEX ix_conversacion_contenedor  ON conversacion_telegram (id_contenedor);
CREATE INDEX ix_conversacion_interaccion ON conversacion_telegram (id_interaccion);
CREATE INDEX ix_conversacion_material    ON conversacion_telegram (id_material_propuesto);

COMMENT ON TABLE  conversacion_telegram IS 'Estado del diálogo del bot con cada cuenta de Telegram, con versión optimista.';
COMMENT ON COLUMN conversacion_telegram.estado  IS 'EstadoConversacion: 1 SinCaneca, 2 CanecaIdentificada, 3 EsperandoConfirmacion, 4 OrdenEmitida.';
COMMENT ON COLUMN conversacion_telegram.version IS 'Sube en cada escritura; una escritura con versión vieja se rechaza.';

-- La llave primaria es el control de duplicados: el segundo INSERT del mismo
-- update_id falla con ORA-00001 y el update se descarta.
CREATE TABLE update_telegram (
    update_id     NUMBER(19)  NOT NULL,
    recibido_utc  TIMESTAMP   NOT NULL,
    CONSTRAINT pk_update_telegram PRIMARY KEY (update_id),
    CONSTRAINT ck_update_id       CHECK (update_id >= 0)
);

CREATE INDEX ix_update_recibido ON update_telegram (recibido_utc);

COMMENT ON TABLE  update_telegram IS 'Updates de Telegram ya procesados; evita atender dos veces un reintento.';
COMMENT ON COLUMN update_telegram.recibido_utc IS 'Hora UTC del servidor; permite purgar los antiguos.';

BEGIN
    DBMS_OUTPUT.PUT_LINE('Migración V004 aplicada: tablas VINCULO_TELEGRAM, TOKEN_VINCULACION, CONVERSACION_TELEGRAM y UPDATE_TELEGRAM creadas.');
END;
/

WHENEVER SQLERROR CONTINUE
