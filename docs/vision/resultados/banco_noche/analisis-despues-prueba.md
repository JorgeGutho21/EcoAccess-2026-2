### prueba (despues)

| Métrica | Valor |
|---|---|
| Muestras | 42 |
| Clave exacta correcta | 32 (76,2 %) |
| Ruta de disposición correcta | 35 (83,3 %) |
| Falsas admisiones (abriría sin deber) | 1 |
| Ofertas indebidas | 1 |
| Abstenciones | 0 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 994 ms / 4701 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| Blanca | 15 | 2 | 2 | 0,88 | 0,88 | 0,88 |
| Bombillos | 0 | 1 | 0 | 0,00 | 0,00 | 0,00 |
| Cortopunzante | 2 | 0 | 2 | 1,00 | 0,50 | 0,67 |
| Evaluar | 4 | 1 | 1 | 0,80 | 0,80 | 0,80 |
| Medicamentos | 2 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Negra | 5 | 0 | 1 | 1,00 | 0,83 | 0,91 |
| NoEsResiduo | 3 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Peligroso | 1 | 0 | 1 | 1,00 | 0,50 | 0,67 |
| ReutilizacionDonacion | 0 | 4 | 0 | 0,00 | 0,00 | 0,00 |
| Verde | 2 | 0 | 1 | 1,00 | 0,67 | 0,80 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | Blanca | Bombillos | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | Peligroso | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 15 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 |
| Cortopunzante | 0 | 1 | 2 | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| Evaluar | 1 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 | 0 |
| Medicamentos | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 |
| Negra | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 0 | 1 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 3 | 0 | 0 | 0 |
| Peligroso | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 |
| Verde | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 2 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| restos-38.jpg | alimentos | `plastic_container` → Blanca | `served_food` → Evaluar | no |
| carton-36.jpg | carton | `shoes` → ReutilizacionDonacion | `cardboard` → Blanca | no |
| especial2-27.jpg | especial | `can` → Blanca | `aerosol` → Peligroso | no |
| especial2-35.jpg | especial | `cap_hat` → ReutilizacionDonacion | `face_mask,medical_mask` → Negra | no |
| vidrio-17.jpg | vidrio | `bulb` → Bombillos | `broken_glass,mirror_or_ceramic` → Cortopunzante | no |
| vidrio-18.jpg | vidrio | `window` → ReutilizacionDonacion | `broken_glass` → Cortopunzante | no |
| vidrio-22.jpg | vidrio | `drinking_glass` → ReutilizacionDonacion | `glass_bottle,mirror_or_ceramic` → Blanca | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- vidrio-21.jpg (vidrio): Eco dijo `glass_bottle`; correcto `glass_bottle,mirror_or_ceramic`.

