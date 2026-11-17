# Seis representaciones de contenedor, reglas de disposición y orientación en Valledupar (12/11/2026)

> **No se movió ningún servomotor.** Las tres canecas reales quedaron configuradas y sin cambios. Las aperturas nuevas solo
> se probaron contra las tres virtuales.

## 1. Las seis representaciones

| Id lógico | Modo | Color | Contenedor Oracle | GPIO | Estado |
|---|---|---|---|---|---|
| REAL-BLANCA | Físico | Blanco | 1093 | 19 | Configurada. Validación estática (prueba de contrato). Física: pendiente de supervisión |
| REAL-NEGRA | Físico | Negro | 1401 | 18 | Igual |
| REAL-VERDE | Físico | Verde | 1400 | 23 | Igual |
| VIRTUAL-BLANCA | Simulado | Blanco | (admisión de 1093) | — | Pruebas funcionales completas en `SimuladorEstacionTresCanecas` |
| VIRTUAL-NEGRA | Simulado | Negro | (admisión de 1401) | — | Igual |
| VIRTUAL-VERDE | Simulado | Verde | (admisión de 1400) | — | Igual |

Definidas en `src/EcoAccess.P3.Logica/Servicios/RepresentacionesContenedor.cs`.

Garantías y cómo se comprueban:

| Garantía | Comprobación |
|---|---|
| No hay más contenedores: la virtual comparte el id de Oracle solo para consultar la admisión | Las virtuales no crean órdenes, eventos ni depósitos: el simulador no tiene acceso a Oracle |
| Los ids y GPIO reales no cambiaron (commits `1fc2443` y `c7eea5b`) | `LasRealesConservanSusIdsYGpioYCoincidenConElSimulador` |
| Las aperturas múltiples solo van a la estación virtual | `CoordinadorAperturasMultiples` lanza con cualquier ejecutor no simulado |
| El QR oficial `EcoAccessCanecaPiloto1` es único y no se tocó | No se crearon QR |
| Los depósitos simulados no llegan a las estadísticas reales | El simulador no escribe en Oracle |

### Aislamiento real/virtual

| Capa | Barrera |
|---|---|
| Proceso | `GuardiaHardware` bloquea los COM y los comandos a bridges de otro proceso en pruebas, evaluaciones y simulaciones |
| Coordinador | Solo acepta `IEjecutorAperturas.EsSimulado` |
| Catálogo | Las 235 entradas nuevas no tienen material: no abren ninguna caneca física |
| Telegram | Varios residuos en una foto → solo orienta; lo no admitido → solo texto y punto de entrega |

## 2. Reglas de disposición (matriz versionada `2026.11.12-1`)

Tabla completa generada desde el código: `docs/vision/CATALOGO_OBJETOS_COLOMBIA.md` (307 tipos).

| Grupo | Regla |
|---|---|
| Electrónica | **RAEE**, nunca la blanca por tener plástico o metal: mouse, AirPods, parlante, control, router, memoria USB, celular, computador… |
| Pilas y baterías | Recolección de pilas. Batería hinchada: advertencia de incendio |
| Ropa y accesorios | **Reutilizar o donar**: zapatos, tenis, gafas, morral, gorra… Ropa interior usada: no se dona por higiene. Prenda rota: trapo o reciclaje textil |
| Higiene | Distingue producto y empaque. Crema, protector solar y champú → **evaluar** (con producto no se recicla; no se abre nada). Tubo de crema dental y pañitos húmedos → NEGRA. Esmalte → peligroso. Preservativo nuevo y cerrado → no es residuo; empaque vacío → NEGRA |
| Sanitarios | NEGRA en bolsa cerrada: pañal, toalla higiénica, servilleta usada, algodón, hisopos, tapabocas, vendaje doméstico |
| Cortopunzantes | Jeringa, aguja, lanceta, cuchilla, pluma de insulina, cuchillo, vidrio roto → recipiente rígido y gestión especial |
| Comida | Restos, cáscaras y huesos → VERDE. Comida entera (arepa, plátano, yuca, mazorca) → **evaluar**: puede seguir siendo alimento |
| Aprovechables | Solo limpios y secos → BLANCA. Un aprovechable sucio → NEGRA (cartón con grasa, vaso de café de papel) |

