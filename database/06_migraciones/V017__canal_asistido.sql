-- Registro asistido por un operador desde Windows Forms: canal 5. No cambia filas existentes.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
BEGIN
    FOR c IN (SELECT constraint_name FROM user_constraints
              WHERE table_name = 'INTERACCION' AND constraint_name LIKE 'CK_INTERACCION_CANAL%') LOOP
        EXECUTE IMMEDIATE 'ALTER TABLE interaccion DROP CONSTRAINT ' || c.constraint_name;
    END LOOP;
    EXECUTE IMMEDIATE 'ALTER TABLE interaccion ADD CONSTRAINT ck_interaccion_canal CHECK (canal IN (1, 2, 3, 4, 5)) ENABLE VALIDATE';
END;
/