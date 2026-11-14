# SELECT WHERE ORDER BY

Ejercicios: 51 · conexión: `ECOACCESS_BD` · temas del banco: 2. SELECT básico; 3. Filtros con WHERE; 4. Ordenar y limitar.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E015 | Todas las columnas | OK |
| E016 | Columnas elegidas | OK |
| E017 | Alias de columna | OK |
| E018 | Columnas calculadas | OK |
| E019 | Concatenar texto | OK |
| E020 | Contar filas | OK |
| E021 | Ver una vista | OK |
| E022 | Valores distintos | OK |
| E023 | Primeras filas | OK |
| E024 | Expresión CASE | OK |
| E025 | Estaciones | OK |
| E026 | Canecas | OK |
| E027 | Catálogo de pesos | OK |
| E028 | Tarifas de EcoPuntos | OK |
| E029 | Literal y expresión | OK |
| E030 | Vista de saldos | OK |
| E031 | Igualdad | OK |
| E032 | Distinto | OK |
| E033 | Mayor que | OK |
| E034 | AND | OK |
| E035 | OR | OK |
| E036 | IN | OK |
| E037 | BETWEEN | OK |
| E038 | LIKE que empieza | OK |
| E039 | LIKE que contiene | OK |
| E040 | IS NULL | OK |
| E041 | El error de = NULL | OK |
| E042 | IS NOT NULL | OK |
| E043 | NOT | OK |
| E044 | Filtro por fecha | OK |
| E045 | Paréntesis | OK |
| E046 | Depósitos sin sensor | OK |
| E047 | Depósitos con peso medido | OK |
| E048 | Alta confianza de la IA | OK |
| E049 | LIKE con ESCAPE | OK |
| E050 | NOT IN con lista | OK |
| E051 | Canal simulador | OK |
| E052 | ORDER BY ascendente | OK |
| E053 | ORDER BY descendente | OK |
| E054 | Dos criterios | OK |
| E055 | NULLS LAST | OK |
| E056 | Ordenar por alias | OK |
| E057 | Paginación | OK |
| E058 | DISTINCT con varias columnas | OK |
| E059 | Empates con WITH TIES | OK |
| E060 | Orden personalizado | OK |
| E061 | OFFSET: segunda página | OK |
| E062 | Ordenar por posición | OK |
| E063 | NULLS FIRST | OK |
| E064 | Top por porcentaje | OK |
| E065 | Últimas fotos | OK |

## E015 · Todas las columnas

**Objetivo:** Ver la tabla ROL completa.  
**Explicación:** El asterisco (*) significa «todas las columnas».  
**Estado:** OK

```sql
SELECT * FROM rol;
```

Salida real:

```text
ID_ROL     CODIGO
---------- ----------
DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	 1	    1
Ciudadano

	 2	    2
Operario

	 3	    3
Administrador


3 filas seleccionadas.
```

## E016 · Columnas elegidas

**Objetivo:** Mostrar solo código y nombre de los materiales.  
**Explicación:** Escribir las columnas es más claro y más rápido que usar *.  
**Estado:** OK

```sql
SELECT codigo, nombre FROM material;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
RESTO_FRUTA_VERDURA
Restos de frutas y verduras

TETRAPAK
Envase multicapa

HOJAS_PODA
Hojas y poda

TAPABOCAS
Tapabocas

ICOPOR_SUCIO
Icopor contaminado

CASCARA_HUEVO
Cáscara de huevo

SERVILLETA_USADA
Servilleta o papel higiénico usado

CARTON
Cartón

PAPEL
Papel de oficina

VIDRIO
Vidrio

PLASTICO_RIGIDO
Envase plástico rígido

LATA_ALUMINIO
Lata de aluminio

… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)
```

## E017 · Alias de columna

**Objetivo:** Cambiar el título de una columna en el resultado.  
**Explicación:** AS pone un nombre más amigable; no cambia la tabla.  
**Estado:** OK

```sql
SELECT codigo AS "Código", nombre AS "Material" FROM material WHERE categoria = 2;
```

Salida real:

```text
Código
------------------------------------------------------------------------------------------------------------------------
Material
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
RESTO_FRUTA_VERDURA
Restos de frutas y verduras

HOJAS_PODA
Hojas y poda

CASCARA_HUEVO
Cáscara de huevo

BORRA_CAFE
Borra de café e infusiones

RESTO_COMIDA
Restos de comida


5 filas seleccionadas.
```

## E018 · Columnas calculadas

