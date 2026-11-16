-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q15 · ¿Cuántas fotos consultó cada usuario frente a cuántos depósitos hizo?
-- JOIN: Subconsultas escalares correlacionadas: una sobre CONSULTA_VISION y otra sobre la cadena del depósito.
-- FK: FK_CONSULTA_USUARIO, FK_ORDEN_INTERACCION

SELECT u.id_usuario,
       'C-' || LPAD(u.id_usuario, 4, '0') AS usuario,
       (SELECT COUNT(*) FROM consulta_vision cv WHERE cv.id_usuario = u.id_usuario) AS fotos_consultadas,
       (SELECT COUNT(*)
        FROM   deposito d
        JOIN   orden_apertura o ON o.id_orden       = d.id_orden
        JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
        WHERE  i.id_usuario = u.id_usuario)                                       AS depositos
FROM   usuario u
ORDER  BY fotos_consultadas DESC, u.id_usuario
FETCH  FIRST 15 ROWS ONLY;
