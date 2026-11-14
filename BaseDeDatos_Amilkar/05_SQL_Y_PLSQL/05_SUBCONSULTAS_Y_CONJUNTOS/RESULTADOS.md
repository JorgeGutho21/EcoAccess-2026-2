# SUBCONSULTAS Y CONJUNTOS

Ejercicios: 32 · conexión: `ECOACCESS_BD` · temas del banco: 8. Subconsultas; 9. Operaciones de conjuntos.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E133 | Subconsulta escalar | OK |
| E134 | IN con subconsulta | OK |
| E135 | NOT IN | OK |
| E136 | EXISTS | OK |
| E137 | NOT EXISTS | OK |
| E138 | Subconsulta correlacionada | OK |
| E139 | Subconsulta en FROM | OK |
| E140 | WITH (CTE) | OK |
| E141 | ANY / ALL | OK |
| E142 | Subconsulta con HAVING | OK |
| E143 | Dos CTE | OK |
| E144 | Subconsulta en UPDATE (con ROLLBACK) | OK |
| E145 | El depósito más pesado | OK |
| E146 | Personas con más de 3 depósitos | OK |
| E147 | Materiales nunca depositados | OK |
| E148 | Sobre el promedio de su material | OK |
| E149 | Subconsulta en la lista SELECT | OK |
| E150 | IN con dos columnas | OK |
| E151 | Vista en línea con ranking | OK |
| E152 | CTE con varios pasos | OK |
| E153 | UNION | OK |
| E154 | UNION ALL | OK |
| E155 | INTERSECT | OK |
| E156 | MINUS | OK |
| E157 | UNION de distintas tablas | OK |
| E158 | Verificar con MINUS | OK |
| E159 | Sugerido o confirmado | OK |
| E160 | Con foto y con depósito | OK |
| E161 | Solo fotos, sin depósito | OK |
| E162 | Embudo con UNION ALL | OK |
| E163 | Colores sin depósitos | OK |
| E164 | Reglas de UNION | OK |

## E133 · Subconsulta escalar

**Objetivo:** Depósitos por encima del peso promedio.  
**Explicación:** La consulta interna devuelve un solo valor.  
**Estado:** OK

```sql
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg > (SELECT AVG(peso_estimado_kg) FROM deposito) ORDER BY peso_estimado_kg DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO PESO_ESTIMADO_KG
----------- ----------------
	 36		  ,6
	101		  ,6
	 54		  ,6
	 42		  ,6
	 21		  ,4

5 filas seleccionadas.
```

## E134 · IN con subconsulta

**Objetivo:** Usuarios que tienen rol Operario.  
**Explicación:** La interna devuelve una lista.  
**Estado:** OK

```sql
SELECT nombre FROM usuario WHERE id_usuario IN (SELECT ur.id_usuario FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = 2);
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Operario Demo Uno (SINTÉTICO)
Operaria Demo Dos (SINTÉTICO)

2 filas seleccionadas.
```

## E135 · NOT IN

**Objetivo:** Ciudadanos sin ninguna sesión.  
**Explicación:** NOT IN excluye la lista (cuidado con NULL: se filtran).  
**Estado:** OK

```sql
SELECT COUNT(*) FROM usuario WHERE id_usuario NOT IN (SELECT id_usuario FROM interaccion WHERE id_usuario IS NOT NULL);
```

Salida real:

```text
COUNT(*)
----------
	22

1 fila seleccionada.
```

## E136 · EXISTS

**Objetivo:** Canecas que tienen al menos un depósito.  
**Explicación:** EXISTS pregunta «¿hay al menos una fila?».  
**Estado:** OK

```sql
SELECT c.nombre FROM contenedor c WHERE EXISTS (SELECT 1 FROM orden_apertura o JOIN deposito d ON d.id_orden = o.id_orden WHERE o.id_contenedor = c.id_contenedor);
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
UPC01 - Caneca blanca
UPC01 - Caneca verde
Legado - Caneca blanca
UPC01 - Caneca negra

4 filas seleccionadas.
```

## E137 · NOT EXISTS

