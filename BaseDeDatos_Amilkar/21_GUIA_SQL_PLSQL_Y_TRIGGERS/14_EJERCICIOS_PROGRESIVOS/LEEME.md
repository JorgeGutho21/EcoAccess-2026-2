# 14 · Ejercicios progresivos

Doce ejercicios que van de lo más sencillo (un SELECT) a lo más completo (un paquete). Todos son **solo lectura** sobre EcoAccess BD.

**Cómo practicar:**

1. Lee el enunciado.
2. Escríbelo tú en una hoja de trabajo nueva.
3. Ejecútalo con Ctrl+Enter (SQL) o F5 (PL/SQL).
4. Compara con [`14_soluciones.sql`](14_soluciones.sql).

| # | Tema | Enunciado | Pista | Repasa en |
|---|---|---|---|---|
| E01 | SELECT | Lista el código y la categoría de todos los materiales, ordenados por código. | `ORDER BY` | [02](../02_SELECT_Y_CONSULTAS_BASICAS/LEEME.md) |
| E02 | WHERE | Muestra las canecas en servicio (estado 1). | `WHERE estado = 1` | [02](../02_SELECT_Y_CONSULTAS_BASICAS/LEEME.md) |
| E03 | JOIN | Cada caneca con el nombre de su estación. | FK CONTENEDOR → PUNTO_INSTALACION | [03](../03_WHERE_JOINS_GROUP_BY_HAVING/LEEME.md) |
| E04 | GROUP BY | Cuántos depósitos hay en cada estado de verificación, con el nombre del estado. | `CASE` + `COUNT(*)` | [03](../03_WHERE_JOINS_GROUP_BY_HAVING/LEEME.md) |
| E05 | HAVING | Materiales declarados en más de 5 depósitos. | `HAVING COUNT(*) > 5` | [03](../03_WHERE_JOINS_GROUP_BY_HAVING/LEEME.md) |
| E06 | Cadena de FK | Los 5 ciudadanos con más depósitos, con seudónimo C-0000. | DEPOSITO → ORDEN_APERTURA → INTERACCION | [12](../12_USUARIOS_Y_RESIDUOS_DEPOSITADOS/GUIA_USUARIOS_Y_RESIDUOS.md) |
| E07 | Subconsulta | Canecas que nunca recibieron un depósito. | `NOT EXISTS` | [03](../03_WHERE_JOINS_GROUP_BY_HAVING/LEEME.md) |
| E08 | Bloque anónimo | Imprime el total de depósitos verificados. | `SELECT … INTO` + `DBMS_OUTPUT` | [04](../04_BLOQUES_ANONIMOS_Y_VARIABLES/LEEME.md) |
| E09 | IF/ELSIF | Clasifica los pendientes: «sin», «pocos» (≤ 10) o «muchos». | `IF … ELSIF … ELSE` | [04](../04_BLOQUES_ANONIMOS_Y_VARIABLES/LEEME.md) |
| E10 | Cursor | Recorre las estaciones activas e imprime cuántas canecas tiene cada una. | `FOR e IN (SELECT …) LOOP` | [05](../05_CURSORES/LEEME.md) |
| E11 | Función | Peso estimado de 10 unidades de los 5 primeros objetos del catálogo. | `fn_peso_estimado_kg` | [07](../07_FUNCIONES/LEEME.md) |
| E12 | Paquete | Compara el saldo del paquete con el de la vista para los 5 ciudadanos con más puntos. | `pkg_ecopuntos.saldo` | [08](../08_PAQUETES_PLSQL/LEEME.md) |

## Resultado esperado

- E01 a E07: tablas de resultados.
- E08 a E10: mensajes en la pestaña **Salida de script** (con `SET SERVEROUTPUT ON`).
- E12: la columna `control` dice **COINCIDE** en todas las filas.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## Retos sin solución (para practicar más)

1. Muestra el porcentaje de depósitos rechazados por material.
2. Escribe un bloque con cursor explícito (OPEN, FETCH, CLOSE) que imprima los 3 últimos depósitos pendientes.
3. En **ECOACCESS_PRACTICAS** (no en EcoAccess BD), crea un trigger que impida notas mayores que 5 y pruébalo con ROLLBACK. Puedes compararlo con TRG_VALIDAR_NOTA de [P03](../../05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P03_triggers_libres.sql).

Hay un banco mayor de ejercicios por tema en [`05_SQL_Y_PLSQL`](../../05_SQL_Y_PLSQL).
