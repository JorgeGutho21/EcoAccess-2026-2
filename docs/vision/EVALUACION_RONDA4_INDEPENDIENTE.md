# Cuarta ronda independiente y corrección de personas y aparatos (10/11/2026)

**Estado:** VERIFICADO MEDIANTE SIMULACIÓN. Son fotos públicas pasadas al modelo local real, no fotos de un teléfono.

## Qué se corrigió y por qué

En la tercera ronda (`EVALUACION_RONDA3_INDEPENDIENTE.md`) el modelo **nombraba bien** y el intérprete no reconocía el nombre. Entonces la segunda lectura respondía «metal» con certeza alta y Echo ofrecía depositarlo. Nombres reales del modelo:

| Foto | Nombre del modelo | Antes | Ahora |
|---|---|---|---|
| Militares saludando | «Military officer saluting.» | oferta de metal | persona |
| Ventilador | «Fan.» | oferta de metal | aparato |
| Aire acondicionado | «Air conditioner.» | oferta de metal | aparato |

Cambio (commit `6e05b1a`):
- Palabras de personas: oficial, soldado, policía, enfermera, músico y oficios comunes.
- Palabras de aparatos: «fan», «heater», «conditioner», y los compuestos «air conditioner», «air conditioning» y «ac unit».
- «computer fan», «cooling fan» y «cpu fan» siguen siendo electrónicos, con prueba.
- 14 casos nuevos en pruebas unitarias.

## Cómo se midió sin hacer trampa

- **La tercera ronda deja de ser independiente:** el ajuste se hizo mirando sus fallos. Se muestra como referencia, no como prueba.
- **Cuarta ronda nueva**
  - 167 candidatas de Wikimedia Commons, con licencias libres; títulos en `resultados/ronda4_fuentes.json`.
  - Se excluyeron los 1 093 títulos de rondas anteriores.
  - Búsquedas nuevas: personas en oficios, ventiladores, calentadores, aires acondicionados, neveras, microondas, metal real, electrónicos y reciclables de control.
- **Etiquetado:** a ojo **antes** de ejecutar el modelo. Quedaron 141 fotos (`resultados/ronda4_etiquetas.txt`).
- **Ejecución:** modelo `lfm3-vl` local; admisión contra la caneca piloto 1093. No se ajustó nada después de medir.

## Resultados

| Conjunto | Clave correcta | Contexto | Falsas admisiones | Ofertas indebidas | Abstenciones | Fallos técnicos | Mediana / p95 |
|---|---|---|---|---|---|---|---|
| **Ronda 4 (independiente)** | **119/141 (84 %)** | 121 | **0** | 4 | 5 | 0 | 1,0 s / 3,3 s |
| Ronda 3, antes | 93/131 | 91 | 7 | 6 | 13 | 0 | 1,0 s / 2,9 s |
| Ronda 3, después (ya no independiente) | 106/131 | 104 | 7 | 3 | 5 | 0 | 1,1 s / 3,3 s |
| Colombia 195, antes | 149/195 | 156 | 3 | 2 | 18 | 0 | 2,5 s / 3,2 s |
| Colombia 195, después (control de retrocesos) | 150/195 | 161 | 3 | 2 | 17 | 0 | 2,6 s / 3,3 s |

Las falsas admisiones de las rondas 3 y Colombia son, en su mayoría, reciclables que irían igual a la caneca blanca pero con otro material registrado (por ejemplo, champú leído como vidrio). Detalle en cada documento.

### Ronda 4 por clase

| Clase | Correctas |
|---|---|
| Aparatos (ventiladores, calentadores, aires, neveras, microondas) | 43/47 |
| Personas | 31/37 |
| Electrónicos varios (ventiladores de PC, routers, placas) | 16/18 |
| Celulares | 7/7 |
| Botellas plásticas | 5/5 |
| Botellas y vasos de vidrio | 7/8 |
| Portátiles | 3/5 |
| Metal real (bandejas de aluminio, chatarra) | 3/8 |
| Cartón | 0/2 |

## Lo que sigue fallando (sin ajustar con estas fotos)

- **Ofertas indebidas (4)**
  - Un dibujo antiguo de un soldado con armadura, ofrecido como metal.
  - Una bobina de un circuito, ofrecida como metal.
  - Un portátil junto a un reloj, ofrecido como vidrio.
  - Un niño entre latas, ofrecido como plástico.
  - En los cuatro casos Echo **ofrece** el botón; no abre solo.
- **Metal real:** las bandejas de aluminio con comida se leen como comida o restos; la chatarra, como restos. Es un rechazo de más, no una apertura indebida.
- **Cartón aplanado o cargado por una persona:** se lee como mueble o queda incierto.

## Qué no dice esta ronda

- No reemplaza la prueba con fotos del iPhone del equipo: **PENDIENTE**.
- El modelo no se reentrenó: cambió el intérprete de nombres.
