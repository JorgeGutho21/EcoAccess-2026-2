-- Reversión de V026 (solo desarrollo o esquema aislado): elimina la tabla PUNTO_DISPOSICION y su semilla.
-- No afecta ninguna otra tabla: PUNTO_DISPOSICION no tiene llaves foráneas hacia ni desde el resto del modelo.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name = 'PUNTO_DISPOSICION';
    IF n = 1 THEN EXECUTE IMMEDIATE 'DROP TABLE punto_disposicion PURGE'; END IF;
END;
/
