# Evaluación con productos vendidos en Colombia y residuos comunes (10/11/2026)

Motor: `ollama-local:lfm3-vl:latest`, en el equipo (las fotos no salen del PC).

**No se entrenó ningún modelo ni se cambió el prompt.** Las pruebas sirven para medir y para corregir el intérprete de
P3, que traduce el nombre que da el modelo a una clave del catálogo.

La admisión se midió contra la caneca piloto real **1093 «EcoAccess — Caneca piloto»**, en servicio, con 11
materiales. La herramienta ahora escribe qué caneca usa (ver *Error de método*).

## Fotos

- **Fuente:** Wikimedia Commons, solo licencias CC0, CC BY, CC BY-SA o dominio público. Se descargaron miniaturas en el
  tamaño estándar de 500 px, con pausas y reintentos, porque Wikimedia respondió 429 a 800 px.
- **Búsquedas:**
  - Marcas vendidas en Colombia: Postobón, Pony Malta, Club Colombia, Águila, Poker, Aguardiente Antioqueño, Chocoramo,
    El Tiempo; además arepas, empanadas y bandeja paisa.
  - Clases que faltaban: cartón, papel, orgánicos, pilas, medicamentos, bombillos, celulares y tapabocas.
- **Selección:** 543 candidatas revisadas a mano en hojas de contacto. Se dejaron solo las que tienen un objeto
  principal claro; se descartaron fábricas, vigilias, conciertos, escenas de reciclaje e imágenes generadas por IA.
- **Resultado:** 200 etiquetadas y 195 tras quitar 5 duplicadas por SHA-256.
  - De esas, **177 son inéditas**: ningún título de Commons coincide con las evaluaciones anteriores.
  - Las fotos se guardan fuera del repositorio, en `C:\ProgramData\EcoAccessP3\Vision\colombia\final`.
- **Archivos en `resultados/`:**
  - `colombia_manifiesto.json`: SHA-256 y claves aceptadas.
  - `colombia_fuentes.json`: título, licencia, URL y búsqueda de cada foto.
  - `colombia_etiquetas_ronda1.txt` y `colombia_etiquetas_ronda2.txt`.
- **Clases con alternativa válida:**
  - Una lata acepta `can` o `metal_item`.
  - Una caja de huevos acepta `cardboard` o `paper`.
  - Los tapabocas aceptan `gloves`, `used_tissue` o abstenerse: **el catálogo no tiene clave para tapabocas**, y lo
    correcto es el mensaje de residuo sanitario o pedir otra foto.

## Resultados

Antes y después corresponden a la corrección del intérprete descrita abajo. Esa corrección se hizo mirando los fallos de
**este mismo conjunto**, así que la cifra «después» es optimista. La comprobación independiente es TrashNet final.

| Medida | Antes (195) | Después (195) | Inéditas antes (177) | Inéditas después (177) |
|---|---|---|---|---|
| Objeto correcto | 134 | **149** | 123 | **136** |
| Contexto correcto (tipo de residuo) | 147 | **156** | 134 | **141** |
| Abre con material distinto al de la foto | 3 | 3 | 2 | 2 |
| Ofrece botón para algo no admisible | 4 | **2** | — | — |
| Aperturas correctas | 46 | **51** | — | — |
| Abstenciones (pide otra foto o elegir) | 23 | 18 | — | — |
| Fallos técnicos | 0 | 0 | — | — |
| Latencia p50 / p95 | 2,9 / 3,6 s | 2,5 / 3,2 s | — | — |

**Las 3 aperturas con material distinto van al mismo destino.** Las tres son aprovechables que la caneca blanca recibe;
el error está solo en el material registrado:

- una caja de leche leída como cartón;
- una botella retornable de vidrio de Postobón leída como PET;
- una botella PET escarchada leída como vidrio.

Ningún residuo sanitario, orgánico o peligroso abrió la caneca. Los botones indebidos no abren nada: son ofertas que la
persona debe confirmar.

