-- =====================================================================
-- EcoAccess P3 — migración V003: interacciones, órdenes de apertura,
-- eventos del dispositivo y depósitos
-- Archivo: database/06_migraciones/V003__interacciones_depositos.sql
--
-- Crea cuatro tablas:
--   INTERACCION        una visita a una caneca (Telegram, apertura manual o
--                      simulador), con la sugerencia de la IA y el material
--                      que la persona confirma.
--   ORDEN_APERTURA     la autorización de abrir: clave de idempotencia,
--                      vencimiento y estado (RN-07). Como máximo una vigente
--                      por caneca (RN-16) y una por interacción.
--   EVENTO_DISPOSITIVO lo que informa la caneca (acuse, apertura, detección,
--                      cierre, fallo). Un id repetido no se guarda dos veces.
--   DEPOSITO           existe solo si el sensor detectó un objeto dentro de
--                      la ventana de una orden vigente (RN-08). Uno por orden
--                      y por interacción (RN-17). Peso nulo si no se midió
--                      (RN-12). Sin ecopuntos todavía.
-- La clasificación sugerida es un par de columnas de INTERACCION y los pasos
-- de la orden son valores de su estado, no tablas.
-- No cambia ninguna tabla de las versiones 001 y 002.
--
-- Ejecutar conectado como dueño del esquema, después de V002.
--   * Sin USUARIO, CONTENEDOR o MATERIAL -> se detiene (ORA-20051).
--   * Ya aplicada (existe INTERACCION)    -> se detiene sin tocar nada (ORA-20050).
--   * Aplicada a medias (alguna tabla)    -> se detiene (ORA-20052); ver reversión.
--
-- Reversión (solo en desarrollo; borra el historial de visitas y depósitos):
--   DROP TABLE deposito;
--   DROP TABLE evento_dispositivo;
--   DROP TABLE orden_apertura;
--   DROP TABLE interaccion;
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_requeridas  NUMBER;
    v_nuevas      NUMBER;
    v_interaccion NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_requeridas FROM user_tables
     WHERE table_name IN ('USUARIO', 'CONTENEDOR', 'MATERIAL');
    SELECT COUNT(*) INTO v_interaccion FROM user_tables WHERE table_name = 'INTERACCION';
    SELECT COUNT(*) INTO v_nuevas FROM user_tables
     WHERE table_name IN ('INTERACCION', 'ORDEN_APERTURA', 'EVENTO_DISPOSITIVO', 'DEPOSITO');

    IF v_requeridas < 3 THEN
        RAISE_APPLICATION_ERROR(-20051,
            'Faltan USUARIO, CONTENEDOR o MATERIAL: ejecuta antes database/02_tablas/01_crear_tablas_iniciales.sql.');
    ELSIF v_interaccion = 1 THEN
        RAISE_APPLICATION_ERROR(-20050,
            'La migración V003 ya estaba aplicada (existe INTERACCION). No se modificó nada.');
    ELSIF v_nuevas > 0 THEN
        RAISE_APPLICATION_ERROR(-20052,
            'V003 quedó aplicada a medias: borra las tablas que creó (ver reversión en la cabecera) y repite.');
    END IF;
END;
/

