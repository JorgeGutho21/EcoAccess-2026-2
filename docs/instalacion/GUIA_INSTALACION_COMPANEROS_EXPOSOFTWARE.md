# Guía de Instalación Multiequipo y Uso Móvil — ECOACCESS P3 (ExpoSoftware)

Esta guía permite instalar **ECOACCESS P3** en cualquier computador con Windows 10/11 **sin depender de OneDrive ni copiar rutas o contraseñas de otro integrante**.

---

## 1. Perfiles de Instalación

| Perfil | Para quién | Qué incluye | Hardware físico (`COM3`) |
|---|---|---|---|
| `EstacionPrincipal` | Computador conectado físicamente al ESP32 y las 3 canecas | App Windows, bot único `@EcoAccessP3_JG_bot`, bridges HMAC y modo físico supervisado | **Sí** (autodescubrimiento seguro del puerto COM) |
| `EstacionCompanero` | Computadores de compañeros de equipo | App Windows, reconocimiento visual local, catálogo 35 materiales, registro manual, QR, Oracle Cloud y modo simulación | **No** (jamás intenta abrir el `COM3` de la estación principal) |

---

## 2. Requisitos Previos (Compañeros)

1. **Windows 10 / 11** con **.NET Framework 4.8** (incluido en Windows actualizado).
2. *(Solo si instalas desde código fuente de GitHub)* **Git** y **.NET SDK 8/10** (`dotnet --version`). Si usas el paquete `.zip` publicado por el responsable, **no necesitas SDK ni Visual Studio**.
3. *(Opcional para reconocimiento visual por IA local)* **Ollama** (`https://ollama.com`) y modelo `lfm3-vl`:
   ```powershell
   ollama pull lfm3-vl
   ```
4. **Wallet de Oracle Cloud (`Wallet_*.zip`)**: se entrega **únicamente por canal privado** (nunca en GitHub ni incrustado en el instalador).

---

## 3. Instalación Paso a Paso (Compañeros)

### Opción A — Desde el paquete distribuible `.zip` (Recomendada)

1. Descomprime `EcoAccess-P3-3.1.0-exposoftware-<commit>.zip` en una carpeta local (ejemplo: `C:\EcoAccess`).
2. Ejecuta en PowerShell:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero -Wallet "$HOME\Downloads\Wallet_ecoaccess.zip"
   ```
3. Verifica que todo quedó correcto:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Verificar-Equipo-EcoAccess.ps1 -Perfil EstacionCompanero
   ```
4. Haz doble clic en el acceso directo **`ECOACCESS P3`** creado en tu Escritorio.

### Opción B — Desde GitHub (Desarrolladores)

```powershell
git config --global core.longpaths true
git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git EcoAccess-P3
cd EcoAccess-P3
git checkout feat/ecoaccess-vision-integracion-final
powershell -ExecutionPolicy Bypass -File .\scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero -Wallet "$HOME\Downloads\Wallet_ecoaccess.zip"
```

### Actualización y Desinstalación Segura

- **Actualizar desde GitHub:**
  ```powershell
  git pull
  powershell -ExecutionPolicy Bypass -File .\scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero
  ```
- **Desinstalar sin perder tu wallet ni tu contraseña guardada:**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\instalacion\Instalar-EcoAccess.ps1 -Desinstalar
  ```

---

## 4. Configuración Individual de Oracle Cloud

1. Al abrir **`ECOACCESS P3`** por primera vez, la aplicación detecta automáticamente tu wallet en `%LOCALAPPDATA%\EcoAccessP3\wallet` (o `TNS_ADMIN`).
2. Ingresa tu contraseña/credencial individual en la pantalla de configuración inicial: la aplicación valida la conexión contra `ecoaccess_tp` y la almacena de forma privada en tu perfil local de Windows (`User Secrets`).
3. **Nunca** subas `Wallet_*.zip`, `secrets.json` ni `bridge-*.json` a GitHub.

---

## 5. Uso del Chatbot de Telegram desde Android o iPhone

1. Abre Telegram en cualquier teléfono (Android o iOS) con Internet y entra a:
   - **https://t.me/EcoAccessP3_JG_bot**
2. **Importante:** solo la **Estación Principal** ejecuta el proceso `EcoAccess.P3.Bot.exe --polling`. Los compañeros **no** deben iniciar un segundo bot con el mismo token para evitar conflictos de polling.
3. Comandos disponibles para cualquier usuario:
   - `/start`, `/ayuda`, `/estado`, `/canecas`, `/modo`, `/materiales`, `/separar`, `/depositos`, `/ecopuntos`, `/historial`, o enviar una fotografía/nombre de un residuo.
4. Comandos de apertura directa (`/verde`, `/blanca`, `/negra`, `/abrir verde blanca`, `/abrir verde negra`, `/abrir blanca negra`, `/abrir verde blanca negra`):
   - **Restringidos estrictamente a chats autorizados (`ECOACCESS_TELEGRAM_CHATS_AUTORIZADOS`)**; en modo físico requieren además sesión supervisada activa en la estación principal. Cualquier usuario no autorizado recibe una denegación auditada y nunca puede accionar servomotores.

---

## 6. Accesos directos (actualización 25/11/2026: tres accesos)

El instalador deja **exactamente tres** accesos de ECOACCESS en el escritorio. Cualquier otro de EcoAccess, incluido el
antiguo «ECOACCESS CHALLENGE», se mueve a `%LOCALAPPDATA%\EcoAccessP3espaldo_accesos_directos`; no se borra.

| Acceso | Qué abre | Perfil |
|---|---|---|
| **ECOACCESS P3** | La aplicación de escritorio (Release). En la estación principal prepara antes el bot, los puentes y el ESP32 sin duplicarlos ni abrir tapas. | Todos |
| **ECOACCESS ESP32** | Panel local `http://localhost:8090` (lo inicia si hace falta): control, diagnóstico y calibración. | Solo `EstacionPrincipal` |
| **ECOACCESS WEB** | Plataforma oficial https://jorgegutho21.github.io/EcoAccess-2026-2/: uso ciudadano, documentación académica y EcoAccess Challenge (`/juego/`, que sigue funcionando como enlace directo). | Todos |

