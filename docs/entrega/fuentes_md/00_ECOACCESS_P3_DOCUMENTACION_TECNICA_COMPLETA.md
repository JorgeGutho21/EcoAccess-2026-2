---
archivo: ECOACCESS_P3_DOCUMENTACION_TECNICA_COMPLETA
titulo: EcoAccess P3 — Documentación técnica completa
subtitulo: Sistema inteligente de orientación y registro de residuos: arquitectura, código, base de datos, inteligencia artificial, pruebas, operación y resultados
codigo: 00
---

# Introducción

EcoAccess P3 es el proyecto de aula de **Programación de Computadores III** y **Base de Datos** de la Universidad Popular
del Cesar. Lo integran Jorge Gutiérrez Thomas, Heyder Canchila y Saray Cadena.

Es un sistema que **orienta a las personas para que depositen cada residuo en la caneca correcta** y **registra** lo que
ocurre, de forma verificable. Tiene cuatro partes:

- una aplicación de escritorio en **Windows Forms** para el personal (operarios y administradores);
- un **bot de Telegram** para los ciudadanos;
- una base de datos **Oracle Autonomous Database** en Oracle Cloud;
- un modelo de **visión artificial local** (Ollama con LFM3-VL) que reconoce el residuo en una foto.

El sistema está preparado para controlar tapas con **ESP32**.

Este documento explica el proyecto **desde cero**: primero cada concepto y después cómo se aplicó, con fragmentos del
código real. Las tablas de clases, formularios, columnas de Oracle, commits y pruebas **no se escribieron a mano**. Se
generaron leyendo el código con Roslyn, el diccionario de Oracle y el historial de Git del commit indicado en la portada.

Cómo leer los estados que aparecen en el documento:

| Estado | Significado |
|---|---|
| VERIFICADO REAL | probado contra el sistema real (Oracle Cloud, Telegram real, la aplicación real) |
| VERIFICADO EN SIMULACIÓN | probado con simuladores o fotos públicas, no con el dispositivo físico ni con un teléfono |
| IMPLEMENTADO SIN VALIDACIÓN EXTERNA | el código existe y tiene pruebas, pero falta la prueba en el mundo real |
| BLOQUEADO | depende de algo que no está disponible (credencial, hardware, servicio) |
| PENDIENTE | no se ha hecho |

# Problema

En Colombia, desde 2021, rige el código de colores para separar en la fuente:

- **blanco** para los aprovechables;
- **verde** para los orgánicos aprovechables;
- **negro** para los no aprovechables.

En la práctica, mucha gente no sabe en qué caneca va cada residuo, y los aprovechables se contaminan al mezclarse.
Tampoco suele haber registro de cuánto se separa ni de quién participa, así que no se puede medir ni premiar la buena
separación.

# Justificación

Orientar **en el momento de desechar** es más eficaz que una campaña general. Un teléfono con Telegram y una foto bastan
para recibir la instrucción. El registro en una base de datos relacional permite medir la participación y otorgar
**ecopuntos** solo por depósitos verificados. El proyecto aplica lo visto en las dos asignaturas:

- **Programación de Computadores III:** C#, programación orientada a objetos, capas, Windows Forms y pruebas.
- **Base de Datos:** modelo relacional, normalización, SQL, PL/SQL, vistas y migraciones.

# Objetivo general

Construir un sistema que oriente la separación de residuos por fotografía y registre cada depósito en Oracle de forma
verificable, con una aplicación de escritorio para el personal y un bot de Telegram para los ciudadanos.

# Objetivos específicos

1. Identificar el residuo por fotografía con un modelo local y decir a qué caneca va.
2. Autorizar una apertura solo con reglas de negocio escritas en C#, nunca por decisión directa del modelo.
3. Registrar cada operación en Oracle sin crear depósitos ni puntos ficticios.
4. Ofrecer al personal una aplicación de escritorio con roles (Operario y Administrador) para verificar depósitos, administrar catálogos y consultar estadísticas.
5. Separar los datos reales de los experimentales en todos los informes.
6. Diseñar la solución para varias canecas (estación de tres canecas) sin reescribirla.
7. Documentar y probar cada parte con pruebas automatizadas reproducibles.

