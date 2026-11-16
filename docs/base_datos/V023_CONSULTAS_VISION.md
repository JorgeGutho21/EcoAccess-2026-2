# V023 — consultas de visión (10/11/2026)

**Estado:**
- **Oracle Cloud:** APLICADA el 10/11/2026 a las 14:40 (hora de Colombia), con la autorización del propietario. Ver «Aplicación en Oracle Cloud».
- **Esquema aislado:** VERIFICADA en `ECOACCESS_P3_PRUEBAS`, en el Oracle FREE local (10/11/2026).
  - Se instaló el modelo completo, con V021 y V022 idénticas a las de la nube (mismas huellas SHA-256).
  - Ciclo probado: V023 aplicada, ejecutada otra vez sin cambios, revertida con R023, reaplicada y ejecutada otra vez sin cambios.

## Aplicación en Oracle Cloud (10/11/2026)

- **Cómo se ejecutó:** SQL*Plus con el wallet, como `ECOACCESS_P3_APP`; la contraseña se pasó por la entrada estándar.
  - Duración: 1 407 ms.
  - Quedó registrada en `ECO_MIGRACION_APLICADA` como V023 `EJECUTADA`, con huella `cb84848225f1…`, la misma del ensayo aislado.
- **Huella antes y después** (`database/05_pruebas/05_instantanea_datos_protegidos.sql`):
  - **Sin cambios** en conteo ni en `ORA_ROWSCN` de las 13 tablas protegidas: usuario (21), credencial (20), rol (21), vínculo de Telegram (0), interacción (0), orden (0), depósito (0), regla de ecopuntos (0), movimiento de ecopuntos (0), material (41), caneca (4), materiales por caneca (30) y punto de instalación (2).
  - Solo se sumó lo de V023: tablas 26→27, vistas 9→10, índices 82→86, secuencias 15→16 (identidad), restricciones 323→342 (11 con nombre y 8 NOT NULL de sistema).
  - Objetos inválidos: 0 antes y 0 después. Las 3 FK quedaron con `SET NULL`.
- **Bot:** se recompiló en Release y se reinició con el vigilante, pausado durante la compilación para que no relanzara el binario anterior.
  - El registro de la sesión `Sesiones/20261110-144112` dice: «Consultas de fotos: se anotan en Oracle (CONSULTA_VISION), sin guardar la foto».
  - Un solo proceso `--polling`. Página de registro: HTTP 400 sin token, en local y en la pública, que es lo esperado.
- **Pruebas contra la nube después de aplicar**
  - `ConsultasVisionOracleTests` y el recorrido completo pasaron (2 de 2). El recorrido dejó `ConsultaVisionPersistida: true` en `docs/pruebas/resultados_e2e/recorrido_completo.json`.
  - Las dos limpiaron lo suyo: 0 consultas de prueba restantes y los mismos 21 usuarios.
- **Si hiciera falta revertir:** `database/06_migraciones/reversion/R023__consulta_vision.sql`, ensayada en el esquema aislado.

## Resultados del esquema aislado (10/11/2026)

