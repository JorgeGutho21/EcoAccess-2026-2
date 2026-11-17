# FUNDAMENTOS SQL

Ejercicios: 57 · conexión: `ECOACCESS_BD` · temas del banco: 1. Conexión y diccionario de datos; 11. Fechas y hora de Colombia; 13. Vistas y seguridad; 5. Funciones de una fila.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E001 | ¿Con qué usuario estoy? | OK |
| E002 | Fecha y hora del servidor | OK |
| E003 | Mis tablas | OK |
| E004 | Columnas de una tabla | OK |
| E005 | Restricciones de una tabla | OK |
| E006 | Comentario de una columna | OK |
| E007 | Cuántos objetos hay de cada tipo | OK |
| E008 | ¿Hay objetos inválidos? | OK |
| E009 | Índices de una tabla | OK |
| E010 | Índices funcionales | OK |
| E011 | Columnas IDENTITY | OK |
| E012 | Columnas de una FK compuesta | OK |
| E013 | Comentario de las tablas | OK |
| E014 | Dependencias de un paquete | OK |
| E066 | UPPER y LOWER | OK |
| E067 | LENGTH | OK |
| E068 | SUBSTR e INSTR | OK |
| E069 | REPLACE | OK |
| E070 | ROUND y TRUNC | OK |
| E071 | MOD | OK |
| E072 | NVL | OK |
| E073 | NVL2 | OK |
| E074 | COALESCE | OK |
| E075 | TO_CHAR con fechas | OK |
| E076 | TO_DATE | OK |
| E077 | Diferencia de fechas | OK |
| E078 | ADD_MONTHS y LAST_DAY | OK |
| E079 | DECODE | OK |
| E080 | Gramos y kilogramos | OK |
| E081 | INITCAP | OK |
| E082 | LPAD y RPAD | OK |
| E083 | TRIM | OK |
| E084 | GREATEST y LEAST | OK |
| E085 | CEIL y FLOOR | OK |
| E086 | NULLIF | OK |
| E087 | REGEXP_LIKE | OK |
| E180 | UTC a Colombia | OK |
| E181 | Hoy en Colombia | OK |
| E182 | Semana calendario actual | OK |
| E183 | Mes calendario | OK |
| E184 | Diferencia de TIMESTAMP | OK |
| E185 | Día de la semana con más uso | OK |
| E186 | FROM_TZ | OK |
| E187 | Hora del día | OK |
| E188 | Lunes de la semana | OK |
| E189 | INTERVAL | OK |
| E190 | Minutos entre emisión y vencimiento | OK |
| E191 | Nombre del día | OK |
| E207 | Vista para Power BI | OK |
| E208 | Privilegios concedidos | OK |
| E209 | Texto de una vista | OK |
| E210 | Vista de solo lectura | OK (error esperado) |
| E211 | Mis privilegios de sistema | OK |
| E212 | Columnas actualizables de una vista | OK |
| E213 | Error ESPERADO: vista de solo lectura | OK (error esperado) |
| E214 | Lo que ve Power BI | OK |
| E215 | Vista de detalle | OK |

## E001 · ¿Con qué usuario estoy?

**Objetivo:** Saber quién y dónde estoy conectado antes de hacer cualquier cosa.  
**Explicación:** USER devuelve la cuenta Oracle; SYS_CONTEXT pregunta datos de la sesión. DUAL es una tabla de una fila que sirve para hacer cálculos.  
**Estado:** OK

```sql
SELECT USER AS usuario, SYS_CONTEXT('USERENV','CON_NAME') AS base_de_datos FROM dual;
```

Salida real:

```text
USUARIO
--------------------------------------------------------------------------------------------------------------------------------
BASE_DE_DATOS
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
ECOACCESS_BD
FREEPDB1


1 fila seleccionada.
```

## E002 · Fecha y hora del servidor

**Objetivo:** Ver la hora del servidor y la de Colombia.  
**Explicación:** SYSDATE es la fecha del servidor; AT TIME ZONE convierte a otra zona.  
**Estado:** OK

```sql
SELECT SYSDATE AS hora_servidor, CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE) AS hora_colombia FROM dual;
```

Salida real:

```text
HORA_SERVIDOR	 HORA_COLOMBIA
---------------- ----------------
2026-11-11 20:27 2026-11-11 20:27

1 fila seleccionada.
```

## E003 · Mis tablas

**Objetivo:** Listar las tablas de mi esquema.  
**Explicación:** USER_TABLES es una vista del diccionario: Oracle guarda ahí la lista de MIS tablas.  
**Estado:** OK

```sql
SELECT table_name FROM user_tables ORDER BY table_name;
```

Salida real:

```text
TABLE_NAME
--------------------------------------------------------------------------------------------------------------------------------
AUDITORIA_DEPOSITO
CONSULTA_VISION
CONTENEDOR
CONTENEDOR_MATERIAL
CREDENCIAL_ACCESO
DEPOSITO
EVENTO_DISPOSITIVO
HISTORIAL_ESTADO_CONTENEDOR
INTERACCION
MATERIAL
MOVIMIENTO_ECOPUNTOS
ORDEN_APERTURA
PESO_REFERENCIAL
PUNTO_INSTALACION
REGLA_ECOPUNTOS
ROL
USUARIO
USUARIO_ROL
VINCULO_TELEGRAM

19 filas seleccionadas.
```

## E004 · Columnas de una tabla

**Objetivo:** Ver columnas, tipos y si aceptan NULL.  
**Explicación:** Es lo mismo que el comando DESCRIBE, pero como consulta.  
**Estado:** OK

```sql
SELECT column_name, data_type, nullable FROM user_tab_columns WHERE table_name = 'MATERIAL' ORDER BY column_id;
```

Salida real:

