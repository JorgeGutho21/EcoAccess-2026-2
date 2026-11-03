-- Quejas, sugerencias y problemas contados en el chat. Aditiva: no modifica opiniones, usuarios ni interacciones.
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name='OPINION';
    IF n<>1 THEN RAISE_APPLICATION_ERROR(-20101,'V020 requiere V010.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name='COMENTARIO_CIUDADANO';
    IF n<>0 THEN RAISE_APPLICATION_ERROR(-20102,'V020 ya aplicada; no se modificó nada.'); END IF;
END;
/
CREATE TABLE comentario_ciudadano (
    id_comentario NUMBER(10) GENERATED ALWAYS AS IDENTITY,
    clave VARCHAR2(64 CHAR) NOT NULL,
    id_usuario NUMBER(10),
    participante VARCHAR2(64 CHAR) NOT NULL,
    tipo NUMBER(2) NOT NULL,
    categoria NUMBER(2) NOT NULL,
    estado NUMBER(2) DEFAULT 1 NOT NULL,
    comentario VARCHAR2(500 CHAR),
    id_interaccion NUMBER(10),
    fecha_utc TIMESTAMP NOT NULL,
    CONSTRAINT pk_comentario_ciudadano PRIMARY KEY(id_comentario),
    CONSTRAINT uq_comentario_clave UNIQUE(clave),
    CONSTRAINT fk_comentario_usuario FOREIGN KEY(id_usuario) REFERENCES usuario(id_usuario),
    CONSTRAINT fk_comentario_interaccion FOREIGN KEY(id_interaccion) REFERENCES interaccion(id_interaccion),
    CONSTRAINT ck_comentario_tipo CHECK(tipo BETWEEN 1 AND 5),
    CONSTRAINT ck_comentario_categoria CHECK(categoria BETWEEN 1 AND 6),
    CONSTRAINT ck_comentario_estado CHECK(estado BETWEEN 1 AND 3),
    CONSTRAINT ck_comentario_participante CHECK(REGEXP_LIKE(participante,'^[0-9a-f]{64}$','c'))
);
CREATE INDEX ix_comentario_fecha ON comentario_ciudadano(fecha_utc);
CREATE INDEX ix_comentario_estado ON comentario_ciudadano(estado,fecha_utc);
PROMPT V020_COMENTARIOS_APLICADA
