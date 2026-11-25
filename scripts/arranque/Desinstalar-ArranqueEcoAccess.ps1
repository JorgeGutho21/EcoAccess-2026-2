<#
.SYNOPSIS
    Quita la tarea programada «EcoAccess P3 - arranque de la estación» y la copia del script de inicio
    (%LOCALAPPDATA%\EcoAccessP3\Arranque). No detiene los servicios que estén corriendo, salvo con -DetenerServicios.
.DESCRIPTION
    No toca la configuración privada (User Secrets), los registros de %ProgramData%\EcoAccessP3Framework ni la tarea
    anterior «EcoAccess P3 - vigilante del bot». Admite -WhatIf.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Desinstalar-ArranqueEcoAccess.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\arranque\Desinstalar-ArranqueEcoAccess.ps1 -DetenerServicios
#>
[CmdletBinding(SupportsShouldProcess)]
param([switch] $DetenerServicios)
$ErrorActionPreference = 'Stop'
$nombre = 'EcoAccess P3 - arranque de la estación'
$base = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'EcoAccessP3Framework'
$log = Join-Path $base 'Sesiones\arranque-ecoaccess.log'

if (Get-ScheduledTask -TaskName $nombre -ErrorAction SilentlyContinue) {
    if ($PSCmdlet.ShouldProcess($nombre, 'Quitar la tarea programada')) {
        Unregister-ScheduledTask -TaskName $nombre -Confirm:$false
        Write-Host "Tarea quitada: $nombre"
        try { Add-Content -LiteralPath $log -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Tarea de arranque desinstalada por $env:USERNAME." -Encoding UTF8 } catch { }
    }
} else { Write-Host 'La tarea no estaba registrada.' }

$copia = Join-Path $env:LOCALAPPDATA 'EcoAccessP3\Arranque'
if ((Test-Path -LiteralPath $copia) -and $PSCmdlet.ShouldProcess($copia, 'Borrar la copia del script de inicio')) {
    Remove-Item -LiteralPath $copia -Recurse -Force
    Write-Host "Copia del script de inicio borrada: $copia"
}
$pausa = Join-Path $base 'Arranque\pausa'
if ((Test-Path -LiteralPath $pausa) -and $PSCmdlet.ShouldProcess($pausa, 'Quitar la pausa (ya no hay tarea que pausar)')) { Remove-Item -LiteralPath $pausa -Force }

if ($DetenerServicios) { & (Join-Path $PSScriptRoot 'Detener-ServiciosEcoAccess.ps1') -WhatIf:$WhatIfPreference }
else { Write-Host 'Los servicios en ejecución siguen activos. Para detenerlos: Detener-ServiciosEcoAccess.ps1.' }
