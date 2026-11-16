### v2 entrenamiento_915a

| Métrica | Valor |
|---|---|
| Muestras | 303 |
| Clave exacta correcta | 213 (70,3 %) |
| Ruta de disposición correcta | 240 (79,2 %) |
| Falsas admisiones (abriría sin deber) | 0 |
| Ofertas indebidas | 3 |
| Abstenciones | 15 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 3732 ms / 8691 ms |
| **Aperturas automáticas (blanca + estación)** | 45 (6 blanca, 39 verde/negra) |
| **Aperturas equivocadas** | 0 (0 blanca, 0 verde/negra) |
| **Precisión de las decisiones automáticas** | 100,0 % |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 14 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 22 | 6 | 13 | 0,79 | 0,63 | 0,70 |
| Bombillos | 4 | 0 | 1 | 1,00 | 0,80 | 0,89 |
| Cortopunzante | 1 | 1 | 2 | 0,50 | 0,33 | 0,40 |
| Evaluar | 65 | 18 | 16 | 0,78 | 0,80 | 0,79 |
| Medicamentos | 9 | 3 | 3 | 0,75 | 0,75 | 0,75 |
| Negra | 33 | 4 | 12 | 0,89 | 0,73 | 0,80 |
| NoEsResiduo | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Peligroso | 0 | 1 | 1 | 0,00 | 0,00 | 0,00 |
| PilasBaterias | 28 | 0 | 5 | 1,00 | 0,85 | 0,92 |
| Raee | 37 | 3 | 2 | 0,93 | 0,95 | 0,94 |
| ReutilizacionDonacion | 5 | 7 | 0 | 0,42 | 1,00 | 0,59 |
| Verde | 36 | 5 | 8 | 0,88 | 0,82 | 0,85 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Bombillos | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 3 | 22 | 0 | 0 | 7 | 0 | 2 | 0 | 0 | 0 | 0 | 1 | 0 |
| Bombillos | 0 | 0 | 4 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Cortopunzante | 2 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 2 | 2 | 0 | 0 | 65 | 1 | 2 | 0 | 0 | 0 | 0 | 4 | 5 |
| Medicamentos | 0 | 0 | 0 | 0 | 2 | 9 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| Negra | 6 | 3 | 0 | 1 | 1 | 1 | 33 | 0 | 0 | 0 | 0 | 0 | 0 |
| Peligroso | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 28 | 3 | 1 | 0 |
| Raee | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 37 | 1 | 0 |
| ReutilizacionDonacion | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 0 |
| Verde | 0 | 0 | 0 | 0 | 7 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 36 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| acarton-010.jpg | aprovechable | `container_with_content` → Evaluar | `cardboard,cereal_box` → Blanca | no |
| acarton-058.jpg | aprovechable | abstención | `cardboard,cereal_box` → Blanca | no |
| alata-004.jpg | aprovechable | `container_with_content` → Evaluar | `can,beer_can,tuna_can` → Blanca | no |
| ametal-007.jpg | aprovechable | `ball` → ReutilizacionDonacion | `metal_item,aluminum_tray,metal_lid,crown_cap` → Blanca | no |
| ametal-009.jpg | aprovechable | `food_in_container` → Evaluar | `metal_item,aluminum_tray,metal_lid,crown_cap` → Blanca | no |
| ametal-010.jpg | aprovechable | abstención | `metal_item,aluminum_tray,metal_lid,crown_cap` → Blanca | no |
| ametal-020.jpg | aprovechable | `snack_wrapper` → Negra | `metal_item,aluminum_tray,metal_lid,crown_cap` → Blanca | no |
| apapel-032.jpg | aprovechable | `cigarette_butt` → Negra | `paper,printer_paper_used,magazine_glossy` → Blanca | no |
| apapel-039.jpg | aprovechable | abstención | `paper,printer_paper_used,magazine_glossy` → Blanca | no |
| apet-031.jpg | aprovechable | `container_with_content` → Evaluar | `plastic_bottle,soda_bottle_big,juice_container,yogurt_bottle` → Blanca | no |
| apet-036.jpg | aprovechable | `container_with_content` → Evaluar | `chemical_container` → Peligroso | no |
| apet-047.jpg | aprovechable | `medicine_bottle` → Medicamentos | `container_with_content,food_in_container` → Evaluar | no |
| apet-060.jpg | aprovechable | `container_with_content` → Evaluar | `glass_bottle` → Blanca | no |
| apet-072.jpg | aprovechable | `container_with_content` → Evaluar | `glass_bottle` → Blanca | no |
| aplastico-005.jpg | aprovechable | abstención | `oil_bottle_kitchen` → Negra | no |
| aplastico-016.jpg | aprovechable | abstención | `container_with_content,food_in_container` → Evaluar | no |
| atetra-025.jpg | aprovechable | `container_with_content` → Evaluar | `drink_carton` → Blanca | no |
| avidrio-057.jpg | aprovechable | `drinking_glass` → ReutilizacionDonacion | `container_with_content,food_in_container` → Evaluar | no |
| avidrio-058.jpg | aprovechable | `drinking_glass` → ReutilizacionDonacion | `container_with_content,food_in_container` → Evaluar | no |
| eaudifono-007.jpg | especial | abstención | `headphones,wireless_earbuds,wired_earphones` → Raee | no |
| ebombillo-012.jpg | especial | `medicine` → Medicamentos | `bulb` → Bombillos | no |
| elitio-002.jpg | especial | `sim_card` → Raee | `lithium_battery,battery` → PilasBaterias | no |
| elitio-004.jpg | especial | `phone` → Raee | `lithium_battery,battery` → PilasBaterias | no |
| elitio-007.jpg | especial | `phone` → Raee | `lithium_battery,battery` → PilasBaterias | no |
| emedicina-024.jpg | especial | `container_with_content` → Evaluar | `medicine,blister_empty,medicine_bottle` → Medicamentos | no |
| emedicina-026.jpg | especial | `container_with_content` → Evaluar | `medicine,blister_empty,medicine_bottle` → Medicamentos | no |
| emedicina-053.jpg | especial | `chemical_container` → Peligroso | `inhaler` → Medicamentos | no |
| eperiferico-011.jpg | especial | `toy` → ReutilizacionDonacion | `computer_mouse,keyboard,tv_remote,usb_drive` → Raee | no |
| epila-031.jpg | especial | `metal_item` → Blanca | `battery` → PilasBaterias | no |
| epila-036.jpg | especial | `cap_hat` → ReutilizacionDonacion | `battery` → PilasBaterias | no |
| ncolilla-008.jpg | no_aprovechable | `can` → Blanca | `cigarette_butt` → Negra | no |
| ndesechable-029.jpg | no_aprovechable | `disposable_cup_paper` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-035.jpg | no_aprovechable | abstención | `disposable_cup_paper,disposable_cutlery,paper_plate,greasy_paper,cardboard_greasy` → Negra | no |
| ndesechable-036.jpg | no_aprovechable | `kitchen_knife` → Cortopunzante | `disposable_cup_paper,disposable_cutlery,paper_plate,greasy_paper,cardboard_greasy` → Negra | no |
| nicopor-014.jpg | no_aprovechable | abstención | `styrofoam,styrofoam_food_box,cooler_box` → Negra | no |
| nicopor-031.jpg | no_aprovechable | abstención | `styrofoam,styrofoam_food_box,cooler_box` → Negra | no |
| nmecato-004.jpg | no_aprovechable | `plastic_container` → Blanca | `snack_wrapper,candy_wrapper` → Negra | no |
| nmecato-008.jpg | no_aprovechable | `medicine` → Medicamentos | `snack_wrapper,candy_wrapper` → Negra | no |
| nmecato-036.jpg | no_aprovechable | `food_scraps` → Verde | `served_food` → Evaluar | no |
| nsanitario-018.jpg | no_aprovechable | `container_with_content` → Evaluar | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-088.jpg | no_aprovechable | abstención | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-090.jpg | no_aprovechable | `plastic_bag` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nservilleta-083.jpg | no_aprovechable | `napkin_clean` → ReutilizacionDonacion | `toilet_paper_roll` → Evaluar | no |
| nvarios-012.jpg | no_aprovechable | abstención | `chewing_gum,ceramic_plate,pen_marker,thermal_paper,sponge` → Negra | no |
| nvarios-033.jpg | no_aprovechable | abstención | `broken_glass` → Cortopunzante | no |
| nvarios-035.jpg | no_aprovechable | abstención | `broken_glass` → Cortopunzante | no |
| ocascara-073.jpg | organico | `food_in_container` → Evaluar | `food_scraps,fruit_peels_mixed,vegetable_scraps` → Verde | no |
| ocomida-002.jpg | organico | `food_scraps` → Verde | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ocomida-018.jpg | organico | `plastic_container` → Blanca | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ocomida-021.jpg | organico | `food_scraps` → Verde | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ocomida-056.jpg | organico | abstención | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ocomida-058.jpg | organico | `plant_waste` → Verde | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ocomida-059.jpg | organico | `snack_wrapper` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| ocomida-072.jpg | organico | `plastic_container` → Blanca | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ofruta-033.jpg | organico | `table` → ReutilizacionDonacion | `whole_produce,plantain` → Evaluar | no |
| ofruta-064.jpg | organico | `food_scraps` → Verde | `whole_produce,plantain` → Evaluar | no |
| ohuevo-010.jpg | organico | `eggs` → Evaluar | `food_scraps` → Verde | no |
| ojardin-016.jpg | organico | `tree` → NoEsResiduo | `plant_waste,cut_flowers` → Verde | no |
| oresto-005.jpg | organico | `served_food` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-022.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-033.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-055.jpg | organico | `served_food` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-077.jpg | organico | `mixed_trash` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- Ninguna.

**Aperturas equivocadas de verde o negra** (estación):

- Ninguna.

