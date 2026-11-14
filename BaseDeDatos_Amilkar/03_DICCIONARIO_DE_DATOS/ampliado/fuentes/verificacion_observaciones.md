
## Revisión especial de las cinco tablas clave

| Tabla | Qué se contrastó | Resultado |
|---|---|---|
| REGLA_ECOPUNTOS | 10 columnas; `CK_REGLA_PUNTOS` (1–100000), `CK_REGLA_INTERVALO`, `PKG_ECOPUNTOS.REGISTRAR_REGLA` (rol 3, `FOR UPDATE` sobre la fila padre de `MATERIAL`, cierre de la versión abierta) y `REGLA_VIGENTE` (`desde <= fecha < hasta`, ORA-20203). Datos: 8 reglas sintéticas; PET regla 1 (10, hasta 22/10/2026) → regla 8 (12, vigente). | Coincide |
| MOVIMIENTO_ECOPUNTOS | 11 columnas; 5 FK (una recursiva); `UQ_MOV_CLAVE`, `UQ_MOV_REVERTIDO`, `CK_MOV_TIPO`; claves `CRED-<depósito>-<n>` y `REV-<movimiento>` en `ACREDITAR`/`REVERTIR`. Datos: 48 movimientos, suma 675; depósito 5 = CRED-5-1 (+30), REV-2 (−30), CRED-5-2 (+10). | Coincide |
| DEPOSITO | 18 columnas; FK compuesta `(ID_EVENTO, ID_ORDEN)`; `CK_DEPOSITO_VERIFICACION`, `CK_DEPOSITO_MEDIDO`, `CK_DEPOSITO_ESTIMADO`; reglas (0)–(7) del trigger 2; trigger 4. Datos: 105 (76/17/12), 72 con sensor y 33 sin sensor, 14 con peso medido. | Coincide |
| CONSULTA_VISION | 20 columnas; 5 FK opcionales; `CK_CONSULTA_IDENTIFICADO`, `CK_CONSULTA_PESO`. Datos: 187 (164 identificadas, 17 piden otra foto, 6 no son residuo), 147 con QR y 40 sin QR. | Coincide |
| AUDITORIA_DEPOSITO | 17 columnas; 1 FK; `CK_AUD_DEP_OPERACION`, `CK_AUD_DEP_ESTADOS`; INSERT del trigger 3 (15 columnas, `FECHA_UTC` por DEFAULT). Datos: 91 (76 VERIFICACION, 13 RECHAZO, 1 CORRECCION, 1 REAPERTURA). | Coincide |

## Discrepancias y observaciones encontradas

No se cambió el esquema Oracle, ningún trigger ni ningún paquete. Lo siguiente queda documentado para que el equipo decida:

1. **AUDITORIA_DEPOSITO no tiene FK en `ID_ACTOR`, `ID_VERIFICADOR_*` ni `ID_MATERIAL_VERIFICADO_*`**, mientras que `HISTORIAL_ESTADO_CONTENEDOR.ID_ACTOR` sí tiene FK (`FK_HIST_ACTOR`). No es un error funcional: los valores vienen de `:OLD`/`:NEW` de DEPOSITO (ya validados por sus FK) o de `PKG_CONTEXTO_APP.FIJAR_ACTOR` (que valida que el usuario exista y esté activo). Pero el criterio no es el mismo en las dos tablas de auditoría; conviene poder explicarlo si el profesor lo nota.
2. **Ninguna restricción declarativa impide dos versiones solapadas de REGLA_ECOPUNTOS para el mismo material.** Lo impide el procedimiento `REGISTRAR_REGLA`: bloquea la fila padre de `MATERIAL` (corrección del 14/11/2026, comprobada con dos sesiones simultáneas) y valida las fechas. Si ocurriera, `REGLA_VIGENTE` lo detecta con ORA-20203. Un INSERT directo con SQL, sin pasar por el procedimiento, todavía podría crear el solapamiento.
3. **`DEPOSITO.ID_ORDEN` no tiene una FK simple hacia ORDEN_APERTURA.** Está validado indirectamente por la FK compuesta `FK_DEPOSITO_EVENTO (ID_EVENTO, ID_ORDEN)` y por `FK_EVENTO_ORDEN`. Es intencional y se explica así en la ficha.
4. **Las FK compuestas con una columna que admite NULL (`FK_INTERACCION_CONTENEDOR`, `FK_EVENTO_ORDEN`) no se evalúan cuando esa columna es NULL** (comportamiento estándar de Oracle). Los CHECK `CK_INTERACCION_AUTORIZADA` y `CK_EVENTO_APLICADO` exigen el valor en los estados donde es obligatorio.
5. **El peso estimado de CONSULTA_VISION no se valida contra el rango del catálogo**; solo el de DEPOSITO lo valida el trigger 2. El CHECK `CK_CONSULTA_PESO` sí exige que sea positivo y tenga fila de catálogo y unidades.
6. **`CK_DEPOSITO_VERIFICACION` no exige `CANTIDAD_VERIFICADA` en un depósito verificado.** Es coherente con las tarifas por depósito; cuando la tarifa es por unidad, `ACREDITAR` lo exige (ORA-20204).
7. **Texto breve del diccionario original:** en `ORDEN_APERTURA.FECHA_EMISION_UTC` la columna «Dónde se usa» dice «Ventana de 10 s», mientras que en los datos sintéticos el vencimiento es de 2 minutos después de la emisión. La explicación ampliada usa lo observado en Oracle; el texto breve no se modificó.
8. **Conteos del enunciado:** los 19 conteos por tabla (6, 9, 3, 8, 3, 7, 5, 6, 8, 3, 15, 20, 8, 8, 18, 10, 11, 17, 8) coinciden con Oracle. Los ejemplos de profundidad del enunciado (ID_REGLA, ID_MATERIAL = 101 PET, PUNTOS = 12) también coinciden con la implementación.
9. **ECOACCESS_PRACTICAS** se mantuvo aparte: sus tablas y triggers solo aparecen en la sección de triggers académicos y no se cuentan entre las 19 tablas.
