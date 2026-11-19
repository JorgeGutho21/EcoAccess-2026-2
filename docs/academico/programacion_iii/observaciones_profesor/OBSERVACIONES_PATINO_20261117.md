# Observaciones del profesor Patiño (17/11/2026)

**Fuente:** audio `AUDIO PATIÑO.ogg` (42,9 s), recibido por WhatsApp el 18/11/2026. Lo transcribió en local Whisper `small` (faster-whisper 1.2.1), sin subir el audio a ningún servicio. La grabación es de un salón con ruido; las palabras dudosas van marcadas con (?).

## Transcripción

| Tiempo | Texto |
|---|---|
| 0:00 | No separa, le oculta las otras cosas. Es otro proyecto que va a correr allá, que se va a conectar a la misma base de datos. |
| 0:08 | Porque sí, esa misma aplicación la puede correr. |
| 0:11 | ¿Qué es, un duplicado? (?) |
| 0:13 | El mismo proyecto, sino que le vas a ocultar el resto de opciones; solo va a tener una. |
| 0:21 | Solo va a tener un solo botón grande que diga «voy a botar…» (?), y escoge. |
| 0:26 | O sea, se hace con botón grande, porque como está aquí tiene otro contexto. |
| 0:32 | Como era un MVP, eso debería ser móvil, pero esto era lo más breve de solucionar. |
| 0:38 | Por eso le dije que si se consigue una (pantalla) táctil. |
| 0:40 | Necesito el video promocional. |

## Matriz de observaciones

| # | Observación | Tipo | Qué se hizo | Archivos | Evidencia | Estado |
|---|---|---|---|---|---|---|
| 1 | La pantalla táctil es **la misma aplicación**, no un duplicado | Instrucción explícita | Modo estación dentro de `EcoAccess.P3.Presentacion`: se abre con `--estacion` o cuando un supervisor configura el equipo como estación | `FormEstacionTactil.cs`, `Program.cs` | Un solo `.exe` y una sola solución | Hecho |
| 2 | Se conecta **a la misma base de datos** | Instrucción explícita | Usa `ServicioCatalogoAmbiental` con la misma conexión a Oracle Cloud | `Program.cs` | Recorrido: «Estación lista (Oracle)» | Hecho |
| 3 | **Se ocultan las demás opciones; solo queda una** | Instrucción explícita | Sin menú, sin inicio de sesión y sin opciones administrativas | `FormEstacionTactil.cs` | Capturas `docs/interfaces/capturas/estacion_*` | Hecho |
| 4 | **Un solo botón grande** («voy a botar…») y luego **escoge** | Instrucción explícita; el texto exacto del botón es dudoso | «Voy a botar algo» → 6 grupos grandes → residuo → caneca con su color | `GuiaEstacionTactil.cs`, `FormEstacionTactil.cs` | 6 pruebas unitarias y 16 pasos del recorrido | Hecho |
| 5 | Lo ideal sería móvil, pero la pantalla táctil es más rápida | Contexto | No se hace una aplicación móvil; queda como trabajo futuro | — | — | Documentado |
| 6 | **Video promocional** | Instrucción explícita | Fuera del código: lo graba el equipo | — | — | **Pendiente (equipo)** |

**Lo que el audio no dice:** nada sobre ocultar o quitar el registro. Por eso el registro **no se eliminó**: salió de la pantalla principal y quedó como el enlace discreto «¿Aún no tienes una cuenta? Regístrate», que abre su propia ventana (`FormAccesoModerno.cs`). Si otra grabación pide quitarlo, basta con ocultar ese enlace; el servicio de registro sigue intacto.

## Decisiones de la estación táctil

- **Solo orienta.** No abre canecas ni registra depósitos. La apertura sigue en el flujo de Telegram, que exige QR y presencia, así que la pantalla nunca da por hecho un depósito que no ocurrió.
- **Lo especial no va en ninguna caneca:** pilas, medicamentos y aparatos se muestran en rojo, «No va en estas canecas», con la ruta de disposición guardada en Oracle.
- **La caneca sale de la categoría guardada en Oracle**, con el mismo `ResolutorDestino` que usa Telegram. Los grupos de la pantalla solo ordenan lo que la persona tiene en la mano. Por ejemplo, la servilleta usada aparece en «Papel y cartón» y aun así va a la caneca negra.
- **Vuelve sola al inicio** tras 45 s sin uso. Si no hay conexión, lo dice y reintenta cada 30 s.
