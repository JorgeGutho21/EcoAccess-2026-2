# Reconocimiento general de objetos

Eco identifica primero lo que aparece en la foto y solo después decide si EcoAccess puede recibirlo. Los motores siguen siendo LFM3-VL en Ollama local y Qwen 3.8 en Groq; `Vision:Modo` sigue en `Automatico`.

## Errores anteriores y sus causas

| Síntoma | Causa | Corrección |
|---|---|---|
| Perro, mouse o personas: «No pude identificarlo con seguridad» | La lista cerrada de 22 opciones solo tenía residuos, y el flujo convertía «sin residuo» y «desconocido» en la misma frase | Primera pregunta con nombre libre; 55 objetos con respuesta propia |
| Comida servida tratada como residuo | La opción `food` era la única con alimentos | `served_food` y `food_scraps` son objetos distintos |
| Botella: «La solicitud venció» | La solicitud 966 empezó a las 12:06:21 y la foto llegó a las 12:21:37: 15 min 16 s. Oracle cuenta 15 min desde el inicio, pero cada botón renovaba solo la conversación | Una solicitud vencida se cierra y la foto abre otra si corresponde. Nunca se reutiliza |
| Toda foto exigía una sesión | La sesión se validaba antes de mirar la foto | Primero se reconoce; sesión, presencia y caneca solo se comprueban para abrir |

## Recorrido de una foto

1. **Calidad, sin modelo.** Una foto borrosa o vacía no se consulta (`CalidadImagen`), porque el modelo inventa un objeto antes que abstenerse.
2. **Nombre libre.** LFM3-VL nombra el objeto principal en pocas palabras. El texto no se muestra ni se guarda: `InterpretadorNombreObjeto` solo busca palabras conocidas y las traduce a una clave de `CatalogoObjetos`.
3. **Lectura por material.** Solo si el objeto es aprovechable o no se pudo nombrar. Corrobora el nombre. Si contradice con algo sanitario o peligroso, Eco se abstiene.
4. **Personas.** Solo para computadores, monitores y teclados: una pregunta de sí o no, sin identificar a nadie.
5. **Oracle.** El código de material sale de la tabla de EcoAccess y Oracle dice si la caneca lo admite. El modelo no puede crear materiales ni códigos.
6. **Apertura.** Exige todo lo anterior, corroboración, un par medido de `PoliticaAperturaAutomatica`, caneca disponible, presencia y una sesión vigente.

Una foto con personas no se conserva para corrección ni se ofrece al servicio externo. Una respuesta sobre un objeto no admitido no crea solicitud en Oracle.

## Decisión sobre la caneca piloto

El catálogo sigue completo para orientar, pero **una foto solo lleva a la caneca lo aprovechable**, y la selección manual del flujo rápido solo lista aprovechables. Esto reemplaza la decisión del 2 de noviembre de recibir cualquier residuo.

La regla está en el código (`ServicioClasificacionImagen`, `MaterialesDeAsync`). En la base real, la caneca 203 aún tiene 41 filas en `CONTENEDOR_MATERIAL`: quitar las 30 que no son aprovechables es una decisión del responsable y está en `database/03_datos_prueba/06_piloto_solo_aprovechables.sql`. Las instalaciones nuevas ya crean el piloto solo con aprovechables. Cuando exista un contenedor específico habrá que revisar esa regla.

## Evaluación

Cuatro conjuntos, en `docs/vision/resultados/objetos_*.json`, sin imágenes:

- **Ajuste, 143 fotos.** Se usó para elegir el método, las palabras y los umbrales.
- **Validación, 217 fotos.** Se ejecutó una sola vez, con el código ya registrado en `af53938`, y sus errores no se usaron para ajustar.
- **Corpus anterior, 240 fotos.** Ya visto en mediciones previas: sirve para comparar el riesgo, no como medida independiente.
- **Muestra para Qwen, 34 fotos** de validación, públicas, sin personas ni temas sanitarios.

Las fotos de ajuste y validación son de Wikimedia Commons con licencia libre, más 20 sintéticas (borrosas y vacías). Se revisaron a mano; se descartó lo que no correspondía a su etiqueta y el contenido sexual explícito o con menores. Atribución: `objetos_atribucion.json`.

### Antes y después, con las mismas 217 fotos

| Medida | Lista cerrada anterior | Recorrido nuevo |
|---|---|---|
| Respuesta de «no pude identificarlo» | 111 | 21, de ellas 13 esperadas (borrosas, vacías o varios objetos) |
| La misma respuesta en objetos ajenos al catálogo | 88 de 142 | 1 de 142 |
| Aperturas automáticas | 24 | 21 |
| Aperturas equivocadas | 8 | **0** |
| Toque ofrecido para algo no admisible | 64 | 2 |
| Mediana / P95 de inferencia | 2,16 s / 2,49 s | 1,26 s / 3,70 s |

La columna anterior se midió llamando al modelo con su instrucción de siempre; las aperturas y toques se dedujeron aplicando sus reglas, sin ejecutar el flujo completo.

### Validación del recorrido nuevo

**192 de 217 correctas (88,5 %)**, 196 con el contexto correcto. No es una precisión garantizada: son fotos públicas, no de teléfono.

