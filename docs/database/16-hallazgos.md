# 16 · Hallazgos de la auditoría (18/11/2026)

Lo que se encontró al revisar la base real y el código. **Solo se corrigió lo que era seguro y no cambia el comportamiento:** `.gitignore` y la documentación.

El resto son **propuestas**. Cambian la base compartida o el comportamiento de la aplicación, así que necesitan aprobación, una migración revisada en un pull request y pruebas de regresión.

| Id | Severidad | Hallazgo | Estado |
|---|---|---|---|
| **H-18** | **Media** | **Tildes dañadas en datos, mensajes y vistas cargados por V026–V028 en Oracle Cloud** | **V029 preparada y ensayada; falta aplicarla en Oracle Cloud** |
| H-01 | Media | Regla de ecopuntos y de verificación escrita dos veces: C# y PL/SQL | Propuesta |
| H-02 | Baja | Equivalencia categoría → color repetida en 5 vistas y en C# | Propuesta |
| H-03 | Media | La aplicación entra con la cuenta dueña del esquema | Propuesta |
| H-04 | Baja | Código de error −20113 con dos significados | Propuesta |
| H-05 | Baja | `PR_CAMBIAR_ESTADO_CONTENEDOR` no restringe transiciones | Pregunta al equipo |
| H-06 | Información | `ranking_verificados` usa `DENSE_RANK` | Documentado |
| H-07 | Baja | `VW_BI_CONSULTAS_VISION.CONTEXTO_QR` usa una columna con otro significado | Propuesta |
| H-08 | Baja | Columnas constantes en dos vistas | Documentado |
| H-09 | Baja | Tildes dañadas en el registro de migraciones V026–V028 | Incluido en V029 |
| H-10 | Baja | Solo 48 de 252 columnas tienen comentario en Oracle | Propuesta |
| H-11 | Información | `CHECK` repetidos (`CK_V006_*`) | Documentado |
| H-12 | **Alta** | `.gitignore` no bloqueaba los archivos del wallet | **Corregido** |
| H-13 | Media | La conexión «EcoAccess BD LOCAL» no es la base de la aplicación | Documentado |
| H-14 | Baja | `Ejecutar-Integracion-Esquema.ps1` usa un contenedor Docker que ya no existe | Documentado (no se tocó) |
| H-15 | Información | Una visita de simulador abierta en Oracle Cloud | Para decidir |
| H-16 | Información | 58 depósitos verificados sin ecopuntos | Correcto según las reglas |
| H-17 | Baja | `VW_CANECA_ESTACION` marca como reales dos canecas DEMO | Propuesta |

## H-01 · Regla de ecopuntos y de verificación en dos lugares

**Qué:**
- La aplicación verifica con su propio `UPDATE deposito` (`RepositorioInteraccionesOracle.VerificarDepositoAsync`).
- Acredita en un paso aparte y manual (`FormEcopuntos` → `ServicioEcopuntos.AcreditarAsync` → `RepositorioEcopuntosOracle`).
- `PKG_DEPOSITO.verificar` hace lo mismo **y además** acredita en la misma transacción mediante `PKG_ECOPUNTOS`.

Las dos implementaciones escriben el mismo libro con las mismas reglas.

**Por qué no hay datos incoherentes:** los dos caminos pasan por las restricciones y disparadores de la base:
- `UQ_ECOP_CREDITO_INICIAL`, `UQ_ECOP_REVERSO` y `UQ_ECOP_OPERACION_CLAVE`;
- `TR_ECOP_UNIDADES`;
- `TRG_DEPOSITO_*`.

Un segundo crédito para el mismo depósito es imposible.

**Riesgo:** si se cambia la regla en un lado y no en el otro, los dos caminos dan resultados distintos.

**Propuesta (no aplicada):**
1. Que la aplicación invoque `PKG_DEPOSITO.verificar` y `PKG_ECOPUNTOS.acreditar/revertir`, que ya existen, desde `RepositorioInteraccionesOracle` y `RepositorioEcopuntosOracle`.
2. Mantener las pruebas `VerificacionDepositosOracleTests` y `EcopuntosOracleTests` como regresión.
3. Decidir antes si verificar debe acreditar **siempre** (como el paquete) o si el crédito sigue siendo un paso manual (como la aplicación).

Es una decisión de negocio y es de Programación III.

## H-02 · Categoría → color repetida