# Alcance

**Incluye:**

- aplicación de escritorio;
- bot de Telegram;
- base de datos con 29 tablas y migraciones hasta V025;
- visión local;
- ecopuntos con verificación;
- estación de tres canecas en datos y en simulación;
- tablero institucional;
- recursos de Power BI;
- simuladores de dispositivos;
- documentación y pruebas.

**No incluye todavía** (ver «Limitaciones»):

- la apertura física probada de las tapas;
- la báscula;
- el alojamiento 24/7 sin el PC del propietario;
- un informe `.pbix` terminado;
- Google Forms.

# Herramientas utilizadas

| Herramienta | Uso |
|---|---|
| C# 9 y .NET Framework 4.8 | lenguaje y plataforma de toda la solución (`Directory.Build.props` fija `net48` y `LangVersion 9`) |
| Visual Studio 2026 Community (versión 18) | edición, diseñador de Windows Forms, depuración y pruebas |
| Windows Forms y Krypton Toolkit | interfaz de escritorio |
| Oracle Autonomous Database 19c (Oracle Cloud, Bogotá) | base de datos principal |
| Oracle Database FREE local | esquema aislado `ECOACCESS_P3_PRUEBAS` para pruebas |
| ODP.NET (`Oracle.ManagedDataAccess` 23.26.400) | conexión desde C# |
| Telegram Bot API | canal de los ciudadanos |
| Ollama y LFM3-VL | visión local |
| Groq (opcional) | segunda opinión externa con consentimiento |
| MSTest 4 | pruebas unitarias y de integración |
| Power BI Desktop | informe avanzado (recursos preparados) |
| ESP32 y Arduino | firmware de la tapa |
| Git y GitHub | control de versiones y PR #11 |
| Roslyn y Markdig | generación de esta documentación desde el código |

# Configuración de Visual Studio

1. Instale **Visual Studio Community** con la carga de trabajo **Desarrollo de escritorio de .NET** y el componente **.NET Framework 4.8 targeting pack**.
2. Abra `EcoAccess.P3.sln` con el acceso del Escritorio **Abrir EcoAccess P3 en Visual Studio**. Abre la solución de esta copia y no la de auditoría ni la V2.
3. Visual Studio restaura los paquetes NuGet al abrir. Por consola: `dotnet restore`.
4. Clic derecho en `EcoAccess.P3.Presentacion` > **Establecer como proyecto de inicio**.
5. Compile con **Ctrl+Mayús+B**, en Debug o en Release.
6. Las pruebas están en **Prueba > Explorador de pruebas**.

Los secretos (conexión a Oracle, token de Telegram) **no** van en el proyecto. Se guardan en User Secrets con
`scripts/instalacion/Configurar-Credenciales.ps1`.

# Creación de la solución

La solución se creó como un conjunto de **proyectos de biblioteca** más dos ejecutables. Cada capa es un proyecto
separado, para que el compilador impida dependencias prohibidas. Por ejemplo, la lógica **no puede** referenciar a Oracle,
porque su proyecto no tiene esa referencia.

# Organización de proyectos

{{proyectos}}

{{conteos}}

# Arquitectura por capas

![Arquitectura por capas de EcoAccess P3](../../fuentes_md/img/arquitectura_capas.png)

- **Entidades:** las clases del dominio (Usuario, Material, Contenedor, Deposito...), con sus validaciones. No dependen de nada.
- **Lógica:** servicios y reglas de negocio. Depende de entidades y de **contratos** (interfaces `IRepositorio*`), no de Oracle.
- **Datos:** repositorios que implementan los contratos con ODP.NET y SQL parametrizado.
- **Presentación:** Windows Forms (personal) y bot de Telegram (ciudadanos). Arman los objetos al iniciar (raíz de composición, `Program.cs`) y llaman a la lógica.
- **Compatibilidad:** utilidades para que C# 9 moderno funcione sobre .NET Framework 4.8.