**Objetivo:** Materiales sin peso de referencia.  
**Explicación:** Útil para encontrar huecos en el catálogo.  
**Estado:** OK

```sql
SELECT m.codigo FROM material m WHERE NOT EXISTS (SELECT 1 FROM peso_referencial p WHERE p.id_material = m.id_material);
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
BOMBILLO
MEDICAMENTOS

2 filas seleccionadas.
```

## E138 · Subconsulta correlacionada

**Objetivo:** Último depósito de cada caneca.  
**Explicación:** La interna se repite por cada fila de la externa.  
**Estado:** OK

```sql
SELECT c.nombre, (SELECT MAX(d.fecha_deteccion_utc) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden WHERE o.id_contenedor = c.id_contenedor) AS ultimo FROM contenedor c;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
ULTIMO
---------------------------------------------------------------------------
UPC01 - Caneca negra
10/11/26 07:39:43,621258 AM

UPC01 - Caneca blanca
11/11/26 08:20:21,615532 AM

UPC01 - Caneca verde
10/11/26 04:27:26,804363 PM

Legado - Caneca blanca
10/11/26 08:05:34,222872 PM


4 filas seleccionadas.
```

## E139 · Subconsulta en FROM

**Objetivo:** Promedio de depósitos por ciudadano.  
**Explicación:** Una consulta puede ser la «tabla» de otra.  
**Estado:** OK

```sql
SELECT ROUND(AVG(n), 2) AS promedio_por_ciudadano FROM (SELECT i.id_usuario, COUNT(*) n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario);
```

Salida real:

```text
PROMEDIO_POR_CIUDADANO
----------------------
		  3,39

1 fila seleccionada.
```

## E140 · WITH (CTE)

**Objetivo:** La misma idea, más legible.  
**Explicación:** WITH le pone nombre a una subconsulta.  
**Estado:** OK

```sql
WITH por_ciudadano AS (SELECT i.id_usuario, COUNT(*) n FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT MAX(n) AS maximo, MIN(n) AS minimo FROM por_ciudadano;
```

Salida real:

```text
MAXIMO     MINIMO
---------- ----------
	11	    1

1 fila seleccionada.
```

## E141 · ANY / ALL

**Objetivo:** Pesos mayores que TODOS los de la caneca verde.  
**Explicación:** ALL compara contra todos los valores.  
**Estado:** OK

```sql
SELECT descripcion_objeto, gramos_tipico FROM peso_referencial WHERE gramos_tipico > ALL (SELECT gramos_tipico FROM peso_referencial p JOIN material m USING (id_material) WHERE m.categoria = 2);
```

Salida real:

```text
DESCRIPCION_OBJETO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
GRAMOS_TIPICO
-------------
Botella PET de 600 ml
	  300

Botella PET de 600 ml
	  620

Caja de pizza con grasa
	  250

Botella de vidrio de 330 ml
	  200


4 filas seleccionadas.
```

## E142 · Subconsulta con HAVING

**Objetivo:** Ciudadanos con más depósitos que el promedio.  
**Explicación:** Se compara un grupo contra un promedio.  
**Estado:** OK

```sql
SELECT i.id_usuario, COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario HAVING COUNT(*) > (SELECT COUNT(*) / COUNT(DISTINCT i2.id_usuario) FROM deposito d2 JOIN orden_apertura o2 ON o2.id_orden = d2.id_orden JOIN interaccion i2 ON i2.id_interaccion = o2.id_interaccion) ORDER BY 2 DESC;
```

Salida real:

```text
ID_USUARIO   COUNT(*)
---------- ----------
	 6	   11
	 8	    8
	13	    7
	 4	    6
	 9	    5
	10	    5
	12	    5
	11	    5
	16	    4
	 7	    4
	 5	    4
	19	    4
	17	    4
	22	    4

14 filas seleccionadas.
```

## E143 · Dos CTE

**Objetivo:** Fotos y depósitos por ciudadano lado a lado.  
**Explicación:** Comparar orientación vs reciclaje.  
**Estado:** OK

