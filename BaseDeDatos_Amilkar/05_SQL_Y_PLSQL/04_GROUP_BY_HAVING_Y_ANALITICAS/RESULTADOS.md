# GROUP BY HAVING Y ANALITICAS

Ejercicios: 37 · conexión: `ECOACCESS_BD` · temas del banco: 10. Funciones analíticas; 6. Agrupar: GROUP BY y HAVING.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E088 | COUNT por grupo | OK |
| E089 | SUM | OK |
| E090 | AVG, MIN, MAX | OK |
| E091 | COUNT(columna) vs COUNT(*) | OK |
| E092 | COUNT DISTINCT | OK |
| E093 | Agrupar por dos columnas | OK |
| E094 | HAVING | OK |
| E095 | WHERE + GROUP BY + HAVING | OK |
| E096 | Agrupar por día | OK |
| E097 | Agrupar por semana | OK |
| E098 | Conteo condicional | OK |
| E099 | ROLLUP | OK |
| E100 | LISTAGG | OK |
| E101 | Promedio de un grupo | OK |
| E102 | Porcentaje | OK |
| E103 | Depósitos por estado | OK |
| E104 | Fotos por resultado | OK |
| E105 | Latencia promedio por canal | OK |
| E106 | CUBE | OK |
| E107 | GROUPING | OK |
| E108 | GROUPING SETS | OK |
| E109 | HAVING con COUNT DISTINCT | OK |
| E165 | ROW_NUMBER | OK |
| E166 | RANK y DENSE_RANK | OK |
| E167 | SUM acumulado | OK |
| E168 | LAG | OK |
| E169 | Porcentaje del total | OK |
| E170 | Primero de cada grupo | OK |
| E171 | Promedio móvil | OK |
| E172 | NTILE | OK |
| E173 | Total por partición | OK |
| E174 | LEAD | OK |
| E175 | FIRST_VALUE | OK |
| E176 | RATIO_TO_REPORT | OK |
| E177 | PERCENT_RANK | OK |
| E178 | Contador por persona | OK |
| E179 | Mejor del mes con RANK | OK |

## E088 · COUNT por grupo

**Objetivo:** Depósitos por estado.  
**Explicación:** GROUP BY forma un grupo por cada valor.  
**Estado:** OK

```sql
SELECT estado_verificacion, COUNT(*) AS cantidad FROM deposito GROUP BY estado_verificacion ORDER BY 1;
```

Salida real:

```text
ESTADO_VERIFICACION   CANTIDAD
------------------- ----------
		  0	    17
		  1	    76
		  2	    12

3 filas seleccionadas.
```

## E089 · SUM

**Objetivo:** Kilogramos estimados verificados.  
**Explicación:** SUM suma; los NULL se ignoran.  
**Estado:** OK

```sql
SELECT ROUND(SUM(peso_estimado_kg), 3) AS kg_estimados FROM deposito WHERE estado_verificacion = 1;
```

Salida real:

```text
KG_ESTIMADOS
------------
       7,697

1 fila seleccionada.
```

## E090 · AVG, MIN, MAX

**Objetivo:** Estadísticas de latencia de la IA.  
**Explicación:** Promedio, mínimo y máximo.  
**Estado:** OK

```sql
SELECT ROUND(AVG(latencia_ms)) AS promedio, MIN(latencia_ms) AS minimo, MAX(latencia_ms) AS maximo FROM consulta_vision;
```

Salida real:

```text
PROMEDIO     MINIMO	  MAXIMO
---------- ---------- ----------
      2363	  723	    4942

1 fila seleccionada.
```

## E091 · COUNT(columna) vs COUNT(*)

**Objetivo:** Ver la diferencia con NULL.  
**Explicación:** COUNT(columna) no cuenta los NULL.  
**Estado:** OK

```sql
SELECT COUNT(*) AS todos, COUNT(peso_medido_kg) AS con_medicion FROM deposito;
```

Salida real:

```text
TODOS CON_MEDICION
---------- ------------
       105	     14

1 fila seleccionada.
```

## E092 · COUNT DISTINCT

**Objetivo:** Ciudadanos distintos con sesión.  
**Explicación:** Cuenta valores sin repetir.  
**Estado:** OK