**Objetivo:** Pasar kilogramos a gramos.  
**Explicación:** Se puede operar con columnas: + - * /.  
**Estado:** OK

```sql
SELECT clave_objeto, gramos_tipico, gramos_tipico / 1000 AS kilos_tipicos FROM peso_referencial WHERE id_material = 101;
```

Salida real:

```text
CLAVE_OBJETO																			 GRAMOS_TIPICO KILOS_TIPICOS
---------------------------------------------------------------------------------------------------------------------------------------------------------------- ------------- -------------
BOTELLA_PET_600ML																			   300		  ,3
BOTELLA_PET_600ML																			   620		 ,62
BOTELLA_PET_600ML																			    22		,022
BOTELLA_PET_1500ML																			    38		,038

4 filas seleccionadas.
```

## E019 · Concatenar texto

**Objetivo:** Unir dos textos en una frase.  
**Explicación:** El operador   
**Estado:** OK

```sql
SELECT nombre || ' va en la caneca ' || DECODE(categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA', 'ESPECIAL') AS frase FROM material WHERE ROWNUM <= 5;
```

Salida real:

```text
FRASE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Restos de frutas y verduras va en la caneca VERDE
Envase multicapa va en la caneca BLANCA
Hojas y poda va en la caneca VERDE
Tapabocas va en la caneca NEGRA
Icopor contaminado va en la caneca NEGRA

5 filas seleccionadas.
```

## E020 · Contar filas

**Objetivo:** ¿Cuántos depósitos hay?  
**Explicación:** COUNT(*) cuenta filas.  
**Estado:** OK

```sql
SELECT COUNT(*) AS depositos FROM deposito;
```

Salida real:

```text
DEPOSITOS
----------
       105

1 fila seleccionada.
```

## E021 · Ver una vista

**Objetivo:** Consultar una vista igual que una tabla.  
**Explicación:** Una vista es una consulta guardada con nombre.  
**Estado:** OK

```sql
SELECT estacion, caneca, color, estado_texto FROM vw_caneca_estacion ORDER BY id_contenedor;
```

Salida real:

```text
ESTACION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
CANECA
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
COLOR  ESTADO_TEXTO
------ -----------------
Estación UPC Sabanas - bloque académico (SIMULACIÓN)
UPC01 - Caneca blanca
BLANCA EN SERVICIO

Estación UPC Sabanas - bloque académico (SIMULACIÓN)
UPC01 - Caneca verde
VERDE  EN SERVICIO

Estación UPC Sabanas - bloque académico (SIMULACIÓN)
UPC01 - Caneca negra
NEGRA  EN SERVICIO

Estación piloto legado QR por caneca (SIMULACIÓN)
Legado - Caneca blanca
BLANCA EN SERVICIO


4 filas seleccionadas.
```

## E022 · Valores distintos

**Objetivo:** ¿Qué canales se usaron?  
**Explicación:** DISTINCT elimina repetidos.  
**Estado:** OK

```sql
SELECT DISTINCT canal FROM interaccion ORDER BY canal;
```

Salida real:

```text
CANAL
----------
	 3
	 4

2 filas seleccionadas.
```

## E023 · Primeras filas

**Objetivo:** Ver solo 5 consultas de visión.  
**Explicación:** FETCH FIRST n ROWS ONLY limita el resultado.  
**Estado:** OK

```sql
SELECT id_consulta, clave_objeto, peso_estimado_kg FROM consulta_vision ORDER BY id_consulta FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_CONSULTA CLAVE_OBJETO																		     PESO_ESTIMADO_KG
----------- ---------------------------------------------------------------------------------------------------------------------------------------------------------------- ----------------
	  1 CAJA_PIZZA																					  ,25
	  2 CAJA_CARTON_PEQUENA 																			  ,36
	  3 TAPABOCAS_DESECHABLE																			 ,008
	  4 LATA_330ML																					 ,028
	  5 CAJA_TETRAPAK_1L																				 ,084

5 filas seleccionadas.
```

## E024 · Expresión CASE

**Objetivo:** Traducir el estado de verificación.  
**Explicación:** CASE es un «si… entonces…» dentro de SQL.  
**Estado:** OK

```sql
SELECT id_deposito, CASE estado_verificacion WHEN 0 THEN 'Pendiente' WHEN 1 THEN 'Verificado' ELSE 'Rechazado' END AS estado FROM deposito FETCH FIRST 6 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO ESTADO
----------- ----------
	  1 Verificado
	  2 Pendiente
	  3 Rechazado
	  4 Verificado
	  5 Verificado
	  6 Verificado

6 filas seleccionadas.
```