| Prueba | Resultado |
|---|---|
| Instalación V002–V022 + V023 (más catálogos y fixtures) | Correcta, 0 objetos inválidos |
| Huellas de V020, V021 y V022 contra Oracle Cloud | Idénticas (`87237853bfc2…`, `91d88156d2c0…`, `59610c700dc0…`) |
| V023, primera ejecución | Crea la tabla, 2 índices y la vista |
| V023, segunda ejecución | Código 0; «ya existía … no se modificó» para tabla e índices |
| Estructura creada | 11 restricciones con nombre (FK con `SET NULL`) + 8 NOT NULL de sistema; 4 índices (PK, UNIQUE y los 2 de V023) |
| Objetos de V022 tras V023 | `PKG_ECOACCESS_STATS`, `FN_RESOLVER_CANECA_DISPONIBLE`, `PR_CONSULTAR_ESTADISTICAS` y `VISTA_CANECAS_ESTACION` siguen válidos |
| Reversión R023 | Quita vista, tabla y registro; 0 objetos inválidos; reaplicar V023 después funciona |
| `ConsultasVisionOracleTests` | Correcta: sin duplicado al reintentar; 0 interacciones, órdenes, depósitos o ecopuntos nuevos; aparece en el agregado y en la vista; queda anónima al borrar a la persona |
| Prueba de PAPEL (`FotoVisionReal_VinculaContratoSeguro_…`) | Correcta, con la tarifa ficticia del esquema aislado. Confirma que en la nube fallaba por datos de prueba |
| Recorrido completo del ciudadano | Correcto |
| Suite de integración completa | 210 correctas, 1 fallida, 4 omitidas a propósito (de 215) |
| Dashboard de escritorio contra el esquema aislado (3 consultas de muestra, borradas después) | Muestra 3 fotos, 2 con destino, 1 «pidió otra foto» y 1,5 s; los depósitos siguen en 0. Captura en `docs/evidencias/winforms/15_dashboard_fotos_echo_esquema_aislado_1366x768.png` |

### Fallo abierto, ajeno a V023

Las pruebas del Bridge (`BridgeOracleTests`) fallan de forma intermitente contra el Oracle local con «La solicitud venció». Cada vez falla una prueba distinta, entre 0 y 4 por corrida.
- Cada una pasa sola (3 de 3).
- El fallo se repite **con V023 revertida** (2 fallos, y luego 0, en dos corridas), así que no lo causa V023.
- En Oracle Cloud esas pruebas pasaron. Allí la latencia es mayor y la base trabaja en UTC; la local trabaja en −05:00.
- No se encontró ningún proceso que marque solicitudes como vencidas. La hora se guarda y se lee sin desfase: 2000 idas y vueltas sin lecturas en el futuro.
- Falta instrumentar esas pruebas para hallar la causa. **PENDIENTE**.

## 1. Qué cambia, exactamente

Archivo: `database/06_migraciones/V023__consulta_vision.sql`.

| Tipo | Objeto | Detalle |
|---|---|---|
| Tabla nueva | `CONSULTA_VISION` | Ver columnas abajo |
| Restricción | `PK_CONSULTA_VISION` | Clave primaria (`id_consulta`) |
| Restricción | `UQ_CONSULTA_VISION_CLAVE` | Única: la misma actualización de Telegram no crea dos filas |
| FK | `FK_CONSULTA_VISION_USUARIO` → `usuario` | `ON DELETE SET NULL` |
| FK | `FK_CONSULTA_VISION_MATERIAL` → `material` | `ON DELETE SET NULL` |
| FK | `FK_CONSULTA_VISION_CONTENEDOR` → `contenedor` | `ON DELETE SET NULL` |
| CHECK | `CK_CONSULTA_VISION_CANAL` | 1–3 (`CanalConsultaVision`) |
| CHECK | `CK_CONSULTA_VISION_MODO` | 1–2 (`ModoConsultaVision`) |
| CHECK | `CK_CONSULTA_VISION_CONTEXTO` | Nulo o 0–10 (`ContextoObjeto`) |
| CHECK | `CK_CONSULTA_VISION_RESULTADO` | 1–10 (`ResultadoClasificacion`) |
| CHECK | `CK_CONSULTA_VISION_ADMISION` | Nulo o 1–4 (`AdmisionMaterial`) |
| CHECK | `CK_CONSULTA_VISION_LATENCIA` | ≥ 0 |
| Índice | `IX_CONSULTA_VISION_FECHA` | `(fecha_utc)` |
| Índice | `IX_CONSULTA_VISION_USUARIO` | `(id_usuario, fecha_utc)` |
| Vista nueva | `EA_R_CONSULTAS_VISION` | Agregado diario en hora de Colombia, sin datos personales |

**Columnas de `CONSULTA_VISION`**

