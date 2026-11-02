-- Solo lectura como ECOACCESS_P3. No aplica migraciones ni modifica datos.
SET PAGESIZE 100
SET LINESIZE 180
SELECT SYS_CONTEXT('USERENV','SESSION_USER') esquema, SYS_CONTEXT('USERENV','CON_NAME') pdb FROM dual;
SELECT banner_full FROM v$version WHERE ROWNUM=1;
SELECT COUNT(*) tablas FROM user_tables;
SELECT COUNT(*) objetos_invalidos FROM user_objects WHERE status<>'VALID';
SELECT COUNT(*) restricciones_sin_validar FROM user_constraints WHERE status<>'ENABLED' OR validated<>'VALIDATED';
SELECT COUNT(*) indices_invalidos FROM user_indexes WHERE status NOT IN ('VALID','N/A');
SELECT table_name FROM user_tables ORDER BY table_name;
SELECT constraint_name,status,validated FROM user_constraints WHERE constraint_name LIKE 'CK_V006_%' ORDER BY constraint_name;
SELECT privilege FROM user_sys_privs ORDER BY privilege;
SELECT granted_role FROM user_role_privs ORDER BY granted_role;
SELECT 'usuario' entidad,COUNT(*) filas FROM usuario
UNION ALL SELECT 'material',COUNT(*) FROM material
UNION ALL SELECT 'punto_instalacion',COUNT(*) FROM punto_instalacion
UNION ALL SELECT 'contenedor',COUNT(*) FROM contenedor
UNION ALL SELECT 'interaccion',COUNT(*) FROM interaccion
UNION ALL SELECT 'orden_apertura',COUNT(*) FROM orden_apertura
UNION ALL SELECT 'deposito',COUNT(*) FROM deposito
UNION ALL SELECT 'movimiento_ecopuntos',COUNT(*) FROM movimiento_ecopuntos;
SELECT COUNT(*) depositos_duplicados FROM
 (SELECT id_orden FROM deposito GROUP BY id_orden HAVING COUNT(*)>1);
SELECT COUNT(*) creditos_iniciales_duplicados FROM
 (SELECT id_deposito FROM movimiento_ecopuntos WHERE tipo=1 AND id_predecesor IS NULL GROUP BY id_deposito HAVING COUNT(*)>1);
