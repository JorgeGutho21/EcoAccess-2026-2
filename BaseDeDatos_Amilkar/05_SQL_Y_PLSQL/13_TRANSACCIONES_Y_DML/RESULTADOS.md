# TRANSACCIONES Y DML

Ejercicios: 15 · conexión: `ECOACCESS_BD` · temas del banco: 12. Modificar datos (DML) y transacciones.

Estado y salida de la última ejecución real en Oracle (datos sintéticos). «OK (error esperado)» significa que el ejercicio demuestra un error a propósito.

| Id | Ejercicio | Estado |
|---|---|---|
| E192 | INSERT y ROLLBACK | OK |
| E193 | UPDATE | OK |
| E194 | DELETE | OK |
| E195 | SAVEPOINT | OK |
| E196 | Error ESPERADO: CHECK | OK (error esperado) |
| E197 | Error ESPERADO: UNIQUE | OK (error esperado) |
| E198 | Error ESPERADO: FK | OK (error esperado) |
| E199 | Error ESPERADO: NOT NULL | OK (error esperado) |
| E200 | INSERT de varias filas | OK |
| E201 | MERGE | OK |
| E202 | UPDATE de varias filas | OK |
| E203 | RETURNING INTO | OK |
| E204 | Error ESPERADO: IDENTITY ALWAYS | OK (error esperado) |
| E205 | Error ESPERADO: borrar un padre | OK (error esperado) |
| E206 | Transacción de dos pasos | OK |

## E192 · INSERT y ROLLBACK

**Objetivo:** Insertar un material y deshacerlo.  
**Explicación:** INSERT agrega una fila; ROLLBACK deshace lo no confirmado.  
**Estado:** OK

```sql
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_X', 'Material de prueba', 1);
SELECT codigo FROM material WHERE codigo = 'PRUEBA_X';
ROLLBACK;
SELECT COUNT(*) AS despues_del_rollback FROM material WHERE codigo = 'PRUEBA_X';
```

Salida real:

```text
1 fila creada.


CODIGO
------------------------------------------------------------------------------------------------------------------------
PRUEBA_X

1 fila seleccionada.


Rollback terminado.


DESPUES_DEL_ROLLBACK
--------------------
		   0

1 fila seleccionada.
```

## E193 · UPDATE

**Objetivo:** Cambiar una descripción (y deshacer).  
**Explicación:** UPDATE ... SET ... WHERE. ¡Sin WHERE cambia TODAS las filas!  
**Estado:** OK

```sql
UPDATE material SET descripcion = 'Texto temporal' WHERE codigo = 'PET';
SELECT descripcion FROM material WHERE codigo = 'PET';
ROLLBACK;
```

Salida real:

```text
1 fila actualizada.


DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Texto temporal

1 fila seleccionada.


Rollback terminado.
```

## E194 · DELETE

**Objetivo:** Borrar una fila de prueba (y deshacer).  
**Explicación:** DELETE ... WHERE elimina filas.  
**Estado:** OK

```sql
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_Y', 'Para borrar', 3);
DELETE FROM material WHERE codigo = 'PRUEBA_Y';
SELECT COUNT(*) FROM material WHERE codigo = 'PRUEBA_Y';
ROLLBACK;
```

Salida real:

```text
1 fila creada.


1 fila suprimida.


  COUNT(*)
----------
	 0

1 fila seleccionada.


Rollback terminado.
```

## E195 · SAVEPOINT

**Objetivo:** Deshacer solo una parte.  
**Explicación:** ROLLBACK TO vuelve al punto guardado.  
**Estado:** OK

```sql
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_A', 'Uno', 1);
SAVEPOINT despues_de_a;
INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_B', 'Dos', 1);
ROLLBACK TO despues_de_a;
SELECT codigo FROM material WHERE codigo LIKE 'PRUEBA_%';
ROLLBACK;
```

Salida real:

```text
1 fila creada.


Punto de grabación creado.


1 fila creada.


Rollback terminado.


CODIGO
------------------------------------------------------------------------------------------------------------------------
PRUEBA_A

1 fila seleccionada.


Rollback terminado.
```

## E196 · Error ESPERADO: CHECK

