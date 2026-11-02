-- Autorización por cuenta de Telegram para enviar fotos a un servicio externo. Sin imágenes ni datos del residuo.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
CREATE TABLE consentimiento_vision_externa (
    telegram_user_id    NUMBER(20)         NOT NULL,
    version_aviso       VARCHAR2(30 CHAR)  NOT NULL,
    fecha_otorgado_utc  TIMESTAMP          NOT NULL,
    fecha_revocado_utc  TIMESTAMP,
    CONSTRAINT pk_consentimiento_vision PRIMARY KEY (telegram_user_id),
    CONSTRAINT ck_consentimiento_vision_fechas CHECK (fecha_revocado_utc IS NULL OR fecha_revocado_utc >= fecha_otorgado_utc)
);
COMMENT ON TABLE consentimiento_vision_externa IS 'Autorización vigente o revocada para analizar fotos con un servicio externo';
