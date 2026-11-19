# 07 · Paquetes PL/SQL

Un **paquete** agrupa procedimientos y funciones de un mismo tema. Tiene dos partes:
- **Especificación** (`PACKAGE`): lo público, es decir los nombres, parámetros y tipos que otros pueden llamar. Es el «contrato».
- **Cuerpo** (`PACKAGE BODY`): cómo está hecho por dentro. Puede tener piezas privadas que nadie más ve, como `PKG_ECOPUNTOS.preparar`, y variables que duran toda la sesión, como `PKG_CONTEXTO_APP.g_actor`.

Si se cambia el cuerpo sin cambiar la especificación, los objetos que usan el paquete no se invalidan.

- **En SQL Developer:** conexión → *Paquetes* (Packages). Al expandir un paquete se ven la especificación y el *Cuerpo*.
- **En GitHub:** [`database/schema/packages/`](../../database/schema/packages/), con `*.pks.sql` para la especificación y `*.pkb.sql` para el cuerpo.
- **Estado verificado el 18/11/2026:** los cinco paquetes, especificación y cuerpo, son `VALID` y no tienen errores en `USER_ERRORS`.

**Transacciones:** ningún paquete hace `COMMIT` ni `ROLLBACK`. Quien los llama decide cuándo confirmar: la aplicación, un bloque de SQL Developer o una prueba. Por eso `PKG_DEPOSITO.verificar` y `PKG_ECOPUNTOS.acreditar` ocurren **en la misma transacción**: o se guardan los dos, o ninguno. Si un paso falla, se lanza el error y el que llamó hace `ROLLBACK`.

**Códigos de error** (`RAISE_APPLICATION_ERROR`):

| Código | Significado |
|---|---|
| −20100 | El actor no existe o está inactivo |
| −20101 | Nadie verifica ni acredita su propio depósito |
| −20103 | Depósito verificado bloqueado (lo lanza un disparador) |
| −20110 | El depósito no existe |
| −20111 | El depósito ya está cerrado, o sigue pendiente cuando se pide corregir |
| −20112 | Falta el motivo |
| −20113 | Estado de corrección inválido. **Ojo:** `TR_ECOP_UNIDADES` usa el mismo código para otra cosa (ver [hallazgos](16-hallazgos.md)) |
| −20120 | El usuario no tiene el rol requerido o está inactivo |
| −20201 a −20208 | Reglas de ecopuntos |
| −20801 a −20803 | Parámetros de estadísticas |

---

## PKG_CONTEXTO_APP — quién está actuando

**Para qué sirve:** la aplicación se conecta a Oracle siempre con la misma cuenta (`ECOACCESS_P3_APP`). Para que la auditoría sepa **qué persona** de EcoAccess hizo un cambio, este paquete guarda el actor en una variable de sesión. Los disparadores la leen al escribir la historia.

| Elemento | Tipo | Parámetros | Qué hace |
|---|---|---|---|
| `fijar_actor` | procedimiento | `p_id_usuario IN usuario.id_usuario%TYPE`, `p_motivo IN VARCHAR2 DEFAULT NULL` | Comprueba que el usuario exista y esté activo (si no, −20100) y guarda actor y motivo (máx. 300 caracteres). También marca la sesión con `DBMS_SESSION.SET_IDENTIFIER('ECO_U' \|\| id)`. |
| `limpiar` | procedimiento | — | Borra actor, motivo e identificador de sesión. |
| `actor` | función → `NUMBER` | — | Devuelve el actor guardado; NULL si no hay. |
| `motivo` | función → `VARCHAR2` | — | Devuelve el motivo guardado. |
| `tiene_rol` | función → `NUMBER` (1/0) | `p_id_usuario`, `p_codigo IN rol.codigo%TYPE` | 1 si el usuario está **activo** y tiene ese rol. |
| `exigir_rol` | procedimiento | `p_id_usuario`, `p_codigos IN VARCHAR2` (p. ej. `'2,3'`) | Lanza −20120 si el usuario no tiene ninguno de esos roles o está inactivo. |

**Tablas que consulta:** `USUARIO`, `USUARIO_ROL`, `ROL`. No modifica tablas.
**Lo usan:** `PKG_DEPOSITO`, `PKG_ECOPUNTOS`, `PR_CAMBIAR_ESTADO_CONTENEDOR`, `TRG_CONTENEDOR_AUD_ESTADO`, `TRG_DEPOSITO_AUD_VERIFICACION` y `TRG_DEPOSITO_COHERENCIA`.

```sql
SELECT pkg_contexto_app.tiene_rol(:id_usuario, 3) AS es_administrador FROM dual;   -- 1 o 0
```

