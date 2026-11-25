# ECOACCESS P3 · Modo ExpoSoftware (versión definitiva)

ECOACCESS: diseño e integración de un prototipo inteligente para orientar y registrar la disposición de residuos
aprovechables en Valledupar. Integrantes: Jorge Gutiérrez Thomas, Heyder Canchila De Angel y Saray Cadena Palomino.

Este documento describe lo que funciona en la demostración, cómo se inicia, qué se midió y qué falta. Todo lo que
dice «verificado» se comprobó con el comando indicado; lo físico se marca como pendiente hasta observarlo.

## 1. Arquitectura (un solo dueño del ESP32)

```
PC (FormEstacionExpo) ─┐
Telegram (@EcoAccessP3_JG_bot) ─┼─► ServicioEstacionExpo ─► Panel local :8090 ─► ESP32 (COM3) ─► 3 servos
Web de control (:8090) ─┘        │  (política 5/7/10 s,     (único dueño     firmware CalibracionTresCanecas v2
                                 │   idempotencia)           del puerto)
                                 └─► OPERACION_ESTACION (Oracle Cloud) ◄─ cola local si Oracle no responde
Foto ─► ReconocedorExpo9 (Ollama local, lfm3-vl, 1 inferencia, 512 px) ─► reglas C# ─► canecas
```

- El **panel** (`src/EcoAccess.P3.PanelCanecas`) es el único proceso que abre el puerto del ESP32. El bot y el PC le
  piden las aperturas por HTTP local con la clave de la estación (`%ProgramData%\EcoAccessP3Framework\Panel\clave.txt`).
- Nada se mueve sin **ACTIVAR CONTROL** en el panel (sesión supervisada de 2 h, solo desde la estación anfitriona).
  **DETENER TODO** cancela, cierra y desactiva el control.
- El firmware guarda la calibración de cada tapa (NVS): verde 0→50°, blanca 0→70°, negra 0→40° (valores vigentes el
  25/11/2026, conservados al cargar la v2). Al encender no mueve ningún motor.
