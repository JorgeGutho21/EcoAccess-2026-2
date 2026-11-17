-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q07 · ¿Cuáles depósitos fueron verificados y por quién?
-- JOIN: DEPOSITO → ORDEN_APERTURA → INTERACCION (titular); DEPOSITO → MATERIAL (verificado).
-- FK: FK_DEPOSITO_VERIFICADOR (DEPOSITO.id_verificador → USUARIO), FK_DEPOSITO_MAT_VERIF

SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')    AS titular,
       'C-' || LPAD(d.id_verificador, 4, '0') AS verificador,
       mv.codigo                             AS material_verificado,
       d.cantidad_verificada,
       TO_CHAR(d.fecha_verificacion_utc, 'YYYY-MM-DD HH24:MI') AS verificado_utc
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material mv      ON mv.id_material   = d.id_material_verificado
WHERE  d.estado_verificacion = 1
ORDER  BY d.fecha_verificacion_utc;
