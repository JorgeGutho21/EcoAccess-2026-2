-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
-- Q01 · ¿Qué usuarios están registrados y qué roles tienen?
-- JOIN: USUARIO → USUARIO_ROL → ROL (LEFT JOIN: también aparece quien no tenga rol).
-- FK: FK_USUARIO_ROL_USUARIO, FK_USUARIO_ROL_ROL

SELECT u.id_usuario,
       'C-' || LPAD(u.id_usuario, 4, '0')                       AS seudonimo,
       u.activo,
       u.es_sintetico,
       TO_CHAR(u.fecha_registro_utc, 'YYYY-MM-DD')               AS fecha_registro,
       LISTAGG(r.descripcion, ', ') WITHIN GROUP (ORDER BY r.codigo) AS roles
FROM   usuario u
LEFT   JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario
LEFT   JOIN rol r          ON r.id_rol      = ur.id_rol
GROUP  BY u.id_usuario, u.activo, u.es_sintetico, u.fecha_registro_utc
ORDER  BY u.id_usuario;
