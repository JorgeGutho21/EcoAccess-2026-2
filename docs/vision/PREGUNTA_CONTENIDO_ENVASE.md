# Pregunta de contenido del envase: medición antes y después (14/11/2026)

## Qué cambió

Antes de que una foto pueda abrir la BLANCA por un par objeto-material (botella plástica, vidrio, lata, cartón), el modelo
local responde una pregunta independiente de sí o no: *¿el envase tiene algo dentro?* Con «yes» Eco responde «un envase con
contenido» y **no abre**; con una respuesta inválida queda incierta. Son instrucciones y reglas en C#: **no hubo
entrenamiento de pesos**. Código: `VocabularioVision.PreguntaLleno` y `ClasificadorOllamaLocal`.

## Método

- Mismo código, mismo día, mismo modelo (`lfm3-vl` local). Cada conjunto se midió dos veces: sin la pregunta
  (`ECOACCESS_VISION_SIN_PREGUNTA_LLENO=1`) y con ella.
- La pregunta se escribió y se revisó **solo con el conjunto de entrenamiento**. Validación, prueba y Colombia se midieron
  una vez, después, y no se usaron para ajustar nada.
- La ventana de 317 fotos independientes es `prueba` (42) + `Colombia` (275).
- Las etiquetas del banco **no registran si el envase está lleno**: marcan una cerveza sin abrir como «vidrio admisible».
  Por eso «clave correcta» baja cuando Eco deja de abrir un envase lleno. Las fotos retiradas se revisaron a ojo y están
  en esta carpeta.

## Resultados

| Conjunto | Fotos | Aperturas de la blanca sin → con | Falsas admisiones sin → con | Clave correcta sin → con | Retiradas con contenido visible |
|---|---|---|---|---|---|
| Entrenamiento | 196 | 19 → 12 | 0 → 0 | 139 → 132 | 7 de 7 |
| Validación | 33 | 2 → 1 | 0 → 0 | 21 → 20 | 1 de 1 (lata de caramelos cerrada) |
| Prueba | 42 | 7 → 6 | 0 → 0 | 31 → 30 | 1 de 1 (gaseosa llena) |
| Colombia | 275 | 9 → 6 | 1 → 1 | 219 → 216 | 3 de 3 (pacas de agua, gel con líquido, botellas sin abrir) |

- **Independientes (317 fotos):** 16 → 12 aperturas de la blanca. Las 4 retiradas tenían contenido visible.
- **Verde y negra:** sin cambios (Colombia 37, prueba 3, validación 4).
- **Latencia:** mediana sin cambio apreciable (Colombia 1 399 → 1 389 ms); el p95 sube unos 0,5 a 1,1 s en las fotos que
  hacen la pregunta.

Capturas: `retiradas_entrenamiento.png` y `retiradas_validacion_prueba_colombia.png`. Resultados por foto: los ocho JSON.

## Lo que sigue fallando

- **`carton-05` (Colombia) todavía abriría la blanca.** Son cubetas de huevos **cerradas** en un estante: el modelo responde
  que no ve contenido. Un envase cerrado y opaco no se puede comprobar con una foto.
- La falsa admisión medida en Colombia (1) no cambia.
- Con 12 aperturas independientes de la blanca el intervalo de confianza sigue siendo ancho. **No está demostrada** una
  precisión de aperturas ≥ 99 %, ni la ruta general ≥ 95 %.
- Son fotos de Wikimedia Commons, no de la estación. **No sustituyen la prueba física.**
- El banco ampliado de 1 000 imágenes nuevas **no se construyó**.

Las fotos de las hojas de contacto conservan su autor y licencia (CC0, CC BY, CC BY-SA o dominio público): están en
`fuentes.json` y `banco.csv` de cada banco, igual que en `EVALUACION_BANCO_COLOMBIA.md`.
