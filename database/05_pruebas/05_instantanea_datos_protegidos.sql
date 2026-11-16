-- EcoAccess P3: huella de solo lectura de los datos que una migración aditiva no debe tocar (ciudadanos, credenciales,
-- roles, vinculaciones de Telegram, solicitudes, depósitos, ecopuntos, catálogo y canecas). Se ejecuta antes y después
-- de aplicar una migración; si alguna fila cambia, cambia su conteo o su ORA_ROWSCN máximo (el SCN del bloque sube con
-- cualquier INSERT, UPDATE o DELETE). Ninguna sentencia escribe.
-- Nota: el bot en marcha puede escribir conversacion_telegram o vinculacion_pendiente mientras tanto; por eso no están
-- en la lista. Si cambia una tabla de la lista, revisar si hubo actividad real (un registro nuevo) antes de culpar a la
-- migración.
SET PAGESIZE 100 LINESIZE 160 FEEDBACK OFF
COLUMN tabla FORMAT A24
SELECT 'USUARIO' tabla, COUNT(*) filas, MAX(ORA_ROWSCN) scn FROM usuario
UNION ALL SELECT 'CREDENCIAL_ACCESO', COUNT(*), MAX(ORA_ROWSCN) FROM credencial_acceso
UNION ALL SELECT 'USUARIO_ROL', COUNT(*), MAX(ORA_ROWSCN) FROM usuario_rol
UNION ALL SELECT 'VINCULO_TELEGRAM', COUNT(*), MAX(ORA_ROWSCN) FROM vinculo_telegram
UNION ALL SELECT 'INTERACCION', COUNT(*), MAX(ORA_ROWSCN) FROM interaccion
UNION ALL SELECT 'ORDEN_APERTURA', COUNT(*), MAX(ORA_ROWSCN) FROM orden_apertura
UNION ALL SELECT 'DEPOSITO', COUNT(*), MAX(ORA_ROWSCN) FROM deposito
UNION ALL SELECT 'REGLA_ECOPUNTOS', COUNT(*), MAX(ORA_ROWSCN) FROM regla_ecopuntos
UNION ALL SELECT 'MOVIMIENTO_ECOPUNTOS', COUNT(*), MAX(ORA_ROWSCN) FROM movimiento_ecopuntos
UNION ALL SELECT 'MATERIAL', COUNT(*), MAX(ORA_ROWSCN) FROM material
UNION ALL SELECT 'CONTENEDOR', COUNT(*), MAX(ORA_ROWSCN) FROM contenedor
UNION ALL SELECT 'CONTENEDOR_MATERIAL', COUNT(*), MAX(ORA_ROWSCN) FROM contenedor_material
UNION ALL SELECT 'PUNTO_INSTALACION', COUNT(*), MAX(ORA_ROWSCN) FROM punto_instalacion
ORDER BY 1;
-- Estructura: objetos y restricciones existentes (una migración aditiva solo puede sumar, nunca cambiar estos).
SELECT object_type, COUNT(*) objetos, SUM(CASE WHEN status <> 'VALID' THEN 1 ELSE 0 END) invalidos
FROM user_objects GROUP BY object_type ORDER BY 1;
SELECT COUNT(*) restricciones, SUM(CASE WHEN status <> 'ENABLED' THEN 1 ELSE 0 END) deshabilitadas FROM user_constraints;