- Respaldo y restauración del firmware: `Documents\01_UNIVERSIDAD\2026-2\_Respaldos_EcoAccess\Firmware_ESP32_2026-11-24\`
  (`calibracion_v1\RESTAURAR.txt` y el volcado completo del firmware HMAC oficial).

## 2. Nueve materiales (Modo ExpoSoftware)

| Código | Material | Caneca | Código del catálogo de 35 | Material Oracle |
|---|---|---|---|---|
| E01 | Media manzana | VERDE | — (no existía) | ORGANICO |
| E02 | Cáscara de banano | VERDE | V01 | ORGANICO |
| E03 | Papa o media papa | VERDE | V03 | ORGANICO |
| E04 | Bolsa de papel kraft o cartón arrugado, limpio y seco | BLANCA | B09 | CARTON |
| E05 | Botella plástica pequeña, vacía y limpia | BLANCA | B01 | PET |
| E06 | Frasco pequeño de vidrio, vacío y limpio | BLANCA | B06 | VIDRIO |
| E07 | Papel higiénico usado (muestra higiénica) | NEGRA | N02 | SERVILLETA_USADA |
| E08 | Servilleta usada o contaminada | NEGRA | N01 | SERVILLETA_USADA |
| E09 | Envoltura de mecato metalizada | NEGRA | N08 | PLASTICO_SIN_ID |

El modelo nombra libremente lo que ve en una sola inferencia; `CatalogoExpo9` (C#) lo traduce. Reglas de seguridad:
- Cables, pilas, medicamentos, cuchillas, vidrio roto, personas → **gestión especial: no se abre ninguna caneca**.
- Papel o «tissue» blanco sin señal de uso y papel aluminio → **por confirmar** (un toque: «Es servilleta o papel
  higiénico usado»). Se midió que una bola de papel cuadriculado y una bolsa plástica se leían como «tissue paper».
- Varios residuos de una misma categoría abren esa caneca **una sola vez**; el orden en la foto no importa.
- El catálogo completo de 35 materiales sigue disponible (Mayús + clic en «Reconocer foto» abre la pantalla avanzada).

## 3. Tiempos y aperturas

| Canecas distintas | Tiempo abierta | Modalidad predeterminada |
|---|---|---|
| 1 | 5 s | — |
| 2 | 7 s | simultánea (cada motor arranca 0,25 s después del anterior) |
| 3 | 10 s | simultánea (0,25 s de escalón) |

El temporizador corre en el firmware (orden `GRUPO`), no en el navegador. Modalidad secuencial (una tapa tras otra, cada
una con el tiempo del grupo) con `ECOACCESS_APERTURA_MODALIDAD=secuencial` o el selector del panel. Las pruebas manuales
de calibración usan el tiempo configurable de cada tapa.

## 4. Inicio (tres accesos directos)

`scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionPrincipal` deja en el escritorio real (también si es de
OneDrive) **solo tres** accesos de ECOACCESS; cualquier otro de ECOACCESS se mueve a
`%LOCALAPPDATA%\EcoAccessP3\respaldo_accesos_directos` (no se borra) y los de otros programas no se tocan. Los iconos
(varias resoluciones, 16–256 px) quedan en la carpeta de instalación. Los compañeros (`-Perfil EstacionCompanero`)
reciben ECOACCESS P3 y ECOACCESS WEB.

| Acceso | Qué hace | Icono |
|---|---|---|
| **ECOACCESS P3** | Inicia Ollama si hace falta, el panel, **un solo** bot en polling y la pasarela ciudadana con su túnel HTTPS (ninguno se duplica) y abre la aplicación instalada (`%LOCALAPPDATA%\Programs\EcoAccess P3`, Release). | `assets/identidad/EcoAccess.ico` |
| **ECOACCESS ESP32** | Inicia el panel local si no está activo y abre `http://localhost:8090`. Detecta el ESP32 solo (COM y reconexión USB); si el USB está desconectado, la página abre igual y lo indica. | `assets/identidad/EcoAccess_ESP32.ico` (`generar_icono_esp32.py`) |
| **ECOACCESS WEB** | Abre https://jorgegutho21.github.io/EcoAccess-2026-2/ en el navegador predeterminado. | `assets/identidad/EcoAccess_WEB.ico` (`generar_icono_web.py`) |

Ambos se ejecutan sin ventana de consola (`conhost --headless`) y ninguno mueve motores al abrir.
Equivalente manual: `powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -AbrirPanel`.

Cuentas de Telegram que pueden abrir sin escanear el QR: un ID numérico por línea en
`%ProgramData%\EcoAccessP3Framework\Bridge\telegram-autorizados.txt` (lo crea el supervisor; no se versiona). Cada
persona obtiene su ID enviando **`/miid`** al bot; el supervisor lo añade y aplica la lista con
`powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EstacionExpo.ps1 -ReiniciarBot`.

## 5. Telegram

Menú: Identificar residuo · Abrir caneca · Mi historial · Mis ecopuntos · Estado de la estación · Escanear estación ·
Mi cuenta · Ayuda. Comandos: `/verde`, `/blanca`, `/negra`, `/abrir verde blanca`, `/abrir verde negra`,
`/abrir blanca negra`, `/abrir verde blanca negra`, `/canecas`, `/modo`, `/historial` (= `/misresiduos`), `/miid`.

Flujo de foto (sin mensajes intermedios: solo «escribiendo…» mientras se clasifica):

1. Descarga la variante de la foto de ≥ 640 px (no la de 1280 px): el reconocedor la reduce a 512 px de todos modos.
2. Una inferencia local → validación C# → canecas.
3. Si el chat está autorizado (o escaneó el QR) y el control está activo: **orden al ESP32 inmediatamente**; el registro
   en Oracle corre en paralelo y no retrasa la respuesta.