```sql
WITH f AS (SELECT id_usuario, COUNT(*) fotos FROM consulta_vision GROUP BY id_usuario), d AS (SELECT i.id_usuario, COUNT(*) deps FROM deposito x JOIN orden_apertura o ON o.id_orden = x.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT f.id_usuario, f.fotos, NVL(d.deps, 0) deps FROM f LEFT JOIN d ON d.id_usuario = f.id_usuario WHERE f.id_usuario IS NOT NULL ORDER BY f.fotos DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_USUARIO	FOTOS	    DEPS
---------- ---------- ----------
	 8	   15	       8
	 6	   14	      11
	12	    9	       5
	19	    9	       4
	 9	    9	       5

5 filas seleccionadas.
```

## E144 · Subconsulta en UPDATE (con ROLLBACK)

**Objetivo:** Cambiar un valor usando otra tabla.  
**Explicación:** Al final ROLLBACK deja todo como estaba.  
**Estado:** OK

```sql
UPDATE material SET descripcion = descripcion || ' (revisado)' WHERE id_material IN (SELECT id_material FROM peso_referencial WHERE gramos_tipico > 500);
ROLLBACK;
```

Salida real:

```text
1 fila actualizada.


Rollback terminado.
```

## E145 · El depósito más pesado

**Objetivo:** El depósito con mayor peso estimado.  
**Explicación:** La subconsulta calcula el máximo; la externa busca quién lo tiene.  
**Estado:** OK

```sql
SELECT id_deposito, peso_estimado_kg FROM deposito WHERE peso_estimado_kg = (SELECT MAX(peso_estimado_kg) FROM deposito);
```

Salida real:

```text
ID_DEPOSITO PESO_ESTIMADO_KG
----------- ----------------
	 36		  ,6
	 42		  ,6
	 54		  ,6
	101		  ,6

4 filas seleccionadas.
```

## E146 · Personas con más de 3 depósitos

**Objetivo:** IN con una subconsulta agrupada.  
**Explicación:** La subconsulta devuelve una lista de ids.  
**Estado:** OK

```sql
SELECT nombre FROM usuario WHERE id_usuario IN (SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario HAVING COUNT(*) > 3) ORDER BY nombre;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Ana Castro Vargas
Andrés Pérez Martínez
Camila Rojas Mendoza
Carlos Vargas Barros
Daniela Martínez Daza
Diana Maestre Pérez
Esteban Daza Maestre
Jhon Zuleta Rojas
Luis Oñate Arias
María Gómez Oñate
Miguel Molina Castro
Paola Arias Rodríguez
Santiago Quintero Fuentes
Valentina Fuentes Ramírez

14 filas seleccionadas.
```

## E147 · Materiales nunca depositados

**Objetivo:** NOT EXISTS: lo que nunca pasó.  
**Explicación:** NOT EXISTS es seguro aunque haya NULL (a diferencia de NOT IN).  
**Estado:** OK

```sql
SELECT m.codigo FROM material m WHERE NOT EXISTS (SELECT 1 FROM deposito d WHERE d.id_material_declarado = m.id_material) ORDER BY m.codigo;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
BOMBILLO
MEDICAMENTOS
PILAS

3 filas seleccionadas.
```

## E148 · Sobre el promedio de su material

**Objetivo:** Correlacionada: comparar con el promedio del mismo material.  
**Explicación:** La subconsulta se repite para cada fila externa.  
**Estado:** OK

```sql
SELECT d.id_deposito, d.id_material_declarado, d.peso_estimado_kg FROM deposito d WHERE d.peso_estimado_kg > (SELECT AVG(d2.peso_estimado_kg) FROM deposito d2 WHERE d2.id_material_declarado = d.id_material_declarado) ORDER BY d.id_deposito FETCH FIRST 8 ROWS ONLY;
```

Salida real:

```text
ID_DEPOSITO ID_MATERIAL_DECLARADO PESO_ESTIMADO_KG
----------- --------------------- ----------------
	  2		      103	       ,36
	  5		      108	      ,084
	 18		      303	      ,015
	 24		      101	      ,076
	 26		      107	      ,165
	 27		      106	      ,042
	 30		      104	      ,015
	 31		      108	      ,084

8 filas seleccionadas.
```

