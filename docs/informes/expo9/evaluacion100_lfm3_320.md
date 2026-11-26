# Evaluación Modo ExpoSoftware (9 materiales) — lfm3-vl:latest, lado 320px

Fecha: 2026-11-25 01:00 · Manifiesto: `datasets/exposoftware9/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1887 ms. Las imágenes marcadas `usada_en_ajuste` se excluyen de las métricas independientes.

| Conjunto | Casos | Canecas correctas | Materiales exactos | Falsas aperturas | Canecas omitidas | Ambiguos/desconocidos |
|---|---:|---:|---:|---:|---:|---:|
| evaluacion | 27 | 17 (63 %) | 14 (52 %) | 0 | 10 | 0 |
| multirresiduo | 20 | 8 (40 %) | 7 (35 %) | 0 | 15 | 9 |
| negativa | 5 | 5 (100 %) | 5 (100 %) | 0 | 0 | 1 |
| **Total independiente** | 52 | 30 (58 %) | 26 (50 %) | 0 | 25 | 10 |

## Exactitud por material (individual, conjunto de evaluación)

| Material | Casos | Material correcto | Caneca correcta |
|---|---:|---:|---:|
| E01 Media manzana | 3 | 1 | 1 |
| E02 Cáscara de banano | 3 | 3 | 3 |
| E03 Papa o media papa | 3 | 2 | 2 |
| E04 Bolsa de papel kraft o cartón arrugado, limpio y seco | 3 | 3 | 3 |
| E05 Botella plástica pequeña, vacía y limpia | 3 | 2 | 2 |
| E06 Frasco pequeño de vidrio, vacío y limpio | 3 | 0 | 3 |
| E07 Papel higiénico usado (muestra higiénica) | 3 | 0 | 0 |
| E08 Servilleta usada o contaminada | 3 | 0 | 0 |
| E09 Envoltura de mecato metalizada no aprovechable | 3 | 3 | 3 |

## Exactitud por categoría (caneca esperada presente en el resultado)

- VERDE: 17/24 (71 %)
- BLANCA: 19/23 (83 %)
- NEGRA: 6/20 (30 %)
- Negativas sin apertura automática: 5/5

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 14 ms | 12 ms | 24 ms | 40 ms |
| Inferencia Ollama | 1430 ms | 1385 ms | 1776 ms | 2600 ms |
| Total reconocimiento | 1447 ms | 1395 ms | 1786 ms | 2618 ms |

## Casos con caneca distinta a la esperada

- `evaluacion/E01_eval_01_real.jpg` (real): esperado VERDE, obtenido ninguna · SinMaterialesExpo · no reconocido: lime
- `evaluacion/E01_evald_01_poca_luz.jpg` (aumentada): esperado VERDE, obtenido ninguna · SinMaterialesExpo · no reconocido: lime
- `evaluacion/E03_eval_01_real.jpg` (real): esperado VERDE, obtenido ninguna · SinMaterialesExpo · no reconocido: mushroom
- `evaluacion/E05_eval_02_ilustracion.jpg` (ilustracion): esperado BLANCA, obtenido ninguna · SinMaterialesExpo · no reconocido: soap dispenser
- `evaluacion/E07_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo
- `evaluacion/E07_eval_02_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue, cup
- `evaluacion/E07_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_02_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `multirresiduo/M02_real_E01_E04.jpg` (real): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · no reconocido: card
- `multirresiduo/M07_compuesta_E07_E08.jpg` (compuesta): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `multirresiduo/M08_compuesta_E01_E05.jpg` (compuesta): esperado VERDE+BLANCA, obtenido ninguna · SinMaterialesExpo · no reconocido: soap dispenser, lime
- `multirresiduo/M10_compuesta_E03_E06.jpg` (compuesta): esperado VERDE+BLANCA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: french fries
- `multirresiduo/M11_compuesta_E01_E08.jpg` (compuesta): esperado VERDE+NEGRA, obtenido VERDE · Reconocido
- `multirresiduo/M12_compuesta_E03_E09.jpg` (compuesta): esperado VERDE+NEGRA, obtenido NEGRA · ParcialConDesconocidos · no reconocido: cheese
- `multirresiduo/M13_compuesta_E05_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: soap dispenser
- `multirresiduo/M14_compuesta_E06_E09.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: bag
- `multirresiduo/M15_compuesta_E01_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M17_compuesta_E03_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · Reconocido
- `multirresiduo/M19_compuesta_E04_E07_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M20_compuesta_E01_E03_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: sponge, tissue