## E025 · Estaciones

**Objetivo:** Ver las estaciones y su QR.  
**Explicación:** PUNTO_INSTALACION es la estación física; el QR identifica la estación, no la caneca.  
**Estado:** OK

```sql
SELECT id_punto_instalacion, nombre, codigo_qr_estacion, activo FROM punto_instalacion;
```

Salida real:

```text
ID_PUNTO_INSTALACION
--------------------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
CODIGO_QR_ESTACION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    ACTIVO
----------
		   1
Estación UPC Sabanas - bloque académico (SIMULACIÓN)
EcoAccessEstacionUPC01
	 1

		   2
Estación piloto legado QR por caneca (SIMULACIÓN)

	 1


2 filas seleccionadas.
```

## E026 · Canecas

**Objetivo:** Ver las canecas con su color en número.  
**Explicación:** categoria: 1 BLANCA, 2 VERDE, 3 NEGRA.  
**Estado:** OK

```sql
SELECT id_contenedor, nombre, categoria, estado FROM contenedor ORDER BY id_contenedor;
```

Salida real:

```text
ID_CONTENEDOR
-------------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 CATEGORIA     ESTADO
---------- ----------
	   11
UPC01 - Caneca blanca
	 1	    1

	   12
UPC01 - Caneca verde
	 2	    1

	   13
UPC01 - Caneca negra
	 3	    1

	   21
Legado - Caneca blanca
	 1	    1


4 filas seleccionadas.
```

## E027 · Catálogo de pesos

**Objetivo:** Ver los gramos típicos de algunos objetos.  
**Explicación:** PESO_REFERENCIAL sirve para ESTIMAR, nunca para medir.  
**Estado:** OK

```sql
SELECT clave_objeto, estado_visual, gramos_minimo, gramos_tipico, gramos_maximo FROM peso_referencial FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
CLAVE_OBJETO																			 ESTADO_VISUAL GRAMOS_MINIMO
---------------------------------------------------------------------------------------------------------------------------------------------------------------- ------------- -------------
GRAMOS_TIPICO GRAMOS_MAXIMO
------------- -------------
CAJA_CARTON_PEQUENA																			     0		  50
	  120		250

BOTELLA_PET_600ML																			     2		 100
	  300		450

LATA_CONSERVA																				     1		  40
	   55		 70

PUNADO_HOJAS																				     0		   5
	   20		100

SERVILLETA																				     0		   1
	    2		  4

BOTELLA_PET_600ML																			     3		 560
	  620		650

BORRA_CAFE_PORCION																			     0		  10
	   20		 40

BOTELLA_PET_600ML																			     1		  18
	   22		 28


8 filas seleccionadas.
```

## E028 · Tarifas de EcoPuntos

**Objetivo:** Ver las reglas de puntos.  
**Explicación:** Las tarifas de esta base son SINTÉTICAS (es_sintetica = 1).  
**Estado:** OK

```sql
SELECT id_regla, id_material, puntos, por_unidad, es_sintetica FROM regla_ecopuntos ORDER BY id_material, desde_utc;
```

Salida real:

```text
ID_REGLA ID_MATERIAL	   PUNTOS POR_UNIDAD ES_SINTETICA
---------- ----------- ---------- ---------- ------------
	 1	   101	       10	   1		1
	 8	   101	       12	   1		1
	 5	   103	       15	   0		1
	 6	   104		5	   0		1
	 3	   105		8	   1		1
	 2	   106	       12	   1		1
	 4	   108	       10	   1		1
	 7	   201		4	   0		1

8 filas seleccionadas.
```

## E029 · Literal y expresión

**Objetivo:** Mostrar un texto fijo junto a cada material.  
**Explicación:** Se puede seleccionar un valor que no está en la tabla.  
**Estado:** OK

```sql
SELECT codigo, 'pertenece al catálogo EcoAccess' AS nota FROM material WHERE ROWNUM <= 5;
```

Salida real:

```text
CODIGO															 NOTA
------------------------------------------------------------------------------------------------------------------------ --------------------------------
RESTO_FRUTA_VERDURA                                                                                                      pertenece al catálogo EcoAccess
TETRAPAK                                                                                                                 pertenece al catálogo EcoAccess
HOJAS_PODA                                                                                                               pertenece al catálogo EcoAccess
TAPABOCAS                                                                                                                pertenece al catálogo EcoAccess
ICOPOR_SUCIO                                                                                                             pertenece al catálogo EcoAccess

5 filas seleccionadas.
```