`DECODE(categoria, 1, 'BLANCA', 2, 'VERDE', 3, 'NEGRA')` aparece en `VW_CANECA_ESTACION`, `VW_DEPOSITO_DETALLE`, `VW_BI_CONSULTAS_VISION`, `VISTA_CANECAS_ESTACION` y `VISTA_DEPOSITOS_ESTADO`. En C# la misma equivalencia está en `CategoriaMaterial` y `ResolutorDestino.PorCategoria` (`DestinoEstacion.cs`).

Hoy coinciden. **Propuesta:** una tabla de catálogo `CATEGORIA_RESIDUO (codigo, color, descripcion)`, de la que lean las vistas, con una prueba que compare el catálogo con el enumerado de C#. Es una migración aditiva, pero cambia cinco vistas.

## H-03 · La aplicación usa la cuenta dueña

`ECOACCESS_P3_APP` puede crear y modificar tablas. Mínimo privilegio sería que la aplicación entre con otra cuenta que tenga solo `ECOACCESS_ROL_EDICION` (más `EXECUTE`). Los roles ya existen.

**Propuesta:** crear la cuenta de ejecución, dar los permisos y cambiar la cadena en los User Secrets. Requiere probar que la aplicación no dependa de nombres sin esquema (`ALTER SESSION SET CURRENT_SCHEMA` al abrir la conexión).

## H-04 · −20113 con dos significados

- `PKG_DEPOSITO.corregir_verificacion`: «estado de corrección no válido».
- `TR_ECOP_UNIDADES`: «la tarifa unitaria exige conteo verificado».

**Propuesta:** dar al disparador un código propio, por ejemplo −20209, en una migración, y ajustar `TraductorErroresOracle` si lo usa.

## H-05 · Transiciones de estado de caneca

`PR_CAMBIAR_ESTADO_CONTENEDOR` acepta cualquier cambio entre 1, 2 y 3, por ejemplo **de «dada de baja» a «en servicio»**. Si una baja debe ser definitiva, falta esa regla. **Pregunta para el equipo:** ¿se puede reactivar una caneca dada de baja?

## H-06 · `ranking_verificados` y los empates

Usa `DENSE_RANK`: «top 5» son **5 puestos**, no 5 filas. Con el último año de datos devolvió **16 filas** por empates. Es correcto si se quiere premiar a todos los empatados; si se quieren exactamente N filas, habría que usar `ROW_NUMBER`.

## H-07 · `CONTEXTO_QR` en `VW_BI_CONSULTAS_VISION`

La vista dice `CON QR` cuando `CONSULTA_VISION.CONTEXTO` no es nulo y hay caneca. Pero `CONTEXTO` es el **tipo de objeto de la foto** (`ContextoObjeto`: aprovechable, persona, animal…), no el QR. La presencia del QR la indica solo `ID_CONTENEDOR`.

**Propuesta:** `CASE WHEN cv.id_contenedor IS NULL THEN 'SIN QR' ELSE 'CON QR' END`. Además, `UNIDADES_ESTIMADAS` y `PESO_ESTIMADO_KG` son siempre NULL: la tabla no guarda esos datos.

## H-08 · Columnas constantes

- `VW_SALDO_ECOPUNTOS.ES_SINTETICO` es siempre 0: no separa los ciudadanos de ensayo.
- `VW_BI_ECOPUNTOS.TARIFA` es siempre `'TARIFA APROBADA'`.

No es un error, pero quien lea esas columnas debe saberlo.

## H-18 · Tildes dañadas por la forma en que se aplicaron V026–V028 (la más importante)

**Qué:** el texto que crearon esas migraciones quedó guardado con **doble codificación UTF-8**. Por ejemplo, «á» se guardó con los bytes `c3 83 c2 a1` y se ve como «Ã¡». Verificado byte a byte con `DUMP` el 18/11/2026.