4. Una sola respuesta con la imagen oficial (ECO verde, ECO blanco, ECO negro o las tres canecas) y el texto:
   «Residuo identificado: botella plástica. / Categoría: aprovechable. / Caneca: blanca. / 🔓 Tienes 5 segundos para
   depositar tu residuo.» Las imágenes (`assets/imagenes/bot`, ≤ 100 KB) se suben una vez y después se reutiliza su
   `file_id` (`%ProgramData%\EcoAccessP3Framework\Telegram\imagenes-file-id.json`).
5. Al cerrar: «✅ Operación completada… Registro guardado correctamente.» **solo** si Oracle confirmó la fila; si no,
   «Registro pendiente». Botón «Sí, deposité» = disposición **declarada** (las tapas no tienen sensores).

Estación desconectada: se informa la caneca correcta y que no se pudo abrir. Ambiguo, desconocido o gestión especial:
no se abre nada. Las pruebas técnicas y aperturas no otorgan ecopuntos.

Historial (`📜 Mi historial`): una cuenta vinculada ve **solo sus** operaciones desde Oracle, una línea por residuo:
«Botella plástica — blanca — 25/11/2026 10:32». Un chat de supervisor sin cuenta ve las de la estación.

## 6. Oracle Cloud

- Migración **V030** `OPERACION_ESTACION` + vista `EA_R_OPERACIONES_ESTACION` (reversión `reversion/R030`).
  Reemplaza la V028 multirresiduo propuesta, que nunca se aplicó en producción (verificado: producción en V029).
- Estados: RECIBIDA, CLASIFICADA, PENDIENTE_APERTURA, EN_EJECUCION, CICLO_COMPLETADO, DISPOSICION_CONFIRMADA,
  REGISTRADA, ERROR, PENDIENTE_SINCRONIZACION. MERGE por clave de idempotencia (`tg:<update>`, `pc:<guid>`, `web:<clave>`):
  una actualización repetida de Telegram o un doble toque no duplica ni la apertura ni la fila.
- Sin Oracle: la operación queda en `%ProgramData%\EcoAccessP3Framework\Estacion\operaciones-pendientes.jsonl` y el bot
  la sube cada 30 s cuando vuelve la conexión (sin repetir la apertura). Las aperturas de la web se suben con canal WEB.
- **Aplicada en producción el 25/11/2026** con autorización, seguida de `05_conceder_permisos_roles.sql` (la tabla queda
  de solo lectura para los roles). Evidencia: `docs/database/evidencias/v030_produccion_20261125.md`.

## 7. Equipos de los compañeros

`scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionCompanero -Wallet <Wallet.zip>`: aplicación + Oracle
(wallet), reconocimiento visual si tienen Ollama, historial compartido. Sin ESP32, la pantalla «Estación de tres canecas»
trabaja en **simulación declarada**. No deben iniciar otro bot (un solo polling por token).
Control remoto de la estación anfitriona solo en la misma red local: en el anfitrión, como administrador,
`netsh http add urlacl url=http://+:8090/ user=<usuario>` y una regla de firewall para el puerto 8090; en el equipo del
compañero `ECOACCESS_PANEL_URL=http://<ip-anfitrión>:8090` y la clave de la estación. Nunca se expone a Internet.

## 8. Página web

- Panel real: `http://localhost:8090` (código en `src/EcoAccess.P3.PanelCanecas`), acceso **ECOACCESS ESP32**.
  Barra superior: USB, ESP32, puerto COM, estación (disponible/ocupada), Oracle y bot (según el pulso que el bot escribe
  cada 30 s en `%ProgramData%\EcoAccessP3Framework\Estacion\bot-estado.json`), ACTIVAR CONTROL y DETENER TODO.
  Cada caneca: abrir, cerrar, detener, probar ciclo, calibrar (grados y tiempo, guardar en el ESP32) y estado.
  Las siete combinaciones usan la misma orden `GRUPO` que Telegram y el PC.
- **Página ciudadana pública**: https://jorgegutho21.github.io/EcoAccess-2026-2/ (`web/ciudadano`). Un solo workflow
  publica GitHub Pages (`.github/workflows/web-ciudadana.yml`, rama `gh-pages`) al llegar a `main`: la página ciudadana
  en la raíz y la demostración del panel (estación simulada) en `/panel-demo/`. El antiguo `pages.yml` se retiró para
  que el demo nunca reemplace la página ciudadana. Cada publicación sube la versión del service worker (`sw.js`).