## E030 · Vista de saldos

**Objetivo:** Ver los saldos de EcoPuntos ya calculados.  
**Explicación:** La vista suma el libro de movimientos; el saldo no se guarda en ninguna tabla.  
**Estado:** OK

```sql
SELECT nombre, saldo, acreditados, revertidos FROM vw_saldo_ecopuntos WHERE saldo > 0 ORDER BY saldo DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     SALDO ACREDITADOS REVERTIDOS
---------- ----------- ----------
Camila Rojas Mendoza
	90	    90		0

Ana Castro Vargas
	72	    72		0

María Gómez Oñate
	64	    64		0

Carlos Vargas Barros
	53	    53		0

Karen Fuentes Ramírez
	49	    49		0


5 filas seleccionadas.
```

## E031 · Igualdad

**Objetivo:** Materiales de la caneca verde.  
**Explicación:** WHERE deja pasar solo las filas que cumplen la condición.  
**Estado:** OK

```sql
SELECT codigo, nombre FROM material WHERE categoria = 2;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
RESTO_FRUTA_VERDURA
Restos de frutas y verduras

HOJAS_PODA
Hojas y poda

CASCARA_HUEVO
Cáscara de huevo

BORRA_CAFE
Borra de café e infusiones

RESTO_COMIDA
Restos de comida


5 filas seleccionadas.
```

## E032 · Distinto

**Objetivo:** Materiales que NO son peligrosos.  
**Explicación:** <> significa «distinto de».  
**Estado:** OK

```sql
SELECT codigo FROM material WHERE categoria <> 4 ORDER BY codigo;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
BORRA_CAFE
CARTON
CARTON_CONTAMINADO
CASCARA_HUEVO
EMPAQUE_METALIZADO
HOJAS_PODA
ICOPOR_SUCIO
LATA_ALUMINIO
METAL_FERROSO
PAPEL
PET
PLASTICO_RIGIDO
RESTO_COMIDA
RESTO_FRUTA_VERDURA
SERVILLETA_USADA
TAPABOCAS
TETRAPAK
VIDRIO

18 filas seleccionadas.
```

## E033 · Mayor que

**Objetivo:** Pesos típicos de más de 100 g.  
**Explicación:** Comparadores: > < >= <=.  
**Estado:** OK

```sql
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico > 100 ORDER BY gramos_tipico;
```

Salida real:

```text
DESCRIPCION_OBJETO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
GRAMOS_TIPICO
-------------
Caja de cartón pequeña
	  120

Sobras de un plato de comida
	  150

Botella de vidrio de 330 ml
	  200

Caja de pizza con grasa
	  250

Botella PET de 600 ml
	  300

Botella PET de 600 ml
	  620


6 filas seleccionadas.
```

## E034 · AND

**Objetivo:** Depósitos verificados y medidos con báscula.  
**Explicación:** AND exige que se cumplan las dos condiciones.  
**Estado:** OK

```sql
SELECT id_deposito, peso_estimado_kg, peso_medido_kg FROM deposito WHERE estado_verificacion = 1 AND peso_medido_kg IS NOT NULL;
```

Salida real:

```text
ID_DEPOSITO PESO_ESTIMADO_KG PESO_MEDIDO_KG
----------- ---------------- --------------
	 23		  ,2	      ,2106
	 28		,014	      ,0127
	 32		,028	      ,0313
	 34		 ,36	      ,4113
	 36		  ,6	      ,5613
	 38		,015	      ,0145
	 44		,015	      ,0163
	 45		 ,15	      ,1457
	 47		  ,4	      ,4148
	 75		 ,02	      ,0191
	 87		,042	      ,0458
	 91		,005	      ,0055
	 95		 ,01	      ,0101
	101		  ,6	       ,543

14 filas seleccionadas.
```

## E035 · OR

**Objetivo:** Depósitos pendientes o rechazados.  
**Explicación:** OR basta con que se cumpla una.  
**Estado:** OK

```sql
SELECT COUNT(*) AS por_revisar FROM deposito WHERE estado_verificacion = 0 OR estado_verificacion = 2;
```

Salida real:

```text
POR_REVISAR
-----------
	 29

1 fila seleccionada.
```

## E036 · IN

**Objetivo:** Lo mismo que el anterior, más corto.  
**Explicación:** IN compara contra una lista.  
**Estado:** OK

