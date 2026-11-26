# Arranque automático de la estación EcoAccess P3

**Encender el PC → conectar el ESP32 → EcoAccess operativo.** Al encender Windows (sin esperar a que alguien inicie
sesión) la estación anfitriona (el PC con el ESP32 por USB) deja en marcha sus servicios sola, en segundo plano y sin
ventanas, sin Claude, Visual Studio, VS Code, terminales ni accesos directos. La estación queda **EN SERVICIO** salvo
una pausa manual o un **DETENER TODO** guardados (`%ProgramData%\EcoAccessP3Framework\Panel\servicio.txt`), que el
arranque **nunca** levanta. El arranque y la reconexión del USB no envían ninguna orden de apertura.

## Qué se inicia

| Servicio | Cómo se evita el duplicado |
|---|---|
| Ollama (reconocimiento visual) | Se comprueba su API (`127.0.0.1:11434`) y que no haya un proceso de Ollama. |
| Panel de canecas `http://localhost:8090` (único dueño del puerto del ESP32) | Se comprueba su API; además el panel tiene un mutex por puerto HTTP. |
| Bot de Telegram en `--polling` (Modo Expo), con el **sincronizador** de Oracle y el pulso del panel | Solo se inicia si no hay otro `EcoAccess.P3.Bot.exe --polling` (un solo consumidor del token). |
| Pasarela ciudadana (`127.0.0.1:8092/8093`) y su **túnel HTTPS** (cloudflared) | Se comprueba el proceso; la pasarela tiene su mutex y abre ella misma el túnel. |

La aplicación de escritorio no se abre sola (se abre con su acceso directo, como antes); usa los servicios que ya
están en marcha.

## ESP32 por USB

Basta con conectar el cable. El panel busca la placa cada segundo por VID USB (CP210x, CH340/CH9102, FTDI, Espressif)
y no depende de COM3: si Windows asigna otro puerto, lo usa. Al desconectar el cable cierra el puerto y vuelve a
buscar; al reconectar no reenvía ninguna orden anterior y abre el puerto sin DTR/RTS (no reinicia el ESP32). Firmware,
GPIO y calibraciones no cambian.

## Piezas

| Archivo | Para qué |
|---|---|
| `scripts\arranque\Instalar-ArranqueEcoAccess.ps1` | Registra la tarea programada. |
| `scripts\arranque\Iniciar-ServiciosEcoAccess.ps1` | Lo que ejecuta la tarea (el instalador lo copia a `%LOCALAPPDATA%\EcoAccessP3\Arranque`). Reutiliza `scripts\desarrollo\Iniciar-EstacionExpo.ps1`. |
| `scripts\arranque\Estado-ServiciosEcoAccess.ps1` | Estado de la tarea, procesos y duplicados. **Solo lectura** (peticiones GET). |
| `scripts\arranque\Detener-ServiciosEcoAccess.ps1` | Detiene bot, pasarela con su túnel y panel (Ollama con `-IncluirOllama`). |
| `scripts\arranque\Desinstalar-ArranqueEcoAccess.ps1` | Quita la tarea y la copia del script de inicio. |
| `scripts\arranque\Autorizar-CuentaTelegram.ps1` | Autoriza UN ID de Telegram verificado (ver [AUTORIZACION_TELEGRAM.md](AUTORIZACION_TELEGRAM.md)). |

Tarea programada: **`EcoAccess P3 - arranque de la estación`** (carpeta raíz del Programador de tareas).

- Instalada con `-AntesDeIniciarSesion` (equipo anfitrión): cuenta del usuario en modo **S4U** («ejecutar tanto si
  el usuario inició sesión como si no», **sin contraseña guardada**), nivel **Limitado**, en la sesión 0 de Windows
  (sin ventanas). Disparadores: **al encender Windows** (45 s de retraso), al iniciar sesión (30 s) y **vigilancia
  cada 5 minutos**. Registrarla así exige una terminal de administrador una sola vez.
- Sin `-AntesDeIniciarSesion` (equipos de compañeros): sesión interactiva del usuario, disparador al iniciar sesión
  con 30 s de retraso y la misma vigilancia.