- **Aperturas físicas desde la página**: GitHub Pages → túnel HTTPS (cloudflared) → pasarela `127.0.0.1:8092` → panel
  `:8090` → ESP32. Solo con el QR presencial que genera el operador en `http://localhost:8093/` (permiso de 2 h, se
  invalida al generar otro) **y** con ACTIVAR CONTROL en el panel. CORS limitado al dominio de Pages, límite de
  aperturas, clave única por intento (recargar no repite) y rechazo de solicitudes de más de 60 s. El túnel rápido
  cambia de dirección en cada arranque: el QR siempre lleva la vigente. Una dirección fija requiere una cuenta y un
  dominio en Cloudflare (decisión del equipo; no se contrató nada).
- **Sincronizador Oracle de la web**: el bot lee cada 30 s `%ProgramData%\EcoAccessP3Framework\Ciudadano\operaciones-ciudadanas.jsonl`
  (y la bitácora de la web de control), sube cada evento a `OPERACION_ESTACION` con MERGE por su clave `web:<clave>`,
  anota los ya confirmados en `Estacion\bitacoras-sincronizadas.txt` (sobrevive a reinicios) y reintenta lo que falló.
  No reescribe la bitácora de la pasarela y nunca mueve una caneca. La confirmación del ciudadano actualiza la misma
  fila a DISPOSICION_CONFIRMADA (declarada, nunca verificada). Probado contra Oracle en `ECOACCESS_P3_PRUEBAS`.
- **Historial conjunto web–Telegram**: en «Mi historial» de la página → «Vincular con Telegram» → el bot recibe
  `/start web_CODIGO` (código de 10 caracteres, un solo uso, 10 minutos) y lo confirma solo si ese chat ya tiene una
  cuenta EcoAccess vinculada → la página canjea el código por un token propio (90 días; el bot guarda solo su SHA-256)
  → la página muestra el historial oficial de Oracle y sus aperturas quedan con su ID_USUARIO, visibles también en
  `/historial` de Telegram y en el PC. Nunca se vincula por nombre. Servicio local del bot: `127.0.0.1:8094`.
- La V030 admite los canales PC, TELEGRAM y WEB: las aperturas del panel técnico se registran como WEB con el
  solicitante del panel (no se creó un canal nuevo para no alterar la migración).

## 9. Mediciones (sin hardware)

### 9.1 Conjunto activo de 100 imágenes (25/11/2026)

`python scripts/vision/construir_dataset_expo9.py` → `datasets/exposoftware9` (objetivo 100, límite absoluto 300):

| Conjunto | Cantidad | Contenido |
|---|---:|---|
| Referencias | 45 | 5 por material: originales + variantes aumentadas etiquetadas |
| Evaluación independiente | 27 | 3 por material: originales nunca usados como referencia; si faltan, derivados digitales (poca luz, sombra, lejanía) de esos mismos originales |
| Multirresiduo | 21 | 4 fotografías reales + 17 composiciones (misma caneca 3, dos canecas 7, tres canecas 7) |
| Negativas | 7 | cable, máquina de afeitar y blíster (gestión especial), papel cuadriculado, bolsa plástica, tomate, papel aluminio |

Origen: 19 reales, 32 ilustraciones del catálogo, 32 aumentadas, 17 compuestas. Sin fuga entre referencia y evaluación
(comprobado por el script). El conjunto anterior de 177 quedó respaldado en
`Documents\01_UNIVERSIDAD\2026-2\_Respaldos_EcoAccess\Dataset_exposoftware9_177_2026-11-25` y en Git. El reconocedor no
envía imágenes de referencia a Ollama (una sola imagen por consulta): el tamaño del conjunto no influye en la latencia.
Siguen faltando fotografías reales de banano, botella, frasco, papel higiénico y envoltura (`manifiesto.json` → `faltantes`).

