# Guía: bot de Telegram con dos motores de reconocimiento

Un único bot, `@EcoAccessP3_JG_bot`, con dos motores intercambiables:

| Motor | Dónde corre | Modelo | ¿Necesita clave? |
|---|---|---|---|
| Local (predeterminado) | tu computador, con Ollama | `lfm3-vl:latest` | no |
| Externo | servidores de Groq | `qwen/qwen3.8-27b` | sí |

Abre **tu propia ventana de PowerShell** en la carpeta del proyecto. Todos los comandos parten de ahí:

```powershell
cd "C:\ruta\a\EcoAccess-P3"
```

Atajo si ya está todo instalado: partes F e I.

## Parte A. Docker y Oracle

```powershell
docker ps --filter "name=oracle-free-p3" --format "{{.Names}} {{.Status}}"
```

Debe decir `Up ... (healthy)`. Si no aparece, abre Docker Desktop y ejecuta `docker start oracle-free-p3`. Si el contenedor no existe, créalo con `docs\instalacion\11_instalacion.md`. Luego:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Preparar-BaseDatos.ps1
```

Repetirlo es seguro: solo aplica migraciones pendientes y no borra datos. La migración `V018` crea la tabla de autorizaciones de fotos.

## Parte B. Ollama

```powershell
ollama --version
```

Si no existe, instálalo desde `https://ollama.com/download` y vuelve a abrir PowerShell. Comprueba que responde:

```powershell
Invoke-RestMethod http://127.0.0.1:11434/api/version
```

## Parte C. LFM3-VL

```powershell
ollama pull lfm3-vl
ollama list
```

`lfm3-vl:latest` debe aparecer en la lista. Compila y prueba una inferencia real con una foto tuya (no crea solicitudes ni órdenes):

```powershell
dotnet build EcoAccess.P3.sln -c Release
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-vision-local "C:\ruta\foto.jpg"
```

## Parte D. Obtener la clave de Groq

Solo si vas a usar los modos Groq o Automático.

1. Entra en `https://console.groq.com/keys` e inicia sesión.
2. Pulsa **Create API Key**, ponle un nombre (por ejemplo `EcoAccess`) y copia la clave. Solo se muestra una vez.
3. Revisa tus límites en `https://console.groq.com/settings/limits`. El modelo está en **vista previa**: Groq puede retirarlo con poco aviso.

No pegues la clave en chats, documentos ni capturas.

## Parte E. Guardar la clave de forma segura

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Credenciales.ps1 -Groq
```

La escritura no se muestra en pantalla. El script comprueba la clave contra la API oficial y que el modelo exista **antes** de guardar nada. Queda en los User Secrets de tu perfil de Windows, fuera del repositorio, y se reutiliza en cada inicio. Para comprobarlo:

```powershell
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-groq
```

`"Estado": "Operativo"` significa que la clave y el modelo responden; no envía ninguna imagen. Para probar una inferencia real, que **sí envía esa foto a Groq**:

```powershell
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-vision-groq "C:\ruta\foto.jpg"
```

Límites configurables en los mismos User Secrets, con estos valores por omisión: `Vision:Groq:MaximoSolicitudesDia` 200, `Vision:Groq:PresupuestoDiaUsd` 1.00, `Vision:Groq:TiempoMaximoSegundos` 20. Precios: `Vision:Groq:PrecioEntradaMillon` 0.80 y `Vision:Groq:PrecioSalidaMillon` 4.00 USD por millón de tokens; actualízalos si Groq los cambia.

## Parte F. Token de Telegram

Con el bot existente no hace falta crear otro. En Telegram, abre `@BotFather`, envía `/mybots`, elige `@EcoAccessP3_JG_bot` y pulsa **API Token**.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Credenciales.ps1 -Telegram
```

El script rechaza un token que no pertenezca a ese bot. Si ya lo configuraste, no repitas este paso.

