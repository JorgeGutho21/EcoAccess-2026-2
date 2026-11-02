-- Auditoría de solo lectura del perfil autenticado; no cambia esquema ni datos.
SET LINESIZE 200 PAGESIZE 100
SELECT SYS_CONTEXT('USERENV','CON_NAME') pdb, USER esquema FROM dual;
SELECT table_name FROM user_tables ORDER BY table_name;
SELECT status, COUNT(*) objetos FROM user_objects GROUP BY status;
SELECT status, validated, COUNT(*) restricciones FROM user_constraints GROUP BY status, validated;
SELECT 'usuarios' entidad, COUNT(*) cantidad FROM usuario
UNION ALL SELECT 'materiales', COUNT(*) FROM material
UNION ALL SELECT 'canecas', COUNT(*) FROM contenedor
UNION ALL SELECT 'interacciones', COUNT(*) FROM interaccion
UNION ALL SELECT 'ordenes', COUNT(*) FROM orden_apertura
UNION ALL SELECT 'depositos', COUNT(*) FROM deposito
UNION ALL SELECT 'movimientos_ecopuntos', COUNT(*) FROM movimiento_ecopuntos;
