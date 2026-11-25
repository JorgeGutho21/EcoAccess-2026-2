# EcoAccess P3 · Informe final de integración (25/11/2026)

Único informe de esta entrega. Todo lo que no se probó en esta sesión sobre la versión integrada queda como pendiente.
Los resultados históricos (2518 C#, V031 14/14, 93 entradas, 7/14/21 s) **no** se toman como pruebas: cada cifra de
abajo se midió hoy sobre la rama integrada.

**Leyenda:** VERIFICADO · VERIFICADO MEDIANTE SIMULACIÓN · PENDIENTE DE ACCESO · PENDIENTE DE PRUEBA FÍSICA · ERROR

## 1. Identificación

| Elemento | Valor |
|---|---|
| Rama | `integracion/ecoaccess-final` (PR #28, actualizado sin forzar) |
| Commit de código evaluado | `3323fcb` (este informe va en el commit siguiente) |
| PR | https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/28 (abierto: faltan pruebas físicas, ver §12) |
| Web ciudadana | https://jorgegutho21.github.io/EcoAccess-2026-2/ |
| EcoAccess Challenge | https://jorgegutho21.github.io/EcoAccess-2026-2/juego/ (200, verificada en el navegador) |
| Panel ESP32 | http://localhost:8090 (200) |
| Bot | https://t.me/EcoAccessP3_JG_bot |

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
| **V033 en producción** | PENDIENTE (requiere tu autorización explícita al aplicarla) | No se aplicó. La app detecta su ausencia y sigue escribiendo solo la cabecera (V030) |

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
| Seguridad y reglas contra el paquete en el esquema aislado (`tests/integracion/JuegoConcurrenciaAislada`) | VERIFICADO | 20/20: inyección y HTML rechazados, sesión inválida o ajena → 401, respuesta inmutable, sin marca de correcta en la pregunta, solo SHA-256 de la sesión, concurso finalizado cerrado |
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

## 6. Visión (11 materiales)

Dataset activo `datasets/ecoaccess11`: **44 imágenes** (límite 200), 0 duplicados exactos y 0 dañadas (SHA-256 y
verificación PIL). Hay 25 fotos reales de ajuste, 9 negativas reales y 10 escenas compuestas, todas en el manifiesto.
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

## 9. Web ciudadana, panel ESP32 y voz

| Punto | Estado |
|---|---|
| Web ciudadana publicada (200) | VERIFICADO |
| Pasarela HTTPS y túnel en marcha (1 pasarela, 1 cloudflared) | VERIFICADO (procesos); flujo completo con QR: PENDIENTE DE ACCESO |
| Panel 8090 responde, sin abrir tapas al iniciar | VERIFICADO (200; el lanzador no movió motores) |
| `/panel-demo/` en Pages | PENDIENTE: se publica con el workflow unificado cuando el PR llegue a `main` (hoy 404) |
| Micrófono es-CO real | PENDIENTE DE ACCESO |

## 10. Compilación y pruebas C#

| Punto | Estado | Resultado |
|---|---|---|
| `dotnet build` Debug y Release de `EcoAccess.P3.sln` | VERIFICADO | correctas |
| Suite unitaria `EcoAccess.P3.Tests` | VERIFICADO | 2518 superadas, 0 con error, 1 omitida (Debug y Release compilados sobre el commit evaluado) |
| `EcoAccess.P3.Datos.Tests` (sin destino aislado) | VERIFICADO | 62 superadas; 192 omitidas: son de integración y se niegan a correr contra producción |

## 11. Instalación, accesos directos y Pages

| Punto | Estado | Evidencia |
|---|---|---|
| Exactamente 4 accesos en el escritorio real (OneDrive\Escritorio): ECOACCESS P3, ESP32, WEB y CHALLENGE, con iconos distintos | VERIFICADO | se creó CHALLENGE (icono dorado nuevo) y se abrieron los cuatro |
| P3 y ESP32 no duplican servicios | VERIFICADO | tras abrirlos: 1 bot, 1 app, 1 panel, 1 pasarela, 1 túnel |
| El instalador reproduce los 4 accesos | VERIFICADO EN CÓDIGO (sintaxis 0 errores); ejecución completa PENDIENTE para no crear una segunda instalación |
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