```sql
SELECT COUNT(*) AS por_revisar FROM deposito WHERE estado_verificacion IN (0, 2);
```

Salida real:

```text
POR_REVISAR
-----------
	 29

1 fila seleccionada.
```

## E037 · BETWEEN

**Objetivo:** Pesos típicos entre 10 y 50 g.  
**Explicación:** BETWEEN incluye los dos extremos.  
**Estado:** OK

```sql
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico BETWEEN 10 AND 50 ORDER BY gramos_tipico;
```

Salida real:

```text
DESCRIPCION_OBJETO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
GRAMOS_TIPICO
-------------
Envase de yogur individual
	   12

Lata de aluminio de 330 ml
	   14

Puñado de hojas secas
	   20

Borra de café de una preparación
	   20

Botella PET de 600 ml
	   22

Pila AA
	   23

Caja multicapa de 1 L
	   28

Cáscara de banano
	   30

Botella PET de 1,5 L
	   38


9 filas seleccionadas.
```

## E038 · LIKE que empieza

**Objetivo:** Materiales cuyo código empieza por C.  
**Explicación:** % significa «cualquier texto».  
**Estado:** OK

```sql
SELECT codigo FROM material WHERE codigo LIKE 'C%';
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
CARTON
CARTON_CONTAMINADO
CASCARA_HUEVO

3 filas seleccionadas.
```

## E039 · LIKE que contiene

**Objetivo:** Nombres que contienen «Gómez».  
**Explicación:** '%texto%' busca en cualquier posición.  
**Estado:** OK

```sql
SELECT nombre FROM usuario WHERE nombre LIKE '%Gómez%' ORDER BY nombre;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Camila Rodríguez Gómez
Kevin Gómez Oñate
María Gómez Oñate
Mónica Gómez Oñate
Sebastián Rodríguez Gómez

5 filas seleccionadas.
```

## E040 · IS NULL

**Objetivo:** Depósitos sin peso medido.  
**Explicación:** Para preguntar por NULL se usa IS NULL, nunca = NULL.  
**Estado:** OK

```sql
SELECT COUNT(*) AS sin_medicion FROM deposito WHERE peso_medido_kg IS NULL;
```

Salida real:

```text
SIN_MEDICION
------------
	  91

1 fila seleccionada.
```

## E041 · El error de = NULL

**Objetivo:** Ver por qué = NULL no funciona.  
**Explicación:** NULL = NULL no es verdadero sino «desconocido»: devuelve 0 filas.  
**Estado:** OK

```sql
SELECT COUNT(*) AS filas FROM deposito WHERE peso_medido_kg = NULL;
```

Salida real:

```text
FILAS
----------
	 0

1 fila seleccionada.
```

## E042 · IS NOT NULL

**Objetivo:** Consultas de foto hechas dentro de una sesión QR.  
**Explicación:** IS NOT NULL = tiene valor.  
**Estado:** OK

```sql
SELECT COUNT(*) AS con_qr FROM consulta_vision WHERE id_interaccion IS NOT NULL;
```

Salida real:

```text
CON_QR
----------
       147

1 fila seleccionada.
```

## E043 · NOT

**Objetivo:** Usuarios que NO son sintéticos.  
**Explicación:** NOT invierte la condición (en esta base no hay reales: 0).  
**Estado:** OK

```sql
SELECT COUNT(*) AS reales FROM usuario WHERE NOT es_sintetico = 1;
```

Salida real:

```text
REALES
----------
	 0

1 fila seleccionada.
```

## E044 · Filtro por fecha

**Objetivo:** Depósitos de los últimos 7 días.  
**Explicación:** Restar un INTERVAL a la hora actual da el límite.  
**Estado:** OK

```sql
SELECT COUNT(*) AS ultimos_7_dias FROM deposito WHERE fecha_deteccion_utc >= SYS_EXTRACT_UTC(SYSTIMESTAMP) - INTERVAL '7' DAY;
```

Salida real:

```text
ULTIMOS_7_DIAS
--------------
	    35

1 fila seleccionada.
```

## E045 · Paréntesis

**Objetivo:** Prioridad entre AND y OR.  
**Explicación:** AND se evalúa antes que OR: los paréntesis dejan clara la intención.  
**Estado:** OK

```sql
SELECT COUNT(*) FROM deposito WHERE sin_sensor = 1 AND (estado_verificacion = 0 OR estado_verificacion = 2);
```

Salida real:

