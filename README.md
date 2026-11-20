# EcoAccess P3

Sistema inteligente de orientación, clasificación y registro de residuos de la Universidad Popular del Cesar. Es **un solo proyecto** con dos componentes académicos:

| Componente | Asignatura | Dónde está |
|---|---|---|
| Aplicación (Windows Forms, bot de Telegram, IA local, tres canecas con ESP32, integración Oracle) | Programación de Computadores III | `src/`, `tests/`, `database/`, `hardware/`, `scripts/`, `reportes/`, `docs/` |
| Modelo de datos, diccionario, SQL, PL/SQL, triggers, paquetes, Power BI y sustentación | Base de Datos (profesor Amílkar) | `BaseDeDatos_Amilkar/` |

Ambos describen el mismo sistema EcoAccess con las mismas tablas (`USUARIO`, `MATERIAL`, `CONTENEDOR`, `DEPOSITO`, `ORDEN_APERTURA`, `MOVIMIENTO_ECOPUNTOS`…). La correspondencia exacta entre los dos esquemas, las clases C#, los formularios, Telegram, las canecas y Power BI está en [docs/UNIFICACION_ECOACCESS_P3.md](docs/UNIFICACION_ECOACCESS_P3.md).

**Plataforma:** .NET Framework 4.8 · C# · Oracle Autonomous Database (Oracle Cloud) · ODP.NET Managed · Telegram Bot API · Ollama (LFM3-VL, local) · ESP32 (Arduino/C++) · Power BI.

## Qué problema resuelve

La gente no sabe en qué caneca va cada residuo y las canecas no registran nada. EcoAccess:

1. **Orienta:** el ciudadano fotografía el residuo en Telegram o lo busca por nombre en el escritorio; la IA local propone el objeto y la lógica de negocio (no la IA) decide si es aprovechable, orgánico, no aprovechable o peligroso.
2. **Abre solo la caneca correcta:** blanca (aprovechables, 1093), verde (orgánicos, 1400) o negra (no aprovechables, 1401), durante 10 segundos y únicamente si el ciudadano está frente a la estación (QR, presencia de 5 minutos). Lo dudoso o peligroso (pilas, cargadores, medicamentos) no abre nada y recibe orientación hacia un punto especializado.
3. **Registra y premia:** cada apertura queda en Oracle (`ORDEN_APERTURA`, `EVENTO_DISPOSITIVO`) y solo un depósito verificado genera ecopuntos.
4. **Informa:** Power BI lee vistas de Oracle para estadísticas de depósitos, materiales, participación y ecopuntos.

## Arquitectura

```
 Windows Forms (Presentacion) ─┐                         ┌─ SQL Developer (administración)
 Bot de Telegram (Bot) ────────┼─► Logica ─► Datos ─► Oracle Cloud (ECOACCESS_P3_APP) ◄─┤
 ESP32 ×3 (bridge HMAC) ◄──────┘   (reglas)  (ODP.NET,           alias ecoaccess_tp     └─ Power BI (vistas EA_R_*, VISTA_*)
                                             repositorios)
 Entidades: dominio sin dependencias · Compatibilidad: utilidades para .NET Framework 4.8
```

- **Presentación** (`EcoAccess.P3.Presentacion`): formularios; nunca accede a Oracle directamente. `Program.cs` es el único punto de composición.
- **Lógica** (`EcoAccess.P3.Logica`): servicios y reglas de negocio (clasificación, política de apertura automática, ecopuntos).
- **Dominio** (`EcoAccess.P3.Entidades`): entidades y validaciones.
- **Datos** (`EcoAccess.P3.Datos`): repositorios Oracle con consultas parametrizadas; invoca `FN_RESOLVER_CANECA_DISPONIBLE` para decidir la caneca (las estadísticas y los ecopuntos los calcula con su propio SQL: ver [hallazgo H-01](docs/database/16-hallazgos.md)).
- **Integraciones** (`EcoAccess.P3.Bot`): Telegram, visión local con Ollama y el bridge firmado (HMAC-SHA256) hacia los ESP32.
- **Pruebas** (`tests/`): 2.217 pruebas unitarias, integración contra Oracle en un esquema aislado y capturas automáticas de la interfaz (`tests/visual/CapturasExpo`).

## Interfaz de escritorio (rediseño ExpoSoftware)

![Inicio del ciudadano a 1920×1080](docs/evidencias/rediseno_expo/02_inicio_ciudadano_1920x1080.jpg)

