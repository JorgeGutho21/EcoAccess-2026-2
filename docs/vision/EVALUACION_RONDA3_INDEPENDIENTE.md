# Evaluación independiente — tercera ronda (10/11/2026)

**Estado:** VERIFICADO MEDIANTE SIMULACIÓN. Son fotos públicas que se pasan al modelo local real, no fotos de un ciudadano por Telegram.

## Qué se midió y cómo

- **Fotos**
  - 172 candidatas descargadas de Wikimedia Commons (licencias libres; títulos y enlaces en `resultados/ronda3_fuentes.json`). Se buscaron por grupo: envases, empaques, higiene, sanitario, electrónicos, objetos cotidianos, seres vivos y comida.
  - Se excluyeron los 921 títulos ya usados en rondas anteriores.
- **Etiquetado:** a ojo, sobre hojas de contacto, **antes** de ejecutar el modelo. Quedaron 131 fotos (`resultados/ronda3_etiquetas.txt`).
  - **Excluidas:** pinturas de objetos, paisajes, canecas vacías, escenas sin sujeto principal y lo que el catálogo no cubre.
  - **Claves aceptadas:** cada foto admite varias cuando hay más de una lectura razonable. Ejemplo: una silla de terraza con mesa acepta `chair`, `table` o `furniture`.
- **Ejecución**
  - Comando: `EcoAccess.P3.Bot.exe --evaluar-objetos <manifiesto> local`.
  - Modelo `lfm3-vl` en Ollama local.
  - Admisión contra la caneca piloto real 1093 «EcoAccess — Caneca piloto» (en servicio, 11 materiales).
- **Código:** el del commit `73d8b99`, sin ningún ajuste posterior a esta medición. Los fallos de abajo se documentan; no se corrigió nada con estas mismas fotos.

## Resultado (`resultados/ronda3_independiente.json`)

| Medida | Valor |
|---|---|
| Fotos | 131 |
| Clave correcta | 93 (71 %) |
| Contexto correcto (vivo, persona, residuo aprovechable, sanitario…) | 91 |
| Abstenciones («envíame otra foto») | 13 |
| Fallos técnicos | 0 |
| Mediana / p95 por foto | 1,0 s / 2,9 s |
| Aperturas correctas | 8 |
| Falsas admisiones (abriría con una clase no aprovechable o con otro material) | 7 |
| Ofertas indebidas (botón «depositar» para algo que no corresponde) | 6 |
| Rechazos indebidos | 9 |

### Resultados por grupo

| Grupo | Fotos | Clave correcta |
|---|---|---|
| Perros, gatos, ave, caballo | 16 | 16 |
| Muebles y sillas | 13 | 13 |
| Zapatos | 5 | 5 |
| Audífonos | 5 | 5 |
| Cargadores | 3 | 3 |
| Comida servida | 13 | 10 |
| Personas | 12 | 9 |
| Electrónicos varios (parlantes, ventilador de PC, radio) | 7 | 5 |
| Botellas de vidrio | 7 | 5 |
| Botellas plásticas (champú, agua) | 8 | 4 |
| Pañales y toallas | 6 | 4 |
| Condones | 6 | 3 |
| Varios objetos (pacas de reciclaje, repisa de baño) | 7 | 5 |
| Electrodomésticos (ventiladores, aires acondicionados) | 14 | 2 |
| Recipientes plásticos (6 tubos de crema dental, 1 bandeja) | 7 | 2 |

## Fallos que importan

1. **Persona leída como metal.** Una foto de militares saludando (`envases-10`) recibió la oferta de depositar «metal».
   - No abre sola: la persona tendría que pulsar el botón.
   - Es el fallo más grave de la ronda.
2. **Ventiladores y aires acondicionados**
   - Dos se ofrecieron como «metal» (`electronico-17`, `electronico-25`).
   - El resto quedó como incierto, «varios objetos», mueble o juguete.
   - El catálogo trata `appliance` como objeto cotidiano. El modelo casi nunca lo nombra así.
3. **Caja de condones leída como cartón** (`sanitario-08`): abriría la caneca. El empaque exterior sí es cartón, pero la etiqueta esperaba la familia sanitaria.
4. **Plástico y vidrio confundidos**
   - Dos frascos de champú se leyeron como vidrio y uno de vidrio como PET.
   - Todos van a la caneca blanca, pero el material registrado sería otro.
5. **Tubos de crema dental:** se leyeron como medicamento (4) o pañal (1). Ninguno abre la caneca.
6. **Sobres de condón y toallas:** se leyeron como envoltura de paquete (`snack_wrapper`), que es no aprovechable. La respuesta al ciudadano sigue siendo segura.

## Qué no dice esta ronda

- No reemplaza la prueba con fotos tomadas por el iPhone del equipo. Esa prueba de aceptación sigue **PENDIENTE**.
- No mide la apertura física: la caneca está en modo software.
- Ninguna de estas fotos se usa para «entrenar» el modelo. El modelo es el mismo. Lo que cambia entre rondas es el intérprete de nombres y el catálogo, y aquí no cambió nada después de medir.

## Siguiente paso sugerido

- Corregir los puntos 1 y 2: persona o electrodoméstico nunca deben ofrecerse como metal.
- Medir esa corrección con **otro** conjunto nuevo, no con este.
