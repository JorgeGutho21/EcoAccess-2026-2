# Contexto para continuar · datos de feria ExpoSoftware 2026

Este documento permite retomar el trabajo sin la conversación anterior.

## Objetivo

Un conjunto de datos sintéticos, identificable y reversible, para demostrar EcoAccess en ExpoSoftware: 50 ciudadanos con perfiles
distintos, 8 operarios, 4 supervisores, 2 administradores, ~30 invitados y ~650 depósitos en 90 días, con Ecopuntos acreditados
por el flujo oficial.

## Qué pasó

1. Claude, en el PC de Jorge, desarrolló `tests/herramientas/EcoAccess.P3.DatosFeria` y generó el lote `SIM-FERIA-2026`
   **directamente en Oracle Cloud** el 2026-11-26 (07:20–07:48 UTC). La cuenta de Jorge llegó a su límite durante el trabajo.
2. Heyder retomó desde su PC: encontró el lote a medio cargar y en escritura, y decidió **dejarlo terminar**.
3. Se verificó en solo lectura (0 fallas), se documentó, se preparó la reversión T004 (sin ejecutar) y se generó la guía.

## Decisiones técnicas

- **Reutilizar V025** (`LOTE_SIMULACION` / `REGISTRO_SIMULACION`) como trazabilidad del lote: es lo que ya usa el proyecto y lo
  que permite verificar y revertir sin tocar nada ajeno.
- **Verificar y no reescribir**: el lote ya está completo; un segundo generador desde otro PC duplicaría o chocaría.
- **Reversión con el patrón de T003**: precondiciones que detienen sin cambios, respaldo `ECO_RESP_T004_*`, una sola transacción
  y comprobación final de que lo ajeno al lote no cambió.
- **No modificar vistas ni código** para separar el lote del dashboard (P1/P2): es una decisión del equipo.

## Dónde está cada cosa

| Qué | Dónde |
|---|---|
| Estado y cifras | `docs/feria-2026/ESTADO_ACTUAL.md` |
| Esquema, roles, tarifas, firmas | `docs/feria-2026/INSPECCION_ORACLE.md` |
| Pendientes y opciones | `docs/feria-2026/PENDIENTES.md` |
| Pasos en el PC de Jorge | `docs/feria-2026/ENTREGA_CLAUDE_JORGE.md` |
| Scripts | `scripts/feria/` |
| Reversión | `database/07_operacion/T004__reversion_lote_feria_2026.sql` · `docs/feria-2026/PROCEDIMIENTO_REVERSION.md` |
| Informes de verificación | `docs/feria-2026/verificaciones/` |

## Si se corta el trabajo

Checkpoint: rama `feature/ecoaccess-datos-feria-2026`. El siguiente paso es el 1 de `ENTREGA_CLAUDE_JORGE.md`. Para saber si el
lote cambió desde este registro, ejecutar `Verificar-LoteFeria.ps1` y comparar su `.json` con el de `verificaciones/`.