La explicación completa, con los ocho recorridos A–H, está en el documento 04.

# Cada carpeta principal

| Carpeta | Contenido |
|---|---|
| `src/EcoAccess.P3.Entidades` | entidades del dominio y enumeraciones |
| `src/EcoAccess.P3.Logica` | servicios (`Servicios/`), contratos (`Contratos/`) y reglas |
| `src/EcoAccess.P3.Datos` | repositorios Oracle (`Repositorios/`), conexión (`Oracle/`) y generador del lote de simulación (`Simulacion/`) |
| `src/EcoAccess.P3.Presentacion` | formularios de Windows Forms, tema visual y componentes propios |
| `src/EcoAccess.P3.Bot` | bot de Telegram, visión (`Vision/`), dispositivos (`Dispositivos/`) y página de registro |
| `src/EcoAccess.P3.Compatibilidad` | utilidades para .NET Framework 4.8 |
| `tests/unitarias` | pruebas sin base de datos |
| `tests/integracion` | pruebas contra Oracle real (esquema aislado o nube) |
| `tests/herramientas` | herramienta de inventario y generación de esta documentación |
| `database/` | administración, consultas, migraciones (`06_migraciones`), pruebas SQL y scripts operativos |
| `hardware/` | firmware del ESP32 y sus pruebas |
| `reportes/powerbi` | recursos para el informe de Power BI |
| `scripts/` | instalación, desarrollo (arranque, vigilante), pruebas y documentación |
| `docs/` | documentación por tema y entregables |

# Entidades

Las entidades son clases con **identidad** y **reglas propias**. Su constructor valida los datos: no se puede crear un
`Usuario` sin nombre o con un correo inválido. Este capítulo describe cada entidad con sus propiedades, constructores,
métodos, validaciones y pruebas relacionadas.

{{entidades}}

# Interfaces

Una **interfaz** es un contrato: dice **qué** se puede hacer, sin decir **cómo**. La lógica depende de las interfaces de
`Lógica (contratos)`. Los repositorios Oracle y los dobles de prueba en memoria las implementan.

{{tipos:Lógica (contratos)}}

# Servicios

Un **servicio** coordina una operación de negocio. Revisa permisos con `ControlAcceso`, valida, llama a los repositorios y
devuelve un `Resultado`.

{{detalle:Lógica}}

# Repositorios

Un **repositorio** traduce entre objetos de C# y filas de Oracle. Usa siempre **parámetros enlazados**.

{{detalle:Datos}}

# Conexión Oracle

{{codigo:src/EcoAccess.P3.Datos/Oracle/FabricaConexionesOracle.cs:11:45}}

- La cadena de conexión se lee de User Secrets (`ConnectionStrings:OracleP3`). No está en el código.
- El wallet (mTLS) está en la carpeta indicada por `TNS_ADMIN`, con alias `ecoaccess_tp`.
- ODP.NET mantiene un pool de conexiones: la primera tarda unos 5 s y las siguientes, menos de 1 s.

Detalle completo en el documento 06.

# Consultas SQL

Las consultas de demostración están en `database/04_consultas`. Las diez estadísticas de cierre
(`09_estadisticas_cierre.sql`) separan lo real de lo experimental con `LEFT JOIN registro_simulacion`.

{{anidar:docs/base_datos/ESTADISTICAS_CIERRE_SQL.md}}

# Migraciones

Cada cambio de estructura es una migración numerada (`V002` a `V025`). Las aplicadas quedan registradas en
`ECO_MIGRACION_APLICADA` con su huella SHA-256 y **no se editan**. Cada migración se prueba en el esquema aislado:

1. se aplica;
2. se repite, para comprobar que es idempotente;
3. se revierte;
4. se vuelve a aplicar.

