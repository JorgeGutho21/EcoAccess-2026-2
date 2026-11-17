-- Vistas de informes para Power BI: dimension de materiales y ciudadanos unicos por mes. Solo lectura y agregados.
-- No concede privilegios, no toca tablas ni expone identidades.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
CREATE OR REPLACE VIEW ea_r_materiales AS
SELECT m.codigo material, m.nombre,
       CASE m.categoria WHEN 1 THEN 'Aprovechable' WHEN 2 THEN TO_CHAR(UNISTR('Org\00E1nico aprovechable')) WHEN 3 THEN 'No aprovechable' ELSE 'Peligroso o especial' END categoria,
       m.habilitado
FROM material m
WITH READ ONLY;

CREATE OR REPLACE VIEW ea_r_participacion_mensual AS
SELECT TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE),'MM') mes,
       CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END es_ensayo,
       COUNT(DISTINCT i.id_usuario) ciudadanos_unicos,
       COUNT(*) depositos
FROM deposito d
JOIN orden_apertura o ON o.id_orden=d.id_orden
JOIN interaccion i ON i.id_interaccion=o.id_interaccion
JOIN contenedor c ON c.id_contenedor=o.id_contenedor
JOIN punto_instalacion p ON p.id_punto_instalacion=c.id_punto_instalacion
JOIN material m ON m.id_material=d.id_material_declarado
GROUP BY TRUNC(CAST(FROM_TZ(d.fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE),'MM'),
         CASE WHEN i.canal IN (3,4) OR UPPER(c.nombre) LIKE '%FICTIC%' OR UPPER(p.nombre) LIKE '%SIMULACI%' OR REGEXP_LIKE(m.codigo,'^(BR_|IT_|TEST_|PRUEBA_)') THEN 1 ELSE 0 END
WITH READ ONLY;

COMMENT ON TABLE ea_r_participacion_mensual IS 'Ciudadanos distintos por mes. No es aditivo: no sumar meses.';