```text
COLUMN_NAME
--------------------------------------------------------------------------------------------------------------------------------
DATA_TYPE															 N
-------------------------------------------------------------------------------------------------------------------------------- -
ID_MATERIAL
NUMBER																 N

CODIGO
VARCHAR2															 N

NOMBRE
VARCHAR2															 N

CATEGORIA
NUMBER																 N

DESCRIPCION
VARCHAR2															 Y

HABILITADO
NUMBER																 N


6 filas seleccionadas.
```

## E005 · Restricciones de una tabla

**Objetivo:** Ver PK, FK, UNIQUE y CHECK de DEPOSITO.  
**Explicación:** constraint_type: P = clave primaria, R = foránea, U = única, C = CHECK o NOT NULL.  
**Estado:** OK

```sql
SELECT constraint_name, constraint_type FROM user_constraints WHERE table_name = 'DEPOSITO' ORDER BY constraint_type, constraint_name;
```

Salida real:

```text
CONSTRAINT_NAME 														 C
-------------------------------------------------------------------------------------------------------------------------------- -
CK_DEPOSITO_CANT_DECL														 C
CK_DEPOSITO_CANT_VERIF														 C
CK_DEPOSITO_ESTADO														 C
CK_DEPOSITO_ESTIMADO														 C
CK_DEPOSITO_MEDIDO														 C
CK_DEPOSITO_MOTIVO														 C
CK_DEPOSITO_SIN_SENSOR														 C
CK_DEPOSITO_VERIFICACION													 C
SYS_C0010664															 C
SYS_C0010665															 C
SYS_C0010666															 C
SYS_C0010667															 C
SYS_C0010668															 C
SYS_C0010669															 C
SYS_C0010670															 C
PK_DEPOSITO															 P
FK_DEPOSITO_EVENTO														 R
FK_DEPOSITO_MAT_DECL														 R
FK_DEPOSITO_MAT_VERIF														 R
FK_DEPOSITO_PESO														 R
FK_DEPOSITO_VERIFICADOR 													 R
UQ_DEPOSITO_EVENTO														 U
UQ_DEPOSITO_ORDEN														 U

23 filas seleccionadas.
```

## E006 · Comentario de una columna

**Objetivo:** Leer el diccionario guardado dentro de Oracle.  
**Explicación:** COMMENT ON guarda la explicación; USER_COL_COMMENTS la muestra.  
**Estado:** OK

```sql
SELECT column_name, comments FROM user_col_comments WHERE table_name = 'CONTENEDOR' ORDER BY column_name;
```

Salida real:

```text
COLUMN_NAME
--------------------------------------------------------------------------------------------------------------------------------
COMMENTS
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
CATEGORIA
Color: 1 BLANCA aprovechables, 2 VERDE orgánicos, 3 NEGRA no aprovechables.

ESTADO
1 En servicio, 2 Fuera de servicio, 3 Dada de baja.

FECHA_ALTA_UTC
Cuándo se registró la caneca.

ID_CONTENEDOR
Número de la caneca.

IDENTIFICADOR_DISPOSITIVO
Nombre del ESP32 que abre la tapa.

IDENTIFICADOR_QR
QR propio de la caneca (modelo legado de P3: un QR por caneca).

ID_PUNTO_INSTALACION
Estación donde está la caneca.

NOMBRE
Nombre de la caneca.


8 filas seleccionadas.
```

## E007 · Cuántos objetos hay de cada tipo

**Objetivo:** Inventario del esquema.  
**Explicación:** GROUP BY agrupa las filas iguales y COUNT las cuenta.  
**Estado:** OK

```sql
SELECT object_type, COUNT(*) AS cantidad FROM user_objects GROUP BY object_type ORDER BY object_type;
```

Salida real:

```text
OBJECT_TYPE		  CANTIDAD
----------------------- ----------
FUNCTION			 3
INDEX				81
PACKAGE 			 4
PACKAGE BODY			 4
PROCEDURE			 2
SEQUENCE			16
TABLE				19
TRIGGER 			 4
VIEW				 9

9 filas seleccionadas.
```

## E008 · ¿Hay objetos inválidos?

**Objetivo:** Comprobar que todo compiló.  
**Explicación:** Si la consulta no devuelve filas, todo está válido.  
**Estado:** OK

```sql
SELECT object_name, object_type FROM user_objects WHERE status <> 'VALID';
```

Salida real:

```text
ninguna fila seleccionada
```

## E009 · Índices de una tabla

**Objetivo:** Ver qué índices tiene DEPOSITO y si son únicos.  
**Explicación:** USER_INDEXES lista los índices; UNIQUENESS dice si impiden repetidos.  
**Estado:** OK

```sql
SELECT index_name, uniqueness FROM user_indexes WHERE table_name = 'DEPOSITO' ORDER BY index_name;
```

Salida real:

```text
INDEX_NAME															 UNIQUENES
-------------------------------------------------------------------------------------------------------------------------------- ---------
IX_DEPOSITO_ESTADO_FECHA													 NONUNIQUE
IX_DEPOSITO_FECHA														 NONUNIQUE
IX_DEPOSITO_MAT_DECL														 NONUNIQUE
IX_DEPOSITO_MAT_VERIF														 NONUNIQUE
IX_DEPOSITO_PESO														 NONUNIQUE
IX_DEPOSITO_VERIFICADOR 													 NONUNIQUE
PK_DEPOSITO															 UNIQUE
UQ_DEPOSITO_EVENTO														 UNIQUE
UQ_DEPOSITO_ORDEN														 UNIQUE

9 filas seleccionadas.
```

## E010 · Índices funcionales

**Objetivo:** Ver la expresión de los índices únicos de reglas de negocio.  
**Explicación:** USER_IND_EXPRESSIONS guarda el CASE que solo indexa filas activas.  
**Estado:** OK

```sql
SELECT index_name, column_expression FROM user_ind_expressions WHERE index_name LIKE 'UQ_%' ORDER BY index_name;
```

Salida real:

