<#
.SYNOPSIS
    Lanzador único del bot: comprueba dependencias y configuración, aplica migraciones pendientes e inicia los servicios.
.DESCRIPTION
    No reinstala nada ni borra datos. Si ya hay servicios en marcha se detiene sin iniciar otro consumidor del bot.
    Ejecútalo en tu propia ventana de PowerShell: solo ahí están tus credenciales. Para terminar: Detener-Demostracion.ps1.
.PARAMETER DispositivoSerial
    Usa el ESP32 conectado por USB en lugar de la caneca simulada. Requiere PuertoSerial en bridge.local.json y la
    alimentación y calibración verificadas según hardware\README.md.
.PARAMETER ConEscritorio
    Abre también la aplicación de escritorio.
#>
[CmdletBinding()]
param([switch] $DispositivoSerial, [switch] $ConEscritorio, [ValidateSet('Debug', 'Release')] [string] $Configuracion = 'Release')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$bot = Join-Path $raiz "src\EcoAccess.P3.Bot\bin\$Configuracion\net48\EcoAccess.P3.Bot.exe"
function Paso([string] $Texto) { Write-Host "-> $Texto" -ForegroundColor Cyan }

Paso 'Evitando procesos duplicados'
if (Get-Process EcoAccess.P3.Bot -ErrorAction SilentlyContinue) { throw 'Ya hay servicios de EcoAccess en marcha. Telegram admite un solo consumidor: ejecuta scripts\desarrollo\Detener-Demostracion.ps1 si quieres reiniciar.' }

Paso 'Comprobando Docker'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Falta Docker Desktop. Consulta docs\instalacion\11_instalacion.md.' }
$ErrorActionPreference = 'Continue'
& docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
$dockerOk = $LASTEXITCODE -eq 0
$ErrorActionPreference = 'Stop'
if (-not $dockerOk) { throw 'Docker no responde: abre Docker Desktop, espera a que termine de iniciar y repite.' }

if (-not (Test-Path $bot)) {
    Paso "Compilando ($Configuracion)"
    & dotnet build (Join-Path $raiz 'EcoAccess.P3.sln') -c $Configuracion -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'La compilación falló.' }
}

Paso 'Iniciando Oracle si está detenido'
$oracle = 'oracle-free-p3'
if (-not (& docker ps -q --filter "name=^$oracle$" --filter 'status=running')) { & docker start $oracle | Out-Null }
$limite = (Get-Date).AddMinutes(5)
while ((& docker inspect -f '{{.State.Health.Status}}' $oracle) -ne 'healthy') {
    if ((Get-Date) -gt $limite) { throw "Oracle no quedó listo en 5 minutos. Revisa: docker logs $oracle" }
    Start-Sleep -Seconds 5
}

Paso 'Aplicando migraciones pendientes (no borra datos)'
& powershell -ExecutionPolicy Bypass -File (Join-Path $raiz 'scripts\instalacion\Preparar-BaseDatos.ps1')
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron aplicar las migraciones.' }

$ErrorActionPreference = 'Continue'
Paso 'Comprobando la identidad del bot'
$telegram = & $bot --comprobar-telegram 2>&1 | ForEach-Object { "$_" }
$codigoTelegram = $LASTEXITCODE
Paso 'Comprobando el motor visual'
$vision = (& $bot --estado-vision 2>$null | Out-String) | ConvertFrom-Json
$ErrorActionPreference = 'Stop'
$telegram | ForEach-Object { Write-Host "   $_" }
if ($codigoTelegram -ne 0) { throw 'Telegram no está listo. Ejecuta scripts\instalacion\Configurar-Credenciales.ps1 -Telegram y repite.' }
Write-Host "   Vision:Modo = $($vision.Modo)"
if ($vision.Modo -ne 'Groq' -and -not $vision.Local.Disponible) { Write-Warning 'Ollama no responde: inícialo si quieres análisis local. Mientras tanto el bot ofrece selección manual.' }
if ($vision.Modo -ne 'Local' -and -not $vision.Groq.ClaveConfigurada) { Write-Warning 'Falta la clave de Groq: el servicio externo queda desactivado.' }

Paso 'Iniciando los servicios'
$argumentos = @{ Configuracion = $Configuracion; ConBot = $true; SinEscritorio = (-not $ConEscritorio); DispositivoSerial = $DispositivoSerial }
& (Join-Path $raiz 'scripts\desarrollo\Iniciar-Demostracion.ps1') @argumentos

Write-Host "`nListo. Abre @EcoAccessP3_JG_bot en tu teléfono y envía /start." -ForegroundColor Green
Write-Host 'Estado:  powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Verificar-EcoAccess-Telegram.ps1'
Write-Host 'Detener: powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Detener-Demostracion.ps1'
