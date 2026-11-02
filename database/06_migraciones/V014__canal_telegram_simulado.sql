-- Extensión de dominio: no cambia filas, roles, credenciales ni volúmenes.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DECLARE condicion VARCHAR2(4000); n NUMBER;
BEGIN
  SELECT search_condition_vc INTO condicion FROM user_constraints
  WHERE table_name='INTERACCION' AND constraint_name='CK_INTERACCION_CANAL';
  IF INSTR(condicion,'4')=0 THEN
    SELECT COUNT(*) INTO n FROM user_constraints WHERE constraint_name='CK_INTERACCION_CANAL_V014';
    IF n=0 THEN
      EXECUTE IMMEDIATE 'ALTER TABLE interaccion ADD CONSTRAINT ck_interaccion_canal_v014 CHECK (canal IN (1,2,3,4)) ENABLE VALIDATE';
    END IF;
    -- La restricción ampliada ya está activa antes de retirar la anterior.
    EXECUTE IMMEDIATE 'ALTER TABLE interaccion DROP CONSTRAINT ck_interaccion_canal';
    EXECUTE IMMEDIATE 'ALTER TABLE interaccion RENAME CONSTRAINT ck_interaccion_canal_v014 TO ck_interaccion_canal';
  END IF;
END;
/
COMMENT ON COLUMN interaccion.canal IS 'CanalInteraccion: 1 Telegram, 2 Manual, 3 Simulador escritorio, 4 Telegram simulado con sesion privada.';
-- Exclusión operativa inmediata; V015 añade agregados de demostración separados.
CREATE OR REPLACE VIEW ea_r_depositos AS
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor,c.nombre caneca,p.id_punto_instalacion,p.nombre ubicacion,
       m.codigo material,COUNT(*) depositos,COUNT(d.id_material_verificado) verificados,
       COUNT(DISTINCT i.id_usuario) ciudadanos_del_dia,COUNT(d.peso_medido_kg) con_peso_medido,SUM(d.peso_medido_kg) kg_medidos
FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
JOIN material m ON m.id_material=d.id_material_declarado
WHERE i.canal NOT IN (3,4) AND UPPER(c.nombre) NOT LIKE '%FICTIC%'
 AND UPPER(p.nombre) NOT LIKE '%SIMULACI%' AND NOT REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)')
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
 c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,m.codigo WITH READ ONLY;
CREATE OR REPLACE VIEW ea_r_puntos AS
SELECT TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
 c.id_contenedor,c.nombre caneca,p.id_punto_instalacion,p.nombre ubicacion,
 SUM(CASE WHEN me.puntos>0 THEN me.puntos ELSE 0 END) acreditados,
 SUM(CASE WHEN me.puntos<0 THEN me.puntos ELSE 0 END) reversos,SUM(me.puntos) netos
FROM movimiento_ecopuntos me JOIN operacion_ecopuntos op ON op.id_operacion=me.id_operacion
JOIN deposito d ON d.id_deposito=me.id_deposito JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion JOIN material m ON m.id_material=d.id_material_declarado
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
WHERE i.canal NOT IN (3,4) AND UPPER(c.nombre) NOT LIKE '%FICTIC%'
 AND UPPER(p.nombre) NOT LIKE '%SIMULACI%' AND NOT REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)')
GROUP BY TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
 c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre WITH READ ONLY;
SELECT constraint_name,status,validated FROM user_constraints WHERE constraint_name='CK_INTERACCION_CANAL';