| Columna | Contenido |
|---|---|
| `id_consulta` | Identidad |
| `clave` | Única; por ejemplo `telegram:<update_id>` |
| `id_usuario` | Nulo si la persona no está vinculada |
| `canal` | Por dónde llegó la foto |
| `modo` | Solo software o con dispositivo |
| `proveedor` | Modelo que la atendió |
| `clave_objeto` | Objeto reconocido |
| `contexto` | Tipo de objeto |
| `resultado` | Resultado de la clasificación |
| `id_material` | Material identificado |
| `admision` | Si la caneca lo admite |
| `id_contenedor` | Caneca consultada |
| `latencia_ms` | Tiempo de respuesta |
| `fecha_utc` | Momento de la consulta |

No se guarda la foto ni el identificador de Telegram.

**Lo que no hace:**
- Ningún `INSERT`, `UPDATE`, `DELETE`, `ALTER` ni `DROP` sobre objetos existentes.
- Ningún `GRANT`.
- Ningún trigger.
- No toca ciudadanos, credenciales, roles, vinculaciones, solicitudes, depósitos, ecopuntos, catálogo ni canecas. Las tres FK salen de la tabla nueva: no agregan columnas ni restricciones a las tablas padre.

**Repetible:**
- La tabla y cada índice solo se crean si faltan.
- La vista se reemplaza por la misma definición.
- Si existe una `CONSULTA_VISION` con otra estructura, se detiene sin tocarla (`-20233`).

### Cambios hechos tras la revisión (antes de aplicarla)

1. **`ON DELETE SET NULL` en las tres FK.** Las limpiezas de pruebas de P3 y de V2 borran usuarios, materiales y canecas ficticios (`BaseIntegracionOracle`, `database/05_pruebas/03_*`, `04_*`). Con una FK simple, una consulta de una persona de prueba bloquearía esa limpieza en V2 (ORA-02292). Ahora la consulta queda anónima y el agregado se conserva.
2. **Segunda ejecución sin error.** Antes, la segunda ejecución terminaba con el error `-20232`. Ahora termina sin error y sin crear nada.

## 2. Compatibilidad

| Con | Resultado de la revisión |
|---|---|
| V021 (V2: `configuracion_caneca`, `vista_canecas_estacion`) | Sin nombres en común; V023 no la lee ni la modifica |
| V022 (V2: `PKG_ECOACCESS_STATS`, `FN_RESOLVER_CANECA_DISPONIBLE`, `PR_CONSULTAR_ESTADISTICAS`) | Sin nombres en común; V022 no referencia `consulta_vision`; no invalida esos objetos (no cambia tablas que usan) |
| Oracle Cloud hoy (consulta de solo lectura, 10/11/2026) | V018–V022 registradas; ningún objeto con nombre de V023; 0 objetos inválidos; privilegios CREATE TABLE, VIEW y SEQUENCE presentes; 15 columnas de identidad ya en uso; cuota 6,6 MB de 2 048 MB |
| Código de P3 | Escribe con un servicio que solo se activa si la tabla existe; lee con métodos que devuelven «pendiente» (`null`) ante ORA-00942 |
| Código de V2 | No conoce la tabla; sus limpiezas ya no pueden quedar bloqueadas (punto 1 de arriba) |
| Valores de los CHECK | Coinciden con los enumerados de C# (`ContextoObjeto` 0–10, `ResultadoClasificacion` 1–10, `AdmisionMaterial` 1–4) |

## 3. Respaldo y recuperación

V023 no modifica objetos existentes, así que el respaldo consiste en demostrar que nada existente cambió y tener la vuelta atrás lista:

