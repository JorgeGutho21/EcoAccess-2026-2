# Incidente: bolsa de agua leída como empaque multicapa abrió la caneca NEGRA (12/11/2026)

## Qué pasó

| Dato | Valor |
|---|---|
| Objeto real | Bolsa plástica transparente de agua o hielo con impresión azul (plástico flexible) |
| Lo que dijo Eco | «Empaque multicapa» (clave `snack_wrapper`, material `OBJETO_COMPUESTO`) |
| Efecto | Se abrió la caneca NEGRA de la estación («¡Caneca abierta! Tienes 9 segundos») |
| Órdenes en Oracle | 1685 y 1686 (18:21Z y 18:26Z), eventos 4002 a 4007 de la estación |
| Corrección de la persona | «Era plástico y se abrió la caneca negra»: quedó como comentario genérico |
| Temporizador | El ESP32 confirmó la tapa abierta 10,1 s. «9 segundos» es lo que faltaba al enviar el mensaje: es correcto |

«Caneca abierta» se anuncia solo cuando el evento de apertura del ESP32 ya está guardado en Oracle; eso funcionó. El error fue de
clasificación y de política de apertura, no de hardware.

## Causas raíz

1. **Palabras genéricas de empaque se interpretaban como empaque metalizado.** En `InterpretadorNombreObjeto`, «packet»,
   «package», «pouch» y «sachet» llevaban a `snack_wrapper`. Una bolsa de agua («water packet», «plastic package») terminaba en
   multicapa. El material no se miraba: se asumía por el nombre.
2. **La verde y la negra se abrían con un solo nombre.** `PoliticaAperturaAutomatica.PermiteEnEstacion` no exigía la segunda
   lectura independiente que sí exige la blanca. Bastaba con que el modelo nombrara el objeto con certeza alta.
3. **Las correcciones no quedaban como incidencias.** «Era plástico», «se abrió la caneca equivocada» o «no es multicapa» se
   guardaban como comentario general, tanto en la ruta de corrección como cuando el bot esperaba un comentario.
4. **Las evaluaciones solo medían la blanca.** Las «falsas admisiones» de los informes anteriores contaban únicamente aperturas
   indebidas de la BLANCA. Una apertura equivocada de la verde o la negra no aparecía en ninguna métrica.

## Cambios (commits `c0f8f0c` y `460e01f`)

| Capa | Cambio |
|---|---|
| Lógica — interpretación | «bag, film, wrapper, packet, package, pouch, sachet» = bolsa plástica. Empaque metalizado solo con señales explícitas: chips, snack, dulce, chocolate, «foil», «shiny», «metallic». «Water bag», «ice bag», «water sachet» = bolsa de agua |
| Lógica — catálogo | `CatalogoObjetos.CorroboraEnEstacion`: compara el material de la segunda lectura con el que el objeto lleva a la verde o la negra |
| Lógica — política | `PermiteEnEstacion` exige `Corroborada`. Sin una segunda lectura que coincida, Eco **solo orienta** y no abre nada |
| Bot — visión | La segunda lectura por material también se pide para objetos de verde y negra (`VocabularioVision.NecesitaLectura`) |
| Lógica — Telegram | Frases de corrección («es plástico», «no es multicapa», «caneca equivocada», «se equivocó»…) quedan como **incidencia** (`ProblemaTecnico`, categoría Caneca si menciona caneca o tapa, si no Reconocimiento), con la operación y la fecha. Respuesta: «Registré una incidencia…» |
| Evaluación | `PoliticaAperturaAutomatica.CanecaEstacionPrevista` aplica la misma regla sin Oracle. `--evaluar-objetos` guarda `CanecaEstacion` y `Analizar-ResultadosBanco.ps1` informa aperturas automáticas, aperturas equivocadas y precisión de las decisiones automáticas en las **tres** canecas |

No hay reglas absolutas: una bolsa plástica no va siempre a la negra. Lo que no se puede corroborar no abre ninguna caneca.

**Ninguna corrección de un usuario entrena el modelo.** Las incidencias quedan para revisión del equipo. Tampoco se reentrenaron
pesos: el modelo sigue siendo `lfm3-vl` local. Los cambios son de interpretación y de política.

## Medición antes y después

«Antes» aplica la regla anterior (sin corroboración en verde y negra) a los resultados ya guardados de la misma versión del
modelo (`-EstimarEstacionSinCorroborar`). «Después» es una ejecución nueva con la corrección (Release de `460e01f`).

| Conjunto | Fotos | Uso | Antes: aperturas / equivocadas | Antes: precisión | Después: aperturas / equivocadas | Después: precisión |
|---|---:|---|---|---:|---|---:|
| Prueba de la noche | 42 | Nunca usado para ajustar | 13 / 0 | 100 % | 10 / 0 | 100 % |
| Validación | 33 | Nunca usado para ajustar | 9 / 0 | 100 % | 6 / 0 | 100 % |
| Banco Colombia | 275 | Nunca usado para ajustar | 63 / 1 (blanca) | 98,4 % | 45 / 1 (blanca) | 97,8 % |
| **Independientes (350)** | 350 | | **85 / 1** | **98,8 %** | **61 / 1** | **98,4 %** |
| Entrenamiento | 196 | Usado para ajustar | 76 / **4 verde o negra** | 94,7 % | 48 / 0 | 100 % |

