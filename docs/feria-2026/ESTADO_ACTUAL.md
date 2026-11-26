# Datos de feria 2026 · Estado actual

> Última actualización: **2026-11-26 07:55 UTC** (02:55 Colombia), desde el PC de Heyder, rama `feature/ecoaccess-datos-feria-2026`
> (base `8f00efb` de `main`). Todo lo de este documento se obtuvo con consultas de **solo lectura** (`SET TRANSACTION READ ONLY`).

## Resumen en una línea

El lote **`SIM-FERIA-2026` ya está cargado y COMPLETO en Oracle Cloud (producción)**. Lo generó Claude desde el PC de Jorge
(no desde un esquema aislado). Su integridad es correcta, pero **se mezcla con la actividad real** en varias estadísticas
(ver `PENDIENTES.md`, P1 y P2). Desde el PC de Heyder **no se escribió nada** en Oracle.

## Cronología observada

| Hora UTC | Observación |
|---|---|
| 07:20 | Se crea `LOTE_SIMULACION` `SIM-FERIA-2026` (semilla `20261126`). |
| 07:41 | 1168 filas trazadas, 564 depósitos, estado `EN_CURSO`, 0 Ecopuntos. |
| 07:42 | 594 depósitos: el proceso seguía escribiendo. Heyder decide **dejarlo terminar**. |
| 07:44 | 644 depósitos. |
| 07:48 | Estado `COMPLETO`, 1404 filas, 650 depósitos, 173 movimientos de Ecopuntos; sin cambios en 20 s. |

## Lo que contiene el lote (verificado)

| Concepto | Cantidad |
|---|---|
| Cuentas `@feria.ecoaccess.test` | 64: 50 ciudadanos (rol 1), 8 operarios (rol 2), 4 supervisores + 2 administradores (rol 3) — todas con contraseña |
| Cuentas reales intactas | 55 (total en la base: 119) |
| Interacciones | 690: Telegram 466 · Manual 40 · Simulador 30 · Asistido 154 |
| Invitados (sin cuenta, con depósito) | 30 |
| Aperturas manuales sin cuenta ni depósito | 40 |
| Depósitos | 650 (verificados 510 · pendientes 98 · rechazados 42) |
| Por caneca | verde 298 (45,8 %) · blanca 246 (37,8 %) · negra 106 (16,3 %) |
| Materiales | 17 distintos, todos admitidos por su caneca y habilitados |
| Antigüedad (días) | 1–7: 100 · 8–14: 110 · 15–21: 95 · 22–30: 95 · 31–60: 150 · 61–90: 100 (igual al plan) |
| Eventos de dispositivo | 1950 sintéticos (`id_externo` `sim-…`): acuse, apertura y cierre sin sensor por orden |
| Ecopuntos | 168 créditos + 5 reversos = 173 movimientos; +1323 −49 = **1274 netos**, solo a cuentas del lote |
| Saldos de los 50 ciudadanos | 0: 14 · 1–20: 15 · 21–100: 19 · >100: 2 |

Las 46 comprobaciones de `scripts/feria/Verificar-LoteFeria.ps1` dan **0 fallas y 2 avisos**:
`verificaciones/verificacion-SIM-FERIA-2026-20261126-0755.md`.

## Riesgos abiertos (detalle en PENDIENTES.md)

1. **P1 · El Dashboard institucional muestra el lote como «Datos operativos».** Las vistas `EA_R_BASE_*` solo marcan como ensayo
   los canales 3 y 4 o nombres «SIMULACI/FICTIC»; no cruzan `REGISTRO_SIMULACION`. En los últimos 100 días el alcance operativo
   tiene 631 depósitos y **620 son del lote**.
2. **P2 · 650 aperturas sintéticas en las canecas reales** frente a 40 aperturas reales; `RepositorioEstadisticasOracle` las suma.
3. **P3 · El generador `tests/herramientas/EcoAccess.P3.DatosFeria` no está en GitHub**: solo existe en el PC de Jorge.
4. **P4 · Las credenciales de las 64 cuentas** solo existen en el PC de Jorge (no se regeneran desde otro equipo).

## Lo que NO se hizo desde el PC de Heyder

- Ninguna escritura en Oracle (ni nube ni esquema aislado). Oracle XE local detenido, Docker sin iniciar.
- No se detuvo ni se tocó el proceso que cargó el lote.
- No se generaron contraseñas ni el archivo de credenciales.
- No se cambió código C#, vistas ni interfaces.

## Actualización 2026-11-26 (PC de Jorge)

Lote sin cambios: verificación de lectura 0 fallas / 2 avisos (650 depósitos, 64 cuentas), y 95/95 comprobaciones de inicio de
sesión por rol. Integración en `integracion/datos-feria-2026`; detalle en `RESULTADOS_PRUEBAS.md` y `CHANGELOG_FERIA.md`.
