# Evaluación consolidada de visión — Modo ExpoSoftware, catálogo de 11 (2026-11-25)

Modelo `lfm3-vl:latest` en Ollama local (modelo residente, precalentado), 512 px, `num_ctx` 2048, `num_predict` 220,
sin hardware (no se movió ninguna caneca ni se envió nada al ESP32). Ejecución:
`src\EcoAccess.P3.Bot\bin\Debug\net48\EcoAccess.P3.Bot.exe --evaluar-expo9 datasets/ecoaccess11/manifiesto.json --conjuntos real_ajuste,negativa,compuesta,evaluacion_externa,complementaria,negativa_externa --salida <informe.md>`
(compilación Debug; el bot de producción en `bin\Release` no se tocó). Dataset: las 125 imágenes activas de
`datasets/ecoaccess11/manifiesto.json`. Anexos de esta carpeta:

| Archivo | Contenido |
|---|---|
| `evaluacion_20261125_antes.md` / `.jsonl` | Línea base (código de `HEAD` d209465, compilado aparte en un worktree temporal ya eliminado) |
| `evaluacion_20261125_despues.md` / `.jsonl` | Versión final con las reglas nuevas |
| `evaluacion_20261125_prompt_descartado.jsonl` | Variante de prompt probada y descartada (§4) |

Los `.jsonl` guardan, por imagen, la respuesta cruda del modelo, el estado, las canecas, el motivo de «por confirmar» y los tiempos.

## 1. Criterios de puntuación (más estrictos que los del informe anterior)

- **Apertura incorrecta**: se abriría SIN preguntar una caneca que no corresponde. Para negativas y para los 15 materiales
  complementarios (fuera del catálogo) **cualquier** apertura automática cuenta como incorrecta, aunque fuera a la caneca
  de la Resolución 2184 (el informe que genera el ejecutable todavía las cuenta aparte como «abre solo su caneca»).
- **Apertura incompleta**: abre solo, sin canecas de más, pero falta alguna esperada (un residuo omitido).
- **Acierto**: catálogo y escenas → apertura automática con exactamente las canecas esperadas; negativas y
  complementarios → ninguna apertura automática (rechazo o «por confirmar»).
- **Por confirmar**: no abre solo pero propone canecas; el ciudadano confirma o elige (Telegram muestra «Confirma antes de
  abrir» con los botones «Abrir …», «Es servilleta (+ negra)» y «Elegir canecas»). **Rechazo**: no propone nada.
- Latencia: tiempo total del reconocimiento por foto (preprocesamiento + inferencia principal + preguntas de verificación).

## 2. Ajuste frente a evaluación independiente

| Bloque | Conjuntos | Imágenes | Naturaleza |
|---|---|---:|---|
| Ajuste | 25 reales (`real_ajuste`), 9 negativas reales, 10 escenas compuestas | 44 | Con estas fotos se escribieron el prompt y las verificaciones previas. **No** miden generalización. |
| Evaluación independiente | 15 cáscaras de banano, 53 complementarios, 13 negativas externas (Wikimedia Commons, licencia libre) | 81 | Lo más independiente que existe en el repositorio; 23 de las 81 ya estaban marcadas `usada_en_ajuste` en el manifiesto (se reporta también el subtotal de las 58 restantes). |

**Advertencia importante**: las reglas nuevas de este informe se diseñaron mirando los errores de **las 125** imágenes,
incluidas las externas (las falsas aperturas de `cascara_aguacate_02`, `bolsa_plastica_01`, `botella_plastica_02`,
`papel_higienico_usado_02`, `toallita_humeda_02`, `papel_blanco_01/03` y `cascara_naranja_01/04` motivaron reglas
concretas). Después de esta sesión **ninguna** de las 125 es ya una evaluación ciega; el «0» de la tabla siguiente es
un resultado sobre fotos vistas, no una garantía sobre fotos nuevas. Hace falta un conjunto nuevo (§7).

## 3. Resultados antes / después (evaluación real, 125 imágenes)

### Antes (línea base, código sin cambios)

