# FUNCIONES

Ejercicios: 3 · conexión: `ECOACCESS_BD` · temas del banco: 17. Procedimientos, funciones y paquetes.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E262 | Llamar una función en SELECT | OK |
| E263 | Función que resuelve caneca | OK |
| E272 | Peso estimado en un SELECT | OK |

## E262 · Llamar una función en SELECT

**Objetivo:** Peso estimado de 3 botellas.  
**Explicación:** Una función devuelve un valor y se puede usar en SQL.  
**Estado:** OK

```sql
SELECT fn_peso_estimado_kg(1, 3) AS kg, fn_peso_estimado_kg(1, 3) * 1000 AS g FROM dual;
```

Salida real:

```text
	KG	    G
---------- ----------
      ,066	   66

1 fila seleccionada.
```

## E263 · Función que resuelve caneca

**Objetivo:** ¿A qué caneca va el PET en la estación 1?  
**Explicación:** Devuelve el id de la caneca.  
**Estado:** OK

```sql
SELECT fn_resolver_caneca_disponible(1, 101) AS caneca_pet, fn_resolver_caneca_disponible(1, 201) AS caneca_fruta FROM dual;
```

Salida real:

```text
CANECA_PET CANECA_FRUTA
---------- ------------
	11	     12

1 fila seleccionada.
```

## E272 · Peso estimado en un SELECT

**Objetivo:** Estimar 3 unidades de cada objeto del catálogo.  
**Explicación:** Una función se usa como una columna más.  
**Estado:** OK

```sql
SELECT clave_objeto, gramos_tipico, fn_peso_estimado_kg(id_peso_referencial, 3) AS kg_3_unidades FROM peso_referencial WHERE ROWNUM <= 6;
```

Salida real:

```text
CLAVE_OBJETO																			 GRAMOS_TIPICO KG_3_UNIDADES
---------------------------------------------------------------------------------------------------------------------------------------------------------------- ------------- -------------
CAJA_CARTON_PEQUENA																			   120		 ,36
BOTELLA_PET_600ML																			   300		  ,9
LATA_CONSERVA																				    55		,165
PUNADO_HOJAS																				    20		 ,06
SERVILLETA																				     2		,006
BOTELLA_PET_600ML																			   620		1,86

6 filas seleccionadas.
```
