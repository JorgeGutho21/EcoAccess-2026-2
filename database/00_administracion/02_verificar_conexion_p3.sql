-- =====================================================================
-- EcoAccess P3 — verificación de la conexión y los permisos del esquema
-- Archivo: database/00_administracion/02_verificar_conexion_p3.sql
--
-- Ejecutar conectado como ECOACCESS_P3 al PDB FREEPDB1 (F5 en SQL Developer).
-- Comprueba: usuario y contenedor correctos, versión de Oracle, y que el
-- esquema puede crear una tabla, insertar, consultar y borrarla.
-- Solo borra la tabla de prueba que él mismo crea.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

-- 1. ¿Quién soy y dónde estoy? Esperado: ECOACCESS_P3 y FREEPDB1.
SELECT USER                              AS usuario,
       SYS_CONTEXT('USERENV', 'CON_NAME') AS contenedor
FROM   dual;

-- 2. Comprobación del resultado.
SELECT product, version_full
FROM   product_component_version;

-- 3. Ciclo mínimo de escritura con la tabla de prueba.
CREATE TABLE prueba_conexion_p3 (
    id      NUMBER        CONSTRAINT pk_prueba_conexion_p3 PRIMARY KEY,
    detalle VARCHAR2(50 CHAR) NOT NULL
);

INSERT INTO prueba_conexion_p3 (id, detalle) VALUES (1, 'conexión verificada, ñandú');
COMMIT;

SELECT id, detalle FROM prueba_conexion_p3;

DROP TABLE prueba_conexion_p3 PURGE;

BEGIN
    DBMS_OUTPUT.PUT_LINE('Verificación completa: el esquema ECOACCESS_P3 puede crear, escribir y leer.');
END;
/

WHENEVER SQLERROR CONTINUE
