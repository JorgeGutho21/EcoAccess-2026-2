# JOIN

Ejercicios: 23 · conexión: `ECOACCESS_BD` · temas del banco: 7. Uniones (JOIN).

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E110 | INNER JOIN | OK |
| E111 | JOIN con tabla puente | OK |
| E112 | Varios JOIN | OK |
| E113 | LEFT JOIN | OK |
| E114 | LEFT JOIN + IS NULL | OK |
| E115 | RIGHT JOIN | OK |
| E116 | FULL OUTER JOIN | OK |
| E117 | SELF JOIN | OK |
| E118 | CROSS JOIN | OK |
| E119 | JOIN con USING | OK |
| E120 | JOIN para verificador | OK |
| E121 | Recorrido completo | OK |
| E122 | JOIN y GROUP BY | OK |
| E123 | Eventos por tipo | OK |
| E124 | Tarifa aplicada | OK |
| E125 | Qué recibe cada caneca | OK |
| E126 | Pesos con su material | OK |
| E127 | Movimientos con el titular | OK |
| E128 | Auditoría con el actor | OK |
| E129 | Eventos de una orden | OK |
| E130 | Fotos con material (LEFT) | OK |
| E131 | JOIN no igualitario | OK |
| E132 | Roles de cada persona | OK |

## E110 · INNER JOIN

**Objetivo:** Canecas con el nombre de su estación.  
**Explicación:** JOIN une filas que coinciden en la clave.  
**Estado:** OK

```sql
SELECT c.nombre AS caneca, p.nombre AS estacion FROM contenedor c JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion;
```

Salida real:

```text
CANECA
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
ESTACION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
UPC01 - Caneca blanca
Estación UPC Sabanas - bloque académico (SIMULACIÓN)

UPC01 - Caneca verde
Estación UPC Sabanas - bloque académico (SIMULACIÓN)

Legado - Caneca blanca
Estación piloto legado QR por caneca (SIMULACIÓN)

UPC01 - Caneca negra
Estación UPC Sabanas - bloque académico (SIMULACIÓN)


4 filas seleccionadas.
```

## E111 · JOIN con tabla puente

**Objetivo:** Roles de cada persona del personal.  
**Explicación:** USUARIO_ROL une USUARIO y ROL (N:M).  
**Estado:** OK

```sql
SELECT u.nombre, r.descripcion FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo IN (2, 3) ORDER BY u.nombre;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Administradora Demo (SINTÉTICO)
Administrador

Operaria Demo Dos (SINTÉTICO)
Operario

Operario Demo Uno (SINTÉTICO)
Operario


3 filas seleccionadas.
```

## E112 · Varios JOIN

**Objetivo:** Depósito con titular, caneca y material.  
**Explicación:** Se sigue la cadena depósito → orden → sesión → usuario.  
**Estado:** OK

```sql
SELECT d.id_deposito, u.nombre, c.nombre AS caneca, m.codigo FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario u ON u.id_usuario = i.id_usuario JOIN contenedor c ON c.id_contenedor = o.id_contenedor JOIN material m ON m.id_material = d.id_material_declarado FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO
-----------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
CANECA
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
CODIGO
------------------------------------------------------------------------------------------------------------------------
	  1
Valentina Fuentes Ramírez
UPC01 - Caneca negra
CARTON_CONTAMINADO

	  2
Valentina Fuentes Ramírez
UPC01 - Caneca blanca
CARTON

	  3
Miguel Molina Castro
UPC01 - Caneca negra
TAPABOCAS

	  4
María Gómez Oñate
UPC01 - Caneca blanca
LATA_ALUMINIO

	  5
Miguel Molina Castro
UPC01 - Caneca blanca
TETRAPAK


5 filas seleccionadas.
```

## E113 · LEFT JOIN

**Objetivo:** Todos los materiales y cuántas veces se verificaron (incluye 0).  
**Explicación:** LEFT JOIN conserva las filas de la izquierda aunque no tengan pareja.  
**Estado:** OK

```sql
SELECT m.codigo, COUNT(d.id_deposito) AS verificaciones FROM material m LEFT JOIN deposito d ON d.id_material_verificado = m.id_material GROUP BY m.codigo ORDER BY verificaciones DESC, m.codigo;
```

Salida real:

