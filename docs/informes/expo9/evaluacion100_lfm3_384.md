# Evaluación Modo ExpoSoftware (9 materiales) — lfm3-vl:latest, lado 384px

Fecha: 2026-11-25 01:02 · Manifiesto: `datasets/exposoftware9/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1810 ms. Las imágenes marcadas `usada_en_ajuste` se excluyen de las métricas independientes.

| Conjunto | Casos | Canecas correctas | Materiales exactos | Falsas aperturas | Canecas omitidas | Ambiguos/desconocidos |
|---|---:|---:|---:|---:|---:|---:|
| evaluacion | 27 | 20 (74 %) | 17 (63 %) | 0 | 7 | 2 |
| multirresiduo | 20 | 10 (50 %) | 9 (45 %) | 0 | 14 | 6 |
| negativa | 5 | 5 (100 %) | 5 (100 %) | 0 | 0 | 1 |
| **Total independiente** | 52 | 35 (67 %) | 31 (60 %) | 0 | 21 | 9 |

## Exactitud por material (individual, conjunto de evaluación)

| Material | Casos | Material correcto | Caneca correcta |
|---|---:|---:|---:|
| E01 Media manzana | 3 | 3 | 3 |
| E02 Cáscara de banano | 3 | 3 | 3 |
| E03 Papa o media papa | 3 | 2 | 2 |
| E04 Bolsa de papel kraft o cartón arrugado, limpio y seco | 3 | 3 | 3 |
| E05 Botella plástica pequeña, vacía y limpia | 3 | 2 | 2 |
| E06 Frasco pequeño de vidrio, vacío y limpio | 3 | 0 | 3 |
| E07 Papel higiénico usado (muestra higiénica) | 3 | 1 | 1 |
| E08 Servilleta usada o contaminada | 3 | 0 | 0 |
| E09 Envoltura de mecato metalizada no aprovechable | 3 | 3 | 3 |

## Exactitud por categoría (caneca esperada presente en el resultado)

- VERDE: 19/24 (79 %)
- BLANCA: 20/23 (87 %)
- NEGRA: 7/20 (35 %)
- Negativas sin apertura automática: 5/5

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 17 ms | 14 ms | 43 ms | 73 ms |
| Inferencia Ollama | 2082 ms | 1808 ms | 3076 ms | 5664 ms |
| Total reconocimiento | 2103 ms | 1834 ms | 3111 ms | 5677 ms |

## Casos con caneca distinta a la esperada

- `evaluacion/E03_eval_01_real.jpg` (real): esperado VERDE, obtenido ninguna · SinMaterialesExpo · no reconocido: mushroom
- `evaluacion/E05_eval_02_ilustracion.jpg` (ilustracion): esperado BLANCA, obtenido ninguna · GestionEspecial · no reconocido: pump dispenser, spray bottle
- `evaluacion/E07_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E07_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_02_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: paper towel
- `multirresiduo/M07_compuesta_E07_E08.jpg` (compuesta): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue, watch
- `multirresiduo/M08_compuesta_E01_E05.jpg` (compuesta): esperado VERDE+BLANCA, obtenido ninguna · SinMaterialesExpo · no reconocido: soap dispenser, lime
- `multirresiduo/M10_compuesta_E03_E06.jpg` (compuesta): esperado VERDE+BLANCA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: french fries
- `multirresiduo/M11_compuesta_E01_E08.jpg` (compuesta): esperado VERDE+NEGRA, obtenido VERDE · Reconocido
- `multirresiduo/M13_compuesta_E05_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · Reconocido
- `multirresiduo/M14_compuesta_E06_E09.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: bag
- `multirresiduo/M15_compuesta_E01_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M17_compuesta_E03_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · Reconocido
- `multirresiduo/M19_compuesta_E04_E07_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M20_compuesta_E01_E03_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: sponge, soap dispenser, lime
