### entrenamiento (antes)

| Métrica | Valor |
|---|---|
| Muestras | 196 |
| Clave exacta correcta | 129 (65,8 %) |
| Ruta de disposición correcta | 146 (74,5 %) |
| Falsas admisiones (abriría sin deber) | 7 |
| Ofertas indebidas | 5 |
| Abstenciones | 19 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 1424 ms / 4987 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 19 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 51 | 15 | 16 | 0,77 | 0,76 | 0,77 |
| Bombillos | 6 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Cortopunzante | 1 | 0 | 3 | 1,00 | 0,25 | 0,40 |
| Evaluar | 40 | 16 | 7 | 0,71 | 0,85 | 0,78 |
| Medicamentos | 2 | 0 | 5 | 1,00 | 0,29 | 0,44 |
| Negra | 5 | 4 | 12 | 0,56 | 0,29 | 0,38 |
| NoEsResiduo | 4 | 6 | 0 | 0,40 | 1,00 | 0,57 |
| Peligroso | 0 | 0 | 6 | 0,00 | 0,00 | 0,00 |
| PilasBaterias | 3 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Raee | 7 | 0 | 2 | 1,00 | 0,78 | 0,88 |
| ReutilizacionDonacion | 2 | 3 | 0 | 0,40 | 1,00 | 0,57 |
| Verde | 11 | 1 | 13 | 0,92 | 0,46 | 0,61 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Bombillos | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 8 | 51 | 0 | 0 | 2 | 0 | 2 | 3 | 0 | 0 | 0 | 1 | 0 |
| Bombillos | 0 | 0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Cortopunzante | 1 | 1 | 0 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 3 | 1 | 0 | 0 | 40 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 1 |
| Medicamentos | 1 | 4 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 3 | 4 | 0 | 0 | 0 | 0 | 5 | 3 | 0 | 0 | 0 | 2 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 |
| Peligroso | 2 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 |
| Raee | 1 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 7 | 0 | 0 |
| ReutilizacionDonacion | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 |
| Verde | 0 | 1 | 0 | 0 | 12 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 11 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| agricola-06.jpg | agricola | `whole_produce` → Evaluar | `food_scraps,plant_waste` → Verde | no |
| agricola-14.jpg | agricola | `mixed_trash` → Evaluar | `plant_waste,food_scraps` → Verde | no |
| agricola-15.jpg | agricola | abstención | `whole_produce,plant_waste` → Evaluar | no |
| agricola-16.jpg | agricola | abstención | `whole_produce` → Evaluar | no |
| hojas2-04.jpg | agricola | `food_scraps` → Verde | `whole_produce` → Evaluar | no |
| comida2-06.jpg | alimentos | `served_food` → Evaluar | `food_scraps` → Verde | no |
| comida2-08.jpg | alimentos | `plastic_container` → Blanca | `whole_produce` → Evaluar | no |
| restos-23.jpg | alimentos | `served_food` → Evaluar | `food_scraps` → Verde | no |
| restos-24.jpg | alimentos | `served_food` → Evaluar | `food_scraps` → Verde | no |
| restos-33.jpg | alimentos | `eggs` → Evaluar | `food_scraps` → Verde | no |
| restos-35.jpg | alimentos | `plastic_container` → Blanca | `food_scraps` → Verde | no |
| restos-36.jpg | alimentos | `served_food` → Evaluar | `food_scraps` → Verde | no |
| carton-07.jpg | carton | abstención | `cardboard` → Blanca | no |
| carton-08.jpg | carton | `toy` → ReutilizacionDonacion | `cardboard` → Blanca | no |
| carton-16.jpg | carton | abstención | `cardboard,eggs` → Blanca | no |
| carton-18.jpg | carton | abstención | `eggs,cardboard` → Evaluar | no |
| carton-31.jpg | carton | `mixed_trash` → Evaluar | `cardboard` → Blanca | no |
| especial-20.jpg | especial | abstención | `cable,usb_cable` → Raee | no |
| especial-21.jpg | especial | abstención | `medicine,medicine_bottle` → Medicamentos | no |
| especial-23.jpg | especial | `plastic_container` → Blanca | `medicine,medicine_bottle` → Medicamentos | no |
| especial-43.jpg | especial | abstención | `paint_can,chemical_container` → Peligroso | no |
| especial-47.jpg | especial | `other_animal` → NoEsResiduo | `cigarette_butt` → Negra | no |
| especial2-11.jpg | especial | `served_food` → Evaluar | `electronic_other,circuit_board_bare` → Raee | no |
| especial2-17.jpg | especial | `cardboard` → Blanca | `medicine` → Medicamentos | no |
| especial2-18.jpg | especial | `cardboard` → Blanca | `medicine` → Medicamentos | no |
| especial2-19.jpg | especial | `cardboard` → Blanca | `medicine` → Medicamentos | **sí** |
| especial2-30.jpg | especial | `can` → Blanca | `aerosol` → Peligroso | **sí** |
| especial2-33.jpg | especial | `plastic_container` → Blanca | `face_mask,medical_mask` → Negra | no |
| metal-35.jpg | especial | abstención | `aerosol` → Peligroso | no |
| metal-02.jpg | metal | abstención | `can` → Blanca | no |
| metal-03.jpg | metal | `people` → NoEsResiduo | `can` → Blanca | no |
| metal-13.jpg | metal | `face_mask` → Negra | `can` → Blanca | no |
| metal-22.jpg | metal | `people` → NoEsResiduo | `metal_item` → Blanca | no |
| mixto-01.jpg | mixto | abstención | `drink_carton` → Blanca | no |
| mixto-29.jpg | mixto | abstención | `plastic_container,paper,styrofoam` → Blanca | no |
| mixto-30.jpg | mixto | abstención | `plastic_container,paper,styrofoam` → Blanca | no |
| mixto2-07.jpg | mixto | `people` → NoEsResiduo | `styrofoam,styrofoam_food_box` → Negra | no |
| papel2-26.jpg | mixto | abstención | `disposable_cup_paper,plastic_container,paper` → Negra | no |
| varios2-06.jpg | mixto | `horse` → NoEsResiduo | `styrofoam` → Negra | no |
| papel-01.jpg | papel | `whole_produce` → Evaluar | `paper` → Blanca | no |
| sanitario-05.jpg | papel | abstención | `used_tissue,paper,disposable_cutlery` → Negra | no |
| plastico-12.jpg | plastico | abstención | `plastic_container` → Blanca | no |
| plastico-13.jpg | plastico | abstención | `plastic_bag` → Blanca | no |
| plastico-32.jpg | plastico | `table` → ReutilizacionDonacion | `disposable_cutlery` → Negra | no |
| plastico2-08.jpg | plastico | `horse` → NoEsResiduo | `plastic_bottle` → Blanca | no |
| plastico2-16.jpg | plastico | `snack_wrapper` → Negra | `plastic_bag` → Blanca | no |
| vidrio-07.jpg | vidrio | abstención | `mirror_or_ceramic` → Negra | no |
| vidrio-19.jpg | vidrio | `mixed_trash` → Evaluar | `broken_glass,glass_bottle` → Cortopunzante | no |
| vidrio-33.jpg | vidrio | `table` → ReutilizacionDonacion | `mirror_or_ceramic` → Negra | no |
| vidrio2-12.jpg | vidrio | abstención | `broken_glass` → Cortopunzante | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- carton-23.jpg (carton): Eco dijo `cardboard`; correcto `cardboard,greasy_paper`.
- plastico-25.jpg (plastico): Eco dijo `plastic_bottle`; correcto `chemical_container,plastic_bottle,plastic_container`.
- especial-39.jpg (especial): Eco dijo `plastic_bottle`; correcto `chemical_container,bleach_bottle_empty`.
- especial2-19.jpg (especial): Eco dijo `cardboard`; correcto `medicine`.
- especial2-30.jpg (especial): Eco dijo `can`; correcto `aerosol`.