```text
CODIGO															 VERIFICACIONES
------------------------------------------------------------------------------------------------------------------------ --------------
LATA_ALUMINIO															      9
PAPEL																      9
CARTON																      8
PET																      6
TETRAPAK															      6
VIDRIO																      6
METAL_FERROSO															      5
PLASTICO_RIGIDO 														      4
RESTO_COMIDA															      4
SERVILLETA_USADA														      4
BORRA_CAFE															      3
ICOPOR_SUCIO															      3
TAPABOCAS															      3
CARTON_CONTAMINADO														      2
HOJAS_PODA															      2
RESTO_FRUTA_VERDURA														      2
BOMBILLO															      0
CASCARA_HUEVO															      0
EMPAQUE_METALIZADO														      0
MEDICAMENTOS															      0
PILAS																      0

21 filas seleccionadas.
```

## E114 · LEFT JOIN + IS NULL

**Objetivo:** Materiales que nunca se han verificado.  
**Explicación:** Las filas sin pareja tienen NULL en la tabla derecha.  
**Estado:** OK

```sql
SELECT m.codigo FROM material m LEFT JOIN deposito d ON d.id_material_verificado = m.id_material WHERE d.id_deposito IS NULL ORDER BY m.codigo;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
BOMBILLO
CASCARA_HUEVO
EMPAQUE_METALIZADO
MEDICAMENTOS
PILAS

5 filas seleccionadas.
```

## E115 · RIGHT JOIN

**Objetivo:** Lo mismo, escrito al revés.  
**Explicación:** RIGHT JOIN conserva las de la derecha.  
**Estado:** OK

```sql
SELECT m.codigo, COUNT(d.id_deposito) AS veces FROM deposito d RIGHT JOIN material m ON m.id_material = d.id_material_declarado GROUP BY m.codigo ORDER BY veces DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
CODIGO															      VECES
------------------------------------------------------------------------------------------------------------------------ ----------
PAPEL																 14
CARTON																 13
LATA_ALUMINIO															 10
METAL_FERROSO															  9
VIDRIO																  8

5 filas seleccionadas.
```

## E116 · FULL OUTER JOIN

**Objetivo:** Canecas y materiales, tengan o no pareja.  
**Explicación:** FULL conserva todas las filas de ambos lados.  
**Estado:** OK

```sql
SELECT c.id_contenedor, m.codigo FROM contenedor_material cm FULL OUTER JOIN material m ON m.id_material = cm.id_material LEFT JOIN contenedor c ON c.id_contenedor = cm.id_contenedor WHERE c.id_contenedor IS NULL;
```

Salida real:

```text
ID_CONTENEDOR CODIGO
------------- ------------------------------------------------------------------------------------------------------------------------
	      MEDICAMENTOS
	      BOMBILLO
	      PILAS

3 filas seleccionadas.
```

## E117 · SELF JOIN

**Objetivo:** Crédito y su reverso (la tabla consigo misma).  
**Explicación:** Se usa la misma tabla con dos alias.  
**Estado:** OK

```sql
SELECT r.id_movimiento AS reverso, r.puntos, c.id_movimiento AS credito, c.puntos AS puntos_credito FROM movimiento_ecopuntos r JOIN movimiento_ecopuntos c ON c.id_movimiento = r.id_movimiento_revertido;
```

Salida real:

```text
REVERSO     PUNTOS	 CREDITO PUNTOS_CREDITO
---------- ---------- ---------- --------------
	47	  -30	       2	     30

1 fila seleccionada.
```

## E118 · CROSS JOIN

**Objetivo:** Todas las combinaciones de rol y estación.  
**Explicación:** Producto cartesiano: 3 roles × 2 estaciones = 6 filas.  
**Estado:** OK

```sql
SELECT r.descripcion, p.nombre FROM rol r CROSS JOIN punto_instalacion p;
```

Salida real:

```text
DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Ciudadano
Estación UPC Sabanas - bloque académico (SIMULACIÓN)

Operario
Estación UPC Sabanas - bloque académico (SIMULACIÓN)

Administrador
Estación UPC Sabanas - bloque académico (SIMULACIÓN)

Ciudadano
Estación piloto legado QR por caneca (SIMULACIÓN)

Operario
Estación piloto legado QR por caneca (SIMULACIÓN)

Administrador
Estación piloto legado QR por caneca (SIMULACIÓN)


6 filas seleccionadas.
```