```text
COUNT(*)
----------
	10

1 fila seleccionada.
```

## E046 · Depósitos sin sensor

**Objetivo:** Ver los depósitos declarados sin sensor.  
**Explicación:** sin_sensor = 1: la caneca cerró sin detectar el objeto.  
**Estado:** OK

```sql
SELECT id_deposito, id_material_declarado, estado_verificacion FROM deposito WHERE sin_sensor = 1 FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO ID_MATERIAL_DECLARADO ESTADO_VERIFICACION
----------- --------------------- -------------------
	  5		      108		    1
	  6		      107		    1
	  8		      202		    0
	 14		      303		    1
	 20		      107		    1
	 21		      105		    0
	 22		      304		    0
	 23		      105		    1

8 filas seleccionadas.
```

## E047 · Depósitos con peso medido

**Objetivo:** Ver solo los que pasaron por una báscula.  
**Explicación:** IS NOT NULL encuentra los que sí tienen medición.  
**Estado:** OK

```sql
SELECT id_deposito, peso_estimado_kg, peso_medido_kg, instrumento_medicion FROM deposito WHERE peso_medido_kg IS NOT NULL;
```

Salida real:

```text
ID_DEPOSITO PESO_ESTIMADO_KG PESO_MEDIDO_KG
----------- ---------------- --------------
INSTRUMENTO_MEDICION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	 23		  ,2	      ,2106
Báscula digital (SIMULADA)

	 28		,014	      ,0127
Báscula digital (SIMULADA)

	 32		,028	      ,0313
Báscula digital (SIMULADA)

	 34		 ,36	      ,4113
Báscula digital (SIMULADA)

	 36		  ,6	      ,5613
Báscula digital (SIMULADA)

	 38		,015	      ,0145
Báscula digital (SIMULADA)

	 44		,015	      ,0163
Báscula digital (SIMULADA)

	 45		 ,15	      ,1457
Báscula digital (SIMULADA)

	 47		  ,4	      ,4148
Báscula digital (SIMULADA)

	 75		 ,02	      ,0191
Báscula digital (SIMULADA)

	 87		,042	      ,0458
Báscula digital (SIMULADA)

	 91		,005	      ,0055
Báscula digital (SIMULADA)

… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)
```

## E048 · Alta confianza de la IA

**Objetivo:** Sesiones donde la sugerencia superó 90 %.  
**Explicación:** confianza_sugerida va de 0 a 1.  
**Estado:** OK

```sql
SELECT id_interaccion, id_material_sugerido, confianza_sugerida FROM interaccion WHERE confianza_sugerida >= 0.9 FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
ID_INTERACCION ID_MATERIAL_SUGERIDO CONFIANZA_SUGERIDA
-------------- -------------------- ------------------
	     2			103		  ,904
	     8			105		   ,91
	     9			202		  ,919
	    11			205		  ,964
	    14			302		    ,9
	    17			101		  ,949
	    35			108		  ,936
	    37			108		  ,923

8 filas seleccionadas.
```

## E049 · LIKE con ESCAPE

**Objetivo:** Códigos que contienen un guion bajo de verdad.  
**Explicación:** En LIKE, _ significa «cualquier letra»; con ESCAPE se busca el carácter literal.  
**Estado:** OK

```sql
SELECT codigo FROM material WHERE codigo LIKE '%\_%' ESCAPE '\' ORDER BY codigo;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
BORRA_CAFE
CARTON_CONTAMINADO
CASCARA_HUEVO
EMPAQUE_METALIZADO
HOJAS_PODA
ICOPOR_SUCIO
LATA_ALUMINIO
METAL_FERROSO
PLASTICO_RIGIDO
RESTO_COMIDA
RESTO_FRUTA_VERDURA
SERVILLETA_USADA

12 filas seleccionadas.
```

## E050 · NOT IN con lista

**Objetivo:** Materiales que NO son de la caneca blanca.  
**Explicación:** NOT IN (lista) excluye varios valores.  
**Estado:** OK

```sql
SELECT codigo, categoria FROM material WHERE categoria NOT IN (1) ORDER BY categoria, codigo;
```

Salida real:

```text
CODIGO															  CATEGORIA
------------------------------------------------------------------------------------------------------------------------ ----------
BORRA_CAFE															  2
CASCARA_HUEVO															  2
HOJAS_PODA															  2
RESTO_COMIDA															  2
RESTO_FRUTA_VERDURA														  2
CARTON_CONTAMINADO														  3
EMPAQUE_METALIZADO														  3
ICOPOR_SUCIO															  3
SERVILLETA_USADA														  3
TAPABOCAS															  3
BOMBILLO															  4
MEDICAMENTOS															  4
PILAS																  4

13 filas seleccionadas.
```

