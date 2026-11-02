# 07. Reconocimiento visual

El modelo **solo propone**: nunca controla los servomotores. Decide la lógica de negocio y confirma la persona.

## Equipo de medición

Intel i5-10300H (4 núcleos), 24 GB de RAM, NVIDIA GTX 1650 con 4 GB y Ollama 0.40.13, con otras aplicaciones abiertas.

## Por qué se cambió de modelo

Qwen3-VL 2B tardaba cerca de 18 s por foto. El desglose mostró que **17,4 s corresponden a codificar la imagen**, fijos con cualquier tamaño (336, 512 o 1024 px) e incluso con el modelo al 100 % en GPU; generar la respuesta tarda 0,3 s. Ningún ajuste de parámetros lo resolvía.

## Comparación

| Modelo | Tamaño | Conjunto | Aciertos | Mediana | P95 | Decisión |
|---|---|---|---|---|---|---|
| **LFM3-VL** (`lfm3-vl`) | 1,38 GB | final | **38/50** | **2.33 s** | **2.45 s** | **elegido** |
| Qwen3-VL 2B | 1,9 GB | evaluación | 26/44 | 17.94 s | 46.24 s | descartado por lentitud y precisión |
| Moondream 2 | 1,62 GB | ajuste | 3/22 | 5.39 s | 5.51 s | descartado: confunde casi todo |

No se evaluó un clasificador ONNX especializado: las redes entrenadas con TrashNet solo conocen seis clases y no distinguen multicapa, pilas ni medicamentos.

LFM3-VL razona por defecto y agota los tokens sin responder: es **obligatorio** enviar `think: false`.

## Evaluación

- **Ajuste:** 46 fotos, usadas para definir el vocabulario y el prompt.
- **Evaluación inicial, sin retoques:** 31/44 en 44 fotos. Sus errores no se usaron para ajustar, salvo como orientación general, y por eso se midió después sobre fotos nuevas.
- **Conjunto final:** 30 fotos de TrashNet no vistas (licencia MIT) y 20 cotidianas de Wikimedia Commons con licencia libre, revisadas a mano.

| Resultado final | Valor |
|---|---|
| Aciertos totales | 38/50 (76 %) |
| Fotos cotidianas | 18/20 |
| Abstenciones | 3 |
| Confusiones entre materiales admitidos | 5 |
| Propuestas admitidas para algo que no lo era | 4 |
| Errores técnicos | 0 |
| Tiempo medio, mediana, P95 | 2.33 s, 2.33 s, 2.45 s |
| Primera foto sin precarga | cerca de 8 s adicionales; el bot precarga el modelo al iniciar |

Por material en el conjunto final: cartón 7/7, vidrio 6/6, lata 3/3, plástico 4/5, papel 4/7, metal 3/5, PET 2/3, multicapa 1/2, pilas 2/2, y **residuo mixto sucio 1/5, su punto débil**.

Objetivos del proyecto: respuesta habitual menor de 5 s y P95 menor de 8 s, **cumplidos en la inferencia local**. El tiempo adicional de Telegram no se ha medido con el bot real.

## Medición ampliada de LFM3-VL (2 de noviembre de 2026)

Con el vocabulario de 22 opciones, sobre 198 fotos con licencia: 36 de Wikimedia Commons y 162 de TrashNet (27 por clase, semilla fija, sin las fotos de ajuste). Es **otro conjunto**: no se compara directamente con el 38/50 anterior. El vocabulario no se retocó después de ver estos resultados.

| Resultado | Valor |
|---|---|
| Aciertos | 140/198 (70,7 %) |
| Errores | 58, de ellos 7 abstenciones |
| Falsas admisiones (propuso aprovechable algo que no lo es) | 19 |
| Falsos rechazos | 2 |
| Fallos técnicos | 0; disponibilidad observada 100 % |
| Inferencia: media, mediana, P95, P99 | 2,42 s, 2,35 s, 2,65 s, 3,01 s |

