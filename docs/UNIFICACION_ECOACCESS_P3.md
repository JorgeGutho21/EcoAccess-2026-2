# Unificación de EcoAccess P3 (17/11/2026)

Informe de la consolidación de los avances de **Programación de Computadores III** y **Base de Datos (profesor Amílkar)** en un solo proyecto. Todo lo que aparece como verificado se ejecutó de verdad el 17/11/2026; lo que no, figura como pendiente.

## 1. Versión oficial

| Elemento | Valor |
|---|---|
| Carpeta de trabajo | `Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\ProyectoDeAula_EcoAccess\EcoAccess-P3` (fuera de OneDrive) |
| Repositorio canónico | `github.com/JorgeGutho21/EcoAccess-2026-2` (privado). `EcoAccess-P3` es un nombre anterior del mismo repositorio: GitHub lo redirige |
| Rama estable | `main` · rama de integración: `integracion/ecoaccess-p3-unificado` |
| Solución principal | `EcoAccess.P3.sln` (6 proyectos de `src/` y 6 de `tests/`) |
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

### 4.1 Inventario real

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

### 4.4 Llevar los objetos académicos a la base oficial (pendiente de autorización)

Aplicar `PKG_DEPOSITO`, `PKG_ECOPUNTOS`, los triggers de `DEPOSITO` y las columnas nuevas cambia el esquema productivo que hoy usa el bot. No se aplicó sin autorización. Plan propuesto, en orden y sin borrar nada:

1. **Respaldo** del esquema `ECOACCESS_P3_APP` con `DBMS_DATAPUMP` (export a `DATA_PUMP_DIR` de Autonomous) y verificación del archivo.
2. **Migración aditiva `V0xx__extension_academica.sql`:**
   - tablas `PESO_REFERENCIAL`, `AUDITORIA_DEPOSITO` e `HISTORIAL_ESTADO_CONTENEDOR`;
   - columnas académicas **nulables** en `DEPOSITO` y `CONSULTA_VISION`;
   - vistas `VW_BI_*` reescritas sobre las columnas reales de la nube.
   - Sin `DROP` ni `UPDATE` masivos.
3. **Paquetes:**
   - `PKG_CONTEXTO_APP`, `PKG_ESTADISTICAS`, `FN_PESO_ESTIMADO_KG` y `FN_DEPOSITOS_CIUDADANO` (solo lectura) primero;
   - `PKG_DEPOSITO` y `PKG_ECOPUNTOS` después, adaptados al diseño de ecopuntos de la nube (`OPERACION_ECOPUNTOS`).
4. **Triggers de auditoría** (`TRG_*_AUD_*`) antes que los de bloqueo. `TRG_DEPOSITO_BLOQUEO_CERRADO` y `TRG_DEPOSITO_COHERENCIA` se prueban antes en `ECOACCESS_P3_PRUEBAS` con las 2.170 pruebas unitarias y las de integración, porque pueden rechazar inserciones del bot.
5. **Power BI académico:** apuntar `ServidorOracle` a `ecoaccess_tp` y comparar las tarjetas con `Validacion.sql`.

## 5. Power BI

| Informe | Fuente | Verificación |
|---|---|---|
| `reportes/powerbi/EcoAccess.pbix` (aplicación) | Oracle Cloud `ecoaccess_tp` (Import), vistas `VISTA_DEPOSITOS_ESTADO`, `EA_R_*`, `VISTA_CANECAS_ESTACION`, `VISTA_LOTES_SIMULACION` | `Validacion.sql` ejecutado contra la nube el 17/11 (sección 7). La actualización del `.pbix` se hace en Power BI Desktop con la credencial guardada del propietario: **no se refrescó en esta sesión** |
| `BaseDeDatos_Amilkar/09_POWER_BI/ARCHIVO_PBIX/EcoAccess_BD.pbix` (académico) | `ECOACCESS_BD` local (`VW_BI_*`) o CSV | vistas legibles: `VW_BI_DEPOSITOS` 105 filas, `VW_BI_CIUDADANOS` 51 y `VW_BI_ECOPUNTOS` 48. Pasarlo a la nube depende del punto 4.4 |

## 6. Telegram, IA y tres canecas

