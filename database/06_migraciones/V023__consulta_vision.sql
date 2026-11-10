-- Consultas de fotos atendidas por Echo. Aditiva: no modifica interacciones, depósitos, análisis ni usuarios.
-- Motivo: en la fase de software (sin dispositivo) Echo informa el destino sin crear una solicitud, así que la
-- clasificación no dejaba rastro en Oracle y ningún tablero podía contarla. Una consulta NO es un depósito.
-- No se guarda la foto ni el identificador de Telegram: solo el resultado, el modelo, la latencia y, si la persona
-- está vinculada, su usuario (para sus propias estadísticas).
-- Numeración: V021 y V022 ya están aplicadas en la base compartida desde otra copia del proyecto; esta es V023.
--
-- Qué crea (y nada más):
--   tabla  CONSULTA_VISION (PK, UNIQUE de clave, 3 FK con ON DELETE SET NULL, 6 CHECK)
--   índices IX_CONSULTA_VISION_FECHA, IX_CONSULTA_VISION_USUARIO
--   vista  EA_R_CONSULTAS_VISION (agregado diario, sin datos personales)
-- No ejecuta INSERT, UPDATE, DELETE, ALTER ni DROP sobre objetos existentes, ni GRANT.
-- Las FK usan ON DELETE SET NULL: borrar un usuario, material o caneca (las limpiezas de pruebas de P3 y V2 lo hacen)
-- nunca queda bloqueado por una consulta; la consulta se conserva anónima para los agregados.
--
-- Repetible: si la tabla o un índice ya existen, no se vuelven a crear; la vista se reemplaza por la misma definición.
-- Si existe una CONSULTA_VISION con otras columnas, se detiene sin tocarla (-20233).
-- Reversión: database/06_migraciones/reversion/R023__consulta_vision.sql
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
    PROCEDURE crear_si_falta(p_tipo VARCHAR2, p_nombre VARCHAR2, p_ddl VARCHAR2) IS
        existe NUMBER;
    BEGIN
        SELECT COUNT(*) INTO existe FROM user_objects WHERE object_type = p_tipo AND object_name = p_nombre;
        IF existe = 0 THEN
            EXECUTE IMMEDIATE p_ddl;
            DBMS_OUTPUT.PUT_LINE('V023: creado ' || p_tipo || ' ' || p_nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('V023: ya existía ' || p_tipo || ' ' || p_nombre || '; no se modificó');
        END IF;
    END;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name IN ('USUARIO','MATERIAL','CONTENEDOR');
    IF n <> 3 THEN RAISE_APPLICATION_ERROR(-20231, 'V023 requiere usuario, material y contenedor.'); END IF;
    -- Una tabla con el mismo nombre pero otra forma no es esta migración: no se toca.
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'CONSULTA_VISION';
    IF n = 1 THEN
        SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name = 'CONSULTA_VISION'
          AND column_name IN ('ID_CONSULTA','CLAVE','ID_USUARIO','CANAL','MODO','PROVEEDOR','CLAVE_OBJETO','CONTEXTO',
                              'RESULTADO','ID_MATERIAL','ADMISION','ID_CONTENEDOR','LATENCIA_MS','FECHA_UTC');
        IF n <> 14 THEN RAISE_APPLICATION_ERROR(-20233, 'Existe CONSULTA_VISION con otra estructura; V023 no la modifica.'); END IF;
    END IF;
    crear_si_falta('TABLE', 'CONSULTA_VISION', q'[
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
            CONSTRAINT fk_consulta_vision_usuario FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE SET NULL,
            CONSTRAINT fk_consulta_vision_material FOREIGN KEY (id_material) REFERENCES material(id_material) ON DELETE SET NULL,
            CONSTRAINT fk_consulta_vision_contenedor FOREIGN KEY (id_contenedor) REFERENCES contenedor(id_contenedor) ON DELETE SET NULL,
            CONSTRAINT ck_consulta_vision_canal CHECK (canal BETWEEN 1 AND 3),
            CONSTRAINT ck_consulta_vision_modo CHECK (modo IN (1, 2)),
            CONSTRAINT ck_consulta_vision_contexto CHECK (contexto IS NULL OR contexto BETWEEN 0 AND 10),
            CONSTRAINT ck_consulta_vision_resultado CHECK (resultado BETWEEN 1 AND 10),
            CONSTRAINT ck_consulta_vision_admision CHECK (admision IS NULL OR admision BETWEEN 1 AND 4),
            CONSTRAINT ck_consulta_vision_latencia CHECK (latencia_ms >= 0)
        )]');
    crear_si_falta('INDEX', 'IX_CONSULTA_VISION_FECHA', 'CREATE INDEX ix_consulta_vision_fecha ON consulta_vision(fecha_utc)');
    crear_si_falta('INDEX', 'IX_CONSULTA_VISION_USUARIO', 'CREATE INDEX ix_consulta_vision_usuario ON consulta_vision(id_usuario, fecha_utc)');
END;
/
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