```text
INDEX_NAME
--------------------------------------------------------------------------------------------------------------------------------
COLUMN_EXPRESSION
--------------------------------------------------------------------------------
UQ_CONTENEDOR_COLOR_ACTIVO
CASE  WHEN "ESTADO"<>3 THEN "ID_PUNTO_INSTALACION" END

UQ_CONTENEDOR_COLOR_ACTIVO
CASE  WHEN "ESTADO"<>3 THEN "CATEGORIA" END

UQ_ORDEN_VIGENTE
CASE  WHEN ("ESTADO">=1 AND "ESTADO"<=5) THEN "ID_CONTENEDOR" END

UQ_VINCULO_TELEGRAM_ACTIVO
CASE  WHEN "FECHA_BAJA_UTC" IS NULL THEN "TELEGRAM_USER_ID" END

UQ_VINCULO_USUARIO_ACTIVO
CASE  WHEN "FECHA_BAJA_UTC" IS NULL THEN "ID_USUARIO" END


5 filas seleccionadas.
```

## E011 · Columnas IDENTITY

**Objetivo:** Saber qué tablas generan su id solas.  
**Explicación:** GENERATION_TYPE: ALWAYS (nadie escribe el id) o BY DEFAULT.  
**Estado:** OK

```sql
SELECT table_name, column_name, generation_type FROM user_tab_identity_cols ORDER BY table_name;
```

Salida real:

```text
TABLE_NAME
--------------------------------------------------------------------------------------------------------------------------------
COLUMN_NAME															 GENERATION
-------------------------------------------------------------------------------------------------------------------------------- ----------
AUDITORIA_DEPOSITO
ID_AUDITORIA															 ALWAYS

CONSULTA_VISION
ID_CONSULTA															 ALWAYS

CONTENEDOR
ID_CONTENEDOR															 BY DEFAULT

DEPOSITO
ID_DEPOSITO															 ALWAYS

EVENTO_DISPOSITIVO
ID_EVENTO															 ALWAYS

HISTORIAL_ESTADO_CONTENEDOR
ID_HISTORIAL															 ALWAYS

INTERACCION
ID_INTERACCION															 ALWAYS

MATERIAL
ID_MATERIAL															 BY DEFAULT

MOVIMIENTO_ECOPUNTOS
ID_MOVIMIENTO															 ALWAYS

ORDEN_APERTURA
ID_ORDEN															 ALWAYS

PESO_REFERENCIAL
ID_PESO_REFERENCIAL														 BY DEFAULT

PUNTO_INSTALACION
ID_PUNTO_INSTALACION														 BY DEFAULT

… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)
```

## E012 · Columnas de una FK compuesta

**Objetivo:** Ver las dos columnas de una clave foránea.  
**Explicación:** USER_CONS_COLUMNS dice qué columnas forman cada restricción y en qué orden.  
**Estado:** OK

```sql
SELECT c.constraint_name, cc.column_name, cc.position FROM user_constraints c JOIN user_cons_columns cc ON cc.constraint_name = c.constraint_name WHERE c.table_name = 'CONTENEDOR_MATERIAL' AND c.constraint_type = 'R' ORDER BY c.constraint_name, cc.position;
```

Salida real:

```text
CONSTRAINT_NAME
--------------------------------------------------------------------------------------------------------------------------------
COLUMN_NAME
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  POSITION
----------
FK_CONT_MAT_CONTENEDOR
ID_CONTENEDOR
	 1

FK_CONT_MAT_CONTENEDOR
CATEGORIA
	 2

FK_CONT_MAT_MATERIAL
ID_MATERIAL
	 1

FK_CONT_MAT_MATERIAL
CATEGORIA
	 2


4 filas seleccionadas.
```

## E013 · Comentario de las tablas

**Objetivo:** Leer la explicación de cada tabla.  
**Explicación:** USER_TAB_COMMENTS guarda el COMMENT ON TABLE.  
**Estado:** OK

```sql
SELECT table_name, SUBSTR(comments, 1, 70) AS comentario FROM user_tab_comments WHERE table_type = 'TABLE' ORDER BY table_name;
```

Salida real:

```text
TABLE_NAME
--------------------------------------------------------------------------------------------------------------------------------
COMENTARIO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
AUDITORIA_DEPOSITO
Valores :OLD y :NEW de cada verificación, rechazo, corrección o reaper

CONSULTA_VISION
Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósit

CONTENEDOR
Caneca de un color (blanca, verde o negra) dentro de una estación.

CONTENEDOR_MATERIAL
Qué materiales recibe cada caneca (N:M), con el color como garantía de

CREDENCIAL_ACCESO
Contraseña de la aplicación de escritorio guardada solo como hash PBKD

DEPOSITO
Residuo detectado por sensor o declarado sin sensor, y su verificación

EVENTO_DISPOSITIVO
Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, c

HISTORIAL_ESTADO_CONTENEDOR
Cada cambio de disponibilidad de una caneca (trigger 1).

INTERACCION
Sesión iniciada al escanear el QR (o apertura manual): estación, ciuda

MATERIAL
Catálogo validado de materiales; la IA solo puede apuntar a estos códi

MOVIMIENTO_ECOPUNTOS
Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.

ORDEN_APERTURA
Orden idempotente de abrir una caneca; máximo una vigente por caneca.

… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)
```

## E014 · Dependencias de un paquete

**Objetivo:** ¿Qué tablas usa PKG_ECOPUNTOS?  
**Explicación:** USER_DEPENDENCIES muestra de qué objetos depende otro.  
**Estado:** OK

```sql
SELECT DISTINCT referenced_name, referenced_type FROM user_dependencies WHERE name = 'PKG_ECOPUNTOS' AND referenced_owner = USER ORDER BY referenced_type, referenced_name;
```

Salida real:

