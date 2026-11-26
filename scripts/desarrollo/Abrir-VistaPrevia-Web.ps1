<#
.SYNOPSIS
    Vista previa local de ECOACCESS WEB (versión de esta rama) sin publicarla en GitHub Pages.
.DESCRIPTION
    Sirve web\ciudadano en http://localhost:5173 (origen ya permitido por la pasarela) y abre el navegador en modo
    demostración (?demo): animaciones y temporizadores simulados, rotulados como tales, sin mover ninguna caneca.
    Sin -Demo abre el modo consulta (clasificación real por texto y voz). Para cerrarla, cierra la ventana de Python.
    No es un acceso directo permanente: el escritorio conserva solo ECOACCESS P3, ECOACCESS ESP32 y ECOACCESS WEB.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Abrir-VistaPrevia-Web.ps1
#>
param([switch] $SinDemo, [int] $Puerto = 5173)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sitio = Join-Path $raiz 'web\ciudadano'
$python = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $python) { throw 'Falta Python 3 (https://www.python.org) para servir la vista previa.' }
$activo = $false
try { $activo = (Invoke-WebRequest "http://127.0.0.1:$Puerto/" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200 } catch { }
if (-not $activo) {
    Start-Process -FilePath $python -ArgumentList '-m', 'http.server', $Puerto, '--bind', '127.0.0.1' -WorkingDirectory $sitio -WindowStyle Minimized | Out-Null
    Start-Sleep -Seconds 2
}
Start-Process ("http://localhost:$Puerto/" + $(if ($SinDemo) { '' } else { '?demo' }))
Write-Host "Vista previa de ECOACCESS WEB en http://localhost:$Puerto/ (rama local; la página pública no cambia)."
