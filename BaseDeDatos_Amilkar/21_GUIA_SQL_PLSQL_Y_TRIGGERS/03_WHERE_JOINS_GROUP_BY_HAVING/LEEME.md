# 03 · WHERE, JOIN, GROUP BY y HAVING

## 1. Qué significa

| Cláusula | Significado |
|---|---|
| `WHERE` | Filtra **filas** antes de agrupar. |
| `JOIN … ON` | Une dos tablas usando una **clave foránea**: la fila hija se junta con su fila padre. |
| `LEFT JOIN` | Igual que JOIN, pero conserva las filas de la izquierda aunque no tengan pareja. |
| `GROUP BY` | Junta filas con el mismo valor para contarlas, sumarlas o promediarlas. |
| `HAVING` | Filtra **grupos** después de agrupar. |

## 2. Para qué sirve y 3. por qué se usa

En un modelo normalizado los datos están repartidos en varias tablas. Por ejemplo, la caneca guarda solo el número de su estación, no su nombre. Para ver la información completa hay que **unir** tablas; para resumirla hay que **agrupar**.

## 4. Cuándo se usa

- `WHERE`: siempre que solo te interese una parte de las filas.
- `JOIN`: cuando la información está en dos o más tablas relacionadas por FK.
- `GROUP BY` / `HAVING`: para totales por categoría, usuario, material…

## 5. Estructura (orden de escritura y orden en que Oracle lo evalúa)

```sql
SELECT   columnas, COUNT(*)        -- 5.º se calcula lo que se muestra
FROM     tabla_a a                 -- 1.º de dónde
JOIN     tabla_b b ON b.pk = a.fk  -- 1.º unir por la FK
WHERE    condición_de_fila         -- 2.º filtrar filas
GROUP BY columnas                  -- 3.º agrupar
HAVING   condición_de_grupo        -- 4.º filtrar grupos
ORDER BY columnas;                 -- 6.º ordenar
```

## 6–7. Ejemplos

Archivo: [`03_where_join_group_by.sql`](03_where_join_group_by.sql).

## 8. Explicación línea por línea

| Bloque | Línea clave | Qué hace | FK usada |
|---|---|---|---|
| A | `WHERE estado = 1` | Solo canecas en servicio. | — |
| B | `IN (1) AND peso_medido_kg IS NULL` | Verificados que no se pesaron. **`= NULL` no funciona**: se escribe `IS NULL`. | — |
| C | `JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion` | Une cada caneca con su estación. | FK_CONTENEDOR_PUNTO |
| D | dos JOIN desde `contenedor_material` | La tabla puente N:M une canecas y materiales. | FK_CONT_MAT_CONTENEDOR, FK_CONT_MAT_MATERIAL |
| E | `LEFT JOIN deposito d ON d.id_material_declarado = m.id_material` | Conserva los materiales sin depósitos (cuentan 0). | FK_DEPOSITO_MAT_DECL |
| F | `GROUP BY estado_verificacion` | Un renglón por estado. | — |
| G | `HAVING COUNT(*) >= 8` | Filtra grupos: solo los materiales con 8 o más verificados. | FK_DEPOSITO_MAT_VERIF |

## 9. Resultado esperado

- F debe dar **PENDIENTE 17, VERIFICADO 76 y RECHAZADO 12**: 105 depósitos sintéticos en total.
- En E, `COUNT(d.id_deposito)` vale 0 para los materiales sin depósitos; si usaras `COUNT(*)`, contaría 1, y es un error típico.

Las salidas reales están en `../15_RESULTADOS_Y_CAPTURAS`.

## 10. Cómo ejecutarlo

**F5** para todo el archivo, o **Ctrl+Enter** sobre una consulta.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| ORA-00979 «no es una expresión GROUP BY» | Una columna del SELECT no está ni en el GROUP BY ni dentro de una función de grupo. |
| ORA-00934 «función de grupo no permitida» | Escribiste `COUNT(*)` en el WHERE; va en HAVING. |
| Resultado multiplicado | Faltó una condición en el JOIN (producto cartesiano). |
| ORA-00918 «columna definida de forma ambigua» | Dos tablas tienen una columna con el mismo nombre; usa el alias (`c.nombre`). |

## 12. Preguntas del profesor

- ¿Diferencia entre WHERE y HAVING? *WHERE filtra filas antes de agrupar; HAVING filtra grupos después.*
- ¿Diferencia entre JOIN y LEFT JOIN? *JOIN solo deja las filas con pareja; LEFT JOIN conserva todas las de la izquierda.*
- ¿Por qué `COUNT(columna)` y no `COUNT(*)` en un LEFT JOIN? *Porque COUNT(columna) ignora los NULL.*