## E119 · JOIN con USING

**Objetivo:** Sintaxis corta cuando la columna se llama igual.  
**Explicación:** USING(columna) evita repetir el ON.  
**Estado:** OK

```sql
SELECT codigo, clave_objeto FROM material JOIN peso_referencial USING (id_material) WHERE codigo = 'PET';
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
CLAVE_OBJETO
----------------------------------------------------------------------------------------------------------------------------------------------------------------
PET
BOTELLA_PET_600ML

PET
BOTELLA_PET_600ML

PET
BOTELLA_PET_600ML

PET
BOTELLA_PET_1500ML


4 filas seleccionadas.
```

## E120 · JOIN para verificador

**Objetivo:** Quién verificó cada depósito.  
**Explicación:** La misma tabla USUARIO aparece dos veces con papeles distintos.  
**Estado:** OK

```sql
SELECT d.id_deposito, t.nombre AS titular, v.nombre AS verificador FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion JOIN usuario t ON t.id_usuario = i.id_usuario JOIN usuario v ON v.id_usuario = d.id_verificador FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO
-----------
TITULAR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
VERIFICADOR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	  1
Valentina Fuentes Ramírez
Operario Demo Uno (SINTÉTICO)

	  3
Miguel Molina Castro
Operario Demo Uno (SINTÉTICO)

	  4
María Gómez Oñate
Operario Demo Uno (SINTÉTICO)

	  9
Ana Castro Vargas
Operario Demo Uno (SINTÉTICO)

	 10
Miguel Maestre Pérez
Operario Demo Uno (SINTÉTICO)


5 filas seleccionadas.
```

## E121 · Recorrido completo

**Objetivo:** Sesión → orden → evento → depósito.  
**Explicación:** Ver cada evidencia separada.  
**Estado:** OK

```sql
SELECT id_interaccion, estado_sesion, fotos_consultadas, estado_orden, aperturas_confirmadas, id_deposito FROM vw_recorrido FETCH FIRST 6 ROWS ONLY;
```

Salida real:

```text
ID_INTERACCION ESTADO_SESIO FOTOS_CONSULTADAS ESTADO_OR APERTURAS_CONFIRMADAS ID_DEPOSITO
-------------- ------------ ----------------- --------- --------------------- -----------
	     1 CON DEPOSITO		    1 CERRADA			    1		1
	     2 CON DEPOSITO		    1 CERRADA			    1		2
	     3 CON DEPOSITO		    1 CERRADA			    1		3
	     4 CON DEPOSITO		    1 CERRADA			    1		4
	     5 CON DEPOSITO		    1 CERRADA			    1		5
	     6 SIN DEPOSITO		    1 VENCIDA			    0

6 filas seleccionadas.
```

## E122 · JOIN y GROUP BY

**Objetivo:** Depósitos por estación.  
**Explicación:** Primero se une, luego se agrupa.  
**Estado:** OK

```sql
SELECT p.nombre, COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN contenedor c ON c.id_contenedor = o.id_contenedor JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion GROUP BY p.nombre;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  COUNT(*)
----------
Estación UPC Sabanas - bloque académico (SIMULACIÓN)
	92

Estación piloto legado QR por caneca (SIMULACIÓN)
	13


2 filas seleccionadas.
```

## E123 · Eventos por tipo

**Objetivo:** Cuántos eventos de cada tipo.  
**Explicación:** Ver aperturas vs detecciones.  
**Estado:** OK

```sql
SELECT DECODE(tipo,1,'Acuse',2,'Apertura',3,'Detección',4,'Cierre',5,'Fallo',6,'Cierre sin sensor') AS tipo, COUNT(*) FROM evento_dispositivo GROUP BY tipo ORDER BY tipo;
```

Salida real:

```text
TIPO		    COUNT(*)
----------------- ----------
Acuse			 114
Apertura		 105
Cierre sin sensor	  33
Detección                 72

4 filas seleccionadas.
```

## E124 · Tarifa aplicada

**Objetivo:** Movimiento con su tarifa y material.  
**Explicación:** JOIN de 3 tablas del módulo de incentivos.  
**Estado:** OK

