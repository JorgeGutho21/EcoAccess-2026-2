# 05 · Cursores

## 1. Qué significa

Un **cursor** es un puntero que recorre, **una por una**, las filas que devuelve un SELECT. Recuerda que `SELECT INTO` solo admite una fila; con un cursor se procesan muchas.

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

Sirve para hacer algo con cada fila: mostrarla, sumarla o validarla. Se usa cuando la lógica no cabe en un solo SQL, o para practicar el recorrido de resultados. En EcoAccess los paquetes devuelven cursores (`SYS_REFCURSOR`) a C# y a SQL Developer (carpeta 08).

## 5. Estructura

```sql
DECLARE
    CURSOR c_nombre (p_param TIPO) IS SELECT …;   -- 1. declarar
    v_fila c_nombre%ROWTYPE;
BEGIN
    OPEN c_nombre(valor);                          -- 2. abrir
    LOOP
        FETCH c_nombre INTO v_fila;                -- 3. traer la siguiente fila
        EXIT WHEN c_nombre%NOTFOUND;               -- 4. salir cuando no hay más
        …                                          -- 5. usar v_fila
    END LOOP;
    CLOSE c_nombre;                                -- 6. cerrar
END;
```

Atajo: `FOR r IN c_nombre LOOP … END LOOP;` abre, trae y cierra solo.

## 6–7. Ejemplos

Archivo: [`05_cursores.sql`](05_cursores.sql). A: ciclo FOR sobre un SELECT. B: cursor explícito con OPEN, FETCH y CLOSE. C: cursor con parámetro sobre los depósitos pendientes.

## 8. Explicación línea por línea (bloque B)

| Línea | Qué hace |
|---|---|
| `CURSOR c_canecas IS SELECT … WHERE c.estado = 1` | Define qué filas recorrerá: las canecas en servicio con su estación (FK_CONTENEDOR_PUNTO). |
| `v_fila c_canecas%ROWTYPE;` | Variable con la misma forma que una fila del cursor. |
| `OPEN c_canecas;` | Ejecuta el SELECT y deja el puntero antes de la primera fila. |
| `FETCH c_canecas INTO v_fila;` | Avanza una fila y la copia en `v_fila`. |
| `EXIT WHEN c_canecas%NOTFOUND;` | Si el último FETCH no trajo nada, sale del ciclo. |
| `c_canecas%ROWCOUNT` | Cuántas filas se han traído hasta ese momento. |
| `CLOSE c_canecas;` | Libera el cursor. |

## 9. Resultado esperado

- A lista los materiales orgánicos.
- B muestra las canecas en servicio y el total (4 con los datos actuales).
- C lista los depósitos pendientes y su total: debe coincidir con el número de PENDIENTE de la carpeta 03.

## 10. Cómo ejecutarlo

**F5**. La salida aparece en **Salida de Script**.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| ORA-01001 «cursor no válido» | FETCH después de CLOSE, o sin OPEN. |
| ORA-06511 «cursor ya abierto» | Se abrió dos veces. |
| Ciclo infinito | Faltó `EXIT WHEN …%NOTFOUND`. |

## 12. Preguntas del profesor

- ¿Diferencia entre cursor implícito y explícito? *El implícito lo maneja Oracle (`FOR r IN (SELECT …)`); el explícito se declara, abre, trae y cierra a mano.*
- ¿Qué devuelve `%NOTFOUND`? *TRUE cuando el último FETCH no trajo fila.*
- ¿Qué es un `SYS_REFCURSOR`? *Un cursor que se puede devolver desde una función o procedimiento; lo usan C# y Power BI.*
