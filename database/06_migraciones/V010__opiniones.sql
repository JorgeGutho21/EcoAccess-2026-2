-- Valoraciones voluntarias. Aditiva: no modifica usuarios, depósitos ni puntos existentes.
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name='INTERACCION' AND column_name='CANTIDAD_DECLARADA';
    IF n<>1 THEN RAISE_APPLICATION_ERROR(-20101,'V010 requiere V009.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name='OPINION';
    IF n<>0 THEN RAISE_APPLICATION_ERROR(-20102,'V010 ya aplicada; no se modificó nada.'); END IF;
END;
/
CREATE TABLE opinion (
    id_opinion NUMBER(10) GENERATED ALWAYS AS IDENTITY,
    id_usuario NUMBER(10),
    participante VARCHAR2(64 CHAR) NOT NULL,
    estrellas NUMBER(1) NOT NULL,
    comentario VARCHAR2(500 CHAR),
    fecha_utc TIMESTAMP NOT NULL,
    dia_utc GENERATED ALWAYS AS (TRUNC(CAST(fecha_utc AS DATE))) VIRTUAL,
    CONSTRAINT pk_opinion PRIMARY KEY(id_opinion),
    CONSTRAINT fk_opinion_usuario FOREIGN KEY(id_usuario) REFERENCES usuario(id_usuario),
    CONSTRAINT uq_opinion_dia UNIQUE(participante,dia_utc),
    CONSTRAINT ck_opinion_estrellas CHECK(estrellas BETWEEN 1 AND 5),
    CONSTRAINT ck_opinion_participante CHECK(REGEXP_LIKE(participante,'^[0-9a-f]{64}$','c')),
    CONSTRAINT ck_opinion_invitado CHECK(id_usuario IS NOT NULL OR comentario IS NULL)
);
CREATE INDEX ix_opinion_fecha ON opinion(fecha_utc);
PROMPT V010_OPINIONES_APLICADA