```text
REFERENCED_NAME 														 REFERENCED_TYPE
-------------------------------------------------------------------------------------------------------------------------------- -------------------
PKG_CONTEXTO_APP														 PACKAGE
PKG_ECOPUNTOS															 PACKAGE
DEPOSITO															 TABLE
INTERACCION															 TABLE
MATERIAL															 TABLE
MOVIMIENTO_ECOPUNTOS														 TABLE
ORDEN_APERTURA															 TABLE
REGLA_ECOPUNTOS 														 TABLE
USUARIO 															 TABLE

9 filas seleccionadas.
```

## E066 · UPPER y LOWER

**Objetivo:** Cambiar mayúsculas.  
**Explicación:** UPPER todo en mayúsculas; LOWER en minúsculas.  
**Estado:** OK

```sql
SELECT UPPER(nombre) AS mayus, LOWER(codigo) AS minusculas FROM material WHERE ROWNUM <= 3;
```

Salida real:

```text
MAYUS
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
MINUSCULAS
------------------------------------------------------------------------------------------------------------------------
RESTOS DE FRUTAS Y VERDURAS
resto_fruta_verdura

ENVASE MULTICAPA
tetrapak

HOJAS Y PODA
hojas_poda


3 filas seleccionadas.
```

## E067 · LENGTH

**Objetivo:** Largo de un texto.  
**Explicación:** LENGTH cuenta caracteres.  
**Estado:** OK

```sql
SELECT codigo, LENGTH(codigo) AS letras FROM material ORDER BY letras DESC FETCH FIRST 3 ROWS ONLY;
```

Salida real:

```text
CODIGO															     LETRAS
------------------------------------------------------------------------------------------------------------------------ ----------
RESTO_FRUTA_VERDURA														 19
CARTON_CONTAMINADO														 18
EMPAQUE_METALIZADO														 18

3 filas seleccionadas.
```

## E068 · SUBSTR e INSTR

**Objetivo:** Sacar el usuario de un correo.  
**Explicación:** INSTR busca la posición de '@'; SUBSTR corta.  
**Estado:** OK

```sql
SELECT correo_electronico, SUBSTR(correo_electronico, 1, INSTR(correo_electronico, '@') - 1) AS usuario FROM usuario WHERE ROWNUM <= 3;
```

Salida real:

```text
CORREO_ELECTRONICO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
USUARIO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
operario.uno@ejemplo.test
operario.uno

operaria.dos@ejemplo.test
operaria.dos

administradora@ejemplo.test
administradora


3 filas seleccionadas.
```

## E069 · REPLACE

**Objetivo:** Reemplazar texto.  
**Explicación:** REPLACE cambia un texto por otro.  
**Estado:** OK

```sql
SELECT REPLACE(clave_objeto, '_', ' ') AS legible FROM peso_referencial WHERE ROWNUM <= 3;
```

Salida real:

```text
LEGIBLE
----------------------------------------------------------------------------------------------------------------------------------------------------------------
CAJA CARTON PEQUENA
BOTELLA PET 600ML
LATA CONSERVA

3 filas seleccionadas.
```

## E070 · ROUND y TRUNC

**Objetivo:** Redondear números.  
**Explicación:** ROUND redondea; TRUNC corta sin redondear.  
**Estado:** OK

```sql
SELECT 2.675 AS valor, ROUND(2.675, 2) AS redondeado, TRUNC(2.675, 2) AS truncado FROM dual;
```

Salida real:

```text
VALOR REDONDEADO	TRUNCADO
---------- ---------- ----------
     2,675	 2,68	    2,67

1 fila seleccionada.
```

## E071 · MOD

**Objetivo:** Residuo de una división.  
**Explicación:** MOD(10,3) = 1. Sirve para saber si un número es par.  
**Estado:** OK

```sql
SELECT id_deposito, MOD(id_deposito, 2) AS es_impar FROM deposito WHERE ROWNUM <= 4;
```

Salida real:

```text
ID_DEPOSITO   ES_IMPAR
----------- ----------
	  1	     1
	  2	     0
	  3	     1
	  4	     0

4 filas seleccionadas.
```

## E072 · NVL

**Objetivo:** Reemplazar NULL por un valor.  
**Explicación:** NVL(x, 0) devuelve 0 cuando x es NULL.  
**Estado:** OK

```sql
SELECT id_deposito, NVL(peso_medido_kg, 0) AS medido_o_cero FROM deposito WHERE ROWNUM <= 5;
```

Salida real:

```text
ID_DEPOSITO MEDIDO_O_CERO
----------- -------------
	  1		0
	  2		0
	  3		0
	  4		0
	  5		0

5 filas seleccionadas.
```

## E073 · NVL2

**Objetivo:** Un valor si hay dato, otro si no.  
**Explicación:** NVL2(x, si_hay, si_no_hay).  
**Estado:** OK

```sql
SELECT id_deposito, NVL2(peso_medido_kg, 'Medido', 'Solo estimado') AS tipo_peso FROM deposito WHERE ROWNUM <= 5;
```

Salida real:

```text
ID_DEPOSITO TIPO_PESO
----------- -------------
	  1 Solo estimado
	  2 Solo estimado
	  3 Solo estimado
	  4 Solo estimado
	  5 Solo estimado

5 filas seleccionadas.
```

## E074 · COALESCE

**Objetivo:** Primer valor no nulo.  
**Explicación:** COALESCE(a, b, c) devuelve el primero que tenga dato.  
**Estado:** OK

```sql
SELECT id_deposito, COALESCE(cantidad_verificada, cantidad_declarada, 1) AS unidades FROM deposito WHERE ROWNUM <= 5;
```

Salida real:

```text
ID_DEPOSITO   UNIDADES
----------- ----------
	  1	     1
	  2	     3
	  3	     2
	  4	     1
	  5	     1

5 filas seleccionadas.
```

## E075 · TO_CHAR con fechas

**Objetivo:** Mostrar la fecha con formato.  
**Explicación:** TO_CHAR convierte fecha en texto con el formato que quieras.  
**Estado:** OK

