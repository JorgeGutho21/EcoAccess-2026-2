# Unificación de EcoAccess P3 (17/11/2026)

Informe de la consolidación de los avances de **Programación de Computadores III** y **Base de Datos (profesor Amílkar)** en un solo proyecto. Todo lo que aparece como verificado se ejecutó de verdad el 17/11/2026; lo que no, figura como pendiente.

## 1. Versión oficial

| Elemento | Valor |
|---|---|
| Carpeta de trabajo | `Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\ProyectoDeAula_EcoAccess\EcoAccess-P3` (fuera de OneDrive) |
| Repositorio canónico | `github.com/JorgeGutho21/EcoAccess-2026-2` (privado). `EcoAccess-P3` es un nombre anterior del mismo repositorio: GitHub lo redirige |
| Rama estable | `main` (integración final por PR desde `integracion/ecoaccess-p3-unificado` y `integracion/oracle-academica-unificada`) |
| Solución principal | `EcoAccess.P3.sln` (7 proyectos de `src/`, incluido el instalador de migraciones, y 6 de `tests/`) |
| Base de datos oficial | Oracle Cloud, alias `ecoaccess_tp`, esquema `ECOACCESS_P3_APP` |

**Por qué fuera de OneDrive.** OneDrive bloquea archivos de `bin/` y `obj/` durante la compilación y no conserva `.git`. Las carpetas de OneDrive quedan como copia de lectura y GitHub como respaldo remoto, sin dos versiones activas.

