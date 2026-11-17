### validacion (contenido v2)

| Métrica | Valor |
|---|---|
| Muestras | 33 |
| Clave exacta correcta | 21 (63,6 %) |
| Ruta de disposición correcta | 25 (75,8 %) |
| Falsas admisiones (abriría sin deber) | 0 |
| Ofertas indebidas | 0 |
| Abstenciones | 4 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 1074 ms / 6280 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 2 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 5 | 0 | 4 | 1,00 | 0,56 | 0,71 |
| Cortopunzante | 1 | 0 | 1 | 1,00 | 0,50 | 0,67 |
| Evaluar | 6 | 4 | 1 | 0,60 | 0,86 | 0,71 |
| Medicamentos | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Negra | 2 | 2 | 1 | 0,50 | 0,67 | 0,57 |
| NoEsResiduo | 2 | 1 | 0 | 0,67 | 1,00 | 0,80 |
| PilasBaterias | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Raee | 2 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| ReutilizacionDonacion | 0 | 2 | 0 | 0,00 | 0,00 | 0,00 |
| Verde | 1 | 1 | 5 | 0,50 | 0,17 | 0,25 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 0 | 5 | 0 | 0 | 0 | 2 | 1 | 0 | 0 | 1 | 0 |
| Cortopunzante | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| Evaluar | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| Medicamentos | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 1 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 |
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
| mixto-26.jpg | mixto | abstención | `styrofoam,plastic_container` → Negra | no |
| papel-09.jpg | papel | `clothes` → ReutilizacionDonacion | `paper` → Blanca | no |
| plastico-45.jpg | plastico | `people` → NoEsResiduo | `plastic_bottle` → Blanca | no |
| vidrio-27.jpg | vidrio | `drinking_glass` → ReutilizacionDonacion | `broken_glass` → Cortopunzante | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- Ninguna.

