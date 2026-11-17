# Cierre de software: comida dentro de un recipiente ya no abre la verde (15–16/11/2026)

**No hubo reentrenamiento.** El modelo es el mismo (`lfm3-vl:latest` en Ollama, temperatura 0). Cambiaron reglas: dos
preguntas de sí o no antes de que la comida abra la VERDE y una clave nueva del catálogo, `food_in_container`, que solo
orienta. Todo se reproduce con los manifiestos `*_v2.json` y los informes de `resultados/banco_ampliado_v1/evaluaciones/`.

## 1. Causa raíz

El nombre libre que da el modelo describe solo la comida («Creamy white dessert», «Roast pork slices», «Chow mein noodles»)
y la lectura por material la corrobora como orgánico (`food` → `ORGANICO`). Ninguna pregunta miraba el recipiente: un
recipiente plástico, una caja de icopor o un plato desechable con comida abría la VERDE con el envase dentro. Los tres casos
reportados (aplastico-053, aplastico-061 de entrenamiento y aplastico-069 de prueba) tenían esa causa, y había más en icopor y
platos desechables que las etiquetas del banco aceptaban como «comida servida».

## 2. Corrección

1. `¿La comida está dentro de un recipiente plástico o de icopor, una caja para llevar, una bandeja o un vaso?`
2. Si la primera dice «no»: `¿La comida está en un plato desechable de papel o de plástico?`

Cualquier «sí» cambia la clave a `food_in_container`: Eco pide separar la comida (VERDE) del recipiente (BLANCA si es plástico
limpio, NEGRA si es icopor o está sucio) y **no abre ninguna caneca**. Una respuesta mal formada cuenta como duda y tampoco abre.
Solo se pregunta cuando la comida todavía podría abrir la verde. Se conservan la regla de doble comprobación de los envases
vacíos (V1) y las preguntas de pila, cortante y papel usado.

## 3. Cómo se eligieron las preguntas (solo entrenamiento)

Sobre las 96 fotos de ENTRENAMIENTO que la versión V1 mandaba a la verde, con el modelo real:

| Pregunta candidata | Recipientes con comida bloqueados | Orgánicos frenados (van a revisión) |
|---|---:|---:|
| «¿desechable de plástico, icopor, papel o aluminio…?» | 6 de 6 | 16 de 90 |
| **«¿recipiente plástico o de icopor, caja para llevar, bandeja o vaso?»** (elegida, v2a) | 5 de 6 | 5 de 90 |
| **«¿plato desechable de papel o de plástico?»** (segunda, v2b) | bloquea el plato de papel | 10 de 90 más |

v2a dejó cuatro aperturas equivocadas en las 915 fotos, todas platos de papel con comida. Por eso v2b añade la segunda
pregunta. Validación confirmó; **prueba no se usó para elegir nada** y se reporta tal cual.

## 4. Etiquetas v2 (regla fija, igual en los tres conjuntos)

Decidida mirando las fotos antes de medir la versión nueva: (a) donde ya se aceptaba `container_with_content` también se acepta
`food_in_container`; (b) en icopor y desechables con comida, `served_food` se reemplaza por `food_in_container`; (c) en el pollo
sobre una servilleta se aceptan las dos. 126 etiquetas cambiaron. La comida suelta o en plato de loza sigue siendo comida.
La versión V1 se volvió a analizar con estas mismas etiquetas para comparar en igualdad.

## 5. Resultados (mismas etiquetas v2)

Aperturas automáticas = la blanca o la caneca de la estación (verde/negra) se abriría sin intervención humana.

| Conjunto | Versión | Ruta correcta | Aperturas | Equivocadas (blanca / estación) | Precisión de apertura | Latencia mediana / p95 |
|---|---|---:|---:|---|---:|---|
| Prueba (139) | V1 | 71,9 % | 25 | 3 (0 / 3) | 88,0 % | 3,5 s / 9,1 s |
| Prueba (139) | v2a | 71,2 % | 22 | 1 (0 / 1) | 95,5 % | 4,5 s / 9,6 s |
| Prueba (139) | **v2b** | **71,2 %** | 21 | **0** | **100 %** | 4,8 s / 9,2 s |
| Validación (171) | V1 | 72,5 % | 28 | 5 (0 / 5) | 82,1 % | 3,5 s / 9,1 s |
| Validación (171) | v2a | 73,1 % | 25 | 2 (0 / 2) | 92,0 % | 4,5 s / 9,2 s |
| Validación (171) | **v2b** | **73,1 %** | 22 | **0** | **100 %** | 4,4 s / 8,8 s |
| Entrenamiento A (303) | V1 | 79,2 % | 56 | 1 | 98,2 % | 3,4 s / 8,0 s |
| Entrenamiento A (303) | **v2b** | 79,2 % | 45 | **0** | 100 % | 3,7 s / 8,7 s |
| Entrenamiento B (302) | V1 | 72,5 % | 60 | 6 | 90,0 % | 3,5 s / 8,8 s |
| Entrenamiento B (302) | **v2b** | 71,9 % | 51 | 1 | 98,0 % | 5,3 s / 8,9 s |

En las 915 fotos: aperturas equivocadas **15 → 1** (la que queda es una servilleta limpia leída como usada → NEGRA, en
entrenamiento); la BLANCA sigue en 0. Ningún residuo peligroso abre una caneca. Abstenciones sin cambio (8 en prueba).

**Lectura honesta**

- La seguridad mejora sin perder exactitud: la ruta correcta en prueba queda en 71,2 % (una foto menos que V1, 71,9 %).
- El precio es cobertura de la verde: en prueba las aperturas automáticas bajan de 25 a 21. Los orgánicos frenados reciben la
  orientación a la VERDE sin abrir. El modelo de 1,7 B confunde platos de loza con desechables.
- La latencia sube ~1 s en la mediana por la pregunta adicional.
- La meta del 90 % de ruta correcta **no se alcanzó**.

## 6. Incidentes (v2b, 915 fotos)

| Incidente | Fotos | Ruta correcta | Aperturas indebidas |
|---|---:|---:|---:|
| Bolsa de agua | 8 | 6 | 0 |
| Pilas (botón, AA, litio) | 49 | 41 | 0 |
| Blíster de medicamento | 10 | 8 | 0 |
| Medicamentos | 11 | 9 | 0 |
| Cargadores | 12 | 11 | 0 |
| Cubeta de huevos | 1 | 0 | 0 |
| Botellas de jugo o gaseosa | 10 | 4 | 0 (las 3 aperturas son PET vacías y limpias, correctas) |

## 7. Pendiente

- Servilleta limpia leída como usada (1 caso, abre la NEGRA).
- Más fotos de envases limpios y vacíos y de cubetas de huevos.
- Validación física supervisada con las tres canecas.
