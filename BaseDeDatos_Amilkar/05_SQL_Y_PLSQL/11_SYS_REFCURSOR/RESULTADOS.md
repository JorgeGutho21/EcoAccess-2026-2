# SYS REFCURSOR

Ejercicios: 2 · conexión: `ECOACCESS_BD` · temas del banco: 17. Procedimientos, funciones y paquetes.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E265 | Procedimiento con cursor de salida | OK |
| E266 | Ranking por función | OK |

## E265 · Procedimiento con cursor de salida

**Objetivo:** Resumen de los últimos 30 días.  
**Explicación:** EXEC llama al procedimiento; PRINT muestra el cursor.  
**Estado:** OK

```sql
VARIABLE rc REFCURSOR
EXEC pr_consultar_estadisticas(SYSDATE - 30, SYSDATE, NULL, NULL, :rc)
PRINT rc
```

Salida real:

```text
Procedimiento PL/SQL terminado correctamente.


 DEPOSITOS VERIFICADOS PENDIENTES RECHAZADOS SIN_SENSOR CIUDADANOS UNIDADES_VERIFICADAS KG_ESTIMADOS_VERIFICADOS CON_PESO_MEDIDO KG_MEDIDOS
---------- ----------- ---------- ---------- ---------- ---------- -------------------- ------------------------ --------------- ----------
	83	    61	       12	  10	     26 	29		    106 		   6,495	      11     1,8387

1 fila seleccionada.
```

## E266 · Ranking por función

**Objetivo:** Top 5 de 30 días.  
**Explicación:** La función devuelve un SYS_REFCURSOR.  
**Estado:** OK

```sql
VARIABLE rc REFCURSOR
EXEC :rc := pkg_estadisticas.ranking_verificados(SYSDATE - 30, SYSDATE, 5)
PRINT rc
```

Salida real:

```text
Procedimiento PL/SQL terminado correctamente.


ID_USUARIO
----------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
DEPOSITOS_VERIFICADOS	UNIDADES KG_ESTIMADOS KG_MEDIDOS     PUESTO
--------------------- ---------- ------------ ---------- ----------
	 6
María Gómez Oñate
		    9	      10	 ,365	    ,013	  1

	13
Santiago Quintero Fuentes
		    5	       8	 ,646	    ,415	  2

	 4
Ana Castro Vargas
		    4	       8	 ,876	    ,211	  3

	 8
Valentina Fuentes Ramírez
		    4	       6	 ,382			  4

	11
Jhon Zuleta Rojas
		    4	       5	 ,215			  5


5 filas seleccionadas.
```
