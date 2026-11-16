### contenido2-entrenamiento (antes, estimado)

| Métrica | Valor |
|---|---|
| Muestras | 196 |
| Clave exacta correcta | 137 (69,9 %) |
| Ruta de disposición correcta | 157 (80,1 %) |
| Falsas admisiones (abriría sin deber) | 0 |
| Ofertas indebidas | 4 |
| Abstenciones | 15 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 1094 ms / 5486 ms |
| **Aperturas automáticas (blanca + estación)** | 76 (19 blanca, 57 verde/negra) |
| **Aperturas equivocadas** | 4 (0 blanca, 4 verde/negra) |
| **Precisión de las decisiones automáticas** | 94,7 % |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 11 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 45 | 7 | 22 | 0,87 | 0,67 | 0,76 |
| Bombillos | 6 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Cortopunzante | 2 | 0 | 2 | 1,00 | 0,50 | 0,67 |
| Evaluar | 42 | 14 | 5 | 0,75 | 0,89 | 0,82 |
| Medicamentos | 4 | 0 | 3 | 1,00 | 0,57 | 0,73 |
| Negra | 10 | 9 | 7 | 0,53 | 0,59 | 0,56 |
| NoEsResiduo | 4 | 5 | 0 | 0,44 | 1,00 | 0,62 |
| Peligroso | 5 | 0 | 1 | 1,00 | 0,83 | 0,91 |
| PilasBaterias | 3 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Raee | 7 | 0 | 2 | 1,00 | 0,78 | 0,88 |
| ReutilizacionDonacion | 2 | 7 | 0 | 0,22 | 1,00 | 0,36 |
| Verde | 13 | 0 | 11 | 1,00 | 0,54 | 0,70 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Bombillos | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 5 | 45 | 0 | 0 | 2 | 0 | 7 | 3 | 0 | 0 | 0 | 5 | 0 |
| Bombillos | 0 | 0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Cortopunzante | 0 | 1 | 0 | 2 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 1 | 1 | 0 | 0 | 42 | 0 | 2 | 0 | 0 | 0 | 0 | 1 | 0 |
| Medicamentos | 1 | 2 | 0 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 2 | 2 | 0 | 0 | 0 | 0 | 10 | 2 | 0 | 0 | 0 | 1 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 |
| Peligroso | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 |
| Raee | 1 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 7 | 0 | 0 |
| ReutilizacionDonacion | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 |
| Verde | 0 | 1 | 0 | 0 | 10 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 13 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| agricola-06.jpg | agricola | `whole_produce` → Evaluar | `food_scraps,plant_waste` → Verde | no |
| agricola-14.jpg | agricola | `mixed_trash` → Evaluar | `plant_waste,food_scraps` → Verde | no |
| hojas2-04.jpg | agricola | `ball` → ReutilizacionDonacion | `whole_produce` → Evaluar | no |
| comida2-06.jpg | alimentos | `served_food` → Evaluar | `food_scraps` → Verde | no |
| comida2-08.jpg | alimentos | `plastic_container` → Blanca | `whole_produce` → Evaluar | no |
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
| especial2-18.jpg | especial | `cardboard` → Blanca | `medicine` → Medicamentos | no |
| especial2-33.jpg | especial | `plastic_container` → Blanca | `face_mask,medical_mask` → Negra | no |
| metal-02.jpg | metal | abstención | `can` → Blanca | no |
| metal-03.jpg | metal | `people` → NoEsResiduo | `can` → Blanca | no |
| metal-13.jpg | metal | `face_mask` → Negra | `can` → Blanca | no |
| metal-22.jpg | metal | `people` → NoEsResiduo | `metal_item` → Blanca | no |
| mixto-01.jpg | mixto | abstención | `drink_carton` → Blanca | no |
| papel2-26.jpg | mixto | abstención | `disposable_cup_paper,plastic_container,paper` → Negra | no |
| varios2-06.jpg | mixto | `horse` → NoEsResiduo | `styrofoam` → Negra | no |
| papel-01.jpg | papel | `whole_produce` → Evaluar | `paper` → Blanca | no |
| papel2-24.jpg | papel | `book` → ReutilizacionDonacion | `paper` → Blanca | no |
| sanitario-09.jpg | papel | `ball` → ReutilizacionDonacion | `used_tissue` → Negra | no |
| plastico-05.jpg | plastico | `cap_hat` → ReutilizacionDonacion | `plastic_container` → Blanca | no |
| plastico-12.jpg | plastico | abstención | `plastic_container` → Blanca | no |
| plastico-13.jpg | plastico | `cap_hat` → ReutilizacionDonacion | `plastic_bag` → Blanca | no |
| plastico-37.jpg | plastico | `cap_hat` → ReutilizacionDonacion | `plastic_container` → Blanca | no |
| plastico2-08.jpg | plastico | `horse` → NoEsResiduo | `plastic_bottle` → Blanca | no |
| plastico2-16.jpg | plastico | `snack_wrapper` → Negra | `plastic_bag` → Blanca | no |
| vidrio-07.jpg | vidrio | abstención | `mirror_or_ceramic` → Negra | no |
| vidrio-19.jpg | vidrio | `mixed_trash` → Evaluar | `broken_glass,glass_bottle` → Cortopunzante | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- Ninguna.

**Aperturas equivocadas de verde o negra** (estación):

- papel-01.jpg (papel): abriría Verde; Eco dijo `whole_produce`; correcto `paper`.
- metal-13.jpg (metal): abriría Negra; Eco dijo `face_mask`; correcto `can`.
- plastico2-16.jpg (plastico): abriría Negra; Eco dijo `snack_wrapper`; correcto `plastic_bag`.
- especial2-11.jpg (especial): abriría Verde; Eco dijo `served_food`; correcto `electronic_other,circuit_board_bare`.