```sql
SELECT m.id_movimiento, mt.codigo, r.puntos AS tarifa, r.por_unidad, m.puntos FROM movimiento_ecopuntos m JOIN regla_ecopuntos r ON r.id_regla = m.id_regla JOIN material mt ON mt.id_material = r.id_material FETCH FIRST 6 ROWS ONLY;
```

Salida real:

```text
ID_MOVIMIENTO CODIGO															   TARIFA POR_UNIDAD	 PUNTOS
------------- ------------------------------------------------------------------------------------------------------------------------ ---------- ---------- ----------
	    4 PET															       10	   1	     10
	   22 PET															       10	   1	     30
	   46 PET															       10	   1	     20
	    1 LATA_ALUMINIO														       12	   1	     12
	    5 LATA_ALUMINIO														       12	   1	     24
	   10 LATA_ALUMINIO														       12	   1	     36

6 filas seleccionadas.
```

## E125 · Qué recibe cada caneca

**Objetivo:** Nombre de caneca y nombre de material.  
**Explicación:** Dos JOIN a través de la tabla puente CONTENEDOR_MATERIAL.  
**Estado:** OK

```sql
SELECT c.nombre AS caneca, m.nombre AS material FROM contenedor c JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor JOIN material m ON m.id_material = cm.id_material WHERE c.id_punto_instalacion = 1 ORDER BY c.nombre, m.nombre;
```

Salida real:

```text
CANECA
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
MATERIAL
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
UPC01 - Caneca blanca
Botella plástica PET

UPC01 - Caneca blanca
Cartón

UPC01 - Caneca blanca
Envase multicapa

UPC01 - Caneca blanca
Envase plástico rígido

UPC01 - Caneca blanca
Lata de acero

UPC01 - Caneca blanca
Lata de aluminio

UPC01 - Caneca blanca
Papel de oficina

UPC01 - Caneca blanca
Vidrio

UPC01 - Caneca negra
Cartón o papel con comida

UPC01 - Caneca negra
Empaque metalizado

UPC01 - Caneca negra
Icopor contaminado

UPC01 - Caneca negra
Servilleta o papel higiénico usado

… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)
```

## E126 · Pesos con su material

**Objetivo:** Catálogo de pesos con el nombre del material.  
**Explicación:** JOIN por id_material.  
**Estado:** OK

```sql
SELECT m.nombre, p.clave_objeto, p.gramos_tipico FROM peso_referencial p JOIN material m ON m.id_material = p.id_material ORDER BY m.nombre FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
CLAVE_OBJETO																			 GRAMOS_TIPICO
---------------------------------------------------------------------------------------------------------------------------------------------------------------- -------------
Borra de café e infusiones
BORRA_CAFE_PORCION																			    20

Botella plástica PET
BOTELLA_PET_600ML																			   300

Botella plástica PET
BOTELLA_PET_1500ML																			    38

Botella plástica PET
BOTELLA_PET_600ML																			    22

Botella plástica PET
BOTELLA_PET_600ML																			   620

Cartón
CAJA_CARTON_PEQUENA																			   120

Cartón o papel con comida
CAJA_PIZZA																				   250

Cáscara de huevo
CASCARA_HUEVO_UNIDAD																			     6


8 filas seleccionadas.
```

## E127 · Movimientos con el titular

**Objetivo:** Quién recibió cada movimiento de EcoPuntos.  
**Explicación:** JOIN con USUARIO para mostrar el nombre.  
**Estado:** OK

```sql
SELECT u.nombre, mv.tipo, mv.puntos FROM movimiento_ecopuntos mv JOIN usuario u ON u.id_usuario = mv.id_usuario ORDER BY mv.id_movimiento FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
      TIPO     PUNTOS
---------- ----------
María Gómez Oñate
	 1	   12

Miguel Molina Castro
	 1	   30

Yuliana Ortega Quintero
	 1	    8

Ana Castro Vargas
	 1	   10

Carlos Vargas Barros
	 1	   24

María Gómez Oñate
	 1	   12

Ana Castro Vargas
	 1	    8

María Gómez Oñate
	 1	   24


8 filas seleccionadas.
```

## E128 · Auditoría con el actor

**Objetivo:** Quién hizo cada verificación auditada.  
**Explicación:** LEFT JOIN por si el actor quedó NULL.  
**Estado:** OK

