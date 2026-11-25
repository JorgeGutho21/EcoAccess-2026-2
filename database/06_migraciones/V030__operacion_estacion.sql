-- Operaciones de la estación de tres canecas (Modo ExpoSoftware). Aditiva: no modifica interacciones, depósitos,
-- órdenes, ecopuntos ni usuarios.
-- Motivo: las aperturas de una, dos o tres canecas desde el PC, Telegram y la web de control (reconocimiento automático,
-- apertura manual y pruebas técnicas) solo quedaban en la memoria del proceso. Esta tabla las conserva en Oracle Cloud,
-- con una fila por operación, para que el PC, Telegram y los equipos de los integrantes consulten el mismo historial.
-- Reemplaza la propuesta database/migrations/V028_catalogo_35_prioritarios_y_multirresiduo.sql (nunca aplicada en
-- producción): REGISTRO_MULTIRRESIDUO no tenía usuario, canal, tiempo de apertura ni estado del hardware.
--
-- Una operación NO es un depósito ni otorga ecopuntos: las tapas no tienen sensores. DISPOSICION_DECLARADA = 1 solo si
-- la persona confirma que depositó; DISPOSICION_VERIFICADA queda en 0 salvo evidencia física adicional.
--
-- Qué crea (y nada más):
--   tabla  OPERACION_ESTACION (PK, UNIQUE de clave de idempotencia, 1 FK con ON DELETE SET NULL, 9 CHECK)
--   índices IX_OPERACION_ESTACION_FECHA, IX_OPERACION_ESTACION_USUARIO
--   vista  EA_R_OPERACIONES_ESTACION (agregado diario por canal, tipo y combinación, sin datos personales)
-- No ejecuta INSERT, UPDATE, DELETE, ALTER ni DROP sobre objetos existentes, ni GRANT (los permisos de los roles se
-- conceden después con database/00_administracion/05_conceder_permisos_roles.sql, que ya la trata como solo lectura).
--
-- Repetible: si la tabla o un índice ya existen, no se vuelven a crear; la vista se reemplaza por la misma definición.
-- Si existe una OPERACION_ESTACION con otras columnas, se detiene sin tocarla (-20303).
-- Reversión: database/06_migraciones/reversion/R030__operacion_estacion.sql
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
            DBMS_OUTPUT.PUT_LINE('V030: creado ' || p_tipo || ' ' || p_nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('V030: ya existía ' || p_tipo || ' ' || p_nombre || '; no se modificó');
        END IF;
    END;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'USUARIO';
    IF n <> 1 THEN RAISE_APPLICATION_ERROR(-20301, 'V030 requiere la tabla usuario.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'OPERACION_ESTACION';
    IF n = 1 THEN
        SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name = 'OPERACION_ESTACION'
          AND column_name IN ('ID_OPERACION','CLAVE','FECHA_UTC','CANAL','TIPO','ID_USUARIO','SOLICITANTE','ESTACION','MATERIALES',
                              'CATEGORIAS','CANECAS','CANTIDAD_RESIDUOS','DURACION_MS','MODALIDAD','RESULTADO_RECONOCIMIENTO',
                              'ESTADO_HARDWARE','ESTADO_OPERACION','DISPOSICION_DECLARADA','DISPOSICION_VERIFICADA','MODELO_VISION',
                              'LATENCIA_MS','SINCRONIZADA_OFFLINE','DETALLE','FECHA_ACTUALIZACION_UTC');
        IF n <> 24 THEN RAISE_APPLICATION_ERROR(-20303, 'Existe OPERACION_ESTACION con otra estructura; V030 no la modifica.'); END IF;
    END IF;
    crear_si_falta('TABLE', 'OPERACION_ESTACION', q'[
        CREATE TABLE operacion_estacion (
            id_operacion NUMBER(10) GENERATED ALWAYS AS IDENTITY,
            clave VARCHAR2(100 CHAR) NOT NULL,
            fecha_utc TIMESTAMP NOT NULL,
            canal VARCHAR2(10 CHAR) NOT NULL,
            tipo VARCHAR2(30 CHAR) NOT NULL,
            id_usuario NUMBER(10),
            solicitante VARCHAR2(60 CHAR),
            estacion VARCHAR2(60 CHAR) NOT NULL,
            materiales VARCHAR2(400 CHAR),
            categorias VARCHAR2(80 CHAR),
            canecas VARCHAR2(30 CHAR),
            cantidad_residuos NUMBER(3),
            duracion_ms NUMBER(6) NOT NULL,
            modalidad VARCHAR2(12 CHAR) NOT NULL,
            resultado_reconocimiento VARCHAR2(30 CHAR),
            estado_hardware VARCHAR2(30 CHAR) NOT NULL,
            estado_operacion VARCHAR2(30 CHAR) NOT NULL,
            disposicion_declarada NUMBER(1) DEFAULT 0 NOT NULL,
            disposicion_verificada NUMBER(1) DEFAULT 0 NOT NULL,
            modelo_vision VARCHAR2(60 CHAR),
            latencia_ms NUMBER(7),
            sincronizada_offline NUMBER(1) DEFAULT 0 NOT NULL,
            detalle VARCHAR2(300 CHAR),
            fecha_actualizacion_utc TIMESTAMP NOT NULL,
            CONSTRAINT pk_operacion_estacion PRIMARY KEY (id_operacion),
            CONSTRAINT uq_operacion_estacion_clave UNIQUE (clave),
            CONSTRAINT fk_operacion_estacion_usuario FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE SET NULL,
            CONSTRAINT ck_opest_canal CHECK (canal IN ('PC', 'TELEGRAM', 'WEB')),
            CONSTRAINT ck_opest_tipo CHECK (tipo IN ('PRUEBA_TECNICA', 'APERTURA_MANUAL', 'RECONOCIMIENTO_AUTOMATICO', 'DISPOSICION_DECLARADA')),
            CONSTRAINT ck_opest_modalidad CHECK (modalidad IN ('SIMULTANEA', 'SECUENCIAL', 'NINGUNA')),
            CONSTRAINT ck_opest_estado CHECK (estado_operacion IN ('RECIBIDA', 'CLASIFICADA', 'PENDIENTE_APERTURA', 'EN_EJECUCION',
                'CICLO_COMPLETADO', 'DISPOSICION_CONFIRMADA', 'REGISTRADA', 'ERROR', 'PENDIENTE_SINCRONIZACION')),
            CONSTRAINT ck_opest_hardware CHECK (estado_hardware IN ('PWM_CONFIRMADO', 'SIMULADO', 'RECHAZADO', 'SIN_RESPUESTA', 'NO_SOLICITADO', 'DUPLICADA_BLOQUEADA')),
            CONSTRAINT ck_opest_flags CHECK (disposicion_declarada IN (0, 1) AND disposicion_verificada IN (0, 1) AND sincronizada_offline IN (0, 1)),
            CONSTRAINT ck_opest_duracion CHECK (duracion_ms BETWEEN 0 AND 60000),
            CONSTRAINT ck_opest_cantidad CHECK (cantidad_residuos IS NULL OR cantidad_residuos BETWEEN 0 AND 99),
            CONSTRAINT ck_opest_latencia CHECK (latencia_ms IS NULL OR latencia_ms >= 0)
        )]');
    crear_si_falta('INDEX', 'IX_OPERACION_ESTACION_FECHA', 'CREATE INDEX ix_operacion_estacion_fecha ON operacion_estacion(fecha_utc)');
    crear_si_falta('INDEX', 'IX_OPERACION_ESTACION_USUARIO', 'CREATE INDEX ix_operacion_estacion_usuario ON operacion_estacion(id_usuario, fecha_utc)');
END;
/
-- Agregado diario sin datos personales para Power BI y el tablero.
CREATE OR REPLACE VIEW ea_r_operaciones_estacion AS
SELECT CAST(TRUNC(CAST(FROM_TZ(o.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS DATE) AS dia,
       o.canal, o.tipo, o.canecas, o.modalidad, o.estado_hardware, o.estado_operacion,
       COUNT(*) AS operaciones,
       SUM(o.disposicion_declarada) AS disposiciones_declaradas,
       ROUND(AVG(o.duracion_ms)) AS duracion_media_ms,
       ROUND(AVG(o.latencia_ms)) AS latencia_media_ms
FROM operacion_estacion o
GROUP BY CAST(TRUNC(CAST(FROM_TZ(o.fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS DATE),
         o.canal, o.tipo, o.canecas, o.modalidad, o.estado_hardware, o.estado_operacion;
PROMPT V030_OPERACION_ESTACION_APLICADA
