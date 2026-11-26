# Evaluación Modo ExpoSoftware (9 materiales) — lfm3-vl:latest, lado 640px

Fecha: 2026-11-24 23:52 · Manifiesto: `datasets/exposoftware9/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1654 ms. Las imágenes marcadas `usada_en_ajuste` se excluyen de las métricas independientes.

| Conjunto | Casos | Canecas correctas | Materiales exactos | Falsas aperturas | Canecas omitidas | Ambiguos/desconocidos |
|---|---:|---:|---:|---:|---:|---:|
| evaluacion | 22 | 16 (73 %) | 14 (64 %) | 0 | 6 | 2 |
| multirresiduo | 49 | 25 (51 %) | 14 (29 %) | 0 | 29 | 21 |
| negativa | 13 | 13 (100 %) | 13 (100 %) | 0 | 0 | 1 |
| **Total independiente** | 84 | 54 (64 %) | 41 (49 %) | 0 | 35 | 24 |

## Exactitud por material (individual, conjunto de evaluación)

| Material | Casos | Material correcto | Caneca correcta |
|---|---:|---:|---:|
| E01 Media manzana | 2 | 2 | 2 |
| E02 Cáscara de banano | 1 | 1 | 1 |
| E03 Papa o media papa | 3 | 2 | 3 |
| E04 Bolsa de papel kraft o cartón arrugado, limpio y seco | 3 | 3 | 3 |
| E05 Botella plástica pequeña, vacía y limpia | 2 | 1 | 1 |
| E06 Frasco pequeño de vidrio, vacío y limpio | 1 | 0 | 1 |
| E07 Papel higiénico usado (muestra higiénica) | 3 | 1 | 1 |
| E08 Servilleta usada o contaminada | 5 | 2 | 2 |
| E09 Envoltura de mecato metalizada no aprovechable | 2 | 2 | 2 |

## Exactitud por categoría (caneca esperada presente en el resultado)

- VERDE: 32/40 (80 %)
- BLANCA: 35/39 (90 %)
- NEGRA: 19/42 (45 %)
- Negativas sin apertura automática: 13/13

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 9 ms | 7 ms | 19 ms | 20 ms |
| Inferencia Ollama | 2100 ms | 2052 ms | 3209 ms | 3310 ms |
| Total reconocimiento | 2110 ms | 2059 ms | 3221 ms | 3320 ms |

## Casos con caneca distinta a la esperada

- `evaluacion/E05_eval_02_ilustracion.jpg` (ilustracion): esperado BLANCA, obtenido ninguna · GestionEspecial · no reconocido: pump dispenser, spray bottle
- `evaluacion/E07_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E07_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_02_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: paper towel
- `multirresiduo/M02_real_E01_E04.jpg` (real): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · no reconocido: yoplax container
- `multirresiduo/M05_compuesta_E01.jpg` (compuesta): esperado VERDE, obtenido ninguna · SinMaterialesExpo · no reconocido: green fruit
- `multirresiduo/M07_compuesta_E07.jpg` (compuesta): esperado NEGRA, obtenido ninguna · SinMaterialesExpo
- `multirresiduo/M14_compuesta_E07_E08.jpg` (compuesta): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: paper cup, tissue paper
- `multirresiduo/M20_compuesta_E01_E08.jpg` (compuesta): esperado VERDE+NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: lime, tissue
- `multirresiduo/M22_compuesta_E02_E07.jpg` (compuesta): esperado VERDE+NEGRA, obtenido VERDE · Reconocido
- `multirresiduo/M23_compuesta_E05_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M25_compuesta_E04_E07.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue paper
- `multirresiduo/M26_compuesta_E01_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: lime, towel
- `multirresiduo/M28_compuesta_E03_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: french fries, tissue paper
- `multirresiduo/M29_compuesta_E01_E02_E05.jpg` (compuesta): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · no reconocido: lime, soap dispenser
- `multirresiduo/M30_compuesta_E03_E04_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M32_compuesta_E01_E07_E08.jpg` (compuesta): esperado VERDE+NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue, lime
- `multirresiduo/M35_compuesta_E01_E04_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: lime, tissue paper
- `multirresiduo/M40_compuesta_E01_E06_E09.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA+NEGRA · ParcialConDesconocidos · no reconocido: lime
- `multirresiduo/M41_compuesta_E03_E04_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M42_compuesta_E02_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M43_compuesta_E01_E03_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: wipe
- `multirresiduo/M45_compuesta_E01_E04_E05_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: paper towel
- `multirresiduo/M46_compuesta_E03_E05_E08_E09.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+NEGRA · ParcialConDesconocidos · no reconocido: soap dispenser, tissue
- `multirresiduo/M47_compuesta_E01_E02_E04_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M48_compuesta_E02_E03_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M49_compuesta_E01_E05_E06_E09.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA+NEGRA · ParcialConDesconocidos · no reconocido: lime
- `multirresiduo/M50_compuesta_E04_E07_E08_E09.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue
