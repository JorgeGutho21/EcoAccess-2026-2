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

## Decisiones de diseño

- **Vocabulario cerrado:** el modelo elige una de 17 opciones y una certeza. El texto para la persona y el código del catálogo los pone EcoAccess.
- **Certeza baja o «no sé»:** no se propone abrir. La certeza es la que declara el modelo: no está calibrada.
- **Confirmación obligatoria:** las propuestas equivocadas se corrigen antes de abrir; ninguna abre la caneca por sí sola.
- **Imagen a 512 px** y contexto de 2048.
- **Sustituible:** `IClasificadorMaterial` aísla al modelo de Telegram, Oracle, reglas y ESP32.

## Limitaciones

- **No se dispuso de las fotos originales de la lata de bebida energética ni del envase Celema:** pendiente de evaluarlas. Multicapa se midió solo con 2 fotos.
- Muestras pequeñas: los porcentajes orientan, no garantizan exactitud.
- Una vista no prueba el polímero: «PET» es una propuesta visual.

Reproducción: `ECOACCESS_PROBAR_VISION_REAL=1` y `ECOACCESS_PROBAR_VISION_INTEGRADA=1` activan las pruebas con el modelo real.
Resultados y fuentes con licencia de las fotos: `docs/vision/resultados/`. Comparador: `scripts/pruebas/vision/comparar_modelos.py`. Las fotos no se incluyen en el repositorio.