**Por clase (después):**

| Resultado | Clases |
|---|---|
| Todas bien | papel 17/17, pilas 5/5, bombillos 2/2, celulares 3/3 |
| Casi todas | latas 18/19, empaques de mecato 11/12, medicamentos 11/12, PET 9/10 |
| Bien con fallos | bolsas 10/13, comida servida 10/13, orgánicos 16/20, cartón 19/25 |
| Débiles | vidrio 13/21, cajas de bebida 4/10, envases plásticos 0/3, tapabocas 0/6 |

**Comprobación independiente** (TrashNet final, 100 fotos que no se miraron, ahora contra la piloto 1093):

- Objeto 72/100 y contexto 87/100: igual que antes de esta corrección.
- **0 aperturas con material equivocado** y 53 aperturas correctas.

## Corrección del intérprete (no del modelo)

El modelo nombraba bien y el intérprete elegía otra palabra:

| Nombre del modelo | Antes | Ahora |
|---|---|---|
| «Egg carton» | carton → caja multicapa | `cardboard` |
| «Cardboard tray» | tray → envase plástico | `cardboard` |
| «Battery pile heap», «Battery box» | pile → varios objetos; box → cartón | `battery` |
| «Broken eggshell», «Egg shell fragment» | egg → comida servida | `food_scraps` |
| «Chip bag», «Chips bag» | bag → bolsa plástica (admitida) | `snack_wrapper` (no se admite) |

**15 fotos mejoraron y ninguna empeoró.** Una de ellas, una botella de vidrio, cambió por variación del modelo y no por
estas reglas. Pruebas: `InterpretadorColombiaTests` (22 casos, 10 de ellos de cosas que no deben cambiar).

## Lo que no se corrigió, y por qué

- **Botellas de cerveza ámbar.** El nombre es correcto («Beer bottle»), pero la segunda lectura del modelo dice
  `battery` con certeza baja o `can` con certeza alta. Esa lectura es la salvaguarda que impide abrir con el nombre
  solo, así que abstenerse aquí es lo correcto. Mejorarlo exige cambiar la definición de «glass» en el prompt y volver
  a medir todos los conjuntos. Queda como siguiente paso, no como entrenamiento.
- **Tapabocas.** El catálogo no tiene clave propia y el modelo dice «medicine», «people» o «diaper». Ninguno abre la
  caneca. Hay que agregar la familia «tapabocas» al catálogo.
- **Cajas de bebida.** Las bolsas triangulares japonesas el modelo las nombra «Yogurt cup» o «Ice cream cone». Son
  fotos poco comunes en Colombia.

## Error de método detectado y corregido

La primera corrida devolvió `CanecaNoDisponible` en todas las fotos.

- **Causa:** la herramienta tomaba primero la caneca de `Telegram:QrDemo`, que es «DEMO - Caneca ficticia». Esa caneca
  hoy está fuera de servicio y solo admite 4 materiales.
- **Corrección en la evaluación:** ahora usa la piloto por su nombre y escribe en pantalla cuál usa.
- **El mismo fallo en el bot real:** el botón «caneca piloto» respondía «no disponible» aunque la piloto 1093 estuviera
  en servicio. Se corrigió en `ServicioConversacionTelegram.BuscarPilotoDisponibleAsync`, con la prueba
  `CanecaDemoFueraDeServicioNoOcultaLaCanecaPilotoReal`. Escanear el QR oficial nunca estuvo afectado.
- La evaluación TrashNet anterior se midió contra esa caneca DEMO; se corrigió su documento.

## Límites

Son fotos de Commons: muchas son de estudio o de producto, no fotos de teléfono en la calle de Valledupar. Ninguna cifra
es una exactitud de campo. El objetivo de 90 % no se alcanza:

- contexto correcto: 156/195 (80 %);
- objeto: 149/195 (76 %), o 136/177 (77 %) en las inéditas.

Las fotos reales del propietario siguen siendo la siguiente evaluación necesaria.
