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

## 6. Accesos directos (actualización final 25/11/2026: cuatro accesos en todos los equipos)

El instalador deja **exactamente cuatro** accesos de ECOACCESS en el escritorio, en los dos perfiles. El antiguo `ECOACCESS CHALLENGE` ya no se crea (el juego forma parte de ECOACCESS WEB) y `ECOACCESS DOS CLICS MANUAL` pasa a llamarse `ECOACCESS DOS CLICS`. Cualquier otro acceso antiguo o duplicado de EcoAccess se mueve a `%LOCALAPPDATA%\EcoAccessP3\respaldo_accesos_directos`; no se borra.

| Acceso | Qué abre | Perfil |
|---|---|---|
| **ECOACCESS P3** | La aplicación de escritorio (Release). En la estación principal prepara antes el bot, los puentes y el ESP32 sin duplicarlos ni abrir tapas. | Todos |
| **ECOACCESS ESP32** | `EstacionPrincipal`: panel local `http://localhost:8090` (lo inicia si hace falta): control, diagnóstico y calibración. `EstacionCompanero`: el panel del anfitrión configurado con `Configurar-Anfitrion.ps1` (nunca un puerto COM local). | Todos |
| **ECOACCESS DOS CLICS** | https://jorgegutho21.github.io/EcoAccess-2026-2/dos-clics/: elegir caneca, escribir o dictar el residuo. | Todos |
| **ECOACCESS WEB** | Plataforma oficial https://jorgegutho21.github.io/EcoAccess-2026-2/: uso ciudadano, documentación académica, SOLID, grafos, arquitectura, hardware, Oracle y EcoAccess Challenge (sección «Challenge» → botón «Pon a prueba tus conocimientos»). | Todos |

- **El juego no necesita instalación**: está integrado en ECOACCESS WEB (sección Challenge) y su enlace directo https://jorgegutho21.github.io/EcoAccess-2026-2/juego/ sigue funcionando (útil para el QR del stand), hablando con los servicios REST (ORDS) de Oracle. No pide cuentas de Oracle ni de Telegram. La clave del panel administrativo y la de respuestas existen solo en el equipo anfitrión (`%LOCALAPPDATA%\EcoAccessP3\juego\`) y no se comparten.
- **El compañero sin ESP32 nunca inicia el bot**: con `EstacionCompanero`, P3 abre solo la aplicación y ECOACCESS ESP32
  solo abre el panel del anfitrión. Un segundo `EcoAccess.P3.Bot.exe --polling` con el mismo token rompe el bot.
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

---

## 8. Estación Oficial EcoAccess: un solo controlador para todos los canales

- **Un único dueño del ESP32:** el panel ECOACCESS ESP32 (`EcoAccess.P3.PanelCanecas.exe`, puerto 8090) del equipo
  anfitrión es el único programa que abre el puerto serial. Telegram, la app C#, ECOACCESS WEB y DOS CLICS (a través de
  la pasarela HTTPS) le envían las órdenes; la misma clave nunca abre dos veces.
- **QR oficial y permanente:** `https://t.me/EcoAccessP3_JG_bot?start=EcoAccessCanecaPiloto1` (no se reimprime). En Oracle
  es la caneca 1093, «Estación Oficial EcoAccess». Escanearlo abre una sesión de 15 minutos para esa persona; si ya envió
  una foto identificada, la caneca se abre al escanear.
- **En servicio por defecto (actualización 25/11/2026, noche):** la estación arranca EN SERVICIO cada vez que se inicia
  el panel (también tras un inicio normal de Windows): las aperturas autorizadas de todos los canales se ejecutan solas
  (7/14/21 s, cierre automático) en cuanto el ESP32 está conectado y confirma la orden. Ya no hay que pulsar nada cada
  jornada. Antes la activación caducaba a las 23:59 y no se restauraba al encender: esa era la causa de que Telegram
  reconociera la cáscara de banano y respondiera «la estación está en pausa».
- **Pausa manual (mantenimiento o emergencia):** en el anfitrión, ECOACCESS ESP32 → **PONER EN PAUSA** (pide el motivo) o
  **DETENER TODO**. La pausa se mantiene aunque se reinicie el equipo, hasta pulsar **REANUDAR SERVICIO**. Mientras está
  en pausa, Telegram responde que la estación está en mantenimiento y nunca dice «Caneca abierta».
  La calibración y las pruebas técnicas siguen pidiendo ACTIVAR CONTROL (sesión supervisada).

### Controlar la estación desde el computador de un compañero (red local)
1. En el anfitrión, una sola vez y como administrador: `powershell -ExecutionPolicy Bypass -File scripts\arranque\Habilitar-PanelEnRed.ps1`.
   Abre el puerto 8090 solo en redes privadas. Reinicia los servicios con `scripts\arranque\Iniciar-ServiciosEcoAccess.ps1`.
2. El anfitrión entrega por canal privado (nunca por GitHub) la clave de `%ProgramData%\EcoAccessP3Framework\Panel\clave.txt`
   y la IP de su equipo (`ipconfig`).
