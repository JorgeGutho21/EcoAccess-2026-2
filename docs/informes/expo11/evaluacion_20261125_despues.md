# Evaluación Modo ExpoSoftware (catálogo de 11) — lfm3-vl:latest, lado 512px, num_predict 220

Fecha: 2026-11-25 17:05 · Manifiesto: `datasets/ecoaccess11/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1654 ms. «Aceptación» = fotos externas NO usadas en ajuste (evaluación independiente).

Correcto: catálogo y compuestas → apertura automática con exactamente las canecas esperadas; negativas → ninguna apertura automática; complementarios (fuera del catálogo) → sin apertura automática o, si abre, solo su caneca de la Resolución 2184. Falsa apertura: abre SIN preguntar una caneca que no corresponde.

| Conjunto | Casos | Correctos | Apertura automática correcta | Caneca correcta (con o sin confirmación) | Pide confirmación | Sin apertura automática | Falsas aperturas | Canecas omitidas |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| complementaria (externa) | 38 | 38 (100 %) | 0 | 27 | 11 | 38 | **0** | 0 |
| evaluacion_externa (externa) | 11 | 7 (64 %) | 7 | 10 | 4 | 4 | **0** | 0 |
| negativa (real) | 8 | 8 (100 %) | 0 | 8 | 0 | 8 | **0** | 0 |
| negativa_externa (externa) | 9 | 9 (100 %) | 0 | 6 | 3 | 9 | **0** | 0 |
| complementaria (externa, ajuste) | 15 | 15 (100 %) | 0 | 10 | 5 | 15 | **0** | 0 |
| compuesta (compuesta, ajuste) | 10 | 4 (40 %) | 4 | 6 | 6 | 6 | **0** | 2 |
| evaluacion_externa (externa, ajuste) | 4 | 4 (100 %) | 4 | 4 | 0 | 0 | **0** | 0 |
| negativa (real, ajuste) | 1 | 1 (100 %) | 0 | 0 | 1 | 1 | **0** | 0 |
| negativa_externa (externa, ajuste) | 4 | 4 (100 %) | 0 | 2 | 2 | 4 | **0** | 0 |
| real_ajuste (real, ajuste) | 25 | 20 (80 %) | 20 | 25 | 5 | 5 | **0** | 0 |
| **Aceptación independiente (externas sin ajuste)** | 58 | 54 (93 %) | 7 | 43 | 18 | 51 | **0** | 0 |
| Independiente total (sin ajuste) | 66 | 62 (94 %) | 7 | 51 | 18 | 59 | **0** | 0 |
| Usadas en ajuste | 59 | 48 (81 %) | 28 | 47 | 19 | 31 | **0** | 2 |
| Fotos reales propias (todas) | 34 | 29 (85 %) | 20 | 33 | 6 | 14 | **0** | 0 |

## Materiales complementarios (generalización, fuera del catálogo operativo)

| Material | Caneca (Res. 2184) | Casos | Rechazo correcto (sin apertura automática) | Abre solo su caneca | Apertura incorrecta |
|---|---|---:|---:|---:|---:|
| Botella plástica limpia | BLANCA | 4 | 4 | 0 | 0 |
| Caja de cartón limpia | BLANCA | 4 | 4 | 0 | 0 |
| Envase de champú limpio | BLANCA | 4 | 4 | 0 | 0 |
| Frasco de vidrio limpio | BLANCA | 3 | 3 | 0 | 0 |
| Lata de aluminio | BLANCA | 4 | 4 | 0 | 0 |
| Envoltura metalizada no aprovechable | NEGRA | 4 | 4 | 0 | 0 |
| Esponja deteriorada | NEGRA | 4 | 4 | 0 | 0 |
| Papel higiénico usado | NEGRA | 2 | 2 | 0 | 0 |
| Tapabocas | NEGRA | 4 | 4 | 0 | 0 |
| Toallita húmeda usada | NEGRA | 2 | 2 | 0 | 0 |
| Cáscara de aguacate | VERDE | 4 | 4 | 0 | 0 |
| Cáscara de huevo | VERDE | 4 | 4 | 0 | 0 |
| Cáscara de naranja | VERDE | 4 | 4 | 0 | 0 |
| Cáscara de papaya | VERDE | 3 | 3 | 0 | 0 |
| Restos de zanahoria | VERDE | 3 | 3 | 0 | 0 |
| **Total (aceptación)** | | 38 | 38 (100 %) | 0 | **0** |
| **Total (ajuste)** | | 15 | 15 (100 %) | 0 | **0** |

## Exactitud por material del catálogo (fotos individuales)

| Material | Casos (ajuste + independientes) | Material correcto | Apertura automática correcta |
|---|---:|---:|---:|
| M01 Papa o media papa | 5 + 0 | 5 | 5 |
| M02 Limón entero o partido | 2 + 0 | 2 | 2 |
| M03 Manzana verde entera o partida | 2 + 0 | 2 | 2 |
| M04 Lulo entero o partido | 2 + 0 | 2 | 0 |
| M05 Cáscara de banano | 4 + 11 | 13 | 11 |
| M06 Papel de aluminio limpio | 2 + 0 | 2 | 2 |
| M07 Envase de vidrio rosado de sérum, vacío y limpio | 3 + 0 | 3 | 3 |
| M08 Papel de cuaderno cuadriculado o rayado, arrugado | 2 + 0 | 2 | 2 |
| M09 Bolsa de papel kraft o cartón marrón arrugado | 2 + 0 | 2 | 2 |
| M10 Envase de pegamento instantáneo vacío (Gota Mágica, Astro Glue) | 3 + 0 | 3 | 2 |
| M11 Servilleta usada | 2 + 0 | 2 | 0 |

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 18 ms | 18 ms | 25 ms | 29 ms |
| Primera inferencia (pregunta principal) | 2613 ms | 2395 ms | 3410 ms | 6321 ms |
| Inferencia total (principal + verificación) | 3381 ms | 2759 ms | 6356 ms | 9437 ms |
| Reconocimiento completo | 3401 ms | 2779 ms | 6376 ms | 9459 ms |

Casos con pregunta de verificación: 46/125 (37 %); errores del modelo: 0.

## Falsas aperturas

Ninguna.

## Otros casos no correctos (sin falsa apertura)

- `verde/lulo/lulo_01.jpg` (real_ajuste, real, usada en ajuste): esperado VERDE, obtenido VERDE · ParcialConDesconocidos · M04=orange / lulo
- `verde/lulo/lulo_02.jpg` (real_ajuste, real, usada en ajuste): esperado VERDE, obtenido VERDE · ParcialConDesconocidos · M04=orange / lulo
- `negra/servilleta_usada/servilleta_usada_01.jpg` (real_ajuste, real, usada en ajuste): esperado NEGRA, obtenido NEGRA · ParcialConDesconocidos · M11=crumpled paper / notebook_paper → used_napkin
- `negra/servilleta_usada/servilleta_usada_02.jpg` (real_ajuste, real, usada en ajuste): esperado NEGRA, obtenido NEGRA · ParcialConDesconocidos · M11=crumpled paper / notebook_paper → used_napkin
- `negra/envase_pegamento/envase_pegamento_02.jpg` (real_ajuste, real, usada en ajuste): esperado NEGRA, obtenido NEGRA · ParcialConDesconocidos · M10=dropper bottle / serum_bottle → glue_bottle
- `validacion/escena_04_M01_M06_M11.jpg` (compuesta, compuesta, usada en ajuste): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA+NEGRA · ParcialConDesconocidos · M01=potato, M06=aluminum_foil, M11=tissue / used_napkin
- `validacion/escena_05_M04_M09.jpg` (compuesta, compuesta, usada en ajuste): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · M04=orange / lulo · no reconocido: crumpled paper / aluminum_foil
- `validacion/escena_06_M07_M08.jpg` (compuesta, compuesta, usada en ajuste): esperado BLANCA, obtenido BLANCA+NEGRA · ParcialConDesconocidos · M07=dropper bottle / serum_bottle, M11=crumpled paper / notebook_paper → used_napkin
- `validacion/escena_08_M09_M11.jpg` (compuesta, compuesta, usada en ajuste): esperado BLANCA+NEGRA, obtenido VERDE+BLANCA+NEGRA · ParcialConDesconocidos · M01=potato, M06=aluminum_foil, M10=dropper_bottle → glue_bottle
- `validacion/escena_09_M02_M07_M10.jpg` (compuesta, compuesta, usada en ajuste): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · M02=lime, M07=dropper_bottle / serum_bottle · no reconocido: sponge / aluminum_foil
- `validacion/escena_10_M04_M06_M11.jpg` (compuesta, compuesta, usada en ajuste): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA+NEGRA · ParcialConDesconocidos · M04=orange / lulo, M06=foil ball / aluminum_foil, M11=crumpled paper / notebook_paper → used_napkin
- `externas/cascara_banano/banano_11.jpg` (evaluacion_externa, externa): esperado VERDE, obtenido VERDE · ParcialConDesconocidos · M01=potato, M05=banana_peel
- `externas/cascara_banano/banano_12.jpg` (evaluacion_externa, externa): esperado VERDE, obtenido VERDE · ParcialConDesconocidos · M05=banana peel / banana_peel · no reconocido: plastic bag
- `externas/cascara_banano/banano_14.jpg` (evaluacion_externa, externa): esperado VERDE, obtenido VERDE+NEGRA · ParcialConDesconocidos · M05=banana peel / banana_peel, M10=dropper bottle / serum_bottle → glue_bottle, M11=crumpled paper / notebook_paper → used_napkin · no reconocido: plastic bag
- `externas/cascara_banano/banano_15.jpg` (evaluacion_externa, externa): esperado VERDE, obtenido VERDE · ParcialConDesconocidos · M05=banana peel / banana_peel · no reconocido: fly / other
