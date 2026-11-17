### v1b prueba_915

| Métrica | Valor |
|---|---|
| Muestras | 139 |
| Clave exacta correcta | 86 (61,9 %) |
| Ruta de disposición correcta | 100 (71,9 %) |
| Falsas admisiones (abriría sin deber) | 0 |
| Ofertas indebidas | 2 |
| Abstenciones | 8 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 3467 ms / 9117 ms |
| **Aperturas automáticas (blanca + estación)** | 25 (4 blanca, 21 verde/negra) |
| **Aperturas equivocadas** | 3 (0 blanca, 3 verde/negra) |
| **Precisión de las decisiones automáticas** | 88,0 % |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 5 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 8 | 9 | 3 | 0,47 | 0,73 | 0,57 |
| Bombillos | 2 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Cortopunzante | 5 | 0 | 1 | 1,00 | 0,83 | 0,91 |
| Evaluar | 37 | 10 | 12 | 0,79 | 0,76 | 0,77 |
| Medicamentos | 2 | 1 | 0 | 0,67 | 1,00 | 0,80 |
| Negra | 18 | 7 | 3 | 0,72 | 0,86 | 0,78 |
| NoEsResiduo | 2 | 3 | 0 | 0,40 | 1,00 | 0,57 |
| Peligroso | 1 | 2 | 4 | 0,33 | 0,20 | 0,25 |
| PilasBaterias | 6 | 0 | 2 | 1,00 | 0,75 | 0,86 |
| Raee | 5 | 1 | 0 | 0,83 | 1,00 | 0,91 |
| ReutilizacionDonacion | 5 | 1 | 5 | 0,83 | 0,50 | 0,62 |
| Verde | 8 | 1 | 10 | 0,89 | 0,44 | 0,59 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Bombillos | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 0 | 8 | 0 | 0 | 1 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| Bombillos | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Cortopunzante | 0 | 0 | 0 | 5 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 |
| Evaluar | 2 | 3 | 0 | 0 | 37 | 1 | 2 | 0 | 2 | 0 | 1 | 0 | 1 |
| Medicamentos | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 0 | 1 | 0 | 0 | 1 | 0 | 18 | 1 | 0 | 0 | 0 | 0 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 |
| Peligroso | 1 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 1 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 6 | 0 | 0 | 0 |
| Raee | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 |
| ReutilizacionDonacion | 1 | 1 | 0 | 0 | 0 | 0 | 2 | 1 | 0 | 0 | 0 | 5 | 0 |
| Verde | 1 | 3 | 0 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 8 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| alata-062.jpg | aprovechable | `aerosol` → Peligroso | `container_with_content,food_in_container` → Evaluar | no |
| alata-080.jpg | aprovechable | `aerosol` → Peligroso | `container_with_content,food_in_container` → Evaluar | no |
| alata-113.jpg | aprovechable | `cigarette_butt` → Negra | `can,beer_can,tuna_can` → Blanca | no |
| apet-040.jpg | aprovechable | `container_with_content` → Evaluar | `chemical_container` → Peligroso | no |
| apet-163.jpg | aprovechable | `container_with_content` → Evaluar | `plastic_bottle,soda_bottle_big,juice_container,yogurt_bottle` → Blanca | no |
| aplastico-018.jpg | aprovechable | `styrofoam` → Negra | `plastic_container` → Blanca | no |
| aplastico-036.jpg | aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| aplastico-060.jpg | aprovechable | abstención | `container_with_content,food_in_container` → Evaluar | no |
| atetra-061.jpg | aprovechable | `medicine` → Medicamentos | `container_with_content,food_in_container` → Evaluar | no |
| eaccesorio-018.jpg | especial | abstención | `handbag` → ReutilizacionDonacion | no |
| ecortante-026.jpg | especial | `people` → NoEsResiduo | `syringe,disposable_razor,scalpel` → Cortopunzante | no |
| ehigiene-016.jpg | especial | abstención | `shampoo_bottle,deodorant,soap_bar,sunscreen,liquid_soap` → Evaluar | no |
| epila-013.jpg | especial | `pen_marker` → Negra | `battery` → PilasBaterias | no |
| epila-047.jpg | especial | `metal_item` → Blanca | `battery` → PilasBaterias | no |
| equimico-020.jpg | especial | `container_with_content` → Evaluar | `paint_can` → Peligroso | no |
| equimico-025.jpg | especial | `container_with_content` → Evaluar | `paint_can` → Peligroso | no |
| equimico-078.jpg | especial | abstención | `motor_oil,chemical_container` → Peligroso | no |
| ndesechable-014.jpg | no_aprovechable | `disposable_cup_paper` → Negra | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-130.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| ndesechable-143.jpg | no_aprovechable | `plastic_container` → Blanca | `container_with_content,food_in_container` → Evaluar | no |
| nsanitario-153.jpg | no_aprovechable | `dog` → NoEsResiduo | `face_mask,medical_mask,gloves,rubber_glove,diaper_or_pad,cotton_swab,wet_wipes` → Negra | no |
| nservilleta-001.jpg | no_aprovechable | `used_tissue` → Negra | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-022.jpg | no_aprovechable | `used_tissue` → Negra | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-024.jpg | no_aprovechable | `paper` → Blanca | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-104.jpg | no_aprovechable | `flower` → NoEsResiduo | `napkin_clean` → ReutilizacionDonacion | no |
| nservilleta-162.jpg | no_aprovechable | `plastic_container` → Blanca | `used_tissue` → Negra | no |
| nservilleta-179.jpg | no_aprovechable | `electronic_other` → Raee | `toilet_paper_roll` → Evaluar | no |
| nvarios-071.jpg | no_aprovechable | `mixed_trash` → Evaluar | `chewing_gum` → Negra | no |
| ocafe-014.jpg | organico | `plastic_container` → Blanca | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocafe-023.jpg | organico | abstención | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocafe-051.jpg | organico | `drinking_glass` → ReutilizacionDonacion | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocafe-055.jpg | organico | `plastic_container` → Blanca | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocafe-074.jpg | organico | `mixed_trash` → Evaluar | `food_scraps,tea_bag,coffee_filter` → Verde | no |
| ocascara-067.jpg | organico | `whole_produce` → Evaluar | `food_scraps,fruit_peels_mixed,vegetable_scraps` → Verde | no |
| ocascara-071.jpg | organico | `served_food` → Evaluar | `food_scraps,fruit_peels_mixed,vegetable_scraps` → Verde | no |
| ocascara-082.jpg | organico | `whole_produce` → Evaluar | `food_scraps,fruit_peels_mixed,vegetable_scraps` → Verde | no |
| ocomida-017.jpg | organico | `food_scraps` → Verde | `served_food,served_fish,arepa,fast_food,beans_lentils` → Evaluar | no |
| ohuevo-012.jpg | organico | `plastic_container` → Blanca | `food_scraps` → Verde | no |
| oresto-029.jpg | organico | `mixed_trash` → Evaluar | `food_scraps,vegetable_scraps,rice_leftovers,pizza_crust,chicken_leftovers` → Verde | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- Ninguna.

**Aperturas equivocadas de verde o negra** (estación):

- ndesechable-085.jpg (no_aprovechable): abriría Verde; Eco dijo `served_food`; correcto `food_in_container`.
- aplastico-069.jpg (aprovechable): abriría Verde; Eco dijo `served_food`; correcto `container_with_content,food_in_container`.
- nicopor-052.jpg (no_aprovechable): abriría Verde; Eco dijo `served_food`; correcto `container_with_content,styrofoam_food_box,food_in_container`.

