-- =====================================================================
-- EcoAccess P3 — migración V002: credenciales de inicio de sesión
-- Archivo: database/06_migraciones/V002__credencial_acceso.sql
--
-- Crea CREDENCIAL_ACCESO: una fila por cuenta con
-- contraseña, con el hash PBKDF2-SHA256, la sal y las iteraciones, más el
-- contador de intentos fallidos y el bloqueo temporal. Nunca la contraseña.
-- No cambia ninguna tabla de la versión 001.
--
-- Ejecutar conectado como dueño del esquema, después de la versión 001.
--   * Sin la tabla USUARIO -> se detiene (ORA-20041).
--   * Ya aplicada          -> se detiene sin tocar nada (ORA-20040).
--
-- Reversión (solo en desarrollo, y solo si nadie depende de las contraseñas):
--   DROP TABLE credencial_acceso;
-- Borra todas las contraseñas: las cuentas tendrían que recibir una nueva.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    v_usuario     NUMBER;
    v_credencial  NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_usuario    FROM user_tables WHERE table_name = 'USUARIO';
    SELECT COUNT(*) INTO v_credencial FROM user_tables WHERE table_name = 'CREDENCIAL_ACCESO';

    IF v_usuario = 0 THEN
        RAISE_APPLICATION_ERROR(-20041,
            'Falta la tabla USUARIO: ejecuta antes database/02_tablas/01_crear_tablas_iniciales.sql.');
    ELSIF v_credencial = 1 THEN
        RAISE_APPLICATION_ERROR(-20040,
            'La migración V002 ya estaba aplicada (existe CREDENCIAL_ACCESO). No se modificó nada.');
    END IF;
END;
/

-- RAW no tiene longitud fija: los CHECK con RAWTOHEX exigen exactamente 32 y
-- 16 bytes (64 y 32 cifras hexadecimales). Sin ON DELETE CASCADE, igual que
-- el resto del esquema: una cuenta con contraseña no se borra sin más.
CREATE TABLE credencial_acceso (
    id_usuario           NUMBER(10)   NOT NULL,
    hash_contrasena      RAW(32)      NOT NULL,
    sal                  RAW(16)      NOT NULL,
    iteraciones          NUMBER(7)    NOT NULL,
    intentos_fallidos    NUMBER(3)    DEFAULT 0 NOT NULL,
    bloqueada_hasta_utc  TIMESTAMP,
    fecha_cambio_utc     TIMESTAMP    NOT NULL,
    CONSTRAINT pk_credencial           PRIMARY KEY (id_usuario),
    CONSTRAINT fk_credencial_usuario   FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    CONSTRAINT ck_credencial_hash      CHECK (LENGTH(RAWTOHEX(hash_contrasena)) = 64),
    CONSTRAINT ck_credencial_sal       CHECK (LENGTH(RAWTOHEX(sal)) = 32),
    CONSTRAINT ck_credencial_iter      CHECK (iteraciones >= 100000),
    CONSTRAINT ck_credencial_intentos  CHECK (intentos_fallidos >= 0)
);

COMMENT ON TABLE  credencial_acceso IS 'Contraseña de inicio de sesión de la GUI: hash PBKDF2-SHA256 con sal. Nunca la contraseña.';
COMMENT ON COLUMN credencial_acceso.hash_contrasena     IS 'PBKDF2-HMAC-SHA256, 32 bytes.';
COMMENT ON COLUMN credencial_acceso.sal                 IS 'Sal aleatoria propia de esta contraseña, 16 bytes.';
COMMENT ON COLUMN credencial_acceso.iteraciones         IS 'Iteraciones con que se calculó el hash; permite subirlas sin invalidar las anteriores.';
COMMENT ON COLUMN credencial_acceso.intentos_fallidos   IS 'Fallos seguidos desde el último acierto o bloqueo.';
COMMENT ON COLUMN credencial_acceso.bloqueada_hasta_utc IS 'Hora UTC hasta la que no se admite inicio de sesión; NULL si no está bloqueada.';
COMMENT ON COLUMN credencial_acceso.fecha_cambio_utc    IS 'Hora UTC del último cambio de contraseña.';

BEGIN
    DBMS_OUTPUT.PUT_LINE('Migración V002 aplicada: tabla CREDENCIAL_ACCESO creada.');
END;
/

WHENEVER SQLERROR CONTINUE