```sql
SELECT COUNT(DISTINCT id_usuario) AS ciudadanos FROM interaccion;
```

Salida real:

```text
CIUDADANOS
----------
	31

1 fila seleccionada.
```

## E093 · Agrupar por dos columnas

**Objetivo:** Depósitos por sensor y estado.  
**Explicación:** Un grupo por cada combinación.  
**Estado:** OK

```sql
SELECT sin_sensor, estado_verificacion, COUNT(*) FROM deposito GROUP BY sin_sensor, estado_verificacion ORDER BY 1, 2;
```

Salida real:

```text
SIN_SENSOR ESTADO_VERIFICACION	 COUNT(*)
---------- ------------------- ----------
	 0		     0	       10
	 0		     1	       53
	 0		     2		9
	 1		     0		7
	 1		     1	       23
	 1		     2		3

6 filas seleccionadas.
```

## E094 · HAVING

**Objetivo:** Ciudadanos con 3 o más sesiones.  
**Explicación:** HAVING filtra GRUPOS (WHERE filtra filas).  
**Estado:** OK

```sql
SELECT id_usuario, COUNT(*) AS sesiones FROM interaccion WHERE id_usuario IS NOT NULL GROUP BY id_usuario HAVING COUNT(*) >= 3 ORDER BY sesiones DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_USUARIO   SESIONES
---------- ----------
	 6	   12
	 8	   12
	 4	    8
	12	    8
	 9	    8

5 filas seleccionadas.
```

## E095 · WHERE + GROUP BY + HAVING

**Objetivo:** Materiales verificados más de 3 veces.  
**Explicación:** Orden: WHERE → GROUP BY → HAVING → ORDER BY.  
**Estado:** OK

```sql
SELECT id_material_verificado, COUNT(*) AS veces FROM deposito WHERE estado_verificacion = 1 GROUP BY id_material_verificado HAVING COUNT(*) > 3 ORDER BY veces DESC;
```

Salida real:

```text
ID_MATERIAL_VERIFICADO	    VECES
---------------------- ----------
		   106		9
		   104		9
		   103		8
		   105		6
		   108		6
		   101		6
		   107		5
		   102		4
		   301		4
		   203		4

10 filas seleccionadas.
```

## E096 · Agrupar por día

**Objetivo:** Depósitos por día (hora Colombia).  
**Explicación:** Se convierte a Colombia y TRUNC quita la hora.  
**Estado:** OK

```sql
SELECT TRUNC(CAST(FROM_TZ(fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) AS dia, COUNT(*) FROM deposito GROUP BY TRUNC(CAST(FROM_TZ(fecha_deteccion_utc,'UTC') AT TIME ZONE 'America/Bogota' AS DATE)) ORDER BY dia DESC FETCH FIRST 7 ROWS ONLY;
```

Salida real:

```text
DIA		   COUNT(*)
---------------- ----------
2026-11-11 00:00	  3
2026-11-10 00:00	  5
2026-11-09 00:00	  3
2026-11-08 00:00	  9
2026-11-07 00:00	  6
2026-11-06 00:00	  6
2026-11-05 00:00	  3

7 filas seleccionadas.
```

## E097 · Agrupar por semana

**Objetivo:** Depósitos por semana ISO.  
**Explicación:** 'IW' lleva cada fecha al lunes de su semana.  
**Estado:** OK

```sql
SELECT TRUNC(fecha_colombia, 'IW') AS semana, COUNT(*) FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia, 'IW') ORDER BY semana;
```

Salida real:

```text
SEMANA		   COUNT(*)
---------------- ----------
2026-09-28 00:00	 10
2026-10-05 00:00	 11
2026-10-12 00:00	 13
2026-10-19 00:00	 13
2026-10-26 00:00	 18
2026-11-02 00:00	 29
2026-11-09 00:00	 11

7 filas seleccionadas.
```

## E098 · Conteo condicional

**Objetivo:** Verificados y pendientes en una sola fila.  
**Explicación:** COUNT(CASE ...) cuenta solo cuando se cumple.  
**Estado:** OK