## Parte G. Verificar la identidad del bot

```powershell
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-telegram
```

Debe decir `Identidad de Telegram comprobada: @EcoAccessP3_JG_bot` y `Sin webhook activo`. No consume mensajes.

## Parte H. Elegir el motor

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Credenciales.ps1 -Modo Local
```

| Modo | Qué hace | ¿Sale la foto del equipo? |
|---|---|---|
| `Local` | Solo LFM3-VL; si falla, selección manual | nunca |
| `Groq` | Solo Qwen 3.8; si falla, selección manual | sí, tras autorizar |
| `Automatico` | LFM3-VL; Groq solo si el local **falla** | solo si la persona autorizó |

En `Automatico`, una foto ambigua **no** es un fallo: no se envía a Groq sola. La persona ve el botón «Segunda opinión externa» y decide. `Comparacion` solo aplica a la herramienta de medición.

Reinicia el bot después de cambiar el modo.

## Parte I. Iniciar Telegram

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EcoAccess-Telegram.ps1
```

Comprueba Docker, Oracle, migraciones, identidad y motor, evita duplicados e inicia Bridge, caneca simulada y bot. Debe terminar con `Servicios activos: Bridge, Simulador, Telegram`.

Estado y detención:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Verificar-EcoAccess-Telegram.ps1
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Detener-Demostracion.ps1
```

Inicia el bot en **un solo computador**: Telegram admite un consumidor por token.

## Parte J. Primera fotografía

1. En tu teléfono abre `@EcoAccessP3_JG_bot` (o escanea `assets\identidad\QR_Caneca_piloto.png`) y envía `/start`.
2. Envía una foto de un solo residuo, con buena luz.
3. Eco responde qué parece ser. Pulsa **Sí, es correcto** o **Cambiar material**.
4. En modo Groq verás primero un aviso: la foto no sale hasta que pulses la autorización. Para retirarla, envía `/revocarfotos`.

## Parte K. Verificar el registro en Oracle

```powershell
@"
ALTER SESSION SET CONTAINER=PRUEBAP3;
ALTER SESSION SET CURRENT_SCHEMA=ECOACCESS_P3_APP;
SET LINESIZE 160 PAGESIZE 50
SELECT id_interaccion, canal, estado, fecha_inicio_utc FROM interaccion ORDER BY id_interaccion DESC FETCH FIRST 5 ROWS ONLY;
SELECT telegram_user_id, version_aviso, fecha_otorgado_utc, fecha_revocado_utc FROM consentimiento_vision_externa;
EXIT
"@ | docker exec -i oracle-free-p3 bash -c "sed '1s/^\xEF\xBB\xBF//' | sqlplus -s / as sysdba"
```

La aplicación de escritorio también muestra los depósitos y su estado.

## Parte L. Conectar el ESP32 por USB

Sigue `hardware\README.md`: carga del firmware, `configuracion_privada.h` y calibración sin tapa. Identifica el puerto:

```powershell
[System.IO.Ports.SerialPort]::GetPortNames()
```

Añade `"PuertoSerial": "COMx"` dentro de `Bridge` en `C:\ProgramData\EcoAccessP3Framework\Bridge\bridge.local.json` e inicia:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EcoAccess-Telegram.ps1 -DispositivoSerial
```

Esto reemplaza la caneca simulada. Nunca uses ambas a la vez.

## Parte M. Apertura física segura

**La fuente de 5 V y 1 A no está validada** para dos servomotores bajo carga. Antes de mover la tapa:

1. Mide el consumo de arranque y de bloqueo con una fuente adecuada; no alimentes los servos desde los 3,3 V del ESP32.
2. Une las tierras de fuente, servos y ESP32.
3. Calibra **sin tapa**: sentido de giro, topes y que los motores no se opongan. Solo entonces pon `CALIBRACION_VERIFICADA = true`.
4. Quédate junto a la caneca durante la primera apertura, con la fuente al alcance, y mide con cronómetro los 20 s.
5. Si algo se bloquea o se reinicia, corta la alimentación. El firmware queda en recuperación manual y no reintenta.