```sql
SELECT a.id_deposito, a.operacion, u.nombre AS actor FROM auditoria_deposito a LEFT JOIN usuario u ON u.id_usuario = a.id_actor ORDER BY a.id_auditoria FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO OPERACION
----------- ------------------------------------------------
ACTOR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	  1 VERIFICACION
Operario Demo Uno (SINTÉTICO)

	  2 RECHAZO
Operaria Demo Dos (SINTÉTICO)

	  3 RECHAZO
Operario Demo Uno (SINTÉTICO)

	  4 VERIFICACION
Operario Demo Uno (SINTÉTICO)

	  5 VERIFICACION
Operaria Demo Dos (SINTÉTICO)

	  6 VERIFICACION
Operaria Demo Dos (SINTÉTICO)

	  7 VERIFICACION
Operaria Demo Dos (SINTÉTICO)

	  9 VERIFICACION
Operario Demo Uno (SINTÉTICO)


8 filas seleccionadas.
```

## E129 · Eventos de una orden

**Objetivo:** Ver acuse, apertura y detección de una orden.  
**Explicación:** JOIN de ORDEN_APERTURA con EVENTO_DISPOSITIVO (1:N).  
**Estado:** OK

```sql
SELECT o.id_orden, e.tipo, e.resultado FROM orden_apertura o JOIN evento_dispositivo e ON e.id_orden = o.id_orden WHERE o.id_orden = (SELECT MIN(id_orden) FROM orden_apertura) ORDER BY e.tipo;
```

Salida real:

```text
ID_ORDEN	 TIPO  RESULTADO
---------- ---------- ----------
	 1	    1	       1
	 1	    2	       1
	 1	    3	       1

3 filas seleccionadas.
```

## E130 · Fotos con material (LEFT)

**Objetivo:** Fotos con el material reconocido, si hubo.  
**Explicación:** LEFT JOIN conserva las fotos «no es residuo» sin material.  
**Estado:** OK

```sql
SELECT cv.id_consulta, cv.resultado, m.nombre FROM consulta_vision cv LEFT JOIN material m ON m.id_material = cv.id_material ORDER BY cv.id_consulta FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
ID_CONSULTA  RESULTADO
----------- ----------
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	  1	     1
Cartón o papel con comida

	  2	     1
Cartón

	  3	     1
Tapabocas

	  4	     1
Lata de aluminio

	  5	     1
Envase multicapa

	  6	     1
Papel de oficina

	  7	     1
Lata de acero

	  8	     1
Vidrio


8 filas seleccionadas.
```

## E131 · JOIN no igualitario

**Objetivo:** ¿En qué rango del catálogo cae cada depósito?  
**Explicación:** La condición del JOIN puede ser BETWEEN, no solo =.  
**Estado:** OK

```sql
SELECT d.id_deposito, d.peso_estimado_kg * 1000 AS gramos, p.clave_objeto FROM deposito d JOIN peso_referencial p ON p.id_peso_referencial = d.id_peso_referencial AND d.peso_estimado_kg * 1000 BETWEEN p.gramos_minimo * NVL(d.cantidad_declarada, 1) AND p.gramos_maximo * NVL(d.cantidad_declarada, 1) FETCH FIRST 6 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO	GRAMOS CLAVE_OBJETO
----------- ---------- ----------------------------------------------------------------------------------------------------------------------------------------------------------------
	  1	   250 CAJA_PIZZA
	  2	   360 CAJA_CARTON_PEQUENA
	  3	     8 TAPABOCAS_DESECHABLE
	  4	    28 LATA_330ML
	  5	    84 CAJA_TETRAPAK_1L
	  6	   110 LATA_CONSERVA

6 filas seleccionadas.
```

## E132 · Roles de cada persona

**Objetivo:** Personas con más de un rol.  
**Explicación:** JOIN + GROUP BY + HAVING sobre la tabla N:M.  
**Estado:** OK

```sql
SELECT u.nombre, LISTAGG(r.descripcion, ', ') WITHIN GROUP (ORDER BY r.codigo) AS roles FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario JOIN rol r ON r.id_rol = ur.id_rol GROUP BY u.nombre HAVING COUNT(*) > 1;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
ROLES
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Operario Demo Uno (SINTÉTICO)
Ciudadano, Operario


1 fila seleccionada.
```
