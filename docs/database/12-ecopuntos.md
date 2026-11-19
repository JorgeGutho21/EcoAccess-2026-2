# 12 · Ecopuntos

## Idea central: el saldo no se guarda, se suma

EcoAccess no tiene una columna «saldo» que se actualice. Cada vez que se ganan o se pierden puntos se agrega una **fila nueva** al libro `MOVIMIENTO_ECOPUNTOS`:
- **crédito:** puntos positivos;
- **reverso:** puntos negativos.

Las filas **nunca se modifican ni se borran**. El saldo es siempre:

```
saldo(ciudadano) = SUM(MOVIMIENTO_ECOPUNTOS.PUNTOS) de ese ciudadano
```

Así el saldo y el historial no pueden contradecirse, y cualquier saldo se puede explicar fila por fila. Lo calculan `VW_SALDO_ECOPUNTOS` y `PKG_ECOPUNTOS.saldo`; la Prueba E comprueba que dan lo mismo.

## Fórmula de un crédito

```
si la tarifa es por depósito (POR_UNIDAD = 0):  puntos = REGLA_ECOPUNTOS.PUNTOS
si la tarifa es por unidad   (POR_UNIDAD = 1):  puntos = REGLA_ECOPUNTOS.PUNTOS × DEPOSITO.CANTIDAD_VERIFICADA
```

**Condiciones** (todas comprobadas en Oracle):

| # | Condición | Dónde está |
|---|---|---|
| 1 | El depósito está **verificado** | `PKG_ECOPUNTOS.preparar` (−20201) |
| 2 | Hay **una** tarifa vigente del material verificado en ese momento (`DESDE_UTC ≤ ahora < HASTA_UTC` o sin fin). Sin tarifa no hay puntos | `regla_vigente` |
| 3 | Si es por unidad, hay conteo verificado y `puntos = tarifa × cantidad` | `calcular` (−20204) y **`TR_ECOP_UNIDADES`** (−20113) |
| 4 | El titular tiene cuenta (un invitado no acumula) | `preparar` (−20208) |
| 5 | Quien acredita no es el titular ni el verificador | `preparar` (−20101) |
| 6 | Un solo crédito inicial por depósito | índice único **`UQ_ECOP_CREDITO_INICIAL`** |
| 7 | Entre 1 y 1 000 000 puntos por crédito | `CK_ECOP_MOV`, `calcular` |

**Tarifas vigentes en Oracle Cloud** (18/11/2026; las siete son **por unidad**):

| Material | Puntos por unidad |
|---|---:|
| `LATA_ALUMINIO` | 5 |
| `PET` | 4 |
| `CARTON` | 3 |
| `METAL` | 3 |
| `VIDRIO` | 3 |
| `MULTICAPA` | 2 |
| `PAPEL` | 2 |

**Ejemplo de cálculo:** un depósito verificado de **3 botellas PET** → `4 × 3 = 12` ecopuntos. Si después un administrador corrige el conteo a 2, `PKG_ECOPUNTOS.recalcular` asienta un **reverso de −12** y un **crédito nuevo de 8**. El saldo queda en 8 y el historial muestra las tres filas.

## Reverso y corrección

- **Reverso:** solo administrador y con motivo. Agrega un movimiento de tipo 2 con los puntos en negativo y `ID_CREDITO_REVERTIDO` apuntando al crédito anulado.
  - Cada crédito se revierte **una sola vez** (`UQ_ECOP_REVERSO`).
  - El reverso es del mismo depósito y ciudadano: lo exige la FK compuesta `FK_ECOP_MOV_REVERTIDO`.
  - Un movimiento solo puede referirse a uno **anterior**: `CK_ECOP_SIN_AUTORREFERENCIA` exige id menor.
- **Corrección:** reverso del crédito anterior más un crédito nuevo con `ID_PREDECESOR`, dentro de la misma `OPERACION_ECOPUNTOS` de tipo 3.

## Idempotencia: por qué no se acredita dos veces

| Capa | Mecanismo |
|---|---|
| Aplicación C# | Cada operación lleva una `CLAVE`; repetirla devuelve la misma operación (`UQ_ECOP_OPERACION_CLAVE`) |
| PL/SQL | `acreditar` devuelve el crédito vigente si ya existe; sus claves son `BD-CRED-<depósito>`, `BD-REV-<crédito>` y `BD-COR-<crédito>` |
| Datos | Aunque las dos capas intentaran acreditar el mismo depósito, `UQ_ECOP_CREDITO_INICIAL` solo admite **un** crédito inicial |

Las pruebas F (PL/SQL) y `EcopuntosOracleTests` (C#) lo comprobaron en el esquema aislado.

## Por qué hoy no hay ecopuntos

Consultado en Oracle Cloud el 18/11/2026:

- El libro tiene **0 movimientos**.
- Hay 58 depósitos verificados, **todos del canal simulador**, verificados el **09/11/2026**.
- La primera tarifa empezó a regir el **11/11/2026**.

Al momento de verificarlos no existía una tarifa vigente, así que la regla «sin tarifa no hay puntos» deja el saldo en 0. **Es el comportamiento correcto, no un error.** Para ver ecopuntos reales hace falta un depósito verificado después del 11/11 de un material con tarifa, y que un operario o administrador lo acredite.

## Dónde está la regla (y el riesgo)

La misma regla existe en dos lugares que escriben el mismo libro:
- en C#: `ServicioEcopuntos` y `RepositorioEcopuntosOracle`;
- en Oracle: `PKG_ECOPUNTOS`.

Los datos están protegidos por las restricciones de la base, pero si alguien cambia la regla en un lado y no en el otro, los dos caminos darían puntos distintos. La propuesta, que **no se aplicó** porque cambia el comportamiento y requiere aprobación, está en [16-hallazgos.md](16-hallazgos.md) H-01.
