# Banco de imágenes y evaluación antes/después (misión nocturna y final, 12/11/2026)

## Resumen

| Dato | Valor |
|---|---|
| Fotos candidatas descargadas | **710** de Wikimedia Commons, con licencia CC0, CC BY, CC BY-SA o dominio público. Procedencia en `resultados/banco_noche/fuentes.json`: título, licencia, autor, URL y búsqueda |
| Fotos etiquetadas y usadas | **281**: 271 de un objeto y 10 con varios residuos |
| Fotos descartadas | 429: dibujos, pinturas, paisajes, escenas sin un residuo claro, duplicados casi iguales. El motivo está escrito en `etiquetas.txt` |
| Separación | Por **autor** (hash SHA-256 del autor): **196** entrenamiento, **33** validación y **42** prueba independiente. Una misma sesión fotográfica nunca queda en dos conjuntos |
| Etiquetado | A ojo, sobre hojas de contacto, **antes** de ejecutar el modelo |
| Modelo | `lfm3-vl:latest` (1,7 B parámetros, GGUF Q4_K_M) en Ollama local. Sin nube |
| Antes | commit `edcbc13`, el código de la rama al empezar la noche |
| Después | rama `desarrollo/mision-nocturna-ia`: descriptor, catálogo de 307 tipos, matriz y reglas |
| Integridad | SHA-256 de cada foto en los manifiestos |

**Meta de 330 fotos (y de 400 de evaluación): NO alcanzada.**
- Quedaron 281 utilizables, de las cuales 75 son de validación y prueba.
- Commons devolvió muchas escenas y obras de arte para las búsquedas, y respondió **429** al pedir miniaturas de 800 px.
- Clases con poca cobertura:
  - metales (15, ninguna de la segunda tanda);
  - multiobjeto (10);
  - negativos (11);
  - ropa y calzado (0: no se buscaron en esta tanda);
  - higiene personal (0).

## Entrenamiento: no viable en este equipo

**No se entrenaron pesos.** No hay ajuste fino. Lo que cambió son reglas, catálogo y lenguaje, medidos sobre fotos que no se usaron para escribirlos.

| Requisito | En el equipo |
|---|---|
| Pesos del modelo | Solo GGUF cuantizado (Q4_K_M) servido por Ollama. Ollama no entrena |
| GPU | GTX 1650 con 4 GB de VRAM: insuficiente para un LoRA de un VLM de 1,7 B en FP16 |
| Python y PyTorch | Solo existe el acceso directo de Microsoft Store. Instalar el entorno no estaba autorizado y cargaría el equipo toda la noche |

## Antes y después, con las mismas etiquetas

La métrica justa es la **ruta de disposición**: BLANCA, NEGRA, VERDE, RAEE, pilas, medicamentos, donar, evaluar…
- La versión anterior no conoce las claves nuevas: para ella se usaron manifiestos con sus claves (`claves_base`).
- Las dos se juzgan contra las mismas etiquetas originales.

| Conjunto | Muestras | Ruta correcta antes | **Ruta correcta después** | Falsas admisiones antes | **Después** | Ofertas indebidas antes → después |
|---|---:|---:|---:|---:|---:|---|
| Prueba independiente | 42 | 32 (76,2 %) | **35 (83,3 %)** | 3 | **1** | 2 → 1 |
| Validación | 33 | 22 (66,7 %) | **25 (75,8 %)** | 1 | **0** | 0 → 0 |
| Entrenamiento | 196 | 146 (74,5 %) | **154 (78,6 %)** | 7 | **4** | 5 → 5 |
| **Total** | **271** | 200 (73,8 %) | **214 (79,0 %)** | 11 | **5** | 7 → 6 |

**Latencia mediana en prueba:** 3 581 ms antes y 994 ms después. No se interpreta como una mejora del código: las
corridas se hicieron en momentos distintos, en dos partes reanudadas y sin controlar si el modelo estaba ya cargado. En
validación y entrenamiento las medianas son parecidas (1,1 a 1,4 s).

**Límites de la comparación:**
- Con 42 fotos de prueba, el intervalo de confianza es amplio: ±12 puntos aproximadamente.
- La mejora es consistente en los tres conjuntos, pero **no es una precisión general garantizada**.
- La métrica de «clave exacta» favorece a la versión nueva, porque las etiquetas usan claves que la anterior no tiene. Por
  eso se informa la ruta.

## Falsas admisiones que quedan (después)

| Foto | Conjunto | Eco dijo | Era | Riesgo |
|---|---|---|---|---|
| vidrio-21 | prueba | `glass_bottle` | vaso de vidrio | Abriría la BLANCA con un vaso, que no es envase aprovechable |
| plastico-25 | entrenamiento | `plastic_bottle` | envase de detergente | **Abriría la BLANCA con un envase químico** |
| especial-39 | entrenamiento | `plastic_bottle` | botellas de cloro | **Igual** |
| especial2-19 | entrenamiento | `cardboard` | cajas de medicamento | Abriría la BLANCA con medicamentos |
| especial2-30 | entrenamiento | `can` | aerosol aplastado | **Abriría la BLANCA con un aerosol** |

