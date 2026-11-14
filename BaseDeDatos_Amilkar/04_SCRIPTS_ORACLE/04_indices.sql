-- =====================================================================
-- EcoAccess BD — 04 · Índices
-- Ejecutar como ECOACCESS_BD después de 03 (F5).
--
-- Dos clases de índice:
--   A) ÚNICOS FUNCIONALES: reglas de negocio que un UNIQUE normal no
--      puede expresar porque solo aplican a ALGUNAS filas. Truco: el CASE
--      devuelve NULL para las filas que no importan, y Oracle no guarda en
--      el índice las filas cuyas columnas son todas NULL.
--   B) ÍNDICES DE CLAVE FORÁNEA y de búsqueda por fecha: aceleran los JOIN
--      y evitan que borrar/actualizar una fila padre bloquee toda la tabla
--      hija.
-- Las PK y UNIQUE ya crearon su propio índice en 02/03.
-- =====================================================================
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

-- ---------------- A) Reglas con índices únicos funcionales ----------------
-- RN: una cuenta de Telegram tiene a lo sumo UN vínculo activo...
CREATE UNIQUE INDEX uq_vinculo_telegram_activo ON vinculo_telegram (
    CASE WHEN fecha_baja_utc IS NULL THEN telegram_user_id END);
-- ...y un usuario EcoAccess también (las bajas se conservan como historia).
CREATE UNIQUE INDEX uq_vinculo_usuario_activo ON vinculo_telegram (
    CASE WHEN fecha_baja_utc IS NULL THEN id_usuario END);
-- RN: una estación tiene como máximo UNA caneca no dada de baja de cada color.
CREATE UNIQUE INDEX uq_contenedor_color_activo ON contenedor (
    CASE WHEN estado <> 3 THEN id_punto_instalacion END,
    CASE WHEN estado <> 3 THEN categoria END);
-- RN: como máximo UNA orden vigente (estados 1 a 5) por caneca, aunque
-- lleguen dos solicitudes a la vez (la segunda recibe ORA-00001).
CREATE UNIQUE INDEX uq_orden_vigente ON orden_apertura (
    CASE WHEN estado BETWEEN 1 AND 5 THEN id_contenedor END);

-- ---------------- B) Índices de claves foráneas y fechas ----------------
CREATE INDEX ix_usuario_rol_rol          ON usuario_rol (id_rol);
CREATE INDEX ix_vinculo_usuario          ON vinculo_telegram (id_usuario);
CREATE INDEX ix_contenedor_punto         ON contenedor (id_punto_instalacion);
CREATE INDEX ix_cont_mat_material        ON contenedor_material (id_material, categoria);
CREATE INDEX ix_cont_mat_contenedor_cat  ON contenedor_material (id_contenedor, categoria);
CREATE INDEX ix_peso_material            ON peso_referencial (id_material);

CREATE INDEX ix_interaccion_punto_fecha  ON interaccion (id_punto_instalacion, fecha_inicio_utc);
CREATE INDEX ix_interaccion_cont_punto   ON interaccion (id_contenedor, id_punto_instalacion);
CREATE INDEX ix_interaccion_usuario      ON interaccion (id_usuario, fecha_inicio_utc);
CREATE INDEX ix_interaccion_mat_sug      ON interaccion (id_material_sugerido);
CREATE INDEX ix_interaccion_mat_conf     ON interaccion (id_material_confirmado);
CREATE INDEX ix_interaccion_operario     ON interaccion (id_operario);

CREATE INDEX ix_consulta_fecha           ON consulta_vision (fecha_utc);
CREATE INDEX ix_consulta_usuario         ON consulta_vision (id_usuario, fecha_utc);
CREATE INDEX ix_consulta_interaccion     ON consulta_vision (id_interaccion);
CREATE INDEX ix_consulta_material        ON consulta_vision (id_material);
CREATE INDEX ix_consulta_peso            ON consulta_vision (id_peso_referencial);
CREATE INDEX ix_consulta_contenedor      ON consulta_vision (id_contenedor_sugerido);

CREATE INDEX ix_orden_contenedor_fecha   ON orden_apertura (id_contenedor, fecha_emision_utc);
CREATE INDEX ix_evento_orden             ON evento_dispositivo (id_orden, id_contenedor);
CREATE INDEX ix_evento_resultado_fecha   ON evento_dispositivo (resultado, fecha_recepcion_utc);

CREATE INDEX ix_deposito_fecha           ON deposito (fecha_deteccion_utc);
CREATE INDEX ix_deposito_estado_fecha    ON deposito (estado_verificacion, fecha_deteccion_utc);
CREATE INDEX ix_deposito_mat_decl        ON deposito (id_material_declarado);
CREATE INDEX ix_deposito_mat_verif       ON deposito (id_material_verificado);
CREATE INDEX ix_deposito_verificador     ON deposito (id_verificador);
CREATE INDEX ix_deposito_peso            ON deposito (id_peso_referencial);

CREATE INDEX ix_regla_material_desde     ON regla_ecopuntos (id_material, desde_utc);
CREATE INDEX ix_regla_creador            ON regla_ecopuntos (id_creador);
CREATE INDEX ix_mov_usuario              ON movimiento_ecopuntos (id_usuario, id_movimiento);
CREATE INDEX ix_mov_deposito             ON movimiento_ecopuntos (id_deposito);
CREATE INDEX ix_mov_regla                ON movimiento_ecopuntos (id_regla);
CREATE INDEX ix_mov_actor                ON movimiento_ecopuntos (id_actor);

CREATE INDEX ix_aud_dep_deposito_fecha   ON auditoria_deposito (id_deposito, fecha_utc);
CREATE INDEX ix_hist_contenedor_fecha    ON historial_estado_contenedor (id_contenedor, fecha_utc);
CREATE INDEX ix_hist_actor               ON historial_estado_contenedor (id_actor);

BEGIN
    DBMS_OUTPUT.PUT_LINE('04_indices: índices creados. Sigue con 05_secuencias.sql.');
END;
/
WHENEVER SQLERROR CONTINUE
