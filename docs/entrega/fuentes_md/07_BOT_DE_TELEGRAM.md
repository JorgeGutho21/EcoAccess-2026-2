---
archivo: 07_BOT_DE_TELEGRAM
titulo: El bot de Telegram de EcoAccess P3
subtitulo: Configuración, recepción de mensajes, QR, fotos, vinculación, historial, servicios y persistencia, con el recorrido exacto de clases y métodos
codigo: 07
---

# Qué hace el bot

El bot `@EcoAccessP3_JG_bot` es la puerta de entrada para los ciudadanos. Con él una persona puede:

- escanear el QR de una caneca y recibir orientación;
- enviar una foto de un residuo y saber a qué caneca va (blanca, verde, negra o posconsumo);
- si tiene cuenta vinculada y la caneca tiene dispositivo, pedir que se abra la tapa correcta;
- consultar sus depósitos, estadísticas y ecopuntos;
- registrarse con un enlace de un solo uso, vincular una cuenta existente o desvincularse;
- calificar el servicio y retirar su autorización para enviar fotos a un servicio externo.

El bot es un programa de consola (`src/EcoAccess.P3.Bot`) que se ejecuta en el PC del propietario. Usa **long polling**:
pregunta a Telegram cada cierto tiempo si hay mensajes nuevos. Así no necesita abrir puertos de entrada.

# Configuración inicial

| Clave de configuración | Para qué sirve | Dónde se guarda |
|---|---|---|
| `Telegram:TokenBot` | token que entrega @BotFather | User Secrets (`ecoaccess-p3`), nunca en el repositorio |
| `Telegram:NombreBot` | nombre público para los enlaces `t.me/...` | User Secrets |
| `Telegram:PaginaRegistro` | URL pública de la página de registro | User Secrets |
| `Telegram:QrDemo` | QR de la caneca DEMO para demostraciones | User Secrets |
| `ConnectionStrings:OracleP3` | conexión a Oracle Cloud | User Secrets |
| `Vision:Ollama:MantenerCargado` | tiempo que Ollama mantiene el modelo en memoria (por defecto 15 min) | User Secrets |
| `Vision:Groq:Modelo` | modelo remoto opcional | User Secrets |

El script `scripts/instalacion/Configurar-Credenciales.ps1 -Telegram` pide el token y lo guarda; `EcoAccess.P3.Bot.exe
--comprobar-telegram` verifica la identidad del bot sin enviar mensajes.

# Modos de ejecución

El ejecutable acepta un modo en la línea de comandos (`src/EcoAccess.P3.Bot/Program.cs`):

| Modo | Qué hace |
|---|---|
| `--polling` | modo normal: atiende Telegram |
| `--webhook` | recibe mensajes por HTTPS (alternativa a polling) |
| `--comprobar-oracle`, `--comprobar-telegram`, `--estado-vision` | diagnósticos sin modificar datos |
| `--evaluar-objetos <manifiesto> local` | mide el reconocimiento con fotos etiquetadas, sin crear solicitudes ni depósitos |
| `--auditar-recorrido <minutos>` | cuenta interacciones, órdenes, eventos y depósitos recientes y busca duplicados |
| `--generar-lote-simulacion <SIM-ID>` | crea el lote experimental (solo una vez por lote) |
| `--bridge`, `--dispositivo-serial`, `--dispositivo-simulado` | comunicación con la caneca |

# Arranque y vigilancia

- `scripts/desarrollo/Iniciar-EcoAccess-Nube.ps1` valida Oracle y Telegram e inicia el bot en modo `--polling`.
- La tarea programada **«EcoAccess P3 - vigilante del bot»** ejecuta `scripts/desarrollo/Vigilar-Bot.ps1` al iniciar sesión y cada 5 minutos. Si el bot de esta copia no corre, lo inicia. Si corre el bot de **otra** copia, no compite por el token.
- **Corrección del cierre:** el vigilante y el lanzador comparaban la ruta del bot con la carpeta sin la barra final. Por eso `EcoAccess-P3` tomaba como propio el bot de `EcoAccess-P3-auditoria`, y el vigilante nunca levantaba el bot nuevo. Ahora se compara con `carpeta + "\"`.
- **Recuperación verificada (11/11/2026):** se detuvo el bot, se compiló Release y la tarea lo relanzó sola desde `EcoAccess-P3\src\EcoAccess.P3.Bot\bin\Release\net48` (registro en `C:\ProgramData\EcoAccessP3Framework\Desarrollo\vigilante.log`).

# Recorrido de un mensaje

1. `BuclePolling.EjecutarAsync` pide a Telegram los updates pendientes (`getUpdates`).
2. Cada update pasa por `TraductorUpdates`, que lo convierte en un `UpdateRecibido` interno (sin datos que no hagan falta).
3. `ProcesadorUpdates.ProcesarAsync` lo entrega a la lógica, `ServicioConversacionTelegram.ProcesarAsync(MensajeEntrante)`, y envía la `RespuestaBot` (texto y botones) por `IClienteTelegram`.
4. `UPDATE_TELEGRAM` guarda los updates ya procesados: si Telegram reenvía el mismo, no se atiende dos veces.
5. `CONVERSACION_TELEGRAM` guarda en qué paso va cada persona, con una **versión optimista**: si dos mensajes llegan a la vez, uno gana y el otro se reintenta, sin pisarse.

