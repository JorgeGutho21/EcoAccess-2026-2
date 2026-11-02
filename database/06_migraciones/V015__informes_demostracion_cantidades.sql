-- Informes administrativos de solo lectura. 
-- No concede privilegios, no toca tablas ni modifica datos.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
CREATE OR REPLACE VIEW ea_r_base_depositos AS
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor, c.nombre caneca, p.id_punto_instalacion, p.nombre ubicacion,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo, m.codigo material, COUNT(*) depositos,
       COUNT(d.id_material_verificado) verificados,
       COUNT(DISTINCT i.id_usuario) ciudadanos_del_dia,
       COUNT(d.peso_medido_kg) con_peso_medido, SUM(d.peso_medido_kg) kg_medidos,
       SUM(CASE WHEN d.id_material_verificado IS NOT NULL THEN NVL(d.cantidad_verificada,0) ELSE 0 END) unidades_verificadas,
       COUNT(CASE WHEN d.id_material_verificado IS NOT NULL AND d.cantidad_verificada IS NULL THEN 1 END) verificados_sin_conteo
FROM deposito d
JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
JOIN material m ON m.id_material=d.id_material_declarado
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,m.codigo,
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;

CREATE OR REPLACE VIEW ea_r_base_puntos AS
SELECT TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor,c.nombre caneca,p.id_punto_instalacion,p.nombre ubicacion,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo,
       SUM(CASE WHEN me.puntos>0 THEN me.puntos ELSE 0 END) acreditados,
       SUM(CASE WHEN me.puntos<0 THEN me.puntos ELSE 0 END) reversos,
       SUM(me.puntos) netos
FROM movimiento_ecopuntos me
JOIN operacion_ecopuntos op ON op.id_operacion=me.id_operacion
JOIN deposito d ON d.id_deposito=me.id_deposito
JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN material m ON m.id_material=d.id_material_declarado
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
GROUP BY TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;

CREATE OR REPLACE VIEW ea_r_solicitudes AS
SELECT TRUNC(CAST(FROM_TZ(i.fecha_inicio_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor,c.nombre caneca,p.id_punto_instalacion,p.nombre ubicacion,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo,COUNT(*) solicitudes
FROM interaccion i
JOIN contenedor c ON c.id_contenedor=i.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
LEFT JOIN material m ON m.id_material=i.id_material_confirmado
GROUP BY TRUNC(CAST(FROM_TZ(i.fecha_inicio_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;

CREATE OR REPLACE VIEW ea_r_depositos AS
SELECT dia,id_contenedor,caneca,id_punto_instalacion,ubicacion,material,depositos,verificados,
       ciudadanos_del_dia,con_peso_medido,kg_medidos,unidades_verificadas,verificados_sin_conteo
FROM ea_r_base_depositos WHERE es_ensayo=0 WITH READ ONLY;
CREATE OR REPLACE VIEW ea_r_puntos AS
SELECT dia,id_contenedor,caneca,id_punto_instalacion,ubicacion,acreditados,reversos,netos
FROM ea_r_base_puntos WHERE es_ensayo=0 WITH READ ONLY;

-- Ausencia de marcas de ensayo NO demuestra procedencia física.
-- ES_ENSAYO=1 incluye demo explícita y fixtures conocidos; nunca son producción institucional.
SELECT object_name,status FROM user_objects WHERE object_name LIKE 'EA_R_%' ORDER BY object_name;