## E149 · Subconsulta en la lista SELECT

**Objetivo:** Cuántas fotos tiene cada persona.  
**Explicación:** Una subconsulta escalar por fila.  
**Estado:** OK

```sql
SELECT u.nombre, (SELECT COUNT(*) FROM consulta_vision cv WHERE cv.id_usuario = u.id_usuario) AS fotos FROM usuario u ORDER BY fotos DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
NOMBRE
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
     FOTOS
----------
Valentina Fuentes Ramírez
	15

María Gómez Oñate
	14

Andrés Pérez Martínez
	 9

Esteban Daza Maestre
	 9

Daniela Martínez Daza
	 9


5 filas seleccionadas.
```

## E150 · IN con dos columnas

**Objetivo:** Pares caneca-material admitidos.  
**Explicación:** Se pueden comparar varias columnas a la vez.  
**Estado:** OK

```sql
SELECT id_contenedor, id_material FROM contenedor_material WHERE (id_contenedor, id_material) IN (SELECT 11, 101 FROM dual UNION ALL SELECT 12, 201 FROM dual);
```

Salida real:

```text
ID_CONTENEDOR ID_MATERIAL
------------- -----------
	   11	      101
	   12	      201

2 filas seleccionadas.
```

## E151 · Vista en línea con ranking

**Objetivo:** Top 3 materiales verificados.  
**Explicación:** La subconsulta en FROM actúa como una tabla temporal.  
**Estado:** OK

```sql
SELECT * FROM (SELECT id_material_verificado, COUNT(*) AS n FROM deposito WHERE estado_verificacion = 1 GROUP BY id_material_verificado ORDER BY n DESC) WHERE ROWNUM <= 3;
```

Salida real:

```text
ID_MATERIAL_VERIFICADO		N
---------------------- ----------
		   106		9
		   104		9
		   103		8

3 filas seleccionadas.
```

## E152 · CTE con varios pasos

**Objetivo:** Fotos, depósitos y razón por persona.  
**Explicación:** WITH ordena el razonamiento en pasos con nombre.  
**Estado:** OK

```sql
WITH f AS (SELECT id_usuario, COUNT(*) AS fotos FROM consulta_vision GROUP BY id_usuario), d AS (SELECT i.id_usuario, COUNT(*) AS depositos FROM deposito x JOIN orden_apertura o ON o.id_orden = x.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion GROUP BY i.id_usuario) SELECT f.id_usuario, f.fotos, NVL(d.depositos, 0) AS depositos FROM f LEFT JOIN d ON d.id_usuario = f.id_usuario ORDER BY f.fotos DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_USUARIO	FOTOS  DEPOSITOS
---------- ---------- ----------
	 8	   15	       8
	 6	   14	      11
	12	    9	       5
	19	    9	       4
	 9	    9	       5

5 filas seleccionadas.
```

## E153 · UNION

**Objetivo:** Lista única de materiales declarados o verificados.  
**Explicación:** UNION quita repetidos.  
**Estado:** OK

```sql
SELECT id_material_declarado AS id FROM deposito UNION SELECT id_material_verificado FROM deposito WHERE id_material_verificado IS NOT NULL ORDER BY 1;
```

Salida real:

```text
	ID
----------
       101
       102
       103
       104
       105
       106
       107
       108
       201
       202
       203
       204
       205
       301
       302
       303
       304
       305

18 filas seleccionadas.
```

## E154 · UNION ALL

**Objetivo:** Embudo de evidencias en una tabla.  
**Explicación:** UNION ALL no quita repetidos y es más rápido.  
**Estado:** OK

```sql
SELECT 'fotos' etapa, COUNT(*) n FROM consulta_vision UNION ALL SELECT 'ordenes', COUNT(*) FROM orden_apertura UNION ALL SELECT 'depositos', COUNT(*) FROM deposito;
```

Salida real:

