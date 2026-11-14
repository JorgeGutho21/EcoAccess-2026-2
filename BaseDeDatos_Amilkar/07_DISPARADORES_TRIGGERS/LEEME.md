# Disparadores (triggers)

La explicación de cada línea de código está en `GUIA_TRIGGERS_LINEA_POR_LINEA.pdf`. El código vive en un solo lugar para no duplicarlo; esta tabla dice dónde está cada disparador.

## Los cuatro disparadores del proyecto (esquema `ECOACCESS_BD`)

Código: `04_SCRIPTS_ORACLE/08_triggers.sql`. Demostración «antes → sentencia → después», con rechazos y ROLLBACK: `demo_cuatro_triggers.sql` (esta carpeta; F5 con la conexión ECOACCESS_BD).

| Disparador | Momento y evento | Qué hace |
|---|---|---|
| `TRG_CONTENEDOR_AUD_ESTADO` | AFTER UPDATE OF estado ON contenedor, FOR EACH ROW, WHEN (OLD.estado <> NEW.estado) | Guarda en HISTORIAL_ESTADO_CONTENEDOR el estado anterior y el nuevo, el actor y el motivo. |
| `TRG_DEPOSITO_COHERENCIA` ★ | BEFORE INSERT OR UPDATE ON deposito, FOR EACH ROW | Aplica las reglas que miran otras tablas: evento coherente, caneca que admite el material, peso en rango, verificador con rol y distinto del titular; además completa `:NEW.fecha_deteccion_utc`. |
| `TRG_DEPOSITO_AUD_VERIFICACION` ★ | AFTER UPDATE de las columnas de verificación, FOR EACH ROW | Guarda en AUDITORIA_DEPOSITO los valores `:OLD` y `:NEW` de cada verificación, rechazo, corrección o reapertura. |
| `TRG_DEPOSITO_BLOQUEO_CERRADO` | BEFORE UPDATE ON deposito, FOR EACH ROW, WHEN (OLD.estado_verificacion <> 0) | Impide editar un depósito cerrado, salvo con la corrección formal y un motivo. |

★ = los dos elegidos para el punto 1 de la tarea de disparadores.

## Disparadores de clase (esquema `ECOACCESS_PRACTICAS`)

Código: `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/`.

| Tema | Disparador | Archivo |
|---|---|---|
| Trigger con secuencia (PK subrogada) | `TRG_BD_EVENTO_ACADEMICO_ID`, BEFORE INSERT | `P01_secuencia_pk.sql` |
| Precio final | `TRG_PRODUCTO_PRECIO_FINAL`, BEFORE INSERT OR UPDATE | `P02_precio_final.sql` |
| Tema libre 1: auditoría salarial (`:OLD`/`:NEW`) | `TRG_AUD_SALARIOS`, AFTER UPDATE OF salario | `P03_triggers_libres.sql` |
| Tema libre 2: validación de nota | `TRG_VALIDAR_NOTA`, BEFORE INSERT OR UPDATE | `P03_triggers_libres.sql` |
| BEFORE que modifica `:NEW` (tope del 20 %) | `TRG_LIMITE_AUMENTO` | `P03_triggers_libres.sql` |
| Descontar stock (ejemplo 32) | `TRG_DESCONTAR_STOCK`, AFTER INSERT | `P03_triggers_libres.sql` |
| PK del CRUD de C# | `TRG_EMPLEADO_PRACTICA_ID` | `P05_crud_sin_delete.sql` |

## Conceptos y pruebas

| Concepto | Dónde verlo |
|---|---|
| BEFORE y AFTER; `:OLD` y `:NEW`; FOR EACH ROW; WHEN sin dos puntos | Guía línea por línea; `06_TAREAS_PROFESOR_AMILKAR/2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW/LEEME.md` |
| Pruebas positivas y negativas de los 4 del proyecto | `demo_cuatro_triggers.sql` → `16_EVIDENCIAS_Y_PRUEBAS/salidas/demo_cuatro_triggers.txt`; prueba T05 del instalador |
| Pruebas de los de clase | `P99_pruebas_practicas.sql` → `16_EVIDENCIAS_Y_PRUEBAS/salidas/P99_practicas_20261111_noche.txt` (19/19) |
| ROLLBACK y auditoría | Ningún trigger hace COMMIT: si la sentencia se deshace, su fila de auditoría también. Lo demuestra el final de la demostración. |
| Ejercicios de consulta sobre triggers | `05_SQL_Y_PLSQL/15_DISPARADORES_EJERCICIOS` |
