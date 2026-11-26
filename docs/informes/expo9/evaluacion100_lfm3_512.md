# Evaluación Modo ExpoSoftware (9 materiales) — lfm3-vl:latest, lado 512px

Fecha: 2026-11-25 01:03 · Manifiesto: `datasets/exposoftware9/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1479 ms. Las imágenes marcadas `usada_en_ajuste` se excluyen de las métricas independientes.

| Conjunto | Casos | Canecas correctas | Materiales exactos | Falsas aperturas | Canecas omitidas | Ambiguos/desconocidos |
|---|---:|---:|---:|---:|---:|---:|
| evaluacion | 27 | 20 (74 %) | 17 (63 %) | 0 | 7 | 2 |
| multirresiduo | 20 | 10 (50 %) | 10 (50 %) | 0 | 11 | 9 |
| negativa | 5 | 5 (100 %) | 5 (100 %) | 0 | 0 | 1 |
| **Total independiente** | 52 | 35 (67 %) | 32 (62 %) | 0 | 18 | 12 |

## Exactitud por material (individual, conjunto de evaluación)

| Material | Casos | Material correcto | Caneca correcta |
|---|---:|---:|---:|
| E01 Media manzana | 3 | 2 | 2 |
| E02 Cáscara de banano | 3 | 3 | 3 |
| E03 Papa o media papa | 3 | 3 | 3 |
| E04 Bolsa de papel kraft o cartón arrugado, limpio y seco | 3 | 3 | 3 |
| E05 Botella plástica pequeña, vacía y limpia | 3 | 2 | 2 |
| E06 Frasco pequeño de vidrio, vacío y limpio | 3 | 0 | 3 |
| E07 Papel higiénico usado (muestra higiénica) | 3 | 1 | 1 |
| E08 Servilleta usada o contaminada | 3 | 0 | 0 |
| E09 Envoltura de mecato metalizada no aprovechable | 3 | 3 | 3 |

## Exactitud por categoría (caneca esperada presente en el resultado)

- VERDE: 21/24 (88 %)
- BLANCA: 21/23 (91 %)
- NEGRA: 7/20 (35 %)
- Negativas sin apertura automática: 5/5

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 17 ms | 16 ms | 35 ms | 68 ms |
| Inferencia Ollama | 1805 ms | 1792 ms | 2251 ms | 2648 ms |
| Total reconocimiento | 1824 ms | 1811 ms | 2268 ms | 2684 ms |

## Casos con caneca distinta a la esperada

- `evaluacion/E01_evald_01_poca_luz.jpg` (aumentada): esperado VERDE, obtenido ninguna · SinMaterialesExpo · no reconocido: green fruit
- `evaluacion/E05_eval_02_ilustracion.jpg` (ilustracion): esperado BLANCA, obtenido ninguna · GestionEspecial · no reconocido: pump dispenser, spray bottle
- `evaluacion/E07_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E07_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_02_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: paper towel
- `multirresiduo/M02_real_E01_E04.jpg` (real): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · no reconocido: juice box
- `multirresiduo/M07_compuesta_E07_E08.jpg` (compuesta): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `multirresiduo/M10_compuesta_E03_E06.jpg` (compuesta): esperado VERDE+BLANCA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: french fries
- `multirresiduo/M11_compuesta_E01_E08.jpg` (compuesta): esperado VERDE+NEGRA, obtenido VERDE · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M13_compuesta_E05_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue paper
- `multirresiduo/M14_compuesta_E06_E09.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: bag
- `multirresiduo/M15_compuesta_E01_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: flower, lime
- `multirresiduo/M17_compuesta_E03_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M19_compuesta_E04_E07_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · Reconocido
- `multirresiduo/M20_compuesta_E01_E03_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue
