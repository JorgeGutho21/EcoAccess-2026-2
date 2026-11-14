-- =====================================================================================
-- Inventario de Oracle Cloud (Programación III) — SOLO LECTURA
-- Ejecutar en SQL Developer con la conexión «EcoAccess Cloud auditoria»
-- (usuario ECOACCESS_P3_APP, alias TNS ecoaccess_tp, wallet en TNS_ADMIN).
-- La contraseña la escribe la persona dueña de la cuenta en la ventana de SQL Developer.
-- Este script SOLO usa SELECT: no crea, no modifica y no borra nada.
-- Pulse F5 (Ejecutar script) y guarde la salida como evidencia.
-- =====================================================================================

-- 1-5. Identidad del entorno (nombre técnico real, servicio, esquema, versión)
SELECT SYS_CONTEXT('USERENV','DB_NAME')        AS nombre_bd,
       SYS_CONTEXT('USERENV','SERVICE_NAME')   AS servicio,
       SYS_CONTEXT('USERENV','CURRENT_SCHEMA') AS esquema,
       SYS_CONTEXT('USERENV','CON_NAME')       AS contenedor
FROM dual;
SELECT banner_full FROM v$version;

-- 6. Tablas
SELECT table_name, num_rows FROM user_tables ORDER BY table_name;
SELECT COUNT(*) AS total_tablas FROM user_tables;

-- 7. Relaciones y restricciones
SELECT constraint_type, COUNT(*) AS total FROM user_constraints
WHERE constraint_name NOT LIKE 'BIN$%' GROUP BY constraint_type ORDER BY 1;
SELECT c.table_name, c.constraint_name, r.table_name AS tabla_referenciada
FROM user_constraints c JOIN user_constraints r ON r.constraint_name = c.r_constraint_name
WHERE c.constraint_type = 'R' ORDER BY 1, 2;

-- 8-12. Vistas, procedimientos, funciones, paquetes y triggers
SELECT object_type, COUNT(*) AS total FROM user_objects
WHERE object_type IN ('VIEW','PROCEDURE','FUNCTION','PACKAGE','PACKAGE BODY','TRIGGER','SEQUENCE','INDEX')
GROUP BY object_type ORDER BY 1;
SELECT view_name FROM user_views ORDER BY 1;
SELECT object_name, object_type FROM user_objects
WHERE object_type IN ('PROCEDURE','FUNCTION','PACKAGE','TRIGGER') ORDER BY 2, 1;

-- 13. Migraciones instaladas
SELECT * FROM eco_migracion_aplicada ORDER BY 1;

-- 14. Objetos inválidos
SELECT object_name, object_type, status FROM user_objects WHERE status <> 'VALID';

-- 15. Estado de las tres categorías de canecas (solo conteos, sin datos personales)
SELECT c.categoria, c.estado, COUNT(*) AS canecas FROM contenedor c GROUP BY c.categoria, c.estado ORDER BY 1, 2;

-- 16. Estado de depósitos y EcoPuntos (agregados, sin identidades)
SELECT d.estado, COUNT(*) AS depositos FROM deposito d GROUP BY d.estado ORDER BY 1;
SELECT COUNT(*) AS movimientos, SUM(puntos) AS puntos_netos FROM movimiento_ecopuntos;
SELECT * FROM ea_r_participacion_mensual ORDER BY mes;
