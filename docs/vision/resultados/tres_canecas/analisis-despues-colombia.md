### Banco Colombia, después

| Métrica | Valor |
|---|---|
| Muestras | 275 |
| Clave exacta correcta | 217 (78,9 %) |
| Ruta de disposición correcta | 229 (83,3 %) |
| Falsas admisiones (abriría sin deber) | 1 |
| Ofertas indebidas | 8 |
| Abstenciones | 14 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 1310 ms / 5968 ms |
| **Aperturas automáticas (blanca + estación)** | 45 (9 blanca, 36 verde/negra) |
| **Aperturas equivocadas** | 1 (1 blanca, 0 verde/negra) |
| **Precisión de las decisiones automáticas** | 97,8 % |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 14 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 21 | 16 | 5 | 0,57 | 0,81 | 0,67 |
| Bombillos | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Cortopunzante | 8 | 1 | 2 | 0,89 | 0,80 | 0,84 |
| Evaluar | 46 | 7 | 12 | 0,87 | 0,79 | 0,83 |
| Medicamentos | 10 | 1 | 1 | 0,91 | 0,91 | 0,91 |
| Negra | 26 | 5 | 16 | 0,84 | 0,62 | 0,71 |
| NoEsResiduo | 1 | 5 | 0 | 0,17 | 1,00 | 0,29 |
| Peligroso | 6 | 3 | 3 | 0,67 | 0,67 | 0,67 |
| PilasBaterias | 12 | 0 | 5 | 1,00 | 0,71 | 0,83 |
| Raee | 45 | 0 | 3 | 1,00 | 0,94 | 0,97 |
| ReutilizacionDonacion | 37 | 4 | 6 | 0,90 | 0,86 | 0,88 |
| Verde | 5 | 1 | 4 | 0,83 | 0,56 | 0,67 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Bombillos | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 0 | 21 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 | 3 | 0 |
| Bombillos | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Cortopunzante | 0 | 0 | 0 | 8 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 8 | 0 | 0 | 0 | 46 | 0 | 0 | 1 | 2 | 0 | 0 | 0 | 1 |
| Medicamentos | 0 | 1 | 0 | 0 | 0 | 10 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 1 | 10 | 0 | 0 | 4 | 1 | 26 | 0 | 0 | 0 | 0 | 0 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 |
| Peligroso | 1 | 1 | 0 | 1 | 0 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 12 | 0 | 1 | 0 |
| Raee | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 2 | 0 | 0 | 45 | 0 | 0 |
| ReutilizacionDonacion | 4 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 | 0 | 37 | 0 |
| Verde | 0 | 0 | 0 | 0 | 3 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 5 |

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
| diverso-12.jpg | diverso | abstención | `pen_marker,pencil` → Negra | no |
| diverso-37.jpg | diverso | abstención | `toy,toy_electronic` → ReutilizacionDonacion | no |
| diverso-48.jpg | diverso | abstención | `flower_pot,plant` → ReutilizacionDonacion | no |
| electronico-03.jpg | electronico | `phone_case` → Negra | `phone` → Raee | no |
| electronico-39.jpg | electronico | `people` → NoEsResiduo | `bluetooth_speaker,speaker_wired` → Raee | no |
| electronico-41.jpg | electronico | `people` → NoEsResiduo | `bluetooth_speaker,speaker_wired` → Raee | no |
| peligroso-03.jpg | especial | `metal_item` → Blanca | `medicine,medicine_bottle,blister_empty` → Medicamentos | no |
| peligroso-46.jpg | especial | abstención | `paint_can,chemical_container` → Peligroso | no |
| peligroso-47.jpg | especial | `metal_item` → Blanca | `paint_can,chemical_container` → Peligroso | no |
| peligroso-51.jpg | especial | `syringe` → Cortopunzante | `aerosol,pesticide_container` → Peligroso | no |
| pilas-17.jpg | especial | `cap_hat` → ReutilizacionDonacion | `lithium_battery,battery` → PilasBaterias | no |
| pilas-20.jpg | especial | `metal_item` → Blanca | `button_battery,battery` → PilasBaterias | no |
| pilas-22.jpg | especial | `metal_item` → Blanca | `button_battery,battery` → PilasBaterias | no |
| pilas-23.jpg | especial | `can` → Blanca | `button_battery,battery` → PilasBaterias | no |
| pilas-24.jpg | especial | `metal_item` → Blanca | `button_battery,battery` → PilasBaterias | no |
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
| sanitario-13.jpg | sanitario | `plastic_bag` → Blanca | `wet_wipes` → Negra | no |
| sanitario-14.jpg | sanitario | `plastic_container` → Blanca | `wet_wipes,chemical_container` → Negra | no |
| sanitario-31.jpg | sanitario | `plastic_container` → Blanca | `cotton_swab` → Negra | no |
| sanitario-33.jpg | sanitario | `plastic_container` → Blanca | `cotton_swab` → Negra | no |
| sanitario-36.jpg | sanitario | `medicine` → Medicamentos | `cotton_swab` → Negra | no |
| vidrio-07.jpg | vidrio | `vase` → ReutilizacionDonacion | `glass_bottle` → Blanca | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- carton-05.jpg (carton): Eco dijo `cardboard`; correcto `cardboard,eggs`.

**Aperturas equivocadas de verde o negra** (estación):

- Ninguna.

