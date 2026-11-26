<#
.SYNOPSIS
    Accesos ECOACCESS WEB y ECOACCESS DOS CLICS del ANFITRIÓN: abren la página pública ya vinculada a la estación física.
.DESCRIPTION
    Pide a la pasarela local (puerto de operador 127.0.0.1:8093, solo este equipo) un enlace con el permiso del navegador
    del anfitrión (12 h) y lo abre en el navegador predeterminado. Así, tocar una caneca, escribir o hablar abre la tapa
    real por el mismo controlador que usan Telegram y la app. No invalida el QR de los celulares.
    Si la pasarela o el túnel aún no están listos (p. ej. recién encendido el equipo) espera hasta 60 s; si no responden,
    abre la página sin vínculo (consulta y vista ilustrativa) y lo avisa.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Abrir-WebEstacion.ps1 -Destino dos-clics
#>
[CmdletBinding()]
param([ValidateSet('web', 'dos-clics')] [string] $Destino = 'web')
$ErrorActionPreference = 'Stop'
$publica = 'https://jorgegutho21.github.io/EcoAccess-2026-2/' + $(if ($Destino -eq 'dos-clics') { 'dos-clics/' } else { '' })
$enlace = $null; $motivo = 'la pasarela de la estación no respondió'
for ($i = 0; $i -lt 30 -and -not $enlace; $i++) {
    try {
        $r = Invoke-RestMethod -Uri 'http://127.0.0.1:8093/enlace-anfitrion' -Method Post -TimeoutSec 5 `
            -Headers @{ 'X-EcoAccess-Operador' = '1' } -ContentType 'application/json' -Body (@{ destino = $Destino } | ConvertTo-Json -Compress)
        if ($r.ok -and $r.enlace) { $enlace = [string]$r.enlace }
    } catch {
        $codigo = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        $motivo = if ($codigo -eq 503) { 'el túnel HTTPS aún no está listo' } else { 'la pasarela de la estación no respondió' }
    }
    if (-not $enlace) { Start-Sleep -Seconds 2 }
}
if ($enlace) { Start-Process $enlace; return }
Add-Type -AssemblyName System.Windows.Forms
Start-Process $publica
[System.Windows.Forms.MessageBox]::Show("Se abrió la página sin vínculo con la estación porque $motivo.`n`nRevisa que los servicios estén en marcha (scripts\arranque\Estado-ServiciosEcoAccess.ps1) y vuelve a abrir el acceso.", 'ECOACCESS', 'OK', 'Warning') | Out-Null
