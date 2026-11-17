# V025 y lote de SIMULACIÓN (fases 7 y 8)

## Qué es y qué no es

El lote `SIM-2026-11-CIERRE` contiene **50 ciudadanos ficticios** y **100 depósitos sintéticos** en Oracle Cloud,
para que los paneles y Power BI tengan datos con qué demostrarse.

**No son depósitos físicos ni pesajes reales:**
- su peso es siempre *estimado* (`peso_medido_kg` vacío en los 100);
- los ciudadanos usan el dominio reservado `simulacion.ecoaccess.invalid`;
- no tienen contraseña ni Telegram, así que no pueden iniciar sesión.

## Cómo se separa de lo real

| Mecanismo | Efecto |
|---|---|
| `REGISTRO_SIMULACION` (V025) | cada fila creada queda anotada con lote y clave: exclusión exacta e idempotencia |
| Canal Simulador (3) y estación «EcoAccess · Estación de SIMULACIÓN …» | las vistas de informes de V015 la marcan `es_ensayo = 1` y la excluyen por defecto |
| `vista_depositos_estado.es_simulacion` / `id_lote` | consultas y paneles filtran `es_simulacion = 0` por defecto; el «modo experimental» filtra por el lote |

Comprobado en la nube (11/11/2026):
- `ea_r_base_depositos` con `es_ensayo = 0` (informes reales): **0** depósitos;
- con `es_ensayo = 1` (modo experimental): **100**.

## Cómo se generó

`EcoAccess.P3.Bot.exe --generar-lote-simulacion SIM-2026-11-CIERRE` llama a `GeneradorLoteSimulacion`, que usa los
**mismos servicios que el bot**: control de acceso, consentimiento, ventana de la orden, idempotencia de eventos y
estimación de peso. El reloj propio reparte los depósitos en los 30 días anteriores. La semilla fija 20261110 hace el lote
reproducible: el esquema aislado y la nube dieron la misma distribución.

| Paso | Resultado en Oracle Cloud |
|---|---|
| Primera ejecución | 233 s, código 0: 50 ciudadanos, 100 depósitos, 0 rechazos |
| Segunda ejecución | código 0: 0 nuevos, 100 existentes |
| Conteos antes → después | USUARIO 21→72, PUNTO_INSTALACION 2→3, CONTENEDOR 6→9, INTERACCION/ORDEN/DEPOSITO 0→100, EVENTO_DISPOSITIVO 0→336, REGISTRO_SIMULACION 0→255 |
| Objetos inválidos | 0 |

Las 255 filas son 1 estación, 3 canecas, 51 cuentas (50 ciudadanos y 1 operario ficticio que verifica), 100
interacciones y 100 depósitos. Los 336 eventos son 36 depósitos con sensor × 4 eventos más 64 sin sensor × 3.

Distribución (`database/04_consultas/11_consultas_cierre.sql`, consulta 3):

| Estado | Blanca | Verde | Negra |
|---|---|---|---|
| DECLARADA_SIN_SENSOR | 24 | 3 | 0 |
| DETECTADA_POR_SENSOR | 14 | 1 | 0 |
| VERIFICADA | 47 | 9 | 2 |

Hay pocos depósitos en la caneca negra: la mezcla del lote es 86 % aprovechables y orgánicos, y la semilla fija
reprodujo esa proporción.

## Pruebas

- V025 en el esquema aislado, ejecutada dos veces: código 0 ambas. Es repetible, porque crea las tablas solo si no existen.
- `LoteSimulacionOracleTests` (esquema aislado) comprueba:
  - 50 y 100 filas creadas;
  - una segunda ejecución sin filas nuevas;
  - 0 pesos medidos;
  - el lote COMPLETO;
  - 0 filas en los informes reales.

  En otra base queda inconclusa: nunca genera datos en la nube por sí sola.
- Huella SHA-256 de V025 registrada en la nube: `cf846b3a23fa927eb34ab336930040000ddd78c802faebc88644b737ae92d07a`.

## Reversión

`R025` quita las tablas y vistas solo si no hay lotes. Los datos del lote **no se borran**: la autorización para crear
registros experimentales no autoriza borrar. Para retirarlos de una demostración basta filtrar `es_simulacion = 0`, que
ya es el valor por defecto.