```sql
SELECT COUNT(CASE WHEN estado_verificacion = 1 THEN 1 END) AS verificados, COUNT(CASE WHEN estado_verificacion = 0 THEN 1 END) AS pendientes FROM deposito;
```

Salida real:

```text
VERIFICADOS PENDIENTES
----------- ----------
	 76	    17

1 fila seleccionada.
```

## E099 · ROLLUP

**Objetivo:** Subtotales por color y total general.  
**Explicación:** ROLLUP agrega filas de subtotal.  
**Estado:** OK

```sql
SELECT color, estado_texto, COUNT(*) FROM vw_deposito_detalle GROUP BY ROLLUP(color, estado_texto) ORDER BY color, estado_texto;
```

Salida real:

```text
COLOR  ESTADO_TEX   COUNT(*)
------ ---------- ----------
BLANCA PENDIENTE	  12
BLANCA RECHAZADO	   7
BLANCA VERIFICADO	  56
BLANCA			  75
NEGRA  PENDIENTE	   3
NEGRA  RECHAZADO	   3
NEGRA  VERIFICADO	   9
NEGRA			  15
VERDE  PENDIENTE	   2
VERDE  RECHAZADO	   2
VERDE  VERIFICADO	  11
VERDE			  15
			 105

13 filas seleccionadas.
```

## E100 · LISTAGG

**Objetivo:** Materiales de cada caneca en una sola línea.  
**Explicación:** LISTAGG une textos de un grupo.  
**Estado:** OK

```sql
SELECT cm.id_contenedor, LISTAGG(m.codigo, ', ') WITHIN GROUP (ORDER BY m.codigo) AS materiales FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material GROUP BY cm.id_contenedor;
```

Salida real:

```text
ID_CONTENEDOR
-------------
MATERIALES
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	   11
CARTON, LATA_ALUMINIO, METAL_FERROSO, PAPEL, PET, PLASTICO_RIGIDO, TETRAPAK, VIDRIO

	   12
BORRA_CAFE, CASCARA_HUEVO, HOJAS_PODA, RESTO_COMIDA, RESTO_FRUTA_VERDURA

	   13
CARTON_CONTAMINADO, EMPAQUE_METALIZADO, ICOPOR_SUCIO, SERVILLETA_USADA, TAPABOCAS

	   21
CARTON, LATA_ALUMINIO, PET


4 filas seleccionadas.
```

## E101 · Promedio de un grupo

**Objetivo:** Peso estimado promedio por estado.  
**Explicación:** AVG con GROUP BY.  
**Estado:** OK

```sql
SELECT estado_verificacion, ROUND(AVG(peso_estimado_kg) * 1000, 1) AS gramos_promedio FROM deposito GROUP BY estado_verificacion ORDER BY 1;
```

Salida real:

```text
ESTADO_VERIFICACION GRAMOS_PROMEDIO
------------------- ---------------
		  0	      123,4
		  1	      101,3
		  2	       86,3

3 filas seleccionadas.
```

## E102 · Porcentaje

**Objetivo:** Qué porcentaje de depósitos fue sin sensor.  
**Explicación:** Se divide y se multiplica por 100.  
**Estado:** OK

```sql
SELECT ROUND(100 * COUNT(CASE WHEN sin_sensor = 1 THEN 1 END) / COUNT(*), 1) AS porcentaje_sin_sensor FROM deposito;
```

Salida real:

```text
PORCENTAJE_SIN_SENSOR
---------------------
		 31,4

1 fila seleccionada.
```

## E103 · Depósitos por estado

**Objetivo:** Cuántos pendientes, verificados y rechazados.  
**Explicación:** Agrupar por el código y traducirlo con CASE.  
**Estado:** OK

```sql
SELECT CASE estado_verificacion WHEN 0 THEN 'PENDIENTE' WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END AS estado, COUNT(*) AS cantidad FROM deposito GROUP BY estado_verificacion ORDER BY estado_verificacion;
```

Salida real:

```text
ESTADO	     CANTIDAD
---------- ----------
PENDIENTE	   17
VERIFICADO	   76
RECHAZADO	   12

3 filas seleccionadas.
```

## E104 · Fotos por resultado

**Objetivo:** Cuántas fotos se identificaron y cuántas no.  
**Explicación:** resultado es un código; se agrupa igual que cualquier columna.  
**Estado:** OK