| Conjunto | Casos | Aciertos | Aperturas incorrectas | Aperturas incompletas | Por confirmar | Rechazo | Media s | Mediana s | P95 s |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Reales de ajuste | 25 | 25 | **0** | 0 | 0 | 0 | 3.28 | 3.00 | 5.88 |
| Negativas reales | 9 | 8 | **1** | 0 | 0 | 8 | 2.60 | 2.17 | 5.71 |
| Escenas compuestas | 10 | 6 | **1** | 0 | 3 | 0 | 3.17 | 2.73 | 5.62 |
| Cáscara de banano externa | 15 | 12 | **0** | 0 | 3 | 0 | 2.65 | 2.17 | 7.64 |
| Complementarios externos (fuera de catálogo) | 53 | 47 | **6** | 0 | 9 | 38 | 3.14 | 2.63 | 6.40 |
| Negativas externas | 13 | 10 | **3** | 0 | 2 | 8 | 3.36 | 2.60 | 6.27 |
| **Ajuste (44)** | 44 | 39 | **2** | 0 | 3 | 8 | 3.11 | 2.44 | 5.71 |
| **Evaluación independiente (81 externas)** | 81 | 69 | **9** | 0 | 14 | 46 | 3.08 | 2.43 | 6.27 |
| Externas no marcadas de ajuste (58) | 58 | 49 | **6** | 0 | 10 | 34 | 3.01 | 2.40 | 6.27 |
| **Total** | 125 | 108 | **11** | 0 | 17 | 54 | 3.09 | 2.43 | 5.91 |

Las 11 aperturas incorrectas de la línea base: `negativo_07` (bolsa plástica → servilleta → negra), `escena_06`
(+ negra), `cascara_naranja_01` y `_04` (naranja → lulo/papa → verde), `cascara_aguacate_02` (aguacate → papa → verde),
`botella_plastica_02` (→ pegamento → negra), `papel_higienico_usado_02` y `toallita_humeda_02` (→ servilleta → negra),
`bolsa_plastica_01` (bolsa → papa → verde), `papel_blanco_01` (→ kraft + servilleta → blanca + negra) y `papel_blanco_03`
(→ servilleta → negra). Se repitió la línea base dos veces: la primera corrida dio las **mismas 11** pero fue más lenta
(mediana 2,93 s, P95 7,41 s, con un caso de 89 s por contención de la GPU), por eso la tabla usa la segunda.

### Después (versión final)

| Conjunto | Casos | Aciertos | Aperturas incorrectas | Aperturas incompletas | Por confirmar | Rechazo | Media s | Mediana s | P95 s |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Reales de ajuste | 25 | 20 | **0** | 0 | 5 | 0 | 3.51 | 3.22 | 6.32 |
| Negativas reales | 9 | 9 | **0** | 0 | 1 | 8 | 2.88 | 2.39 | 6.38 |
| Escenas compuestas | 10 | 4 | **0** | 0 | 6 | 0 | 4.24 | 3.70 | 6.56 |
| Cáscara de banano externa | 15 | 11 | **0** | 0 | 4 | 0 | 3.11 | 2.41 | 9.10 |
| Complementarios externos (fuera de catálogo) | 53 | 53 | **0** | 0 | 17 | 36 | 3.34 | 2.76 | 6.86 |
| Negativas externas | 13 | 13 | **0** | 0 | 5 | 8 | 3.50 | 2.76 | 6.36 |
| **Ajuste (44)** | 44 | 33 | **0** | 0 | 12 | 8 | 3.54 | 3.22 | 6.32 |
| **Evaluación independiente (81 externas)** | 81 | 77 | **0** | 0 | 26 | 44 | 3.32 | 2.75 | 6.47 |
| Externas no marcadas de ajuste (58) | 58 | 54 | **0** | 0 | 19 | 32 | 3.23 | 2.75 | 6.36 |
| **Total** | 125 | 110 | **0** | 0 | 38 | 52 | 3.40 | 2.78 | 6.38 |

Resumen: aperturas incorrectas **11 → 0** (ajuste 2 → 0; independiente 9 → 0); aciertos 108 → 110; «por confirmar»
17 → 38. El precio es que cinco fotos reales que antes abrían solas ahora piden confirmación (2 lulos, 2 servilletas y
`envase_pegamento_02`) y en escenas los aciertos bajan de 6 a 4 (`escena_04` y `escena_10` llevan servilleta o lulo).
Ninguna de esas pasa a abrir una caneca equivocada: quedan en «por confirmar» con la caneca correcta sugerida.