| Lado | Canecas correctas | Individual | Multirresiduo | Negativas sin apertura | Falsas aperturas | Inferencia media / mediana / P95 / máx. |
|---|---|---|---|---|---|---|
| 320 px | 58 % | 63 % | 40 % | 5/5 | 0 | 1,43 / 1,39 / 1,78 / 2,60 s |
| 384 px | 67 % | 74 % | 50 % | 5/5 | 0 | 2,08 / 1,81 / 3,08 / 5,66 s |
| **512 px (vigente)** | **67 %** | **74 %** | **50 %** | **5/5** | **0** | **1,81 / 1,79 / 2,25 / 2,65 s** |

Por caneca (512 px): verde 88 %, blanca 91 %, negra 35 %. La negra baja porque servilleta y papel higiénico usados se
leen como «tissue»: por seguridad quedan **por confirmar** (un toque) y nunca abren solos. Informes:
`docs/informes/expo9/evaluacion100_lfm3_{320,384,512}.md`.

Desglose en Ollama (muestra de 14 imágenes): la lectura de la imagen y del prompt ocupa ~0,7–0,9 s y la respuesta
JSON (~22–30 tokens) ~0,4–0,5 s; quitar el campo `count` no cambia el tiempo. Carga en frío del modelo al iniciar el
bot: 19,5 s (precalentado al arrancar; después queda en memoria).

Alternativa medida en el mismo equipo (25/11/2026, 5 fotos de evaluación a 512 px): **qwen3-vl:4b** 31–36 s por foto
con el modelo cargado (54 s en frío; 3,3 GB, desplaza a lfm3-vl de la VRAM) y respuestas vacías en 3 de 5. No es viable
en una GTX 1650 de 4 GB; se mantiene **lfm3-vl** (recarga en frío medida: 12–20 s).