- **Acceso:** ilustración de ECO en la UPC, tarjeta con *Tipo de acceso*, **Continuar como invitado** y **¿Aún no tienes una cuenta? Regístrate**.
- **Inicio:** saludo, cuatro indicadores reales con actualización automática (30 s, hasta 120 s si Oracle falla), buscador sin tildes ni mayúsculas, tres canecas seleccionables y resultado con la caneca correcta.
- **Catálogo del prototipo:** 15 residuos (6 verde, 5 blanca, 4 negra). Pilas, medicamentos o aceite reciben una advertencia y no se asignan a ninguna caneca.
- **Depósito en cuatro pasos** con el estado real de Oracle y la ventana de 10 s del firmware; sin ecopuntos sin verificación.
- Las imágenes viven en `assets/imagenes/` y se incrustan en el ejecutable: no dependen de OneDrive.

Guía completa para el equipo: [docs/desarrollo/REDISENO_EXPO_CANECAS.md](docs/desarrollo/REDISENO_EXPO_CANECAS.md). Capturas a 1366, 1600 y 1920: [docs/evidencias/rediseno_expo/](docs/evidencias/rediseno_expo/).

## Instalar y abrir EcoAccess

**Usuarios (compañeros que solo usan la aplicación):** descomprimen el paquete `EcoAccess-P3-<commit>.zip` y ejecutan:

```powershell
powershell -ExecutionPolicy Bypass -File .\Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"
```

**Desarrolladores (desde el repositorio):**

```powershell
git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git EcoAccess-P3
cd EcoAccess-P3
git config core.longpaths true
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"
```

El instalador restaura NuGet, compila en Release, copia la aplicación a `%LOCALAPPDATA%\Programs\EcoAccess P3`, instala el wallet fuera del repositorio y crea en el escritorio **un único acceso directo «EcoAccess P3»** con el logo, sin consola. La primera vez, la aplicación pide la contraseña del esquema en su pantalla de configuración, la valida con Oracle y la guarda solo en el perfil de Windows; después abre directamente. **Docker no es necesario.** Detalle: [docs/instalacion/INSTALACION_COMPANEROS.md](docs/instalacion/INSTALACION_COMPANEROS.md).

> `git config core.longpaths true` es obligatorio en Windows: algunos modelos de Data Modeler de `BaseDeDatos_Amilkar` superan 260 caracteres de ruta.

## Conexión con Oracle

**Documentación completa de la base de datos:**
- [docs/database/](docs/database/README.md): arquitectura, diccionario de las 252 columnas, vistas, paquetes, pruebas y la [guía para compañeros](docs/database/14-guia-companeros.md);
- [database/schema/](database/schema/): DDL real extraído de Oracle Cloud.

| Entorno | Destino | Uso |
|---|---|---|
| **Oficial** | Oracle Cloud, alias `ecoaccess_tp` (wallet), esquema `ECOACCESS_P3_APP` | aplicación, Telegram, Power BI y SQL Developer |
| Desarrollo opcional | `localhost:1522/PRUEBAP3` (Docker) o `localhost:1521/FREEPDB1` (Oracle FREE) | pruebas de integración aisladas (`ECOACCESS_P3_PRUEBAS`) |
| Académico | `localhost:1521/FREEPDB1`, esquema `ECOACCESS_BD` | prácticas de la asignatura de Base de Datos (datos sintéticos) |

- `App.config` solo tiene la parte pública (`Data Source=ecoaccess_tp;User Id=ECOACCESS_P3_APP`). La contraseña vive en los User Secrets `ecoaccess-p3-framework` del perfil de Windows o en la variable `ECOACCESS_ConnectionStrings__OracleP3`. Nunca en GitHub.
- La aplicación encuentra el wallet en `TNS_ADMIN`, `%LOCALAPPDATA%\EcoAccessP3\wallet` o `%USERPROFILE%\.oracle\wallets\EcoAccess`.
- **Objetos PL/SQL de la base oficial:** `PKG_DEPOSITO`, `PKG_ECOPUNTOS`, `PKG_ESTADISTICAS`, `PKG_CONTEXTO_APP`, `PKG_ECOACCESS_STATS`, `FN_RESOLVER_CANECA_DISPONIBLE`, `FN_PESO_ESTIMADO_KG`, `FN_DEPOSITOS_CIUDADANO`, `PR_CAMBIAR_ESTADO_CONTENEDOR`, `PR_CONSULTAR_ESTADISTICAS`, 5 disparadores y 21 vistas (`EA_R_*`, `VISTA_*`, `VW_*`). Los de la asignatura de Base de Datos llegaron con la migración V027 ([docs/UNIFICACION_ECOACCESS_P3.md](docs/UNIFICACION_ECOACCESS_P3.md), sección 4.4).
- **Migraciones:** `src\EcoAccess.P3.Instalador\bin\Release\net48\EcoAccess.P3.Instalador.exe plan` muestra qué versiones están aplicadas en Oracle Cloud (registro `ECO_MIGRACION_APLICADA`); no usa Docker.
- **SQL Developer:** Nueva conexión → Tipo «Cartera de nube» → el mismo `Wallet_*.zip` → servicio `ecoaccess_tp` → usuario `ECOACCESS_P3_APP`. SQL Developer es solo la herramienta: el servidor es Oracle Cloud.

