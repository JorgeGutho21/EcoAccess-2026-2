# 10 · Tarea de los 4 disparadores del profesor Amílkar

Esta carpeta **no copia** la tarea: la ordena para estudiarla y enlaza los archivos originales.

- Guía completa de la tarea: [`06_TAREAS_PROFESOR_AMILKAR/TAREA_DISPARADORES_CUATRO_PUNTOS/GUIA_PRACTICA_TRIGGERS_Y_ENTREGA.md`](../../06_TAREAS_PROFESOR_AMILKAR/TAREA_DISPARADORES_CUATRO_PUNTOS/GUIA_PRACTICA_TRIGGERS_Y_ENTREGA.md)
- Clase de :OLD y :NEW: [`06_TAREAS_PROFESOR_AMILKAR/2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW`](../../06_TAREAS_PROFESOR_AMILKAR/2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW)

## Los 4 puntos y dónde está cada uno

| Punto | Qué pide | Esquema | Archivo original |
|---|---|---|---|
| 1 | Dos triggers que pertenezcan al proyecto. | ECOACCESS_BD | [`08_triggers.sql`](../../04_SCRIPTS_ORACLE/08_triggers.sql): TRG_DEPOSITO_COHERENCIA y TRG_DEPOSITO_AUD_VERIFICACION. Explicados en la [carpeta 09](../09_TRIGGERS_ECOACCESS/LEEME.md). Demostración: [`demo_cuatro_triggers.sql`](../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql). |
| 2 | Un trigger que asigne automáticamente una clave única al insertar. | ECOACCESS_PRACTICAS | [`P01_secuencia_pk.sql`](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P01_secuencia_pk.sql): SEQ_BD_EVENTO_ACADEMICO + TRG_BD_EVENTO_ACADEMICO_ID. |
| 3 | Dos triggers de tema libre. | ECOACCESS_PRACTICAS | [`P03_triggers_libres.sql`](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P03_triggers_libres.sql): TRG_AUD_SALARIOS y TRG_VALIDAR_NOTA. |
| 4 | Actualizar automáticamente el precio final. | ECOACCESS_PRACTICAS | [`P02_precio_final.sql`](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P02_precio_final.sql): precio_final = precio_unitario × (1 + utilidad / 100). |
| Pruebas | PASA/FALLA de todo lo anterior | ECOACCESS_PRACTICAS | [`P99_pruebas_practicas.sql`](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P99_pruebas_practicas.sql) (termina en ROLLBACK). |

**¿Por qué dos esquemas?** EcoAccess no vende productos ni maneja salarios o notas. Esos ejemplos viven en ECOACCESS_PRACTICAS para no meter tablas ajenas en la base del proyecto.

## Los 10 conceptos que hay que saber explicar

| Concepto | Explicación corta | Dónde se ve |
|---|---|---|
| `BEFORE` | Corre antes de guardar; puede rechazar o asignar `:NEW`. | P01, P02, TRG_DEPOSITO_COHERENCIA |
| `AFTER` | Corre después de guardar; sirve para auditar. | TRG_AUD_SALARIOS, TRG_DEPOSITO_AUD_VERIFICACION |
| `INSERT` | El trigger se dispara al insertar; `:OLD` vale NULL. | P01 |
| `UPDATE [OF col]` | Se dispara al modificar (solo esas columnas si hay `OF`). | P02, TRG_AUD_SALARIOS |
| `FOR EACH ROW` | Una ejecución por fila afectada; sin él, una sola por sentencia. | Todos |
| `:OLD` | Valor anterior (solo lectura). | TRG_AUD_SALARIOS |
| `:NEW` | Valor nuevo; se asigna solo en BEFORE. | P01 (`:NEW.id_evento := …NEXTVAL`), P02 (`:NEW.precio_final := …`) |
| `RAISE_APPLICATION_ERROR` | Cancela la operación con un error propio (-20000 a -20999). | TRG_VALIDAR_NOTA (ORA-20001), TRG_DEPOSITO_COHERENCIA (ORA-20101…20109) |
| `NEXTVAL` | Pide el siguiente número de la secuencia; el número consumido no se devuelve aunque haya ROLLBACK. | P01 |
| `ROLLBACK` | Deshace los datos de la transacción. **No** deshace un `CREATE`. | Final de P99 y de demo_cuatro_triggers |

## Ejemplo básico (punto 2, resumido de P01)

```sql
CREATE SEQUENCE seq_bd_evento_academico START WITH 1 INCREMENT BY 1;

CREATE OR REPLACE TRIGGER trg_bd_evento_academico_id
BEFORE INSERT ON bd_evento_academico
FOR EACH ROW
BEGIN
    IF :NEW.id_evento IS NULL THEN
        :NEW.id_evento := seq_bd_evento_academico.NEXTVAL;   -- asigna la clave
    END IF;
END;
/
```

Línea por línea:

- `BEFORE INSERT` es obligatorio, porque solo antes de guardar se puede escribir `:NEW`.
- `IF … IS NULL` respeta el valor si alguien ya lo envió.
- `NEXTVAL` entrega 1, 2, 3…

## Ejemplo con EcoAccess (punto 1)

La explicación línea por línea de los dos disparadores del proyecto está en la [carpeta 09](../09_TRIGGERS_ECOACCESS/LEEME.md).

## Cómo ejecutarlo en SQL Developer

1. Para crear o probar las prácticas: conexión **ECOACCESS_PRACTICAS** → abrir P01, P02, P03 → **F5** cada uno → después P99 (**F5**), que muestra PASA/FALLA.
   - Advertencia: P01, P02 y P03 ejecutan `CREATE OR REPLACE` en el esquema de prácticas. Esos cambios de estructura no se deshacen con ROLLBACK. Nunca los ejecutes en ECOACCESS_BD.
2. Para solo **comprobar** que existen: [`10_consultar_practicas.sql`](10_consultar_practicas.sql) en ECOACCESS_PRACTICAS (**F5**, solo SELECT).

## Resultado esperado

- P99: todas las pruebas en PASA, por ejemplo 1000 → 1200 y 1500 → 1800.
- `10_consultar_practicas.sql`: el esquema actual es ECOACCESS_PRACTICAS y aparecen los disparadores y la secuencia de P01 a P03.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## Errores frecuentes

| Error | Causa |
|---|---|
| ORA-00942 | Se ejecutó en la conexión equivocada (las tablas de práctica no existen en ECOACCESS_BD). |
| ORA-04091 tabla mutante | El trigger de fila consulta la misma tabla que se está modificando. |
| ORA-04084 | Se asignó `:NEW` en un trigger AFTER. |
| Dos filas de auditoría por cambio | Hay dos triggers de auditoría de salario activos a la vez. |

## Preguntas del profesor

- ¿Por qué la clave se asigna en BEFORE? *Porque en AFTER ya no se puede escribir `:NEW`.*
- Si hago ROLLBACK, ¿se recupera el número de la secuencia? *No: la secuencia queda avanzada y puede haber huecos, que es normal.*
- ¿Qué pasa si actualizo solo el nombre del producto? *El precio no se recalcula, porque el trigger es `UPDATE OF precio_unitario, utilidad, precio_final`.*
