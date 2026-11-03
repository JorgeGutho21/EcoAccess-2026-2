# Informe de implementación: Telegram con dos motores visuales

Fecha: 2 de noviembre de 2026. Rama: `desarrollo/telegram-doble-ia`.

**Conclusión:** el software está implementado y probado hasta donde el entorno lo permite. **No se declara funcional desde un teléfono, con inferencia real de Groq ni con el ESP32**, porque esas tres verificaciones siguen pendientes.

**Entorno:** las herramientas de desarrollo corren en una aplicación aislada y no ven los User Secrets del perfil del responsable: allí `--comprobar-groq` dice `NoConfigurado` aunque la clave exista. El responsable sí confirmó en su PowerShell la identidad del bot, el token, la clave y el modelo. Lo que necesita esas credenciales se ejecuta desde su PowerShell.

Estados usados: **implementado** (el código existe), **probado** (prueba automática verificable), **validado realmente** (entorno real), **pendiente** (falta una condición).

## A. Estado inicial

- Solo había un motor visual: LFM3-VL local. No existía integración con Groq ni selector de modos.
- Un adjunto que no fuera foto (PDF, video, audio) o una imagen enviada como archivo no recibía respuesta.
- El vocabulario no distinguía residuos sanitarios, jeringas ni empaques metalizados.
- El mensaje de privacidad afirmaba siempre un análisis local.
- El panel de escritorio mostraba «selección manual» cuando no había `Vision:Proveedor`, aunque el bot usara el motor local.
- Una instalación limpia fallaba en la migración `V016` (corregido en esta rama).

## B. Modificaciones

| Área | Archivos | Motivo |
|---|---|---|
| Motores | `Vision/ClasificadorGroq`, `SelectorVision`, `ConfiguracionVision`, `UsoGroq`, `DiagnosticoGroq`, `MetricasVision` | segundo motor, modos, límite diario, diagnóstico y métricas |
| Contrato | `IClasificadorMaterial`, `VocabularioVision`, `ServicioClasificacionImagen` | marca de envío externo, estado observable, validación estricta, mensajes |
| Conversación | `ServicioConversacionTelegram`, `MensajesTelegram`, `TraductorUpdates`, `AsistenteEco` | autorización, `/revocarfotos`, segunda opinión, adjuntos |
| Oracle | `V018__consentimiento_vision_externa.sql`, `RepositorioConsentimientoVisionOracle` | autorización persistente |
| Herramientas | `Configurar-Credenciales`, `Verificar-EcoAccess-Telegram`, `Iniciar-EcoAccess-Telegram`, `preparar_corpus.py` | configuración única, diagnóstico, lanzador, corpus reproducible |
| Panel | `EstadoServiciosDemostracion` | estado de Groq y corrección del estado local |

## C. LFM3-VL

**Validado realmente** con Ollama 0.40.13 y `lfm3-vl:latest` en este equipo, sobre 198 fotos con licencia, con el vocabulario ajustado: 144 aciertos (72,7 %), 54 errores, 23 abstenciones, 12 falsas admisiones, 10 falsos rechazos, 0 fallos técnicos. Inferencia: media 2,36 s, mediana 2,32 s, P95 2,50 s, P99 2,84 s. Antes del ajuste: 140 aciertos, 19 falsas admisiones, 2 falsos rechazos y 7 abstenciones. Método, límites y detalle: `docs/vision/07_reconocimiento_visual.md`.

Objetivos: P95 menor de 5 s **cumplido**; precisión de 90 % **no cumplido**. El ajuste cambia falsas admisiones por más selección manual. Metalizado y multicapa siguen débiles.

## D. Qwen 3.8 en Groq

**Clave y modelo validados por el responsable con `--comprobar-groq`. Inferencia real con foto: pendiente.** El cliente solo se ha probado contra HTTP simulado.

Verificado en la documentación oficial: `qwen/qwen3.8-27b` existe, está en **vista previa**, admite imágenes y JSON Schema, y cuesta 0,80 y 4,00 USD por millón de tokens de entrada y salida.

Corregido al revisar los límites: la pausa de 2,2 s entre fotos respetaba las solicitudes por minuto, pero no los tokens por minuto, y la comparación habría fallado con 429. Ahora son 20 s, configurables, sobre 66 fotos. Un error 400 conserva el código de la API para diagnosticarlo, sin su texto libre.

Implementado: HTTPS con TLS 1.2 o 1.3, sin redirecciones, tiempo máximo configurable, cancelación, dos envíos simultáneos como máximo, un reintento en fallos transitorios, 429 con `Retry-After`, bloqueo tras 401, 403 o 404, respuesta acotada a 64 KiB, rechazo de un modelo distinto o una respuesta incompleta, foto idéntica simultánea en una sola llamada, tokens, costo estimado y límite diario que sobrevive al reinicio.

