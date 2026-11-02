-- Solo lectura. Ejecutar como ECOACCESS_P3 en FREEPDB1.
-- Fixture creado realmente desde FormUsuarios el 2026-10-27, sin datos personales reales.
SET LINESIZE 220
SET PAGESIZE 60
COLUMN esquema FORMAT A20
COLUMN servicio FORMAT A20
COLUMN nombre FORMAT A40
COLUMN correo_electronico FORMAT A38
SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') AS esquema,
       SYS_CONTEXT('USERENV','SERVICE_NAME') AS servicio FROM dual;

SELECT u.id_usuario, u.nombre, u.correo_electronico, u.activo,
       TO_CHAR(u.fecha_registro_utc, 'YYYY-MM-DD HH24:MI:SS') AS registro_utc,
       (SELECT COUNT(*) FROM credencial_acceso c WHERE c.id_usuario=u.id_usuario) AS credenciales
FROM usuario u WHERE u.correo_electronico='gui.271026.p3@ejemplo.com';

SELECT u.id_usuario, r.codigo AS codigo_rol
FROM usuario u JOIN usuario_rol ur ON ur.id_usuario=u.id_usuario
JOIN rol r ON r.id_rol=ur.id_rol
WHERE u.correo_electronico='gui.271026.p3@ejemplo.com'
ORDER BY r.codigo;