```sql
SELECT resultado, COUNT(*) AS fotos FROM consulta_vision GROUP BY resultado ORDER BY resultado;
```

Salida real:

```text
RESULTADO	FOTOS
---------- ----------
	 1	  164
	 2	   17
	 3	    6

3 filas seleccionadas.
```

## E105 · Latencia promedio por canal

**Objetivo:** ¿Cuánto tarda Eco en responder?  
**Explicación:** AVG ignora los NULL; ROUND deja el número legible.  
**Estado:** OK

```sql
SELECT canal, ROUND(AVG(latencia_ms)) AS ms_promedio, MAX(latencia_ms) AS ms_maximo FROM consulta_vision GROUP BY canal;
```

Salida real:

```text
CANAL MS_PROMEDIO	MS_MAXIMO
---------- ----------- ----------
	 1	  2363	     4942

1 fila seleccionada.
```

## E106 · CUBE

**Objetivo:** Subtotales en todas las combinaciones.  
**Explicación:** CUBE agrega filas de subtotal por cada columna y el total general.  
**Estado:** OK

```sql
SELECT sin_sensor, estado_verificacion, COUNT(*) AS n FROM deposito GROUP BY CUBE(sin_sensor, estado_verificacion) ORDER BY sin_sensor, estado_verificacion;
```

Salida real:

```text
SIN_SENSOR ESTADO_VERIFICACION		N
---------- ------------------- ----------
	 0		     0	       10
	 0		     1	       53
	 0		     2		9
	 0			       72
	 1		     0		7
	 1		     1	       23
	 1		     2		3
	 1			       33
			     0	       17
			     1	       76
			     2	       12
				      105

12 filas seleccionadas.
```

## E107 · GROUPING

**Objetivo:** Distinguir una fila de subtotal.  
**Explicación:** GROUPING(col) = 1 en las filas que ROLLUP agregó.  
**Estado:** OK

```sql
SELECT DECODE(GROUPING(categoria), 1, 'TOTAL', TO_CHAR(categoria)) AS categoria, COUNT(*) AS materiales FROM material GROUP BY ROLLUP(categoria);
```

Salida real:

```text
CATEGORIA				 MATERIALES
---------------------------------------- ----------
1						  8
2						  5
3						  5
4						  3
TOTAL						 21

5 filas seleccionadas.
```

## E108 · GROUPING SETS

**Objetivo:** Dos agrupaciones en una sola consulta.  
**Explicación:** Equivale a dos GROUP BY unidos con UNION ALL.  
**Estado:** OK

```sql
SELECT canal, estado, COUNT(*) AS n FROM interaccion GROUP BY GROUPING SETS ((canal), (estado)) ORDER BY canal, estado;
```

Salida real:

```text
CANAL     ESTADO	       N
---------- ---------- ----------
	 3		      32
	 4		      98
		    3	     105
		    4	      25

4 filas seleccionadas.
```

## E109 · HAVING con COUNT DISTINCT

**Objetivo:** Materiales depositados por al menos 3 personas distintas.  
**Explicación:** HAVING filtra grupos ya formados.  
**Estado:** OK

```sql
SELECT d.id_material_declarado, COUNT(DISTINCT i.id_usuario) AS personas FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY d.id_material_declarado HAVING COUNT(DISTINCT i.id_usuario) >= 3 ORDER BY personas DESC;
```

Salida real:

```text
ID_MATERIAL_DECLARADO	PERSONAS
--------------------- ----------
		  104	      11
		  103	       9
		  106	       9
		  108	       8
		  107	       7
		  105	       7
		  303	       6
		  102	       6
		  101	       4
		  205	       4
		  203	       4
		  302	       3

12 filas seleccionadas.
```

## E165 · ROW_NUMBER

**Objetivo:** Numerar los depósitos de cada ciudadano.  
**Explicación:** OVER(PARTITION BY ...) reinicia la cuenta por grupo.  
**Estado:** OK

```sql
SELECT id_titular, id_deposito, ROW_NUMBER() OVER (PARTITION BY id_titular ORDER BY fecha_colombia) AS n FROM vw_deposito_detalle WHERE id_titular IN (SELECT MIN(id_titular) FROM vw_deposito_detalle);
```

