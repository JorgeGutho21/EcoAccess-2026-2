<#
.SYNOPSIS
    Lo que hace el acceso directo «ECOACCESS ESP32»: inicia (si no está activo) el panel web de las tres
    canecas y lo abre en el navegador. No habilita la sesión supervisada ni mueve ningún motor.
.DESCRIPTION
    El panel (src\EcoAccess.P3.PanelCanecas) es el único dueño del puerto del ESP32 mientras está abierto y requiere el
    firmware hardware\CalibracionTresCanecas. Registro: %ProgramData%\EcoAccessP3Framework\Panel\panel.*.log
#>
$ErrorActionPreference = 'Continue'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$exe = Join-Path $raiz 'src\EcoAccess.P3.PanelCanecas\bin\Release\net48\EcoAccess.P3.PanelCanecas.exe'
$log = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework\Panel'
New-Item -ItemType Directory -Force $log | Out-Null
$url = 'http://localhost:8090/'
$activo = $false
try { $activo = [bool](Invoke-RestMethod ($url + 'api/historial') -TimeoutSec 2) } catch { }
if (-not $activo) {
    if (-not (Test-Path -LiteralPath $exe)) {
        & dotnet build (Join-Path $raiz 'src\EcoAccess.P3.PanelCanecas\EcoAccess.P3.PanelCanecas.csproj') -c Release -v q --nologo | Out-Null
    }
    Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $log 'panel.salida.log') -RedirectStandardError (Join-Path $log 'panel.error.log') | Out-Null
    for ($i = 0; $i -lt 20 -and -not $activo; $i++) { Start-Sleep -Milliseconds 500; try { $activo = [bool](Invoke-RestMethod ($url + 'api/historial') -TimeoutSec 2) } catch { } }
}
Start-Process $url
