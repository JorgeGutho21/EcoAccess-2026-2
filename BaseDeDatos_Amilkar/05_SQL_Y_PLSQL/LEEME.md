# SQL y PL/SQL

## `00_PRACTICAS_VERIFICADAS` (esquema `ECOACCESS_PRACTICAS`)

Son las prácticas de clase, listas para ejecutar en orden. `P99_pruebas_practicas.sql` las prueba todas: 19 pruebas, 19 aprobadas en la última ejecución.

| Archivo | Tema |
|---|---|
| `P00_tablas_de_clase.sql` | Tablas de los ejemplos de clase y copias de HR (HR no se modifica). |
| `P01_secuencia_pk.sql` | SEQUENCE + trigger BEFORE INSERT para la PK subrogada; huecos tras un ROLLBACK. |
| `P02_precio_final.sql` | Trigger del precio final (1000 + 20 % = 1200; 1500 + 20 % = 1800). |
| `P03_triggers_libres.sql` | Auditoría salarial AFTER, tope del 20 % con BEFORE, validación de nota y descuento de stock. |
| `P04_hr_plsql.sql` | Bloque anónimo, cursores, procedimiento y función con SYS_REFCURSOR sobre HR. |
| `P05_crud_sin_delete.sql` | CRUD de empleado sin eliminación (lo usa la aplicación C#). |
| `P99_pruebas_practicas.sql` | Pruebas automáticas de todas las prácticas. |

## Ejercicios por tema

Cada carpeta tiene `ejercicios.sql`, con el código, el objetivo y la explicación de cada ejercicio, y `RESULTADOS.md`, con el estado y la salida real de Oracle. Salen del banco verificado de 309 ejercicios de `08_CONSULTAS_Y_EJERCICIOS` y se regeneran con `pwsh -File 05_SQL_Y_PLSQL\generar_carpetas_por_tema.ps1`.

| Carpeta | Ejercicios | Contenido |
|---|---:|---|
| `01_FUNDAMENTOS_SQL` | 57 | Conexión y diccionario de Oracle, funciones de una fila, fechas en hora de Colombia, vistas y seguridad. |
| `02_SELECT_WHERE_ORDER_BY` | 51 | SELECT, filtros con WHERE, ordenar y limitar. |
| `03_JOIN` | 23 | Uniones internas y externas, y uniones de varias tablas. |
| `04_GROUP_BY_HAVING_Y_ANALITICAS` | 37 | Agrupar, HAVING y funciones analíticas (RANK, LAG, acumulados). |
| `05_SUBCONSULTAS_Y_CONJUNTOS` | 32 | Subconsultas, EXISTS, UNION, INTERSECT y MINUS. |
| `06_BLOQUES_ANONIMOS_Y_CICLOS` | 11 | Bloques anónimos, LOOP, WHILE y FOR. |
| `07_VARIABLES_Y_CONDICIONALES` | 7 | Variables, `%TYPE`, `%ROWTYPE`, IF y CASE. |
| `08_CURSORES` | 14 | Cursores implícitos y explícitos, FOR de cursor y atributos. |
| `09_PROCEDIMIENTOS` | 2 | Procedimientos con parámetros. |
| `10_FUNCIONES` | 3 | Funciones y su uso en SELECT. |
| `11_SYS_REFCURSOR` | 2 | Procedimientos y funciones que devuelven un cursor. |
| `12_PAQUETES` | 9 | Uso de los paquetes del proyecto. |
| `13_TRANSACCIONES_Y_DML` | 15 | INSERT, UPDATE y DELETE con ROLLBACK, SAVEPOINT y errores esperados. |
| `14_EXCEPCIONES` | 14 | Excepciones predefinidas, propias y `RAISE_APPLICATION_ERROR`. |
| `15_DISPARADORES_EJERCICIOS` | 12 | Consultas sobre los disparadores y su efecto. |
| `16_ESQUEMA_HR_PRACTICAS` | 20 | Prácticas sobre HR (esquema `ECOACCESS_PRACTICAS`). |
| **Total** | **309** | 299 correctos y 10 con error intencional (marcados «error esperado»). |

La guía teórica de cada tema está en `11_GUIAS_PASO_A_PASO/GUIA_SQL_Y_PLSQL.pdf`, y el orden recomendado para estudiarlos, en `00_EMPEZAR_AQUI/ORDEN_PARA_ESTUDIAR.md`.