| Tipo de objeto | Clave exacta | Contexto correcto | Fotos |
|---|---|---|---|
| Animal vivo | 24 | 24 | 26 |
| Planta viva | 15 | 15 | 16 |
| Persona | 8 | 8 | 8 |
| Electrónico | 41 | 44 | 45 |
| Objeto cotidiano | 43 | 44 | 47 |
| Aprovechable | 34 | 34 | 39 |
| Orgánico | 5 | 5 | 5 |
| Especial (pilas) | 4 | 4 | 4 |
| No aprovechable (empaque) | 2 | 2 | 4 |
| **Sanitario** | **3** | **3** | **9** |
| Borrosa, vacía o varios | 13 | 13 | 14 |

| Medida | Valor |
|---|---|
| Falsas admisiones (abriría algo indebido) | 0 de 21 aperturas |
| Toques indebidos | 2: un perro leído como botella y una toalla higiénica leída como papel |
| Falsos rechazos | 5 |
| Abstenciones indebidas | 8 |
| Fallos técnicos | 0 |
| Solo objeto no admitido: mediana / P95 | 1,25 s / 2,50 s |
| Residuo identificado: mediana / P95 | 3,50 s / 4,22 s |
| Peor caso | 7,63 s |

El punto débil son los **residuos sanitarios**: 3 de 9, con pañales y toallas en 0 de 5. Ninguno abrió, pero suelen recibir otra respuesta o una abstención.

Con 0 errores en 21 aperturas, el límite superior aproximado al 95 % sigue siendo cerca del 13 %: la muestra es pequeña.

### Corpus anterior

101 aperturas de 240 fotos, ninguna de una clase sanitaria, peligrosa ni orgánica. Por etiqueta salen 15 «equivocadas»: 11 son fotos con etiqueta genérica de metal que Eco llamó lata, sin revisar una a una, y 4 son confusiones entre aprovechables (vidrio por PET 2, papel por cartón 1, plástico por vidrio 1). La medición anterior daba 10 de 114 con otra regla, sin botellas automáticas, así que **no se puede afirmar que mejorara en este corpus**.

Además, 4 fotos sanitarias, sucias o electrónicas recibirían un toque: la persona vería una pregunta, no una apertura.

### Qwen 3.8

Groq respondió 22 de 34 fotos; las otras 12 quedaron sin medir por su límite de uso y no se cuentan como errores.

| Mismas 22 fotos | LFM3-VL | Qwen 3.8 |
|---|---|---|
| Correctas | 21 | 21 |
| Aperturas | 7 | 8 |
| Aperturas equivocadas | 0 | 0 |
| Mediana | 3,23 s | 1,85 s |
| P95 | 4,37 s | 22,33 s, por esperas del límite |

**No hay ganador**: la muestra es muy pequeña. LFM3-VL sigue como principal y Qwen solo actúa si el local falla o si la persona lo pide y lo autoriza. Consumo del día: 123 solicitudes, USD 0,13 estimados.

## Pruebas

Instalación desde cero en un Oracle temporal, ya retirado:

| Conjunto | Aprobadas | Fallidas | Omitidas |
|---|---|---|---|
| Unitarias | 1577 | 0 | 1 |
| Integración | 195 | 0 | 5 |
| Recorrido con modelo real y caneca simulada | Aprobado | 0 | — |

Se actualizaron pruebas existentes de `FlujoEcoDefinitivoTests` y `ServicioClasificacionImagenTests` por los textos y el orden nuevo; ninguna se eliminó. La última corrección, el reintento de descarga, se comprobó con las unitarias.

## Prueba real desde Telegram

Registro del bot, sin imágenes, el 3 de noviembre de 16:33 a 16:43 UTC, con la caneca simulada:

| Hora | Reconocido | Decisión | Oracle |
|---|---|---|---|
| 16:33:58 | `can` | Abre sin confirmar | Solicitud 968, cerrada a los 16:34:10 |
| 16:38:52 | `plastic_bottle` | Abre sin confirmar | Solicitud 969 |
| 16:40:08 | `plastic_bottle` | Abre sin confirmar | Solicitud 970 |
| 16:42:53 | `condom` | No abre | Sin solicitud |

Falta que la persona confirme lo que vio en su teléfono. Una foto de las 16:32:50 no llegó al reconocimiento y su causa no quedó registrada; ahora el registro la distingue y la descarga se reintenta. Algunas atenciones tardaron de 17 a 31 s mientras este equipo ejecutaba pruebas y otras mediciones al mismo tiempo, así que esos tiempos no representan el uso normal.

## Riesgos pendientes

- **Las cinco fotos del usuario no estuvieron disponibles** para esta evaluación: el bot no guarda imágenes. Hay que reenviarlas por Telegram.
- Fotos de teléfono: el corpus es público y un desenfoque leve no se detecta como tal.
- Un texto impreso en la foto puede influir en el nombre libre, aunque nunca se muestra y abrir exige otra lectura y las demás condiciones.
- Si una persona no aparece nombrada por el modelo y el objeto no es un computador, no se detecta su presencia.
- Una lectura sin nombre conocido nunca abre, pero puede ofrecer un toque.
- Sigue pendiente la validación física con el ESP32.
