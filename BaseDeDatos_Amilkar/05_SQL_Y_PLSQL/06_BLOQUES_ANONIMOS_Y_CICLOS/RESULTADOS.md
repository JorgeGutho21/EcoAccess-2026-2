# BLOQUES ANONIMOS Y CICLOS

Ejercicios: 11 · conexión: `ECOACCESS_BD` · temas del banco: 14. PL/SQL básico.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E216 | Hola mundo | OK |
| E218 | SELECT INTO | OK |
| E223 | LOOP con EXIT | OK |
| E224 | WHILE | OK |
| E225 | FOR numérico | OK |
| E226 | FOR REVERSE | OK |
| E229 | BOOLEAN | OK |
| E230 | Registro propio (RECORD) | OK |
| E231 | Colección y BULK COLLECT | OK |
| E232 | Arreglo asociativo | OK |
| E233 | CONTINUE en un ciclo | OK |

## E216 · Hola mundo

**Objetivo:** El bloque anónimo más simple.  
**Explicación:** BEGIN ... END; y DBMS_OUTPUT.PUT_LINE para escribir. En SQL Developer: Ver > Salida DBMS.  
**Estado:** OK

```sql
BEGIN
    DBMS_OUTPUT.PUT_LINE('Hola, EcoAccess');
END;
/
```

Salida real:

```text
Hola, EcoAccess

Procedimiento PL/SQL terminado correctamente.
```

## E218 · SELECT INTO

**Objetivo:** Guardar un resultado en una variable.  
**Explicación:** SELECT ... INTO exige que salga exactamente UNA fila.  
**Estado:** OK

```sql
DECLARE
    v_total NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_total FROM deposito;
    DBMS_OUTPUT.PUT_LINE('Depósitos: ' || v_total);
END;
/
```

Salida real:

```text
Depósitos: 105

Procedimiento PL/SQL terminado correctamente.
```

## E223 · LOOP con EXIT

**Objetivo:** Contar del 1 al 3.  
**Explicación:** LOOP repite hasta que EXIT WHEN se cumple.  
**Estado:** OK

```sql
DECLARE
    i PLS_INTEGER := 1;
BEGIN
    LOOP
        DBMS_OUTPUT.PUT_LINE('Vuelta ' || i);
        i := i + 1;
        EXIT WHEN i > 3;
    END LOOP;
END;
/
```

Salida real:

```text
Vuelta 1
Vuelta 2
Vuelta 3

Procedimiento PL/SQL terminado correctamente.
```

## E224 · WHILE

**Objetivo:** Repetir mientras se cumpla.  
**Explicación:** WHILE revisa la condición antes de cada vuelta.  
**Estado:** OK

```sql
DECLARE
    v_gramos NUMBER := 0;
BEGIN
    WHILE v_gramos < 100 LOOP
        v_gramos := v_gramos + 22;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Botellas hasta pasar 100 g: ' || v_gramos / 22 || ' (' || v_gramos || ' g)');
END;
/
```

Salida real:

```text
Botellas hasta pasar 100 g: 5 (110 g)

Procedimiento PL/SQL terminado correctamente.
```

## E225 · FOR numérico

**Objetivo:** Tabla de multiplicar de puntos.  
**Explicación:** FOR i IN 1..n crea el contador solo.  
**Estado:** OK

```sql
BEGIN
    FOR i IN 1 .. 5 LOOP
        DBMS_OUTPUT.PUT_LINE(i || ' botellas PET x 12 puntos = ' || i * 12);
    END LOOP;
END;
/
```

Salida real:

```text
1 botellas PET x 12 puntos = 12
2 botellas PET x 12 puntos = 24
3 botellas PET x 12 puntos = 36
4 botellas PET x 12 puntos = 48
5 botellas PET x 12 puntos = 60

Procedimiento PL/SQL terminado correctamente.
```

## E226 · FOR REVERSE

**Objetivo:** Contar hacia atrás.  
**Explicación:** REVERSE invierte el recorrido.  
**Estado:** OK