## PKG_DEPOSITO — verificar, rechazar y corregir depósitos

**Para qué sirve:** es la única puerta para cerrar un depósito.
- **Verificar:** un operario confirma el material y cuenta las unidades. El mismo paso acredita los ecopuntos.
- **Rechazar:** el depósito no corresponde.
- **Corregir:** solo un administrador, con motivo.

| Elemento | Tipo | Parámetros | Qué hace |
|---|---|---|---|
| `estado` | función → `NUMBER` | `p_id_deposito` | **1** si tiene material verificado; **2** si lo último en la auditoría es `RECHAZO`; **0** pendiente. Lanza −20110 si no existe. Es la regla que usan las vistas. |
| `verificar` | procedimiento | `p_id_deposito`, `p_id_verificador`, `p_id_material`, `p_cantidad`, `p_peso_medido_kg DEFAULT NULL`, `p_motivo DEFAULT NULL` | Ver los pasos debajo de la tabla. |
| `rechazar` | procedimiento | `p_id_deposito`, `p_id_verificador`, `p_motivo` | Exige motivo (−20112), rol 2 o 3 y que el depósito esté pendiente. Inserta un `RECHAZO` en `AUDITORIA_DEPOSITO`; el estado pasa a 2. |
| `corregir_verificacion` | procedimiento | `p_id_deposito`, `p_id_actor`, `p_nuevo_estado` (0 reabrir, 1 verificado con otros datos), `p_id_material`, `p_cantidad`, `p_motivo` | Ver los pasos debajo de la tabla. |
| `correccion_en_curso` | función → `BOOLEAN` | — | TRUE solo mientras `corregir_verificacion` hace su `UPDATE`. La consulta `TRG_DEPOSITO_BLOQUEO_CERRADO` para permitir ese único cambio. |

Pasos de **`verificar`**, en este orden:
1. Exige que el verificador sea operario o administrador (`exigir_rol '2,3'`).
2. Bloquea la fila con `SELECT … FOR UPDATE WAIT 10` y exige que esté pendiente (−20111).
3. Prohíbe que el verificador sea el titular (−20101).
4. Fija el actor y actualiza `DEPOSITO`: material, cantidad, verificador, fecha y peso.
5. Si el depósito tiene titular, llama a `PKG_ECOPUNTOS.acreditar` **en la misma transacción**.

Pasos de **`corregir_verificacion`**:
1. Exige motivo (−20112), estado 0 o 1 (−20113) y rol administrador.
2. Si el depósito está pendiente, rechaza la corrección (−20111): un pendiente se verifica o se rechaza, no se corrige.
3. Si se reabre y había crédito, revierte los ecopuntos.
4. Activa `correccion_en_curso`, actualiza el depósito y registra la `REAPERTURA`.
5. Si queda verificado con otros datos, llama a `PKG_ECOPUNTOS.recalcular`.

**Tablas:**
- Lee `DEPOSITO`, `ORDEN_APERTURA`, `INTERACCION` y `AUDITORIA_DEPOSITO`.
- Modifica `DEPOSITO` e inserta en `AUDITORIA_DEPOSITO`.
- Indirectamente, a través de `PKG_ECOPUNTOS`, escribe en `OPERACION_ECOPUNTOS` y `MOVIMIENTO_ECOPUNTOS`.

**Invoca:** `PKG_CONTEXTO_APP` y `PKG_ECOPUNTOS`.

**Concurrencia:** `FOR UPDATE WAIT 10` impide que dos operarios verifiquen el mismo depósito a la vez. El segundo espera hasta 10 s y luego recibe un error.

```sql
-- Solo en el esquema aislado o dentro de una transacción de prueba:
BEGIN
    pkg_deposito.verificar(p_id_deposito => :dep, p_id_verificador => :operario, p_id_material => :material, p_cantidad => 2);
END;
/
SELECT pkg_deposito.estado(:dep) FROM dual;   -- 1
ROLLBACK;
```

## PKG_ECOPUNTOS — el libro de ecopuntos

**Para qué sirve:** acreditar, revertir y recalcular ecopuntos con las mismas reglas que la aplicación C#, en el **mismo libro** (`OPERACION_ECOPUNTOS` + `MOVIMIENTO_ECOPUNTOS`). La explicación completa, con la fórmula, está en [12-ecopuntos.md](12-ecopuntos.md).

