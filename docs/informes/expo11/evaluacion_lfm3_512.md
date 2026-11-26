# Evaluación Modo ExpoSoftware (9 materiales) — lfm3-vl:latest, lado 512px

Fecha: 2026-11-25 10:25 · Manifiesto: `datasets/ecoaccess11/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1730 ms. Las imágenes marcadas `usada_en_ajuste` se excluyen de las métricas independientes.

| Conjunto | Casos | Canecas correctas | Materiales exactos | Falsas aperturas | Canecas omitidas | Ambiguos/desconocidos |
|---|---:|---:|---:|---:|---:|---:|
| negativa | 9 | 8 (89 %) | 8 (89 %) | 1 | 0 | 3 |
| **Total independiente** | 9 | 8 (89 %) | 8 (89 %) | 1 | 0 | 3 |

## Exactitud por material (individual, conjunto de evaluación)

| Material | Casos | Material correcto | Caneca correcta |
|---|---:|---:|---:|
| M01 Papa o media papa | 0 | 0 | 0 |
| M02 Limón entero o partido | 0 | 0 | 0 |
| M03 Manzana verde entera o partida | 0 | 0 | 0 |
| M04 Lulo entero o partido | 0 | 0 | 0 |
| M05 Cáscara de banano | 0 | 0 | 0 |
| M06 Papel de aluminio limpio | 0 | 0 | 0 |
| M07 Envase de vidrio rosado de sérum, vacío y limpio | 0 | 0 | 0 |
| M08 Papel de cuaderno cuadriculado o rayado, arrugado | 0 | 0 | 0 |
| M09 Bolsa de papel kraft o cartón marrón arrugado | 0 | 0 | 0 |
| M10 Envase de pegamento instantáneo vacío (Gota Mágica, Astro Glue) | 0 | 0 | 0 |
| M11 Servilleta usada | 0 | 0 | 0 |

## Exactitud por categoría (caneca esperada presente en el resultado)

- VERDE: 0/0 (-)
- BLANCA: 0/0 (-)
- NEGRA: 0/0 (-)
- Negativas sin apertura automática: 8/9

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 21 ms | 21 ms | 30 ms | 33 ms |
| Inferencia Ollama | 3069 ms | 2372 ms | 5757 ms | 5820 ms |
| Total reconocimiento | 3091 ms | 2387 ms | 5781 ms | 5836 ms |

## Casos con caneca distinta a la esperada

- `negativos/negativo_07.jpg` (real): esperado ninguna, obtenido NEGRA · Reconocido · **FALSA APERTURA**
- `validacion/escena_05_M04_M09.jpg` (compuesta, usada en ajuste): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · no reconocido: crumpled paper / aluminum_foil
- `validacion/escena_06_M07_M08.jpg` (compuesta, usada en ajuste): esperado BLANCA, obtenido BLANCA+NEGRA · Reconocido · **FALSA APERTURA**
- `validacion/escena_08_M09_M11.jpg` (compuesta, usada en ajuste): esperado BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: dropper_bottle, plastic_bag
- `validacion/escena_09_M02_M07_M10.jpg` (compuesta, usada en ajuste): esperado VERDE+BLANCA+NEGRA, obtenido VERDE · ParcialConDesconocidos · no reconocido: dropper_bottle / serum_bottle, sponge / aluminum_foil