## Visual Studio

Abra **`EcoAccess.P3.sln`** (la única solución principal) y ejecute `EcoAccess.P3.Presentacion` con F5. La solución `BaseDeDatos_Amilkar/10_CSHARP_VISUAL_STUDIO/SOLUCION_CSHARP/EcoAccessBD.sln` es una práctica de la asignatura de Base de Datos, no parte de la aplicación. El formulario de práctica `FormEjemploPropiedades` se abre con el argumento `--laboratorio-propiedades`.

```powershell
dotnet build EcoAccess.P3.sln -c Release
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Release
```

Si el bot está en marcha, detenlo antes de compilar, porque sus archivos de `bin\Release` quedan bloqueados: `pwsh -File scripts\desarrollo\Detener-Demostracion.ps1`. El vigilante lo vuelve a iniciar en menos de 5 minutos.

## Telegram y estación de tres canecas

- **Modo software contra Oracle Cloud (sin hardware):** `scripts\desarrollo\Iniciar-EcoAccess-Nube.ps1`; Eco orienta el destino y no emite órdenes de apertura. Es el que mantiene en marcha la tarea «EcoAccess P3 - vigilante del bot» (`scripts\desarrollo\Instalar-Arranque-Automatico.ps1`).
- **Modo simulado con Docker (desarrollo):** `scripts\desarrollo\Iniciar-Bot-Eco.ps1`; requiere el contenedor `oracle-free-p3` y envía las órdenes a canecas simuladas.
- **Estación física:** `scripts\desarrollo\Iniciar-Estacion-Fisica.ps1` (3 adaptadores serie, 3 bridges y el bot); detener con `scripts\desarrollo\Detener-Demostracion.ps1`. Solo debe existir **una** instancia del bot.
- Uso: escanear el QR oficial (`assets/identidad/QR_Caneca_piloto.png`, `/start EcoAccessCanecaPiloto1`), enviar la foto de un solo objeto en los siguientes 5 minutos y depositar durante los 10 segundos de apertura.
- Flujo completo: [docs/telegram/FLUJO_ECO_DEFINITIVO.md](docs/telegram/FLUJO_ECO_DEFINITIVO.md) · hardware: [hardware/](hardware/).

## Power BI

| Informe | Archivo | Fuente |
|---|---|---|
| Aplicación (Programación III) | `reportes/powerbi/EcoAccess.pbix` | Oracle Cloud `ecoaccess_tp`, vistas `EA_R_*` y `VISTA_*` |
| Académico (Base de Datos) | `BaseDeDatos_Amilkar/09_POWER_BI/ARCHIVO_PBIX/EcoAccess_BD.pbix` | `ECOACCESS_BD` local (vistas `VW_BI_*`) o CSV |

Para contrastar las tarjetas con SQL: `reportes/powerbi/Validacion.sql`.

## Actualizar desde GitHub

- **Desarrolladores:** `git switch main` y `git pull`, luego vuelvan a ejecutar `scripts\instalacion\Instalar-EcoAccess.ps1` para actualizar la copia instalada.
- **Usuarios:** instalan el paquete publicado más reciente (`scripts\instalacion\Publicar-Paquete.ps1` lo genera).

## Estructura

| Carpeta | Contenido |
|---|---|
| `src/` | Compatibilidad, Entidades, Logica, Datos, Presentacion y Bot |
| `tests/` | pruebas unitarias, de integración Oracle, visuales y herramientas |
| `database/` | modelo, migraciones `V0xx`, catálogos y consultas del esquema oficial |
| `BaseDeDatos_Amilkar/` | entrega completa de la asignatura de Base de Datos (documento, MER, diccionario, scripts, PL/SQL, Power BI, sustentación) |
| `hardware/` | firmware de la estación de tres canecas (ESP32) y banco de calibración |
| `assets/imagenes/` | ECO, campus y fotos de los 15 residuos (incrustadas en la aplicación) |
| `scripts/` | instalación, desarrollo, demostración y pruebas |
| `docs/` | documentación técnica y académica (`docs/academico/` contiene la rúbrica de Programación III) |
| `reportes/` | informe Power BI de la aplicación |

## Equipo

Jorge Alfonso Gutiérrez Thomas (responsable principal), Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino.
