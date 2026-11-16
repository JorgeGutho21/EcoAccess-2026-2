-- =====================================================================
-- EcoAccess P3 — migración V026: puntos de disposición especial (Valledupar)
-- Archivo: database/06_migraciones/V026__puntos_disposicion_valledupar.sql
--
-- Misión final (12/11/2026), fase 7. ADITIVA: crea PUNTO_DISPOSICION y siembra los
-- puntos verificados con fuentes públicas. No modifica ni borra filas existentes.
--
--   * Un punto sin dirección es un PROGRAMA (estado SOLO_PROGRAMA); un punto con
--     dirección solo puede estar LISTADO_SIN_CONFIRMAR o CONFIRMADO.
--   * FUENTE debe ser una URL https; FECHA_VERIFICACION es obligatoria.
--   * RUTAS: nombres de RutaDisposicion separados por coma (Raee,PilasBaterias,…).
--
-- La semilla coincide con CatalogoPuntosValledupar.Semilla (C#). El bloque marcado
-- con las marcas de inicio y fin es PL/SQL puro: lo ejecutan SQL*Plus y la prueba de
-- integración (esquema aislado). Reversión: reversion/R026__puntos_disposicion_valledupar.sql
-- Estado al 12/11/2026: aplicada SOLO en el esquema aislado de pruebas.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
-- BLOQUE-INICIO
DECLARE
    n NUMBER;
    PROCEDURE punto(p_codigo VARCHAR2, p_nombre VARCHAR2, p_direccion VARCHAR2, p_rutas VARCHAR2, p_recibe VARCHAR2,
                    p_estado VARCHAR2, p_fuente VARCHAR2) IS
    BEGIN
        -- SQL dinámico: la tabla se crea en este mismo bloque y no existe al compilarlo.
        EXECUTE IMMEDIATE 'MERGE INTO punto_disposicion d USING (SELECT :c codigo, :n nombre, :d direccion, :r rutas, :e recibe, :s estado, :f fuente FROM dual) s
            ON (d.codigo = s.codigo)
            WHEN MATCHED THEN UPDATE SET d.nombre = s.nombre, d.direccion = s.direccion, d.rutas = s.rutas, d.recibe = s.recibe,
                d.estado = s.estado, d.fuente = s.fuente, d.fecha_verificacion = DATE ''2026-11-12''
            WHEN NOT MATCHED THEN INSERT (codigo, nombre, direccion, municipio, rutas, recibe, estado, fuente, fecha_verificacion)
                VALUES (s.codigo, s.nombre, s.direccion, ''Valledupar'', s.rutas, s.recibe, s.estado, s.fuente, DATE ''2026-11-12'')'
            USING p_codigo, p_nombre, p_direccion, p_rutas, p_recibe, p_estado, p_fuente;
    END;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'PUNTO_DISPOSICION';
    IF n = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLE punto_disposicion (
            codigo             VARCHAR2(40)  CONSTRAINT pk_punto_disposicion PRIMARY KEY,
            nombre             VARCHAR2(160) NOT NULL,
            direccion          VARCHAR2(200),
            municipio          VARCHAR2(60)  NOT NULL,
            rutas              VARCHAR2(200) NOT NULL,
            recibe             VARCHAR2(400) NOT NULL,
            estado             VARCHAR2(30)  NOT NULL CONSTRAINT ck_punto_estado CHECK (estado IN (''CONFIRMADO'', ''LISTADO_SIN_CONFIRMAR'', ''SOLO_PROGRAMA'')),
            fuente             VARCHAR2(400) NOT NULL CONSTRAINT ck_punto_fuente CHECK (fuente LIKE ''https://%''),
            fecha_verificacion DATE          NOT NULL,
            horario            VARCHAR2(200),
            activo             NUMBER(1)     DEFAULT 1 NOT NULL CONSTRAINT ck_punto_activo CHECK (activo IN (0, 1)),
            CONSTRAINT ck_punto_direccion CHECK ((direccion IS NULL AND estado = ''SOLO_PROGRAMA'') OR (direccion IS NOT NULL AND estado <> ''SOLO_PROGRAMA''))
        )';
        EXECUTE IMMEDIATE 'CREATE INDEX ix_punto_municipio ON punto_disposicion (municipio, activo)';
    END IF;
    punto('VUP-UNICENTRO-LITO', 'Centro Comercial Unicentro Valledupar', 'Cra. 9 #6-42', 'Raee,PilasBaterias,Bombillos',
          'Aparece en la lista de puntos de recolección de la Fundación Puntos Verdes Lito (RAEE, pilas y lámparas)',
          'LISTADO_SIN_CONFIRMAR', 'https://fundacionpuntosverdes.com/puntos-de-recoleccion/');
    punto('VUP-CORPOCESAR-AMARILLO', 'CORPOCESAR (punto amarillo «Cierra el ciclo»)',
          'Km 2 vía La Paz, Lote 1 U.I.C. Casa e'' Campo, frente a la feria ganadera', 'Peligroso',
          'Envases y empaques de insecticidas domésticos (programa posconsumo de plaguicidas)',
          'LISTADO_SIN_CONFIRMAR', 'https://www.corpocesar.gov.co/residuos-y-consumo-sostenible/programas-de-posconsumo');
    punto('PROG-PUNTO-AZUL', 'Punto Azul (medicamentos posconsumo)', NULL, 'Medicamentos',
          'Medicamentos vencidos o parcialmente consumidos y sus empaques, en contenedores de droguerías y supermercados',
          'SOLO_PROGRAMA', 'https://www.corpocesar.gov.co/residuos-y-consumo-sostenible/programas-de-posconsumo');
    punto('PROG-PILAS-AMBIENTE', 'Pilas con el Ambiente', NULL, 'PilasBaterias', 'Pilas de uso doméstico (programa posconsumo nacional)',
          'SOLO_PROGRAMA', 'https://www.eltiempo.com/vida/educacion/como-deben-botarse-residuos-como-medicamentos-computadores-bombillos-y-pilas-36663');
    punto('PROG-LUMINA', 'Lúmina (bombillos posconsumo)', NULL, 'Bombillos', 'Bombillos fluorescentes y ahorradores (programa posconsumo nacional)',
          'SOLO_PROGRAMA', 'https://www.cvc.gov.co/documentos/residuos-posconsumo/puntos-de-recoleccion-posconsumo');
    punto('PROG-RED-POSCONSUMO', 'Aplicación Red Posconsumo (Minambiente)', NULL, 'Raee,PilasBaterias,Medicamentos,Bombillos,Peligroso',
          'Buscador oficial de contenedores posconsumo cercanos',
          'SOLO_PROGRAMA', 'https://www.eltiempo.com/vida/medio-ambiente/donde-se-puede-botar-basura-peligrosa-en-colombia-88872');
    COMMIT;
END;
-- BLOQUE-FIN
/
