# V024 — Estación piloto de tres canecas con el QR oficial

## Qué resuelve

La Resolución 2184 de 2019 define tres colores:
- **blanca**: aprovechables limpios y secos;
- **verde**: orgánicos aprovechables;
- **negra**: no aprovechables.

Los residuos peligrosos y los RAEE no van en ninguna de las tres: van a un punto posconsumo.

El piloto tenía una sola caneca (1093, QR oficial `EcoAccessCanecaPiloto1`). V024 la convierte en la caneca
**blanca** de una estación de tres. **El QR no cambia**: el mismo código lleva a la estación, y
`ResolutorDestino` elige el color según la categoría del material o el contexto del objeto reconocido.

| Caneca | Color | Estado | ESP32 | Ubicación |
|---|---|---|---|---|
| EcoAccess · Caneca piloto (1093, QR oficial) | BLANCA | en servicio (sin cambios) | `esp32-piloto-blanca` | izquierda |
| EcoAccess · Caneca verde (nueva, QR interno) | VERDE | fuera de servicio | `esp32-piloto-verde` | centro |
| EcoAccess · Caneca negra (nueva, QR interno) | NEGRA | fuera de servicio | `esp32-piloto-negra` | derecha |

Las dos nuevas quedan fuera de servicio hasta que exista la caneca física y un administrador las habilite. Su QR
interno no se imprime.

## La «cuarta caneca»

La base tiene cuatro canecas anteriores a V024:
- 1, 289 y 290 son la estación DEMO de D002 (blanca, verde y negra);
- 1093 es la caneca del piloto.

No se borró ninguna y no se cambió el QR de ninguna. El análisis está en `docs/operacion/PROGRESO.md`, Fase 0.

## Comportamiento

- El bot muestra en la respuesta a cada foto el color y la ubicación de la caneca que corresponde (`ConDestinoEstacionAsync`).
- Los restos de comida ya no reciben el aviso «EcoAccess todavía no dispone de un contenedor adecuado»: ahora se indica la caneca verde.
- **No cambia qué se abre.** La apertura por foto sigue reservada a los aprovechables, por la regla de
  `ServicioClasificacionImagen`. Lo orgánico y lo no aprovechable reciben orientación hasta que sus canecas físicas estén en servicio.
- Sin la tabla, sin configuración o con un fallo de Oracle, la respuesta queda igual que antes.

## Pruebas (10/11/2026)

| Prueba | Dónde | Resultado |
|---|---|---|
| V024 aplicada y ejecutada otra vez (idempotente) | esquema aislado `ECOACCESS_P3_PRUEBAS` | código 0 ambas veces |
| R024 (reversión) y reaplicación | esquema aislado | 4→2→4 canecas, 3→0→3 configuraciones |
| `EstacionTresCanecasOracleTests` (2) | esquema aislado y Oracle Cloud | 2/2 en ambos |
| `ResolutorDestinoTests` y flujo con foto (18) | unitarias | 18/18 |
| Aplicación en Oracle Cloud | `ECOACCESS_P3_APP` | código 0, 1,2 s |

**Conteos en Oracle Cloud antes y después**:
- CONTENEDOR 4→6
- CONFIGURACION_CANECA 3→6
- CONTENEDOR_MATERIAL 30→46
- ECO_MIGRACION_APLICADA 28→29

Ninguna otra tabla cambió y siguió habiendo 0 objetos inválidos. Huella SHA-256 registrada:
`01622d6c46acd429d2750e1bda473d4bf67dbd20366713ad04603034dcfd0aa7`.

## Reversión

`database/06_migraciones/reversion/R024__estacion_piloto_tres_canecas.sql`:
- se detiene sin borrar si las canecas nuevas ya tienen interacciones;
- conserva la caneca piloto, su QR y sus materiales.

No la ejecuta ningún instalador.
