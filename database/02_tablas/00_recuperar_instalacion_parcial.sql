-- =====================================================================
-- EcoAccess P3 — recuperación de una instalación PARCIAL de la versión 001
-- Archivo: database/02_tablas/00_recuperar_instalacion_parcial.sql
--
-- SOLO PARA DESARROLLO. Úsalo únicamente cuando 01_crear_tablas_iniciales.sql
-- se detuvo con ORA-20011 (existen entre 1 y 6 de las 7 tablas) porque la
-- primera ejecución falló a mitad de camino.
--
-- BORRA DATOS: elimina las tablas iniciales que existan, con sus filas, sin
-- pasar por la papelera (PURGE). No toca otras tablas del esquema.
--
-- Protecciones:
--   * Se niega a actuar si están las 7 tablas (instalación completa) o si no
--     hay ninguna: en esos casos no hay nada que recuperar.
--   * Se niega a actuar si alguna tabla tiene filas, salvo los catálogos ROL y
--     MATERIAL, que 01_catalogos.sql vuelve a cargar.
--
-- Después de ejecutarlo, vuelve a correr 01_crear_tablas_iniciales.sql.
-- Estado: escrito y revisado; pendiente de ejecución en el PC local.
-- =====================================================================

SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

DECLARE
    TYPE t_nombres IS TABLE OF VARCHAR2(30);
    -- Orden inverso a la creación: primero las hijas, después las padres.
    v_tablas t_nombres := t_nombres('CONTENEDOR_MATERIAL', 'CONTENEDOR', 'PUNTO_INSTALACION',
                                    'MATERIAL', 'USUARIO_ROL', 'USUARIO', 'ROL');
    v_existentes NUMBER;
    v_filas      NUMBER;
    v_existe     NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_existentes
    FROM   user_tables
    WHERE  table_name IN ('ROL', 'USUARIO', 'USUARIO_ROL', 'MATERIAL',
                          'PUNTO_INSTALACION', 'CONTENEDOR', 'CONTENEDOR_MATERIAL');

    IF v_existentes = 0 THEN
        RAISE_APPLICATION_ERROR(-20012, 'No existe ninguna tabla inicial: no hay nada que recuperar.');
    ELSIF v_existentes = 7 THEN
        RAISE_APPLICATION_ERROR(-20013, 'Las siete tablas existen: la instalación está completa y no se borra nada.');
    END IF;

    -- Primero se revisa todo; solo si nada tiene datos de uso se borra.
    FOR i IN 1 .. v_tablas.COUNT LOOP
        SELECT COUNT(*) INTO v_existe FROM user_tables WHERE table_name = v_tablas(i);
        IF v_existe = 1 AND v_tablas(i) NOT IN ('ROL', 'MATERIAL') THEN
            EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ' || DBMS_ASSERT.SIMPLE_SQL_NAME(v_tablas(i)) INTO v_filas;
            IF v_filas > 0 THEN
                RAISE_APPLICATION_ERROR(-20014,
                    'La tabla ' || v_tablas(i) || ' tiene ' || v_filas || ' filas. No se borró nada.');
            END IF;
        END IF;
    END LOOP;

    FOR i IN 1 .. v_tablas.COUNT LOOP
        SELECT COUNT(*) INTO v_existe FROM user_tables WHERE table_name = v_tablas(i);
        IF v_existe = 1 THEN
            EXECUTE IMMEDIATE 'DROP TABLE ' || DBMS_ASSERT.SIMPLE_SQL_NAME(v_tablas(i)) || ' CASCADE CONSTRAINTS PURGE';
            DBMS_OUTPUT.PUT_LINE('Borrada ' || v_tablas(i));
        END IF;
    END LOOP;

    DBMS_OUTPUT.PUT_LINE('Recuperación terminada. Ejecuta de nuevo 01_crear_tablas_iniciales.sql.');
END;
/

WHENEVER SQLERROR CONTINUE
