### validacion (antes)

| Métrica | Valor |
|---|---|
| Muestras | 33 |
| Clave exacta correcta | 20 (60,6 %) |
| Ruta de disposición correcta | 22 (66,7 %) |
| Falsas admisiones (abriría sin deber) | 1 |
| Ofertas indebidas | 0 |
| Abstenciones | 5 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 1396 ms / 6628 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 3 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 5 | 2 | 4 | 0,71 | 0,56 | 0,63 |
| Cortopunzante | 1 | 0 | 1 | 1,00 | 0,50 | 0,67 |
| Evaluar | 6 | 5 | 1 | 0,55 | 0,86 | 0,67 |
| Medicamentos | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Negra | 0 | 0 | 3 | 0,00 | 0,00 | 0,00 |
| NoEsResiduo | 2 | 2 | 0 | 0,50 | 1,00 | 0,67 |
| PilasBaterias | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Raee | 2 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| ReutilizacionDonacion | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Verde | 1 | 1 | 5 | 0,50 | 0,17 | 0,25 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 1 | 5 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 1 | 0 |
| Cortopunzante | 0 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| Medicamentos | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 1 | 1 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 |
| Raee | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 |
| Verde | 1 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| agricola-01.jpg | agricola | `mixed_trash` → Evaluar | `plant_waste` → Verde | no |
| agricola-18.jpg | agricola | `mixed_trash` → Evaluar | `plant_waste` → Verde | no |
| hojas2-11.jpg | agricola | abstención | `plant_waste` → Verde | no |
| fruta-50.jpg | alimentos | `plant_waste` → Verde | `whole_produce` → Evaluar | no |
| carton-21.jpg | carton | `people` → NoEsResiduo | `cardboard,greasy_paper` → Blanca | no |
| mixto-26.jpg | mixto | abstención | `styrofoam,plastic_container` → Negra | no |
| mixto2-06.jpg | mixto | `served_food` → Evaluar | `styrofoam,plastic_container` → Negra | no |
| papel-09.jpg | papel | `clothes` → ReutilizacionDonacion | `paper` → Blanca | no |
| plastico-20.jpg | plastico | abstención | `plastic_container,styrofoam` → Blanca | no |
| plastico-45.jpg | plastico | `people` → NoEsResiduo | `plastic_bottle` → Blanca | no |
| vidrio-27.jpg | vidrio | `glass_bottle` → Blanca | `broken_glass` → Cortopunzante | **sí** |

**Falsas admisiones** (Eco abriría sola sin deber):

- vidrio-27.jpg (vidrio): Eco dijo `glass_bottle`; correcto `broken_glass`.

