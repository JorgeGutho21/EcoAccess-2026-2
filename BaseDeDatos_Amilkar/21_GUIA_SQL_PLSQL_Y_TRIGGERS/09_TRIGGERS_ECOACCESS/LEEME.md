# 09 · Los 4 disparadores (triggers) de EcoAccess

Fuente única del código: [`04_SCRIPTS_ORACLE/08_triggers.sql`](../../04_SCRIPTS_ORACLE/08_triggers.sql). Aquí se explica, no se copia otra versión.

## 1. Qué significa

Un **disparador** es un bloque PL/SQL que Oracle ejecuta **solo**, sin que nadie lo llame, cuando ocurre un evento sobre una tabla (INSERT, UPDATE o DELETE).

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

- Para reglas que **deben cumplirse siempre**, se cambie el dato desde C#, el bot o SQL Developer.
- Para **auditar**: guardar quién cambió qué.
- Se usa cuando la regla mira **otras tablas** o necesita los valores de antes y de después. Si la regla mira solo la propia fila, basta un `CHECK`.

## 5. Estructura

```sql
CREATE OR REPLACE TRIGGER nombre
{BEFORE | AFTER} {INSERT | UPDATE [OF columnas] | DELETE} ON tabla
FOR EACH ROW                         -- una vez por cada fila afectada
[FOLLOWS otro_trigger]               -- orden entre triggers del mismo momento
[WHEN (OLD.col <> NEW.col)]          -- condición, SIN dos puntos
DECLARE
    variables;
BEGIN
    … :OLD.columna (antes) … :NEW.columna (después) …
    RAISE_APPLICATION_ERROR(-20xxx, 'mensaje');   -- rechaza la operación
END;
/
```

| Palabra | Significado |
|---|---|
| `BEFORE` | Corre antes de guardar. Puede **rechazar** y puede **asignar** `:NEW`. |
| `AFTER` | Corre después de guardar. Solo **lee** `:OLD` y `:NEW`; ideal para auditar lo que quedó. |
| `:OLD` | La fila como estaba. En INSERT todo es NULL. |
| `:NEW` | La fila como va a quedar. En DELETE todo es NULL. |
| `INSERTING` / `UPDATING` | Dicen qué operación disparó un trigger de varios eventos. |
| Sin `COMMIT` | El trigger pertenece a la transacción: si la sentencia falla, su auditoría también se deshace. |

## 6. Ejemplo básico

El disparador 1 es el más corto (11 líneas) y sirve de ejemplo básico.

## 7. Los 4 disparadores reales

| # | Nombre | Momento y evento | Tabla | Para qué |
|---|---|---|---|---|
| 1 | `TRG_CONTENEDOR_AUD_ESTADO` | AFTER UPDATE OF estado, FOR EACH ROW, WHEN (OLD.estado <> NEW.estado) | CONTENEDOR | Guarda el cambio de estado de la caneca en HISTORIAL_ESTADO_CONTENEDOR. |
| 2 ★ | `TRG_DEPOSITO_COHERENCIA` | BEFORE INSERT OR UPDATE, FOR EACH ROW, FOLLOWS el 4 | DEPOSITO | Rechaza depósitos incoherentes y completa la fecha de detección. |
| 3 ★ | `TRG_DEPOSITO_AUD_VERIFICACION` | AFTER UPDATE OF estado_verificacion, id_material_verificado, cantidad_verificada, peso_medido_kg, id_verificador | DEPOSITO | Audita cada verificación, rechazo, reapertura o corrección con valores de antes y de después. |
| 4 | `TRG_DEPOSITO_BLOQUEO_CERRADO` | BEFORE UPDATE, FOR EACH ROW, WHEN (OLD.estado_verificacion <> 0) | DEPOSITO | Impide modificar un depósito ya cerrado, salvo con una corrección autorizada. |

★ = los dos que se exponen en la sustentación.

### Disparador 1 — línea por línea

```sql
CREATE OR REPLACE TRIGGER trg_contenedor_aud_estado
AFTER UPDATE OF estado ON contenedor          -- solo si el UPDATE toca la columna estado
FOR EACH ROW                                  -- una vez por cada caneca modificada
WHEN (OLD.estado <> NEW.estado)               -- y solo si el estado de verdad cambió
BEGIN
    INSERT INTO historial_estado_contenedor
        (id_contenedor, estado_anterior, estado_nuevo, id_actor, usuario_bd, motivo)
    VALUES
        (:OLD.id_contenedor, :OLD.estado, :NEW.estado,      -- antes y después
         pkg_contexto_app.actor, USER, pkg_contexto_app.motivo);  -- quién, con qué cuenta y por qué
END;
```

**¿Por qué AFTER?** Así solo se registra lo que de verdad quedó guardado: si un CHECK rechaza el UPDATE, el AFTER ni siquiera corre.

### Disparador 4 — bloqueo de depósitos cerrados

```sql
BEFORE UPDATE ON deposito FOR EACH ROW
WHEN (OLD.estado_verificacion <> 0)            -- ya verificado (1) o rechazado (2)
BEGIN
    IF NOT pkg_deposito.correccion_en_curso THEN
        RAISE_APPLICATION_ERROR(-20103, 'El depósito … ya está VERIFICADO/RECHAZADO …');
    END IF;
END;
```

Un `UPDATE deposito SET cantidad_verificada = 5 WHERE id_deposito = …` escrito a mano sobre un depósito cerrado falla con **ORA-20103**. Solo `PKG_DEPOSITO.corregir_verificacion` (que exige motivo) activa `correccion_en_curso` y puede pasar.

### Disparador 2 ★ — coherencia del depósito (las reglas, en orden)