```sql
SELECT TO_CHAR(SYSDATE, 'DD/MM/YYYY HH24:MI') AS ahora, TO_CHAR(SYSDATE, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia FROM dual;
```

Salida real:

```text
AHORA		 DIA
---------------- ------------------------------------
11/11/2026 20:27 Miércoles

1 fila seleccionada.
```

## E076 · TO_DATE

**Objetivo:** Convertir texto en fecha.  
**Explicación:** Hay que decir el formato del texto.  
**Estado:** OK

```sql
SELECT TO_DATE('20/09/2026', 'DD/MM/YYYY') AS fecha_avance FROM dual;
```

Salida real:

```text
FECHA_AVANCE
----------------
2026-09-20 00:00

1 fila seleccionada.
```

## E077 · Diferencia de fechas

**Objetivo:** Días entre dos fechas.  
**Explicación:** Restar dos DATE da días.  
**Estado:** OK

```sql
SELECT TO_DATE('22/09/2026','DD/MM/YYYY') - TO_DATE('18/09/2026','DD/MM/YYYY') AS dias_del_avance FROM dual;
```

Salida real:

```text
DIAS_DEL_AVANCE
---------------
	      4

1 fila seleccionada.
```

## E078 · ADD_MONTHS y LAST_DAY

**Objetivo:** Fechas de calendario.  
**Explicación:** ADD_MONTHS suma meses; LAST_DAY da el último día del mes.  
**Estado:** OK

```sql
SELECT ADD_MONTHS(TRUNC(SYSDATE), 1) AS en_un_mes, LAST_DAY(SYSDATE) AS fin_de_mes FROM dual;
```

Salida real:

```text
EN_UN_MES	 FIN_DE_MES
---------------- ----------------
2026-12-11 00:00 2026-11-30 20:27

1 fila seleccionada.
```

## E079 · DECODE

**Objetivo:** Traducir códigos (estilo Oracle).  
**Explicación:** DECODE(valor, si1, entonces1, ..., otro).  
**Estado:** OK

```sql
SELECT codigo, DECODE(categoria, 1, 'Blanca', 2, 'Verde', 3, 'Negra', 'Ruta especial') AS caneca FROM material WHERE ROWNUM <= 6;
```

Salida real:

```text
CODIGO															 CANECA
------------------------------------------------------------------------------------------------------------------------ -------------
RESTO_FRUTA_VERDURA													 Verde
TETRAPAK														 Blanca
HOJAS_PODA														 Verde
TAPABOCAS														 Negra
ICOPOR_SUCIO														 Negra
CASCARA_HUEVO														 Verde

6 filas seleccionadas.
```

## E080 · Gramos y kilogramos

**Objetivo:** Mostrar el mismo peso en g y kg.  
**Explicación:** Unidad clara: 1 kg = 1000 g.  
**Estado:** OK

```sql
SELECT id_deposito, peso_estimado_kg AS kg, peso_estimado_kg * 1000 AS g FROM deposito WHERE peso_estimado_kg IS NOT NULL AND ROWNUM <= 5;
```

Salida real:

```text
ID_DEPOSITO	    KG		G
----------- ---------- ----------
	  1	   ,25	      250
	  2	   ,36	      360
	  3	  ,008		8
	  4	  ,028	       28
	  5	  ,084	       84

5 filas seleccionadas.
```

## E081 · INITCAP

**Objetivo:** Poner en mayúscula la primera letra.  
**Explicación:** INITCAP convierte «cartón» en «Cartón».  
**Estado:** OK

```sql
SELECT codigo, INITCAP(LOWER(codigo)) AS bonito FROM material WHERE ROWNUM <= 5;
```

Salida real:

```text
CODIGO
------------------------------------------------------------------------------------------------------------------------
BONITO
------------------------------------------------------------------------------------------------------------------------
RESTO_FRUTA_VERDURA
Resto_Fruta_Verdura

TETRAPAK
Tetrapak

HOJAS_PODA
Hojas_Poda

TAPABOCAS
Tapabocas

ICOPOR_SUCIO
Icopor_Sucio


5 filas seleccionadas.
```

## E082 · LPAD y RPAD

**Objetivo:** Rellenar para alinear.  
**Explicación:** LPAD agrega caracteres a la izquierda: sirve para seudónimos como C-0007.  
**Estado:** OK

```sql
SELECT 'C-' || LPAD(7, 4, '0') AS seudonimo, RPAD('PET', 8, '.') || '|' AS relleno FROM dual;
```

Salida real:

```text
SEUDON RELLENO
------ ---------
C-0007 PET.....|

1 fila seleccionada.
```

## E083 · TRIM

**Objetivo:** Quitar espacios sobrantes.  
**Explicación:** TRIM limpia al inicio y al final; por eso el CHECK de nombre lo exige.  
**Estado:** OK

```sql
SELECT '[' || TRIM('   María   ') || ']' AS limpio, LENGTH('   María   ') AS antes, LENGTH(TRIM('   María   ')) AS despues FROM dual;
```

Salida real:

```text
LIMPIO	      ANTES    DESPUES
-------- ---------- ----------
[María]          11          5

1 fila seleccionada.
```

## E084 · GREATEST y LEAST

**Objetivo:** El mayor y el menor de varios valores.  
**Explicación:** Comparan columnas de una misma fila.  
**Estado:** OK

```sql
SELECT clave_objeto, GREATEST(gramos_minimo, gramos_tipico, gramos_maximo) AS mayor, LEAST(gramos_minimo, gramos_tipico, gramos_maximo) AS menor FROM peso_referencial WHERE ROWNUM <= 5;
```

Salida real:

```text
CLAVE_OBJETO																			      MAYOR	 MENOR
---------------------------------------------------------------------------------------------------------------------------------------------------------------- ---------- ----------
CAJA_CARTON_PEQUENA																			250	    50
BOTELLA_PET_600ML																			450	   100
LATA_CONSERVA																				 70	    40
PUNADO_HOJAS																				100	     5
SERVILLETA																				  4	     1

5 filas seleccionadas.
```

