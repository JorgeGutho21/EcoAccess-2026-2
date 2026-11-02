<#
.SYNOPSIS
    Detiene únicamente los procesos iniciados por Iniciar-Demostracion.ps1, comprobando su identidad.
#>
[CmdletBinding()]
param()
$control = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3\Desarrollo\sesion.json'
if (-not (Test-Path $control)) { Write-Output 'No hay una sesión registrada.'; return }
$sesion = Get-Content $control -Raw | ConvertFrom-Json
foreach ($r in $sesion.Procesos) {
    $p = Get-Process -Id $r.IdProceso -ErrorAction SilentlyContinue
    if ($p -and $p.StartTime.ToUniversalTime().Ticks -eq $r.InicioTicks) { Stop-Process -Id $r.IdProceso; Write-Output "Detenido $($r.Componente)." }
}
Remove-Item $control