Solo después se lleva a la nube. Las tres más recientes son:

- **V023:** consulta de visión.
- **V024:** estación de tres canecas.
- **V025:** lote de simulación.

# Windows Forms

Windows Forms construye ventanas con controles y eventos. En EcoAccess, los formularios **no contienen reglas de negocio**:
validan por comodidad, llaman a un servicio con `await` y muestran su `Resultado`. El documento 09 explica el diseñador,
`InitializeComponent` y seis ejercicios guiados.

# Cada formulario

La tabla de cada formulario sale del código. Cada fila es una línea real donde se conecta un evento.

{{formularios}}

# Botones y eventos

Un botón dispara `Click`. El manejador (`async void`, la única excepción permitida a devolver `Task`) sigue siempre estos
pasos:

1. bloquea la ventana;
2. llama al servicio;
3. muestra el resultado;
4. desbloquea en `finally`.

Ejemplo real, el botón Guardar de la configuración inicial:

{{codigo:src/EcoAccess.P3.Presentacion/FormConfiguracionInicial.Designer.cs:130:140}}

# Telegram

El bot `@EcoAccessP3_JG_bot` atiende por **long polling**: pregunta a Telegram si hay mensajes nuevos, así que no necesita
abrir puertos. Guarda los updates procesados (`UPDATE_TELEGRAM`) para no atender dos veces el mismo, y el estado de cada
conversación con versión optimista (`CONVERSACION_TELEGRAM`). Detalle en el documento 07.

{{tipos:Presentación (bot de Telegram)}}

# Ollama

Ollama ejecuta el modelo LFM3-VL en el propio PC (`127.0.0.1:11434`), así que la foto no sale del equipo. EcoAccess
**no entrena** el modelo: lo usa con un vocabulario cerrado y reglas en C#. Detalle en el documento 08.

# Reconocimiento visual

{{anidar:docs/vision/EVALUACION_RONDA5_INDEPENDIENTE.md}}

# Catálogo de residuos

`MATERIAL` guarda cada material con su categoría (aprovechable, orgánico, no aprovechable, peligroso o posconsumo), su
color de caneca y su peso estimado de catálogo. Desde V024, cada material va **por identificador** a la caneca que lo
recibe. `GuiaAmbiental` y `ResolutorDestino` traducen el material al color y la ubicación.

# Tres canecas

La estación piloto tiene tres canecas, cada una con su propio ESP32:

| Caneca | Id | Color | Ubicación | Estado |
|---|---|---|---|---|
| Caneca piloto | 1093 | blanca | izquierda | en servicio |
| Caneca verde | 1400 | verde | centro | fuera de servicio hasta su validación física |
| Caneca negra | 1401 | negra | derecha | fuera de servicio hasta su validación física |

Verificado en Oracle Cloud (`VISTA_CANECAS_ESTACION`). Una orden para la blanca nunca abre la verde ni la negra
(`EstacionSimuladaTests`).

# QR

El QR oficial lleva a `t.me/<bot>?start=EcoAccessCanecaPiloto1` e identifica la **estación**. No se reemplazó ni se
regeneró. La presencia junto a la caneca dura 5 minutos y vive en memoria: una foto sola no prueba que la persona esté
allí.

# Pesos estimados

{{anidar:docs/negocio/PESOS_Y_ECOPUNTOS.md}}

# Ecopuntos

- **Reglas:** 7 tarifas vigentes por unidad verificada, creadas con T001 por el administrador 4521 el 11/11/2026: lata 5, PET 4, cartón 3, metal 3, vidrio 3, multicapa 2 y papel 2.
- **Libro contable:** `MOVIMIENTO_ECOPUNTOS` registra créditos (tipo 1) y reversos (tipo 2). El saldo es la suma de los movimientos; no se guarda aparte.
- **Disparador `tr_ecop_unidades`:** impide acreditar puntos que no correspondan a una cantidad verificada (ORA-20113).
- **Estado real:** 0 ecopuntos otorgados, porque los 100 depósitos son sintéticos y nadie debe ganar puntos por datos inventados.

