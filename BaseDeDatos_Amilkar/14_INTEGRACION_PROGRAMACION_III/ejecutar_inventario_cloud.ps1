<#
  Ejecuta inventario_oracle_cloud_SOLO_LECTURA.sql contra Oracle Cloud (P3) en SOLO LECTURA.
  - La contraseña la escribe la persona dueña de la cuenta cuando SQL*Plus la pide (no se guarda en ningún archivo).
  - La sesión se abre con SET TRANSACTION READ ONLY: Oracle rechazaría cualquier INSERT/UPDATE/DELETE.
  - La salida queda en 16_EVIDENCIAS_Y_PRUEBAS/salidas/inventario_oracle_cloud_<fecha>.txt
  Uso:  powershell -File 14_INTEGRACION_PROGRAMACION_III\ejecutar_inventario_cloud.ps1
#>
param([string]$Usuario = 'ECOACCESS_P3_APP', [string]$Alias = 'ecoaccess_tp')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$tmp = Join-Path $env:TEMP 'ecoinv'; New-Item -ItemType Directory -Force $tmp | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'inventario_oracle_cloud_SOLO_LECTURA.sql') (Join-Path $tmp 'inv.sql') -Force
$sello = Get-Date -Format 'yyyyMMdd_HHmm'
@"
SET TRANSACTION READ ONLY;
SET LINESIZE 200 PAGESIZE 500 TRIMSPOOL ON FEEDBACK ON TAB OFF
COLUMN banner_full FORMAT A120
COLUMN table_name FORMAT A32
COLUMN constraint_name FORMAT A32
COLUMN tabla_referenciada FORMAT A32
COLUMN object_name FORMAT A40
COLUMN view_name FORMAT A40
SPOOL inventario.txt
PROMPT ===== Inventario Oracle Cloud (solo lectura) $sello
@inv.sql
PROMPT ===== FIN DEL INVENTARIO
SPOOL OFF
ROLLBACK;
EXIT
"@ | Set-Content (Join-Path $tmp 'correr.sql') -Encoding ascii
Push-Location $tmp
try {
  Write-Host "Conectando a $Usuario@$Alias (TNS_ADMIN=$env:TNS_ADMIN). Escriba la contraseña cuando SQL*Plus la pida:" -ForegroundColor Cyan
  sqlplus -L "$Usuario@$Alias" '@correr.sql'
} finally { Pop-Location }
$dest = Join-Path $raiz "16_EVIDENCIAS_Y_PRUEBAS\salidas\inventario_oracle_cloud_$sello.txt"
if (Test-Path (Join-Path $tmp 'inventario.txt')) { Copy-Item (Join-Path $tmp 'inventario.txt') $dest -Force; Remove-Item (Join-Path $tmp 'inventario.txt'); Write-Host "Evidencia guardada en $dest" -ForegroundColor Green }
else { Write-Host 'No se generó salida (¿contraseña incorrecta o conexión cancelada?).' -ForegroundColor Yellow }
