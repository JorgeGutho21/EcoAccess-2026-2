# Evaluación Modo ExpoSoftware (9 materiales) — lfm3-vl:latest, lado 512px

Fecha: 2026-11-24 23:49 · Manifiesto: `datasets/exposoftware9/manifiesto.json` · Sin hardware (no se movió ninguna caneca).
Precalentamiento del modelo: 1180 ms. Las imágenes marcadas `usada_en_ajuste` se excluyen de las métricas independientes.

| Conjunto | Casos | Canecas correctas | Materiales exactos | Falsas aperturas | Canecas omitidas | Ambiguos/desconocidos |
|---|---:|---:|---:|---:|---:|---:|
| evaluacion | 22 | 16 (73 %) | 15 (68 %) | 0 | 6 | 2 |
| multirresiduo | 49 | 24 (49 %) | 12 (24 %) | 0 | 32 | 22 |
| negativa | 13 | 13 (100 %) | 13 (100 %) | 0 | 0 | 1 |
| **Total independiente** | 84 | 53 (63 %) | 40 (48 %) | 0 | 38 | 25 |

## Exactitud por material (individual, conjunto de evaluación)

| Material | Casos | Material correcto | Caneca correcta |
|---|---:|---:|---:|
| E01 Media manzana | 2 | 2 | 2 |
| E02 Cáscara de banano | 1 | 1 | 1 |
| E03 Papa o media papa | 3 | 3 | 3 |
| E04 Bolsa de papel kraft o cartón arrugado, limpio y seco | 3 | 3 | 3 |
| E05 Botella plástica pequeña, vacía y limpia | 2 | 1 | 1 |
| E06 Frasco pequeño de vidrio, vacío y limpio | 1 | 0 | 1 |
| E07 Papel higiénico usado (muestra higiénica) | 3 | 1 | 1 |
| E08 Servilleta usada o contaminada | 5 | 2 | 2 |
| E09 Envoltura de mecato metalizada no aprovechable | 2 | 2 | 2 |

## Exactitud por categoría (caneca esperada presente en el resultado)

- VERDE: 31/40 (78 %)
- BLANCA: 34/39 (87 %)
- NEGRA: 18/42 (43 %)
- Negativas sin apertura automática: 13/13

## Latencia del reconocimiento (modelo cargado, sin caché)

| Etapa | Media | Mediana | P95 | Máximo |
|---|---:|---:|---:|---:|
| Preprocesamiento | 15 ms | 14 ms | 30 ms | 53 ms |
| Inferencia Ollama | 1765 ms | 1694 ms | 2558 ms | 2874 ms |
| Total reconocimiento | 1782 ms | 1709 ms | 2612 ms | 2904 ms |

## Casos con caneca distinta a la esperada

- `evaluacion/E05_eval_02_ilustracion.jpg` (ilustracion): esperado BLANCA, obtenido ninguna · GestionEspecial · no reconocido: pump dispenser, spray bottle
- `evaluacion/E07_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E07_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_01_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_02_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: tissue
- `evaluacion/E08_eval_03_ilustracion.jpg` (ilustracion): esperado NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: paper towel
- `multirresiduo/M02_real_E01_E04.jpg` (real): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · no reconocido: juice box
- `multirresiduo/M05_compuesta_E01.jpg` (compuesta): esperado VERDE, obtenido ninguna · SinMaterialesExpo · no reconocido: green fruit
- `multirresiduo/M07_compuesta_E07.jpg` (compuesta): esperado NEGRA, obtenido ninguna · SinMaterialesExpo
- `multirresiduo/M16_compuesta_E07_E09.jpg` (compuesta): esperado NEGRA, obtenido ninguna · GestionEspecial · no reconocido: tissue, cable
- `multirresiduo/M17_compuesta_E01_E05.jpg` (compuesta): esperado VERDE+BLANCA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: lime
- `multirresiduo/M20_compuesta_E01_E08.jpg` (compuesta): esperado VERDE+NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: lime, tissue
- `multirresiduo/M22_compuesta_E02_E07.jpg` (compuesta): esperado VERDE+NEGRA, obtenido VERDE · Reconocido
- `multirresiduo/M23_compuesta_E05_E08.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M25_compuesta_E04_E07.jpg` (compuesta): esperado BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: tissue paper
- `multirresiduo/M26_compuesta_E01_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: lime
- `multirresiduo/M28_compuesta_E03_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: french fries
- `multirresiduo/M29_compuesta_E01_E02_E05.jpg` (compuesta): esperado VERDE+BLANCA, obtenido VERDE · ParcialConDesconocidos · no reconocido: lime, soap dispenser
- `multirresiduo/M30_compuesta_E03_E04_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M32_compuesta_E01_E07_E08.jpg` (compuesta): esperado VERDE+NEGRA, obtenido ninguna · SinMaterialesExpo · no reconocido: lime
- `multirresiduo/M35_compuesta_E01_E04_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA · ParcialConDesconocidos · no reconocido: lime
- `multirresiduo/M36_compuesta_E02_E05_E09.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE · ParcialConDesconocidos · no reconocido: french fries, soap dispenser
- `multirresiduo/M40_compuesta_E01_E06_E09.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M41_compuesta_E03_E04_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M42_compuesta_E02_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M43_compuesta_E01_E03_E05_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: lime
- `multirresiduo/M45_compuesta_E01_E04_E05_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue paper
- `multirresiduo/M46_compuesta_E03_E05_E08_E09.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido NEGRA · ParcialConDesconocidos · no reconocido: soap dispenser, french fries
- `multirresiduo/M47_compuesta_E01_E02_E04_E08.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · Reconocido
- `multirresiduo/M48_compuesta_E02_E03_E06_E07.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido VERDE+BLANCA · ParcialConDesconocidos · no reconocido: tissue
- `multirresiduo/M49_compuesta_E01_E05_E06_E09.jpg` (compuesta): esperado VERDE+BLANCA+NEGRA, obtenido BLANCA+NEGRA · ParcialConDesconocidos · no reconocido: lime
