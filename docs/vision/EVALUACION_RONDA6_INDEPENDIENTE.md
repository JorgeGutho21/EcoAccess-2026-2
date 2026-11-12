# Evaluación visual, ronda 6 (independiente, 11/11/2026)

**Objetivo:** medir la regla de objetos confundibles (`ServicioClasificacionImagen.MensajesObjetosConfundibles`, commit `42db01e`) con fotos que no se usaron para escribirla.

Esta ronda **no es un entrenamiento**: el modelo `lfm3-vl` es el mismo de la ronda 5. Solo cambió una regla en C#.

## Método

| Dato | Valor |
|---|---|
| Modelo | Ollama local `lfm3-vl:latest`, en este PC |
| Código | rama `desarrollo/auditoria-tercer-corte`, commit `42db01e`, compilación Release |
| Fotos | 49 candidatas de Wikimedia Commons (licencias libres, en `resultados/ronda6_fuentes.json`). Se excluyeron todos los títulos de las rondas 3, 4 y 5 |
| Etiquetado | A ojo, **antes** de ejecutar el modelo (`resultados/ronda6_etiquetas.txt`). Se descartaron 30 fotos que no son una foto clara de un residuo, como dibujos, escenas o estantes; quedaron **19 evaluables** |
| Integridad | SHA-256 de las 19 fotos comprobado contra el manifiesto: 19 de 19 iguales |
| Caneca de admisión | 1093 «EcoAccess — Caneca piloto», en servicio |
| Comando | `EcoAccess.P3.Bot.exe --evaluar-objetos <ronda6>\fotos\manifiesto.json local`. Solo lee el catálogo; no crea solicitudes, órdenes ni depósitos |
| Ejecución | 19:24:30 a 19:26:00 (hora de Colombia), sin pruebas de carga simultáneas. Las pruebas unitarias corrieron en paralelo cerca de un minuto, lo que puede elevar algo las latencias |
| Resultado | `resultados/ronda6_resultado.json` |

## Resultados

| Métrica | Ronda 5 (102 fotos) | **Ronda 6 (19 fotos)** |
|---|---|---|
| Respuestas correctas | 77 (75,5 %) | **14 (73,7 %)** |
| Contexto correcto | 74 (72,5 %) | 16 (84,2 %) |
| Falsas admisiones (abriría la tapa sin deber) | 2 | **1** |
| Ofertas indebidas (botón «depositar» sin deber) | 10 | **1** |
| Falsos rechazos | 6 | 2 |
| Abstenciones | 9 | 1 |
| Fallos técnicos | 0 | 0 |
| Aperturas correctas | n/d | 7 |
| Latencia mediana | 2 472 ms | 3 455 ms |
| Latencia p95 | 3 849 ms | 14 129 ms (la primera foto incluye la carga del modelo) |

### Lo que comprueba la regla nueva

| Objeto | Fotos | Antes (ronda 5) | Ronda 6 |
|---|---|---|---|
| Icopor visto como `plastic_container` | 2 | ofrecía depositar en la blanca | **0 ofertas**: orienta y explica cómo distinguirlo |
| Empaque metalizado (`snack_wrapper`) | 2 | 1 oferta como `plastic_bag` | **0 ofertas** y 0 aperturas |
| Bolsa limpia (`plastic_bag`) | 3 | — | 2 orientan sin oferta: un paso más para la persona, como se esperaba |

### Errores, uno por uno

| Foto | Esperado | Leyó | Consecuencia | Gravedad |
|---|---|---|---|---|
| `persona-03` | personas | `cardboard` (cartón) | **Oferta indebida**: botón «Sí, continuar» ante la foto de una persona. No abre por sí sola | Alta: es el caso que más importa corregir |
| `multicapa-04` | `drink_carton` | `plastic_bottle` (PET) | **Falsa admisión**: abriría la blanca. Material equivocado, pero la misma caneca que le correspondía | Media: el destino físico es correcto y el registro del material no |
| `bolsa-05` | `plastic_bag` | `paper` | Oferta de papel para una bolsa (los dos van a la blanca) | Baja |
| `persona-01` | personas | `clothes` (ropa) | Orienta, sin oferta ni apertura | Baja |
| `persona-05` | personas | — | «No estoy seguro»: abstención segura | Ninguna |

### Por material

| Clase | Correctas |
|---|---|
| vidrio | 3/3 |
| PET | 2/2 |
| lata | 2/2 |
| icopor | 2/2 |
| metalizado | 2/2 |
| bolsa | 2/3 |
| multicapa | 1/2 |
| persona | 0/3 (ninguna abre; 1 oferta indebida) |

## Conclusiones

1. La regla cumple su propósito: **0 ofertas para icopor y metalizados** en fotos nuevas.
2. **Una persona recibió una oferta de depositar cartón.** En la ronda 5 hubo 0 ofertas sobre 20 personas y animales; aquí 1 de 3. Con 3 fotos no se puede estimar una tasa. Sí demuestra que el caso ocurre, y queda como **riesgo abierto**. La protección que sigue funcionando es que la oferta requiere el toque de la persona y nunca abre sola.
3. Con 19 fotos, la ronda 6 **no sirve para declarar una precisión general**. Su intervalo de confianza es muy amplio. La referencia general sigue siendo la ronda 5 (102 fotos).
4. No se ajustó nada con estas fotos. Si en el futuro se corrige `persona → cartón` usándolas, la ronda 6 deja de ser independiente y habrá que medir con una ronda 7.