## E085 · CEIL y FLOOR

**Objetivo:** Redondear hacia arriba y hacia abajo.  
**Explicación:** CEIL(2.1)=3 y FLOOR(2.9)=2.  
**Estado:** OK

```sql
SELECT CEIL(2.1) AS techo, FLOOR(2.9) AS piso, CEIL(-2.1) AS techo_negativo FROM dual;
```

Salida real:

```text
TECHO	 PISO TECHO_NEGATIVO
---------- ---------- --------------
	 3	    2		  -2

1 fila seleccionada.
```

## E086 · NULLIF

**Objetivo:** Evitar dividir por cero.  
**Explicación:** NULLIF(a, b) devuelve NULL si a = b; dividir por NULL da NULL, no error.  
**Estado:** OK

```sql
SELECT 10 / NULLIF(0, 0) AS sin_error, NVL(10 / NULLIF(0, 0), 0) AS con_cero FROM dual;
```

Salida real:

```text
SIN_ERROR   CON_CERO
---------- ----------
		    0

1 fila seleccionada.
```

## E087 · REGEXP_LIKE

**Objetivo:** Validar un correo con expresión regular.  
**Explicación:** Es la misma idea que usa el CHECK del correo.  
**Estado:** OK

```sql
SELECT correo_electronico FROM usuario WHERE REGEXP_LIKE(correo_electronico, '^[a-z0-9._-]+@ejemplo\.test$') FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
CORREO_ELECTRONICO
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
operario.uno@ejemplo.test
operaria.dos@ejemplo.test
administradora@ejemplo.test
ciudadano01@ejemplo.test
ciudadano02@ejemplo.test

5 filas seleccionadas.
```

## E180 · UTC a Colombia

**Objetivo:** Convertir una fecha guardada en UTC.  
**Explicación:** FROM_TZ dice «esta hora es UTC»; AT TIME ZONE la pasa a Bogotá (UTC−5).  
**Estado:** OK

```sql
SELECT fecha_deteccion_utc, CAST(FROM_TZ(fecha_deteccion_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE) AS colombia FROM deposito WHERE ROWNUM <= 3;
```

Salida real:

```text
FECHA_DETECCION_UTC							    COLOMBIA
--------------------------------------------------------------------------- ----------------
30/09/26 12:25:14,126637 AM						    2026-09-29 19:25
02/10/26 05:20:11,352546 AM						    2026-10-02 00:20
04/10/26 09:53:59,468095 AM						    2026-10-04 04:53

3 filas seleccionadas.
```

## E181 · Hoy en Colombia

**Objetivo:** Depósitos de hoy (hora Colombia).  
**Explicación:** Se compara con el inicio del día local.  
**Estado:** OK

```sql
SELECT COUNT(*) AS hoy FROM vw_deposito_detalle WHERE fecha_colombia >= TRUNC(CAST(SYSTIMESTAMP AT TIME ZONE 'America/Bogota' AS DATE));
```

Salida real:

```text
HOY
----------
	 3

1 fila seleccionada.
```

## E182 · Semana calendario actual

**Objetivo:** Lunes a domingo de esta semana.  
**Explicación:** TRUNC(fecha,'IW') = lunes.  
**Estado:** OK

```sql
SELECT TRUNC(SYSDATE, 'IW') AS lunes, TRUNC(SYSDATE, 'IW') + 6 AS domingo FROM dual;
```

Salida real:

```text
LUNES		 DOMINGO
---------------- ----------------
2026-11-09 00:00 2026-11-15 00:00

1 fila seleccionada.
```

## E183 · Mes calendario

**Objetivo:** Depósitos de este mes.  
**Explicación:** TRUNC(fecha,'MM') = día 1 del mes.  
**Estado:** OK

```sql
SELECT COUNT(*) FROM vw_deposito_detalle WHERE fecha_colombia >= TRUNC(SYSDATE, 'MM');
```

Salida real:

```text
COUNT(*)
----------
	41

1 fila seleccionada.
```

## E184 · Diferencia de TIMESTAMP

**Objetivo:** Segundos entre emitir y cerrar una orden.  
**Explicación:** EXTRACT saca partes de un intervalo.  
**Estado:** OK

```sql
SELECT id_orden, EXTRACT(SECOND FROM (fecha_cierre_utc - fecha_emision_utc)) + 60 * EXTRACT(MINUTE FROM (fecha_cierre_utc - fecha_emision_utc)) AS segundos FROM orden_apertura WHERE estado = 6 FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
ID_ORDEN   SEGUNDOS
---------- ----------
	 1	   14
	 2	   14
	 3	   14
	 4	   14
	 5	   14

5 filas seleccionadas.
```

## E185 · Día de la semana con más uso

**Objetivo:** ¿Qué día se recicla más?  
**Explicación:** TO_CHAR(fecha,'D') o el nombre del día.  
**Estado:** OK

```sql
SELECT TO_CHAR(fecha_colombia, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia, COUNT(*) FROM vw_deposito_detalle GROUP BY TO_CHAR(fecha_colombia, 'Day', 'NLS_DATE_LANGUAGE=SPANISH') ORDER BY 2 DESC;
```

Salida real:

```text
DIA				       COUNT(*)
------------------------------------ ----------
Sábado                                       20
Domingo 				     19
Martes					     19
Lunes					     14
Miércoles                                    13
Viernes 				     12
Jueves					      8

7 filas seleccionadas.
```

## E186 · FROM_TZ

**Objetivo:** Declarar que una hora está en UTC y pasarla a Colombia.  
**Explicación:** FROM_TZ le pone zona a un TIMESTAMP sin zona; AT TIME ZONE la convierte.  
**Estado:** OK

```sql
SELECT id_consulta, fecha_utc, FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS hora_colombia FROM consulta_vision WHERE ROWNUM <= 3;
```