Salida real:

```text
ID_TITULAR ID_DEPOSITO		N
---------- ----------- ----------
	 1	    26		1
	 1	    94		2
	 1	    48		3

3 filas seleccionadas.
```

## E166 · RANK y DENSE_RANK

**Objetivo:** Ranking con empates.  
**Explicación:** RANK salta puestos tras un empate; DENSE_RANK no.  
**Estado:** OK

```sql
SELECT titular, COUNT(*) n, RANK() OVER (ORDER BY COUNT(*) DESC) AS rank, DENSE_RANK() OVER (ORDER BY COUNT(*) DESC) AS dense FROM vw_deposito_detalle WHERE estado_verificacion = 1 GROUP BY titular FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
TITULAR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	 N	 RANK	   DENSE
---------- ---------- ----------
María Gómez Oñate
	 9	    1	       1

Jhon Zuleta Rojas
	 5	    2	       2

Santiago Quintero Fuentes
	 5	    2	       2

Valentina Fuentes Ramírez
	 5	    2	       2

Ana Castro Vargas
	 5	    2	       2

Camila Rojas Mendoza
	 4	    6	       3

Carlos Vargas Barros
	 4	    6	       3

Andrés Pérez Martínez
	 3	    8	       4


8 filas seleccionadas.
```

## E167 · SUM acumulado

**Objetivo:** Depósitos acumulados por día.  
**Explicación:** SUM() OVER (ORDER BY ...) va sumando.  
**Estado:** OK

```sql
SELECT dia, n, SUM(n) OVER (ORDER BY dia) AS acumulado FROM (SELECT TRUNC(fecha_colombia) dia, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia)) ORDER BY dia DESC FETCH FIRST 7 ROWS ONLY;
```

Salida real:

```text
DIA			  N  ACUMULADO
---------------- ---------- ----------
2026-11-11 00:00	  3	   105
2026-11-10 00:00	  5	   102
2026-11-09 00:00	  3	    97
2026-11-08 00:00	  9	    94
2026-11-07 00:00	  6	    85
2026-11-06 00:00	  6	    79
2026-11-05 00:00	  3	    73

7 filas seleccionadas.
```

## E168 · LAG

**Objetivo:** Comparar cada semana con la anterior.  
**Explicación:** LAG trae el valor de la fila anterior.  
**Estado:** OK

```sql
SELECT semana, n, LAG(n) OVER (ORDER BY semana) AS semana_anterior, n - LAG(n) OVER (ORDER BY semana) AS diferencia FROM (SELECT TRUNC(fecha_colombia,'IW') semana, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia,'IW')) ORDER BY semana;
```

Salida real:

```text
SEMANA			  N SEMANA_ANTERIOR DIFERENCIA
---------------- ---------- --------------- ----------
2026-09-28 00:00	 10
2026-10-05 00:00	 11		 10	     1
2026-10-12 00:00	 13		 11	     2
2026-10-19 00:00	 13		 13	     0
2026-10-26 00:00	 18		 13	     5
2026-11-02 00:00	 29		 18	    11
2026-11-09 00:00	 11		 29	   -18

7 filas seleccionadas.
```

## E169 · Porcentaje del total

**Objetivo:** Participación de cada color.  
**Explicación:** RATIO_TO_REPORT calcula la fracción del total.  
**Estado:** OK

```sql
SELECT color, COUNT(*) n, ROUND(100 * RATIO_TO_REPORT(COUNT(*)) OVER (), 1) AS porcentaje FROM vw_deposito_detalle GROUP BY color;
```

Salida real:

```text
COLOR		N PORCENTAJE
------ ---------- ----------
NEGRA	       15	14,3
BLANCA	       75	71,4
VERDE	       15	14,3

3 filas seleccionadas.
```

## E170 · Primero de cada grupo

**Objetivo:** Primer depósito de cada caneca.  
**Explicación:** FIRST_VALUE toma el primero según el orden.  
**Estado:** OK

```sql
SELECT DISTINCT caneca, FIRST_VALUE(id_deposito) OVER (PARTITION BY caneca ORDER BY fecha_colombia) AS primer_deposito FROM vw_deposito_detalle;
```

