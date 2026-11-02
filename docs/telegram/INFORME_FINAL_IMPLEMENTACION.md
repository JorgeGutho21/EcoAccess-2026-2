# Informe de implementación: Telegram con dos motores visuales

Fecha: 2 de noviembre de 2026. Rama: `desarrollo/telegram-doble-ia`.

**Conclusión:** el software está implementado y probado hasta donde el entorno lo permite. **No se declara funcional desde un teléfono, con Groq ni con el ESP32**, porque esas tres verificaciones reales siguen pendientes.

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

**Validado realmente** con Ollama 0.40.13 y `lfm3-vl:latest` en este equipo, sobre 198 fotos con licencia: 140 aciertos (70,7 %), 58 errores, 7 abstenciones, 19 falsas admisiones, 2 falsos rechazos, 0 fallos técnicos. Inferencia: media 2,42 s, mediana 2,35 s, P95 2,65 s, P99 3,01 s. Detalle por material: `docs/vision/07_reconocimiento_visual.md`.

Objetivos: P95 menor de 5 s **cumplido**; precisión de 90 % **no cumplido**. El vocabulario no se retocó tras ver estos resultados para no invalidar la medición. Las nuevas opciones sanitarias y metalizadas **no están medidas**: el corpus no contiene esas fotos.

## D. Qwen 3.8 en Groq

**Implementado y probado contra HTTP simulado. Pendiente de validar con la API real:** no hay clave en este entorno.

Verificado en la documentación oficial: `qwen/qwen3.8-27b` existe, está en **vista previa**, admite imágenes y JSON Schema, y cuesta 0,80 y 4,00 USD por millón de tokens de entrada y salida. El servidor real responde 401 sin clave, lo que solo demuestra que es alcanzable.

Implementado: HTTPS con TLS 1.2 o 1.3, sin redirecciones, tiempo máximo configurable, cancelación, dos envíos simultáneos como máximo, un reintento en fallos transitorios, 429 con `Retry-After`, bloqueo tras 401, 403 o 404, respuesta acotada a 64 KiB, rechazo de un modelo distinto o una respuesta incompleta, foto idéntica simultánea en una sola llamada, tokens, costo estimado y límite diario que sobrevive al reinicio.

Por confirmar con la API real: el modo `reasoning_effort: none` y `json_schema` sin `strict` son lectura de la documentación, sin medición. Tampoco se revisaron las condiciones de retención de datos de Groq: hazlo antes de ofrecerlo a terceros.

## E. Comparación

**Sin datos de Qwen 3.8: no se declara ganador ni precisión.** La herramienta está lista y solo mide lo que puede: `--comparar-vision`, con el mismo manifiesto y hashes para los dos motores. Las 198 fotos enviadas a Groq costarían unos 0,43 USD estimados, a razón de 2.048 tokens por imagen más unos 500 de instrucciones y 30 de salida. La cifra es un cálculo, no una factura.

## F. Telegram

| Prueba de la misión | Clase de evidencia | Resultado |
|---|---|---|
| 1. `/start` y menú | unitaria | aprobada |
| 2. Fotografía válida | modelo real y Oracle real, Telegram simulado | aprobada |
| 3. Archivo no compatible | unitaria | aprobada |
| 4. Cartón | modelo real: 24/30 | medida |
| 5. Botella plástica | modelo real: PET 12/13, plástico 10/18 | medida |
| 6. Multicapa | modelo real: 2/5; metalizado sin fotos | **débil** |
| 7. Residuo sanitario | unitaria: nunca se presenta como aprovechable; modelo sin fotos | parcial |
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

El token existe solo en tu perfil de Windows; este entorno no puede leerlo y no se intentó eludirlo.

No se ejecutaron pruebas de resistencia de larga duración ni de carga contra Telegram o Groq. Hay pruebas de concurrencia y de cola por persona, no una medición de memoria sostenida.

## G. Oracle

