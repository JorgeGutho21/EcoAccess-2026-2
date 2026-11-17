-- 14_EXCEPCIONES — 14 ejercicios del banco verificado (conexión: ECOACCESS_BD)
-- Generado por 05_SQL_Y_PLSQL/generar_carpetas_por_tema.ps1 desde 08_CONSULTAS_Y_EJERCICIOS. No editar a mano.
-- Selecciona un ejercicio y pulsa Ctrl+Enter (consulta) o F5 (bloque PL/SQL). Los que modifican datos terminan en ROLLBACK.
SET SERVEROUTPUT ON SIZE UNLIMITED

-- ===== E248 · 16. Excepciones · NO_DATA_FOUND
-- Objetivo: SELECT INTO sin filas.
-- Explicación: Se captura y se da un mensaje amable.
DECLARE
    v material.nombre%TYPE;
BEGIN
    SELECT nombre INTO v FROM material WHERE codigo = 'NO_EXISTE';
EXCEPTION
    WHEN NO_DATA_FOUND THEN DBMS_OUTPUT.PUT_LINE('No existe ese material.');
END;
/

-- ===== E249 · 16. Excepciones · TOO_MANY_ROWS
-- Objetivo: SELECT INTO con varias filas.
-- Explicación: SELECT INTO solo admite una fila.
DECLARE
    v material.nombre%TYPE;
BEGIN
    SELECT nombre INTO v FROM material WHERE categoria = 1;
EXCEPTION
    WHEN TOO_MANY_ROWS THEN DBMS_OUTPUT.PUT_LINE('Hay varios materiales blancos: usa un cursor.');
END;
/

-- ===== E250 · 16. Excepciones · ZERO_DIVIDE
-- Objetivo: División por cero.
-- Explicación: Excepción predefinida de Oracle.
DECLARE
    v NUMBER;
BEGIN
    v := 10 / 0;
EXCEPTION
    WHEN ZERO_DIVIDE THEN DBMS_OUTPUT.PUT_LINE('No se puede dividir entre cero.');
END;
/

-- ===== E251 · 16. Excepciones · Excepción propia
-- Objetivo: Declarar y lanzar una excepción.
-- Explicación: RAISE dispara la excepción declarada.
DECLARE
    e_peso_invalido EXCEPTION;
    v_kg NUMBER := -1;
BEGIN
    IF v_kg <= 0 THEN RAISE e_peso_invalido; END IF;
EXCEPTION
    WHEN e_peso_invalido THEN DBMS_OUTPUT.PUT_LINE('El peso debe ser positivo.');
END;
/

-- ===== E252 · 16. Excepciones · RAISE_APPLICATION_ERROR
-- Objetivo: Error con número y mensaje propio.
-- Explicación: Rango permitido: -20000 a -20999. Así funcionan los triggers del proyecto.
BEGIN
    RAISE_APPLICATION_ERROR(-20555, 'Ejemplo de error de negocio');
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('Capturado: ' || SQLCODE || ' ' || SQLERRM);
END;
/

-- ===== E253 · 16. Excepciones · DUP_VAL_ON_INDEX
-- Objetivo: Capturar un duplicado.
-- Explicación: Excepción de Oracle para ORA-00001.
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Duplicado', 1);
EXCEPTION
    WHEN DUP_VAL_ON_INDEX THEN DBMS_OUTPUT.PUT_LINE('Ese código ya existe.');
END;
/

-- ===== E254 · 16. Excepciones · PRAGMA EXCEPTION_INIT
-- Objetivo: Ponerle nombre a un error de Oracle.
-- Explicación: Asocia ORA-02291 a un nombre legible.
DECLARE
    e_padre_no_existe EXCEPTION;
    PRAGMA EXCEPTION_INIT(e_padre_no_existe, -2291);
BEGIN
    INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (999, 'Fantasma', 1);
EXCEPTION
    WHEN e_padre_no_existe THEN DBMS_OUTPUT.PUT_LINE('La estación 999 no existe.');
END;
/

-- ===== E255 · 16. Excepciones · WHEN OTHERS con SQLERRM
-- Objetivo: Atrapar cualquier error y explicarlo.
-- Explicación: Úsalo con cuidado: no escondas errores.
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('x', 'minúscula', 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('Error ' || SQLCODE || ': ' || SUBSTR(SQLERRM, 1, 80));
END;
/

-- ===== E256 · 16. Excepciones · VALUE_ERROR
-- Objetivo: Convertir texto que no es número.
-- Explicación: En PL/SQL, TO_NUMBER('abc') produce VALUE_ERROR.
DECLARE
    v NUMBER;
BEGIN
    v := TO_NUMBER('abc');
EXCEPTION
    WHEN VALUE_ERROR THEN DBMS_OUTPUT.PUT_LINE('No es un número: se atrapó VALUE_ERROR.');
END;
/

-- ===== E257 · 16. Excepciones · SQLCODE
-- Objetivo: Leer el número del error.
-- Explicación: SQLCODE es negativo para errores de Oracle.
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Repetido', 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('SQLCODE = ' || SQLCODE); ROLLBACK;
END;
/

-- ===== E258 · 16. Excepciones · Propagación
-- Objetivo: Un error interno sube al bloque de afuera.
-- Explicación: Si el bloque interno no lo atrapa, lo maneja el externo.
BEGIN
    BEGIN
        RAISE NO_DATA_FOUND;
    END;
    DBMS_OUTPUT.PUT_LINE('Esta línea no se ejecuta.');
EXCEPTION
    WHEN NO_DATA_FOUND THEN DBMS_OUTPUT.PUT_LINE('El bloque externo atrapó el error del interno.');
END;
/

-- ===== E259 · 16. Excepciones · Atrapar un error de negocio
-- Objetivo: Capturar ORA-20813 de la función de canecas.
-- Explicación: PRAGMA EXCEPTION_INIT da nombre a un código propio.
DECLARE
    e_peligroso EXCEPTION;
    PRAGMA EXCEPTION_INIT(e_peligroso, -20813);
    v NUMBER;
BEGIN
    SELECT MIN(id_material) INTO v FROM material WHERE categoria = 4;
    v := fn_resolver_caneca_disponible(1, v);
EXCEPTION
    WHEN e_peligroso THEN DBMS_OUTPUT.PUT_LINE('Residuo peligroso: va por ruta especial, no a una caneca.');
END;
/

-- ===== E260 · 16. Excepciones · Volver a lanzar (RAISE)
-- Objetivo: Registrar el error y dejar que siga.
-- Explicación: RAISE sin nombre relanza la misma excepción. ERROR ESPERADO.
BEGIN
    UPDATE material SET categoria = 9 WHERE codigo = 'PET';
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Registro: ' || SQLERRM);
        RAISE;
END;
/

-- ===== E261 · 16. Excepciones · Dónde ocurrió el error
-- Objetivo: FORMAT_ERROR_BACKTRACE indica la línea.
-- Explicación: Muy útil para depurar paquetes grandes.
DECLARE
    v NUMBER;
BEGIN
    v := 1 / 0;
EXCEPTION
    WHEN ZERO_DIVIDE THEN DBMS_OUTPUT.PUT_LINE('Error en: ' || TRIM(DBMS_UTILITY.FORMAT_ERROR_BACKTRACE));
END;
/