| Punto | Estado | Evidencia |
|---|---|---|
| QR único `EcoAccessCanecaPiloto1` y presencia de 5 min | verificado por software | pruebas `QrOficialTests`; código en `ServicioConversacionTelegram.FlujoEco.cs` |
| Clasificación y caneca por material | verificado en Oracle Cloud | `--resolver-caneca 1015 9` → 1093 (blanca, botella PET); `1015 211` → 1400 (verde, restos de alimentos); material inexistente → ORA-20811 controlado |
| Apertura automática de 10 s, sin confirmación | verificado por software y registrado en Oracle | 6 órdenes del 17/11 (n.º 1704–1709, caneca blanca 1093, 15:35–15:52 hora de Colombia), cada una con eventos *recibido → aplicado → completado* y cierre unos 15 s después |
| Movimiento físico de las tapas | verificado por observación del propietario el 17/11 (blanca y verde a 90°; la negra a 90° por decisión del propietario, con observación de que podría forzar) | `docs/evidencias/esp32_fisico/movimientos_servos.csv`. **En esta sesión no se ejecutaron pruebas físicas** |
| Verde y negra abiertas desde Telegram | **pendiente** | solo hay órdenes de Telegram para la blanca |
| Residuos especiales (pilas, cargadores) sin apertura | verificado por software | pruebas de catálogo y política (`PoliticaAperturaAutomatica`) |

## 7. Pruebas del 17/11/2026

| Prueba | Tipo | Resultado |
|---|---|---|
| Compilación Release de `EcoAccess.P3.sln` | real | 0 errores (46 avisos de estilo MSTest) |
| Unitarias `EcoAccess.P3.Tests` | real | 2.170 superadas, 0 fallidas, 1 omitida (enlace simbólico) |
| Integración Oracle en esquema aislado `ECOACCESS_P3_PRUEBAS` (FREEPDB1), con concurrencia y carga | real | ver `docs/evidencias/unificacion_20261117/integracion.txt` |
| Inicio de la aplicación contra Oracle Cloud | real | abre «EcoAccess · Bienvenida» |
| Inicio sin contraseña guardada | real | «Configuración inicial»: wallet instalado y base accesible, sin pasos de Docker |
| Instalador y acceso directo «EcoAccess P3» | real | 5/5 comprobaciones; el acceso abre la app instalada sin consola |
| Instalación del wallet (`sqlnet.ora`) | real con un zip de prueba sin claves | ruta corregida |
| `FN_RESOLVER_CANECA_DISPONIBLE` | real, Oracle Cloud, solo lectura | correcta |
| Validación de Power BI de la aplicación | real, SQL de solo lectura | depósitos operativos 10, de ensayo 103 (58 verificados, 125 unidades); solicitudes 16/107; opiniones 3 (promedio 3,67); materiales 11/14/2/14 por categoría |
| Ecopuntos en la nube | real | `MOVIMIENTO_ECOPUNTOS` con 0 filas: **no hay ecopuntos acreditados todavía** |
| Power BI Desktop (refresco de los `.pbix`) | **pendiente** | requiere abrir Power BI con la credencial del propietario |
| Instalación en un segundo computador | **pendiente** | — |
| Pruebas físicas del ESP32 | **no ejecutadas** (prohibidas en esta etapa) | — |

## 8. Limpieza propuesta (requiere autorización; nada se borró)

| Carpeta | Motivo | Respaldo |
|---|---|---|
| `…\ProyectoDeAula_EcoAccess\EcoAccess_Version_Final` | reemplazada por `EcoAccess-P3`; sus cambios ya están en la integración | GitHub y `EcoAccess-P3`. **Antes:** detener la estación física, que se ejecuta desde ella, y repuntar la tarea «EcoAccess P3 - vigilante del bot». Su carpeta `docs/CONSOLIDACION_LOCAL` (informes locales, excluida de Git) debe copiarse antes |
| `…\Base de Datos-SS463-\EcoAccess-2026-2` | clon limpio de una rama ya fusionada | GitHub |
| OneDrive `ProyectoDeAula_EcoAccess\EcoAccess_V2`, `EcoAccess-P3-LImpio` | versiones anteriores sin `.git` | todo su código está en el historial de Git; los archivos privados se conservan en `Respaldo_EcoAccess_2026-11-16` |
| OneDrive `Rubrica_EcoAccess-Sucio-` | copiada al repositorio | `docs/academico/programacion_iii/entregas_previas` |
| OneDrive `BaseDeDatos_Amilkar` | copiada al repositorio, **salvo `_LOCAL_NO_PUBLICAR`**, que debe conservarse | GitHub |

Acceso directo anterior «Iniciar EcoAccess P3»: el instalador lo movió, no lo borró, a `%LOCALAPPDATA%\EcoAccessP3\respaldo_accesos_directos`.