3. En el equipo del compañero, una vez:
   `powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Anfitrion.ps1 -Anfitrion <IP-del-anfitrión>`
   (pide la clave, comprueba la conexión sin mover tapas y la guarda solo para tu usuario en
   `%LOCALAPPDATA%\EcoAccessP3\anfitrion.txt`). Después: `scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero`.
4. Resultado: ECOACCESS P3 abre las canecas reales a través del anfitrión (si el anfitrión no responde, lo dice; nunca
   simula una apertura) y ECOACCESS ESP32 abre el panel del anfitrión (la primera vez pide la misma clave).
5. El compañero **no** inicia el bot ni abre ningún puerto COM: Telegram ya llega a la estación a través del anfitrión.
   (El método anterior con `setx ECOACCESS_PANEL_URL` y la copia de `clave.txt` sigue funcionando.)

### Cambiar de anfitrión (llevar el ESP32 a otro computador)
1. En el equipo anterior: `scripts\arranque\Detener-ServiciosEcoAccess.ps1 -Pausar` (no vuelve a arrancar al iniciar sesión).
2. Conecta el ESP32 al nuevo equipo (driver CP210x) e instala con `scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionPrincipal`
   y `scripts\arranque\Instalar-ArranqueEcoAccess.ps1`. El panel detecta el puerto COM solo (busca el firmware
   `estacion_tres_canecas` en los puertos disponibles), sin tocar el código.
3. Copia en el nuevo anfitrión los secretos de la estación (User Secrets del bot y `clave.txt`) por canal privado.
   El bot solo puede correr en UN equipo a la vez.

## 9. Telegram en el equipo de un compañero y el ESP32 en el anfitrión (26/11/2026)

Reglas: **un solo anfitrión** (el equipo con el ESP32 por USB, único dueño del puerto COM) y **un solo bot de Telegram**
para todo el proyecto (Telegram entrega cada mensaje a un solo consumidor; dos bots con el mismo token se expulsan
con «409 Conflict»). La dirección del anfitrión nunca está en el código: se guarda en
`%LOCALAPPDATA%\EcoAccessP3\anfitrion.txt` con `Configurar-Anfitrion.ps1`.

### En el anfitrión (una vez, PowerShell como administrador, desde la carpeta del repositorio)

1. La red Wi-Fi debe estar en perfil **Privado** (Configuración → Red e Internet → Wi-Fi → la red → Tipo de perfil de red: Privada).
2. `powershell -ExecutionPolicy Bypass -File scripts\arranque\Habilitar-PanelEnRed.ps1` y reiniciar los servicios.
3. Anotar la IP que muestra el script y entregar la clave (`%ProgramData%\EcoAccessP3Framework\Panel\clave.txt`) solo por un canal privado.
4. Si el bot va a correr en el equipo del compañero: `powershell -ExecutionPolicy Bypass -File scripts\arranque\Ubicar-BotTelegram.ps1 -EnOtroEquipo "PC de Logos"`.
   El panel y el ESP32 siguen funcionando; solo se detiene el bot local.

### En el equipo del compañero (desde la carpeta del repositorio, nunca desde System32)

```powershell
git status
git fetch origin
git checkout integracion/ecoaccess-final
git pull --ff-only
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionPrincipal
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Anfitrion.ps1 -Anfitrion <IP del anfitrión>
powershell -ExecutionPolicy Bypass -Command "& '.\scripts\arranque\Detener-ServiciosEcoAccess.ps1' -Confirm:`$false"
powershell -ExecutionPolicy Bypass -File scripts\arranque\Iniciar-ServiciosEcoAccess.ps1 -Raiz (Get-Location).Path
```

* Si `git status` muestra cambios propios, se confirman o se guardan antes del `pull`; `--ff-only` nunca los sobrescribe.
* `Configurar-Anfitrion.ps1` pide la clave en privado (no se muestra ni se guarda en GitHub) y comprueba el anfitrión sin mover tapas.
* Con `anfitrion.txt` apuntando a otro equipo, el arranque **no inicia panel local ni pasarela** (no abre ningún COM) y
  comprueba el panel del anfitrión. El bot de este equipo envía las aperturas al anfitrión; su registro muestra
  «estación anfitrión de red http://<IP>:8090».
* El equipo del bot necesita Oracle (wallet y User Secrets) y Ollama con `lfm3-vl`.
* Si cambia la IP del anfitrión: repetir solo `Configurar-Anfitrion.ps1` con la nueva IP y reiniciar los servicios.
* Para devolver el bot al anfitrión: `Ubicar-BotTelegram.ps1 -EnEsteEquipo` en el anfitrión, después de detener el bot del compañero.
* Limitación conocida: las aperturas hechas desde la web de control del anfitrión (`operaciones-web.jsonl`) las sube a Oracle el
  bot que corre en el anfitrión. Si el bot corre en otro equipo, esas operaciones quedan en la cola local del anfitrión
  hasta que el bot vuelva a correr allí.
