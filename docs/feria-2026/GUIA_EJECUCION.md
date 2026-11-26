# Guía de ejecución · herramientas de feria

Todos los comandos se ejecutan desde la raíz del repositorio, en PowerShell, en cualquier PC de un integrante. Los scripts
encuentran la raíz solos (buscan `EcoAccess.P3.sln`) y no usan rutas de ningún equipo en particular.

## Requisitos

- SDK de .NET según `global.json` (10.0.100 o superior de la misma banda).
- Conexión a Oracle configurada con `scripts\instalacion\Configurar-Integrante.ps1` (User Secrets `ecoaccess-p3-framework`) o la
  variable de entorno `ECOACCESS_ORACLE_CADENA`. **Cada integrante usa sus propias credenciales**; los scripts nunca imprimen la
  cadena de conexión.
- `Oracle.ManagedDataAccess.dll`: lo toman de `%LOCALAPPDATA%\Programs\EcoAccess P3` o de la compilación del repositorio.

## Scripts (`scripts/feria/`)

| Script | Escribe en Oracle | Para qué |
|---|---|---|
| `Compilar-Y-Probar.ps1` | No | Restaura NuGet, compila Debug y Release y corre las pruebas que no tocan Oracle. No toca la instalación ni los servicios. |
| `Verificar-LoteFeria.ps1` | **No** (READ ONLY) | 46 comprobaciones del lote; informe `.md` + `.json` en `docs/feria-2026/verificaciones/`. Código 1 si hay fallas. |
| `Generar-GuiaDemostracion.ps1` | **No** (READ ONLY) | `ECOACCESS_GUIA_DEMOSTRACION_FERIA.txt` en el escritorio, con cuentas elegidas por datos reales. Sin contraseñas. |
| `Revertir-LoteFeria.ps1` | Solo con `-Aplicar -Confirmar SIM-FERIA-2026` | Sin parámetros muestra qué borraría; `-ValidarSintaxis` compila T004 sin ejecutarlo. |
| `ComunOracle.ps1` | — | Funciones comunes (raíz, controlador, conexión de solo lectura). |

## Secuencia recomendada

```powershell
powershell -ExecutionPolicy Bypass -File scripts\feria\Compilar-Y-Probar.ps1
```
```powershell
powershell -ExecutionPolicy Bypass -File scripts\feria\Verificar-LoteFeria.ps1
```
```powershell
powershell -ExecutionPolicy Bypass -File scripts\feria\Generar-GuiaDemostracion.ps1
```

Parámetros útiles: `-Lote` (formato `SIM-XXXX`), `-Salida`, `-MedirProgreso 0` (no esperar para medir si otro equipo escribe),
`-Destino` (carpeta de la guía; por defecto el escritorio real del usuario de Windows).

## Archivos que nunca van a GitHub

`ECOACCESS_CREDENCIALES_FERIA_*.txt`, `ECOACCESS_GUIA_DEMOSTRACION_FERIA*.txt`, Wallet, `.env`, User Secrets. El `.gitignore`
los excluye. Antes de cada push: `git status` y `git diff --cached --stat`.

## Generar o completar otro lote

El generador que creó `SIM-FERIA-2026` está solo en el PC de Jorge (PENDIENTES P3). No volver a ejecutarlo sobre el mismo lote
desde otro equipo: el lote está COMPLETO y las cuentas ya tienen contraseña. Un lote nuevo debe:
1. Ensayarse primero en `ECOACCESS_P3_PRUEBAS*` (los tests de integración y los fixtures se niegan a correr fuera de él).
2. Usar canales 3/4 o aplicar antes la corrección P1, para no mezclarse con los datos operativos.
3. Verificarse con `Verificar-LoteFeria.ps1 -Lote <nuevo>` antes y después.
