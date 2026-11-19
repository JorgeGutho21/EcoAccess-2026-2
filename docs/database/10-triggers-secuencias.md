# 10 · Disparadores (triggers) y secuencias

Un **disparador** es código que Oracle ejecuta **solo** cuando ocurre un evento sobre una tabla (`INSERT`, `UPDATE`, `DELETE`). Nadie lo llama: se activa solo. Se usa para reglas que no deben depender de que la aplicación «se acuerde» de cumplirlas.

- **En SQL Developer:** conexión → *Disparadores* (Triggers), o dentro de la tabla → pestaña *Disparadores*.
- **En GitHub:** [`database/schema/triggers/`](../../database/schema/triggers/).
- **Verificado el 18/11/2026:** hay **5 disparadores**, todos `ENABLED` y `VALID`, sobre `DEPOSITO`, `CONTENEDOR` y `MOVIMIENTO_ECOPUNTOS`. No hay disparadores de `DELETE`, ni sobre `USUARIO`, ni para mantener saldos: el saldo no se guarda, se suma (ver [12-ecopuntos.md](12-ecopuntos.md)).

| Disparador | Tabla | Momento y evento | Condición | Migración |
|---|---|---|---|---|
| `TRG_DEPOSITO_BLOQUEO_CERRADO` | `DEPOSITO` | `BEFORE UPDATE`, por fila | `WHEN (OLD.id_material_verificado IS NOT NULL)` | V027 |
| `TRG_DEPOSITO_COHERENCIA` | `DEPOSITO` | `BEFORE INSERT OR UPDATE`, por fila; se ejecuta **después** del anterior (`FOLLOWS`) | — | V027 |
| `TRG_DEPOSITO_AUD_VERIFICACION` | `DEPOSITO` | `AFTER UPDATE OF id_material_verificado, cantidad_verificada, peso_medido_kg, id_verificador`, por fila | algún valor cambió | V027 |
| `TRG_CONTENEDOR_AUD_ESTADO` | `CONTENEDOR` | `AFTER UPDATE OF estado`, por fila | `WHEN (OLD.estado <> NEW.estado)` | V027 |
| `TR_ECOP_UNIDADES` | `MOVIMIENTO_ECOPUNTOS` | `BEFORE INSERT`, por fila | `WHEN (NEW.tipo = 1)`, solo créditos | V011 |

## TRG_DEPOSITO_BLOQUEO_CERRADO

**Regla:** un depósito ya **verificado no se cambia**, salvo con `PKG_DEPOSITO.corregir_verificacion`, que exige rol de administrador y un motivo.
**Acción:** si `PKG_DEPOSITO.correccion_en_curso` es FALSE, lanza **−20103**.
**Depende de:** `PKG_DEPOSITO`.

```sql
UPDATE deposito SET cantidad_verificada = 5 WHERE id_deposito = :verificado;   -- ORA-20103
```

## TRG_DEPOSITO_COHERENCIA

**Reglas:**
1. Al **actualizar**, no se pueden cambiar orden, evento, material declarado, `sin_sensor` ni fecha de detección (−20109). Son la evidencia de lo que pasó.
2. Al **insertar**, la caneca de la orden debe **admitir** el material declarado (`CONTENEDOR_MATERIAL`); si no, −20102.
3. Si se pone un verificador, debe ser **Operario o Administrador activo** (−20104).

**Detalle:** si la orden no existe, el disparador no falla. Deja que la clave foránea rechace el `INSERT` con su propio error.

**Depende de:** `PKG_CONTEXTO_APP.tiene_rol`.

```sql
UPDATE deposito SET fecha_deteccion_utc = fecha_deteccion_utc - 1 WHERE id_deposito = :dep;   -- ORA-20109
```

## TRG_DEPOSITO_AUD_VERIFICACION

**Regla:** todo cambio en la verificación queda registrado.

**Acción:** inserta una fila en `AUDITORIA_DEPOSITO` con los valores anterior y nuevo, el actor y el motivo.
- **Actor:** `PKG_CONTEXTO_APP.actor`, o el verificador si no se fijó ninguno.
- **Operación:**
  - `VERIFICACION`: de pendiente a verificado;
  - `REAPERTURA`: de verificado a pendiente;
  - `CORRECCION`: cualquier otro cambio.

Si los cuatro valores no cambiaron, no escribe nada.

**Efecto secundario:** el `UPDATE` que hace `PKG_DEPOSITO.verificar` deja su huella aquí sin que el paquete tenga que hacer el `INSERT`.

## TRG_CONTENEDOR_AUD_ESTADO

