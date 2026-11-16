# 07 · Funciones

## 1. Qué significa

Una **función** es un programa PL/SQL con nombre que **siempre devuelve un valor** con `RETURN`. Como devuelve un valor, se puede usar **dentro de un SELECT**, igual que `UPPER()` o `ROUND()`.

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

Sirve para calcular algo con reglas propias, por ejemplo el peso estimado según el catálogo. Así el cálculo se escribe una sola vez y todos lo usan igual. Se usa cuando el resultado es **un dato**: un número, un texto o un cursor.

## 5. Estructura

```sql
CREATE OR REPLACE FUNCTION nombre (p_dato IN tipo) RETURN tipo_resultado IS
    v_local tipo;
BEGIN
    …
    RETURN valor;            -- obligatorio
EXCEPTION
    WHEN NO_DATA_FOUND THEN RETURN NULL;
END nombre;
/
-- Uso:  SELECT nombre(columna) FROM tabla;
```

## 6. Ejemplo básico

Bloque A de [`07_funciones.sql`](07_funciones.sql): función local `precio_final` (la misma fórmula del trigger de precio final de la tarea del profesor).

## 7. Las funciones reales de EcoAccess

| Función | Parámetros | Devuelve | Reglas |
|---|---|---|---|
| `FN_PESO_ESTIMADO_KG` | `p_id_peso_referencial`, `p_unidades` | NUMBER (kg) | `gramos_tipico × unidades / 1000`, redondeado a 4 decimales. Devuelve **NULL** si falta el catálogo o si las unidades son NULL o menores que 1: no inventa pesos. |
| `FN_RESOLVER_CANECA_DISPONIBLE` | `p_id_punto`, `p_id_material` | id de la caneca | Comprueba que la estación exista y esté activa, que el material exista, esté habilitado y no sea de ruta especial, que la estación tenga una sola caneca de ese color en servicio y que esa caneca admita el material. Cada falla tiene su código: ORA-20810 a ORA-20819. |
| `FN_DEPOSITOS_CIUDADANO` | `p_id_usuario` | SYS_REFCURSOR | Abre un cursor sobre la vista `VW_DEPOSITO_DETALLE` con los depósitos de esa persona. |

Funciones dentro de paquetes (`PKG_ECOPUNTOS.saldo`, `PKG_ECOPUNTOS.regla_vigente`, `PKG_CONTEXTO_APP.tiene_rol` y `PKG_ESTADISTICAS.ranking_verificados`): carpeta 08.

## 8. Explicación línea por línea (bloque E)

| Línea | Qué hace |
|---|---|
| `fn_resolver_caneca_disponible(c.id_punto_instalacion, m.id_material)` | Llama a la función **una vez por cada fila** del SELECT. |
| `FROM contenedor_material cm JOIN contenedor c … JOIN material m` | Toma combinaciones que sí son válidas: la caneca admite ese material (FK_CONT_MAT_CONTENEDOR y FK_CONT_MAT_MATERIAL). |
| `WHERE c.estado = 1 AND p.activo = 1 AND m.habilitado = 1` | Evita combinaciones que la función rechazaría con un error: caneca fuera de servicio, estación inactiva o material deshabilitado. |

## 9. Resultado esperado

- C: el peso de 3 unidades es 3 × gramos_tipico / 1000.
- D: dos NULL.
- E: `caneca_resuelta` coincide con la caneca de la fila.
- F: los depósitos del ciudadano con más depósitos.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## 10. Cómo ejecutarlo

**F5** (por `VARIABLE` y `PRINT`). Para ver el código: **Conexiones → EcoAccess BD LOCAL → Funciones** → doble clic.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| ORA-14551 | Se llamó desde un SELECT a una función que modifica datos (INSERT o UPDATE). Las funciones de EcoAccess usadas en SELECT solo leen. |
| ORA-06503 «función devuelve sin valor» | Un camino del código terminó sin `RETURN`. |
| ORA-20814 / ORA-20816 / ORA-20817 | La estación no tiene caneca de ese color, la caneca está fuera de servicio o no admite el material. |

## 12. Preguntas del profesor

- ¿Por qué una función se puede usar en un SELECT y un procedimiento no? *Porque la función devuelve un valor.*
- ¿Qué pasa si `fn_peso_estimado_kg` no encuentra el catálogo? *Devuelve NULL: no se inventa un peso.*
- ¿Qué es un SYS_REFCURSOR? *Un cursor que una función o procedimiento entrega a quien lo llama (C#, Power BI o SQL Developer).*
