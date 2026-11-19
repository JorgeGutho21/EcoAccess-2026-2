# 06 · Vistas

Una **vista** es una consulta guardada con nombre: se consulta como si fuera una tabla, pero no guarda datos propios. Cada vez que se lee, Oracle ejecuta su `SELECT` sobre las tablas reales. Sirve para:
- escribir una sola vez un cálculo que muchos necesitan (el estado de un depósito, el saldo);
- ocultar columnas personales (Power BI ve `C-0042`, no nombres ni correos).

Las nueve vistas `VW_*` de la captura de SQL Developer las creó la migración [V027](../../database/06_migraciones/V027__capa_academica_bd.sql); [V028](../../database/06_migraciones/V028__vista_bi_consultas_vision.sql) corrigió `VW_BI_CONSULTAS_VISION`. Todas son `WITH READ ONLY`: no admiten `INSERT`, `UPDATE` ni `DELETE`. La definición de cada una que aparece aquí es la **real**, extraída de Oracle Cloud el 18/11/2026 con `DBMS_METADATA`.

- **En SQL Developer:** conexión → *Vistas* (Views) → clic en la vista → pestaña *SQL* (definición) o *Datos* (filas).
- **En GitHub:** [`database/schema/views/`](../../database/schema/views/), un archivo por vista.
- **Prueba:** [`database/tests/D_consultas_vistas.sql`](../../database/tests/D_consultas_vistas.sql) comprueba que cada vista responde y que sus columnas son exactamente las de esta página.

> **Nota:** hasta el 18/11/2026, `VW_CANECA_ESTACION`, `VW_BI_ECOPUNTOS`, `VW_BI_CONSULTAS_VISION` y `VW_BI_AUDITORIA` devolvían textos con las tildes dañadas, por ejemplo «CRÃ‰DITO». La migración V029 lo corrigió ese mismo día ([hallazgo H-18](16-hallazgos.md)).

**Convenciones que se repiten:**
- `CAST(FROM_TZ(fecha_utc, 'UTC') AT TIME ZONE 'America/Bogota' AS DATE)` convierte una fecha guardada en UTC a hora de Colombia.
- `'C-' || LPAD(id_usuario, 4, '0')` convierte el id 42 en `C-0042`: un seudónimo sin nombre ni correo.
- `DECODE(k.categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA')` traduce la categoría de la caneca a su color. La regla está en `CONFIGURACION_CANECA.CATEGORIA`, no en el nombre de la caneca.
- **`ES_SINTETICO` / `ORIGEN_DATO`:** marca los datos de ensayo, para separarlos de los reales en los informes. Se reconocen por canal simulado (3, 4), nombres «FICTICIA» o «SIMULACIÓN», correos `@prueba`, `@ejemplo`, `.invalid` o `.test`, y códigos `BR_`, `IT_`, `TEST_` o `PRUEBA_`.