Por confirmar con la API real: el modo `reasoning_effort: none` y `json_schema` sin `strict` son lectura de la documentación, sin medición. Tampoco se revisaron las condiciones de retención de datos de Groq: hazlo antes de ofrecerlo a terceros.

## E. Comparación

**Sin datos de Qwen 3.8: no se declara ganador ni precisión, y no se cambia el motor principal.** La herramienta está lista: `--comparar-vision`, con el mismo manifiesto y hashes para los dos motores. `manifiesto_groq.json` tiene 66 fotos; se estiman 0,14 USD, a razón de 2.048 tokens por imagen más unos 500 de instrucciones y 30 de salida. La cifra es un cálculo, no una factura.

## F. Telegram

| Prueba de la misión | Clase de evidencia | Resultado |
|---|---|---|
| 1. `/start` y menú | unitaria | aprobada |
| 2. Fotografía válida | modelo real y Oracle real, Telegram simulado | aprobada |
| 3. Archivo no compatible | unitaria | aprobada |
| 4. Cartón | modelo real: 24/30 | medida |
| 5. Botella plástica | modelo real, familia plástica: 27/31 | medida |
| 6. Multicapa y metalizado | modelo real: multicapa 2/5 y 0/4; metalizado 1/6 | **débil** |
| 7. Residuo sanitario | unitaria: nunca se presenta como aprovechable; modelo real: toalla 3/3, condón 1/3, jeringa 5/5 | parcial |
| 8. Material desconocido | unitaria | aprobada |
| 9. Ollama desconectado | unitaria con motores falsos | aprobada |
| 10. Groq sin credenciales | ejecución real de `--comprobar-groq` y unitaria | aprobada |
| 11. Groq con 429 | HTTP simulado | aprobada |
| 12. Oracle desconectado | unitaria: la foto no sale y no hay orden | aprobada |
| 13. ESP32 desconectado | integración con Oracle real y caneca sin respuesta | aprobada; sin hardware |
| 14. Confirmación duplicada | unitaria e integración | aprobada |
| 15. Reinicio del bot | unitaria e integración | aprobada |
| 16. Varios usuarios | unitaria | aprobada |
| 17. Privacidad | unitaria: 12 casos de autorización | aprobada |
| 18. Bot oficial real | — | **pendiente: necesita tu token y tu teléfono** |

El token existe solo en el perfil de Windows del responsable. Telegram se marcará validado desde un teléfono únicamente cuando el responsable confirme haber enviado una foto real y recibido respuesta.

No se ejecutaron pruebas de resistencia de larga duración ni de carga contra Telegram o Groq. Hay pruebas de concurrencia y de cola por persona, no una medición de memoria sostenida.

## G. Oracle

- **Validado realmente:** instalación desde cero en un contenedor temporal, 21 migraciones aplicadas; al repetir, 0.
- **Validado realmente:** `V018` aplicada en la base del proyecto. Crea una tabla vacía y no modifica otros datos.
- La admisión la decide el catálogo de Oracle, nunca el modelo. Se conserva la idempotencia de órdenes y ecopuntos.
- Las pruebas que escriben corrieron solo en bases temporales, retiradas al terminar. No se crearon ecopuntos en la base real.

**Admisión y tarifa son cosas distintas.** En una base temporal preparada como el piloto: 41 materiales admitidos y regla de 2 puntos en los 41, incluidos 14 no aprovechables y 14 peligrosos. Un material **no admitido** en una caneca no genera orden ni puntos (`RepositorioEmiteOrdenEspecialSiLaCanecaLoAdmite` y la admisión del servicio). Los puntos solo se acreditan tras verificar un depósito. Por decisión del responsable, los 2 puntos se conservan en todo lo admitido. **Riesgo:** en la caneca única, depositar y verificar una jeringa o un condón suma puntos, lo que premia ese depósito. La política se cambia añadiendo o retirando reglas en `regla_ecopuntos`, sin tocar la admisión.

## H. ESP32

| Aspecto | Estado |
|---|---|
| Telegram simulado → Oracle → Bridge → caneca simulada, con 20 s, cierre, eventos, aviso y sin duplicar orden | **probado** en `RecorridoCompletoDeEco…` con Oracle temporal y modelo real; no es una apertura física |
| Guía de validación | `docs/hardware/GUIA_VALIDACION_ESP32.md`, sin ejecutar |
| Protocolo HMAC, órdenes vencidas o repetidas | probado |
| `-DispositivoSerial` en el lanzador | **implementado, sin ejecutar**: no hay puerto COM |
| Carga, calibración, fuente, apertura y 20 s físicos | **pendiente: sin hardware** |

