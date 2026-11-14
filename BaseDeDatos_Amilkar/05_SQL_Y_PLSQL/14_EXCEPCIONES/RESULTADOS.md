# EXCEPCIONES

Ejercicios: 14 · conexión: `ECOACCESS_BD` · temas del banco: 16. Excepciones.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E248 | NO_DATA_FOUND | OK |
| E249 | TOO_MANY_ROWS | OK |
| E250 | ZERO_DIVIDE | OK |
| E251 | Excepción propia | OK |
| E252 | RAISE_APPLICATION_ERROR | OK |
| E253 | DUP_VAL_ON_INDEX | OK |
| E254 | PRAGMA EXCEPTION_INIT | OK |
| E255 | WHEN OTHERS con SQLERRM | OK |
| E256 | VALUE_ERROR | OK |
| E257 | SQLCODE | OK |
| E258 | Propagación | OK |
| E259 | Atrapar un error de negocio | OK |
| E260 | Volver a lanzar (RAISE) | OK (error esperado) |
| E261 | Dónde ocurrió el error | OK |

## E248 · NO_DATA_FOUND

**Objetivo:** SELECT INTO sin filas.  
**Explicación:** Se captura y se da un mensaje amable.  
**Estado:** OK

```sql
DECLARE
    v material.nombre%TYPE;
BEGIN
    SELECT nombre INTO v FROM material WHERE codigo = 'NO_EXISTE';
EXCEPTION
    WHEN NO_DATA_FOUND THEN DBMS_OUTPUT.PUT_LINE('No existe ese material.');
END;
/
```

Salida real:

```text
No existe ese material.

Procedimiento PL/SQL terminado correctamente.
```

## E249 · TOO_MANY_ROWS

**Objetivo:** SELECT INTO con varias filas.  
**Explicación:** SELECT INTO solo admite una fila.  
**Estado:** OK

```sql
DECLARE
    v material.nombre%TYPE;
BEGIN
    SELECT nombre INTO v FROM material WHERE categoria = 1;
EXCEPTION
    WHEN TOO_MANY_ROWS THEN DBMS_OUTPUT.PUT_LINE('Hay varios materiales blancos: usa un cursor.');
END;
/
```

Salida real:

```text
Hay varios materiales blancos: usa un cursor.

Procedimiento PL/SQL terminado correctamente.
```

## E250 · ZERO_DIVIDE

**Objetivo:** División por cero.  
**Explicación:** Excepción predefinida de Oracle.  
**Estado:** OK

```sql
DECLARE
    v NUMBER;
BEGIN
    v := 10 / 0;
EXCEPTION
    WHEN ZERO_DIVIDE THEN DBMS_OUTPUT.PUT_LINE('No se puede dividir entre cero.');
END;
/
```

Salida real:

```text
No se puede dividir entre cero.

Procedimiento PL/SQL terminado correctamente.
```

## E251 · Excepción propia

**Objetivo:** Declarar y lanzar una excepción.  
**Explicación:** RAISE dispara la excepción declarada.  
**Estado:** OK

```sql
DECLARE
    e_peso_invalido EXCEPTION;
    v_kg NUMBER := -1;
BEGIN
    IF v_kg <= 0 THEN RAISE e_peso_invalido; END IF;
EXCEPTION
    WHEN e_peso_invalido THEN DBMS_OUTPUT.PUT_LINE('El peso debe ser positivo.');
END;
/
```

Salida real:

```text
El peso debe ser positivo.

Procedimiento PL/SQL terminado correctamente.
```

## E252 · RAISE_APPLICATION_ERROR

**Objetivo:** Error con número y mensaje propio.  
**Explicación:** Rango permitido: -20000 a -20999. Así funcionan los triggers del proyecto.  
**Estado:** OK

```sql
BEGIN
    RAISE_APPLICATION_ERROR(-20555, 'Ejemplo de error de negocio');
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('Capturado: ' || SQLCODE || ' ' || SQLERRM);
END;
/
```

Salida real:

```text
Capturado: -20555 ORA-20555: Ejemplo de error de negocio

Procedimiento PL/SQL terminado correctamente.
```

## E253 · DUP_VAL_ON_INDEX

**Objetivo:** Capturar un duplicado.  
**Explicación:** Excepción de Oracle para ORA-00001.  
**Estado:** OK

```sql
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Duplicado', 1);
EXCEPTION
    WHEN DUP_VAL_ON_INDEX THEN DBMS_OUTPUT.PUT_LINE('Ese código ya existe.');
END;
/
```

Salida real:

```text
Ese código ya existe.

Procedimiento PL/SQL terminado correctamente.
```

## E254 · PRAGMA EXCEPTION_INIT

**Objetivo:** Ponerle nombre a un error de Oracle.  
**Explicación:** Asocia ORA-02291 a un nombre legible.  
**Estado:** OK