-- Canal (CanalInteraccion): 1 Telegram, 2 Manual (CU-17), 3 Simulador.
-- Estado (EstadoInteraccion): 1 Iniciada, 2 Autorizada, 3 ConDeposito,
-- 4 SinDeposito, 5 Rechazada. Desde 3 en adelante la interacción está cerrada.
-- La apertura manual nace cerrada como SinDeposito, con su operario y motivo,
-- y nunca genera depósito ni puntos automáticos.
CREATE TABLE interaccion (
    id_interaccion          NUMBER(10)          GENERATED ALWAYS AS IDENTITY,
    id_contenedor           NUMBER(10)          NOT NULL,
    id_usuario              NUMBER(10),
    canal                   NUMBER(1)           NOT NULL,
    estado                  NUMBER(1)           DEFAULT 1 NOT NULL,
    id_material_sugerido    NUMBER(10),
    confianza_sugerida      NUMBER(4,3),
    id_material_confirmado  NUMBER(10),
    id_operario             NUMBER(10),
    motivo                  VARCHAR2(300 CHAR),
    fecha_inicio_utc        TIMESTAMP           NOT NULL,
    fecha_cierre_utc        TIMESTAMP,
    CONSTRAINT pk_interaccion                PRIMARY KEY (id_interaccion),
    CONSTRAINT uq_interaccion_contenedor     UNIQUE (id_interaccion, id_contenedor),
    CONSTRAINT fk_interaccion_contenedor     FOREIGN KEY (id_contenedor)          REFERENCES contenedor (id_contenedor),
    CONSTRAINT fk_interaccion_usuario        FOREIGN KEY (id_usuario)             REFERENCES usuario (id_usuario),
    CONSTRAINT fk_interaccion_mat_sugerido   FOREIGN KEY (id_material_sugerido)   REFERENCES material (id_material),
    CONSTRAINT fk_interaccion_mat_confirmado FOREIGN KEY (id_material_confirmado) REFERENCES material (id_material),
    CONSTRAINT fk_interaccion_operario       FOREIGN KEY (id_operario)            REFERENCES usuario (id_usuario),
    CONSTRAINT ck_interaccion_canal          CHECK (canal IN (1, 2, 3)),
    CONSTRAINT ck_interaccion_estado         CHECK (estado IN (1, 2, 3, 4, 5)),
    CONSTRAINT ck_interaccion_confianza      CHECK (confianza_sugerida BETWEEN 0 AND 1),
    CONSTRAINT ck_interaccion_sugerencia     CHECK (
        (id_material_sugerido IS NULL AND confianza_sugerida IS NULL) OR
        (id_material_sugerido IS NOT NULL AND confianza_sugerida IS NOT NULL)),
    CONSTRAINT ck_interaccion_motivo         CHECK (motivo IS NULL OR (TRIM(motivo) IS NOT NULL AND motivo = TRIM(motivo))),
    CONSTRAINT ck_interaccion_cierre         CHECK (
        (estado IN (1, 2) AND fecha_cierre_utc IS NULL) OR
        (estado IN (3, 4, 5) AND fecha_cierre_utc IS NOT NULL AND fecha_cierre_utc >= fecha_inicio_utc)),
    -- Autorizada o con orden ya cerrada: el material confirmado es el que se autorizó.
    CONSTRAINT ck_interaccion_confirmado     CHECK (
        (estado NOT IN (2, 3) AND NOT (estado = 4 AND canal <> 2)) OR id_material_confirmado IS NOT NULL),
    CONSTRAINT ck_interaccion_rechazo        CHECK (estado <> 5 OR motivo IS NOT NULL),
    CONSTRAINT ck_interaccion_manual         CHECK (
        (canal = 2 AND estado = 4 AND id_operario IS NOT NULL AND motivo IS NOT NULL
                   AND id_material_sugerido IS NULL AND id_material_confirmado IS NULL) OR
        (canal <> 2 AND id_operario IS NULL))
);

CREATE INDEX ix_interaccion_contenedor  ON interaccion (id_contenedor, fecha_inicio_utc);
CREATE INDEX ix_interaccion_usuario     ON interaccion (id_usuario);
CREATE INDEX ix_interaccion_operario    ON interaccion (id_operario);
CREATE INDEX ix_interaccion_mat_sug     ON interaccion (id_material_sugerido);
CREATE INDEX ix_interaccion_mat_conf    ON interaccion (id_material_confirmado);

