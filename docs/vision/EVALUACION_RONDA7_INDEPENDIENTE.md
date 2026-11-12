# Evaluación visual, ronda 7 (independiente, 11/11/2026)

**Objetivo:** medir, con fotos que no se usaron para escribirlas, dos correcciones:
- la validación del sujeto vivo (persona o animal) antes de ofrecer depositar, del commit `19f8aff`;
- el reconocimiento de comida, pescado cocinado y huevos, del commit `1fa52ac`.

Esta ronda **no es un entrenamiento**: el modelo `lfm3-vl` es el mismo de las rondas 5 y 6. Solo cambiaron las preguntas
que se le hacen y las reglas en C#.

## Método

| Dato | Valor |
|---|---|
| Modelo | Ollama local `lfm3-vl:latest`, en este PC |
| Código | rama `desarrollo/telegram-doble-ia`, después del commit `1f8c30b`, compilación Debug |
| Fotos | 23 candidatas de Wikimedia Commons, con licencias libres (`resultados/ronda7_fuentes.json`). Se excluyeron los 672 títulos de las rondas 3 a 6 |
| Etiquetado | a ojo, **antes** de ejecutar el modelo (`resultados/ronda7_etiquetas.txt`). Se descartaron 8 que no son una foto clara de un único sujeto (pintura, dibujo, escenas, varios envases, un bebé con un envase); quedaron **15 evaluables** |
| Integridad | SHA-256 de cada foto en el manifiesto (`resultados/ronda7_manifiesto.json`) |
| Comando | `EcoAccess.P3.Bot.exe --evaluar-objetos <ronda7>\fotos\manifiesto.json local`. Solo lee el catálogo; no crea solicitudes, órdenes ni depósitos |
| Ejecución | 20:50 (hora de Colombia), 57 s, sin pruebas simultáneas |
| Resultado | `resultados/ronda7_resultado.json` |

## Resultados

| Métrica | Ronda 6 (19 fotos) | **Ronda 7 (15 fotos)** |
|---|---|---|
| Respuestas correctas | 14 (73,7 %) | **13 (86,7 %)** |
| Falsas admisiones (abriría la tapa sin deber) | 1 | **0** |
| Ofertas indebidas (botón «depositar» sin deber) | 1 | **0** |
| Falsos rechazos | 2 | 1 |
| Abstenciones | 1 | 1 |
| Fallos técnicos | 0 | 0 |
| Latencia mediana | 3 455 ms | 1 663 ms |
| Latencia p95 | 14 129 ms | 15 520 ms (la primera foto incluye la carga del modelo) |

Las dos rondas tienen fotos y clases distintas. La comparación orienta, pero **no mide una mejora general**: 15 fotos dan
un intervalo de confianza muy amplio.

### Por clase

| Clase | Fotos | Resultado |
|---|---|---|
| Personas | 2 | 2 «personas», sin oferta ni apertura |
| Animales (perro, gato) | 2 | 2 correctos, sin oferta |
| Pescado cocinado en un plato | 3 | 3 leídos como comida (1 «comida con pescado», 2 «comida preparada»); **ninguno como animal** |
| Comida servida | 4 | 4 «comida preparada» (incluye un plato con huevo) |
| Huevos enteros | 1 | 1 «huevos» |
| PET | 2 | 1 correcto, que abriría la blanca; 1 falso rechazo |
| Icopor | 1 | abstención («no estoy seguro»), sin oferta |

### Errores, uno por uno

| Foto | Esperado | Leyó | Consecuencia | Gravedad |
|---|---|---|---|---|
| `pet-02` | botella PET sostenida con la mano | «personas» | **Falso rechazo**: la persona debe repetir la foto sin la mano o elegir el material en el menú | Baja para la seguridad, media para la comodidad. Es el costo previsto de la validación del sujeto vivo |
| `icopor-01` | vaso de icopor (no aprovechable) | incierta | Abstención segura: no ofrece ni abre | Ninguna |

## Conclusiones

1. En estas 15 fotos nuevas, **ninguna persona, animal ni comida recibió una oferta de depositar o abrir**.
2. **El pescado cocinado ya no se lee como animal** (3 de 3). Huevos y comida servida se reconocen y se pregunta si se
   van a desechar.
3. La validación del sujeto vivo tiene un costo: una mano que sostiene la botella puede rechazarse (1 de 2 PET). Es un
   fallo seguro (no abre), pero incomoda. No se ajustó con esta foto para no contaminar la ronda; cualquier ajuste se
   mide con una ronda 8.
4. Con 15 fotos no se declara una precisión general. La referencia amplia sigue siendo la ronda 5 (102 fotos), medida
   antes de estas correcciones.
