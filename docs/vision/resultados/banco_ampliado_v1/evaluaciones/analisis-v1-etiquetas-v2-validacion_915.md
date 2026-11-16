### v1b validacion_915

| Métrica | Valor |
|---|---|
| Muestras | 171 |
| Clave exacta correcta | 101 (59,1 %) |
| Ruta de disposición correcta | 124 (72,5 %) |
| Falsas admisiones (abriría sin deber) | 0 |
| Ofertas indebidas | 2 |
| Abstenciones | 5 |
| Fallos técnicos | 1 |
| Latencia mediana / p95 | 3458 ms / 9130 ms |
| **Aperturas automáticas (blanca + estación)** | 28 (7 blanca, 21 verde/negra) |
| **Aperturas equivocadas** | 5 (0 blanca, 5 verde/negra) |
| **Precisión de las decisiones automáticas** | 82,1 % |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 5 | 0 | 0,00 | 0,00 | 0,00 |
| AceiteUsado | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 16 | 12 | 8 | 0,57 | 0,67 | 0,62 |
| Cortopunzante | 4 | 0 | 2 | 1,00 | 0,67 | 0,80 |
| Evaluar | 31 | 11 | 14 | 0,74 | 0,69 | 0,71 |
| Medicamentos | 2 | 1 | 0 | 0,67 | 1,00 | 0,80 |
| Negra | 9 | 7 | 10 | 0,56 | 0,47 | 0,51 |
| NoEsResiduo | 4 | 3 | 0 | 0,57 | 1,00 | 0,73 |
| Peligroso | 6 | 1 | 0 | 0,86 | 1,00 | 0,92 |
| PilasBaterias | 7 | 0 | 1 | 1,00 | 0,88 | 0,93 |
| Raee | 17 | 1 | 0 | 0,94 | 1,00 | 0,97 |
| ReutilizacionDonacion | 9 | 2 | 5 | 0,82 | 0,64 | 0,72 |
| Verde | 19 | 3 | 7 | 0,86 | 0,73 | 0,79 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | AceiteUsado | Blanca | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 2 | 0 | 16 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| Cortopunzante | 1 | 0 | 1 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 1 | 1 | 3 | 0 | 31 | 0 | 5 | 0 | 1 | 0 | 0 | 1 | 2 |
| Medicamentos | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 0 | 0 | 7 | 0 | 1 | 1 | 9 | 0 | 0 | 0 | 0 | 0 | 1 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 |
| Peligroso | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 7 | 1 | 0 | 0 |
| Raee | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 17 | 0 | 0 |
| ReutilizacionDonacion | 1 | 0 | 1 | 0 | 0 | 0 | 2 | 1 | 0 | 0 | 0 | 9 | 0 |
| Verde | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 19 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| abolsa-033.jpg | aprovechable | abstención | `plastic_bag,water_bag,milk_bag,bread_bag` → Blanca | no |
| acarton-065.jpg | aprovechable | abstención | `cardboard,cereal_box` → Blanca | no |
| alata-013.jpg | aprovechable | `container_with_content` → Evaluar | `can,beer_can,tuna_can` → Blanca | no |
| alata-073.jpg | aprovechable | `aerosol` → Peligroso | `container_with_content,food_in_container` → Evaluar | no |
| alata-148.jpg | aprovechable | `container_with_content` → Evaluar | `can,beer_can,tuna_can` → Blanca | no |
| apet-078.jpg | aprovechable | `container_with_content` → Evaluar | `plastic_bottle,soda_bottle_big,juice_container,yogurt_bottle` → Blanca | no |
| apet-159.jpg | aprovechable | `cooking_oil` → AceiteUsado | `container_with_content,food_in_container` → Evaluar | no |
| apet-179.jpg | aprovechable | `cap_hat` → ReutilizacionDonacion | `plastic_bottle,soda_bottle_big,juice_container,yogurt_bottle` → Blanca | no |
| aplastico-059.jpg | aprovechable | `ice_cream_stick` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| avidrio-090.jpg | aprovechable | `container_with_content` → Evaluar | `glass_bottle,glass_jar_food` → Blanca | no |
| avidrio-093.jpg | aprovechable | `container_with_content` → Evaluar | `glass_bottle,glass_jar_food` → Blanca | no |
| ecortante-018.jpg | especial | abstención | `syringe,disposable_razor,scalpel` → Cortopunzante | no |
| ehigiene-002.jpg | especial | `plastic_bag` → Blanca | `shampoo_bottle,deodorant,soap_bar,sunscreen,liquid_soap` → Evaluar | no |
| ejuguete-022.jpg | especial | `people` → NoEsResiduo | `toy,doll_stuffed,book` → ReutilizacionDonacion | no |
| ejuguete-026.jpg | especial | `pen_marker` → Negra | `pencil` → ReutilizacionDonacion | no |
| ejuguete-034.jpg | especial | abstención | `eraser` → ReutilizacionDonacion | no |
| elitio-014.jpg | especial | `sim_card` → Raee | `lithium_battery,battery` → PilasBaterias | no |
| evaso-010.jpg | especial | `plastic_container` → Blanca | `drinking_glass` → ReutilizacionDonacion | no |
| ndesechable-068.jpg | no_aprovechable | `container_with_content` → Evaluar | `disposable_cup_paper,disposable_cutlery,paper_plate,greasy_paper,cardboard_greasy` → Negra | no |
| ndesechable-092.jpg | no_aprovechable | `food_scraps` → Verde | `food_in_container` → Evaluar | no |
| ndesechable-114.jpg | no_aprovechable | `plastic_container` → Blanca | `disposable_cutlery` → Negra | no |
| ndesechable-133.jpg | no_aprovechable | `disposable_cup_paper` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-164.jpg | no_aprovechable | `plastic_container` → Blanca | `disposable_cutlery` → Negra | no |
| nicopor-059.jpg | no_aprovechable | `ball` → ReutilizacionDonacion | `container_with_content,styrofoam_food_box,food_in_container` → Evaluar | no |
| nicopor-061.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,styrofoam_food_box,food_in_container` → Evaluar | no |
| nicopor-066.jpg | no_aprovechable | `food_scraps` → Verde | `container_with_content,styrofoam_food_box,food_in_container` → Evaluar | no |
| nmecato-027.jpg | no_aprovechable | `snack_wrapper` → Negra | `served_food` → Evaluar | no |
| nmecato-030.jpg | no_aprovechable | `snack_wrapper` → Negra | `served_food` → Evaluar | no |
| nmecato-043.jpg | no_aprovechable | `food_scraps` → Verde | `snack_wrapper,candy_wrapper` → Negra | no |
| nsanitario-068.jpg | no_aprovechable | `plastic_container` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-154.jpg | no_aprovechable | `medicine` → Medicamentos | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-161.jpg | no_aprovechable | `plastic_bag` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-164.jpg | no_aprovechable | `plastic_bag` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nservilleta-011.jpg | no_aprovechable | `used_tissue` → Negra | `napkin_clean` → ReutilizacionDonacion | no |
| nvarios-004.jpg | no_aprovechable | `plastic_container` → Blanca | `chewing_gum,ceramic_plate,pen_marker,thermal_paper,sponge` → Negra | no |
| nvarios-037.jpg | no_aprovechable | `glass_bottle` → Blanca | `broken_glass` → Cortopunzante | no |
| nvarios-076.jpg | no_aprovechable | `paper` → Blanca | `thermal_paper` → Negra | no |
| ocafe-017.jpg | organico | `served_food` → Evaluar | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocascara-084.jpg | organico | `whole_produce` → Evaluar | `food_scraps,fruit_peels_mixed,vegetable_scraps` → Verde | no |
| ocascara-085.jpg | organico | `other_animal` → NoEsResiduo | `food_scraps,fruit_peels_mixed,vegetable_scraps` → Verde | no |
| ocomida-015.jpg | organico | `snack_wrapper` → Negra | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ocomida-127.jpg | organico | `plastic_container` → Blanca | `served_food,fast_food` → Evaluar | no |
| ofruta-028.jpg | organico | `flower` → NoEsResiduo | `food_scraps` → Verde | no |
| ofruta-056.jpg | organico | abstención | `whole_produce,plantain` → Evaluar | no |
| oresto-002.jpg | organico | `served_food` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-062.jpg | organico | `served_food` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-116.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- Ninguna.

**Aperturas equivocadas de verde o negra** (estación):

- ndesechable-039.jpg (no_aprovechable): abriría Verde; Eco dijo `served_food`; correcto `food_in_container`.
- ndesechable-084.jpg (no_aprovechable): abriría Verde; Eco dijo `served_food`; correcto `food_in_container`.
- ndesechable-088.jpg (no_aprovechable): abriría Verde; Eco dijo `served_food`; correcto `food_in_container`.
- nicopor-073.jpg (no_aprovechable): abriría Verde; Eco dijo `served_food`; correcto `container_with_content,styrofoam_food_box,food_in_container`.
- nicopor-077.jpg (no_aprovechable): abriría Verde; Eco dijo `served_food`; correcto `container_with_content,styrofoam_food_box,food_in_container`.

