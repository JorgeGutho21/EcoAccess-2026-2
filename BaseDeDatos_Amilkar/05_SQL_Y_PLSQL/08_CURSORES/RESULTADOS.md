# CURSORES

Ejercicios: 14 · conexión: `ECOACCESS_BD` · temas del banco: 15. Cursores.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E234 | Cursor implícito SQL%ROWCOUNT | OK |
| E235 | Cursor FOR (implícito) | OK |
| E236 | Cursor explícito | OK |
| E237 | Cursor con parámetro | OK |
| E238 | Cursor con %ROWTYPE | OK |
| E239 | SYS_REFCURSOR | OK |
| E240 | Acumular con cursor | OK |
| E241 | Cursor FOR UPDATE | OK |
| E242 | %FOUND y %NOTFOUND | OK |
| E243 | %ISOPEN | OK |
| E244 | Cursores anidados | OK |
| E245 | BULK COLLECT con LIMIT | OK |
| E246 | Cursor dinámico | OK |
| E247 | Recorrer el cursor de un paquete | OK |

## E234 · Cursor implícito SQL%ROWCOUNT

**Objetivo:** Cuántas filas cambió un UPDATE.  
**Explicación:** SQL% describe la última sentencia.  
**Estado:** OK

```sql
BEGIN
    UPDATE material SET habilitado = habilitado WHERE categoria = 1;
    DBMS_OUTPUT.PUT_LINE('Filas tocadas: ' || SQL%ROWCOUNT);
    ROLLBACK;
END;
/
```

Salida real:

```text
Filas tocadas: 8

Procedimiento PL/SQL terminado correctamente.
```

## E235 · Cursor FOR (implícito)

**Objetivo:** Recorrer filas fácilmente.  
**Explicación:** Oracle abre, lee y cierra solo.  
**Estado:** OK

```sql
BEGIN
    FOR r IN (SELECT codigo, nombre FROM material WHERE categoria = 2 ORDER BY codigo) LOOP
        DBMS_OUTPUT.PUT_LINE(r.codigo || ': ' || r.nombre);
    END LOOP;
END;
/
```

Salida real:

```text
BORRA_CAFE: Borra de café e infusiones
CASCARA_HUEVO: Cáscara de huevo
HOJAS_PODA: Hojas y poda
RESTO_COMIDA: Restos de comida
RESTO_FRUTA_VERDURA: Restos de frutas y verduras

Procedimiento PL/SQL terminado correctamente.
```

## E236 · Cursor explícito

**Objetivo:** OPEN, FETCH, CLOSE paso a paso.  
**Explicación:** %NOTFOUND indica que no hay más filas.  
**Estado:** OK

```sql
DECLARE
    CURSOR c IS SELECT nombre FROM contenedor ORDER BY id_contenedor;
    v_nombre contenedor.nombre%TYPE;
BEGIN
    OPEN c;
    LOOP
        FETCH c INTO v_nombre;
        EXIT WHEN c%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(c%ROWCOUNT || '. ' || v_nombre);
    END LOOP;
    CLOSE c;
END;
/
```

Salida real:

```text
1. UPC01 - Caneca blanca
2. UPC01 - Caneca verde
3. UPC01 - Caneca negra
4. Legado - Caneca blanca

Procedimiento PL/SQL terminado correctamente.
```

## E237 · Cursor con parámetro

**Objetivo:** El mismo cursor para distintos colores.  
**Explicación:** Los parámetros hacen reutilizable el cursor.  
**Estado:** OK

```sql
DECLARE
    CURSOR c (p_cat NUMBER) IS SELECT codigo FROM material WHERE categoria = p_cat ORDER BY codigo;
BEGIN
    FOR r IN c(3) LOOP
        DBMS_OUTPUT.PUT_LINE('Negra: ' || r.codigo);
    END LOOP;
END;
/
```

Salida real:

```text
Negra: CARTON_CONTAMINADO
Negra: EMPAQUE_METALIZADO
Negra: ICOPOR_SUCIO
Negra: SERVILLETA_USADA
Negra: TAPABOCAS

Procedimiento PL/SQL terminado correctamente.
```

## E238 · Cursor con %ROWTYPE

**Objetivo:** Leer filas completas del cursor.  
**Explicación:** cursor%ROWTYPE toma la forma del SELECT.  
**Estado:** OK

```sql
DECLARE
    CURSOR c IS SELECT id_deposito, estado_texto, color FROM vw_deposito_detalle WHERE estado_verificacion = 2;
    r c%ROWTYPE;
BEGIN
    OPEN c;
    FETCH c INTO r;
    DBMS_OUTPUT.PUT_LINE('Primer rechazado: ' || r.id_deposito || ' en caneca ' || r.color);
    CLOSE c;
END;
/
```

Salida real:

```text
Primer rechazado: 37 en caneca BLANCA

Procedimiento PL/SQL terminado correctamente.
```

## E239 · SYS_REFCURSOR