| Vista | Para qué sirve | Depende de | La usa |
|---|---|---|---|
| [`VW_DEPOSITO_DETALLE`](#vw_deposito_detalle) | Ficha completa de cada depósito, con su estado | 8 tablas + `PKG_DEPOSITO.ESTADO` | `VW_BI_DEPOSITOS`, `FN_DEPOSITOS_CIUDADANO`, SQL Developer |
| [`VW_BI_DEPOSITOS`](#vw_bi_depositos) | Depósitos seudonimizados para Power BI | `VW_DEPOSITO_DETALLE`, `MATERIAL` | Power BI |
| [`VW_SALDO_ECOPUNTOS`](#vw_saldo_ecopuntos) | Saldo de ecopuntos de cada ciudadano | `USUARIO`, `USUARIO_ROL`, `ROL`, `MOVIMIENTO_ECOPUNTOS` | SQL Developer, Prueba D |
| [`VW_BI_ECOPUNTOS`](#vw_bi_ecopuntos) | Movimientos de ecopuntos para Power BI | `MOVIMIENTO_ECOPUNTOS`, `OPERACION_ECOPUNTOS`, `REGLA_ECOPUNTOS`, `MATERIAL` | Power BI |
| [`VW_CANECA_ESTACION`](#vw_caneca_estacion) | Las canecas de cada estación, con color y estado | `CONTENEDOR`, `PUNTO_INSTALACION`, `CONFIGURACION_CANECA` | Power BI (CSV), SQL Developer |
| [`VW_RECORRIDO`](#vw_recorrido) | Cada visita paso a paso: sesión → orden → apertura → depósito | `INTERACCION`, `CONTENEDOR`, `ORDEN_APERTURA`, `EVENTO_DISPOSITIVO`, `DEPOSITO` + `PKG_DEPOSITO.ESTADO` | SQL Developer (trazabilidad) |
| [`VW_BI_CIUDADANOS`](#vw_bi_ciudadanos) | Ciudadanos seudonimizados para Power BI | `USUARIO`, `USUARIO_ROL`, `ROL` | Power BI |
| [`VW_BI_AUDITORIA`](#vw_bi_auditoria) | Historia de verificaciones y correcciones | `AUDITORIA_DEPOSITO` | Power BI |
| [`VW_BI_CONSULTAS_VISION`](#vw_bi_consultas_vision) | Consultas por foto («¿dónde va esto?») | `CONSULTA_VISION`, `MATERIAL` | Power BI |

> **Por qué una vista y no el mismo cálculo en C#:** si el saldo o el estado se calculan en tres sitios, tarde o temprano dan tres resultados distintos. En la vista, la regla vive en un solo lugar y SQL Developer, Power BI y las pruebas ven el mismo número. Ver [16-hallazgos.md](16-hallazgos.md): la aplicación C# todavía calcula el saldo con su propia consulta.

---

## VW_DEPOSITO_DETALLE

**Objetivo:** reunir en una fila todo lo que se sabe de un depósito: quién lo hizo, en qué caneca y estación, qué material declaró y cuál se verificó, su estado, el peso y si fue un dato de ensayo.

**Explicación sencilla:** la tabla `DEPOSITO` solo guarda números (ids). Esta vista sigue los ids hasta las demás tablas y devuelve los nombres, el color y el estado ya escritos.

**Cómo se construye:**
- `DEPOSITO` → `ORDEN_APERTURA` → `INTERACCION`: el titular y el canal.
- `→ CONTENEDOR → PUNTO_INSTALACION`: la caneca y la estación.
- `MATERIAL` dos veces: alias `md` para el material declarado y `mv` para el verificado (`LEFT JOIN`, porque puede faltar).
- `LEFT JOIN CONFIGURACION_CANECA`: el color.
- `LEFT JOIN USUARIO` dos veces: el titular y el verificador; un invitado no tiene titular.
- **Estado:** no es una columna de la tabla. Lo calcula `PKG_DEPOSITO.ESTADO(id)`: **1 VERIFICADO** si hay material verificado; **2 RECHAZADO** si lo último en la auditoría es un `RECHAZO`; **0 PENDIENTE** en otro caso.
- **Gramos:** `ROUND(kg × 1000, 1)`.

Sin filtros ni agrupación: una fila por depósito (la Prueba D lo comprueba).

| Columna | Significado |
|---|---|
| `ID_DEPOSITO`, `ID_INTERACCION` | depósito y visita de origen |
| `ID_TITULAR`, `TITULAR` | ciudadano dueño del depósito; vacío para invitados |
| `ID_CONTENEDOR`, `CANECA`, `COLOR`, `ESTACION` | caneca, su color (BLANCA, VERDE, NEGRA) y la estación |
| `MATERIAL_DECLARADO`, `MATERIAL_VERIFICADO` | códigos de material declarado y verificado |
| `CANTIDAD_DECLARADA`, `CANTIDAD_VERIFICADA` | unidades que dijo la persona y las que contó el operario |
| `EVIDENCIA_FISICA` | `DETECTADO POR SENSOR` o `DECLARADO SIN SENSOR` |
| `ESTADO_VERIFICACION`, `ESTADO_TEXTO` | 0/1/2 y PENDIENTE/VERIFICADO/RECHAZADO |
| `ID_VERIFICADOR`, `VERIFICADOR` | quién verificó |
| `PESO_ESTIMADO_KG/_G`, `PESO_MEDIDO_KG/_G` | peso estimado (foto) y medido (báscula); el estimado nunca se suma como peso real |
| `FECHA_DETECCION_UTC`, `FECHA_COLOMBIA` | fecha en UTC y en hora de Colombia |
| `CANAL` | 1 Telegram, 2 manual, 3 simulador, 4 Telegram simulado, 5 asistido |
| `ES_SINTETICO` | 1 si es un dato de ensayo |

**Ejemplo:**
```sql
SELECT id_deposito, fecha_colombia, color, material_declarado, estado_texto, evidencia_fisica
FROM   vw_deposito_detalle
WHERE  es_sintetico = 0
ORDER  BY fecha_colombia DESC FETCH FIRST 10 ROWS ONLY;
```
**Resultado esperado:** hasta 10 filas con los depósitos reales más recientes, cada uno con color y estado en palabras. El 18/11/2026 la vista tenía 113 filas: 58 VERIFICADO y 55 PENDIENTE; 103 eran datos de ensayo.

Archivo: [`database/schema/views/vw_deposito_detalle.sql`](../../database/schema/views/vw_deposito_detalle.sql)

## VW_BI_DEPOSITOS

**Objetivo:** dar a Power BI los depósitos sin nombres de personas.

**Explicación sencilla:** toma `VW_DEPOSITO_DETALLE` y cambia el nombre del titular por `C-0042` (o `SIN CUENTA`), el canal por su nombre y el código del material por su nombre.

**Reglas:**
- `MATERIAL` = el verificado si existe; si no, el declarado (`NVL`).
- `UNIDADES` = las verificadas si existen; si no, las declaradas.
- `EVIDENCIA` = `SIN SENSOR` o `SENSOR`.
- `MATERIAL_NOMBRE` sale de una subconsulta escalar a `MATERIAL`.

| Columna | Significado |
|---|---|
| `ID_DEPOSITO` | depósito |
| `CIUDADANO` | seudónimo `C-0000` o `SIN CUENTA` |
| `DIA`, `FECHA_HORA` | día y fecha-hora en Colombia |
| `ESTACION`, `CANECA` | estación y color de la caneca |
| `MATERIAL`, `MATERIAL_NOMBRE` | código y nombre del material (verificado o, si falta, declarado) |
| `ESTADO`, `EVIDENCIA` | PENDIENTE/VERIFICADO/RECHAZADO y SENSOR/SIN SENSOR |
| `UNIDADES` | unidades verificadas o declaradas |
| `PESO_ESTIMADO_KG`, `PESO_MEDIDO_KG` | pesos (ver arriba) |
| `CANAL` | TELEGRAM, MANUAL, SIMULADOR, TELEGRAM SIMULADO o ASISTIDO |
| `ORIGEN_DATO` | `REAL` o `DATOS DE ENSAYO` |

**Ejemplo:**
```sql
SELECT caneca, estado, COUNT(*) AS depositos
FROM   vw_bi_depositos
WHERE  origen_dato = 'REAL'
GROUP  BY caneca, estado ORDER BY caneca, estado;
```
**Resultado esperado:** una fila por color y estado, con el número de depósitos reales.

Archivo: [`database/schema/views/vw_bi_depositos.sql`](../../database/schema/views/vw_bi_depositos.sql)

## VW_SALDO_ECOPUNTOS

**Objetivo:** consultar el saldo de ecopuntos de cada ciudadano. **Sí, para eso sirve:** se verificó en su definición.

**Explicación sencilla:** el saldo no se guarda en ninguna columna. Es la **suma de todos los movimientos** del ciudadano en el libro `MOVIMIENTO_ECOPUNTOS`: los créditos suman y los reversos (negativos) restan. Así el saldo no puede «desincronizarse» del historial.

**Cómo se construye:**
1. Parte de `USUARIO`, unido a `USUARIO_ROL` y `ROL` con `codigo = 1`: solo ciudadanos.
2. Hace un `LEFT JOIN` a `MOVIMIENTO_ECOPUNTOS`: un ciudadano sin movimientos aparece con saldo 0 gracias a `NVL`.
3. Agrupa con `GROUP BY` por ciudadano.

| Columna | Significado |
|---|---|
| `ID_USUARIO`, `NOMBRE` | ciudadano |
| `SALDO` | `SUM(puntos)`: lo que tiene hoy |
| `ACREDITADOS` | suma de los créditos (tipo 1) |
| `REVERTIDOS` | suma de los reversos (tipo 2, valor negativo) |
| `MOVIMIENTOS` | cuántos movimientos tiene |
| `ES_SINTETICO` | siempre 0: esta vista no clasifica datos de ensayo (ver [hallazgos](16-hallazgos.md)) |

Cumple `SALDO = ACREDITADOS + REVERTIDOS`. Coincide con `PKG_ECOPUNTOS.SALDO(id)`, y la Prueba E lo comprueba.

**Ejemplo:**
```sql
SELECT id_usuario, saldo, acreditados, revertidos FROM vw_saldo_ecopuntos WHERE id_usuario = :id;
```
**Resultado esperado:** una fila con el saldo del ciudadano `:id`. El 18/11/2026 todos los saldos eran 0 porque el libro no tenía movimientos (ver [12-ecopuntos.md](12-ecopuntos.md#por-qué-hoy-no-hay-ecopuntos)).

Archivo: [`database/schema/views/vw_saldo_ecopuntos.sql`](../../database/schema/views/vw_saldo_ecopuntos.sql)

## VW_BI_ECOPUNTOS

**Objetivo:** dar a Power BI cada movimiento de ecopuntos, seudonimizado.

**Cómo se construye:**
- `MOVIMIENTO_ECOPUNTOS` → `OPERACION_ECOPUNTOS`: la fecha.
- `→ REGLA_ECOPUNTOS → MATERIAL`: el material de la tarifa.
- `TIPO` en palabras: `CRÉDITO` o `REVERSO`.

| Columna | Significado |
|---|---|
| `ID_MOVIMIENTO` | movimiento del libro |
| `CIUDADANO` | `C-0000` |
| `DIA` | día de la operación en Colombia |
| `TIPO` | CRÉDITO o REVERSO |
| `PUNTOS` | positivo o negativo |
| `MATERIAL` | código del material de la tarifa |
| `TARIFA` | texto fijo `TARIFA APROBADA`: solo hay movimientos con tarifas aprobadas |

**Ejemplo:** `SELECT dia, SUM(puntos) FROM vw_bi_ecopuntos GROUP BY dia ORDER BY dia;`

**Resultado esperado:** puntos netos por día. El 18/11/2026 la vista estaba vacía: 0 movimientos.

Archivo: [`database/schema/views/vw_bi_ecopuntos.sql`](../../database/schema/views/vw_bi_ecopuntos.sql)

## VW_CANECA_ESTACION

**Objetivo:** ver cada caneca con su estación, color, tipo de residuo, estado y dispositivo.

**Explicación sencilla:** es la vista para responder «¿cuáles son las tres canecas de la estación y en qué estado están?».

**Cómo se construye:**
- `CONTENEDOR` → `PUNTO_INSTALACION`.
- `LEFT JOIN CONFIGURACION_CANECA`: una caneca sin configurar aparece con color vacío.

**Reglas:**
- Color y tipo de residuo:
  - 1 → **BLANCA**, Aprovechables;
  - 2 → **VERDE**, Orgánicos aprovechables;
  - 3 → **NEGRA**, No aprovechables.
- Estado: 1 EN SERVICIO, 2 FUERA DE SERVICIO, 3 DADA DE BAJA.

| Columna | Significado |
|---|---|
| `ID_PUNTO_INSTALACION`, `ESTACION` | estación |
| `CODIGO_QR_ESTACION`, `QR_LEGADO_CANECA` | identificador del QR de la caneca: las dos columnas muestran `CONTENEDOR.IDENTIFICADOR_QR` |
| `ID_CONTENEDOR`, `CANECA` | caneca |
| `CATEGORIA`, `COLOR`, `TIPO_RESIDUO` | 1/2/3, color y tipo |
| `ESTADO`, `ESTADO_TEXTO` | 1/2/3 y su texto |
| `IDENTIFICADOR_DISPOSITIVO` | ESP32 que la abre |
| `ES_SINTETICO` | 1 para canecas o estaciones de demostración o simulación |

**Ejemplo:**
```sql
SELECT estacion, caneca, color, tipo_residuo, estado_texto
FROM   vw_caneca_estacion WHERE es_sintetico = 0 ORDER BY categoria;
```
**Resultado verificado el 18/11/2026:** 5 filas.
- Las tres canecas de la *Estación piloto*: 1093 BLANCA, 1400 VERDE y 1401 NEGRA, EN SERVICIO.
- Dos canecas de demostración: 289 VERDE y 290 NEGRA, FUERA DE SERVICIO. La vista no las reconoce como ensayo porque solo busca «FICTIC» en el nombre de la caneca y «SIMULACI» en el de la estación (hallazgo H-17).

Para ver solo las canecas reales en servicio, agregue `AND estado = 1`.

Archivo: [`database/schema/views/vw_caneca_estacion.sql`](../../database/schema/views/vw_caneca_estacion.sql)

## VW_RECORRIDO

**Objetivo:** seguir una visita de principio a fin, para explicar o auditar el flujo.

**Explicación sencilla:** muestra, para cada visita:
1. en qué quedó la sesión (iniciada, autorizada, con depósito…);
2. si se emitió una orden de apertura y en qué estado está;
3. cuántas aperturas confirmó la caneca;
4. si hubo depósito y si ya se verificó.

**Cómo se construye:**
- `INTERACCION` → `CONTENEDOR`: la estación.
- `LEFT JOIN ORDEN_APERTURA` y `LEFT JOIN DEPOSITO`: puede no haber orden ni depósito.
- Subconsulta que cuenta los `EVENTO_DISPOSITIVO` de tipo 2 (apertura) de la orden.
- `PKG_DEPOSITO.ESTADO` solo si hay depósito.

| Columna | Significado |
|---|---|
| `ID_INTERACCION`, `ID_USUARIO`, `ID_PUNTO_INSTALACION`, `CANAL` | visita, persona, estación y canal |
| `ESTADO_SESION` | INICIADA, AUTORIZADA, CON DEPOSITO, SIN DEPOSITO o RECHAZADA |
| `ID_MATERIAL_SUGERIDO` | lo que sugirió la IA |
| `ID_ORDEN`, `ESTADO_ORDEN` | orden de apertura: EMITIDA, ENVIADA, ACUSADA, ABIERTA, DETECTADA, CERRADA, VENCIDA o FALLIDA |
| `APERTURAS_CONFIRMADAS` | eventos de apertura recibidos de la caneca |
| `ID_DEPOSITO`, `SIN_SENSOR`, `ESTADO_VERIFICACION` | depósito, si fue sin sensor y su estado 0/1/2 |
| `INICIO_COLOMBIA` | inicio de la visita |
| `ES_SINTETICO` | 1 si el canal es simulado |

**Ejemplo:**
```sql
SELECT estado_sesion, estado_orden, COUNT(*) FROM vw_recorrido GROUP BY estado_sesion, estado_orden ORDER BY 1, 2;
```
**Resultado esperado:** cuántas visitas terminaron en cada combinación. Si una visita tuviera más de una orden, aparecería más de una vez; la Prueba D exige al menos una fila por visita.

Archivo: [`database/schema/views/vw_recorrido.sql`](../../database/schema/views/vw_recorrido.sql)

## VW_BI_CIUDADANOS

**Objetivo:** contar ciudadanos en Power BI sin exponer datos personales.

**Cómo se construye:**
- Incluye solo los usuarios con rol Ciudadano (subconsulta `EXISTS`).
- Devuelve el seudónimo, el día de registro y si la cuenta está activa.
- Marca como ensayo los correos de prueba.

| Columna | Significado |
|---|---|
| `CIUDADANO` | `C-0000` |
| `FECHA_REGISTRO` | día de registro (Colombia) |
| `ACTIVO` | 1/0 |
| `ES_SINTETICO`, `ORIGEN_DATO` | dato de ensayo o real |

**Ejemplo:**
```sql
SELECT origen_dato, COUNT(*) FROM vw_bi_ciudadanos GROUP BY origen_dato;
```
**Resultado esperado:** dos filas, `REAL` y `DATOS DE ENSAYO`, con su cantidad. El 18/11/2026: 74 ciudadanos, 3 reales y 71 de ensayo.

Archivo: [`database/schema/views/vw_bi_ciudadanos.sql`](../../database/schema/views/vw_bi_ciudadanos.sql)

## VW_BI_AUDITORIA

**Objetivo:** mostrar la historia de cada depósito: verificaciones, rechazos, reaperturas y correcciones.

**Cómo se construye:**
- Lee `AUDITORIA_DEPOSITO`.
- Traduce los estados 0/1/2 a PENDIENTE/VERIFICADO/RECHAZADO.
- Cambia el actor por `U-0000`, o `SIN ACTOR DE APLICACIÓN` si el cambio no se hizo desde EcoAccess.
- Convierte la fecha a Colombia.

| Columna | Significado |
|---|---|
| `ID_AUDITORIA`, `ID_DEPOSITO`, `OPERACION` | registro, depósito y tipo de cambio |
| `ESTADO_ANTERIOR`, `ESTADO_NUEVO` | estado antes y después |
| `CANTIDAD_ANTERIOR/NUEVA`, `PESO_MEDIDO_ANTERIOR_KG/NUEVO_KG` | qué cambió |
| `ACTOR` | `U-0000` |
| `MOTIVO` | explicación |
| `FECHA_COLOMBIA` | cuándo |

**Ejemplo:**
```sql
SELECT operacion, COUNT(*) FROM vw_bi_auditoria GROUP BY operacion;
```
**Resultado esperado:** cuántas verificaciones, rechazos, reaperturas y correcciones hubo. El 18/11/2026 no había ninguna: la tabla de auditoría se creó ese día y los depósitos verificados son del 9/11.

Archivo: [`database/schema/views/vw_bi_auditoria.sql`](../../database/schema/views/vw_bi_auditoria.sql)

## VW_BI_CONSULTAS_VISION

**Objetivo:** analizar en Power BI las consultas por foto: canal, resultado, material y caneca indicada.

**Cómo se construye:**
- `CONSULTA_VISION`, con `LEFT JOIN MATERIAL`.
- `RESULTADO` en palabras, según los códigos reales que corrigió V028:
  - 1 y 10 → IDENTIFICADO;
  - 2, 3, 4 y 6 → PIDIÓ OTRA FOTO;
  - 5 → NO ES RESIDUO;
  - el resto → ERROR.
- `CANECA_INDICADA` = color según la categoría del material (4 = RUTA ESPECIAL).

| Columna | Significado |
|---|---|
| `ID_CONSULTA`, `CIUDADANO`, `DIA` | consulta, seudónimo o `SIN CUENTA`, y día |
| `CANAL`, `MODO` | TELEGRAM/ESCRITORIO/EVALUACION y SOFTWARE/CON DISPOSITIVO |
| `RESULTADO`, `MATERIAL`, `CANECA_INDICADA` | qué respondió el sistema |
| `UNIDADES_ESTIMADAS`, `PESO_ESTIMADO_KG` | siempre vacías: la tabla no guarda ese dato y la vista no lo inventa |
| `CONTEXTO_QR` | `CON QR` si la consulta tiene contexto **y** caneca; si no, `SIN QR` (ver hallazgo H-07) |
| `LATENCIA_MS` | tiempo de respuesta |
| `ORIGEN_DATO` | `DATOS DE ENSAYO` para el canal 3 (evaluación) |

**Ejemplo:**
```sql
SELECT resultado, COUNT(*), ROUND(AVG(latencia_ms)) AS ms
FROM   vw_bi_consultas_vision WHERE origen_dato = 'REAL' GROUP BY resultado;
```
**Resultado esperado:** cuántas consultas reales terminaron en cada resultado y su latencia media.

Archivo: [`database/schema/views/vw_bi_consultas_vision.sql`](../../database/schema/views/vw_bi_consultas_vision.sql)

---

## Otras vistas del esquema (anteriores a V027)

No aparecen en la captura, pero están en la base y en [`database/schema/views/`](../../database/schema/views/):

| Vista | Migración | Uso |
|---|---|---|
| `EA_R_*` (9 vistas) | V013, V014, V015, V019, V023 | Agregados diarios sin datos personales para el informe institucional de la aplicación (`RepositorioInformeInstitucionalOracle`) y para Power BI |
| `VISTA_CANECAS_ESTACION` | V021 | Canecas de la estación con color y dispositivo; anterior a `VW_CANECA_ESTACION` |
| `VISTA_DEPOSITOS_ESTADO`, `VISTA_LOTES_SIMULACION` | V025 | Separan los datos de simulación de los reales |