No se cambió el protocolo ni el firmware. La fuente de 5 V y 1 A sigue sin validar.

## I. Pruebas

| Conjunto | Ejecutadas | Aprobadas | Fallidas | Omitidas |
|---|---|---|---|---|
| Unitarias | 1431 | 1430 | 0 | 1 |
| Integración, Oracle temporal con piloto | 197 | 192 | 0 | 5 |
| Modelo real, Oracle temporal con piloto | 4 | 3 | 0 | 1 |

- **Resuelta:** `FotoVisionReal_VinculaContratoSeguro…` fallaba por el montaje: exige el piloto y su tarifa. Con `Preparar-Demostracion.ps1` en una base temporal pasa. No había defecto en el código.
- `ParticionFijada24FotosPublicas…` se omitió porque necesita otra variable de entorno.
- **Mitigada, causa sin confirmar:** `ArchivoDeSesionSolicitaCancelacion…` falló una vez y no se reprodujo en 8 corridas seguidas en parejas simultáneas. Hipótesis: el temporizador usa el grupo de hilos y 3 s era un margen justo con el equipo cargado. El margen pasó a 30 s, porque la prueba mide que la señal llega, no su latencia.
- La integración se ejecutó antes del cambio de vocabulario; después se repitieron compilación y unitarias, y el modelo se midió con 198 fotos. El vocabulario no afecta a Oracle.

## J. Riesgos

1. **Precisión de 72,7 %:** más de una de cada cuatro fotos puede salir mal. Lo mitigan la confirmación y la corrección manual.
2. **Conflicto de requisitos sobre residuos peligrosos:** por tu decisión de una sola caneca, se admite cualquier residuo, incluidos jeringas y pilas, con aviso. Sigue siendo un riesgo real para quien manipule la caneca.
3. **Tarifa de 2 puntos** también en residuos sanitarios y peligrosos: se conserva por decisión del responsable del proyecto (2 de noviembre de 2026), aunque premia depositarlos. Ver la sección G.
4. **Groq en vista previa:** puede retirarse con poco aviso. El bot no lo sustituye solo.
5. **Memoria del equipo:** con dos Oracle y Ollama quedan unos 4,5 GB libres de 24.
6. **Primer uso del bot real sin comprobar:** pueden aparecer fallos que los dobles no reproducen.

## K. Credenciales pendientes

| Credencial | Comando | Dónde se obtiene |
|---|---|---|
| Clave de Groq | **ya configurada** por el responsable | `console.groq.com/keys` |
| Token de Telegram | **ya configurado** por el responsable | `@BotFather` |

No hace falta generarlas de nuevo. Pasos que faltan: `GUIA_TELEGRAM_DOBLE_IA.md`, parte O.

## L. Ejecución

Desde la carpeta del proyecto, en tu propia ventana de PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-EcoAccess-Telegram.ps1
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Verificar-EcoAccess-Telegram.ps1
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Detener-Demostracion.ps1
```

El lanzador se probó hasta la comprobación de Telegram, donde se detuvo correctamente sin iniciar procesos. **Su tramo final no se ha ejecutado con un token real.**

## M. Git

Commits por unidad en la rama `desarrollo/telegram-doble-ia`, con la identidad del autor. El pull request queda **sin fusionar**: hay verificaciones críticas pendientes y falta revisión.

## N. Veredicto

| Componente | Implementado | Probado | Validado realmente |
|---|---|---|---|
| Compilación Release | sí | sí | sí |
| Oracle y migraciones | sí | sí | sí |
| LFM3-VL | sí | sí | sí; precisión bajo el objetivo |
| Selector y respaldo | sí | sí | no: falta Groq real |
| Qwen 3.8 en Groq | sí | HTTP simulado | clave y modelo sí; **inferencia pendiente** |
| Autorización de fotos | sí | sí | tabla real; flujo pendiente en Telegram |
| Credenciales y scripts | sí | parcial | pendiente con tu perfil |
| Telegram oficial | sí | simulado | identidad y token sí; **teléfono pendiente** |
| Bridge y simulador | sí | sí | sí, como simulador |
| ESP32 físico | sin cambios | banco no ejecutado | **pendiente** |

Criterios de aceptación pendientes de evidencia: 2 con token real, 3, 6, 8 con Groq real y 16. No se da un porcentaje global porque no mediría nada adicional a esta tabla.
