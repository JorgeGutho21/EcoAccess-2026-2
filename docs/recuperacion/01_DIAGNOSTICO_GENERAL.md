# 01 · Diagnóstico general de la recuperación

Fechas: 2026-11-08/09 · Windows 11 Pro 10.0.26200 recién formateado · Repositorio oficial: **`JorgeGutho21/EcoAccess-P3`** · Rama de trabajo: `recovery/ecoaccess-oracle-cloud` (desde `8c7ed50`, `desarrollo/telegram-doble-ia`).

## 1. Dos repositorios

| | `JorgeGutho21/EcoAccess-P3` (**oficial**) | `JorgeGutho21/EcoAccess_P3` (línea anterior) |
|---|---|---|
| Marco | .NET Framework 4.8, C# 9 | .NET 10 |
| Migraciones | V001–V020 (+ V021 de esta recuperación) | V001–V008 |
| Visión | LFM3-VL con Ollama, comparación con Groq | Qwen3-VL 2B / Gemini |
| Telegram | «doble IA», registro HTTPS, mediciones 10/20 usuarios | bot básico |

La recuperación empezó por error en la línea anterior (ahí quedó su propio trabajo en la rama del mismo nombre) y se trasladó a este repositorio al encontrar LFM3-VL y V009–V020. La estructura de V001–V008 es idéntica en ambos (solo cambian comentarios), por eso la base de Oracle Cloud instalada desde la línea anterior se completó aquí sin rehacer nada.

Copias locales: `EcoAccess-P3` (original, rama `desarrollo/telegram-doble-ia`, intacta), `EcoAccess-P3-Nube` (clon de trabajo de esta recuperación) y `EcoAccess_V2` (línea anterior). No se modificaron las copias del usuario.

## 2. Herramientas del equipo

| Herramienta | Estado inicial | Acción |
|---|---|---|
| Git | solo el de Visual Studio | instalado Git for Windows 2.56 (WinGet) |
| GitHub CLI | no | instalado 2.103 |
| Visual Studio | 2026 Community (v18) | — |
| .NET SDK / .NET Framework 4.8 | 10.0.403 / Developer Pack presente | — |
| SQL Developer | no | instalado 26.2 |
| PowerShell 7 | no | instalado 7.6.9 (para la ventana del token de Telegram) |
| Ollama | no | instalado 0.40.15 + `lfm3-vl` |
| Docker | **no instalado** | no se instala: ya no se necesita |
| Oracle local | **Oracle Database Free nativo en marcha** (`OracleServiceFREE`) | no se usa ni se toca; decisión del propietario desinstalarlo |
| Python / Node | no | no necesarios |

## 3. Dependencias de Docker encontradas y sustituidas

| Lugar | Antes | Ahora |
|---|---|---|
| `Presentacion/Program.cs` | destino fijo `localhost:1522/PRUEBAP3` | Oracle Cloud con wallet |
| `FormActivacionOracle` | `docker inspect oracle-free-p3`, puerto local | wallet, red a Oracle Cloud, credenciales |
| `scripts/instalacion/Preparar-BaseDatos.ps1` | `docker cp`/`exec` + SQL*Plus | `src/EcoAccess.P3.Instalador` (el script queda como histórico) |
| Mensajes del panel y del bot | «Revisa Docker» | Internet, wallet, Oracle Cloud |

## 4. Datos

| Elemento | Situación |
|---|---|
| Código, scripts, documentación, recursos de Eco | recuperado de GitHub |
| Datos del Oracle local en Docker | perdidos (eran de prueba); el esquema se reconstruyó en Oracle Cloud con los scripts versionados |
| Cadena Oracle, token Telegram | perdidos; reconstruidos (wallet, `guardar-conexion`, BotFather) |
| Modelo LFM3-VL y corpus TrashNet | descargados de nuevo (`ollama pull`, revisión fijada del corpus MIT) |
| Manifiesto de la partición de 24 fotos | no está en el repositorio: no recuperable |
