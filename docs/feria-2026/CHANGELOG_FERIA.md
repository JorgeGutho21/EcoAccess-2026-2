# Cambios · datos de feria 2026

## 2026-11-26 · PC de Heyder · rama `feature/ecoaccess-datos-feria-2026` (base `8f00efb`)

**Oracle:** ninguna escritura. Migraciones aplicadas: ninguna. Entorno consultado: Oracle Cloud, solo lectura.

Archivos creados:
- `docs/feria-2026/`: `CONTEXTO_CONTINUACION.md`, `ESTADO_ACTUAL.md`, `INSPECCION_ORACLE.md`, `MANIFIESTO_LOTE.json`,
  `RESULTADOS_PRUEBAS.md`, `GUIA_EJECUCION.md`, `PROCEDIMIENTO_REVERSION.md`, `ENTREGA_CLAUDE_JORGE.md`, `PENDIENTES.md`, este archivo
  y `verificaciones/verificacion-SIM-FERIA-2026-20261126-0755.{md,json}`.
- `scripts/feria/`: `ComunOracle.ps1`, `Verificar-LoteFeria.ps1`, `Generar-GuiaDemostracion.ps1`, `Revertir-LoteFeria.ps1`,
  `Compilar-Y-Probar.ps1`.
- `database/07_operacion/T004__reversion_lote_feria_2026.sql` (preparado, **no ejecutado**).

Archivos modificados:
- `.gitignore`: excluye `ECOACCESS_CREDENCIALES_FERIA_*.txt` y `ECOACCESS_GUIA_DEMOSTRACION_FERIA*.txt`.

SHA del contenido: **`f57cf67`** (la rama puede tener encima este registro del SHA). Recuperar con
`git fetch origin` · `git switch feature/ecoaccess-datos-feria-2026`.

## 2026-11-26 · PC de Jorge · rama `integracion/datos-feria-2026`

**Oracle:** ninguna escritura nueva (solo verificaciones de lectura e inicios de sesión de las cuentas del lote). V036 y T004 sin aplicar.

- Integra la rama de Heyder con `infra/arranque-automatico` (arranque de la estación al encender Windows).
- **P3 resuelto:** publica `tests/herramientas/EcoAccess.P3.DatosFeria` (generador y verificador, sin contraseñas) y
  `docs/operacion/LOTE_DEMOSTRACION_FERIA.md`.
- **P4 resuelto:** los TXT de credenciales y guía estaban ausentes del escritorio; se regeneraron con `verificar` a partir de las
  credenciales privadas existentes (`%LOCALAPPDATA%\EcoAccessP3\privado\feria`), sin crear contraseñas nuevas.
- La guía ya no promete un filtro «Solo demostración» ni separación de estadísticas (dependen de V036): ahora pide aclarar que
  la actividad `@feria.ecoaccess.test` es sintética.
- Los borradores locales sin aprobar (V036/R036, T004/R_T004 de desactivación y `09_depurar_demo_simulacion.sql`) se sacaron de
  `database/` para que el instalador no pueda aplicarlos; quedan en `%LOCALAPPDATA%\EcoAccessP3\privado\feria\sql_no_aplicados`.
