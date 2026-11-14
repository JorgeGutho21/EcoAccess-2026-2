# Tarea de disparadores (cuatro puntos)

- **Fecha de asignación:** por verificar. La tarea está vinculada al tema de la clase del 10/11/2026 (`../2026-11-10_CLASE_TRIGGERS_OLD_Y_NEW`). Ninguna fuente disponible indica la fecha exacta de asignación ni la de entrega, así que no se escribe ninguna.

## Enunciado (cuatro puntos)

1. Identificar **dos disparadores del proyecto final** y explicar qué hacen y cuándo se ejecutan.
2. Crear una **llave primaria subrogada** con una SEQUENCE y un trigger BEFORE INSERT.
3. Presentar **dos disparadores de tema libre**, con explicación y pruebas.
4. Crear un trigger que **calcule el precio final** cuando se inserta un producto o cambia el precio unitario, la utilidad o ambos. Ejemplo: precio 1.000 con 20 % de utilidad da 1.200; si el precio pasa a 1.500, el precio final debe pasar a 1.800.

## Solución y evidencia

| Punto | Solución | Esquema | Prueba y evidencia |
|---|---|---|---|
| 1 | `TRG_DEPOSITO_COHERENCIA` (BEFORE INSERT OR UPDATE) y `TRG_DEPOSITO_AUD_VERIFICACION` (AFTER UPDATE), los dos elegidos para exponer, más los otros dos del proyecto (`TRG_CONTENEDOR_AUD_ESTADO` y `TRG_DEPOSITO_BLOQUEO_CERRADO`) — `04_SCRIPTS_ORACLE/08_triggers.sql` | `ECOACCESS_BD` | `07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql` (antes → sentencia → después, rechazos y ROLLBACK); salida en `16_EVIDENCIAS_Y_PRUEBAS/salidas/demo_cuatro_triggers.txt` |
| 2 | `P01_secuencia_pk.sql`: `seq_bd_evento_academico` + `trg_bd_evento_academico_id` (BEFORE INSERT, FOR EACH ROW, WHEN `NEW.id_evento IS NULL`) | `ECOACCESS_PRACTICAS` | P99, caso SQ01; muestra además que un ROLLBACK deja huecos en la secuencia |
| 3 | `P03_triggers_libres.sql`: `trg_aud_salarios` (AFTER, auditoría con `:OLD`/`:NEW`) y `trg_validar_nota` (BEFORE, rechaza notas fuera de 0–5); incluye `trg_limite_aumento` y `trg_descontar_stock` | `ECOACCESS_PRACTICAS` | P99, casos TL01, TL02, EJ32 y EJ34 |
| 4 | `P02_precio_final.sql`: `trg_producto_precio_final` (BEFORE INSERT OR UPDATE OF precio_unitario, utilidad, precio_final) | `ECOACCESS_PRACTICAS` | P99, casos PR01–PR06: 1000 + 20 % = 1200; 1500 + 20 % = 1800; precio final escrito a mano se recalcula |

Los scripts de los puntos 2 a 4 están en `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/`. La explicación línea por línea de los ocho disparadores está en `07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA.pdf`.

**Resultado de la última verificación:** `INSTALAR_Y_PROBAR.ps1` ejecuta las pruebas P99. La salida guardada indica 19 pruebas aprobadas de 19, sin fallos.
