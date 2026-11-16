-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q04 · ¿Qué material deposita con mayor frecuencia cada usuario?
-- JOIN: Igual que Q03, con MATERIAL por el material declarado.
-- FK: FK_DEPOSITO_MAT_DECL, FK_ORDEN_INTERACCION

SELECT usuario, material, depositos
FROM  (SELECT 'C-' || LPAD(i.id_usuario, 4, '0') AS usuario,
              m.codigo                           AS material,
              COUNT(*)                           AS depositos,
              ROW_NUMBER() OVER (PARTITION BY i.id_usuario ORDER BY COUNT(*) DESC, m.codigo) AS puesto
       FROM   deposito d
       JOIN   orden_apertura o ON o.id_orden       = d.id_orden
       JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
       JOIN   material m       ON m.id_material    = d.id_material_declarado
       WHERE  i.id_usuario IS NOT NULL
       GROUP  BY i.id_usuario, m.codigo)
WHERE  puesto = 1
ORDER  BY depositos DESC, usuario;
