# Diagnóstico: el bot dejó de responder

Fecha: 2 y 3 de noviembre de 2026. Bot: `@EcoAccessP3_JG_bot`.

## Causa real

**Ningún proceso estaba leyendo los mensajes.** Las credenciales estaban bien.

| Comprobación | Resultado |
|---|---|
| `EcoAccess.P3.Bot.exe` en ejecución | ninguno |
| `getMe` | identidad correcta |
| Webhook | sin webhook |
| Actualizaciones pendientes | subieron de 8 a 12 mientras la persona escribía: Telegram las recibía |
| Oracle del proyecto | 0 updates, 0 conversaciones, 0 análisis: este bot nunca había atendido nada |
| Sesiones `EcoAccessP3Framework\Sesiones` | solo Bridge y Simulador, sin Telegram |
| Sesión anterior de desarrollo del bot (tarea de Windows en la carpeta `EcoAccessP3`) | atendió hasta el 1 de noviembre a las 21:27 y se detuvo el 2 a las 09:36 |

Las respuestas anteriores, incluidas «envase multicapa» y «No encontré ese material», salieron de esa sesión anterior. No se determinó quién la detuvo.

## Defectos corregidos

| Defecto | Corrección |
|---|---|
| Saludos, correcciones y `Cancelar` se buscaban como materiales | `IntencionesTelegram`: lista cerrada, sin modelo, antes de buscar en el catálogo |
| `Te equivocaste` respondía «No encontré ese material» | Con una identificación reciente ofrece cambiar material, otra foto, segunda opinión y menú. Sin ella, lo explica |
| `/start` creaba una solicitud y no mostraba el menú completo | Bienvenida y 12 opciones; no crea ni cancela solicitudes |
| Un fallo inesperado de `getUpdates` podía terminar el proceso | Se registra y se reintenta con espera creciente |
| Un diagnóstico «correcto» con el bot apagado | Informa `BOT DETENIDO`, mensajes pendientes, latido real y supervisor |
| Nada reiniciaba el bot | Supervisor `--supervisar`, con mutex y un máximo de 5 reinicios en 30 minutos |

El supervisor no reinicia ante un webhook, un conflicto de consumidor o un token rechazado: esos casos necesitan a una persona. Tampoco reinicia por falta de internet, porque el bucle ya reintenta. Un bot vivo que lleva 3 minutos sin pedir mensajes y sin fallos de red sí se reinicia.

## Windows Defender

Defender bloqueó tres scripts recién escritos con el error «contains a virus or potentially unwanted software»: `Iniciar-EcoAccess-Telegram.ps1`, `Iniciar-Bot.ps1` y `Registrar-InicioAutomatico.ps1`. No se crearon exclusiones. Una variante sin el bloque que recorría `*.cs` y recompilaba y sin `powershell -ExecutionPolicy Bypass` anidado siguió legible, pero no se aisló qué línea exacta lo dispara. Los tres archivos siguen bloqueados en la carpeta, ignorados por Git, hasta que Defender los resuelva.

Por eso **el lanzador es `Iniciar-Bot-Eco.ps1`**, y el supervisor está en C#. El inicio automático con Windows no quedó implementado.

## Evidencia

| Servicio | Prueba | Resultado | Latencia | Clase |
|---|---|---|---|---|
| Telegram | Bot real con mensajes pendientes de la cuenta | 6 updates registrados en Oracle; atención y envío sin errores en el registro | 67–367 ms de atención, 470–825 ms de envío | real, **sin confirmar la recepción en el teléfono** |
| Telegram | `/start`, `Hola`, foto y corrección desde el teléfono | **pendiente** | — | necesita a la persona |
| Telegram | Segunda cuenta | **pendiente** | — | necesita otra cuenta |
| Supervisor | Se mató el proceso de Telegram | Reiniciado en 16 s, sin segundo consumidor | — | real |
| LFM3-VL | 198 y 66 fotos | 144/198 y 46/66 | mediana 2,3 s | real |
| Qwen 3.8 | Una foto de cartón por el comando de comprobación | `CARTON` | 4,6 s | real |
| Qwen 3.8 | Comparación de 66 fotos | **incompleta**: unas 40 aceptadas y resultados perdidos | — | ver abajo |
| Oracle | Catálogo con el perfil real | 41 materiales | — | real |
| Caneca simulada | `RecorridoCompletoDeEco…`, Oracle temporal y modelo real | aprobado | 45 s | Telegram simulado |
| Desconexión de Telegram | Corte real de red | **no probado**; solo con cliente falso | — | simulado |

**Groq:** tras 98.600 tokens pidió esperar 152 s y luego 1075 s, muy por debajo de los 200.000 diarios publicados. Sugiere un límite menor en esta cuenta; no está confirmado. Mientras tanto, la segunda opinión puede pedir que se espere. La comparación ahora guarda cada foto y continúa con el mismo comando.

## Actualización: la primera foto del invitado

El mismo 3 de noviembre apareció un segundo defecto: la primera foto de un invitado se rechazaba porque la sesión aún no estaba en Oracle. Causa, corrección y flujo nuevo: `FLUJO_ECO_DEFINITIVO.md`.

## Operación

Desde la carpeta del proyecto, en Windows PowerShell:

```powershell
# Diagnóstico
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Verificar-EcoAccess-Telegram.ps1
# Iniciar: comprueba, aplica migraciones pendientes e inicia el supervisor
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-Bot-Eco.ps1
# Detener
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Detener-Demostracion.ps1
```

| El diagnóstico dice | Qué significa | Qué hacer |
|---|---|---|
| `BOT DETENIDO: N mensajes` | No hay consumidor | Iniciar |
| `Telegram: recepción NO DISPONIBLE` | Sin red hacia Telegram | Revisar internet; se recupera solo |
| `Telegram: recepción ERROR` | El proceso existe pero no pide mensajes | El supervisor lo reinicia a los 3 minutos |
| `Supervisor ERROR` | Conflicto, token o demasiados reinicios | Leer `supervisor.log` y los registros de la sesión |

Compila solo con el bot detenido: sus archivos quedan bloqueados mientras corre. Los registros están en `C:\ProgramData\EcoAccessP3Framework\Sesiones\<fecha>`. No elimines mensajes pendientes: Telegram los entrega al reiniciar.

Si el computador está apagado, Eco no responde. Telegram guarda los mensajes y los entrega al volver.

## Comprobar Oracle después de la prueba

```powershell
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --auditar-recorrido 60
```

Solo lee y cuenta. Las interacciones en la caneca piloto configurada como demostración quedan en el canal `TelegramSimulado`: son ensayo, no actividad física.
