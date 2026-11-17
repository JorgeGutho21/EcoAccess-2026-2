# 02 · SELECT y consultas básicas

## 1. Qué significa

`SELECT` es la sentencia que **lee** datos. No cambia nada: solo muestra.

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

Sirve para responder preguntas («¿qué materiales hay?», «¿qué canecas existen?»). Se usa siempre que quieras ver información. Es la sentencia más segura: puedes ejecutarla las veces que quieras.

## 5. Estructura

```sql
SELECT columna1, columna2 AS alias   -- qué columnas
FROM   tabla                         -- de qué tabla
WHERE  condición                     -- (opcional) qué filas: carpeta 03
ORDER  BY columna;                   -- (opcional) en qué orden
```

## 6. Ejemplo básico y 7. ejemplo de EcoAccess

Archivo: [`02_select_basico.sql`](02_select_basico.sql).

## 8. Explicación línea por línea

| Bloque | Línea | Qué hace |
|---|---|---|
| A | `SELECT * FROM rol;` | `*` significa «todas las columnas». Muestra los 3 roles. |
| B | `codigo AS codigo_material` | `AS` pone otro nombre (alias) a la columna en el resultado; no cambia la tabla. |
| B | `ORDER BY categoria, nombre` | Ordena primero por color de caneca y, dentro de cada color, alfabéticamente. |
| C | `CASE categoria WHEN 1 THEN … END` | Convierte un número en un texto entendible. |
| D | `SELECT DISTINCT …` | Quita las filas repetidas: muestra cada estado una sola vez. |
| E | `FETCH FIRST 5 ROWS ONLY` | Limita el resultado a 5 filas. |

## 9. Resultado esperado

- A: 3 filas (Ciudadano, Operario y Administrador).
- B y C: los materiales del catálogo ordenados.
- D: los estados 0, 1 y 2.
- E: hasta 5 canecas; con los datos actuales salen 4, porque solo hay 4 canecas registradas.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS/salida_02_select_basico.txt`.

## 10. Cómo ejecutarlo en SQL Developer

Abre el archivo, elige **EcoAccess BD LOCAL**, pon el cursor en una consulta y pulsa **Ctrl+Enter**. Para ejecutar todas, pulsa **F5**.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| ORA-00942 | La tabla no existe o estás conectado con otro usuario (por ejemplo, el lector solo ve vistas). |
| ORA-00904 | Columna mal escrita, por ejemplo `nombre_material` en vez de `nombre`. |
| ORA-00923 | Falta una coma o la palabra `FROM`. |

## 12. Preguntas del profesor

- ¿Qué diferencia hay entre `SELECT *` y nombrar las columnas? *Nombrarlas es más claro y no se rompe si la tabla cambia.*
- ¿El alias cambia la tabla? *No; solo el encabezado del resultado.*
- ¿Para qué sirve `DISTINCT`? *Para no repetir valores.*