1. **Huella antes**
   - Ejecutar `database/05_pruebas/05_instantanea_datos_protegidos.sql`, que es de solo lectura.
   - Mide conteo y `ORA_ROWSCN` máximo de usuario, credencial, rol, vínculo de Telegram, interacción, orden, depósito, regla y movimiento de ecopuntos, material, caneca, materiales por caneca y punto de instalación, además del inventario de objetos y restricciones.
   - Línea base de Oracle Cloud tomada el 10/11/2026:

     | Tabla | Filas |
     |---|---|
     | Usuarios | 21 |
     | Credenciales | 20 |
     | Roles asignados | 21 |
     | Vinculaciones | 0 |
     | Interacciones | 0 |
     | Órdenes | 0 |
     | Depósitos | 0 |
     | Reglas de ecopuntos | 0 |
     | Movimientos de ecopuntos | 0 |
     | Materiales | 41 |
     | Canecas | 4 |
     | Materiales por caneca | 30 |
     | Puntos de instalación | 2 |

     Objetos: 26 tablas, 9 vistas, 82 índices, 15 secuencias y 323 restricciones.
2. **Aplicar V023** con el registro de `ECO_MIGRACION_APLICADA`.
3. **Huella después**
   - Mismos conteos y SCN en esas tablas.
   - Objetos: +1 tabla, +1 vista y +4 índices: los 2 de V023, el de la PK y el de la UNIQUE.
   - Restricciones: +11 con nombre (PK, UNIQUE, 3 FK y 6 CHECK) más los NOT NULL de sistema que Oracle crea por columna obligatoria. El total exacto se mide en el esquema aislado.
4. **Si algo falla o se decide volver atrás**
   - Ejecutar `database/06_migraciones/reversion/R023__consulta_vision.sql`.
   - Quita la vista, la tabla y su registro de versión, y nada más. Se niega a borrar si otra tabla llegara a referenciar `CONSULTA_VISION`.
   - Antes de revertir, si se quieren conservar las consultas: `SELECT * FROM consulta_vision` exportado a CSV desde SQL Developer.
5. **Respaldo de plataforma:** Oracle Autonomous Database guarda copias automáticas y permite restaurar a un punto en el tiempo. No se comprobó desde aquí, porque requiere la consola de Oracle Cloud.

## 4. Esquema aislado

El ensayo previo va en un esquema que no es el de la aplicación:
- `ECOACCESS_P3_PRUEBAS` en el Oracle Database FREE instalado en este PC (`localhost:1521/freepdb1`, servicio en marcha).
- El entorno anterior (contenedor Docker `oracle-free-p3`) ya no existe: Docker no está instalado.

Crear ese esquema exige la contraseña de SYSTEM local. No está en el proyecto, y la autenticación del sistema operativo no está habilitada (habilitarla sería cambiar la seguridad de Windows). Por eso:

1. **El propietario ejecuta, una sola vez:** `scripts/pruebas/Crear-Esquema-Aislado-Local.ps1`.
   - Pide la contraseña sin mostrarla y la pasa a SQL*Plus por la entrada estándar, sin guardarla.
   - Crea el esquema con contraseña aleatoria.
   - Guarda solo esa conexión de pruebas en el perfil privado de Windows.
2. **Luego, sin SYSTEM**, se ejecuta `scripts/pruebas/Instalar-Esquema-Aislado-Local.ps1`. Instala tablas, catálogos y V002–V020 de P3, V021 y V022 de V2, V023 y el catálogo ambiental.
3. **Segunda ejecución:** con `-RepetirMigracion` vuelve a ejecutar V023 para demostrar que es repetible.
4. **Integración contra ese esquema:** `ConsultasVisionOracleTests` comprueba que:
   - la clave repetida no duplica;
   - no se crean solicitudes, depósitos, órdenes ni ecopuntos;
   - aparece en los agregados y en la vista;
   - borrar a la persona deja la fila anónima.

   Además corre la suite completa, incluida la prueba de PAPEL con su tarifa ficticia, que solo se crea en el esquema aislado.

Los resultados se agregan en la sección 6 cuando se ejecuten.

## 5. La prueba de puntos de PAPEL

`FotoVisionReal_VinculaContratoSeguro_…` falla en Oracle Cloud porque busca una tarifa vigente de ecopuntos para PAPEL.

