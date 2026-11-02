-- Conserva las tarifas anteriores por depósito (por_unidad=0), sin inventar conteos históricos.
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables WHERE table_name='OPINION';
    IF n<>1 THEN RAISE_APPLICATION_ERROR(-20111,'V011 requiere V010.'); END IF;
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE
        (table_name='DEPOSITO' AND column_name='CANTIDAD_VERIFICADA') OR
        (table_name='REGLA_ECOPUNTOS' AND column_name='POR_UNIDAD');
    IF n<>0 THEN RAISE_APPLICATION_ERROR(-20112,'V011 aplicada o parcial; revisar antes de continuar.'); END IF;
END;
/
ALTER TABLE deposito ADD (cantidad_verificada NUMBER(4));
ALTER TABLE deposito ADD CONSTRAINT ck_deposito_cantidad CHECK (cantidad_verificada IS NULL OR
    (cantidad_verificada BETWEEN 1 AND 1000 AND cantidad_verificada=TRUNC(cantidad_verificada) AND id_material_verificado IS NOT NULL));
ALTER TABLE regla_ecopuntos ADD (por_unidad NUMBER(1) DEFAULT 0 NOT NULL);
ALTER TABLE regla_ecopuntos ADD CONSTRAINT ck_regla_por_unidad CHECK(por_unidad IN (0,1));
COMMENT ON COLUMN deposito.cantidad_verificada IS 'Conteo independiente con el responsable y fecha de verificacion; no se copia de la declaracion.';
COMMENT ON COLUMN regla_ecopuntos.por_unidad IS '0 tarifa por deposito historica, 1 tarifa por unidad verificada.';
-- Defensa también para ejecutables anteriores que aún no entienden por_unidad.
-- No modifica créditos históricos y no acredita nada desde el trigger.
CREATE OR REPLACE TRIGGER tr_ecop_unidades
BEFORE INSERT ON movimiento_ecopuntos
FOR EACH ROW
WHEN (NEW.tipo=1)
DECLARE
    unidad NUMBER;
    tarifa NUMBER;
    cantidad NUMBER;
BEGIN
    SELECT por_unidad,puntos INTO unidad,tarifa FROM regla_ecopuntos WHERE id_regla=:NEW.id_regla;
    IF unidad=1 THEN
        SELECT cantidad_verificada INTO cantidad FROM deposito WHERE id_deposito=:NEW.id_deposito;
        IF cantidad IS NULL OR :NEW.puntos<>tarifa*cantidad THEN
            RAISE_APPLICATION_ERROR(-20113,'La tarifa unitaria exige conteo verificado y puntos coincidentes.');
        END IF;
    END IF;
END;
/
SHOW ERRORS TRIGGER tr_ecop_unidades
PROMPT V011_TARIFAS_UNITARIAS_APLICADA