**Regla:** cada cambio de estado de una caneca queda en `HISTORIAL_ESTADO_CONTENEDOR`, con estado anterior y nuevo, actor, cuenta de Oracle (`USER`) y motivo.
**Se activa:** con `PR_CAMBIAR_ESTADO_CONTENEDOR` o con cualquier `UPDATE contenedor SET estado = …`. Si se hace sin fijar actor, `ID_ACTOR` queda vacío y `VW_BI_AUDITORIA` lo mostraría como «sin actor de aplicación».

## TR_ECOP_UNIDADES

**Regla:** si la tarifa es **por unidad** (`REGLA_ECOPUNTOS.POR_UNIDAD = 1`), el crédito exige:
- un conteo verificado (`DEPOSITO.CANTIDAD_VERIFICADA` no nulo);
- que `puntos = tarifa × cantidad`.

Si no se cumple, lanza **−20113**.

**Por qué importa:** aunque un programa calcule mal los puntos, la base no los acepta. Protege igual a la aplicación C# y a `PKG_ECOPUNTOS`.

**Ojo:** comparte el código −20113 con `PKG_DEPOSITO.corregir_verificacion` (ver [hallazgos](16-hallazgos.md) H-04).

## ¿Se duplican operaciones entre la aplicación, los paquetes y los disparadores?

Revisado sobre el código real:

- **Auditoría:** no hay duplicación. Los disparadores escriben la auditoría; los paquetes no la repiten, salvo el `RECHAZO` y la `REAPERTURA` desde rechazado. Esas dos inserta `PKG_DEPOSITO` porque no cambian columnas de `DEPOSITO`, y por eso ningún disparador las vería.
- **Ecopuntos: sí hay duplicación de lógica, no de datos.**
  - La aplicación C# (`ServicioEcopuntos` + `RepositorioEcopuntosOracle`) y `PKG_ECOPUNTOS` aplican las mismas reglas y escriben en el mismo libro.
  - No se pueden acreditar dos veces: lo impiden el índice único `UQ_ECOP_CREDITO_INICIAL` (un crédito inicial por depósito), `UQ_ECOP_REVERSO` (un reverso por crédito), `UQ_ECOP_OPERACION_CLAVE` y `TR_ECOP_UNIDADES`.
  - El riesgo es que las dos copias de la regla dejen de coincidir con el tiempo. Ver [16-hallazgos.md](16-hallazgos.md) (H-01).

---

## Secuencias

El esquema **no tiene secuencias creadas a mano**. Las 19 secuencias `ISEQ$$_…` son de sistema: Oracle las crea solo para cada columna `GENERATED … AS IDENTITY` (Oracle 12c o posterior).

| Columna identidad | Tipo |
|---|---|
| 14 columnas: `USUARIO.ID_USUARIO`, `DEPOSITO.ID_DEPOSITO`, `INTERACCION.ID_INTERACCION`, `ORDEN_APERTURA.ID_ORDEN`, `EVENTO_DISPOSITIVO.ID_EVENTO`, `VINCULO_TELEGRAM.ID_VINCULO`, `REGLA_ECOPUNTOS.ID_REGLA`, `OPERACION_ECOPUNTOS.ID_OPERACION`, `MOVIMIENTO_ECOPUNTOS.ID_MOVIMIENTO`, `OPINION.ID_OPINION`, `COMENTARIO_CIUDADANO.ID_COMENTARIO`, `CONSULTA_VISION.ID_CONSULTA`, `HISTORIAL_ESTADO_CONTENEDOR.ID_HISTORIAL`, `AUDITORIA_DEPOSITO.ID_AUDITORIA` | `GENERATED ALWAYS`: no se puede escribir el id a mano |
| 5 columnas de catálogo: `ROL.ID_ROL`, `MATERIAL.ID_MATERIAL`, `CONTENEDOR.ID_CONTENEDOR`, `PUNTO_INSTALACION.ID_PUNTO_INSTALACION`, `PESO_REFERENCIAL.ID_PESO_REFERENCIAL` | `GENERATED BY DEFAULT`: se puede dar el id a mano (útil para cargar catálogos con ids conocidos); si no se da, lo genera Oracle |

La lista completa, secuencia por secuencia, está en [`database/schema/INVENTARIO.md`](../../database/schema/INVENTARIO.md#secuencias).

**Consecuencias prácticas:**
- No se usa `secuencia.NEXTVAL`. Se inserta sin el id y se recupera con `RETURNING id INTO :v`, como hacen los paquetes.
- Un `ROLLBACK` no devuelve los números ya usados: es normal que haya **huecos** (por ejemplo, del 290 se salta al 1093). No indica datos borrados.
- En una instalación nueva, las secuencias empiezan otra vez en 1. Las crea la definición de cada tabla; no hay que exportarlas.

```sql
SELECT table_name, column_name, generation_type, sequence_name FROM user_tab_identity_cols ORDER BY 1;
```