**Objetivo:** Ver una restricción en acción (ORA-02290).  
**Explicación:** Oracle rechaza datos que rompen una regla. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
INSERT INTO material (codigo, nombre, categoria) VALUES ('MAL', 'Categoría mala', 7);
```

Salida real:

```text
INSERT INTO material (codigo, nombre, categoria) VALUES ('MAL', 'Categoría mala', 7)
*
ERROR en línea 1:
ORA-02290: restricción de control (ECOACCESS_BD.CK_MATERIAL_CATEGORIA) violada
Ayuda: https://docs.oracle.com/error-help/db/ora-02290/
```

## E197 · Error ESPERADO: UNIQUE

**Objetivo:** Código repetido (ORA-00001).  
**Explicación:** UNIQUE impide duplicados. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Otro PET', 1);
```

Salida real:

```text
INSERT INTO material (codigo, nombre, categoria) VALUES ('PET', 'Otro PET', 1)
*
ERROR en línea 1:
ORA-00001: Se ha violado la restricción única (ECOACCESS_BD.UQ_MATERIAL_CODIGO) para la tabla ECOACCESS_BD.MATERIAL en las columnas (CODIGO)
ORA-03301: (Detalles de ORA-00001) ya existe la fila con los valores de columna (CODIGO:'PET')
Ayuda: https://docs.oracle.com/error-help/db/ora-00001/
```

## E198 · Error ESPERADO: FK

**Objetivo:** Caneca en una estación que no existe (ORA-02291).  
**Explicación:** La FK exige que el padre exista. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (999, 'Caneca fantasma', 1);
```

Salida real:

```text
INSERT INTO contenedor (id_punto_instalacion, nombre, categoria) VALUES (999, 'Caneca fantasma', 1)
*
ERROR en línea 1:
ORA-02291: restricción de integridad (ECOACCESS_BD.FK_CONTENEDOR_PUNTO) violada - clave principal no encontrada
Ayuda: https://docs.oracle.com/error-help/db/ora-02291/
```

## E199 · Error ESPERADO: NOT NULL

**Objetivo:** Material sin nombre (ORA-01400).  
**Explicación:** NOT NULL exige un valor. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
INSERT INTO material (codigo, nombre, categoria) VALUES ('SIN_NOMBRE', NULL, 1);
```

Salida real:

```text
INSERT INTO material (codigo, nombre, categoria) VALUES ('SIN_NOMBRE', NULL, 1)
                                                                       *
ERROR en línea 1:
ORA-01400: no se puede realizar una inserción NULL en ("ECOACCESS_BD"."MATERIAL"."NOMBRE")
Ayuda: https://docs.oracle.com/error-help/db/ora-01400/
```

## E200 · INSERT de varias filas

**Objetivo:** Agregar dos materiales a la vez (y deshacer).  
**Explicación:** INSERT ... SELECT inserta todas las filas que devuelve la consulta; cada una recibe su propio id IDENTITY (con INSERT ALL se repetiría el id y daría ORA-00001).  
**Estado:** OK

```sql
INSERT INTO material (codigo, nombre, categoria) SELECT 'PRUEBA_M1', 'Prueba uno', 1 FROM dual UNION ALL SELECT 'PRUEBA_M2', 'Prueba dos', 3 FROM dual;
SELECT codigo FROM material WHERE codigo LIKE 'PRUEBA_M%';
ROLLBACK;
```

Salida real:

```text
2 filas creadas.


CODIGO
------------------------------------------------------------------------------------------------------------------------
PRUEBA_M1
PRUEBA_M2

2 filas seleccionadas.


Rollback terminado.
```

## E201 · MERGE

**Objetivo:** Insertar o actualizar según exista (y deshacer).  
**Explicación:** MERGE decide fila por fila: WHEN MATCHED actualiza, WHEN NOT MATCHED inserta.  
**Estado:** OK

```sql
MERGE INTO material m USING (SELECT 'PET' AS codigo FROM dual UNION ALL SELECT 'PRUEBA_MERGE' FROM dual) s ON (m.codigo = s.codigo) WHEN MATCHED THEN UPDATE SET m.descripcion = 'Actualizado por MERGE' WHEN NOT MATCHED THEN INSERT (codigo, nombre, categoria) VALUES (s.codigo, 'Nuevo por MERGE', 1);
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'PRUEBA_MERGE');
ROLLBACK;
```

