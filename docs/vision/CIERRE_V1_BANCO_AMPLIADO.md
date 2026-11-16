# Cierre V1: banco ampliado de 915 fotos y medición antes/después (15/11/2026)

**No hubo reentrenamiento.** El modelo es el mismo (`lfm3-vl:latest` en Ollama, temperatura 0, sin «thinking»). Lo que cambió son
reglas: preguntas de sí o no adicionales, vocabulario y la política de apertura. Las cifras de este documento se reproducen con
los manifiestos y los informes JSON de `docs/vision/resultados/banco_ampliado_v1/`.

## 1. Banco de imágenes

| Concepto | Cantidad |
|---|---:|
| Candidatas descargadas de Wikimedia Commons (licencia libre) | 3 395 |
| Aceptadas tras revisión visual una por una | **915** |
| Meta de la misión | 1 000 (no se alcanzó; se prefirió calidad a cantidad) |
| Duplicados exactos o casi exactos (dHash) excluidos | 71 |
| Autores distintos | 768 (máximo 3 fotos por autor y categoría) |

Por grupo y conjunto (separación por autor con SHA-256: ninguna persona aporta fotos a dos conjuntos):

| Grupo | Entrenamiento | Validación | Prueba | Total |
|---|---:|---:|---:|---:|
| Aprovechable | 118 | 33 | 29 | 180 |
| Orgánico | 182 | 45 | 39 | 266 |
| No aprovechable | 142 | 34 | 33 | 209 |
| Especial / ambiguo / no admitido | 163 | 59 | 38 | 260 |
| **Total** | **605** | **171** | **139** | **915** |

Por contexto de la etiqueta: ObjetoCotidiano 368, ResiduoEspecial 121, ResiduoOrganico 118, ResiduoAprovechable 93,
ResiduoNoAprovechable 78, ResiduoElectronico 61, ResiduoSanitario 61, AnimalVivo 8, PlantaViva 4, Persona 3. Solo 87 fotos
deberían abrir la blanca (envases limpios y vacíos); 119 son envases con contenido. Es la carencia principal del banco.

Licencias principales: CC BY-SA 4.0 (375), CC BY-SA 3.0 (174), CC0 (96), CC BY 2.0 (84), dominio público (52). Cada foto
conserva autor, título, licencia, URL, búsqueda y SHA-256 en `banco.csv`. Las fotos no se suben al repositorio.

Las marcas colombianas aparecen solo donde Commons tiene fotos libres; Eco clasifica material y estado, no el logotipo.

## 2. Método

- Etiquetas puestas a ojo **antes** de ejecutar el modelo (`revision.txt`), con claves que existen en el catálogo.
- Las reglas se decidieron mirando **solo entrenamiento**; validación sirvió para confirmar. **Prueba no se usó para ajustar
  nada**: se reporta tal cual salió.
- El evaluador admite como máximo 400 muestras, así que entrenamiento se partió en dos mitades (A 303, B 302) sin mover fotos
  de conjunto.
- Ejecución: `EcoAccess.P3.Bot.exe --evaluar-objetos <manifiesto> local` con `ECOACCESS_SIN_HARDWARE=1` (sin COM3, sin servos).
  Análisis: `scripts/pruebas/vision/Analizar-ResultadosBanco.ps1`.

Versiones comparadas:

| Versión | Commit | Qué cambia |
|---|---|---|
| Línea base | `0bf18c5` | Reglas del 14/11 («¿tiene algo dentro?») |
| Final intermedia | `90e72a4` | + pila, cortante y papel usado; «¿se ve vacío?» **reemplaza** a «¿tiene algo dentro?» |
| **V1 (adoptada)** | este cierre | + pila, cortante y papel usado; un envase solo abre si **las dos** preguntas dicen vacío |

## 3. Resultados

Aperturas automáticas = la blanca o la caneca de la estación (verde/negra) se abriría sin intervención humana.

