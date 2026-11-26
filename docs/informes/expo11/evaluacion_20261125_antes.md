# Evaluación Modo ExpoSoftware (catálogo de 11) — lfm3-vl:latest, lado 512px, num_predict 220

Fecha: 2026-11-25 16:49 · Manifiesto: `datasets/ecoaccess11/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1515 ms. «Aceptación» = fotos externas NO usadas en ajuste (evaluación independiente).

Correcto: catálogo y compuestas → apertura automática con exactamente las canecas esperadas; negativas → ninguna apertura automática; complementarios (fuera del catálogo) → sin apertura automática o, si abre, solo su caneca de la Resolución 2184. Falsa apertura: abre SIN preguntar una caneca que no corresponde.

| Conjunto | Casos | Correctos | Apertura automática correcta | Caneca correcta (con o sin confirmación) | Pide confirmación | Sin apertura automática | Falsas aperturas | Canecas omitidas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| complementaria (externa) | 38 | 37 (97 %) | 0 | 29 | 4 | 33 | **1** | 0 |
| evaluacion_externa (externa) | 11 | 8 (73 %) | 8 | 10 | 3 | 3 | **0** | 0 |
| negativa (real) | 8 | 8 (100 %) | 0 | 8 | 0 | 8 | **0** | 0 |
| negativa_externa (externa) | 9 | 8 (89 %) | 0 | 6 | 2 | 8 | **1** | 0 |
| complementaria (externa, ajuste) | 15 | 15 (100 %) | 0 | 10 | 4 | 14 | **0** | 0 |
| compuesta (compuesta, ajuste) | 10 | 6 (60 %) | 6 | 6 | 3 | 3 | **1** | 4 |
| evaluacion_externa (externa, ajuste) | 4 | 4 (100 %) | 4 | 4 | 0 | 0 | **0** | 0 |
| negativa (real, ajuste) | 1 | 0 (0 %) | 0 | 0 | 0 | 0 | **1** | 0 |
| negativa_externa (externa, ajuste) | 4 | 2 (50 %) | 0 | 2 | 0 | 2 | **2** | 0 |
| real_ajuste (real, ajuste) | 25 | 25 (100 %) | 25 | 25 | 0 | 0 | **0** | 0 |
| **Aceptación independiente (externas sin ajuste)** | 58 | 53 (91 %) | 8 | 45 | 9 | 44 | **2** | 0 |
| Independiente total (sin ajuste) | 66 | 61 (92 %) | 8 | 53 | 9 | 52 | **2** | 0 |
| Usadas en ajuste | 59 | 52 (88 %) | 35 | 47 | 7 | 19 | **4** | 4 |
| Fotos reales propias (todas) | 34 | 33 (97 %) | 25 | 33 | 0 | 8 | **1** | 0 |

## Materiales complementarios (generalización, fuera del catálogo operativo)

| Material | Caneca (Res. 2184) | Casos | Rechazo correcto (sin apertura automática) | Abre solo su caneca | Apertura incorrecta |
|---|---|---:|---:|---:|---:|
| Botella plástica limpia | BLANCA | 4 | 3 | 0 | 1 |
| Caja de cartón limpia | BLANCA | 4 | 4 | 0 | 0 |
| Envase de champú limpio | BLANCA | 4 | 4 | 0 | 0 |
| Frasco de vidrio limpio | BLANCA | 3 | 3 | 0 | 0 |
| Lata de aluminio | BLANCA | 4 | 4 | 0 | 0 |
| Envoltura metalizada no aprovechable | NEGRA | 4 | 4 | 0 | 0 |
| Esponja deteriorada | NEGRA | 4 | 4 | 0 | 0 |
| Papel higiénico usado | NEGRA | 2 | 1 | 1 | 0 |
| Tapabocas | NEGRA | 4 | 4 | 0 | 0 |
| Toallita húmeda usada | NEGRA | 2 | 1 | 1 | 0 |
| Cáscara de aguacate | VERDE | 4 | 3 | 1 | 0 |
| Cáscara de huevo | VERDE | 4 | 4 | 0 | 0 |
| Cáscara de naranja | VERDE | 4 | 2 | 2 | 0 |
| Cáscara de papaya | VERDE | 3 | 3 | 0 | 0 |
| Restos de zanahoria | VERDE | 3 | 3 | 0 | 0 |
| **Total (aceptación)** | | 38 | 33 (87 %) | 4 | **1** |
| **Total (ajuste)** | | 15 | 14 (93 %) | 1 | **0** |

## Exactitud por material del catálogo (fotos individuales)

| Material | Casos (ajuste + independientes) | Material correcto | Apertura automática correcta |
|---|---:|---:|---:|
| M01 Papa o media papa | 5 + 0 | 5 | 5 |
| M02 Limón entero o partido | 2 + 0 | 2 | 2 |
| M03 Manzana verde entera o partida | 2 + 0 | 2 | 2 |
| M04 Lulo entero o partido | 2 + 0 | 2 | 2 |
| M05 Cáscara de banano | 4 + 11 | 13 | 12 |
| M06 Papel de aluminio limpio | 2 + 0 | 2 | 2 |
| M07 Envase de vidrio rosado de sérum, vacío y limpio | 3 + 0 | 3 | 3 |
| M08 Papel de cuaderno cuadriculado o rayado, arrugado | 2 + 0 | 2 | 2 |
| M09 Bolsa de papel kraft o cartón marrón arrugado | 2 + 0 | 2 | 2 |
| M10 Envase de pegamento instantáneo vacío (Gota Mágica, Astro Glue) | 3 + 0 | 3 | 3 |
| M11 Servilleta usada | 2 + 0 | 2 | 2 |

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 22 ms | 21 ms | 35 ms | 101 ms |
| Primera inferencia (pregunta principal) | 2473 ms | 2317 ms | 3377 ms | 4500 ms |
| Inferencia total (principal + verificación) | 3070 ms | 2416 ms | 5896 ms | 7620 ms |
| Reconocimiento completo | 3093 ms | 2433 ms | 5910 ms | 7644 ms |

Casos con pregunta de verificación: 33/125 (26 %); errores del modelo: 0.

## Falsas aperturas

- `negativos/negativo_07.jpg` (negativa, real, usada en ajuste): esperado ninguna, obtenido NEGRA · Reconocido (automática) · M11=crumpled_tissue / notebook_paper → used_napkin · **FALSA APERTURA**
- `validacion/escena_06_M07_M08.jpg` (compuesta, compuesta, usada en ajuste): esperado BLANCA, obtenido BLANCA+NEGRA · Reconocido (automática) · M07=dropper bottle / serum_bottle, M11=crumpled paper / notebook_paper → used_napkin · **FALSA APERTURA**
- `externas/botella_plastica/botella_plastica_02.jpg` (complementaria, externa): esperado sin apertura o BLANCA, obtenido NEGRA · Reconocido (automática) · M10=dropper bottle / serum_bottle → glue_bottle · **FALSA APERTURA**
- `externas/negativos/bolsa_plastica/bolsa_plastica_01.jpg` (negativa_externa, externa, usada en ajuste): esperado ninguna, obtenido VERDE · Reconocido (automática) · M01=potato · **FALSA APERTURA**
- `externas/negativos/papel_blanco/papel_blanco_01.jpg` (negativa_externa, externa, usada en ajuste): esperado ninguna, obtenido BLANCA+NEGRA · Reconocido (automática) · M09=paper / kraft_paper, M11=paper / notebook_paper → used_napkin · **FALSA APERTURA**
- `externas/negativos/papel_blanco/papel_blanco_03.jpg` (negativa_externa, externa): esperado ninguna, obtenido NEGRA · Reconocido (automática) · M11=crumpled paper / notebook_paper → used_napkin · **FALSA APERTURA**

## Otros casos no correctos (sin falsa apertura)

- `validacion/escena_05_M04_M09.jpg` (compuesta, compuesta, usada en ajuste): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · M04=orange / lulo · no reconocido: crumpled paper / aluminum_foil
- `validacion/escena_08_M09_M11.jpg` (compuesta, compuesta, usada en ajuste): esperado BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · M01=potato, M06=aluminum_foil · no reconocido: dropper_bottle
- `validacion/escena_09_M02_M07_M10.jpg` (compuesta, compuesta, usada en ajuste): esperado VERDE+BLANCA+NEGRA, obtenido VERDE · ParcialConDesconocidos · M02=lime · no reconocido: dropper_bottle / serum_bottle, sponge / aluminum_foil
- `externas/cascara_banano/banano_12.jpg` (evaluacion_externa, externa): esperado VERDE, obtenido VERDE · ParcialConDesconocidos · M05=banana peel / banana_peel · no reconocido: plastic bag, leafy greens / green_apple
- `externas/cascara_banano/banano_14.jpg` (evaluacion_externa, externa): esperado VERDE, obtenido VERDE+NEGRA · ParcialConDesconocidos · M05=banana peel / banana_peel, M10=dropper bottle / serum_bottle → glue_bottle, M11=crumpled paper / notebook_paper → used_napkin · no reconocido: plastic bag
- `externas/cascara_banano/banano_15.jpg` (evaluacion_externa, externa): esperado VERDE, obtenido VERDE · ParcialConDesconocidos · M05=banana peel / banana_peel · no reconocido: fly / other