**Rutas largas.** Algunos modelos de Data Modeler llegan a 218 caracteres dentro del repositorio, es decir, 345 caracteres en la carpeta de Programación III. Por eso:
- el repositorio usa `git config core.longpaths true`;
- la prueba de secretos lee esas rutas con el prefijo `\\?\`;
- para abrirlas desde el Explorador de Windows, un administrador puede activar «Habilitar rutas largas de Win32» (`LongPathsEnabled=1`). Es un ajuste del sistema y queda a decisión del propietario.

## 2. Auditoría de carpetas

Cada archivo se comparó por hash de contenido (blob de Git) con todo el historial de los repositorios.

| Carpeta | Estado | Contenido sin copia en Git antes de unificar | Decisión |
|---|---|---|---|
| `Documents\…\ProyectoDeAula_EcoAccess\EcoAccess_Version_Final` | clon en `feat/consulta-caneca-oracle`, 47 commits detrás de `main` | 3 cambios sin commit (`--resolver-caneca`, calibración) | trasladados a la integración (commit 979c28e) |
| OneDrive `…\BaseDeDatos_Amilkar` (26 elementos) | sin `.git`; 1.624 archivos idénticos a `main` | 13 archivos nuevos y 2 más recientes (carpeta 21, documento detallado y `.pbix`) | incorporados (commit 938f7a5) |
| OneDrive `…\BaseDeDatos_Amilkar\_LOCAL_NO_PUBLICAR` | privado: credenciales locales, Data Pump y bundles de respaldo | — | **no se publica**; queda solo en OneDrive |
| OneDrive `…\ProyectoDeAula_EcoAccess\EcoAccess-P3-LImpio` | instantánea del 06/11 sin `.git` | `Iniciar-Bot.ps1`, `Registrar-InicioAutomatico.ps1` (reemplazados por `Iniciar-Bot-Eco.ps1` e `Instalar-Arranque-Automatico.ps1`), un `.log` y un `.csproj.user` | nada que integrar |
| OneDrive `…\ProyectoDeAula_EcoAccess\EcoAccess_V2` | versión de octubre (repositorio `EcoAccess_P3_SUCIO`) | `ACCESOS_ECOACCESS.txt` (accesos privados), 2 PDF de referencia con derechos de autor y una versión antigua de `FormPilotoVisual` | **no se publica** (privado o de terceros) |
| OneDrive `…\ProyectoDeAula_EcoAccess\Rubrica_EcoAccess-Sucio-` | 16 documentos Word/PDF de la rúbrica | los 16 | incorporados en `docs/academico/programacion_iii/entregas_previas` |
| `Documents\…\Base de Datos-SS463-\EcoAccess-2026-2` | clon en `entrega/base-de-datos-amilkar-2026`, limpio | ninguno (PR #12 fusionado) | redundante |
| `Documents\…\Respaldo_EcoAccess_2026-11-16` | respaldos del 16/11 (originales y copia verificada) | cambios sin commit de `EcoAccess-P3-Nube` y `EcoAccess_V2` (versiones anteriores) | se conserva como respaldo |

## 3. GitHub

| Repositorio | Estado |
|---|---|
| `EcoAccess-2026-2` | **canónico**: 13 PR fusionados (#1–#13) y 22 ramas remotas |
| `EcoAccess_P3_SUCIO` | versión V2 de octubre; histórico, no se usa |
| `EcoAccess_P2`, `EcoAccess` | proyectos de semestres o asignaturas anteriores |

- **Identidad Git** en la carpeta oficial: `Jorge Gutiérrez Thomas <263990811+JorgeGutho21@users.noreply.github.com>`.
- La autoría de Heyder y Saray se conserva en su historial.
- El commit 4478100 de Heyder dejaba `main` abriendo su formulario de práctica en lugar de EcoAccess. Se corrigió sin borrar su formulario, que ahora se abre con `--laboratorio-propiedades`.

## 4. Oracle: una base oficial

### 4.1 Inventario real antes de la unificación (la sección 4.4 describe el estado final)

| | Oracle Cloud `ECOACCESS_P3_APP` (**oficial**) | Oracle FREE local `ECOACCESS_BD` (académico) |
|---|---|---|
| Servidor | Autonomous Database, servicio `…_ecoaccess_tp.adb.oraclecloud.com` | `localhost:1521/FREEPDB1` (servicio Windows `OracleServiceFREE`) |
| Tablas | 29 | 19 |
| Objetos PL/SQL | `FN_RESOLVER_CANECA_DISPONIBLE`, `PKG_ECOACCESS_STATS` (especificación y cuerpo), `PR_CONSULTAR_ESTADISTICAS`, trigger `TR_ECOP_UNIDADES` | `PKG_DEPOSITO`, `PKG_ECOPUNTOS`, `PKG_ESTADISTICAS`, `PKG_CONTEXTO_APP`, `FN_RESOLVER_CANECA_DISPONIBLE`, `FN_PESO_ESTIMADO_KG`, `FN_DEPOSITOS_CIUDADANO`, `PR_CAMBIAR_ESTADO_CONTENEDOR`, `PR_CONSULTAR_ESTADISTICAS` y los triggers `TRG_DEPOSITO_COHERENCIA`, `TRG_DEPOSITO_BLOQUEO_CERRADO`, `TRG_DEPOSITO_AUD_VERIFICACION`, `TRG_CONTENEDOR_AUD_ESTADO` |
| Vistas | 12 (`EA_R_*`, `VISTA_*`) | `VW_BI_*`, `VW_CANECA_ESTACION`, `VW_DEPOSITO_DETALLE`, `VW_RECORRIDO`, `VW_SALDO_ECOPUNTOS` |
| Índices / secuencias | 90 / 16 (identidades) | definidos en `04_indices.sql` / `05_secuencias.sql` |
| Estado | todos los objetos `VALID` | la cuenta de lectura ve 6 vistas; la contraseña de `ECOACCESS_BD` del archivo local ya no es válida (ORA-01017) |
| Datos (conteo exacto 17/11) | 113 depósitos, 76 usuarios, 115 órdenes de apertura, 41 materiales | sintéticos: 105 depósitos y 51 ciudadanos |
| Docker `localhost:1522/PRUEBAP3` | — | no está en ejecución; Docker Desktop no está instalado en este equipo |

### 4.2 Diferencias entre los dos esquemas

Se compararon columna por columna: `user_tab_columns` de la nube contra `BaseDeDatos_Amilkar/04_SCRIPTS_ORACLE/02_tablas.sql`.

| Tabla | Solo en el académico | Solo en la nube |
|---|---|---|
| `DEPOSITO` | `CANTIDAD_DECLARADA`, `ESTADO_VERIFICACION`, `ID_PESO_REFERENCIAL`, `INSTRUMENTO_MEDICION`, `MOTIVO_VERIFICACION`, `PRECISION_MEDICION_G` | — |
| `MOVIMIENTO_ECOPUNTOS` | `CLAVE_IDEMPOTENCIA`, `FECHA_UTC`, `ID_ACTOR`, `ID_MOVIMIENTO_REVERTIDO`, `MOTIVO` | `ID_OPERACION`, `ID_PREDECESOR`, `ID_CREDITO_REVERTIDO` (diseño distinto) |
| `CONSULTA_VISION` | `ESTADO_VISUAL`, `ES_SINTETICO`, `ID_CONTENEDOR_SUGERIDO`, `ID_INTERACCION`, `ID_PESO_REFERENCIAL`, `MODELO`, `PESO_ESTIMADO_KG`, `UNIDADES_ESTIMADAS` | `CONTEXTO`, `ID_CONTENEDOR` |
| `CONTENEDOR` / `CONTENEDOR_MATERIAL` | `CATEGORIA`, `IDENTIFICADOR_DISPOSITIVO` / `CATEGORIA` | — |
| `INTERACCION`, `PUNTO_INSTALACION`, `USUARIO`, `REGLA_ECOPUNTOS` | `ES_SINTETICO` (y `ID_PUNTO_INSTALACION`, `CODIGO_QR_ESTACION`) | — |
| `MATERIAL` | — | `PRECAUCIONES`, `RUTA_SEPARACION`, `SINONIMOS` |

- **Tablas que solo existen en el académico:** `AUDITORIA_DEPOSITO`, `HISTORIAL_ESTADO_CONTENEDOR` y `PESO_REFERENCIAL`.
- **Tablas que solo existen en la nube (operación del bot):** `ANALISIS_IMAGEN`, `CONFIGURACION_CANECA`, `CONVERSACION_TELEGRAM`, `UPDATE_TELEGRAM`, `TOKEN_VINCULACION`, `VINCULACION_PENDIENTE`, `OPERACION_ECOPUNTOS`, `OPINION`, `COMENTARIO_CIUDADANO`, `LOTE_SIMULACION`, `REGISTRO_SIMULACION`, `CONSENTIMIENTO_VISION_EXTERNA` y `ECO_MIGRACION_APLICADA`.

**Conclusión.** El esquema académico es una extensión docente del núcleo de la aplicación. Ninguna base sobrescribe a la otra.

### 4.3 Arquitectura acordada

- **Oracle Cloud** es la única base productiva. SQL Developer, Visual Studio, Telegram y Power BI de la aplicación se conectan a ella con el mismo wallet.
- **Oracle FREE local** queda para:
  - las prácticas de la asignatura (`ECOACCESS_BD`);
  - las pruebas de integración aisladas (`ECOACCESS_P3_PRUEBAS`).
- **Docker** pasa a ser opcional. La aplicación ya no lo comprueba salvo que `App.config` apunte a `localhost:1522`.
- **Operaciones de negocio:** no se duplican en C#. La aplicación llama a `FN_RESOLVER_CANECA_DISPONIBLE` y `PKG_ECOACCESS_STATS`; el resto de reglas vive en la lógica de negocio probada (2.170 pruebas) y en las restricciones del esquema oficial.

### 4.4 Capa académica aplicada en la base oficial (17/11/2026)

Con autorización del propietario, los objetos de la asignatura de Base de Datos se llevaron a Oracle Cloud **adaptados a las columnas reales**, sin crear un segundo libro de ecopuntos ni una segunda base:

| Paso | Resultado |
|---|---|
| Respaldo previo | DDL de 118 objetos y datos de las 29 tablas en `%LOCALAPPDATA%\EcoAccessP3\privado\respaldos\oracle_cloud_20261117_antes_V026_V027` (fuera de Git y de OneDrive) |
| Ensayo en el esquema aislado `ECOACCESS_P3_PRUEBAS` | V027 27/27 y suite de integración completa **214 superadas, 0 fallidas** con los disparadores activos |
| `V026__puntos_disposicion_valledupar` | aplicada: 6 puntos y programas verificados en `PUNTO_DISPOSICION` |
| `V027__capa_academica_bd` | aplicada; crea los objetos de la tabla siguiente |
| `V028__vista_bi_consultas_vision` | aplicada: códigos reales de resultado (1–10) en `VW_BI_CONSULTAS_VISION` |
| Registro | V026–V028 en `ECO_MIGRACION_APLICADA` con su huella SHA-256 (`.gitattributes` conserva sus bytes) |
| Verificación en la nube | todos los objetos `VALID`; V027 27/27 con `ROLLBACK` (0 filas residuales); estructura idéntica a la del esquema aislado (750 columnas, restricciones y objetos) |

| Objeto (V027) | Qué hace en la base oficial |
|---|---|
| `PKG_CONTEXTO_APP` | fija qué persona de la aplicación actúa (`fijar_actor`, `tiene_rol`, `exigir_rol`); lo leen los disparadores |
| `PKG_DEPOSITO` | `verificar` (y acredita en la misma transacción), `rechazar` (queda en `AUDITORIA_DEPOSITO`), `corregir_verificacion` y `estado` (0 pendiente, 1 verificado, 2 rechazado) |
| `PKG_ECOPUNTOS` | `regla_vigente`, `credito_vigente`, `acreditar`, `revertir`, `recalcular`, `saldo`, sobre **el mismo libro** de la aplicación (`OPERACION_ECOPUNTOS` + `MOVIMIENTO_ECOPUNTOS`) y con sus reglas: titular distinto del verificador y del actor, tarifa vigente, por unidad con conteo verificado, sin doble crédito |
| `PKG_ESTADISTICAS` | `consultar_resumen` y `ranking_verificados` con `SYS_REFCURSOR`, fechas en hora de Colombia |
| `FN_PESO_ESTIMADO_KG`, `FN_DEPOSITOS_CIUDADANO`, `PR_CAMBIAR_ESTADO_CONTENEDOR` | peso estimado con `PESO_REFERENCIAL` (19 objetos de referencia); historial de un ciudadano; cambio de estado de caneca solo por Administrador |
| `TRG_DEPOSITO_BLOQUEO_CERRADO` | un depósito verificado solo se cambia con `PKG_DEPOSITO.corregir_verificacion` (ORA-20103) |
| `TRG_DEPOSITO_COHERENCIA` | material declarado admitido por la caneca (ORA-20102), datos de origen inmutables (ORA-20109), verificador con rol Operario o Administrador (ORA-20104) |
| `TRG_DEPOSITO_AUD_VERIFICACION`, `TRG_CONTENEDOR_AUD_ESTADO` | auditoría en `AUDITORIA_DEPOSITO` e `HISTORIAL_ESTADO_CONTENEDOR` con el actor y el motivo |
| `VW_CANECA_ESTACION`, `VW_DEPOSITO_DETALLE`, `VW_SALDO_ECOPUNTOS`, `VW_RECORRIDO`, `VW_BI_*` | mismas columnas que en `ECOACCESS_BD`, sobre los datos reales; lectura para `ECOACCESS_ROL_CONSULTA`, ejecución de paquetes para `ECOACCESS_ROL_EDICION` |

Diferencias conscientes con el esquema académico: la base oficial no tiene `ESTADO_VERIFICACION` ni `MOTIVO_VERIFICACION` en `DEPOSITO`, así que el estado se deriva y el motivo queda en la auditoría; `PR_CONSULTAR_ESTADISTICAS` y `FN_RESOLVER_CANECA_DISPONIBLE` se conservan en su versión de V022. `ECOACCESS_BD` sigue en Oracle FREE local solo como copia de prácticas. Reversión: `database/06_migraciones/reversion/R027__capa_academica_bd.sql`.

**Instalador de migraciones.** `src/EcoAccess.P3.Instalador` (traído de la rama `recovery/ecoaccess-oracle-cloud`) aplica o verifica las migraciones en Oracle Cloud con el wallet, sin Docker: `EcoAccess.P3.Instalador.exe plan` muestra las 31 versiones aplicadas. Avisa que V001–V007 «cambiaron después de aplicarse» porque se aplicaron desde el repositorio anterior `EcoAccess-P3-Nube`; la estructura resultante es idéntica.

**Ecopuntos en 0: causa.** Los 10 depósitos reales por Telegram son de visitantes **sin cuenta vinculada** (`VINCULO_TELEGRAM` tiene 0 filas): por diseño no generan puntos. Los 58 verificados con cuenta son de ensayo y nadie los acreditó. Para sumar puntos reales, el ciudadano se registra y vincula su Telegram (página de registro del bot), deposita, y un Operario o Administrador verifica el depósito.

## 5. Power BI

| Informe | Fuente | Verificación |
|---|---|---|
| `reportes/powerbi/EcoAccess.pbix` (aplicación) | Oracle Cloud `ecoaccess_tp` (Import), vistas `VISTA_DEPOSITOS_ESTADO`, `EA_R_*`, `VISTA_CANECAS_ESTACION`, `VISTA_LOTES_SIMULACION` | `reportes/powerbi/Validacion.sql` ejecutado contra la nube el 17/11 (sección 7) |
| `BaseDeDatos_Amilkar/09_POWER_BI` (académico) | **Oracle Cloud** desde el 17/11: el PBIP usa `ServidorOracle = ecoaccess_tp` y el esquema `ECOACCESS_P3_APP` (vistas `VW_BI_*` de V027, mismas columnas y estados que en `ECOACCESS_BD`); queda la opción CSV | `DATOS_Y_CONSULTAS/validacion_nube.sql`: 10 depósitos reales pendientes, 103 de ensayo (58 verificados, 125 unidades), 74 ciudadanos (3 reales, 71 de ensayo), 34 consultas de visión identificadas |

Refrescar los `.pbix` exige Power BI Desktop con la credencial guardada del propietario, así que **no se hizo en esta sesión**. Para el académico: abrir `PROYECTO_PBIP/EcoAccess_BD/EcoAccess_BD.pbip`, escribir la credencial de `ECOACCESS_P3_APP` (Base de datos), pulsar Actualizar y guardar como `ARCHIVO_PBIX/EcoAccess_BD.pbix`. Cada tarjeta debe coincidir con `validacion_nube.sql`.

## 6. Telegram, IA y tres canecas

| Punto | Estado | Evidencia |
|---|---|---|
| Bot en marcha | verificado: **un solo** proceso `--polling --sin-dispositivo` desde `EcoAccess-P3`, `@EcoAccessP3_JG_bot` comprobado, página de registro en 8081; la tarea «EcoAccess P3 - vigilante del bot» apunta a la carpeta oficial | `C:\ProgramData\EcoAccessP3Framework\Sesiones\20261117-193033` |
| QR único `EcoAccessCanecaPiloto1` y presencia de 5 min | verificado por software | pruebas `QrOficialTests`; código en `ServicioConversacionTelegram.FlujoEco.cs` |
| Clasificación y caneca por material | verificado en Oracle Cloud | `--resolver-caneca 1015 9` → 1093 (blanca, PET); `1015 211` → 1400 (verde, restos de alimentos); `1015 3` → 1401 (negra, servilleta usada); material inexistente → ORA-20811 |
| Apertura automática de 10 s, sin confirmación | verificado por software y registrado en Oracle | 6 órdenes del 17/11 (n.º 1704–1709, caneca blanca 1093, 15:35–15:52 hora de Colombia), cada una con eventos *recibido → aplicado → completado* y cierre unos 15 s después |
| Movimiento físico de las tapas | observado por el propietario el 17/11 (blanca y verde a 90°; la negra a 90° por decisión del propietario, con observación de que podría forzar) | `docs/evidencias/esp32_fisico/movimientos_servos.csv`. **En esta sesión no se ejecutaron pruebas físicas** |
| Verde y negra abiertas desde Telegram | **pendiente** (requiere la estación física: `scripts\desarrollo\Iniciar-Estacion-Fisica.ps1`) | solo hay órdenes de Telegram para la blanca |
| Residuos especiales sin apertura y punto de entrega | verificado por software; `PUNTO_DISPOSICION` (V026) ya está en la nube | pruebas de catálogo y `PoliticaAperturaAutomatica` |

## 7. Pruebas del 17/11/2026

| Prueba | Tipo | Resultado |
|---|---|---|
| Compilación Release de `EcoAccess.P3.sln` (7 proyectos de `src/` con el instalador) | real | 0 errores |
| Unitarias `EcoAccess.P3.Tests` | real | 2.170 superadas, 0 fallidas, 1 omitida (enlace simbólico) |
| Integración Oracle en el esquema aislado `ECOACCESS_P3_PRUEBAS` (nunca producción), antes y **después** de V027 | real | 214 superadas, 0 fallidas, 7 omitidas en ambas ejecuciones · `integracion.txt`, `integracion_con_v027.txt` |
| Capa académica V027 (27 comprobaciones: verificar, acreditar, idempotencia, bloqueo, corrección, rechazo, auditoría, estadísticas, vistas, peso) | real, esquema aislado y **Oracle Cloud** con `ROLLBACK` | 27/27 en ambos; 0 filas residuales en la nube · `v027_pruebas_oracle_cloud.txt` |
| Instalador de migraciones y wallet | real | 22/22 pruebas; `plan` en la nube: 31 versiones aplicadas, 0 pendientes |
| Estructura nube contra esquema aislado | real, solo lectura | idéntica (750 columnas, restricciones y objetos) |
| Concurrencia de usuarios simulados (registro, página y chat) | real, esquema aislado | 1, 10, 20 y 30 personas a la vez: 0 errores (10 nuevas en 6,7 s; 30 nuevas en 13,9 s) · `carga/*.json` |
| Inicio de la aplicación contra Oracle Cloud y acceso directo | real | «EcoAccess · Bienvenida»; sin contraseña guardada, «Configuración inicial» sin pasos de Docker; el acceso abre la app instalada sin consola |
| Validación SQL de ambos Power BI | real, solo lectura | sección 5 |
| Ecopuntos | real | 0 movimientos reales; causa explicada en 4.4. PKG_ECOPUNTOS acredita, revierte y corrige correctamente en la prueba con `ROLLBACK` |
| Power BI Desktop (refresco de los `.pbix`) | **pendiente** | requiere la credencial del propietario en Power BI |
| Instalación en un segundo computador | **pendiente** | — |
| Pruebas físicas del ESP32 | **no ejecutadas** en esta etapa | — |

Al compilar con el bot en marcha, MSBuild no puede sobrescribir `src\*\bin\Release` (archivos bloqueados): hay que detener el bot (`scripts\desarrollo\Detener-Demostracion.ps1` o el archivo `pausa-vigilante`) antes de compilar y volver a iniciarlo después.

## 8. Limpieza realizada (17/11/2026, con autorización; nada se borró)

| Carpeta anterior | Ahora | Antes de moverla |
|---|---|---|
| `Documents\…\ProyectoDeAula_EcoAccess\EcoAccess_Version_Final` | `Documents\…\Respaldo_EcoAccess_2026-11-16\Retirado_20261117\EcoAccess_Version_Final` | bot detenido y tarea del vigilante repuntada a `EcoAccess-P3`; `hardware/estacion_tres_canecas/configuracion_privada.h` (claves HMAC del firmware, ignorado por Git) copiado idéntico a `EcoAccess-P3`; guía maestra integrada desde su rama |
| `Documents\…\Base de Datos-SS463-\EcoAccess-2026-2` | `…\Retirado_20261117\EcoAccess-2026-2_clon_BaseDeDatos` | clon limpio de una rama ya fusionada |
| OneDrive `ProyectoDeAula_EcoAccess\EcoAccess_V2` y `Rubrica_EcoAccess-Sucio-` | OneDrive `Programación de Computadores III-SS462-\_ARCHIVO_ECOACCESS_20261117` | código ya en Git; rúbrica en `docs/academico` |
| OneDrive `Base de Datos-SS463-\BaseDeDatos_Amilkar` (con `_LOCAL_NO_PUBLICAR`) | OneDrive `Base de Datos-SS463-\_ARCHIVO_ECOACCESS_20261117\BaseDeDatos_Amilkar` | contenido ya en `EcoAccess-P3\BaseDeDatos_Amilkar`; `_LOCAL_NO_PUBLICAR` se conserva intacto |
| OneDrive `ProyectoDeAula_EcoAccess\EcoAccess-P3-LImpio` | **sin mover** | Visual Studio la tiene abierta (`EcoAccess-P3-LImpio\EcoAccess.P3.sln`). Al cerrarla se puede mover a `_ARCHIVO_ECOACCESS_20261117` |

En OneDrive quedan `LEEME_CARPETA_OFICIAL.txt` y `LEEME_ECOACCESS_UNIFICADO.txt` con la ruta de la carpeta oficial. El acceso directo anterior «Iniciar EcoAccess P3» está en `%LOCALAPPDATA%\EcoAccessP3\respaldo_accesos_directos`.

Ramas: todas las ramas remotas con trabajo propio quedaron integradas (`docs/guia-maestra-ecoaccess-v1` por merge, `recovery/ecoaccess-oracle-cloud` por sus archivos nuevos). El único commit sin integrar de `desarrollo/auditoria-tercer-corte`, `desarrollo/mision-nocturna-ia` y `recovery` («redactar las notas de autoría…», 8 líneas) solo cambiaba dos Word/PDF de `docs/entrega/academica` que `main` ya reemplazó por versiones posteriores. Las ramas y los PR antiguos se conservan como historial.