- **El juego no necesita instalación**: vive dentro de ECOACCESS WEB y habla con los servicios REST (ORDS) de Oracle. No
  pide cuentas de Oracle ni de Telegram. La clave del panel administrativo y la de respuestas existen solo en el equipo
  anfitrión (`%LOCALAPPDATA%\EcoAccessP3\juego\`) y no se comparten.
- **El compañero sin ESP32 nunca inicia el bot**: con `EstacionCompanero` no se crea el acceso ESP32 y P3 abre solo la
  aplicación. Un segundo `EcoAccess.P3.Bot.exe --polling` con el mismo token rompe el bot.
- Comprobar el equipo (solo lectura):
  `powershell -ExecutionPolicy Bypass -File "%LOCALAPPDATA%\Programs\EcoAccess P3\Verificar-Equipo-EcoAccess.ps1"`.

---

## 7. Requisitos detallados por herramienta

| Herramienta | Para qué | Cómo |
|---|---|---|
| **Visual Studio 2022 o posterior** (Community sirve) | Abrir `EcoAccess.P3.sln` y depurar | Carga de trabajo «Desarrollo de escritorio de .NET» y el paquete de destino de **.NET Framework 4.8**. La solución compila para `net48` con **C# 9** (`Directory.Build.props`). |
| **Visual Studio Code** (opcional) | Editar la web (`web/`), scripts y SQL | Extensiones C# y PowerShell. No hace falta para usar la app. |
| **.NET SDK 8 o posterior** (opcional) | Compilar desde la terminal: `dotnet build EcoAccess.P3.sln -c Release` | https://dotnet.microsoft.com |
| **Wallet de Oracle** | Conexión cifrada (mTLS) con Oracle Autonomous | La entrega Jorge por canal privado. Descomprímela en `%USERPROFILE%\.oracle\wallets\EcoAccess`. Nunca la subas a GitHub. |
| **Credencial individual** | Cada integrante tiene su cuenta (`ECO_HEYDER`, `ECO_SARAY`), no la del esquema | `powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1`. La contraseña la escribe cada persona y queda en sus User Secrets. |
| **Roles** | Qué puede hacer cada cuenta en Oracle | `ROL_CONSULTA` (lectura) y `ROL_EDICION` (registro), otorgados con `database/00_administracion/05_conceder_permisos_roles.sql`. Nadie recibe DBA ni privilegios globales. |
| **SQL Developer** | Consultar la base | Nueva conexión → Tipo de conexión **Cartera en la nube (Cloud Wallet)** → archivo `Wallet_*.zip` → servicio `ecoaccess_tp` → usuario y contraseña individuales → **Probar**. |
| **Ollama + `lfm3-vl`** (opcional) | Reconocimiento de fotos local | https://ollama.com y `ollama pull lfm3-vl`. Sin Ollama siguen funcionando el texto, la voz y el registro manual. |
| **Driver CP210x** (solo la estación principal) | Puerto COM del ESP32 | Silicon Labs CP210x USB to UART Bridge. Windows 11 suele instalarlo solo; debe aparecer como `COM3` (o el que detecte el panel). |
| **Instalador** | App, iconos, verificador y accesos | `powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero` (o el paquete `.zip` de la versión). |

> Estado real: el instalador y estos pasos se probaron en el equipo de la estación principal. **En los computadores de
> Heyder y Saray todavía no se han probado directamente**; ejecutad el verificador y enviad su salida.