```text
ETAPA		   N
--------- ----------
fotos		 187
ordenes 	 114
depositos	 105

3 filas seleccionadas.
```

## E155 · INTERSECT

**Objetivo:** Materiales que fueron declarados Y verificados.  
**Explicación:** INTERSECT deja lo común.  
**Estado:** OK

```sql
SELECT id_material_declarado FROM deposito INTERSECT SELECT id_material_verificado FROM deposito ORDER BY 1;
```

Salida real:

```text
ID_MATERIAL_DECLARADO
---------------------
		  101
		  102
		  103
		  104
		  105
		  106
		  107
		  108
		  201
		  203
		  204
		  205
		  301
		  302
		  303
		  305

16 filas seleccionadas.
```

## E156 · MINUS

**Objetivo:** Ciudadanos con fotos pero sin depósitos.  
**Explicación:** MINUS resta conjuntos.  
**Estado:** OK

```sql
SELECT id_usuario FROM consulta_vision WHERE id_usuario IS NOT NULL MINUS SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1;
```

Salida real:

```text
ID_USUARIO
----------
	26
	33
	36
	38
	39
	40
	41
	46
	47
	49
	50
	51

12 filas seleccionadas.
```

## E157 · UNION de distintas tablas

**Objetivo:** Bitácora única de cambios (auditoría + historial).  
**Explicación:** Las columnas deben coincidir en número y tipo.  
**Estado:** OK

```sql
SELECT 'DEPOSITO' AS objeto, id_deposito AS id, operacion AS cambio, fecha_utc FROM auditoria_deposito WHERE operacion <> 'VERIFICACION' UNION ALL SELECT 'CANECA', id_contenedor, estado_anterior || '->' || estado_nuevo, fecha_utc FROM historial_estado_contenedor ORDER BY fecha_utc;
```

Salida real:

```text
OBJETO		 ID CAMBIO									       FECHA_UTC
-------- ---------- ---------------------------------------------------------------------------------- ---------------------------------------------------------------------------
CANECA		 11 2->1									       11/11/26 03:47:29,332461 PM
CANECA		 13 2->1									       11/11/26 03:47:29,336931 PM
CANECA		 12 2->1									       11/11/26 03:47:29,336931 PM
CANECA		 21 2->1									       11/11/26 03:47:29,339198 PM
CANECA		 13 2->1									       11/11/26 03:47:29,340851 PM
CANECA		 13 1->2									       11/11/26 03:47:29,340851 PM
DEPOSITO	  2 RECHAZO									       11/11/26 03:47:29,820296 PM
DEPOSITO	  3 RECHAZO									       11/11/26 03:47:29,824764 PM
DEPOSITO	 10 RECHAZO									       11/11/26 03:47:30,011456 PM
DEPOSITO	 37 RECHAZO									       11/11/26 03:47:30,230774 PM
DEPOSITO	 53 RECHAZO									       11/11/26 03:47:30,319682 PM
DEPOSITO	 54 RECHAZO									       11/11/26 03:47:30,322689 PM
DEPOSITO	 58 RECHAZO									       11/11/26 03:47:30,335092 PM
DEPOSITO	 63 RECHAZO									       11/11/26 03:47:30,361679 PM
DEPOSITO	 64 RECHAZO									       11/11/26 03:47:30,367065 PM
DEPOSITO	 67 RECHAZO									       11/11/26 03:47:30,379177 PM
DEPOSITO	 68 RECHAZO									       11/11/26 03:47:30,380847 PM
DEPOSITO	 82 RECHAZO									       11/11/26 03:47:30,467721 PM
DEPOSITO	 93 RECHAZO									       11/11/26 03:47:30,519050 PM
DEPOSITO	  5 CORRECCION									       11/11/26 03:47:30,685145 PM
DEPOSITO	  2 REAPERTURA									       11/11/26 03:47:30,697605 PM

21 filas seleccionadas.
```

## E158 · Verificar con MINUS

**Objetivo:** Comprobar que la vista coincide con la tabla.  
**Explicación:** Si MINUS no devuelve filas, son iguales.  
**Estado:** OK

