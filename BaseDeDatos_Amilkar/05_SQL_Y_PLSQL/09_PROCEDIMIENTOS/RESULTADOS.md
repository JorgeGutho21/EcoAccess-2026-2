# PROCEDIMIENTOS

Ejercicios: 2 · conexión: `ECOACCESS_BD` · temas del banco: 17. Procedimientos, funciones y paquetes.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E270 | Fuente de un procedimiento | OK |
| E276 | Procedimiento local | OK |

## E270 · Fuente de un procedimiento

**Objetivo:** Leer el código guardado en Oracle.  
**Explicación:** USER_SOURCE guarda el código fuente línea por línea.  
**Estado:** OK

```sql
SELECT line, text FROM user_source WHERE name = 'FN_PESO_ESTIMADO_KG' ORDER BY line FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
LINE
----------
TEXT
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	 1
FUNCTION fn_peso_estimado_kg (

	 2
    p_id_peso_referencial IN peso_referencial.id_peso_referencial%TYPE,

	 3
    p_unidades		  IN NUMBER

	 4
) RETURN NUMBER

	 5
IS

	 6
    v_gramos peso_referencial.gramos_tipico%TYPE;

	 7
BEGIN

	 8
    IF p_id_peso_referencial IS NULL OR p_unidades IS NULL OR p_unidades < 1 THEN


8 filas seleccionadas.
```

## E276 · Procedimiento local

**Objetivo:** Un procedimiento que solo existe dentro del bloque.  
**Explicación:** Sirve para no repetir código en un script.  
**Estado:** OK

```sql
DECLARE
    v NUMBER;   -- las variables van ANTES de los subprogramas locales (si no: PLS-00103)
    PROCEDURE mostrar(p_titulo VARCHAR2, p_valor NUMBER) IS
    BEGIN
        DBMS_OUTPUT.PUT_LINE(RPAD(p_titulo, 22, '.') || p_valor);
    END;
BEGIN
    SELECT COUNT(*) INTO v FROM deposito; mostrar('Depósitos', v);
    SELECT COUNT(*) INTO v FROM consulta_vision; mostrar('Fotos', v);
END;
/
```

Salida real:

```text
Depósitos.............105
Fotos.................187

Procedimiento PL/SQL terminado correctamente.
```
