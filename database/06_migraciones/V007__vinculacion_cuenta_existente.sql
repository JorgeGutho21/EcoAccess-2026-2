-- =====================================================================
-- EcoAccess P3 — migración V007: vinculación de cuentas existentes (P-15)
-- Archivo: database/06_migraciones/V007__vinculacion_cuenta_existente.sql
--
-- Solo AGREGA la tabla VINCULACION_PENDIENTE; no modifica ni borra tablas o filas existentes.
-- Ejecutar como dueño del esquema después de V006. Se niega a ejecutarse dos veces.
--
-- Flujo: la página HTTPS autenticada prepara una fila por token de enlace con el HASH de un código de un solo
-- uso; el bot la confirma cuando el mismo Telegram escribe el código. Al confirmar se crea el vínculo, se marca
-- el token como usado y la fila se borra. El código en claro nunca se guarda.
--
-- Reversión documentada (solo si no se usa todavía): DROP TABLE vinculacion_pendiente PURGE;
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_cantidad NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_cantidad FROM user_tables WHERE table_name = 'VINCULACION_PENDIENTE';
    IF v_cantidad > 0 THEN
        RAISE_APPLICATION_ERROR(-20071, 'V007 ya estaba aplicada. No se modificó nada.');
    END IF;

    SELECT COUNT(*) INTO v_cantidad FROM user_tables WHERE table_name IN ('TOKEN_VINCULACION', 'VINCULO_TELEGRAM', 'USUARIO');
    IF v_cantidad < 3 THEN
        RAISE_APPLICATION_ERROR(-20072, 'V007 requiere V004 aplicada en este esquema. No se modificó nada.');
    END IF;
END;
/

CREATE TABLE vinculacion_pendiente (
    hash_token             VARCHAR2(64 CHAR)  NOT NULL,
    telegram_user_id       NUMBER(19)         NOT NULL,
    id_usuario             NUMBER(10)         NOT NULL,
    hash_codigo            VARCHAR2(64 CHAR)  NOT NULL,
    fecha_creacion_utc     TIMESTAMP          NOT NULL,
    fecha_vencimiento_utc  TIMESTAMP          NOT NULL,
    intentos               NUMBER(2)          DEFAULT 0 NOT NULL,
    CONSTRAINT pk_vinculacion_pendiente PRIMARY KEY (hash_token),
    CONSTRAINT fk_vinc_pend_token       FOREIGN KEY (hash_token) REFERENCES token_vinculacion (hash_token),
    CONSTRAINT fk_vinc_pend_usuario     FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    CONSTRAINT ck_vinc_pend_codigo      CHECK (REGEXP_LIKE(hash_codigo, '^[0-9a-f]{64}$')),
    CONSTRAINT ck_vinc_pend_telegram    CHECK (telegram_user_id > 0),
    CONSTRAINT ck_vinc_pend_vence       CHECK (fecha_vencimiento_utc > fecha_creacion_utc),
    CONSTRAINT ck_vinc_pend_intentos    CHECK (intentos BETWEEN 0 AND 5)
);

-- El bot busca por Telegram y vigencia; la FK a usuario necesita su índice para no bloquear USUARIO.
CREATE INDEX ix_vinc_pend_telegram ON vinculacion_pendiente (telegram_user_id, fecha_vencimiento_utc);
CREATE INDEX ix_vinc_pend_usuario  ON vinculacion_pendiente (id_usuario);

COMMENT ON TABLE vinculacion_pendiente IS 'Vinculación de cuenta existente en espera del código escrito en Telegram (P-15)';
COMMENT ON COLUMN vinculacion_pendiente.hash_codigo IS 'SHA-256 de hash_token:código; el código en claro no se guarda';

BEGIN
    DBMS_OUTPUT.PUT_LINE('Migración V007 aplicada: tabla VINCULACION_PENDIENTE creada.');
END;
/

WHENEVER SQLERROR CONTINUE