```sql
BEGIN
    FOR i IN REVERSE 1 .. 3 LOOP
        DBMS_OUTPUT.PUT_LINE('Cerrando tapa en ' || i);
    END LOOP;
END;
/
```

Salida real:

```text
Cerrando tapa en 3
Cerrando tapa en 2
Cerrando tapa en 1

Procedimiento PL/SQL terminado correctamente.
```

## E229 · BOOLEAN

**Objetivo:** Variables verdadero/falso en PL/SQL.  
**Explicación:** En PL/SQL existe BOOLEAN; en tablas se usa NUMBER(1).  
**Estado:** OK

```sql
DECLARE
    v_hay_pendientes BOOLEAN;
    v_n NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_n FROM deposito WHERE estado_verificacion = 0;
    v_hay_pendientes := v_n > 0;
    IF v_hay_pendientes THEN DBMS_OUTPUT.PUT_LINE('Hay ' || v_n || ' depósitos por verificar.'); END IF;
END;
/
```

Salida real:

```text
Hay 17 depósitos por verificar.

Procedimiento PL/SQL terminado correctamente.
```

## E230 · Registro propio (RECORD)

**Objetivo:** Agrupar varios datos en una variable.  
**Explicación:** TYPE ... IS RECORD define una estructura.  
**Estado:** OK

```sql
DECLARE
    TYPE t_resumen IS RECORD (verificados NUMBER, pendientes NUMBER);
    r t_resumen;
BEGIN
    SELECT COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END), COUNT(CASE WHEN estado_verificacion = 0 THEN 1 END) INTO r.verificados, r.pendientes FROM deposito;
    DBMS_OUTPUT.PUT_LINE('Verificados=' || r.verificados || ' Pendientes=' || r.pendientes);
END;
/
```

Salida real:

```text
Verificados=76 Pendientes=17

Procedimiento PL/SQL terminado correctamente.
```

## E231 · Colección y BULK COLLECT

**Objetivo:** Traer muchas filas de una sola vez.  
**Explicación:** BULK COLLECT llena una colección; es más rápido que fila por fila.  
**Estado:** OK

```sql
DECLARE
    TYPE t_codigos IS TABLE OF material.codigo%TYPE;
    v t_codigos;
BEGIN
    SELECT codigo BULK COLLECT INTO v FROM material WHERE categoria = 1 ORDER BY codigo;
    DBMS_OUTPUT.PUT_LINE('Materiales blancos: ' || v.COUNT || '. Primero: ' || v(1) || ', último: ' || v(v.LAST));
END;
/
```

Salida real:

```text
Materiales blancos: 8. Primero: CARTON, último: VIDRIO

Procedimiento PL/SQL terminado correctamente.
```

## E232 · Arreglo asociativo

**Objetivo:** Una «tabla» indexada por texto.  
**Explicación:** INDEX BY VARCHAR2 funciona como un diccionario.  
**Estado:** OK

```sql
DECLARE
    TYPE t_color IS TABLE OF VARCHAR2(10) INDEX BY PLS_INTEGER;
    color t_color;
BEGIN
    color(1) := 'BLANCA'; color(2) := 'VERDE'; color(3) := 'NEGRA';
    FOR i IN 1 .. 3 LOOP DBMS_OUTPUT.PUT_LINE(i || ' = ' || color(i)); END LOOP;
END;
/
```

Salida real:

```text
1 = BLANCA
2 = VERDE
3 = NEGRA

Procedimiento PL/SQL terminado correctamente.
```

## E233 · CONTINUE en un ciclo

**Objetivo:** Saltar los números pares.  
**Explicación:** CONTINUE pasa a la siguiente vuelta.  
**Estado:** OK

```sql
BEGIN
    FOR i IN 1 .. 7 LOOP
        CONTINUE WHEN MOD(i, 2) = 0;
        DBMS_OUTPUT.PUT_LINE('Impar: ' || i);
    END LOOP;
END;
/
```

Salida real:

```text
Impar: 1
Impar: 3
Impar: 5
Impar: 7

Procedimiento PL/SQL terminado correctamente.
```
