-- Informes administrativos de solo lectura. 
-- No concede privilegios, no toca tablas ni modifica datos.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
CREATE OR REPLACE VIEW ea_r_depositos AS
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor, c.nombre caneca, p.id_punto_instalacion, p.nombre ubicacion,
       m.codigo material, COUNT(*) depositos,
       COUNT(d.id_material_verificado) verificados,
       COUNT(DISTINCT i.id_usuario) ciudadanos_del_dia,
       COUNT(d.peso_medido_kg) con_peso_medido, SUM(d.peso_medido_kg) kg_medidos
FROM deposito d
JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
JOIN material m ON m.id_material=d.id_material_declarado
WHERE i.canal <> 3
  AND UPPER(c.nombre) NOT LIKE '%FICTIC%'
  AND UPPER(p.nombre) NOT LIKE '%SIMULACI%'
  AND NOT REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)')
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre,m.codigo
WITH READ ONLY;

CREATE OR REPLACE VIEW ea_r_puntos AS
SELECT TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       c.id_contenedor,c.nombre caneca,p.id_punto_instalacion,p.nombre ubicacion,
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
WHERE i.canal<>3 AND UPPER(c.nombre) NOT LIKE '%FICTIC%'
  AND UPPER(p.nombre) NOT LIKE '%SIMULACI%'
  AND NOT REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)')
GROUP BY TRUNC(CAST(FROM_TZ(op.fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)),
         c.id_contenedor,c.nombre,p.id_punto_instalacion,p.nombre
WITH READ ONLY;

CREATE OR REPLACE VIEW ea_r_opiniones AS
SELECT TRUNC(CAST(FROM_TZ(fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) dia,
       COUNT(*) opiniones,SUM(estrellas) suma_estrellas,
       COUNT(CASE WHEN id_usuario IS NULL THEN 1 END) invitadas
FROM opinion
GROUP BY TRUNC(CAST(FROM_TZ(fecha_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE))
WITH READ ONLY;

-- No expone usuarios, comentarios, fotos, contraseñas ni vínculos.
-- Los ciudadanos del día son NO aditivos entre días/canecas/materiales.
-- Excluir fixtures conocidos no certifica por sí mismo procedencia física.
SELECT object_name,status FROM user_objects
WHERE object_name IN ('EA_R_DEPOSITOS','EA_R_PUNTOS','EA_R_OPINIONES') ORDER BY object_name;
SELECT COUNT(*) AS grupos_depositos FROM ea_r_depositos;
SELECT COUNT(*) AS grupos_puntos FROM ea_r_puntos;
SELECT COUNT(*) AS dias_opiniones FROM ea_r_opiniones;
