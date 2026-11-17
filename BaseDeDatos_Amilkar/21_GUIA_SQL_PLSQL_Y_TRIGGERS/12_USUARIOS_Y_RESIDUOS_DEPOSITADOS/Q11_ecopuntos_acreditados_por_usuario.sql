-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q11 · ¿Cuántos EcoPuntos se acreditaron a cada usuario?
-- JOIN: Ninguno: MOVIMIENTO_ECOPUNTOS ya tiene id_usuario.
-- FK: FK_MOV_USUARIO (MOVIMIENTO_ECOPUNTOS.id_usuario → USUARIO)

SELECT 'C-' || LPAD(mv.id_usuario, 4, '0')                     AS usuario,
       SUM(CASE WHEN mv.tipo = 1 THEN mv.puntos ELSE 0 END)    AS creditos,
       SUM(CASE WHEN mv.tipo = 2 THEN mv.puntos ELSE 0 END)    AS reversos,
       SUM(mv.puntos)                                          AS saldo
FROM   movimiento_ecopuntos mv
GROUP  BY mv.id_usuario
ORDER  BY saldo DESC, usuario;
