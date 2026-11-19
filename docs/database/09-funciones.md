# 09 · Funciones independientes

Una **función** recibe parámetros y **devuelve un valor**. Si no modifica datos, puede usarse dentro de un `SELECT`: `SELECT fn_peso_estimado_kg(3, 2) FROM dual`.

- **En SQL Developer:** conexión → *Funciones* (Functions).
- **En GitHub:** [`database/schema/functions/`](../../database/schema/functions/).
- **Estado verificado el 18/11/2026:** las tres son `VALID`.

## FN_RESOLVER_CANECA_DISPONIBLE

**Objetivo:** dada una estación y un material, devolver **la caneca donde debe ir**, solo si de verdad está disponible. Es la regla oficial de clasificación en Oracle. **La aplicación C# la usa**: `RepositorioEstacionesOracle` ejecuta `SELECT FN_RESOLVER_CANECA_DISPONIBLE(:punto, :material) FROM DUAL`.

| Parámetro | Tipo | Significado |
|---|---|---|
| `p_id_punto` | `NUMBER` | estación (`PUNTO_INSTALACION`) |
| `p_id_material` | `NUMBER` | material |

**Devuelve:** `NUMBER`, el `id_contenedor` de la caneca correcta.

**Cómo decide, paso a paso** (cada paso que falla lanza un error con su motivo; la función nunca adivina):

| Paso | Comprobación | Error si falla |
|---|---|---|
| 1 | Los dos ids no son nulos, son > 0 y enteros | −20810 *Identificadores no válidos* |
| 2 | La estación existe | −20818 |
| 3 | La estación está activa | −20819 |
| 4 | El material existe | −20811 |
| 5 | El material está habilitado | −20812 |
| 6 | La categoría del material es 1, 2 o 3. La categoría 4 (peligroso) no va a ninguna caneca | −20813 *Material de ruta especial* |
| 7 | En la estación hay **exactamente una** caneca no dada de baja configurada con esa categoría (`CONFIGURACION_CANECA.CATEGORIA`) | 0 canecas → −20814; más de una → −20815 *ambigua* |
| 8 | Esa caneca está **en servicio** (estado 1) | −20816 |
| 9 | La caneca **admite** el material (`CONTENEDOR_MATERIAL`) | −20817 |

**Disponibilidad real:** sí la considera. Revisa que la estación esté activa, que la caneca esté en servicio (paso 8) y que acepte ese material (paso 9). No considera el nivel de llenado: las canecas no tienen sensor de nivel y la base no guarda ese dato.

**Tablas:** lee `PUNTO_INSTALACION`, `MATERIAL`, `CONFIGURACION_CANECA`, `CONTENEDOR` y `CONTENEDOR_MATERIAL`. Se ejecuta con los derechos del dueño (`AUTHID DEFINER`).

```sql
SELECT fn_resolver_caneca_disponible(:estacion, :material) AS caneca FROM dual;
```

**Resultado esperado:** con la estación piloto (id 1015) y el material 4, un aprovechable que admite la caneca blanca, devuelve **1093**. Lo verificó la prueba E1.1 el 18/11/2026, buscando los datos en la base.

**Casos límite:**
- `NULL`, `−1` o `1.5` → −20810.
- Estación inexistente → −20818.
- Material inexistente → −20811.
- Pila o batería (categoría 4) → −20813.

Pruebas E1.2 a E1.7. Los valores nulos no se tratan en silencio: dan error.

> **No duplicar en C#.** Esta función ya decide la caneca. Si la aplicación necesita mostrar el color antes de pedir la apertura, debe leer la categoría de Oracle, no repetir la regla. La regla que hoy repite la aplicación se describe en [16-hallazgos.md](16-hallazgos.md) (H-02).

## FN_PESO_ESTIMADO_KG

**Objetivo:** estimar, nunca medir, cuántos kilogramos pesan *n* unidades de un objeto típico.

| Parámetro | Tipo | Significado |
|---|---|---|
| `p_id_peso_referencial` | `peso_referencial.id_peso_referencial%TYPE` | objeto de referencia (por ejemplo, «Botella PET de 600 ml») |
| `p_unidades` | `NUMBER` | cuántas unidades |

**Devuelve:** `NUMBER`, igual a `ROUND(gramos_tipico × unidades / 1000, 4)`.

**Regla:** usa el peso **típico** de `PESO_REFERENCIAL`. Por ejemplo, 2 botellas PET de 600 ml a 22 g cada una pesan `2 × 22 / 1000 = 0,044 kg`, como comprueba la prueba F.

**Nulos y errores:**

| Caso | Devuelve |
|---|---|
| Referencia nula | NULL |
| Unidades nulas o menores que 1 | NULL |
| Referencia inexistente | NULL: atrapa `NO_DATA_FOUND`, no lanza error |

Así un dato faltante produce «sin estimación» y no un número inventado.

```sql
SELECT descripcion_objeto, fn_peso_estimado_kg(id_peso_referencial, 3) AS kg_3_unidades FROM peso_referencial;
```

**Casos límite** (pruebas E2.1 a E2.4): `(NULL, 2)` → NULL; `(ref, 0)` → NULL; `(-1, 2)` → NULL; `(ref, 3)` → `3 × gramos_típico / 1000`.

## FN_DEPOSITOS_CIUDADANO

**Objetivo:** devolver el historial de depósitos de un ciudadano, del más reciente al más antiguo.

| Parámetro | Tipo |
|---|---|
| `p_id_usuario` | `usuario.id_usuario%TYPE` |

**Devuelve:** un `SYS_REFCURSOR` con estas columnas de `VW_DEPOSITO_DETALLE`:
- `id_deposito`, `fecha_colombia`, `color`;
- `material_declarado`, `material_verificado`;
- `estado_texto`, `evidencia_fisica`;
- `peso_estimado_g`, `peso_medido_g`.

**Regla:** filtra con `id_titular = p_id_usuario` y ordena por fecha descendente. Reutiliza la vista en lugar de repetir los `JOIN`.

**Casos límite:** un usuario sin depósitos o inexistente devuelve un cursor **vacío**, sin error (prueba E3.2). La prueba E3.1 comprueba que devuelve exactamente las mismas filas que la vista.

```sql
VARIABLE rc REFCURSOR
EXEC :rc := fn_depositos_ciudadano(:id_usuario)
PRINT rc
```
