### prueba (antes)

| Métrica | Valor |
|---|---|
| Muestras | 42 |
| Clave exacta correcta | 31 (73,8 %) |
| Ruta de disposición correcta | 32 (76,2 %) |
| Falsas admisiones (abriría sin deber) | 3 |
| Ofertas indebidas | 2 |
| Abstenciones | 2 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 3581 ms / 4845 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 2 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 15 | 5 | 2 | 0,75 | 0,88 | 0,81 |
| Bombillos | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Cortopunzante | 2 | 0 | 2 | 1,00 | 0,50 | 0,67 |
| Evaluar | 4 | 1 | 1 | 0,80 | 0,80 | 0,80 |
| Medicamentos | 2 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Negra | 3 | 0 | 3 | 1,00 | 0,50 | 0,67 |
| NoEsResiduo | 3 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Peligroso | 0 | 0 | 2 | 0,00 | 0,00 | 0,00 |
| ReutilizacionDonacion | 0 | 2 | 0 | 0,00 | 0,00 | 0,00 |
| Verde | 2 | 0 | 1 | 1,00 | 0,67 | 0,80 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Bombillos | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 1 | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| Cortopunzante | 0 | 0 | 1 | 2 | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| Evaluar | 0 | 1 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 | 0 |
| Medicamentos | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 |
| Negra | 1 | 2 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 |
| Peligroso | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Verde | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 2 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| restos-38.jpg | alimentos | `plastic_container` → Blanca | `served_food` → Evaluar | no |
| carton-36.jpg | carton | `shoes` → ReutilizacionDonacion | `cardboard` → Blanca | no |
| especial2-27.jpg | especial | `can` → Blanca | `aerosol` → Peligroso | no |
| especial2-28.jpg | especial | `can` → Blanca | `aerosol` → Peligroso | **sí** |
| varios-13.jpg | metal | abstención | `can` → Blanca | no |
| sanitario-19.jpg | papel | abstención | `thermal_paper,paper` → Negra | no |
| plastico-29.jpg | plastico | `plastic_container` → Blanca | `disposable_cutlery` → Negra | no |
| vidrio-17.jpg | vidrio | `bulb` → Bombillos | `broken_glass,mirror_or_ceramic` → Cortopunzante | no |
| vidrio-18.jpg | vidrio | `window` → ReutilizacionDonacion | `broken_glass` → Cortopunzante | no |
| vidrio-34.jpg | vidrio | `can` → Blanca | `mirror_or_ceramic` → Negra | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- vidrio-21.jpg (vidrio): Eco dijo `glass_bottle`; correcto `glass_bottle,mirror_or_ceramic`.
- vidrio-22.jpg (vidrio): Eco dijo `glass_bottle`; correcto `glass_bottle,mirror_or_ceramic`.
- especial2-28.jpg (especial): Eco dijo `can`; correcto `aerosol`.