Por etiqueta: cartón 24/30, papel 21/30, vidrio 27/29, lata 27/28, PET 12/13, plástico 10/18, orgánico 8/8, multicapa 2/5, pilas 3/3, medicamento 2/2, bombillo 2/2, metal 2/4, aparato 0/1, sin residuo 0/4. El residuo mixto de TrashNet es su punto débil: de 27 fotos, 21 cayeron fuera de las etiquetas aceptadas para esa clase, y de ahí salen 9 de las 19 falsas admisiones. Otras 8 son plásticos confundidos con lata, vidrio, multicapa u orgánico. `PLASTICO_SIN_ID` figura en Oracle como no aprovechable, así que una botella que el modelo no reconoce como PET no se ofrece como reciclable.

Frente a los objetivos: **P95 menor de 5 s, cumplido**; ausencia de fallos técnicos, cumplido; **precisión de 90 %, no cumplido**. Por eso la persona siempre confirma antes de abrir y puede corregir el material. Los tiempos son de inferencia con corpus en disco; el tiempo total por Telegram se mide aparte en `ProcesadorUpdates`.

Resultados: `resultados/lfm_comparacion_198.json`. Manifiesto con hashes: `resultados/manifiesto_comparacion_198.json`. Para reconstruir el corpus y repetir la medición, con los dos motores si hay clave de Groq (las fotos se envían a Groq; hay pausa de 2,2 s entre fotos y aplica el límite diario):

```powershell
python scripts\pruebas\vision\preparar_corpus.py C:\eco-corpus
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comparar-vision C:\eco-corpus\manifiesto.json 198
```

Usa una ruta corta: .NET Framework 4.8 no abre rutas de más de 260 caracteres. La comparación solo lee el catálogo: no crea solicitudes, órdenes, depósitos ni ecopuntos.

## Qwen 3.8 en Groq

Segundo motor, `qwen/qwen3.8-27b`, en **vista previa** según la documentación oficial consultada el 2 de noviembre de 2026: admite imágenes y JSON Schema; 0,80 y 4,00 USD por millón de tokens de entrada y salida; 2048 tokens por imagen.

**Sin resultados propios todavía:** no hay clave de Groq en el entorno de medición, así que no se declara precisión ni velocidad. El cliente solo está probado contra un HTTP simulado; el modo de razonamiento `none` y la salida `json_schema` sin `strict` quedan por confirmar con la API real.

## Decisiones de diseño

- **Vocabulario cerrado:** el modelo elige una de 22 opciones y una certeza. El texto para la persona y el código del catálogo los pone EcoAccess. Groq añade `estado` y `requiere_aclaracion`, también cerrados; un campo inesperado invalida la respuesta.
- **Certeza baja o «no sé»:** no se propone abrir. La certeza es la que declara el modelo: no está calibrada.
- **Confirmación obligatoria:** las propuestas equivocadas se corrigen antes de abrir; ninguna abre la caneca por sí sola.
- **Imagen a 512 px** y contexto de 2048.
- **Sustituible:** `IClasificadorMaterial` aísla al modelo de Telegram, Oracle, reglas y ESP32. `SelectorVision` elige el motor con `Vision:Modo`.
- **Sanitarios y metalizados:** condón, pañal o toalla, servilleta usada, jeringa y empaque metalizado tienen opción propia; sus códigos son no aprovechables o peligrosos en Oracle. El corpus no incluye fotos de estos objetos: **esas opciones no están medidas**.

## Limitaciones

- **No se dispuso de las fotos originales de la lata de bebida energética ni del envase Celema:** pendiente de evaluarlas. Multicapa se midió solo con 2 fotos.
- Muestras pequeñas: los porcentajes orientan, no garantizan exactitud.
- Una vista no prueba el polímero: «PET» es una propuesta visual.

Reproducción: `ECOACCESS_PROBAR_VISION_REAL=1` y `ECOACCESS_PROBAR_VISION_INTEGRADA=1` activan las pruebas con el modelo real.
Resultados y fuentes con licencia de las fotos: `docs/vision/resultados/`. Comparador: `scripts/pruebas/vision/comparar_modelos.py`. Las fotos no se incluyen en el repositorio.
