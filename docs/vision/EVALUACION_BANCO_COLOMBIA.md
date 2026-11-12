# Banco Colombia y medición final de la versión con comprobación de contenido (12/11/2026, mañana)

## Qué se midió

| Dato | Valor |
|---|---|
| Versión | Rama `desarrollo/mision-nocturna-ia` con la comprobación de contenido v2 (commit `4e42065`) |
| Banco Colombia | Objetos comunes en Colombia, de Wikimedia Commons con licencia libre. Se excluyeron los 1 397 títulos ya usados en rondas anteriores |
| Etiquetado | A ojo, antes de ejecutar el modelo. **Todo el banco es prueba**: no se usó para ajustar nada |
| Fotos evaluadas | **77 de un objeto** y 1 con varios residuos. La descarga de electrónica, higiene, ropa, pilas y peligrosos seguía en curso: Wikimedia limita la velocidad |
| Prueba independiente de la noche | Las mismas 42 fotos de antes, que nunca se usaron para ajustar |

## Resultado final

| Conjunto | Ruta correcta | Falsas admisiones | Ofertas indebidas | Abstenciones |
|---|---|---|---|---|
| Prueba de la noche (42), versión original `edcbc13` | 76,2 % | 3 | 2 | 2 |
| Prueba de la noche (42), versión final | **81,0 %** | **0** | 1 | 0 |
| Banco Colombia (77), versión final | **77,9 %** | **1** | 2 | 6 |

Total con fotos nunca usadas para ajustar: **119 fotos, 94 rutas correctas (79,0 %) y 1 falsa admisión.**

**La falsa admisión que queda:**
- Foto `carton-05`: cubetas de cartón apiladas con huevos.
- Eco dijo cartón, que abriría la BLANCA.
- La etiqueta fue conservadora: hay huevos a la vista. Las cubetas vacías sí son cartón aprovechable.

**Errores que no abren nada (seguros):**
- abstenciones ante frutas enteras, yuca y mazorca;
- icopor leído como cartón o envase: el resultado se mostró sin abrir;
- un florero leído como reutilizable;
- una botella leída como envase químico: se pierde una apertura, pero no es peligroso.

## Lo que esto significa

- **No hay reconocimiento visual sin errores.** Con fotos que nunca vio, Eco acierta la ruta en 8 de cada 10.
- La comprobación de contenido llevó a **cero** las falsas admisiones peligrosas medidas: detergente, cloro, medicamento y
  aerosol. Queda 1 caso dudoso de cartón con huevos.
- La regla de diseño sigue siendo: **ante la duda, Eco orienta y no abre.**

Detalle con la matriz de confusión y los errores uno por uno: `resultados/banco_colombia/analisis-final-colombia.md` y
`resultados/banco_noche/analisis-final-prueba.md`.