Salida real:

```text
2 filas fusionadas.


CODIGO
------------------------------------------------------------------------------------------------------------------------
DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
PET
Actualizado por MERGE

PRUEBA_MERGE



2 filas seleccionadas.


Rollback terminado.
```

## E202 · UPDATE de varias filas

**Objetivo:** Deshabilitar todos los peligrosos (y deshacer).  
**Explicación:** SQL%ROWCOUNT / el mensaje dice cuántas filas cambiaron.  
**Estado:** OK

```sql
UPDATE material SET habilitado = 0 WHERE categoria = 4;
SELECT codigo, habilitado FROM material WHERE categoria = 4;
ROLLBACK;
```

Salida real:

```text
3 filas actualizadas.


CODIGO															 HABILITADO
------------------------------------------------------------------------------------------------------------------------ ----------
MEDICAMENTOS															  0
BOMBILLO															  0
PILAS																  0

3 filas seleccionadas.


Rollback terminado.
```

## E203 · RETURNING INTO

**Objetivo:** Saber el id que generó un INSERT.  
**Explicación:** RETURNING devuelve columnas de la fila insertada.  
**Estado:** OK

```sql
DECLARE
    v_id material.id_material%TYPE;
BEGIN
    INSERT INTO material (codigo, nombre, categoria) VALUES ('PRUEBA_RET', 'Con returning', 1) RETURNING id_material INTO v_id;
    DBMS_OUTPUT.PUT_LINE('Id generado: ' || v_id);
    ROLLBACK;
END;
/
```

Salida real:

```text
Id generado: 436

Procedimiento PL/SQL terminado correctamente.
```

## E204 · Error ESPERADO: IDENTITY ALWAYS

**Objetivo:** Escribir el id de un usuario a mano (ORA-32795).  
**Explicación:** Con GENERATED ALWAYS solo Oracle pone el id. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
INSERT INTO usuario (id_usuario, nombre, correo_electronico) VALUES (99999, 'Prueba', 'prueba@ejemplo.test');
```

Salida real:

```text
INSERT INTO usuario (id_usuario, nombre, correo_electronico) VALUES (99999, 'Prueba', 'prueba@ejemplo.test')
                     *
ERROR en línea 1:
ORA-32795: no se puede insertar una columna de identidad siempre generada
Ayuda: https://docs.oracle.com/error-help/db/ora-32795/
```

## E205 · Error ESPERADO: borrar un padre

**Objetivo:** Borrar un material usado por canecas (ORA-02292).  
**Explicación:** Sin ON DELETE CASCADE, Oracle protege a los hijos. ERROR ESPERADO.  
**Estado:** OK (error esperado)

```sql
DELETE FROM material WHERE codigo = 'PET';
```

Salida real:

```text
DELETE FROM material WHERE codigo = 'PET'
*
ERROR en línea 1:
ORA-02292: restricción de integridad (ECOACCESS_BD.FK_CONT_MAT_MATERIAL) violada - registro secundario encontrado
Ayuda: https://docs.oracle.com/error-help/db/ora-02292/
```

## E206 · Transacción de dos pasos

**Objetivo:** Dos cambios que se confirman o se deshacen juntos.  
**Explicación:** Hasta el COMMIT, nadie más ve los cambios; aquí se deshacen con ROLLBACK.  
**Estado:** OK

```sql
UPDATE material SET descripcion = 'Paso 1' WHERE codigo = 'PET';
UPDATE material SET descripcion = 'Paso 2' WHERE codigo = 'VIDRIO';
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'VIDRIO');
ROLLBACK;
SELECT codigo, descripcion FROM material WHERE codigo IN ('PET', 'VIDRIO');
```

Salida real:

```text
1 fila actualizada.


1 fila actualizada.


CODIGO
------------------------------------------------------------------------------------------------------------------------
DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
PET
Paso 1

VIDRIO
Paso 2


2 filas seleccionadas.


Rollback terminado.


CODIGO
------------------------------------------------------------------------------------------------------------------------
DESCRIPCION
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
PET
Botellas de bebidas marcadas con el número 1.

VIDRIO
Botellas y frascos de vidrio sin tapa.


2 filas seleccionadas.
```
