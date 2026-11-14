# Ejemplos 32, 33 y 34 (diapositivas de la exposición «PL/SQL y disparadores»)

- **Fecha de la clase:** por verificar.
- **Origen:** las diapositivas 32, 33 y 34 de la exposición de Juan José San Juan Fragozo, María del Mar Maestre Luna y Saray Marcela Cadena Palomino (`../FECHA_POR_VERIFICAR_PLSQL_HR_CURSORES_PROCEDIMIENTOS_FUNCIONES/EXPOSICION_PLSQL_Y_DISPARADORES.pdf`).

| Ejemplo | Disparador | Qué enseña |
|---|---|---|
| 32 | `trg_descontar_stock` (AFTER INSERT ON ventas) | Usar `:NEW.cantidad` para actualizar otra tabla: stock 50 → 45. |
| 33 | `trg_aud_salarios` (AFTER UPDATE OF salario) | Auditoría con `:OLD` y `:NEW` y un WHEN sin dos puntos. |
| 34 | `trg_validar_nota` (BEFORE INSERT OR UPDATE ON notas) | Rechazar una nota fuera de 0–5 con `RAISE_APPLICATION_ERROR(-20001)`. |

## Archivos de esta carpeta

Los siete scripts `PASO_1` … `PASO_7` y `LEEME.txt` se escribieron para reproducir los ejemplos de la exposición en orden: crear tablas y datos, crear el procedimiento de aumento, ejemplo 32, ejemplo 33, crear el trigger del ejemplo 34, nota inválida (falla a propósito con ORA-20001) y nota válida. Se conservan tal como se prepararon.

## Solución verificada en la entrega

Los tres disparadores están en `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P03_triggers_libres.sql`, en el esquema `ECOACCESS_PRACTICAS`, junto con `trg_limite_aumento`, que ilustra la versión BEFORE que modifica `:NEW`. `P99_pruebas_practicas.sql` los prueba (casos EJ32, EJ34, TL01 y TL02), y la salida real está en `16_EVIDENCIAS_Y_PRUEBAS/salidas/P99_practicas_20261111_noche.txt`.