**Ningún cambio de esta noche se ajustó con las fotos de prueba.** El siguiente ajuste se haría solo con las de
entrenamiento, y después se mediría otra vez la prueba:
- una pregunta de seguridad al modelo antes de abrir: «¿el envase muestra un químico, un medicamento o un aerosol?»;
- si la respuesta no es un «no» claro, no se abre.

Estas cinco son la razón principal para **no habilitar todavía aperturas nuevas en las canecas físicas**.

## Varios residuos en una foto (`--evaluar-multiobjeto`)

| Métrica | Valor (10 fotos) |
|---|---|
| Conjunto de canecas exacto | 2 de 10 |
| BLANCA | precisión 1,00, recall 0,80, F1 0,89 |
| NEGRA | precisión 1,00, recall 0,14, F1 0,25 |
| Fotos sin lista útil | 2 |
| Error medio en el número de objetos | 1,3 |
| Latencia mediana / p95 | 1 321 ms / 6 610 ms |

- El modelo nombra bien los aprovechables, pero casi nunca nombra la basura genérica que va a la negra.
- Con 10 fotos no hay una tasa fiable.
- Por eso Eco, ante varios residuos, **solo orienta** y pide uno por uno.

## Detalle por conjunto

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


### validacion (despues)

| Métrica | Valor |
|---|---|
| Muestras | 33 |
| Clave exacta correcta | 21 (63,6 %) |
| Ruta de disposición correcta | 25 (75,8 %) |
| Falsas admisiones (abriría sin deber) | 0 |
| Ofertas indebidas | 0 |
| Abstenciones | 4 |
| Fallos técnicos | 0 |
| Latencia mediana / p95 | 1118 ms / 6141 ms |

**Precisión, recall y F1 por ruta** (sobre la ruta exacta de la primera clave aceptada):

| Ruta | VP | FP | FN | Precisión | Recall | F1 |
|---|---:|---:|---:|---:|---:|---:|
| (abstención) | 0 | 2 | 0 | 0,00 | 0,00 | 0,00 |
| Blanca | 5 | 0 | 4 | 1,00 | 0,56 | 0,71 |
| Cortopunzante | 1 | 0 | 1 | 1,00 | 0,50 | 0,67 |
| Evaluar | 6 | 4 | 1 | 0,60 | 0,86 | 0,71 |
| Medicamentos | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Negra | 2 | 2 | 1 | 0,50 | 0,67 | 0,57 |
| NoEsResiduo | 2 | 1 | 0 | 0,67 | 1,00 | 0,80 |
| PilasBaterias | 1 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| Raee | 2 | 0 | 0 | 1,00 | 1,00 | 1,00 |
| ReutilizacionDonacion | 0 | 2 | 0 | 0,00 | 0,00 | 0,00 |
| Verde | 1 | 1 | 5 | 0,50 | 0,17 | 0,25 |

**Matriz de confusión** (filas: ruta esperada; columnas: ruta predicha):

| Esperada \ Predicha | (abstención) | Blanca | Cortopunzante | Evaluar | Medicamentos | Negra | NoEsResiduo | PilasBaterias | Raee | ReutilizacionDonacion | Verde |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 0 | 5 | 0 | 0 | 0 | 2 | 1 | 0 | 0 | 1 | 0 |
| Cortopunzante | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| Evaluar | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| Medicamentos | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| Negra | 1 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 |
| NoEsResiduo | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 |
| PilasBaterias | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 0 |
| Raee | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 |
| Verde | 1 | 0 | 0 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |

**Errores de ruta** (objeto, predicción, clase correcta):

| Foto | Grupo | Predicción (clave → ruta) | Correcto | ¿Abriría? |
|---|---|---|---|---|
| agricola-01.jpg | agricola | `mixed_trash` → Evaluar | `plant_waste` → Verde | no |
| agricola-18.jpg | agricola | `mixed_trash` → Evaluar | `plant_waste` → Verde | no |
| hojas2-11.jpg | agricola | abstención | `plant_waste` → Verde | no |
| fruta-50.jpg | alimentos | `plant_waste` → Verde | `whole_produce` → Evaluar | no |
| mixto-26.jpg | mixto | abstención | `styrofoam,plastic_container` → Negra | no |
| papel-09.jpg | papel | `clothes` → ReutilizacionDonacion | `paper` → Blanca | no |
| plastico-45.jpg | plastico | `people` → NoEsResiduo | `plastic_bottle` → Blanca | no |
| vidrio-27.jpg | vidrio | `drinking_glass` → ReutilizacionDonacion | `broken_glass` → Cortopunzante | no |

**Falsas admisiones** (Eco abriría sola sin deber):

- Ninguna.


El detalle de entrenamiento y de «antes» está en `resultados/banco_noche/`. Reproducir: `scripts/pruebas/vision/Preparar-ManifiestosBanco.ps1`, `EcoAccess.P3.Bot.exe --evaluar-objetos <manifiesto> local` y `scripts/pruebas/vision/Analizar-ResultadosBanco.ps1`.
