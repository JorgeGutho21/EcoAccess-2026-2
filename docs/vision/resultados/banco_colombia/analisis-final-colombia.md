### Banco Colombia (200 fotos, versión final)

| Métrica | Valor |
|---|---|
| Muestras | 200 |
| Clave exacta correcta | 154 (77,0 %) |
| Ruta de disposición correcta | 166 (83,0 %) |
| Falsas admisiones (abriría sin deber) | 1 |
| Ofertas indebidas | 2 |
| Abstenciones | 10 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 998 ms / 5347 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 10 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 21 | 9 | 5 | 0,70 | 0,81 | 0,75 |
| Cortopunzante | 2 | 0 | 2 | 1,00 | 0,50 | 0,67 |
| Evaluar | 46 | 7 | 12 | 0,87 | 0,79 | 0,83 |
| Medicamentos | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Negra | 22 | 5 | 14 | 0,81 | 0,61 | 0,70 |
| NoEsResiduo | 1 | 4 | 0 | 0,20 | 1,00 | 0,33 |
| Peligroso | 0 | 3 | 0 | 0,00 | 0,00 | 0,00 |
| PilasBaterias | 10 | 0 | 1 | 1,00 | 0,91 | 0,95 |
| Raee | 45 | 0 | 3 | 1,00 | 0,94 | 0,97 |
| ReutilizacionDonacion | 4 | 4 | 3 | 0,50 | 0,57 | 0,53 |
| Verde | 5 | 1 | 4 | 0,83 | 0,56 | 0,67 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 0 | 21 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 | 3 | 0 |
| Cortopunzante | 0 | 0 | 2 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 8 | 0 | 0 | 46 | 0 | 0 | 1 | 2 | 0 | 0 | 0 | 1 |
| Negra | 0 | 9 | 0 | 4 | 1 | 22 | 0 | 0 | 0 | 0 | 0 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 10 | 0 | 1 | 0 |
| Raee | 0 | 0 | 0 | 0 | 0 | 1 | 2 | 0 | 0 | 45 | 0 | 0 |
| ReutilizacionDonacion | 2 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 4 | 0 |
| Verde | 0 | 0 | 0 | 3 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 5 |

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
| electronico-03.jpg | electronico | `phone_case` → Negra | `phone` → Raee | no |
| electronico-39.jpg | electronico | `people` → NoEsResiduo | `bluetooth_speaker,speaker_wired` → Raee | no |
| electronico-41.jpg | electronico | `people` → NoEsResiduo | `bluetooth_speaker,speaker_wired` → Raee | no |
| pilas-17.jpg | especial | `cap_hat` → ReutilizacionDonacion | `lithium_battery,battery` → PilasBaterias | no |
| higiene-11.jpg | higiene | `plastic_container` → Blanca | `toothpaste_tube` → Negra | no |
| higiene-18.jpg | higiene | `chemical_container` → Peligroso | `deodorant` → Evaluar | no |
| higiene-19.jpg | higiene | `chemical_container` → Peligroso | `deodorant` → Evaluar | no |
| higiene-25.jpg | higiene | abstención | `soap_bar` → ReutilizacionDonacion | no |
| higiene-29.jpg | higiene | `rubber_items` → Negra | `disposable_razor` → Cortopunzante | no |
| higiene-33.jpg | higiene | `toothpaste_tube` → Negra | `disposable_razor` → Cortopunzante | no |
| higiene-42.jpg | higiene | abstención | `cream_jar,makeup` → Evaluar | no |
| higiene-43.jpg | higiene | abstención | `cream_jar,makeup` → Evaluar | no |
| icopor-01.jpg | mixto | `cardboard` → Blanca | `styrofoam,styrofoam_food_box,served_food` → Negra | no |
| icopor-03.jpg | mixto | `cardboard` → Blanca | `styrofoam,styrofoam_food_box,served_food` → Negra | no |
| icopor-06.jpg | mixto | `plastic_container` → Blanca | `styrofoam` → Negra | no |
| papel-07.jpg | papel | `umbrella` → ReutilizacionDonacion | `paper` → Blanca | no |
| pet-17.jpg | plastico | `chemical_container` → Peligroso | `plastic_bottle` → Blanca | no |
| plastico-03.jpg | plastico | `drinking_glass` → ReutilizacionDonacion | `plastic_container` → Blanca | no |
| plastico-16.jpg | plastico | `other_animal` → NoEsResiduo | `plastic_bottle` → Blanca | no |
| sanitario-02.jpg | sanitario | abstención | `napkin_clean,used_tissue` → ReutilizacionDonacion | no |
| sanitario-14.jpg | sanitario | `plastic_container` → Blanca | `wet_wipes,chemical_container` → Negra | no |
| sanitario-31.jpg | sanitario | `plastic_container` → Blanca | `cotton_swab` → Negra | no |
| sanitario-33.jpg | sanitario | `plastic_container` → Blanca | `cotton_swab` → Negra | no |
| sanitario-36.jpg | sanitario | `medicine` → Medicamentos | `cotton_swab` → Negra | no |
| vidrio-07.jpg | vidrio | `vase` → ReutilizacionDonacion | `glass_bottle` → Blanca | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- carton-05.jpg (carton): Eco dijo `cardboard`; correcto `cardboard,eggs`.

