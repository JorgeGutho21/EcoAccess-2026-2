-- =====================================================================
-- EcoAccess P3 — registro de versiones aplicadas (instalador sin Docker)
-- Archivo: database/00_administracion/03_registro_migraciones.sql
--
-- Crea ECO_MIGRACION_APLICADA si no existe. El instalador
-- (src/EcoAccess.P3.Instalador) lo ejecuta antes de cada instalación y
-- registra aquí cada versión que termina bien, con la huella SHA-256 del
-- script. Así sabe cuáles faltan y nunca repite una ya aplicada.
--
-- Ejecutar como el dueño del esquema (ECOACCESS_P3_APP en Oracle Cloud).
-- Es repetible: si la tabla existe, no hace nada. Compatible con Oracle 19c.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_existe NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_existe FROM user_tables WHERE table_name = 'ECO_MIGRACION_APLICADA';
    IF v_existe = 0 THEN
        EXECUTE IMMEDIATE q'[
            CREATE TABLE eco_migracion_aplicada (
                version        VARCHAR2(10 CHAR)  NOT NULL,
                descripcion    VARCHAR2(200 CHAR) NOT NULL,
                archivo        VARCHAR2(200 CHAR) NOT NULL,
                huella_sha256  VARCHAR2(64 CHAR)  NOT NULL,
                modo           VARCHAR2(12 CHAR)  NOT NULL,
                duracion_ms    NUMBER(10)         NOT NULL,
                aplicada_utc   TIMESTAMP          DEFAULT SYS_EXTRACT_UTC(SYSTIMESTAMP) NOT NULL,
                aplicada_por   VARCHAR2(128 CHAR) DEFAULT SYS_CONTEXT('USERENV', 'SESSION_USER') NOT NULL,
                CONSTRAINT pk_eco_migracion_aplicada PRIMARY KEY (version),
                CONSTRAINT ck_eco_migracion_modo CHECK (modo IN ('EJECUTADA', 'MARCADA')),
                CONSTRAINT ck_eco_migracion_huella CHECK (LENGTH(huella_sha256) = 64),
                CONSTRAINT ck_eco_migracion_duracion CHECK (duracion_ms >= 0)
            )]';
        DBMS_OUTPUT.PUT_LINE('Creada ECO_MIGRACION_APLICADA.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('ECO_MIGRACION_APLICADA ya existía: no se modificó.');
    END IF;
END;
/
