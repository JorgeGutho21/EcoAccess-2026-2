---
archivo: 08_OLLAMA_E_INTELIGENCIA_ARTIFICIAL
titulo: Ollama e inteligencia artificial visual en EcoAccess P3
subtitulo: Modelo local LFM3-VL, motor externo opcional, vocabulario cerrado, política de apertura, privacidad y las cinco rondas de medición
codigo: 08
---

# Qué problema resuelve la visión

Una persona frente a la caneca no siempre sabe si su residuo es aprovechable. EcoAccess le permite **enviar una foto por
Telegram** y recibir la caneca correcta: blanca (aprovechables), verde (orgánicos), negra (no aprovechables) o un punto
posconsumo (pilas, electrónicos, medicamentos).

La inteligencia artificial **propone**, pero no decide sola. Lo que se dice a la persona, el material del catálogo y la
decisión de ofrecer una apertura los pone el código C#, con reglas comprobadas por pruebas.

# Conceptos básicos

- **Modelo de visión y lenguaje (VLM):** un modelo que recibe una imagen y texto y responde texto. EcoAccess usa **LFM3-VL**.
- **Ollama:** un programa que ejecuta modelos en el propio PC y los expone en `http://127.0.0.1:11434`. La foto no sale del equipo.
- **Inferencia:** usar un modelo ya entrenado para obtener una respuesta. **EcoAccess no entrena modelos.** Lo que se ajusta es el vocabulario, las instrucciones y las reglas de C#.
- **Certeza:** el número entre 0 y 1 que el modelo acompaña a su respuesta. Por sí solo no es una garantía.
- **Falsa admisión:** el sistema ofrecería abrir la tapa para algo que no debía entrar. Es el error más grave.

# Arquitectura de la visión

| Pieza | Clase | Responsabilidad |
|---|---|---|
| Contrato | `IClasificadorMaterial` (Lógica) | lo que el resto del sistema sabe de la visión |
| Motor local | `ClasificadorOllamaLocal` | llama a Ollama con la foto en base64 y valida el JSON de respuesta |
| Motor externo opcional | `ClasificadorGroq` | segunda opinión en Groq, solo con consentimiento y clave configurada |
| Selector | `SelectorVision` | un solo clasificador para el bot con dos motores intercambiables |
| Activación | `SeleccionVisionLocal` | si Ollama no está, el bot ofrece selección manual |
| Vocabulario | `VocabularioVision` | lista cerrada de respuestas posibles y su traducción al catálogo |
| Calidad | `CalidadImagen` | descarta fotos borrosas o vacías **antes** del modelo |
| Preparación | `PreparadorImagen` | decodifica con límites, orienta y quita EXIF |
| Política | `PoliticaAperturaAutomatica` (Lógica) | decide si se puede ofrecer abrir |
| Medición | `EvaluacionObjetos`, `MetricasVision` | evalúa fotos etiquetadas sin crear solicitudes |

# El vocabulario cerrado

El modelo no escribe texto libre para la persona: elige una opción de una lista y da una certeza.

{{codigo:src/EcoAccess.P3.Bot/Vision/VocabularioVision.cs:11:30}}

Así se evita que el modelo invente un material que no existe en el catálogo, o que diga algo inapropiado.

# La llamada a Ollama

{{codigo:src/EcoAccess.P3.Bot/Vision/ClasificadorOllamaLocal.cs:103:118}}

- `temperature = 0`: la misma foto produce la misma respuesta.
- `num_ctx = 2048`: un contexto pequeño, suficiente para una foto y la instrucción.
- `keep_alive`: cuánto tiempo conserva Ollama el modelo en memoria. Por defecto son 15 minutos (`Vision:Ollama:MantenerCargado`). La primera foto tras cargar el modelo tarda más: 16,6 s en la ronda 5.

# La política de apertura

{{codigo:src/EcoAccess.P3.Logica/Servicios/PoliticaAperturaAutomatica.cs:12:33}}

Para ofrecer una apertura deben cumplirse **todas** estas condiciones:

1. El material fue identificado y la respuesta fue corroborada.
2. El objeto no se ve contaminado.
3. El material está admitido en esa caneca y es aprovechable.
4. El objeto y el material coinciden en una lista de pares permitidos.

Papel, multicapa, bolsas y orgánicos quedan fuera de la lista, porque su error medido es alto. Para ellos el bot orienta,
pero no ofrece abrir.

# Privacidad

- La foto **no se guarda**. En Oracle queda solo `CONSULTA_VISION` (V023): resultado, material y latencia, sin imagen ni identificador de Telegram.
- El motor externo exige un **consentimiento** vigente (`CONSENTIMIENTO_VISION_EXTERNA`), que se puede revocar con `/revocarfotos`.
- Una foto con personas no se envía al servicio externo.

# Las cinco rondas de medición

| Ronda | Fotos | Exactitud | Falsas admisiones | Ofertas indebidas | Independiente |
|---|---|---|---|---|---|
| 3 | 131 | 71,0 % | 7 | 6 | ya no (se usó para corregir) |
| 4 | 141 | 84,4 % | 0 | 4 | sí |
| **5** | **102** | **75,5 %** | **2** | **10** | **sí** |

La quinta ronda (`docs/vision/EVALUACION_RONDA5_INDEPENDIENTE.md`) usó 156 fotos candidatas de Wikimedia Commons con
licencias libres. Se evaluaron 102 y se excluyeron 54, con el motivo escrito. Todas se etiquetaron **antes** de ejecutar el
modelo.

Resultados clave:

- 20 fotos de personas y animales: **ninguna oferta de apertura**.
- 0 fallos técnicos.
- Latencia: mediana de 2,5 s y p95 de 3,8 s.
- Puntos débiles: icopor (4 de 7 leídos como plástico rígido) y multicapa (0 de 5).

**No se ajustó nada con la ronda 5**, para que siga siendo independiente. Las correcciones candidatas quedan para una
sexta ronda nueva.

# Pruebas automáticas de la visión

{{pruebas:Vision}}

# Qué está verificado y qué no

| Aspecto | Estado |
|---|---|
| Motor local con fotos públicas etiquetadas | Verificado en simulación (5 rondas) |
| Fotos desde un teléfono real por Telegram | Pendiente del propietario |
| Motor externo Groq | Implementado; depende de la clave del propietario |
| Entrenamiento propio de un modelo | **No existe** y no se afirma |

# Preguntas probables

**¿El modelo fue entrenado con fotos de Colombia?** No. Se usa un modelo preentrenado. Lo que se ajustó es el
vocabulario y las reglas de C#, y cada ajuste se midió con fotos nuevas.

**¿Por qué bajó la exactitud en la ronda 5?** Porque incluyó a propósito materiales difíciles (icopor, metalizados,
multicapa) que no estaban en la ronda 4. Se informa la cifra real.

**¿Qué pasa si el modelo se equivoca de material?** La apertura solo se ofrece para pares seguros, el ciudadano debe
confirmarla y los ecopuntos se acreditan cuando un operario verifica el depósito.
