# VARIABLES Y CONDICIONALES

Ejercicios: 7 · conexión: `ECOACCESS_BD` · temas del banco: 14. PL/SQL básico.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E217 | Variables | OK |
| E219 | %TYPE | OK |
| E220 | %ROWTYPE | OK |
| E221 | IF / ELSIF / ELSE | OK |
| E222 | CASE en PL/SQL | OK |
| E227 | Bloque anidado | OK |
| E228 | Constante | OK |

## E217 · Variables

**Objetivo:** Declarar, asignar y mostrar.  
**Explicación:** DECLARE crea variables; := asigna.  
**Estado:** OK

```sql
DECLARE
    v_nombre VARCHAR2(50) := 'Caneca verde';
    v_kg     NUMBER := 0.25;
BEGIN
    DBMS_OUTPUT.PUT_LINE(v_nombre || ' recibió ' || v_kg * 1000 || ' g');
END;
/
```

Salida real:

```text
Caneca verde recibió 250 g

Procedimiento PL/SQL terminado correctamente.
```

## E219 · %TYPE

**Objetivo:** La variable copia el tipo de una columna.  
**Explicación:** Si la columna cambia de tamaño, la variable también.  
**Estado:** OK

```sql
DECLARE
    v_codigo material.codigo%TYPE;
BEGIN
    SELECT codigo INTO v_codigo FROM material WHERE id_material = 101;
    DBMS_OUTPUT.PUT_LINE('Material 101: ' || v_codigo);
END;
/
```

Salida real:

```text
Material 101: PET

Procedimiento PL/SQL terminado correctamente.
```

## E220 · %ROWTYPE

**Objetivo:** Una variable con TODA la fila.  
**Explicación:** Cada campo se lee como v.columna.  
**Estado:** OK

```sql
DECLARE
    v material%ROWTYPE;
BEGIN
    SELECT * INTO v FROM material WHERE codigo = 'VIDRIO';
    DBMS_OUTPUT.PUT_LINE(v.nombre || ' - categoría ' || v.categoria);
END;
/
```

Salida real:

```text
Vidrio - categoría 1

Procedimiento PL/SQL terminado correctamente.
```

## E221 · IF / ELSIF / ELSE

**Objetivo:** Decidir el color de la caneca.  
**Explicación:** IF evalúa condiciones en orden.  
**Estado:** OK

```sql
DECLARE
    v_cat material.categoria%TYPE := 2;
BEGIN
    IF v_cat = 1 THEN DBMS_OUTPUT.PUT_LINE('BLANCA');
    ELSIF v_cat = 2 THEN DBMS_OUTPUT.PUT_LINE('VERDE');
    ELSIF v_cat = 3 THEN DBMS_OUTPUT.PUT_LINE('NEGRA');
    ELSE DBMS_OUTPUT.PUT_LINE('Ruta especial');
    END IF;
END;
/
```

Salida real:

```text
VERDE

Procedimiento PL/SQL terminado correctamente.
```

## E222 · CASE en PL/SQL

**Objetivo:** Lo mismo con CASE.  
**Explicación:** CASE es más ordenado cuando hay muchos valores.  
**Estado:** OK

```sql
DECLARE
    v_estado NUMBER := 1;
BEGIN
    DBMS_OUTPUT.PUT_LINE(CASE v_estado WHEN 0 THEN 'Pendiente' WHEN 1 THEN 'Verificado' ELSE 'Rechazado' END);
END;
/
```

Salida real:

```text
Verificado

Procedimiento PL/SQL terminado correctamente.
```

## E227 · Bloque anidado

**Objetivo:** Un bloque dentro de otro.  
**Explicación:** Cada bloque tiene sus variables y su EXCEPTION.  
**Estado:** OK

```sql
DECLARE
    v_externa NUMBER := 1;
BEGIN
    DECLARE
        v_interna NUMBER := 2;
    BEGIN
        DBMS_OUTPUT.PUT_LINE('Suma: ' || (v_externa + v_interna));
    END;
END;
/
```

Salida real:

```text
Suma: 3

Procedimiento PL/SQL terminado correctamente.
```

## E228 · Constante

**Objetivo:** Una variable que no puede cambiar.  
**Explicación:** CONSTANT obliga a dar valor y protege de cambios.  
**Estado:** OK

```sql
DECLARE
    c_ventana CONSTANT NUMBER := 10;
BEGIN
    DBMS_OUTPUT.PUT_LINE('La caneca queda abierta ' || c_ventana || ' segundos.');
END;
/
```

Salida real:

```text
La caneca queda abierta 10 segundos.

Procedimiento PL/SQL terminado correctamente.
```