**Objetivo:** Cursor que se devuelve a otro programa (C#).  
**Explicación:** Lo usa fn_depositos_ciudadano.  
**Estado:** OK

```sql
VARIABLE rc REFCURSOR
EXEC :rc := fn_depositos_ciudadano(6)
PRINT rc
```

Salida real:

```text
Procedimiento PL/SQL terminado correctamente.


ID_DEPOSITO FECHA_COLOMBIA   COLOR  MATERIAL_DECLARADO
----------- ---------------- ------ ------------------------------------------------------------------------------------------------------------------------
MATERIAL_VERIFICADO													 ESTADO_TEX EVIDENCIA_FISICA	 PESO_ESTIMADO_G PESO_MEDIDO_G
------------------------------------------------------------------------------------------------------------------------ ---------- -------------------- --------------- -------------
	 24 2026-11-09 06:47 BLANCA PET
PET															 VERIFICADO DETECTADO POR SENSOR	      76

	  4 2026-11-08 06:58 BLANCA LATA_ALUMINIO
LATA_ALUMINIO														 VERIFICADO DETECTADO POR SENSOR	      28

	 33 2026-11-08 04:34 BLANCA PAPEL
SERVILLETA_USADA													 VERIFICADO DETECTADO POR SENSOR	       5

	 16 2026-11-06 13:49 BLANCA PET
PET															 VERIFICADO DETECTADO POR SENSOR	      22

	 88 2026-11-06 04:12 VERDE  BORRA_CAFE
BORRA_CAFE														 VERIFICADO DECLARADO SIN SENSOR	      20

	 84 2026-11-03 06:42 VERDE  RESTO_COMIDA
RESTO_COMIDA														 VERIFICADO DECLARADO SIN SENSOR	     150

	 25 2026-10-30 04:37 VERDE  RESTO_FRUTA_VERDURA
RESTO_FRUTA_VERDURA													 VERIFICADO DETECTADO POR SENSOR	      30

	 93 2026-10-24 22:09 BLANCA METAL_FERROSO
															 RECHAZADO  DECLARADO SIN SENSOR	     110

	 28 2026-10-21 03:19 BLANCA LATA_ALUMINIO
LATA_ALUMINIO														 VERIFICADO DETECTADO POR SENSOR	      14	  12,7

	 90 2026-10-17 09:54 VERDE  HOJAS_PODA
HOJAS_PODA														 VERIFICADO DETECTADO POR SENSOR	      20

	 63 2026-10-03 20:44 VERDE  RESTO_FRUTA_VERDURA
															 RECHAZADO  DECLARADO SIN SENSOR	      30

… (salida recortada; completa en 16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt)
```

## E240 · Acumular con cursor

**Objetivo:** Sumar kilos verificados con un cursor.  
**Explicación:** Ejemplo de cálculo fila por fila (en SQL sería SUM).  
**Estado:** OK

```sql
DECLARE
    v_total NUMBER := 0;
BEGIN
    FOR r IN (SELECT peso_estimado_kg FROM deposito WHERE estado_verificacion = 1 AND peso_estimado_kg IS NOT NULL) LOOP
        v_total := v_total + r.peso_estimado_kg;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Kg estimados verificados: ' || ROUND(v_total, 3));
END;
/
```

Salida real:

```text
Kg estimados verificados: 7,697

Procedimiento PL/SQL terminado correctamente.
```

## E241 · Cursor FOR UPDATE

**Objetivo:** Bloquear filas para modificarlas (con ROLLBACK).  
**Explicación:** WHERE CURRENT OF actualiza la fila del cursor.  
**Estado:** OK

```sql
DECLARE
    CURSOR c IS SELECT id_material, descripcion FROM material WHERE categoria = 4 FOR UPDATE OF descripcion;
BEGIN
    FOR r IN c LOOP
        UPDATE material SET descripcion = descripcion || ' (peligroso)' WHERE CURRENT OF c;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('Actualizados y deshechos.');
    ROLLBACK;
END;
/
```

Salida real:

```text
Actualizados y deshechos.

Procedimiento PL/SQL terminado correctamente.
```

## E242 · %FOUND y %NOTFOUND

**Objetivo:** Recorrer un cursor manualmente.  
**Explicación:** %NOTFOUND se vuelve TRUE cuando ya no hay filas.  
**Estado:** OK

```sql
DECLARE
    CURSOR c IS SELECT nombre FROM contenedor ORDER BY id_contenedor;
    v contenedor.nombre%TYPE;
BEGIN
    OPEN c;
    LOOP
        FETCH c INTO v;
        EXIT WHEN c%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(c%ROWCOUNT || '. ' || v);
    END LOOP;
    CLOSE c;
END;
/
```

Salida real:

```text
1. UPC01 - Caneca blanca
2. UPC01 - Caneca verde
3. UPC01 - Caneca negra
4. Legado - Caneca blanca

Procedimiento PL/SQL terminado correctamente.
```

## E243 · %ISOPEN

**Objetivo:** Preguntar si un cursor está abierto.  
**Explicación:** Evita el error de cerrar un cursor ya cerrado.  
**Estado:** OK

```sql
DECLARE
    CURSOR c IS SELECT 1 FROM dual;
BEGIN
    DBMS_OUTPUT.PUT_LINE('Antes: ' || CASE WHEN c%ISOPEN THEN 'abierto' ELSE 'cerrado' END);
    OPEN c;
    DBMS_OUTPUT.PUT_LINE('Después: ' || CASE WHEN c%ISOPEN THEN 'abierto' ELSE 'cerrado' END);
    CLOSE c;
END;
/
```

Salida real:

```text
Antes: cerrado
Después: abierto

Procedimiento PL/SQL terminado correctamente.
```

## E244 · Cursores anidados

**Objetivo:** Cada caneca con sus materiales.  
**Explicación:** Un cursor FOR dentro de otro, usando un parámetro.  
**Estado:** OK

```sql
DECLARE
    CURSOR c_mat(p_cont NUMBER) IS SELECT m.codigo FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material WHERE cm.id_contenedor = p_cont ORDER BY m.codigo;
BEGIN
    FOR c IN (SELECT id_contenedor, nombre FROM contenedor WHERE id_punto_instalacion = 1 ORDER BY id_contenedor) LOOP
        DBMS_OUTPUT.PUT(c.nombre || ': ');
        FOR m IN c_mat(c.id_contenedor) LOOP DBMS_OUTPUT.PUT(m.codigo || ' '); END LOOP;
        DBMS_OUTPUT.NEW_LINE;
    END LOOP;
END;
/
```

Salida real:

```text
UPC01 - Caneca blanca: CARTON LATA_ALUMINIO METAL_FERROSO PAPEL PET PLASTICO_RIGIDO TETRAPAK VIDRIO
UPC01 - Caneca verde: BORRA_CAFE CASCARA_HUEVO HOJAS_PODA RESTO_COMIDA RESTO_FRUTA_VERDURA
UPC01 - Caneca negra: CARTON_CONTAMINADO EMPAQUE_METALIZADO ICOPOR_SUCIO SERVILLETA_USADA TAPABOCAS

Procedimiento PL/SQL terminado correctamente.
```

## E245 · BULK COLLECT con LIMIT

**Objetivo:** Traer filas por lotes.  
**Explicación:** LIMIT evita cargar millones de filas en memoria.  
**Estado:** OK

```sql
DECLARE
    CURSOR c IS SELECT id_consulta FROM consulta_vision;
    TYPE t_ids IS TABLE OF consulta_vision.id_consulta%TYPE;
    v t_ids;
    lotes NUMBER := 0;
BEGIN
    OPEN c;
    LOOP
        FETCH c BULK COLLECT INTO v LIMIT 50;
        EXIT WHEN v.COUNT = 0;
        lotes := lotes + 1;
        DBMS_OUTPUT.PUT_LINE('Lote ' || lotes || ': ' || v.COUNT || ' fotos');
    END LOOP;
    CLOSE c;
END;
/
```

Salida real:

```text
Lote 1: 50 fotos
Lote 2: 50 fotos
Lote 3: 50 fotos
Lote 4: 37 fotos

Procedimiento PL/SQL terminado correctamente.
```

## E246 · Cursor dinámico

**Objetivo:** OPEN FOR con un texto SQL.  
**Explicación:** Útil cuando la consulta se arma en tiempo de ejecución; el parámetro va con USING.  
**Estado:** OK

```sql
DECLARE
    rc SYS_REFCURSOR;
    v_n NUMBER;
BEGIN
    OPEN rc FOR 'SELECT COUNT(*) FROM material WHERE categoria = :c' USING 2;
    FETCH rc INTO v_n;
    CLOSE rc;
    DBMS_OUTPUT.PUT_LINE('Materiales de la caneca verde: ' || v_n);
END;
/
```

Salida real:

```text
Materiales de la caneca verde: 5

Procedimiento PL/SQL terminado correctamente.
```

## E247 · Recorrer el cursor de un paquete

**Objetivo:** Leer el ranking que devuelve PKG_ESTADISTICAS.  
**Explicación:** Un SYS_REFCURSOR se recorre con FETCH igual que un cursor normal.  
**Estado:** OK

```sql
DECLARE
    rc SYS_REFCURSOR;
    v_id NUMBER; v_nom VARCHAR2(100); v_dep NUMBER; v_uni NUMBER; v_est NUMBER; v_med NUMBER; v_pue NUMBER;
BEGIN
    rc := pkg_estadisticas.ranking_verificados(SYSDATE - 30, SYSDATE, 3);
    LOOP
        FETCH rc INTO v_id, v_nom, v_dep, v_uni, v_est, v_med, v_pue;
        EXIT WHEN rc%NOTFOUND;
        DBMS_OUTPUT.PUT_LINE(v_pue || '. ' || v_nom || ' - ' || v_dep || ' verificados');
    END LOOP;
    CLOSE rc;
END;
/
```

Salida real:

```text
1. María Gómez Oñate - 9 verificados
2. Santiago Quintero Fuentes - 5 verificados
3. Ana Castro Vargas - 4 verificados

Procedimiento PL/SQL terminado correctamente.
```
