# Banco Colombia y medición final de la versión con comprobación de contenido (12/11/2026, mañana)

## Qué se midió

| Dato | Valor |
|---|---|
| Versión | Rama `desarrollo/mision-nocturna-ia` con la comprobación de contenido v2 (commit `4e42065`) |
| Banco Colombia | Objetos comunes en Colombia, de Wikimedia Commons con licencia libre. Se excluyeron los 1 397 títulos ya usados en rondas anteriores |
| Etiquetado | A ojo, antes de ejecutar el modelo. **Todo el banco es prueba**: no se usó para ajustar nada |
| Fotos evaluadas | **200 de un objeto** y 1 con varios residuos, de unas 450 candidatas |
| Grupos cubiertos | Alimentos y comida colombiana, electrónica (celulares, audífonos, mouse, cargadores, controles, parlantes, teclados, portátiles, cables), pilas y baterías, higiene, sanitarios, plástico, icopor, vidrio, papel y metal |
| Pendientes de descarga | Peligrosos (medicamentos, jeringas, químicos), ropa y objetos diversos: Wikimedia limita la velocidad. Esas rutas se prueban hoy por reglas, no con fotos |
| Prueba independiente de la noche | Las mismas 42 fotos, que nunca se usaron para ajustar |

## Resultado final

| Conjunto | Ruta correcta | Falsas admisiones | Ofertas indebidas | Abstenciones |
|---|---|---|---|---|
| Prueba de la noche (42), versión original `edcbc13` | 76,2 % | 3 | 2 | 2 |
| Prueba de la noche (42), versión final | **81,0 %** | **0** | 1 | 0 |
| Banco Colombia (200), versión final | **83,0 %** | **1** | 2 | 10 |
| **Total nunca usado para ajustar (242)** | **82,6 %** (200) | **1** | 3 | 10 |

Contando todas las fotos evaluadas en la noche y en la mañana: **471 fotos de un objeto y 11 con varios residuos.** Las 196
de entrenamiento son las únicas que se usaron para ajustar.

### Por grupo (banco Colombia, ruta correcta)

| Grupo | Fotos | Correctas | % |
|---|---:|---:|---:|
| Electrónica (celular, audífonos, mouse, cargador, control, parlante, teclado, portátil, cable) | 47 | 44 | 93,6 % |
| Pilas y baterías | 12 | 11 | 91,7 % |
| Alimentos y comida colombiana | 49 | 41 | 83,7 % |
| Vidrio | 6 | 5 | 83,3 % |
| Plástico | 17 | 14 | 82,4 % |
| Icopor y envolturas | 12 | 9 | 75,0 % |
| Papel | 4 | 3 | 75,0 % |
| Higiene (cepillo, crema dental, champú, desodorante, jabón, cuchilla) | 30 | 22 | 73,3 % |
| Sanitarios (papel higiénico, pañitos, toallas, hisopos, tapabocas) | 18 | 13 | 72,2 % |
| Cartón, metal y otros | 6 | 5 | — |

Los grupos con menos de 10 fotos no permiten conclusiones.

### La falsa admisión que queda

- Foto `carton-05`: cubetas de cartón apiladas con huevos.
- Eco dijo cartón, que abriría la BLANCA.
- La etiqueta fue conservadora: hay huevos a la vista. Las cubetas vacías sí son cartón aprovechable.

### Errores que no abren nada (seguros)

| Error | Efecto |
|---|---|
| Hisopos y pañitos leídos como envase plástico | Solo orienta: ese objeto nunca abre |
| Desodorante leído como químico | Ruta de peligrosos; no abre |
| Cuchillas leídas como caucho o crema dental | NEGRA, solo orientación. **Se pierde la advertencia de cortopunzante**: es el error más relevante que queda |
| Crema dental leída como envase plástico | Solo orienta |
| Frutas enteras, jabón, cremas | Abstención: Eco pide otra foto o que la persona elija |
| Un celular con forro leído como forro | Ruta NEGRA en vez de RAEE; no abre |

## Lo que esto significa

- **No hay reconocimiento visual sin errores.** Con 242 fotos que nunca vio, Eco acierta la ruta en el **83 %**.
- La comprobación de contenido llevó a **cero** las falsas admisiones peligrosas medidas: detergente, cloro, medicamento y
  aerosol. Queda 1 caso dudoso de cartón con huevos.
- La regla de diseño sigue siendo: **ante la duda, Eco orienta y no abre.**
- **Siguiente mejora, con un banco de ENTRENAMIENTO nuevo y no con este:**
  - las cuchillas (que no pierdan la advertencia de cortopunzante);
  - los hisopos y pañitos;
  - medir pilas, peligrosos y ropa cuando terminen de descargarse.

Detalle con la matriz de confusión y los errores uno por uno: `resultados/banco_colombia/analisis-final-colombia.md` y
`resultados/banco_noche/analisis-final-prueba.md`.

### Errores nuevos de electrónica y pilas (también seguros)

- Dos parlantes sostenidos en la mano se leyeron como «personas». Solo orientan, sin abrir. Se pierde la ruta RAEE.
- Una batería de cámara se leyó como gorra (reutilizar). No abre, pero se pierde la ruta de pilas.