## E051 · Canal simulador

**Objetivo:** Recorridos hechos con el simulador.  
**Explicación:** canal 3 = Simulador, igual que el enum de C#.  
**Estado:** OK

```sql
SELECT COUNT(*) AS recorridos_simulador FROM interaccion WHERE canal = 3;
```

Salida real:

```text
RECORRIDOS_SIMULADOR
--------------------
		  32

1 fila seleccionada.
```

## E052 · ORDER BY ascendente

**Objetivo:** Materiales en orden alfabético.  
**Explicación:** ASC es el orden por defecto.  
**Estado:** OK

```sql
SELECT nombre FROM material ORDER BY nombre;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Bombillo o luminaria
Borra de café e infusiones
Botella plástica PET
Cartón
Cartón o papel con comida
Cáscara de huevo
Empaque metalizado
Envase multicapa
Envase plástico rígido
Hojas y poda
Icopor contaminado
Lata de acero
Lata de aluminio
Medicamentos vencidos
Papel de oficina
Pilas y baterías
Restos de comida
Restos de frutas y verduras
Servilleta o papel higiénico usado
Tapabocas
Vidrio

21 filas seleccionadas.
```

## E053 · ORDER BY descendente

**Objetivo:** Objetos más pesados primero.  
**Explicación:** DESC invierte el orden.  
**Estado:** OK

```sql
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial ORDER BY gramos_tipico DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
DESCRIPCION_OBJETO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
GRAMOS_TIPICO
-------------
Botella PET de 600 ml
	  620

Botella PET de 600 ml
	  300

Caja de pizza con grasa
	  250

Botella de vidrio de 330 ml
	  200

Sobras de un plato de comida
	  150


5 filas seleccionadas.
```

## E054 · Dos criterios

**Objetivo:** Ordenar por color y luego por nombre.  
**Explicación:** Si empatan en el primero, decide el segundo.  
**Estado:** OK

```sql
SELECT categoria, nombre FROM material ORDER BY categoria, nombre;
```

Salida real:

```text
CATEGORIA
----------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	 1
Botella plástica PET

	 1
Cartón

	 1
Envase multicapa

	 1
Envase plástico rígido

	 1
Lata de acero

	 1
Lata de aluminio

	 1
Papel de oficina

	 1
Vidrio

	 2
Borra de café e infusiones

	 2
Cáscara de huevo

	 2
Hojas y poda

	 2
Restos de comida

… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)
```

## E055 · NULLS LAST

**Objetivo:** Pesos medidos, dejando los vacíos al final.  
**Explicación:** Por defecto los NULL van al final en ASC; se puede elegir.  
**Estado:** OK

```sql
SELECT id_deposito, peso_medido_kg FROM deposito ORDER BY peso_medido_kg DESC NULLS LAST FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO PESO_MEDIDO_KG
----------- --------------
	 36	     ,5613
	101	      ,543
	 47	     ,4148
	 34	     ,4113
	 23	     ,2106

5 filas seleccionadas.
```

## E056 · Ordenar por alias

**Objetivo:** Ordenar por una columna calculada.  
**Explicación:** Se puede ordenar por el alias.  
**Estado:** OK

```sql
SELECT clave_objeto, gramos_maximo - gramos_minimo AS rango FROM peso_referencial ORDER BY rango DESC FETCH FIRST 3 ROWS ONLY;
```

Salida real:

```text
CLAVE_OBJETO																			      RANGO
---------------------------------------------------------------------------------------------------------------------------------------------------------------- ----------
BOTELLA_PET_600ML																			350
SOBRA_PLATO																				350
CAJA_PIZZA																				250

3 filas seleccionadas.
```

## E057 · Paginación

**Objetivo:** Ver la «página 2» de materiales (filas 6 a 10).  
**Explicación:** OFFSET salta filas; útil en pantallas con páginas.  
**Estado:** OK

```sql
SELECT codigo FROM material ORDER BY codigo OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
EMPAQUE_METALIZADO
HOJAS_PODA
ICOPOR_SUCIO
LATA_ALUMINIO
MEDICAMENTOS

5 filas seleccionadas.
```

## E058 · DISTINCT con varias columnas

