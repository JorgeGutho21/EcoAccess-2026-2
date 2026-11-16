-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q08 · ¿Cuáles depósitos fueron rechazados y por qué?
-- JOIN: Igual que Q07, con el material declarado.
-- FK: FK_ORDEN_INTERACCION, FK_DEPOSITO_MAT_DECL

SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')    AS titular,
       md.codigo                             AS material_declarado,
       'C-' || LPAD(d.id_verificador, 4, '0') AS verificador,
       d.motivo_verificacion
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material md      ON md.id_material   = d.id_material_declarado
WHERE  d.estado_verificacion = 2
ORDER  BY d.id_deposito;