COMMENT ON TABLE  interaccion IS 'Visita a una caneca por un canal; la sugerencia de la IA no es material verificado.';
COMMENT ON COLUMN interaccion.id_usuario             IS 'Ciudadano identificado; NULL para un visitante o una apertura manual.';
COMMENT ON COLUMN interaccion.canal                  IS 'CanalInteraccion: 1 Telegram, 2 Manual, 3 Simulador.';
COMMENT ON COLUMN interaccion.estado                 IS 'EstadoInteraccion: 1 Iniciada, 2 Autorizada, 3 ConDeposito, 4 SinDeposito, 5 Rechazada.';
COMMENT ON COLUMN interaccion.id_material_sugerido   IS 'Material que sugirió la clasificación por imagen; solo orienta.';
COMMENT ON COLUMN interaccion.confianza_sugerida     IS 'Confianza de la sugerencia, de 0 a 1.';
COMMENT ON COLUMN interaccion.id_material_confirmado IS 'Material que la persona confirmó y que se autorizó en la caneca (RN-06).';
COMMENT ON COLUMN interaccion.id_operario            IS 'Operario que hizo la apertura manual (CU-17).';
COMMENT ON COLUMN interaccion.motivo                 IS 'Motivo de la apertura manual o del rechazo.';

-- Estado (EstadoOrdenApertura): 1 Emitida, 2 Enviada, 3 Acusada, 4 Abierta,
-- 5 Detectada (el sensor vio el objeto; falta el cierre), 6 Cerrada,
-- 7 Vencida (sin confirmación a tiempo), 8 Fallida (fallo o estado desconocido).
-- Vigentes: 1 a 5. Terminales: 6 a 8.
CREATE TABLE orden_apertura (
    id_orden               NUMBER(10)          GENERATED ALWAYS AS IDENTITY,
    id_interaccion         NUMBER(10)          NOT NULL,
    id_contenedor          NUMBER(10)          NOT NULL,
    clave_idempotencia     VARCHAR2(64 CHAR)   NOT NULL,
    estado                 NUMBER(1)           DEFAULT 1 NOT NULL,
    fecha_emision_utc      TIMESTAMP           NOT NULL,
    fecha_vencimiento_utc  TIMESTAMP           NOT NULL,
    fecha_cierre_utc       TIMESTAMP,
    CONSTRAINT pk_orden_apertura         PRIMARY KEY (id_orden),
    CONSTRAINT uq_orden_clave            UNIQUE (clave_idempotencia),
    CONSTRAINT uq_orden_interaccion      UNIQUE (id_interaccion),
    CONSTRAINT uq_orden_contenedor       UNIQUE (id_orden, id_contenedor),
    CONSTRAINT fk_orden_interaccion      FOREIGN KEY (id_interaccion, id_contenedor)
                                         REFERENCES interaccion (id_interaccion, id_contenedor),
    CONSTRAINT ck_orden_clave            CHECK (TRIM(clave_idempotencia) IS NOT NULL AND clave_idempotencia = TRIM(clave_idempotencia)
                                                AND LENGTH(clave_idempotencia) >= 8),
    CONSTRAINT ck_orden_estado           CHECK (estado IN (1, 2, 3, 4, 5, 6, 7, 8)),
    CONSTRAINT ck_orden_vencimiento      CHECK (fecha_vencimiento_utc > fecha_emision_utc),
    CONSTRAINT ck_orden_cierre           CHECK (
        (estado BETWEEN 1 AND 5 AND fecha_cierre_utc IS NULL) OR
        (estado BETWEEN 6 AND 8 AND fecha_cierre_utc IS NOT NULL AND fecha_cierre_utc >= fecha_emision_utc))
);

-- RN-16: el índice solo tiene entrada para las órdenes vigentes, así que dos
-- vigentes en la misma caneca chocan con ORA-00001 aunque lleguen a la vez.
CREATE UNIQUE INDEX uq_orden_vigente ON orden_apertura (
    CASE WHEN estado BETWEEN 1 AND 5 THEN id_contenedor END);

CREATE INDEX ix_orden_contenedor ON orden_apertura (id_contenedor, fecha_emision_utc);

