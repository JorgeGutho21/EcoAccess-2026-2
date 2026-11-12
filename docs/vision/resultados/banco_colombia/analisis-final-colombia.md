### Banco Colombia (versión final)

| Métrica | Valor |
|---|---|
| Muestras | 77 |
| Clave exacta correcta | 56 (72,7 %) |
| Ruta de disposición correcta | 60 (77,9 %) |
| Falsas admisiones (abriría sin deber) | 1 |
| Ofertas indebidas | 2 |
| Abstenciones | 6 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 1425 ms / 5942 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 6 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 21 | 5 | 5 | 0,81 | 0,81 | 0,81 |
| Evaluar | 17 | 6 | 8 | 0,74 | 0,68 | 0,71 |
| Negra | 7 | 1 | 9 | 0,88 | 0,44 | 0,58 |
| NoEsResiduo | 1 | 2 | 0 | 0,33 | 1,00 | 0,50 |
| Peligroso | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Raee | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| ReutilizacionDonacion | 0 | 3 | 0 | 0,00 | 0,00 | 0,00 |
| Verde | 5 | 1 | 3 | 0,83 | 0,62 | 0,71 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Evaluar | Negra | NoEsResiduo | Peligroso | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 0 | 21 | 0 | 0 | 1 | 1 | 0 | 3 | 0 |
| Evaluar | 6 | 0 | 17 | 0 | 1 | 0 | 0 | 0 | 1 |
| Negra | 0 | 5 | 4 | 7 | 0 | 0 | 0 | 0 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| Raee | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 |
| Verde | 0 | 0 | 2 | 1 | 0 | 0 | 0 | 0 | 5 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| fruta-03.jpg | alimentos | `people` → NoEsResiduo | `whole_produce` → Evaluar | no |
| fruta-11.jpg | alimentos | `food_scraps` → Verde | `whole_produce` → Evaluar | no |
| fruta-19.jpg | alimentos | abstención | `whole_produce,cassava` → Evaluar | no |
| fruta-25.jpg | alimentos | abstención | `whole_produce,corn_cob` → Evaluar | no |
| fruta-28.jpg | alimentos | abstención | `whole_produce,corn_cob,served_food` → Evaluar | no |
| fruta-29.jpg | alimentos | abstención | `whole_produce,corn_cob,served_food` → Evaluar | no |
| fruta-35.jpg | alimentos | abstención | `whole_produce` → Evaluar | no |
| organico-41.jpg | alimentos | `ash` → Negra | `food_scraps` → Verde | no |
| carton-04.jpg | carton | abstención | `eggs,cardboard` → Evaluar | no |
| icopor-01.jpg | mixto | `cardboard` → Blanca | `styrofoam,styrofoam_food_box,served_food` → Negra | no |
| icopor-03.jpg | mixto | `cardboard` → Blanca | `styrofoam,styrofoam_food_box,served_food` → Negra | no |
| icopor-06.jpg | mixto | `plastic_container` → Blanca | `styrofoam` → Negra | no |
| papel-07.jpg | papel | `umbrella` → ReutilizacionDonacion | `paper` → Blanca | no |
| pet-17.jpg | plastico | `chemical_container` → Peligroso | `plastic_bottle` → Blanca | no |
| plastico-03.jpg | plastico | `drinking_glass` → ReutilizacionDonacion | `plastic_container` → Blanca | no |
| plastico-16.jpg | plastico | `other_animal` → NoEsResiduo | `plastic_bottle` → Blanca | no |
| vidrio-07.jpg | vidrio | `vase` → ReutilizacionDonacion | `glass_bottle` → Blanca | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- carton-05.jpg (carton): Eco dijo `cardboard`; correcto `cardboard,eggs`.

