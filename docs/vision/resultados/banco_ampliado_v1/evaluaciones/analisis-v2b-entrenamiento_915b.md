### v2 entrenamiento_915b

| Métrica | Valor |
|---|---|
| Muestras | 302 |
| Clave exacta correcta | 186 (61,6 %) |
| Ruta de disposición correcta | 217 (71,9 %) |
| Falsas admisiones (abriría sin deber) | 1 |
| Ofertas indebidas | 7 |
| Abstenciones | 10 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 5330 ms / 8932 ms |
| **Aperturas automáticas (blanca + estación)** | 51 (10 blanca, 41 verde/negra) |
| **Aperturas equivocadas** | 1 (0 blanca, 1 verde/negra) |
| **Precisión de las decisiones automáticas** | 98,0 % |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 7 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 13 | 25 | 10 | 0,34 | 0,57 | 0,43 |
| Cortopunzante | 7 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Evaluar | 96 | 25 | 31 | 0,79 | 0,76 | 0,77 |
| Medicamentos | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Negra | 37 | 7 | 17 | 0,84 | 0,69 | 0,76 |
| NoEsResiduo | 5 | 4 | 4 | 0,56 | 0,56 | 0,56 |
| Peligroso | 12 | 7 | 3 | 0,63 | 0,80 | 0,71 |
| ReutilizacionDonacion | 29 | 9 | 8 | 0,76 | 0,78 | 0,77 |
| Verde | 14 | 4 | 16 | 0,78 | 0,47 | 0,58 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 0 | 13 | 0 | 5 | 0 | 0 | 0 | 4 | 1 | 0 |
| Cortopunzante | 0 | 0 | 7 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 3 | 12 | 0 | 96 | 1 | 5 | 2 | 2 | 3 | 3 |
| Negra | 4 | 7 | 0 | 3 | 0 | 37 | 1 | 0 | 1 | 1 |
| NoEsResiduo | 0 | 1 | 0 | 0 | 0 | 0 | 5 | 1 | 2 | 0 |
| Peligroso | 0 | 1 | 0 | 2 | 0 | 0 | 0 | 12 | 0 | 0 |
| ReutilizacionDonacion | 0 | 4 | 0 | 2 | 0 | 2 | 0 | 0 | 29 | 0 |
| Verde | 0 | 0 | 0 | 13 | 0 | 0 | 1 | 0 | 2 | 14 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| alata-065.jpg | aprovechable | `aerosol` → Peligroso | `container_with_content,food_in_container` → Evaluar | no |
| alata-070.jpg | aprovechable | `container_with_content` → Evaluar | `can,beer_can,tuna_can` → Blanca | no |
| alata-072.jpg | aprovechable | `aerosol` → Peligroso | `can,beer_can,tuna_can` → Blanca | no |
| alata-096.jpg | aprovechable | `aerosol` → Peligroso | `can,beer_can,tuna_can` → Blanca | no |
| alata-105.jpg | aprovechable | `chemical_container` → Peligroso | `can,beer_can,tuna_can` → Blanca | no |
| alata-145.jpg | aprovechable | `aerosol` → Peligroso | `can,beer_can,tuna_can` → Blanca | no |
| alata-150.jpg | aprovechable | `container_with_content` → Evaluar | `can,beer_can,tuna_can` → Blanca | no |
| alata-153.jpg | aprovechable | `other_animal` → NoEsResiduo | `container_with_content,food_in_container` → Evaluar | no |
| alata-158.jpg | aprovechable | `container_with_content` → Evaluar | `can,beer_can,tuna_can` → Blanca | no |
| apet-092.jpg | aprovechable | `container_with_content` → Evaluar | `plastic_bottle,soda_bottle_big,juice_container,yogurt_bottle` → Blanca | no |
| aplastico-066.jpg | aprovechable | `snack_wrapper` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| aplastico-068.jpg | aprovechable | abstención | `container_with_content,food_in_container` → Evaluar | no |
| aplastico-072.jpg | aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| atetra-067.jpg | aprovechable | `drink_carton` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| atetra-079.jpg | aprovechable | `medicine` → Medicamentos | `container_with_content,food_in_container` → Evaluar | no |
| atetra-090.jpg | aprovechable | `people` → NoEsResiduo | `container_with_content,food_in_container` → Evaluar | no |
| avidrio-098.jpg | aprovechable | `vase` → ReutilizacionDonacion | `glass_bottle,glass_jar_food` → Blanca | no |
| avidrio-100.jpg | aprovechable | `container_with_content` → Evaluar | `glass_bottle,glass_jar_food` → Blanca | no |
| ehigiene-013.jpg | especial | `plastic_container` → Blanca | `shampoo_bottle,deodorant,soap_bar,sunscreen,liquid_soap` → Evaluar | no |
| ehigiene-015.jpg | especial | `plastic_container` → Blanca | `shampoo_bottle,deodorant,soap_bar,sunscreen,liquid_soap` → Evaluar | no |
| ehigiene-018.jpg | especial | `aerosol` → Peligroso | `shampoo_bottle,deodorant,soap_bar,sunscreen,liquid_soap` → Evaluar | no |
| ehigiene-022.jpg | especial | abstención | `shampoo_bottle,deodorant,soap_bar,sunscreen,liquid_soap` → Evaluar | no |
| equimico-023.jpg | especial | `container_with_content` → Evaluar | `paint_can` → Peligroso | no |
| equimico-058.jpg | especial | `container_with_content` → Evaluar | `chemical_container` → Peligroso | no |
| equimico-063.jpg | especial | `metal_item` → Blanca | `chemical_container` → Peligroso | no |
| evaso-004.jpg | especial | `plastic_container` → Blanca | `drinking_glass` → ReutilizacionDonacion | no |
| evaso-006.jpg | especial | `plastic_container` → Blanca | `drinking_glass` → ReutilizacionDonacion | no |
| evaso-016.jpg | especial | `disposable_cup_paper` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| evaso-027.jpg | especial | `mirror_or_ceramic` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| evivo-016.jpg | especial | `flower_pot` → ReutilizacionDonacion | `plant` → NoEsResiduo | no |
| evivo-019.jpg | especial | `jewelry` → ReutilizacionDonacion | `people` → NoEsResiduo | no |
| evivo-020.jpg | especial | `nail_polish` → Peligroso | `people` → NoEsResiduo | no |
| evivo-026.jpg | especial | `metal_item` → Blanca | `people` → NoEsResiduo | no |
| ndesechable-105.jpg | no_aprovechable | abstención | `disposable_cutlery` → Negra | no |
| ndesechable-110.jpg | no_aprovechable | `plastic_container` → Blanca | `disposable_cutlery` → Negra | no |
| ndesechable-123.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-124.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-129.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-132.jpg | no_aprovechable | abstención | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-134.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| nicopor-045.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,styrofoam_food_box,food_in_container` → Evaluar | no |
| nicopor-071.jpg | no_aprovechable | `food_scraps` → Verde | `container_with_content,styrofoam_food_box,food_in_container` → Evaluar | no |
| nicopor-076.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,styrofoam_food_box,food_in_container` → Evaluar | no |
| nicopor-084.jpg | no_aprovechable | `bird` → NoEsResiduo | `styrofoam_food_box,styrofoam` → Negra | no |
| nsanitario-094.jpg | no_aprovechable | abstención | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-105.jpg | no_aprovechable | `plastic_container` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-112.jpg | no_aprovechable | `can` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-152.jpg | no_aprovechable | `plastic_bag` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-157.jpg | no_aprovechable | `plastic_bag` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-160.jpg | no_aprovechable | abstención | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-163.jpg | no_aprovechable | abstención | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-165.jpg | no_aprovechable | `plastic_container` → Blanca | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-172.jpg | no_aprovechable | `mixed_trash` → Evaluar | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nsanitario-178.jpg | no_aprovechable | `food_in_container` → Evaluar | `bandage_used` → Negra | no |
| nsanitario-192.jpg | no_aprovechable | `napkin_clean` → ReutilizacionDonacion | `bandage_used` → Negra | no |
| nservilleta-098.jpg | no_aprovechable | `used_tissue` → Negra | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-108.jpg | no_aprovechable | `container_with_content` → Evaluar | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-109.jpg | no_aprovechable | `mixed_trash` → Evaluar | `used_tissue` → Negra | no |
| nservilleta-115.jpg | no_aprovechable | `paper` → Blanca | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-136.jpg | no_aprovechable | `used_tissue` → Negra | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-174.jpg | no_aprovechable | `paper` → Blanca | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-181.jpg | no_aprovechable | `plastic_bag` → Blanca | `toilet_paper_roll` → Evaluar | no |
| nvarios-074.jpg | no_aprovechable | `food_scraps` → Verde | `chewing_gum` → Negra | no |
| nvarios-083.jpg | no_aprovechable | `paper` → Blanca | `thermal_paper` → Negra | no |
| ocafe-046.jpg | organico | `mixed_trash` → Evaluar | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocafe-061.jpg | organico | `served_food` → Evaluar | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocafe-062.jpg | organico | `food_in_container` → Evaluar | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocascara-087.jpg | organico | `food_in_container` → Evaluar | `food_scraps,fruit_peels_mixed,vegetable_scraps` → Verde | no |
| ocomida-121.jpg | organico | `food_scraps` → Verde | `served_food,fast_food` → Evaluar | no |
| ocomida-123.jpg | organico | `jewelry` → ReutilizacionDonacion | `served_food,fast_food` → Evaluar | no |
| ocomida-128.jpg | organico | `used_tissue` → Negra | `served_food,fast_food` → Evaluar | no |
| ocomida-131.jpg | organico | `jewelry` → ReutilizacionDonacion | `served_food,fast_food` → Evaluar | no |
| ofruta-144.jpg | organico | `plastic_bag` → Blanca | `whole_produce,plantain` → Evaluar | no |
| ohuevo-032.jpg | organico | `eggs` → Evaluar | `food_scraps` → Verde | no |
| ohuevo-048.jpg | organico | `people` → NoEsResiduo | `food_scraps` → Verde | no |
| oresto-087.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-088.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-089.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-091.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-097.jpg | organico | `served_food` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-101.jpg | organico | `cap_hat` → ReutilizacionDonacion | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-104.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-107.jpg | organico | `whole_produce` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-128.jpg | organico | `served_food` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |
| oresto-129.jpg | organico | `ball` → ReutilizacionDonacion | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- Ninguna.

**Aperturas equivocadas de verde o negra** (estación):

- nservilleta-098.jpg (no_aprovechable): abriría Negra; Eco dijo `used_tissue`; correcto `napkin_clean`.