Salida real:

```text
ID_CONSULTA FECHA_UTC									HORA_COLOMBIA
----------- --------------------------------------------------------------------------- ---------------------------------------------------------------------------
	  1 26/10/26 02:22:31,701302 AM 						25/10/26 09:22:31,701302 PM AMERICA/BOGOTA
	  2 10/10/26 08:48:56,470922 AM 						10/10/26 03:48:56,470922 AM AMERICA/BOGOTA
	  3 08/11/26 04:35:57,535226 AM 						07/11/26 11:35:57,535226 PM AMERICA/BOGOTA

3 filas seleccionadas.
```

## E187 · Hora del día

**Objetivo:** ¿A qué hora de Colombia se toman más fotos?  
**Explicación:** EXTRACT(HOUR ...) saca la hora; se resta la zona antes.  
**Estado:** OK

```sql
SELECT EXTRACT(HOUR FROM (FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota')) AS hora, COUNT(*) AS fotos FROM consulta_vision GROUP BY EXTRACT(HOUR FROM (FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota')) ORDER BY fotos DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
HORA	FOTOS
---------- ----------
	21	   11
	14	   11
	 0	   11
	23	   10
	 1	   10

5 filas seleccionadas.
```

## E188 · Lunes de la semana

**Objetivo:** TRUNC(fecha, 'IW') da el lunes ISO.  
**Explicación:** Sirve para agrupar por semana calendario.  
**Estado:** OK

```sql
SELECT TRUNC(DATE '2026-11-11', 'IW') AS lunes, TRUNC(DATE '2026-11-11', 'IW') + 6 AS domingo FROM dual;
```

Salida real:

```text
LUNES		 DOMINGO
---------------- ----------------
2026-11-09 00:00 2026-11-15 00:00

1 fila seleccionada.
```

## E189 · INTERVAL

**Objetivo:** Sumar 10 segundos (la ventana de depósito).  
**Explicación:** INTERVAL representa una duración.  
**Estado:** OK

```sql
SELECT SYSTIMESTAMP AS ahora, SYSTIMESTAMP + INTERVAL '10' SECOND AS cierre_ventana FROM dual;
```

Salida real:

```text
AHORA									    CIERRE_VENTANA
--------------------------------------------------------------------------- ---------------------------------------------------------------------------
11/11/26 08:27:10,747238 PM -05:00					    11/11/26 08:27:20,747238000 PM -05:00

1 fila seleccionada.
```

## E190 · Minutos entre emisión y vencimiento

**Objetivo:** Cuánto vive una orden de apertura.  
**Explicación:** Restar TIMESTAMP da un INTERVAL; EXTRACT saca sus partes.  
**Estado:** OK

```sql
SELECT id_orden, EXTRACT(MINUTE FROM (fecha_vencimiento_utc - fecha_emision_utc)) AS minutos, EXTRACT(SECOND FROM (fecha_vencimiento_utc - fecha_emision_utc)) AS segundos FROM orden_apertura WHERE ROWNUM <= 5;
```

Salida real:

```text
ID_ORDEN    MINUTOS	SEGUNDOS
---------- ---------- ----------
	 1	    2	       0
	 2	    2	       0
	 3	    2	       0
	 4	    2	       0
	 5	    2	       0

5 filas seleccionadas.
```

## E191 · Nombre del día

**Objetivo:** Día de la semana en español.  
**Explicación:** TO_CHAR con 'Day' y el idioma indicado.  
**Estado:** OK

```sql
SELECT TO_CHAR(DATE '2026-11-11', 'Day', 'NLS_DATE_LANGUAGE=SPANISH') AS dia FROM dual;
```

Salida real:

```text
DIA
----------
Miércoles

1 fila seleccionada.
```

## E207 · Vista para Power BI

**Objetivo:** Ver qué muestra la vista anónima.  
**Explicación:** El ciudadano aparece como C-0001: sin nombre ni correo.  
**Estado:** OK

```sql
SELECT ciudadano, dia, caneca, material, estado, origen_dato FROM vw_bi_depositos FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
CIUDADANO	   DIA		    CANECA MATERIAL														    ESTADO     ORIGEN_DATO
------------------ ---------------- ------ ------------------------------------------------------------------------------------------------------------------------ ---------- -----------------
C-0008             2026-10-25 00:00 NEGRA  CARTON_CONTAMINADO                                                                                                       VERIFICADO DATOS SINTÉTICOS
C-0008             2026-10-10 00:00 BLANCA CARTON                                                                                                                   PENDIENTE  DATOS SINTÉTICOS
C-0017             2026-11-07 00:00 NEGRA  TAPABOCAS                                                                                                                RECHAZADO  DATOS SINTÉTICOS
C-0006             2026-11-08 00:00 BLANCA LATA_ALUMINIO                                                                                                            VERIFICADO DATOS SINTÉTICOS
C-0017             2026-10-26 00:00 BLANCA TETRAPAK                                                                                                                 VERIFICADO DATOS SINTÉTICOS

5 filas seleccionadas.
```

## E208 · Privilegios concedidos

**Objetivo:** Qué puede leer el rol de informes.  
**Explicación:** USER_TAB_PRIVS_MADE muestra lo que yo concedí.  
**Estado:** OK

```sql
SELECT grantee, table_name, privilege FROM user_tab_privs_made ORDER BY table_name;
```

Salida real:

```text
GRANTEE
--------------------------------------------------------------------------------------------------------------------------------
TABLE_NAME															 PRIVILEGE
-------------------------------------------------------------------------------------------------------------------------------- ----------------------------------------
PUBLIC
ECOACCESS_BD															 INHERIT PRIVILEGES

ROL_ECO_LECTURA_BI
VW_BI_AUDITORIA 														 SELECT

ROL_ECO_LECTURA_BI
VW_BI_CIUDADANOS														 SELECT

ROL_ECO_LECTURA_BI
VW_BI_CONSULTAS_VISION														 SELECT

ROL_ECO_LECTURA_BI
VW_BI_DEPOSITOS 														 SELECT

ROL_ECO_LECTURA_BI
VW_BI_ECOPUNTOS 														 SELECT

ROL_ECO_LECTURA_BI
VW_CANECA_ESTACION														 SELECT


7 filas seleccionadas.
```