# Roles y seguridad

| Rol | Puede |
|---|---|
| Ciudadano (1) | consultar sus ecopuntos, depósitos y estadísticas |
| Operario (2) | verificar depósitos de terceros, buscar ciudadanos, ver estadísticas y el informe institucional |
| Administrador (3) | todo lo anterior, más usuarios, roles, materiales, canecas, reglas de ecopuntos y diagnóstico |

Una cuenta puede tener **varios roles**. La cuenta 4521 (Jorge Gutiérrez Thomas) tiene **Administrador y Operario**,
verificado en Oracle Cloud el 11/11/2026. Nadie puede verificar su propio depósito. Detalle en el documento 11.

{{codigo:src/EcoAccess.P3.Logica/Servicios/MatrizPermisos.cs:30:50}}

# Dashboards

El dashboard institucional (`FormDashboardInstitucional`) lo ven el operario y el administrador. Tiene dos modos:

- **operativo:** solo datos reales; hoy son 0 depósitos;
- **experimental:** el lote `SIM-2026-11-CIERRE`, rotulado como tal.

Separa los pesos estimados de los medidos. Detalle en el documento 12.

# Power BI

Están preparados las vistas (V019), la cuenta de solo lectura, las consultas de Power Query, las medidas DAX y la
validación SQL. El archivo `.pbix` de P3 **no está construido**: necesita la contraseña de la cuenta de informes, que
solo conoce el propietario. El cruce SQL confirma que el informe debe mostrar 0 depósitos operativos. Detalle en el
documento 12.

# Pruebas

EcoAccess tiene pruebas **unitarias**, que corren sin base de datos con dobles en memoria, y **de integración**, que corren
contra Oracle real en el esquema aislado o en la nube. Los resultados de la regresión de cierre están en el documento 13 y
en `docs/pruebas/resultados_cierre`.

{{pruebas}}

# GitHub

El repositorio es `JorgeGutho21/EcoAccess-P3`. La rama de entrega es `desarrollo/telegram-doble-ia` y el pull request
**#11** la propone para `main`. La rama de auditoría `desarrollo/auditoria-tercer-corte` se integró por avance rápido,
sin conflictos. No se usó `force push`.

# Commits y PR

{{commits}}

# Instalación

{{anidar:docs/instalacion/INSTALACION_COMPANEROS.md}}

# Operación

{{anidar:docs/operacion/GUIA_INICIO_RAPIDO_ECOACCESS.md}}

# Integración futura ESP32

La guía completa está en el documento 16. En resumen:

- un ESP32 por caneca, identificado en Oracle (`esp32-piloto-blanca`, `-verde` y `-negra`);
- órdenes firmadas con HMAC, con una clave propia por dispositivo;
- ventana de apertura de 10 segundos;
- eventos de detección que confirman el depósito.

**Ningún motor se ha movido desde el software.** La validación física es la siguiente etapa.

# Resultados

| Requisito | Estado | Evidencia |
|---|---|---|
| Compilación Debug y Release | VERIFICADO REAL | regresión del 11/11/2026 (documento 13) |
| Pruebas unitarias | VERIFICADO REAL | 1 768 superadas, 0 fallidas, 1 omitida (enlace simbólico, necesita permisos de Windows) |
| Oracle Cloud con V023, V024 y V025 | VERIFICADO REAL | huellas en `ECO_MIGRACION_APLICADA` |
| Primer administrador (4521) con Administrador y Operario | VERIFICADO REAL | consulta de `USUARIO_ROL` |
| 7 tarifas de ecopuntos | VERIFICADO REAL | `t001_oracle_cloud.txt` |
| Lote experimental (50 ciudadanos, 100 depósitos) | VERIFICADO REAL (datos sintéticos) | `estadisticas_sql_nube.txt` |
| Tres canecas en Oracle | VERIFICADO REAL | `VISTA_CANECAS_ESTACION` |
| Flujo Eco completo | VERIFICADO EN SIMULACIÓN | `FlujoEcoDefinitivoTests`, `BridgeOracleTests` |
| Visión local | VERIFICADO EN SIMULACIÓN | ronda 5: 75,5 % |
| Bot en vivo y vigilante | VERIFICADO REAL | proceso único desde `EcoAccess-P3`, relanzado por la tarea |
| Foto real desde un teléfono | PENDIENTE | necesita al propietario con su teléfono |
| Apertura física | PENDIENTE | prohibido mover motores sin el equipo presente |
| Informe Power BI P3 | BLOQUEADO | contraseña de la cuenta de informes |
| Alojamiento 24/7 | BLOQUEADO | sin capacidad gratuita compatible |