```sql
DECLARE
    e_padre_no_existe EXCEPTION;
    PRAGMA EXCEPTION_INIT(e_padre_no_existe, -2291);
BEGIN
    INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (999, 'Fantasma', 1);
EXCEPTION
    WHEN e_padre_no_existe THEN DBMS_OUTPUT.PUT_LINE('La estación 999 no existe.');
END;
/
```

Salida real:

```text
La estación 999 no existe.

Procedimiento PL/SQL terminado correctamente.
```

## E255 · WHEN OTHERS con SQLERRM

**Objetivo:** Atrapar cualquier error y explicarlo.  
**Explicación:** Úsalo con cuidado: no escondas errores.  
**Estado:** OK

```sql
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('x', 'minúscula', 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('Error ' || SQLCODE || ': ' || SUBSTR(SQLERRM, 1, 80));
END;
/
```

Salida real:

```text
Error -2290: ORA-02290: restricción de control (ECOACCESS_BD.CK_MATERIAL_CODIGO) violada

Procedimiento PL/SQL terminado correctamente.
```

## E256 · VALUE_ERROR

**Objetivo:** Convertir texto que no es número.  
**Explicación:** En PL/SQL, TO_NUMBER('abc') produce VALUE_ERROR.  
**Estado:** OK

```sql
DECLARE
    v NUMBER;
BEGIN
    v := TO_NUMBER('abc');
EXCEPTION
    WHEN VALUE_ERROR THEN DBMS_OUTPUT.PUT_LINE('No es un número: se atrapó VALUE_ERROR.');
END;
/
```

Salida real:

```text
No es un número: se atrapó VALUE_ERROR.

Procedimiento PL/SQL terminado correctamente.
```

## E257 · SQLCODE

**Objetivo:** Leer el número del error.  
**Explicación:** SQLCODE es negativo para errores de Oracle.  
**Estado:** OK

```sql
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Repetido', 1);
EXCEPTION
    WHEN OTHERS THEN DBMS_OUTPUT.PUT_LINE('SQLCODE = ' || SQLCODE); ROLLBACK;
END;
/
```

Salida real:

```text
SQLCODE = -1

Procedimiento PL/SQL terminado correctamente.
```

## E258 · Propagación

**Objetivo:** Un error interno sube al bloque de afuera.  
**Explicación:** Si el bloque interno no lo atrapa, lo maneja el externo.  
**Estado:** OK

```sql
BEGIN
    BEGIN
        RAISE NO_DATA_FOUND;
    END;
    DBMS_OUTPUT.PUT_LINE('Esta línea no se ejecuta.');
EXCEPTION
    WHEN NO_DATA_FOUND THEN DBMS_OUTPUT.PUT_LINE('El bloque externo atrapó el error del interno.');
END;
/
```

Salida real:

```text
El bloque externo atrapó el error del interno.

Procedimiento PL/SQL terminado correctamente.
```

## E259 · Atrapar un error de negocio

**Objetivo:** Capturar ORA-20813 de la función de canecas.  
**Explicación:** PRAGMA EXCEPTION_INIT da nombre a un código propio.  
**Estado:** OK

```sql
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
```

Salida real:

```text
Residuo peligroso: va por ruta especial, no a una caneca.

Procedimiento PL/SQL terminado correctamente.
```

## E260 · Volver a lanzar (RAISE)

**Objetivo:** Registrar el error y dejar que siga.  
**Explicación:** RAISE sin nombre relanza la misma excepción. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
BEGIN
    UPDATE material SET categoria = 9 WHERE codigo = 'PET';
EXCEPTION
    WHEN OTHERS THEN
        DBMS_OUTPUT.PUT_LINE('Registro: ' || SQLERRM);
        RAISE;
END;
/
```

Salida real:

```text
Registro: ORA-02290: restricción de control (ECOACCESS_BD.CK_MATERIAL_CATEGORIA) violada
BEGIN
*
ERROR en línea 1:
ORA-02290: restricción de control (ECOACCESS_BD.CK_MATERIAL_CATEGORIA) violada
ORA-06512: en línea 6
ORA-06512: en línea 2
Ayuda: https://docs.oracle.com/error-help/db/ora-02290/
```

## E261 · Dónde ocurrió el error

**Objetivo:** FORMAT_ERROR_BACKTRACE indica la línea.  
**Explicación:** Muy útil para depurar paquetes grandes.  
**Estado:** OK

```sql
DECLARE
    v NUMBER;
BEGIN
    v := 1 / 0;
EXCEPTION
    WHEN ZERO_DIVIDE THEN DBMS_OUTPUT.PUT_LINE('Error en: ' || TRIM(DBMS_UTILITY.FORMAT_ERROR_BACKTRACE));
END;
/
```

Salida real:

```text
Error en: ORA-06512: en línea 4


Procedimiento PL/SQL terminado correctamente.
```