- Vigilancia: si bot, panel o pasarela se cerraron, la tarea inicia solo lo que falta (sin duplicar lo demás); si todo
  está en marcha termina sin escribir nada. El túnel HTTPS lo reabre la propia pasarela; el bot reintenta solo cuando
  vuelve Internet u Oracle. Un candado `Global\` impide dos ejecuciones simultáneas, y los mutex `Global\` del panel,
  la pasarela y el supervisor del bot impiden procesos duplicados aunque se abran desde otra sesión.
- Antes de iniciar sesión, Ollama se inicia como `ollama serve` (sin bandeja). Los ejecutables, la wallet de Oracle y
  los User Secrets deben estar en disco local (en OneDrive, «Mantener siempre en este dispositivo»).
- Reinicio ante fallo **acotado**: si el panel no queda en línea, el script sale con código 1 y la tarea se repite
  como máximo **3 veces, cada 2 minutos**. Límite de ejecución: 10 minutos. Instancias múltiples: se ignoran.
- Se ejecuta con `conhost.exe --headless`: no aparece ninguna ventana.
- La configuración privada (User Secrets del usuario: token del bot, conexión a Oracle) la leen los propios programas;
  ningún script la abre ni la imprime.

Registro de cada ejecución: `%ProgramData%\EcoAccessP3Framework\Sesiones\arranque-ecoaccess.log` (conteo de procesos
antes y después, y la salida del lanzador; sin secretos). Los registros del bot y la pasarela siguen en la misma carpeta
(`expo-bot-*.log`, `pasarela-*.log`).

## Instalar

Desde la carpeta **oficial** del repositorio (no desde una copia de `.claude\worktrees`), con los programas compilados
en Release (`dotnet build EcoAccess.P3.sln -c Release`):

```powershell
# Equipo anfitrión (terminal de administrador): arranca al encender Windows, sin iniciar sesión.
powershell -ExecutionPolicy Bypass -File scripts\arranque\Instalar-ArranqueEcoAccess.ps1 -AntesDeIniciarSesion
# Equipo de un compañero (sin administrador): al iniciar sesión.
powershell -ExecutionPolicy Bypass -File scripts\arranque\Instalar-ArranqueEcoAccess.ps1
# Otra carpeta o retraso:
powershell -ExecutionPolicy Bypass -File scripts\arranque\Instalar-ArranqueEcoAccess.ps1 -Raiz "C:\ruta\EcoAccess-P3" -RetrasoSegundos 45
```

Volver a ejecutarlo reemplaza la tarea (por ejemplo, tras actualizar los scripts). Si encuentra activa la tarea del
modo anterior `EcoAccess P3 - vigilante del bot` (de otra copia del repositorio), respalda su definición en
`%LOCALAPPDATA%\EcoAccessP3\respaldos` y la deshabilita, para que nunca arranque un segundo bot.

## Ver el estado (solo lectura)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\arranque\Estado-ServiciosEcoAccess.ps1
Get-ScheduledTask -TaskName 'EcoAccess P3 - arranque de la estación' | Get-ScheduledTaskInfo
```

Sale con código 2 si encuentra más de un panel, bot en polling o pasarela.

## Detener

```powershell
# Ver qué haría, sin detener nada:
powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1 -WhatIf
# Detener (pide confirmación; -Confirm:$false para no preguntar). La vigilancia los vuelve a iniciar en 5 min o menos:
powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1
# Detener y que NO vuelvan a arrancar solos hasta quitar la pausa:
powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1 -Pausar
powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1 -QuitarPausa
```

Si el panel informa una tapa en ciclo, el script se niega a cerrarlo: espera a que termine o pulsa **DETENER TODO** en
el panel (o usa `-Forzar`). El script nunca envía órdenes al ESP32. En una emergencia, corta la alimentación de los
servos: detener por software no la reemplaza.

## Reiniciar

```powershell
# Sin reiniciar Windows: inicia lo que falte (no duplica lo que ya corre).
Start-ScheduledTask -TaskName 'EcoAccess P3 - arranque de la estación'
# Reinicio completo de los servicios:
powershell -ExecutionPolicy Bypass -File scripts\arranque\Detener-ServiciosEcoAccess.ps1
Start-ScheduledTask -TaskName 'EcoAccess P3 - arranque de la estación'
# Solo el bot (por ejemplo, tras autorizar una cuenta de Telegram):
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -ReiniciarBot
```

## Desinstalar

```powershell
powershell -ExecutionPolicy Bypass -File scripts\arranque\Desinstalar-ArranqueEcoAccess.ps1
# Además detener los servicios que estén corriendo:
powershell -ExecutionPolicy Bypass -File scripts\arranque\Desinstalar-ArranqueEcoAccess.ps1 -DetenerServicios
```

No toca los User Secrets, los registros ni la tarea anterior `EcoAccess P3 - vigilante del bot` (queda deshabilitada).

## Verificación hecha en la estación (25/11/2026)

- Tarea registrada y comprobada con `Get-ScheduledTask`: estado `Ready`, disparador de inicio de sesión con `PT30S`,
  `RunLevel Limited`, `RestartCount 3`, `RestartInterval PT2M`, `MultipleInstances IgnoreNew`.
- `Start-ScheduledTask` ejecutado **una vez** con todo ya en marcha: resultado `0x0` en 7 s y los mismos 7 procesos
  antes y después (Ollama ×2, panel ×1, bot `--polling` ×1, pasarela ×1, cloudflared ×1, aplicación ×1), con los mismos
  PID. Ningún proceso duplicado, ninguna orden al ESP32, sesión supervisada inactiva.
- No se reinició Windows: el disparador de inicio de sesión queda por comprobar en el próximo inicio de sesión real
  (revisar `arranque-ecoaccess.log`).