# Limitaciones

- **Hardware:** no se ha probado la apertura física. Las canecas verde y negra están fuera de servicio en los datos hasta validarlas.
- **Sin báscula:** los pesos son estimados.
- **Visión:** 75,5 % de exactitud en la ronda independiente 5, con debilidades en icopor y multicapa. Por eso la apertura pide confirmación y los ecopuntos esperan la verificación del operario.
- **Disponibilidad:** depende del PC del propietario encendido.
- **Datos:** no hay depósitos reales todavía; todas las estadísticas con datos son del lote experimental.
- **Cuentas del equipo:** Heyder Canchila y Saray Cadena aún no tienen cuenta, porque faltan sus correos reales.

# Conclusiones

EcoAccess P3 cumple su objetivo central **en software**. Orienta la separación por foto con un modelo local que no expone
la imagen. Registra todo en Oracle con integridad: claves, restricciones, disparadores, transacciones y migraciones con
huella. Separa siempre lo real de lo experimental y protege cada operación con roles revalidados en cada llamada.

La arquitectura por capas permitió agregar la estación de tres canecas, el lote experimental y el primer administrador
sin reescribir lo existente, y las más de 1 900 pruebas automatizadas sostienen esos cambios. Lo que falta es del mundo
físico (tapas, báscula, teléfono) o de infraestructura (alojamiento), y se informa como pendiente, sin presentarlo como
hecho.

# Referencias

- Ministerio de Ambiente y Desarrollo Sostenible de Colombia. Resolución 2184 de 2019 (código de colores para la separación en la fuente, vigente desde 2021).
- Microsoft. Documentación de C#, .NET Framework 4.8 y Windows Forms. learn.microsoft.com.
- Oracle. Oracle Autonomous Database y ODP.NET Managed Driver, documentación oficial. docs.oracle.com.
- Telegram. Telegram Bot API. core.telegram.org/bots/api.
- Ollama. Documentación de la API local. github.com/ollama/ollama.
- Liquid AI. Modelos LFM de visión y lenguaje (LFM3-VL).
- Martin, R. C. (2017). *Clean Architecture*. Prentice Hall.
- Larman, C. (2004). *Applying UML and Patterns* (patrones GRASP). Prentice Hall.
- Microsoft. MSTest, documentación. learn.microsoft.com.
- Wikimedia Commons. Fotos con licencias libres usadas en la evaluación visual (lista en `docs/vision/resultados/ronda5_fuentes.json`).

# Anexos

## Anexo A. Detalle de la presentación de escritorio

{{detalle:Presentación (Windows Forms)}}

## Anexo B. Detalle del bot de Telegram

{{detalle:Presentación (bot de Telegram)}}

## Anexo C. Detalle de compatibilidad

{{detalle:Compatibilidad}}

## Anexo D. Diccionario de tablas de Oracle

{{oracle_tablas}}

## Anexo E. Diccionario de columnas de Oracle

{{oracle_columnas}}

## Anexo F. Cobertura de la documentación

Qué tipos y formularios del código quedaron documentados y dónde.

{{cobertura}}
