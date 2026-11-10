-- Consultas de fotos atendidas por Echo. Aditiva: no modifica interacciones, depósitos, análisis ni usuarios.
-- Motivo: en la fase de software (sin dispositivo) Echo informa el destino sin crear una solicitud, así que la
-- clasificación no dejaba rastro en Oracle y ningún tablero podía contarla. Una consulta NO es un depósito.
-- No se guarda la foto ni el identificador de Telegram: solo el resultado, el modelo, la latencia y, si la persona
-- está vinculada, su usuario (para sus propias estadísticas).
-- Numeración: V021 y V022 ya están aplicadas en la base compartida desde otra copia del proyecto; esta es V023.
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name IN ('USUARIO','MATERIAL','CONTENEDOR');
    IF n<>3 THEN RAISE_APPLICATION_ERROR(-20231,'V023 requiere usuario, material y contenedor.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name='CONSULTA_VISION';
    IF n<>0 THEN RAISE_APPLICATION_ERROR(-20232,'V023 ya aplicada; no se modificó nada.'); END IF;
END;
/
CREATE TABLE consulta_vision (
    id_consulta NUMBER(10) GENERATED ALWAYS AS IDENTITY,
    clave VARCHAR2(80 CHAR) NOT NULL,
    id_usuario NUMBER(10),
    canal NUMBER(2) NOT NULL,
    modo NUMBER(2) NOT NULL,
    proveedor VARCHAR2(80 CHAR) NOT NULL,
    clave_objeto VARCHAR2(40 CHAR),
    contexto NUMBER(2),
    resultado NUMBER(2) NOT NULL,
    id_material NUMBER(10),
    admision NUMBER(2),
    id_contenedor NUMBER(10),
    latencia_ms NUMBER(7) NOT NULL,
    fecha_utc TIMESTAMP NOT NULL,
    CONSTRAINT pk_consulta_vision PRIMARY KEY (id_consulta),
    CONSTRAINT uq_consulta_vision_clave UNIQUE (clave),
    CONSTRAINT fk_consulta_vision_usuario FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario),
    CONSTRAINT fk_consulta_vision_material FOREIGN KEY (id_material) REFERENCES material(id_material),
    CONSTRAINT fk_consulta_vision_contenedor FOREIGN KEY (id_contenedor) REFERENCES contenedor(id_contenedor),
    CONSTRAINT ck_consulta_vision_canal CHECK (canal BETWEEN 1 AND 3),
    CONSTRAINT ck_consulta_vision_modo CHECK (modo IN (1, 2)),
    CONSTRAINT ck_consulta_vision_contexto CHECK (contexto IS NULL OR contexto BETWEEN 0 AND 10),
    CONSTRAINT ck_consulta_vision_resultado CHECK (resultado BETWEEN 1 AND 10),
    CONSTRAINT ck_consulta_vision_admision CHECK (admision IS NULL OR admision BETWEEN 1 AND 4),
    CONSTRAINT ck_consulta_vision_latencia CHECK (latencia_ms >= 0)
);
CREATE INDEX ix_consulta_vision_fecha ON consulta_vision(fecha_utc);
CREATE INDEX ix_consulta_vision_usuario ON consulta_vision(id_usuario, fecha_utc);
-- Agregado diario sin datos personales para Power BI y el tablero: operaciones reales, separadas de las evaluaciones.
CREATE OR REPLACE VIEW ea_r_consultas_vision AS
SELECT CAST(TRUNC(CAST(FROM_TZ(c.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS DATE) AS dia,
       c.canal, c.modo, c.proveedor, c.contexto, c.resultado, c.admision,
       m.codigo AS material,
       COUNT(*) AS consultas,
       COUNT(DISTINCT c.id_usuario) AS ciudadanos,
       ROUND(AVG(c.latencia_ms)) AS latencia_media_ms,
       MAX(c.latencia_ms) AS latencia_maxima_ms
FROM consulta_vision c
LEFT JOIN material m ON m.id_material = c.id_material
GROUP BY CAST(TRUNC(CAST(FROM_TZ(c.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS DATE),
         c.canal, c.modo, c.proveedor, c.contexto, c.resultado, c.admision, m.codigo;
PROMPT V023_CONSULTA_VISION_APLICADA
