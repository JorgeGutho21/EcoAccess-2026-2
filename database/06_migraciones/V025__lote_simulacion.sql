-- =====================================================================
-- EcoAccess P3 — migración V025: registro de lotes de SIMULACIÓN y vistas de estado
-- Archivo: database/06_migraciones/V025__lote_simulacion.sql
--
-- Fases 7 y 8 de la misión de cierre. Aditiva: crea dos tablas y dos vistas; no
-- modifica ni borra filas existentes.
--
--   * LOTE_SIMULACION: cada lote experimental (id 'SIM-…', semilla, conteos).
--   * REGISTRO_SIMULACION: qué filas creó cada lote (usuario, punto, caneca,
--     interacción, depósito) con una clave estable; así el generador es
--     idempotente y el lote se puede excluir o revertir con exactitud.
--   * VISTA_DEPOSITOS_ESTADO: cada depósito con su estado de registro
--     (DECLARADA_SIN_SENSOR / DETECTADA_POR_SENSOR / VERIFICADA), pesos medido y
--     estimado por separado, color de caneca y marca es_simulacion / id_lote.
--   * VISTA_LOTES_SIMULACION: resumen por lote para la sustentación.
--
-- Los datos de un lote además se crean con canal Simulador (3) en una estación
-- cuyo nombre contiene «SIMULACIÓN»: las vistas de informes existentes (V015,
-- es_ensayo) ya los dejan fuera por defecto.
-- Reversión (solo en desarrollo): database/06_migraciones/reversion/R025__lote_simulacion.sql
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

-- Repetible: si las tablas ya existen no se tocan (las vistas se recrean con CREATE OR REPLACE).
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name = 'DEPOSITO' AND column_name IN ('SIN_SENSOR', 'PESO_ESTIMADO_KG');
    IF n <> 2 THEN RAISE_APPLICATION_ERROR(-20252, 'V025 requiere V016 (deposito.sin_sensor).'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'LOTE_SIMULACION';
    IF n = 0 THEN
        EXECUTE IMMEDIATE q'[
CREATE TABLE lote_simulacion (
    id_lote             VARCHAR2(40 CHAR)  NOT NULL,
    descripcion         VARCHAR2(200 CHAR) NOT NULL,
    semilla             NUMBER(10)         NOT NULL,
    fecha_creacion_utc  TIMESTAMP          NOT NULL,
    ciudadanos          NUMBER(5)          DEFAULT 0 NOT NULL,
    registros           NUMBER(5)          DEFAULT 0 NOT NULL,
    estado              VARCHAR2(12 CHAR)  DEFAULT 'EN_CURSO' NOT NULL,
    CONSTRAINT pk_lote_simulacion        PRIMARY KEY (id_lote),
    CONSTRAINT ck_lote_simulacion_id     CHECK (REGEXP_LIKE(id_lote, '^SIM-[A-Z0-9-]{4,36}$', 'c')),
    CONSTRAINT ck_lote_simulacion_desc   CHECK (TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion)),
    CONSTRAINT ck_lote_simulacion_estado CHECK (estado IN ('EN_CURSO', 'COMPLETO')),
    CONSTRAINT ck_lote_simulacion_conteo CHECK (ciudadanos >= 0 AND registros >= 0)
)]';
        EXECUTE IMMEDIATE q'[COMMENT ON TABLE lote_simulacion IS 'Lotes de datos experimentales (SIMULACION). No son depósitos físicos ni pesajes reales.']';
    END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'REGISTRO_SIMULACION';
    IF n = 0 THEN
        EXECUTE IMMEDIATE q'[
CREATE TABLE registro_simulacion (
    id_lote      VARCHAR2(40 CHAR) NOT NULL,
    tabla        VARCHAR2(30 CHAR) NOT NULL,
    id_registro  NUMBER(10)        NOT NULL,
    clave        VARCHAR2(80 CHAR) NOT NULL,
    CONSTRAINT pk_registro_simulacion    PRIMARY KEY (id_lote, tabla, id_registro),
    CONSTRAINT uq_registro_simulacion    UNIQUE (id_lote, clave),
    CONSTRAINT fk_registro_simulacion    FOREIGN KEY (id_lote) REFERENCES lote_simulacion (id_lote),
    CONSTRAINT ck_registro_sim_tabla     CHECK (tabla IN ('USUARIO', 'PUNTO_INSTALACION', 'CONTENEDOR', 'INTERACCION', 'DEPOSITO')),
    CONSTRAINT ck_registro_sim_clave     CHECK (TRIM(clave) IS NOT NULL AND clave = TRIM(clave))
)]';
        EXECUTE IMMEDIATE 'CREATE INDEX ix_registro_simulacion_fila ON registro_simulacion (tabla, id_registro)';
        EXECUTE IMMEDIATE q'[COMMENT ON TABLE registro_simulacion IS 'Filas creadas por cada lote de SIMULACION, con clave estable para idempotencia y exclusión exacta.']';
    END IF;
END;
/

CREATE OR REPLACE VIEW vista_depositos_estado AS
SELECT d.id_deposito,
       d.fecha_deteccion_utc,
       m.codigo                                   AS material,
       m.categoria                                AS categoria_material,
       c.id_contenedor,
       c.nombre                                   AS caneca,
       DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 'SIN CONFIGURAR') AS color_caneca,
       p.id_punto_instalacion,
       p.nombre                                   AS estacion,
       i.canal,
       CASE WHEN d.id_material_verificado IS NOT NULL THEN 'VERIFICADA'
            WHEN d.sin_sensor = 1 THEN 'DECLARADA_SIN_SENSOR'
            ELSE 'DETECTADA_POR_SENSOR' END       AS estado_registro,
       d.peso_medido_kg,
       d.peso_estimado_kg,
       i.cantidad_declarada,
       d.cantidad_verificada,
       CASE WHEN rs.id_lote IS NOT NULL THEN 1 ELSE 0 END AS es_simulacion,
       rs.id_lote,
       CASE WHEN rs.id_lote IS NOT NULL OR i.canal IN (3, 4) OR UPPER(c.nombre) LIKE '%FICTIC%'
                 OR UPPER(p.nombre) LIKE '%SIMULACI%' THEN 1 ELSE 0 END AS es_ensayo
FROM   deposito d
JOIN   orden_apertura o     ON o.id_orden = d.id_orden
JOIN   interaccion i        ON i.id_interaccion = o.id_interaccion
JOIN   material m           ON m.id_material = d.id_material_declarado
JOIN   contenedor c         ON c.id_contenedor = o.id_contenedor
JOIN   punto_instalacion p  ON p.id_punto_instalacion = c.id_punto_instalacion
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor
LEFT JOIN registro_simulacion rs ON rs.tabla = 'DEPOSITO' AND rs.id_registro = d.id_deposito
WITH READ ONLY;

CREATE OR REPLACE VIEW vista_lotes_simulacion AS
SELECT l.id_lote, l.descripcion, l.semilla, l.fecha_creacion_utc, l.estado, l.ciudadanos, l.registros,
       (SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote = l.id_lote AND r.tabla = 'USUARIO')     AS usuarios_creados,
       (SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote = l.id_lote AND r.tabla = 'INTERACCION') AS interacciones_creadas,
       (SELECT COUNT(*) FROM registro_simulacion r WHERE r.id_lote = l.id_lote AND r.tabla = 'DEPOSITO')    AS depositos_creados
FROM   lote_simulacion l
WITH READ ONLY;

PROMPT V025_LOTE_SIMULACION_APLICADA
