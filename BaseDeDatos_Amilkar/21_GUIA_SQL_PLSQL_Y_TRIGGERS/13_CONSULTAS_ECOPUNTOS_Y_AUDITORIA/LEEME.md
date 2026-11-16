# 13 · Consultas de EcoPuntos y auditoría

## 1. Qué significa

- **EcoPuntos**: el incentivo que recibe un ciudadano por un depósito **verificado**. Funciona como un libro contable: cada crédito o reverso es una fila de MOVIMIENTO_ECOPUNTOS, y el saldo es la suma de esas filas.
- **Tarifa**: REGLA_ECOPUNTOS. Cada material tiene versiones con fecha desde y hasta. La vigente es la que tiene `hasta_utc` en NULL.
- **Auditoría**:
  - AUDITORIA_DEPOSITO guarda los cambios de verificación (disparador 3).
  - HISTORIAL_ESTADO_CONTENEDOR guarda los cambios de estado de las canecas (disparador 1).

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

Para responder ante el profesor o un auditor: ¿cuántos puntos tiene cada persona?, ¿por qué?, ¿quién verificó?, ¿se acreditó algo dos veces? El saldo **no se guarda** en una columna, para que nunca quede desactualizado: se calcula.

## 5. Estructura de las relaciones (FK reales)

```
MATERIAL ─FK_REGLA_MATERIAL─< REGLA_ECOPUNTOS ─FK_MOV_REGLA─< MOVIMIENTO_ECOPUNTOS >─FK_MOV_USUARIO─ USUARIO
                                                                     │  └─FK_MOV_REVERTIDO (reverso → crédito)
                                                                     └─FK_MOV_DEPOSITO─ DEPOSITO ─< AUDITORIA_DEPOSITO
CONTENEDOR ─FK_HIST_CONTENEDOR─< HISTORIAL_ESTADO_CONTENEDOR
```

`tipo` = 1 crédito (puntos positivos), 2 reverso (puntos negativos).

## 6. Ejemplo básico

Bloque A: lista las versiones de tarifa con un JOIN simple.

## 7. Ejemplo con EcoAccess

[`13_ecopuntos_y_auditoria.sql`](13_ecopuntos_y_auditoria.sql):

| Bloque | Pregunta que responde |
|---|---|
| A | ¿Qué tarifa tiene cada material y desde cuándo? |
| B | ¿Cuántos puntos tiene cada ciudadano? Usa la vista `VW_SALDO_ECOPUNTOS`, sin mostrar el nombre. |
| C | ¿Qué material genera más puntos? |
| D | ¿Algún depósito tiene dos créditos **vigentes** (sin revertir)? (control de idempotencia) |
| D2 | ¿Cómo queda un depósito corregido? Crédito, reverso y crédito nuevo. |
| E | ¿Hay créditos vigentes de depósitos que ya no están verificados? |
| F | ¿Cuántas verificaciones, rechazos, reaperturas y correcciones hubo? |
| G | ¿Quién hizo los últimos cambios? |
| H | ¿Qué canecas cambiaron de estado y por qué? |

Para los residuos **por usuario**, la cadena DEPOSITO → ORDEN_APERTURA → INTERACCION → USUARIO está en la [carpeta 12](../12_USUARIOS_Y_RESIDUOS_DEPOSITADOS/GUIA_USUARIOS_Y_RESIDUOS.md).

## 8. Explicación línea por línea (bloque E)

| Línea | Qué hace |
|---|---|
| `WHERE mv.tipo = 1` | Solo créditos. |
| `AND d.estado_verificacion <> 1` | Depósitos que hoy **no** están verificados (reabiertos o rechazados por una corrección). |
| `AND NOT EXISTS (… rv.id_movimiento_revertido = mv.id_movimiento)` | Y cuyo crédito **no** fue revertido. |
| resultado | Debe salir **0 filas**: cuando una corrección quita la verificación, `PKG_ECOPUNTOS.revertir` deja un reverso. |

## 9. Resultado esperado

- D y E: **0 filas** (son controles).
- D2: los depósitos con reverso muestran su crédito, el reverso (puntos negativos) y, si se volvió a verificar, un crédito nuevo con clave `CRED-<depósito>-2`. Por eso un conteo simple de créditos puede dar 2 para un depósito corregido: no es un error, porque el primer crédito quedó compensado.
- B: saldo = acreditados + revertidos (los revertidos son negativos).
- F: solo las operaciones VERIFICACION, RECHAZO, REAPERTURA y CORRECCION.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## 10. Cómo ejecutarlo

**F5** en la conexión EcoAccess BD LOCAL. Solo hay SELECT.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| ORA-00904 identificador no válido | Se buscó un nombre de columna que no existe, por ejemplo `deposito.id_usuario`. El depósito **no** tiene usuario directo. |
| Saldo duplicado | Se hizo JOIN de movimientos con auditoría sin agrupar antes: cada fila se repite. |
| ORA-01031 | La conexión de solo lectura (LECTOR) no tiene permiso sobre una tabla. Usa las vistas `VW_BI_*`. |

## 12. Preguntas del profesor

- ¿Por qué no hay columna saldo en USUARIO? *Porque sería un dato derivado que podría desincronizarse; se calcula sumando movimientos.*
- ¿Cómo se evita acreditar dos veces? *`PKG_ECOPUNTOS.acreditar` busca primero un crédito sin revertir del depósito y, si existe, devuelve ese mismo sin insertar otro. Además, `clave_idempotencia` (`CRED-<depósito>-<n>`) es única (UQ_MOV_CLAVE). Solo después de un reverso se puede crear `CRED-<depósito>-2`.*
- ¿Qué pasa si cambia la tarifa? *Se crea una versión nueva; los movimientos viejos siguen apuntando a la regla con la que se calcularon.*
- ¿Por qué un reverso y no un DELETE? *Para conservar la historia: nada se borra, todo se compensa.*
