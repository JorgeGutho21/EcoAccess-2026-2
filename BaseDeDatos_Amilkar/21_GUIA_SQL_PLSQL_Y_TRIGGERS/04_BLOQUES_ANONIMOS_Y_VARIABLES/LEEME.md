# 04 · Bloques anónimos y variables

## 1. Qué significa

PL/SQL es el lenguaje de programación de Oracle: agrega a SQL **variables, decisiones (IF), ciclos y manejo de errores**. Un **bloque anónimo** es un pedazo de PL/SQL sin nombre: se ejecuta una vez y no queda guardado en la base.

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

Sirve para hacer varias cosas en orden y tomar decisiones con los datos: contar, comparar y mostrar un mensaje. Se usa para practicar y para pruebas rápidas. Es el paso previo a los procedimientos, que son bloques **con nombre** guardados en la base.

## 5. Estructura

```sql
DECLARE                 -- (opcional) aquí se declaran las variables
    v_nombre TIPO := valor_inicial;
BEGIN                   -- (obligatorio) aquí van las instrucciones
    instrucciones;
EXCEPTION               -- (opcional) qué hacer si ocurre un error
    WHEN nombre_error THEN instrucciones;
END;                    -- (obligatorio)
/                       -- en SQL Developer/SQL*Plus la barra ejecuta el bloque
```

## 6–7. Ejemplos

Archivo: [`04_bloques_y_variables.sql`](04_bloques_y_variables.sql).

## 8. Explicación línea por línea (bloque C de EcoAccess)

| Línea | Qué hace |
|---|---|
| `SET SERVEROUTPUT ON` | Activa la pantalla donde aparece `DBMS_OUTPUT.PUT_LINE`. Sin esto no se ve nada. |
| `v_estado deposito.estado_verificacion%TYPE := 1;` | Declara una variable del **mismo tipo** que la columna. Si la columna cambia de tipo, el código sigue funcionando. |
| `SELECT COUNT(*) INTO v_total FROM deposito;` | `INTO` guarda el resultado del SELECT en la variable. Debe devolver exactamente una fila. |
| `IF … ELSIF … ELSE … END IF;` | Decide qué mensaje mostrar. Se escribe `ELSIF`, sin la letra E de «ELSE IF». |
| `\|\|` | Concatena textos. |
| `/` | Le dice a SQL Developer que el bloque terminó y debe ejecutarlo. |

En el bloque D, `material%ROWTYPE` crea una variable con **todas** las columnas de MATERIAL, y `EXCEPTION WHEN NO_DATA_FOUND` atrapa el error de un `SELECT INTO` que no encuentra filas.

## 9. Resultado esperado (Salida de Script)

```text
Hola desde PL/SQL
Depósitos sintéticos · ejemplo de cálculo: 119
Depósitos registrados: 105
Depósitos verificados: 76
Hay depósitos pendientes o rechazados: 29
PET es el material … , categoría 1
No existe ese material: NO_DATA_FOUND se atrapa y el bloque termina bien.
```

Los números exactos de la base están en `../15_RESULTADOS_Y_CAPTURAS`.

## 10. Cómo ejecutarlo

Abre el archivo y pulsa **F5**. Los mensajes salen en **Salida de Script**.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| No aparece ningún mensaje | Falta `SET SERVEROUTPUT ON`, o se ejecutó con Ctrl+Enter sin abrir **Ver → Salida de DBMS**. |
| ORA-01403 (NO_DATA_FOUND) | El `SELECT INTO` no encontró filas. |
| ORA-01422 (TOO_MANY_ROWS) | El `SELECT INTO` devolvió más de una fila. |
| PLS-00103 | Falta un punto y coma, o se escribió `ELSE IF` en vez de `ELSIF`. |

## 12. Preguntas del profesor

- ¿Qué diferencia hay entre un bloque anónimo y un procedimiento? *El bloque no tiene nombre ni queda guardado.*
- ¿Para qué sirve `%TYPE`? *Para declarar la variable con el tipo exacto de una columna.*
- ¿Qué pasa si un `SELECT INTO` no encuentra filas? *Ocurre NO_DATA_FOUND (ORA-01403).*
