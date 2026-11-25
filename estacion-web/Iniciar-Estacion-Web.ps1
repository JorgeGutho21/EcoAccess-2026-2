<# Inicia la estacion web de canecas solo en este equipo (127.0.0.1).
   -Modo simulado : demostracion, ninguna tapa fisica se mueve (predeterminado).
   -Modo bot      : reenvia cada apertura al servicio local de EcoAccess (-UrlAperturas), que crea la orden en Oracle;
                    el bridge y el ESP32 (firma HMAC) mueven la tapa. El navegador nunca habla con el ESP32. #>
[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)][int]$Puerto = 4173,
    [ValidateSet('simulado', 'bot')][string]$Modo = 'simulado',
    [string]$UrlAperturas = '',
    [string]$FueraDeServicio = ''
)

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Se necesita Node.js para iniciar la estacion web.' }
if (-not (Test-Path (Join-Path $raiz 'dist\index.html'))) { throw 'No se encontro dist\index.html.' }
if ($Modo -eq 'bot' -and -not $UrlAperturas) { $UrlAperturas = 'http://127.0.0.1:8190/aperturas/' }
if ($Modo -eq 'bot' -and $UrlAperturas -notmatch '^http://127\.0\.0\.1:\d+/') { throw 'En modo bot indica -UrlAperturas http://127.0.0.1:<puerto>/<ruta>/' }

$ocupado = Get-NetTCPConnection -LocalPort $Puerto -State Listen -ErrorAction SilentlyContinue
if ($ocupado) {
    Write-Host "El puerto $Puerto ya esta en uso; se abre la pagina que ya corre ahi." -ForegroundColor Yellow
} else {
    $env:ECOACCESS_WEB_PORT = "$Puerto"
    $env:ECOACCESS_MODO = $Modo
    $env:ECOACCESS_URL_APERTURAS = $UrlAperturas
    $env:ECOACCESS_FUERA_SERVICIO = $FueraDeServicio
    Start-Process -FilePath 'node' -ArgumentList 'server-local.js' -WorkingDirectory $raiz -WindowStyle Hidden
    Start-Sleep -Milliseconds 750
}
$url = "http://localhost:$Puerto"
Start-Process $url
Write-Host "EcoAccess Estacion abierta en $url (modo $Modo)" -ForegroundColor Green