| Dónde | Oracle Cloud | Esquema aislado |
|---|---|---|
| `PUNTO_DISPOSICION` (los ve el ciudadano) | **6 de 6 filas** | 0 de 6 |
| `PESO_REFERENCIAL` | **19 de 19 filas** | 19 de 19 |
| Mensajes de error en PL/SQL | `PKG_CONTEXTO_APP`, `PKG_DEPOSITO`, `PKG_ECOPUNTOS`, `PKG_ESTADISTICAS`, `PR_CAMBIAR_ESTADO_CONTENEDOR`, `TRG_DEPOSITO_BLOQUEO_CERRADO`, `TRG_DEPOSITO_COHERENCIA` | igual |
| Textos que **devuelven** 4 vistas | `VW_CANECA_ESTACION.TIPO_RESIDUO` («OrgÃ¡nicos aprovechables»), `VW_BI_ECOPUNTOS.TIPO` («CRÃ‰DITO»), `VW_BI_CONSULTAS_VISION.RESULTADO` («PIDIÃ“ OTRA FOTO») y `VW_BI_AUDITORIA.ACTOR` | igual |
| Descripción en `ECO_MIGRACION_APLICADA` | V026, V027 y V028 | — |

El archivo `V027__capa_academica_bd.sql` del repositorio está **bien**: tiene 0 caracteres dañados. **Causa probable:** las migraciones se ejecutaron con un cliente que no estaba en UTF-8. En Windows, SQL\*Plus sin `NLS_LANG=…AL32UTF8` envía el archivo como `WE8MSWIN1252`. Las migraciones anteriores, V001 a V025, no tienen el problema.

**Consecuencias:**
- un filtro de Power BI por `TIPO = 'CRÉDITO'` no encontraría nada;
- los mensajes de error y los puntos de disposición especial se ven con símbolos raros.

**Corrección preparada: V029.** [`V029__corregir_codificacion_v026_v028.sql`](../../database/06_migraciones/V029__corregir_codificacion_v026_v028.sql):
- **Solo ASCII**, así que no se puede volver a dañar al ejecutarlo.
- **Fase 1** calcula cada reparación (`RAW_TO_CHAR(STRING_TO_RAW(texto, 'WE8MSWIN1252'), 'AL32UTF8')`) y exige que sea **exactamente reversible**. Si una sola falla, se detiene **sin cambiar nada**.
- **Fase 2** corrige los datos por `ROWID` y recrea los objetos con `DBMS_METADATA` y `CREATE OR REPLACE`, lo que conserva los permisos.
- **Fase 3** recompila y comprueba que no quedan objetos inválidos ni textos dañados.
- Es repetible y está registrada en el plan del instalador.

**Ensayo** ([evidencia](evidencias/v029_ensayo_20261118.md)):

| Dónde | Resultado |
|---|---|
| Esquema aislado | **Aplicada**: 13 objetos recreados y 26 valores reparados. El DDL después es igual al de antes reparado en 161 de 163 archivos; los 2 restantes difieren solo en un espacio delante de `WHEN`. Repetirla no hace nada. Pruebas A–F, integración C# (58/0/1) e instalador (51/51) aprobadas |
| Oracle Cloud | **Solo validación**, sin cambios: 13 objetos y 37 valores por reparar, **0 no reversibles** |

**Para aplicarla en Oracle Cloud** (requiere tu aprobación; no se ha hecho):
1. Fusionar el PR. Compilar el instalador: `dotnet build src\EcoAccess.P3.Instalador -c Release`.
2. **Detener el bot** mientras se aplica. Al reemplazar `PKG_CONTEXTO_APP` y `PKG_DEPOSITO`, una sesión abierta que ya los usaba recibiría una vez `ORA-04068`.
3. `src\EcoAccess.P3.Instalador\bin\Release\net48\EcoAccess.P3.Instalador.exe plan`: debe mostrar solo **V029** pendiente.
4. `… EcoAccess.P3.Instalador.exe instalar`. Debe terminar en `V029 aplicada: 13 objetos recreados, 37 valores reparados, 0 objetos invalidos, 0 textos danados.`
5. `pwsh -File database\scripts\Exportar-EsquemaOracle.ps1` y `pwsh -File database\scripts\Ejecutar-PruebasBD.ps1`. Revisar el `git diff` de `database/schema`: solo deben cambiar las tildes. Reanudar el bot.

**Volver atrás:** no se revierte a texto dañado. El DDL anterior queda en el historial de Git de `database/schema`.

**Nota:** `database/schema` guarda el DDL **tal como está hoy en Oracle**, con esos caracteres dañados. Cuando se aplique la corrección, se vuelve a exportar.

## H-09 · Tildes dañadas en `ECO_MIGRACION_APLICADA`

Las descripciones de V026, V027 y V028 están guardadas como `DisposiciÃ³n` y `AcadÃ©mica`: se registraron con el cliente en otra codificación. Las demás filas están bien. Es cosmético.

