-- Libro de ecopuntos: reglas versionadas, operaciones idempotentes y movimientos inmutables desde la API.
-- No instala tarifas: los valores y materiales deben configurarse expresamente por un administrador.
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE n NUMBER;
BEGIN
 SELECT COUNT(*) INTO n FROM user_tables WHERE table_name IN ('REGLA_ECOPUNTOS','OPERACION_ECOPUNTOS','MOVIMIENTO_ECOPUNTOS');
 IF n <> 0 THEN RAISE_APPLICATION_ERROR(-20050,'V005 ya aplicada o parcial; se conservan las tablas existentes.'); END IF;
END;
/
CREATE TABLE regla_ecopuntos (
 id_regla NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
 id_material NUMBER(10) NOT NULL REFERENCES material(id_material),
 puntos NUMBER(7) NOT NULL CHECK (puntos BETWEEN 1 AND 1000000),
 descripcion VARCHAR2(200 CHAR) NOT NULL CHECK (TRIM(descripcion) IS NOT NULL AND descripcion=TRIM(descripcion)),
 desde_utc TIMESTAMP NOT NULL,
 hasta_utc TIMESTAMP,
 id_creador NUMBER(10) NOT NULL REFERENCES usuario(id_usuario),
 creada_utc TIMESTAMP DEFAULT SYS_EXTRACT_UTC(SYSTIMESTAMP) NOT NULL,
 CONSTRAINT ck_regla_intervalo CHECK (hasta_utc IS NULL OR hasta_utc > desde_utc)
);
CREATE TABLE operacion_ecopuntos (
 id_operacion NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
 clave VARCHAR2(80 CHAR) NOT NULL,
 tipo NUMBER(1) NOT NULL,
 id_deposito NUMBER(10) NOT NULL REFERENCES deposito(id_deposito),
 id_actor NUMBER(10) NOT NULL REFERENCES usuario(id_usuario),
 id_credito NUMBER(10),
 id_regla NUMBER(10) REFERENCES regla_ecopuntos(id_regla),
 motivo VARCHAR2(300 CHAR) NOT NULL CHECK (TRIM(motivo) IS NOT NULL AND motivo=TRIM(motivo)),
 fecha_utc TIMESTAMP NOT NULL,
 CONSTRAINT uq_ecop_operacion_clave UNIQUE(clave),
 CONSTRAINT uq_ecop_operacion_deposito UNIQUE(id_operacion,id_deposito),
 CONSTRAINT ck_ecop_clave CHECK(TRIM(clave) IS NOT NULL AND clave=TRIM(clave)),
 CONSTRAINT ck_ecop_operacion CHECK (
  (tipo=1 AND id_credito IS NULL AND id_regla IS NOT NULL) OR
  (tipo=2 AND id_credito IS NOT NULL AND id_regla IS NULL) OR
  (tipo=3 AND id_credito IS NOT NULL AND id_regla IS NOT NULL))
);
CREATE TABLE movimiento_ecopuntos (
 id_movimiento NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
 id_operacion NUMBER(10) NOT NULL,
 id_deposito NUMBER(10) NOT NULL REFERENCES deposito(id_deposito),
 id_usuario NUMBER(10) NOT NULL REFERENCES usuario(id_usuario),
 id_regla NUMBER(10) NOT NULL REFERENCES regla_ecopuntos(id_regla),
 tipo NUMBER(1) NOT NULL,
 puntos NUMBER(7) NOT NULL,
 id_credito_revertido NUMBER(10),
 id_predecesor NUMBER(10),
 CONSTRAINT fk_ecop_mov_operacion FOREIGN KEY(id_operacion,id_deposito) REFERENCES operacion_ecopuntos(id_operacion,id_deposito),
 CONSTRAINT uq_ecop_mov_titular UNIQUE(id_movimiento,id_deposito,id_usuario),
 CONSTRAINT fk_ecop_mov_revertido FOREIGN KEY(id_credito_revertido,id_deposito,id_usuario) REFERENCES movimiento_ecopuntos(id_movimiento,id_deposito,id_usuario),
 CONSTRAINT fk_ecop_mov_predecesor FOREIGN KEY(id_predecesor,id_deposito,id_usuario) REFERENCES movimiento_ecopuntos(id_movimiento,id_deposito,id_usuario),
 CONSTRAINT uq_ecop_reverso UNIQUE(id_credito_revertido),
 CONSTRAINT uq_ecop_sucesor UNIQUE(id_predecesor),
 CONSTRAINT ck_ecop_mov CHECK (
  (tipo=1 AND puntos BETWEEN 1 AND 1000000 AND id_credito_revertido IS NULL) OR
  (tipo=2 AND puntos BETWEEN -1000000 AND -1 AND id_credito_revertido IS NOT NULL AND id_predecesor IS NULL)),
 CONSTRAINT ck_ecop_sin_autorreferencia CHECK ((id_credito_revertido IS NULL OR id_credito_revertido < id_movimiento)
  AND (id_predecesor IS NULL OR id_predecesor < id_movimiento))
);
CREATE UNIQUE INDEX uq_ecop_credito_inicial ON movimiento_ecopuntos
 (CASE WHEN tipo=1 AND id_predecesor IS NULL THEN id_deposito END);
CREATE INDEX ix_ecop_usuario ON movimiento_ecopuntos(id_usuario,id_movimiento);
CREATE INDEX ix_ecop_deposito ON movimiento_ecopuntos(id_deposito);
ALTER TABLE operacion_ecopuntos ADD CONSTRAINT fk_ecop_credito_objetivo
 FOREIGN KEY(id_credito) REFERENCES movimiento_ecopuntos(id_movimiento);
COMMENT ON TABLE regla_ecopuntos IS 'Versiones de tarifa; crear una versión nueva, nunca modificar la que respalda movimientos';
COMMENT ON TABLE movimiento_ecopuntos IS 'Libro auditable; la API solo agrega créditos y reversos, saldo = suma de puntos';
WHENEVER SQLERROR CONTINUE