**Objetivo:** Combinaciones únicas de canal y modo.  
**Explicación:** DISTINCT mira la fila completa.  
**Estado:** OK

```sql
SELECT DISTINCT canal, modo FROM consulta_vision ORDER BY canal, modo;
```

Salida real:

```text
CANAL	 MODO
---------- ----------
	 1	    1
	 1	    2

2 filas seleccionadas.
```

## E059 · Empates con WITH TIES

**Objetivo:** Top 3 de pesos incluyendo empates.  
**Explicación:** WITH TIES agrega las filas que empatan con la última.  
**Estado:** OK

```sql
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial ORDER BY gramos_tipico FETCH FIRST 3 ROWS WITH TIES;
```

Salida real:

```text
DESCRIPCION_OBJETO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
GRAMOS_TIPICO
-------------
Servilleta de papel usada
	    2

Bolsa de papas o galletas
	    4

Tapabocas desechable
	    4


3 filas seleccionadas.
```

## E060 · Orden personalizado

**Objetivo:** Ordenar canecas: verde, blanca, negra.  
**Explicación:** CASE dentro de ORDER BY permite cualquier orden.  
**Estado:** OK

```sql
SELECT nombre, categoria FROM contenedor WHERE id_punto_instalacion = 1 ORDER BY CASE categoria WHEN 2 THEN 1 WHEN 1 THEN 2 ELSE 3 END;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 CATEGORIA
----------
UPC01 - Caneca verde
	 2

UPC01 - Caneca blanca
	 1

UPC01 - Caneca negra
	 3


3 filas seleccionadas.
```

## E061 · OFFSET: segunda página

**Objetivo:** Ver los materiales 6 a 10.  
**Explicación:** OFFSET salta filas; FETCH NEXT toma las siguientes.  
**Estado:** OK

```sql
SELECT codigo FROM material ORDER BY codigo OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
EMPAQUE_METALIZADO
HOJAS_PODA
ICOPOR_SUCIO
LATA_ALUMINIO
MEDICAMENTOS

5 filas seleccionadas.
```

## E062 · Ordenar por posición

**Objetivo:** ORDER BY 2 ordena por la segunda columna.  
**Explicación:** Funciona, pero es menos claro que usar el nombre.  
**Estado:** OK

```sql
SELECT codigo, nombre FROM material ORDER BY 2 FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
BOMBILLO
Bombillo o luminaria

BORRA_CAFE
Borra de café e infusiones

PET
Botella plástica PET

CARTON
Cartón

CARTON_CONTAMINADO
Cartón o papel con comida


5 filas seleccionadas.
```

## E063 · NULLS FIRST

**Objetivo:** Poner primero los depósitos sin verificador.  
**Explicación:** Por defecto, en ASC los NULL van al final.  
**Estado:** OK

```sql
SELECT id_deposito, id_verificador FROM deposito ORDER BY id_verificador NULLS FIRST FETCH FIRST 6 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO ID_VERIFICADOR
----------- --------------
	  2
	 21
	 19
	 18
	 12
	  8

6 filas seleccionadas.
```

## E064 · Top por porcentaje

**Objetivo:** El 10 % de depósitos más pesados (estimado).  
**Explicación:** FETCH FIRST n PERCENT toma un porcentaje de las filas.  
**Estado:** OK

```sql
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg IS NOT NULL ORDER BY peso_estimado_kg DESC FETCH FIRST 10 PERCENT ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO PESO_ESTIMADO_KG
----------- ----------------
	101		  ,6
	 54		  ,6
	 42		  ,6
	 36		  ,6
	 47		  ,4
	 21		  ,4
	  2		 ,36
	 65		 ,36
	 52		 ,36
	 39		 ,36
	 34		 ,36

11 filas seleccionadas.
```

## E065 · Últimas fotos

**Objetivo:** Las 5 consultas de foto más recientes.  
**Explicación:** DESC pone primero lo más nuevo.  
**Estado:** OK

```sql
SELECT id_consulta, resultado, fecha_utc FROM consulta_vision ORDER BY fecha_utc DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_CONSULTA  RESULTADO FECHA_UTC
----------- ---------- ---------------------------------------------------------------------------
	144	     1 11/11/26 08:19:53,615532 AM
	 72	     1 11/11/26 07:38:53,980243 AM
	 71	     2 11/11/26 07:38:46,980243 AM
	113	     1 11/11/26 06:00:04,959318 AM
	112	     2 11/11/26 05:59:57,959318 AM

5 filas seleccionadas.
```
