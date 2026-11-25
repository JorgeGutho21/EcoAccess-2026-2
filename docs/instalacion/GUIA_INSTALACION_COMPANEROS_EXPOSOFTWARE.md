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

## 6. Accesos directos y EcoAccess Challenge (actualización 25/11/2026)

El instalador deja **exactamente cuatro** accesos de ECOACCESS en el escritorio (cualquier otro de EcoAccess se mueve a
`%LOCALAPPDATA%\EcoAccessP3\respaldo_accesos_directos`; no se borra):

| Acceso | Qué abre | Perfil |
|---|---|---|
| **ECOACCESS P3** | La aplicación (Release). En la estación principal prepara antes bot, puentes y ESP32 sin duplicarlos ni abrir tapas. | Todos |
| **ECOACCESS ESP32** | Panel local `http://localhost:8090` (lo inicia si hace falta). | Solo `EstacionPrincipal` |
| **ECOACCESS WEB** | https://jorgegutho21.github.io/EcoAccess-2026-2/ | Todos |
| **ECOACCESS CHALLENGE** | https://jorgegutho21.github.io/EcoAccess-2026-2/juego/ | Todos |

- **El juego no necesita instalar nada**: es una página publicada en GitHub Pages que habla con los servicios REST (ORDS)
  de la base Oracle. No pide cuentas de Oracle ni de Telegram. La clave del panel administrativo y la clave de
  respuestas existen solo en el equipo anfitrión (`%LOCALAPPDATA%\EcoAccessP3\juego\`) y no se comparten.
- **El compañero sin ESP32 nunca inicia el bot**: con el perfil `EstacionCompanero` no se crea el acceso ESP32 y el
  acceso P3 abre solo la aplicación. Un segundo `EcoAccess.P3.Bot.exe --polling` con el mismo token rompe el bot.
- Comprobar el equipo (solo lectura): `powershell -ExecutionPolicy Bypass -File "%LOCALAPPDATA%\Programs\EcoAccess P3\Verificar-Equipo-EcoAccess.ps1"`.