| Paso | Cuándo | Regla | Error |
|---|---|---|---|
| 0 | UPDATING | Orden, evento, material declarado, sin_sensor y fecha de detección no cambian. | ORA-20109 |
| 1 | INSERTING | Un depósito nuevo nace PENDIENTE (estado 0). | ORA-20107 |
| 2 | INSERTING | Si no trae fecha de detección, `:NEW.fecha_deteccion_utc := SYS_EXTRACT_UTC(SYSTIMESTAMP)`. Esto **solo es posible en BEFORE**. | — |
| 3 | INSERTING | sin_sensor = 0 exige evento tipo 3 (detección); sin_sensor = 1 exige tipo 6 (cierre sin sensor). | ORA-20108 |
| — | siempre | Lee la orden: titular (`INTERACCION.id_usuario`) y caneca. Si la orden no existe, sale (`RETURN`) y deja que la FK la rechace con ORA-02291. | — |
| 4 | INSERTING | La caneca admite el material declarado (CONTENEDOR_MATERIAL). | ORA-20102 |
| 5 | si hay peso estimado | El peso cabe en `gramos_minimo … gramos_maximo × unidades` (PESO_REFERENCIAL). | ORA-20106 |
| 6 | al verificar o rechazar | Quien verifica no es el titular, y es Operario o Administrador activo. | ORA-20101 / ORA-20104 |
| 7 | verificado | Si el material comprobado es de otro color que la caneca, exige `motivo_verificacion`. | ORA-20105 |

**¿Por qué no un CHECK?** Porque estas reglas consultan **otras tablas**. Lo que solo mira la propia fila ya está en las restricciones `ck_deposito_*` y no se repite. Además, el trigger **no consulta DEPOSITO**, para evitar ORA-04091 (tabla mutante).

### Disparador 3 ★ — auditoría de la verificación

1. Una función local `cambio(a, b)` devuelve 0 si los valores son iguales (NULL = NULL cuenta como igual) y 1 si cambiaron.
2. Suma los cambios de las 5 columnas. Si la suma es 0, sale: un UPDATE que deja los mismos valores no deja auditoría.
3. Decide la operación:
   - 0 → 1: `VERIFICACION`
   - 0 → 2: `RECHAZO`
   - cerrado → 0: `REAPERTURA`
   - otro cambio: `CORRECCION`
4. Inserta en AUDITORIA_DEPOSITO los valores `:OLD` y `:NEW` de cada columna, el actor (`pkg_contexto_app.actor`), la cuenta (`USER`) y el motivo.

## 8. Orden de ejecución en un UPDATE de DEPOSITO

```
UPDATE deposito …
 ├─ BEFORE fila: 4 TRG_DEPOSITO_BLOQUEO_CERRADO  (¿está cerrado?)
 ├─ BEFORE fila: 2 TRG_DEPOSITO_COHERENCIA       (FOLLOWS el 4)
 ├─ se guarda la fila, se comprueban CHECK y FK
 └─ AFTER fila:  3 TRG_DEPOSITO_AUD_VERIFICACION (audita)
```

## 9. Resultado esperado de [`09_triggers_consulta.sql`](09_triggers_consulta.sql)

- A: 4 filas, todas ENABLED y VALID. Tipos: `AFTER EACH ROW` (1 y 3) y `BEFORE EACH ROW` (2 y 4).
- B: 2 cláusulas WHEN (disparadores 1 y 4). `USER_TRIGGER_COLS` lista las columnas que **usa** cada disparador: las del `UPDATE OF` y también las que lee con `:OLD`/`:NEW` (23 filas en total).
- C: TRG_DEPOSITO_COHERENCIA FOLLOWS TRG_DEPOSITO_BLOQUEO_CERRADO.
- D, E y F: filas reales del historial y de la auditoría.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## 10. Cómo ejecutarlo y cómo verlos actuar

1. Consulta (no modifica nada): abre `09_triggers_consulta.sql` y pulsa **F5**.
2. Para verlos en el árbol: **Conexiones → EcoAccess BD LOCAL → Disparadores** → doble clic → pestaña **Código**.
3. Para verlos **actuar**: [`07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql`](../../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql). Esta demostración **modifica datos** dentro de una transacción y al final hace `ROLLBACK`, así que la base queda como estaba. Ejecútala completa con F5; no la cortes a la mitad.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| ORA-04091 tabla mutante | Un trigger de fila consulta la misma tabla que se está modificando. Por eso el 2 no consulta DEPOSITO. |
| ORA-04098 trigger inválido | El trigger no compiló; revisa `SHOW ERRORS TRIGGER nombre`. |
| ORA-04084 no se puede cambiar NEW | Se intentó asignar `:NEW` en un trigger AFTER. |
| ORA-20101 … ORA-20109 | Las reglas de negocio de los disparadores 2 y 4 (tabla del paso 2). |

## 12. Preguntas del profesor

- ¿BEFORE o AFTER para auditar? *AFTER: se registra solo lo que quedó guardado.*
- ¿Qué diferencia hay entre `:OLD` y `OLD`? *Con dos puntos dentro del cuerpo; sin dos puntos en la cláusula WHEN.*
- ¿Por qué ningún trigger hace COMMIT? *Porque pertenece a la transacción de la sentencia que lo disparó.*
- ¿Qué hace FOLLOWS? *Fija que el 2 corra después del 4 cuando ambos son BEFORE de fila.*
- ¿Los triggers dan EcoPuntos? *No: eso lo hace solo `PKG_ECOPUNTOS.acreditar`, llamado por `PKG_DEPOSITO.verificar`.*

La tarea de 4 puntos del profesor está en la carpeta [`10_TAREA_TRIGGERS_PROFESOR_AMILKAR`](../10_TAREA_TRIGGERS_PROFESOR_AMILKAR/LEEME.md).