| Elemento | Tipo | Parámetros | Qué hace |
|---|---|---|---|
| `regla_vigente` | función → `id_regla` | `p_id_material`, `p_fecha_utc` | Tarifa vigente del material en esa fecha. NULL si no hay; −20203 si hubiera dos a la vez. |
| `credito_vigente` | función → `id_movimiento` | `p_id_deposito` | Crédito del depósito que todavía no fue revertido, o NULL. |
| `acreditar` | procedimiento | `p_id_deposito`, `p_id_actor`, `p_id_movimiento OUT` | Ver los pasos debajo de la tabla. |
| `revertir` | procedimiento | `p_id_deposito`, `p_id_actor`, `p_motivo` | Solo administrador y con motivo. Asienta un reverso con los puntos en negativo. |
| `recalcular` | procedimiento | `p_id_deposito`, `p_id_actor`, `p_motivo` | Corrección: revierte el crédito anterior y acredita con la tarifa vigente del material verificado actual. |
| `saldo` | función → `NUMBER` | `p_id_usuario` | `SUM(puntos)` del libro; 0 si no tiene movimientos. |

Pasos de **`acreditar`**:
1. Solo acredita depósitos **verificados** (−20201).
2. La persona que acredita debe ser distinta del titular y del verificador (−20101).
3. Si ya existe un crédito vigente, devuelve ese mismo sin crear otro: es **idempotente**.
4. Si la tarifa vigente no existe, no acredita y el saldo no cambia.
5. Si existe, crea la operación `BD-CRED-<depósito>` y el movimiento de tipo 1.

Piezas **privadas** del cuerpo:
- `preparar`: valida y bloquea, en el mismo orden que C# (material → actor → depósito) para no causar bloqueos mutuos.
- `calcular`: aplica la tarifa, por depósito o por unidad, con un tope de 1 000 000.
- `operacion` y `movimiento`: hacen los `INSERT`.
- `ahora`: devuelve la hora UTC.

**Tablas:**
- Lee `DEPOSITO`, `MATERIAL`, `USUARIO`, `ORDEN_APERTURA`, `INTERACCION` y `REGLA_ECOPUNTOS`.
- Inserta en `OPERACION_ECOPUNTOS` y `MOVIMIENTO_ECOPUNTOS`; **nunca actualiza ni borra** movimientos.

```sql
SELECT pkg_ecopuntos.saldo(:id_usuario) AS saldo FROM dual;     -- igual a VW_SALDO_ECOPUNTOS.SALDO
```

## PKG_ESTADISTICAS — resúmenes por periodo

**Para qué sirve:** dar cifras de un periodo con fechas en hora de Colombia. Valida el periodo: positivo y de 400 días como máximo.

| Elemento | Tipo | Parámetros | Devuelve |
|---|---|---|---|
| `a_utc` | función → `TIMESTAMP` | `p_fecha_col IN DATE` | La fecha de Colombia convertida a UTC (`FROM_TZ … 'America/Bogota'`). |
| `consultar_resumen` | procedimiento | `p_desde_col`, `p_hasta_col` (`DATE`), `p_id_punto` (estación o NULL), `p_categoria` (1–4 o NULL), `p_resultado OUT SYS_REFCURSOR` | Un cursor con **una fila**: `depositos`, `verificados`, `pendientes_o_rechazados`, `sin_sensor`, `ciudadanos`, `unidades_verificadas`, `kg_estimados_verificados`, `con_peso_medido`, `kg_medidos`. Errores: −20801 periodo, −20802 estación, −20803 categoría. |
| `ranking_verificados` | función → `SYS_REFCURSOR` | `p_desde_col`, `p_hasta_col`, `p_top DEFAULT 10` | Ciudadanos con más depósitos verificados: `id_usuario`, `nombre`, `depositos_verificados`, `unidades`, `kg_estimados`, `kg_medidos`, `puesto`. Usa `DENSE_RANK`: «top 10» son **10 puestos**; con empates salen más de 10 filas (verificado: el top 5 del último año devolvió 16 filas). |

**Tablas que consulta:** `DEPOSITO`, `ORDEN_APERTURA`, `INTERACCION`, `CONTENEDOR`, `CONFIGURACION_CANECA` y `USUARIO`. No modifica nada.

```sql
VARIABLE rc REFCURSOR
EXEC pkg_estadisticas.consultar_resumen(SYSDATE - 30, SYSDATE + 1, NULL, NULL, :rc)
PRINT rc
```

## PKG_ECOACCESS_STATS (V022) — estadísticas en UTC

No aparece en la captura, pero existe. Es el paquete original de estadísticas: `consultar_resumen` recibe fechas **UTC** (`TIMESTAMP`) y filtra la categoría **del material**. `PKG_ESTADISTICAS` en cambio filtra por la categoría **de la caneca**. Lo envuelve `PR_CONSULTAR_ESTADISTICAS` (ver [08-procedimientos.md](08-procedimientos.md)). Los dos se conservan: V027 no los tocó, para no romper lo que ya funcionaba.