Salida real:

```text
CANECA
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
PRIMER_DEPOSITO
---------------
Legado - Caneca blanca
	      9

UPC01 - Caneca blanca
	     99

UPC01 - Caneca negra
	     67

UPC01 - Caneca verde
	     11


4 filas seleccionadas.
```

## E171 · Promedio móvil

**Objetivo:** Promedio de 3 días.  
**Explicación:** ROWS BETWEEN 2 PRECEDING AND CURRENT ROW.  
**Estado:** OK

```sql
SELECT dia, n, ROUND(AVG(n) OVER (ORDER BY dia ROWS BETWEEN 2 PRECEDING AND CURRENT ROW), 2) AS movil_3 FROM (SELECT TRUNC(fecha_colombia) dia, COUNT(*) n FROM vw_deposito_detalle GROUP BY TRUNC(fecha_colombia)) ORDER BY dia DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
DIA			  N    MOVIL_3
---------------- ---------- ----------
2026-11-11 00:00	  3	  3,67
2026-11-10 00:00	  5	  5,67
2026-11-09 00:00	  3	     6
2026-11-08 00:00	  9	     7
2026-11-07 00:00	  6	     5

5 filas seleccionadas.
```

## E172 · NTILE

**Objetivo:** Dividir ciudadanos en 4 grupos por actividad.  
**Explicación:** NTILE(4) reparte en cuartiles.  
**Estado:** OK

```sql
SELECT titular, n, NTILE(4) OVER (ORDER BY n DESC) AS cuartil FROM (SELECT titular, COUNT(*) n FROM vw_deposito_detalle WHERE titular IS NOT NULL GROUP BY titular) FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
TITULAR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	 N    CUARTIL
---------- ----------
María Gómez Oñate
	11	    1

Valentina Fuentes Ramírez
	 8	    1

Santiago Quintero Fuentes
	 7	    1

Ana Castro Vargas
	 6	    1

Daniela Martínez Daza
	 5	    1

Camila Rojas Mendoza
	 5	    1

Jhon Zuleta Rojas
	 5	    1

Andrés Pérez Martínez
	 5	    1


8 filas seleccionadas.
```

## E173 · Total por partición

**Objetivo:** Cada depósito junto al total de su material.  
**Explicación:** SUM() OVER (PARTITION BY) no colapsa filas como GROUP BY.  
**Estado:** OK

```sql
SELECT id_deposito, id_material_declarado, COUNT(*) OVER (PARTITION BY id_material_declarado) AS del_mismo_material FROM deposito ORDER BY id_material_declarado, id_deposito FETCH FIRST 10 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO ID_MATERIAL_DECLARADO DEL_MISMO_MATERIAL
----------- --------------------- ------------------
	  9		      101		   6
	 16		      101		   6
	 24		      101		   6
	 43		      101		   6
	 66		      101		   6
	105		      101		   6
	 17		      102		   7
	 56		      102		   7
	 62		      102		   7
	 64		      102		   7

10 filas seleccionadas.
```

## E174 · LEAD

**Objetivo:** Ver la siguiente foto de la misma persona.  
**Explicación:** LEAD mira la fila de adelante dentro de la partición.  
**Estado:** OK

```sql
SELECT id_usuario, id_consulta, LEAD(id_consulta) OVER (PARTITION BY id_usuario ORDER BY fecha_utc) AS siguiente FROM consulta_vision ORDER BY id_usuario, fecha_utc FETCH FIRST 10 ROWS ONLY;
```

Salida real:

```text
ID_USUARIO ID_CONSULTA	SIGUIENTE
---------- ----------- ----------
	 1	    32	       97
	 1	    97	      133
	 1	   133	       66
	 1	    66
	 4	    10	       27
	 4	    27	       54
	 4	    54	       90
	 4	    90	       67
	 4	    67	       55
	 4	    55	      146

10 filas seleccionadas.
```

## E175 · FIRST_VALUE

**Objetivo:** Primer material que depositó cada persona.  
**Explicación:** FIRST_VALUE toma el primero según el orden.  
**Estado:** OK

