---
archivo: 08_OLLAMA_E_INTELIGENCIA_ARTIFICIAL
titulo: Ollama e inteligencia artificial visual en EcoAccess P3
subtitulo: Modelo local LFM3-VL, motor externo opcional, vocabulario cerrado, política de apertura, privacidad y las seis rondas de medición
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

# Las siete rondas de medición

| Ronda | Fotos | Exactitud | Falsas admisiones | Ofertas indebidas | Independiente |
|---|---|---|---|---|---|
| 3 | 131 | 71,0 % | 7 | 6 | ya no (se usó para corregir) |
| 4 | 141 | 84,4 % | 0 | 4 | sí |
| **5** | **102** | **75,5 %** | **2** | **10** | **sí** |
| 6 | 19 | 73,7 % | 1 | 1 | sí (muestra pequeña) |

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

## Sexta ronda: objetos confundibles

{{incluir:docs/vision/EVALUACION_RONDA6_INDEPENDIENTE.md}}

## Séptima ronda: personas, animales y comida

{{incluir:docs/vision/EVALUACION_RONDA7_INDEPENDIENTE.md}}

# Correcciones del 11/11/2026: personas, comida y huevos

Cada cambio sigue el mismo esquema: qué fallaba, por qué, qué se cambió, dónde y cómo se prueba.

| | Persona leída como cartón | Pescado cocinado leído como animal | Huevos sin recorrido |
|---|---|---|---|
| **Problema** | `persona-03` recibió «Sí, continuar» para depositar cartón | un plato con pescado y papas recibió «Identifiqué un animal» | unos huevos enteros quedaron en «comida» y el recorrido no seguía |
| **Causa** | las dos preguntas al modelo (nombre y material) dijeron «cartón»; nadie preguntaba si el sujeto era una persona | «fish» solo estaba en la lista de animales; la frase se cortaba en «with» | «eggs» (plural) no tenía clave |
| **Solución** | tercera pregunta independiente antes de cualquier oferta: «¿el sujeto es una persona o un animal vivo?» | carne o pescado junto a una palabra de cocina («grilled», «plate», «rice»…) es comida; con «raw», comida cruda | clave nueva `eggs`; un huevo frito es comida servida |
| **Archivo** | `src/EcoAccess.P3.Bot/Vision/ClasificadorOllamaLocal.cs` y `VocabularioVision.cs` | `src/EcoAccess.P3.Logica/Servicios/InterpretadorNombreObjeto.cs` y `CatalogoObjetos.cs` | los mismos |
| **Clase y método** | `VocabularioVision.SiHaySujetoVivo` y `RespondioNo` | `InterpretadorNombreObjeto.Alimento` | `InterpretadorNombreObjeto.Alimento` |
| **Ejemplo** | nombre «cardboard box», material «cardboard», sujeto «yes»: la foto queda como «personas», sin material | «Grilled fish with potatoes» → `served_fish` | «Brown eggs» → `eggs` |
| **Prueba** | `VisionLocalTests.SujetoVivo_AnulaElMaterialYNoPuedeAbrir` | `InterpretadorAlimentosTests` | `InterpretadorAlimentosTests` |
| **Resultado** | pasa con un modelo simulado; la eficacia real se mide con fotos nuevas | pasa | pasa |

**¿Qué responde Eco ante comida?** No da por hecho que sea un residuo. Responde, por ejemplo: «Identifiqué comida
preparada, que incluye pescado y acompañamientos. Si son restos que deseas desechar, puedo orientarte hacia la caneca
verde. ¿Deseas desechar estos alimentos?», con los botones «Sí, desechar» y «No, solo consultar».

- **«Sí, desechar»:** indica la caneca verde y «Ecopuntos: 0, según la política actual para residuos orgánicos». No abre
  ninguna tapa: la foto no abre la verde, y la verde 1400 sigue fuera de servicio físico.
- **«No, solo consultar»:** no registra nada.

Lo prueban tres pruebas de `ServicioConversacionTelegramTests`:
- `Alimentos_PreguntanSiSeDesechanSinAbrirNiCrearSolicitud`;
- `SiDesechar_OrientaALaVerdeConCeroEcopuntosYNoAbre`;
- `NoSoloConsultar_NoRegistraNada`.

**Importante:** nada de esto es un entrenamiento del modelo. El modelo es el mismo; cambian las preguntas que se le hacen
y las reglas en C# que interpretan sus respuestas.

# Pruebas automáticas de la visión

{{pruebas:Vision}}

# Qué está verificado y qué no

| Aspecto | Estado |
|---|---|
| Motor local con fotos públicas etiquetadas | Verificado en simulación (6 rondas) |
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