### Complementarios (15 materiales fuera de catálogo, 53 fotos)

| Material | Caneca Res. 2184 | Fotos | Abre solo antes → después | Por confirmar después | Rechazo después |
|---|---|---:|---:|---:|---:|
| Cáscara de naranja | verde | 4 | 2 → 0 | 4 | 0 |
| Cáscara de papaya | verde | 3 | 0 → 0 | 1 | 2 |
| Cáscara de aguacate | verde | 4 | 1 → 0 | 1 | 3 |
| Restos de zanahoria | verde | 3 | 0 → 0 | 0 | 3 |
| Cáscara de huevo | verde | 4 | 0 → 0 | 0 | 4 |
| Botella plástica | blanca | 4 | 1 → 0 | 2 | 2 |
| Lata de aluminio | blanca | 4 | 0 → 0 | 1 | 3 |
| Caja de cartón | blanca | 4 | 0 → 0 | 0 | 4 |
| Frasco de vidrio | blanca | 3 | 0 → 0 | 1 | 2 |
| Envase de champú | blanca | 4 | 0 → 0 | 2 | 2 |
| Papel higiénico usado | negra | 2 | 1 → 0 | 2 | 0 |
| Envoltura metalizada | negra | 4 | 0 → 0 | 0 | 4 |
| Toallita húmeda | negra | 2 | 1 → 0 | 2 | 0 |
| Esponja deteriorada | negra | 4 | 0 → 0 | 0 | 4 |
| Tapabocas | negra | 4 | 0 → 0 | 1 | 3 |
| **Total** | | 53 | **6 → 0** | 17 | 36 |

**Decisión: no se amplía el catálogo operativo.** Ningún complementario se reconoce de forma consistente en su caneca:
las 6 aperturas de la línea base eran parecidos forzados (naranja → lulo/papa, aguacate → papa, botella plástica →
pegamento, papel higiénico y toallita → servilleta), y en el mejor caso (naranja) el modelo no lo separa del lulo.
Caja de cartón, lata, huevo, zanahoria, envoltura y esponja se rechazan casi siempre, lo cual es correcto pero no es
reconocimiento. Con 2–4 fotos por material tampoco habría evidencia suficiente para incorporarlos.

## 4. Alternativas probadas (cada una con la evaluación real de las 125)

| Versión | Cambio | Aperturas incorrectas (total) | Aciertos | Mediana / P95 | Decisión |
|---|---|---:|---:|---|---|
| Línea base | — | 11 (repetida: 11) | 108 | 2,43 s / 5,91 s | — |
| v1 | Reglas A–F de §5 salvo «servilleta siempre por confirmar» | 0 | 112 | 2,39 s / 5,65 s | Base de la final |
| v2 | v1 + prompt con más ejemplos (`napkin, tissue, plain white paper, orange, avocado, egg shell, plastic bottle…` y la lista de `other` ampliada) | **6** (+1 incompleta) | 102 | 2,55 s / 6,42 s | **Descartada** |
| v3 (final) | v1 + servilleta usada nunca abre sola | 0 | 110 | 2,78 s / 6,38 s | **Conservada** |

Lo que enseñó v2: con el prompt ampliado el modelo escribe `name: "tissue", label: "used_napkin"` para la bolsa plástica
de `negativo_07`, el papel higiénico, la toallita húmeda y el papel blanco liso. Es decir, **el nombre libre «tissue/napkin»
no distingue una servilleta real de sus confusores**, y la regla «exigir napkin/tissue en el nombre» (que sí se conserva)
no basta por sí sola. Por eso v3 deja la servilleta siempre en «por confirmar». Con el prompt original v1 ya daba 0, pero
dependía de que el modelo no etiquetara directamente `used_napkin`, algo frágil frente a cualquier cambio de prompt o modelo.

