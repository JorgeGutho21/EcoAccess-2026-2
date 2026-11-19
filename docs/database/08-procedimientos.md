# 08 · Procedimientos independientes

Un **procedimiento** es un bloque PL/SQL con nombre que **hace algo**: cambia datos o abre un cursor. No devuelve un valor como una función; lo que entrega lo hace con parámetros `OUT`. Se ejecuta con `EXEC nombre(...)` o dentro de un `BEGIN … END;`.

- **En SQL Developer:** conexión → *Procedimientos* (Procedures).
- **En GitHub:** [`database/schema/procedures/`](../../database/schema/procedures/).
- **Estado verificado el 18/11/2026:** los dos son `VALID`.

## PR_CAMBIAR_ESTADO_CONTENEDOR

**Para qué sirve:** poner una caneca en servicio, sacarla de servicio o darla de baja, dejando constancia de quién lo hizo y por qué.

**Cuándo se ejecuta:** cuando un supervisor (rol Administrador) detecta una caneca dañada o la repara.
**Quién puede:** solo un usuario **activo con rol 3, Administrador**. Lo exige `PKG_CONTEXTO_APP.exigir_rol(p_id_actor, '3')`.

| Parámetro | Tipo | Significado |
|---|---|---|
| `p_id_contenedor` | `contenedor.id_contenedor%TYPE` | caneca |
| `p_estado` | `contenedor.estado%TYPE` | nuevo estado |
| `p_id_actor` | `usuario.id_usuario%TYPE` | administrador que hace el cambio |
| `p_motivo` | `VARCHAR2` | por qué |

**Estados válidos**, según el `CHECK CK_CONTENEDOR_ESTADO` y el propio procedimiento:
- **1** en servicio;
- **2** fuera de servicio;
- **3** dada de baja.

Cualquier otro valor da **−20121**. El procedimiento **no restringe las transiciones**: permite, por ejemplo, pasar de 3 a 1. Si el proyecto necesita que una baja sea definitiva, falta esa regla (ver [hallazgos](16-hallazgos.md)).

**Qué hace, en orden:**
1. `exigir_rol` (−20120 si no es administrador).
2. Valida el estado (−20121).
3. `fijar_actor`, para que el disparador sepa quién fue.
4. `UPDATE contenedor SET estado = …`. Si no existe la caneca, −20121.
5. `limpiar`.

Si algo falla, limpia el contexto y vuelve a lanzar el error. **No hace `COMMIT`.**

**Efecto adicional:** el `UPDATE` dispara `TRG_CONTENEDOR_AUD_ESTADO`, que guarda la fila en `HISTORIAL_ESTADO_CONTENEDOR` solo si el estado cambió de verdad.

**Tablas:** modifica `CONTENEDOR` e, indirectamente, `HISTORIAL_ESTADO_CONTENEDOR`; lee `USUARIO`, `USUARIO_ROL` y `ROL`.

```sql
-- En el esquema aislado:
EXEC pr_cambiar_estado_contenedor(:caneca, 2, :administrador, 'Servo dañado');
SELECT estado_anterior, estado_nuevo, id_actor, motivo FROM historial_estado_contenedor WHERE id_contenedor = :caneca;
ROLLBACK;
```

**Cómo comprobarlo:**
- `SELECT estado FROM contenedor WHERE id_contenedor = :caneca` → 2.
- `HISTORIAL_ESTADO_CONTENEDOR` tiene una fila nueva con el actor y el motivo.

**Operación inválida:**

| Caso | Error |
|---|---|
| Un operario o ciudadano | −20120 |
| Estado 9 | −20121 |
| Caneca inexistente | −20121 |

En los tres casos no se modifica nada: lo comprueban las pruebas E7.1, E7.2 y F.

## PR_CONSULTAR_ESTADISTICAS

**Para qué sirve:** devolver el resumen de depósitos de un periodo. Es la «puerta» estable que se creó en V022; por dentro llama a `PKG_ECOACCESS_STATS.consultar_resumen`.

**Quién puede:** cualquiera con permiso `EXECUTE`. Se ejecuta con los derechos del dueño (`AUTHID DEFINER`), así que no hace falta dar permiso sobre las tablas.

| Parámetro | Tipo | Significado |
|---|---|---|
| `p_desde_utc`, `p_hasta_utc` | `TIMESTAMP` | periodo en **UTC**, con un máximo de 400 días |
| `p_id_punto` | `NUMBER` | estación, o NULL para todas |
| `p_categoria` | `NUMBER` | categoría **del material** (1–4), o NULL |
| `p_resultado` | `OUT SYS_REFCURSOR` | resultado |

**Qué devuelve** (mecanismo: un `SYS_REFCURSOR` con una fila):
- `depositos_periodo`, `depositos_verificados`, `depositos_sin_sensor`, `ciudadanos_distintos`;
- `unidades_verificadas`, `verificados_sin_conteo`;
- `con_peso_medido`, `kg_medidos`.

**Validaciones:**

| Caso | Error |
|---|---|
| Periodo nulo, invertido o de más de 400 días | −20801 |
| Estación ≤ 0 o con decimales | −20802 |
| Categoría fuera de 1–4 | −20803 |

**Tablas:** lee `DEPOSITO`, `ORDEN_APERTURA`, `INTERACCION`, `CONTENEDOR` y `MATERIAL`. No modifica nada.

```sql
VARIABLE rc REFCURSOR
EXEC pr_consultar_estadisticas(SYS_EXTRACT_UTC(SYSTIMESTAMP) - 30, SYS_EXTRACT_UTC(SYSTIMESTAMP), NULL, NULL, :rc)
PRINT rc
```

**Cómo comprobarlo:** la prueba E8.9 compara `depositos_periodo` con un `COUNT(*)` directo sobre `DEPOSITO` en el mismo periodo, y coinciden.
