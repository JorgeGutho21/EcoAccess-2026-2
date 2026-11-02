-- EcoAccess P3: traza mínima de análisis, sin imágenes ni respuestas libres.
-- Aditiva. Ejecutar como dueño después de V007; ensayada únicamente en PRUEBAP3.
-- No modifica canal, usuarios, órdenes, depósitos ni ecopuntos. No ejecuta DROP ni DELETE.
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'ANALISIS_IMAGEN';
    IF n > 0 THEN RAISE_APPLICATION_ERROR(-20081, 'V008 ya aplicada; no se modificó nada.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name IN ('INTERACCION','MATERIAL','VINCULACION_PENDIENTE');
    IF n <> 3 THEN RAISE_APPLICATION_ERROR(-20082, 'V008 requiere V007 y tablas del dominio.'); END IF;
END;
/
CREATE TABLE analisis_imagen (
    clave VARCHAR2(100 CHAR) NOT NULL,
    id_interaccion NUMBER(10) NOT NULL,
    proveedor VARCHAR2(80 CHAR) NOT NULL,
    modelo VARCHAR2(120 CHAR) NOT NULL,
    version_modelo VARCHAR2(40 CHAR) NOT NULL,
    estado NUMBER(2) NOT NULL,
    id_material_sugerido NUMBER(10),
    fecha_utc TIMESTAMP NOT NULL,
    id_material_confirmado NUMBER(10),
    fecha_confirmacion_utc TIMESTAMP,
    CONSTRAINT pk_analisis_imagen PRIMARY KEY (clave),
    CONSTRAINT fk_analisis_interaccion FOREIGN KEY (id_interaccion) REFERENCES interaccion(id_interaccion),
    CONSTRAINT fk_analisis_sugerido FOREIGN KEY (id_material_sugerido) REFERENCES material(id_material),
    CONSTRAINT fk_analisis_confirmado FOREIGN KEY (id_material_confirmado) REFERENCES material(id_material),
    CONSTRAINT ck_analisis_estado CHECK (estado BETWEEN 1 AND 8),
    CONSTRAINT ck_analisis_confirmacion CHECK ((id_material_confirmado IS NULL AND fecha_confirmacion_utc IS NULL)
        OR (id_material_confirmado IS NOT NULL AND fecha_confirmacion_utc IS NOT NULL AND fecha_confirmacion_utc >= fecha_utc))
);
CREATE INDEX ix_analisis_interaccion ON analisis_imagen(id_interaccion, fecha_utc);
PROMPT V008_TRAZABILIDAD_APLICADA