Pruebas de preguntas sí/no hechas fuera del flujo (script con la misma reducción de imagen y el mismo cuerpo de petición):
- «Is there a potato or half potato in the photo?»: «sí» en las 5 papas reales, `escena_01` y `escena_04`; «no» en
  `cascara_aguacate_02`, `bolsa_plastica_01`, `escena_08`, `negativo_08` (tomate) y `negativo_09` (cebolla). Las variantes
  «Is there a real potato (brown or yellow skin…)?» e «Is one of the objects a potato?» respondían «no» a `papa_01` y
  `escena_01`; se descartaron. Coste medido: 0,6–1,2 s por foto con papa.
- Contraste lulo/naranja («Is it a lulo (naranjilla)…?» y «Is the fruit a citrus orange, mandarin or orange peel?»): **no
  discrimina** (la segunda dice «sí» a los dos lulos reales y a las cuatro naranjas; la primera dice «no» a casi todo).
  Se descartó y el lulo/naranja se resuelve con confirmación (§5 regla B).
- No se repitió la pregunta «¿es papel suave y no plástico?» (ya descartada en la sesión anterior).

## 5. Reglas conservadas (código)

`src/EcoAccess.P3.Logica/Vision35/Expo/CatalogoExpo9.cs` y `ReconocedorExpo9.cs`. Nuevo campo
`MaterialDetectadoExpo.Duda`: el material se muestra con su caneca sugerida y la marca «por confirmar», el estado pasa a
`ParcialConDesconocidos` y `PuedeAbrirAutomaticamente` es falso si **cualquier** material de la foto tiene duda.

- **A. Descarte no es evidencia.** En las verificaciones de pares, «sí» es evidencia directa; «no» solo descarta. Si un «no»
  cambia el material (cuaderno → servilleta porque «no tiene cuadrícula», sérum → pegamento porque «no es vidrio rosado»),
  el nuevo material queda por confirmar. Era el camino de `negativo_07`, `escena_06`, `papel_blanco_*`, papel higiénico,
  toallita y botella plástica. Si la pregunta no responde, también queda por confirmar (antes se quitaba el material).
- **B. Cítricos.** Nombre libre con `orange, mandarin, tangerine, clementine, satsuma, citrus peel, naranja, mandarina` en
  un material verde → por confirmar, salvo que el nombre diga `lulo` o `naranjilla`. Las mandarinas no son compatibles con
  la etiqueta `lulo` y se rechazan.
- **C. Servilleta usada (M11) nunca abre sola**; y su etiqueta solo se acepta si el nombre libre dice `napkin, tissue,
  paper towel, kleenex, serviette, servilleta`. Cualquier etiqueta de papel (cuaderno o servilleta) con `plastic, bag,
  wrapper, wrap, film, wipe, nylon, polythene` en el nombre se rechaza.
- **D. Confirmación de papa.** Si queda una papa, una pregunta sí/no «Is there a potato or half potato in the photo?»;
  si no es «sí», por confirmar (aguacate y bolsa plástica nombrados «potato»).
- **E. Multirresiduo.** Si en una foto hay dos o más objetos del mismo grupo confundible (papeles; frascos) la pregunta
  sí/no no sabría a cuál se refiere y no se hace; si esos objetos van a canecas distintas, todos quedan por confirmar.
- **F. Nombres con guion bajo** (`crumpled_tissue`, `plastic_bag`, `dropper_bottle`) se separan antes de aplicar las reglas.
- El prompt principal queda **igual** que en la línea base (la variante v2 se revirtió).

El evaluador (`src/EcoAccess.P3.Bot/Vision/ComandoExpo9.cs`) añade al detalle `.jsonl` el campo `por_confirmar` con el
motivo de cada duda.

## 6. Latencia

| Corrida | Media | Mediana | P95 | Máximo | Preguntas al modelo (125 fotos) | Fotos con verificación |
|---|---:|---:|---:|---:|---:|---:|
| Línea base, 1.ª corrida | 4,23 s | 2,93 s | 7,41 s | 89,4 s | 172 | 30 |
| Línea base, 2.ª corrida | 3,09 s | 2,43 s | 5,91 s | 7,6 s | 175 | 33 |
| v1 | 2,93 s | 2,39 s | 5,65 s | 7,7 s | 190 | 44 |
| v3 (final) | 3,40 s | 2,78 s | 6,38 s | 9,5 s | 192 | 46 |

