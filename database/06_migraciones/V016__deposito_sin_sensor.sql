-- Caneca sin sensor de ingreso: al terminar la ventana de apertura se conserva un depósito pendiente de verificación.
-- La marca distingue ese registro de una detección real. No cambia filas existentes.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
ALTER TABLE deposito ADD sin_sensor NUMBER(1) DEFAULT 0 NOT NULL;
ALTER TABLE deposito ADD CONSTRAINT ck_deposito_sin_sensor CHECK (sin_sensor IN (0, 1));
ALTER TABLE evento_dispositivo DROP CONSTRAINT ck_evento_tipo;
ALTER TABLE evento_dispositivo ADD CONSTRAINT ck_evento_tipo CHECK (tipo IN (1, 2, 3, 4, 5, 6));-- Las vistas de informes dependen de DEPOSITO: se recompilan para no dejarlas pendientes.
BEGIN DBMS_UTILITY.COMPILE_SCHEMA(USER, FALSE); END;
/
