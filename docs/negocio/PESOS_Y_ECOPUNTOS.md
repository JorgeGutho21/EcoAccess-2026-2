# Pesos estimados y ecopuntos

## Pesos (fase 5): `CatalogoPesosEstimados`, versión 2026.11.10-1

- `peso_medido_kg` solo lo llena una báscula. Una foto o una declaración **nunca** producen un peso medido.
- Sin báscula, el depósito guarda en `peso_estimado_kg` el resultado de `kg por unidad × unidades declaradas`, con tres decimales. Si no se declararon unidades, cuenta 1.
- No se estima si:
  - el material no tiene referencia;
  - la cantidad es menor que 1 o mayor que 50.

  En esos casos el depósito queda «sin peso».
- Los valores son de orden de magnitud para envases comunes y no se midieron en el proyecto. Cambiarlos exige una versión nueva.

| Material | kg por unidad | Unidad típica |
|---|---|---|
| PET | 0,025 | botella de 600 ml |
| LATA_ALUMINIO | 0,015 | lata de 330 ml |
| METAL | 0,050 | lata de acero |
| CARTON | 0,100 | caja pequeña plegada |
| PAPEL | 0,005 | hoja carta |
| VIDRIO | 0,250 | botella de 330 ml |
| MULTICAPA | 0,030 | envase de 1 L |
| PEAD | 0,040 | envase de detergente pequeño |
| PLASTICO_RIGIDO | 0,030 | recipiente rígido |
| PLASTICO_FLEXIBLE, BOLSA_PLASTICA | 0,005 | empaque o bolsa |
| ORGANICO, RESTOS_ALIMENTOS | 0,150 | porción |
| SERVILLETA_USADA | 0,003 | servilleta |
| ICOPOR_CONTAMINADO | 0,010 | recipiente |

> **Actualización 21/11/2026:** la matriz vigente en el código es la versión 2026.11.21-1 (lata 10, PET 8, cartón 7, vidrio 7, papel 5, orgánico 2, caneca negra 0), pendiente de aprobación; se aplica en Oracle con `database/07_operacion/T002__tarifas_ecopuntos_2026_11_21.sql`. Detalle en `docs/desarrollo/CATALOGO_FOTOS_ECOPUNTOS.md`. Lo siguiente describe la versión anterior.

## Ecopuntos (fase 6): `MatrizEcopuntosPropuesta`, versión 2026.11.10-1

| Material | Puntos por unidad |
|---|---|
| Lata de aluminio | 5 |
| PET | 4 |
| Cartón | 3 |
| Vidrio | 3 |
| Acero (METAL) | 3 |
| Papel | 2 |
| Multicapa | 2 |

Los flexibles, el icopor, los orgánicos, los no aprovechables, los sanitarios y los peligrosos dan **0** puntos. La
categoría manda: un material que no sea aprovechable da 0 aunque su código esté en la tabla.

**Estados** (se deducen de los datos, no se guardan aparte):

| Estado | Cuándo |
|---|---|
| Provisional | el depósito existe y no está verificado; los puntos son potenciales |
| Confirmado | un operario lo verificó y los puntos netos son mayores que 0 |
| Anulado | verificado con puntos netos 0, por reversión o porque el material no da puntos |

**Configurable**:
- En Oracle cada valor es una `regla_ecopuntos` por unidad, con vigencia.
- El script `database/07_operacion/T001__tarifas_ecopuntos_propuestas.sql` la crea. No lo ejecuta ningún instalador.
  - Exige un **administrador real** como creador y se negó con un id vacío y con uno inexistente.
  - No duplica una regla vigente: en el esquema aislado, dos ejecuciones crearon 0 reglas nuevas.
  - Las siete reglas ya existían por el fixture del piloto simulado, así que **la rama que inserta reglas nuevas no se ejecutó** en esta prueba.
- **Oracle Cloud sigue con 0 reglas.** Falta una cuenta Administrador real que ejecute T001.

Pruebas: `PesosYEcopuntosTests` (25 casos) y `ProcesarAsync_DepositoSinBascula_MuestraSoloElPesoEstimadoDelCatalogo`.