## E209 · Texto de una vista

**Objetivo:** Ver la consulta guardada.  
**Explicación:** USER_VIEWS.TEXT contiene el SELECT de la vista.  
**Estado:** OK

```sql
SELECT view_name, text_length FROM user_views ORDER BY view_name;
```

Salida real:

```text
VIEW_NAME															 TEXT_LENGTH
-------------------------------------------------------------------------------------------------------------------------------- -----------
VW_BI_AUDITORIA 															 660
VW_BI_CIUDADANOS															 504
VW_BI_CONSULTAS_VISION															1070
VW_BI_DEPOSITOS 															1616
VW_BI_ECOPUNTOS 															 640
VW_CANECA_ESTACION															 891
VW_DEPOSITO_DETALLE															1988
VW_RECORRIDO																1003
VW_SALDO_ECOPUNTOS															 630

9 filas seleccionadas.
```

## E210 · Vista de solo lectura

**Objetivo:** Intentar modificar una vista READ ONLY (ERROR ESPERADO).  
**Explicación:** WITH READ ONLY impide INSERT/UPDATE/DELETE por la vista.  
**Estado:** OK (error esperado)

```sql
UPDATE vw_caneca_estacion SET caneca = 'X' WHERE id_contenedor = 11;
```

Salida real:

```text
UPDATE vw_caneca_estacion SET caneca = 'X' WHERE id_contenedor = 11
                              *
ERROR en línea 1:
ORA-42399: no se puede realizar una operación DML en una vista de sólo lectura
Ayuda: https://docs.oracle.com/error-help/db/ora-42399/
```

## E211 · Mis privilegios de sistema

**Objetivo:** ¿Qué puede hacer mi cuenta?  
**Explicación:** SESSION_PRIVS lista los privilegios activos de la sesión.  
**Estado:** OK

```sql
SELECT privilege FROM session_privs ORDER BY privilege;
```

Salida real:

```text
PRIVILEGE
----------------------------------------
CREATE PROCEDURE
CREATE SEQUENCE
CREATE SESSION
CREATE TABLE
CREATE TRIGGER
CREATE VIEW

6 filas seleccionadas.
```

## E212 · Columnas actualizables de una vista

**Objetivo:** ¿Se puede escribir en VW_CANECA_ESTACION?  
**Explicación:** USER_UPDATABLE_COLUMNS dice qué columnas aceptan UPDATE.  
**Estado:** OK

```sql
SELECT column_name, updatable FROM user_updatable_columns WHERE table_name = 'VW_CANECA_ESTACION' AND ROWNUM <= 6;
```

Salida real:

```text
COLUMN_NAME															 UPD
-------------------------------------------------------------------------------------------------------------------------------- ---
ID_PUNTO_INSTALACION														 NO
ESTACION															 NO
CODIGO_QR_ESTACION														 NO
ID_CONTENEDOR															 NO
CANECA																 NO
CATEGORIA															 NO

6 filas seleccionadas.
```

## E213 · Error ESPERADO: vista de solo lectura

**Objetivo:** Intentar modificar una vista WITH READ ONLY (ORA-42399).  
**Explicación:** Las vistas de Power BI no permiten escribir. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
UPDATE vw_caneca_estacion SET estacion = 'X' WHERE id_contenedor = 11;
```

Salida real:

```text
UPDATE vw_caneca_estacion SET estacion = 'X' WHERE id_contenedor = 11
                              *
ERROR en línea 1:
ORA-42399: no se puede realizar una operación DML en una vista de sólo lectura
Ayuda: https://docs.oracle.com/error-help/db/ora-42399/
```

## E214 · Lo que ve Power BI

**Objetivo:** Ranking desde la vista con seudónimos.  
**Explicación:** La vista nunca muestra el nombre real: solo C-0001.  
**Estado:** OK

```sql
SELECT ciudadano, COUNT(*) AS verificados FROM vw_bi_depositos WHERE estado = 'VERIFICADO' GROUP BY ciudadano ORDER BY verificados DESC FETCH FIRST 5 ROWS ONLY;
```

Salida real:

```text
CIUDADANO	   VERIFICADOS
------------------ -----------
C-0006			     9
C-0008			     5
C-0011			     5
C-0004			     5
C-0013			     5

5 filas seleccionadas.
```

## E215 · Vista de detalle

**Objetivo:** Un depósito con todo su contexto en una fila.  
**Explicación:** VW_DEPOSITO_DETALLE junta 6 tablas para que la consulta sea simple.  
**Estado:** OK

```sql
SELECT id_deposito, titular, color, material_declarado, estado_texto, peso_estimado_g FROM vw_deposito_detalle WHERE ROWNUM <= 5;
```

Salida real:

```text
ID_DEPOSITO
-----------
TITULAR
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
COLOR  MATERIAL_DECLARADO													ESTADO_TEX PESO_ESTIMADO_G
------ ------------------------------------------------------------------------------------------------------------------------ ---------- ---------------
	  1
Valentina Fuentes Ramírez
NEGRA  CARTON_CONTAMINADO													VERIFICADO	       250

	  2
Valentina Fuentes Ramírez
BLANCA CARTON															PENDIENTE	       360

	  3
Miguel Molina Castro
NEGRA  TAPABOCAS														RECHAZADO		 8

	  4
María Gómez Oñate
BLANCA LATA_ALUMINIO														VERIFICADO		28

	  5
Miguel Molina Castro
BLANCA TETRAPAK 														VERIFICADO		84


5 filas seleccionadas.
```