**Propuesta:** una migración que actualice esas tres descripciones. Es un `UPDATE` sobre el registro, así que requiere aprobación.

## H-10 · Comentarios del diccionario en Oracle

Solo 48 de las 252 columnas tienen `COMMENT` en Oracle. El diccionario completo está en [05-tablas-relaciones.md](05-tablas-relaciones.md).

**Propuesta:** una migración `V029__comentarios_diccionario.sql` con `COMMENT ON COLUMN` para las 204 restantes, generada desde ese documento. No toca datos, pero es DDL sobre la base compartida.

## H-11 · `CHECK` duplicados

V006 añadió `CK_V006_*` iguales a algunos `CK_*` que ya existían (en `ROL`, `MATERIAL`, `CONTENEDOR`, `USUARIO` y `PUNTO_INSTALACION`). Son inofensivos: Oracle evalúa los dos. Se podrían quitar con una migración, sin urgencia.

## H-12 · Wallet sin bloquear en `.gitignore` — corregido

La guía `docs/oracle-cloud/01_CONEXION_SQL_DEVELOPER.md` decía que el `.gitignore` bloqueaba los archivos del wallet, pero **no tenía esas reglas**. Se agregaron:
- `cwallet.sso`, `ewallet.p12` y `ewallet.pem`;
- `keystore.jks` y `truststore.jks`;
- `Wallet_*.zip` y `**/wallets/`;
- `_LOCAL_NO_PUBLICAR/` y `credenciales_locales.env`.

La Prueba I comprueba que están ignorados y que no hay ningún archivo secreto versionado: **0 encontrados**.

## H-13 · «EcoAccess BD LOCAL» no es la base de la aplicación

Ver [01-arquitectura.md](01-arquitectura.md). Además:
- la contraseña de `ECOACCESS_BD` guardada en el archivo local de credenciales ya no es válida (alguien la cambió);
- por eso no se pudo leer el código de los objetos académicos originales; no se pidió la contraseña.

## H-14 · Script de integración desactualizado

`scripts/pruebas/Ejecutar-Integracion-Esquema.ps1` usa `docker exec oracle-free-p3` y el PDB `PRUEBAP3`. En este PC Oracle está instalado sin Docker, con el servicio `FREEPDB1`. No se modificó porque está fuera del alcance de esta fase.

Las pruebas de integración se ejecutaron directamente con `dotnet test`, apuntando al esquema aislado (ver [13-pruebas.md](13-pruebas.md)).

## H-15 · Visita del simulador abierta en Oracle Cloud

Durante esta auditoría el conteo pasó de 123 a 124 visitas y de 115 a 116 órdenes. La fila nueva es la visita **2410**:
- canal **3, simulador de escritorio**, sin cuenta (invitado);
- empezó el **18/11/2026 a las 17:41**, antes de esta fase;
- tiene una orden en estado 1, *emitida*.

No la creó ninguna prueba de base de datos: las que corren en Oracle Cloud no tienen `INSERT`. Viene del uso de la aplicación. Queda como está; el equipo decide si se cierra o se deja.

## H-16 · 58 depósitos verificados sin ecopuntos

Los 58 son del canal simulador y se verificaron el 09/11/2026, antes de que existiera la primera tarifa (11/11/2026). Sin tarifa vigente no hay puntos. **Es el resultado correcto.** Ver [12-ecopuntos.md](12-ecopuntos.md#por-qué-hoy-no-hay-ecopuntos).

## H-17 · Dos canecas DEMO aparecen como reales en `VW_CANECA_ESTACION`

La vista marca `ES_SINTETICO = 1` solo si el nombre de la caneca contiene «FICTIC» o el de la estación contiene «SIMULACI». Las canecas **289** («DEMO - Caneca verde») y **290** («DEMO - Caneca negra») están en la estación «DEMO - Bloque ficticio»: ni la caneca dice «ficticia» ni la estación dice «simulación». Por eso `ES_SINTETICO` sale 0. Lo detectó la verificación A2 (I6: 5 canecas «reales» en lugar de 3).

Las dos están FUERA DE SERVICIO, así que no afectan a la operación.

**Propuesta:** que la vista reconozca también `'DEMO%'` y `'%FICTIC%'` en el nombre de la estación, o mejor, una marca explícita de ensayo en `PUNTO_INSTALACION`, como ya se hace con `REGISTRO_SIMULACION`.
