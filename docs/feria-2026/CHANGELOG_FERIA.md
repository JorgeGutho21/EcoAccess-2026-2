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
