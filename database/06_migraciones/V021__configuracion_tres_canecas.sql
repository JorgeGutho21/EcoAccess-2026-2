-- =====================================================================
-- EcoAccess P3 — migración V021: estación con tres canecas (blanca, verde, negra)
-- Archivo: database/06_migraciones/V021__configuracion_tres_canecas.sql
--
-- Aditiva. Reutiliza PUNTO_INSTALACION (la estación) y CONTENEDOR (cada caneca):
-- una estación ya puede tener varias canecas. Agrega CONFIGURACION_CANECA, 1:1
-- con CONTENEDOR, con lo que faltaba para dirigir cada residuo a su caneca:
--   * categoria: 1 Aprovechable (blanca), 2 Orgánico aprovechable (verde),
--     3 No aprovechable (negra). Los peligrosos (4) NO tienen caneca: van a un
--     punto posconsumo (Resolución 2184 de 2019).
--   * identificador_dispositivo: el ESP32 que mueve SU tapa (único). El
--     firmware rechaza órdenes de otra caneca, así que una orden para la blanca
--     nunca abre la verde ni la negra.
--   * ubicacion_en_estacion: «izquierda», «centro»…, para el operario.
-- El color no se guarda: se deduce de la categoría (una sola fuente de verdad).
-- No modifica filas existentes. Una caneca sin configuración sigue funcionando
-- como antes (caneca piloto de un compartimento).
-- Reversión (solo en desarrollo): DROP VIEW vista_canecas_estacion;
--                                  DROP TABLE configuracion_caneca;
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'CONFIGURACION_CANECA';
    IF n > 0 THEN RAISE_APPLICATION_ERROR(-20211, 'V021 ya aplicada; no se modificó nada.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name IN ('CONTENEDOR', 'PUNTO_INSTALACION', 'ORDEN_APERTURA');
    IF n <> 3 THEN RAISE_APPLICATION_ERROR(-20212, 'V021 requiere CONTENEDOR, PUNTO_INSTALACION y ORDEN_APERTURA.'); END IF;
END;
/

CREATE TABLE configuracion_caneca (
    id_contenedor              NUMBER(10)         NOT NULL,
    categoria                  NUMBER(1)          NOT NULL,
    identificador_dispositivo  VARCHAR2(40 CHAR),
    ubicacion_en_estacion      VARCHAR2(60 CHAR),
    fecha_actualizacion_utc    TIMESTAMP          NOT NULL,
    CONSTRAINT pk_configuracion_caneca      PRIMARY KEY (id_contenedor),
    CONSTRAINT fk_configuracion_contenedor  FOREIGN KEY (id_contenedor) REFERENCES contenedor (id_contenedor),
    CONSTRAINT ck_configuracion_categoria   CHECK (categoria IN (1, 2, 3)),
    CONSTRAINT uq_configuracion_dispositivo UNIQUE (identificador_dispositivo),
    CONSTRAINT ck_configuracion_dispositivo CHECK (identificador_dispositivo IS NULL OR (
        LENGTH(identificador_dispositivo) BETWEEN 3 AND 40
        AND TRANSLATE(identificador_dispositivo,
                      '#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', '#') IS NULL)),
    CONSTRAINT ck_configuracion_ubicacion   CHECK (ubicacion_en_estacion IS NULL OR (
        TRIM(ubicacion_en_estacion) IS NOT NULL AND ubicacion_en_estacion = TRIM(ubicacion_en_estacion)))
);

COMMENT ON TABLE  configuracion_caneca IS 'Color (por categoría), dispositivo y ubicación de cada caneca de una estación de tres canecas.';
COMMENT ON COLUMN configuracion_caneca.categoria IS '1 blanca aprovechable, 2 verde orgánico, 3 negra no aprovechable; mismos valores que CategoriaMaterial.';
COMMENT ON COLUMN configuracion_caneca.identificador_dispositivo IS 'ESP32 vinculado a esta caneca; coincide con DISPOSITIVO del firmware y del bridge.';

-- Vista de consulta para SQL Developer, informes y la sustentación.
CREATE OR REPLACE VIEW vista_canecas_estacion AS
SELECT p.id_punto_instalacion,
       p.nombre                         AS estacion,
       c.id_contenedor,
       c.nombre                         AS caneca,
       c.estado,
       k.categoria,
       DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 'SIN CONFIGURAR') AS color,
       k.identificador_dispositivo,
       k.ubicacion_en_estacion,
       c.identificador_qr
FROM   contenedor c
JOIN   punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion
LEFT JOIN configuracion_caneca k ON k.id_contenedor = c.id_contenedor;

PROMPT V021_TRES_CANECAS_APLICADA
