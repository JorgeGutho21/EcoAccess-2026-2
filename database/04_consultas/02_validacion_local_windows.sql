-- Abrir en SQL Developer conectado como ECOACCESS_P3, localhost:1522/FREEPDB1.
-- Ejecutar como script (F5). Solo SELECT; no incluye claves ni datos privados reales.
SET PAGESIZE 100
SET LINESIZE 180
SELECT SYS_CONTEXT('USERENV','SESSION_USER') esquema, SYS_CONTEXT('USERENV','CON_NAME') pdb FROM dual;
SELECT id_material, codigo, nombre, habilitado FROM material WHERE codigo='WIN261026';
SELECT id_punto_instalacion, nombre, activo FROM punto_instalacion WHERE nombre='Punto ficticio validacion Windows';
SELECT id_contenedor, nombre, id_punto_instalacion, estado FROM contenedor WHERE nombre='Caneca ficticia validacion Windows';
SELECT u.id_usuario, u.nombre, u.activo, r.codigo rol
FROM usuario u JOIN usuario_rol ur ON ur.id_usuario=u.id_usuario JOIN rol r ON r.id_rol=ur.id_rol
WHERE u.correo_electronico IN ('admin.demo@ejemplo.com','ciudadano.windows@ejemplo.com') ORDER BY u.id_usuario,r.codigo;
SELECT id_interaccion, id_contenedor, estado FROM interaccion ORDER BY id_interaccion FETCH FIRST 20 ROWS ONLY;
SELECT id_deposito, id_material_declarado, id_material_verificado, peso_medido_kg FROM deposito ORDER BY id_deposito FETCH FIRST 20 ROWS ONLY;
SELECT id_usuario, SUM(puntos) saldo FROM movimiento_ecopuntos GROUP BY id_usuario;
SELECT constraint_name, status, validated FROM user_constraints WHERE constraint_name LIKE 'CK_V006_%' ORDER BY constraint_name;
SELECT table_name, constraint_name, status, validated FROM user_constraints WHERE status<>'ENABLED' OR validated<>'VALIDATED';
SELECT object_name, object_type FROM user_objects WHERE status<>'VALID';
