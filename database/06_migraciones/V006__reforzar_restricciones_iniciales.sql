-- Refuerzo aditivo para instalaciones locales creadas con el DDL inicial antiguo.
-- Ejecutar como dueño del esquema. No borra ni corrige registros.
-- Conserva las restricciones previas y añade las reglas actuales con nombres propios.
-- La prevalidación completa ocurre antes del primer ALTER. Si hay datos incompatibles,
-- detenerse y revisar sus IDs; no normalizarlos automáticamente ni eliminar historia.
-- Oracle confirma DDL implícitamente: un fallo posterior puede dejar una instalación
-- parcial. El script permite reanudar y rechaza una restricción homónima distinta.
-- Reversión: no automática; retirar protecciones requiere decisión específica.
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE
DECLARE
    TYPE regla IS RECORD (tabla VARCHAR2(30), nombre VARCHAR2(30), condicion VARCHAR2(1000));
    TYPE reglas IS TABLE OF regla INDEX BY PLS_INTEGER;
    plan reglas;
    n NUMBER;
    tabla_real VARCHAR2(128);
    tipo_real VARCHAR2(1);
    estado_real VARCHAR2(20);
    validada_real VARCHAR2(20);
    condicion_real VARCHAR2(4000);
BEGIN
    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name='MATERIAL' AND column_name='CODIGO' AND nullable='N';
    IF n <> 1 THEN RAISE_APPLICATION_ERROR(-20074, 'V006 requiere MATERIAL.CODIGO NOT NULL. Revisar sin alterar datos.'); END IF;
    plan(1).tabla := 'ROL'; plan(1).nombre := 'CK_V006_ROL_DESCRIPCION';
    plan(1).condicion := 'TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion)';
    plan(2).tabla := 'USUARIO'; plan(2).nombre := 'CK_V006_USUARIO_NOMBRE';
    plan(2).condicion := 'TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2';
    plan(3).tabla := 'MATERIAL'; plan(3).nombre := 'CK_V006_MATERIAL_NOMBRE';
    plan(3).condicion := plan(2).condicion;
    plan(4).tabla := 'PUNTO_INSTALACION'; plan(4).nombre := 'CK_V006_PUNTO_NOMBRE';
    plan(4).condicion := plan(2).condicion;
    plan(5).tabla := 'CONTENEDOR'; plan(5).nombre := 'CK_V006_CONTENEDOR_NOMBRE';
    plan(5).condicion := plan(2).condicion;
    plan(6).tabla := 'MATERIAL'; plan(6).nombre := 'CK_V006_MATERIAL_CODIGO';
    plan(6).condicion := q'[LENGTH(codigo) >= 2 AND INSTR('ABCDEFGHIJKLMNOPQRSTUVWXYZ', SUBSTR(codigo, 1, 1)) > 0 AND TRANSLATE(codigo, '#ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_', '#') IS NULL]';

    FOR i IN 1..plan.COUNT LOOP
        SELECT COUNT(*) INTO n FROM user_tables WHERE table_name=plan(i).tabla;
        IF n <> 1 THEN RAISE_APPLICATION_ERROR(-20071, 'Falta tabla requerida por V006: ' || plan(i).tabla); END IF;
        EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM ' || plan(i).tabla || ' WHERE NOT (' || plan(i).condicion || ')' INTO n;
        IF n <> 0 THEN
            RAISE_APPLICATION_ERROR(-20072, 'Datos incompatibles en ' || plan(i).tabla || ': ' || n || '. No se modifico nada.');
        END IF;
        SELECT COUNT(*) INTO n FROM user_constraints WHERE constraint_name=plan(i).nombre;
        IF n=1 THEN
            SELECT table_name, constraint_type, status, validated, search_condition_vc
            INTO tabla_real, tipo_real, estado_real, validada_real, condicion_real
            FROM user_constraints WHERE constraint_name=plan(i).nombre;
            IF tabla_real <> plan(i).tabla OR tipo_real <> 'C' OR estado_real <> 'ENABLED'
               OR validada_real <> 'VALIDATED' OR condicion_real IS NULL
               OR TRIM(condicion_real) <> TRIM(plan(i).condicion) THEN
                RAISE_APPLICATION_ERROR(-20073, 'Restriccion homonima incompatible: ' || plan(i).nombre || '. Revisar sin borrar.');
            END IF;
        END IF;
    END LOOP;

    FOR i IN 1..plan.COUNT LOOP
        SELECT COUNT(*) INTO n FROM user_constraints WHERE constraint_name=plan(i).nombre;
        IF n=0 THEN
            EXECUTE IMMEDIATE 'ALTER TABLE ' || plan(i).tabla || ' ADD CONSTRAINT ' || plan(i).nombre
                || ' CHECK (' || plan(i).condicion || ') ENABLE VALIDATE';
            DBMS_OUTPUT.PUT_LINE('Creada y validada: ' || plan(i).nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('Ya verificada, se conserva: ' || plan(i).nombre);
        END IF;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('V006 completa. Sin cambios en filas existentes.');
END;
/
WHENEVER SQLERROR CONTINUE
