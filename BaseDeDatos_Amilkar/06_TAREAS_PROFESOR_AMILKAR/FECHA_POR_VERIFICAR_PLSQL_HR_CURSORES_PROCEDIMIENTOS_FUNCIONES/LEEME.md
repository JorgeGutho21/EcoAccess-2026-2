# PL/SQL sobre HR: bloque anónimo, cursores, procedimientos y funciones

- **Fecha de la clase:** por verificar. Solo se sabe que el material de la exposición se descargó el 09/11/2026.
- **Material:** `EXPOSICION_PLSQL_Y_DISPARADORES.pdf`, exposición de clase de Juan José San Juan Fragozo, María del Mar Maestre Luna y Saray Marcela Cadena Palomino. Sus módulos son: SQL a PL/SQL, bloque anónimo, variables, `%TYPE`, IF, FOR, cursores, funciones, procedimientos, paquetes, disparadores e integración final.

## Lo que pedía la práctica

1. Un **bloque anónimo** que recorra los empleados de un departamento de HR y los imprima.
2. Convertirlo en un **procedimiento** (`pr_empleados`) para reutilizarlo.
3. Una **función** que devuelva un `SYS_REFCURSOR` (`fn_empleados`) para que una aplicación lea las filas.
4. Consultar desde **C#** (o Java) la función y mostrar el resultado.

## Dónde está la solución verificada

| Punto | Archivo | Esquema |
|---|---|---|
| Bloque anónimo, cursor explícito, procedimiento y función con `SYS_REFCURSOR` | `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P04_hr_plsql.sql` | `ECOACCESS_PRACTICAS` (lee HR sin modificarlo) |
| Lectura desde C# | `10_CSHARP_VISUAL_STUDIO/SOLUCION_CSHARP/` | — |
| Ejercicios por tema (variables, IF, FOR, cursores, funciones, procedimientos, paquetes) | `05_SQL_Y_PLSQL/` (una carpeta por tema) | `ECOACCESS_BD` y `ECOACCESS_PRACTICAS` |

## Evidencia

- `16_EVIDENCIAS_Y_PRUEBAS/salidas/practicas_P00_P05.txt`: salida real de las prácticas.
- `16_EVIDENCIAS_Y_PRUEBAS/salidas/P99_practicas_20261111_noche.txt`: 19 pruebas automáticas de las prácticas, todas aprobadas.