**Objetivo de 2 s:** la clasificación local cumple en mediana (1,8 s) y queda cerca en P95 (2,25 s) a 512 px. La respuesta
completa de Telegram suma la descarga de la foto y el envío del mensaje, que dependen de la red y **aún no se han medido
con una foto real** (el registro del bot anota `descarga`, `inferencia` y el instante de la orden al ESP32). Con una
GTX 1650 el límite está en el codificador visual del modelo; para bajar de forma estable de 2 s haría falta un detector
ligero entrenado (p. ej. YOLO/ONNX en C#) con fotografías reales de los nueve materiales, que todavía no existen.

### 9.2 Medición anterior (conjunto de 177, 84 casos)

- Evaluación (`EcoAccess.P3.Bot.exe --evaluar-expo9`, informes en `docs/informes/expo9/`), 84 casos independientes:

| Lado | Canecas correctas | Individual | Multirresiduo | Negativas sin apertura | Falsas aperturas | Inferencia mediana / P95 |
|---|---|---|---|---|---|---|
| 384 px | 56 % | 68 % | 39 % | 13/13 | 0 | 1,48 s / 2,12 s |
| **512 px (elegido)** | **63 %** | **73 %** | **49 %** | **13/13** | **0** | **1,69 s / 2,56 s** |
| 640 px | 64 % | 73 % | 51 % | 13/13 | 0 | 2,05 s / 3,21 s |

- Latencia antes/después en las mismas 22 fotos (`--comparar-latencia-expo9`): recorrido anterior de Telegram
  mediana **7,3 s** (P95 13,9 s) → una inferencia mediana **2,1 s** (P95 2,7 s). Carga en frío del modelo: ~14 s
  (por eso el bot y la app lo precalientan al iniciar y lo mantienen en memoria).
- Los errores restantes son omisiones (casi siempre escenas compuestas de 3–4 ilustraciones pequeñas y la manzana
  verde entera, que el modelo llama «lime»), no aperturas indebidas. Para la demostración: usar media manzana roja o
  cortada y separar los residuos en la foto.

## 10. Pruebas físicas pendientes (una sesión presencial supervisada)

Para cada canal (PC manual, PC foto, Telegram manual, Telegram foto, panel web): C01 verde 5 s, C02 blanca 5 s,
C03 negra 5 s, C04 verde+blanca 7 s, C05 verde+negra 7 s, C06 blanca+negra 7 s, C07 las tres 10 s.
Anotar: caneca correcta, ángulo, tiempo, apertura y cierre **observados**, fila en Oracle y consulta posterior.
El panel tiene «Prueba C01–C07 (5/7/10 s)» en Diagnóstico para la parte web.

## 11. Visual Studio y Visual Studio Code

- **Visual Studio 2022/2026**: abrir `EcoAccess.P3.sln` (proyecto de inicio: `EcoAccess.P3.Presentacion`). Requiere la
  carga de trabajo «Desarrollo de escritorio de .NET» y el .NET Framework 4.8 Developer Pack.
- **Visual Studio Code**: `.vscode/tasks.json` (compilar Debug/Release, pruebas C#, pruebas del catálogo y de
  integración web, iniciar estación) y `.vscode/launch.json` (aplicación, panel, pasarela sin túnel y bot en consola
  simulado, depurador `clr`). Extensión C#. Sin rutas personales ni credenciales.
- Ambos usan el SDK fijado en `global.json` (.NET 10, compila net48). Oracle por User Secrets (`ecoaccess-p3-framework`
  producción, `ecoaccess-p3-pruebas` esquema aislado). El bot real de Telegram no se lanza desde el IDE: una sola
  instancia en polling (acceso ECOACCESS P3).

## 12. Equipos de los compañeros y cambio de anfitrión

- Instalación: `Publicar-Paquete.ps1` genera `dist\EcoAccess-P3-<versión>-<commit>.zip` (aplicación Release, instalador,
  verificador e iconos; sin wallet ni secretos). En su equipo: `Instalar-EcoAccess.ps1 -Perfil EstacionCompanero -Wallet <Wallet.zip>`
  y después `Verificar-Equipo-EcoAccess.ps1 -Perfil EstacionCompanero` (wallet, TNS_ADMIN, alias, conexión C#, Ollama).
  Cada integrante usa sus propias credenciales; nunca inician otro bot.
- Sin ESP32 conectado su aplicación no abre ningún puerto COM: usa el panel del anfitrión por HTTP
  (`ECOACCESS_PANEL_URL=http://<ip-anfitrión>:8090` + clave de la estación, solo red local) o la simulación declarada.
- **Cambiar de anfitrión** (por ejemplo, si el ESP32 se conecta al equipo de Heyder): en el anfitrión actual cerrar
  panel, bot y pasarela (DETENER TODO y salir); desconectar el USB; en el nuevo equipo ejecutar
  `Instalar-EcoAccess.ps1 -Perfil EstacionPrincipal` con el repositorio y abrir ECOACCESS P3. Telegram rechaza un segundo
  consumidor del mismo token (409), así que nunca quedan dos bots activos; el panel es el único dueño del COM.

## 13. Rediseño de las interfaces (25/11/2026)

Sistema de diseño compartido: `docs/diseno/SISTEMA_DISENO_ECOACCESS.md`. Evidencia (capturas antes/después y
Lighthouse): `docs/informes/rediseno/`.

- **ECOACCESS WEB** (`web/ciudadano`): la página es la estación: pared mineral, piso continuo y las tres canecas reales
  de pie; la tapa gira sobre la bisagra trasera mostrando su cara interna y la boca (capas de
  `scripts/web/separar_capas_canecas.py`). Móvil con consulta en un dock inferior y las tres canecas en la primera
  pantalla (320–430 px); escritorio con la consulta a la izquierda y el escenario a la derecha. ECO con estados de voz
  (escuchando con ondas, interpretando, no comprendido…), temporizador circular grande, historial con pestañas
  «En este dispositivo» / «Historial oficial». Service worker v3. Pruebas: `node --test web/ciudadano/pruebas/interfaz.test.mjs`
  (Playwright local; se omite si no está).
- **ECOACCESS ESP32** (`src/EcoAccess.P3.PanelCanecas/wwwroot`): tablero oscuro «bosque nocturno»; barra fija con
  USB, ESP32, COM, estación, Oracle, Telegram y pasarela (punto + palabra + antigüedad; pulso de más de 90 s =
  «Sin actualización»); franja de conexión física; tarjetas por caneca con la tapa por capas que refleja el estado
  comandado («Posición desconocida» sin conexión); calibración con transportador y cambios «actual → nuevo», guardado
  confirmado solo tras releer el ESP32; siete combinaciones con miniaturas; **Verificar sistema** (`GET /api/diagnostico`,
  solo lectura).
- **Vista previa local de la web nueva** (sin tocar GitHub Pages hasta fusionar):
  `powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Abrir-VistaPrevia-Web.ps1` (modo demostración) o con
  `-SinDemo` (modo consulta). El panel nuevo ya es el instalado: acceso **ECOACCESS ESP32**.

## 14. Consolidación del 25/11/2026 (11 residuos, Telegram, invitados)

- **Error de Telegram «No pude descargar la foto»**: causa real, .NET Framework limita a 2 las conexiones por servidor;
  el largo polling ocupaba una y getFile esperaba hasta vencer (registro: 10 159 ms). Solución: límite de 16 conexiones,
  un HttpClient exclusivo para el polling y un reintento acotado de getFile ante errores transitorios.
- **Catálogo visual de 11 residuos** (M01–M11): papa, limón, manzana verde, lulo, cáscara de banano (verde); papel
  aluminio, envase de vidrio rosado de sérum, papel de cuaderno, papel kraft (blanca); envase de pegamento, servilleta
  usada (negra). Una inferencia devuelve nombre libre + etiqueta; la etiqueta solo vale si el nombre es compatible, y
  los pares que cambian de caneca se confirman con una pregunta sí/no (vidrio rosado/pegamento, papel marrón/blanco,
  cuadrícula/servilleta). Conjunto: `datasets/ecoaccess11` (`python scripts/vision/construir_dataset_expo11.py`).
- **Evaluación real** (`docs/informes/expo11/evaluacion_lfm3_512.md`): 25/25 canecas correctas en las fotos reales
  (24/25 material exacto), 8/9 negativas sin apertura, 6/10 escenas compuestas (3 piden confirmación). Las fotos son de
  un objeto y una sesión y se usaron para ajustar: **no prueban generalización**. Faltan fotos de cáscara de banano y
  de otros objetos/sesiones. Limitación medida: una bolsa plástica arrugada se lee como servilleta (negra).
- **Latencia** (GTX 1650): ~2 s frutas y aluminio; ~3,4 s frascos y kraft; ~5,7 s papel blanco (dos verificaciones).
- **Política 7/14/21 s** por canecas distintas en C#, panel, pasarela, web y demo (el firmware admite hasta 30 s).
- **Telegram**: «🟢 Papa detectada.» / «Caneca verde abierta. Puedes desechar tu residuo.» / «Tiempo disponible: 7
  segundos.»; sin confirmación del ESP32: «Identifiqué tu residuo, pero la caneca no está conectada en este momento».
- **Web**: catálogo ampliado a 93 entradas (las 90 originales + limón, lulo y envase de pegamento) con sinónimos
  (patata, naranjilla, hoja cuadriculada, sérum, Gota Mágica, Astro Glue…); «pegamento» pregunta si está vacío.
- **Invitados y detalle (V033)**: cada operación guarda tipo de usuario (INVITADO/REGISTRADO), método (FOTO, TEXTO,
  VOZ, MANUAL) y una fila por residuo. Probada en el esquema aislado (`docs/database/evidencias/v031_pruebas_20261125.md`);
  pendiente de autorización para producción.
- **Estadísticas en el PC**: Estadísticas → «Operaciones de la estación» (filtros por fecha, material, caneca, canal
  y tipo de usuario; invitados frente a registrados, Telegram frente a web, aperturas ejecutadas y fallidas, materiales,
  horas y el historial), siempre desde Oracle.
