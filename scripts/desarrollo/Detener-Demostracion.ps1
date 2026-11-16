<#
.SYNOPSIS
    Detiene únicamente los procesos iniciados por Iniciar-Demostracion.ps1, comprobando su identidad.
#>
[CmdletBinding()]
param()
$control = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Desarrollo\sesion.json'
if (-not (Test-Path $control)) { Write-Output 'No hay una sesión registrada.'; return }
# El supervisor se detiene primero: si no, volvería a iniciar lo que se está deteniendo.
$supervisor = Join-Path (Split-Path -Parent $control) 'supervisor.json'
if (Test-Path $supervisor) {
    $datos = Get-Content $supervisor -Raw | ConvertFrom-Json
    $s = Get-CimInstance Win32_Process -Filter "ProcessId=$($datos.Proceso)" -ErrorAction SilentlyContinue
    if ($s -and $s.CommandLine -match '--supervisar') { Stop-Process -Id $datos.Proceso -ErrorAction SilentlyContinue; Write-Output 'Detenido el supervisor.' }
    Remove-Item $supervisor -ErrorAction SilentlyContinue
}
$sesion = Get-Content $control -Raw | ConvertFrom-Json
foreach ($r in $sesion.Procesos) {
    $p = Get-Process -Id $r.IdProceso -ErrorAction SilentlyContinue
    if ($p -and $p.StartTime.ToUniversalTime().Ticks -eq $r.InicioTicks) { Stop-Process -Id $r.IdProceso; Write-Output "Detenido $($r.Componente)." }
}
Remove-Item $control