Las respuestas siguen el formato pedido, corto y concreto:

> 🎧 Identifiqué unos audífonos inalámbricos.
> 🔌 Es un aparato eléctrico o electrónico: no va en las canecas de EcoAccess…
> 📍 Lugar: Centro Comercial Unicentro Valledupar · 🏠 Dirección: Cra. 9 #6-42, Valledupar · 🗺️ Ubicación: (enlace) ·
> ⚠️ Aparece en la lista oficial del programa, pero no hemos confirmado que siga activo ni que reciba unos audífonos
> inalámbricos: confírmalo antes de desplazarte.

## 3. Puntos de entrega en Valledupar

- Catálogo persistente: tabla `PUNTO_DISPOSICION`, creada por la migración V026.
- Reversión: R026.
- Semilla equivalente en `CatalogoPuntosValledupar`. Si la tabla no existe, el bot usa la semilla.

| Código | Lugar o programa | Dirección | Recibe | Estado | Fuente |
|---|---|---|---|---|---|
| VUP-UNICENTRO-LITO | Centro Comercial Unicentro Valledupar | Cra. 9 #6-42 | RAEE, pilas, lámparas | Listado sin confirmar | [Fundación Puntos Verdes Lito](https://fundacionpuntosverdes.com/puntos-de-recoleccion/) |
| VUP-CORPOCESAR-AMARILLO | CORPOCESAR, punto amarillo «Cierra el ciclo» | Km 2 vía La Paz, Lote 1 U.I.C. Casa e' Campo | Envases de insecticidas domésticos | Listado sin confirmar | [CORPOCESAR](https://www.corpocesar.gov.co/residuos-y-consumo-sostenible/programas-de-posconsumo) |
| PROG-PUNTO-AZUL | Punto Azul | — (droguerías) | Medicamentos | Solo programa | CORPOCESAR |
| PROG-PILAS-AMBIENTE | Pilas con el Ambiente | — | Pilas | Solo programa | El Tiempo |
| PROG-LUMINA | Lúmina | — | Bombillos | Solo programa | CVC |
| PROG-RED-POSCONSUMO | App Red Posconsumo | — | Varios | Solo programa | El Tiempo |

**Lo que no está verificado y se declara así:**
- El sitio de la Fundación Puntos Verdes Lito tenía un certificado TLS inválido. El dato de Unicentro viene de la búsqueda
  que indexa su página oficial.
- No se confirmó que el punto siga activo ni qué objetos concretos recibe.
- CORPOCESAR nombra otros puntos amarillos sin publicar su dirección: Jumbo, Homecenter, Areandina, Comfacesar,
  Supermercado Mi Futuro, UPC sede Sabanas y UDES. No se incluyen.
- **No hay puntos de donación de ropa, zapatos o gafas verificados:** Eco lo dice expresamente («pendiente de verificación»).

Por cada punto, una persona debe confirmar en el lugar o con el operador antes de cambiar el estado a `CONFIRMADO`.

## 4. Pruebas

| Prueba | Resultado |
|---|---|
| `MisionFinalCatalogoTests` y `ReconocimientoGeneralTests` (orientación) | Suite unitaria 2074/0/1 |
| `PuntosDisposicionOracleTests`, en el esquema aislado | 2/2 en dos corridas. Solo corre en `ECOACCESS_P3_PRUEBAS` |

Lo que cubre la suite unitaria:
- más de 300 claves;
- ninguna entrada ampliada con material;
- electrónica, ropa, higiene, cocina, salud y la matriz mínima;
- decisiones medidas intactas;
- escenarios B, D y E;
- las seis representaciones;
- la semilla sin direcciones inventadas;
- la caída de Oracle;
- Telegram sin órdenes.

**Pendiente con el propietario:**
- aplicar V026 en producción;
- confirmar los puntos;
- la prueba física supervisada de las tres canecas reales (`GUIA_PRUEBAS_SUPERVISADAS_13-11-2026.md`).
