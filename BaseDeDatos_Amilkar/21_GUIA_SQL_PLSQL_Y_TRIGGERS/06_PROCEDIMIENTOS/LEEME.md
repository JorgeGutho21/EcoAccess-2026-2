# 06 · Procedimientos

## 1. Qué significa

Un **procedimiento** es un bloque PL/SQL **con nombre** que queda guardado en la base. Se llama cuantas veces se quiera y **realiza una operación**. Puede recibir datos (`IN`) y devolver datos (`OUT`), pero no tiene `RETURN` de valor; eso es lo que lo distingue de una función (carpeta 07).

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

- Para encerrar una operación con sus reglas: cambiar el estado de una caneca exige ser administrador, que el estado sea válido y que quede historial.
- Así, C#, el bot o SQL Developer llaman **un solo nombre** y no repiten las reglas.
- Se usa cuando una tarea tiene varios pasos o modifica datos con controles.

## 5. Estructura

```sql
CREATE OR REPLACE PROCEDURE nombre (
    p_entrada IN  tipo,
    p_salida  OUT tipo
) IS
    v_local tipo;            -- variables propias
BEGIN
    instrucciones;
EXCEPTION
    WHEN … THEN …;
END nombre;
/
-- Llamada:  BEGIN nombre(valor, :variable); END;   o   EXEC nombre(valor, :variable)
```

## 6. Ejemplo básico

Bloque A de [`06_procedimientos.sql`](06_procedimientos.sql): un procedimiento **local** `saludar`, declarado dentro de un bloque anónimo. Se practica la sintaxis sin crear nada en la base.

## 7. Los procedimientos reales de EcoAccess

| Procedimiento | Parámetros (tipo) | Qué hace | ¿Modifica datos? |
|---|---|---|---|
| `PR_CAMBIAR_ESTADO_CONTENEDOR` | `p_id_contenedor` (contenedor.id_contenedor%TYPE), `p_estado` (1, 2 o 3), `p_id_actor` (usuario), `p_motivo` (VARCHAR2) | Exige rol Administrador (`PKG_CONTEXTO_APP.exigir_rol(…, '3')`), valida el estado (ORA-20121), registra quién y por qué (`fijar_actor`) y hace el UPDATE. El **disparador 1** guarda el cambio en HISTORIAL_ESTADO_CONTENEDOR. | **Sí** |
| `PR_CONSULTAR_ESTADISTICAS` | `p_desde_col`, `p_hasta_col` (DATE, hora Colombia), `p_id_punto` (NUMBER o NULL), `p_categoria` (NUMBER o NULL), `p_resultado` **OUT SYS_REFCURSOR** | Llama a `PKG_ESTADISTICAS.consultar_resumen` y devuelve un cursor con el resumen del periodo. Rechaza periodos inválidos (ORA-20801) y estaciones o colores inexistentes (ORA-20802, ORA-20803). | No |

Los procedimientos de los paquetes (`PKG_DEPOSITO.verificar`, `PKG_ECOPUNTOS.acreditar`…) están en la carpeta 08.

## 8. Explicación línea por línea (bloque D)

| Línea | Qué hace |
|---|---|
| `VARIABLE rc REFCURSOR` | Crea en SQL Developer una variable capaz de recibir un cursor. Solo funciona con **F5**. |
| `pr_consultar_estadisticas(p_desde_col => TRUNC(SYSDATE) - 30, …` | Llamada con **parámetros por nombre** (`=>`): no importa el orden. |
| `p_id_punto => NULL` | NULL significa «sin filtro»: todas las estaciones. |
| `p_resultado => :rc` | El parámetro OUT se guarda en la variable `:rc` (los dos puntos indican variable externa). |
| `PRINT rc` | Muestra las filas del cursor. |

## 9. Resultado esperado

- B lista 2 procedimientos: `PR_CAMBIAR_ESTADO_CONTENEDOR` y `PR_CONSULTAR_ESTADISTICAS`, ambos `VALID`.
- C muestra los 5 parámetros, con `P_RESULTADO` como `OUT` y `REF CURSOR`.
- D muestra el resumen del periodo.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## 10. Cómo ejecutarlo

Abre el archivo y pulsa **F5**. No uses Ctrl+Enter para el bloque D: `VARIABLE` y `PRINT` son comandos de script. También puedes ver el código de un procedimiento en el panel **Conexiones → EcoAccess BD LOCAL → Procedimientos**, con doble clic.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| PLS-00306 «número o tipos de argumentos erróneos» | Faltó un parámetro o tiene otro tipo. |
| ORA-06550 | Error de compilación del bloque que llama (por ejemplo, el nombre mal escrito). |
| ORA-20120 | `exigir_rol`: el actor no tiene el rol pedido (por ejemplo, un operario intentando cambiar una caneca). |
| ORA-20801 | Periodo de estadísticas inválido o mayor de 400 días. |

## 12. Preguntas del profesor

- ¿Diferencia entre procedimiento y función? *El procedimiento realiza una operación; la función devuelve un valor con RETURN y se puede usar dentro de un SELECT.*
- ¿Qué significan IN y OUT? *IN entra al procedimiento; OUT sale de él hacia quien lo llama.*
- ¿Por qué el procedimiento no hace COMMIT? *Quien lo llama decide si confirma o deshace toda la transacción.*
- ¿Por qué `PR_CAMBIAR_ESTADO_CONTENEDOR` no inserta el historial él mismo? *Porque el disparador 1 lo hace siempre, se cambie el estado por donde se cambie.*