# El QR y la presencia

El QR de la caneca contiene un enlace `t.me/<bot>?start=<IdentificadorQr>`. El oficial del piloto es
`EcoAccessCanecaPiloto1` (caneca 1093).

- Escanearlo identifica la caneca y, desde V024, su **estación de tres canecas** (blanca, verde, negra).
- La **presencia** dura 5 minutos (`VigenciaPresencia`) y vive en memoria. Una foto sola no demuestra que la persona esté junto a la caneca.
- Reiniciar el bot borra la presencia: hay que volver a escanear.

# La foto

`ServicioConversacionTelegram.FlujoEco.cs` contiene el recorrido definitivo de Eco: saludo, foto, apertura y despedida.

1. La foto se descarga con un límite de tamaño. Los clasificadores la reducen y normalizan con `PreparadorImagen` antes de enviarla al modelo.
2. `ServicioClasificacionImagen.ClasificarAsync` pide al motor de visión una propuesta (documento 08).
3. `ConDestinoEstacionAsync` usa `ResolutorDestino` para decir el color de la caneca y su ubicación.
4. Si `PoliticaAperturaAutomatica.Permite`, el bot ofrece abrir; la apertura se anuncia cuando la caneca la **confirma**, no antes.
5. Las canecas verde y negra del piloto están **fuera de servicio** hasta que existan físicamente: el bot las nombra y orienta, pero no las abre.

**Privacidad:** la foto no se guarda. En Oracle queda solo una traza (`CONSULTA_VISION`, V023) con resultado, material y
latencia, sin imagen ni identificador de Telegram. Una foto con personas no se ofrece a servicios externos.

# Registro y vinculación

- `/vincular` o el botón de registro genera un **token de un solo uso** (`TokenVinculacion`, vigente 10 minutos). En Oracle se guarda solo su hash SHA-256.
- La página `PaginaRegistro` (puerto local 8081, publicada con Tailscale Funnel) recibe nombre, correo, contraseña y consentimiento, y llama a `ServicioVinculacionTelegram.RegistrarCiudadanoAsync`.
- Una cuenta existente se vincula con `PrepararVinculacionCuentaExistenteAsync` y un código que la persona escribe en Telegram (`VINCULACION_PENDIENTE`).
- `/desvincular` registra la baja en `VINCULO_TELEGRAM` (no borra la historia).

# Comandos reconocidos

Los comandos que la lógica reconoce, según el código, son:
`/acerca`, `/ayuda` (`/help`), `/calificar`, `/cancelar`, `/cantidad`, `/categorias`, `/confirmar`, `/demo`, `/depositos`,
`/desvincular`, `/eco_informacion`, `/ecopuntos`, `/educacion`, `/estadisticas`, `/estado`, `/evolucion`, `/foto`,
`/inicio`, `/manual`, `/materiales`, `/menu`, `/mismateriales`, `/operaciones`, `/potenciales`, `/privacidad`,
`/proximamente`, `/revocarfotos`, `/separar` y `/vincular`. También se entienden frases como «mis ecopuntos» o
«mi saldo» (`IntencionesTelegram`).

# Clases del bot

{{tipos:Presentación (bot de Telegram)}}

# Qué está verificado y qué no

| Aspecto | Estado | Evidencia |
|---|---|---|
| Identidad del bot y polling | Verificado real | `--comprobar-telegram`; bot en vivo desde `EcoAccess-P3` |
| Recuperación por la tarea vigilante | Verificado real | relanzado por la tarea el 11/11/2026 |
| Flujo Eco completo (QR, foto, destino, ventana, depósito) | Verificado en simulación | `FlujoEcoDefinitivoTests`, `BridgeOracleTests`, `EstacionSimuladaTests` |
| Tres canecas desde el QR oficial | Verificado real (datos en Oracle) | `EstacionTresCanecasOracleTests` contra la nube |
| Prueba desde un teléfono real con foto real | **Pendiente del propietario** | requiere enviar la foto desde el iPhone |
| Apertura física de tapas | **No realizada** | prohibido mover motores en la misión de cierre |

# Preguntas probables

**¿Por qué long polling y no webhook?** Porque no exige abrir puertos ni tener un dominio con HTTPS apuntando al PC. El
webhook existe como alternativa (`ServidorWebhook`), con un secreto en el encabezado.

**¿Qué pasa si llegan dos mensajes del mismo usuario a la vez?** `CONVERSACION_TELEGRAM` tiene versión optimista: el
repositorio guarda solo si la versión no cambió. El segundo mensaje se reintenta con el estado nuevo.

**¿El bot guarda las fotos?** No. Solo guarda metadatos (`CONSULTA_VISION`) y, si hubo solicitud, el análisis estructurado (`ANALISIS_IMAGEN`).