- **Es un problema de datos de prueba.**
  - La prueba (02/11/2026) se escribió para el entorno aislado.
  - Su fixture `database/03_datos_prueba/05_tarifas_piloto_simulado.sql` se niega a ejecutarse fuera de él: «Las tarifas ficticias solo se preparan en el esquema aislado».
  - No se agregó ninguna tarifa a Oracle Cloud.
- **Detrás hay una decisión de negocio real.**
  - Oracle Cloud no tiene **ninguna** regla de ecopuntos: 0 en total, para cualquier material.
  - Hoy un depósito verificado no podría acreditar puntos. La aplicación lo rechaza con «No hay una regla vigente…», no inventa puntos.
  - En modo software no hay depósitos, así que no afecta lo que ya funciona.
  - Antes de la prueba física, el equipo debe decidir la tarifa real de cada material (por ejemplo con el docente). Eso no se resuelve con una tarifa ficticia.
- **Cambio en los fixtures**
  - Los fixtures `04_piloto_simulado.sql` y `05_tarifas_piloto_simulado.sql` aceptan ahora el PDB `PRUEBAP3` (Docker) **o** un esquema cuyo nombre empiece por `ECOACCESS_P3_PRUEBAS` (Oracle FREE local).
  - Siguen rechazando Oracle Cloud (`ECOACCESS_P3_APP`).

## 6. Pruebas

| Prueba | Resultado | Estado |
|---|---|---|
| Unitarias (incluye 4 nuevas del agregado del tablero y 4 de consultas en Telegram) | 1685/1686, 1 omitida a propósito | VERIFICADO |
| `ConsultasVisionOracleTests` contra Oracle Cloud sin V023 | Inconclusa a propósito: no escribe nada | VERIFICADO |
| V023 dos veces en el esquema aislado | Correcta (ver arriba) | VERIFICADO |
| `ConsultasVisionOracleTests` en el esquema aislado | Correcta | VERIFICADO |
| Suite de integración completa en el esquema aislado | 210/215; 1 fallo intermitente del Bridge, ajeno a V023 | VERIFICADO con fallo abierto |
| Huella después de aplicar en la nube | Solo tras la autorización final | PENDIENTE |

## 7. Quién lee las nuevas estadísticas

| Interfaz | Cómo | Estado |
|---|---|---|
| Telegram, «Mi actividad» | `ResumirDeUsuarioAsync`: solo las consultas propias | Pruebas unitarias; contra Oracle, pendiente del esquema aislado |
| Windows Forms, dashboard institucional | Nueva fila «Fotos consultadas a Echo (no son depósitos)» con total, con destino, «pidió otra foto» y tiempo medio. Sin V023 muestra «Pendiente: migración V023 sin aplicar». | VERIFICADO contra Oracle (esquema aislado), con captura |
| Power BI | Tabla `ConsultasVision` y cuatro medidas en la carpeta «Echo» (repositorio V2). Sin la vista queda vacía en vez de romper la actualización. | IMPLEMENTADO SIN VALIDACIÓN: falta abrirlo en Power BI Desktop |

## 8. Riesgos

| Riesgo | Mitigación |
|---|---|
| Una limpieza de V2 bloqueada por una FK | `ON DELETE SET NULL` |
| Ejecutarla dos veces | Repetible: no crea nada la segunda vez |
| Que la vista sea lenta con mucho volumen | Índice por fecha; el agregado es diario |
| Crecimiento de la tabla | Una fila por foto, unos cientos de bytes; con la cuota actual (2 GB) no es un problema a la escala del proyecto |
| Una consulta confundida con un depósito | Tabla, vista, tarjetas y medidas separadas; la prueba de integración comprueba que no se crean interacciones, órdenes, depósitos ni movimientos |
| Datos personales | Solo `id_usuario` (para «Mi actividad»); la vista y Power BI no lo exponen |
| La FK a usuario permite saber quién consultó | Mismo nivel de acceso que el resto del esquema; solo administración ve agregados y cada persona ve los suyos |