COMMENT ON TABLE  orden_apertura IS 'Autorización para abrir una caneca; abrirla no es reciclar (RN-07, RN-14, RN-16).';
COMMENT ON COLUMN orden_apertura.id_contenedor         IS 'Caneca de la interacción; se repite para poder indexar la orden vigente.';
COMMENT ON COLUMN orden_apertura.clave_idempotencia    IS 'Clave del canal: repetir la solicitud con la misma clave devuelve la misma orden.';
COMMENT ON COLUMN orden_apertura.estado                IS 'EstadoOrdenApertura: 1 Emitida, 2 Enviada, 3 Acusada, 4 Abierta, 5 Detectada, 6 Cerrada, 7 Vencida, 8 Fallida.';
COMMENT ON COLUMN orden_apertura.fecha_vencimiento_utc IS 'Después de esta hora la orden no se reenvía ni admite un depósito.';

-- Tipo (TipoEventoDispositivo): 1 Acuse, 2 Apertura, 3 Deteccion, 4 Cierre, 5 Fallo.
-- Resultado (ResultadoEvento): 1 Aplicado, 2 Revision (tardío, repetido o sin
-- orden vigente: no crea depósito, RN-09).
CREATE TABLE evento_dispositivo (
    id_evento              NUMBER(10)          GENERATED ALWAYS AS IDENTITY,
    id_contenedor          NUMBER(10)          NOT NULL,
    id_externo             VARCHAR2(64 CHAR)   NOT NULL,
    tipo                   NUMBER(1)           NOT NULL,
    id_orden               NUMBER(10),
    resultado              NUMBER(1)           NOT NULL,
    fecha_dispositivo_utc  TIMESTAMP,
    fecha_recepcion_utc    TIMESTAMP           NOT NULL,
    CONSTRAINT pk_evento_dispositivo  PRIMARY KEY (id_evento),
    CONSTRAINT uq_evento_externo      UNIQUE (id_contenedor, id_externo),
    CONSTRAINT uq_evento_orden        UNIQUE (id_evento, id_orden),
    CONSTRAINT fk_evento_contenedor   FOREIGN KEY (id_contenedor) REFERENCES contenedor (id_contenedor),
    CONSTRAINT fk_evento_orden        FOREIGN KEY (id_orden, id_contenedor)
                                      REFERENCES orden_apertura (id_orden, id_contenedor),
    CONSTRAINT ck_evento_externo      CHECK (TRIM(id_externo) IS NOT NULL AND id_externo = TRIM(id_externo)),
    CONSTRAINT ck_evento_tipo         CHECK (tipo IN (1, 2, 3, 4, 5)),
    CONSTRAINT ck_evento_resultado    CHECK (resultado IN (1, 2)),
    CONSTRAINT ck_evento_aplicado     CHECK (resultado = 2 OR id_orden IS NOT NULL)
);

CREATE INDEX ix_evento_orden     ON evento_dispositivo (id_orden);
CREATE INDEX ix_evento_resultado ON evento_dispositivo (resultado, fecha_recepcion_utc);

COMMENT ON TABLE  evento_dispositivo IS 'Evento recibido de una caneca ya autenticada por el adaptador (RN-20).';
COMMENT ON COLUMN evento_dispositivo.id_externo            IS 'Id que asigna el dispositivo; el mismo id en la misma caneca es una retransmisión.';
COMMENT ON COLUMN evento_dispositivo.tipo                  IS 'TipoEventoDispositivo: 1 Acuse, 2 Apertura, 3 Deteccion, 4 Cierre, 5 Fallo.';
COMMENT ON COLUMN evento_dispositivo.resultado             IS 'ResultadoEvento: 1 Aplicado a la orden, 2 Revision.';
COMMENT ON COLUMN evento_dispositivo.fecha_dispositivo_utc IS 'Hora según el reloj de la caneca; puede faltar o no ser fiable.';
COMMENT ON COLUMN evento_dispositivo.fecha_recepcion_utc   IS 'Hora UTC del servidor; es la que cuenta para la ventana de la orden.';