```sql
SELECT DISTINCT i.id_usuario, FIRST_VALUE(d.id_material_declarado) OVER (PARTITION BY i.id_usuario ORDER BY d.fecha_deteccion_utc) AS primer_material FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY i.id_usuario FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
ID_USUARIO PRIMER_MATERIAL
---------- ---------------
	 1	       107
	 4	       101
	 5	       302
	 6	       201
	 7	       102
	 8	       203
	 9	       105
	10	       101

8 filas seleccionadas.
```

## E176 · RATIO_TO_REPORT

**Objetivo:** Porcentaje de cada estado sobre el total.  
**Explicación:** Divide cada valor por la suma de su ventana.  
**Estado:** OK

```sql
SELECT estado_verificacion, COUNT(*) AS n, ROUND(100 * RATIO_TO_REPORT(COUNT(*)) OVER (), 1) AS porcentaje FROM deposito GROUP BY estado_verificacion ORDER BY estado_verificacion;
```

Salida real:

```text
ESTADO_VERIFICACION	     N PORCENTAJE
------------------- ---------- ----------
		  0	    17	     16,2
		  1	    76	     72,4
		  2	    12	     11,4

3 filas seleccionadas.
```

## E177 · PERCENT_RANK

**Objetivo:** Posición relativa de cada peso.  
**Explicación:** 0 = el menor, 1 = el mayor.  
**Estado:** OK

```sql
SELECT id_deposito, peso_estimado_kg, ROUND(PERCENT_RANK() OVER (ORDER BY peso_estimado_kg), 2) AS posicion FROM deposito WHERE peso_estimado_kg IS NOT NULL ORDER BY peso_estimado_kg FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO PESO_ESTIMADO_KG   POSICION
----------- ---------------- ----------
	 68		,002	      0
	 33		,005	    ,01
	 91		,005	    ,01
	 79		,005	    ,01
	 73		,005	    ,01
	 83		,005	    ,01
	 46		,005	    ,01
	100		,006	    ,07

8 filas seleccionadas.
```

## E178 · Contador por persona

**Objetivo:** Numerar las fotos de cada persona.  
**Explicación:** COUNT(*) OVER con ORDER BY cuenta acumulado.  
**Estado:** OK

```sql
SELECT id_usuario, id_consulta, COUNT(*) OVER (PARTITION BY id_usuario ORDER BY id_consulta) AS foto_numero FROM consulta_vision ORDER BY id_usuario, id_consulta FETCH FIRST 10 ROWS ONLY;
```

Salida real:

```text
ID_USUARIO ID_CONSULTA FOTO_NUMERO
---------- ----------- -----------
	 1	    32		 1
	 1	    66		 2
	 1	    97		 3
	 1	   133		 4
	 4	    10		 1
	 4	    27		 2
	 4	    54		 3
	 4	    55		 4
	 4	    67		 5
	 4	    68		 6

10 filas seleccionadas.
```

## E179 · Mejor del mes con RANK

**Objetivo:** El puesto de cada persona en el mes por verificados.  
**Explicación:** Agrupar primero y numerar después.  
**Estado:** OK

```sql
SELECT TO_CHAR(fecha_colombia, 'YYYY-MM') AS mes, titular, COUNT(*) AS verificados, RANK() OVER (PARTITION BY TO_CHAR(fecha_colombia, 'YYYY-MM') ORDER BY COUNT(*) DESC) AS puesto FROM vw_deposito_detalle WHERE estado_verificacion = 1 GROUP BY TO_CHAR(fecha_colombia, 'YYYY-MM'), titular ORDER BY mes, puesto FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
MES
-------
TITULAR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
VERIFICADOS	PUESTO
----------- ----------
2026-09
Jhon Zuleta Rojas
	  1	     1

2026-09
Camila Rojas Mendoza
	  1	     1

2026-10
Valentina Fuentes Ramírez
	  4	     1

2026-10
Carlos Vargas Barros
	  4	     1

2026-10
María Gómez Oñate
	  3	     3

2026-10
Luis Oñate Arias
	  3	     3

2026-10
Santiago Quintero Fuentes
	  3	     3

2026-10
Miguel Molina Castro
	  3	     3


8 filas seleccionadas.
```