Un recibo confirma la orden PWM, no la posición de la tapa ni el ingreso del residuo. Sin sensor, el depósito queda pendiente de verificación y no genera ecopuntos hasta que un operador lo confirme.

## Parte N. Errores y recuperación

| Lo que ves | Causa | Qué hacer |
|---|---|---|
| `Ya hay servicios de EcoAccess en marcha` | otro proceso del bot | `Detener-Demostracion.ps1` y repetir |
| `Conflict` o código de salida 3 | otro consumidor o un webhook activo | detén el otro computador; `--comprobar-telegram` indica si hay webhook |
| Telegram rechazó el token (código 4) | token revocado | `Configurar-Credenciales.ps1 -Telegram` |
| `Falta ConnectionStrings:OracleP3` | el script corrió fuera de tu perfil | ejecútalo en tu PowerShell; `Preparar-BaseDatos.ps1` |
| Oracle no responde | Docker reiniciado | abre Docker Desktop; el lanzador arranca Oracle |
| «El reconocimiento no está disponible» | Ollama detenido y sin respaldo | inicia Ollama o elige el material a mano |
| Groq `CredencialRechazada` | clave revocada | nueva clave con `-Groq`; reinicia el bot, que deja de insistir hasta entonces |
| Groq pide esperar (429) | límite de la cuenta | el bot respeta `Retry-After`; usa selección manual mientras tanto |
| `Se alcanzó el límite diario` | presupuesto propio | sube `Vision:Groq:MaximoSolicitudesDia` o espera al día siguiente (UTC) |
| Groq `ModeloNoDisponible` | modelo retirado | define `Vision:Groq:Modelo` con un sustituto compatible; nunca se cambia solo |
| «Ese archivo no lo puedo analizar» | PDF, video, audio o imagen de más de 5 MB | envía una foto JPEG, PNG o WebP |
| La caneca no se abre | Bridge detenido o ESP32 desconectado | `Verificar-EcoAccess-Telegram.ps1`; el bot no anuncia una apertura que no ocurrió |

Registros de cada inicio: `C:\ProgramData\EcoAccessP3Framework\Sesiones\`. No contienen imágenes ni claves.

## Parte O. Validación con credenciales ya configuradas

Ejecuta cada paso en tu PowerShell, en la carpeta del proyecto, y revisa el resultado antes de seguir.

| # | Comando o acción | Resultado esperado |
|---|---|---|
| 1 | `powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Ejecutar-ComparacionGroq.ps1` | pide confirmación; unos 30 minutos; termina con la tabla y `comparacion_groq.json` válido |
| 2 | `Configurar-Credenciales.ps1 -Modo Automatico` | `Motor visual: Automatico` |
| 3 | `Iniciar-EcoAccess-Telegram.ps1` | `Servicios activos: Bridge, Simulador, Telegram` |
| 4 | Teléfono: `/start`, una foto, confirmar | LFM3-VL responde, botones y apertura **simulada** de 20 s |
| 5 | Teléfono: otra foto y `🔎 Segunda opinión externa`; autoriza | responde Qwen; la foto solo sale tras autorizar |
| 6 | `/estado`, `/depositos`, `/ecopuntos`, `/manual`, `/revocarfotos` | respuesta comprensible en cada uno |
| 7 | `EcoAccess.P3.Bot.exe --auditar-recorrido 60` | canal `Telegram`, los dos modelos en `AnalisisPorModelo` y `SinDuplicados: true` |

El script se detiene sin enviar nada si falla una comprobación previa. Si la foto de prueba responde `HTTP 400`, el código entre paréntesis indica qué rechazó Groq. La comparación se ejecuta con el bot detenido, para que no compita por el modelo local.
