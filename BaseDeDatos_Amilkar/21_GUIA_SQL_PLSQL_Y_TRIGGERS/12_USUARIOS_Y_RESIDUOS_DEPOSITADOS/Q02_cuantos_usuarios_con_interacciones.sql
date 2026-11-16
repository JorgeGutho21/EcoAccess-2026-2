-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q02 · ¿Cuántos usuarios han realizado interacciones (sesiones QR)?
-- JOIN: Ninguno: se cuenta sobre INTERACCION.
-- FK: INTERACCION.id_usuario es FK a USUARIO (FK_INTERACCION_USUARIO) y admite NULL.

SELECT COUNT(*)                        AS interacciones,
       COUNT(i.id_usuario)             AS interacciones_con_usuario,
       COUNT(*) - COUNT(i.id_usuario)  AS interacciones_de_visitante,
       COUNT(DISTINCT i.id_usuario)    AS usuarios_distintos
FROM   interaccion i;