- **Validado realmente:** instalación desde cero en un contenedor temporal, 21 migraciones aplicadas; al repetir, 0.
- **Validado realmente:** `V018` aplicada en la base del proyecto. Crea una tabla vacía y no modifica otros datos.
- La admisión la decide el catálogo de Oracle, nunca el modelo. Se conserva la idempotencia de órdenes y ecopuntos.

## H. ESP32

| Aspecto | Estado |
|---|---|
| Bridge y caneca simulada, ventana de 20 s | **probado** con Oracle real y modelo real; simulador |
| Protocolo HMAC, órdenes vencidas o repetidas | probado |
| `-DispositivoSerial` en el lanzador | **implementado, sin ejecutar**: no hay puerto COM |
| Carga, calibración, fuente, apertura y 20 s físicos | **pendiente: sin hardware** |

No se cambió el protocolo ni el firmware. La fuente de 5 V y 1 A sigue sin validar.

## I. Pruebas

| Conjunto | Ejecutadas | Aprobadas | Fallidas | Omitidas |
|---|---|---|---|---|
| Unitarias | 1430 | 1429 | 0 | 1 |
| Integración con Oracle limpio | 197 | 193 | 0 | 4, que usan el modelo real |
| Modelo real con Oracle limpio | 4 | 2 | **1** | 1 |

- **Fallo sin resolver:** `FotoVisionReal_VinculaContratoSeguro…` exige la tarifa del piloto, que solo existe tras preparar el modo demostración. No se ejecutó con esa preparación: para hacerlo hay que usar `Preparar-Demostracion.ps1`, y no se corrió contra la base real para no crear créditos en ella. **No está demostrado que pase.**
- `ParticionFijada24FotosPublicas…` se omitió porque necesita otra variable de entorno.
- **Prueba intermitente:** `ArchivoDeSesionSolicitaCancelacionSinEliminarloNiMatarProcesos` falló una vez (5 s) con el equipo cargado y pasó en todas las repeticiones. Sin causa confirmada.
- La integración se ejecutó antes de un último cambio de texto en un mensaje; después se repitieron compilación y unitarias.

## J. Riesgos

1. **Precisión de 70,7 %:** una de cada tres fotos puede salir mal. Lo mitigan la confirmación y la corrección manual.
2. **Conflicto de requisitos sobre residuos peligrosos:** por tu decisión de una sola caneca, se admite cualquier residuo, incluidos jeringas y pilas, con aviso. Sigue siendo un riesgo real para quien manipule la caneca.
3. **Tarifa de 2 puntos** también en residuos sanitarios y peligrosos: decisión pendiente, porque premia depositarlos. No se cambió.
4. **Groq en vista previa:** puede retirarse con poco aviso. El bot no lo sustituye solo.
5. **Memoria del equipo:** con dos Oracle y Ollama quedan unos 4,5 GB libres de 24.
6. **Primer uso del bot real sin comprobar:** pueden aparecer fallos que los dobles no reproducen.

## K. Credenciales pendientes

| Credencial | Comando | Dónde se obtiene |
|---|---|---|
| Clave de Groq | `Configurar-Credenciales.ps1 -Groq` | `console.groq.com/keys` |
| Token de Telegram, solo si no está guardado | `Configurar-Credenciales.ps1 -Telegram` | `@BotFather` |

No las pegues en un chat. Pasos completos: `GUIA_TELEGRAM_DOBLE_IA.md`.

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
| Qwen 3.8 en Groq | sí | HTTP simulado | **pendiente** |
| Autorización de fotos | sí | sí | tabla real; flujo pendiente en Telegram |
| Credenciales y scripts | sí | parcial | pendiente con tu perfil |
| Telegram oficial | sí | simulado | **pendiente** |
| Bridge y simulador | sí | sí | sí, como simulador |
| ESP32 físico | sin cambios | banco no ejecutado | **pendiente** |

Criterios de aceptación pendientes de evidencia: 2 con token real, 3, 6, 8 con Groq real y 16. No se da un porcentaje global porque no mediría nada adicional a esta tabla.