- La variación entre corridas del **mismo** código es grande: v1 y v3 solo difieren en una regla sin coste (servilleta
  siempre por confirmar) y aun así la mediana pasó de 2,39 a 2,78 s; en las 110 fotos con el mismo número de preguntas,
  la mediana fue 2,59 s en la línea base y 2,74 s en v3. Diferencias de ±0,4 s en la mediana son ruido del equipo/GPU.
- Coste real de las reglas: la pregunta de la papa (+0,6–1,2 s) en las fotos con papa; las reglas A, B, C, E y F no
  añaden llamadas al modelo. En total 17 preguntas más en 125 fotos (175 → 192).
- **La meta de 1–2 s no se cumple**: mediana ≈ 2,4–2,8 s y P95 ≈ 5,7–6,4 s. Los casos lentos (5–7 s) siguen siendo los de
  papel blanco/cuaderno (dos preguntas de verificación, una a 768 px).

## 7. Limitaciones y lo que sigue sin resolver

1. **No hay evaluación ciega tras esta sesión.** El 0 de aperturas incorrectas se midió sobre las mismas 125 fotos que
   guiaron las reglas. No se afirma 100 % ni generalización a objetos o fotos no probados.
2. **«Por confirmar» con caneca sugerida equivocada.** En la versión final, 16 de los 38 casos por confirmar proponen una
   caneca que no corresponde (antes 8 de 17): p. ej. botella plástica, lata, frasco o champú → «pegamento» → negra;
   bolsa plástica → «papa» → verde; papel liso → «servilleta» → negra. Si el ciudadano pulsa «Abrir …» sin mirar, se
   abriría la caneca equivocada. El riesgo pasó del sistema a la confirmación humana; no desapareció.
3. **Servilleta usada y lulo ya no abren solos nunca** con fotos como las actuales (0/2 servilletas reales y 0/2 lulos
   reales automáticos; también `escena_04` y `escena_10`). Es una decisión de seguridad, no un acierto del modelo.
4. **Omisiones y confusiones en escenas.** Las escenas con varios residuos siguen fallando objetos (`escena_05`: el kraft
   sale como «crumpled paper / aluminum_foil», no reconocido; `escena_08`: kraft y servilleta salen como papa, aluminio y
   frasco; `escena_09`: el pegamento sale como «sponge»). En esta corrida no abrieron solas por otras
   dudas, pero en la variante v2 `escena_09` abrió verde + blanca sin la negra (apertura incompleta). Una omisión no abre
   una caneca equivocada, pero deja un residuo sin caneca.
5. **El modelo no es determinista entre corridas** (p. ej. `cascara_papaya_01` y `banano_11` cambiaron de respuesta con
   el mismo prompt y temperatura 0), así que cualquier cifra individual puede variar ±1–2 casos.
6. **Latencia fuera de la meta** (§6).
7. Faltan fotos reales nuevas para una evaluación independiente: cáscara de banano real (0), al menos 5 por material de
   otros objetos/días, bolsas plásticas blancas, papel liso, papel higiénico, toallitas, naranjas/mandarinas junto a
   lulos reales, y escenas reales (no compuestas) con kraft + servilleta y sérum + cuaderno.

## 8. Pruebas unitarias

`tests/unitarias/EcoAccess.P3.Tests/Logica/Vision35/PruebasModoExpo9Tests.cs`: pruebas nuevas para las reglas A–F
(servilleta con nombre de papel suave y rechazo de plástico, cítricos por confirmar, mandarinas no compatibles,
lulo/naranjilla sí abre, servilleta nunca abre sola, verificación por descarte, «sí» como evidencia y verificación sin
respuesta, multirresiduo de canecas distintas, un material dudoso bloquea toda la apertura, pregunta de la papa).
`dotnet test tests/unitarias/EcoAccess.P3.Tests/EcoAccess.P3.Tests.csproj -c Debug`: **2589 correctas, 0 con error,
1 omitida (preexistente: `EnlaceSimbolicoSeRechazaAntesDeClasificar`), 2590 en total**.