-- El depósito apunta al evento de detección y a su orden con una clave
-- compuesta: no se puede ligar a la detección de otra orden. La interacción
-- se obtiene de la orden (una orden por interacción), de modo que RN-17 queda
-- garantizada por uq_deposito_orden.
CREATE TABLE deposito (
    id_deposito             NUMBER(10)    GENERATED ALWAYS AS IDENTITY,
    id_orden                NUMBER(10)    NOT NULL,
    id_evento               NUMBER(10)    NOT NULL,
    id_material_declarado   NUMBER(10)    NOT NULL,
    id_material_verificado  NUMBER(10),
    id_verificador          NUMBER(10),
    fecha_verificacion_utc  TIMESTAMP,
    peso_medido_kg          NUMBER(6,3),
    peso_estimado_kg        NUMBER(6,3),
    fecha_deteccion_utc     TIMESTAMP     NOT NULL,
    CONSTRAINT pk_deposito                PRIMARY KEY (id_deposito),
    CONSTRAINT uq_deposito_orden          UNIQUE (id_orden),
    CONSTRAINT uq_deposito_evento         UNIQUE (id_evento),
    CONSTRAINT fk_deposito_evento         FOREIGN KEY (id_evento, id_orden)
                                          REFERENCES evento_dispositivo (id_evento, id_orden),
    CONSTRAINT fk_deposito_mat_declarado  FOREIGN KEY (id_material_declarado)  REFERENCES material (id_material),
    CONSTRAINT fk_deposito_mat_verificado FOREIGN KEY (id_material_verificado) REFERENCES material (id_material),
    CONSTRAINT fk_deposito_verificador    FOREIGN KEY (id_verificador)         REFERENCES usuario (id_usuario),
    CONSTRAINT ck_deposito_verificacion   CHECK (
        (id_material_verificado IS NULL AND id_verificador IS NULL AND fecha_verificacion_utc IS NULL) OR
        (id_material_verificado IS NOT NULL AND id_verificador IS NOT NULL AND fecha_verificacion_utc IS NOT NULL
         AND fecha_verificacion_utc >= fecha_deteccion_utc)),
    CONSTRAINT ck_deposito_peso_medido    CHECK (peso_medido_kg > 0),
    CONSTRAINT ck_deposito_peso_estimado  CHECK (peso_estimado_kg > 0)
);

CREATE INDEX ix_deposito_mat_declarado  ON deposito (id_material_declarado);
CREATE INDEX ix_deposito_mat_verificado ON deposito (id_material_verificado);
CREATE INDEX ix_deposito_verificador    ON deposito (id_verificador);
CREATE INDEX ix_deposito_fecha          ON deposito (fecha_deteccion_utc);

COMMENT ON TABLE  deposito IS 'Depósito confirmado por el sensor dentro de la ventana de una orden vigente (RN-08, RN-17).';
COMMENT ON COLUMN deposito.id_material_declarado  IS 'Material confirmado por la persona al pedir la apertura; no está verificado.';
COMMENT ON COLUMN deposito.id_material_verificado IS 'Material comprobado por un operario; NULL mientras nadie lo verifique.';
COMMENT ON COLUMN deposito.peso_medido_kg         IS 'Peso medido por una báscula; NULL si no se midió (RN-12).';
COMMENT ON COLUMN deposito.peso_estimado_kg       IS 'Estimación (por imagen u otra fuente); nunca se suma como kilogramos reales.';

BEGIN
    DBMS_OUTPUT.PUT_LINE('Migración V003 aplicada: tablas INTERACCION, ORDEN_APERTURA, EVENTO_DISPOSITIVO y DEPOSITO creadas.');
END;
/

WHENEVER SQLERROR CONTINUE