| Conjunto | Versión | Ruta correcta | Aperturas | Equivocadas (blanca / estación) | Precisión de apertura | Abstenciones | Latencia mediana / p95 |
|---|---|---:|---:|---|---:|---:|---|
| Validación (171) | Línea base | 73,1 % | 30 | 0 (0 / 0) | 100 % | 5 | 3,2 s / 8,7 s |
| Validación (171) | Final intermedia | 72,5 % | 34 | 3 (3 / 0) | 91,2 % | 5 | 3,2 s / 7,0 s |
| Validación (171) | **V1** | 72,5 % | 28 | **0 (0 / 0)** | **100 %** | 5 | 3,5 s / 9,1 s |
| Prueba (139) | Línea base | 69,8 % | 26 | 2 (1 / 1) | 92,3 % | 9 | 3,4 s / 8,7 s |
| Prueba (139) | Final intermedia | 71,2 % | 28 | 4 (3 / 1) | 85,7 % | 8 | 3,1 s / 7,9 s |
| Prueba (139) | **V1** | **71,9 %** | 25 | **1 (0 / 1)** | **96,0 %** | 8 | 3,5 s / 9,1 s |
| Entrenamiento A (303) | Final intermedia | 79,9 % | 60 | 1 (1 / 0) | 98,3 % | 16 | 3,2 s / 7,5 s |
| Entrenamiento A (303) | **V1** | 79,2 % | 56 | **0** | **100 %** | 15 | 3,4 s / 8,0 s |
| Entrenamiento B (302) | Final intermedia | 73,2 % | 62 | 4 (1 / 3) | 93,5 % | 10 | 3,5 s / 8,8 s |
| Entrenamiento B (302) | **V1** | 72,5 % | 60 | 3 (0 / 3) | 95,0 % | 10 | 3,5 s / 8,8 s |

La línea base en entrenamiento se midió solo sobre el banco anterior de 584 fotos (78,1 %, 59 aperturas, 3 equivocadas); la
corrida sobre las 605 se cortó para dar tiempo a la versión V1.

**Lectura honesta**

- La versión final intermedia ganaba rutas pero abría mal la blanca más veces que la línea base (3 en validación, 3 en prueba).
  Era una regresión de seguridad y no se entrega.
- V1 no abre mal la blanca en ningún conjunto (0 de 915). En prueba sube la ruta correcta (69,8 → 71,9 %) y la precisión de
  apertura (92,3 → 96,0 %).
- El precio es la cobertura: menos aperturas automáticas (prueba 26 → 25, validación 30 → 28). Con las dos preguntas, en los 44
  envases de entrenamiento pasan como vacíos 0 de 29 con contenido, pero se bloquean 7 de 15 vacíos, que van a revisión sin abrir.
  Un caso real: la lata aplastada de TrashNet de la prueba de integración ahora se trata como envase con contenido.
- La meta del 90 % de ruta correcta **no se alcanzó**.

## 4. Errores peligrosos e incidentes

Ninguna foto de ningún conjunto abre una caneca con un residuo peligroso (pilas, medicamentos, cortopunzantes, químicos,
aerosoles, RAEE). Errores de apertura que quedan con V1 (todos de la estación, no de la blanca):

| Foto | Conjunto | Qué pasa | Gravedad |
|---|---|---|---|
| aplastico-069 | prueba | Recipiente plástico con comida leído como «comida servida» → verde | Contaminación (el envase va con lo orgánico) |
| aplastico-053, aplastico-061 | entrenamiento | Igual que el anterior | Igual |
| nservilleta-098 | entrenamiento | Servilleta limpia leída como usada → negra | Baja (iría a no aprovechables) |

Incidentes reportados por la persona, medidos en el banco con V1:

| Incidente | Fotos | Ruta correcta | Aperturas indebidas |
|---|---:|---:|---:|
| Bolsa de agua | 8 | 6 | 0 |
| Pilas (botón, AA, litio) | 49 | 41 | 0 |
| Blíster de medicamento | 10 | 8 | 0 |
| Cubeta de huevos | 1 | 0 (leída como recipiente plástico) | 0 |

Las pilas mal nombradas lo fueron como lápiz, gorra, SIM, teléfono o «objeto metálico»; ninguna abre. La cubeta de huevos tiene
una sola foto libre: no hay muestra suficiente para afirmar nada.

## 5. Pendiente

- Pregunta para «comida servida» dentro de un envase (los 3 casos de arriba). Se dejó sin cambiar para no ajustar sin tiempo de
  volver a medir.
- Más envases limpios y vacíos (87 de 915) y más cubetas de huevos.
- Validación física supervisada (sección de pasos en el documento maestro).

> **Actualización (16/11/2026):** el recipiente con comida quedó corregido en `docs/vision/CIERRE_SOFTWARE_COMIDA_EN_RECIPIENTE.md` (aperturas equivocadas en las 915 fotos: 15 → 1; prueba: 0).