Las 4 aperturas equivocadas de entrenamiento con la regla anterior, y qué pasa ahora:

| Foto | Antes | Ahora |
|---|---|---|
| `plastico2-16` (bolsa plástica) | `snack_wrapper` → abriría la NEGRA (el mismo error que las órdenes 1685 y 1686) | `plastic_bag`, material `BOLSA_PLASTICA`, ruta correcta, no abre |
| `metal-13` | `face_mask` → NEGRA | Mismo nombre equivocado, pero sin corroboración: no abre |
| `papel-01` | `whole_produce` → VERDE | No abre |
| `especial2-11` | `served_food` → VERDE | No abre |

La única apertura equivocada que queda en las 350 fotos independientes es `carton-05`: cubetas de cartón con huevos a la vista.
Eco dijo cartón y abriría la BLANCA (ver `EVALUACION_BANCO_COLOMBIA.md`).

### Qué dice y qué no dice esta medición

- **Ruta correcta** (sin cambios relevantes): validación 75,8 % antes y después; Colombia 83,6 % → 83,3 % (229 frente a 230, el
  modelo no es determinista); entrenamiento 80,1 % → 80,6 %.
- **Precisión de las decisiones automáticas en fotos independientes: 98,4 % (60 de 61).** El objetivo inicial del 95 % se
  cumple en el valor medido, pero con 61 aperturas el intervalo de confianza del 95 % (Wilson) va de **91,3 % a 99,7 %**: no
  está demostrado con margen que la precisión real sea ≥ 95 %. Hacen falta más fotos independientes.
- **Costo:** se abren solas menos fotos (independientes 85 → 61). Las demás reciben orientación sin apertura.
- Las imágenes son de Wikimedia Commons (licencias libres, autor y licencia en `fuentes.json` de cada banco), no fotos tomadas
  en la estación. **No sustituyen la prueba física.**
- **Total evaluado: 546 fotos de un objeto y 11 con varios.** La meta de 1 500 imágenes (500 por caneca) **no está cumplida.**

Resultados completos (JSON por foto y análisis con matriz, precisión, exhaustividad y F1 por ruta):
`docs/vision/resultados/tres_canecas/`.

## Pruebas automáticas

- `EstacionDisposicionTests.Disposicion_SinSegundaLecturaQueCorroboreNoAbreNingunaCaneca` (multicapa sin lectura, multicapa
  leído como bolsa, papel higiénico leído como papel, comida sin lectura): ninguna orden.
- `EstacionDisposicionTests.LaBolsaDeAguaNoEsUnEmpaqueMetalizado`: «Clear plastic water bag», «Ice bag», «Plastic packet»,
  «Transparent plastic package» y «Water sachet» no son `snack_wrapper`. «Chip packet» y «Shiny candy wrapper» sí.
- `IncidenciasTelegramTests`: las tres frases del caso real y la corrección escrita cuando se esperaba un comentario quedan como
  incidencia. Un comentario normal sigue siendo comentario.
- Suite unitaria: **2 096 correctas, 0 con error, 1 omitida a propósito.**

## Lo que falta (requiere al dueño presente)

- Repetir con la bolsa de agua real en la estación: no debe abrir ninguna tapa, y «es plástico» debe quedar como incidencia en
  `COMENTARIO_CIUDADANO`.
- Pruebas físicas de las tres canecas:
  - apertura y cierre confirmados por el ESP32;
  - persistencia en Oracle;
  - caída de red y de hardware;
  - concurrencia;
  - mensajes y calificación en Telegram;
  - objetos no admitidos.

  No se movieron servomotores sin supervisión.
- Ampliar el banco independiente hacia las 1 500 imágenes para estrechar el intervalo de confianza.

## Procedimiento para actualizar el modelo o las reglas

1. Separar fotos nuevas en entrenamiento, validación y prueba **antes** de ejecutar el modelo. Nada de prueba se usa para ajustar.
2. Ajustar solo con entrenamiento. Comprobar en validación.
3. Ejecutar `EcoAccess.P3.Bot.exe --evaluar-objetos <manifiesto> local` con `ECOACCESS_SIN_HARDWARE=1`.
4. Ejecutar `scripts/pruebas/vision/Analizar-ResultadosBanco.ps1` y revisar «Aperturas equivocadas» de las tres canecas.
5. Aceptar el cambio solo si no aumentan las aperturas equivocadas en prueba ni en validación, y si la suite unitaria pasa.
6. Las incidencias de Telegram se revisan a mano. Una foto entra al banco solo con etiqueta verificada por una persona, nunca por
   la afirmación del usuario.
