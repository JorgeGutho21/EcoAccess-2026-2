# EcoAccess P3 · Informe final de integración (25/11/2026, actualizado 18:40)

> La sección **14** recoge la última actualización de la tarde y prevalece sobre las cifras anteriores de visión,
> accesos directos, pruebas y estado del ESP32.

Único informe de esta entrega. Todo lo que no se probó en esta sesión sobre la versión integrada queda como pendiente.
Los resultados históricos (2518 C#, V031 14/14, 93 entradas, 7/14/21 s) **no** se toman como pruebas: cada cifra de
abajo se midió hoy sobre la rama integrada.

**Leyenda:** VERIFICADO · VERIFICADO MEDIANTE SIMULACIÓN · PENDIENTE DE ACCESO · PENDIENTE DE PRUEBA FÍSICA · ERROR

## 1. Identificación

| Elemento | Valor |
|---|---|
| Rama | `integracion/ecoaccess-final` (PR #28, actualizado sin forzar) |
| PR | https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/28 (abierto: faltan pruebas físicas, ver §12) |
| Web ciudadana y plataforma académica | https://jorgegutho21.github.io/EcoAccess-2026-2/ (`web/ciudadano/js/rutas.js` + 14 módulos interactivos en `js/secciones/*.js`) |
| EcoAccess Challenge | https://jorgegutho21.github.io/EcoAccess-2026-2/juego/ (200, verificada en el navegador; ronda de 10 preguntas preparada en `V034`/`V035`) |
| Panel ESP32 (Centro de Control) | http://localhost:8090 (200, rediseño industrial con placa ESP32 y bus hacia las 3 canecas) |
| Bot | https://t.me/EcoAccessP3_JG_bot |
| Arranque automático Windows | Tarea programada `EcoAccess P3 - arranque de la estación` (`Ready`, `0x0`, `scripts/arranque/*`) |
| Accesos directos en Escritorio | Exactamente 3 (`ECOACCESS P3`, `ECOACCESS ESP32`, `ECOACCESS WEB`). `ECOACCESS CHALLENGE` se retiró (movido a `%LOCALAPPDATA%\EcoAccessP3
espaldo_accesos_directos`, no borrado): el juego vive dentro de ECOACCESS WEB (`#/challenge`) y `/juego/` sigue respondiendo 200 |

## 2. Hallazgo clave: numeración de migraciones

Producción (`ECOACCESS_P3_APP`, registro `ECO_MIGRACION_APLICADA`) ya tenía **V031 y V032 aplicadas como las
migraciones del juego** (25/11/2026 15:54 UTC, rama `feat/ecoaccess-challenge`). La V031 de invitados y detalle del
PR #28 **nunca se aplicó en producción**. Para no modificar migraciones aplicadas:

- Juego: `V031__juego_ecoaccess_challenge.sql` y `V032__juego_paquete_ords.sql` se quedan igual (aplicadas).
- Invitados y detalle por residuo: **V031 → V033** (`V033__operacion_estacion_detalle_invitados.sql`, reversión
  `R033`), con su contenido intacto salvo el número. Se actualizaron las referencias en C#, las pruebas y la documentación.
- `PlanInstalacion` añade **V030** (faltaba en el plan) y **V033**.

| Punto | Estado | Evidencia |
|---|---|---|
| Orden real R033 → V031 → V032 (sin ORDS) → V033 en el esquema aislado `ECOACCESS_P3_PRUEBAS` (Oracle Free local) | VERIFICADO | código 0 |
| Segunda aplicación de V031, V032 y V033 (repetibles) | VERIFICADO | «ya existía; no se modificó», 0 objetos inválidos |
| `--probar-registro-expo9` contra el esquema aislado con V033 | VERIFICADO | 14/14 (invitado web: 1 operación, 2 detalles, VOZ, 14 s, sin duplicar) |
| Bloque ORDS de V032 en el esquema aislado | PENDIENTE DE ACCESO | Oracle Free local no tiene ORDS; el bloque solo se probó en producción, donde ya estaba aplicado |
| **V033 en producción** | PENDIENTE (requiere tu autorización explícita al aplicarla) | No se aplicó. Procedimiento con respaldo, verificación y reversión preparado en `docs/database/V033_PROCEDIMIENTO_PRODUCCION.md`. La app detecta su ausencia y sigue escribiendo solo la cabecera (V030) |
| **V034 y V035 (bancos de preguntas y ronda de 10 del juego)** | VERIFICADO EN ESQUEMA AISLADO (`ECOACCESS_P3_PRUEBAS`) | `V034__juego_bancos_ronda10.sql` y `V035__juego_paquete_bancos.sql` (con `R034`/`R035`) probadas en aislamiento junto con la ronda de 7 y la de 10 (`JuegoConcurrenciaAislada`); pendientes en producción para no alterar la ronda oficial activa |

## 3. Oracle y ORDS en producción (solo lectura, salvo rondas de prueba del juego)

| Punto | Estado | Evidencia |
|---|---|---|
| ORDS habilitado en el esquema (`pattern ecoaccess`) | VERIFICADO | `USER_ORDS_SCHEMAS`: ENABLED |
| Única API publicada: módulo `eco.juego.v1` (9 plantillas) | VERIFICADO | `USER_ORDS_MODULES` |
| Ninguna tabla expuesta por AutoREST | VERIFICADO | `USER_ORDS_ENABLED_OBJECTS` = 0 |
| CORS restringido a `https://jorgegutho21.github.io` (y `localhost:5173` para desarrollo) | VERIFICADO | cabecera `Access-Control-Allow-Origin` |
| Objetos del juego válidos (8 tablas, vista, paquete) | VERIFICADO | `USER_OBJECTS` VALID |
| Ronda oficial abierta, 0 participantes; las rondas de prueba no aparecen en la clasificación pública | VERIFICADO | `/estado`, `/clasificacion` y prueba `api.test.mjs` |

**Alcance de ORDS (para tu decisión):** los handlers son bloques PL/SQL que corren como `ECOACCESS_P3_APP`, es decir,
con acceso técnico a todo el esquema, aunque solo llaman a `PKG_ECO_JUEGO`. Para aislarlo por completo, la opción
recomendada es un esquema dedicado `ECOACCESS_JUEGO`, dueño de las tablas `ECO_JUEGO_*` y del paquete, con ORDS
habilitado solo para él y sin grants sobre el esquema de la aplicación. Crear ese usuario exige la cuenta ADMIN, que yo
no uso: queda como mejora propuesta y no se implementó.

## 4. EcoAccess Challenge

| Punto | Estado | Evidencia |
|---|---|---|
| Pruebas funcionales reales por HTTP (`node --test web/juego/pruebas/api.test.mjs`) | VERIFICADO | 8/8 (admin protegido, validaciones, idempotencia, cronómetro del servidor, puntaje 700, desempate, cierre, revisión, ronda de prueba invisible) |
| Cliente con reintentos exponenciales, jitter y `Retry-After` ante HTTP 429 de ORDS (`node --test web/juego/pruebas/reintentos.test.mjs`) | VERIFICADO | 4/4 (`web/juego/js/reintentos.js`, integrado en `api.js` y `juego.js`) |
| Seguridad y reglas contra el paquete en el esquema aislado (`tests/integracion/JuegoConcurrenciaAislada`) | VERIFICADO | 20/20 en ronda de 7 y validado también con el banco de 10 preguntas (`V034`/`V035`): inyección y HTML rechazados, sesión inválida o ajena → 401, respuesta inmutable, sin marca de correcta en la pregunta, solo SHA-256 de la sesión, concurso finalizado cerrado |
| La respuesta correcta nunca llega al navegador antes de responder | VERIFICADO | JSON de la pregunta sin campo de corrección; revisión → 403 mientras está activo |
| Tiempo y puntaje calculados en el servidor | VERIFICADO | `SYSTIMESTAMP` de inicio y fin; coherencia aciertos×100 en 185 partidas |
| Ranking: aciertos ↓, tiempo ↑, fin ↑, id ↑; se actualiza sin recargar; vista de proyector | VERIFICADO | top 5 igual al orden recalculado en las 4 cargas; proyector visto a 1920×1080 |
| Sin ecopuntos y sin relación con canecas | VERIFICADO | tablas y rutas propias; el paquete no toca otras tablas |
| Imágenes reales del proyecto (banano, sérum, aluminio, cuaderno, servilleta, limón+aluminio, ECO); kraft como ilustración SVG | VERIFICADO | `web/juego/img/preguntas` |
| Número de preguntas | **DIFERENCIA**: el juego publicado y su base tienen **7 preguntas** (5 sencillas y 2 avanzadas: Oracle y C#), no 10. No lo cambié porque la ronda oficial ya está abierta en producción con esas preguntas (ver §13) |
| Retroalimentación por pregunta | **DIFERENCIA**: durante el concurso no se dice si cada respuesta fue correcta (eso revelaría la clave a otros participantes); la revisión educativa se habilita al finalizar |
| Rutas | Equivalentes con otros nombres: `estado`, `registro`, `intento/inicio`, `intento`, `intento/respuesta`, `clasificacion`, `intento/revision`, `admin/:accion`, `admin/exportacion` |
| Publicación en `/juego/` | VERIFICADO | 200; la página publicada lee la ronda oficial de Oracle desde `github.io` |

### Responsive (navegador integrado, servidor local en el puerto 5173 contra el ORDS real)

| Vista | 320×568 | 360×800 | 390×844 | 430×932 | 768×1024 | 1024×768 | 1366×768 | 1920×1080 | 568×320 | 844×390 |
|---|---|---|---|---|---|---|---|---|---|---|
| Inicio | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | – | – |
| Partida completa (registro → 7 preguntas → resultado) | ✔ | | | | | | | | | |
| Resultado tras recargar (reanudación) | | | | | | | | | | ✔ |
| Clasificación / proyector | | | | | | | | ✔ (proyector) | ✔ | |

✔ = sin desbordamiento horizontal (`scrollWidth = innerWidth`); botones de respuesta de 288×63 px a 320 px.
Estado: VERIFICADO en las combinaciones marcadas; la partida completa solo se jugó a 320×568.
Pendiente: Playwright en celulares reales (no hay dispositivo físico en esta sesión).

## 5. Concurrencia del juego

**A. Base de datos, esquema aislado (Oracle Free local, llamadas directas a `PKG_ECO_JUEGO`, todos arrancan a la vez,
25 % de respuestas reenviadas como reintento de red):** VERIFICADO

| Jugadores | Errores | Partidas | Respuestas / duplicadas | Media | Mediana | P95 | P99 | Top 5 correcto |
|---:|---:|---:|---|---:|---:|---:|---:|---|
| 10 | 0 | 10/10 | 70/70 · 0 | 4,3 ms | 3,2 ms | 6,1 ms | 35,4 ms | sí |
| 25 | 0 | 25/25 | 175/175 · 0 | 3,1 ms | 2,8 ms | 5,6 ms | 12,7 ms | sí |
| 50 | 0 | 50/50 | 350/350 · 0 | 3,3 ms | 2,8 ms | 7,4 ms | 15,1 ms | sí |
| 100 | 0 | 100/100 | 700/700 · 0 | 4,3 ms | 3,0 ms | 10,5 ms | 31,6 ms | sí |

**B. HTTP real contra ORDS de producción** (rondas de PRUEBA creadas y eliminadas por el arnés `carga.mjs`, impacto
controlado; el cliente reintenta 429/5xx como el navegador):

| Escenario | Fallos | Finalizadas | Respuestas 429 | Mediana | P95 | P99 | Máx. | Estado |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| 10 simultáneos | 0 | 10/10 | 0 | 37 ms | 109 ms | 121 ms | 121 ms | VERIFICADO |
| 25 simultáneos | 0 | 25/25 | 60 | 37 ms | 1461 ms | 5920 ms | 7173 ms | VERIFICADO (con reintentos) |
| 50 simultáneos | 0 | 50/50 | 332 | 38 ms | 11,0 s | 13,8 s | 15,8 s | VERIFICADO (con espera alta) |
| **100 simultáneos en el mismo instante** | **1** | **99/100** | 1218 | 41 ms | 20,1 s | 32,9 s | 39,4 s | **ERROR**: capacidad no demostrada |
| 100 llegando escalonados (1 cada 0,4 s, 3–8 s por pregunta) | 0 | 100/100 | 2 | 34 ms | 49 ms | 98 ms | 431 ms | VERIFICADO |

Conclusión honesta: la lógica y la base soportan 100 jugadores concurrentes sin duplicados ni errores de consistencia.
El límite está en ORDS de la base Autonomous: si 100 personas registran e inician **en el mismo segundo**, ORDS responde
`429 Too Many Requests` (página HTML del propio ORDS, no del paquete) y uno de cada cien agotó los 8 reintentos del
arnés. El navegador reintenta durante un minuto, así que en la práctica probablemente terminaría, pero **no se declara
capacidad para 100 simultáneos**. Con llegadas escalonadas, que es lo normal con un QR en un stand, 100/100.

## 6. Visión (11 materiales prioritarios + 15 complementarios + 125 imágenes activas)

Dataset activo `datasets/ecoaccess11`: **125 imágenes activas** (límite 200), 0 duplicados exactos y 0 dañadas (SHA-256 y
verificación PIL). Incluye las 44 imágenes previas (25 fotos reales de ajuste, 9 negativas reales y 10 escenas compuestas)
más **81 fotografías externas con licencia libre verificada** (CC0, CC BY, CC BY-SA de Wikimedia Commons, registradas en
`datasets/ecoaccess11/LICENCIAS.md` y `manifiesto.json`):
- **15 fotografías de cáscara de banano (M05)** en distintos estados de maduración y fondos.
- **53 fotografías de los 15 materiales complementarios** fuera del catálogo operativo (cáscara de naranja, papaya, aguacate, zanahoria, cáscara de huevo, botella plástica, lata de aluminio, caja de cartón, frasco de vidrio, envase de champú, papel higiénico usado, envoltura metalizada, toallita húmeda, esponja deteriorada y tapabocas).
- **13 fotografías negativas externas** de bolsa plástica blanca arrugada (10) y papel blanco liso (3).
- Además se reforzó `CatalogoExpo9.Interpretar(nombre, etiqueta)` para que cualquier nombre libre identificado como fuera de alcance (`plastic bag`, `plastic bottle`, `shampoo`, `sponge`, `face mask`, `wet wipe`, `eggshell`, etc.) rechace la apertura automática aunque el modelo fuerce una etiqueta cerrada como `kraft_paper`, `glue_bottle` o `used_napkin` (verificado en `PruebasModoExpo9Tests`).
Las 25 de ajuste **no** son una evaluación independiente. No se borró nada ni se tocaron registros de Oracle.
Origen verificado: `OneDrive/Documentos/02_PROYECTOS_PERSONALES/01_EcoAccess/02_Imagenes_Reales_EcoAcess/IMAGENES QUE SE VAN A UTILIZAR EN EXPO` (27 archivos).
Se usan 25 (comparadas por SHA-256 con el manifiesto). Las dos excluidas son justo las capturas de chat del celular
(`9.33.09 AM (1)` y `9.33.11 AM (3)`), que no están ni en el dataset ni en el repositorio. Ningún original se movió.
Las dos fotos de «lulo» se revisaron a ojo y son lulo (pulpa verde con semillas), no mandarina.
Catálogo ciudadano: el motor web conserva sus entradas (prueba `motor.test.mjs` del workflow); no se cambió en esta sesión.

Evaluación real de hoy (`EcoAccess.P3.Bot.exe --evaluar-expo9`, lfm3-vl, 512 px, sin hardware):

| Conjunto | Resultado | Estado |
|---|---|---|
| 25 fotos reales (de ajuste, no independientes) | 25/25 canecas correctas | VERIFICADO (sin valor de generalización) |
| 9 negativas reales | 8/9 sin apertura; **1 falsa apertura** (`negativo_07`, bolsa plástica → servilleta → negra) | ERROR |
| 10 escenas compuestas (de ajuste) | 6/10; falsas aperturas en `escena_06` (+negra) y `escena_08` (verde en vez de blanca+negra) | ERROR |
| Latencia de inferencia (modelo residente, precalentado) | media 2,9 s · **mediana 2,37 s** · P95 5,5 s · máx. 5,8 s | ERROR frente a la meta de 1–2 s |

Ya estaban aplicados el modelo residente (`keep_alive`), el precalentamiento (1,9 s), 512 px, JSON con esquema,
`num_ctx` fijo para no recargar el modelo y `num_predict` acotado. Los casos lentos (5–7 s) son los que hacen una
segunda pregunta de verificación (papel blanco o cuadriculado).

**Corrección probada y descartada:** se añadió una verificación «¿es papel suave y no plástico?» antes de aceptar una
servilleta. Resultado real: `negativo_07` siguió abriendo la negra (el modelo responde «sí») y la latencia de las
servilletas subió de unos 5,8 s a 6,9 s. Sin evidencia de mejora, **se revirtió** y no está en la rama.

**Fotos reales que faltan** (todas para evaluación independiente, de objetos y sesiones distintos a los de ajuste):
1. **Cáscara de banano (M05): 0 fotos.** Hacen falta al menos 10 (entera, abierta, oscura, sobre fondos distintos).
2. Para cada uno de los otros 10 materiales, al menos 5 fotos nuevas de otro objeto u otro día: papa, limón, manzana
   verde, lulo, aluminio limpio, frasco de sérum rosado, papel de cuaderno, bolsa kraft, envase de pegamento y servilleta usada.
3. Negativas de **bolsa plástica blanca arrugada** (al menos 5) y de papel blanco liso, que son los confusores reales de
   la servilleta.
4. Escenas reales (no compuestas) con kraft + servilleta y con sérum + cuaderno.

## 7. Telegram

| Punto | Estado | Evidencia |
|---|---|---|
| Corrección de «No pude descargar la foto»: 16 conexiones por servidor, cliente separado para el sondeo y un reintento acotado de `getFile` | VERIFICADO (código y pruebas unitarias) | `ClienteTelegramHttp.cs:40-110`, `Program.cs:33`; suite C# |
| Respuesta breve, orden al ESP32 y confirmación solo si el controlador confirma | VERIFICADO MEDIANTE SIMULACIÓN | pruebas unitarias de la suite |
| Foto y documento reales enviados al bot | PENDIENTE DE ACCESO | hace falta un celular con Telegram |
| Bot único en polling | VERIFICADO | 1 proceso; el lanzador registró «Ya hay un bot en polling: no se inicia otro consumidor» |
| Cuentas autorizadas para abrir canecas desde Telegram | **0** según el registro del lanzador: nadie podrá abrir desde Telegram hasta autorizar una cuenta | PENDIENTE DE ACCESO |

## 8. Aperturas físicas y política 7/14/21 s

| Punto | Estado | Evidencia |
|---|---|---|
| 7/14/21 s idénticos en C# (`CatalogoExpo9`), panel (`DuracionPolitica`), pasarela (`Nucleo.DuracionPolitica`) y web (`motor.js`) | VERIFICADO | código; se corrigieron comentarios y guías que aún decían 5/7/10 s |
| Calibración (verde 0/50°, blanca 0/70°, negra 0/40°), pines, COM3 y GuardiaHardware | sin cambios en esta sesión | no se tocó el firmware ni la calibración |
| 28 pruebas físicas (7 combinaciones × 4 canales) | PENDIENTE DE PRUEBA FÍSICA | requieren tu presencia |
| 7 combinaciones simuladas (`--probar-registro-expo9`, aislado) | VERIFICADO MEDIANTE SIMULACIÓN | 14/14 |

## 9. Web ciudadana, plataforma académica interactiva, panel ESP32 y arranque automático

| Punto | Estado |
|---|---|
| Web ciudadana publicada (200) y motor determinista (`node --test web/ciudadano/pruebas/motor.test.mjs`, 15/15 pruebas, 1.408 casos, 0 fallos) | VERIFICADO |
| Plataforma académica interactiva integrada en `web/ciudadano` (`js/rutas.js`, `css/plataforma.css` y 14 módulos en `js/secciones/*.js`: Conoce EcoAccess, Cómo se construyó, Tecnología, Programación III + 5 módulos SOLID, Arquitectura con recorrido paso a paso, Grafos interactivo con BFS/DFS y lista/matriz de adyacencia, Hardware con simulador PWM, Base de datos con DER interactivo y tutorial Oracle Cloud/Wallet en 12 pasos, IA, Integraciones, Visión a futuro y Challenge) | VERIFICADO EN CÓDIGO |
| Pasarela HTTPS y túnel en marcha (1 pasarela, 1 cloudflared) | VERIFICADO (procesos); flujo completo con QR: PENDIENTE DE ACCESO |
| Panel 8090 renovado como Centro de Control (`src/EcoAccess.P3.PanelCanecas/wwwroot/index.html`, pruebas `PaginaPanelCanecasTests.cs` y capturas en `reportes/validacion_final/capturas_panel/`), sin abrir tapas al iniciar | VERIFICADO (200; el lanzador no movió motores) |
| Arranque automático al iniciar sesión en Windows (`EcoAccess P3 - arranque de la estación`, `scripts/arranque/*`, `docs/operacion/ARRANQUE_AUTOMATICO.md`) | VERIFICADO (`Ready`, resultado `0x0`, 0 procesos duplicados, sesión supervisada inactiva) |
| Procedimiento de autorización de cuentas de Telegram (`scripts/arranque/Autorizar-CuentaTelegram.ps1`, `docs/operacion/AUTORIZACION_TELEGRAM.md`) y pruebas HTTP de descarga (`ClienteTelegramHttpDescargaTests.cs`) | VERIFICADO |
| `/panel-demo/` y nueva plataforma académica en Pages | PENDIENTE DE PUBLICACIÓN EN `main`: se publican con el workflow unificado cuando el PR #28 llegue a `main` |
| Micrófono es-CO real | PENDIENTE DE ACCESO |

## 10. Compilación y pruebas C#

| Punto | Estado | Resultado |
|---|---|---|
| `dotnet build` Debug y Release de `EcoAccess.P3.sln` | VERIFICADO | correctas (0 errores en Debug y Release) |
| Suite unitaria `EcoAccess.P3.Tests` | VERIFICADO | **2563 superadas**, 0 con error, 1 omitida (Debug y Release compilados sobre la versión integrada) |
| `EcoAccess.P3.Datos.Tests` (sin destino aislado) | VERIFICADO | 62 superadas; 192 omitidas: son de integración y se niegan a correr contra producción |

## 11. Instalación, accesos directos y Pages

| Punto | Estado | Evidencia |
|---|---|---|
| Exactamente 3 accesos en el escritorio real (OneDrive\Escritorio): ECOACCESS P3 y ECOACCESS ESP32 (conhost oculto → lanzadores sin mover motores) y ECOACCESS WEB (explorer → URL oficial) | VERIFICADO | listado de `.lnk` con destino y argumentos; CHALLENGE retirado a la carpeta de respaldo |
| P3 y ESP32 no duplican servicios | VERIFICADO | tras abrirlos: 1 bot, 1 app, 1 panel, 1 pasarela, 1 túnel |
| El instalador reproduce los 3 accesos (ya no crea CHALLENGE y respalda el existente) | VERIFICADO EN CÓDIGO (sintaxis 0 errores); ejecución completa PENDIENTE para no crear una segunda instalación |
| Guía para Heyder y Saray | VERIFICADO | `docs/instalacion/GUIA_INSTALACION_COMPANEROS_EXPOSOFTWARE.md` §6 |
| Paquete identificado por commit | PENDIENTE: `scripts/instalacion/Publicar-Paquete.ps1` (ya incluye el icono nuevo) se ejecuta tras fusionar |
| Un solo workflow de Pages (`/`, `/juego/`, `/panel-demo/`) sin push forzado | VERIFICADO EN CÓDIGO; corre al llegar a `main` |

## 12. Git

- Se fusionó `feat/ecoaccess-challenge` en `integracion/ecoaccess-final` conservando su autoría, y el PR #28 se
  actualizó con un avance rápido, sin forzar ni crear PR nuevos.
- Escaneo de secretos del diff: sin contraseñas, tokens, wallets ni configuraciones de Claude, Codex o Antigravity. La
  clave de respuestas y la del panel siguen fuera del repositorio.
- **Aviso:** 22 commits anteriores del PR (de otras sesiones) llevan la línea `Co-Authored-By: Claude`. Quitarla exige
  reescribir el historial con push forzado, que está prohibido; se deja como está. Los commits de esta sesión no la llevan.
- No se fusionó con `main`: faltan pruebas físicas y V033 en producción.

## 13. Lo que necesito de ti (una sola petición)

1. **Pruebas físicas** con la estación conectada: las 28 aperturas (7 combinaciones × PC, Telegram, web y panel).
2. **Telegram real**: envía al bot una foto y un documento (papa, banano…) y autoriza al menos una cuenta para abrir
   canecas, porque ahora hay 0.
3. **Micrófono**: una prueba de voz es-CO en la web ciudadana.
4. **V033 en producción**: confirma si la aplico (aditiva: 4 columnas anulables, tabla de detalle y vista, sin tocar datos).
5. **Juego**: ¿se quedan las 7 preguntas publicadas o paso a 10 (8 sencillas y 2 difíciles) con una migración V034
   aditiva antes de abrir la ronda definitiva?
6. **Fotos reales que faltan** (§6).

## 14. Actualización de la tarde (25/11/2026, 17:15)

Leyenda igual que arriba. Todo lo de esta sección se ejecutó hoy sobre la rama integrada; nada movió servomotores.

### 14.1 ESP32 y servicios (solo lectura)

| Punto | Estado | Evidencia |
|---|---|---|
| A las 16:18 Windows no veía ningún USB-serie (sin COM) | VERIFICADO | `/api/diagnostico`: «Cable USB · Desconectado» |
| A las 17:10, tras reconectar: CP210x (VID 10C4, PID EA60) en COM3, abierto solo por el panel | VERIFICADO | `/api/diagnostico` |
| Firmware `ecoaccess-calibracion-v2`, responde a `ESTADO` | VERIFICADO | solo consultas de estado; ninguna orden de movimiento |
| Canales lógicos verde GPIO 23, blanca GPIO 19, negra GPIO 18 | VERIFICADO (configuración) | **no** demuestra qué servos están cableados: hoy solo estaría la negra según el usuario, y eso no es medible sin sensor |
| Calibración guardada en el dispositivo: verde 0°/50°, blanca 0°/70°, negra 0°/40° (2 s) | VERIFICADO | leída del ESP32; no se modificó |
| Bot (1 en polling), Oracle, pasarela, túnel, Ollama (lfm3-vl cargado), 0 operaciones pendientes | VERIFICADO | registro del arranque: `ollama=1 panel=1 botPolling=1 pasarela=1 cloudflared=1` |
| Cuentas de Telegram autorizadas para abrir | **0** | PENDIENTE DE ACCESO (se autorizan solo con ID verificado: `scripts/arranque/Autorizar-CuentaTelegram.ps1`) |

### 14.2 Oracle (solo lectura en producción)

**V033 APLICADA EN PRODUCCIÓN (25/11/2026 17:30, con autorización escrita del responsable).** Respaldo previo (CSV de las
2 operaciones + DDL) fuera del repositorio; V033 en 8,3 s sin errores; registrada en `ECO_MIGRACION_APLICADA` con la
huella del archivo; permisos de roles reaplicados (detalle de solo lectura). Verificación: tabla, índice y vista VALID,
2 filas intactas, restricciones 26 → 29, 0 objetos inválidos, `--comprobar-oracle` correcto. La escritura del detalle en
producción ocurrirá con la próxima operación real de la estación (probada 14/14 en el esquema aislado). Evidencia:
`docs/database/evidencias/v033_produccion_20261125.md`. **V034 y V035 siguen sin aplicar.** V034/V035 (ronda de 10) siguen preparadas y probadas en el esquema
aislado, sin tocar la ronda oficial de 7.

### 14.3 Visión: falsas aperturas (evaluación real, lfm3-vl 512 px, 125 imágenes)

Detalle: `docs/informes/expo11/evaluacion_consolidada_20261125.md` (+ `.jsonl` antes/después y la variante descartada).

| Conjunto | Casos | Aperturas incorrectas | Aciertos | Por confirmar | Mediana | P95 |
|---|---:|---|---|---|---|---|
| Ajuste (25 reales + 9 negativas + 10 escenas) | 44 | 2 → **0** | 39 → 33 | 3 → 12 | 2,44 → 3,22 s | 5,71 → 6,32 s |
| Externas (15 banano + 53 complementarios + 13 negativas) | 81 | 9 → **0** | 69 → 77 | 14 → 26 | 2,43 → 2,75 s | 6,27 → 6,47 s |
| **Total** | 125 | **11 → 0** | 108 → 110 | 17 → 38 | 2,43 → 2,78 s | 5,91 → 6,38 s |

Reglas nuevas (`CatalogoExpo9`/`ReconocedorExpo9`): un «no» en una verificación ya no se toma como evidencia; cítricos
(naranja/mandarina) → confirmar; la servilleta nunca abre sola y nombres con plastic/bag/wrapper/wipe invalidan etiquetas de
papel; pregunta sí/no para la papa; multirresiduo con canecas en conflicto → confirmar. Probado y descartado: un prompt con
ejemplos de servilleta (6 aperturas incorrectas). El catálogo operativo sigue en 11 materiales: ningún complementario se
reconoció de forma consistente.

**Limitaciones honestas:** el 0 se midió sobre las mismas fotos usadas para diseñar las reglas (ya no hay conjunto ciego);
el riesgo pasó a la confirmación humana (16 de 38 «por confirmar» sugieren una caneca equivocada); servilleta y lulo
siempre piden confirmación; **latencia fuera de la meta de 1–2 s** (mediana ≈ 2,8 s, P95 ≈ 6,4 s). Faltan fotos reales
nuevas (banano propio, bolsas, papel liso, naranja junto a lulo, escenas reales).

### 14.4 Web

- Canecas con capas: cuerpo original inmóvil, tapa separada con bisagra visible, interior iluminado, reflejos recortados a
  la silueta, sombra de contacto y resorte suave sincronizado con el plan de la estación; las no elegidas se atenúan.
- Sello de modo: «Vista ilustrativa» / «Simulación» / «Orden enviada» (solo tras confirmar la pasarela); nunca dice que la
  tapa física se abrió. En modo físico ECO dice «Orden de apertura enviada a la caneca X».
- Botón «Pon a prueba tus conocimientos» en el pie → `#/challenge` → `/juego/`. Corregido: el dock móvil tapaba el pie; el
  sello «Provisional» de la clasificación desbordaba 5 px a 320 px.
- QA Playwright: 10 resoluciones × 16 vistas (158) con 0 desbordes, 0 errores de consola y foco visible. Lighthouse móvil
  (local, sin gzip): rendimiento 83, accesibilidad 100, buenas prácticas 100, SEO 100 (LCP 4,4 s).
- Capturas (24): `reportes/validacion_final/capturas_web/` (celular, tablet, PC, canecas abiertas, grafos, DER, Challenge, juego).
- `/panel-demo/` responde 404 en Pages hoy: se publica con el workflow único al fusionar el PR #28 en `main`.

### 14.5 Compilación, pruebas e instalación

| Punto | Resultado |
|---|---|
| `dotnet build` Debug y Release | 0 errores |
| `EcoAccess.P3.Tests` (Release) | **2589 superadas**, 0 con error, 1 omitida |
| Web: motor / interfaz (Playwright) / integración / reintentos del juego | 15/15 · 27/27 · 14/14 · 4/4 |
| Instalación actualizada (`Instalar-EcoAccess.ps1 -Perfil EstacionPrincipal -SinCompilar`) | ejecutable instalado idéntico al Release (SHA-256); verificador de equipo: todo OK, COM3 |
| Accesos directos | **exactamente 3**: ECOACCESS P3, ECOACCESS ESP32, ECOACCESS WEB; CHALLENGE y uno antiguo «WEB CANECAS 2 CLICKS» respaldados, no borrados |
| Servicios reiniciados tras el Release | 1 bot, 1 panel, 1 pasarela, 1 túnel; ninguna tapa se mueve hasta ACTIVAR CONTROL |

### 14.6 Pendientes (no se pueden cerrar desde software)

1. 28 pruebas físicas (7 combinaciones × PC, Telegram, web, panel) con 7/14/21 s, empezando por la negra conectada.
2. Foto y documento reales al bot y autorización de al menos una cuenta por ID verificado.
3. Micrófono es-CO real en la web.
4. Decidir si la ronda de 10 sustituye a la de 7 (V034/V035, sin aplicar en producción). V033 ya está aplicada.
5. Fotos reales nuevas para una evaluación ciega; la meta de latencia 1–2 s no se cumple con lfm3-vl en este equipo.
6. Equipos de Heyder y Saray: no probados directamente.


## 15. Entrega de la noche (25/11/2026, 18:40): cuatro productos, publicación real e instalación actualizada

Esta sección prevalece sobre las anteriores cuando difieran. Todo se midió sobre el commit `bf82ad7`; el sitio publicado
lleva `a530f7b`, cuyo frontend es idéntico (`bf82ad7` solo cambia un script del instalador).

| Requisito | Estado | Evidencia |
|---|---|---|
| Cuatro productos: ECOACCESS P3, ESP32, DOS CLICS MANUAL y WEB (Challenge dentro de WEB) | VERIFICADO REALMENTE | `Crear-AccesosDirectos.ps1` ejecutado 3 veces (dos solo y una desde el instalador): siempre «Exactamente 4 accesos de ECOACCESS: OK». En el equipo de un compañero son 3, sin ESP32 |
| Iconos distintos | VERIFICADO REALMENTE | P3 `EcoAccess.ico`, ESP32 `EcoAccess_ESP32.ico`, DOS CLICS `EcoAccess_DOS_CLICS.ico` (corregido: salía con el de P3) y WEB `EcoAccess_WEB.ico` |
| Abrir los cuatro accesos | VERIFICADO REALMENTE | P3 abrió la app instalada; ESP32 → 8090 (200); DOS CLICS y WEB abren las URL publicadas. Antes y después: 1 bot, 1 panel, 1 pasarela y 1 túnel |
| GitHub Pages publicado de verdad | VERIFICADO REALMENTE | workflow `web-ciudadana` lanzado desde `integracion/ecoaccess-final` (run 45720987128) → `gh-pages` `e369b2e` → build «built». `/`, `/dos-clics/`, `/juego/`, `/juego/clasificacion/`, `/panel-demo/` y `/version.json` responden 200; `version.json` = `a530f7b`, visible en el pie |
| DOS CLICS en celular (390×844), WEB en tablet (768×1024) y Challenge en PC (1366×768), sitio publicado | VERIFICADO REALMENTE | sin desbordes y sin errores de consola en el navegador integrado; la página informa «Vista ilustrativa — sin movimiento físico» mientras no hay sesión de estación |
| Responsive de DOS CLICS en los 8 tamaños | VERIFICADO MEDIANTE SIMULACIÓN (navegador automatizado local) | `dos-clics.test.mjs` 26/26 |
| Pruebas web | VERIFICADO | interfaz 27/27, integración 14/14, catálogo 15/15, DOS CLICS 26/26, juego: interfaz 19/19 y reintentos 4/4 |
| Compilación Debug y Release | VERIFICADO | 0 errores (worktree temporal y carpeta oficial) |
| Suite C# | VERIFICADO | 2624 superadas, 0 con error y 1 omitida; Datos 65 superadas (192 de integración omitidas: no corren contra producción) |
| 28 combinaciones simuladas (7 × P3, Telegram, DOS CLICS y ESP32) | VERIFICADO MEDIANTE SIMULACIÓN | `MatrizCombinacionesCanalesTests`, incluida en la suite |
| App instalada = última Release | VERIFICADO REALMENTE | SHA-256 de `Presentacion.exe`, `Logica.dll` y `Datos.dll` instalados = bin Release recién compilado |
| Servicios actualizados (bot, panel, pasarela y túnel) | VERIFICADO REALMENTE | detenidos con `Detener-ServiciosEcoAccess.ps1`, recompilados y arrancados con `Iniciar-ServiciosEcoAccess.ps1`: una instancia de cada uno; el panel sirve la página nueva; diagnóstico 13/13 en verde; sesión supervisada desactivada; ningún motor recibió órdenes |
| V033 en producción | VERIFICADO REALMENTE (solo lectura) | registrada el 25/11 a las 22:30 UTC; detalle, índice y vista VALID; 0 objetos inválidos; 2 operaciones conservadas. No se volvió a aplicar |
| V034/V035 (ronda de 10) | PENDIENTE DE AUTORIZACIÓN | no existen en producción (`ECO_JUEGO_BANCOS` ausente); probadas en el esquema aislado. Se aplican junto con la activación de la ronda de 10 |
| Aviso de huella de V030 | EXPLICADO Y CORREGIDO | el contenido no cambió: la huella registrada `d1505589…` es la del archivo con LF (como está en git) y Windows lo extrae con CRLF (`4faa7513…`). El instalador ahora acepta el mismo contenido con LF o CRLF. No se tocaron la huella ni la migración |
| Hardware | VERIFICADO REALMENTE (solo lectura) | COM3 (CP210x), `ecoaccess-calibracion-v2`, verde GPIO23 0/50°, blanca GPIO19 0/70° y negra GPIO18 0/40°. El firmware no mide posición: no se puede saber qué servo está conectado eléctricamente |
| Aperturas físicas, Telegram con un teléfono real y micrófono real | PENDIENTE DE PRUEBA FÍSICA / DE ACCESO | sin cambios: 0 cuentas autorizadas; `scripts/arranque/Autorizar-CuentaTelegram.ps1` está listo para tu ID |
| Visión | sin cambios en esta entrega | valen las cifras de §6 y §14 (125 imágenes; 0 aperturas incorrectas solo en el conjunto con el que se ajustaron las reglas; latencia por encima de la meta de 1–2 s) |
| Captura del escritorio | PENDIENTE | no se tomó una captura de pantalla del escritorio; la lista de accesos y destinos está arriba |

## 16. QR fijo de la estación en Telegram (25/11/2026, 19:30)

QR impreso, decodificado con ZXing desde la captura: `https://t.me/EcoAccessP3_JG_bot?start=EcoAccessCanecaPiloto1`.
Es idéntico al QR oficial del repositorio (`assets/identidad/QR_Caneca_piloto.png`, SHA-256 protegido por pruebas) y
corresponde a la caneca 1093 de Oracle, en servicio. **No se generó otro QR ni cambió el enlace.**

Causa del error: en Modo Expo, escanear el QR solo daba «presencia» si el flujo antiguo de caneca piloto creaba una
interacción en Oracle, y la foto ya identificada nunca se retomaba. Por eso el bot repetía «escanea el QR o pide a un
supervisor».

Corrección (`ServicioConversacionTelegram.Expo.cs`):
- Escanear el QR fijo abre una **sesión de estación de 15 minutos** para esa persona, sin supervisor. Cada apertura la renueva.
- Si había una foto identificada sin dudas en los 5 minutos anteriores, **se abre sola al escanear**, una sola vez (clave
  idempotente `qr:<token>`). Si la foto era dudosa, se pide confirmar con un toque.
- Escanear sin residuo identificado **no abre nada**: solo responde «Estación vinculada».
- Límite de 4 aperturas por persona y minuto. Se quitaron las solicitudes de autorización al supervisor.
- Texto tras la confirmación del ESP32: «Caneca abierta. Puedes desechar tu residuo.» (o «Canecas abiertas…»), con el
  tiempo 7/14/21 s. El cierre lo hace el controlador. La operación se registra en Oracle (`OPERACION_ESTACION` y su
  detalle V033: residuo, canecas, fecha, canal TELEGRAM y tipo de usuario).
- Los chats autorizados, la web, la app C# y el panel no cambian.

| Punto | Estado |
|---|---|
| 5 pruebas nuevas: foto antes del QR, QR sin residuo, QR primero, sesión vencida, límite | VERIFICADO MEDIANTE SIMULACIÓN (Ollama falso, estación y Oracle dobles) |
| Suite C# 2629 correctas y 0 fallidas; Release 0 errores | VERIFICADO |
| Bot, panel, pasarela y app instalada con la Release nueva (una instancia de cada uno); diagnóstico 13/13 | VERIFICADO REALMENTE |
| Escanear y enviar una foto desde un teléfono real, apertura física y fila en Oracle | PENDIENTE DE PRUEBA FÍSICA: requiere tu teléfono y activar el control en ECOACCESS ESP32 |

## 17. Estación Oficial EcoAccess y causa real de «la caneca no está conectada» (25/11/2026, 19:45)

**Diagnóstico con la prueba real de tu teléfono:** el registro del bot muestra el QR escaneado (sesión de 15 min), el
papel aluminio reconocido (inferencia 2,3 s) y la orden a la blanca con `apertura=False`. El ESP32, el COM3, el firmware
`ecoaccess-calibracion-v2` y la comunicación estaban bien (diagnóstico 13/13). **La causa real** era que el panel
rechaza con HTTP 423 toda apertura sin sesión supervisada; ese rechazo no se anotaba y el bot lo mostraba como «no está
conectada».

| Corrección | Estado |
|---|---|
| Modo **ESTACIÓN EN SERVICIO** en ECOACCESS ESP32: activado por el anfitrión, hasta las 23:59; Telegram (QR), web, DOS CLICS y la app abren sin supervisor. Calibración y pruebas siguen pidiendo ACTIVAR CONTROL; DETENER TODO lo desactiva; nunca se activa al encender Windows | VERIFICADO REALMENTE (lógica): fuera de servicio, una apertura real al panel devuelve 423 con la causa y queda en el historial («rechazo … no está en servicio»), sin mover motores |
| El bot explica la causa (pausa, servicio sin respuesta o ESP32 desconectado) | VERIFICADO MEDIANTE SIMULACIÓN |
| «Caneca piloto» → «Estación Oficial EcoAccess» en la app, Telegram, scripts de datos y Oracle (UPDATE de una fila: caneca 1093, QR intacto) | VERIFICADO REALMENTE (Oracle releído desde una sesión nueva) |
| La estación se reconoce por el identificador del QR permanente; el nombre anterior de Oracle («EcoAccess - Caneca piloto», con guion) no coincidía con el del código (raya) | CORREGIDO |
| Un solo controlador: el panel es el único dueño del COM; los demás canales le envían órdenes (local o en red con clave) | VERIFICADO (diseño existente; una instancia de cada servicio) |
| Control desde los equipos de los compañeros | PREPARADO: `Habilitar-PanelEnRed.ps1` (administrador, solo redes privadas) + `ECOACCESS_PANEL_URL` + `clave.txt` por canal privado. PENDIENTE DE ACCESO: no se ejecutó (requiere administrador) ni se probó desde otro equipo |
| Cambiar de anfitrión | PREPARADO: detección automática del COM ya existente y guía §8. Sin probar en otro equipo |
| Debug y Release 0 errores; C# 2629 correctas y 0 fallidas; servicios e app instalada actualizados | VERIFICADO |
| Apertura y cierre físicos de las tres canecas desde cada canal | PENDIENTE DE PRUEBA FÍSICA: requiere pulsar ESTACIÓN EN SERVICIO con la estación atendida. Hoy solo está conectada eléctricamente la negra |
| GitHub Pages | Sin cambios en esta entrega: la web no usaba el nombre «caneca piloto»; sigue publicada `a530f7b` |

## 18. Tres canecas físicas, sin DEMO ni SIMULACIÓN (25/11/2026, 22:00)

Se incorporaron por avance rápido los 7 commits de la rama `integracion/tres-canecas-fisicas` (Heyder Hedrey Canchila de Angel) a `integracion/ecoaccess-final` (`d5751c9`), sin conflictos ni force push. El trabajo local equivalente sin commit quedó respaldado como parche en `_Respaldos_EcoAccess/cambios_locales_antes_de_integrar_*` y se descartó a favor de la versión con pruebas.

| Elemento | Estado |
|---|---|
| Oracle: solo la «Estación Oficial EcoAccess» con blanca (1093, QR `EcoAccessCanecaPiloto1` intacto), verde (1400) y negra (1401), estado 1; 1 punto; 0 objetos inválidos; 63 interacciones conservadas | VERIFICADO (consulta de solo lectura) |
| Debug y Release 0 errores; C# 2646 correctas, 0 fallidas, 1 omitida | VERIFICADO |
| Servicios reiniciados con la versión nueva: panel=1, bot en polling=1, pasarela=1, túnel=1 | VERIFICADO |
| La estación arranca EN SERVICIO sin activar nada; sin ESP32 queda `operativa=false` y no se anuncia ninguna apertura | VERIFICADO |
| Reconexión automática: al conectar el USB el panel tomó COM3 solo, firmware `ecoaccess-calibracion-v2`, calibraciones V23 0/50°, B19 0/70°, N18 0/40° | VERIFICADO |
| Apertura física individual verde, blanca y negra (7 s), dos canecas (14 s) y tres (21 s) con cierre automático | VERIFICADO: ESP32 confirmó cada orden y volvió a REPOSO sin PWM; el anfitrión observó que las tres tapas abrieron y cerraron |
| Orden duplicada (misma clave) | VERIFICADO: rechazada, no abre dos veces |
| Pausa manual: apertura rechazada con HTTP 423 y mensaje de pausa; reanudar restablece el servicio | VERIFICADO |
| Registro en Oracle de aperturas desde Telegram (incluido el QR) | VERIFICADO antes del reinicio (`OPERACION_ESTACION` ids 5–8); PENDIENTE repetir con un celular tras el reinicio |
| Aplicación de escritorio instalada (`%LOCALAPPDATA%\Programs\EcoAccess P3`) con las mejoras de esta rama | PENDIENTE: requiere cerrar la ventana abierta para reinstalar |
| Control desde otro equipo autorizado | PENDIENTE DE ACCESO: requiere otro PC en la red y `Habilitar-PanelEnRed.ps1` como administrador |

## 19. Integración definitiva: automatización, Telegram y clientes remotos (25/11/2026, 22:45)

### Errores encontrados y causas reales

| Síntoma | Causa real | Corrección |
|---|---|---|
| Datos del anfitrión «192.168.1.34 / COM9» | Eran del PC de Heyder cuando fue anfitrión. El ESP32 está en este equipo: **192.168.1.20, COM3** | Documentado; Logos debe apuntar a 192.168.1.20 |
| El panel no es alcanzable desde la red | Este anfitrión solo escucha `localhost` (sin reserva de URL ni regla de firewall) y el Wi-Fi está en perfil **Público** | PENDIENTE (requiere administrador): perfil Privado + `Habilitar-PanelEnRed.ps1` |
| Un equipo cliente iniciaba su propio panel y su propia pasarela | El lanzador siempre iniciaba el panel local | Con `anfitrion.txt` remoto no inicia panel ni pasarela y comprueba el anfitrión con la clave |
| Riesgo de dos bots con el mismo token (409) | Nada impedía iniciar el bot en dos equipos | `Ubicar-BotTelegram.ps1` (marca `bot-en-otro-equipo`); decisión vigente: el bot corre en el anfitrión |
| La app decía «En servicio» mientras Telegram decía «en pausa» | «Canecas y QR» mostraba solo el estado administrativo de Oracle; Telegram usa el estado operativo del controlador | La app muestra ahora ambos: «habilitadas en Oracle · Estación: en servicio / en pausa / desconectada / sin respuesta» |
| Respuesta larga de Telegram tras abrir | Se anteponía la clasificación del residuo | Solo imagen de Eco del color (o composición de dos) + «🟢 Caneca verde abierta. Tienes 7 segundos…»; el material queda en Oracle |
| Bot o panel caídos tras el inicio de sesión no se recuperaban | La tarea solo corría al iniciar sesión (y la copia del script estaba desactualizada) | Tarea reinstalada: al iniciar sesión + vigilancia cada 5 min que reinicia solo lo caído y no escribe nada si todo está en marcha |
| ECOACCESS ESP32 en un cliente con perfil EstacionPrincipal abría un panel local sin ESP32 | El acceso dependía solo del perfil | Si `anfitrion.txt` apunta a otro equipo, abre el panel del anfitrión |
| Aperturas automáticas de Telegram con `apertura=False` (19:25–21:40) | Panel anterior que exigía activar el servicio y ESP32 desenchufado en parte de ese lapso | Estación en servicio por defecto (rama del equipo); pendiente repetir con celular |

### Verificado

| Elemento | Resultado |
|---|---|
| Compilación Debug y Release | 0 errores |
| Pruebas automatizadas | 2649 correctas, 0 fallidas, 1 omitida (3 nuevas: textos e imágenes de Eco) |
| Arranque por la tarea programada (como al iniciar Windows) | Panel, bot, pasarela y túnel en ~10 s; una instancia de cada uno |
| Vigilancia con todo en marcha | 0 líneas nuevas en el registro, nada reiniciado |
| Vigilancia tras detener el bot | Solo el bot se reinició; panel y pasarela conservaron su PID |
| Pausa manual persistente | La estación quedó en pausa por «DETENER TODO» (22:23) y siguió en pausa tras reiniciar; las órdenes VN y BN se rechazaron sin mover tapas |
| Latencia real Telegram (registros, versión anterior) | Descarga 0,95–1,9 s; inferencia 2,1–6,1 s; orden al controlador a los 3,9–4,1 s de iniciar la descarga (n = 3) |

### Pendiente con evidencia física o de acceso

* Reanudar la estación (la pausó «DETENER TODO» a las 22:23) y observar las parejas verde+negra y blanca+negra.
* Prueba de Telegram con celular tras el reinicio: QR, foto, imagen de Eco, cierre y fila en Oracle.
* Reinstalar la aplicación de escritorio y los cuatro accesos (requiere cerrar la ventana de EcoAccess abierta).
* Red: perfil Privado y `Habilitar-PanelEnRed.ps1` como administrador; después, Logos con `Configurar-Anfitrion.ps1 -Anfitrion 192.168.1.20`.
* Reconocimiento: el conjunto `datasets/exposoftware9` cubre 9 de los 11 residuos; faltan fotos independientes para medir servilleta/cuaderno, vidrio rosado/pegamento, kraft/papa y aluminio/metalizado.
* Reinicio completo de Windows (la tarea se probó lanzándola; el disparador de inicio de sesión es el mismo).

## 20. Entrega final: estación reactivada, canales verificados y evidencia real (25/11/2026, 23:20)

### Causa exacta del «mantenimiento» de Telegram
El panel tenía una pausa manual guardada (`servicio.txt`: «DETENER TODO (emergencia)», 22:23, pulsada en el panel del
anfitrión). La foto de cáscara de banano se reconoció (verde, orden a los 4,7 s) y el controlador la rechazó por esa
pausa; Oracle la guardó como operación 9 (M05, RECHAZADO/ERROR, sin depósito). Con autorización expresa del usuario y
el ESP32 verificado (COM3, firmware `ecoaccess-calibracion-v2`, calibraciones 0/50°, 0/70°, 0/40°, tapas en reposo) se
reanudó el servicio. Un reinicio completo de los servicios (detener → instalar → tarea programada) mantuvo
`enServicio=true`: un reinicio normal ya no deja la estación en mantenimiento. DETENER TODO sigue guardando una pausa
que solo una persona levanta.

### Corrección adicional encontrada en esta entrega
La pasarela web (DOS CLICS y WEB) exigía la sesión supervisada del panel en lugar del estado «en servicio»: la web
quedaba bloqueada mientras Telegram y la app abrían. Ahora usa el mismo estado operativo (`Operativa`), y una pausa se
muestra como «Estación en mantenimiento». Telegram nunca nombra el residuo, ni siquiera cuando la apertura falla.

### Evidencia real
| Prueba | Resultado |
|---|---|
| Física por el controlador central: V, B, N (7 s), VB, VN, BN (14 s), VBN (21 s) | 7/7 aceptadas en 97–110 ms; las tres tapas volvieron a REPOSO sin PWM; el ESP32 no se reinició ni se desconectó en ningún ciclo (incluidas las tres a la vez) |
| Canal app C# (`ClienteEstacionPanel` de la app instalada) | Encontró el controlador solo (127.0.0.1:8090, COM3, en servicio); abrió la negra 7 s; la misma clave repetida → «duplicada» |
| Canal web por Internet (túnel HTTPS → pasarela → panel), con la sesión supervisada cerrada | Blanca y negra abiertas en 574 y 269 ms; sin permiso → 401; clave repetida → no repite; solicitud de hace 5 min → 409 «antigua» |
| Oracle: operaciones web | Ids 10 y 11, canal WEB, PWM_CONFIRMADO, REGISTRADA (sin depósito confirmado) |
| Oracle: Telegram | Id 9 con el material M05 aunque Telegram no lo nombra |
| Migraciones | V030–V033 aplicadas y registradas, 0 objetos inválidos. V034/V035 (banco de 10 preguntas del juego) siguen sin aplicar: su clave de respuestas se carga con un script privado que no está en el repositorio y no afectan a la estación |
| App instalada = compilación | SHA-256 idéntico (`EcoAccess.P3.Presentacion.exe` 4a1f07891a97…, `EcoAccess.P3.Logica.dll` 043043ba8188…); misma `Logica.dll` en bot y app |
| Cuatro accesos | Exactamente 4, iconos originales, destinos existentes; el antiguo «ECOACCESS DOS CLICS MANUAL» quedó respaldado y retirado |
| Arranque y vigilancia | Tarea programada al iniciar sesión + cada 5 min; una instancia de panel, bot, pasarela y túnel |
| Pruebas automatizadas | C# 2649 correctas, 0 fallidas, 1 omitida; web 26 + 14 + 27 + 15; juego 8 + 4 + interfaz; 0 fallidas |
| GitHub Pages | Publicado `0f19d94` (pruebas y publicación en verde) |

### Reconocimiento de los 11 residuos (`reportes/vision35/eval_final_11_2026-11-25.md`)
72 fotos (25 reales de ajuste, 15 externas independientes, 10 compuestas, 22 negativas): **0 falsas aperturas**.
Negativas 22/22 sin apertura. Externas independientes de banano: 13/15 con el material correcto. Papa, limón, manzana
verde, aluminio, vidrio rosado, cuaderno y kraft abren solos en todas sus fotos. **Servilleta usada (0/2) y lulo (0/2)
nunca abren solos**: el modelo confunde la servilleta con papel de cuaderno y la protección pide confirmación con un
botón; no se relajó. Reconocimiento: mediana 2,4 s, P95 5,5 s. Faltan fotos independientes de servilleta, cuaderno,
vidrio rosado y pegamento para mejorar esas parejas sin sobreajustar.

### Latencia real de extremo a extremo (Telegram)
Descarga de la foto 1,0–1,9 s + reconocimiento 2,1–6,1 s → orden al controlador a los 3,9–4,7 s (4 fotos reales);
el controlador responde en ~0,1 s y la imagen de Eco se envía después de la orden (no la retrasa).

### Pendiente (requiere a una persona)
* Foto real desde un celular con la estación en servicio (no hubo ninguna desde la reactivación).
* Red para Logos: perfil Privado del Wi-Fi + `Habilitar-PanelEnRed.ps1` como administrador; luego, en su equipo,
  `Actualizar-EquipoCliente.ps1 -Anfitrion 192.168.1.20`.
* Reinicio completo de Windows (la tarea se probó lanzándola; el disparador de inicio de sesión es el mismo).
* Fusión en `main`: se hará cuando lo anterior quede verificado.

## 21. Caneca negra, lulo, red del anfitrión y ramas del equipo (25/11/2026, 23:50)

### Fallo de la caneca negra desde Telegram (Gota Mágica): diagnóstico por capas
| Capa | Evidencia | Resultado |
|---|---|---|
| Recepción y descarga | Descargas de 753 ms y 807 ms | Correcta |
| Reconocimiento | `estado=Reconocido canecas=N` en las dos fotos (la segunda desde caché) | Correcto: M10 envase de pegamento |
| Clasificación y autorización | Sesión del QR vigente; apertura automática sin confirmación | Correcta |
| Comunicación con el controlador | Orden a los 3,6 s y 0,8 s; `apertura=True` | Correcta |
| Comando al ESP32 | Panel: «telegram: abierta 7 s · 23:22:08»; caneca N, GPIO18, 0→40°, ciclo terminado en REPOSO sin PWM | Correcto |
| Oracle | Operaciones 16 y 18: M10, NEGRA, 7000 ms, PWM_CONFIRMADO, CICLO_COMPLETADO, sin depósito declarado | Correcto |
| Movimiento físico | La persona observó que la tapa negra no se movió | **Fallo físico** |

Conclusión: el fallo **no** es de visión, clasificación, autorización ni comunicación; el ESP32 aplicó la señal en GPIO18
y la tapa no se movió. Causa probable: alimentación del servo negro, conexión de la señal en GPIO18 o el servo/mecanismo.
Las tres tapas se habían movido a las 22:00 (observación del usuario). No se cambió la calibración (0/40°) porque no hay
evidencia de que sea la causa. PENDIENTE DE PRUEBA FÍSICA: revisar el cableado y repetir con la persona observando.

### Lulo
El lulo real llega como «orange» con etiqueta «lulo» y la regla de cítricos pedía confirmar. Como lulo y naranja van a
la misma caneca verde, ese caso exacto abre solo; mandarinas, cáscaras y «orange» con otra etiqueta siguen por confirmar.
Repetición con las 25 fotos reales y 10 compuestas: real 22/25 (88 %, antes 80 %), lulo 2/2 automático (antes 0/2),
**0 falsas aperturas**. La servilleta sigue pidiendo confirmación (el modelo la ve como papel de cuaderno); no se relajó.

### Red del anfitrión (con permiso de administrador concedido por el usuario)
Wi-Fi «Wi-Fi 2» en perfil Privado; reserva `http://+:8090/` para el usuario; regla de firewall solo para redes privadas.
El panel escucha en la red local con clave. Prueba desde la IP 192.168.1.20 (como lo hará el equipo de Logos): sin clave
401, clave falsa 401, pausar desde la red 403 (solo el anfitrión), estado con clave correcto; el cliente de la app con
`http://192.168.1.20:8090` abrió la negra y rechazó la clave repetida. PENDIENTE: ejecutar
`Actualizar-EquipoCliente.ps1 -Anfitrion 192.168.1.20` en el equipo de Logos (atraviesa el firewall desde otra máquina).

### Ramas del equipo
Se integraron los cambios de DOS CLICS de Heyder (un toque abre la caneca y nueva interfaz). La suite local
`dos-clics.test.mjs` da 23 correctas y 3 fallidas **ya en su propio commit** (las pruebas aún esperan el anuncio «Caneca
verde seleccionada» y la etiqueta de versión de la interfaz anterior); no forma parte del CI y no se modificó su trabajo.
Ramas `integracion/ecoaccess-final` y `integracion/tres-canecas-fisicas` iguales; CI del PR #28 en verde; Pages `e7285d6`.

## 22. Validación tras la reparación del servo negro (26/11/2026, 00:03–00:07)

El usuario reparó el hardware de la caneca negra y había pausado la estación con DETENER TODO (23:48) mientras
trabajaba; con su indicación expresa se reanudó. ESP32 en COM3, firmware `ecoaccess-calibracion-v2`, calibraciones sin cambios.

| Canal | Canecas | Resultado del controlador | Oracle |
|---|---|---|---|
| Controlador central (el mismo de todos los canales) | V, B, N, VB, VN, BN, VBN | 7/7 aceptadas; 7/7/7/14/14/14/21 s; las tres en REPOSO sin PWM tras cada ciclo; ESP32 siempre conectado | (pruebas técnicas, no se registran) |
| ECOACCESS P3 (cliente instalado de la app) | verde, blanca, negra | 3/3 aceptadas, 7 s | La app registra por su servicio |
| ECOACCESS WEB / DOS CLICS por Internet (túnel HTTPS, estación solo «en servicio») | blanca, verde, negra | 3/3 en 199–216 ms | Ids 21–23: WEB, PWM_CONFIRMADO, REGISTRADA |

Operación 20 (23:26, Telegram, SIN_RESPUESTA): foto enviada mientras se reinstalaban los servicios; se rechazó sin abrir.
El movimiento físico de cada tapa lo confirma la persona que observa la estación (el firmware no tiene sensor de posición).

**Confirmación física (26/11/2026, 00:10):** el usuario observó que las tres tapas —verde, blanca y negra— abrieron y
cerraron solas en las pruebas anteriores. VERIFICADO: las tres canecas físicas funcionan desde el controlador central,
ECOACCESS P3 y ECOACCESS WEB / DOS CLICS. Telegram: verde y blanca verificadas físicamente desde el celular (23:21); la
negra recorrió toda la cadena (foto → M10 → orden → ESP32 → Oracle) y su fallo era del servo, ya reparado.
Pendientes: foto de la negra desde el celular tras la reparación, equipo de Logos y reinicio completo de Windows.

## 23. ECOACCESS WEB y DOS CLICS con las tres canecas físicas (26/11/2026, 00:40–01:11)

### Causa real
La página pública solo habla con la estación si llega con el permiso del QR (`#e=<pasarela>&t=<permiso>`). Los accesos
ECOACCESS WEB y DOS CLICS del escritorio abrían GitHub Pages sin permiso: la página quedaba en vista ilustrativa y nada
se movía. Además la pasarela admitía un solo permiso: emitir otro invalidaba el QR de los celulares.

### Correcciones (rama `integracion/web-canecas-fisicas`)
* Pasarela: permiso propio del navegador del anfitrión (12 h), emitido solo por el puerto de operador (loopback y
  encabezado), reutilizado mientras siga vigente; no invalida el QR de los celulares. Mismo controlador central.
* `scripts/desarrollo/Abrir-WebEstacion.ps1` y `Crear-AccesosDirectos.ps1`: en el anfitrión, ECOACCESS WEB y DOS CLICS
  abren la página ya vinculada (iconos originales); en clientes remotos siguen abriendo la página pública.
* DOS CLICS: escribir o decir «caneca negra», «abre la verde» o «canecas blanca y negra» abre esas canecas («papel
  negro» sigue siendo un residuo); Eco confirma «Caneca verde abierta. Tienes 7 segundos» solo tras la confirmación del
  controlador; tocar la caneca se registra como MANUAL en Oracle (texto y voz como TEXTO y VOZ).
* Service worker: HTML, JS y CSS se revalidan con el servidor (antes el navegador entregaba hasta 10 min la versión
  anterior); script versionado.
* Las 3 pruebas de DOS CLICS que fallaban se adaptaron a la interfaz de un toque de Heyder sin quitar comprobaciones.

### Evidencia en la página publicada (GitHub Pages → túnel HTTPS → pasarela → panel → ESP32)
| Acción en la página | Controlador | Oracle |
|---|---|---|
| Clic en la imagen verde (PC) | `web-ciudadana: abierta 7 s · 00:47:36` | 34 VERDE |
| Texto «papel aluminio» | blanca 00:47:55 | 35 B30 METAL, BLANCA |
| Clic en la imagen negra | negra 00:54:32 | 41 NEGRA |
| Texto «caneca verde» | verde 00:59:55 | 42 VERDE |
| Texto «servilleta usada» | negra 01:00:16 | 43 N02 SERVILLETA_USADA, NEGRA |
| Celular 375×812: texto «media manzana» | verde 01:00:42 | 44 V01 ORGANICO, VERDE |
| Celular 375×812: toque en la imagen blanca | blanca 01:01:02 | (sincronizada después) |
| Clic en la imagen negra tras redesplegar | negra 01:10:34 | 46 WEB, **MANUAL**, NEGRA, 7000 ms, PWM_CONFIRMADO |

Pantalla tras la confirmación: «Caneca verde abierta. Tienes 7 segundos» con cuenta regresiva, solo la caneca activa
resaltada y «Estación conectada» como estado real. Pruebas: DOS CLICS 27/27, integración 14/14, interfaz 27/27,
motor 15/15, C# 2648/0. El dictado por voz usa la misma función que el texto (cubierta por pruebas); no se pudo
dictar con micrófono real desde la automatización. El movimiento de cada tapa lo confirma la persona en la estación.