```sql
SELECT id_deposito FROM deposito MINUS SELECT id_deposito FROM vw_deposito_detalle;
```

Salida real:

```text
ninguna fila seleccionada
```

## E159 · Sugerido o confirmado

**Objetivo:** Materiales que la IA sugirió o el ciudadano confirmó.  
**Explicación:** UNION quita los repetidos.  
**Estado:** OK

```sql
SELECT id_material_sugerido AS id_material FROM interaccion WHERE id_material_sugerido IS NOT NULL UNION SELECT id_material_confirmado FROM interaccion WHERE id_material_confirmado IS NOT NULL ORDER BY 1;
```

Salida real:

```text
ID_MATERIAL
-----------
	101
	102
	103
	104
	105
	106
	107
	108
	201
	202
	203
	204
	205
	301
	302
	303
	304
	305
	401
	402
	403

21 filas seleccionadas.
```

## E160 · Con foto y con depósito

**Objetivo:** Personas que hicieron las dos cosas.  
**Explicación:** INTERSECT deja solo lo común.  
**Estado:** OK

```sql
SELECT id_usuario FROM consulta_vision INTERSECT SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1 FETCH FIRST 10 ROWS ONLY;
```

Salida real:

```text
ID_USUARIO
----------
	 1
	 4
	 5
	 6
	 7
	 8
	 9
	10
	11
	12

10 filas seleccionadas.
```

## E161 · Solo fotos, sin depósito

**Objetivo:** Personas que preguntaron pero no depositaron.  
**Explicación:** MINUS = el primer conjunto menos el segundo.  
**Estado:** OK

```sql
SELECT id_usuario FROM consulta_vision MINUS SELECT i.id_usuario FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion ORDER BY 1;
```

Salida real:

```text
ID_USUARIO
----------
	26
	33
	36
	38
	39
	40
	41
	46
	47
	49
	50
	51


13 filas seleccionadas.
```

## E162 · Embudo con UNION ALL

**Objetivo:** Contar cada etapa en una sola tabla.  
**Explicación:** UNION ALL pega filas sin quitar repetidos (y es más rápido).  
**Estado:** OK

```sql
SELECT '1 Fotos' AS etapa, COUNT(*) AS n FROM consulta_vision UNION ALL SELECT '2 Sesiones', COUNT(*) FROM interaccion UNION ALL SELECT '3 Órdenes', COUNT(*) FROM orden_apertura UNION ALL SELECT '4 Depósitos', COUNT(*) FROM deposito UNION ALL SELECT '5 Verificados', COUNT(*) FROM deposito WHERE estado_verificacion = 1 ORDER BY 1;
```

Salida real:

```text
ETAPA		       N
------------- ----------
1 Fotos 	     187
2 Sesiones	     130
3 Órdenes            114
4 Depósitos          105
5 Verificados	      76

5 filas seleccionadas.
```

## E163 · Colores sin depósitos

**Objetivo:** Categorías de material sin ningún depósito.  
**Explicación:** MINUS entre catálogo y uso.  
**Estado:** OK

```sql
SELECT categoria FROM material MINUS SELECT m.categoria FROM deposito d JOIN material m ON m.id_material = d.id_material_declarado;
```

Salida real:

```text
CATEGORIA
----------
	 4

1 fila seleccionada.
```

## E164 · Reglas de UNION

**Objetivo:** Las columnas deben coincidir en número y tipo.  
**Explicación:** Se usan alias en la primera consulta; ORDER BY va al final.  
**Estado:** OK

```sql
SELECT codigo AS texto, 'material' AS origen FROM material WHERE categoria = 3 UNION SELECT nombre, 'caneca' FROM contenedor WHERE categoria = 3 ORDER BY origen, texto;
```

Salida real:

```text
TEXTO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
ORIGEN
--------
UPC01 - Caneca negra
caneca

CARTON_CONTAMINADO
material

EMPAQUE_METALIZADO
material

ICOPOR_SUCIO
material

SERVILLETA_USADA
material

TAPABOCAS
material


6 filas seleccionadas.
```
